using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;
using Nito.AsyncEx;
using Newtonsoft.Json.Linq;

namespace Benchmarker.Models
{
	public class RunSet
	{
		PostgresRow postgresRow;

		List<Result> results;
		public List<Result> Results { get { return results; } }
		public DateTime StartDateTime { get; set; }
		public DateTime FinishDateTime { get; set; }
		public Config Config { get; set; }
		public Commit Commit { get; set; }
		public List<Commit> SecondaryCommits { get; set; }
		public string BuildURL { get; set; }
		public string LogURL { get; set; }
		public string PullRequestURL { get; set; }
		public long? PullRequestBaselineRunSetId { get; set; }
		public List<string> TimedOutBenchmarks { get; set; }
		public List<string> CrashedBenchmarks { get; set; }

		public RunSet ()
		{
			results = new List<Result> ();
			TimedOutBenchmarks = new List<string> ();
			CrashedBenchmarks = new List<string> ();
		}

		public IEnumerable<Result.Run> AllRuns {
			get {
				return Results.SelectMany (res => res.Runs);
			}
		}

		public static bool MachineExistsInPostgres (NpgsqlConnection conn, Machine machine)
		{
			using (var cmd = conn.CreateCommand ()) {
				cmd.CommandText = "select architecture, isDedicated from machine where name = :name";
				cmd.Parameters.Add (new NpgsqlParameter ("name", NpgsqlTypes.NpgsqlDbType.Varchar)).Value = machine.Name;

				using (var reader = cmd.ExecuteReader ()) {
					if (!reader.Read ())
						return false;

					if (reader.GetString (0) != machine.Architecture)
						throw new Exception (string.Format ("Error: Machine {0} exists but is not the same as the local machine.", machine.Name));

					return true;
				}
			}
		}

		static string GetOrUploadMachineToPostgres (NpgsqlConnection conn, Machine machine)
		{
			if (MachineExistsInPostgres (conn, machine))
				return machine.Name;

			Logging.GetLogging ().Info ("machine " + machine.Name + " not found - inserting");

			using (var cmd = conn.CreateCommand ()) {
				cmd.CommandText = "insert into machine (name, architecture, isDedicated) values (:name, :arch, :dedicated)";
				cmd.Parameters.Add (new NpgsqlParameter ("name", NpgsqlTypes.NpgsqlDbType.Varchar)).Value = machine.Name;
				cmd.Parameters.Add (new NpgsqlParameter ("arch", NpgsqlTypes.NpgsqlDbType.Varchar)).Value = machine.Architecture;
				cmd.Parameters.Add (new NpgsqlParameter ("dedicated", NpgsqlTypes.NpgsqlDbType.Boolean)).Value = false;

				if (cmd.ExecuteNonQuery () != 1)
					throw new Exception ("Error: Failure inserting machine " + machine.Name);
			}

			return machine.Name;
		}

		public static RunSet FromId (NpgsqlConnection conn, Machine machine, long id, Config config, Commit mainCommit, List<Commit> secondaryCommits, string buildURL, string logURL)
		{
			var whereValues = new PostgresRow ();
			whereValues.Set ("id", NpgsqlTypes.NpgsqlDbType.Integer, id);
			var row = PostgresInterface.Select (conn, new Dictionary<string, string> { ["RunSet"] = "rs", ["Config"] = "c", ["Machine"] = "m" },
				new string[] {
					"rs.id",
					"rs.startedAt",
					"rs.finishedAt",
					"rs.buildURL",
					"rs.commit",
					"rs.timedOutBenchmarks",
					"rs.crashedBenchmarks",
					"rs.secondaryCommits",
					"rs.logURLs",
					"m.name",
					"m.architecture",
					"c.name",
					"c.monoExecutable",
					"c.monoEnvironmentVariables",
					"c.monoOptions"
					},
				"rs.id = :id and rs.config = c.name and rs.machine = m.name", whereValues).First ();

			var runSet = new RunSet {
				postgresRow = row,
				StartDateTime = row.GetValue<DateTime> ("rs.startedAt").Value,
				FinishDateTime = row.GetValue<DateTime> ("rs.finishedAt").Value,
				BuildURL = row.GetReference<string> ("rs.buildURL"),
				LogURL = logURL,
				Config = config,
				Commit = mainCommit,
				SecondaryCommits = secondaryCommits,
				TimedOutBenchmarks = row.GetReference<string[]> ("rs.timedOutBenchmarks").ToList (),
				CrashedBenchmarks = row.GetReference<string[]> ("rs.crashedBenchmarks").ToList ()
			};

			if (mainCommit.Hash != row.GetReference<string> ("rs.commit"))
				throw new Exception (String.Format ("Commit ({0}) does not match the one in the database ({1}).", mainCommit.Hash, row.GetReference<string> ("rs.commit")));
			if (buildURL != null && buildURL != runSet.BuildURL)
				throw new Exception ("Build URL does not match the one in the database.");
			if (machine.Name != row.GetReference<string> ("m.name") || machine.Architecture != row.GetReference<string> ("m.architecture"))
				throw new Exception ("Machine does not match the one in the database.");
			if (!config.EqualsPostgresObject (row, "c."))
				throw new Exception ("Config does not match the one in the database.");

			if (secondaryCommits != null) {
				var secondaryHashes = row.GetReference<string[]> ("rs.secondaryCommits") ?? new string[] { };
				if (secondaryHashes.Length != secondaryCommits.Count || secondaryCommits.Any (c => !secondaryHashes.Contains (c.Hash)))
					throw new Exception ("Secondary commits do not match the ones in the database.");
			}

			return runSet;
		}

		public Tuple<long, long?> UploadToPostgres (NpgsqlConnection conn, Machine machine)
		{
			// FIXME: for amended run sets, delete existing runs of benchmarks we just ran

			var logURLs = new Dictionary<string, string> ();

			if (postgresRow != null) {
				var originalLogURLs = postgresRow.GetReference<JObject> ("rs.logURLs");
				foreach (var p in originalLogURLs.Properties ())
					logURLs [p.Name] = p.Value.Value<string> ();
			}

			if (LogURL != null) {
				string defaultURL;
				logURLs.TryGetValue ("*", out defaultURL);
				if (defaultURL == null) {
					logURLs ["*"] = LogURL;
				} else if (defaultURL != LogURL) {
					foreach (var result in results)
						logURLs [result.Benchmark.Name] = LogURL;
				}
			}

			var row = new PostgresRow ();
			long? prId = null;

			if (postgresRow == null) {
				row.Set ("machine", NpgsqlTypes.NpgsqlDbType.Varchar, GetOrUploadMachineToPostgres (conn, machine));
				row.Set ("config", NpgsqlTypes.NpgsqlDbType.Varchar, Config.GetOrUploadToPostgres (conn));
				row.Set ("commit", NpgsqlTypes.NpgsqlDbType.Varchar, Commit.GetOrUploadToPostgres (conn));
				var secondaryHashes = new List<string> ();
				foreach (var commit in SecondaryCommits)
					secondaryHashes.Add (commit.GetOrUploadToPostgres (conn));
				row.Set ("secondaryCommits", NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Varchar, secondaryHashes);
				row.Set ("buildURL", NpgsqlTypes.NpgsqlDbType.Varchar, BuildURL);
				row.Set ("startedAt", NpgsqlTypes.NpgsqlDbType.TimestampTZ, StartDateTime);

				if (PullRequestURL != null) {
					var prRow = new PostgresRow ();
					prRow.Set ("URL", NpgsqlTypes.NpgsqlDbType.Varchar, PullRequestURL);
					prRow.Set ("baselineRunSet", NpgsqlTypes.NpgsqlDbType.Integer, PullRequestBaselineRunSetId);
					prId = PostgresInterface.Insert<long> (conn, "PullRequest", prRow, "id");
					row.Set ("pullRequest", NpgsqlTypes.NpgsqlDbType.Integer, prId);
				}
			} else {
				row.TakeValuesFrom (postgresRow, "rs.");
			}

			row.Set ("finishedAt", NpgsqlTypes.NpgsqlDbType.TimestampTZ, FinishDateTime);

			row.Set ("logURLs", NpgsqlTypes.NpgsqlDbType.Jsonb, logURLs);

			row.Set ("timedOutBenchmarks", NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Varchar, TimedOutBenchmarks);
			row.Set ("crashedBenchmarks", NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Varchar, CrashedBenchmarks);

			Logging.GetLogging ().Info ("uploading run set");

			long runSetId;
			if (postgresRow == null) {
				runSetId = PostgresInterface.Insert<long> (conn, "RunSet", row, "id");
			} else {
				PostgresInterface.Update (conn, "RunSet", row, "id");
				runSetId = postgresRow.GetValue<long> ("rs.id").Value;
			}

			Logging.GetLogging ().Info ("uploading runs");

			foreach (var result in results) {
				if (result.Config != Config)
					throw new Exception ("Results must have the same config as their RunSets");
				result.UploadRunsToPostgres (conn, runSetId);
			}

			Logging.GetLogging ().Info ("done uploading");

			return Tuple.Create (runSetId, prId);
		}
	}
}

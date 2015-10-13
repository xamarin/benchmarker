using Benchmarker;
using bm = Benchmarker.Models;
using System;
using System.Text.RegularExpressions;
using Common.Logging.Simple;
using Common.Logging;
using Npgsql;
using System.Collections.Generic;
using Xamarin.TestCloud.Api.V0;
using Nito.AsyncEx;

namespace xtclog
{
	class MainClass
	{
		static void UsageAndExit (bool success)
		{
			Console.WriteLine ("Usage:");
			Console.WriteLine ("    xtcloghelper.exe --push XTCJOBID RUNSETID");
			Console.WriteLine ("                     --crawl-logs");
			Environment.Exit (success ? 0 : 1);
		}

		public static void Main (string[] args)
		{
			LogManager.Adapter = new ConsoleOutLoggerFactoryAdapter ();
			Logging.SetLogging (LogManager.GetLogger<MainClass> ());

			if (args.Length == 0)
				UsageAndExit (true);

			if (args [0] == "--push") {
				if (args.Length <= 2) {
					UsageAndExit (false);
				}
				string xtcJobGuid = args [1];
				long runSetId = Int64.Parse(args [2]);
				var connection = PostgresInterface.Connect ();
				PushXTCJobId (connection, xtcJobGuid, runSetId);
			} else if (args [0] == "--crawl-logs") {
				var connection = PostgresInterface.Connect ();
				var xtcapikey = Accredit.GetCredentials ("xtcapikey") ["xtcapikey"].ToString ();
				var xtcapi = new Client (xtcapikey);

				foreach (var xtcjobtuple in PullXTCJobIds (connection)) {
					long xtcjobid = xtcjobtuple.Item1;
					string xtcjobguid = xtcjobtuple.Item2;
					long runsetid = xtcjobtuple.Item3;
					Console.WriteLine ("XTC Job ID Pending: " + xtcjobtuple);
					var guid = Guid.Parse (xtcjobguid);
					Console.WriteLine ("guid: \"{0}\"", guid);
					ResultCollection results = AsyncContext.Run (() => xtcapi.TestRuns.Results (guid));

					if (!results.Finished) {
						Console.WriteLine ("Job \"{0}\" not finished yet, skip processing", xtcjobguid);
						continue;
					}

					if (results.Logs.Devices.Count > 1) {
						Console.WriteLine ("found more than one device in logs, not supported: " + results.Logs.Devices);
						Environment.Exit (2);
					}
					foreach (var device in results.Logs.Devices) {
						Console.WriteLine ("device id: " + device.DeviceConfigurationId);
						Console.WriteLine ("devicelog url: " + device.DeviceLog);

						var tuple = ProcessLog (connection, device.DeviceLog, runsetid);
						tuple.Item1.UploadToPostgres (connection, tuple.Item2);
					}
					DeleteXTCJobId (connection, xtcjobid);
				}
			} else {
				UsageAndExit (false);
			}
		}

		private static void PushXTCJobId (NpgsqlConnection conn, string xtcJobGuid, long runSetId)
		{
			PostgresRow row = new PostgresRow ();
			row.Set ("job", NpgsqlTypes.NpgsqlDbType.Varchar, xtcJobGuid);
			row.Set ("runSet", NpgsqlTypes.NpgsqlDbType.Bigint, runSetId);
			PostgresInterface.Insert<long> (conn, "XamarinTestcloudJobIDs", row, "id");
		}

		private static void DeleteXTCJobId (NpgsqlConnection conn, long xtcJobId)
		{
			PostgresRow row = new PostgresRow ();
			row.Set ("id", NpgsqlTypes.NpgsqlDbType.Bigint, xtcJobId);
			PostgresInterface.Delete (conn, "XamarinTestcloudJobIDs", "id = :id", row);
		}

		private static List<Tuple<long, string, long>> PullXTCJobIds (NpgsqlConnection conn)
		{
			var l = new List<Tuple<long, string, long>> ();
			foreach (var s in PostgresInterface.Select (conn, "XamarinTestcloudJobIDs", new string[] {"id", "job", "runset"}, null, null)) {
				long? dbid = s.GetValue<long> ("id");
				long? runset = s.GetValue<long> ("runset");
				if (dbid.HasValue && runset.HasValue) {
					l.Add (new Tuple<long, string, long> (dbid.Value, s.GetReference<string> ("job"), runset.Value));
				} else {
					Console.WriteLine ("error: invalid id in database");
					Environment.Exit (3);
				}
			}
			return l;
		}

		private static string DownloadLog (string url)
		{
			using (var wc = new System.Net.WebClient ()) {
				return wc.DownloadString (url);
			}
		}

		public const int BENCHMARK_ITERATIONS = 10;

		static bm.Commit ParseCommit (string log)
		{
			string regex_commit = @"I\/benchmarker\(\s*\d+\): Benchmarker \| commit ""(?<hash>[0-9A-Za-z]{40})"" on branch ""(?<branch>[\w\-_\.]+)""";
			Match match_commit = Regex.Match (log, regex_commit);
			var commit = new bm.Commit ();
			commit.Hash = match_commit.Groups ["hash"].Value;
			commit.Branch = match_commit.Groups ["branch"].Value;
			return commit;
		}

		static bm.Machine ParseMachine (string log)
		{
			string regex_machine = @"I\/benchmarker\(\s*\d+\): Benchmarker \| hostname ""(?<hostname>[\w\s\.]+)"" architecture ""(?<architecture>[\w\-]+)""";
			Match match_machine = Regex.Match (log, regex_machine);
			var machine = new bm.Machine {
				Name = match_machine.Groups ["hostname"].Value,
				Architecture = match_machine.Groups ["architecture"].Value
			};
			Console.WriteLine ("machine name: " + machine.Name);
			return machine;
		}

		static bm.Config ParseConfig (string log)
		{
			string regex_config = @"I\/benchmarker\(\s*\d+\): Benchmarker \| configname ""(?<name>[\w\-\.]+)""";
			Match match_config = Regex.Match (log, regex_config);
			return new bm.Config {
				Name = match_config.Groups ["name"].Value,
				Mono = String.Empty,
				MonoOptions = new string[0],
				MonoEnvironmentVariables = new Dictionary<string, string> (),
				Count = BENCHMARK_ITERATIONS
			};
		}

		static Dictionary<string, List<TimeSpan>> ParseRuns (string log)
		{
			string regex_runs = @"I\/benchmarker\(\s*\d+\): Benchmarker \| Benchmark ""(?<name>[\w\-_\d]+)"": finished iteration (?<iteration>\d+), took (?<time>\d+)ms";
			Dictionary<string, List<TimeSpan>> bench_results = new Dictionary<string, List<TimeSpan>> ();
			foreach (Match match_run in Regex.Matches (log, regex_runs)) {
				string name = match_run.Groups ["name"].Value;
				string iteration = match_run.Groups ["iteration"].Value;
				string time = match_run.Groups ["time"].Value;
				if (!bench_results.ContainsKey (name)) {
					bench_results.Add (name, new List<TimeSpan> ());
				}
				bench_results [name].Add (TimeSpan.FromMilliseconds (Int64.Parse (time)));
				Console.WriteLine ("{0}, Iteration #{1}: {2}ms", name, iteration, time);
			}
			return bench_results;
		}

		private static Tuple<bm.RunSet, bm.Machine> ProcessLog (NpgsqlConnection connection, string logUrl, long runSetId)
		{
			string log = DownloadLog (logUrl);

			var commit = ParseCommit (log);
			var machine = ParseMachine (log);
			var config = ParseConfig (log);

			var runSet = bm.RunSet.FromId (connection, machine, runSetId, config, commit, null, logUrl);

			var bench_results = ParseRuns (log);

			foreach (string benchmark in bench_results.Keys) {
				var result = new bm.Result {
					DateTime = DateTime.Now,
					Benchmark = new bm.Benchmark { Name = benchmark },
					Config = config
				};

				int count = 0;
				foreach (TimeSpan t in bench_results[benchmark]) {
					var run = new bm.Result.Run ();
					run.RunMetrics.Add (new bm.Result.RunMetric {
						Metric = bm.Result.RunMetric.MetricType.Time,
						Value = t
					});
					result.Runs.Add (run);
					count++;
				}
				runSet.Results.Add (result);
				if (count < BENCHMARK_ITERATIONS) {
					runSet.CrashedBenchmarks.Add (benchmark);
				}
			}

			return new Tuple<bm.RunSet, bm.Machine>(runSet, machine);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using Benchmarker;
using Common.Logging;
using Common.Logging.Simple;
using Nito.AsyncEx;
using Npgsql;
using Xamarin.TestCloud.Api.V0;
using bm = Benchmarker.Models;

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
				long runSetId = Int64.Parse (args [2]);
				var connection = PostgresInterface.Connect ();
				PushXTCJobId (connection, xtcJobGuid, runSetId);
				Console.WriteLine ("created an entry for runSet {0}", runSetId);
			} else if (args [0] == "--crawl-logs") {
				var connection = PostgresInterface.Connect ();
				var xtcapikey = Accredit.GetCredentials ("xtcapikey") ["xtcapikey"].ToString ();
				var xtcapi = new Client (xtcapikey);

				foreach (var xtcjobtuple in PullXTCJobIds (connection)) {
					long xtcjobid = xtcjobtuple.Item1;
					string xtcjobguid = xtcjobtuple.Item2;
					long runsetid = xtcjobtuple.Item3;
					DateTime startedAt = xtcjobtuple.Item4;
					TimeSpan timeDiff = DateTime.Now - startedAt;

					Console.WriteLine ("XTC Job ID Pending: " + xtcjobtuple);
					var guid = Guid.Parse (xtcjobguid);
					Console.WriteLine ("guid: \"{0}\"", guid);
					ResultCollection results = AsyncContext.Run (() => xtcapi.TestRuns.Results (guid));


					if (!results.Finished /* && timeDiff.Days < 3 */) {
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

						var tuple = ProcessLog (connection, device.DeviceLog, runsetid, xtcjobguid);
						tuple.Item1.UploadToPostgres (connection, tuple.Item2);
					}
					DeleteXTCJobId (connection, xtcjobid);
				}
				Console.WriteLine ("done processing the queue");
			} else {
				UsageAndExit (false);
			}
		}

		private static void PushXTCJobId (NpgsqlConnection conn, string xtcJobGuid, long runSetId)
		{
			PostgresRow row = new PostgresRow ();
			row.Set ("job", NpgsqlTypes.NpgsqlDbType.Varchar, xtcJobGuid);
			row.Set ("runSet", NpgsqlTypes.NpgsqlDbType.Bigint, runSetId);
			row.Set ("startedAt", NpgsqlTypes.NpgsqlDbType.TimestampTZ, DateTime.Now);
			PostgresInterface.Insert<long> (conn, "XamarinTestcloudJobIDs", row, "id");
		}

		private static void DeleteXTCJobId (NpgsqlConnection conn, long xtcJobId)
		{
			PostgresRow row = new PostgresRow ();
			row.Set ("id", NpgsqlTypes.NpgsqlDbType.Bigint, xtcJobId);
			PostgresInterface.Delete (conn, "XamarinTestcloudJobIDs", "id = :id", row);
		}

		private static List<Tuple<long, string, long, DateTime>> PullXTCJobIds (NpgsqlConnection conn)
		{
			var l = new List<Tuple<long, string, long, DateTime>> ();
			foreach (var s in PostgresInterface.Select (conn, "XamarinTestcloudJobIDs", new string[] {"id", "job", "runset", "startedAt"}, null, null)) {
				long? dbid = s.GetValue<long> ("id");
				long? runset = s.GetValue<long> ("runset");
				string job = s.GetReference<string> ("job");
				DateTime? startedAt = s.GetValue<DateTime> ("startedAt");
				if (dbid.HasValue && runset.HasValue && startedAt.HasValue) {
					l.Add (new Tuple<long, string, long, DateTime> (dbid.Value, job, runset.Value, startedAt.Value));
				} else {
					Console.WriteLine ("error: invalid id in database");
					Environment.Exit (3);
				}
			}
			return l;
		}

		private const int ZIP_LEAD_BYTES = 0x04034b50;

		private static bool hasZipFileHeader (byte[] data)
		{
			Debug.Assert (data != null && data.Length >= 4);
			return (BitConverter.ToInt32 (data, 0) == ZIP_LEAD_BYTES);
		}

		private static List<string> DownloadLogs (string url)
		{
			using (var wc = new System.Net.WebClient ()) {
				var logs = new List<string> ();
				byte[] data = wc.DownloadData (url);

				// when tests are chunked, XTC API returns a ZIP file containing all logs instead of a single log file.
				if (hasZipFileHeader (data)) {
					foreach (var file in new ZipArchive (new MemoryStream (data)).Entries) {
						logs.Add (new StreamReader(file.Open ()).ReadToEnd ());
					}
				} else {
					logs.Add (System.Text.Encoding.UTF8.GetString (data));
				}
				return logs;
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

		private static string XTC_UI_PREFIX = "https://testcloud.xamarin.com/test/androidagent_";

		private static Tuple<bm.RunSet, bm.Machine> ProcessLog (NpgsqlConnection connection, string logUrl, long runSetId, string jobguid)
		{
			List<string> logs = DownloadLogs (logUrl);

			var commit = ParseCommit (logs [0]);
			var machine = ParseMachine (logs [0]);
			var config = ParseConfig (logs [0]);

			var runSet = bm.RunSet.FromId (connection, machine, runSetId, config, commit, null, null, XTC_UI_PREFIX + jobguid);

			Dictionary<string, List<TimeSpan>> bench_results = new Dictionary<string, List<TimeSpan>> ();
			foreach (string log in logs) {
				var tmp = ParseRuns (log);
				foreach (string benchmark in tmp.Keys) {
					bench_results [benchmark] = tmp [benchmark];
				}
			}

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

			return new Tuple<bm.RunSet, bm.Machine> (runSet, machine);
		}
	}
}

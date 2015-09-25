using System;
using System.Threading.Tasks;
using Npgsql;
using Nito.AsyncEx;
using Benchmarker;
using Benchmarker.Common;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Common.Logging.Simple;
using Common.Logging;

namespace DbTool
{
	class MainClass
	{
		static string SlackHooksUrl;

		static Dictionary<long, IDictionary<string, double[]>> resultsForRunSetId = new Dictionary<long, IDictionary<string, double[]>> ();

		static IDictionary<string, double[]> FetchResultsForRunSet (NpgsqlConnection conn, long runSetId)
		{
			if (!resultsForRunSetId.ContainsKey (runSetId)) {
				var whereValues = new PostgresRow ();
				whereValues.Set ("runSet", NpgsqlTypes.NpgsqlDbType.Bigint, runSetId);
				whereValues.Set ("metric", NpgsqlTypes.NpgsqlDbType.Varchar, "time");
				var rows = PostgresInterface.Select (conn, "\"1\".results",
					           new string[] {
						"benchmark",
						"results"
					},
					           "runSet = :runSet and metric = :metric and disabled is not true",
					           whereValues);
				var dict = new Dictionary<string, double[]> ();
				foreach (var row in rows)
					dict [row.GetReference<string> ("benchmark")] = row.GetReference<double[]> ("results");
				resultsForRunSetId [runSetId] = dict;
			}
			return resultsForRunSetId [runSetId];
		}

		static async Task MakePostRequest (string RequestUrl, MultipartFormDataContent Content)
		{
			var httpClient = new HttpClient ();

			var response = await httpClient.PostAsync (RequestUrl, Content);
			if (response.StatusCode != System.Net.HttpStatusCode.OK)
				throw new Exception ("HTTP response is not 200 OK");
		}

		static async Task SendSlackMessage (string text, string channel, string username, string iconEmoji)
		{
			var payload = new Dictionary<string, string> {
				{ "text",  text },
				{ "channel", channel },
				{ "username",  username },
				{ "icon_emoji", iconEmoji }
			};

			var form = new MultipartFormDataContent ();
			form.Add (new StringContent (JsonConvert.SerializeObject (payload)), "payload");

			await MakePostRequest (SlackHooksUrl, form);
		}

		static string SlackCommitString (string hash)
		{
			var url = "https://github.com/mono/mono/commit/" + hash;
			return "<" + url + "|" + hash + ">";
		}

		static async Task<IEnumerable<string>> WarnIfNecessary (bool testRun, List<string> benchmarksToWarn, List<string> warnedBenchmarks,
			bool faster, PostgresRow testRunSet, PostgresRow previousRunSet, string machineName, string configName)
		{
			var newlyWarned = benchmarksToWarn.Where (b => !warnedBenchmarks.Contains (b)).ToList ();
			if (newlyWarned.Count == 0)
				return new string[] { };

			var commit = testRunSet.GetReference<string> ("c_hash");
			var previousCommit = previousRunSet.GetReference<string> ("c_hash");

			string benchmarksString;
			if (newlyWarned.Count == 1) {
				benchmarksString = String.Format ("benchmark `{0}`", newlyWarned [0]);
			} else if (newlyWarned.Count == 2) {
				benchmarksString = String.Format ("benchmarks `{0}` and `{1}`", newlyWarned [0], newlyWarned [1]);
			} else {
				var allButLast = newlyWarned.GetRange (0, newlyWarned.Count - 1).Select (n => String.Format ("`{0}`", n));
				benchmarksString = String.Format ("benchmarks {0}, and `{1}`", String.Join (", ", allButLast), newlyWarned.Last ());
			}
			var timelineUrl = String.Format ("http://xamarin.github.io/benchmarker/front-end/index.html#machine={0}&config={1}",
				                  machineName, configName);
			var compareUrl = String.Format ("http://xamarin.github.io/benchmarker/front-end/compare.html#ids={0}+{1}",
				previousRunSet.GetValue<long> ("rs_id").Value,
				testRunSet.GetValue<long> ("rs_id").Value);
			var message = String.Format ("The {0} got {1} between commits {2} and {3} on <{4}|{5}> — <{6}|compare>",
							             benchmarksString, (faster ? "faster" : "slower"),
							             SlackCommitString (previousCommit), SlackCommitString (commit),
										 timelineUrl, testRunSet.GetReference<string> ("m_architecture"), compareUrl);
			var botName = faster ? "goodbot" : "badbot";
			if (testRun)
				Console.WriteLine ("{0}: {1}", botName, message);
			else
				await SendSlackMessage (message, "#performance-bots", botName, faster ? ":thumbsup:" : ":red_circle:");
			
			return newlyWarned;
		}

		static IDictionary<string, double> JsonMapToDictionary (JObject map)
		{
			var dict = new Dictionary<string, double> ();
			foreach (var p in map.Properties ())
				dict [p.Name] = p.Value.Value<double> ();
			return dict;
		}

		static void InsertWarned (NpgsqlConnection conn, long testRunSetId, string benchmark, bool faster)
		{
			PostgresRow row = new PostgresRow ();
			row.Set ("runSet", NpgsqlTypes.NpgsqlDbType.Bigint, testRunSetId);
			row.Set ("benchmark", NpgsqlTypes.NpgsqlDbType.Varchar, benchmark);
			row.Set ("faster", NpgsqlTypes.NpgsqlDbType.Boolean, faster);
			PostgresInterface.Insert<long> (conn, "RegressionsWarned", row, "id");
		}

		static async Task FindRegressions (NpgsqlConnection conn, string machineName, string configName, bool testRun, bool onlyNecessary)
		{
			const int baselineWindowSize = 5;
			const int testWindowSize = 3;
			const double controlLimitSize = 6;

			var summaryValues = new PostgresRow ();
			summaryValues.Set ("machine", NpgsqlTypes.NpgsqlDbType.Varchar, machineName);
			summaryValues.Set ("config", NpgsqlTypes.NpgsqlDbType.Varchar, configName);
			summaryValues.Set ("metric", NpgsqlTypes.NpgsqlDbType.Varchar, "time");
			var runSets = PostgresInterface.Select (conn, "\"1\".summary",
				new string[] {
					"rs_id",
					"rs_startedAt",
					"c_hash",
					"rs_timedOutBenchmarks",
					"rs_crashedBenchmarks",
					"c_hash",
					"c_commitDate",
					"m_architecture",
					"averages",
					"variances"
				},
				"m_name = :machine and cfg_name = :config and metric = :metric and rs_pullRequest is null",
				summaryValues);
			var sortedRunSets = runSets.ToList ();
			sortedRunSets.Sort ((a, b) => {
				var aCommitDate = a.GetValue<DateTime> ("c_commitDate").Value;
				var bCommitDate = b.GetValue<DateTime> ("c_commitDate").Value;
				var result = aCommitDate.CompareTo (bCommitDate);
				if (result != 0)
					return result;
				var aStartedDate = a.GetValue<DateTime> ("rs_startedAt").Value;
				var bStartedDate = b.GetValue<DateTime> ("rs_startedAt").Value;
				return aStartedDate.CompareTo (bStartedDate);
			});
			var lastWarningIndex = new Dictionary<string, int> ();
			for (var i = baselineWindowSize; i <= sortedRunSets.Count - testWindowSize; ++i) {
				var windowAverages = new Dictionary<string, double> ();
				var windowVariances = new Dictionary<string, double> ();
				var benchmarkCounts = new Dictionary<string, int> ();

				for (var j = 1; j <= baselineWindowSize; ++j) {
					var baselineRunSet = sortedRunSets [i - j];
					var averages = JsonMapToDictionary (baselineRunSet.GetReference<JObject> ("averages"));
					var variances = JsonMapToDictionary (baselineRunSet.GetReference<JObject> ("variances"));
					foreach (var kvp in averages) {
						var name = kvp.Key;
						var average = kvp.Value;
						var variance = variances [name];
						if (!windowAverages.ContainsKey (name)) {
							windowAverages [name] = 0.0;
							windowVariances [name] = 0.0;
							benchmarkCounts [name] = 0;
						}
						windowAverages [name] += average;
						windowVariances [name] += variance;
						benchmarkCounts [name] += 1;
					}					
				}

				foreach (var kvp in benchmarkCounts) {
					var name = kvp.Key;
					var count = kvp.Value;
					windowAverages [name] /= count;
					windowVariances [name] /= count; 
				}

				var testResults = new Dictionary<string, List<double>> ();
				for (var j = 0; j < testWindowSize; ++j) {
					var results = FetchResultsForRunSet (conn, sortedRunSets [i + j].GetValue<long> ("rs_id").Value);
					foreach (var kvp in results) {
						var benchmark = kvp.Key;
						if (!testResults.ContainsKey (benchmark))
							testResults.Add (benchmark, new List<double> ());
						testResults [benchmark].AddRange (kvp.Value);
					}
				}

				var testRunSet = sortedRunSets [i];

				var commitHash = testRunSet.GetReference<string> ("c_hash");
				var testRunSetId = testRunSet.GetValue<long> ("rs_id").Value;
				Console.WriteLine ("{0} {1}", testRunSetId, commitHash);

				var fasterBenchmarks = new List<string> ();
				var slowerBenchmarks = new List<string> ();

				foreach (var kvp in benchmarkCounts) {
					var benchmark = kvp.Key;
					if (kvp.Value < baselineWindowSize)
						continue;
					if (lastWarningIndex.ContainsKey (benchmark) && lastWarningIndex [benchmark] >= i - baselineWindowSize)
						continue;
					var average = windowAverages [benchmark];
					var variance = windowVariances [benchmark];
					var stdDev = Math.Sqrt (variance);
					var lowerControlLimit = average - controlLimitSize * stdDev;
					var upperControlLimit = average + controlLimitSize * stdDev;
					if (!testResults.ContainsKey (benchmark))
						continue;
					var results = testResults [benchmark];
					if (results.Count < 5)
						continue;
					var numOutliersFaster = 0;
					var numOutliersSlower = 0;
					foreach (var elapsed in results) {
						if (elapsed < lowerControlLimit)
							++numOutliersFaster;
						if (elapsed > upperControlLimit)
							++numOutliersSlower;
					}
					if (numOutliersFaster > results.Count * 3 / 4) {
						Console.WriteLine ("+ regression in {0}: {1}/{2}", benchmark, numOutliersFaster, results.Count);
						lastWarningIndex [benchmark] = i;
						fasterBenchmarks.Add (benchmark);
					} else if (numOutliersSlower > results.Count * 3 / 4) {
						Console.WriteLine ("- regression in {0}: {1}/{2}", benchmark, numOutliersSlower, results.Count);
						lastWarningIndex [benchmark] = i;
						slowerBenchmarks.Add (benchmark);
					}
					/*
					else if (numOutliersFaster == 0 && numOutliersSlower == 0) {
						Console.WriteLine ("  nothing in    {0}", name);
					} else {
						Console.WriteLine ("? suspected in  {0}: {1}-/{2}+/{3}", name, numOutliersSlower, numOutliersFaster, runs.Count);
					}
					*/
				}

				if (fasterBenchmarks.Count != 0 || slowerBenchmarks.Count != 0) {
					var warnedFasterBenchmarks = new List<string> ();
					var warnedSlowerBenchmarks = new List<string> ();

					if (onlyNecessary) {
						var warningValues = new PostgresRow ();
						warningValues.Set ("runset", NpgsqlTypes.NpgsqlDbType.Bigint, testRunSetId);
						var warnings = PostgresInterface.Select (conn, "RegressionsWarned",
							new string[] {
								"benchmark",
								"faster"
							},
							"runSet = :runset", warningValues);

						foreach (var row in warnings) {
							var benchmark = row.GetReference<string> ("benchmark");
							if (row.GetValue<bool> ("faster").Value)
								warnedFasterBenchmarks.Add (benchmark);
							else
								warnedSlowerBenchmarks.Add (benchmark);
						}
					}

					var previousRunSet = sortedRunSets [i - 1];
					var newlyWarnedFaster = await WarnIfNecessary (testRun, fasterBenchmarks, warnedFasterBenchmarks, true, testRunSet, previousRunSet, machineName, configName);
					var newlyWarnedSlower = await WarnIfNecessary (testRun, slowerBenchmarks, warnedSlowerBenchmarks, false, testRunSet, previousRunSet, machineName, configName);

					if (!testRun) {
						foreach (var benchmark in newlyWarnedFaster)
							InsertWarned (conn, testRunSetId, benchmark, true);
						foreach (var benchmark in newlyWarnedSlower)
							InsertWarned (conn, testRunSetId, benchmark, false);
					}
				}
			}
		}

		static void UsageAndExit (bool success)
		{
			Console.WriteLine ("Usage:");
			Console.WriteLine ("    DbTool.exe --find-regressions MACHINE-ID CONFIG-ID [--test-run [--only-necessary]]");
			Environment.Exit (success ? 0 : 1);
		}

		public static void Main (string[] args)
		{
			LogManager.Adapter = new ConsoleOutLoggerFactoryAdapter();
			Logging.SetLogging (LogManager.GetLogger<MainClass> ());
			if (args.Length == 0)
				UsageAndExit (true);

			if (args [0] == "--find-regressions") {
				var machineId = args [1];
				var configId = args [2];
				var testRun = args.Length > 3 && args [3] == "--test-run";
				var onlyNecessary = !testRun || (args.Length > 4 && args [4] == "--only-necessary");
				if (!testRun) {
					var credentials = Accredit.GetCredentials ("regressionSlack");
					SlackHooksUrl = credentials ["hooksURL"].ToString ();
				}
				var conn = PostgresInterface.Connect ();
				AsyncContext.Run (() => FindRegressions (conn, machineId, configId, testRun, onlyNecessary));
			} else {
				UsageAndExit (false);
			}
		}
	}
}

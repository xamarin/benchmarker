using System;
using System.Threading.Tasks;
using Parse;
using Nito.AsyncEx;
using Benchmarker.Common;
using System.Linq;
using System.Collections.Generic;

namespace DbTool
{
	class MainClass
	{
		static async Task FixRunSet (ParseObject runSet)
		{
			var runs = await ParseInterface.PageQueryWithRetry (() => {
				return ParseObject.GetQuery ("Run")
					.Include ("benchmark")
					.WhereEqualTo ("runSet", runSet);
			});
			var benchmarkNames = runs.Select (r => (string) (((ParseObject)r ["benchmark"]) ["name"])).Distinct ();
			Console.WriteLine ("run set {0} has {1} runs {2} benchmarks", runSet.ObjectId, runs.Count (), benchmarkNames.Count ());
			var averages = new Dictionary <string, double> ();
			var variances = new Dictionary <string, double> ();
			foreach (var name in benchmarkNames) {
				var numbers = runs.Where (r => (string)(((ParseObject)r ["benchmark"]) ["name"]) == name).Select (r => ParseInterface.NumberAsDouble (r ["elapsedMilliseconds"])).ToArray ();
				var avg = numbers.Average ();
				averages [name] = avg;
				var sum = 0.0;
				foreach (var v in numbers) {
					var diff = v - avg;
					sum += diff * diff;
				}
				var variance = sum / numbers.Length;
				variances [name] = variance;
				Console.WriteLine ("benchmark {0} average {1} variance {2}", name, avg, variance);
			}
			runSet ["elapsedTimeAverages"] = averages;
			runSet ["elapsedTimeVariances"] = variances;
			await runSet.SaveAsync ();
		}

		static async Task AddAverages ()
		{
			var runSets = await ParseInterface.PageQueryWithRetry (() => ParseObject.GetQuery ("RunSet"));
			foreach (var runSet in runSets) {
				if (runSet.ContainsKey ("elapsedTimeAverages") && runSet.ContainsKey ("elapsedTimeVariances")) {
					var averages = runSet.Get<Dictionary<string, object>> ("elapsedTimeAverages");
					var variances = runSet.Get<Dictionary<string, object>> ("elapsedTimeVariances");
					var averagesKeys = new SortedSet<string> (averages.Keys);
					var variancesKeys = new SortedSet<string> (variances.Keys);
					if (averagesKeys.SetEquals (variancesKeys))
						continue;
				}
				await FixRunSet (runSet);
			}
			Console.WriteLine ("got {0} run sets", runSets.Count ());
		}

		static async Task DeleteRunSet (string runSetId)
		{
			var runSets = await ParseInterface.PageQueryWithRetry (() => {
				return ParseObject.GetQuery ("RunSet")
					.WhereEqualTo ("objectId", runSetId);
			});
			if (runSets.Count () != 1)
				throw new Exception ("Could not fetch run set");
			var runSet = runSets.First ();
			var runs = await ParseInterface.PageQueryWithRetry (() => {
				return ParseObject.GetQuery ("Run")
					.WhereEqualTo ("runSet", runSet);
			});
			Console.WriteLine ("deleting " + runs.Count () + " runs");
			foreach (var run in runs)
				await run.DeleteAsync ();
			await runSet.DeleteAsync ();
		}

		static Dictionary<string, List<ParseObject>> runsForRunSetId = new Dictionary<string, List<ParseObject>> ();

		static async Task<IEnumerable<ParseObject>> FetchRunsForRunSet (ParseObject runSet)
		{
			var id = runSet.ObjectId;
			if (!runsForRunSetId.ContainsKey (id)) {
				var runs = await ParseInterface.PageQueryWithRetry (() => {
					return ParseObject.GetQuery ("Run")
						.WhereEqualTo ("runSet", runSet)
						.Include ("benchmark");
				});
				runsForRunSetId [id] = runs.ToList ();
			}
			return runsForRunSetId [id];
		}

		static async Task FindRegressions (string machineId, string configId)
		{
			const int baselineWindowSize = 5;
			const int testWindowSize = 3;
			const double controlLimitSize = 6;

			var machine = await ParseObject.GetQuery ("Machine").GetAsync (machineId);
			var config = await ParseObject.GetQuery ("Config").GetAsync (configId);
			var runSets = await ParseInterface.PageQueryWithRetry (() => {
				return ParseObject.GetQuery ("RunSet")
					.WhereEqualTo ("config", config)
					.WhereEqualTo ("machine", machine)
					.WhereNotEqualTo ("failed", true)
					.Include ("commit");
			});
			var sortedRunSets = runSets.ToList ();
			sortedRunSets.Sort ((a, b) => {
				var aCommitDate = a.Get<ParseObject> ("commit").Get<DateTime> ("commitDate");
				var bCommitDate = b.Get<ParseObject> ("commit").Get<DateTime> ("commitDate");
				var result = aCommitDate.CompareTo (bCommitDate);
				if (result != 0)
					return result;
				var aStartedDate = a.Get<DateTime> ("startedAt");
				var bStartedDate = b.Get<DateTime> ("startedAt");
				return aStartedDate.CompareTo (bStartedDate);
			});
			var lastWarningIndex = new Dictionary<string, int> ();
			for (var i = baselineWindowSize; i <= sortedRunSets.Count - testWindowSize; ++i) {
				var windowAverages = new Dictionary<string, double> ();
				var windowVariances = new Dictionary<string, double> ();
				var benchmarkCounts = new Dictionary<string, int> ();

				for (var j = 1; j <= baselineWindowSize; ++j) {
					var baselineRunSet = sortedRunSets [i - j];
					var averages = baselineRunSet.Get <Dictionary<string, object>> ("elapsedTimeAverages");
					var variances = baselineRunSet.Get <Dictionary<string, object>> ("elapsedTimeVariances");
					foreach (var kvp in averages) {
						var name = kvp.Key;
						var average = ParseInterface.NumberAsDouble (kvp.Value);
						var variance = ParseInterface.NumberAsDouble (variances [name]);
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

				var testRuns = new List<ParseObject> ();
				for (var j = 0; j < testWindowSize; ++j) {
					var runs = await FetchRunsForRunSet (sortedRunSets [i + j]);
					testRuns.AddRange (runs);
				}

				var testRunSet = sortedRunSets [i];

				var commitHash = testRunSet.Get<ParseObject> ("commit").Get<string> ("hash");
				Console.WriteLine ("{0} {1}", testRunSet.ObjectId, commitHash);

				foreach (var kvp in benchmarkCounts) {
					var name = kvp.Key;
					if (kvp.Value < baselineWindowSize)
						continue;
					if (lastWarningIndex.ContainsKey (name) && lastWarningIndex [name] >= i - baselineWindowSize)
						continue;
					var average = windowAverages [name];
					var variance = windowVariances [name];
					var stdDev = Math.Sqrt (variance);
					var lowerControlLimit = average - controlLimitSize * stdDev;
					var upperControlLimit = average + controlLimitSize * stdDev;
					var runs = testRuns.Where (o => o.Get<ParseObject> ("benchmark").Get<string> ("name") == name).ToList ();
					if (runs.Count < 5)
						continue;
					var numOutliersFaster = 0;
					var numOutliersSlower = 0;
					foreach (var run in runs) {
						var elapsed = ParseInterface.NumberAsDouble (run ["elapsedMilliseconds"]);
						if (elapsed < lowerControlLimit)
							++numOutliersFaster;
						if (elapsed > upperControlLimit)
							++numOutliersSlower;
					}
					if (numOutliersFaster > runs.Count * 3 / 4) {
						Console.WriteLine ("+ regression in {0}: {1}/{2}", name, numOutliersFaster, runs.Count);
						lastWarningIndex [name] = i;
					} else if (numOutliersSlower > runs.Count * 3 / 4) {
						Console.WriteLine ("- regression in {0}: {1}/{2}", name, numOutliersSlower, runs.Count);
						lastWarningIndex [name] = i;
					}
					/*
					else if (numOutliersFaster == 0 && numOutliersSlower == 0) {
						Console.WriteLine ("  nothing in    {0}", name);
					} else {
						Console.WriteLine ("? suspected in  {0}: {1}-/{2}+/{3}", name, numOutliersSlower, numOutliersFaster, runs.Count);
					}
					*/
				}

				await Task.Delay (1000);
			}
		}

		static void UsageAndExit (bool success)
		{
			Console.WriteLine ("Usage:");
			Console.WriteLine ("    DbTool.exe --add-averages");
			Console.WriteLine ("    DbTool.exe --delete-run-set ID");
			Console.WriteLine ("    DbTool.exe --find-regressions MACHINE-ID CONFIG-ID");
			Environment.Exit (success ? 0 : 1);
		}

		public static void Main (string[] args)
		{
			if (!ParseInterface.Initialize ()) {
				Console.Error.WriteLine ("Error: Could not initialize Parse interface.");
				Environment.Exit (1);
			}

			if (args.Length == 0)
				UsageAndExit (true);

			if (args [0] == "--add-averages") {
				AsyncContext.Run (() => AddAverages ());
			} else if (args [0] == "--delete-run-set") {
				var runSet = args [1];
				AsyncContext.Run (() => DeleteRunSet (runSet));
			} else if (args [0] == "--find-regressions") {
				var machineId = args [1];
				var configId = args [2];
				AsyncContext.Run (() => FindRegressions (machineId, configId));
			} else {
				UsageAndExit (false);
			}
		}
	}
}

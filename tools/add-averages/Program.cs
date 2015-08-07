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
		static async Task<IEnumerable<T>> PageQuery<T> (Func<ParseQuery<T>> makeQuery) where T : ParseObject
		{
			IEnumerable<T> results = new List<T> ();
			var limit = 100;
			for (var skip = 0;; skip += limit) {
				var query = makeQuery ().Limit (limit).Skip (skip);
				//Console.WriteLine ("skipping {0}", skip);
				var page = await query.FindAsync ();
				results = results.Concat (page);
				if (page.Count () < limit)
					break;
			}
			return results;
		}

		static async Task FixRunSet (ParseObject runSet)
		{
			var runs = await PageQuery (() => {
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
			var runSets = await PageQuery (() => ParseObject.GetQuery ("RunSet"));
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
			var runSets = await PageQuery (() => {
				return ParseObject.GetQuery ("RunSet")
					.WhereEqualTo ("objectId", runSetId);
			});
			if (runSets.Count () != 1)
				throw new Exception ("Could not fetch run set");
			var runSet = runSets.First ();
			var runs = await PageQuery (() => {
				return ParseObject.GetQuery ("Run")
					.WhereEqualTo ("runSet", runSet);
			});
			Console.WriteLine ("deleting " + runs.Count () + " runs");
			foreach (var run in runs)
				await run.DeleteAsync ();
			await runSet.DeleteAsync ();
		}

		static void UsageAndExit (bool success)
		{
			Console.WriteLine ("Usage:");
			Console.WriteLine ("    DbTool.exe --add-averages");
			Console.WriteLine ("    DbTool.exe --delete-run-set ID");
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
			} else {
				UsageAndExit (false);
			}
		}
	}
}

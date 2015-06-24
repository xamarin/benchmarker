using System;
using System.Threading.Tasks;
using Parse;
using Nito.AsyncEx;
using Benchmarker.Common;
using System.Linq;
using System.Collections.Generic;

namespace addaverages
{
	class MainClass
	{
		static async Task<IEnumerable<T>> PageQuery<T> (Func<ParseQuery<T>> makeQuery) where T : ParseObject
		{
			IEnumerable<T> results = new List<T> ();
			var limit = 100;
			for (var skip = 0;; skip += limit) {
				var query = makeQuery ().Limit (limit).Skip (skip);
				Console.WriteLine ("skipping {0}", skip);
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
			foreach (var name in benchmarkNames) {
				var avg = runs.Where (r => (string)(((ParseObject)r ["benchmark"]) ["name"]) == name).Select (r => (double)(long)r ["elapsedMilliseconds"]).Average ();
				Console.WriteLine ("benchmark {0} average {1}", name, avg);
				averages [name] = avg;
			}
			runSet ["elapsedTimeAverages"] = averages;
			await runSet.SaveAsync ();
		}

		static async Task Work ()
		{
			var runSets = await PageQuery (() => ParseObject.GetQuery ("RunSet"));
			foreach (var runSet in runSets) {
				if (runSet.ContainsKey ("elapsedTimeAverages"))
					continue;
				await FixRunSet (runSet);
			}
			Console.WriteLine ("got {0} run sets", runSets.Count ());
		}

		public static void Main (string[] args)
		{
			if (!ParseInterface.Initialize ()) {
				Console.Error.WriteLine ("Error: Could not initialize Parse interface.");
				Environment.Exit (1);
			}

			AsyncContext.Run (() => Work ());
		}
	}
}

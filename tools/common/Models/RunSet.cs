using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Parse;
using Mono.Unix.Native;
using System.Linq;

namespace Benchmarker.Common.Models
{
	public class RunSet
	{
		ParseObject parseObject;

		List<Result> results;
		public List<Result> Results { get { return results; } }
		public DateTime StartDateTime { get; set; }
		public DateTime FinishDateTime { get; set; }
		public Config Config { get; set; }
		public Commit Commit { get; set; }
		public string BuildURL { get; set; }
		public string LogURL { get; set; }

		List<Benchmark> timedOutBenchmarks;
		public List<Benchmark> TimedOutBenchmarks { get { return timedOutBenchmarks; } }

		List<Benchmark> crashedBenchmarks;
		public List<Benchmark> CrashedBenchmarks { get { return crashedBenchmarks; } }

		public RunSet ()
		{
			results = new List<Result> ();
			timedOutBenchmarks = new List<Benchmark> ();
			crashedBenchmarks = new List<Benchmark> ();
		}

		static Tuple<string, string> LocalHostnameAndArch () {
			Utsname utsname;
			var res = Syscall.uname (out utsname);
			string arch;
			string hostname;
			if (res != 0) {
				arch = "unknown";
				hostname = "unknown";
			} else {
				arch = utsname.machine;
				hostname = utsname.nodename;
			}

			return Tuple.Create (hostname, arch);
		}

		async Task<ParseObject> GetOrUploadMachineToParse (List<ParseObject> saveList)
		{
			var hostnameAndArch = LocalHostnameAndArch ();
			var hostname = hostnameAndArch.Item1;
			var arch = hostnameAndArch.Item2;
			var results = await ParseInterface.RunWithRetry (() => ParseObject.GetQuery ("Machine").WhereEqualTo ("name", hostname).WhereEqualTo ("architecture", arch).FindAsync ());
			//Console.WriteLine ("FindAsync Machine");
			if (results.Count () > 0)
				return results.First ();
			var obj = ParseInterface.NewParseObject ("Machine");
			obj ["name"] = hostname;
			obj ["architecture"] = arch;
			obj ["isDedicated"] = false;
			saveList.Add (obj);
			return obj;
		}

		static bool AreWeOnParseMachine (ParseObject obj)
		{
			var hostnameAndArch = LocalHostnameAndArch ();
			var hostname = hostnameAndArch.Item1;
			var arch = hostnameAndArch.Item2;
			return hostname == obj.Get<string> ("name") && arch == obj.Get<string> ("architecture");
		}

		static async Task<ParseObject[]> BenchmarkListToParseObjectArray (IList<Benchmark> l, List<ParseObject> saveList)
		{
			var pos = new List<ParseObject> ();
			foreach (var b in l)
				pos.Add (await b.GetOrUploadToParse (saveList));
			return pos.ToArray ();
		}

		public static async Task<RunSet> FromId (string id, Config config, Commit commit, string buildURL, string logURL)
		{
			var obj = await ParseInterface.RunWithRetry (() => ParseObject.GetQuery ("RunSet").GetAsync (id));
			//Console.WriteLine ("GetAsync RunSet");
			if (obj == null)
				throw new Exception ("Could not fetch run set.");

			var runSet = new RunSet {
				parseObject = obj,
				StartDateTime = obj.Get<DateTime> ("startedAt"),
				FinishDateTime = obj.Get<DateTime> ("finishedAt"),
				BuildURL = obj.Get<string> ("buildURL"),
				LogURL = logURL
			};

			var configObj = obj.Get<ParseObject> ("config");
			var commitObj = obj.Get<ParseObject> ("commit");
			var machineObj = obj.Get<ParseObject> ("machine");

			await ParseInterface.RunWithRetry (() => ParseObject.FetchAllAsync (new ParseObject[] { configObj, commitObj, machineObj }));
			//Console.WriteLine ("FindAllAsync config, commit, machine");

			if (!config.EqualToParseObject (configObj))
				throw new Exception ("Config does not match the one in the database.");
			if (commit.Hash != commitObj.Get<string> ("hash"))
				throw new Exception ("Commit does not match the one in the database.");
			if (buildURL != runSet.BuildURL)
				throw new Exception ("Build URL does not match the one in the database.");
			if (!AreWeOnParseMachine (machineObj))
				throw new Exception ("Machine does not match the one in the database.");

			runSet.Config = config;
			runSet.Commit = commit;

			foreach (var o in obj.Get<List<object>> ("timedOutBenchmarks"))
				runSet.timedOutBenchmarks.Add (await Benchmark.FromId (((ParseObject)o).ObjectId));
			foreach (var o in obj.Get<List<object>> ("crashedBenchmarks"))
				runSet.crashedBenchmarks.Add (await Benchmark.FromId (((ParseObject)o).ObjectId));

			return runSet;
		}

		public async Task<ParseObject> UploadToParse ()
		{
			// FIXME: for amended run sets, delete existing runs of benchmarks we just ran

			var averages = new Dictionary<string, double> ();
			var variances = new Dictionary<string, double> ();
			var logURLs = new Dictionary<string, string> ();

			if (parseObject != null) {
				var originalAverages = parseObject.Get<Dictionary<string, object>> ("elapsedTimeAverages");
				foreach (var kvp in originalAverages)
					averages [kvp.Key] = ParseInterface.NumberAsDouble (kvp.Value);

				var originalVariances = parseObject.Get<Dictionary<string, object>> ("elapsedTimeVariances");
				foreach (var kvp in originalVariances)
					variances [kvp.Key] = ParseInterface.NumberAsDouble (kvp.Value);

				var originalLogURLs = parseObject.Get<Dictionary<string, object>> ("logURLs");
				if (originalLogURLs != null) {
					foreach (var kvp in originalLogURLs)
						logURLs [kvp.Key] = (string)kvp.Value;
				}
			}

			foreach (var result in results) {
				var avgAndVariance = result.AverageAndVarianceWallClockTimeMilliseconds;
				if (avgAndVariance == null)
					continue;
				averages [result.Benchmark.Name] = avgAndVariance.Item1;
				variances [result.Benchmark.Name] = avgAndVariance.Item2;
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

			var saveList = new List<ParseObject> ();
			var obj = parseObject ?? ParseInterface.NewParseObject ("RunSet");

			if (parseObject == null) {
				var m = await GetOrUploadMachineToParse (saveList);
				var c = await Config.GetOrUploadToParse (saveList);
				var commit = await Commit.GetOrUploadToParse (saveList);
				obj ["machine"] = m;
				obj ["config"] = c;
				obj ["commit"] = commit;
				obj ["buildURL"] = BuildURL;
				obj ["startedAt"] = StartDateTime;
			}

			obj ["finishedAt"] = FinishDateTime;

			obj ["failed"] = averages.Count == 0;
			obj ["elapsedTimeAverages"] = averages;
			obj ["elapsedTimeVariances"] = variances;
			obj ["logURLs"] = logURLs;

			obj ["timedOutBenchmarks"] = await BenchmarkListToParseObjectArray (timedOutBenchmarks, saveList);
			obj ["crashedBenchmarks"] = await BenchmarkListToParseObjectArray (crashedBenchmarks, saveList);

			Console.WriteLine ("uploading run set");

			saveList.Add (obj);
			await ParseInterface.RunWithRetry (() => ParseObject.SaveAllAsync (saveList));
			//Console.WriteLine ("SaveAllAsync saveList 1");
			saveList.Clear ();

			parseObject = obj;

			Console.WriteLine ("uploading runs");

			foreach (var result in results) {
				if (result.Config != Config)
					throw new Exception ("Results must have the same config as their RunSets");
				await result.UploadRunsToParse (obj, saveList);
			}
			await ParseInterface.RunWithRetry (() => ParseObject.SaveAllAsync (saveList));
			//Console.WriteLine ("SaveAllAsync saveList 2");

			Console.WriteLine ("done uploading");

			return obj;
		}
	}
}

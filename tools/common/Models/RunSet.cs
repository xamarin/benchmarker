﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Parse;
using Mono.Unix.Native;
using System.Linq;

namespace Benchmarker.Common.Models
{
	public class RunSet
	{
		List<Result> results;
		public List<Result> Results { get { return results; } }
		public DateTime StartDateTime { get; set; }
		public DateTime FinishDateTime { get; set; }
		public Config Config { get; set; }
		public Commit Commit { get; set; }
		public string BuildURL { get; set; }

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

		async Task<ParseObject> GetOrUploadMachineToParse (List<ParseObject> saveList)
		{
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

			var results = await ParseObject.GetQuery ("Machine").WhereEqualTo ("name", hostname).WhereEqualTo ("architecture", arch).FindAsync ();
			if (results.Count () > 0)
				return results.First ();
			var obj = ParseInterface.NewParseObject ("Machine");
			obj ["name"] = hostname;
			obj ["architecture"] = arch;
			saveList.Add (obj);
			return obj;
		}

		static async Task<ParseObject[]> BenchmarkListToParseObjectArray (IList<Benchmark> l, List<ParseObject> saveList)
		{
			var pos = new List<ParseObject> ();
			foreach (var b in l)
				pos.Add (await b.GetOrUploadToParse (saveList));
			return pos.ToArray ();
		}

		public async Task<ParseObject> UploadToParse ()
		{
			var averages = new Dictionary<string, double> ();
			foreach (var result in results) {
				var avg = result.AverageWallClockTime;
				if (avg == null)
					continue;
				averages [result.Benchmark.Name] = avg.Value.TotalMilliseconds;
			}

			var saveList = new List<ParseObject> ();
			var m = await GetOrUploadMachineToParse (saveList);
			var c = await Config.GetOrUploadToParse (saveList);
			var commit = await Commit.GetOrUploadToParse (saveList);
			var obj = ParseInterface.NewParseObject ("RunSet");
			obj ["machine"] = m;
			obj ["config"] = c;
			obj ["commit"] = commit;
			obj ["buildURL"] = BuildURL;
			obj ["startedAt"] = StartDateTime;
			obj ["finishedAt"] = FinishDateTime;
			obj ["elapsedTimeAverages"] = averages;

			obj ["timedOutBenchmarks"] = await BenchmarkListToParseObjectArray (timedOutBenchmarks, saveList);
			obj ["crashedBenchmarks"] = await BenchmarkListToParseObjectArray (crashedBenchmarks, saveList);

			Console.WriteLine ("uploading run set");

			saveList.Add (obj);
			await ParseObject.SaveAllAsync (saveList);
			saveList.Clear ();

			Console.WriteLine ("uploading runs");

			foreach (var result in results) {
				if (result.Config != Config)
					throw new Exception ("Results must have the same config as their RunSets");
				await result.UploadRunsToParse (obj, saveList);
			}
			await ParseObject.SaveAllAsync (saveList);

			Console.WriteLine ("done uploading");

			return obj;
		}
	}
}
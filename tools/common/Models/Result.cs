using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Parse;
using System.Threading.Tasks;

namespace Benchmarker.Common.Models
{
	public class Result
	{
		public DateTime DateTime { get; set; }
		public Benchmark Benchmark { get; set; }
		public Config Config { get; set; }
		public string Version { get; set; }

		List<Run> runs;
		public List<Run> Runs { get { return runs; } }

		public Result ()
		{
			runs = new List<Run> ();
		}

		public static Result LoadFromString (string content)
		{
			return JsonConvert.DeserializeObject<Result> (content);
		}

		public Tuple<double, double> AverageAndVarianceWallClockTimeMilliseconds {
			get {
				if (runs.Count == 0)
					return null;
				
				var timesInMs = runs.Select (run => run.WallClockTime.TotalMilliseconds).ToArray ();
				var avg = timesInMs.Average ();

				var sum = 0.0;
				foreach (var v in timesInMs) {
					var diff = v - avg;
					sum += diff * diff;
				}
				var variance = sum / runs.Count;

				return Tuple.Create<double, double> (avg, variance);
			}
		}

		public class Run {
			public TimeSpan WallClockTime { get; set; }
			public string Output { get; set; }
			public string Error { get; set; }
		}

		public async Task UploadRunsToParse (ParseObject runSet, List<ParseObject> saveList) {
			var b = await Benchmark.GetOrUploadToParse (saveList);
			foreach (var run in Runs) {
				var obj = ParseInterface.NewParseObject ("Run");
				obj ["benchmark"] = b;
				obj ["runSet"] = runSet;
				obj ["elapsedMilliseconds"] = run.WallClockTime.TotalMilliseconds;
				saveList.Add (obj);
			}
		}
	}
}

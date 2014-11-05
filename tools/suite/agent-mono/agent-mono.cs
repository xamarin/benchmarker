using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Benchmarker.Common.LogProfiler;
using Benchmarker.Common.Models;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;

public class Program
{
	static void UsageAndExit (string message = null, int exitcode = 0)
	{
		Console.Error.WriteLine ("usage: [parameters] [--] <tests-dir> <benchmarks-dir> <config-file> [<config-file>+]");
		Console.Error.WriteLine ("parameters:");
		Console.Error.WriteLine ("        --help            display this help");
		Console.Error.WriteLine ("    -b, --benchmarks      benchmarks to run, separated by commas; default to all of them");
		Console.Error.WriteLine ("    -a, --architecture    architecture to run against, values can be \"amd64\" or \"x86\"");
		Console.Error.WriteLine ("    -c, --commit          commit to run against, identified by it's sha commit");
		Console.Error.WriteLine ("    -t, --timeout         execution timeout for each benchmark, in seconds; default to no timeout");

		Environment.Exit (exitcode);
	}

	public static void Main (string[] args)
	{
		var benchmarksnames = new string[0];
		var architecture = Environment.Is64BitOperatingSystem ? "amd64" : "x86";
		var commit = String.Empty;
		var timeout = Int32.MaxValue;

		var optindex = 0;

		for (; optindex < args.Length; ++optindex) {
			if (args [optindex] == "-b" || args [optindex] == "--benchmarks") {
				benchmarksnames = args [++optindex].Split (',').Select (s => s.Trim ()).Union (benchmarksnames).ToArray ();
			} else if (args [optindex] == "-a" || args [optindex] == "--architecture") {
				architecture = args [++optindex];
			} else if (args [optindex] == "-c" || args [optindex] == "--commit") {
				commit = args [++optindex];
			} else if (args [optindex] == "-t" || args [optindex] == "--timeout") {
				timeout = Int32.Parse (args [++optindex]);
			} else if (args [optindex].StartsWith ("--help")) {
				UsageAndExit ();
			} else if (args [optindex] == "--") {
				optindex += 1;
				break;
			} else if (args [optindex].StartsWith ("-")) {
				Console.Error.WriteLine ("unknown parameter {0}", args [optindex]);
				UsageAndExit ();
			} else {
				break;
			}
		}

		architecture = "amd64";
		commit = "2a14bc4dad10850631656b97396413e6c6a8be83";
		benchmarksnames = new string[] { "ahcbench" };

		if (args.Length - optindex < 3)
			UsageAndExit (null, 1);

		var testsdir = args [optindex++];
		var benchmarksdir = args [optindex++];
		var configfiles = args.Skip (optindex).ToArray ();

		var benchmarks = Benchmark.LoadAllFrom (benchmarksdir, benchmarksnames).OrderBy (b => b.Name).ToArray ();
		var configs = configfiles.Select (c => Config.LoadFrom (c)).ToArray ();

		var revision = String.IsNullOrEmpty (commit) ? Revision.Last ("mono", architecture) : Revision.Get ("mono", architecture, commit);

		var revisionfolder = Directory.CreateDirectory (Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ())).FullName;
		var profilesfolder = Directory.CreateDirectory (Path.Combine (revisionfolder, String.Join ("_", DateTime.Now.ToString ("s").Replace (':', '-'), revision.Commit))).FullName;

		revision.FetchInto (revisionfolder);

		var profiles = new List<Profile> (benchmarks.Length * configs.Length);

		foreach (var benchmark in benchmarks) {
			foreach (var config in configs) {
				var profile = benchmark.Profile (config, revision, revisionfolder, profilesfolder, testsdir, timeout);

				profile.StoreTo (Path.Combine (profilesfolder, profile.ToString () + ".json"));

				profiles.Add (profile);
			}
		}

		var graphfolder = Directory.CreateDirectory (Path.Combine (profilesfolder, "graphs")).FullName;
		Console.Out.WriteLine ("Generating graphs in \"{0}\"", graphfolder);

		foreach (var profile in profiles) {
			// Dictionary [Profile.Run] => List[KeyValuePair[Counter.Timestamp, List[Counter]]]
			Dictionary<Profile.Run, List<KeyValuePair<ulong, List<Counter>>>> countersvalues =
				profile.Runs.AsParallel ().Select (r =>
					KeyValuePair.Create (r, r.GetCounters (profilesfolder).ToList ())
				).ToDictionary (kv => kv.Key, kv => kv.Value);

			// Dictionary [Counter.CounterID] => Counter.ToString ()
			Dictionary<ulong, string> countersdesc =
				countersvalues.Values.SelectMany (timestamps =>
					timestamps.SelectMany (timestamp =>
						timestamp.Value.Select (c =>
							KeyValuePair.Create (c.CounterID, c.ToString ())
						)
					)
				).Aggregate (new Dictionary<ulong, string> (), (d, kv) => { d [kv.Key] = kv.Value; return d; });

			// Dictionary [Counter.CounterID] => Dictionary [Profile.Run] => List[KeyValuePair[Counter.Timestamp, Counter]]
			Dictionary<ulong, Dictionary<Profile.Run, List<KeyValuePair<ulong, Counter>>>> counters =
				countersdesc.Select (cdesc =>
					KeyValuePair.Create (cdesc.Key, countersvalues.Select (pr =>
						KeyValuePair.Create (pr.Key, pr.Value.Select (cs =>
							KeyValuePair.Create (cs.Key, cs.Value.Where (c => c.CounterID == cdesc.Key).SingleOrDefault ())
						).ToList ())
					).ToDictionary (t => t.Key, t => t.Value))
				).ToDictionary (t => t.Key, t => t.Value);

			foreach (var cd in countersdesc) {
				var cid = cd.Key;
				var cname = cd.Value;

				var plot = new PlotModel {
					Title = cname,
					LegendPlacement = LegendPlacement.Outside,
					LegendPosition = LegendPosition.RightMiddle,
					LegendOrientation = LegendOrientation.Vertical,
					LegendBorderThickness = 0,
				};

				plot.Axes.Add (new LinearAxis { Minimum = 0, AbsoluteMinimum = 0 });

				for (var i = 0; i < counters [cid].Count; ++i) {
					var run = counters [cid].ElementAt (i);
					var serie = new LineSeries { Title = i.ToString () };

					foreach (var counter in run.Value)
						serie.Points.Add (new DataPoint (Convert.ToDouble (counter.Key), Convert.ToDouble (counter.Value.Value)));

					plot.Series.Add (serie);
				}

				using (var stream = new FileStream (Path.Combine (Directory.CreateDirectory (Path.Combine (graphfolder, profile.ToString ())).FullName, cname + ".svg"), FileMode.Create))
					SvgExporter.Export (plot, stream, 1440, 900, true);
			}
		}

		Console.Out.WriteLine ("Copying files to storage");

		var info = new ProcessStartInfo {
			FileName = "scp",
			Arguments = String.Format ("-r -B -i /Users/ludovic/.ssh/id_rsa~builder@nas.bos.xamarin.com '{0}' builder@nas:/volume1/storage/benchmarker/runs", profilesfolder),
			UseShellExecute = true 
		};

		Process.Start (info).WaitForExit ();
	}

	struct KeyValuePair
	{
		public static KeyValuePair<TKey, TValue> Create<TKey, TValue> (TKey key, TValue value)
		{
			return new KeyValuePair<TKey, TValue> (key, value);
		}
	}
}

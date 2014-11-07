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
using Newtonsoft.Json;
using System.IO.Compression;

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
		Console.Error.WriteLine ("        --ssh-key         path to ssh key for builder@nas");

		Environment.Exit (exitcode);
	}

	public static void Main (string[] args)
	{
		var benchmarksnames = new string[0];
		var architecture = Environment.Is64BitOperatingSystem ? "amd64" : "x86";
		var commit = String.Empty;
		var timeout = Int32.MaxValue;
		var sshkey = String.Empty;

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
			} else if (args [optindex] == "--ssh-key") {
				sshkey = args [++optindex];
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

		if (args.Length - optindex < 3)
			UsageAndExit (null, 1);

		var testsdir = args [optindex++];
		var benchmarksdir = args [optindex++];
		var configfiles = args.Skip (optindex).ToArray ();

		var benchmarks = Benchmark.LoadAllFrom (benchmarksdir, benchmarksnames).OrderBy (b => b.Name).ToArray ();
		var configs = configfiles.Select (c => Config.LoadFrom (c)).ToArray ();

		var revision = String.IsNullOrEmpty (commit) ? Revision.Last ("mono", architecture) : Revision.Get ("mono", architecture, commit);
		if (revision == null) {
			Console.Out.WriteLine ("Revision not found");
			Environment.Exit (2);
		}

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
			// Dictionary [Counter] => Dictionary [Profile.Run.ID] => SortedDictionary [Sample.TimeStamp] => Sample.Value
			Dictionary<Counter, Dictionary<int, SortedDictionary<TimeSpan, object>>> counters =
				profile.Runs.AsParallel ()
					.Select (r => KeyValuePair.Create (r, r.GetCounters (profilesfolder)))
					.SelectMany (run => run.Value.Select (counter => KeyValuePair.Create (counter.Key, KeyValuePair.Create (run.Key.Index, counter.Value))))
					.Aggregate (new Dictionary<Counter, Dictionary<int, SortedDictionary<TimeSpan, object>>> (), (d, kv) => {
						if (!d.ContainsKey (kv.Key))
							d.Add (kv.Key, new Dictionary<int, SortedDictionary<TimeSpan, object>> ());
						d [kv.Key][kv.Value.Key] = kv.Value.Value;

						return d;
					});

			foreach (var counter in counters) {
				var runs = counter.Value.ToList ();

				var plot = new PlotModel {
					Title = counter.Key.Name,
					LegendPlacement = LegendPlacement.Outside,
					LegendPosition = LegendPosition.RightMiddle,
					LegendOrientation = LegendOrientation.Vertical,
					LegendBorderThickness = 0,
				};

				plot.Axes.Add (new LinearAxis { Minimum = 0, AbsoluteMinimum = 0 });

				foreach (var run in runs) {
					var timestamps = run.Value;
					var serie = new LineSeries { Title = run.Key.ToString (), MarkerType = MarkerType.Circle };

					foreach (var timestamp in timestamps) {
						double value, rawvalue = Convert.ToDouble (timestamp.Value);

						switch (counter.Key.Type) {
						case CounterType.Long:
							if (counter.Key.Unit == CounterUnit.Time)
								value = rawvalue / 10000d;
							else
								value = rawvalue;
							break;
						case CounterType.TimeInterval:
							value = rawvalue / 1000d;
							break;
						default:
							value = rawvalue;
							break;
						}

						serie.Points.Add (new DataPoint { X = timestamp.Key.TotalSeconds, Y = value });
					}

					plot.Series.Add (serie);
				}

				using (var stream = new FileStream (Path.Combine (Directory.CreateDirectory (Path.Combine (graphfolder, profile.ToString ())).FullName, counter.Key.ToString () + ".svg"), FileMode.Create))
					SvgExporter.Export (plot, stream, 720, 450, true);
			}

			var serializer = new JsonSerializer { Formatting = Formatting.Indented };
			using (var writer = new StreamWriter (new GZipStream (new FileStream (Path.Combine (profilesfolder, profile.ToString () + ".counters.json.gz"), FileMode.Create), CompressionMode.Compress)))
				serializer.Serialize (writer, counters);
		}

		Console.Out.WriteLine ("Copying files to storage");

		var info = new ProcessStartInfo {
			FileName = "scp",
			Arguments = String.Format ("-r -B {0} '{1}' builder@nas:/volume1/storage/benchmarker/runs", String.IsNullOrWhiteSpace (sshkey) ? "" : ("-i " + sshkey), profilesfolder),
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

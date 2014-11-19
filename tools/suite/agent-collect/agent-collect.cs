using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Benchmarker.Common.Models;
using System.Collections.Generic;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

class Program
{
	static void UsageAndExit (string message = null, int exitcode = 0)
	{
		Console.Error.WriteLine ("usage: [parameters] [--] <project> <architecture>");
		Console.Error.WriteLine ("parameters:");
		Console.Error.WriteLine ("        --help            display this help");
		Console.Error.WriteLine ("    -b, --benchmarks      benchmarks to run, separated by commas; default to all of them");
		Console.Error.WriteLine ("        --sshkey          path to ssh key for builder@nas");

		Environment.Exit (exitcode);
	}

	public static void Main (string[] args)
	{
		var benchmarksnames = new string[0];
		var sshkey = String.Empty;

		var optindex = 0;

		for (; optindex < args.Length; ++optindex) {
			if (args [optindex] == "-b" || args [optindex] == "--benchmarks") {
				benchmarksnames = args [++optindex].Split (',').Select (s => s.Trim ()).Union (benchmarksnames).ToArray ();
			} else if (args [optindex] == "--sshkey") {
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

		if (args.Length - optindex < 2)
			UsageAndExit (null, 1);

		var project = args [optindex++];
		var architecture = args [optindex++];

		var resultsfolder = Directory.CreateDirectory (Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ())).FullName;

		Console.WriteLine ("Downloading results files into \"{0}\"", resultsfolder);

		if (benchmarksnames.Length == 0) {
			SCPFromRemote (sshkey, String.Format ("/volume1/storage/benchmarker/runs/{0}/{1}/*/*.json.gz", project, architecture), resultsfolder);
		} else {
			Parallel.ForEach (benchmarksnames, b => {
				SCPFromRemote (sshkey, String.Format ("/volume1/storage/benchmarker/runs/{0}/{1}/*/{2}*.json.gz", project, architecture, b), resultsfolder);
			});
		}

		Console.WriteLine ("Reading results files");

		var profiles = Directory.EnumerateFiles (resultsfolder, "*.json.gz")
			.AsParallel ()
			.Select (p => ProfileResult.LoadFrom (p, true))
			.Aggregate (new Dictionary<Benchmark, Dictionary<Config, Dictionary<Revision, ProfileResult>>> (), (d, pr) => {
				if (!d.ContainsKey (pr.Benchmark))
					d.Add (pr.Benchmark, new Dictionary<Config, Dictionary<Revision, ProfileResult>>());
				if (!d [pr.Benchmark].ContainsKey (pr.Config))
					d [pr.Benchmark].Add (pr.Config, new Dictionary<Revision, ProfileResult> ());

				if (!d [pr.Benchmark][pr.Config].ContainsKey (pr.Revision))
					d [pr.Benchmark][pr.Config].Add (pr.Revision, pr);
				else if (d [pr.Benchmark][pr.Config][pr.Revision].DateTime < pr.DateTime)
					d [pr.Benchmark][pr.Config][pr.Revision] = pr;

				return d;
			});

		Console.WriteLine ("Generating graphs in \"{0}\"", resultsfolder);

		Parallel.ForEach (profiles, benchmark => {
			var plot = new PlotModel {
				Title = benchmark.Key.Name,
				LegendPlacement = LegendPlacement.Outside,
				LegendPosition = LegendPosition.RightMiddle,
				LegendOrientation = LegendOrientation.Vertical,
				LegendBorderThickness = 0,
			};

			plot.Axes.Add (new LinearAxis { Minimum = 0, MinimumPadding = 0.1, MaximumPadding = 0.1 });

			foreach (var config in benchmark.Value) {
				var values = config.Value
						.OrderBy (kv => kv.Value.DateTime)
						.Select (revision => revision.Value.Runs.Select (run => run.WallClockTime.TotalMilliseconds).ToArray ())
						.ToArray ();

				var serie = new LineSeries { Color = OxyColors.Automatic };

				for (var i = 0; i < values.Length; ++i)
					serie.Points.Add (new DataPoint ((double) i, values [i].Sum () / values [i].Length));

				plot.Series.Add (serie);
			}

			using (var stream = new FileStream (Path.Combine (resultsfolder, benchmark.Key.Name + ".svg"), FileMode.Create))
				SvgExporter.Export (plot, stream, 1024, 768, true);
		});

		Console.WriteLine ("Uploading graphs");
		SCPToRemote (sshkey, String.Join (" ", Directory.EnumerateFiles (resultsfolder, "*.svg")), String.Format ("/volume1/storage/benchmarker/graphs/{0}/{1}", project, architecture));
	}

	static void SCPFromRemote (string sshkey, string files, string destination)
	{
		sshkey = String.IsNullOrWhiteSpace (sshkey) ? String.Empty : ("-i '" + sshkey + "'");

		Process.Start (new ProcessStartInfo {
			FileName = "scp",
			Arguments = String.Format ("-r -B {0} builder@nas.bos.xamarin.com:'{1}' {2}", sshkey, files, destination),
			UseShellExecute = true,
		}).WaitForExit ();
	}

	static void SCPToRemote (string sshkey, string files, string destination, bool remove = true)
	{
		sshkey = String.IsNullOrWhiteSpace (sshkey) ? String.Empty : ("-i " + sshkey);

		if (remove) {
			Process.Start (new ProcessStartInfo {
				FileName = "ssh",
				Arguments = String.Format ("{0} builder@nas.bos.xamarin.com \"rm -rf '{1}'\"", sshkey, destination),
				UseShellExecute = true,
			}).WaitForExit ();
		}

		Process.Start (new ProcessStartInfo {
			FileName = "ssh",
			Arguments = String.Format ("{0} builder@nas.bos.xamarin.com \"mkdir -p '{1}'\"", sshkey, destination),
			UseShellExecute = true,
		}).WaitForExit ();

		Process.Start (new ProcessStartInfo {
			FileName = "scp",
			Arguments = String.Format ("{0} -r -B {1} builder@nas.bos.xamarin.com:{2}", sshkey, files, destination),
			UseShellExecute = true,
		}).WaitForExit ();
	}

	struct KeyValuePair
	{
		public static KeyValuePair<TKey, TValue> Create<TKey, TValue> (TKey key, TValue value)
		{
			return new KeyValuePair<TKey, TValue> (key, value);
		}
	}
}

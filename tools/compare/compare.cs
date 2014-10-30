using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Benchmarker.Common;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System.Reflection;
using System.Diagnostics;

class Program
{
	static void
	Usage (int exitcode = 1)
	{
		Console.Error.WriteLine ("usage: [parameters] [--] <tests-dir> <results-dir> <benchmarks-dir> <config-file> [<config-file>+]");
		Console.Error.WriteLine ("paremeters:");
		Console.Error.WriteLine ("    --help                display this help");
		Console.Error.WriteLine ("    --benchmarks, -b      benchmarks to run, separated by commas; default to all of them");
		Console.Error.WriteLine ("                             ex: -b ahcbench,db,message,raytracer2");
		Console.Error.WriteLine ("    --timeout,    -t      timeout for each benchmark execution, in seconds; default to no timeout");
		// Console.Error.WriteLine ("    --pause-time, -p      benchmark garbage collector pause times; value : true / false");
		Console.Error.WriteLine ("    --mono-exe,   -m      path to mono executable; default to system mono");
		Console.Error.WriteLine ("    --graph,      -g      path to graph to generate; default to current directory");
		Console.Error.WriteLine ("    --no-graph            do not generate a graph");
		Console.Error.WriteLine ("    --load-run    -l      load run from file, disable the run ");
		Environment.Exit (exitcode);
	}

	public static void Main (string[] args)
	{
		var benchmarksnames = new string[0];
		var timeout = Int32.MaxValue;
		var pausetime = false;
		var monoexe = String.Empty;
		var graph = "graph.svg";
		var nograph = false;
		var norun = false;

		var optindex = 0;

		for (; optindex < args.Length; ++optindex) {
			if (args [optindex] == "-b" || args [optindex] == "--benchmarks") {
				benchmarksnames = args [++optindex].Split (',').Select (s => s.Trim ()).ToArray ();
			} else if (args [optindex] == "-t" || args [optindex] == "--timeout") {
				timeout = Int32.Parse (args [++optindex]) * 1000;
			// } else if (args [optindex] == "-p" || args [optindex] == "--pause-time") {
			// 	pausetime = Boolean.Parse (args [++optindex]);
			} else if (args [optindex] == "-m" || args [optindex] == "--mono-exe") {
				monoexe = args [++optindex];
			} else if (args [optindex] == "-g" || args [optindex] == "--graph") {
				graph = args [++optindex];
			} else if (args [optindex] == "--no-graph") {
				nograph = true;
			} else if (args [optindex] == "--no-run") {
				norun = true;
			} else if (args [optindex].StartsWith ("--help")) {
				Usage (0);
			} else if (args [optindex] == "--") {
				optindex += 1;
				break;
			} else if (args [optindex].StartsWith ("-")) {
				Console.Error.WriteLine ("unknown parameter {0}", args [optindex]);
				Usage ();
			} else {
				break;
			}
		}

		if (args.Length - optindex < 4)
			Usage ();

		timeout = timeout == 0 ? Int32.MaxValue : timeout;

		var testsdir = args [optindex++];
		var resultsdir = args [optindex++];
		var benchmarksdir = args [optindex++];
		var configfiles = args.Skip (optindex).ToArray ();

		var benchmarks = Benchmark.LoadAllFrom (benchmarksdir, benchmarksnames).ToArray ();
		var configs = configfiles.Select (c => Config.LoadFrom (c)).ToArray ();

		var runs = new Dictionary<string, List<double[]>> (benchmarks.Length * configs.Length);

		for (var i = 0; i < benchmarks.Length; ++i) {
			var configtimes = new List<double[]> (configs.Length);

			for (var j = 0; j < configs.Length; ++j) {
				Run run;

				if (norun) {
					var runfile = Directory.EnumerateFiles (resultsdir, String.Join ("_", benchmarks [i].Name, configs [j].Name, "*"))
								.OrderByDescending (s => s)
								.FirstOrDefault ();

					if (runfile != default (string))
						run = Run.LoadFrom (runfile);
					else
						run = null;
				} else {
					if (configs [j].Count <= 0)
						throw new ArgumentOutOfRangeException (String.Format ("configs [\"{0}\"].Count <= 0", configs [j].Name));

					run = benchmarks [i].Run (configs [j], testsdir, timeout, monoexe, pausetime);
					run.StoreTo (Path.Combine (resultsdir, String.Join ("_", benchmarks [i].Name, configs [j].Name, DateTime.Now.ToString ("s"))));
				}

				configtimes.Add (run != null ? run.Times.Select (ts => ts.TotalMilliseconds).ToArray () : new double [] { -1 });
			}

			runs.Add (benchmarks [i].Name, configtimes);
		}

		if (!nograph) {
			var plot = new PlotModel {
				LegendPlacement = LegendPlacement.Outside,
				LegendPosition = LegendPosition.BottomCenter,
				LegendOrientation = LegendOrientation.Horizontal,
				LegendBorderThickness = 0,
			};

			var categoryaxis = new CategoryAxis { Position = AxisPosition.Bottom };
			var valueaxis = new LinearAxis { Position = AxisPosition.Left };

			var series = new List<ErrorColumnSeries> ();

			foreach (var config in configs) {
				var s = new ErrorColumnSeries { Title = config.Name, StrokeThickness = 1 };

				series.Add (s);
				plot.Series.Add (s);
			}

			var min = Double.NaN;
			var max = Double.NaN;

			foreach (var kv in runs) {
				var name = kv.Key;
				var configtimes = kv.Value;

				categoryaxis.Labels.Add (name);

				var ratio = Double.MaxValue;

				for (var j = 0; j < configtimes.Count && ratio == Double.MaxValue; ++j) {
					if (configtimes [j].All (ts => ts >= 0))
						ratio = configtimes [j].Sum () / configtimes [j].Length;
				}

				var means = configtimes.Select (ts => ts.Any (t => t < 0) ? 0d : ts.Sum () / ts.Count () / ratio).ToList ();
				var errors = configtimes.Select (ts => ts.Any (t => t < 0) ? 0d : ((ts.Sum () / ts.Count ()) - ts.Min ()) / ratio).ToList ();

				if (means.Any (m => m > 0)) {
					min = Math.Min (Double.IsNaN (min) ? Double.MaxValue : min, means.Where (m => m > 0).Zip (errors.Where (e => e > 0), (m, e) => m - e).Min ());
					max = Math.Max (Double.IsNaN (max) ? Double.MinValue : max, means.Where (m => m > 0).Zip (errors.Where (e => e > 0), (m, e) => m + e).Max ());
				}

				for (var j = 0; j < configtimes.Count; ++j) {
					series [j].Items.Add (new ErrorColumnItem { Value = means [j], Error = errors [j], Color = OxyColors.Automatic });
				}
			}

			valueaxis.AbsoluteMinimum = valueaxis.Minimum = min * 0.95;
			valueaxis.AbsoluteMaximum = valueaxis.Maximum = max * 1.05;

			plot.Axes.Add (categoryaxis);
			plot.Axes.Add (valueaxis);

			using (var stream = new FileStream (graph, FileMode.Create)) {
				SvgExporter.Export (plot, stream, 1024, 768, true);
			}
		}
	}
}

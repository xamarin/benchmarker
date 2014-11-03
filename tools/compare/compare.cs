using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Benchmarker.Common;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System.Diagnostics;

class Program
{
	static void
	Usage (string message = null, int exitcode = 0)
	{
		if (!String.IsNullOrEmpty (message))
			Console.Error.WriteLine ("{0}\n", message);

		Console.Error.WriteLine ("usage: [parameters] [--] <tests-dir> <results-dir> <benchmarks-dir> <config-file> [<config-file>+]");
		Console.Error.WriteLine ("paremeters:");
		Console.Error.WriteLine ("        --help            display this help");
		Console.Error.WriteLine ("    -b, --benchmarks      benchmarks to run, separated by commas; default to all of them");
		Console.Error.WriteLine ("                             ex: -b ahcbench,db,message,raytracer2");
		Console.Error.WriteLine ("    -t, --timeout         execution timeout for each benchmark, in seconds; default to no timeout");
		// Console.Error.WriteLine ("    -p, --pause-time       benchmark garbage collector pause times; value : true / false");
		Console.Error.WriteLine ("    -m, --mono-exe        path to mono executable; default to system mono");
		Console.Error.WriteLine ("    -g, --graph           output graph to FILE; default to current graph.svg in current directory");
		Console.Error.WriteLine ("    -l, --load-from       load run from FILES, separated by commas");
		Console.Error.WriteLine ("                             ex: -l results/ahcbench_all_2014-10-31T10:00:00,results/ahcbench_default_2014-10-31T10:01:00");
		Console.Error.WriteLine ("        --no-graph        disable graph generation");
		Console.Error.WriteLine ("        --no-run          disable the benchmark execution, load run from existing file in <results-dir>");
		Console.Error.WriteLine ("    -c, --counter         compare value of COUNTER");
		Console.Error.WriteLine ("        --geomean         output geometric mean of the relative execution speed for each config");
		Console.Error.WriteLine ("        --minimum         only include benchmarks where reference value is at least VALUE");

		Environment.Exit (exitcode);
	}

	public static void Main (string[] args)
	{
		var benchmarksnames = new string[0];
		var timeout = Int32.MaxValue;
		var pausetime = false;
		var monoexe = String.Empty;
		var graph = "graph.svg";
		var loadrunfrom = new string [0];
		var nograph = false;
		var norun = false;
		var counter = String.Empty;
		var geomean = false;
		var minimum = Double.NaN;

		var optindex = 0;

		for (; optindex < args.Length; ++optindex) {
			if (args [optindex] == "-b" || args [optindex] == "--benchmarks") {
				benchmarksnames = args [++optindex].Split (',').Select (s => s.Trim ()).Union (benchmarksnames).ToArray ();
			} else if (args [optindex] == "-t" || args [optindex] == "--timeout") {
				timeout = Int32.Parse (args [++optindex]) * 1000;
				timeout = timeout == 0 ? Int32.MaxValue : timeout;
			// } else if (args [optindex] == "-p" || args [optindex] == "--pause-time") {
			// 	pausetime = Boolean.Parse (args [++optindex]);
			} else if (args [optindex] == "-m" || args [optindex] == "--mono-exe") {
				monoexe = args [++optindex];
			} else if (args [optindex] == "-g" || args [optindex] == "--graph") {
				graph = args [++optindex];
			} else if (args [optindex] == "-l" || args [optindex] == "--load-from") {
				loadrunfrom = args [++optindex].Split (',').Select (s => s.Trim ()).Union (loadrunfrom).ToArray ();
			} else if (args [optindex] == "--no-graph") {
				nograph = true;
			} else if (args [optindex] == "--no-run") {
				norun = true;
			} else if (args [optindex] == "-c" || args [optindex] == "--counter") {
				counter = args [++optindex];
			} else if (args [optindex] == "--geomean") {
				geomean = true;
			} else if (args [optindex] == "--minimum") {
				minimum = Double.Parse (args [++optindex]);
			} else if (args [optindex].StartsWith ("--help")) {
				Usage ();
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

		if (norun && nograph)
			Usage ("You cannot disable run and graph at the same time", 1);

		if (!norun && loadrunfrom.Length > 0)
			Usage ("You cannot load a run from a file if you run the benchmarks", 1);

		if (args.Length - optindex < 4)
			Usage (null, 1);

		var testsdir = args [optindex++];
		var resultsdir = args [optindex++];
		var benchmarksdir = args [optindex++];
		var configfiles = args.Skip (optindex).ToArray ();

		var benchmarks = Benchmark.LoadAllFrom (benchmarksdir, benchmarksnames).OrderBy (b => b.Name).ToArray ();
		var configs = configfiles.Select (c => Config.LoadFrom (c)).ToArray ();

		var runs = new Dictionary<string, List<Run.Time[]>> (benchmarks.Length * configs.Length);

		/* Run or load the benchmarks */
		for (var i = 0; i < benchmarks.Length; ++i) {
			var configtimes = new List<Run.Time[]> (configs.Length);

			for (var j = 0; j < configs.Length; ++j) {
				var runfileprefix = String.Join ("_", benchmarks [i].Name, configs [j].Name, "");

				if (!norun) {
					/* Run the benchmarks */
					if (configs [j].Count <= 0)
						throw new ArgumentOutOfRangeException (String.Format ("configs [\"{0}\"].Count <= 0", configs [j].Name));

					var run = benchmarks [i].Run (configs [j], testsdir, timeout, monoexe, pausetime);
					run.StoreTo (Path.Combine (resultsdir, runfileprefix + DateTime.Now.ToString ("s")));

					configtimes.Add (run.Times.ToArray ());
				} else {
					/* Load the benchmarks */
					var runfile = loadrunfrom.FirstOrDefault (f => Path.GetFileName (f).StartsWith (runfileprefix));

					if (runfile == default (string))
						runfile = Directory.EnumerateFiles (resultsdir, runfileprefix + "*")
									.OrderByDescending (s => s)
									.FirstOrDefault ();

					if (runfile == default (string)) {
						configtimes.Add (new Run.Time[] { new Run.Time { Value = TimeSpan.Zero } });
					} else {
						var run = Run.LoadFrom (runfile);
						if (run == null)
							throw new InvalidDataException (String.Format ("Cannot load Run from {0}", runfile));

						configtimes.Add (run.Times.ToArray ());
					}
				}
			}

			runs.Add (benchmarks [i].Name, configtimes);
		}

		/* Generate the graph */
		if (!nograph) {
			var categoryaxis = new CategoryAxis { Position = AxisPosition.Bottom };
			var valueaxis = new LinearAxis { Position = AxisPosition.Left };

			var series = new List<ErrorColumnSeries> ();

			foreach (var config in configs)
				series.Add (new ErrorColumnSeries { Title = config.Name, StrokeThickness = 1 });

			if (geomean)
				series.Add (new ErrorColumnSeries { Title = "geomean", StrokeThickness = 1 });

			var min = Double.NaN;
			var max = Double.NaN;

			foreach (var kv in runs) {
				var name = kv.Key;
				var configtimes = kv.Value;

				Debug.Assert (series.Count == configtimes.Count + (geomean ? 1 : 0));

				categoryaxis.Labels.Add (name);

				if (configtimes.Any (ts => ts.Any (t => t.Value == TimeSpan.Zero))) {
					Console.Out.WriteLine ("Don't have data for \"{0}\" in all configurations - removing", name);

					for (var j = 0; j < series.Count; ++j)
						series [j].Items.Add (new ErrorColumnItem { Value = 0, Error = 0, Color = OxyColors.Automatic });
				} else {
					var values = configtimes.Select (ts => ts.Select (t => {
						if (String.IsNullOrEmpty (counter)) {
							return t.Value.TotalMilliseconds;
						} else {
							var line = t.Output.Split (Environment.NewLine.ToCharArray ()).Where (s => s.StartsWith (counter)).LastOrDefault ();
							if (line == default (string) || !line.Contains (':'))
								throw new InvalidDataException (String.Format ("The value \"{0}\" passed with --counter is not a valid counter", counter));

							var value = new string (line.Split (new char [] { ':' }, 2)
											.ElementAt (1)
											.Trim ()
											.ToCharArray ()
											.TakeWhile (c => (c >= '0' && c <= '9') || c == '.' || c == ',')
											.ToArray ());

							try {
								return Double.Parse (value);
							} catch (FormatException) {
								Console.Error.WriteLine ("could not parse value \"{0}\" for counter \"{1}\"", value, counter);
								Environment.Exit (1);
								// Silent the compiler
								return Double.NaN;
							}
						}
					}).ToList ()).ToList ();

					var means = values.Select (ts => ts.Sum () / ts.Count ());
					var errors = values.Zip (means, (ts, m) => m - ts.Min ());

					if (minimum != Double.NaN && means.Any (m => m < minimum)) {
						Console.Out.WriteLine ("Mean value for \"{0}\" us below minimum - removing", name);

						for (var j = 0; j < series.Count; ++j)
							series [j].Items.Add (new ErrorColumnItem { Value = 0, Error = 0, Color = OxyColors.Automatic });
					} else {
						var ratio = means.ElementAt (0);

						var nmeans = means.Select (m => m / ratio).ToList ();
						var nerrors = errors.Select (e => e / ratio).ToList ();

						if (geomean) {
							nmeans.Add (Math.Pow (nmeans.Aggregate (1d, (a, m) => m * a), 1d / nmeans.Count));
							nerrors.Add (0d);
						}

						min = Math.Min (Double.IsNaN (min) ? Double.MaxValue : min, nmeans.Zip (nerrors, (m, e) => m - e).Min ());
						max = Math.Max (Double.IsNaN (max) ? Double.MinValue : max, nmeans.Zip (nerrors, (m, e) => m + e).Max ());

						for (var j = 0; j < series.Count; ++j)
							series [j].Items.Add (new ErrorColumnItem { Value = nmeans [j], Error = nerrors [j], Color = OxyColors.Automatic });
					}
				}
			}

			valueaxis.AbsoluteMinimum = valueaxis.Minimum = min * 0.95;
			valueaxis.AbsoluteMaximum = valueaxis.Maximum = max * 1.05;

			var plot = new PlotModel {
				LegendPlacement = LegendPlacement.Outside,
				LegendPosition = LegendPosition.BottomCenter,
				LegendOrientation = LegendOrientation.Horizontal,
				LegendBorderThickness = 0,
			};

			foreach (var serie in series)
				plot.Series.Add (serie);

			plot.Axes.Add (categoryaxis);
			plot.Axes.Add (valueaxis);

			using (var stream = new FileStream (graph, FileMode.Create)) {
				SvgExporter.Export (plot, stream, 1024, 768, true);
			}
		}
	}
}

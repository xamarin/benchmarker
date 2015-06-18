using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Benchmarker.Common.Models;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System.Threading.Tasks;

class Compare
{
	static OxyColor [] XamarinColors = new [] {
		OxyColor.FromRgb (119, 208, 101), // Green
		OxyColor.FromRgb ( 44,  62,  80), // Dark Blue
		OxyColor.FromRgb (180,  85, 182), // Purple
		OxyColor.FromRgb (180, 188, 188), // Light Gray
		OxyColor.FromRgb ( 52, 152, 219), // Blue
		OxyColor.FromRgb (115, 129, 130), // Gray
	};

	static void UsageAndExit (string message = null, int exitcode = 0)
	{
		if (!String.IsNullOrEmpty (message))
			Console.Error.WriteLine ("{0}\n", message);

		Console.Error.WriteLine ("usage:          [parameters] [--] <tests-dir> <results-dir> <benchmarks-dir> <config-file> [<config-file>+]");
		Console.Error.WriteLine ("       --no-run [parameters] [--] <tests-dir> <results-dir> <benchmarks-dir> [<config-name>+]");
		Console.Error.WriteLine ("parameters:");
		Console.Error.WriteLine ("        --help            display this help");
		Console.Error.WriteLine ("    -b, --benchmarks      benchmarks to run, separated by commas; default to all of them");
		Console.Error.WriteLine ("                             ex: -b ahcbench,db,message,raytracer2");
		Console.Error.WriteLine ("    -t, --timeout         execution timeout for each benchmark, in seconds; default to no timeout");
		// Console.Error.WriteLine ("    -p, --pause-time       benchmark garbage collector pause times; value : true / false");
		Console.Error.WriteLine ("    -m, --mono-exe        path to mono executable; default to system mono");
		Console.Error.WriteLine ("    -g, --graph           output graph to FILE; default to \"graph.svg\" in current directory");
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
		var loadresultfrom = new string [0];
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
				loadresultfrom = args [++optindex].Split (',').Select (s => s.Trim ()).Union (loadresultfrom).ToArray ();
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

		if (norun && nograph)
			UsageAndExit ("You cannot disable run and graph at the same time", 1);

		if (!norun && loadresultfrom.Length > 0)
			UsageAndExit ("You cannot load a run from a file if you run the benchmarks", 1);

		if (args.Length - optindex < (norun ? 3 : 4))
			UsageAndExit (null, 1);

		var testsdir = args [optindex++];
		var resultsdir = args [optindex++];
		var benchmarksdir = args [optindex++];
		var configfiles = args.Skip (optindex).ToArray ();

		List<Result> results = new List<Result> ();

		if (norun) {
			foreach (var resultfile in (loadresultfrom.Length > 0 ? loadresultfrom : Directory.EnumerateFiles (resultsdir, "*.json"))) {
				var result = Result.LoadFrom (resultfile);
				if (result == null)
					throw new InvalidDataException (String.Format ("Cannot load Result from {0}", resultfile));

				foreach (var r in results) {
					if (!r.Benchmark.Equals(result.Benchmark) || !r.Config.Equals (result.Config))
						continue;

					if (r.DateTime < result.DateTime)
						results.Remove (r);

					break;
				}

				// If we have been given configuration names, the result's
				// configuration must match one of them.
				if (configfiles.Length > 0 && !configfiles.Any (n => n == result.Config.Name))
					continue;

				results.Add (result);
			}
		} else {
			var configs = configfiles.Select (c => Config.LoadFrom (c)).ToList ();

			/* Run or load the benchmarks */
			foreach (var benchmark in Benchmark.LoadAllFrom (benchmarksdir, benchmarksnames).OrderBy (b => b.Name)) {
				foreach (var config in configs) {
					var runfileprefix = String.Join ("_", benchmark.Name, config.Name, "");
					var version = String.Empty;

					/* Run the benchmarks */
					if (config.Count <= 0)
						throw new ArgumentOutOfRangeException (String.Format ("configs [\"{0}\"].Count <= 0", config.Name));

					Console.Out.WriteLine ("Running benchmark \"{0}\" with config \"{1}\"", benchmark.Name, config.Name);

					var info = new ProcessStartInfo () {
						WorkingDirectory = Path.Combine (testsdir, benchmark.TestDirectory),
						UseShellExecute = false,
						RedirectStandardOutput = true,
						RedirectStandardError = true,
					};

					if (config.NoMono) {
						info.FileName = Path.Combine (info.WorkingDirectory, benchmark.CommandLine [0]);
						benchmark.CommandLine = benchmark.CommandLine.Skip (1).ToArray ();
					} else {
						info.FileName = !String.IsNullOrEmpty (monoexe) ? monoexe : !String.IsNullOrEmpty (config.Mono) ? config.Mono : "mono";
					}

					foreach (var env in config.MonoEnvironmentVariables)
						info.EnvironmentVariables.Add (env.Key, env.Value);

					var envvar = String.Join (" ", config.MonoEnvironmentVariables.Select (kv => kv.Key + "=" + kv.Value));
					var arguments = String.Join (" ", config.MonoOptions.Union (benchmark.CommandLine));

					if (!config.NoMono) {
						/* Run without timing with --version */
						info.Arguments = "--version " + arguments;

						Console.Out.WriteLine ("\t$> {0} {1} {2}", envvar, info.FileName, info.Arguments);

						var process1 = Process.Start (info);
						version = Task.Run (() => new StreamReader (process1.StandardOutput.BaseStream).ReadToEnd ()).Result;
						var versionerror = Task.Run (() => new StreamReader (process1.StandardError.BaseStream).ReadToEnd ());

						process1.WaitForExit ();
					} else {
						info.Arguments = arguments;
					}

					/* Run with timing */
					if (!config.NoMono)
						info.Arguments = "--stats " + arguments;

					var result = new Result {
						DateTime = DateTime.Now,
						Benchmark = benchmark,
						Config = config,
						Version = version,
						Timedout = false,
						Runs = new Result.Run [config.Count]
					};

					for (var i = 0; i < config.Count + 1; ++i) {
						Console.Out.WriteLine ("\t$> {0} {1} {2}", envvar, info.FileName, info.Arguments);
						Console.Out.Write ("\t\t-> {0} ", i == 0 ? "[dry run]" : String.Format ("({0}/{1})", i, config.Count));

						timeout = benchmark.Timeout > 0 ? benchmark.Timeout : timeout;

						var sw = Stopwatch.StartNew ();

						var process = Process.Start (info);
						var stdout = Task.Factory.StartNew (() => new StreamReader (process.StandardOutput.BaseStream).ReadToEnd (), TaskCreationOptions.LongRunning);
						var stderr = Task.Factory.StartNew (() => new StreamReader (process.StandardError.BaseStream).ReadToEnd (), TaskCreationOptions.LongRunning);
						var success = process.WaitForExit (timeout < 0 ? -1 : (Math.Min (Int32.MaxValue / 1000, timeout) * 1000));

						sw.Stop ();

						if (!success)
							process.Kill ();

						Console.Out.WriteLine (success ? sw.ElapsedMilliseconds.ToString () + "ms" : "timeout!");

						// skip first one
						if (i > 0) {
							result.Runs [i - 1] = new Result.Run {
								WallClockTime = success ? TimeSpan.FromMilliseconds (sw.ElapsedMilliseconds) : TimeSpan.Zero,
								Output = success ? stdout.Result : null,
								Error = success ? stderr.Result : null
							};

							result.Timedout = result.Timedout || !success;
						}

						process.Close ();
					}

					// FIXME: implement pausetime
					if (pausetime)
						throw new NotImplementedException ();

					result.StoreTo (Path.Combine (resultsdir, runfileprefix + DateTime.Now.ToString ("s").Replace (':', '-') + ".json"));
					results.Add (result);
				}
			}
		}

		/* Generate the graph */
		if (!nograph) {
			Console.WriteLine ("Generate graph in \"{0}\"", graph);
			var plot = new PlotModel {
				LegendPlacement = LegendPlacement.Outside,
				LegendPosition = LegendPosition.TopCenter,
				LegendOrientation = LegendOrientation.Horizontal,
				LegendBorderThickness = 0,
				Padding = new OxyThickness (10, 0, 0, 75),
				DefaultColors = XamarinColors,
			};

			var categoryaxis = new CategoryAxis { Position = AxisPosition.Bottom, Angle = 90 };
			var valueaxis = new LinearAxis {
				Position = AxisPosition.Left,
				MinorGridlineStyle = LineStyle.Automatic,
				MajorGridlineStyle = LineStyle.Automatic,
			};

			plot.Axes.Add (categoryaxis);
			plot.Axes.Add (valueaxis);

			var benchmarks = results
				.GroupBy (r => r.Benchmark)
				.Select (benchmark => {
					var benchmarkconfigs = benchmark.Select (r => r.Config).ToArray ();

					Debug.Assert (benchmarkconfigs.Length == benchmarkconfigs.Distinct ().Count (), "There are duplicate configs for benchmark \"{0}\" : {1}",
						benchmark.Key.Name, String.Join (", ", benchmarkconfigs.OrderBy (c => c.Name).Select (c => c.Name)));

					if (benchmark.Any (r => r.Runs.Any (ru => ru.WallClockTime == TimeSpan.Zero || !string.IsNullOrEmpty(ru.Error)))) {
						Console.WriteLine ("Don't have data for benchmark \"{0}\" in all configs - removing", benchmark.Key.Name);
						return null;
					}

					double[][] values;

					try {
						counter = String.IsNullOrWhiteSpace (counter) ? "Time" : counter;
						values = benchmark.Select (r => r.Runs.Select (ru => ExtractCounterValue (counter, ru)).ToArray ()).ToArray ();
					} catch (FormatException) {
						Console.Error.WriteLine ("Could not parse value for counter \"{1}\"", counter);
						return null;
					}

					double[] means = values.Select (vs => vs.Sum () / vs.Length).ToArray ();
					double[] errors = values.Zip (means, (vs, m) => m - vs.Min ()).ToArray ();

					if (minimum != Double.NaN && means.Any (m => m < minimum)) {
						Console.WriteLine ("Mean value for benchmark \"{0}\" below minimum - removing", benchmark.Key.Name);
						return null;
					}

					double ratio = means.ElementAt (0);

					double[] nmeans = means.Select (m => m / ratio).ToArray ();
					double[] nerrors = errors.Select (e => e / ratio).ToArray ();

					Debug.Assert (benchmarkconfigs.Length == nmeans.Length);
					Debug.Assert (benchmarkconfigs.Length == nerrors.Length);

					return new { Benchmark = benchmark.Key, Configs = benchmarkconfigs, NormalizedMeans = nmeans, NormalizedErrors = nerrors };
				})
				.Where (t => t != null)
				.ToArray ();

			foreach (var n in benchmarks.Select (v => v.Benchmark.Name).Distinct ())
				categoryaxis.Labels.Add (n);

			var configs = benchmarks
				.SelectMany (b => b.Configs.Select ((c, i) => new { Benchmark = b.Benchmark, Config = c, NormalizedMean = b.NormalizedMeans [i], NormalizedError = b.NormalizedErrors [i] }))
				.GroupBy (v => v.Config);

			foreach (var config in configs) {
				var serie = new ErrorColumnSeries { Title = config.Key.Name, LabelFormatString = "{0:F2}", StrokeThickness = 1 };

				var nmeans = config.Select (c => c.NormalizedMean).ToArray ();
				var nerrors = config.Select (c => c.NormalizedError).ToArray ();

				for (int i = 0, l = nmeans.Length; i < l; ++i) {
					serie.Items.Add (new ErrorColumnItem { Value = nmeans [i], Error = nerrors [i], Color = OxyColors.Automatic });
				}

				plot.Series.Add (serie);
			}

			if (geomean) {
				var geomeanserie = new ColumnSeries { Title = "geomean", LabelFormatString = "{0:F2}", StrokeThickness = 1 };

				foreach (var v in benchmarks) {
					geomeanserie.Items.Add (new ColumnItem {
						Value = Math.Pow (v.NormalizedMeans.Aggregate (1d, (a, m) => m * a), 1d / v.NormalizedMeans.Length),
						Color = OxyColors.Automatic,
					});
				}

				plot.Series.Add (geomeanserie);
			}

			valueaxis.AbsoluteMinimum = valueaxis.Minimum = benchmarks.Aggregate (Double.MaxValue, (a, v) => Math.Min (a, v.NormalizedMeans.Zip (v.NormalizedErrors, (m, e) => m - e).Min ())) * 0.99;
			valueaxis.AbsoluteMaximum = valueaxis.Maximum = benchmarks.Aggregate (Double.MinValue, (a, v) => Math.Max (a, v.NormalizedMeans.Zip (v.NormalizedErrors, (m, e) => m + e).Max ())) * 1.01;

			using (var stream = new FileStream (graph, FileMode.Create))
				SvgExporter.Export (plot, stream, 1024, 768, true);
		}
	}

	static Double ExtractCounterValue (string counter, Result.Run run)
	{

		if (counter == "Time") {
			return run.WallClockTime.TotalMilliseconds;
		} else {
			var line = run.Output.Split (Environment.NewLine.ToCharArray ()).Where (s => s.StartsWith (counter)).LastOrDefault ();
			if (line == default (string) || !line.Contains (':'))
				throw new FormatException (String.Format ("The value \"{0}\" passed with --counter is not a valid counter", counter));

			var value = new string (line.Split (new char [] { ':' }, 2)
				.ElementAt (1)
				.Trim ()
				.ToCharArray ()
				.TakeWhile (c => (c >= '0' && c <= '9') || c == '.' || c == ',')
				.ToArray ());

			return Double.Parse (value);
		}
	}
}

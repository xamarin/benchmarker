using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;

using XamarinProfiler.Core;

class Program
{
	static void
	Usage ()
	{
		Console.Error.WriteLine ("usage: [parameters] [--] proflog-out [proflog-out...] ");
		Console.Error.WriteLine ("paremeters:");
		Console.Error.WriteLine ("    --help           display this help");
		Console.Error.WriteLine ("    -n, --names=     counters names to display (case insensitive)");
		Console.Error.WriteLine ("                       ex: -c \"Minor GC time,Minor GC collections\"");
		Console.Error.WriteLine ("    -s, --sections=  counters sections to display (case insensitive)");
		Console.Error.WriteLine ("                       ex: -s \"Mono JIT,Mono GC\"");
		Console.Error.WriteLine ("    -c, --columns=   # of columns to display");
		Console.Error.WriteLine ("    -h, --height=    height of each graph");
		Console.Error.WriteLine ("    -w, --width=     width of each graph");
	}

	static ProfileRun
	CreateProfileRun (string filename)
	{
		var options = new ProfileOptions () { ExecutableFileName = filename };
		var profiler = new Profiler () { Options = options };
		var run = profiler.CreateRun ();

		run.ConsoleOutputReceived += (sender, e) => Console.Write ("[RUNNER] " + e.Data + "\n");
		run.Start ();

		return run;
	}

	static SortedDictionary <ulong, SortedDictionary<PerfCounter, object>>
	BuildCounters (IEnumerable<PerfCounter> counters, string[] sections, string[] names)
	{
		var result = new SortedDictionary <ulong, SortedDictionary<PerfCounter, object>> ();
		var history = new Dictionary<PerfCounter, object> ();

		var timestamps = counters.Aggregate (new HashSet <ulong> (), (s, c) => { s.UnionWith (c.Values.Select (v => v.TimeStamp)); return s; })
									.ToArray ();

		if (names.Length > 0)
			counters = counters.Where (c => names.Any (n => n.ToLower () == c.Name.ToLower ()));
		if (sections.Length > 0)
			counters = counters.Where (c => sections.Any (s => s.ToLower () == c.Section.ToLower ()));

		foreach (var t in timestamps) {
			var values = new SortedDictionary <PerfCounter, object> (history);

			foreach (var c in counters) {
				var val = c.Values.Where (v => v.TimeStamp == t).Select (v => v.Value).SingleOrDefault ();

				if (val == null && history.ContainsKey (c))
					val = history [c];

				if (val == null)
					continue;

				if (values.ContainsKey (c))
					values [c] = val;
				else
					values.Add (c, val);

				if (history.ContainsKey (c))
					history [c] = val;
				else
					history.Add (c, val);
			}

			result.Add (t, values);
		}

		return result;
	}

	static void
	DumpGnuPlot (TextWriter stream, int columns, int width, int height, List<Tuple<string, SortedDictionary <ulong, SortedDictionary<PerfCounter, object>>>> runs)
	{
		var counters = new List <PerfCounter> ();

		foreach (var r in runs) {
			foreach (var cs in r.Item2.Values) {
				counters.AddRange (cs.Keys);
			}
		}

		counters = counters.Distinct ().ToList ();

		var rows = (int) Math.Ceiling ((double)(counters.Count + 1) / columns);

		stream.WriteLine ("reset");
		stream.WriteLine ("set terminal png size {0},{1}", width * columns, height * rows);
		stream.WriteLine ("set xlabel \"Time (in ms)\"");
		stream.WriteLine ("set grid");
		stream.WriteLine ("set style data linespoints");
		stream.WriteLine ("set yrange [0:]");
		stream.WriteLine ("set xrange [{0}:]", runs.Min (r => r.Item2.Keys.Min ()));
		stream.WriteLine ("set key box opaque");
		stream.WriteLine ("set linetype 1 lc rgb \"dark-violet\" lw 2");
		stream.WriteLine ("set linetype 2 lc rgb \"sea-green\" lw 2");
		stream.WriteLine ("set linetype 3 lc rgb \"cyan\" lw 2");
		stream.WriteLine ("set linetype 4 lc rgb \"dark-red\" lw 2");
		stream.WriteLine ("set linetype 5 lc rgb \"blue\" lw 2");
		stream.WriteLine ("set linetype 6 lc rgb \"dark-orange\" lw 2");
		stream.WriteLine ("set linetype 7 lc rgb \"black\" lw 2");
		stream.WriteLine ("set linetype 8 lc rgb \"goldenrod\" lw 2");
		stream.WriteLine ("set linetype cycle 8");

		stream.WriteLine ("set key at screen {0}, screen {1} vertical nobox", 1, (double)0.95 / rows, width, height);
		stream.WriteLine ("set multiplot layout {0},{1}", rows, columns);

		foreach (var c in counters) {
			stream.WriteLine ("\nset title \"{0}\"", c);

			for (int j = 0, c2 = runs.Count; j < c2; ++j) {
				stream.Write ("{0}", j == 0 ? "plot " : "     ");
				stream.Write ("'-' using 1:2 title '{0}'", runs [j].Item1);
				stream.Write ("{0}\n", j < c2 -1 ? ", \\" : "");
			}

			foreach (var r in runs) {
				foreach (var t in r.Item2) {
					if (!t.Value.ContainsKey (c))
						continue;

					stream.WriteLine ("\t{0}\t{1}", t.Key, t.Value [c]);
				}
				stream.WriteLine ("e");
			}
		}

		stream.WriteLine ("unset multiplot");
	}

	public static void Main (string[] args)
	{
		if (args.Length < 1) {
			Usage ();
			Environment.Exit (1);
		}

		var sections = new string [0];
		var names = new string [0];

		var columns = 2;
		var width = 640;
		var height = 480;

		var optindex = 0;

		for (; optindex < args.Length; ++optindex) {
			if (args [optindex] == "-n" || args [optindex].StartsWith ("--names=")) {
				names = (args [optindex] == "-n" ? args [++optindex] : args [optindex].Substring ("--names=".Length)).Split (',').Select (s => s.Trim ()).ToArray ();
			} else if (args [optindex] == "-s" || args [optindex].StartsWith ("--sections=")) {
				sections = (args [optindex] == "-s" ? args [++optindex] : args [optindex].Substring ("--sections=".Length)).Split (',').Select (s => s.Trim ()).ToArray ();
			} else if (args [optindex] == "-c" || args [optindex].StartsWith ("--columns=")) {
				columns = Int32.Parse (args [optindex] == "-c" ? args [++optindex] : args [optindex].Substring ("--columns=".Length));
			} else if (args [optindex] == "-h" || args [optindex].StartsWith ("--height=")) {
				height = Int32.Parse (args [optindex] == "-h" ? args [++optindex] : args [optindex].Substring ("--height=".Length));
			} else if (args [optindex] == "-w" || args [optindex].StartsWith ("--width=")) {
				width = Int32.Parse (args [optindex] == "-w" ? args [++optindex] : args [optindex].Substring ("--width=".Length));
			} else if (args [optindex].StartsWith ("--help")) {
				Usage ();
				Environment.Exit (0);
			} else if (args [optindex] == "--") {
				optindex += 1;
				break;
			} else if (args [optindex].StartsWith ("-")) {
				Console.Error.WriteLine ("unknown parameter {0}", args [optindex]);
				Usage ();
				Environment.Exit (1);
			} else {
				break;
			}
		}

		// var sw = new Stopwatch ();
		// sw.Start ();

		var runs = args.Skip (optindex).Select (a => Tuple.Create (Path.GetFileName (a), CreateProfileRun (a))).ToList ();

		while (runs.Any (r => r.Item2.IsRunning))
			Thread.Sleep (10);

		// sw.Stop ();
		// Console.Error.WriteLine ("CreateProfileRun : {0}ms", sw.ElapsedMilliseconds);
		//
		// sw.Restart ();
	
		var history = runs.Select (r => Tuple.Create (r.Item1, BuildCounters (r.Item2.ProfileMetadata.GetPerfCounters (), sections, names))).ToList ();

		// sw.Stop ();
		// Console.Error.WriteLine ("BuildCounters : {0}ms", sw.ElapsedMilliseconds);
		//
		// sw.Restart ();

		DumpGnuPlot (Console.Out, columns, width, height, history);

		// sw.Stop ();
		// Console.Error.WriteLine ("DumpGnuPlot : {0}ms", sw.ElapsedMilliseconds);
	}
}

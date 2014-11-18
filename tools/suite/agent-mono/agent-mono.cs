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

		var datetimestart = DateTime.Now;

		var benchmarks = Benchmark.LoadAllFrom (benchmarksdir, benchmarksnames).OrderBy (b => b.Name).ToArray ();
		var configs = configfiles.Select (c => Config.LoadFrom (c)).ToArray ();

		var revision = String.IsNullOrEmpty (commit) ? Revision.Last ("mono", architecture) : Revision.Get ("mono", architecture, commit);
		if (revision == null) {
			Console.Out.WriteLine ("Revision not found");
			Environment.Exit (2);
		}

		var revisionfolder = Directory.CreateDirectory (Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ())).FullName;
		var profilesfolder = Directory.CreateDirectory (Path.Combine (revisionfolder, String.Join ("_", datetimestart.ToString ("s").Replace (':', '-'), revision.Commit))).FullName;

		if (!revision.FetchInto (revisionfolder))
			Environment.Exit (0);

		var profiles = new List<ProfileResult> (benchmarks.Length * configs.Length);

		foreach (var benchmark in benchmarks) {
			foreach (var config in configs) {
				profiles.Add (benchmark.Profile (config, revision, revisionfolder, profilesfolder, testsdir, timeout));
			}
		}

		Parallel.ForEach (profiles, profile => {
			Parallel.ForEach (profile.Runs, run => {
				run.Counters = ProfileResult.Run.ParseCounters (profilesfolder + run.ProfilerOutput);
			});

			profile.StoreTo (Path.Combine (profilesfolder, profile.ToString () + ".json.gz"), true);
		});

		Console.Out.WriteLine ("Copying files to storage");

		SCP (sshkey, profilesfolder, "/runs");
	}

	static void SCP (string sshkey, string files, string destination)
	{
		var info = new ProcessStartInfo {
			FileName = "scp",
			Arguments = String.Format ("-r -B {0} {1} builder@nas.bos.xamarin.com:/volume1/storage/benchmarker{2}", String.IsNullOrWhiteSpace (sshkey) ? "" : ("-i " + sshkey), files, destination),
			UseShellExecute = true,
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

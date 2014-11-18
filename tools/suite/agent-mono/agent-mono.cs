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

		var countersfolder = Directory.CreateDirectory (Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ())).FullName;

		var profiles = new List<ProfileResult> (benchmarks.Length * configs.Length);

		foreach (var benchmark in benchmarks) {
			foreach (var config in configs) {
				var profile = benchmark.Profile (config, revision, revisionfolder, profilesfolder, testsdir, timeout);

				profile.StoreTo (Path.Combine (profilesfolder, profile.ToString () + ".json"));

				profiles.Add (profile);
			}
		}

		var serializer = new JsonSerializer { Formatting = Formatting.Indented };

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

			var countersfilename = String.Join ("_", new string [] { profile.Benchmark.Name, profile.Config.Name,
				datetimestart.ToString ("s").Replace (':', '-'), revision.Commit }.Select (s => s.Replace ('_', '-'))) + ".json.gz";

			using (var writer = new StreamWriter (new GZipStream (new FileStream (Path.Combine (countersfolder, countersfilename), FileMode.Create), CompressionMode.Compress)))
				serializer.Serialize (writer, counters);
		}

		Console.Out.WriteLine ("Copying files to storage");

		SCP (sshkey, profilesfolder, "/runs");
		SCP (sshkey, String.Join (" ", Directory.EnumerateFiles (countersfolder, "*.json.gz")), "/counters");
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

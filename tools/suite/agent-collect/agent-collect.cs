using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Benchmarker.Common.LogProfiler;
using Benchmarker.Common.Models;
using Newtonsoft.Json;

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
		var graphsfolder = Directory.CreateDirectory (Path.Combine (resultsfolder, "graphs")).FullName;

		Console.WriteLine ("Downloading results files into \"{0}\"", resultsfolder);

		RSyncFromRemote (sshkey, String.Format ("/volume1/storage/benchmarker/runs/{0}/{1}/*", project, architecture), resultsfolder);

		Console.WriteLine ("Reading results files");

		IEnumerable<ProfileResult> profiles = Directory.EnumerateFiles (resultsfolder, "*.json.gz", SearchOption.AllDirectories)
			.AsParallel ()
			.Where (f => !f.EndsWith (".counters.json.gz"))
			.Select (f => ProfileResult.LoadFrom (f, true));

		if (benchmarksnames.Length > 0)
			profiles = profiles.Where (p => benchmarksnames.Any (n => p.Benchmark.Name == n));

		profiles = profiles.AsParallel ().ToArray ();

		Console.WriteLine ("Generating graphs data in \"{0}\"", graphsfolder);

		{
			// Time + Counters data for each benchmark
			Parallel.ForEach (profiles.GroupBy (p => p.Benchmark), benchmark => {
				// Counter.Name | "Time" => Config.Name => List [KeyValuePair[Revision.Commit, Value]]
				var data = new Dictionary<string, Dictionary<string, List<KeyValuePair<string, double>>>> ();

				Console.Write ("Benchmark: {0}, Counter: {1}{2}", benchmark.Key.Name, "Time", Environment.NewLine);
				data.Add ("Time", BenchmarkData (benchmark.Key, benchmark, run => run.WallClockTime.TotalMilliseconds));

				foreach (var counter in ExtractIntersectCounters (benchmark)) {
					try {
						Console.Write ("Benchmark: {0}, Counter: {1}{2}", benchmark.Key.Name, counter, Environment.NewLine);
						data.Add (counter.ToString (), BenchmarkData (benchmark.Key, benchmark.AsEnumerable (), run => ExtractLastCounterValue (run, counter)));
					} catch (InvalidCastException e) {
						Console.Out.WriteLine ("Cannot convert \"{0}\" last value to double : {1}{2}", counter, Environment.NewLine, e.ToString ());
					}
				}

				using (var stream = new FileStream (Path.Combine (graphsfolder, benchmark.Key.Name + ".json"), FileMode.Create))
				using (var writer = new StreamWriter (stream))
					writer.Write (JsonConvert.SerializeObject (
						data.Select (kv => KeyValuePair.Create (
								kv.Key,
								kv.Value.Select (kv1 => KeyValuePair.Create (kv1.Key, kv1.Value.Select (kv2 => new { Commit = kv2.Key, Value = kv2.Value }).ToList ()))
									.ToDictionary (kv1 => kv1.Key, kv1 => kv1.Value)
							))
							.ToDictionary (kv => kv.Key, kv => kv.Value),
						Formatting.Indented
					));
			});
		}

		{
			// Min/Average/Max counter data for each config
			Parallel.ForEach (profiles.GroupBy (p => p.Config), config => {
				// Counter.Name | "Time" => List [KeyValuePair[Revision.Commit, Tuple[Min, Average, Max]]]
				var data = new Dictionary<string, List<KeyValuePair<string, Tuple<double, double, double>>>> ();

				Console.Write ("Config: {0}, Counter: {1}{2}", config.Key.Name, "Time", Environment.NewLine);
				data.Add ("Time", ConfigData (config.Key, config, run => run.WallClockTime.TotalMilliseconds));

				foreach (var counter in ExtractIntersectCounters (config)) {
					try {
						Console.Write ("Config: {0}, Counter: {1}{2}", config.Key.Name, counter, Environment.NewLine);
						data.Add (counter.ToString (), ConfigData (config.Key, config.AsEnumerable (), run => ExtractLastCounterValue (run, counter)));
					} catch (InvalidCastException e) {
						Console.Out.WriteLine ("Cannot convert \"{0}\" last value to double : {1}{2}", counter, Environment.NewLine, e.ToString ());
					}
				}

				using (var stream = new FileStream (Path.Combine (graphsfolder, config.Key.Name + ".config.json"), FileMode.Create))
				using (var writer = new StreamWriter (stream))
					writer.Write (JsonConvert.SerializeObject (
						data.Select (kv => KeyValuePair.Create (
								kv.Key,
								kv.Value.Select (kv1 => new { Commit = kv1.Key, Min = kv1.Value.Item1, Average = kv1.Value.Item2, Max = kv1.Value.Item3 })
									.ToList ()
							))
							.ToDictionary (kv => kv.Key, kv => kv.Value)
					));
			});
		}

		Console.WriteLine ("Uploading graphs");

		SCPToRemote (sshkey, Directory.EnumerateFileSystemEntries (graphsfolder).ToList (), String.Format ("/volume1/storage/benchmarker/graphs/{0}/{1}", project, architecture));
	}

	// Config.Name => List [KeyValuePair[Revision.Commit, Value]]
	static Dictionary<string, List<KeyValuePair<string, double>>> BenchmarkData (Benchmark benchmark, IEnumerable<ProfileResult> profiles, Func<ProfileResult.Run, double> selector)
	{
		return profiles.GroupBy (p => p.Config)
			.Select (g => KeyValuePair.Create (g.Key.Name, g.Select (p => KeyValuePair.Create (p.Revision.Commit, p.Runs.Select (selector).Sum () / p.Runs.Length)).ToList ()))
			.ToDictionary (kv => kv.Key, kv => kv.Value);
	}

	// List [KeyValuePair[Revision.Commit, Tuple[Min, Average, Max]]]
	static List<KeyValuePair<string, Tuple<double, double, double>>> ConfigData (Config config, IEnumerable<ProfileResult> profiles, Func<ProfileResult.Run, double> selector)
	{
		List<KeyValuePair<Benchmark, List<KeyValuePair<Revision, double>>>> benchmarks =
			profiles.GroupBy (p => p.Benchmark)
				.Select (g => {
					return KeyValuePair.Create (
						g.Key,
						g.OrderBy (p => p.Revision.CommitDate)
							.Select (p => {
								var values = p.Runs.Select (selector).ToArray ();
								if (values.Any (v => Double.IsNaN (v)))
									return KeyValuePair.Create (p.Revision, Double.NaN);
								return KeyValuePair.Create (p.Revision, values.Sum () / values.Length);
							})
							.ToList ()
					);
				})
				.Where (kv => !kv.Value.Any (kv1 => Double.IsNaN (kv1.Value)))
				.ToList ();

		Dictionary<Benchmark, double> medians =
			benchmarks.Select (b => KeyValuePair.Create (b.Key, b.Value.Select (r => r.Value).OrderBy (d => d).ElementAt (b.Value.Count / 2)))
				.ToDictionary (kv => kv.Key, kv => kv.Value);

		SortedDictionary<Revision, List<KeyValuePair<Benchmark, double>>> revisionsnormalized =
			benchmarks.Select (b => KeyValuePair.Create (b.Key, b.Value.Select (r => KeyValuePair.Create (r.Key, medians [b.Key] == 0d ? 0d : r.Value / medians [b.Key]))))
				.Aggregate (new SortedDictionary<Revision, List<KeyValuePair<Benchmark, double>>> (), (d, b) => {
					foreach (var r in b.Value) {
						if (!d.ContainsKey (r.Key))
							d.Add (r.Key, new List<KeyValuePair<Benchmark, double>> ());
						d [r.Key].Add (KeyValuePair.Create (b.Key, r.Value));
					}

					return d;
				});

		return revisionsnormalized.Select (r => KeyValuePair.Create (r.Key.Commit, Tuple.Create (r.Value.Min (b => b.Value), r.Value.Sum (b => b.Value) / r.Value.Count, r.Value.Max (b => b.Value))))
					.ToList ();
	}

	static double ExtractLastCounterValue (ProfileResult.Run run, Counter counter)
	{
		var counters = run.Counters.Where (kv => kv.Key.Equals (counter)).ToList ();

		if (counters.Count != 1)
			return Double.NaN;

		var value = counters.Single ().Value;

		if (value.Count == 0)
			return Double.NaN;

		return Convert.ToDouble (value.Last ().Value);
	}

	static IEnumerable<Counter> ExtractIntersectCounters (IEnumerable<ProfileResult> profiles)
	{
		return profiles.SelectMany (p => p.Runs)
				.Aggregate (new Counter[0], (acc, r) => acc.Length == 0 ? ExtractCounters (r).ToArray () : acc.Intersect (ExtractCounters (r)).ToArray ())
				.Distinct ()
				.OrderBy (c => c.Section + c.Name)
				.ToArray ();
	}

	static IEnumerable<Counter> ExtractCounters (ProfileResult.Run run)
	{
		return run.Counters.Where (kv => kv.Value.Count > 0).Select (kv => kv.Key);
	}

	static string SlugifyCounter (Counter counter)
	{
		return counter.Name.ToLower ().Replace (' ', '-').Replace ('/', '-').Replace (':', '-').Replace ("#", "").Replace ("&", "").Replace ("?", "");
	}

	static void RSyncFromRemote (string sshkey, string files, string destination)
	{
		sshkey = String.IsNullOrWhiteSpace (sshkey) ? String.Empty : ("-i '" + sshkey + "'");

		Process.Start ("rsync", String.Format ("-rvz --exclude '*.mlpd' -e \"ssh {0}\" builder@nas.bos.xamarin.com:'{1}' {2}", sshkey, files, destination)).WaitForExit ();
	}

	static void SCPToRemote (string sshkey, List<string> files, string destination)
	{
		sshkey = String.IsNullOrWhiteSpace (sshkey) ? String.Empty : ("-i " + sshkey);

		Process.Start ("ssh", String.Format ("{0} builder@nas.bos.xamarin.com \"rm -rf '{2}/*'\"", sshkey, files, destination)).WaitForExit ();
		Process.Start ("ssh", String.Format ("{0} builder@nas.bos.xamarin.com \"mkdir -p '{2}'\"", sshkey, files, destination)).WaitForExit ();

		for (int i = 0, step = 100; i < files.Count; i += step) {
			Process.Start ("scp", String.Format ("{0} -r -B {1} builder@nas.bos.xamarin.com:{2}", sshkey, String.Join (" ", files.Skip (i).Take (step)), destination)).WaitForExit ();
		}
	}

	struct KeyValuePair
	{
		public static KeyValuePair<TKey, TValue> Create<TKey, TValue> (TKey key, TValue value)
		{
			return new KeyValuePair<TKey, TValue> (key, value);
		}
	}
}

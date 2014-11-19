using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Benchmarker.Common.LogProfiler;
using Benchmarker.Common.Models;
using Newtonsoft.Json;
using System.IO.Compression;
using Benchmarker.Common;
using Newtonsoft.Json.Linq;

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
		Console.Error.WriteLine ("        --sshkey          path to ssh key for builder@nas");
		Console.Error.WriteLine ("    -u, --upload          upload results to storage; default to no");

		Environment.Exit (exitcode);
	}

	public static void Main (string[] args)
	{
		var benchmarksnames = new string[0];
		var architecture = Environment.Is64BitOperatingSystem ? "amd64" : "x86";
		var commit = String.Empty;
		var timeout = Int32.MaxValue;
		var sshkey = String.Empty;
		var upload = false;

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
			} else if (args [optindex] == "--sshkey") {
				sshkey = args [++optindex];
			} else if (args [optindex] == "-u" || args [optindex] == "--upload") {
				upload = Boolean.Parse (args [++optindex]);
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
				Console.Out.WriteLine ("Profiling benchmark \"{0}\" with config \"{1}\"", benchmark.Name, config.Name);

				var timedout = false;

				var info = new ProcessStartInfo {
					FileName = Path.Combine (revisionfolder, "mono"),
					WorkingDirectory = Path.Combine (testsdir,benchmark. TestDirectory),
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
				};

				foreach (var env in config.MonoEnvironmentVariables) {
					if (env.Key == "MONO_PATH" || env.Key == "LD_LIBRARY_PATH")
						continue;

					info.EnvironmentVariables.Add (env.Key, env.Value);
				}

				info.EnvironmentVariables.Add ("MONO_PATH", revisionfolder);
				info.EnvironmentVariables.Add ("LD_LIBRARY_PATH", revisionfolder);

				var envvar = String.Join (" ", config.MonoEnvironmentVariables.Union (new KeyValuePair<string, string>[] { new KeyValuePair<string, string> ("MONO_PATH", revisionfolder), new KeyValuePair<string, string> ("LD_LIBRARY_PATH", revisionfolder) })
					.Select (kv => kv.Key + "=" + kv.Value));

				var arguments = String.Join (" ", config.MonoOptions.Union (benchmark.CommandLine));

				var profile = new ProfileResult { DateTime = DateTime.Now, Benchmark = benchmark, Config = config, Revision = revision, Timedout = timedout, Runs = new ProfileResult.Run [config.Count] };

				for (var i = 0; i < config.Count; ++i) {
					var profilefilename = String.Join ("_", new string [] { profile.ToString (), i.ToString () }) + ".mlpd";

					info.Arguments = String.Format ("--profile=log:counters,countersonly,nocalls,noalloc,output={0} ", Path.Combine (
						profilesfolder, profilefilename)) + arguments;

					Console.Out.WriteLine ("\t$> {0} {1} {2}", envvar, info.FileName, info.Arguments);
					Console.Out.Write ("\t\t-> {0} ", String.Format ("({0}/{1})", i + 1, config.Count));

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

					profile.Runs [i] = new ProfileResult.Run {
						Index = i,
						WallClockTime = success ? TimeSpan.FromTicks (sw.ElapsedTicks) : TimeSpan.Zero,
						Output = success ? stdout.Result : null,
						Error = success ? stderr.Result : null,
						ProfilerOutput = profilefilename
					};

					profile.Timedout = profile.Timedout || !success;
				}

				profiles.Add (profile);
			}
		}

		Parallel.ForEach (profiles, profile => {
			Parallel.ForEach (profile.Runs, run => {
				run.Counters = ProfileResult.Run.ParseCounters (Path.Combine (profilesfolder, run.ProfilerOutput));
			});

			profile.StoreTo (Path.Combine (profilesfolder, profile.ToString () + ".json.gz"), true);
		});

		if (upload) {
			Console.Out.WriteLine ("Copying files to storage from \"{0}\"", profilesfolder);
			SCPToRemote (sshkey, profilesfolder, "/volume1/storage/benchmarker/runs/mono/" + architecture);
		}
	}

	static void SCPToRemote (string sshkey, string files, string destination)
	{
		sshkey = String.IsNullOrWhiteSpace (sshkey) ? String.Empty : ("-i " + sshkey);

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

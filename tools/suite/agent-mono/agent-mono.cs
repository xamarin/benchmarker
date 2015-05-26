using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Benchmarker.Common;
//using Benchmarker.Common.LogProfiler;
using Benchmarker.Common.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class Program
{
	static void UsageAndExit (string error = null, int exitcode = 0)
	{
		if (!String.IsNullOrEmpty (error)) {
			Console.Error.WriteLine ("Error : {0}", error);
			Console.Error.WriteLine ();
		}

		Console.Error.WriteLine ("usage: [parameters] [--] <mono-executable> <mono-path> <library-path> <architecture> <commit> <tests-dir> <benchmarks-dir> <config-file> [<config-file>+]");
		Console.Error.WriteLine ("parameters:");
		Console.Error.WriteLine ("        --help            display this help");
		Console.Error.WriteLine ("    -b, --benchmarks      benchmarks to run, separated by commas; default to all of them");
		Console.Error.WriteLine ("    -t, --timeout         execution timeout for each benchmark, in seconds; default to no timeout");
		Console.Error.WriteLine ("        --sshkey          path to ssh key for builder@nas");
		Console.Error.WriteLine ("    -u, --upload          upload results to storage; default to no");

		Environment.Exit (exitcode);
	}

	public static void Main (string[] args)
	{
		var benchmarksnames = new string[0];
		var timeout = Int32.MaxValue;
		var sshkey = String.Empty;
		var upload = false;

		var optindex = 0;

		for (; optindex < args.Length; ++optindex) {
			if (args [optindex] == "-b" || args [optindex] == "--benchmarks") {
				benchmarksnames = args [++optindex].Split (',').Select (s => s.Trim ()).Union (benchmarksnames).ToArray ();
			} else if (args [optindex] == "-t" || args [optindex] == "--timeout") {
				timeout = Int32.Parse (args [++optindex]);
			} else if (args [optindex] == "--sshkey") {
				sshkey = args [++optindex];
			} else if (args [optindex] == "-u" || args [optindex] == "--upload") {
				upload = true;
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

		if (args.Length - optindex < 8)
			UsageAndExit (null, 1);

		var monoexecutable = args [optindex++];
		var monopath = args [optindex++];
		var librarypath = args [optindex++];
		var architecture = args [optindex++];
		var commit = args [optindex++];
		var testsdir = args [optindex++];
		var benchmarksdir = args [optindex++];
		var configfiles = args.Skip (optindex).ToArray ();

		var benchmarks = Benchmark.LoadAllFrom (benchmarksdir, benchmarksnames).OrderBy (b => b.Name).ToArray ();
		var configs = configfiles.Select (c => Config.LoadFrom (c)).ToArray ();

		var revision = Revision.Get ("mono", architecture, commit);

		var profilesdirname = String.Join ("_", revision.CommitDate.ToString ("s").Replace (':', '-'), revision.Commit);
		if (Directory.Exists (profilesdirname))
			Directory.Delete (profilesdirname, true);

		var profilesdir = Directory.CreateDirectory (profilesdirname).FullName;

		var profiles = new List<ProfileResult> (benchmarks.Length * configs.Length);

		foreach (var benchmark in benchmarks) {
			foreach (var config in configs) {
				Console.Out.WriteLine ("Profiling benchmark \"{0}\" with config \"{1}\"", benchmark.Name, config.Name);

				var timedout = false;

				var info = new ProcessStartInfo {
					FileName = monoexecutable,
					WorkingDirectory = Path.Combine (testsdir, benchmark.TestDirectory),
					UseShellExecute = false,
				};

				foreach (var env in config.MonoEnvironmentVariables) {
					if (env.Key == "MONO_PATH" || env.Key == "LD_LIBRARY_PATH")
						continue;

					info.EnvironmentVariables.Add (env.Key, env.Value);
				}

				info.EnvironmentVariables ["MONO_PATH"] = monopath;
				info.EnvironmentVariables ["DYLD_LIBRARY_PATH"] = librarypath + ":" + info.EnvironmentVariables ["DYLD_LIBRARY_PATH"];

				var envvar = String.Join (" ",
					config.MonoEnvironmentVariables
						.Union (new KeyValuePair<string, string>[] {
							new KeyValuePair<string, string> ("MONO_PATH", info.EnvironmentVariables["MONO_PATH"]),
							new KeyValuePair<string, string> ("DYLD_LIBRARY_PATH", info.EnvironmentVariables["DYLD_LIBRARY_PATH"])
						})
						.Select (kv => kv.Key + "=" + kv.Value)
				);

				var arguments = String.Join (" ", config.MonoOptions.Union (benchmark.CommandLine));

				var profile = new ProfileResult { DateTime = DateTime.Now, Benchmark = benchmark, Config = config, Revision = revision, Timedout = timedout, Runs = new ProfileResult.Run [config.Count] };

				for (var i = 0; i < config.Count; ++i) {
					var profilefilename = String.Join ("_", new string [] { ProfileFilename (profile), i.ToString () }) + ".mlpd";

					info.Arguments = String.Format ("--profile=log:counters,countersonly,nocalls,noalloc,output={0} ", Path.Combine (
						profilesdir, profilefilename)) + arguments;

					Console.Out.WriteLine ("$> {0} {1} {2}", envvar, info.FileName, info.Arguments);

					timeout = benchmark.Timeout > 0 ? benchmark.Timeout : timeout;

					var sw = Stopwatch.StartNew ();

					using (var process = Process.Start (info)) {
						var success = process.WaitForExit (timeout < 0 ? -1 : (Math.Min (Int32.MaxValue / 1000, timeout) * 1000));

						sw.Stop ();

						if (!success)
							process.Kill ();

						Console.Out.WriteLine ("-> ({0}/{1}) {2}", i + 1, config.Count, success ? sw.Elapsed.ToString () : "timeout!");

						profile.Runs [i] = new ProfileResult.Run {
							Index = i,
							WallClockTime = success ? TimeSpan.FromTicks (sw.ElapsedTicks) : TimeSpan.Zero,
							ProfilerOutput = profilefilename
						};

						profile.Timedout = profile.Timedout || !success;
					}
				}

				profiles.Add (profile);
			}
		}

		Parallel.ForEach (profiles, profile => {
#if false
			Parallel.ForEach (profile.Runs, run => {
				run.Counters = ProfileResult.Run.ParseCounters (Path.Combine (profilesdir, run.ProfilerOutput));
				run.CountersFile = ProfileFilename (profile) + "_" + run.Index + ".counters.json.gz";
				run.StoreCountersTo (Path.Combine (profilesdir, run.CountersFile));
			});
#endif

			profile.StoreTo (Path.Combine (profilesdir, ProfileFilename (profile) + ".json.gz"), true);
		});

		if (upload) {
			Console.Out.WriteLine ("Copying files to storage from \"{0}\"", profilesdir);
			SCPToRemote (sshkey, profilesdir, "/volume1/storage/benchmarker/runs/mono/" + architecture);
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

	static string ProfileFilename (ProfileResult profile)
	{
		return String.Join ("_", new [] { profile.Revision.Project, profile.Revision.Architecture, profile.Revision.Commit, profile.Benchmark.Name, profile.Config.Name });
	}

	struct KeyValuePair
	{
		public static KeyValuePair<TKey, TValue> Create<TKey, TValue> (TKey key, TValue value)
		{
			return new KeyValuePair<TKey, TValue> (key, value);
		}
	}
}

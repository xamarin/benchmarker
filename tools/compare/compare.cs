using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Benchmarker.Common;
using Benchmarker.Common.Models;
using System.Threading.Tasks;
using Parse;
using Nito.AsyncEx;

class Compare
{
	static void UsageAndExit (string message = null, int exitcode = 0)
	{
		if (!String.IsNullOrEmpty (message))
			Console.Error.WriteLine ("{0}\n", message);

		Console.Error.WriteLine ("Usage:");
		Console.Error.WriteLine ("    compare.exe [options]");
		Console.Error.WriteLine ("    compare.exe [options] [--] <tests-dir> <benchmarks-dir> <machines-dir> <config-file>");
		Console.Error.WriteLine ("Options:");
		Console.Error.WriteLine ("        --help              display this help");
		Console.Error.WriteLine ("    -c, --config-file       the config file");
		Console.Error.WriteLine ("    -b, --benchmarks        benchmarks to run, separated by commas; default to all of them");
		Console.Error.WriteLine ("                               ex: -b ahcbench,db,message,raytracer2");
		Console.Error.WriteLine ("    -l, --list-benchmarks   list all available benchmarks");
		Console.Error.WriteLine ("    -t, --timeout           execution timeout for each benchmark, in seconds; default to no timeout");
		Console.Error.WriteLine ("        --commit            the hash of the commit being tested");
		Console.Error.WriteLine ("        --git-repo          the directory of the Git repository");
		Console.Error.WriteLine ("        --create-run-set    just create a run set, don't run any benchmarks");
		Console.Error.WriteLine ("        --pull-request-url  GitHub URL of a pull request to create the run set with");
		Console.Error.WriteLine ("        --run-set-id        the Parse ID of the run set to amend");
		Console.Error.WriteLine ("        --build-url         the URL of the binary build");
		Console.Error.WriteLine ("        --log-url           the URL where the log files will be accessible");
		Console.Error.WriteLine ("        --root              will be substituted for $ROOT in the config");

		Environment.Exit (exitcode);
	}

	public static void Main (string[] args)
	{
		string[] benchmarkNames = null;
		//var pausetime = false;
		var timeout = -1;
		string commitFromCmdline = null;
		string gitRepoDir = null;
		string rootFromCmdline = null;
		string buildURL = null;
		string logURL = null;
		string pullRequestURL = null;
		string runSetId = null;
		string configFile = null;
		bool justCreateRunSet = false;
		bool justListBenchmarks = false;

		var optindex = 0;

		for (; optindex < args.Length; ++optindex) {
			if (args [optindex] == "-b" || args [optindex] == "--benchmarks") {
				var newNames = args [++optindex].Split (',').Select (s => s.Trim ());
				if (benchmarkNames == null)
					benchmarkNames = newNames.ToArray ();
				else
					benchmarkNames = newNames.Union (benchmarkNames).ToArray ();
			} else if (args [optindex] == "-c" || args [optindex] == "--config-file") {
				configFile = args [++optindex];
			} else if (args [optindex] == "-l" || args [optindex] == "--list-benchmarks") {
				justListBenchmarks = true;
			} else if (args [optindex] == "--commit") {
				commitFromCmdline = args [++optindex];
			} else if (args [optindex] == "--git-repo") {
				gitRepoDir = args [++optindex];
			} else if (args [optindex] == "--build-url") {
				buildURL = args [++optindex];
			} else if (args [optindex] == "--log-url") {
				logURL = args [++optindex];
			} else if (args [optindex] == "--pull-request-url") {
				pullRequestURL = args [++optindex];
			} else if (args [optindex] == "--create-run-set") {
				justCreateRunSet = true;
			} else if (args [optindex] == "--run-set-id") {
				runSetId = args [++optindex];
			} else if (args [optindex] == "--root") {
				rootFromCmdline = args [++optindex];
			} else if (args [optindex] == "-t" || args [optindex] == "--timeout") {
				timeout = Int32.Parse (args [++optindex]);
				timeout = timeout <= 0 ? -1 : timeout;
			// } else if (args [optindex] == "-p" || args [optindex] == "--pause-time") {
			// 	pausetime = Boolean.Parse (args [++optindex]);
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

		if (justCreateRunSet && runSetId != null) {
			Console.Error.WriteLine ("Error: --create-run-set and --run-set-id are incompatible.");
			Environment.Exit (1);
		}

		if (justListBenchmarks && benchmarkNames != null) {
			Console.Error.WriteLine ("Error: -b/--benchmarks and -l/--list-benchmarks are incompatible.");
			Environment.Exit (1);
		}

		string testsDir, benchmarksDir, machinesDir;

		if (args.Length - optindex == 4) {
			if (configFile != null) {
				Console.Error.WriteLine ("Error: You must not specify the config file twice.");
				Environment.Exit (1);
			}

			testsDir = args [optindex++];
			benchmarksDir = args [optindex++];
			machinesDir = args [optindex++];
			configFile = args [optindex++];
		} else if (args.Length - optindex == 0) {
			var exeLocation = System.Reflection.Assembly.GetEntryAssembly ().Location;
			var exeName = Path.GetFileName (exeLocation);
			var exeDir = Path.GetDirectoryName (exeLocation);
			if (exeName != "compare.exe") {
				Console.Error.WriteLine ("Error: Executable is not `compare.exe`.  Please specify all paths manually.");
				Environment.Exit (1);
			}
			if (Path.GetFileName (exeDir) != "tools") {
				Console.Error.WriteLine ("Error: Executable is not in the `tools` directory.  Please specify all paths manually.");
				Environment.Exit (1);
			}
			var root = Path.GetDirectoryName (exeDir);

			testsDir = Path.Combine (root, "tests");
			benchmarksDir = Path.Combine (root, "benchmarks");
			machinesDir = Path.Combine (root, "machines");

			if (configFile == null)
				configFile = Path.Combine (root, "configs", "default-sgen.conf");
		} else {
			UsageAndExit (null, 1);
			return;
		}

		var benchmarks = Benchmark.LoadAllFrom (benchmarksDir, benchmarkNames);
		if (benchmarks == null) {
			Console.WriteLine ("Error: Could not load all benchmarks.");
			Environment.Exit (1);
		}

		if (justListBenchmarks) {
			foreach (var benchmark in benchmarks.OrderBy (b => b.Name)) {
				Console.Out.WriteLine (benchmark.Name);
			}
			Environment.Exit (0);
		}

		Config.InitializeGitHubClient ();

		if (!ParseInterface.Initialize ()) {
			Console.Error.WriteLine ("Error: Could not initialize Parse interface.");
			Environment.Exit (1);
		}

		var config = Config.LoadFrom (configFile, rootFromCmdline);

		var machine = Machine.LoadCurrentFrom (machinesDir);

		var commit = AsyncContext.Run (() => config.GetCommit (commitFromCmdline, gitRepoDir));

		if (commit == null) {
			Console.WriteLine ("Error: Could not get commit");
			Environment.Exit (1);
		}
		if (commit.CommitDate == null) {
			Console.WriteLine ("Error: Could not get a commit date.");
			Environment.Exit (1);
		}

		RunSet runSet;
		if (runSetId != null) {
			if (pullRequestURL != null) {
				Console.WriteLine ("Error: Pull request URL cannot be specified for an existing run set.");
				Environment.Exit (1);
			}
			runSet = AsyncContext.Run (() => RunSet.FromId (runSetId, config, commit, buildURL, logURL));
			if (runSet == null) {
				Console.WriteLine ("Error: Could not get run set.");
				Environment.Exit (1);
			}
		} else {
			runSet = new RunSet {
				StartDateTime = DateTime.Now,
				Config = config,
				Commit = commit,
				BuildURL = buildURL,
				LogURL = logURL,
				PullRequestURL = pullRequestURL
			};
		}

		if (!justCreateRunSet) {
			var someSuccess = false;

			foreach (var benchmark in benchmarks.OrderBy (b => b.Name)) {
				/* Run the benchmarks */
				if (config.Count <= 0)
					throw new ArgumentOutOfRangeException (String.Format ("configs [\"{0}\"].Count <= 0", config.Name));

				Console.Out.WriteLine ("Running benchmark \"{0}\" with config \"{1}\"", benchmark.Name, config.Name);

				var runner = new Runner (testsDir, config, benchmark, machine, timeout);

				var result = new Result {
					DateTime = DateTime.Now,
					Benchmark = benchmark,
					Config = config,
				};

				var haveTimedOut = false;
				var haveCrashed = false;

				for (var i = 0; i < config.Count + 1; ++i) {
					bool timedOut;

					Console.Out.Write ("\t\t-> {0} ", i == 0 ? "[dry run]" : String.Format ("({0}/{1})", i, config.Count));

					var run = runner.Run (out timedOut);

					// skip first one
					if (i == 0)
						continue;

					if (run != null) {
						result.Runs.Add (run);
						someSuccess = true;
					} else {
						if (timedOut)
							haveTimedOut = true;
						else
							haveCrashed = true;
					}
				}

				if (haveTimedOut)
					runSet.TimedOutBenchmarks.Add (benchmark);
				if (haveCrashed)
					runSet.CrashedBenchmarks.Add (benchmark);

				// FIXME: implement pausetime
				//if (pausetime)
				//	throw new NotImplementedException ();

				runSet.Results.Add (result);
			}

			if (!someSuccess)
				Console.WriteLine ("all runs failed.");
		}
		
		runSet.FinishDateTime = DateTime.Now;

		Console.WriteLine ("uploading");
		try {
			var parseObject = AsyncContext.Run (() => runSet.UploadToParse ());
			Console.WriteLine ("http://xamarin.github.io/benchmarker/front-end/runset.html#{0}", parseObject.ObjectId);
			Console.WriteLine ("{{ \"runSetId\": \"{0}\" }}", parseObject.ObjectId);
		} catch (Exception exc) {
			Console.WriteLine ("Error: Failure uploading data: " + exc);
			Environment.Exit (1);
		}
	}
}

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

		Console.Error.WriteLine ("Usage:          [options] [--] <tests-dir> <benchmarks-dir> <machines-dir> <config-file>");
		Console.Error.WriteLine ("Options:");
		Console.Error.WriteLine ("        --help            display this help");
		Console.Error.WriteLine ("    -b, --benchmarks      benchmarks to run, separated by commas; default to all of them");
		Console.Error.WriteLine ("                             ex: -b ahcbench,db,message,raytracer2");
		Console.Error.WriteLine ("    -l, --list-benchmarks list all available benchmarks");
		Console.Error.WriteLine ("    -t, --timeout         execution timeout for each benchmark, in seconds; default to no timeout");
		Console.Error.WriteLine ("        --commit          the hash of the commit being tested");
		Console.Error.WriteLine ("        --create-run-set  just create a run set, don't run any benchmarks");
		Console.Error.WriteLine ("        --run-set-id      the Parse ID of the run set to amend");
		Console.Error.WriteLine ("        --build-url       the URL of the binary build");
		Console.Error.WriteLine ("        --root            will be substituted for $ROOT in the config");
		// Console.Error.WriteLine ("    -p, --pause-time       benchmark garbage collector pause times; value : true / false");

		Environment.Exit (exitcode);
	}

	public static void Main (string[] args)
	{
		string[] benchmarkNames = null;
		//var pausetime = false;
		var timeout = -1;
		string commitFromCmdline = null;
		string rootFromCmdline = null;
		string buildURL = null;
		string runSetId = null;
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
			} else if (args [optindex] == "-l" || args [optindex] == "--list-benchmarks") {
				justListBenchmarks = true;
			} else if (args [optindex] == "--commit") {
				commitFromCmdline = args [++optindex];
			} else if (args [optindex] == "--build-url") {
				buildURL = args [++optindex];
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

		if (args.Length - optindex != 4)
			UsageAndExit (null, 1);

		var testsdir = args [optindex++];
		var benchmarksdir = args [optindex++];
		var machinesdir = args [optindex++];
		var configfile = args [optindex++];

		var benchmarks = Benchmark.LoadAllFrom (benchmarksdir, benchmarkNames);
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

		if (!ParseInterface.Initialize ()) {
			Console.Error.WriteLine ("Error: Could not initialize Parse interface.");
			Environment.Exit (1);
		}

		var config = Config.LoadFrom (configfile, rootFromCmdline);

		var machine = Machine.LoadCurrentFrom (machinesdir);

		var commit = AsyncContext.Run (() => config.GetCommit (commitFromCmdline));

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
			runSet = AsyncContext.Run (() => RunSet.FromId (runSetId, config, commit, buildURL));
			if (runSet == null) {
				Console.WriteLine ("Error: Could not get run set.");
				Environment.Exit (1);
			}
		} else {
			runSet = new RunSet {
				StartDateTime = DateTime.Now,
				Config = config,
				Commit = commit,
				BuildURL = buildURL
			};
		}

		if (!justCreateRunSet) {
			var someSuccess = false;

			foreach (var benchmark in benchmarks.OrderBy (b => b.Name)) {
				/* Run the benchmarks */
				if (config.Count <= 0)
					throw new ArgumentOutOfRangeException (String.Format ("configs [\"{0}\"].Count <= 0", config.Name));

				Console.Out.WriteLine ("Running benchmark \"{0}\" with config \"{1}\"", benchmark.Name, config.Name);

				var runner = new Runner (testsdir, config, benchmark, machine, timeout);

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
			Console.WriteLine ("{{ \"runSetId\": \"{0}\" }}", parseObject.ObjectId);
		} catch (Exception exc) {
			Console.WriteLine ("Error: Failure uploading data: " + exc);
			Environment.Exit (1);
		}
		Console.WriteLine ("uploaded");
	}
}

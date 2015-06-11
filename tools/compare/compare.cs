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

		Console.Error.WriteLine ("Usage:          [options] [--] <tests-dir> <benchmarks-dir> <config-file>");
		Console.Error.WriteLine ("Options:");
		Console.Error.WriteLine ("        --help            display this help");
		Console.Error.WriteLine ("    -b, --benchmarks      benchmarks to run, separated by commas; default to all of them");
		Console.Error.WriteLine ("                             ex: -b ahcbench,db,message,raytracer2");
		Console.Error.WriteLine ("    -t, --timeout         execution timeout for each benchmark, in seconds; default to no timeout");
		Console.Error.WriteLine ("        --commit          the hash of the commit being tested");
		Console.Error.WriteLine ("        --build-url       the URL of the binary build");
		Console.Error.WriteLine ("        --root            will be substituted for $ROOT in the config");
		// Console.Error.WriteLine ("    -p, --pause-time       benchmark garbage collector pause times; value : true / false");

		Environment.Exit (exitcode);
	}

	public static void Main (string[] args)
	{
		var benchmarksnames = new string[0];
		//var pausetime = false;
		var timeout = Int32.MaxValue;
		string commitFromCmdline = null;
		string rootFromCmdline = null;
		string buildURL = null;

		var optindex = 0;

		ParseClient.Initialize ("7khPUBga9c7L1YryD1se1bp6VRzKKJESc0baS9ES", "FwqUX9gNQP5HmP16xDcZRoh0jJRCDvdoDpv8L87p");

		for (; optindex < args.Length; ++optindex) {
			if (args [optindex] == "-b" || args [optindex] == "--benchmarks") {
				benchmarksnames = args [++optindex].Split (',').Select (s => s.Trim ()).Union (benchmarksnames).ToArray ();
			} else if (args [optindex] == "--commit") {
				commitFromCmdline = args [++optindex];
			} else if (args [optindex] == "--build-url") {
				buildURL = args [++optindex];
			} else if (args [optindex] == "--root") {
				rootFromCmdline = args [++optindex];
			} else if (args [optindex] == "-t" || args [optindex] == "--timeout") {
				timeout = Int32.Parse (args [++optindex]) * 1000;
				timeout = timeout == 0 ? Int32.MaxValue : timeout;
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

		if (args.Length - optindex != 3)
			UsageAndExit (null, 1);

		var testsdir = args [optindex++];
		var benchmarksdir = args [optindex++];
		var configfile = args [optindex++];

		var config = Config.LoadFrom (configfile, rootFromCmdline);

		var commit = AsyncContext.Run (() => config.GetCommit (commitFromCmdline));

		if (commit == null) {
			Console.WriteLine ("Could not get commit");
			Environment.Exit (1);
		}
		if (commit.CommitDate == null) {
			Console.WriteLine ("Could not get a commit date.");
			Environment.Exit (1);
		}

		var runSet = new RunSet {
			StartDateTime = DateTime.Now,
			Config = config,
			Commit = commit,
			BuildURL = buildURL
		};

		foreach (var benchmark in Benchmark.LoadAllFrom (benchmarksdir, benchmarksnames).OrderBy (b => b.Name)) {
			/* Run the benchmarks */
			if (config.Count <= 0)
				throw new ArgumentOutOfRangeException (String.Format ("configs [\"{0}\"].Count <= 0", config.Name));

			Console.Out.WriteLine ("Running benchmark \"{0}\" with config \"{1}\"", benchmark.Name, config.Name);

			var runner = new Runner (testsdir, config, benchmark, timeout);

			var result = new Result {
				DateTime = DateTime.Now,
				Benchmark = benchmark,
				Config = config,
				Timedout = false,
			};

			for (var i = 0; i < config.Count + 1; ++i) {
				Console.Out.Write ("\t\t-> {0} ", i == 0 ? "[dry run]" : String.Format ("({0}/{1})", i, config.Count));

				var run = runner.Run ();

				// skip first one
				if (i == 0)
					continue;
				
				result.Timedout = result.Timedout || run == null;

				if (run != null)
					result.Runs.Add (run);
			}

			// FIXME: implement pausetime
			//if (pausetime)
			//	throw new NotImplementedException ();

			runSet.Results.Add (result);
		}

		runSet.FinishDateTime = DateTime.Now;

		Console.WriteLine ("uploading");
		try {
			AsyncContext.Run (() => runSet.UploadToParse ());
		} catch (Exception exc) {
			Console.WriteLine ("Failure uploading data: " + exc);
			Environment.Exit (1);
		}
		Console.WriteLine ("uploaded");
	}
}

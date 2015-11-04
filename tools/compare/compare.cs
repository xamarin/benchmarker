using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Benchmarker;
using Benchmarker.Models;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Nito.AsyncEx;
using Common.Logging;
using Common.Logging.Simple;
using Npgsql;
using compare;

class Compare
{
	static int UsageAndExit (string message = null, int exitcode = 0)
	{
		if (!String.IsNullOrEmpty (message))
			Console.Error.WriteLine ("{0}\n", message);

		Console.Error.WriteLine ("Usage:");
		Console.Error.WriteLine ("    compare.exe [options]");
		Console.Error.WriteLine ("Options:");
		Console.Error.WriteLine ("        --help                  display this help");
		Console.Error.WriteLine ("    -c, --config-file FILE      the config file");
		Console.Error.WriteLine ("    -b, --benchmarks LIST       benchmarks to run, separated by commas; default to all of them");
		Console.Error.WriteLine ("                                  ex: -b ahcbench,db,message,raytracer2");
		Console.Error.WriteLine ("    -l, --list-benchmarks       list all available benchmarks");
		Console.Error.WriteLine ("        --machine NAME          machine to list benchmarks or to create run set for");
		Console.Error.WriteLine ("    -t, --timeout SECONDS       execution timeout for each benchmark, in seconds; default to no timeout");
		Console.Error.WriteLine ("        --main-product PRODUCT HASH");
		Console.Error.WriteLine ("        --secondary-product PRODUCT HASH");
		Console.Error.WriteLine ("                                specify one main and any number of secondary products");
		Console.Error.WriteLine ("        --create-run-set        just create a run set, don't run any benchmarks");
		Console.Error.WriteLine ("        --pull-request-url URL  GitHub URL of a pull request to create the run set with");
		Console.Error.WriteLine ("        --mono-repository DIR   Path of your local Mono repository (for pull requests)");
		Console.Error.WriteLine ("        --run-set-id ID         the database ID of the run set to amend");
		Console.Error.WriteLine ("        --build-url URL         the URL of the binary build");
		Console.Error.WriteLine ("        --log-url URL           the URL where the log files will be accessible");
		Console.Error.WriteLine ("        --root DIR              will be substituted for $ROOT in the config");
		Console.Error.WriteLine ("        --valgrind-massif VALGRIND FILE");
		Console.Error.WriteLine ("                                run the benchmark with Valgrind's Massif tool");
		Console.Error.WriteLine ("                                  VALGRIND is the path to the valgrind binary");
		Console.Error.WriteLine ("                                  FILE is the output filename");
		Console.Error.WriteLine ("        --valgrind-cachegrind VALGRIND FILE");
		Console.Error.WriteLine ("                                run the benchmark with Valgrind's Cachegrind tool");
		Console.Error.WriteLine ("                                  VALGRIND is the path to the valgrind binary");
		Console.Error.WriteLine ("                                  FILE is the output filename");
		Console.Error.WriteLine ("        --upload-pause-times BINPROT");
		Console.Error.WriteLine ("                                Upload the pause times from the specified binary protocol file");
		Console.Error.WriteLine ("        --sgen-grep-binprot PATH");
		Console.Error.WriteLine ("                                Specify the path to the sgen-grep-binprot tool");
		Console.Error.WriteLine ("        --run-id ID             the database ID of the run to upload to");

		Environment.Exit (exitcode);
		return exitcode;
	}

	static async Task<long?> GetPullRequestBaselineRunSetId (NpgsqlConnection conn, Product product, string pullRequestURL, compare.Repository repository, Config config)
	{
		var gitHubClient = GitHubInterface.GitHubClient;
		var match = Regex.Match (pullRequestURL, product.PullRequestRegexp);
		if (match == null) {
			Console.Error.WriteLine ("Error: Cannot parse pull request URL.");
			Environment.Exit (1);
		}
		var pullRequestNumber = Int32.Parse (match.Groups [1].Value);
		Console.WriteLine ("pull request {0}", pullRequestNumber);

		var pullRequest = await gitHubClient.PullRequest.Get ("mono", "mono", pullRequestNumber);

		var prRepo = pullRequest.Head.Repository.SshUrl;
		var prBranch = pullRequest.Head.Ref;

		var prSha = repository.Fetch (prRepo, prBranch);
		if (prSha == null) {
			Console.Error.WriteLine ("Error: Could not fetch pull request branch {0} from repo {1}", prBranch, prRepo);
			Environment.Exit (1);
		}

		var masterSha = repository.Fetch (product.GitRepositoryUrl, "master");
		if (masterSha == null) {
			Console.Error.WriteLine ("Error: Could not fetch master.");
			Environment.Exit (1);
		}

		var baseSha = repository.MergeBase (prSha, masterSha);
		if (baseSha == null) {
			Console.Error.WriteLine ("Error: Could not determine merge base of pull request.");
			Environment.Exit (1);
		}

		Console.WriteLine ("Merge base sha is {0}", baseSha);

		var revList = repository.RevList (baseSha);
		if (revList == null) {
			Console.Error.WriteLine ("Error: Could not get rev-list for merge base {0}.", baseSha);
			Environment.Exit (1);
		}
		Console.WriteLine ("{0} commits in rev-list", revList.Length);

		if (!config.ExistsInPostgres (conn)) {
			Console.Error.WriteLine ("Error: The config {0} does not exist or is incompatible.", config.Name);
			Environment.Exit (1);
		}

		// FIXME: also support `--machine`
		var hostarch = compare.Utils.LocalHostnameAndArch ();
		var machine = new Machine { Name = hostarch.Item1, Architecture = hostarch.Item2 };
		if (!RunSet.MachineExistsInPostgres (conn, machine)) {
			Console.Error.WriteLine ("Error: The machine does not exist.");
			Environment.Exit (1);
		}

		var whereValues = new PostgresRow ();
		whereValues.Set ("machine", NpgsqlTypes.NpgsqlDbType.Varchar, machine.Name);
		whereValues.Set ("config", NpgsqlTypes.NpgsqlDbType.Varchar, config.Name);
		var rows = PostgresInterface.Select (conn, "runSet", new string[] { "id", "commit" }, "machine = :machine and config = :config", whereValues);
		Console.WriteLine ("{0} run sets", rows.Count ());

		var runSetIdsByCommits = new Dictionary<string, long> ();
		foreach (var row in rows) {
			var sha = row.GetReference<string> ("commit");
			if (runSetIdsByCommits.ContainsKey (sha)) {
				// FIXME: select between them?
				continue;
			}
			runSetIdsByCommits.Add (sha, row.GetValue<long> ("id").Value);
		}

		foreach (var sha in revList) {
			if (runSetIdsByCommits.ContainsKey (sha)) {
				Console.WriteLine ("tested base commit is {0}", sha);
				return runSetIdsByCommits [sha];
			}
		}

		return null;
	}

	private static void InitCommons() {
		LogManager.Adapter = new ConsoleOutLoggerFactoryAdapter();
		Logging.SetLogging (LogManager.GetLogger<Compare> ());

		GitHubInterface.githubCredentials = Accredit.GetCredentials ("gitHub") ["privateReadAccessToken"].ToString ();
	}

	static Tuple<long, long> ParseMassifEntry (Dictionary<string, string> dict) {
		var time = dict ["time"];
		var bytes = dict ["mem_heap_B"];
		return Tuple.Create (Int64.Parse (time), Int64.Parse (bytes));
	}

	static double SumArray (List<double> l, int start, int end) {
		if (start == end)
			return 0.0;
		if (start + 1 == end)
			return l [start];
		var middle = start + (end - start) / 2;
		return SumArray (l, start, middle) + SumArray (l, middle, end);
	}

	static Tuple<double, long> MemoryIntegral (string massifFilename) {
		var snapshotRegexp = new Regex ("^snapshot=\\d+");
		var keyValueRegexp = new Regex ("^(\\w+)=(\\w+)");
		var lines = File.ReadLines (massifFilename);
		Dictionary<string, string> dict = null;
		var entries = new List<Tuple<long, long>> ();
		foreach (var line in lines) {
			if (snapshotRegexp.Match (line).Success) {
				if (dict != null)
					entries.Add (ParseMassifEntry (dict));
				dict = new Dictionary<string, string> ();
			} else {
				var match = keyValueRegexp.Match (line);
				if (!match.Success)
					continue;
				dict.Add (match.Groups [1].Value, match.Groups [2].Value);
			}
		}
		if (dict != null)
			entries.Add (ParseMassifEntry (dict));
		var values = new List<double> ();
		for (var i = 1; i < entries.Count; ++i) {
			var entryA = entries [i - 1];
			var entryB = entries [i];
			var timeA = (double)entryA.Item1 / 1000000000.0;
			var memA = (double)entryA.Item2 / (1024.0 * 1024.0);
			var timeB = (double)entryB.Item1 / 1000000000.0;
			var memB = (double)entryB.Item2 / (1024.0 * 1024.0);
			var dTime = timeB - timeA;
			var value = (memA * dTime + memB * dTime) / 2.0;
			values.Add (value);
		}
		return Tuple.Create (SumArray (values, 0, values.Count), entries.Last ().Item1);
	}

	const double L1Hit = 1;
	const double LlHit = 10;
	const double LlMiss = 100;

	// returns all items from summary, weighted cache miss rate, branch misprediction rate
	static Tuple<double[], double, double> CacheAndBranches (string cachegrindFilename) {
		var summaryRegexp = new Regex ("^summary:((\\s+\\d+)+)");
		var lines = File.ReadLines (cachegrindFilename);
		foreach (var line in lines) {
			var match = summaryRegexp.Match (line);
			if (!match.Success)
				continue;
			var values = match.Groups [1].Value.Split (' ', '\t').Skip (1).Select (s => (double)Int64.Parse (s)).ToArray ();
			// values are:
			// i refs, i1 misses, lli misses, d refs rd, d1 misses rd, lld misses rs, d refs wr, d1 misses wr, lld misses wr, branches cond, mispred cond, branches ind, mispred ind
			if (values.Length != 13)
				throw new Exception ("Cachegrind summary must contain 13 entries");
			
			var iRefs = values [0];
			var i1Misses = values [1];
			var iLlMisses = values [2];

			var i1Hits = iRefs - i1Misses;
			var iLlHits = i1Misses - iLlMisses;

			var iMissTime = iLlHits * LlHit + iLlMisses * LlMiss;
			var iTotalTime = i1Hits * L1Hit + iMissTime;

			var dRefs = values [3] + values [6];
			var d1Misses = values [4] + values [7];
			var dLlMisses = values [5] + values [8];

			var d1Hits = dRefs - d1Misses;
			var dLlHits = d1Misses - dLlMisses;

			var dMissTime = dLlHits * LlHit + dLlMisses * LlMiss;
			var dTotalTime = d1Hits * L1Hit + dMissTime;

			var cacheMissRate = (iMissTime + dMissTime) / (iTotalTime + dTotalTime);

			var totalBranches = values [9] + values [11];
			var missedBranches = values [10] + values [12];

			var branchMissRate = missedBranches / totalBranches;

			return Tuple.Create (values, cacheMissRate, branchMissRate);
		}
		throw new Exception ("No summary line found in Cachegrind output file.");
	}

	static bool UploadPauseTimes (NpgsqlConnection conn, string binprotFilePath, string grepBinprotPath, long runId) {
		var grepString = Utils.RunForStdout (grepBinprotPath, null, "--pause-times", "--input", binprotFilePath);
		if (grepString == null) {
			Console.Error.WriteLine ("Error: sgen-grep-binprot failed.");
			return false;
		}
		var times = new List<double> ();
		foreach (var line in grepString.Split ('\n')) {
			var fields = line.Split (' ', '\t');
			if (fields.Count () < 6)
				continue;
			if (fields [0] != "pause-time")
				continue;
			var ticks = Int64.Parse (fields [4]);
			var ms = (double)ticks / 10000.0;
			times.Add (ms);
		}

		if (times.Count == 0) {
			Console.Error.WriteLine ("Error: no pause times.");
			return false;
		}

		Console.WriteLine ("Uploading pause times: {0}", string.Join (" ", times));

		var metricRow = new PostgresRow ();
		metricRow.Set ("run", NpgsqlTypes.NpgsqlDbType.Integer, runId);
		metricRow.Set ("metric", NpgsqlTypes.NpgsqlDbType.Varchar, "pause-times");
		metricRow.Set ("resultArray", NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Double, times.ToArray ());
		PostgresInterface.Insert<long> (conn, "RunMetric", metricRow, "id");

		return true;
	}

	enum ValgrindTool {
		Massif,
		Cachegrind
	};

	public static int Main (string[] args)
	{
		IEnumerable<string> benchmarkNames = null;
		//var pausetime = false;
		var timeout = -1;
		string rootFromCmdline = null;
		string buildURL = null;
		string logURL = null;
		string pullRequestURL = null;
		string monoRepositoryPath = null;
		long? runSetId = null;
		long? runId = null;
		string configFile = null;
		string machineName = null;
		bool justCreateRunSet = false;
		bool justListBenchmarks = false;
		string valgrindBinary = null;
		ValgrindTool? valgrindTool = null;
		string valgrindOutputFilename = null;
		string grepBinprotPath = null;
		string binprotFilePath = null;
		Commit mainCommit = null;
		List<Commit> secondaryCommits = new List<Commit> ();

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

		var testsDir = Path.Combine (root, "tests");
		var benchmarksDir = Path.Combine (root, "benchmarks");
		var machinesDir = Path.Combine (root, "machines");
		var productsDir = Path.Combine (root, "products");

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
			} else if (args [optindex] == "--machine") {
				machineName = args [++optindex];
			} else if (args [optindex] == "--build-url") {
				buildURL = args [++optindex];
			} else if (args [optindex] == "--log-url") {
				logURL = args [++optindex];
			} else if (args [optindex] == "--pull-request-url") {
				pullRequestURL = args [++optindex];
			} else if (args [optindex] == "--mono-repository") {
				monoRepositoryPath = args [++optindex];
			} else if (args [optindex] == "--create-run-set") {
				justCreateRunSet = true;
			} else if (args [optindex] == "--run-set-id") {
				runSetId = Int64.Parse (args [++optindex]);
			} else if (args [optindex] == "--run-id") {
				runId = Int64.Parse (args [++optindex]);
			} else if (args [optindex] == "--root") {
				rootFromCmdline = args [++optindex];
			} else if (args [optindex] == "--main-product") {
				var name = args [++optindex];
				var hash = args [++optindex];
				if (mainCommit != null) {
					Console.Error.WriteLine ("Error: Only one --main-product is supported.");
					UsageAndExit ();
				}
				var product = compare.Utils.LoadProductFromFile (name, productsDir);
				mainCommit = new Commit { Product = product, Hash = hash };
			} else if (args [optindex] == "--secondary-product") {
				var name = args [++optindex];
				var hash = args [++optindex];
				var product = compare.Utils.LoadProductFromFile (name, productsDir);
				secondaryCommits.Add (new Commit { Product = product, Hash = hash });
			} else if (args [optindex] == "--valgrind-massif") {
				if (valgrindBinary != null) {
					Console.Error.WriteLine ("Error: More than one Valgrind option given.");
					UsageAndExit ();
				}
				valgrindBinary = args [++optindex];
				valgrindOutputFilename = args [++optindex];
				valgrindTool = ValgrindTool.Massif;
			} else if (args [optindex] == "--valgrind-cachegrind") {
				if (valgrindBinary != null) {
					Console.Error.WriteLine ("Error: More than one Valgrind option given.");
					UsageAndExit ();
				}
				valgrindBinary = args [++optindex];
				valgrindOutputFilename = args [++optindex];
				valgrindTool = ValgrindTool.Cachegrind;
			} else if (args [optindex] == "-t" || args [optindex] == "--timeout") {
				timeout = Int32.Parse (args [++optindex]);
				timeout = timeout <= 0 ? -1 : timeout;
			} else if (args [optindex] == "--sgen-grep-binprot") {
				grepBinprotPath = args [++optindex];
			} else if (args [optindex] == "--upload-pause-times") {
				binprotFilePath = args [++optindex];
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

		var configFileFromCommandLine = configFile != null;
		if (!configFileFromCommandLine)
			configFile = Path.Combine (root, "configs", "default-sgen.conf");
		var config = compare.Utils.LoadConfigFromFile (configFile, rootFromCmdline, !(justListBenchmarks || binprotFilePath != null));

		if (justCreateRunSet && runSetId != null) {
			Console.Error.WriteLine ("Error: --create-run-set and --run-set-id are incompatible.");
			Environment.Exit (1);
		}

		if (justListBenchmarks && benchmarkNames != null) {
			Console.Error.WriteLine ("Error: -b/--benchmarks and -l/--list-benchmarks are incompatible.");
			Environment.Exit (1);
		}
		if (justListBenchmarks && !configFileFromCommandLine) {
			Console.Error.WriteLine ("Error: -l/--list-benchmarks requires --config-file.");
			Environment.Exit (1);
		}

		if (args.Length - optindex != 0)
			return UsageAndExit (null, 1);

		if (binprotFilePath != null && (runId == null || grepBinprotPath == null)) {
			Console.Error.WriteLine ("Error: --upload-pause-times also requires --run-id and --sgen-grep-binprot.");
			Environment.Exit (1);
		}

		if (benchmarkNames == null)
			benchmarkNames = config.Benchmarks;

		var benchmarks = compare.Utils.LoadAllBenchmarksFrom (benchmarksDir, benchmarkNames);
		if (benchmarks == null) {
			Console.Error.WriteLine ("Error: Could not load all benchmarks.");
			Environment.Exit (1);
		}

		if (justListBenchmarks) {
			if (machineName != null) {
				var listMachine = compare.Utils.LoadMachineFromFile (machineName, machinesDir);
				if (listMachine == null) {
					Console.Error.WriteLine ("Error: Could not load machine `{0}`.", machineName);
					Environment.Exit (1);
				}
				if (listMachine.ExcludeBenchmarks != null)
					benchmarks = benchmarks.Where (b => !listMachine.ExcludeBenchmarks.Contains (b.Name)).ToList ();
			}
			foreach (var benchmark in benchmarks.OrderBy (b => b.Name)) {
				Console.Out.WriteLine (benchmark.Name);
			}
			Environment.Exit (0);
		}

		InitCommons ();

		var dbConnection = PostgresInterface.Connect ();

		if (binprotFilePath != null) {
			var success = UploadPauseTimes (dbConnection, binprotFilePath, grepBinprotPath, runId.Value);
			Environment.Exit (success ? 0 : 1);
		}

		if (mainCommit == null)
			mainCommit = new Commit { Product = compare.Utils.LoadProductFromFile ("mono", productsDir) };

		var gitHubClient = GitHubInterface.GitHubClient;

		Machine machine = null;
		if (machineName == null) {
			machine = compare.Utils.LoadMachineCurrentFrom (machinesDir);
		} else {
			machine = compare.Utils.LoadMachineFromFile (machineName, machinesDir);
		}

		if (machine != null && machine.ExcludeBenchmarks != null)
			benchmarks = benchmarks.Where (b => !machine.ExcludeBenchmarks.Contains (b.Name)).ToList ();

		if (machine == null) { // couldn't find machine file
			var hostarch = compare.Utils.LocalHostnameAndArch ();
			machine = new Machine ();
			machine.Name = hostarch.Item1;
			machine.Architecture = hostarch.Item2;
		}

		foreach (var commit in new Commit[] { mainCommit }.Concat (secondaryCommits)) {
			if (!AsyncContext.Run (() => compare.Utils.CompleteCommit (config, commit))) {
				Console.Error.WriteLine ("Error: Could not get commit for product {0}.", commit.Product.Name);
				Environment.Exit (1);
			}
		}

		RunSet runSet;
		if (runSetId != null) {
			if (pullRequestURL != null) {
				Console.Error.WriteLine ("Error: Pull request URL cannot be specified for an existing run set.");
				Environment.Exit (1);
			}
			runSet = AsyncContext.Run (() => RunSet.FromId (dbConnection, machine, runSetId.Value, config, mainCommit, secondaryCommits, buildURL, logURL));
			if (runSet == null) {
				Console.Error.WriteLine ("Error: Could not get run set.");
				Environment.Exit (1);
			}
		} else {
			long? pullRequestBaselineRunSetId = null;

			if (pullRequestURL != null) {
				if (monoRepositoryPath == null) {
					Console.Error.WriteLine ("Error: Must specify a mono repository path to test a pull request.");
					Environment.Exit (1);
				}

				var repo = new compare.Repository (monoRepositoryPath);

				pullRequestBaselineRunSetId = AsyncContext.Run (() => GetPullRequestBaselineRunSetId (dbConnection, mainCommit.Product, pullRequestURL, repo, config));
				if (pullRequestBaselineRunSetId == null) {
					Console.Error.WriteLine ("Error: No appropriate baseline run set found.");
					Environment.Exit (1);
				}
			}

			runSet = new RunSet {
				StartDateTime = DateTime.Now,
				Config = config,
				Commit = mainCommit,
				SecondaryCommits = secondaryCommits,
				BuildURL = buildURL,
				LogURL = logURL,
				PullRequestURL = pullRequestURL,
				PullRequestBaselineRunSetId = pullRequestBaselineRunSetId
			};

			Console.Error.WriteLine ("Set start time to {0}", runSet.StartDateTime);
		}

		var reportFailure = false;

		if (!justCreateRunSet) {
			var someSuccess = false;

			var runTool = valgrindBinary;
			string runToolArguments = null;
			if (runTool != null) {
				switch (valgrindTool) {
				case ValgrindTool.Massif:
					runToolArguments = string.Format ("--tool=massif --massif-out-file={0} --max-snapshots=1000 --detailed-freq=100 --pages-as-heap=yes", valgrindOutputFilename);
					break;
				case ValgrindTool.Cachegrind:
					runToolArguments = string.Format ("--tool=cachegrind --cachegrind-out-file={0} --cache-sim=yes --branch-sim=yes", valgrindOutputFilename);
					break;
				default:
					Console.Error.WriteLine ("Error: Unsupported Valgrind tool.");
					Environment.Exit (1);
					break;
				}
			}

			int binaryProtocolIndex = 0;

			foreach (var benchmark in benchmarks.OrderBy (b => b.Name)) {
				// Run the benchmarks
				if (config.Count <= 0)
					throw new ArgumentOutOfRangeException (String.Format ("configs [\"{0}\"].Count <= 0", config.Name));

				Console.Out.WriteLine ("Running benchmark \"{0}\" with config \"{1}\"", benchmark.Name, config.Name);

				var runner = new compare.UnixRunner (testsDir, config, benchmark, machine, timeout, runTool, runToolArguments);

				var result = new Result {
					DateTime = DateTime.Now,
					Benchmark = benchmark,
					Config = config,
				};

				var haveTimedOut = false;
				var haveCrashed = false;

				var count = valgrindBinary == null ? config.Count + 1 : 1;

				for (var i = 0; i < count; ++i) {
					bool timedOut;

					if (valgrindBinary == null)
						Console.Out.Write ("\t\t-> {0} ", i == 0 ? "[dry run]" : String.Format ("({0}/{1})", i, config.Count));

					string binaryProtocolFile = null;
					string workingDirectory = Path.Combine (testsDir, benchmark.TestDirectory);
					if (config.ProducesBinaryProtocol) {
						do {
							++binaryProtocolIndex;
							binaryProtocolFile = Path.Combine(workingDirectory, string.Format ("binprot.{0}", binaryProtocolIndex));
						} while (File.Exists (binaryProtocolFile));
					}
	
					var elapsedMilliseconds = runner.Run (binaryProtocolFile, out timedOut);

					// if running for time, the first one is the dry run
					if (valgrindBinary == null && i == 0)
						continue;

					if (elapsedMilliseconds != null) {
						var run = new Result.Run { BinaryProtocolFilename = binaryProtocolFile == null ? null : Path.Combine(workingDirectory, binaryProtocolFile) };

						if (valgrindBinary == null) {
							run.RunMetrics.Add (new Result.RunMetric {
								Metric = Result.RunMetric.MetricType.Time,
								Value = TimeSpan.FromMilliseconds (elapsedMilliseconds.Value)
							});
						} else {
							switch (valgrindTool) {
							case ValgrindTool.Massif:
								{
									var results = MemoryIntegral (valgrindOutputFilename);
									run.RunMetrics.Add (new Result.RunMetric {
										Metric = Result.RunMetric.MetricType.MemoryIntegral,
										Value = results.Item1
									});
									run.RunMetrics.Add (new Result.RunMetric {
										Metric = Result.RunMetric.MetricType.Instructions,
										Value = results.Item2
									});
								}
								break;
							case ValgrindTool.Cachegrind:
								{
									var results = CacheAndBranches (valgrindOutputFilename);
									run.RunMetrics.Add (new Result.RunMetric {
										Metric = Result.RunMetric.MetricType.CachegrindResults,
										Value = results.Item1
									});
									run.RunMetrics.Add (new Result.RunMetric {
										Metric = Result.RunMetric.MetricType.CacheMissRate,
										Value = results.Item2
									});
									run.RunMetrics.Add (new Result.RunMetric {
										Metric = Result.RunMetric.MetricType.BranchMispredictionRate,
										Value = results.Item3
									});
								}
								break;
							}
						}
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
					runSet.TimedOutBenchmarks.Add (benchmark.Name);
				if (haveCrashed)
					runSet.CrashedBenchmarks.Add (benchmark.Name);

				runSet.Results.Add (result);

				if (haveTimedOut || result.Runs.Count == 0)
					reportFailure = true;
			}

			if (!someSuccess)
				Console.WriteLine ("all runs failed.");
		}
		
		runSet.FinishDateTime = DateTime.Now;
		Console.Error.WriteLine ("Start time is {0} - finish time is {1}", runSet.StartDateTime, runSet.FinishDateTime);

		Console.WriteLine ("uploading");
		try {
			var newIds = runSet.UploadToPostgres (dbConnection, machine);
			Console.WriteLine ("http://xamarin.github.io/benchmarker/front-end/runset.html#id={0}", newIds.Item1);
			if (pullRequestURL != null) {
				Console.WriteLine ("http://xamarin.github.io/benchmarker/front-end/pullrequest.html#id={0}", newIds.Item2.Value);
			}
			Console.Write ("{{ \"runSetId\": \"{0}\"", newIds.Item1);
            if (pullRequestURL != null)
				Console.Write (", \"pullRequestId\": \"{0}\"", newIds.Item2.Value);
			Console.Write (", \"runs\": [ ");

			var runStrings = new List<string> ();
			foreach (var run in runSet.AllRuns) {
				var str = string.Format ("\"id\": {0}", run.PostgresId.Value);
				if (run.BinaryProtocolFilename != null)
					str = string.Format ("{0}, \"binaryProtocolFile\": \"{1}\"", str, run.BinaryProtocolFilename);
				runStrings.Add ("{ " + str + " }");
			}
			Console.Write (string.Join (", ", runStrings));

			Console.Write (" ]");
            Console.WriteLine (" }");
		} catch (Exception exc) {
			Console.Error.WriteLine ("Error: Failure uploading data: " + exc);
			Environment.Exit (1);
		}

		dbConnection.Close ();

		if (reportFailure) {
			Console.Error.WriteLine ("Error: Some benchmarks timed out or failed completely.");
			return 1;
		}

		return 0;
	}
}

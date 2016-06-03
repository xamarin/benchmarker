using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Mono.Unix.Native;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;
using Benchmarker;
using Benchmarker.Models;

namespace compare
{
	public class Utils
	{
		public static string RunForStdout (string command, string workingDirectory, params string[] args)
		{
			var startInfo = new ProcessStartInfo (command);
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardOutput = true;
			startInfo.WorkingDirectory = workingDirectory;
			startInfo.Arguments = String.Join (" ", args);

			using (var process = Process.Start (startInfo)) {
				var stdout = Task.Run (() => new StreamReader (process.StandardOutput.BaseStream).ReadToEnd ()).Result;

				process.WaitForExit ();

				if (process.ExitCode != 0)
					return null;

				return stdout;
			}
		}

		public static List<Benchmark> LoadAllBenchmarksFrom (string directory)
		{
			return LoadAllBenchmarksFrom (directory, new string[0]);
		}

		public static List<Benchmark> LoadAllBenchmarksFrom (string directory, IEnumerable<string> names)
		{
			var allPaths = Directory.EnumerateFiles (directory)
				.Where (f => f.EndsWith (".benchmark"));
			if (names != null) {
				foreach (var name in names) {
					if (!allPaths.Any (p => Path.GetFileNameWithoutExtension (p) == name))
						return null;
				}
				allPaths = allPaths
					.Where (f => names.Any (n => Path.GetFileNameWithoutExtension (f) == n));
			}
			return allPaths
				.Select (f => LoadBenchmarkFromFile (f))
				.Where (b => !b.OnlyExplicit || (names != null && names.Contains (b.Name)))
				.ToList ();
		}

		private static Benchmark LoadBenchmarkFromFile(string filename)
		{
			using (var reader = new StreamReader (new FileStream (filename, FileMode.Open))) {
				return Benchmark.LoadFromString (reader.ReadToEnd ());
			}
		}

		public static Config LoadConfigFromFile(string filename, string root, bool expandRoot)
		{
			using (var reader = new StreamReader (new FileStream (filename, FileMode.Open))) {
				return Config.LoadFromString (reader.ReadToEnd (), root, expandRoot);
			}
		}

		public static Machine LoadMachineFromFile(string machineName, string directory)
		{
			string filename = Path.Combine (directory, String.Format ("{0}.conf", machineName));

			try {
				using (var reader = new StreamReader (new FileStream (filename, FileMode.Open))) {
					return Machine.LoadFromString (reader.ReadToEnd ());
				}
			} catch (FileNotFoundException) {
				return null;
			}
		}

		public static Machine LoadMachineCurrentFrom (string directory)
		{
			return LoadMachineFromFile (Environment.MachineName, directory);
		}

		public static Product LoadProductFromFile (string productName, string directory) {
			var filename = Path.Combine (directory, String.Format ("{0}.conf", productName));
			return Product.LoadFromString (File.ReadAllText (filename));
		}

		public static Tuple<string, string> LocalHostnameAndArch () {
			Utsname utsname;
			var res = Syscall.uname (out utsname);
			string arch;
			string hostname;
			if (res != 0) {
				arch = "unknown";
				hostname = "unknown";
			} else {
				arch = utsname.machine;
				hostname = utsname.nodename;
			}

			return Tuple.Create (hostname, arch);
		}

		public static void SetProcessStartEnvironmentVariables (ProcessStartInfo info, Config cfg, string binaryProtocolFilename)
		{
			info.EnvironmentVariables.Clear ();

			foreach (var env in cfg.ProcessMonoEnvironmentVariables (binaryProtocolFilename))
				info.EnvironmentVariables.Add (env.Key, env.Value);
		}

		public static ProcessStartInfo NewProcessStartInfo (Config cfg, string binaryProtocolFilename)
		{
			var info = new ProcessStartInfo {
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
			};

			if (!cfg.NoMono) {
				if (string.IsNullOrWhiteSpace (cfg.Mono)) {
					Console.Error.WriteLine ("Error: No mono executable specified.");
					Environment.Exit (1);
				}
				var monoPath = cfg.Mono;
				if (Path.GetDirectoryName (monoPath) != "")
					monoPath = Path.GetFullPath (monoPath);
				info.FileName = monoPath;
			}

			SetProcessStartEnvironmentVariables (info, cfg, binaryProtocolFilename);

			return info;
		}

		public static string PrintableEnvironmentVariables (ProcessStartInfo info)
		{
			var builder = new StringBuilder ();
			foreach (DictionaryEntry entry in info.EnvironmentVariables) {
				builder.Append (entry.Key.ToString ());
				builder.Append ("=");
				builder.Append (entry.Value.ToString ());
				builder.Append (" ");
			}
			return builder.ToString ();
		}


		public async static Task<bool> CompleteCommit (Config cfg, Commit commit)
		{
			if (commit.Product.Name == "mono" && !cfg.NoMono) {
				var binaryProtocolFile = cfg.ProducesBinaryProtocol ? "/tmp/binprot.dummy" : null;
				var info = NewProcessStartInfo (cfg, binaryProtocolFile);
				if (!String.IsNullOrWhiteSpace (info.FileName)) {
					/* Run without timing with --version */
					info.Arguments = "--version";

					Console.Out.WriteLine ("\t$> {0} {1} {2}", PrintableEnvironmentVariables (info), info.FileName, info.Arguments);

					var process = Process.Start (info);
					var version = Task.Run (() => new StreamReader (process.StandardOutput.BaseStream).ReadToEnd ()).Result;
					var versionError = Task.Run (() => new StreamReader (process.StandardError.BaseStream).ReadToEnd ()).Result;

					process.WaitForExit ();
					process.Close ();

					var line = version.Split (new char[] { '\n' }, 2) [0];
					var regex = new Regex ("^Mono JIT.*\\((.*)/([0-9a-f]+) (.*)\\)");
					var match = regex.Match (line);

					if (match.Success) {
						commit.Branch = match.Groups [1].Value;
						var hash = match.Groups [2].Value;
						if (commit.Hash != null) {
							if (!commit.Hash.StartsWith (hash)) {
								Console.Error.WriteLine ("Error: Commit hash for mono specified on command line does not match the one reported with --version.");
								return false;
							}
						} else {
							commit.Hash = hash;
						}
						var date = match.Groups [3].Value;
						Console.WriteLine ("branch: " + commit.Branch + " hash: " + commit.Hash + " date: " + date);
					}
				}

				if (commit.Branch == "(detached")
					commit.Branch = null;

				try {
					var gitRepoDir = Path.GetDirectoryName (cfg.Mono);
					var repo = new Repository (gitRepoDir);
					var gitHash = repo.RevParse (commit.Hash);
					if (gitHash == null) {
						Console.WriteLine ("Could not get commit " + commit.Hash + " from repository");
					} else {
						Console.WriteLine ("Got commit " + gitHash + " from repository");

						if (commit.Hash != null && commit.Hash != gitHash) {
							Console.Error.WriteLine ("Error: Commit hash specified on command line does not match the one from the git repository.");
							return false;
						}

						commit.Hash = gitHash;
						commit.MergeBaseHash = repo.MergeBase (commit.Hash, "master");
						commit.CommitDate = repo.CommitDate (commit.Hash);

						if (commit.CommitDate == null) {
							Console.Error.WriteLine ("Error: Could not get commit date from the git repository.");
							return false;
						}

						Console.WriteLine ("Commit {0} merge base {1} date {2}", commit.Hash, commit.MergeBaseHash, commit.CommitDate);
					}
				} catch (Exception) {
					Console.WriteLine ("Could not get git repository");
				}
			}

			if (commit.Hash == null) {
				Console.Error.WriteLine ("Error: cannot parse mono version and no commit given.");
				return false;
			}

			Octokit.Commit gitHubCommit = await ResolveFullHashViaGithub (commit);

			if (gitHubCommit == null) {
				Console.WriteLine ("Could not get commit " + commit.Hash + " from GitHub");
			} else {
				commit.Hash = gitHubCommit.Sha;
				if (commit.CommitDate == null)
					commit.CommitDate = gitHubCommit.Committer.Date.DateTime.ToLocalTime ();
				Console.WriteLine ("Got commit " + commit.Hash + " from GitHub");
			}

			if (commit.CommitDate == null) {
				Console.Error.WriteLine ("Error: Could not get a commit date.");
				return false;
			}

			return true;
		}

		private static async Task<Octokit.Commit> ResolveFullHashViaGithub (Commit commit) {
			if (commit == null) {
				return null;
			}

			Octokit.Commit gitHubCommit = null;
			Octokit.TreeResponse treeResponse = null;
			try {
				var gitHubClient = GitHubInterface.GitHubClient;
				treeResponse = await GitHubInterface.RunWithRetry (() => gitHubClient.GitDatabase.Tree.Get (commit.Product.GitHubUser, commit.Product.GitHubRepo, commit.Hash));
				gitHubCommit = await GitHubInterface.RunWithRetry (() => gitHubClient.GitDatabase.Commit.Get (commit.Product.GitHubUser, commit.Product.GitHubRepo, treeResponse.Sha));
			} catch (Octokit.NotFoundException) {
				Console.WriteLine ("Commit " + commit + " not found on GitHub");
			}

			return gitHubCommit;
		}
	}
}

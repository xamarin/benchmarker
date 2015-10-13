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
		private Utils ()
		{
		}

		public static List<Benchmark> LoadAllBenchmarksFrom (string directory)
		{
			return LoadAllBenchmarksFrom (directory, new string[0]);
		}

		public static List<Benchmark> LoadAllBenchmarksFrom (string directory, string[] names)
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
				.ToList ();
		}

		private static Benchmark LoadBenchmarkFromFile(string filename)
		{
			using (var reader = new StreamReader (new FileStream (filename, FileMode.Open))) {
				return Benchmark.LoadFromString (reader.ReadToEnd ());
			}
		}

		public static Config LoadConfigFromFile(string filename, string root)
		{
			using (var reader = new StreamReader (new FileStream (filename, FileMode.Open))) {
				return Config.LoadFromString (reader.ReadToEnd (), root);
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

		public static Result LoadResultFromFile(string filename)
		{
			using (var reader = new StreamReader (new FileStream (filename, FileMode.Open))) {
				return Result.LoadFromString (reader.ReadToEnd ());
			}
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


		public static ProcessStartInfo NewProcessStartInfo (Config cfg)
		{
			var info = new ProcessStartInfo {
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
			};

			if (!cfg.NoMono) {
				if (String.IsNullOrEmpty (cfg.Mono)) {
					Console.Error.WriteLine ("Error: No mono executable specified.");
					Environment.Exit (1);
				}
				var monoPath = cfg.Mono;
				if (Path.GetDirectoryName (monoPath) != "")
					monoPath = Path.GetFullPath (monoPath);
				info.FileName = monoPath;
			}

			foreach (var env in cfg.processedMonoEnvironmentVariables)
				info.EnvironmentVariables.Add (env.Key, env.Value);

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


		public async static Task<Commit> GetCommit (Config cfg, string optionalCommitHash, string optionalGitRepoDir)
		{
			if (cfg.NoMono && optionalCommitHash == null) {
				// FIXME: return a dummy commit
				return null;
			}

			var commit = new Commit ();
			var info = NewProcessStartInfo (cfg);
			if (!String.IsNullOrEmpty (info.FileName)) {
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
					commit.Hash = match.Groups [2].Value;
					var date = match.Groups [3].Value;
					Console.WriteLine ("branch: " + commit.Branch + " hash: " + commit.Hash + " date: " + date);
				} else {
					if (optionalCommitHash == null) {
						Console.Error.WriteLine ("Error: cannot parse mono version and no commit given.");
						return null;
					}
				}
			}

			if (commit.Branch == "(detached")
				commit.Branch = null;

			if (optionalCommitHash != null) {
				if (commit.Hash != null && !optionalCommitHash.StartsWith (commit.Hash)) {
					Console.Error.WriteLine ("Error: Commit hash specified on command line does not match the one reported with --version.");
					return null;
				}
				commit.Hash = optionalCommitHash;
			}

			try {
				var gitRepoDir = optionalGitRepoDir ?? Path.GetDirectoryName (cfg.Mono);
				var repo = new Repository (gitRepoDir);
				var gitHash = repo.RevParse (commit.Hash);
				if (gitHash == null) {
					Console.WriteLine ("Could not get commit " + commit.Hash + " from repository");
				} else {
					Console.WriteLine ("Got commit " + gitHash + " from repository");

					if (optionalCommitHash != null && optionalCommitHash != gitHash) {
						Console.Error.WriteLine ("Error: Commit hash specified on command line does not match the one from the git repository.");
						return null;
					}

					commit.Hash = gitHash;
					commit.MergeBaseHash = repo.MergeBase (commit.Hash, "master");
					commit.CommitDate = repo.CommitDate (commit.Hash);

					if (commit.CommitDate == null) {
						Console.Error.WriteLine ("Error: Could not get commit date from the git repository.");
						return null;
					}

					Console.WriteLine ("Commit {0} merge base {1} date {2}", commit.Hash, commit.MergeBaseHash, commit.CommitDate);
				}
			} catch (Exception) {
				Console.WriteLine ("Could not get git repository");
			}

			if (commit.Hash == null && optionalCommitHash == null) {
				Console.Error.WriteLine ("Error: Neither `mono --version' or `--commit` provides a hash ");
				return null;
			}
			Octokit.Commit gitHubCommit = await ResolveFullHashViaGithub (commit.Hash);
			Octokit.Commit gitHubOptionalCommit = await ResolveFullHashViaGithub (optionalCommitHash);

			if (gitHubCommit == null) {
				Console.WriteLine ("Could not get commit " + commit.Hash + " from GitHub");
			} else {
				if (optionalCommitHash != null && (optionalCommitHash != gitHubCommit.Sha && gitHubOptionalCommit.Sha != gitHubCommit.Sha)) {
					Console.Error.WriteLine ("Error: Commit hash specified on command line does not match the one from GitHub.");
					return null;
				}

				commit.Hash = gitHubCommit.Sha;
				if (commit.CommitDate == null)
					commit.CommitDate = gitHubCommit.Committer.Date.DateTime;
				Console.WriteLine ("Got commit " + commit.Hash + " from GitHub");
			}

			return commit;
		}

		private static async Task<Octokit.Commit> ResolveFullHashViaGithub (string commit) {
			if (commit == null) {
				return null;
			}

			Octokit.Commit gitHubCommit = null;
			Octokit.TreeResponse treeResponse = null;
			try {
				var gitHubClient = GitHubInterface.GitHubClient;
				treeResponse = await GitHubInterface.RunWithRetry (() => gitHubClient.GitDatabase.Tree.Get ("mono", "mono", commit));
				gitHubCommit = await GitHubInterface.RunWithRetry (() => gitHubClient.GitDatabase.Commit.Get ("mono", "mono", treeResponse.Sha));
			} catch (Octokit.NotFoundException) {
				Console.WriteLine ("Commit " + commit + " not found on GitHub");
			}

			return gitHubCommit;
		}
	}
}

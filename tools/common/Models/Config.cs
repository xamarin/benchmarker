using System;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Parse;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections;
using System.Text;
using Benchmarker.Common.Git;

namespace Benchmarker.Common.Models
{
	public class Config
	{
		const string rootVarString = "$ROOT";

		public string Name { get; set; }
		public int Count { get; set; }
		public bool NoMono {get; set; }
		public string Mono { get; set; }
		public string[] MonoOptions { get; set; }
		public Dictionary<string, string> MonoEnvironmentVariables { get; set; }
		public Dictionary<string, string> UnsavedMonoEnvironmentVariables { get; set; }

		public string MonoExecutable {
			get {
				return Path.GetFileName (Mono);
			}
		}

		Dictionary<string, string> processedMonoEnvironmentVariables;

		public Config ()
		{
		}

		static void ExpandRootInEnvironmentVariables (Dictionary<string, string> processedEnvVars, Dictionary<string, string> unexpandedEnvVars, string root)
		{
			foreach (var kvp in unexpandedEnvVars) {
				var key = kvp.Key;
				var unexpandedValue = kvp.Value;
				if (unexpandedValue.Contains (rootVarString)) {
					if (root != null)
						processedEnvVars [key] = unexpandedValue.Replace (rootVarString, root);
					else
						throw new InvalidDataException ("Configuration requires a root directory.");
				} else {
					processedEnvVars [key] = unexpandedValue;
				}
			}
		}

		public static Config LoadFrom (string filename, string root)
		{
			using (var reader = new StreamReader (new FileStream (filename, FileMode.Open))) {
				var config = JsonConvert.DeserializeObject<Config> (reader.ReadToEnd ());

				if (String.IsNullOrEmpty (config.Name))
					throw new InvalidDataException ("Configuration does not have a `Name`.");

				if (config.NoMono) {
					Debug.Assert (config.MonoOptions == null || config.MonoOptions.Length == 0);
					Debug.Assert (config.MonoEnvironmentVariables == null || config.MonoEnvironmentVariables.Count == 0);
					Debug.Assert (config.UnsavedMonoEnvironmentVariables == null || config.UnsavedMonoEnvironmentVariables.Count == 0);
				}

				if (String.IsNullOrEmpty (config.Mono)) {
					config.Mono = String.Empty;
				} else if (root != null) {
					config.Mono = config.Mono.Replace (rootVarString, root);
				} else if (config.Mono.Contains (rootVarString)) {
					throw new InvalidDataException ("Configuration requires a root directory.");
				}

				if (config.Count < 1)
					config.Count = 10;

				if (config.MonoOptions == null)
					config.MonoOptions = new string[0];

				if (config.MonoEnvironmentVariables == null)
					config.MonoEnvironmentVariables = new Dictionary<string, string> ();
				if (config.UnsavedMonoEnvironmentVariables == null)
					config.UnsavedMonoEnvironmentVariables = new Dictionary<string, string> ();

				config.processedMonoEnvironmentVariables = new Dictionary<string, string> ();
				ExpandRootInEnvironmentVariables (config.processedMonoEnvironmentVariables, config.MonoEnvironmentVariables, root);
				ExpandRootInEnvironmentVariables (config.processedMonoEnvironmentVariables, config.UnsavedMonoEnvironmentVariables, root);

				return config;
			}
		}

		public ProcessStartInfo NewProcessStartInfo ()
		{
			var info = new ProcessStartInfo {
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
			};

			if (!NoMono) {
				if (Mono == null) {
					Console.Error.WriteLine ("Error: No mono executable specified.");
					Environment.Exit (1);
				}
				info.FileName = Mono;
			}

			foreach (var env in processedMonoEnvironmentVariables)
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

		public async Task<Commit> GetCommit (string optionalCommitHash, string optionalGitRepoDir)
		{
			if (NoMono) {
				// FIXME: return a dummy commit
				return null;
			}

			var info = NewProcessStartInfo ();
			/* Run without timing with --version */
			info.Arguments = "--version";

			Console.Out.WriteLine ("\t$> {0} {1} {2}", PrintableEnvironmentVariables (info), info.FileName, info.Arguments);

			var process = Process.Start (info);
			var version = Task.Run (() => new StreamReader (process.StandardOutput.BaseStream).ReadToEnd ()).Result;
			var versionError = Task.Run (() => new StreamReader (process.StandardError.BaseStream).ReadToEnd ()).Result;

			process.WaitForExit ();
			process.Close ();

			var line = version.Split (new char[] {'\n'}, 2) [0];
			var regex = new Regex ("^Mono JIT.*\\((.*)/([0-9a-f]+) (.*)\\)");
			var match = regex.Match (line);

			var commit = new Commit ();

			if (match.Success) {
				commit.Branch = match.Groups [1].Value;
				commit.Hash = match.Groups [2].Value;
				var date = match.Groups [3].Value;
				Console.WriteLine ("branch: " + commit.Branch + " hash: " + commit.Hash + " date: " + date);
			} else {
				if (optionalCommitHash == null) {
					Console.WriteLine ("Error: cannot parse mono version and no commit given.");
					return null;
				}
			}

			if (commit.Branch == "(detached")
				commit.Branch = null;

			if (optionalCommitHash != null) {
				if (commit.Hash != null && !optionalCommitHash.StartsWith (commit.Hash)) {
					Console.WriteLine ("Error: Commit hash specified on command line does not match the one reported with --version.");
					return null;
				}
				commit.Hash = optionalCommitHash;
			}

			try {
				var gitRepoDir = optionalGitRepoDir ?? Path.GetDirectoryName (Mono);
				var repo = new Repository (gitRepoDir);
				var gitHash = repo.RevParse (commit.Hash);
				if (gitHash == null) {
					Console.WriteLine ("Could not get commit " + commit.Hash + " from repository");
				} else {
					Console.WriteLine ("Got commit " + gitHash + " from repository");

					if (optionalCommitHash != null && optionalCommitHash != gitHash) {
						Console.WriteLine ("Error: Commit hash specified on command line does not match the one from the git repository.");
						return null;
					}

					commit.Hash = gitHash;
					commit.MergeBaseHash = repo.MergeBase (commit.Hash, "master");
					commit.CommitDate = repo.CommitDate (commit.Hash);

					if (commit.CommitDate == null) {
						Console.WriteLine ("Error: Could not get commit date from the git repository.");
						return null;
					}

					Console.WriteLine ("Commit {0} merge base {1} date {2}", commit.Hash, commit.MergeBaseHash, commit.CommitDate);
				}
			} catch (Exception) {
				Console.WriteLine ("Could not get git repository");
			}

			Octokit.Commit gitHubCommit = null;
			try {
				var gitHubClient = GitHubInterface.GitHubClient;
				gitHubCommit = await ParseInterface.RunWithRetry (() => gitHubClient.GitDatabase.Commit.Get ("mono", "mono", commit.Hash), typeof (Octokit.NotFoundException));
			} catch (Octokit.NotFoundException) {
				Console.WriteLine ("Commit " + commit.Hash + " not found on GitHub");
			}
			if (gitHubCommit == null) {
				Console.WriteLine ("Could not get commit " + commit.Hash + " from GitHub");
			} else {
				if (optionalCommitHash != null && optionalCommitHash != gitHubCommit.Sha) {
					Console.WriteLine ("Error: Commit hash specified on command line does not match the one from GitHub.");
					return null;
				}

				commit.Hash = gitHubCommit.Sha;
				if (commit.CommitDate == null)
					commit.CommitDate = gitHubCommit.Committer.Date.DateTime;
				Console.WriteLine ("Got commit " + commit.Hash + " from GitHub");
			}

			return commit;
		}
			
		public override bool Equals (object other)
		{
			if (other == null)
				return false;

			var config = other as Config;
			if (config == null)
				return false;

			return Name.Equals (config.Name);
		}

		public override int GetHashCode ()
		{
			return Name.GetHashCode ();
		}

		static bool OptionsEqual (IEnumerable<string> native, IEnumerable<object> parse)
		{
			if (parse == null)
				return false;
			foreach (var s in native) {
				var found = false;
				foreach (var p in parse) {
					var ps = p as string;
					if (s == ps) {
						found = true;
						break;
					}
				}
				if (!found)
					return false;
			}
			return true;
		}

		static bool EnvironmentVariablesEqual (IDictionary<string, string> native, IDictionary<string, object> parse)
		{
			if (parse == null)
				return false;
			foreach (var kv in native) {
				if (!parse.ContainsKey (kv.Key))
					return false;
				var v = parse [kv.Key] as string;
				if (v != kv.Value)
					return false;
			}
			return true;
		}

		public bool EqualToParseObject (ParseObject o) {
			if (Name != o.Get<string> ("name"))
				return false;
			if (MonoExecutable != o.Get<string> ("monoExecutable"))
				return false;
			if (!OptionsEqual (MonoOptions, o ["monoOptions"] as IEnumerable<object>))
				return false;
			if (!EnvironmentVariablesEqual (MonoEnvironmentVariables, o ["monoEnvironmentVariables"] as IDictionary<string, object>))
				return false;
			return true;
		}

		public async Task<ParseObject> GetFromParse ()
		{
			var results = await ParseInterface.RunWithRetry (() => ParseObject.GetQuery ("Config")
				.WhereEqualTo ("name", Name)
				.WhereEqualTo ("monoExecutable", MonoExecutable)
				.FindAsync ());
			//Console.WriteLine ("FindAsync Config");
			foreach (var o in results) {
				if (EqualToParseObject (o)) {
					Console.WriteLine ("found config " + o.ObjectId);
					return o;
				}
			}
			return null;
		}

		public async Task<ParseObject> GetOrUploadToParse (List<ParseObject> saveList)
		{
			var obj = await GetFromParse ();
			if (obj != null)
				return obj;

			Console.WriteLine ("creating new config");

			obj = ParseInterface.NewParseObject ("Config");
			obj ["name"] = Name;
			obj ["monoExecutable"] = MonoExecutable;
			obj ["monoOptions"] = MonoOptions;
			obj ["monoEnvironmentVariables"] = MonoEnvironmentVariables;
			saveList.Add (obj);
			return obj;
		}
	}
}

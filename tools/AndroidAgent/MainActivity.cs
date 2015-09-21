using System;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using System.Reflection;
using System.Diagnostics;
using Benchmarker.Common;
using Benchmarks.BH;
using Java.Util.Logging;
using Common.Logging;
using Newtonsoft.Json.Linq;
using Benchmarker.Common.Models;
using Nito.AsyncEx;
using System.Text.RegularExpressions;
using Parse;
using System.Collections.Generic;

namespace AndroidAgent
{
	[Activity (Label = "AndroidAgent", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Activity
	{
		static MainActivity ()
		{
			Logging.SetLogging (new AndroidLogger());
		}

		string GetMonoVersion ()
		{
			Type type = Type.GetType("Mono.Runtime");
			if (type != null)
			{
				MethodInfo displayName = type.GetMethod("GetDisplayName", BindingFlags.Public | BindingFlags.Static);
				if (displayName != null)
					return displayName.Invoke (null, null).ToString ();
			}
			return "(no version)";
		}

		void SetStartButtonText (string text)
		{
			Button button = FindViewById<Button> (Resource.Id.myButton);
			button.Text = text;
		}

		Benchmarker.Common.Models.Result.Run Iteration (string benchmark, int iteration)
		{
			Logging.GetLogging().InfoFormat ("MainActivity | Benchmark {0}: start iteration {1}", benchmark, iteration);
			GC.Collect (1);
			System.Threading.Thread.Sleep (5 * 1000); // cool down?

			var sw = Stopwatch.StartNew ();
			switch (benchmark) {
			case "bh":
				BH.Main (new string[] { "-b", "400", "-s", "200" }, Logging.GetLogging ());
				break;
			case "n-body":
				NBody.Main (new string[] { "5000000" }, Logging.GetLogging ());
				break;
			case "strcat": 
				strcat.Main (new string[] { "40000000" });
				break;
			default:
				throw new NotImplementedException ();
			}
			sw.Stop ();
			Logging.GetLogging().InfoFormat ("MainActivity | Benchmark {0}: finished iteration {1}, took {2}ms", benchmark, iteration, sw.ElapsedMilliseconds);
			return new Benchmarker.Common.Models.Result.Run {
				WallClockTime = TimeSpan.FromMilliseconds (sw.ElapsedMilliseconds),
				Output = "<no stdout>",
				Error = "<no stderr>"
			};
		}

		private Commit GetCommit ()
		{
			// e.g.: "4.3.0 (master/[a-f0-9A-F]{7..40})"
			var regex = new Regex ("^[0-9].*\\((.*)/([0-9a-f]+)\\)");
			var match = regex.Match (GetMonoVersion ());

			var commit = new Commit ();
			if (match.Success) {
				commit.Branch = match.Groups [1].Value;
				commit.Hash = match.Groups [2].Value;
				Logging.GetLogging().Debug ("branch: " + commit.Branch + " hash: " + commit.Hash);
			} else {
				commit.Branch = "<unknown>";
				commit.Hash = "<unknown>";
				Logging.GetLogging().Debug ("couldn't read git information: \"" + GetMonoVersion () + "\"");
			}
			Octokit.Commit gitHubCommit = null;
			try {
				var gitHubClient = GitHubInterface.GitHubClient;
				Octokit.TreeResponse treeResponse = AsyncContext.Run (() => GitHubInterface.RunWithRetry (() => gitHubClient.GitDatabase.Tree.Get ("mono", "mono", commit.Hash)));
				gitHubCommit = AsyncContext.Run (() => GitHubInterface.RunWithRetry (() => gitHubClient.GitDatabase.Commit.Get ("mono", "mono", treeResponse.Sha)));
			} catch (Octokit.NotFoundException e) {
				Logging.GetLogging().Debug ("Commit " + commit.Hash + " not found on GitHub");
				throw e;
			}
			if (gitHubCommit == null) {
				Logging.GetLogging().Debug ("Could not get commit " + commit.Hash + " from GitHub");
			} else {
				commit.Hash = gitHubCommit.Sha;
				commit.CommitDate = gitHubCommit.Committer.Date.DateTime;
				Logging.GetLogging().Info ("Got commit " + commit.Hash + " from GitHub");
			}

			return commit;
		}

		void RunBenchmark (string runSetId, string benchmarkName, string hostname, string architecture)
		{
			const int tryRuns = 10;
			var commit = GetCommit ();
			var config = new Config { Name = "auto-sgen", Mono = String.Empty, MonoOptions = new string[0], MonoEnvironmentVariables = new Dictionary<string, string> (), Count = 10, };
			// TODO: buildURL => wrench log?
			// TODO: logURL => XTC url?
			var runSet = AsyncContext.Run (() => RunSet.FromId (architecture, hostname, runSetId, config, commit, null, null));
			new Task (() => {
				var result = new Benchmarker.Common.Models.Result {
					DateTime = DateTime.Now,
					Benchmark = new Benchmark { Name = benchmarkName, },
					Config = config,
				};
				try {
					for (var i = 0; i < (config.Count + tryRuns); i++) {
						var run = Iteration (benchmarkName, i);
						if (i < tryRuns) {
							continue;
						}
						if (run != null) {
							result.Runs.Add (run);
						} else {
							Logging.GetLogging().DebugFormat ("no result available for #{0}!", i);
						}
					}

					runSet.Results.Add (result);
					var objectId = runSet.UploadToParseGetObjectId (hostname, architecture);
					Logging.GetLogging().InfoFormat ("http://xamarin.github.io/benchmarker/front-end/runset.html#{0}", objectId);
					Logging.GetLogging().InfoFormat ("{{ \"runSetId\": \"{0}\" }}", objectId);
					RunOnUiThread (() => SetStartButtonText ("start"));
				} catch (Exception e) {
					Logging.GetLogging().Error (e);
				}
			}).Start ();
			Logging.GetLogging().InfoFormat ("Benchmark started, run set id {0}", runSetId);
		}

		private static void InitCommons(string bmUsername, string bmPassword, string githubAPIKey) {
			ParseInterface.benchmarkerCredentials = JObject.Parse ("{'username': '" + bmUsername + "', 'password': '" + bmPassword + "'}");
			if (!ParseInterface.Initialize ()) {
				Logging.GetLogging().Error ("Error: Could not initialize Parse interface.");
				throw new Exception ("Error: Could not initialize Parse interface.");
			} else {
				Logging.GetLogging().Info ("InitCommons: Parse successful");
			}
			GitHubInterface.githubCredentials = githubAPIKey;
		}

		protected override void OnCreate (Bundle savedInstanceState)
		{
			string architecture = Android.OS.Build.CpuAbi;
			string hostname = Android.OS.Build.Model + "_" + Android.OS.Build.VERSION.Release;
			base.OnCreate (savedInstanceState);
			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);
			FindViewById<Button> (Resource.Id.myButton).Click += delegate {
				var runSetId = FindViewById<TextView> (Resource.Id.runSetId).Text;
				var benchmarkName = FindViewById<TextView> (Resource.Id.benchmark).Text;
				var bmUsername = FindViewById<TextView> (Resource.Id.bmUsername).Text;
				var bmPassword = FindViewById<TextView> (Resource.Id.bmPassword).Text;
				var githubAPIKey = FindViewById<TextView> (Resource.Id.githubAPIKey).Text;
				InitCommons (bmUsername, bmPassword, githubAPIKey);
				SetStartButtonText ("running");
				RunBenchmark (runSetId, benchmarkName, hostname, architecture);
			};
			string v = ".NET version:\n" + System.Environment.Version.ToString ();
			v += "\n\nMonoVersion:\n" + GetMonoVersion ();
			v += "\nArchitecture: " + architecture;
			v += "\nHostname: " + hostname;
			FindViewById<TextView> (Resource.Id.versionText).Text = v;
			Logging.GetLogging().Info (v);
			Logging.GetLogging().Info ("OnCreate finished");
		}
	}
}

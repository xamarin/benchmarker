using System;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

using Benchmarker;
using System.Reflection;
using System.Diagnostics;

using Benchmarks.BH;
using Benchmarks.Nbody;
using Benchmarks.Strcat;

using Java.Util.Logging;
using Common.Logging;
using Newtonsoft.Json.Linq;
using Nito.AsyncEx;
using System.Text.RegularExpressions;
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

		private static string GetMonoVersion ()
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

		void Iteration (string benchmark, int iteration, bool isDryRun)
		{
			var dryRun = isDryRun ? " dryrun" : "";
			Logging.GetLogging().InfoFormat ("Benchmarker | Benchmark{0} \"{1}\": start iteration {2}", dryRun, benchmark, iteration);
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
			Logging.GetLogging().InfoFormat ("Benchmarker | Benchmark{0} \"{1}\": finished iteration {2}, took {3}ms", dryRun, benchmark, iteration, sw.ElapsedMilliseconds);
		}

		private static void PrintCommit ()
		{
			// e.g.: "4.3.0 (master/[a-f0-9A-F]{7..40})"
			var regex = new Regex ("^[0-9].*\\((.*)/([0-9a-f]+)\\)");
			var match = regex.Match (GetMonoVersion ());

			string branch, hash;
			if (match.Success) {
				branch = match.Groups [1].Value;
				hash = match.Groups [2].Value;
				Logging.GetLogging().Debug ("branch: " + branch + " hash: " + hash);
			} else {
				branch = "<unknown>";
				hash = "<unknown>";
				Logging.GetLogging().Debug ("couldn't read git information: \"" + GetMonoVersion () + "\"");
			}
			Octokit.Commit gitHubCommit = null;
			try {
				var gitHubClient = GitHubInterface.GitHubClient;
				Octokit.TreeResponse treeResponse = AsyncContext.Run (() => GitHubInterface.RunWithRetry (() => gitHubClient.GitDatabase.Tree.Get ("mono", "mono", hash)));
				gitHubCommit = AsyncContext.Run (() => GitHubInterface.RunWithRetry (() => gitHubClient.GitDatabase.Commit.Get ("mono", "mono", treeResponse.Sha)));
			} catch (Octokit.NotFoundException e) {
				Logging.GetLogging().Debug ("Commit " + hash + " not found on GitHub");
				throw e;
			}
			if (gitHubCommit == null) {
				Logging.GetLogging().Debug ("Could not get commit " + hash + " from GitHub");
			} else {
				hash = gitHubCommit.Sha;
				// commit.CommitDate = gitHubCommit.Committer.Date.DateTime;
				Logging.GetLogging().Info ("Got commit " + hash + " from GitHub");
			}

			Logging.GetLogging().InfoFormat ("Benchmarker | commit \"{0}\" on branch \"{1}\"", hash, branch);
		}

		void RunBenchmark (string runSetId, string benchmarkName, string hostname, string architecture)
		{
			const int TRY_RUNS = 10;
			const int ITERATIONS = 10;

			PrintCommit ();
			Logging.GetLogging().InfoFormat ("Benchmarker | hostname \"{0}\" architecture \"{1}\"", hostname, architecture);
			Logging.GetLogging ().InfoFormat ("Becnhmarker | configname \"{0}\"", "default");
			// TODO: buildURL => wrench log?
			// TODO: logURL => XTC url?
			Logging.GetLogging ().InfoFormat ("Benchmarker | runSetId \"{0}\"", runSetId);
			new Task (() => {
				try {
					for (var i = 0; i < (ITERATIONS + TRY_RUNS); i++) {
						Iteration (benchmarkName, i, i < TRY_RUNS);
					}
					RunOnUiThread (() => SetStartButtonText ("start"));
				} catch (Exception e) {
					RunOnUiThread (() => SetStartButtonText ("failed"));
					Logging.GetLogging().Error (e);
				}
			}).Start ();
			Logging.GetLogging().InfoFormat ("Benchmark started, run set id {0}", runSetId);
		}

		private static void InitCommons(string githubAPIKey) {
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
				var githubAPIKey = FindViewById<TextView> (Resource.Id.githubAPIKey).Text;
				InitCommons (githubAPIKey);
				SetStartButtonText ("running");
				RunBenchmark (runSetId, benchmarkName, hostname, architecture);
			};
			string v = ".NET version:\n" + System.Environment.Version.ToString ();
			v += "\n\nMonoVersion:\n" + GetMonoVersion ();
			v += "\nArchitecture: " + architecture;
			v += "\nHostname: " + hostname;
			FindViewById<TextView> (Resource.Id.versionText).Text = v;
			Logging.GetLogging().Info (v);
			if (IsRooted ()) {
				Logging.GetLogging ().Info ("Ohai: On a rooted device!");
			} else {
				Logging.GetLogging ().Warn ("device not rooted, thus can't set CPU frequency: expect flaky results");
			}
			Logging.GetLogging().Info ("OnCreate finished");
		}

		private Boolean IsRooted() {
			try {
				Java.Lang.Process su = Java.Lang.Runtime.GetRuntime ().Exec ("su");
				Java.IO.DataOutputStream outSu = new Java.IO.DataOutputStream (su.OutputStream);
				outSu.WriteBytes ("exit\n");
				outSu.Flush ();
				su.WaitFor ();
				return su.ExitValue () == 0;
			} catch (Java.Lang.Exception _) {
				return false;
			}
		}

		private string[] AvailableCPUFreuquencies() {
			return null;
		}

		private string[] AvailableCPUGovenors() {
			return null;
		}

		private void SetCPUGovenor() {
		}

		private void SetCPUFrequency() {
			return;
		}
	}
}

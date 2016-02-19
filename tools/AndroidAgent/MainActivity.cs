using System;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

using Benchmarker;
using models = Benchmarker.Models;
using System.Reflection;
using System.Diagnostics;

using Benchmarks.BH;
using Benchmarks.BinaryTrees;
using Benchmarks.BiSort;
using Benchmarks.Euler;
using Benchmarks.Except;
using Benchmarks.GrandeTracer;
using graph4 = Benchmarks.Graph4;
using graph8 = Benchmarks.Graph8;
using Benchmarks.Hash3;
using Benchmarks.Health;
using Benchmarks.Lists;
using Benchmarks.Mandelbrot;
using Benchmarks.Nbody;
using Benchmarks.Objinst;
using Benchmarks.OneList;
using Benchmarks.Perimeter;
using Benchmarks.Raytracer2;
using Benchmarks.Raytracer3;
using Benchmarks.SciMark;
using Benchmarks.SpecRaytracer;
using Benchmarks.Strcat;

using Java.Util.Logging;
using Common.Logging;
using Newtonsoft.Json.Linq;
using Nito.AsyncEx;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Java.IO;

namespace AndroidAgent
{
	[Activity (Label = "AndroidAgent", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Activity
	{
		
		static MainActivity ()
		{
			Logging.SetLogging (new AndroidLogger ());
		}

		private static string GetMonoVersion ()
		{
			Type type = Type.GetType ("Mono.Runtime");
			if (type != null) {
				MethodInfo displayName = type.GetMethod ("GetDisplayName", BindingFlags.Public | BindingFlags.Static);
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

		private models.Run Iteration (string benchmark, int iteration, bool isDryRun)
		{
			var dryRun = isDryRun ? " dryrun" : "";
			Logging.GetLogging ().InfoFormat ("Benchmarker | Benchmark{0} \"{1}\": start iteration {2}", dryRun, benchmark, iteration);
			GC.Collect (1);
			System.Threading.Thread.Sleep (5 * 1000); // cool down?

			var sw = Stopwatch.StartNew ();
			switch (benchmark) {
			case "bh":
				BH.Main (new string[] { "-b", "400", "-s", "200" }, Logging.GetLogging ());
				break;
			case "binarytree":
				BinaryTrees.Main (new string[] { "17" }, Logging.GetLogging ());
				break;
			case "bisort":
				BiSort.Main (new string[] { "-s", "1500000" }, Logging.GetLogging ());
				break;
			case "euler":
				Euler.Main (new string[] { }, Logging.GetLogging ());
				break;
			case "except":
				except.Main (new string[] { "500000" }, Logging.GetLogging ());
				break;
			case "grandetracer":
				RayTracer.Main (new string[] { }, Logging.GetLogging ());
				break;
			case "graph4":
				graph4.Node.Main (Logging.GetLogging ());
				break;
			case "graph8":
				graph8.Node.Main (Logging.GetLogging ());
				break;
			case "hash3":
				Hash3.Main (new string[] { "400000" }, Logging.GetLogging ());
				break;
			case "health":
				Health.Main (new string[] { "-l", "10", "-t", "16" }, Logging.GetLogging ());
				break;
			case "lists":
				Lists.Main (new string[] { "1000" }, Logging.GetLogging ());
				break;
			case "mandelbrot":
				Mandelbrot.Main (new string[] { "1500" }, Logging.GetLogging ());
				break;
			case "n-body":
				NBody.Main (new string[] { "400000" }, Logging.GetLogging ());
				break;
			case "objinst":
				Objinst.Main (new string[] { "4000000" }, Logging.GetLogging ());
				break;
			case "onelist":
				OneList.Main ();
				break;
			case "perimeter":
				Perimeter.Main (new string[] { "-l", "17" }, Logging.GetLogging ());
				break;
			case "raytracer2":
				RayTracer2.Main (new string[] { "120" }, Logging.GetLogging ());
				break;
			case "raytracer3":
				RayTracer3.Main (new string[] { "120" }, Logging.GetLogging ());
				break;
			case "scimark-fft":
				ScimarkEntrypoint.Main (new string[] { "fft" }, Logging.GetLogging ());
				break;
			case "scimark-sor":
				ScimarkEntrypoint.Main (new string[] { "sor" }, Logging.GetLogging ());
				break;
			case "scimark-mc":
				ScimarkEntrypoint.Main (new string[] { "mc" }, Logging.GetLogging ());
				break;
			case "scimark-mm":
				ScimarkEntrypoint.Main (new string[] { "mm" }, Logging.GetLogging ());
				break;
			case "scimark-lu":
				ScimarkEntrypoint.Main (new string[] { "lu" }, Logging.GetLogging ());
				break;
			case "specraytracer":
				MainCL.Main (new string[] { "200", "1250" }, Logging.GetLogging ());
				break;
			case "strcat": 
				strcat.Main (new string[] { "40000000" });
				break;
			default:
				throw new NotImplementedException ();
			}
			sw.Stop ();
			Logging.GetLogging ().InfoFormat ("Benchmarker | Benchmark{0} \"{1}\": finished iteration {2}, took {3}ms", dryRun, benchmark, iteration, sw.ElapsedMilliseconds);
			var run = new models.Run { Benchmark = new models.Benchmark { Name = benchmark } };
			run.RunMetrics.Add (
				new models.RunMetric {
					Metric = models.RunMetric.MetricType.Time,
					Value = TimeSpan.FromMilliseconds (sw.ElapsedMilliseconds)
				}
			);
			return run;
		}

		private static models.Commit DetermineCommit ()
		{
			// e.g.: "4.3.0 (master/[a-f0-9A-F]{7..40})"
			var regex = new Regex ("^[0-9].*\\((.*)/([0-9a-f]+)\\)");
			var match = regex.Match (GetMonoVersion ());

			string branch, hash;
			if (match.Success) {
				branch = match.Groups [1].Value;
				hash = match.Groups [2].Value;
				Logging.GetLogging ().Debug ("branch: " + branch + " hash: " + hash);
			} else {
				branch = "<unknown>";
				hash = "<unknown>";
				Logging.GetLogging ().Debug ("couldn't read git information: \"" + GetMonoVersion () + "\"");
			}
			Octokit.Commit gitHubCommit = null;
			try {
				var gitHubClient = GitHubInterface.GitHubClient;
				Octokit.TreeResponse treeResponse = AsyncContext.Run (() => GitHubInterface.RunWithRetry (() => gitHubClient.GitDatabase.Tree.Get ("mono", "mono", hash)));
				gitHubCommit = AsyncContext.Run (() => GitHubInterface.RunWithRetry (() => gitHubClient.GitDatabase.Commit.Get ("mono", "mono", treeResponse.Sha)));
			} catch (Octokit.NotFoundException e) {
				Logging.GetLogging ().Debug ("Commit " + hash + " not found on GitHub");
				throw e;
			}
			if (gitHubCommit == null) {
				Logging.GetLogging ().Debug ("Could not get commit " + hash + " from GitHub");
			} else {
				hash = gitHubCommit.Sha;
				// commit.CommitDate = gitHubCommit.Committer.Date.DateTime;
				Logging.GetLogging ().Info ("Got commit " + hash + " from GitHub");
			}

			Logging.GetLogging ().InfoFormat ("Benchmarker | commit \"{0}\" on branch \"{1}\"", hash, branch);
			return new models.Commit {
				Hash = hash,
				Branch = branch,
				Product = new models.Product {
					Name = "mono",
					GitHubUser = "mono",
					GitHubRepo = "mono"
				}
			};
		}


		AndroidCPUManagment CpuManager;

		void RunBenchmark (long runSetId, string benchmarkName, string hostname, string architecture)
		{
			const int DRY_RUNS = 3;
			const int ITERATIONS = 10;


			Logging.GetLogging ().InfoFormat ("Benchmarker | hostname \"{0}\" architecture \"{1}\"", hostname, architecture);
			Logging.GetLogging ().InfoFormat ("Benchmarker | configname \"{0}\"", "default");

			models.Commit mainCommit = DetermineCommit ();
			models.Machine machine = new models.Machine { Name = hostname, Architecture = architecture };
			models.Config config = new models.Config { Name = "default", Mono = String.Empty,		
				MonoOptions = new string[0],		
				MonoEnvironmentVariables = new Dictionary<string, string> (),		
				Count = ITERATIONS
			};
			models.RunSet runSet = AsyncContext.Run (() => models.RunSet.FromId (machine, runSetId, config, mainCommit, null, null, null /* TODO: logURL? */));

			if (runSet == null) {
				Logging.GetLogging ().Warn ("RunSetID " + runSetId + " not found");
				return;
			}
			new Task (() => {
				try {
					for (var i = 0; i < (ITERATIONS + DRY_RUNS); i++) {
						var run = Iteration (benchmarkName, i, i < DRY_RUNS);
						if (i >= DRY_RUNS) {
							runSet.Runs.Add (run);
						}
					}
					AsyncContext.Run (() => runSet.Upload ());
					RunOnUiThread (() => SetStartButtonText ("start"));
				} catch (Exception e) {
					RunOnUiThread (() => SetStartButtonText ("failed"));
					Logging.GetLogging ().Error (e);
				} finally {
					if (AndroidCPUManagment.IsRooted ()) {
						CpuManager.RestoreCPUStates ();
					}
				}
			}).Start ();
		}

		private static void InitCommons (string githubAPIKey, string httpAPITokens)
		{
			GitHubInterface.githubCredentials = githubAPIKey;
			models.HttpApi.AuthToken = httpAPITokens;
		}

		protected override void OnCreate (Bundle savedInstanceState)
		{
			string architecture = Android.OS.Build.CpuAbi;
			string hostname = Android.OS.Build.Model + "_" + Android.OS.Build.VERSION.Release;
			base.OnCreate (savedInstanceState);
			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);
			FindViewById<Button> (Resource.Id.myButton).Click += delegate {
				string benchmarkName = FindViewById<TextView> (Resource.Id.benchmark).Text;
				string githubAPIKey = FindViewById<TextView> (Resource.Id.githubAPIKey).Text;
				string httpAPITokens = FindViewById<TextView> (Resource.Id.httpAPITokens).Text;
				long runSetId = Int64.Parse (FindViewById<TextView> (Resource.Id.runSetId).Text);
				InitCommons (githubAPIKey, httpAPITokens);
				SetStartButtonText ("running");
				RunBenchmark (runSetId, benchmarkName, hostname, architecture);
			};
			string v = ".NET version:\n" + System.Environment.Version.ToString ();
			v += "\n\nMonoVersion:\n" + GetMonoVersion ();
			v += "\nArchitecture: " + architecture;
			v += "\nHostname: " + hostname;
			FindViewById<TextView> (Resource.Id.versionText).Text = v;
			Logging.GetLogging ().Info (v);

			if (AndroidCPUManagment.IsRooted ()) {
				Logging.GetLogging ().Info ("Ohai: On a rooted device!");
				CpuManager = new AndroidCPUManagment ();
				CpuManager.ConfigurePerformanceMode ();
			} else {
				Logging.GetLogging ().Warn ("device not rooted, thus can't set CPU frequency: expect flaky results");
			}
			Logging.GetLogging ().Info ("OnCreate finished");
		}
	}
}

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
using Java.Util.Logging;
using Common.Logging;
using Newtonsoft.Json.Linq;
using Benchmarker.Common.Models;
using Nito.AsyncEx;
using System.Text.RegularExpressions;
using Parse;

namespace AndroidAgent
{
	[Activity (Label = "AndroidAgent", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Activity
	{
		string GetMonoVersion () {
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
			Console.WriteLine ("MainActivity | Benchmark {0}: start iteration {1}", benchmark, iteration);
			var sw = Stopwatch.StartNew ();
			switch (benchmark) {
			case "strcat": 
				strcat.Main (new string[] { "10000000" });
				break;
			default:
				throw new NotImplementedException ();
			}
			sw.Stop ();
			Console.WriteLine ("MainActivity | Benchmark {0}: finished iteration {1}", benchmark, iteration);
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
				Console.WriteLine ("branch: " + commit.Branch + " hash: " + commit.Hash);
			} else {
				commit.Branch = "<unknown>";
				commit.Hash = "<unknown>";
				Console.WriteLine ("couldn't read git information: \"" + GetMonoVersion () + "\"");
			}
			Octokit.Commit gitHubCommit = null;
			try {
				var gitHubClient = GitHubInterface.GitHubClient;
				gitHubCommit = AsyncContext.Run (() => GitHubInterface.RunWithRetry (() => gitHubClient.GitDatabase.Commit.Get ("mono", "mono", commit.Hash)));
			} catch (Octokit.NotFoundException e) {
				Console.WriteLine ("Commit " + commit.Hash + " not found on GitHub");
				Console.WriteLine (e.StackTrace);
			}
			if (gitHubCommit == null) {
				Console.WriteLine ("Could not get commit " + commit.Hash + " from GitHub");
			} else {
				commit.Hash = gitHubCommit.Sha;
				commit.CommitDate = gitHubCommit.Committer.Date.DateTime;
				Console.WriteLine ("Got commit " + commit.Hash + " from GitHub");
			}

			return commit;
		}

		void RunBenchmark (string runSetId, string hostname, string architecture)
		{
			const string benchmark = "strcat";
			var commit = GetCommit ();
			var config = new Config { Name = "auto-sgen", Count = 10, };
			var runSet = new RunSet {
				StartDateTime = DateTime.Now,
				Config = config,
				Commit = commit,
				BuildURL = null, // TODO: wrench url?
				LogURL = null, // TODO: XTC url?
			};
			new Task (() => {
				var result = new Benchmarker.Common.Models.Result {
					DateTime = DateTime.Now,
					Benchmark = new Benchmark { Name = benchmark, },
					Config = config,
				};
				try {
					for (var i = 0; i < config.Count + 1; i++) {
						var run = Iteration (benchmark, i);
						if (i == 0) {
							continue;
						}
						if (run != null) {
							result.Runs.Add (run);
						}		
					}

					var objectId = runSet.UploadToParseGetObjectId (hostname, architecture);
					Console.WriteLine ("http://xamarin.github.io/benchmarker/front-end/runset.html#{0}", objectId);
					Console.Write ("{{ \"runSetId\": \"{0}\"", objectId);
					RunOnUiThread (() => SetStartButtonText ("start"));
					Console.WriteLine (" }");
				} catch (Exception e) {
					Console.WriteLine (e);
				}
			}).Start ();
			Console.WriteLine ("Benchmark started, run set id {0}", runSetId);
		}

		private static void InitCommons() {
			Logging.SetLogging (new AndroidLogger());
			ParseInterface.benchmarkerCredentials = JObject.Parse ("{'username': 'nope', 'password': 'never'}");
			if (!ParseInterface.Initialize ()) {
				Console.Error.WriteLine ("Error: Could not initialize Parse interface.");
				throw new Exception ("Error: Could not initialize Parse interface.");
			} else {
				Console.WriteLine ("InitCommons: Parse successful");
			}
			GitHubInterface.githubCredentials = "magichash";
		}

		protected override void OnCreate (Bundle savedInstanceState)
		{
			string architecture = Android.OS.Build.CpuAbi;
			string hostname = Android.OS.Build.Model + "_" + Android.OS.Build.VERSION.Release;
			base.OnCreate (savedInstanceState);
			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);
			FindViewById<Button> (Resource.Id.myButton).Click += delegate {
				TextView textView = FindViewById<TextView> (Resource.Id.runSetId);
				var runSetId = textView.Text;
				SetStartButtonText ("running");
				RunBenchmark (runSetId, hostname, architecture);
			};
			string v = ".NET version:\n" + System.Environment.Version.ToString ();
			v += "\n\nMonoVersion:\n" + GetMonoVersion ();
			v += "\nArchitecture: " + architecture;
			v += "\nHostname: " + hostname;
			FindViewById<TextView> (Resource.Id.versionText).Text = v;
			Console.WriteLine (v);
			InitCommons ();
			Console.WriteLine ("OnCreate finished");
		}
	}

	class AndroidLogger : ILog {
		#region ILog implementation
		public void Trace (object message)
		{
			throw new NotImplementedException ();
		}
		public void Trace (object message, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public void TraceFormat (string format, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void TraceFormat (string format, Exception exception, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void TraceFormat (IFormatProvider formatProvider, string format, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void TraceFormat (IFormatProvider formatProvider, string format, Exception exception, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void Trace (Action<FormatMessageHandler> formatMessageCallback)
		{
			throw new NotImplementedException ();
		}
		public void Trace (Action<FormatMessageHandler> formatMessageCallback, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public void Trace (IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback)
		{
			throw new NotImplementedException ();
		}
		public void Trace (IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public void Debug (object message)
		{
			Console.WriteLine (message);
		}
		public void Debug (object message, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public void DebugFormat (string format, params object[] args)
		{
			switch (args.Length) {
			case 0:
				Console.WriteLine (format);
				break;
			case 1:
				Console.WriteLine (format, args [0]);
				break;
			case 2:
				Console.WriteLine (format, args [0], args [1]);
				break;
			case 3:
				Console.WriteLine (format, args [0], args [1], args [2]);
				break;
			default:
				throw new NotImplementedException ();
			}
		}
		public void DebugFormat (string format, Exception exception, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void DebugFormat (IFormatProvider formatProvider, string format, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void DebugFormat (IFormatProvider formatProvider, string format, Exception exception, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void Debug (Action<FormatMessageHandler> formatMessageCallback)
		{
			throw new NotImplementedException ();
		}
		public void Debug (Action<FormatMessageHandler> formatMessageCallback, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public void Debug (IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback)
		{
			throw new NotImplementedException ();
		}
		public void Debug (IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public void Info (object message)
		{
			Console.WriteLine (message);
		}
		public void Info (object message, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public void InfoFormat (string format, params object[] args)
		{
			switch (args.Length) {
			case 0:
				Console.WriteLine (format);
				break;
			case 1:
				Console.WriteLine (format, args [0]);
				break;
			case 2:
				Console.WriteLine (format, args [0], args [1]);
				break;
			case 3:
				Console.WriteLine (format, args [0], args [1], args [2]);
				break;
			default:
				throw new NotImplementedException ();
			}
		}
		public void InfoFormat (string format, Exception exception, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void InfoFormat (IFormatProvider formatProvider, string format, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void InfoFormat (IFormatProvider formatProvider, string format, Exception exception, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void Info (Action<FormatMessageHandler> formatMessageCallback)
		{
			throw new NotImplementedException ();
		}
		public void Info (Action<FormatMessageHandler> formatMessageCallback, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public void Info (IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback)
		{
			throw new NotImplementedException ();
		}
		public void Info (IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public void Warn (object message)
		{
			Console.WriteLine (message);
		}
		public void Warn (object message, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public void WarnFormat (string format, params object[] args)
		{
			switch (args.Length) {
			case 0:
				Console.WriteLine (format);
				break;
			case 1:
				Console.WriteLine (format, args [0]);
				break;
			case 2:
				Console.WriteLine (format, args [0], args [1]);
				break;
			case 3:
				Console.WriteLine (format, args [0], args [1], args [2]);
				break;
			default:
				throw new NotImplementedException ();
			}
		}
		public void WarnFormat (string format, Exception exception, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void WarnFormat (IFormatProvider formatProvider, string format, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void WarnFormat (IFormatProvider formatProvider, string format, Exception exception, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void Warn (Action<FormatMessageHandler> formatMessageCallback)
		{
			throw new NotImplementedException ();
		}
		public void Warn (Action<FormatMessageHandler> formatMessageCallback, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public void Warn (IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback)
		{
			throw new NotImplementedException ();
		}
		public void Warn (IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public void Error (object message)
		{
			Console.WriteLine (message);
		}
		public void Error (object message, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public void ErrorFormat (string format, params object[] args)
		{
			switch (args.Length) {
			case 0:
				Console.WriteLine (format);
				break;
			case 1:
				Console.WriteLine (format, args [0]);
				break;
			case 2:
				Console.WriteLine (format, args [0], args [1]);
				break;
			case 3:
				Console.WriteLine (format, args [0], args [1], args [2]);
				break;
			default:
				throw new NotImplementedException ();
			}
		}
		public void ErrorFormat (string format, Exception exception, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void ErrorFormat (IFormatProvider formatProvider, string format, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void ErrorFormat (IFormatProvider formatProvider, string format, Exception exception, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void Error (Action<FormatMessageHandler> formatMessageCallback)
		{
			throw new NotImplementedException ();
		}
		public void Error (Action<FormatMessageHandler> formatMessageCallback, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public void Error (IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback)
		{
			throw new NotImplementedException ();
		}
		public void Error (IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public void Fatal (object message)
		{
			throw new NotImplementedException ();
		}
		public void Fatal (object message, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public void FatalFormat (string format, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void FatalFormat (string format, Exception exception, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void FatalFormat (IFormatProvider formatProvider, string format, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void FatalFormat (IFormatProvider formatProvider, string format, Exception exception, params object[] args)
		{
			throw new NotImplementedException ();
		}
		public void Fatal (Action<FormatMessageHandler> formatMessageCallback)
		{
			throw new NotImplementedException ();
		}
		public void Fatal (Action<FormatMessageHandler> formatMessageCallback, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public void Fatal (IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback)
		{
			throw new NotImplementedException ();
		}
		public void Fatal (IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback, Exception exception)
		{
			throw new NotImplementedException ();
		}
		public bool IsTraceEnabled {
			get {
				throw new NotImplementedException ();
			}
		}
		public bool IsDebugEnabled {
			get {
				throw new NotImplementedException ();
			}
		}
		public bool IsErrorEnabled {
			get {
				throw new NotImplementedException ();
			}
		}
		public bool IsFatalEnabled {
			get {
				throw new NotImplementedException ();
			}
		}
		public bool IsInfoEnabled {
			get {
				throw new NotImplementedException ();
			}
		}
		public bool IsWarnEnabled {
			get {
				throw new NotImplementedException ();
			}
		}
		public IVariablesContext GlobalVariablesContext {
			get {
				throw new NotImplementedException ();
			}
		}
		public IVariablesContext ThreadVariablesContext {
			get {
				throw new NotImplementedException ();
			}
		}
		#endregion
	}
}

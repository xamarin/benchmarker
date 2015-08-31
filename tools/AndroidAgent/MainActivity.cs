using System;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using System.Reflection;

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

		void RunBenchmark (string runSetId)
		{
			new Task (() => {
				try {
					Console.WriteLine ("MainActivity | Benchmark : start");
					strcat.Main (new string[] { "10000000" });
					Console.WriteLine ("MainActivity | Benchmark : finished");

					RunOnUiThread (() => {
						SetStartButtonText ("start");
					});
				} catch (Exception e) {
					Console.WriteLine (e);
				}
			}).Start ();
			Console.WriteLine ("Benchmark started, run set id {0}", runSetId);
		}

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);
			FindViewById<Button> (Resource.Id.myButton).Click += delegate {
				TextView textView = FindViewById<TextView> (Resource.Id.runSetId);
				var runSetId = textView.Text;
				SetStartButtonText ("running");
				RunBenchmark (runSetId);
			};
			string v = ".NET version:\n" + System.Environment.Version.ToString ();
			v += "\n\nMonoVersion:\n" + GetMonoVersion ();
			FindViewById<TextView> (Resource.Id.versionText).Text = v;
			Console.WriteLine (".NET version: {0}", System.Environment.Version.ToString ());
			Console.WriteLine ("MonoVersion: {0}", GetMonoVersion ());
			Console.WriteLine ("OnCreate finished");
		}
	}
}

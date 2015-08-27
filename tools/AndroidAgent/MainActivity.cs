using System;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace AndroidAgent
{
	[Activity (Label = "AndroidAgent", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Activity
	{
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

			Console.WriteLine ("OnCreate finished");
		}
	}
}

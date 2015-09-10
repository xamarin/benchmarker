using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Xamarin.UITest;
using Xamarin.UITest.Android;
using Xamarin.UITest.Queries;
using System.Reflection;
using Newtonsoft.Json;

namespace AndroidAgent.UITests
{
	[TestFixture]
	public class Tests
	{
		AndroidApp app;

		[SetUp]
		public void BeforeEachTest ()
		{
			app = ConfigureApp.Android.StartApp ();
		}

		[Test]
		public void RunBenchmark ()
		{
			var assembly = Assembly.GetExecutingAssembly ();
			using (Stream stream = assembly.GetManifestResourceStream ("AndroidAgent.UITests.params.json")) {
				using (StreamReader reader = new StreamReader (stream)) {
					dynamic json = JsonConvert.DeserializeObject (reader.ReadToEnd ());
					string runSetId = json.runSetId;
					string bmUsername = json.bmUsername;
					string bmPassword = json.bmPassword;
					string githubAPIKey = json.githubAPIKey;

					app.Screenshot ("init");

					app.ClearText (c => c.Marked ("runSetId"));
					app.EnterText (c => c.Marked ("runSetId"), runSetId);
					app.Screenshot ("enter RunSetId");

					app.ClearText (c => c.Marked ("bmUsername"));
					app.EnterText (c => c.Marked ("bmUsername"), bmUsername);
					app.Screenshot ("enter bmUsername");

					app.ClearText (c => c.Marked ("bmPassword"));
					app.EnterText (c => c.Marked ("bmPassword"), bmPassword);
					app.Screenshot ("enter bmPassword");

					app.ClearText (c => c.Marked ("githubAPIKey"));
					app.ClearText (c => c.Marked ("githubAPIKey"));
					app.EnterText (c => c.Marked ("githubAPIKey"), githubAPIKey);
					app.Screenshot ("enter githubAPIKey");

					app.Tap (c => c.Marked("myButton"));
					app.Screenshot ("after tap");
					app.WaitForNoElement (c => c.Marked ("myButton").Text ("running"), "Benchmark is taking too long", TimeSpan.FromMinutes(179));
					app.Screenshot ("after benchmark");
				}
			}
		}
	}
}

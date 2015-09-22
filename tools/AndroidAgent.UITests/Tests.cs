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

		private void clearAndSetTextField (string id, string value)
		{
			app.ClearText (c => c.Marked (id));
			app.ClearText (c => c.Marked (id));
			app.EnterText (c => c.Marked (id), value);
			app.Screenshot ("enter " + id);
		}

		[Test]
		public void RunBenchmarkBh ()
		{
			RunBenchmarkHelper ("bh");
		}

		[Test]
		public void RunBenchmarkNbody ()
		{
			RunBenchmarkHelper ("n-body");
		}

		[Test]
		public void RunBenchmarkStrcat ()
		{
			RunBenchmarkHelper ("strcat");
		}

		public void RunBenchmarkHelper (string benchmark)
		{
			var assembly = Assembly.GetExecutingAssembly ();
			using (Stream stream = assembly.GetManifestResourceStream ("AndroidAgent.UITests.params.json")) {
				using (StreamReader reader = new StreamReader (stream)) {
					dynamic json = JsonConvert.DeserializeObject (reader.ReadToEnd ());
					string runSetId = json.runSetId;
					string githubAPIKey = json.githubAPIKey;

					app.Screenshot ("init");

					clearAndSetTextField ("runSetId", runSetId);
					clearAndSetTextField ("benchmark", benchmark);
					clearAndSetTextField ("githubAPIKey", githubAPIKey);

					app.Tap (c => c.Marked ("myButton"));
					app.Screenshot ("after tap");
					app.WaitForNoElement (c => c.Marked ("myButton").Text ("running"), "Benchmark is taking too long", TimeSpan.FromMinutes (179));
					Assert.AreEqual (app.Query (c => c.Marked ("myButton")).First ().Text, "start");
					app.Screenshot ("after benchmark");
				}
			}
		}
	}
}

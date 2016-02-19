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
			// wait for init, dropping root commands can take a while (TODO: better idea?)
			System.Threading.Thread.Sleep (10000);
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
		public void RunBenchmarkBinarytree ()
		{
			RunBenchmarkHelper ("binarytree");
		}

		[Test]
		public void RunBenchmarkBisort ()
		{
			RunBenchmarkHelper ("bisort");
		}

		[Test]
		public void RunBenchmarkEuler ()
		{
			RunBenchmarkHelper ("euler");
		}

		[Test]
		public void RunBenchmarkExcept ()
		{
			RunBenchmarkHelper ("except");
		}

		[Test]
		public void RunBenchmarkGrandeTracer ()
		{
			RunBenchmarkHelper ("grandetracer");
		}

		[Test]
		public void RunBenchmarkGraph4 ()
		{
			RunBenchmarkHelper ("graph4");
		}

		[Test]
		public void RunBenchmarkGraph8 ()
		{
			RunBenchmarkHelper ("graph8");
		}

		[Test]
		public void RunBenchmarkHash3 ()
		{
			RunBenchmarkHelper ("hash3");
		}

		[Test]
		public void RunBenchmarkHealth ()
		{
			RunBenchmarkHelper ("health");
		}

		[Test]
		public void RunBenchmarkLists ()
		{
			RunBenchmarkHelper ("lists");
		}

		[Test]
		public void RunBenchmarkMandelbrot ()
		{
			RunBenchmarkHelper ("mandelbrot");
		}

		[Test]
		public void RunBenchmarkNbody ()
		{
			RunBenchmarkHelper ("n-body");
		}

		[Test]
		public void RunBenchmarkObjinst ()
		{
			RunBenchmarkHelper ("objinst");
		}

		[Test]
		public void RunBenchmarkOnelist ()
		{
			RunBenchmarkHelper ("onelist");
		}

		[Test]
		public void RunBenchmarkPerimeter ()
		{
			RunBenchmarkHelper ("perimeter");
		}

		[Test]
		public void RunBenchmarkRaytracer2 ()
		{
			RunBenchmarkHelper ("raytracer2");
		}

		[Test]
		public void RunBenchmarkRaytracer3 ()
		{
			RunBenchmarkHelper ("raytracer3");
		}

		[Test]
		public void RunBenchmarkScimarkFFT ()
		{
			RunBenchmarkHelper ("scimark-fft");
		}

		[Test]
		public void RunBenchmarkScimarkSOR ()
		{
			RunBenchmarkHelper ("scimark-sor");
		}

		[Test]
		public void RunBenchmarkScimarkMC ()
		{
			RunBenchmarkHelper ("scimark-mc");
		}

		[Test]
		public void RunBenchmarkScimarkMM ()
		{
			RunBenchmarkHelper ("scimark-mm");
		}

		[Test]
		public void RunBenchmarkScimarkLU ()
		{
			RunBenchmarkHelper ("scimark-lu");
		}

		[Test]
		public void RunBenchmarkSpecRaytracer ()
		{
			RunBenchmarkHelper ("specraytracer");
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
					string githubAPIKey = json.githubAPIKey;
					string httpAPITokens = json.httpAPITokens;
					string runSetId = json.runSetId;

					app.Screenshot ("init");

					clearAndSetTextField ("benchmark", benchmark);
					clearAndSetTextField ("githubAPIKey", githubAPIKey);
					clearAndSetTextField ("httpAPITokens", httpAPITokens);
					clearAndSetTextField ("runSetId", runSetId);

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

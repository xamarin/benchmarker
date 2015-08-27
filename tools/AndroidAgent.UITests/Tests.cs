using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Xamarin.UITest;
using Xamarin.UITest.Android;
using Xamarin.UITest.Queries;

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
		public void ClickingButtonTwiceShouldChangeItsLabel ()
		{
			app.Screenshot ("init");
			app.EnterText (c => c.Marked ("runSetId"), "123435");
			app.Screenshot ("enter RunSetId");
			app.Tap (c => c.Marked("myButton"));
			app.WaitForNoElement (c => c.Marked ("myButton").Text ("running"), "Benchmark is taking too long", TimeSpan.FromMinutes(179));
			app.Screenshot ("after benchmark");
		}
	}
}


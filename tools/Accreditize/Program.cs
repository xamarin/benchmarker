using System;
using Common.Logging;
using Common.Logging.Simple;
using Benchmarker;
using Newtonsoft.Json;

namespace Accreditize
{
	class MainClass
	{
		public static int Main (string[] args)
		{
			if (args.Length != 1) {
				Console.Error.WriteLine ("Usage: Accreditize SERVICE");
				return 1;
			}

			var service = args [0];

			LogManager.Adapter = new NoOpLoggerFactoryAdapter ();
			Logging.SetLogging (LogManager.GetLogger<MainClass> ());

			var credentials = Accredit.GetCredentials (service);
			Console.WriteLine ("{0}", JsonConvert.SerializeObject (credentials));

			return 0;
		}
	}
}

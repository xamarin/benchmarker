using Benchmarker;
using System;
using Common.Logging.Simple;
using Common.Logging;

namespace xtclog
{
	class MainClass
	{
		static void UsageAndExit (bool success)
		{
			Console.WriteLine ("Usage:");
			Console.WriteLine ("    xtcloghelper.exe --push XTCJOBID");
			Console.WriteLine ("                     --crawl-logs");
			Environment.Exit (success ? 0 : 1);
		}

		public static void Main (string[] args)
		{
			LogManager.Adapter = new ConsoleOutLoggerFactoryAdapter();
			Logging.SetLogging (LogManager.GetLogger<MainClass> ());

			if (args.Length == 0)
				UsageAndExit (true);

			if (args [0] == "--push") {
				
			} else if (args [0] == "--crawl-logs") {
				Console.WriteLine ("NIY");
				Environment.Exit (3);
			} else {
				UsageAndExit (false);
			}
		}
	}
}

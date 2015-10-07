using Benchmarker;
using System;
using Common.Logging.Simple;
using Common.Logging;
using Npgsql;

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
				if (args.Length <= 1) {
					UsageAndExit (false);
				}
				string xtcJobId = args [1];
				var connection = PostgresInterface.Connect ();
				PushXTCJobId (connection, xtcJobId);
			} else if (args [0] == "--crawl-logs") {
				Console.WriteLine ("NIY");
				Environment.Exit (3);
			} else {
				UsageAndExit (false);
			}
		}

		private static void PushXTCJobId(NpgsqlConnection conn, string xtcJobId) {
			PostgresRow row = new PostgresRow ();
			row.Set ("job", NpgsqlTypes.NpgsqlDbType.Varchar, xtcJobId);
			PostgresInterface.Insert<long> (conn, "XamarinTestcloudJobIDs", row, "id");
		}
	}
}

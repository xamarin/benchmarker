using System;
using System.Diagnostics;
using Common.Logging;

namespace Benchmarker
{
	public class Logging
	{
		private static ILog log;
		public static void SetLogging(ILog ilog)
		{
			log = ilog;
		}

		public static ILog GetLogging()
		{
			Debug.Assert (log != null, "must initialize Logging");
			return log;
		}
	}
}


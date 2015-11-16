using System;
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
			if (log == null)
				throw new Exception ("must initialize Logging");
			return log;
		}
	}
}


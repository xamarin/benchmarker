// $Id: except.csharp,v 1.4 2005-02-22 19:05:07 igouy-guest Exp $
// http://shootout.alioth.debian.org
// contributed by Erik Saltwell
// Some clean-ups based on suggestions by Isaac Gouy

using System;
using Common.Logging;

namespace Benchmarks.Except
{
	class LoException : System.Exception
	{
		public LoException ()
		{
		}
	}

	class HiException : System.Exception
	{
		public HiException ()
		{
		}
	}

	public class except
	{
		static int Lo = 0;
		static int Hi = 0;
		static int count = 0;
		static ILog logger = null;

		public static void Main (string[] args, ILog ilog)
		{
			int n = int.Parse (args [0]);
			for (count = 0; count < n; count++) {
				SomeFunction ();
			}
			logger = ilog;
			logger.InfoFormat ("Exceptions: HI={0} / LO={1}", Hi, Lo);
		}

		public static void SomeFunction ()
		{
			try {
				HiFunction ();
			} catch (Exception e) {
				logger.InfoFormat ("We shouldn't get here: {0}", e.Message);
			}
		}

		public static void HiFunction ()
		{
			try {
				LoFunction ();
			} catch (HiException) {
				Hi++;
			}
		}

		public static void LoFunction ()
		{
			try {
				BlowUp ();
			} catch (LoException) {
				Lo++;
			}
		}

		public static void BlowUp ()
		{
			if ((count & 1) == 0) {
				throw new LoException ();
			} else {
				throw new HiException ();
			}
		}
	}
}

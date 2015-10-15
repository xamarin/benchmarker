using System;
using Common.Logging;

namespace Benchmarks.OneList
{
	public class OneList
	{
		OneList next;

		static OneList MakeList (int length)
		{
			OneList rest = null;
			for (int i = 0; i < length; ++i) {
				OneList first = new OneList ();
				first.next = rest;
				rest = first;
			}
			return rest;
		}

		public static void Main ()
		{
			MakeList (10000000);
		}
	}
}

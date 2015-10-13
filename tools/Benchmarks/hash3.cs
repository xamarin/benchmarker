using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Common.Logging;

namespace Benchmarks.Hash3
{
	public class Hash3
	{
		public static void Main (string[] args, ILog logger)
		{
			int n = 1;
			int count = 0;
			Random rnd = new Random (42);
			Stopwatch st = new Stopwatch ();
			if (args.Length > 0)
				n = Int32.Parse (args [0]);

			int[] v = new int [n];
			for (int i = 0; i < n; i++)
				v [i] = i;

			/* Shuffle */
			for (int i = n - 1; i > 0; i--) {
				int t, pos;
				pos = rnd.Next () % i;
				t = v [i];
				v [i] = v [pos];
				v [pos] = t;
			}

			Dictionary<int, int> table = new Dictionary<int, int> ();

			st.Start ();
			for (int i = 0; i < n; i++)
				table.Add (v [i], v [i]);
			for (int i = 0; i < n; i++)
				table.Remove (v [i]);
			for (int i = n - 1; i >= 0; i--)
				table.Add (v [i], v [i]);
			st.Stop ();  
			logger.InfoFormat ("Addition {0}", st.ElapsedMilliseconds);

			st.Reset ();
			st.Start ();
			for (int j = 0; j < 8; j++) {
				for (int i = 0; i < n; i++) {
					if (table.ContainsKey (v [i]))
						count++;
				}	
			}
			st.Stop ();
			logger.InfoFormat ("Lookup {0} - Count {1}", st.ElapsedMilliseconds, count);
		}
	}
}

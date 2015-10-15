/// <license>
/// This is a port of the SciMark2a Java Benchmark to C# by
/// Chris Re (cmr28@cornell.edu) and Werner Vogels (vogels@cs.cornell.edu)
/// 
/// For details on the original authors see http://math.nist.gov/scimark2
/// 
/// This software is likely to burn your processor, bitflip your memory chips
/// anihilate your screen and corrupt all your disks, so you it at your
/// own risk.
/// </license>

using System;

namespace Benchmarks.SciMark
{
	/// <summary>
	/// Provides a stopwatch to measure elapsed time.
	/// </summary>
	/// <author> 
	/// Roldan Pozo
	/// </author>
	/// <version> 
	/// 14 October 1997, revised 1999-04-24
	/// </version>
	/// 
	class Stopwatch
	{
		private bool running;
		private double last_time;
		private double total;

		/// 
		/// <summary>R
		/// eturn system time (in seconds)
		/// </summary>
		public static double seconds ()
		{
			return (System.DateTime.Now.Ticks * 1.0E-7);
		}

		public virtual void  reset ()
		{
			running = false;
			last_time = 0.0;
			total = 0.0;
		}

		public Stopwatch ()
		{
			reset ();
		}

		/// 
		/// <summary>
		/// Start (and reset) timer
		/// </summary>
		public virtual void  start ()
		{
			if (!running) {
				running = true;
				total = 0.0;
				last_time = seconds ();
			}
		}

		/// 
		/// <summary>
		/// Resume timing, after stopping.  (Does not wipe out accumulated times.)
		/// </summary>
		public virtual void  resume ()
		{
			if (!running) {
				last_time = seconds ();
				running = true;
			}
		}

		/// 
		/// <summary>
		/// Stop timer
		/// </summary>
		public virtual double stop ()
		{
			if (running) {
				total += seconds () - last_time;
				running = false;
			}
			return total;
		}

		/// 
		/// <summary>
		/// return the elapsed time (in seconds)
		/// </summary>
		public virtual double read ()
		{
			if (running) {
				total += seconds () - last_time;
				last_time = seconds ();
			}
			return total;
		}
	}
}
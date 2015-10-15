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
using System.Runtime.CompilerServices;

namespace Benchmarks.SciMark
{
	/* Random.java based on Java Numerical Toolkit (JNT) Random.UniformSequence
	class.  We do not use Java's own java.util.Random so that we can compare
	results with equivalent C and Fortran coces.*/
	
	public class Random
	{
		/*------------------------------------------------------------------------------
		CLASS VARIABLES
		------------------------------------------------------------------------------ */
		
		internal int seed = 0;
		
		private int[] m;
		private int i = 4;
		private int j = 16;
		
		private const int mdig = 32;
		private const int one = 1;
		private int m1;
		private int m2;
		
		private double dm1;
		
		private bool haveRange = false;
		private double left = 0.0;
		private double right = 1.0;
		private double width = 1.0;
		
		
		/* ------------------------------------------------------------------------------
		CONSTRUCTORS
		------------------------------------------------------------------------------ */
		
		/// <summary>
		/// Initializes a sequence of uniformly distributed quasi random numbers with a
		/// seed based on the system clock.
		/// </summary>
		public Random ()
		{
			initialize ((int)System.DateTime.Now.Ticks);
		}

		/// <summary>
		/// Initializes a sequence of uniformly distributed quasi random numbers on a
		/// given half-open interval [left,right) with a seed based on the system
		/// clock.
		/// </summary>
		/// <param name="<B>left</B>">(double)<BR>
		/// The left endpoint of the half-open interval [left,right).
		/// </param>
		/// <param name="<B>right</B>">(double)<BR>
		/// The right endpoint of the half-open interval [left,right).
		/// </param>
		public Random (double left, double right)
		{
			initialize ((int)System.DateTime.Now.Ticks);
			this.left = left;
			this.right = right;
			width = right - left;
			haveRange = true;
		}

		/// <summary>
		/// Initializes a sequence of uniformly distributed quasi random numbers with a
		/// given seed.
		/// </summary>
		/// <param name="<B>seed</B>">(int)<BR>
		/// The seed of the random number generator.  Two sequences with the same
		/// seed will be identical.
		/// </param>
		public Random (int seed)
		{
			initialize (seed);
		}

		/// <summary>Initializes a sequence of uniformly distributed quasi random numbers
		/// with a given seed on a given half-open interval [left,right).
		/// </summary>
		/// <param name="<B>seed</B>">(int)<BR>
		/// The seed of the random number generator.  Two sequences with the same
		/// seed will be identical.
		/// </param>
		/// <param name="<B>left</B>">(double)<BR>
		/// The left endpoint of the half-open interval [left,right).
		/// </param>
		/// <param name="<B>right</B>">(double)<BR>
		/// The right endpoint of the half-open interval [left,right).
		/// </param>
		public Random (int seed, double left, double right)
		{
			initialize (seed);
			this.left = left;
			this.right = right;
			width = right - left;
			haveRange = true;
		}
		
		/* ------------------------------------------------------------------------------
		PUBLIC METHODS
		------------------------------------------------------------------------------ */
		
		/// <summary>
		/// Returns the next random number in the sequence.
		/// </summary>
		public double nextDouble ()
		{
			lock (this) {
				int k;
		
				k = m [i] - m [j];
				if (k < 0)
					k += m1;
				m [j] = k;
		
				if (i == 0)
					i = 16;
				else
					i--;
		
				if (j == 0)
					j = 16;
				else
					j--;
		
				if (haveRange)
					return left + dm1 * (double)k * width;
				else
					return dm1 * (double)k;
			}
		}

		/// <summary>
		/// Returns the next N random numbers in the sequence, as
		/// a vector.
		/// </summary>
		public void  nextDoubles (double[] x)
		{
			lock (this) {
				int N = x.Length;
				int remainder = N & 3; 
		
				if (haveRange) {
					for (int count = 0; count < N; count++) {
						int k = m [i] - m [j];
				
						if (i == 0)
							i = 16;
						else
							i--;
				
						if (k < 0)
							k += m1;
						m [j] = k;
				
						if (j == 0)
							j = 16;
						else
							j--;
				
						x [count] = left + dm1 * (double)k * width;
					}
			
				} else {
			
					for (int count = 0; count < remainder; count++) {
						int k = m [i] - m [j];
				
						if (i == 0)
							i = 16;
						else
							i--;
				
						if (k < 0)
							k += m1;
						m [j] = k;
				
						if (j == 0)
							j = 16;
						else
							j--;
				
				
						x [count] = dm1 * (double)k;
					}
			
					for (int count = remainder; count < N; count += 4) {
						int k = m [i] - m [j];
						if (i == 0)
							i = 16;
						else
							i--;
						if (k < 0)
							k += m1;
						m [j] = k;
						if (j == 0)
							j = 16;
						else
							j--;
						x [count] = dm1 * (double)k;
				
				
						k = m [i] - m [j];
						if (i == 0)
							i = 16;
						else
							i--;
						if (k < 0)
							k += m1;
						m [j] = k;
						if (j == 0)
							j = 16;
						else
							j--;
						x [count + 1] = dm1 * (double)k;
				
				
						k = m [i] - m [j];
						if (i == 0)
							i = 16;
						else
							i--;
						if (k < 0)
							k += m1;
						m [j] = k;
						if (j == 0)
							j = 16;
						else
							j--;
						x [count + 2] = dm1 * (double)k;
				
				
						k = m [i] - m [j];
						if (i == 0)
							i = 16;
						else
							i--;
						if (k < 0)
							k += m1;
						m [j] = k;
						if (j == 0)
							j = 16;
						else
							j--;
						x [count + 3] = dm1 * (double)k;
					}
				}
			}
		}
		
		/*----------------------------------------------------------------------------
		PRIVATE METHODS
		------------------------------------------------------------------------ */
		
		private void initialize (int seed)
		{
			// First the initialization of the member variables;
			m1 = (one << mdig - 2) + ((one << mdig - 2) - one);
			m2 = one << mdig / 2;
			dm1 = 1.0 / (double)m1;
		
			int jseed, k0, k1, j0, j1, iloop;
			
			this.seed = seed;
			
			m = new int[17];
			
			jseed = System.Math.Min (System.Math.Abs (seed), m1);
			if (jseed % 2 == 0)
				--jseed;
			k0 = 9069 % m2;
			k1 = 9069 / m2;
			j0 = jseed % m2;
			j1 = jseed / m2;
			for (iloop = 0; iloop < 17; ++iloop) {
				jseed = j0 * k0;
				j1 = (jseed / m2 + j0 * k1 + j1 * k0) % (m2 / 2);
				j0 = jseed % m2;
				m [iloop] = j0 + m2 * j1;
			}
			i = 4;
			j = 16;
			
		}
	}
}
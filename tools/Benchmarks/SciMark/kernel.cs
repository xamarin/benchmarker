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
	public class kernel
	{
		// each measurement returns approx Mflops
		public static double measureFFT (int N, double mintime, Random R)
		{
			// initialize FFT data as complex (N real/img pairs)
			
			double[] x = RandomVector (2 * N, R);
			long cycles = 20000;
			Stopwatch Q = new Stopwatch ();
			Q.start ();
			int i = 0;
			while (i < cycles) {
				FFT.transform (x); // forward transform
				FFT.inverse (x); // backward transform
				i++;
			}
			Q.stop ();

			const double EPS = 1.0e-10;
			if (FFT.test (x) / N > EPS)
				return 0.0;
			
			return FFT.num_flops (N) * (double) cycles / Q.read () * 1.0e-6;
		}

		
		public static double measureSOR (int N, double min_time, Random R)
		{
			double[][] G = RandomMatrix (N, N, R);
			
			Stopwatch Q = new Stopwatch ();
			int num_iterations = 40000;
			Q.start ();
			SOR.execute (1.25, G, num_iterations);
			Q.stop ();
			// approx Mflops
			return SOR.num_flops (N, N, num_iterations) / Q.read () * 1.0e-6;
		}

		public static double measureMonteCarlo (double min_time, Random R)
		{
			Stopwatch Q = new Stopwatch ();
			int num_samples = 40000000;
			Q.start ();
			MonteCarlo.integrate (num_samples);
			Q.stop ();
			// approx Mflops
			return MonteCarlo.num_flops (num_samples) / Q.read () * 1.0e-6;
		}

		
		public static double measureSparseMatmult (int N, int nz, double min_time, Random R)
		{
			// initialize vector multipliers and storage for result
			// y = A*y;
			
			double[] x = RandomVector (N, R);
			double[] y = new double[N];
			
			// initialize square sparse matrix
			//
			// for this test, we create a sparse matrix wit M/nz nonzeros
			// per row, with spaced-out evenly between the begining of the
			// row to the main diagonal.  Thus, the resulting pattern looks
			// like
			//             +-----------------+
			//             +*                +
			//             +***              +
			//             +* * *            +
			//             +** *  *          +
			//             +**  *   *        +
			//             +* *   *   *      +
			//             +*  *   *    *    +
			//             +*   *    *    *  + 
			//             +-----------------+
			//
			// (as best reproducible with integer artihmetic)
			// Note that the first nr rows will have elements past
			// the diagonal.
			
			int nr = nz / N; // average number of nonzeros per row
			int anz = nr * N; // _actual_ number of nonzeros
			
			
			double[] val = RandomVector (anz, R);
			int[] col = new int[anz];
			int[] row = new int[N + 1];
			
			row [0] = 0;
			for (int r = 0; r < N; r++) {
				// initialize elements for row r
				
				int rowr = row [r];
				row [r + 1] = rowr + nr;
				int step = r / nr;
				if (step < 1)
					step = 1;
				// take at least unit steps
				
				
				for (int i = 0; i < nr; i++)
					col [rowr + i] = i * step;
				
			}
			
			Stopwatch Q = new Stopwatch ();
			int cycles = 150000;
			Q.start ();
			SparseCompRow.matmult (y, val, row, col, x, cycles);
			Q.stop ();
			// approx Mflops
			return SparseCompRow.num_flops (N, nz, cycles) / Q.read () * 1.0e-6;
		}

		
		public static double measureLU (int N, double min_time, Random R)
		{
			// compute approx Mlfops, or O if LU yields large errors
			
			double[][] A = RandomMatrix (N, N, R);
			double[][] lu = new double[N][];
			for (int i = 0; i < N; i++) {
				lu [i] = new double[N];
			}
			int[] pivot = new int[N];
			
			Stopwatch Q = new Stopwatch ();
			int cycles = 4095;
			Q.start ();

			for (int j = 0; j < cycles; j++) {
					CopyMatrix (lu, A);
					LU.factor (lu, pivot);
			}
			Q.stop ();
			
			// verify that LU is correct
			double[] b = RandomVector (N, R);
			double[] x = NewVectorCopy (b);
			
			LU.solve (lu, pivot, x);
			
			const double EPS = 1.0e-12;
			if (normabs (b, matvec (A, x)) / N > EPS)
				return 0.0;
			// else return approx Mflops
			//
			return LU.num_flops (N) * cycles / Q.read () * 1.0e-6;
		}

		
		private static double[] NewVectorCopy (double[] x)
		{
			int N = x.Length;
			
			double[] y = new double[N];
			for (int i = 0; i < N; i++)
				y [i] = x [i];
			
			return y;
		}

		private static void  CopyVector (double[] B, double[] A)
		{
			int N = A.Length;
			
			for (int i = 0; i < N; i++)
				B [i] = A [i];
		}

		
		private static double normabs (double[] x, double[] y)
		{
			int N = x.Length;
			double sum = 0.0;
			
			for (int i = 0; i < N; i++)
				sum += System.Math.Abs (x [i] - y [i]);
			
			return sum;
		}

		private static void  CopyMatrix (double[][] B, double[][] A)
		{
			int M = A.Length;
			int N = A [0].Length;
			
			int remainder = N & 3; // N mod 4;
			
			for (int i = 0; i < M; i++) {
				double[] Bi = B [i];
				double[] Ai = A [i];
				for (int j = 0; j < remainder; j++)
					Bi [j] = Ai [j];
				for (int j = remainder; j < N; j += 4) {
					Bi [j] = Ai [j];
					Bi [j + 1] = Ai [j + 1];
					Bi [j + 2] = Ai [j + 2];
					Bi [j + 3] = Ai [j + 3];
				}
			}
		}

		private static double[][] RandomMatrix (int M, int N, Random R)
		{
			double[][] A = new double[M][];
			for (int i = 0; i < M; i++) {
				A [i] = new double[N];
			}
			
			for (int i = 0; i < N; i++)
				for (int j = 0; j < N; j++)
					A [i] [j] = R.nextDouble ();
			return A;
		}

		private static double[] RandomVector (int N, Random R)
		{
			double[] A = new double[N];
			
			for (int i = 0; i < N; i++)
				A [i] = R.nextDouble ();
			return A;
		}

		private static double[] matvec (double[][] A, double[] x)
		{
			int N = x.Length;
			double[] y = new double[N];
			
			matvec (A, x, y);
			
			return y;
		}

		private static void  matvec (double[][] A, double[] x, double[] y)
		{
			int M = A.Length;
			int N = A [0].Length;
			
			for (int i = 0; i < M; i++) {
				double sum = 0.0;
				double[] Ai = A [i];
				for (int j = 0; j < N; j++)
					sum += Ai [j] * x [j];
				
				y [i] = sum;
			}
		}
	}
}
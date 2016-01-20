using System;

namespace SciMark2
{
	public class kernel
	{
		public static double measureFFT(int N, double mintime, Random R)
		{
			double[] data = kernel.RandomVector(checked (2 * N), R);
			long num1 = 20000;
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.start();
			int num2 = 0;
			while ((long) num2 < num1)
			{
				FFT.transform(data);
				FFT.inverse(data);
				checked { ++num2; }
			}
			stopwatch.stop();
			if (FFT.test(data) / (double) N > 0.0 / 1.0)
				return 0.0;
			return FFT.num_flops(N) * (double) num1 / stopwatch.read() * 1E-06;
		}

		public static double measureSOR(int N, double min_time, Random R)
		{
			double[][] G = kernel.RandomMatrix(N, N, R);
			Stopwatch stopwatch = new Stopwatch();
			int num_iterations = 40000;
			stopwatch.start();
			SOR.execute(1.25, G, num_iterations);
			stopwatch.stop();
			return SOR.num_flops(N, N, num_iterations) / stopwatch.read() * 1E-06;
		}

		public static double measureMonteCarlo(double min_time, Random R)
		{
			Stopwatch stopwatch = new Stopwatch();
			int Num_samples = 40000000;
			stopwatch.start();
			MonteCarlo.integrate(Num_samples);
			stopwatch.stop();
			return MonteCarlo.num_flops(Num_samples) / stopwatch.read() * 1E-06;
		}

		public static double measureSparseMatmult(int N, int nz, double min_time, Random R)
		{
			double[] x = kernel.RandomVector(N, R);
			double[] y = new double[N];
			int num1 = nz / N;
			int N1 = checked (num1 * N);
			double[] val = kernel.RandomVector(N1, R);
			int[] col = new int[N1];
			int[] row = new int[checked (N + 1)];
			row[0] = 0;
			int index = 0;
			while (index < N)
			{
				int num2 = row[index];
				row[checked (index + 1)] = checked (num2 + num1);
				int num3 = index / num1;
				if (num3 < 1)
					num3 = 1;
				int num4 = 0;
				while (num4 < num1)
				{
					col[checked (num2 + num4)] = checked (num4 * num3);
					checked { ++num4; }
				}
				checked { ++index; }
			}
			Stopwatch stopwatch = new Stopwatch();
			int num5 = 150000;
			stopwatch.start();
			SparseCompRow.matmult(y, val, row, col, x, num5);
			stopwatch.stop();
			return SparseCompRow.num_flops(N, nz, num5) / stopwatch.read() * 1E-06;
		}

		public static double measureLU(int N, double min_time, Random R)
		{
			double[][] A = kernel.RandomMatrix(N, N, R);
			double[][] numArray1 = new double[N][];
			int index = 0;
			while (index < N)
			{
				numArray1[index] = new double[N];
				checked { ++index; }
			}
			int[] numArray2 = new int[N];
			Stopwatch stopwatch = new Stopwatch();
			int num1 = 4095;
			stopwatch.start();
			int num2 = 0;
			while (num2 < num1)
			{
				kernel.CopyMatrix(numArray1, A);
				LU.factor(numArray1, numArray2);
				checked { ++num2; }
			}
			stopwatch.stop();
			double[] x = kernel.RandomVector(N, R);
			double[] numArray3 = kernel.NewVectorCopy(x);
			LU.solve(numArray1, numArray2, numArray3);
			if (kernel.normabs(x, kernel.matvec(A, numArray3)) / (double) N > 0.0 / 1.0)
				return 0.0;
			return LU.num_flops(N) * (double) num1 / stopwatch.read() * 1E-06;
		}

		private static double[] NewVectorCopy(double[] x)
		{
			int length = x.Length;
			double[] numArray = new double[length];
			int index = 0;
			while (index < length)
			{
				numArray[index] = x[index];
				checked { ++index; }
			}
			return numArray;
		}

		private static void CopyVector(double[] B, double[] A)
		{
			int length = A.Length;
			int index = 0;
			while (index < length)
			{
				B[index] = A[index];
				checked { ++index; }
			}
		}

		private static double normabs(double[] x, double[] y)
		{
			int length = x.Length;
			double num = 0.0;
			int index = 0;
			while (index < length)
			{
				num += Math.Abs(x[index] - y[index]);
				checked { ++index; }
			}
			return num;
		}

		private static void CopyMatrix(double[][] B, double[][] A)
		{
			int length1 = A.Length;
			int length2 = A[0].Length;
			int num = length2 & 3;
			int index1 = 0;
			while (index1 < length1)
			{
				double[] numArray1 = B[index1];
				double[] numArray2 = A[index1];
				int index2 = 0;
				while (index2 < num)
				{
					numArray1[index2] = numArray2[index2];
					checked { ++index2; }
				}
				int index3 = num;
				while (index3 < length2)
				{
					numArray1[index3] = numArray2[index3];
					numArray1[checked (index3 + 1)] = numArray2[checked (index3 + 1)];
					numArray1[checked (index3 + 2)] = numArray2[checked (index3 + 2)];
					numArray1[checked (index3 + 3)] = numArray2[checked (index3 + 3)];
					checked { index3 += 4; }
				}
				checked { ++index1; }
			}
		}

		private static double[][] RandomMatrix(int M, int N, Random R)
		{
			double[][] numArray = new double[M][];
			int index1 = 0;
			while (index1 < M)
			{
				numArray[index1] = new double[N];
				checked { ++index1; }
			}
			int index2 = 0;
			while (index2 < N)
			{
				int index3 = 0;
				while (index3 < N)
				{
					numArray[index2][index3] = R.nextDouble();
					checked { ++index3; }
				}
				checked { ++index2; }
			}
			return numArray;
		}

		private static double[] RandomVector(int N, Random R)
		{
			double[] numArray = new double[N];
			int index = 0;
			while (index < N)
			{
				numArray[index] = R.nextDouble();
				checked { ++index; }
			}
			return numArray;
		}

		private static double[] matvec(double[][] A, double[] x)
		{
			double[] y = new double[x.Length];
			kernel.matvec(A, x, y);
			return y;
		}

		private static void matvec(double[][] A, double[] x, double[] y)
		{
			int length1 = A.Length;
			int length2 = A[0].Length;
			int index1 = 0;
			while (index1 < length1)
			{
				double num = 0.0;
				double[] numArray = A[index1];
				int index2 = 0;
				while (index2 < length2)
				{
					num += numArray[index2] * x[index2];
					checked { ++index2; }
				}
				y[index1] = num;
				checked { ++index1; }
			}
		}
	}
}


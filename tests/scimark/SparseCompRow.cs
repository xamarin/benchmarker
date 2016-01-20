namespace SciMark2
{
	public class SparseCompRow
	{
		public static double num_flops(int N, int nz, int num_iterations)
		{
			return (double) checked (unchecked (nz / N) * N) * 2.0 * (double) num_iterations;
		}

		public static void matmult(double[] y, double[] val, int[] row, int[] col, double[] x, int NUM_ITERATIONS)
		{
			int num1 = checked (row.Length - 1);
			int num2 = 0;
			while (num2 < NUM_ITERATIONS)
			{
				int index1 = 0;
				while (index1 < num1)
				{
					double num3 = 0.0;
					int num4 = row[index1];
					int num5 = row[checked (index1 + 1)];
					int index2 = num4;
					while (index2 < num5)
					{
						num3 += x[col[index2]] * val[index2];
						checked { ++index2; }
					}
					y[index1] = num3;
					checked { ++index1; }
				}
				checked { ++num2; }
			}
		}
	}
}


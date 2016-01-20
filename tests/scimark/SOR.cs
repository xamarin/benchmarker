namespace SciMark2
{
	public class SOR
	{
		public static double num_flops(int M, int N, int num_iterations)
		{
			return ((double) M - 1.0) * ((double) N - 1.0) * (double) num_iterations * 6.0;
		}

		public static void execute(double omega, double[][] G, int num_iterations)
		{
			int length1 = G.Length;
			int length2 = G[0].Length;
			double num1 = omega * 0.25;
			double num2 = 1.0 - omega;
			int num3 = checked (length1 - 1);
			int num4 = checked (length2 - 1);
			int num5 = 0;
			while (num5 < num_iterations)
			{
				int index1 = 1;
				while (index1 < num3)
				{
					double[] numArray1 = G[index1];
					double[] numArray2 = G[checked (index1 - 1)];
					double[] numArray3 = G[checked (index1 + 1)];
					int index2 = 1;
					while (index2 < num4)
					{
						numArray1[index2] = num1 * (numArray2[index2] + numArray3[index2] + numArray1[checked (index2 - 1)] + numArray1[checked (index2 + 1)]) + num2 * numArray1[index2];
						checked { ++index2; }
					}
					checked { ++index1; }
				}
				checked { ++num5; }
			}
		}
	}
}


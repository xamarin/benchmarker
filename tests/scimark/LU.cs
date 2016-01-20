using System;

namespace SciMark2
{
	public class LU
	{
		private double[][] m_LU;
		private int[] m_pivot;

		public LU(double[][] A)
		{
			this.m_LU = LU.new_copy(A);
			this.m_pivot = new int[A.Length];
			LU.factor(this.m_LU, this.m_pivot);
		}

		public static double num_flops(int N)
		{
			double num = (double) N;
			return 2.0 * num * num * num / 3.0;
		}

		protected internal static double[] new_copy(double[] x)
		{
			double[] numArray = new double[x.Length];
			x.CopyTo((Array) numArray, 0);
			return numArray;
		}

		protected internal static double[][] new_copy(double[][] A)
		{
			int length1 = A.Length;
			int length2 = A[0].Length;
			double[][] numArray = new double[length1][];
			int index1 = 0;
			while (index1 < length1)
			{
				numArray[index1] = new double[length2];
				checked { ++index1; }
			}
			int index2 = 0;
			while (index2 < length1)
			{
				A[index2].CopyTo((Array) numArray[index2], 0);
				checked { ++index2; }
			}
			return numArray;
		}

		public static int[] new_copy(int[] x)
		{
			int[] numArray = new int[x.Length];
			x.CopyTo((Array) numArray, 0);
			return numArray;
		}

		protected internal static void insert_copy(double[][] B, double[][] A)
		{
			int index = 0;
			while (index < A.Length)
			{
				A[index].CopyTo((Array) B[index], 0);
				checked { ++index; }
			}
		}

		public virtual double[] solve(double[] b)
		{
			double[] b1 = LU.new_copy(b);
			LU.solve(this.m_LU, this.m_pivot, b1);
			return b1;
		}

		public static int factor(double[][] A, int[] pivot)
		{
			int length1 = A.Length;
			int length2 = A[0].Length;
			int num1 = Math.Min(length2, length1);
			int index1 = 0;
			while (index1 < num1)
			{
				int index2 = index1;
				double num2 = Math.Abs(A[index1][index1]);
				int index3 = checked (index1 + 1);
				while (index3 < length2)
				{
					double num3 = Math.Abs(A[index3][index1]);
					if (num3 > num2)
					{
						index2 = index3;
						num2 = num3;
					}
					checked { ++index3; }
				}
				pivot[index1] = index2;
				if (A[index2][index1] == 0.0)
					return 1;
				if (index2 != index1)
				{
					double[] numArray = A[index1];
					A[index1] = A[index2];
					A[index2] = numArray;
				}
				if (index1 < checked (length2 - 1))
				{
					double num3 = 1.0 / A[index1][index1];
					int index4 = checked (index1 + 1);
					while (index4 < length2)
					{
						A[index4][index1] *= num3;
						checked { ++index4; }
					}
				}
				if (index1 < checked (num1 - 1))
				{
					int index4 = checked (index1 + 1);
					while (index4 < length2)
					{
						double[] numArray1 = A[index4];
						double[] numArray2 = A[index1];
						double num3 = numArray1[index1];
						int index5 = checked (index1 + 1);
						while (index5 < length1)
						{
							numArray1[index5] -= num3 * numArray2[index5];
							checked { ++index5; }
						}
						checked { ++index4; }
					}
				}
				checked { ++index1; }
			}
			return 0;
		}

		public static void solve(double[][] A, int[] pvt, double[] b)
		{
			int length1 = A.Length;
			int length2 = A[0].Length;
			int num1 = 0;
			int index1 = 0;
			while (index1 < length1)
			{
				int index2 = pvt[index1];
				double num2 = b[index2];
				b[index2] = b[index1];
				if (num1 == 0)
				{
					int index3 = num1;
					while (index3 < index1)
					{
						num2 -= A[index1][index3] * b[index3];
						checked { ++index3; }
					}
				}
				else if (num2 == 0.0)
					num1 = index1;
				b[index1] = num2;
				checked { ++index1; }
			}
			int index4 = checked (length2 - 1);
			while (index4 >= 0)
			{
				double num2 = b[index4];
				int index2 = checked (index4 + 1);
				while (index2 < length2)
				{
					num2 -= A[index4][index2] * b[index2];
					checked { ++index2; }
				}
				b[index4] = num2 / A[index4][index4];
				checked { --index4; }
			}
		}
	}
}


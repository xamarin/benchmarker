using System;

namespace SciMark2
{
	public class FFT
	{
		public static double num_flops(int N)
		{
			double num1 = (double) N;
			double num2 = (double) FFT.log2(N);
			return (5.0 * num1 - 2.0) * num2 + 2.0 * (num1 + 1.0);
		}

		public static void transform(double[] data)
		{
			FFT.transform_internal(data, -1);
		}

		public static void inverse(double[] data)
		{
			FFT.transform_internal(data, 1);
			int length = data.Length;
			double num = 1.0 / (double) (length / 2);
			int index = 0;
			while (index < length)
			{
				data[index] *= num;
				checked { ++index; }
			}
		}

		public static double test(double[] data)
		{
			int length = data.Length;
			double[] numArray = new double[length];
			Array.Copy((Array) data, 0, (Array) numArray, 0, length);
			FFT.transform(data);
			FFT.inverse(data);
			double num1 = 0.0;
			int index = 0;
			while (index < length)
			{
				double num2 = data[index] - numArray[index];
				num1 += num2 * num2;
				checked { ++index; }
			}
			return Math.Sqrt(num1 / (double) length);
		}

		public static double[] makeRandom(int n)
		{
			int length = checked (2 * n);
			double[] numArray = new double[length];
			System.Random random = new System.Random();
			int index = 0;
			while (index < length)
			{
				numArray[index] = random.NextDouble();
				checked { ++index; }
			}
			return numArray;
		}

		protected internal static int log2(int n)
		{
			int num1 = 0;
			int num2 = 1;
			while (num2 < n)
			{
				checked { num2 *= 2; }
				checked { ++num1; }
			}
			if (n != 1 << num1)
				throw new ApplicationException("FFT: Data length is not a power of 2!: " + (object) n);
			return num1;
		}

		protected internal static void transform_internal(double[] data, int direction)
		{
			if (data.Length == 0)
				return;
			int n = data.Length / 2;
			if (n == 1)
				return;
			int num1 = FFT.log2(n);
			FFT.bitreverse(data);
			int num2 = 0;
			int num3 = 1;
			while (num2 < num1)
			{
				double num4 = 1.0;
				double num5 = 0.0;
				double a = 2.0 * (double) direction * Math.PI / (2.0 * (double) num3);
				double num6 = Math.Sin(a);
				double num7 = Math.Sin(a / 2.0);
				double num8 = 2.0 * num7 * num7;
				int num9 = 0;
				while (num9 < n)
				{
					int index1 = checked (2 * num9);
					int index2 = checked (2 * num9 + num3);
					double num10 = data[index2];
					double num11 = data[checked (index2 + 1)];
					data[index2] = data[index1] - num10;
					data[checked (index2 + 1)] = data[checked (index1 + 1)] - num11;
					data[index1] += num10;
					data[checked (index1 + 1)] += num11;
					checked { num9 += 2 * num3; }
				}
				int num12 = 1;
				while (num12 < num3)
				{
					double num10 = num4 - num6 * num5 - num8 * num4;
					double num11 = num5 + num6 * num4 - num8 * num5;
					num4 = num10;
					num5 = num11;
					int num13 = 0;
					while (num13 < n)
					{
						int index1 = checked (2 * num13 + num12);
						int index2 = checked (2 * num13 + num12 + num3);
						double num14 = data[index2];
						double num15 = data[checked (index2 + 1)];
						double num16 = num4 * num14 - num5 * num15;
						double num17 = num4 * num15 + num5 * num14;
						data[index2] = data[index1] - num16;
						data[checked (index2 + 1)] = data[checked (index1 + 1)] - num17;
						data[index1] += num16;
						data[checked (index1 + 1)] += num17;
						checked { num13 += 2 * num3; }
					}
					checked { ++num12; }
				}
				checked { ++num2; }
				checked { num3 *= 2; }
			}
		}

		protected internal static void bitreverse(double[] data)
		{
			int num1 = data.Length / 2;
			int num2 = checked (num1 - 1);
			int num3 = 0;
			int num4 = 0;
			while (num3 < num2)
			{
				int index1 = num3 << 1;
				int index2 = num4 << 1;
				int num5 = num1 >> 1;
				if (num3 < num4)
				{
					double num6 = data[index1];
					double num7 = data[checked (index1 + 1)];
					data[index1] = data[index2];
					data[checked (index1 + 1)] = data[checked (index2 + 1)];
					data[index2] = num6;
					data[checked (index2 + 1)] = num7;
				}
				while (num5 <= num4)
				{
					checked { num4 -= num5; }
					num5 >>= 1;
				}
				checked { num4 += num5; }
				checked { ++num3; }
			}
		}
	}
}


using System;
using System.Runtime.CompilerServices;

namespace SciMark2
{
	public class Random
	{
		internal int seed = 0;
		private int i = 4;
		private int j = 16;
		private bool haveRange = false;
		private double left = 0.0;
		private double right = 1.0;
		private double width = 1.0;
		private int[] m;
		private const int mdig = 32;
		private const int one = 1;
		private int m1;
		private int m2;
		private double dm1;

		public Random()
		{
			this.initialize(checked ((int) DateTime.Now.Ticks));
		}

		public Random(double left, double right)
		{
			this.initialize(checked ((int) DateTime.Now.Ticks));
			this.left = left;
			this.right = right;
			this.width = right - left;
			this.haveRange = true;
		}

		public Random(int seed)
		{
			this.initialize(seed);
		}

		public Random(int seed, double left, double right)
		{
			this.initialize(seed);
			this.left = left;
			this.right = right;
			this.width = right - left;
			this.haveRange = true;
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public double nextDouble()
		{
			int num = checked (this.m[this.i] - this.m[this.j]);
			if (num < 0)
				checked { num += this.m1; }
			this.m[this.j] = num;
			if (this.i == 0)
				this.i = 16;
			else
				checked { --this.i; }
			if (this.j == 0)
				this.j = 16;
			else
				checked { --this.j; }
			if (this.haveRange)
				return this.left + this.dm1 * (double) num * this.width;
			return this.dm1 * (double) num;
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public void nextDoubles(double[] x)
		{
			int length = x.Length;
			int num1 = length & 3;
			if (this.haveRange)
			{
				int index = 0;
				while (index < length)
				{
					int num2 = checked (this.m[this.i] - this.m[this.j]);
					if (this.i == 0)
						this.i = 16;
					else
						checked { --this.i; }
					if (num2 < 0)
						checked { num2 += this.m1; }
					this.m[this.j] = num2;
					if (this.j == 0)
						this.j = 16;
					else
						checked { --this.j; }
					x[index] = this.left + this.dm1 * (double) num2 * this.width;
					checked { ++index; }
				}
			}
			else
			{
				int index1 = 0;
				while (index1 < num1)
				{
					int num2 = checked (this.m[this.i] - this.m[this.j]);
					if (this.i == 0)
						this.i = 16;
					else
						checked { --this.i; }
					if (num2 < 0)
						checked { num2 += this.m1; }
					this.m[this.j] = num2;
					if (this.j == 0)
						this.j = 16;
					else
						checked { --this.j; }
					x[index1] = this.dm1 * (double) num2;
					checked { ++index1; }
				}
				int index2 = num1;
				while (index2 < length)
				{
					int num2 = checked (this.m[this.i] - this.m[this.j]);
					if (this.i == 0)
						this.i = 16;
					else
						checked { --this.i; }
					if (num2 < 0)
						checked { num2 += this.m1; }
					this.m[this.j] = num2;
					if (this.j == 0)
						this.j = 16;
					else
						checked { --this.j; }
					x[index2] = this.dm1 * (double) num2;
					int num3 = checked (this.m[this.i] - this.m[this.j]);
					if (this.i == 0)
						this.i = 16;
					else
						checked { --this.i; }
					if (num3 < 0)
						checked { num3 += this.m1; }
					this.m[this.j] = num3;
					if (this.j == 0)
						this.j = 16;
					else
						checked { --this.j; }
					x[checked (index2 + 1)] = this.dm1 * (double) num3;
					int num4 = checked (this.m[this.i] - this.m[this.j]);
					if (this.i == 0)
						this.i = 16;
					else
						checked { --this.i; }
					if (num4 < 0)
						checked { num4 += this.m1; }
					this.m[this.j] = num4;
					if (this.j == 0)
						this.j = 16;
					else
						checked { --this.j; }
					x[checked (index2 + 2)] = this.dm1 * (double) num4;
					int num5 = checked (this.m[this.i] - this.m[this.j]);
					if (this.i == 0)
						this.i = 16;
					else
						checked { --this.i; }
					if (num5 < 0)
						checked { num5 += this.m1; }
					this.m[this.j] = num5;
					if (this.j == 0)
						this.j = 16;
					else
						checked { --this.j; }
					x[checked (index2 + 3)] = this.dm1 * (double) num5;
					checked { index2 += 4; }
				}
			}
		}

		private void initialize(int seed)
		{
			this.m1 = int.MaxValue;
			this.m2 = 65536;
			this.dm1 = 1.0 / (double) this.m1;
			this.seed = seed;
			this.m = new int[17];
			int num1 = Math.Min(Math.Abs(seed), this.m1);
			if (num1 % 2 == 0)
				checked { --num1; }
			int num2 = 9069 % this.m2;
			int num3 = 9069 / this.m2;
			int num4 = num1 % this.m2;
			int num5 = num1 / this.m2;
			int index = 0;
			while (index < 17)
			{
				int num6 = checked (num4 * num2);
				num5 = checked (unchecked (num6 / this.m2) + num4 * num3 + num5 * num2) % (this.m2 / 2);
				num4 = num6 % this.m2;
				this.m[index] = checked (num4 + this.m2 * num5);
				checked { ++index; }
			}
			this.i = 4;
			this.j = 16;
		}
	}
}


namespace SciMark2
{
	public class MonteCarlo
	{
		internal const int SEED = 113;

		public static double num_flops(int Num_samples)
		{
			return (double) Num_samples * 4.0;
		}

		public static double integrate(int Num_samples)
		{
			Random random = new Random(113);
			int num1 = 0;
			int num2 = 0;
			while (num2 < Num_samples)
			{
				double num3 = random.nextDouble();
				double num4 = random.nextDouble();
				if (num3 * num3 + num4 * num4 <= 1.0)
					checked { ++num1; }
				checked { ++num2; }
			}
			return (double) num1 / (double) Num_samples * 4.0;
		}
	}
}


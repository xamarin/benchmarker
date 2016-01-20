using System;

namespace SciMark2
{
	public class CommandLine
	{
		[STAThread]
		public static void Main(string[] args)
		{
			double num = 2.0;
			CommandLine.Benchmarks benchmarks = CommandLine.Benchmarks.NONE;
			int N1 = 1024;
			int N2 = 100;
			int N3 = 1000;
			int nz = 5000;
			int N4 = 100;
			int index = 0;
			while (index < args.Length)
			{
				string str = args[index];
				if (str.Equals("FFT"))
					benchmarks |= CommandLine.Benchmarks.FFT;
				else if (str.Equals("SOR"))
					benchmarks |= CommandLine.Benchmarks.SOR;
				else if (str.Equals("MC"))
					benchmarks |= CommandLine.Benchmarks.MC;
				else if (str.Equals("MM"))
					benchmarks |= CommandLine.Benchmarks.MM;
				else if (str.Equals("LU"))
					benchmarks |= CommandLine.Benchmarks.LU;
				else if (str.ToUpper().Equals("-h"))
					break;
				checked { ++index; }
			}
			if (index < args.Length)
			{
				if (args[index].ToUpper().Equals("-h") || args[index].ToUpper().Equals("-help"))
				{
					Console.WriteLine("Usage: [-large] [minimum_time]");
					return;
				}
				if (args[index].ToUpper().Equals("-LARGE"))
				{
					N1 = 1048576;
					N2 = 1000;
					N3 = 100000;
					nz = 1000000;
					N4 = 1000;
					checked { ++index; }
				}
				if (args.Length > index)
					num = double.Parse(args[index]);
			}
			Console.WriteLine("**                                                               **");
			Console.WriteLine("** SciMark2a Numeric Benchmark, see http://math.nist.gov/scimark **");
			Console.WriteLine("**                                                               **");
			double[] numArray = new double[6];
			Random R = new Random(101010);
			Console.WriteLine("Mininum running time = {0} seconds", (object) num);
			if (benchmarks.HasFlag((Enum) CommandLine.Benchmarks.FFT))
				numArray[1] = kernel.measureFFT(N1, num, R);
			if (benchmarks.HasFlag((Enum) CommandLine.Benchmarks.SOR))
				numArray[2] = kernel.measureSOR(N2, num, R);
			if (benchmarks.HasFlag((Enum) CommandLine.Benchmarks.MC))
				numArray[3] = kernel.measureMonteCarlo(num, R);
			if (benchmarks.HasFlag((Enum) CommandLine.Benchmarks.MM))
				numArray[4] = kernel.measureSparseMatmult(N3, nz, num, R);
			if (benchmarks.HasFlag((Enum) CommandLine.Benchmarks.LU))
				numArray[5] = kernel.measureLU(N4, num, R);
			Console.WriteLine();
			if (benchmarks.HasFlag((Enum) CommandLine.Benchmarks.FFT))
				Console.WriteLine("FFT            : {0} - ({1})", numArray[1] != 0.0 ? (object) numArray[1].ToString("F2") : (object) "ERROR, INVALID NUMERICAL RESULT!", (object) N1);
			if (benchmarks.HasFlag((Enum) CommandLine.Benchmarks.SOR))
				Console.WriteLine("SOR            : {1:F2} - ({0}x{0})", (object) N2, (object) numArray[2]);
			if (benchmarks.HasFlag((Enum) CommandLine.Benchmarks.MC))
				Console.WriteLine("Monte Carlo    :  {0:F2}", (object) numArray[3]);
			if (benchmarks.HasFlag((Enum) CommandLine.Benchmarks.MM))
				Console.WriteLine("Sparse MatMult : {2:F2} - (N={0}, nz={1})", (object) N3, (object) nz, (object) numArray[4]);
			if (!benchmarks.HasFlag((Enum) CommandLine.Benchmarks.LU))
				return;
			Console.WriteLine("LU             : {1} - ({0}x{0})", (object) N4, numArray[5] != 0.0 ? (object) numArray[5].ToString("F2") : (object) "ERROR, INVALID NUMERICAL RESULT!");
		}

		[Flags]
		private enum Benchmarks
		{
			NONE = 0,
			FFT = 1,
			SOR = 2,
			MC = 4,
			MM = 8,
			LU = 16,
		}
	}
}


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
using Common.Logging;

namespace Benchmarks.SciMark
{
	/// <summary>
	/// SciMark2: A Java numerical benchmark measuring performance
	/// of computational kernels for FFTs, Monte Carlo simulation,
	/// sparse matrix computations, Jacobi SOR, and dense LU matrix
	/// factorizations.  
	/// </summary>
	
	public class ScimarkEntrypoint
	{
		/// <summary>
		///  Benchmark 5 kernels with individual Mflops.
		///  "results[0]" has the average Mflop rate.
		/// </summary>
		public static void  Main (System.String[] args, ILog logger)
		{
			// default to the (small) cache-contained version
			double min_time = Constants.RESOLUTION_DEFAULT;
			
			int FFT_size = Constants.FFT_SIZE;
			int SOR_size = Constants.SOR_SIZE;
			int Sparse_size_M = Constants.SPARSE_SIZE_M;
			int Sparse_size_nz = Constants.SPARSE_SIZE_nz;
			int LU_size = Constants.LU_SIZE;
			
			// look for runtime options
			if (args.Length < 1) {
				logger.InfoFormat ("Usage: <benchmark-name>");
				return;
			}


			logger.InfoFormat ("**                                                               **");
			logger.InfoFormat ("** SciMark2a Numeric Benchmark, see http://math.nist.gov/scimark **");
			logger.InfoFormat ("**                                                               **");
		
			// run the benchmark
			SciMark.Random R = new SciMark.Random (Constants.RANDOM_SEED);
			
			logger.InfoFormat ("Mininum running time = {0} seconds", min_time);
			double res;
			switch (args [0]) {
			case "fft":
				res = kernel.measureFFT (FFT_size, min_time, R);
				logger.InfoFormat ("FFT            : {0} - ({1})", res == 0.0 ? "ERROR, INVALID NUMERICAL RESULT!" : res.ToString ("F2"), FFT_size);            
				break;
			case "sor":
				res = kernel.measureSOR (SOR_size, min_time, R);
				logger.InfoFormat ("SOR            : {1:F2} - ({0}x{0})", SOR_size, res);
				break;
			case "mc":
				res = kernel.measureMonteCarlo (min_time, R);
				logger.InfoFormat ("Monte Carlo    :  {0:F2}", res);
				break;
			case "mm":
				res = kernel.measureSparseMatmult (Sparse_size_M, Sparse_size_nz, min_time, R);
				logger.InfoFormat ("Sparse MatMult : {2:F2} - (N={0}, nz={1})", Sparse_size_M, Sparse_size_nz, res);
				break;
			case "lu":
				res = kernel.measureLU (LU_size, min_time, R);	
				logger.InfoFormat ("LU             : {1} - ({0}x{0})", LU_size, res == 0.0 ? "ERROR, INVALID NUMERICAL RESULT!" : res.ToString ("F2"));
				break;
			default:
				logger.InfoFormat ("Invalid benchmark: {0}", args [1]);
				break;
			}
		}
	}
}
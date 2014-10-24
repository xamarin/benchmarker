using System;
using System.Threading;
using System.Diagnostics;

using MBF.PerfTests;

namespace MBF.PerfTests
{
	public class TestRunner
	{
		public static void Main ()
		{
			PerfTests perfTests = new PerfTests ();
        
			perfTests.ObjectModelPerfUsingSmallSizeFlavobacteriabacteriumTestData();
			MemoryRelax (ref perfTests);
			perfTests.ObjectModelPerfUsingLargeSizeFlavoBacteriabacteriumTestData();				
			MemoryRelax (ref perfTests);
			perfTests.MUMmerPerfTestUsingSmallSizeEcoliData();
			MemoryRelax (ref perfTests);
			perfTests.MUMmerPerfTestUsingLargeSizeEcoliData();
			MemoryRelax (ref perfTests);
			perfTests.NUCmerPerfTestUsingSmallSizeEcoliData();
			MemoryRelax (ref perfTests);
			perfTests.NUCmerPerfTestUsingMediumSizeEcoliData();
			MemoryRelax (ref perfTests);
			//perfTests.PaDeNAPerfUsingLargeSizeEulerTestData();
			//MemoryRelax (ref perfTests);
			perfTests.PaDeNAPerfUsingSmallSizeEulerTestData();
			MemoryRelax (ref perfTests);
			perfTests.PAMSAMPerfTestUsingMediumSizeTestData();
			MemoryRelax (ref perfTests);
			perfTests.PAMSAMPerfTestUsingLargeSizeTestData();
			MemoryRelax (ref perfTests);
			perfTests.SmithWatermanPerfTestUsingSmallSizeTestData();       
			MemoryRelax (ref perfTests);
			perfTests.SmithWatermanPerfTestUsingMediumSizeTestData();        
			MemoryRelax (ref perfTests);
			perfTests.NeedlemanWunschPerfTestUsingSmallSizeTestData();
			MemoryRelax (ref perfTests);
			perfTests.NeedlemanWunschPerfTestUsingMediumSizeTestData();
			MemoryRelax (ref perfTests);
			perfTests.BAMParserPerfTestUsingLargeSizeTestData();
			MemoryRelax (ref perfTests);
			perfTests.BAMParserPerfTestUsingVeryLargeSizeTestData();
			MemoryRelax (ref perfTests);
			perfTests.SAMParserPerfTestUsingLargeSizeTestData();
			MemoryRelax (ref perfTests);
			perfTests.SAMParserPerfTestUsingVeryLargeSizeTestData();
		}

		static void MemoryRelax (ref PerfTests perfTests)
		{
			perfTests = null;
			Console.WriteLine ();
			GC.Collect ();
			Thread.Sleep (500);
			perfTests = new PerfTests ();
			Console.WriteLine ();
		}
	}
}

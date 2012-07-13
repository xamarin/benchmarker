
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

/**
 * A Java implementation of the <tt>health</tt> Olden benchmark.  The Olden
 * benchmark simulates the Columbian health-care system:
 * <p/>
 * <cite>
 * G. Lomow , J. Cleary, B. Unger, and D. West. "A Performance Study of
 * Time Warp," In SCS Multiconference on Distributed Simulation, pages 50-55,
 * Feb. 1988.
 * </cite>
 */
public class Health
{

	/**
	 * The size of the health-care system.
	 **/
	private static int maxLevel = 5;
	/**
	 * The maximum amount of time to use in the simulation.
	 **/
	private static int maxTime = 20;
	/**
	 * Set to true to print the results.
	 **/
	private static bool printResult = true;
	/**
	 * Set to true to print information messages.
	 **/
	private static bool printMsgs = true;

	/**
	 * The main routnie which creates the data structures for the Columbian
	 * health-care system and executes the simulation for a specified time.
	 *
	 * @param args the command line arguments
	 */
	public static void Main(String[] args)
	{
		parseCmdLine(args);

		Village.initVillageStatic();

		Village top = Village.createVillage(maxLevel, 0, true, 1);

		if(printMsgs)
			Console.WriteLine("Columbian Health Care Simulator\nWorking...");

		for(int i = 0; i < maxTime; i++)
			top.simulate();

		Results r = top.getResults();

		if(printResult)
		{
			Console.WriteLine("# People treated: " + (int)r.totalPatients);
			Console.WriteLine("Avg. length of stay: " + r.totalTime / r.totalPatients);
			Console.WriteLine("Avg. # of hospitals visited: " + r.totalHospitals / r.totalPatients);
		}

		Console.WriteLine("Done!");
	}

	private static void parseCmdLine(String[] args)
	{
		String arg;
		int i = 0;
		while(i < args.Length && args[i].StartsWith("-"))
		{
			arg = args[i++];

			// check for options that require arguments
			if(arg.Equals("-l"))
			{
				if(i < args.Length)
					maxLevel = Int32.Parse(args[i++]);
				else
					throw new Exception("-l requires the number of levels");
			}
			else if(arg.Equals("-t"))
			{
				if(i < args.Length)
					maxTime = Int32.Parse(args[i++]);
				else
					throw new Exception("-t requires the amount of time");
			}
			else if(arg.Equals("-p"))
			{
				printResult = true;
			}
			else if(arg.Equals("-m"))
			{
				printMsgs = true;
			}
			else if(arg.Equals("-h"))
			{
				usage();
			}
		}

		if(maxTime == 0 || maxLevel == 0)
			usage();
	}

	/**
	 * The usage routine which describes the program options.
	 **/
	private static void usage()
	{
		Console.WriteLine("usage: java Health -l <levels> -t <time> -s <seed> [-p] [-m] [-h]");
		Console.WriteLine("    -l the size of the health care system");
		Console.WriteLine("    -t the amount of simulation time");
		Console.WriteLine("    -p (print results)");
		Console.WriteLine("    -m (print information messages");
		Console.WriteLine("    -h (this message)");
		System.Environment.Exit(0);
	}
}

/*
hsize = 5;
simtime = 20;
=>
# People treated: 374
Avg. length of stay: 20
Avg. # of hospitals visited: 1.0614973262032086
*/



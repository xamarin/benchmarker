using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

public class BH
{

	private static int nbody = 20;

	/**
	 * The maximum number of time steps to take in the simulation
	 **/
	private static int nsteps = 10;

	/**
	 * Should we print information messsages
	 **/
	private static bool printMsgs = true;
	/**
	 * Should we print detailed results
	 **/
	private static bool printResults = true;

	public static double DTIME = 0.0125;

	public static void Main(String[] args)
	{
		parseCmdLine(args);

		Node.staticInitNode();
		Cell.initCell();

		if(nbody < 2)
		{
			Console.WriteLine("Needs at least 2 bodies.");
			System.Environment.Exit(1);
		}

		if(printMsgs)
			Console.WriteLine("nbody = " + nbody);

		BTree root = BTree.makeTreeX();
		root.createTestData(nbody);

		if(printMsgs)
			Console.WriteLine("Bodies created");

		int i = 0;
		while(i < nsteps)
			root.stepSystem(i++);

		if(printResults)
		{
			int j = 0;
			while(j < root.bodyTab.Count)
			{
				Body b = root.bodyTab[j];
				Console.Write("body " + j++ + " -- ");
				b.pos.printMathVector();
				Console.WriteLine();
			}
		}

		Console.WriteLine("Done!");
	}

	/**
	 * Random number generator used by the orignal BH benchmark.
	 *
	 * @param seed the seed to the generator
	 * @return a random number
	 */
	public static double myRand(double seed)
	{
		double t = 16807.0 * seed + 1.0;

		seed = t - (2147483647.0 * Math.Floor(t / 2147483647.0));
		return seed;
	}

	/**
	 * Generate a floating point random number.  Used by
	 * the original BH benchmark.
	 *
	 * @param xl lower bound
	 * @param xh upper bound
	 * @param r  seed
	 * @return a floating point randon number
	 */
	public static double xRand(double xl, double xh, double r)
	{
		double res = xl + (xh - xl) * r / 2147483647.0;
		return res;
	}

	private static void parseCmdLine(String[] args)
	{
		int i = 0;
		String arg;

		while(i < args.Length && args[i].StartsWith("-"))
		{
			arg = args[i++];

			// check for options that require arguments
			if(arg.Equals("-b"))
			{
				if(i < args.Length)
				{
					nbody = Int32.Parse(args[i++]);
				}
				else
				{
					throw new Exception("-l requires the number of levels");
				}
			}
			else if(arg.Equals("-s"))
			{
				if(i < args.Length)
				{
					nsteps = Int32.Parse(args[i++]);
				}
				else
				{
					throw new Exception("-l requires the number of levels");
				}
			}
			else if(arg.Equals("-m"))
			{
				printMsgs = true;
			}
			else if(arg.Equals("-p"))
			{
				printResults = true;
			}
			else if(arg.Equals("-h"))
			{
				usage();
			}
		}
		if(nbody == 0)
			usage();
	}

	/**
	 * The usage routine which describes the program options.
	 **/
	private static void usage()
	{
		Console.WriteLine("usage: java BH -b <size> [-s <steps>] [-p] [-m] [-h]");
		Console.WriteLine("    -b the number of bodies");
		Console.WriteLine("    -s the max. number of time steps (default=10)");
		Console.WriteLine("    -p (print detailed results)");
		Console.WriteLine("    -m (print information messages");
		Console.WriteLine("    -h (this message)");
		System.Environment.Exit(0);
	}
}

/*
bodies = 20;
=>
body 0 -- -1.2821861020089862 -1.586457202911863 -0.6506114161479286
body 1 -- 1.078932587274072 -0.3846961264683965 -0.4867034017582266
body 2 -- -1.1197392398134007 -0.23329715657985645 1.5172767963138205
body 3 -- -0.08812328592401991 -1.3623351405118682 -1.2339936274527314
body 4 -- -1.6982459252015967 -0.12281962308388987 0.11523162101262925
body 5 -- -1.0482479005425909 -1.2673876484514504 1.3471552460692195
body 6 -- 0.08258469896331147 1.9653422276670778 0.25379180508430627
body 7 -- 0.09194452888506448 -0.5118807392867327 1.3432743581735012
body 8 -- 1.86369603025833 1.1952035229337794 -1.5465775640145192
body 9 -- 1.7237292395119896 1.8205751310825782 -1.1546533339248115
body 10 -- 0.3186652391947364 -0.15457496278499516 -1.6058353022735967
body 11 -- 0.605291783348556 -1.0369777860345504 -0.09159035796034952
body 12 -- -1.3493725660842568 -1.0693634247955446 -1.4339985370649933
body 13 -- -0.5672475293822767 -0.06131802664332048 -0.4393572022622689
body 14 -- 0.9647770604208221 -1.1348832412353123 0.795648254335815
body 15 -- -0.14554429897865334 0.41977293636165425 1.8273604062256656
body 16 -- 0.6647756704796238 1.435968943980836 1.834269899996304
body 17 -- -0.14323827438316802 1.1977118746641604 -1.2702463359539806
body 18 -- 0.09558867877286309 0.9173730933041042 -0.8939648534389841
body 19 -- -0.047924788703112765 -0.0260922873275686 1.773578363593425
*/


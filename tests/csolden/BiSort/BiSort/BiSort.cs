
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

/**
 * A Java implementation of the <tt>bisort</tt> Olden benchmark.  The Olden
 * benchmark implements a Bitonic Sort as described in :
 * <p><cite>
 * G. Bilardi and A. Nicolau, "Adaptive Bitonic Sorting: An optimal parallel
 * algorithm for shared-memory machines." SIAM J. Comput. 18(2):216-228, 1998.
 * </cite>
 * <p/>
 * The benchmarks sorts N numbers where N is a power of 2.  If the user provides
 * an input value that is not a power of 2, then we use the nearest power of
 * 2 value that is less than the input value.
 */
public class BiSort
{
	private static int treesize = 10;
	private static bool printMsgs = true;
	private static bool printResults = true;

	/**
	 * The main routine which creates a tree and sorts it a couple of times.
	 *
	 * @param args the command line arguments
	 */
	public static void Main(String[] args)
	{
		Value.initValue();

		parseCmdLine(args);

		if(printMsgs)
			Console.WriteLine("Bisort with " + treesize + " values");

		Value tree = Value.createTree(treesize, 12345768);
		int sval = Value.random(245867) % Value.RANGE;

		if(printMsgs)
		{
			tree.inOrder();
			Console.Write("\n");
		}

		if(printMsgs)
			Console.WriteLine("BEGINNING BITONIC SORT ALGORITHM HERE");

		sval = tree.bisort(sval, Value.FORWARD);

		if(printResults)
		{
			tree.inOrder();
			Console.Write("\n");
		}

		sval = tree.bisort(sval, Value.BACKWARD);

		if(printResults)
		{
			tree.inOrder();
			Console.Write("\n");
		}

		Console.WriteLine("Done!");
	}

	private static void parseCmdLine(String[] args)
	{
		int i = 0;
		String arg;

		while(i < args.Length && args[i].StartsWith("-"))
		{
			arg = args[i++];

			// check for options that require arguments
			if(arg.Equals("-s"))
			{
				if(i < args.Length)
					treesize = Int32.Parse(args[i++]);
				else
					throw new Exception("-l requires the number of levels");
			}
			else if(arg.Equals("-p"))
				printResults = true;
			else if(arg.Equals("-m"))
				printMsgs = true;
			else if(arg.Equals("-h"))
				usage();
		}

		if(treesize == 0)
			usage();
	}

	/**
	 * The usage routine which describes the program options.
	 **/
	private static void usage()
	{
		Console.WriteLine("usage: java BiSort -s <size> [-p] [-i] [-h]");
		Console.WriteLine("    -s the number of values to sort");
		Console.WriteLine("    -m (print informative messages)");
		Console.WriteLine("    -h (print this message)");
		System.Environment.Exit(0);
	}
}

/*
treesize = 10
=>
11 10 37 29 42 21 88
8 10 11 21 29 37 42
88 42 37 29 21 11 10
*/


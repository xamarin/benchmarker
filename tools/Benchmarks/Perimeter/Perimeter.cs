using System;
using System.IO;
using System.Text;
using Common.Logging;

namespace Benchmarks.Perimeter
{
	/**
 * A Java version of the <tt>perimeter</tt> Olden benchmark.
 * <p/>
 * The algorithm computes the total perimeter of a region
 * in a binary image represented by a quadtree.  The
 * algorithm was presented in the paper:
 * <p/>
 * <cite>
 * Hanan Samet, "Computing Perimeters of Regions in Images
 * Represented by Quadtrees," IEEE Transactions on Pattern
 * Analysis and Machine Intelligence, PAMI-3(6), November, 1981.
 * </cite>
 * <p/>
 * The benchmark creates an image, count the number of leaves on the
 * quadtree and then computes the perimeter of the image using Samet's
 * algorithm.
 */
	public class Perimeter
	{
		static ILog logger;
		/**
	 * The number of levels in the tree/image.
	 **/
		private static int levels = 12;
		/**
	 * Set to true to print the final result.
	 **/
		private static bool printResult = true;

		/**
	 * The entry point to computing the perimeter of an image.
	 *
	 * @param args the command line arguments
	 */
		public static void Main (String[] args, ILog ilog)
		{
			logger = ilog;
			parseCmdLine (args);
			Quadrant.staticInitQuadrant ();

			int size = 1 << levels;
			int msize = 1 << (levels - 1);
			QuadTreeNode.gcmp = size * 1024;
			QuadTreeNode.lcmp = msize * 1024;

			QuadTreeNode tree = QuadTreeNode.createTree (msize, 0, 0, null, Quadrant.cSouthEast, levels);

			int leaves = tree.countTree ();
			int perm = tree.perimeter (size);

			if (printResult) {
				logger.InfoFormat ("Perimeter is " + perm);
				logger.InfoFormat ("Number of leaves " + leaves);
			}

			logger.InfoFormat ("Done!");
		}

		private static void parseCmdLine (String[] args)
		{
			int i = 0;
			String arg;

			while (i < args.Length && args [i].StartsWith ("-")) {
				arg = args [i++];

				if (arg.Equals ("-l")) {
					if (i < args.Length) {
						levels = System.Int32.Parse (args [i++]);
					} else {
						throw new Exception ("-l requires the number of levels");
					}
				} else if (arg.Equals ("-p")) {
					printResult = true;
				} else if (arg.Equals ("-h")) {
					usage ();
				}
			}
			if (levels == 0)
				usage ();
		}

		/**
	 * The usage routine which describes the program options.
	 **/
		private static void usage ()
		{
			logger.InfoFormat ("usage: java Perimeter -l <num> [-p] [-m] [-h]");
			logger.InfoFormat ("    -l number of levels in the quadtree (image size = 2^l)");
			logger.InfoFormat ("    -p (print the results)");
			logger.InfoFormat ("    -h (this message)");
			throw new Exception ("exit after usage info");
		}
	}
}
/*
levels = 12
=>
Perimeter is 27968
Number of leaves 83728
*/


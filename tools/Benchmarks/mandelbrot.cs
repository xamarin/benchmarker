/* The Computer Language Benchmarks Game
 *    http://shootout.alioth.debian.org/
 *
 * Adapted by Antti Lankila from the earlier Isaac Gouy's implementation
 */

using System;
using System.IO;
using Common.Logging;

namespace Benchmarks.Mandelbrot
{
	public class Mandelbrot
	{
		public static void Main (String[] args, ILog logger)
		{
			int width = 100;
			if (args.Length > 0)
				width = Int32.Parse (args [0]);

			int height = width;
			int maxiter = 50;
			double limit = 4.0;

			logger.InfoFormat ("P4");
			logger.InfoFormat ("{0} {1}", width, height);
			Stream s = Stream.Null;

			for (int y = 0; y < height; y++) {
				int bits = 0;
				int xcounter = 0;
				double Ci = 2.0 * y / height - 1.0;

				for (int x = 0; x < width; x++) {
					double Zr = 0.0;
					double Zi = 0.0;
					double Cr = 2.0 * x / width - 1.5;
					int i = maxiter;

					bits = bits << 1;
					do {
						double Tr = Zr * Zr - Zi * Zi + Cr;
						Zi = 2.0 * Zr * Zi + Ci;
						Zr = Tr;
						if (Zr * Zr + Zi * Zi > limit) {
							bits |= 1;
							break;
						}
					} while (--i > 0);

					if (++xcounter == 8) {
						s.WriteByte ((byte)(bits ^ 0xff));
						bits = 0;
						xcounter = 0;
					}
				}
				if (xcounter != 0)
					s.WriteByte ((byte)((bits << (8 - xcounter)) ^ 0xff));
			}
		}
	}
}

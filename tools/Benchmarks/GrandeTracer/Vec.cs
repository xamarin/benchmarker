using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/**
 * This class reflects the 3d vectors used in 3d computations
 */
namespace Benchmarks.GrandeTracer
{
	public class Vec
	{
		public double x;
		public double y;
		public double z;

		public Vec (double a, double b, double c)
		{
			x = a;
			y = b;
			z = c;
		}

		public Vec (Vec a)
		{
			x = a.x;
			y = a.y;
			z = a.z;
		}

		public Vec ()
		{
			x = 0.0;
			y = 0.0;
			z = 0.0;
		}

		public void add (Vec a)
		{
			x += a.x;
			y += a.y;
			z += a.z;
		}

		public static Vec adds (double s, Vec a, Vec b)
		{
			return new Vec (s * a.x + b.x, s * a.y + b.y, s * a.z + b.z);
		}

		public void adds (double s, Vec b)
		{
			x += s * b.x;
			y += s * b.y;
			z += s * b.z;
		}

		public static Vec sub (Vec a, Vec b)
		{
			return new Vec (a.x - b.x, a.y - b.y, a.z - b.z);
		}

		public void sub2 (Vec a, Vec b)
		{
			this.x = a.x - b.x;
			this.y = a.y - b.y;
			this.z = a.z - b.z;
		}

		public static Vec mult (Vec a, Vec b)
		{
			return new Vec (a.x * b.x, a.y * b.y, a.z * b.z);
		}

		public static Vec cross (Vec a, Vec b)
		{
			return
		  new Vec (a.y * b.z - a.z * b.y,
				a.z * b.x - a.x * b.z,
				a.x * b.y - a.y * b.x);
		}

		public static double dot (Vec a, Vec b)
		{
			return a.x * b.x + a.y * b.y + a.z * b.z;
		}

		public static Vec comb (double a, Vec A, double b, Vec B)
		{
			return
		  new Vec (a * A.x + b * B.x,
				a * A.y + b * B.y,
				a * A.z + b * B.z);
		}

		public void comb2 (double a, Vec A, double b, Vec B)
		{
			x = a * A.x + b * B.x;
			y = a * A.y + b * B.y;
			z = a * A.z + b * B.z;
		}

		public void scale (double t)
		{
			x *= t;
			y *= t;
			z *= t;
		}

		public void negate ()
		{
			x = -x;
			y = -y;
			z = -z;
		}

		public double normalize ()
		{
			double len;
			len = Math.Sqrt (x * x + y * y + z * z);
			if (len > 0.0) {
				x /= len;
				y /= len;
				z /= len;
			}
			return len;
		}

		public override String ToString ()
		{
			return "<" + x + "," + y + "," + z + ">";
		}
	}
}
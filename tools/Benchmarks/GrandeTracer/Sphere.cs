using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Benchmarks.GrandeTracer
{
	public class Sphere : Primitive
	{
		Vec c;
		double r, r2;
		Vec v, b;
		// temporary vecs used to minimize the memory load

		public Sphere (Vec center, double radius)
		{
			c = center;
			r = radius;
			r2 = r * r;
			v = new Vec ();
			b = new Vec ();
		}

		public override Isect intersect (Ray ry)
		{
			double b, disc, t;
			Isect ip;
			v.sub2 (c, ry.P);
			b = Vec.dot (v, ry.D);
			disc = b * b - Vec.dot (v, v) + r2;
			if (disc < 0.0) {
				return null;
			}
			disc = Math.Sqrt (disc);
			t = (b - disc < 1e-6) ? b + disc : b - disc;
			if (t < 1e-6) {
				return null;
			}
			ip = new Isect ();
			ip.t = t;
			ip.enter = Vec.dot (v, v) > r2 + 1e-6 ? 1 : 0;
			ip.prim = this;
			ip.surf = surf;
			return ip;
		}

		public override Vec normal (Vec p)
		{
			Vec r;
			r = Vec.sub (p, c);
			r.normalize ();
			return r;
		}

		public override String ToString ()
		{
			return "Sphere {" + c.ToString () + "," + r + "}";
		}

		public override Vec getCenter ()
		{
			return c;
		}

		public override void setCenter (Vec c)
		{
			this.c = c;
		}
	}
}
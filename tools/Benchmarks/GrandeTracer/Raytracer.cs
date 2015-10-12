using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Logging;

namespace Benchmarks.GrandeTracer
{
	public class RayTracer
	{
		Scene scene;
		Light[] lights;
		Primitive[] prim;
		View view;

		//Temporary ray
		Ray tRay = new Ray ();

		//Alpha channel
		const int alpha = 255 << 24;

		//Null vector (for speedup, instead of <code>new Vec(0,0,0)</code>
		static Vec voidVec = new Vec ();

		//Temporary vect
		Vec L = new Vec ();

		//Current intersection instance (only one is needed!)
		Isect inter = new Isect ();

		//Height of the Image to be rendered
		int height;

		//Width of the Image to be rendered
		int width;

		int[] datasizes = { 150, 500 };

		long checksum = 0;

		int size;

		int numobjects;

		Scene createScene ()
		{
			int x = 0;
			int y = 0;

			Scene scene = new Scene ();

			Primitive p;
			int nx = 4;
			int ny = 4;
			int nz = 4;
			for (int i = 0; i < nx; i++) {
				for (int j = 0; j < ny; j++) {
					for (int k = 0; k < nz; k++) {
						double xx = 20.0 / (nx - 1) * i - 10.0;
						double yy = 20.0 / (ny - 1) * j - 10.0;
						double zz = 20.0 / (nz - 1) * k - 10.0;

						p = new Sphere (new Vec (xx, yy, zz), 3);
						p.setColor (0, 0, (i + j) / (double)(nx + ny - 2));
						p.surf.shine = 15.0;
						p.surf.ks = 1.5 - 1.0;
						p.surf.kt = 1.5 - 1.0;
						scene.Objects.Add (p);
					}
				}
			}

			scene.Lights.Add (new Light (100, 100, -50, 1.0));
			scene.Lights.Add (new Light (-100, 100, -50, 1.0));
			scene.Lights.Add (new Light (100, -100, -50, 1.0));
			scene.Lights.Add (new Light (-100, -100, -50, 1.0));
			scene.Lights.Add (new Light (200, 200, 0, 1.0));

			View v = new View (new Vec (x, 20, -30), new Vec (x, y, 0), new Vec (0, 1, 0), 1.0, 35.0 * 3.14159265 / 180.0, 1.0);

			scene.RTView = v;

			return scene;
		}


		public void setScene (Scene scene)
		{
			// Get the objects count
			int nLights = scene.Lights.Count;
			int nObjects = scene.Objects.Count;

			lights = new Light[nLights];
			prim = new Primitive[nObjects];

			// Get the lights
			for (int l = 0; l < nLights; l++) {
				lights [l] = scene.Lights [l];
			}

			// Get the primitives
			for (int o = 0; o < nObjects; o++) {
				prim [o] = scene.Objects [o];
			}

			// Set the view
			view = scene.RTView;
		}

		public void render (Interval interval)
		{
			int[] row = new int[interval.width * (interval.yto - interval.yfrom)];
			int pixCounter = 0; //iterator

			int x, y, red, green, blue;
			double xlen, ylen;
			Vec viewVec;

			viewVec = Vec.sub (view.at, view.from);

			viewVec.normalize ();

			Vec tmpVec = new Vec (viewVec);
			tmpVec.scale (Vec.dot (view.up, viewVec));

			Vec upVec = Vec.sub (view.up, tmpVec);
			upVec.normalize ();

			Vec leftVec = Vec.cross (view.up, viewVec);
			leftVec.normalize ();

			double frustrumwidth = view.dist * Math.Tan (view.angle);

			upVec.scale (-frustrumwidth);
			leftVec.scale (view.aspect * frustrumwidth);

			Ray r = new Ray (view.from, voidVec);
			Vec col = new Vec ();

			// For each line
			for (y = interval.yfrom; y < interval.yto; y++) {
				ylen = (double)(2.0 * y) / (double)interval.width - 1.0;
				for (x = 0; x < interval.width; x++) {
					xlen = (double)(2.0 * x) / (double)interval.width - 1.0;
					r.D = Vec.comb (xlen, leftVec, ylen, upVec);
					r.D.add (viewVec);
					r.D.normalize ();
					col = trace (0, 1.0, r);

					// computes the color of the ray
					red = (int)(col.x * 255.0);
					if (red > 255)
						red = 255;
					green = (int)(col.y * 255.0);
					if (green > 255)
						green = 255;
					blue = (int)(col.z * 255.0);
					if (blue > 255)
						blue = 255;

					checksum += red;
					checksum += green;
					checksum += blue;

					// Sets the pixels
					row [pixCounter] = alpha | (red << 16) | (green << 8) | (blue);
					pixCounter++;
				}
			}

		}

		bool intersect (Ray r, double maxt)
		{
			Isect tp;
			int i, nhits;

			nhits = 0;
			inter.t = 1e9;
			for (i = 0; i < prim.Length; i++) {
				// uses global temporary Prim (tp) as temp.object for speedup
				tp = prim [i].intersect (r);
				if (tp != null && tp.t < inter.t) {
					inter.t = tp.t;
					inter.prim = tp.prim;
					inter.surf = tp.surf;
					inter.enter = tp.enter;
					nhits++;
				}
			}
			return nhits > 0 ? true : false;
		}

		/**
	 * Checks if there is a shadow
	 * @param r The ray
	 * @return Returns 1 if there is a shadow, 0 if there isn't
	 */
		int Shadow (Ray r, double tmax)
		{
			if (intersect (r, tmax))
				return 0;
			return 1;
		}

		/**
	 * Return the Vector's reflection direction
	 * @return The specular direction
	 */
		Vec SpecularDirection (Vec I, Vec N)
		{
			Vec r;
			r = Vec.comb (1.0 / Math.Abs (Vec.dot (I, N)), I, 2.0, N);
			r.normalize ();
			return r;
		}

		/**
	 * Return the Vector's transmission direction
	 */
		Vec TransDir (Surface m1, Surface m2, Vec I, Vec N)
		{
			double n1, n2, eta, c1, cs2;
			Vec r;
			n1 = m1 == null ? 1.0 : m1.ior;
			n2 = m2 == null ? 1.0 : m2.ior;
			eta = n1 / n2;
			c1 = -Vec.dot (I, N);
			cs2 = 1.0 - eta * eta * (1.0 - c1 * c1);
			if (cs2 < 0.0)
				return null;
			r = Vec.comb (eta, I, eta * c1 - Math.Sqrt (cs2), N);
			r.normalize ();
			return r;
		}

		/**
	 * Returns the shaded color
	 * @return The color in Vec form (rgb)
	 */
		Vec shade (int level, double weight, Vec P, Vec N, Vec I, Isect hit)
		{
			Vec tcol;
			Vec R;
			double t, diff, spec;
			Surface surf;
			Vec col;
			int l;

			col = new Vec ();
			surf = hit.surf;
			R = new Vec ();
			if (surf.shine > 1e-6) {
				R = SpecularDirection (I, N);
			}

			// Computes the effectof each light
			for (l = 0; l < lights.Length; l++) {
				L.sub2 (lights [l].pos, P);
				if (Vec.dot (N, L) >= 0.0) {
					t = L.normalize ();

					tRay.P = P;
					tRay.D = L;

					// Checks if there is a shadow
					if (Shadow (tRay, t) > 0) {
						diff = Vec.dot (N, L) * surf.kd *
						lights [l].brightness;

						col.adds (diff, surf.color);
						if (surf.shine > 1e-6) {
							spec = Vec.dot (R, L);
							if (spec > 1e-6) {
								spec = Math.Pow (spec, surf.shine);
								col.x += spec;
								col.y += spec;
								col.z += spec;
							}
						}
					}
				}
			}

			tRay.P = P;
			if (surf.ks * weight > 1e-3) {
				tRay.D = SpecularDirection (I, N);
				tcol = trace (level + 1, surf.ks * weight, tRay);
				col.adds (surf.ks, tcol);
			}
			if (surf.kt * weight > 1e-3) {
				if (hit.enter > 0)
					tRay.D = TransDir (null, surf, I, N);
				else
					tRay.D = TransDir (surf, null, I, N);
				tcol = trace (level + 1, surf.kt * weight, tRay);
				col.adds (surf.kt, tcol);
			}

			// garbaging...
			tcol = null;
			surf = null;

			return col;
		}

		/**
	 * Launches a ray
	 */
		Vec trace (int level, double weight, Ray r)
		{
			Vec P, N;
			bool hit;

			// Checks the recursion level
			if (level > 6) {
				return new Vec ();
			}

			hit = intersect (r, 1e6);
			if (hit) {
				P = r.point (inter.t);
				N = inter.prim.normal (P);
				if (Vec.dot (r.D, N) >= 0.0) {
					N.negate ();
				}
				return shade (level, weight, P, N, r.D, inter);
			}
			// no intersection --> col = 0,0,0
			return voidVec;
		}

		private void validate ()
		{
			long[] refval = new long[2];
			refval [0] = 2676692;
			refval [1] = 29827635;
			long dev = checksum - refval [size];
			if (dev != 0) {
				logger.InfoFormat ("Validation failed");
				logger.InfoFormat ("Pixel checksum = " + checksum);
				logger.InfoFormat ("Reference value = " + refval [size]);
			}
		}

		static ILog logger;
		public static void Main (String[] argv, ILog ilog)
		{
			logger = ilog;
			RayTracer rt = new RayTracer ();

			rt.size = 0;
			rt.width = rt.datasizes [rt.size];
			rt.height = rt.datasizes [rt.size];

			// create the objects to be rendered 
			rt.scene = rt.createScene ();

			// get lights, objects etc. from scene. 
			rt.setScene (rt.scene);

			rt.numobjects = rt.scene.Objects.Count;

			// Set interval to be rendered to the whole picture 
			// (overkill, but will be useful to retain this for parallel versions)
			Interval interval = new Interval (0, rt.width, rt.height, 0, rt.height, 1);

			// Do the business!
			rt.render (interval);

			rt.validate ();
		}
	}
}

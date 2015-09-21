
using System;
using System.Collections.Generic;

namespace Benchmarks.BH
{
/**
 * A class that represents the root of the data structure used
 * to represent the N-bodies in the Barnes-Hut algorithm.
 */
	public class BTree
	{
		public MathVector rmin;
		public double rsize;

		/**
	 * A reference to the root node.
	 */
		public Cell root;

		/**
	 * The complete list of bodies that have been created.
	 */
		public List<Body> bodyTab;
		/**
	 * The complete list of bodies that have been created - in reverse.
	 */
		private List<Body> bodyTabRev;

		/**
	 * Construct the root of the data structure that represents the N-bodies.
	 */

		public static BTree makeTreeX ()
		{
			BTree t = new BTree ();
			t.rmin = MathVector.makeMathVector ();
			t.rsize = -2.0 * -2.0;
			t.root = null;
			t.bodyTab = null;
			t.bodyTabRev = null;

			t.rmin.setValue (0, -2.0);
			t.rmin.setValue (1, -2.0);
			t.rmin.setValue (2, -2.0);

			return t;
		}

		/**
	 * Create the testdata used in the benchmark.
	 *
	 * @param nbody the number of bodies to create
	 */
		public void createTestData (int nbody)
		{
			MathVector cmr = MathVector.makeMathVector ();
			MathVector cmv = MathVector.makeMathVector ();

			bodyTab = new List<Body> ();

			double rsc = 3.0 * 3.1415 / 16.0;
			double vsc = Math.Sqrt (1.0 / rsc);
			double seed = 123.0;

			int k;
			for (int i = 0; i < nbody; i++) {
				Body p = Body.makeBody ();
				bodyTab.Add (p);
				p.mass = 1.0 / (double)nbody;

				seed = BH.myRand (seed);
				double t1 = BH.xRand (0.0, 0.999, seed);
				t1 = Math.Pow (t1, (-2.0 / 3.0)) - 1.0;
				double r = 1.0 / Math.Sqrt (t1);

				double coeff = 4.0;
				for (k = 0; k < MathVector.NDIM; k++) {
					seed = BH.myRand (seed);
					r = BH.xRand (0.0, 0.999, seed);
					p.pos.setValue (k, coeff * r);
				}

				cmr.addition (p.pos);

				double x = 0.0;
				double y = 0.0;
				do {
					seed = BH.myRand (seed);
					x = BH.xRand (0.0, 1.0, seed);
					seed = BH.myRand (seed);
					y = BH.xRand (0.0, 0.1, seed);
				} while(y > x * x * Math.Pow (1.0 - x * x, 3.5));

				double v = Math.Sqrt (2.0) * x / Math.Pow (1.0 + r * r, 0.25);

				double rad = vsc * v;
				double rsq = 0.0;
				do {
					for (k = 0; k < MathVector.NDIM; k++) {
						seed = BH.myRand (seed);
						p.vel.setValue (k, BH.xRand (-1.0, 1.0, seed));
					}
					rsq = p.vel.dotProduct ();
				} while(rsq > 1.0);
				double rsc1 = rad / Math.Sqrt (rsq);
				p.vel.multScalar1 (rsc1);
				cmv.addition (p.vel);
			}

			cmr.divScalar ((double)nbody);
			cmv.divScalar ((double)nbody);

			this.bodyTabRev = new List<Body> ();

			for (int j = 0; j < this.bodyTab.Count; ++j) {
				Body b = this.bodyTab [j];
				b.pos.subtraction1 (cmr);
				b.vel.subtraction1 (cmv);
				this.bodyTabRev.Add (b);
			}
		}


		/**
	 * Advance the N-body system one time-step.
	 *
	 * @param nstep the current time step
	 */
		public void stepSystem (int nstep)
		{
			// free the tree
			root = null;

			makeTree (nstep);

			// compute the gravity for all the particles
			for (int i = 0; i < this.bodyTabRev.Count; ++i)
				this.bodyTabRev [i].hackGravity (rsize, root);

			vp (bodyTabRev, nstep);
		}

		/**
	 * Initialize the tree structure for hack force calculation.
	 *
	 * @param nsteps the current time step
	 */
		private void makeTree (int nstep)
		{
			Body q0 = this.bodyTabRev [0];
			q0.expandBox (this, nstep);

			Body q1 = this.bodyTabRev [1];
			q1.expandBox (this, nstep);
			MathVector xqicinit = intcoord (q1.pos);
			this.root = q0.loadTree (q1, xqicinit, Node.IMAX >> 1, this);

			for (int i = 2; i < this.bodyTabRev.Count; ++i) {
				Body q = this.bodyTabRev [i];
				if (q.mass != 0.0) {
					q.expandBox (this, nstep);
					MathVector xqic = intcoord (q.pos);
					root = root.loadTree (q, xqic, Node.IMAX >> 1, this);
				}
			}
			root.hackcofm ();
		}

		/**
	 * Compute integerized coordinates.
	 *
	 * @return the coordinates or null if rp is out of bounds
	 */
		public MathVector intcoord (MathVector vp)
		{
			MathVector xp = MathVector.makeMathVector ();

			double imxv = (double)Node.IMAX;
			double xsc = (vp.value (0) - rmin.value (0)) / rsize;
			if (0.0 <= xsc && xsc < 1.0)
				xp.setValue (0, Math.Floor (imxv * xsc));
			else
				return null;

			xsc = (vp.value (1) - rmin.value (1)) / rsize;
			if (0.0 <= xsc && xsc < 1.0)
				xp.setValue (1, Math.Floor (imxv * xsc));
			else
				return null;

			xsc = (vp.value (2) - rmin.value (2)) / rsize;
			if (0.0 <= xsc && xsc < 1.0)
				xp.setValue (2, Math.Floor (imxv * xsc));
			else
				return null;

			return xp;
		}

		static private void vp (List<Body> bv, int nstep)
		{
			MathVector dacc = MathVector.makeMathVector ();
			MathVector dvel = MathVector.makeMathVector ();
			double dthf = 0.5 * BH.DTIME;

			for (int i = 0; i < bv.Count; ++i) {
				Body b = bv [i];
				MathVector acc1 = b.newAcc.cloneMathVector ();
				if (nstep > 0) {
					dacc.subtraction2 (acc1, b.acc);
					dvel.multScalar2 (dacc, dthf);
					dvel.addition (b.vel);
					b.vel = dvel.cloneMathVector ();
				}

				b.acc = acc1.cloneMathVector ();

				dvel.multScalar2 (b.acc, dthf);

				MathVector vel1 = b.vel.cloneMathVector ();
				vel1.addition (dvel);
				MathVector dpos = vel1.cloneMathVector ();
				dpos.multScalar1 (BH.DTIME);
				dpos.addition (b.pos);
				b.pos = dpos.cloneMathVector ();
				vel1.addition (dvel);
				b.vel = vel1.cloneMathVector ();
			}
		}
	}
}

namespace Benchmarks.BH
{
	/**
 * A class which is used to compute and save information during the
 * gravity computation phse.
 */
	public class HG
	{
		/**
	 * Body to skip in force evaluation
	 */
		public Body pskip;
		/**
	 * Point at which to evaluate field
	 */
		public MathVector pos0;
		/**
	 * Computed potential at pos0
	 */
		public double phi0;
		/**
	 * computed acceleration at pos0
	 */
		public MathVector acc0;

		/**
	 * Create a HG  object.
	 *
	 * @param b the body object
	 * @param p a vector that represents the body
	 */
		public static HG makeHG (Body b, MathVector p)
		{
			HG hg = new HG ();
			hg.pskip = b;
			hg.pos0 = p.cloneMathVector ();
			hg.phi0 = 0.0;
			hg.acc0 = MathVector.makeMathVector ();
			return hg;
		}
	}
}

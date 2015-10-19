/*
 * @(#)Vector.java	1.4 06/17/98
 *
 * Vector.java
 * Implements the functionality of a 3-space vector.
 *
 * Copyright (c) 1998 Standard Performance Evaluation Corporation (SPEC)
 *               All rights reserved.
 * Copyright (c) 1996,1997,1998 Sun Microsystems, Inc. All rights reserved.
 *
 * This source code is provided as is, without any express or implied warranty.
 */

namespace Benchmarks.SpecRaytracer
{
	/**
 * class Vector
 */
	public class Vector : Point
	{
		/**
	 * Vector
	 */
		public Vector ()
			: base ()
		{
			Set (0.0f, 0.0f, 0.0f);
		}

		/**
	 * Vector
	 *
	 * @param newX
	 * @param newY
	 * @param newZ
	 */
		public Vector (double newX, double newY, double newZ)
			: base (newX, newY, newZ)
		{
			;
		}

		/**
	 * Vector
	 *
	 * @param newvec
	 */
		public Vector (Vector newvec)
		{
			Set (newvec.GetX (), newvec.GetY (), newvec.GetZ ());
		}

		/**
	 * Cross
	 *
	 * @param op1
	 * @param op2
	 * @return Vector
	 */
		public Vector Cross (Vector op1, Vector op2)
		{
			Set ((op1.GetY () * op2.GetZ ()) - (op1.GetZ () * op2.GetY ()),
				(op1.GetZ () * op2.GetX ()) - (op1.GetX () * op2.GetZ ()),
				(op1.GetX () * op2.GetY ()) - (op1.GetY () * op2.GetX ()));
			return (this);
		}

		/**
	 * Scale
	 *
	 * @param newlen
	 * @return Vector
	 */
		public Vector Scale (double newlen)
		{
			double len = Length ();
			if (len != 0.0f) {
				double factor = newlen / len;
				Set (GetX () * factor, GetY () * factor, GetZ () * factor);
			}
			return (this);
		}

		/**
	 * Negate
	 *
	 * @return Vector
	 */
		public Vector Negate ()
		{
			Set (-GetX (), -GetY (), -GetZ ());
			return (this);
		}

		/**
	 * Normalize
	 *
	 * @return Vector
	 */
		public Vector Normalize ()
		{
			return (Scale (1.0f));
		}

		/**
	 * Sub
	 *
	 * @param op1
	 * @param op2
	 * @return Vector
	 */
		public Vector Sub (Point op1, Point op2)
		{
			Set (op1.GetX () - op2.GetX (), op1.GetY () - op2.GetY (), op1.GetZ () - op2.GetZ ());
			return (this);
		}

		/**
	 * Dot
	 *
	 * @param operand
	 * @return double
	 */
		public double Dot (Vector operand)
		{
			return (GetX () * operand.GetX () + GetY () * operand.GetY () + GetZ () * operand.GetZ ());
		}

		/**
	 * SquaredLength
	 *
	 * @return double
	 */
		public double SquaredLength ()
		{
			return (GetX () * GetX () + GetY () * GetY () + GetZ () * GetZ ());
		}

		/**
	 * Length
	 *
	 * @return double
	 */
		public double Length ()
		{
			return ((double)System.Math.Sqrt (SquaredLength ()));
		}
	}
}

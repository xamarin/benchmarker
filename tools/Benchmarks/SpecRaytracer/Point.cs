/*
 * @(#)Point.java	1.4 06/17/98
 *
 * Point.java
 * Holds the coordinates of a point in 3-space. Provides some additional
 * functionality in finding a new point from some operators.
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
 * class Point
 */
	public class Point
	{
		private double x;
		private double y;
		private double z;

		/**
	 * Point
	 */
		public Point ()
		{
			x = 0.0f;
			y = 0.0f;
			z = 0.0f;
		}

		/**
	 * Point
	 *
	 * @param newX
	 * @param newY
	 * @param newZ
	 */
		public Point (double newX, double newY, double newZ)
		{
			x = newX;
			y = newY;
			z = newZ;
		}

		/**
	 * Point
	 *
	 * @param newpoint
	 */
		public Point (Point newpoint)
		{
			Set (newpoint.GetX (), newpoint.GetY (), newpoint.GetZ ());
		}

		/**
	 * Set
	 *
	 * @param newX
	 * @param newY
	 * @param newZ
	 */
		public void Set (double newX, double newY, double newZ)
		{
			x = newX;
			y = newY;
			z = newZ;
		}

		/**
	 * GetX
	 *
	 * @return double
	 */
		public double GetX ()
		{
			return (x);
		}

		/**
	 * GetY
	 *
	 * @return double
	 */
		public double GetY ()
		{
			return (y);
		}

		/**
	 * GetZ
	 *
	 * @return double
	 */
		public double GetZ ()
		{
			return (z);
		}

		/**
	 * SetX
	 *
	 * @param newx
	 */
		public void SetX (double newx)
		{
			x = newx;
		}

		/**
	 * SetY
	 *
	 * @param newy
	 */
		public void SetY (double newy)
		{
			y = newy;
		}

		/**
	 * SetZ
	 *
	 * @param newz
	 */
		public void SetZ (double newz)
		{
			z = newz;
		}

		/**
	 * FindCorner
	 *
	 * @param view
	 * @param up
	 * @param plane
	 * @param origin
	 */
		public void FindCorner (Vector view, Vector up, Vector plane, Point origin)
		{
			x = origin.GetX ();
			y = origin.GetY ();
			z = origin.GetZ ();
			Add (view);
			Add (up);
			Add (plane);
		}

		/**
	 * Add
	 *
	 * @param added
	 * @return Point
	 */
		public Point Add (Vector added)
		{
			x += added.GetX ();
			y += added.GetY ();
			z += added.GetZ ();
			return (this);
		}

		/**
	 * Combine
	 *
	 * @param pt
	 * @param vector
	 * @param ptscale
	 * @param vecscale
	 * @return Point
	 */
		public Point Combine (Point pt, Vector vector, double ptscale, double vecscale)
		{
			x = ptscale * pt.GetX () + vecscale * vector.GetX ();
			y = ptscale * pt.GetY () + vecscale * vector.GetY ();
			z = ptscale * pt.GetZ () + vecscale * vector.GetZ ();
			return (this);
		}
	}
}

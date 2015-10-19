/*
 * @(#)Face.java	1.4 06/17/98
 *
 * Face.java
 * The class to hold the coordinates of a face of the octree cubes used in
 * raytracing.
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
 * class Face
 */
	public class Face
	{
		private Point[] Verts;

		/**
	 * Face
	 */
		public Face ()
		{
			Verts = new Point[4];
		}

		/**
	 * SetVert
	 *
	 * @param newVert
	 * @param index
	 */
		public void SetVert (Point newVert, int index)
		{
			Verts [index] = newVert;
		}

		/**
	 * GetVert
	 *
	 * @param index
	 * @return Point
	 */
		public Point GetVert (int index)
		{
			return (Verts [index]);
		}
	}
}

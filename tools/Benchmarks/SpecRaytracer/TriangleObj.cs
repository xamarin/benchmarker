/*
 * @(#)TriangleObj.java	1.3 06/17/98
 *
 * TriangleObj.java
 * The class for a triangle. It has a function which tells if a point lies
 * within the triangle.
 *
 * Copyright (c) 1998 Standard Performance Evaluation Corporation (SPEC)
 *               All rights reserved.
 * Copyright (c) 1996,1997,1998 Sun Microsystems, Inc. All rights reserved.
 *
 * This source code is provided as is, without any express or implied warranty.
 */

//import IntersectPt;
//import Material;
//import Point;
//import PolyTypeObj;
//import Ray;
//import Vector;

namespace Benchmarks.SpecRaytracer
{
	/**
 * class TriangleObj
 */
	public class TriangleObj : PolyTypeObj
	{
		private Vector S1;
		private Vector S2;
		private Vector S3;

		/**
	 * Check
	 *
	 * @param ray
	 * @param pt
	 * @return boolean
	 */
		protected override bool Check (Ray ray, IntersectPt pt)
		{
			Vector intersectVec = new Vector (pt.GetIntersection ().GetX (), pt.GetIntersection ().GetY (), pt.GetIntersection ().GetZ ());
			double check = S1.Dot (intersectVec);
			if (check < 0.0f || check > 1.0f) {
				return (false);
			}
			check = S2.Dot (intersectVec);
			if (check < 0.0f || check > 1.0f) {
				return (false);
			}
			check = S3.Dot (intersectVec);
			if (check < 0.0f || check > 1.0f) {
				return (false);
			}
			return (true);
		}

		/**
	 * TriangleObj
	 *
	 * @param objmaterial
	 * @param newobjID
	 * @param numverts
	 * @param vertices
	 * @param MaxX
	 * @param MinX
	 * @param MaxY
	 * @param MinY
	 * @param MaxZ
	 * @param MinZ
	 */
		public TriangleObj (Material objmaterial, int newobjID, int numverts, Point[] vertices, Point max, Point min)
			: base (objmaterial, newobjID, numverts, vertices, max, min)
		{
			Vector[] temp = new Vector[3];
			for (int i = 0; i < 3; i++) {
				temp [i] = new Vector (vertices [i].GetX (), vertices [i].GetY (), vertices [i].GetZ ());
			}

			S1 = new Vector ();
			S2 = new Vector ();
			S3 = new Vector ();
			S1.Cross (temp [1], temp [2]);
			S2.Cross (temp [2], temp [0]);
			S3.Cross (temp [0], temp [1]);
			double delta = 1.0f / S1.Dot (temp [0]);
			S1.Scale (delta * S1.Length ());
			S2.Scale (delta * S2.Length ());
			S3.Scale (delta * S3.Length ());
		}
	}
}

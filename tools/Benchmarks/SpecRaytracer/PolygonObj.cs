/*
 * @(#)PolygonObj.java	1.3 06/17/98
 *
 * Polygon.java
 * The class for all planar polygons with 4 or more vertices. Does the actual
 * testing of whether a point lies in polygon or not.
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
 * class PolygonObj
 */
	public class PolygonObj : PolyTypeObj
	{
		private int MaxComp;

		/**
	 * Check
	 *
	 * @param ray
	 * @param pt
	 * @return boolean
	 */
		protected override bool Check (Ray ray, IntersectPt pt)
		{
			return (InsidePolygon (GetVerts (), GetNumVerts (), pt.GetIntersection (), ray));
		}

		/**
	 * InsidePolygon
	 *
	 * @param verts
	 * @param num
	 * @param pt
	 * @param ray
	 * @return boolean
	 */
		private bool InsidePolygon (Point[] verts, int num, Point pt, Ray ray)
		{
			int cross = 0;
			int xindex, yindex, index = 0;
			double xtest, ytest, x0, y0, x1, y1;

			if (MaxComp == 0) {
				xindex = 1;
				yindex = 2;
				xtest = pt.GetY ();
				ytest = pt.GetZ ();
			} else if (MaxComp == 1) {
				xindex = 0;
				yindex = 2;
				xtest = pt.GetX ();
				ytest = pt.GetZ ();
			} else {
				xindex = 0;
				yindex = 1;
				xtest = pt.GetX ();
				ytest = pt.GetY ();
			}
			x0 = GetCoord (verts [num - 1], xindex) - xtest;
			y0 = GetCoord (verts [num - 1], yindex) - ytest;
			while (num-- != 0) {
				x1 = GetCoord (verts [index], xindex) - xtest;
				y1 = GetCoord (verts [index], yindex) - ytest;
				if (y0 > 0.0f) {
					if (y1 <= 0.0f) {
						if (x1 * y0 > y1 * x0) {
							cross++;
						}
					}
				} else {
					if (y1 > 0.0f) {
						if (x0 * y1 > y0 * x1) {
							cross++;
						}
					}
				}
				x0 = x1;
				y0 = y1;
				index++;
			}
			return ((cross & 1) == 1);
		}

		/**
	 * GetCoord
	 *
	 * @param pt
	 * @param index
	 * @return double
	 */
		private double GetCoord (Point pt, int index)
		{
			if (index == 0)
				return (pt.GetX ());
			else if (index == 1)
				return (pt.GetY ());
			else
				return (pt.GetZ ());
		}

		/**
	 * PolygonObj
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
		public PolygonObj (Material objmaterial, int newobjID, int numverts, Point[] vertices, Point max, Point min)
			: base (objmaterial, newobjID, numverts, vertices, max, min)
		{
			double x = System.Math.Abs (GetNormal ().GetX ());
			double y = System.Math.Abs (GetNormal ().GetY ());
			double z = System.Math.Abs (GetNormal ().GetZ ());
			if (x >= y && x >= z) {
				MaxComp = 0;
			} else if (y >= z) {
				MaxComp = 1;
			} else {
				MaxComp = 2;
			}
		}
	}
}

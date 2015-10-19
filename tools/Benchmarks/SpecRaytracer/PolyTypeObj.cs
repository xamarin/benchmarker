/*
 * @(#)PolyTypeObj.java	1.4 06/17/98
 *
 * PolyType.java
 * The abstract class for all planar polygonal objects. Holds the vertices and
 * the normal of the polygon. Implements the intersection test for the polygon.
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
 * class PolyTypeObj
 */
	public abstract class PolyTypeObj : ObjectType
	{
		private int numVertices;
		private Point[] Vertices;
		private Vector Normal;
		private double D;

		/**
	 * Check
	 *
	 * @param ray
	 * @param pt
	 * @return boolean
	 */
		protected abstract bool Check (Ray ray, IntersectPt pt);

		/**
	 * CalculateNormal
	 */
		private void CalculateNormal ()
		{
			Vector ba = new Vector ();
			Vector bc = new Vector ();

			Normal = new Vector ();
			ba.Sub (Vertices [0], Vertices [1]);
			bc.Sub (Vertices [2], Vertices [1]);
			Normal.Cross (bc, ba);
			Normal.Normalize ();
		}

		/**
	 * PolyTypeObj
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
		protected PolyTypeObj (Material objmaterial, int newobjID, int numverts, Point[] vertices,
		                     Point max, Point min)
			: base (objmaterial, newobjID)
		{
			numVertices = numverts;
			Vertices = vertices;

			CalculateNormal ();
			Vector temp = new Vector (Vertices [0].GetX (), Vertices [0].GetY (), Vertices [0].GetZ ());
			D = -Normal.Dot (temp);
			GetMax ().Set (Vertices [0].GetX (), Vertices [0].GetY (), Vertices [0].GetZ ());
			GetMin ().Set (Vertices [0].GetX (), Vertices [0].GetY (), Vertices [0].GetZ ());
			for (int i = 1; i < numVertices; i++) {
				if (Vertices [i].GetX () > GetMax ().GetX ()) {
					GetMax ().SetX (Vertices [i].GetX ());
				} else if (Vertices [i].GetX () < GetMin ().GetX ()) {
					GetMin ().SetX (Vertices [i].GetX ());
				}
				if (Vertices [i].GetY () > GetMax ().GetY ()) {
					GetMax ().SetY (Vertices [i].GetY ());
				} else if (Vertices [i].GetY () < GetMin ().GetY ()) {
					GetMin ().SetY (Vertices [i].GetY ());
				}
				if (Vertices [i].GetZ () > GetMax ().GetZ ()) {
					GetMax ().SetZ (Vertices [i].GetZ ());
				} else if (Vertices [i].GetZ () < GetMin ().GetZ ()) {
					GetMin ().SetZ (Vertices [i].GetZ ());
				}
			}
			if (GetMax ().GetX () > max.GetX ()) {
				max.SetX (GetMax ().GetX ());
			}
			if (GetMax ().GetY () > max.GetY ()) {
				max.SetY (GetMax ().GetY ());
			}
			if (GetMax ().GetZ () > max.GetZ ()) {
				max.SetZ (GetMax ().GetZ ());
			}
			if (GetMin ().GetX () < min.GetX ()) {
				min.SetX (GetMin ().GetX ());
			}
			if (GetMin ().GetY () < min.GetY ()) {
				min.SetY (GetMin ().GetY ());
			}
			if (GetMin ().GetZ () < min.GetZ ()) {
				min.SetZ (GetMin ().GetZ ());
			}
		}

		/**
	 * GetNumVerts
	 *
	 * @return int
	 */
		public int GetNumVerts ()
		{
			return (numVertices);
		}

		/**
	 * GetVerts
	 *
	 * @return Point
	 */
		public Point[] GetVerts ()
		{
			return (Vertices);
		}

		/**
	 * GetNormal
	 *
	 * @return Vector
	 */
		public Vector GetNormal ()
		{
			return (Normal);
		}

		/**
	 * Intersect
	 *
	 * @param ray
	 * @param pt
	 * @return boolean
	 */
		public override bool Intersect (Ray ray, IntersectPt pt)
		{
			double vd = Normal.Dot (ray.GetDirection ());
			double vo, t;
			Vector origVec = new Vector (ray.GetOrigin ().GetX (), ray.GetOrigin ().GetY (), ray.GetOrigin ().GetZ ());

			if (vd == 0.0f) {
				return (false);
			}
			vo = -Normal.Dot (origVec) - D;
			t = vo / vd;
			if (t < pt.GetThreshold ()) {
				return (false);
			}
			pt.GetIntersection ().Combine (ray.GetOrigin (), ray.GetDirection (), 1.0f, t);
			if (!Check (ray, pt)) {
				return (false);
			}
			pt.SetT (t);
			pt.SetIntersectObj (this);
			if (GetObjID () == pt.GetOriginal ()) {
				pt.SetEnter (false);
			} else {
				pt.SetEnter (true);
			}
			GetCachePt ().Set (ray.GetID (), pt);
			return (true);
		}

		/**
	 * FindNormal
	 *
	 * @param point
	 * @param normal
	 */
		public override void FindNormal (Point point, Vector normal)
		{
			normal.Set (Normal.GetX (), Normal.GetY (), Normal.GetZ ());
		}
	}
}

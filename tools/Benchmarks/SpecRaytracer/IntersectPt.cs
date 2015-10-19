/*
 * @(#)IntersectPt.java	1.4 06/17/98
 *
 * IntersectPt.java
 * This holds a ray intersection point with an object, as well as the t
 * parameter of the ray when intersecting the object, whether it is entering
 * or leaving the object, the intersected object and the original object from
 * which the ray came. Does the intersection test on a list of objects.
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
 * class IntersectPt
 */
	public class IntersectPt
	{
		private double t;
		private bool Enter;
		private ObjectType IntersectObj;
		private int OriginalObjID;
		private Point Intersection;
		private static double Threshold = 1.0e-4f;

		/**
	 * SetIsectPt
	 *
	 * @param isectpt
	 */
		protected void SetIsectPt (IntersectPt isectpt)
		{
			t = isectpt.GetT ();
			Enter = isectpt.GetEnter ();
			IntersectObj = isectpt.GetIntersectObj ();
			OriginalObjID = isectpt.GetOriginal ();
			Intersection.Set (isectpt.GetIntersection ().GetX (), isectpt.GetIntersection ().GetY (), isectpt.GetIntersection ().GetZ ());
		}

		/**
	 * IntersectPt
	 */
		public IntersectPt ()
		{
			t = 0.0f;
			Intersection = new Point ();
		}

		/**
	 * FindNearestIsect
	 *
	 * @param octree
	 * @param ray
	 * @param originID
	 * @param level
	 * @param isectnode
	 * @return boolean
	 */
		public bool FindNearestIsect (OctNode octree, Ray ray, int originID, int level, OctNode isectnode)
		{
			Point testpt = new Point (ray.GetOrigin ());
			OctNode current;
			ObjNode currentnode;
			CacheIntersectPt isectptr;
			IntersectPt test = new IntersectPt ();

			if (level == 0) {
				testpt.Combine (ray.GetOrigin (), ray.GetDirection (), 1.0f, Threshold);
			}

			current = octree.FindTreeNode (testpt);
			IntersectObj = null;
			while (current != null) {
				currentnode = current.GetList ();
				while (currentnode != null) {
					bool found = false;
					if (currentnode.GetObj ().GetCachePt ().GetID () == ray.GetID ()) {
						isectptr = currentnode.GetObj ().GetCachePt ();
						if (current == current.FindTreeNode (isectptr.GetIntersection ())) {
							if (IntersectObj == null) {
								SetIsectPt (isectptr);
								isectnode.Copy (current);
							} else {
								if (isectptr.GetT () < t) {
									SetIsectPt (isectptr);
									isectnode.Copy (current);
								}
							}
							found = true;
						}
					}
					if (!found) {
						test.SetOrigID (originID);
						if (currentnode.GetObj ().Intersect (ray, test)) {
							if (current == current.FindTreeNode (test.GetIntersection ())) {
								if (IntersectObj == null) {
									SetIsectPt (test);
									isectnode.Copy (current);
								} else {
									if (test.GetT () < t) {
										SetIsectPt (test);
										isectnode.Copy (current);
									}
								}
							}
						}
					}
					currentnode = currentnode.Next ();
				}
				if (IntersectObj == null) {
					OctNode adjacent = current.Intersect (ray, testpt, Threshold);
					if (adjacent == null) {
						current = null;
					} else {
						current = adjacent.FindTreeNode (testpt);
					}
				} else {
					current = null;
				}
			}
			if (IntersectObj == null) {
				return (false);
			} else {
				return (true);
			}
		}

		/**
	 * GetT
	 *
	 * @return double
	 */
		public double GetT ()
		{
			return (t);
		}

		/**
	 * GetEnter
	 *
	 * @return boolean
	 */
		public bool GetEnter ()
		{
			return (Enter);
		}

		/**
	 * GetIntersectObj
	 *
	 * @return ObjectType
	 */
		public ObjectType GetIntersectObj ()
		{
			return (IntersectObj);
		}

		/**
	 * GetOriginal
	 *
	 * @return int
	 */
		public int GetOriginal ()
		{
			return (OriginalObjID);
		}

		/**
	 * GetIntersection
	 *
	 * @return Point
	 */
		public Point GetIntersection ()
		{
			return (Intersection);
		}

		/**
	 * GetThreshold
	 *
	 * @return double
	 */
		public double GetThreshold ()
		{
			return (Threshold);
		}

		/**
	 * SetT
	 *
	 * @param newT
	 */
		public void SetT (double newT)
		{
			t = newT;
		}

		/**
	 * SetEnter
	 *
	 * @param enter
	 */
		public void SetEnter (bool enter)
		{
			Enter = enter;
		}

		/**
	 * SetIntersectObj
	 *
	 * @param newObj
	 */
		public void SetIntersectObj (ObjectType newObj)
		{
			IntersectObj = newObj;
		}

		/**
	 * SetOrigID
	 *
	 * @param ID
	 */
		public void SetOrigID (int ID)
		{
			OriginalObjID = ID;
		}
	}
}

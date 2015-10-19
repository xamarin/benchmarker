/*
 * @(#)ObjectType.java	1.4 06/17/98
 *
 * ObjectType.java
 * The base class for all objects. Holds the material for the object, its
 * ID and bounding box and the ray intersection cache.
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
 * class ObjectType
 */
	public abstract class ObjectType
	{
		private Material objMaterial;
		private int objID;
		private Point Max;
		private Point Min;
		private CacheIntersectPt IntersectCache;

		/**
	 * ObjectType
	 *
	 * @param newmaterial
	 * @param newID
	 */
		protected ObjectType (Material newmaterial, int newID)
		{
			objMaterial = newmaterial;
			objID = newID;

			Max = new Point ();
			Min = new Point ();
			IntersectCache = new CacheIntersectPt ();
		}

		/**
	 * GetMax
	 *
	 * @return Point
	 */
		public Point GetMax ()
		{
			return (Max);
		}

		/**
	 * GetMin
	 *
	 * @return Point
	 */
		public Point GetMin ()
		{
			return (Min);
		}

		/**
	 * GetCachePt
	 *
	 * @return CacheIntersectPt
	 */
		public CacheIntersectPt GetCachePt ()
		{
			return (IntersectCache);
		}

		/**
	 * GetMaterial
	 *
	 * @return Material
	 */
		public Material GetMaterial ()
		{
			return (objMaterial);
		}

		/**
	 * GetObjID
	 *
	 * @return int
	 */
		public int GetObjID ()
		{
			return (objID);
		}

		/**
	 * Intersect
	 *
	 * @param ray
	 * @param pt
	 * @return boolean
	 */
		public abstract bool Intersect (Ray ray, IntersectPt pt);

		/**
	 * FindNormal
	 *
	 * @param point
	 * @param normal
	 */
		public abstract void FindNormal (Point point, Vector normal);
	}
}

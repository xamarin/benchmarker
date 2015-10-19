/*
 * CacheIntersectPt.java
 * @(#)CacheIntersectPt.java	1.4 06/17/98
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
 * class CacheIntersectPt
 */
	public class CacheIntersectPt : IntersectPt
	{
		private int RayID;

		/**
	 * CacheIntersectPt
	 */
		public CacheIntersectPt ()
			: base ()
		{
			RayID = 0;
		}

		/**
	 * GetID
	 *
	 * @return int
	 */
		public int GetID ()
		{
			return (RayID);
		}

		/**
	 * Set
	 *
	 * @param newID
	 * @param newIntersect
	 */
		public void Set (int newID, IntersectPt newIntersect)
		{
			RayID = newID;
			SetIsectPt (newIntersect);
		}
	}
}

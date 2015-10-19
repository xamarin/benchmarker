/*
 * @(#)OctNode.java	1.4 06/17/98
 *
 * OctNode.java
 * Implements an octree node for speeding up the raytracing algorithm. Provides
 * a way of doing space subdivision. Holds pointers to adjacent nodes and the
 * child nodes of the octree node. Holds the linked list of objects in the
 * octree node and the number of objects.
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
 * class OctNode
 */
	public class OctNode
	{
		private OctNode[] Adjacent;
		private Face[] OctFaces;
		private OctNode[] Child;
		private ObjNode ObjList;
		private int NumObj;
		private const int MaxObj = 15;
		private const int MaxDepth = 10;
		private const int MAXX = 0;
		private const int MAXY = 1;
		private const int MAXZ = 2;
		private const int MINX = 3;
		private const int MINY = 4;
		private const int MINZ = 5;

		/**
	 * Initialize
	 */
		private void Initialize ()
		{
			int i;

			Adjacent = new OctNode[6];
			OctFaces = new Face[6];
			Child = new OctNode[8];
			for (i = 0; i < 6; i++) {
				Adjacent [i] = null;
			}
			for (i = 0; i < 8; i++) {
				Child [i] = null;
			}
			ObjList = null;
			NumObj = 0;
		}

		/**
	 * CreateFaces
	 *
	 * @param maxX
	 * @param minX
	 * @param maxY
	 * @param minY
	 * @param maxZ
	 * @param minZ
	 */
		private void CreateFaces (double maxX, double minX, double maxY, double minY, double maxZ, double minZ)
		{
			Point temp;

			for (int i = 0; i < 6; i++) {
				OctFaces [i] = new Face ();
			}

			Face[] OFaces = this.OctFaces;
			Face MAXXF = OFaces [MAXX];
			Face MAXYF = OFaces [MAXY];
			Face MAXZF = OFaces [MAXZ];
			Face MINXF = OFaces [MINX];
			Face MINYF = OFaces [MINY];
			Face MINZF = OFaces [MINZ];

			temp = new Point (maxX, maxY, maxZ);
			MAXXF.SetVert (temp, 0);
			temp = new Point (maxX, minY, maxZ);
			MAXXF.SetVert (temp, 1);
			temp = new Point (maxX, minY, minZ);
			MAXXF.SetVert (temp, 2);
			temp = new Point (maxX, maxY, minZ);
			MAXXF.SetVert (temp, 3);
			temp = new Point (minX, maxY, maxZ);
			MAXYF.SetVert (temp, 0);
			temp = new Point (maxX, maxY, maxZ);
			MAXYF.SetVert (temp, 1);
			temp = new Point (maxX, maxY, minZ);
			MAXYF.SetVert (temp, 2);
			temp = new Point (minX, maxY, minZ);
			MAXYF.SetVert (temp, 3);
			temp = new Point (maxX, maxY, maxZ);
			MAXZF.SetVert (temp, 0);
			temp = new Point (minX, maxY, maxZ);
			MAXZF.SetVert (temp, 1);
			temp = new Point (minX, minY, maxZ);
			MAXZF.SetVert (temp, 2);
			temp = new Point (maxX, minY, maxZ);
			MAXZF.SetVert (temp, 3);
			temp = new Point (minX, minY, maxZ);
			MINXF.SetVert (temp, 0);
			temp = new Point (minX, maxY, maxZ);
			MINXF.SetVert (temp, 1);
			temp = new Point (minX, maxY, minZ);
			MINXF.SetVert (temp, 2);
			temp = new Point (minX, minY, minZ);
			MINXF.SetVert (temp, 3);
			temp = new Point (maxX, minY, maxZ);
			MINYF.SetVert (temp, 0);
			temp = new Point (minX, minY, maxZ);
			MINYF.SetVert (temp, 1);
			temp = new Point (minX, minY, minZ);
			MINYF.SetVert (temp, 2);
			temp = new Point (maxX, minY, minZ);
			MINYF.SetVert (temp, 3);
			temp = new Point (minX, maxY, minZ);
			MINZF.SetVert (temp, 0);
			temp = new Point (maxX, maxY, minZ);
			MINZF.SetVert (temp, 1);
			temp = new Point (maxX, minY, minZ);
			MINZF.SetVert (temp, 2);
			temp = new Point (minX, minY, minZ);
			MINZF.SetVert (temp, 3);
		}

		/**
	 * CreateTree
	 *
	 * @param objects
	 * @param numObjects
	 */
		private void CreateTree (ObjNode objects, int numObjects)
		{
			if (objects != null) {
				if (numObjects > MaxObj) {
					CreateChildren (objects, 1);
				} else {
					ObjNode currentObj = objects;
					while (currentObj != null) {
						ObjNode newnode = new ObjNode (currentObj.GetObj (), ObjList);
						ObjList = newnode;
						currentObj = currentObj.Next ();
					}
					NumObj = numObjects;
				}
			}
		}

		/**
	 * CreateChildren
	 *
	 * @param objects
	 * @param depth
	 */
		private void CreateChildren (ObjNode objects, int depth)
		{

			double maxX = OctFaces [MAXX].GetVert (0).GetX ();
			double minX = OctFaces [MINX].GetVert (0).GetX ();
			double maxY = OctFaces [MAXY].GetVert (0).GetY ();
			double minY = OctFaces [MINY].GetVert (0).GetY ();
			double maxZ = OctFaces [MAXZ].GetVert (0).GetZ ();
			double minZ = OctFaces [MINZ].GetVert (0).GetZ ();
			Point midpt = new Point ((maxX + minX) / 2.0f, (maxY + minY) / 2.0f, (maxZ + minZ) / 2.0f);
			Point max = new Point ();
			Point min = new Point ();
			ObjNode currentnode;
			int i;

			max.Set (maxX, maxY, maxZ);
			min.Set (midpt.GetX (), midpt.GetY (), midpt.GetZ ());
			Child [0] = new OctNode (max, min);
			max.Set (maxX, midpt.GetY (), maxZ);
			min.Set (midpt.GetX (), minY, midpt.GetZ ());
			Child [1] = new OctNode (max, min);
			max.Set (maxX, midpt.GetY (), midpt.GetZ ());
			min.Set (midpt.GetX (), minY, minZ);
			Child [2] = new OctNode (max, min);
			max.Set (maxX, maxY, midpt.GetZ ());
			min.Set (midpt.GetX (), midpt.GetY (), minZ);
			Child [3] = new OctNode (max, min);
			max.Set (midpt.GetX (), maxY, maxZ);
			min.Set (minX, midpt.GetY (), midpt.GetZ ());
			Child [4] = new OctNode (max, min);
			max.Set (midpt.GetX (), midpt.GetY (), maxZ);
			min.Set (minX, minY, midpt.GetZ ());
			Child [5] = new OctNode (max, min);
			max.Set (midpt.GetX (), midpt.GetY (), midpt.GetZ ());
			min.Set (minX, minY, minZ);
			Child [6] = new OctNode (max, min);
			max.Set (midpt.GetX (), maxY, midpt.GetZ ());
			min.Set (minX, midpt.GetY (), minZ);
			Child [7] = new OctNode (max, min);

			OctNode[] adj = this.Adjacent;
			OctNode[] chld = this.Child;

			OctNode adj0 = adj [0];
			OctNode adj1 = adj [1];
			OctNode adj2 = adj [2];
			OctNode adj3 = adj [3];
			OctNode adj4 = adj [4];
			OctNode adj5 = adj [5];

			OctNode chld0 = chld [0];
			OctNode chld1 = chld [1];
			OctNode chld2 = chld [2];
			OctNode chld3 = chld [3];
			OctNode chld4 = chld [4];
			OctNode chld5 = chld [5];
			OctNode chld6 = chld [6];
			OctNode chld7 = chld [7];

			Child [0].FormAdjacent (adj0, adj1, adj2, chld4, chld1, chld3);
			Child [1].FormAdjacent (adj0, chld0, adj2, chld5, adj4, chld2);
			Child [2].FormAdjacent (adj0, chld3, chld1, chld6, adj4, adj5);
			Child [3].FormAdjacent (adj0, adj1, chld0, chld7, chld2, adj5);
			Child [4].FormAdjacent (chld0, adj1, adj2, adj3, chld5, chld7);
			Child [5].FormAdjacent (chld1, chld4, adj2, adj3, adj4, chld6);
			Child [6].FormAdjacent (chld2, chld7, chld5, adj3, adj4, adj5);
			Child [7].FormAdjacent (chld3, adj1, chld4, adj3, chld6, adj5);
			if (objects != null) {
				currentnode = objects;
			} else {
				currentnode = ObjList;
			}
			while (currentnode != null) {
				ObjectType currentobj = currentnode.GetObj ();
				for (i = 0; i < 8; i++) {
					OctNode cc = chld [i];
					max = cc.GetFace (0).GetVert (0);
					min = cc.GetFace (5).GetVert (3);
					if (!((currentobj.GetMin ().GetX () > max.GetX ()) ||
					  (currentobj.GetMax ().GetX () < min.GetX ()))) {
						if (!((currentobj.GetMin ().GetY () > max.GetY ()) ||
						  (currentobj.GetMax ().GetY () < min.GetY ()))) {
							if (!((currentobj.GetMin ().GetZ () > max.GetZ ()) ||
							  (currentobj.GetMax ().GetZ () < min.GetZ ()))) {
								ObjNode newnode = new ObjNode (currentobj, Child [i].GetList ());
								cc.SetList (newnode);
								cc.IncNumObj ();
							}
						}
					}
				}
				currentnode = currentnode.Next ();
			}
			if (objects == null) {
				NumObj = 0;
				ObjList = null;
			}
			if (depth < MaxDepth) {
				for (i = 0; i < 8; i++) {
					if (Child [i].GetNumObj () > MaxObj) {
						Child [i].CreateChildren (null, depth + 1);
					}
				}
			}
		}

		/**
	 * FormAdjacent
	 *
	 * @param maxX
	 * @param maxY
	 * @param maxZ
	 * @param minX
	 * @param minY
	 * @param minZ
	 */
		private void FormAdjacent (OctNode maxX, OctNode maxY, OctNode maxZ, OctNode minX, OctNode minY, OctNode minZ)
		{
			Adjacent [0] = maxX;
			Adjacent [1] = maxY;
			Adjacent [2] = maxZ;
			Adjacent [3] = minX;
			Adjacent [4] = minY;
			Adjacent [5] = minZ;
		}

		/**
	 * OctNode
	 */
		public OctNode ()
		{
			Initialize ();
		}

		/**
	 * OctNode
	 *
	 * @param scene
	 * @param numObj
	 */
		public OctNode (Scene scene, int numObj)
		{
			Initialize ();
			CreateFaces (scene.GetMaxX (), scene.GetMinX (), scene.GetMaxY (), scene.GetMinY (), scene.GetMaxZ (), scene.GetMinZ ());
			CreateTree (scene.GetObjects (), numObj);
		}

		/**
	 * OctNode
	 *
	 * @param max
	 * @param min
	 */
		public OctNode (Point max, Point min)
		{
			Initialize ();
			CreateFaces (max.GetX (), min.GetX (), max.GetY (), min.GetY (), max.GetZ (), min.GetZ ());
		}


		/**
	 * Copy
	 *
	 * @param original
	 */
		public void Copy (OctNode original)
		{
			int i;

			for (i = 0; i < 6; i++) {
				Adjacent [i] = original.GetAdjacent (i);
				OctFaces [i] = original.GetFace (i);
			}
			for (i = 0; i < 8; i++) {
				Child [i] = original.GetChild (i);
			}
			ObjList = original.GetList ();
			NumObj = original.GetNumObj ();
		}

		/**
	 * GetFace
	 *
	 * @param index
	 * @return Face
	 */
		public Face GetFace (int index)
		{
			return (OctFaces [index]);
		}

		/**
	 * GetList
	 *
	 * @return ObjNode
	 */
		public ObjNode GetList ()
		{
			return (ObjList);
		}

		/**
	 * GetNumObj
	 *
	 * @return int
	 */
		public int GetNumObj ()
		{
			return (NumObj);
		}

		/**
	 * GetAdjacent
	 *
	 * @param index
	 * @return OctNode
	 */
		public OctNode GetAdjacent (int index)
		{
			return (Adjacent [index]);
		}

		/**
	 * GetChild
	 *
	 * @param index
	 */
		public OctNode GetChild (int index)
		{
			return (Child [index]);
		}

		/**
	 * SetList
	 *
	 * @param newlist
	 */
		public void SetList (ObjNode newlist)
		{
			ObjList = newlist;
		}

		/**
	 * IncNumObj
	 */
		public void IncNumObj ()
		{
			NumObj++;
		}

		/**
	 * FindTreeNode
	 *
	 * @param point
	 * @return OctNode
	 */
		public OctNode FindTreeNode (Point point)
		{
			OctNode found;

			if (point.GetX () < OctFaces [MINX].GetVert (0).GetX () || point.GetX () >= OctFaces [MAXX].GetVert (0).GetX ()) {
				return (null);
			}
			if (point.GetY () < OctFaces [MINY].GetVert (0).GetY () || point.GetY () >= OctFaces [MAXY].GetVert (0).GetY ()) {
				return (null);
			}
			if (point.GetZ () < OctFaces [MINZ].GetVert (0).GetZ () || point.GetZ () >= OctFaces [MAXZ].GetVert (0).GetZ ()) {
				return (null);
			}
			if (Child [0] != null) {
				for (int i = 0; i < 8; i++) {
					found = Child [i].FindTreeNode (point);
					if (found != null) {
						return (found);
					}
				}
			}
			return (this);
		}

		/**
	 * Intersect
	 *
	 * @param ray
	 * @param intersect
	 * @param Threshold
	 * @return OctNode
	 */
		public OctNode Intersect (Ray ray, Point intersect, double Threshold)
		{
			Vector delta = new Vector (0.0f, 0.0f, 0.0f);
			double current = 0.0f;
			double t;
			int[] facehits = new int[3];
			facehits [0] = -1;
			facehits [1] = -1;
			facehits [2] = -1;
			OctNode adjacent = null;

			Face[] OFaces = this.OctFaces;
			Face MAXXF = OFaces [MAXX];
			Face MAXYF = OFaces [MAXY];
			Face MAXZF = OFaces [MAXZ];
			Face MINXF = OFaces [MINX];
			Face MINYF = OFaces [MINY];
			Face MINZF = OFaces [MINZ];

			if (ray.GetDirection ().GetX () != 0.0) {
				t = -(ray.GetOrigin ().GetX () - OctFaces [MAXX].GetVert (0).GetX ()) / ray.GetDirection ().GetX ();
				if (t > Threshold && t > current) {
					intersect.Combine (ray.GetOrigin (), ray.GetDirection (), 1.0f, t);
					if ((intersect.GetY () <= MAXYF.GetVert (0).GetY ()) && (intersect.GetY () >= MINYF.GetVert (0).GetY ()) &&
					  (intersect.GetZ () <= MAXZF.GetVert (0).GetZ ()) && (intersect.GetZ () >= MINZF.GetVert (0).GetZ ())) {
						current = t;
						facehits [0] = MAXX;
						delta.SetX (Threshold);
					}
				}
				t = -(ray.GetOrigin ().GetX () - OctFaces [MINX].GetVert (0).GetX ()) / ray.GetDirection ().GetX ();
				if (t > Threshold && t > current) {
					intersect.Combine (ray.GetOrigin (), ray.GetDirection (), 1.0f, t);
					if ((intersect.GetY () <= MAXYF.GetVert (0).GetY ()) && (intersect.GetY () >= MINYF.GetVert (0).GetY ()) &&
					  (intersect.GetZ () <= MAXZF.GetVert (0).GetZ ()) && (intersect.GetZ () >= MINZF.GetVert (0).GetZ ())) {
						current = t;
						facehits [0] = MINX;
						delta.SetX (-Threshold);
					}
				}
			}
			if (ray.GetDirection ().GetY () != 0.0) {
				t = -(ray.GetOrigin ().GetY () - OctFaces [MAXY].GetVert (0).GetY ()) / ray.GetDirection ().GetY ();
				if (t > Threshold) {
					if (t > current) {
						intersect.Combine (ray.GetOrigin (), ray.GetDirection (), 1.0f, t);
						if ((intersect.GetX () <= MAXXF.GetVert (0).GetX ()) && (intersect.GetX () >= MINXF.GetVert (0).GetX ()) &&
						  (intersect.GetZ () <= MAXZF.GetVert (0).GetZ ()) && (intersect.GetZ () >= MINZF.GetVert (0).GetZ ())) {
							current = t;
							facehits [0] = MAXY;
							delta.Set (0.0f, Threshold, 0.0f);
						}
					} else if (t == current) {
						facehits [1] = MAXY;
						delta.SetY (Threshold);
					}
				}
				t = -(ray.GetOrigin ().GetY () - OctFaces [MINY].GetVert (0).GetY ()) / ray.GetDirection ().GetY ();
				if (t > Threshold) {
					if (t > current) {
						intersect.Combine (ray.GetOrigin (), ray.GetDirection (), 1.0f, t);
						if ((intersect.GetX () <= MAXXF.GetVert (0).GetX ()) && (intersect.GetX () >= MINXF.GetVert (0).GetX ()) &&
						  (intersect.GetZ () <= MAXZF.GetVert (0).GetZ ()) && (intersect.GetZ () >= MINZF.GetVert (0).GetZ ())) {
							current = t;
							facehits [0] = MINY;
							delta.Set (0.0f, -Threshold, 0.0f);
						}
					} else if (t == current) {
						facehits [1] = MINY;
						delta.SetY (-Threshold);
					}
				}
			}
			if (ray.GetDirection ().GetZ () != 0.0) {
				t = -(ray.GetOrigin ().GetZ () - OctFaces [MAXZ].GetVert (0).GetZ ()) / ray.GetDirection ().GetZ ();
				if (t > Threshold) {
					if (t > current) {
						intersect.Combine (ray.GetOrigin (), ray.GetDirection (), 1.0f, t);
						if ((intersect.GetX () <= MAXXF.GetVert (0).GetX ()) && (intersect.GetX () >= MINXF.GetVert (0).GetX ()) &&
						  (intersect.GetY () <= MAXYF.GetVert (0).GetY ()) && (intersect.GetY () >= MINYF.GetVert (0).GetY ())) {
							current = t;
							facehits [0] = MAXZ;
							delta.Set (0.0f, 0.0f, Threshold);
						}
					} else if (t == current) {
						if (facehits [1] < 0) {
							facehits [1] = MAXZ;
						} else {
							facehits [2] = MAXZ;
						}
						delta.SetZ (Threshold);
					}
				}
				t = -(ray.GetOrigin ().GetZ () - OctFaces [MINZ].GetVert (0).GetZ ()) / ray.GetDirection ().GetZ ();
				if (t > Threshold) {
					if (t > current) {
						intersect.Combine (ray.GetOrigin (), ray.GetDirection (), 1.0f, t);
						if ((intersect.GetX () <= MAXXF.GetVert (0).GetX ()) && (intersect.GetX () >= MINXF.GetVert (0).GetX ()) &&
						  (intersect.GetY () <= MAXYF.GetVert (0).GetY ()) && (intersect.GetY () >= MINYF.GetVert (0).GetY ())) {
							current = t;
							facehits [0] = MINZ;
							delta.Set (0.0f, 0.0f, -Threshold);
						}
					} else if (t == current) {
						if (facehits [1] < 0) {
							facehits [1] = MINZ;
						} else {
							facehits [2] = MINZ;
						}
						delta.SetZ (-Threshold);
					}
				}
			}
			if (facehits [0] >= MAXX) {
				intersect.Combine (ray.GetOrigin (), ray.GetDirection (), 1.0f, current);
				intersect.Add (delta);
				adjacent = Adjacent [facehits [0]];
				if (facehits [1] >= MAXX) {
					if (adjacent != null) {
						adjacent = adjacent.GetAdjacent (facehits [1]);
						if (facehits [2] >= MAXX) {
							if (adjacent != null) {
								adjacent = adjacent.GetAdjacent (facehits [2]);
							} else {
								adjacent = null;
							}
						}
					} else {
						adjacent = null;
					}
				}
			}
			return (adjacent);
		}
	}
}

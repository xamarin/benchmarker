/*
 * @(#)Scene.java	1.13 06/17/98
 *
 * Scene.java
 * Contains all the elements of the scene. Has functions to load the scene from
 * a file and render it. Traverses the octree data structure to find the
 * color contributions to each pixel of the various lights and objects in the
 * scene, using reflection and reflection.
 *
 * Copyright (c) 1998 Standard Performance Evaluation Corporation (SPEC)
 *               All rights reserved.
 * Copyright (c) 1996,1997,1998 Sun Microsystems, Inc. All rights reserved.
 *
 * This source code is provided as is, without any express or implied warranty.
 */

using System;
using System.Text;
using System.IO;

namespace Benchmarks.SpecRaytracer
{
	/**
 * class Scene
 */
	public class Scene
	{
		private Camera camera;
		private LightNode lights;
		private ObjNode objects;
		private MaterialNode materials;
		private OctNode octree;
		private double MaxX;
		private double MinX;
		private double MaxY;
		private double MinY;
		private double MaxZ;
		private double MinZ;
		private int RayID;
		private const double AmbLightIntensity = 0.5f;
		private const double Brightness = 1.0f;
		private const double MinFactor = 0.1f;
		private const int MaxLevel = 3;


		/**
	 * LoadScene **NS**
	 * <p/>
	 * I tried to make this method load the static data once in to static variables so the MT version
	 * could have the data only read once, but this did not work. For some reason the scene defination
	 * must be changed when the RT does its picture so as a compromise I get this routine to sync on
	 * an object to be sure the MT version does not confuse spec.io.FileInputStream which I suspect
	 * might break with several readers at once. **NS**
	 *
	 * @param filename
	 * @return int
	 */
		private int LoadScene ()
		{
			return LoadSceneOrig ();
		}

		private static String[] sceneLines = null;
		private static int scenePos = -1;

		private static String readString ()
		{
			if (scenePos < sceneLines.Length) {
				String rp = sceneLines [scenePos];
				++scenePos;
				return rp;
			} else
				return null;
		}

		/**
	 * LoadSceneOrig
	 *
	 * @param filename
	 * @return int
	 */
		private int LoadSceneOrig ()
		{
			String instr = Constants.INPUT;
			char[] spt = new char[1];
			spt [0] = '\n';
			sceneLines = instr.Split (spt);
			MainCL.logger.InfoFormat ("number of lines: {0}", sceneLines.Length);
			scenePos = 0;

			int numObj = 0, ObjID = 0;

			camera = null;
			lights = null;
			objects = null;
			materials = null;
			MaxX = MinX = MaxY = MinY = MaxZ = MinZ = 0.0f;
			String input;

			input = readString ();

			while (input != null) {
				if (input.Equals ("camera {")) {
					ReadCamera ();
				} else if (input.Equals ("point_light {")) {
					ReadLight ();
				} else if (input.Equals ("sphere {")) {
					numObj += ReadSphere (ObjID);
				} else if (input.Equals ("poly_set {")) {
					numObj += ReadPoly (ObjID);
				} else {
					;
				}

				input = readString ();
			}

			return (numObj);
		}

		/**
	 * ReadCamera
	 *
	 * @param infile
	 */
		private void ReadCamera ()
		{
			String temp;
			double[] input = new double[3];
			int i;

			temp = readString ();
			temp = temp.Substring (11);
			for (i = 0; i < 2; i++) {
				input [i] = (double)Double.Parse (temp.Substring (0, temp.IndexOf (' ')));
				temp = temp.Substring (temp.IndexOf (' ') + 1);
			}
			input [2] = (double)Double.Parse (temp);
			Point position = new Point (input [0], input [1], input [2]);
			MaxX = MinX = input [0];
			MaxY = MinY = input [1];
			MaxZ = MinZ = input [2];
			temp = readString ();
			temp = temp.Substring (16);
			for (i = 0; i < 2; i++) {
				input [i] = (double)Double.Parse (temp.Substring (0, temp.IndexOf (' ')));
				temp = temp.Substring (temp.IndexOf (' ') + 1);
			}
			input [2] = (double)Double.Parse (temp);
			Vector viewDir = new Vector (input [0], input [1], input [2]);
			temp = readString ();
			temp = temp.Substring (16);
			double focalDist = (double)Double.Parse (temp);
			temp = readString ();
			temp = temp.Substring (10);
			for (i = 0; i < 2; i++) {
				input [i] = (double)Double.Parse (temp.Substring (0, temp.IndexOf (' ')));
				temp = temp.Substring (temp.IndexOf (' ') + 1);
			}
			input [2] = (double)Double.Parse (temp);
			Vector orthoUp = new Vector (input [0], input [1], input [2]);
			temp = readString ();
			temp = temp.Substring (14);
			double FOV = (double)Double.Parse (temp);
			temp = readString ();
			camera = new Camera (position, viewDir, focalDist, orthoUp, FOV);
		}

		/**
	 * ReadLight
	 *
	 * @param infile
	 */
		private void ReadLight ()
		{
			String temp;
			double[] input = new double[3];
			int i;

			temp = readString ();
			temp = temp.Substring (11);
			for (i = 0; i < 2; i++) {
				input [i] = (double)Double.Parse (temp.Substring (0, temp.IndexOf (' ')));
				temp = temp.Substring (temp.IndexOf (' ') + 1);
			}
			input [2] = (double)Double.Parse (temp);
			Point position = new Point (input [0], input [1], input [2]);
			temp = readString ();
			temp = temp.Substring (8);
			for (i = 0; i < 2; i++) {
				input [i] = (double)Double.Parse (temp.Substring (0, temp.IndexOf (' ')));
				temp = temp.Substring (temp.IndexOf (' ') + 1);
			}
			input [2] = (double)Double.Parse (temp);
			Color color = new Color (input [0], input [1], input [2]);
			temp = readString ();
			Light newlight = new Light (position, color);
			LightNode newnode = new LightNode (newlight, lights);
			lights = newnode;
		}

		/**
	 * ReadSphere
	 *
	 * @param infile
	 * @param ObjID
	 * @return int
	 */
		private int ReadSphere (int ObjID)
		{
			String temp;
			double[] input = new double[3];
			int i;
			double radius;
			Point max = new Point (MaxX, MaxY, MaxZ);
			Point min = new Point (MinX, MinY, MinZ);

			temp = readString ();
			temp = readString ();
			Material theMaterial = ReadMaterial ();
			temp = readString ();
			temp = temp.Substring (9);
			for (i = 0; i < 2; i++) {
				input [i] = (double)Double.Parse (temp.Substring (0, temp.IndexOf (' ')));
				temp = temp.Substring (temp.IndexOf (' ') + 1);
			}
			input [2] = (double)Double.Parse (temp);
			Point origin = new Point (input [0], input [1], input [2]);
			temp = readString ();
			temp = temp.Substring (9);
			radius = (double)Double.Parse (temp);
			for (i = 0; i < 7; i++) {
				temp = readString ();
			}
			SphereObj newsphere = new SphereObj (theMaterial, ++ObjID, origin, radius, max, min);
			ObjNode newnode = new ObjNode (newsphere, objects);
			objects = newnode;
			MaxX = max.GetX ();
			MaxY = max.GetY ();
			MaxZ = max.GetZ ();
			MinX = min.GetX ();
			MinY = min.GetY ();
			MinZ = min.GetZ ();

			return (1);
		}

		/**
	 * ReadPoly
	 *
	 * @param infile
	 * @param ObjID
	 * @return int
	 */
		private int ReadPoly (int ObjID)
		{
			String temp;
			double[] input = new double[3];
			int i, j, k;
			int numpolys = 0;
			int numverts;
			bool trimesh, vertnormal;
			Point max = new Point (MaxX, MaxY, MaxZ);
			Point min = new Point (MinX, MinY, MinZ);

			temp = readString ();
			temp = readString ();
			Material theMaterial = ReadMaterial ();
			temp = readString ();
			if (temp.Substring (7).Equals ("POLYSET_TRI_MESH")) {
				trimesh = true;
			} else {
				trimesh = false;
			}
			temp = readString ();
			if (temp.Substring (11).Equals ("PER_VERTEX_NORMAL")) {
				vertnormal = true;
			} else {
				vertnormal = false;
			}
			for (i = 0; i < 4; i++) {
				temp = readString ();
			}
			temp = temp.Substring (11);
			numpolys = Int32.Parse (temp);
			ObjID++;
			for (i = 0; i < numpolys; i++) {
				temp = readString ();
				temp = readString ();
				temp = temp.Substring (16);
				numverts = Int32.Parse (temp);
				Point[] vertices = new Point[numverts];
				for (j = 0; j < numverts; j++) {
					temp = readString ();
					temp = temp.Substring (8);
					for (k = 0; k < 2; k++) {
						input [k] = (double)Double.Parse (temp.Substring (0, temp.IndexOf (' ')));
						temp = temp.Substring (temp.IndexOf (' ') + 1);
					}
					input [2] = (double)Double.Parse (temp);
					vertices [j] = new Point (input [0], input [1], input [2]);
					if (vertnormal) {
						temp = readString ();
					}
				}
				temp = readString ();
				TriangleObj newtriangle;
				PolygonObj newpoly;
				ObjNode newnode;
				if (trimesh) {
					newtriangle = new TriangleObj (theMaterial, ObjID, numverts, vertices, max, min);
					newnode = new ObjNode (newtriangle, objects);
				} else {
					newpoly = new PolygonObj (theMaterial, ObjID, numverts, vertices, max, min);
					newnode = new ObjNode (newpoly, objects);
				}
				objects = newnode;
			}
			temp = readString ();
			MaxX = max.GetX ();
			MaxY = max.GetY ();
			MaxZ = max.GetZ ();
			MinX = min.GetX ();
			MinY = min.GetY ();
			MinZ = min.GetZ ();

			return (numpolys);
		}

		/**
	 * ReadMaterial
	 *
	 * @param infile
	 * @return Material
	 */
		private Material ReadMaterial ()
		{
			String temp;
			double[] input = new double[3];
			Color[] colors = new Color[4];
			int i, j;
			double shininess, ktran;

			temp = readString ();
			for (i = 0; i < 4; i++) {
				temp = readString ();
				if (i != 1) {
					temp = temp.Substring (14);
				} else {
					temp = temp.Substring (13);
				}
				for (j = 0; j < 2; j++) {
					input [j] = (double)Double.Parse (temp.Substring (0, temp.IndexOf (' ')));
					temp = temp.Substring (temp.IndexOf (' ') + 1);
				}
				input [2] = (double)Double.Parse (temp);
				colors [i] = new Color (input [0], input [1], input [2]);
			}
			temp = readString ();
			shininess = (double)Double.Parse (temp.Substring (14));
			temp = readString ();
			ktran = (double)Double.Parse (temp.Substring (10));
			temp = readString ();
			Material newmaterial = new Material (colors [0], colors [1], colors [2], colors [3], shininess, ktran);
			MaterialNode newnode = new MaterialNode (newmaterial, materials);
			materials = newnode;
			return (newmaterial);
		}

		/**
	 * Shade
	 *
	 * @param tree
	 * @param eyeRay
	 * @param color
	 * @param factor
	 * @param level
	 * @param originID
	 */
		private void Shade (OctNode tree, Ray eyeRay, Color color, double factor, int level, int originID)
		{
			Color lightColor = new Color (0.0f, 0.0f, 0.0f);
			Color reflectColor = new Color (0.0f, 0.0f, 0.0f);
			Color refractColor = new Color (0.0f, 0.0f, 0.0f);
			IntersectPt intersect = new IntersectPt ();
			OctNode baseoctn = new OctNode ();
			Vector normal = new Vector ();
			Ray reflect = new Ray ();
			Ray refract = new Ray ();
			double mu;
			int current;

			if (intersect.FindNearestIsect (tree, eyeRay, originID, level, baseoctn)) {
				intersect.GetIntersectObj ().FindNormal (intersect.GetIntersection (), normal);
				GetLightColor (baseoctn, intersect.GetIntersection (), normal, intersect.GetIntersectObj (), lightColor);
				if (level < MaxLevel) {
					double check = factor * (1.0f - intersect.GetIntersectObj ().GetMaterial ().GetKTran ()) * intersect.GetIntersectObj ().GetMaterial ().GetShininess ();
					if (check > MinFactor) {
						reflect.SetOrigin (intersect.GetIntersection ());
						reflect.GetDirection ().Combine (eyeRay.GetDirection (), normal, 1.0f, -2.0f * normal.Dot (eyeRay.GetDirection ()));
						reflect.SetID (RayID);
						this.RayID = this.RayID + 1;
						Shade (baseoctn, reflect, reflectColor, check, level + 1, originID);
						reflectColor.Scale ((1.0f - intersect.GetIntersectObj ().GetMaterial ().GetKTran ()) * intersect.GetIntersectObj ().GetMaterial ().GetShininess (),
							intersect.GetIntersectObj ().GetMaterial ().GetSpecColor ());
					}
					check = factor * intersect.GetIntersectObj ().GetMaterial ().GetKTran ();
					if (check > MinFactor) {
						if (intersect.GetEnter ()) {
							mu = 1.0f / intersect.GetIntersectObj ().GetMaterial ().GetRefIndex ();
							current = intersect.GetIntersectObj ().GetObjID ();
						} else {
							mu = intersect.GetIntersectObj ().GetMaterial ().GetRefIndex ();
							normal.Negate ();
							current = 0;
						}
						double IdotN = normal.Dot (eyeRay.GetDirection ());
						double TotIntReflect = 1.0f - mu * mu * (1.0f - IdotN * IdotN);
						if (TotIntReflect >= 0.0) {
							double gamma = -mu * IdotN - (double)Math.Sqrt (TotIntReflect);
							refract.SetOrigin (intersect.GetIntersection ());
							refract.GetDirection ().Combine (eyeRay.GetDirection (), normal, mu, gamma);
							refract.SetID (RayID);
							this.RayID = RayID + 1;
							Shade (baseoctn, refract, refractColor, check, level + 1, current);
							refractColor.Scale (intersect.GetIntersectObj ().GetMaterial ().GetKTran (), intersect.GetIntersectObj ().GetMaterial ().GetSpecColor ());
						}
					}
				}
				color.Combine (intersect.GetIntersectObj ().GetMaterial ().GetEmissColor (), intersect.GetIntersectObj ().GetMaterial ().GetAmbColor (),
					AmbLightIntensity, lightColor, reflectColor, refractColor);
			}
		}

		/**
	 * GetLightColor
	 *
	 * @param tree
	 * @param point
	 * @param normal
	 * @param currentObj
	 * @param color
	 */
		private void GetLightColor (OctNode tree, Point point, Vector normal, ObjectType currentObj, Color color)
		{
			Ray shadow = new Ray ();
			LightNode current = lights;
			double maxt;

			while (current != null) {
				shadow.SetOrigin (point);
				shadow.GetDirection ().Sub (current.GetLight ().GetPosition (), point);
				maxt = shadow.GetDirection ().Length ();
				shadow.GetDirection ().Normalize ();
				shadow.SetID (RayID);
				this.RayID = this.RayID + 1;
				if (!FindLightBlock (tree, shadow, maxt)) {
					double factor = Math.Max (0.0f, normal.Dot (shadow.GetDirection ()));
					if (factor != 0.0) {
						color.Mix (factor, current.GetLight ().GetColor (), currentObj.GetMaterial ().GetDiffColor ());
					}
				}
				current = current.Next ();
			}
		}

		/**
	 * FindLightBlock
	 *
	 * @param tree
	 * @param ray
	 * @param maxt
	 * @return boolean
	 */
		private bool FindLightBlock (OctNode tree, Ray ray, double maxt)
		{
			OctNode current = tree.FindTreeNode (ray.GetOrigin ());
			IntersectPt test = new IntersectPt ();
			Point testpt = new Point ();

			while (current != null) {
				ObjNode currentnode = current.GetList ();
				while (currentnode != null) {
					bool found = false;
					if (currentnode.GetObj ().GetCachePt ().GetID () == ray.GetID ()) {
						found = true;
					}
					if (!found) {
						test.SetOrigID (0);
						if (currentnode.GetObj ().Intersect (ray, test)) {
							if (test.GetT () < maxt) {
								return (true);
							}
						}
					}
					currentnode = currentnode.Next ();
				}
				OctNode adjacent = current.Intersect (ray, testpt, test.GetThreshold ());
				if (adjacent == null) {
					current = null;
				} else {
					current = adjacent.FindTreeNode (testpt);
				}
			}
			return (false);
		}

		/**
	 * Scene
	 *
	 * @param width
	 * @param height
	 * @param filename
	 */
		public Scene ()
		{
			int numObj = LoadScene ();
			octree = new OctNode (this, numObj);
		}

		/**
	 * RenderScene
	 */
		public void RenderScene (Canvas canvas, int width, int section, int nsections)
		{
			Vector view = camera.GetViewDir ();
			Vector up = camera.GetOrthoUp ();
			Vector plane = new Vector ();
			Vector horIncr = new Vector ();
			Vector vertIncr = new Vector ();
			double ylen = camera.GetFocalDist () * (double)Math.Tan (0.5f * camera.GetFOV ());
			double xlen = ylen * canvas.GetWidth () / canvas.GetHeight ();
			Point upleft = new Point ();
			Point upright = new Point ();
			Point lowleft = new Point ();
			Point basepoint = new Point ();
			Point current;
			Ray eyeRay = new Ray ();
			int ypixel, xpixel;

			RayID = 1;
			plane.Cross (view, up);
			view.Scale (camera.GetFocalDist ());
			up.Scale (ylen);
			plane.Scale (-xlen);
			upleft.FindCorner (view, up, plane, camera.GetPosition ());
			plane.Negate ();
			upright.FindCorner (view, up, plane, camera.GetPosition ());
			up.Negate ();
			plane.Negate ();
			lowleft.FindCorner (view, up, plane, camera.GetPosition ());
			horIncr.Sub (upright, upleft);
			horIncr.Scale (horIncr.Length () / ((double)canvas.GetWidth ()));
			vertIncr.Sub (lowleft, upleft);
			vertIncr.Scale (vertIncr.Length () / ((double)canvas.GetHeight ()));
			basepoint.Set (upleft.GetX () + 0.5f * (horIncr.GetX () + vertIncr.GetX ()), upleft.GetY () + 0.5f * (horIncr.GetY () + vertIncr.GetY ()),
				upleft.GetZ () + 0.5f * (horIncr.GetZ () + vertIncr.GetZ ()));
			eyeRay.SetOrigin (camera.GetPosition ());

			int xstart = section * width / nsections;
			int xend = xstart + width / nsections;

			MainCL.logger.InfoFormat ("+" + xstart + " to " + (xend - 1) + " by " + canvas.GetHeight ());

			for (ypixel = 0; ypixel < canvas.GetHeight (); ypixel++) {
				current = new Point (basepoint);
				for (xpixel = 0; xpixel < canvas.GetWidth (); xpixel++) {
					if (xpixel >= xstart && xpixel < xend) {
						Color color = new Color (0.0f, 0.0f, 0.0f);
						eyeRay.GetDirection ().Sub (current, eyeRay.GetOrigin ());
						eyeRay.GetDirection ().Normalize ();
						eyeRay.SetID (RayID);
						this.RayID = this.RayID + 1;
						Shade (octree, eyeRay, color, 1.0f, 0, 0);
						canvas.Write (Brightness, xpixel, ypixel, color);
					}
					current.Add (horIncr);
				}
				basepoint.Add (vertIncr);
			}
			MainCL.logger.InfoFormat ("-" + xstart + " to " + (xend - 1) + " by " + canvas.GetHeight ());
		}

		/**
	 * GetObjects
	 *
	 * @return ObjNode
	 */
		public ObjNode GetObjects ()
		{
			return (objects);
		}

		/**
	 * GetMaxX
	 *
	 * @return double
	 */
		public double GetMaxX ()
		{
			return (MaxX);
		}

		/**
	 * GetMinX
	 *
	 * @return double
	 */
		public double GetMinX ()
		{
			return (MinX);
		}

		/**
	 * GetMaxY
	 *
	 * @return double
	 */
		public double GetMaxY ()
		{
			return (MaxY);
		}

		/**
	 * GetMinY
	 *
	 * @return double
	 */
		public double GetMinY ()
		{
			return (MinY);
		}

		/**
	 * GetMaxZ
	 *
	 * @return double
	 */
		public double GetMaxZ ()
		{
			return (MaxZ);
		}

		/**
	 * GetMinZ
	 *
	 * @return double
	 */
		public double GetMinZ ()
		{
			return (MinZ);
		}
	}

	class Constants {
		public const string INPUT = @"Composer format 2.1 ascii
camera {
  position -64.0168 -0.199505 47.609
  viewDirection 0.709891 -0.151829 -0.687752
  focalDistance 83.7942
  orthoUp 0.137405 0.98758 -0.0761914
  verticalFOV 0.785398
}
point_light {
  position -40.0437 8.17026 -3.60726
  color 0.5 0.5 0.5
}
point_light {
  position 4.93125 30.0526 34.5669
  color 1 1 1
}
poly_set {
  name ""
  numMaterials 1
  material {
    diffColor 0.183673 0.183177 0.182681
    ambColor 0.2 0.2 0.2
    specColor 0.744898 0.744898 0.744898
    emisColor 0 0 0
    shininess 0.2
    ktran 0
  }
  type POLYSET_FACE_SET
  normType PER_FACE_NORMAL
  materialBinding PER_OBJECT_MATERIAL
  hasTextureCoords FALSE
  rowSize 0
  numPolys 6
  poly {
    numVertices 4
    pos -73.3735 -19.2762 73.6046
    pos -73.3735 -23.9396 73.6046
    pos 73.0973 -23.9396 73.6046
    pos 73.0973 -19.2762 73.6046
  }
  poly {
    numVertices 4
    pos -73.3735 -19.2762 -72.8662
    pos 73.0973 -19.2762 -72.8662
    pos 73.0973 -23.9396 -72.8662
    pos -73.3735 -23.9396 -72.8662
  }
  poly {
    numVertices 4
    pos -73.3735 -19.2762 73.6046
    pos -73.3735 -19.2762 -72.8662
    pos -73.3735 -23.9396 -72.8662
    pos -73.3735 -23.9396 73.6046
  }
  poly {
    numVertices 4
    pos 73.0973 -19.2762 73.6046
    pos 73.0973 -23.9396 73.6046
    pos 73.0973 -23.9396 -72.8662
    pos 73.0973 -19.2762 -72.8662
  }
  poly {
    numVertices 4
    pos -73.3735 -19.2762 73.6046
    pos 73.0973 -19.2762 73.6046
    pos 73.0973 -19.2762 -72.8662
    pos -73.3735 -19.2762 -72.8662
  }
  poly {
    numVertices 4
    pos -73.3735 -23.9396 73.6046
    pos -73.3735 -23.9396 -72.8662
    pos 73.0973 -23.9396 -72.8662
    pos 73.0973 -23.9396 73.6046
  }
}
poly_set {
  name ""
  numMaterials 1
  material {
    diffColor 0.75 0.5775 0.375
    ambColor 0.15 0.15 0.15
    specColor 0 0 0
    emisColor 0 0 0
    shininess 0.2
    ktran 0
  }
  type POLYSET_TRI_MESH
  normType PER_VERTEX_NORMAL
  materialBinding PER_OBJECT_MATERIAL
  hasTextureCoords FALSE
  rowSize 0
  numPolys 1395
  poly {
    numVertices 3
    pos -0.357896 -13.2263 0.0601258
    norm -0.146638 -9.46451 -3.22515
    pos -2.46167 -12.6482 -0.746944
    norm -2.10803 -9.73919 -0.83927
    pos -1.72534 -12.5177 -1.18808
    norm 0.78716 -9.81368 -1.75274
  }
  poly {
    numVertices 3
    pos 14.1432 -15.9577 11.0633
    norm -3.28274 -8.75119 3.55532
    pos 13.5226 -15.8573 10.5449
    norm -7.75747 -4.87038 4.01261
    pos 12.5064 -15.4412 9.36332
    norm -7.26255 -2.16443 6.52462
  }
  poly {
    numVertices 3
    pos -5.58623 -19.4082 2.71577
    norm -2.11591 -7.41205 6.37059
    pos -5.76928 -18.8956 2.82659
    norm -6.02046 0.817659 7.94264
    pos -6.12839 -18.9616 2.31741
    norm -9.08452 0.785912 4.10534
  }
  poly {
    numVertices 3
    pos -4.82848 -0.196341 -2.26623
    norm 5.5997 1.58585 -8.13194
    pos -5.40552 0.16046 -2.46657
    norm 2.65352 2.18544 -9.39056
    pos -4.39043 0.99754 -1.6567
    norm 6.19548 1.38771 -7.72596
  }
  poly {
    numVertices 3
    pos -0.527417 -19.112 6.20347
    norm -5.13615 -0.573981 8.56099
    pos -0.582583 -18.4332 5.84974
    norm -6.51616 5.90666 4.75933
    pos -1.12623 -18.9312 5.58733
    norm -8.76816 0.530275 4.77893
  }
  poly {
    numVertices 3
    pos -0.527417 -19.112 6.20347
    norm -5.13615 -0.573981 8.56099
    pos 0.453254 -19.2285 6.45999
    norm -1.44472 -4.54158 8.7913
    pos 0.564647 -18.3093 6.31246
    norm -3.23203 1.75189 9.29973
  }
  poly {
    numVertices 3
    pos 0.453254 -19.2285 6.45999
    norm -1.44472 -4.54158 8.7913
    pos 0.916356 -19.1587 6.45121
    norm 1.08535 -2.93586 9.49751
    pos 0.564647 -18.3093 6.31246
    norm -3.23203 1.75189 9.29973
  }
  poly {
    numVertices 3
    pos -5.76928 -18.8956 2.82659
    norm -6.02046 0.817659 7.94264
    pos -5.46907 -18.1557 2.47566
    norm -8.77681 2.5456 4.06048
    pos -6.12839 -18.9616 2.31741
    norm -9.08452 0.785912 4.10534
  }
  poly {
    numVertices 3
    pos 0.564647 -18.3093 6.31246
    norm -3.23203 1.75189 9.29973
    pos -0.290922 -17.9807 5.59832
    norm -8.3273 2.79946 4.77694
    pos -0.582583 -18.4332 5.84974
    norm -6.51616 5.90666 4.75933
  }
  poly {
    numVertices 3
    pos -0.527417 -19.112 6.20347
    norm -5.13615 -0.573981 8.56099
    pos 0.564647 -18.3093 6.31246
    norm -3.23203 1.75189 9.29973
    pos -0.582583 -18.4332 5.84974
    norm -6.51616 5.90666 4.75933
  }
  poly {
    numVertices 3
    pos 0.916356 -19.1587 6.45121
    norm 1.08535 -2.93586 9.49751
    pos 1.11266 -17.1594 6.36618
    norm 3.1409 -0.394874 9.48572
    pos 0.564647 -18.3093 6.31246
    norm -3.23203 1.75189 9.29973
  }
  poly {
    numVertices 3
    pos -3.56044 -18.5697 -5.15075
    norm 9.07698 -2.52043 -3.35499
    pos -4.49025 -19.4335 -5.45155
    norm 3.35634 -9.41696 0.236478
    pos -4.15299 -19.3924 -5.7299
    norm 3.34049 -8.72104 -3.57554
  }
  poly {
    numVertices 3
    pos -6.11087 -18.8517 -3.75757
    norm -5.56098 0.461625 8.29834
    pos -6.6304 -18.4853 -4.01595
    norm -8.33235 3.20496 4.50557
    pos -6.34574 -19.2106 -3.95814
    norm -4.83054 -5.13626 7.09118
  }
  poly {
    numVertices 3
    pos -5.23633 4.51948 -1.50026
    norm 0.50282 2.06563 -9.77141
    pos -5.48677 8.83448 -0.682687
    norm -6.53133 2.0409 -7.29221
    pos -4.56752 7.83438 -0.79817
    norm 3.64294 1.59626 -9.17502
  }
  poly {
    numVertices 3
    pos -5.76928 -18.8956 2.82659
    norm -6.02046 0.817659 7.94264
    pos -4.98335 -18.4228 2.99007
    norm -2.65841 1.1534 9.57092
    pos -5.25459 -17.6511 2.69229
    norm -6.31361 1.33198 7.63965
  }
  poly {
    numVertices 3
    pos -5.76928 -18.8956 2.82659
    norm -6.02046 0.817659 7.94264
    pos -5.25459 -17.6511 2.69229
    norm -6.31361 1.33198 7.63965
    pos -5.46907 -18.1557 2.47566
    norm -8.77681 2.5456 4.06048
  }
  poly {
    numVertices 3
    pos -5.0833 0.98698 -2.12313
    norm 3.99763 1.4455 -9.05149
    pos -5.05599 2.40479 -1.97649
    norm 3.02521 1.52326 -9.40893
    pos -4.39043 0.99754 -1.6567
    norm 6.19548 1.38771 -7.72596
  }
  poly {
    numVertices 3
    pos 5.44222 -18.8925 2.97636
    norm -1.541 -4.79986 8.63635
    pos 5.14061 -18.4373 2.96289
    norm -5.53908 0.70348 8.29601
    pos 4.68402 -18.8672 2.53061
    norm -6.64904 -2.30815 7.10371
  }
  poly {
    numVertices 3
    pos -5.0833 0.98698 -2.12313
    norm 3.99763 1.4455 -9.05149
    pos -5.52957 1.33959 -2.16328
    norm 0.737567 1.89886 -9.79032
    pos -5.05599 2.40479 -1.97649
    norm 3.02521 1.52326 -9.40893
  }
  poly {
    numVertices 3
    pos -4.98335 -18.4228 2.99007
    norm -2.65841 1.1534 9.57092
    pos -4.73337 -17.0081 2.82271
    norm -1.32038 -0.222874 9.90994
    pos -5.25459 -17.6511 2.69229
    norm -6.31361 1.33198 7.63965
  }
  poly {
    numVertices 3
    pos -4.42361 -18.4625 3.06688
    norm 1.2685 -1.31164 9.83212
    pos -4.07782 -16.7707 2.7645
    norm 6.10066 0.127016 7.92249
    pos -4.73337 -17.0081 2.82271
    norm -1.32038 -0.222874 9.90994
  }
  poly {
    numVertices 3
    pos 0.564647 -18.3093 6.31246
    norm -3.23203 1.75189 9.29973
    pos -0.0845011 -16.56 5.88587
    norm -7.63868 -0.0609581 6.45344
    pos -0.290922 -17.9807 5.59832
    norm -8.3273 2.79946 4.77694
  }
  poly {
    numVertices 3
    pos 6.14551 -17.6639 3.10086
    norm 0.518151 1.5958 9.85824
    pos 5.14061 -18.4373 2.96289
    norm -5.53908 0.70348 8.29601
    pos 5.44222 -18.8925 2.97636
    norm -1.541 -4.79986 8.63635
  }
  poly {
    numVertices 3
    pos 6.64033 -18.4669 3.05128
    norm 4.54137 -1.31627 8.81155
    pos 6.14551 -17.6639 3.10086
    norm 0.518151 1.5958 9.85824
    pos 5.44222 -18.8925 2.97636
    norm -1.541 -4.79986 8.63635
  }
  poly {
    numVertices 3
    pos -5.22285 4.31418 0.629247
    norm -2.87914 -0.928193 9.53148
    pos -4.90397 4.0578 0.6206
    norm 0.628265 -1.13764 9.9152
    pos -4.75588 6.33448 0.975947
    norm -0.537424 -1.34268 9.89487
  }
  poly {
    numVertices 3
    pos -6.11087 -18.8517 -3.75757
    norm -5.56098 0.461625 8.29834
    pos -5.83976 -18.1071 -4.05654
    norm -8.73339 4.82933 0.636729
    pos -6.6304 -18.4853 -4.01595
    norm -8.33235 3.20496 4.50557
  }
  poly {
    numVertices 3
    pos -5.09561 -18.4673 -3.07797
    norm -0.377619 0.392749 9.98515
    pos -5.40374 -17.5601 -3.37764
    norm -7.27576 1.06843 6.77657
    pos -6.11087 -18.8517 -3.75757
    norm -5.56098 0.461625 8.29834
  }
  poly {
    numVertices 3
    pos -5.40374 -17.5601 -3.37764
    norm -7.27576 1.06843 6.77657
    pos -5.83976 -18.1071 -4.05654
    norm -8.73339 4.82933 0.636729
    pos -6.11087 -18.8517 -3.75757
    norm -5.56098 0.461625 8.29834
  }
  poly {
    numVertices 3
    pos -5.46907 -18.1557 2.47566
    norm -8.77681 2.5456 4.06048
    pos -5.25459 -17.6511 2.69229
    norm -6.31361 1.33198 7.63965
    pos -5.74922 -17.0399 2.18766
    norm -9.74152 0.168909 2.2526
  }
  poly {
    numVertices 3
    pos 1.11266 -17.1594 6.36618
    norm 3.1409 -0.394874 9.48572
    pos 0.606488 -16.9722 6.3572
    norm -3.58062 -0.587752 9.31846
    pos 0.564647 -18.3093 6.31246
    norm -3.23203 1.75189 9.29973
  }
  poly {
    numVertices 3
    pos -5.25459 -17.6511 2.69229
    norm -6.31361 1.33198 7.63965
    pos -5.52638 -15.6086 2.63084
    norm -8.20097 0.228738 5.71767
    pos -5.74922 -17.0399 2.18766
    norm -9.74152 0.168909 2.2526
  }
  poly {
    numVertices 3
    pos -4.73337 -17.0081 2.82271
    norm -1.32038 -0.222874 9.90994
    pos -5.52638 -15.6086 2.63084
    norm -8.20097 0.228738 5.71767
    pos -5.25459 -17.6511 2.69229
    norm -6.31361 1.33198 7.63965
  }
  poly {
    numVertices 3
    pos 5.14061 -18.4373 2.96289
    norm -5.53908 0.70348 8.29601
    pos 6.14551 -17.6639 3.10086
    norm 0.518151 1.5958 9.85824
    pos 5.40388 -17.1397 2.85426
    norm -3.0169 1.39344 9.43168
  }
  poly {
    numVertices 3
    pos -4.70929 5.13828 -1.26631
    norm 4.614 1.41352 -8.7586
    pos -5.23633 4.51948 -1.50026
    norm 0.50282 2.06563 -9.77141
    pos -4.56752 7.83438 -0.79817
    norm 3.64294 1.59626 -9.17502
  }
  poly {
    numVertices 3
    pos -3.971 0.991199 -1.18359
    norm 9.0112 1.12036 -4.18846
    pos -3.94628 2.11456 -0.763765
    norm 9.93457 0.42033 -1.0619
    pos -3.80574 -0.0732203 -0.647841
    norm 9.93586 1.06025 0.39308
  }
  poly {
    numVertices 3
    pos -4.9726 -18.8315 -3.15757
    norm -0.342929 -2.75362 9.60729
    pos -4.859 -16.7585 -2.9291
    norm -4.02286 -1.00849 9.09942
    pos -5.40374 -17.5601 -3.37764
    norm -7.27576 1.06843 6.77657
  }
  poly {
    numVertices 3
    pos 5.14061 -18.4373 2.96289
    norm -5.53908 0.70348 8.29601
    pos 5.40388 -17.1397 2.85426
    norm -3.0169 1.39344 9.43168
    pos 4.96878 -17.7485 2.58786
    norm -8.08089 0.867681 5.82636
  }
  poly {
    numVertices 3
    pos -0.0845011 -16.56 5.88587
    norm -7.63868 -0.0609581 6.45344
    pos 0.606488 -16.9722 6.3572
    norm -3.58062 -0.587752 9.31846
    pos 0.255151 -15.9748 6.21275
    norm -6.12898 0.0820215 7.90119
  }
  poly {
    numVertices 3
    pos 4.7818 -18.0847 2.10223
    norm -9.96826 0.236757 0.760023
    pos 4.96878 -17.7485 2.58786
    norm -8.08089 0.867681 5.82636
    pos 4.40375 -16.8088 2.12267
    norm -8.71396 -2.20582 4.38192
  }
  poly {
    numVertices 3
    pos 4.96878 -17.7485 2.58786
    norm -8.08089 0.867681 5.82636
    pos 5.40388 -17.1397 2.85426
    norm -3.0169 1.39344 9.43168
    pos 4.49541 -16.1976 2.44063
    norm -6.32529 -0.408662 7.73459
  }
  poly {
    numVertices 3
    pos -4.39043 0.99754 -1.6567
    norm 6.19548 1.38771 -7.72596
    pos -4.33288 2.84798 -1.3933
    norm 7.4742 1.04776 -6.56038
    pos -3.971 0.991199 -1.18359
    norm 9.0112 1.12036 -4.18846
  }
  poly {
    numVertices 3
    pos -0.283247 -16.1208 5.31343
    norm -9.98621 0.0295973 0.524017
    pos -0.0845011 -16.56 5.88587
    norm -7.63868 -0.0609581 6.45344
    pos -0.121226 -15.4517 5.7487
    norm -8.02474 -0.205263 5.96334
  }
  poly {
    numVertices 3
    pos -5.40374 -17.5601 -3.37764
    norm -7.27576 1.06843 6.77657
    pos -4.859 -16.7585 -2.9291
    norm -4.02286 -1.00849 9.09942
    pos -5.70136 -16.3119 -3.53508
    norm -9.17219 1.17268 3.80733
  }
  poly {
    numVertices 3
    pos 0.255151 -15.9748 6.21275
    norm -6.12898 0.0820215 7.90119
    pos -0.121226 -15.4517 5.7487
    norm -8.02474 -0.205263 5.96334
    pos -0.0845011 -16.56 5.88587
    norm -7.63868 -0.0609581 6.45344
  }
  poly {
    numVertices 3
    pos 5.40388 -17.1397 2.85426
    norm -3.0169 1.39344 9.43168
    pos 4.98477 -15.6632 2.63136
    norm -1.57615 0.274975 9.87118
    pos 4.49541 -16.1976 2.44063
    norm -6.32529 -0.408662 7.73459
  }
  poly {
    numVertices 3
    pos 0.255151 -15.9748 6.21275
    norm -6.12898 0.0820215 7.90119
    pos 0.606488 -16.9722 6.3572
    norm -3.58062 -0.587752 9.31846
    pos 0.794315 -15.5449 6.52063
    norm -2.07978 -0.0475231 9.78122
  }
  poly {
    numVertices 3
    pos 5.40388 -17.1397 2.85426
    norm -3.0169 1.39344 9.43168
    pos 5.78605 -16.2582 2.61357
    norm 3.53917 3.53024 8.66092
    pos 4.98477 -15.6632 2.63136
    norm -1.57615 0.274975 9.87118
  }
  poly {
    numVertices 3
    pos -4.09451 -15.121 3.07477
    norm 2.64513 -3.03033 9.15534
    pos -4.89572 -15.6929 2.96688
    norm -1.82954 -1.64476 9.69266
    pos -4.73337 -17.0081 2.82271
    norm -1.32038 -0.222874 9.90994
  }
  poly {
    numVertices 3
    pos 2.28293 -13.2826 1.18152
    norm -0.148885 -8.9958 -4.36503
    pos 1.95109 -12.8602 0.366746
    norm 2.67699 -7.63905 -5.87185
    pos 2.80769 -13.2753 0.903531
    norm -5.46502 -7.46703 -3.7917
  }
  poly {
    numVertices 3
    pos -4.859 -16.7585 -2.9291
    norm -4.02286 -1.00849 9.09942
    pos -4.93657 -15.5553 -2.98597
    norm -6.98659 1.26198 7.04238
    pos -5.70136 -16.3119 -3.53508
    norm -9.17219 1.17268 3.80733
  }
  poly {
    numVertices 3
    pos -4.09451 -15.121 3.07477
    norm 2.64513 -3.03033 9.15534
    pos -4.5486 -14.5695 3.24243
    norm -2.0754 -1.60425 9.64982
    pos -4.89572 -15.6929 2.96688
    norm -1.82954 -1.64476 9.69266
  }
  poly {
    numVertices 3
    pos -4.39043 0.99754 -1.6567
    norm 6.19548 1.38771 -7.72596
    pos -3.971 0.991199 -1.18359
    norm 9.0112 1.12036 -4.18846
    pos -3.8842 -0.17551 -1.42712
    norm 8.45929 1.22575 -5.19018
  }
  poly {
    numVertices 3
    pos -4.5486 -14.5695 3.24243
    norm -2.0754 -1.60425 9.64982
    pos -5.18117 -14.6785 3.00876
    norm -5.47977 -0.386899 8.35599
    pos -4.89572 -15.6929 2.96688
    norm -1.82954 -1.64476 9.69266
  }
  poly {
    numVertices 3
    pos -0.121226 -15.4517 5.7487
    norm -8.02474 -0.205263 5.96334
    pos 0.0206174 -13.8208 5.93032
    norm -7.14273 0.0755825 6.99827
    pos -0.146719 -14.4176 5.83305
    norm -7.59837 0.152613 6.49934
  }
  poly {
    numVertices 3
    pos 5.47606 -15.4799 2.53888
    norm 4.71801 -0.115056 8.8163
    pos 5.01767 -14.9434 2.7632
    norm 0.414586 -5.90578 8.05915
    pos 4.98477 -15.6632 2.63136
    norm -1.57615 0.274975 9.87118
  }
  poly {
    numVertices 3
    pos -4.93657 -15.5553 -2.98597
    norm -6.98659 1.26198 7.04238
    pos -5.13742 -15.1904 -3.4014
    norm -8.75339 4.09711 2.56746
    pos -5.70136 -16.3119 -3.53508
    norm -9.17219 1.17268 3.80733
  }
  poly {
    numVertices 3
    pos 4.98477 -15.6632 2.63136
    norm -1.57615 0.274975 9.87118
    pos 5.01767 -14.9434 2.7632
    norm 0.414586 -5.90578 8.05915
    pos 4.37221 -15.1109 2.26629
    norm -6.46204 -3.07488 6.98479
  }
  poly {
    numVertices 3
    pos 0.794315 -15.5449 6.52063
    norm -2.07978 -0.0475231 9.78122
    pos 1.30906 -14.7929 6.48271
    norm 1.28867 -0.814554 9.8831
    pos 0.702972 -14.1291 6.34669
    norm -4.11256 -0.320562 9.10956
  }
  poly {
    numVertices 3
    pos 4.28666 -6.41083 4.51174
    norm 2.28514 7.5741 6.11647
    pos 5.03743 -7.43863 5.19513
    norm 1.90716 6.02343 7.7512
    pos 6.07528 -7.88127 5.21381
    norm 4.44789 6.86933 5.74706
  }
  poly {
    numVertices 3
    pos -4.58881 -18.4664 -5.96442
    norm -1.03224 3.63142 -9.25998
    pos -4.53285 -19.0919 -6.25921
    norm 3.24862 -1.65157 -9.31229
    pos -4.7385 -19.2777 -5.96447
    norm -1.84915 -5.05636 -8.42698
  }
  poly {
    numVertices 3
    pos -5.18117 -14.6785 3.00876
    norm -5.47977 -0.386899 8.35599
    pos -5.33984 -13.5362 2.54091
    norm -8.93817 1.05547 4.35833
    pos -5.52638 -15.6086 2.63084
    norm -8.20097 0.228738 5.71767
  }
  poly {
    numVertices 3
    pos -5.18117 -14.6785 3.00876
    norm -5.47977 -0.386899 8.35599
    pos -4.5486 -14.5695 3.24243
    norm -2.0754 -1.60425 9.64982
    pos -4.83519 -13.6808 3.17317
    norm -6.09209 1.06364 7.85844
  }
  poly {
    numVertices 3
    pos 6.46578 -13.7723 3.93901
    norm 3.91257 -9.19225 -0.440992
    pos 9.16125 -15.5749 5.49072
    norm -1.40995 -8.29987 -5.39669
    pos 5.88352 -14.1734 4.86874
    norm -0.798628 -9.82047 1.70898
  }
  poly {
    numVertices 3
    pos -1.62321 -13.1215 -3.66567
    norm 8.23487 0.150715 -5.67135
    pos -1.60103 -14.4594 -3.38552
    norm 9.60254 -2.66033 -0.844921
    pos -1.62787 -14.1294 -3.74326
    norm 8.44954 -1.83973 -5.02204
  }
  poly {
    numVertices 3
    pos -4.93657 -15.5553 -2.98597
    norm -6.98659 1.26198 7.04238
    pos -3.95801 -13.9431 -2.18399
    norm -7.31147 0.0977299 6.82151
    pos -5.13742 -15.1904 -3.4014
    norm -8.75339 4.09711 2.56746
  }
  poly {
    numVertices 3
    pos -4.3253 -1.34326 -2.01213
    norm 6.76499 1.32773 -7.24377
    pos -3.23906 -2.74681 -1.22788
    norm 7.77713 3.54821 -5.18909
    pos -3.91682 -3.02274 -2.01956
    norm 5.91153 2.82406 -7.55504
  }
  poly {
    numVertices 3
    pos -4.09451 -15.121 3.07477
    norm 2.64513 -3.03033 9.15534
    pos -3.57977 -13.548 3.56998
    norm 2.22492 -0.0265895 9.74931
    pos -4.5486 -14.5695 3.24243
    norm -2.0754 -1.60425 9.64982
  }
  poly {
    numVertices 3
    pos -0.773334 -10.1339 -2.91499
    norm 6.91569 -1.80504 -6.99394
    pos -0.28981 -9.73938 -2.63913
    norm 3.64756 -2.20968 -9.04503
    pos -0.677277 -11.281 -2.11471
    norm 5.59542 -5.69032 -6.0259
  }
  poly {
    numVertices 3
    pos -3.95801 -13.9431 -2.18399
    norm -7.31147 0.0977299 6.82151
    pos -4.21837 -13.639 -2.81587
    norm -9.71203 2.22499 0.851998
    pos -5.13742 -15.1904 -3.4014
    norm -8.75339 4.09711 2.56746
  }
  poly {
    numVertices 3
    pos -3.57977 -13.548 3.56998
    norm 2.22492 -0.0265895 9.74931
    pos -4.23926 -13.4181 3.47991
    norm -3.49412 0.851568 9.33092
    pos -4.5486 -14.5695 3.24243
    norm -2.0754 -1.60425 9.64982
  }
  poly {
    numVertices 3
    pos -4.23926 -13.4181 3.47991
    norm -3.49412 0.851568 9.33092
    pos -4.83519 -13.6808 3.17317
    norm -6.09209 1.06364 7.85844
    pos -4.5486 -14.5695 3.24243
    norm -2.0754 -1.60425 9.64982
  }
  poly {
    numVertices 3
    pos -0.146719 -14.4176 5.83305
    norm -7.59837 0.152613 6.49934
    pos 0.0206174 -13.8208 5.93032
    norm -7.14273 0.0755825 6.99827
    pos -0.368209 -14.303 5.31731
    norm -9.65013 0.450162 2.58308
  }
  poly {
    numVertices 3
    pos 1.67285 -14.3322 6.3714
    norm 4.82088 -3.01384 8.22653
    pos 1.41579 -13.7077 6.69625
    norm -0.879791 -2.42279 9.66209
    pos 1.30906 -14.7929 6.48271
    norm 1.28867 -0.814554 9.8831
  }
  poly {
    numVertices 3
    pos -3.91682 -3.02274 -2.01956
    norm 5.91153 2.82406 -7.55504
    pos -3.23906 -2.74681 -1.22788
    norm 7.77713 3.54821 -5.18909
    pos -2.34291 -3.79958 -1.0176
    norm 7.08379 4.76794 -5.20448
  }
  poly {
    numVertices 3
    pos 1.30906 -14.7929 6.48271
    norm 1.28867 -0.814554 9.8831
    pos 1.41579 -13.7077 6.69625
    norm -0.879791 -2.42279 9.66209
    pos 0.702972 -14.1291 6.34669
    norm -4.11256 -0.320562 9.10956
  }
  poly {
    numVertices 3
    pos -4.87718 9.30128 1.3301
    norm -2.26571 -1.63239 9.60218
    pos -5.28706 7.99038 0.975844
    norm -5.77012 -0.83039 8.12503
    pos -4.75588 6.33448 0.975947
    norm -0.537424 -1.34268 9.89487
  }
  poly {
    numVertices 3
    pos -4.10131 -1.57873 0.24944
    norm 4.88343 2.51075 8.35752
    pos -4.47609 -2.48377 0.540099
    norm 0.315882 2.7505 9.60911
    pos -3.52944 -3.05024 0.736041
    norm 1.87693 6.12494 7.67869
  }
  poly {
    numVertices 3
    pos 0.508093 -17.5007 3.96244
    norm -2.8014 -0.75583 -9.56979
    pos -0.153755 -14.7952 4.09062
    norm -7.99686 -0.275063 -5.99788
    pos 0.634711 -13.1459 3.57186
    norm -5.46732 -6.82679 -4.84804
  }
  poly {
    numVertices 3
    pos -4.83519 -13.6808 3.17317
    norm -6.09209 1.06364 7.85844
    pos -5.33984 -13.5362 2.54091
    norm -8.93817 1.05547 4.35833
    pos -5.18117 -14.6785 3.00876
    norm -5.47977 -0.386899 8.35599
  }
  poly {
    numVertices 3
    pos -5.52957 1.33959 -2.16328
    norm 0.737567 1.89886 -9.79032
    pos -6.03612 2.2523 -2.00852
    norm -1.11556 2.14083 -9.70424
    pos -5.05599 2.40479 -1.97649
    norm 3.02521 1.52326 -9.40893
  }
  poly {
    numVertices 3
    pos -3.67895 -14.6637 -2.20106
    norm -1.54476 -2.89539 9.44619
    pos -3.46427 -13.6093 -1.77623
    norm -2.8395 -2.96263 9.11921
    pos -3.95801 -13.9431 -2.18399
    norm -7.31147 0.0977299 6.82151
  }
  poly {
    numVertices 3
    pos -4.75588 6.33448 0.975947
    norm -0.537424 -1.34268 9.89487
    pos -4.31311 8.69838 1.23407
    norm 4.55417 -1.47168 8.78031
    pos -4.87718 9.30128 1.3301
    norm -2.26571 -1.63239 9.60218
  }
  poly {
    numVertices 3
    pos -3.8842 -0.17551 -1.42712
    norm 8.45929 1.22575 -5.19018
    pos -4.82848 -0.196341 -2.26623
    norm 5.5997 1.58585 -8.13194
    pos -4.39043 0.99754 -1.6567
    norm 6.19548 1.38771 -7.72596
  }
  poly {
    numVertices 3
    pos 1.3167 -12.7386 6.82562
    norm -2.05511 -0.887939 9.74619
    pos 1.41579 -13.7077 6.69625
    norm -0.879791 -2.42279 9.66209
    pos 2.64476 -12.8334 6.95851
    norm -0.118875 -1.26298 9.91921
  }
  poly {
    numVertices 3
    pos -3.57977 -13.548 3.56998
    norm 2.22492 -0.0265895 9.74931
    pos -4.22326 -12.3853 3.23113
    norm -1.65301 0.685175 9.83861
    pos -4.23926 -13.4181 3.47991
    norm -3.49412 0.851568 9.33092
  }
  poly {
    numVertices 3
    pos -3.8027 -18.8281 -4.01741
    norm 7.55102 -5.20987 3.97988
    pos -3.47146 -17.0699 -3.9623
    norm 8.9259 -3.66105 2.63157
    pos -3.74297 -16.894 -3.39166
    norm 6.85428 -3.50839 6.38045
  }
  poly {
    numVertices 3
    pos 4.62076 -14.2961 3.4172
    norm -1.57245 -9.64242 2.13335
    pos 2.31774 -13.6426 2.28765
    norm -2.74809 -9.60543 0.428646
    pos 3.98159 -14.4435 2.56982
    norm -4.78334 -7.88491 3.86624
  }
  poly {
    numVertices 3
    pos -4.33288 2.84798 -1.3933
    norm 7.4742 1.04776 -6.56038
    pos -3.94628 2.11456 -0.763765
    norm 9.93457 0.42033 -1.0619
    pos -3.971 0.991199 -1.18359
    norm 9.0112 1.12036 -4.18846
  }
  poly {
    numVertices 3
    pos -5.28706 7.99038 0.975844
    norm -5.77012 -0.83039 8.12503
    pos -5.4013 9.20118 1.04275
    norm -7.09335 -0.913391 6.98928
    pos -5.70448 8.23418 0.42774
    norm -9.06602 0.144092 4.21741
  }
  poly {
    numVertices 3
    pos -5.434 -12.9887 2.25442
    norm -8.85879 -0.99168 4.53194
    pos -4.76517 -12.5792 2.96131
    norm -5.98565 -0.453797 7.99788
    pos -5.90258 -11.7864 2.19089
    norm -8.17213 -2.99435 4.92445
  }
  poly {
    numVertices 3
    pos -0.134512 -12.5962 5.47859
    norm -9.74782 1.36904 1.76229
    pos 0.0206174 -13.8208 5.93032
    norm -7.14273 0.0755825 6.99827
    pos 0.190796 -12.0301 6.22176
    norm -8.04967 1.6415 5.70161
  }
  poly {
    numVertices 3
    pos 0.0206174 -13.8208 5.93032
    norm -7.14273 0.0755825 6.99827
    pos 0.740866 -12.1068 6.69971
    norm -4.56911 0.0836025 8.89473
    pos 0.190796 -12.0301 6.22176
    norm -8.04967 1.6415 5.70161
  }
  poly {
    numVertices 3
    pos 1.51897 -11.4031 6.89312
    norm -1.99592 0.663922 9.77627
    pos 1.3167 -12.7386 6.82562
    norm -2.05511 -0.887939 9.74619
    pos 2.64476 -12.8334 6.95851
    norm -0.118875 -1.26298 9.91921
  }
  poly {
    numVertices 3
    pos 6.70286 -11.8926 6.86067
    norm -1.18294 -0.156284 9.92856
    pos 9.21038 -13.7496 7.20827
    norm -1.84563 0.888925 9.78792
    pos 8.83447 -11.2836 6.62974
    norm 1.89934 4.28739 8.83237
  }
  poly {
    numVertices 3
    pos -3.57977 -13.548 3.56998
    norm 2.22492 -0.0265895 9.74931
    pos -3.73572 -12.4283 3.1961
    norm 3.40211 1.349 9.30623
    pos -4.22326 -12.3853 3.23113
    norm -1.65301 0.685175 9.83861
  }
  poly {
    numVertices 3
    pos -3.59295 -18.7918 -4.53027
    norm 9.00954 -4.2537 0.856904
    pos -4.49025 -19.4335 -5.45155
    norm 3.35634 -9.41696 0.236478
    pos -3.56044 -18.5697 -5.15075
    norm 9.07698 -2.52043 -3.35499
  }
  poly {
    numVertices 3
    pos -4.76517 -12.5792 2.96131
    norm -5.98565 -0.453797 7.99788
    pos -4.93713 -11.9342 3.1063
    norm -4.17429 -1.88759 8.88889
    pos -5.90258 -11.7864 2.19089
    norm -8.17213 -2.99435 4.92445
  }
  poly {
    numVertices 3
    pos -4.76517 -12.5792 2.96131
    norm -5.98565 -0.453797 7.99788
    pos -4.22326 -12.3853 3.23113
    norm -1.65301 0.685175 9.83861
    pos -4.93713 -11.9342 3.1063
    norm -4.17429 -1.88759 8.88889
  }
  poly {
    numVertices 3
    pos 3.90577 -11.3992 7.03794
    norm -0.291325 0.858354 9.95883
    pos 2.64476 -12.8334 6.95851
    norm -0.118875 -1.26298 9.91921
    pos 5.15808 -11.259 7.02667
    norm 1.60188 -1.11773 9.80738
  }
  poly {
    numVertices 3
    pos 5.86548 -12.502 6.64516
    norm -1.79559 -3.13139 9.32579
    pos 4.95033 -12.8112 6.32751
    norm 1.34532 -6.21054 7.72135
    pos 5.28919 -13.2799 6.08438
    norm -1.80188 -6.628 7.26794
  }
  poly {
    numVertices 3
    pos 6.49444 -13.181 6.57407
    norm -3.31623 -3.50608 8.75842
    pos 5.86548 -12.502 6.64516
    norm -1.79559 -3.13139 9.32579
    pos 5.28919 -13.2799 6.08438
    norm -1.80188 -6.628 7.26794
  }
  poly {
    numVertices 3
    pos 5.86548 -12.502 6.64516
    norm -1.79559 -3.13139 9.32579
    pos 5.8825 -11.6913 6.83246
    norm 0.0785027 -0.309978 9.99488
    pos 4.95033 -12.8112 6.32751
    norm 1.34532 -6.21054 7.72135
  }
  poly {
    numVertices 3
    pos -4.22326 -12.3853 3.23113
    norm -1.65301 0.685175 9.83861
    pos -3.97647 -11.0843 3.3047
    norm 0.178145 -1.54634 9.87811
    pos -4.93713 -11.9342 3.1063
    norm -4.17429 -1.88759 8.88889
  }
  poly {
    numVertices 3
    pos 12.9711 -16.5308 7.91567
    norm 3.64796 -8.60927 -3.54583
    pos 14.6449 -15.9389 9.7333
    norm 8.68951 -3.85237 -3.10672
    pos 13.937 -16.4061 9.72636
    norm 1.69619 -9.73679 1.52245
  }
  poly {
    numVertices 3
    pos 7.25101 -13.1962 6.91231
    norm -3.06769 -1.87857 9.33061
    pos 6.70286 -11.8926 6.86067
    norm -1.18294 -0.156284 9.92856
    pos 6.49444 -13.181 6.57407
    norm -3.31623 -3.50608 8.75842
  }
  poly {
    numVertices 3
    pos 9.68214 -12.6627 7.02945
    norm -0.82539 3.09652 9.47261
    pos 8.83447 -11.2836 6.62974
    norm 1.89934 4.28739 8.83237
    pos 9.21038 -13.7496 7.20827
    norm -1.84563 0.888925 9.78792
  }
  poly {
    numVertices 3
    pos 2.64476 -12.8334 6.95851
    norm -0.118875 -1.26298 9.91921
    pos 3.90577 -11.3992 7.03794
    norm -0.291325 0.858354 9.95883
    pos 2.35778 -10.1694 6.88436
    norm -0.929721 1.62577 9.82306
  }
  poly {
    numVertices 3
    pos 6.70286 -11.8926 6.86067
    norm -1.18294 -0.156284 9.92856
    pos 5.86548 -12.502 6.64516
    norm -1.79559 -3.13139 9.32579
    pos 6.49444 -13.181 6.57407
    norm -3.31623 -3.50608 8.75842
  }
  poly {
    numVertices 3
    pos -5.05599 2.40479 -1.97649
    norm 3.02521 1.52326 -9.40893
    pos -4.33288 2.84798 -1.3933
    norm 7.4742 1.04776 -6.56038
    pos -4.39043 0.99754 -1.6567
    norm 6.19548 1.38771 -7.72596
  }
  poly {
    numVertices 3
    pos -5.90258 -11.7864 2.19089
    norm -8.17213 -2.99435 4.92445
    pos -4.93713 -11.9342 3.1063
    norm -4.17429 -1.88759 8.88889
    pos -5.91918 -10.9343 2.52807
    norm -7.05401 -1.02711 7.01328
  }
  poly {
    numVertices 3
    pos -3.73572 -12.4283 3.1961
    norm 3.40211 1.349 9.30623
    pos -3.97647 -11.0843 3.3047
    norm 0.178145 -1.54634 9.87811
    pos -4.22326 -12.3853 3.23113
    norm -1.65301 0.685175 9.83861
  }
  poly {
    numVertices 3
    pos -3.07857 -11.868 2.95479
    norm 3.02265 -2.34406 9.23953
    pos -3.97647 -11.0843 3.3047
    norm 0.178145 -1.54634 9.87811
    pos -3.73572 -12.4283 3.1961
    norm 3.40211 1.349 9.30623
  }
  poly {
    numVertices 3
    pos -0.134512 -12.5962 5.47859
    norm -9.74782 1.36904 1.76229
    pos 0.0524526 -11.2599 5.43388
    norm -7.75667 -1.39258 6.15587
    pos 0.0136405 -12.2344 4.57136
    norm -8.80427 -3.52215 3.1748
  }
  poly {
    numVertices 3
    pos 0.411964 -11.0887 5.99328
    norm -8.24775 2.2903 5.17002
    pos 0.0524526 -11.2599 5.43388
    norm -7.75667 -1.39258 6.15587
    pos -0.134512 -12.5962 5.47859
    norm -9.74782 1.36904 1.76229
  }
  poly {
    numVertices 3
    pos 0.190796 -12.0301 6.22176
    norm -8.04967 1.6415 5.70161
    pos 0.411964 -11.0887 5.99328
    norm -8.24775 2.2903 5.17002
    pos -0.134512 -12.5962 5.47859
    norm -9.74782 1.36904 1.76229
  }
  poly {
    numVertices 3
    pos -4.43964 -10.526 3.38932
    norm -1.6898 -0.93405 9.81184
    pos -4.93713 -11.9342 3.1063
    norm -4.17429 -1.88759 8.88889
    pos -3.97647 -11.0843 3.3047
    norm 0.178145 -1.54634 9.87811
  }
  poly {
    numVertices 3
    pos 5.86548 -12.502 6.64516
    norm -1.79559 -3.13139 9.32579
    pos 6.70286 -11.8926 6.86067
    norm -1.18294 -0.156284 9.92856
    pos 5.8825 -11.6913 6.83246
    norm 0.0785027 -0.309978 9.99488
  }
  poly {
    numVertices 3
    pos -5.33984 -13.5362 2.54091
    norm -8.93817 1.05547 4.35833
    pos -5.434 -12.9887 2.25442
    norm -8.85879 -0.99168 4.53194
    pos -5.46645 -13.3129 1.79579
    norm -9.88526 -0.608969 -1.38229
  }
  poly {
    numVertices 3
    pos -3.07857 -11.868 2.95479
    norm 3.02265 -2.34406 9.23953
    pos -3.5269 -10.4585 3.5318
    norm -3.71794 -3.3034 8.67552
    pos -3.97647 -11.0843 3.3047
    norm 0.178145 -1.54634 9.87811
  }
  poly {
    numVertices 3
    pos -2.83174 -11.4541 3.35851
    norm -4.15278 -6.81769 6.02275
    pos -3.5269 -10.4585 3.5318
    norm -3.71794 -3.3034 8.67552
    pos -3.07857 -11.868 2.95479
    norm 3.02265 -2.34406 9.23953
  }
  poly {
    numVertices 3
    pos -0.440669 -11.6301 4.8394
    norm -3.31797 -6.12626 7.17356
    pos -0.141278 -10.8745 5.46159
    norm -3.66977 -2.60014 8.93153
    pos -1.41203 -11.4555 4.84936
    norm -3.54584 -5.00315 7.89908
  }
  poly {
    numVertices 3
    pos 0.0524526 -11.2599 5.43388
    norm -7.75667 -1.39258 6.15587
    pos -0.440669 -11.6301 4.8394
    norm -3.31797 -6.12626 7.17356
    pos 0.0136405 -12.2344 4.57136
    norm -8.80427 -3.52215 3.1748
  }
  poly {
    numVertices 3
    pos 0.0524526 -11.2599 5.43388
    norm -7.75667 -1.39258 6.15587
    pos -0.141278 -10.8745 5.46159
    norm -3.66977 -2.60014 8.93153
    pos -0.440669 -11.6301 4.8394
    norm -3.31797 -6.12626 7.17356
  }
  poly {
    numVertices 3
    pos -4.43964 -10.526 3.38932
    norm -1.6898 -0.93405 9.81184
    pos -4.99807 -10.1784 3.23696
    norm -4.53826 -0.206921 8.9085
    pos -4.93713 -11.9342 3.1063
    norm -4.17429 -1.88759 8.88889
  }
  poly {
    numVertices 3
    pos -3.5269 -10.4585 3.5318
    norm -3.71794 -3.3034 8.67552
    pos -4.43964 -10.526 3.38932
    norm -1.6898 -0.93405 9.81184
    pos -3.97647 -11.0843 3.3047
    norm 0.178145 -1.54634 9.87811
  }
  poly {
    numVertices 3
    pos 0.0524526 -11.2599 5.43388
    norm -7.75667 -1.39258 6.15587
    pos 0.411964 -11.0887 5.99328
    norm -8.24775 2.2903 5.17002
    pos 0.581387 -10.2181 5.92358
    norm -5.48614 1.27675 8.26271
  }
  poly {
    numVertices 3
    pos -4.99807 -10.1784 3.23696
    norm -4.53826 -0.206921 8.9085
    pos -5.91918 -10.9343 2.52807
    norm -7.05401 -1.02711 7.01328
    pos -4.93713 -11.9342 3.1063
    norm -4.17429 -1.88759 8.88889
  }
  poly {
    numVertices 3
    pos -1.41203 -11.4555 4.84936
    norm -3.54584 -5.00315 7.89908
    pos -2.1638 -10.6043 4.65384
    norm -5.26288 -1.68372 8.33469
    pos -2.75314 -10.7816 4.20168
    norm -6.37077 -2.93335 7.12804
  }
  poly {
    numVertices 3
    pos -1.54057 -9.76134 5.1475
    norm -3.87715 -0.467208 9.20594
    pos -2.1638 -10.6043 4.65384
    norm -5.26288 -1.68372 8.33469
    pos -1.41203 -11.4555 4.84936
    norm -3.54584 -5.00315 7.89908
  }
  poly {
    numVertices 3
    pos 0.0524526 -11.2599 5.43388
    norm -7.75667 -1.39258 6.15587
    pos 0.581387 -10.2181 5.92358
    norm -5.48614 1.27675 8.26271
    pos -0.141278 -10.8745 5.46159
    norm -3.66977 -2.60014 8.93153
  }
  poly {
    numVertices 3
    pos 1.51897 -11.4031 6.89312
    norm -1.99592 0.663922 9.77627
    pos 2.35778 -10.1694 6.88436
    norm -0.929721 1.62577 9.82306
    pos 1.69011 -10.2507 6.73267
    norm -3.88336 2.71503 8.80615
  }
  poly {
    numVertices 3
    pos -0.141278 -10.8745 5.46159
    norm -3.66977 -2.60014 8.93153
    pos 0.581387 -10.2181 5.92358
    norm -5.48614 1.27675 8.26271
    pos -0.432926 -9.78983 5.41481
    norm -3.28229 -0.131218 9.44507
  }
  poly {
    numVertices 3
    pos 1.12225 -10.7054 6.58825
    norm -5.91535 2.71199 7.59301
    pos 0.581387 -10.2181 5.92358
    norm -5.48614 1.27675 8.26271
    pos 0.411964 -11.0887 5.99328
    norm -8.24775 2.2903 5.17002
  }
  poly {
    numVertices 3
    pos -5.90258 -11.7864 2.19089
    norm -8.17213 -2.99435 4.92445
    pos -5.91918 -10.9343 2.52807
    norm -7.05401 -1.02711 7.01328
    pos -6.41234 -10.2989 1.92819
    norm -8.64858 -1.11023 4.89586
  }
  poly {
    numVertices 3
    pos -0.432926 -9.78983 5.41481
    norm -3.28229 -0.131218 9.44507
    pos -1.54057 -9.76134 5.1475
    norm -3.87715 -0.467208 9.20594
    pos -0.141278 -10.8745 5.46159
    norm -3.66977 -2.60014 8.93153
  }
  poly {
    numVertices 3
    pos 2.35778 -10.1694 6.88436
    norm -0.929721 1.62577 9.82306
    pos 3.90577 -11.3992 7.03794
    norm -0.291325 0.858354 9.95883
    pos 3.92801 -9.63744 6.72303
    norm -0.215369 2.67197 9.63402
  }
  poly {
    numVertices 3
    pos -6.52836 -10.8655 1.39294
    norm -9.40618 -3.11557 1.34795
    pos -6.41234 -10.2989 1.92819
    norm -8.64858 -1.11023 4.89586
    pos -6.74061 -9.87395 1.32155
    norm -9.66247 0.589177 2.50792
  }
  poly {
    numVertices 3
    pos -5.91918 -10.9343 2.52807
    norm -7.05401 -1.02711 7.01328
    pos -4.99807 -10.1784 3.23696
    norm -4.53826 -0.206921 8.9085
    pos -5.59569 -9.69985 2.82243
    norm -5.75759 0.908653 8.12555
  }
  poly {
    numVertices 3
    pos -2.75314 -10.7816 4.20168
    norm -6.37077 -2.93335 7.12804
    pos -2.1638 -10.6043 4.65384
    norm -5.26288 -1.68372 8.33469
    pos -2.70757 -9.17808 4.45397
    norm -5.76781 0.131912 8.16792
  }
  poly {
    numVertices 3
    pos -1.54057 -9.76134 5.1475
    norm -3.87715 -0.467208 9.20594
    pos -2.70757 -9.17808 4.45397
    norm -5.76781 0.131912 8.16792
    pos -2.1638 -10.6043 4.65384
    norm -5.26288 -1.68372 8.33469
  }
  poly {
    numVertices 3
    pos 1.12225 -10.7054 6.58825
    norm -5.91535 2.71199 7.59301
    pos 1.69011 -10.2507 6.73267
    norm -3.88336 2.71503 8.80615
    pos 0.581387 -10.2181 5.92358
    norm -5.48614 1.27675 8.26271
  }
  poly {
    numVertices 3
    pos 3.90577 -11.3992 7.03794
    norm -0.291325 0.858354 9.95883
    pos 5.15808 -11.259 7.02667
    norm 1.60188 -1.11773 9.80738
    pos 4.646 -9.84243 6.84369
    norm 1.07643 3.00155 9.47797
  }
  poly {
    numVertices 3
    pos 5.15808 -11.259 7.02667
    norm 1.60188 -1.11773 9.80738
    pos 6.52046 -10.2486 6.39982
    norm 2.01654 3.13315 9.27992
    pos 4.646 -9.84243 6.84369
    norm 1.07643 3.00155 9.47797
  }
  poly {
    numVertices 3
    pos -6.09022 -9.17202 2.34186
    norm -7.40475 0.743075 6.67963
    pos -6.41234 -10.2989 1.92819
    norm -8.64858 -1.11023 4.89586
    pos -5.91918 -10.9343 2.52807
    norm -7.05401 -1.02711 7.01328
  }
  poly {
    numVertices 3
    pos -4.43964 -10.526 3.38932
    norm -1.6898 -0.93405 9.81184
    pos -4.53457 -9.51144 3.39841
    norm -3.41061 0.998939 9.34718
    pos -4.99807 -10.1784 3.23696
    norm -4.53826 -0.206921 8.9085
  }
  poly {
    numVertices 3
    pos 4.646 -9.84243 6.84369
    norm 1.07643 3.00155 9.47797
    pos 3.92801 -9.63744 6.72303
    norm -0.215369 2.67197 9.63402
    pos 3.90577 -11.3992 7.03794
    norm -0.291325 0.858354 9.95883
  }
  poly {
    numVertices 3
    pos -2.43726 -15.3994 -3.93026
    norm 8.14933 -4.94443 -3.02343
    pos -1.62787 -14.1294 -3.74326
    norm 8.44954 -1.83973 -5.02204
    pos -1.60103 -14.4594 -3.38552
    norm 9.60254 -2.66033 -0.844921
  }
  poly {
    numVertices 3
    pos -4.53457 -9.51144 3.39841
    norm -3.41061 0.998939 9.34718
    pos -5.59569 -9.69985 2.82243
    norm -5.75759 0.908653 8.12555
    pos -4.99807 -10.1784 3.23696
    norm -4.53826 -0.206921 8.9085
  }
  poly {
    numVertices 3
    pos -2.75314 -10.7816 4.20168
    norm -6.37077 -2.93335 7.12804
    pos -2.70757 -9.17808 4.45397
    norm -5.76781 0.131912 8.16792
    pos -3.63628 -9.06782 3.69973
    norm -5.65925 0.263433 8.24036
  }
  poly {
    numVertices 3
    pos 2.40176 -9.20136 6.5942
    norm -1.69627 3.79548 9.09489
    pos 1.69011 -10.2507 6.73267
    norm -3.88336 2.71503 8.80615
    pos 2.35778 -10.1694 6.88436
    norm -0.929721 1.62577 9.82306
  }
  poly {
    numVertices 3
    pos -4.87718 9.30128 1.3301
    norm -2.26571 -1.63239 9.60218
    pos -5.00519 10.6159 1.59781
    norm -4.66419 -1.49476 8.71843
    pos -5.4013 9.20118 1.04275
    norm -7.09335 -0.913391 6.98928
  }
  poly {
    numVertices 3
    pos -3.23906 -2.74681 -1.22788
    norm 7.77713 3.54821 -5.18909
    pos -3.48307 -2.10146 -0.874187
    norm 9.14499 3.8158 -1.34493
    pos -2.34291 -3.79958 -1.0176
    norm 7.08379 4.76794 -5.20448
  }
  poly {
    numVertices 3
    pos -1.54057 -9.76134 5.1475
    norm -3.87715 -0.467208 9.20594
    pos -0.737537 -8.75539 5.35773
    norm -2.93786 1.25827 9.47553
    pos -2.04821 -8.4586 4.76938
    norm -4.59977 2.26527 8.58549
  }
  poly {
    numVertices 3
    pos 1.69011 -10.2507 6.73267
    norm -3.88336 2.71503 8.80615
    pos 1.50041 -8.76085 6.05027
    norm -3.71296 3.35072 8.65948
    pos 0.581387 -10.2181 5.92358
    norm -5.48614 1.27675 8.26271
  }
  poly {
    numVertices 3
    pos -5.87396 1.74018 0.199279
    norm -5.63666 -0.143113 8.25879
    pos -5.61575 2.92003 0.423229
    norm -4.51975 -0.407768 8.91098
    pos -6.24211 2.74096 -0.200528
    norm -8.29056 0.385716 5.57834
  }
  poly {
    numVertices 3
    pos 0.581387 -10.2181 5.92358
    norm -5.48614 1.27675 8.26271
    pos 0.191765 -8.83552 5.61098
    norm -2.88706 1.92562 9.37853
    pos -0.432926 -9.78983 5.41481
    norm -3.28229 -0.131218 9.44507
  }
  poly {
    numVertices 3
    pos 3.92801 -9.63744 6.72303
    norm -0.215369 2.67197 9.63402
    pos 3.22428 -7.81218 6.12013
    norm 0.340707 5.13796 8.57235
    pos 3.05568 -9.19868 6.57617
    norm -0.526782 3.67993 9.28336
  }
  poly {
    numVertices 3
    pos -5.22402 6.57518 0.862218
    norm -4.59514 -0.820964 8.84369
    pos -5.28706 7.99038 0.975844
    norm -5.77012 -0.83039 8.12503
    pos -5.68235 6.67428 0.515124
    norm -7.4058 -0.233301 6.71563
  }
  poly {
    numVertices 3
    pos -4.53457 -9.51144 3.39841
    norm -3.41061 0.998939 9.34718
    pos -4.43964 -10.526 3.38932
    norm -1.6898 -0.93405 9.81184
    pos -3.94479 -9.40631 3.45026
    norm -3.72836 -0.432315 9.26889
  }
  poly {
    numVertices 3
    pos 0.581387 -10.2181 5.92358
    norm -5.48614 1.27675 8.26271
    pos 1.50041 -8.76085 6.05027
    norm -3.71296 3.35072 8.65948
    pos 0.191765 -8.83552 5.61098
    norm -2.88706 1.92562 9.37853
  }
  poly {
    numVertices 3
    pos 3.05568 -9.19868 6.57617
    norm -0.526782 3.67993 9.28336
    pos 2.34706 -7.76509 5.85884
    norm -2.15846 5.01183 8.3799
    pos 2.40176 -9.20136 6.5942
    norm -1.69627 3.79548 9.09489
  }
  poly {
    numVertices 3
    pos -4.82848 -0.196341 -2.26623
    norm 5.5997 1.58585 -8.13194
    pos -3.8842 -0.17551 -1.42712
    norm 8.45929 1.22575 -5.19018
    pos -4.3253 -1.34326 -2.01213
    norm 6.76499 1.32773 -7.24377
  }
  poly {
    numVertices 3
    pos -4.53457 -9.51144 3.39841
    norm -3.41061 0.998939 9.34718
    pos -3.94479 -9.40631 3.45026
    norm -3.72836 -0.432315 9.26889
    pos -4.69667 -8.59835 3.17691
    norm -4.03678 0.617014 9.12818
  }
  poly {
    numVertices 3
    pos -1.54057 -9.76134 5.1475
    norm -3.87715 -0.467208 9.20594
    pos -2.04821 -8.4586 4.76938
    norm -4.59977 2.26527 8.58549
    pos -2.70757 -9.17808 4.45397
    norm -5.76781 0.131912 8.16792
  }
  poly {
    numVertices 3
    pos -3.94479 -9.40631 3.45026
    norm -3.72836 -0.432315 9.26889
    pos -3.63628 -9.06782 3.69973
    norm -5.65925 0.263433 8.24036
    pos -4.69667 -8.59835 3.17691
    norm -4.03678 0.617014 9.12818
  }
  poly {
    numVertices 3
    pos 1.69011 -10.2507 6.73267
    norm -3.88336 2.71503 8.80615
    pos 2.40176 -9.20136 6.5942
    norm -1.69627 3.79548 9.09489
    pos 1.50041 -8.76085 6.05027
    norm -3.71296 3.35072 8.65948
  }
  poly {
    numVertices 3
    pos -5.16347 1.65372 0.514513
    norm -1.3543 -0.579078 9.89093
    pos -4.90397 4.0578 0.6206
    norm 0.628265 -1.13764 9.9152
    pos -5.61575 2.92003 0.423229
    norm -4.51975 -0.407768 8.91098
  }
  poly {
    numVertices 3
    pos -4.69667 -8.59835 3.17691
    norm -4.03678 0.617014 9.12818
    pos -5.59577 -8.97342 2.66893
    norm -5.65369 0.982106 8.1897
    pos -4.53457 -9.51144 3.39841
    norm -3.41061 0.998939 9.34718
  }
  poly {
    numVertices 3
    pos 2.34706 -7.76509 5.85884
    norm -2.15846 5.01183 8.3799
    pos 3.05568 -9.19868 6.57617
    norm -0.526782 3.67993 9.28336
    pos 3.22428 -7.81218 6.12013
    norm 0.340707 5.13796 8.57235
  }
  poly {
    numVertices 3
    pos -4.69667 -8.59835 3.17691
    norm -4.03678 0.617014 9.12818
    pos -3.63628 -9.06782 3.69973
    norm -5.65925 0.263433 8.24036
    pos -3.42154 -7.48252 3.49583
    norm -3.93015 1.20342 9.11624
  }
  poly {
    numVertices 3
    pos -0.432926 -9.78983 5.41481
    norm -3.28229 -0.131218 9.44507
    pos 0.191765 -8.83552 5.61098
    norm -2.88706 1.92562 9.37853
    pos -0.737537 -8.75539 5.35773
    norm -2.93786 1.25827 9.47553
  }
  poly {
    numVertices 3
    pos -2.04821 -8.4586 4.76938
    norm -4.59977 2.26527 8.58549
    pos -2.97108 -8.36129 4.09266
    norm -5.79013 2.12898 7.87031
    pos -2.70757 -9.17808 4.45397
    norm -5.76781 0.131912 8.16792
  }
  poly {
    numVertices 3
    pos -4.75588 6.33448 0.975947
    norm -0.537424 -1.34268 9.89487
    pos -5.28706 7.99038 0.975844
    norm -5.77012 -0.83039 8.12503
    pos -5.22402 6.57518 0.862218
    norm -4.59514 -0.820964 8.84369
  }
  poly {
    numVertices 3
    pos -0.737537 -8.75539 5.35773
    norm -2.93786 1.25827 9.47553
    pos 0.191765 -8.83552 5.61098
    norm -2.88706 1.92562 9.37853
    pos -0.0299261 -7.59195 5.2301
    norm -2.19697 3.87407 8.95349
  }
  poly {
    numVertices 3
    pos -5.68235 6.67428 0.515124
    norm -7.4058 -0.233301 6.71563
    pos -5.70448 8.23418 0.42774
    norm -9.06602 0.144092 4.21741
    pos -5.82201 4.33718 0.299859
    norm -6.71597 -0.248429 7.40501
  }
  poly {
    numVertices 3
    pos -6.09022 -9.17202 2.34186
    norm -7.40475 0.743075 6.67963
    pos -5.59577 -8.97342 2.66893
    norm -5.65369 0.982106 8.1897
    pos -5.96826 -7.88366 2.26762
    norm -7.97156 2.15979 5.63823
  }
  poly {
    numVertices 3
    pos -5.82201 4.33718 0.299859
    norm -6.71597 -0.248429 7.40501
    pos -5.22402 6.57518 0.862218
    norm -4.59514 -0.820964 8.84369
    pos -5.68235 6.67428 0.515124
    norm -7.4058 -0.233301 6.71563
  }
  poly {
    numVertices 3
    pos -5.35082 -1.32634 -2.74761
    norm 2.54944 1.17083 -9.59841
    pos -5.40552 0.16046 -2.46657
    norm 2.65352 2.18544 -9.39056
    pos -4.82848 -0.196341 -2.26623
    norm 5.5997 1.58585 -8.13194
  }
  poly {
    numVertices 3
    pos -3.42154 -7.48252 3.49583
    norm -3.93015 1.20342 9.11624
    pos -4.83457 -7.89267 3.07841
    norm -3.74292 0.659117 9.24966
    pos -4.69667 -8.59835 3.17691
    norm -4.03678 0.617014 9.12818
  }
  poly {
    numVertices 3
    pos -6.24211 2.74096 -0.200528
    norm -8.29056 0.385716 5.57834
    pos -5.61575 2.92003 0.423229
    norm -4.51975 -0.407768 8.91098
    pos -5.82201 4.33718 0.299859
    norm -6.71597 -0.248429 7.40501
  }
  poly {
    numVertices 3
    pos -5.96826 -7.88366 2.26762
    norm -7.97156 2.15979 5.63823
    pos -5.48614 -7.78082 2.73145
    norm -5.5324 1.47646 8.19833
    pos -5.33063 -6.746 2.43856
    norm -6.81865 4.05886 6.08536
  }
  poly {
    numVertices 3
    pos -4.83457 -7.89267 3.07841
    norm -3.74292 0.659117 9.24966
    pos -3.42154 -7.48252 3.49583
    norm -3.93015 1.20342 9.11624
    pos -3.66308 -6.70622 3.43613
    norm -3.22591 1.59914 9.32932
  }
  poly {
    numVertices 3
    pos -5.22285 4.31418 0.629247
    norm -2.87914 -0.928193 9.53148
    pos -4.75588 6.33448 0.975947
    norm -0.537424 -1.34268 9.89487
    pos -5.22402 6.57518 0.862218
    norm -4.59514 -0.820964 8.84369
  }
  poly {
    numVertices 3
    pos 0.191765 -8.83552 5.61098
    norm -2.88706 1.92562 9.37853
    pos 1.40944 -7.87463 5.54854
    norm -2.27225 4.64026 8.56183
    pos -0.0299261 -7.59195 5.2301
    norm -2.19697 3.87407 8.95349
  }
  poly {
    numVertices 3
    pos -4.3253 -1.34326 -2.01213
    norm 6.76499 1.32773 -7.24377
    pos -5.35082 -1.32634 -2.74761
    norm 2.54944 1.17083 -9.59841
    pos -4.82848 -0.196341 -2.26623
    norm 5.5997 1.58585 -8.13194
  }
  poly {
    numVertices 3
    pos -3.91682 -3.02274 -2.01956
    norm 5.91153 2.82406 -7.55504
    pos -2.34291 -3.79958 -1.0176
    norm 7.08379 4.76794 -5.20448
    pos -3.06933 -3.75821 -1.76762
    norm 5.86742 4.02452 -7.02685
  }
  poly {
    numVertices 3
    pos 6.75368 -11.3013 1.63419
    norm 7.89281 1.31516 -5.99784
    pos 7.0604 -11.1212 2.16792
    norm 9.17089 1.40861 -3.72968
    pos 6.92175 -12.6385 1.96847
    norm 9.27388 -1.3204 -3.50026
  }
  poly {
    numVertices 3
    pos -4.3253 -1.34326 -2.01213
    norm 6.76499 1.32773 -7.24377
    pos -3.8842 -0.17551 -1.42712
    norm 8.45929 1.22575 -5.19018
    pos -3.798 -1.41512 -1.41323
    norm 8.64647 1.8449 -4.67279
  }
  poly {
    numVertices 3
    pos -1.10285 -3.98427 0.285084
    norm 5.66868 7.07042 -4.22792
    pos -2.34291 -3.79958 -1.0176
    norm 7.08379 4.76794 -5.20448
    pos -2.47359 -3.30754 -0.0466174
    norm 6.20974 7.66014 -1.66176
  }
  poly {
    numVertices 3
    pos -3.7529 -1.36371 -0.192882
    norm 9.0451 2.36578 3.54812
    pos -3.22086 -2.49356 0.0420619
    norm 7.05189 6.2924 3.26751
    pos -3.48307 -2.10146 -0.874187
    norm 9.14499 3.8158 -1.34493
  }
  poly {
    numVertices 3
    pos -5.48614 -7.78082 2.73145
    norm -5.5324 1.47646 8.19833
    pos -4.26162 -6.14556 2.99808
    norm -5.36902 4.23534 7.29626
    pos -5.33063 -6.746 2.43856
    norm -6.81865 4.05886 6.08536
  }
  poly {
    numVertices 3
    pos -4.56752 7.83438 -0.79817
    norm 3.64294 1.59626 -9.17502
    pos -4.21069 6.00638 -0.841693
    norm 6.98346 0.861646 -7.10555
    pos -4.70929 5.13828 -1.26631
    norm 4.614 1.41352 -8.7586
  }
  poly {
    numVertices 3
    pos -3.42154 -7.48252 3.49583
    norm -3.93015 1.20342 9.11624
    pos -2.84257 -6.72762 3.60283
    norm -3.14435 3.20493 8.93541
    pos -3.66308 -6.70622 3.43613
    norm -3.22591 1.59914 9.32932
  }
  poly {
    numVertices 3
    pos -4.2911 4.37588 0.509789
    norm 4.66363 -0.868751 8.80317
    pos -4.14416 6.34258 0.769167
    norm 5.7389 -1.36193 8.07528
    pos -4.90397 4.0578 0.6206
    norm 0.628265 -1.13764 9.9152
  }
  poly {
    numVertices 3
    pos -4.45284 -11.0302 -2.99362
    norm -8.65514 -2.57609 -4.29561
    pos -4.44626 -9.80535 -3.71284
    norm -6.99382 -0.865093 -7.09493
    pos -3.78867 -11.5941 -3.7573
    norm -7.40648 -1.66693 -6.50888
  }
  poly {
    numVertices 3
    pos 4.97961 -15.3822 -0.338328
    norm 2.2475 -1.2381 -9.66518
    pos 5.03338 -16.9736 0.343904
    norm -2.88769 -4.37162 -8.51764
    pos 4.38104 -15.3573 -0.19007
    norm -3.86855 -2.08298 -8.98307
  }
  poly {
    numVertices 3
    pos 10.2423 -14.7218 7.58383
    norm -4.23151 0.303576 9.0555
    pos 9.45757 -14.5149 7.30453
    norm -3.4148 -1.1364 9.32994
    pos 10.1191 -15.3859 7.4543
    norm -5.10142 -3.25194 7.96243
  }
  poly {
    numVertices 3
    pos 10.7132 -15.3275 7.90101
    norm -5.44395 -0.289947 8.38328
    pos 10.702 -14.262 7.7392
    norm -3.33812 2.62744 9.05282
    pos 10.2423 -14.7218 7.58383
    norm -4.23151 0.303576 9.0555
  }
  poly {
    numVertices 3
    pos 10.702 -14.262 7.7392
    norm -3.33812 2.62744 9.05282
    pos 10.14 -13.7587 7.4528
    norm -2.9087 1.48174 9.45219
    pos 10.2423 -14.7218 7.58383
    norm -4.23151 0.303576 9.0555
  }
  poly {
    numVertices 3
    pos -3.66308 -6.70622 3.43613
    norm -3.22591 1.59914 9.32932
    pos -2.84257 -6.72762 3.60283
    norm -3.14435 3.20493 8.93541
    pos -3.2464 -5.55141 3.19226
    norm -2.69615 5.13143 8.14857
  }
  poly {
    numVertices 3
    pos -0.0299261 -7.59195 5.2301
    norm -2.19697 3.87407 8.95349
    pos 0.118262 -6.74988 4.74419
    norm -2.00505 5.46897 8.12836
    pos -2.1032 -6.8853 4.14129
    norm -3.60766 4.55602 8.13803
  }
  poly {
    numVertices 3
    pos -5.28706 7.99038 0.975844
    norm -5.77012 -0.83039 8.12503
    pos -4.87718 9.30128 1.3301
    norm -2.26571 -1.63239 9.60218
    pos -5.4013 9.20118 1.04275
    norm -7.09335 -0.913391 6.98928
  }
  poly {
    numVertices 3
    pos -4.26162 -6.14556 2.99808
    norm -5.36902 4.23534 7.29626
    pos -5.60496 -6.43976 1.71988
    norm -7.91846 4.27069 4.36567
    pos -5.33063 -6.746 2.43856
    norm -6.81865 4.05886 6.08536
  }
  poly {
    numVertices 3
    pos 0.118262 -6.74988 4.74419
    norm -2.00505 5.46897 8.12836
    pos -1.78565 -5.33574 3.16769
    norm -1.93798 5.95125 7.79915
    pos -2.1032 -6.8853 4.14129
    norm -3.60766 4.55602 8.13803
  }
  poly {
    numVertices 3
    pos 0.118262 -6.74988 4.74419
    norm -2.00505 5.46897 8.12836
    pos 1.13881 -6.01673 4.42871
    norm -0.628232 6.48513 7.58607
    pos -1.78565 -5.33574 3.16769
    norm -1.93798 5.95125 7.79915
  }
  poly {
    numVertices 3
    pos 11.2811 -14.4095 8.1446
    norm -5.26387 3.75025 7.63069
    pos 11.8032 -13.8701 8.00983
    norm -1.52941 7.19719 6.77211
    pos 10.702 -14.262 7.7392
    norm -3.33812 2.62744 9.05282
  }
  poly {
    numVertices 3
    pos 11.087 -15.991 7.88525
    norm -5.75047 -4.4346 6.87505
    pos 10.1191 -15.3859 7.4543
    norm -5.10142 -3.25194 7.96243
    pos 8.86135 -15.5277 6.3165
    norm -4.56015 -8.37189 3.01935
  }
  poly {
    numVertices 3
    pos 10.7132 -15.3275 7.90101
    norm -5.44395 -0.289947 8.38328
    pos 10.1191 -15.3859 7.4543
    norm -5.10142 -3.25194 7.96243
    pos 11.087 -15.991 7.88525
    norm -5.75047 -4.4346 6.87505
  }
  poly {
    numVertices 3
    pos -5.87396 1.74018 0.199279
    norm -5.63666 -0.143113 8.25879
    pos -5.16347 1.65372 0.514513
    norm -1.3543 -0.579078 9.89093
    pos -5.61575 2.92003 0.423229
    norm -4.51975 -0.407768 8.91098
  }
  poly {
    numVertices 3
    pos -2.84257 -6.72762 3.60283
    norm -3.14435 3.20493 8.93541
    pos -2.58326 -5.15962 2.93761
    norm -1.51553 6.20177 7.69684
    pos -3.2464 -5.55141 3.19226
    norm -2.69615 5.13143 8.14857
  }
  poly {
    numVertices 3
    pos -3.66308 -6.70622 3.43613
    norm -3.22591 1.59914 9.32932
    pos -3.2464 -5.55141 3.19226
    norm -2.69615 5.13143 8.14857
    pos -4.26162 -6.14556 2.99808
    norm -5.36902 4.23534 7.29626
  }
  poly {
    numVertices 3
    pos -3.2464 -5.55141 3.19226
    norm -2.69615 5.13143 8.14857
    pos -2.58326 -5.15962 2.93761
    norm -1.51553 6.20177 7.69684
    pos -3.49371 -4.82625 2.27745
    norm -3.80578 5.76134 7.23347
  }
  poly {
    numVertices 3
    pos -5.9866 -6.63685 -0.256173
    norm -9.85918 -0.491795 -1.59838
    pos -6.06486 -6.60961 0.267663
    norm -9.84512 1.30847 1.16685
    pos -6.37764 -4.77125 -0.192311
    norm -9.64338 -0.808735 2.52017
  }
  poly {
    numVertices 3
    pos -6.06486 -6.60961 0.267663
    norm -9.84512 1.30847 1.16685
    pos -5.91146 -4.64372 0.615297
    norm -7.7944 1.00732 6.18325
    pos -6.37764 -4.77125 -0.192311
    norm -9.64338 -0.808735 2.52017
  }
  poly {
    numVertices 3
    pos 11.087 -15.991 7.88525
    norm -5.75047 -4.4346 6.87505
    pos 11.293 -14.9098 8.29398
    norm -6.13054 0.29173 7.89502
    pos 10.7132 -15.3275 7.90101
    norm -5.44395 -0.289947 8.38328
  }
  poly {
    numVertices 3
    pos -4.78387 -5.40819 1.89858
    norm -5.84149 4.85291 6.50586
    pos -5.60496 -6.43976 1.71988
    norm -7.91846 4.27069 4.36567
    pos -4.26162 -6.14556 2.99808
    norm -5.36902 4.23534 7.29626
  }
  poly {
    numVertices 3
    pos -3.2464 -5.55141 3.19226
    norm -2.69615 5.13143 8.14857
    pos -4.78387 -5.40819 1.89858
    norm -5.84149 4.85291 6.50586
    pos -4.26162 -6.14556 2.99808
    norm -5.36902 4.23534 7.29626
  }
  poly {
    numVertices 3
    pos 12.5064 -15.4412 9.36332
    norm -7.26255 -2.16443 6.52462
    pos 13.2549 -14.7931 10.3128
    norm -8.0047 3.59806 4.79363
    pos 12.4158 -14.8994 9.2069
    norm -6.3765 4.03786 6.56018
  }
  poly {
    numVertices 3
    pos -6.20664 -19.3608 -4.92499
    norm -4.21599 -8.80304 -2.17529
    pos -5.39593 -19.2514 -6.0843
    norm -4.47909 -1.31291 -8.84386
    pos -4.49025 -19.4335 -5.45155
    norm 3.35634 -9.41696 0.236478
  }
  poly {
    numVertices 3
    pos -4.78387 -5.40819 1.89858
    norm -5.84149 4.85291 6.50586
    pos -5.50148 -5.66874 1.22353
    norm -7.83408 2.50715 5.68695
    pos -5.60496 -6.43976 1.71988
    norm -7.91846 4.27069 4.36567
  }
  poly {
    numVertices 3
    pos 10.14 -13.7587 7.4528
    norm -2.9087 1.48174 9.45219
    pos 9.21038 -13.7496 7.20827
    norm -1.84563 0.888925 9.78792
    pos 9.45757 -14.5149 7.30453
    norm -3.4148 -1.1364 9.32994
  }
  poly {
    numVertices 3
    pos -4.95038 -4.76278 1.42374
    norm -5.38059 3.36811 7.7269
    pos -5.50148 -5.66874 1.22353
    norm -7.83408 2.50715 5.68695
    pos -4.78387 -5.40819 1.89858
    norm -5.84149 4.85291 6.50586
  }
  poly {
    numVertices 3
    pos -3.2464 -5.55141 3.19226
    norm -2.69615 5.13143 8.14857
    pos -3.49371 -4.82625 2.27745
    norm -3.80578 5.76134 7.23347
    pos -4.78387 -5.40819 1.89858
    norm -5.84149 4.85291 6.50586
  }
  poly {
    numVertices 3
    pos 10.2423 -14.7218 7.58383
    norm -4.23151 0.303576 9.0555
    pos 10.14 -13.7587 7.4528
    norm -2.9087 1.48174 9.45219
    pos 9.45757 -14.5149 7.30453
    norm -3.4148 -1.1364 9.32994
  }
  poly {
    numVertices 3
    pos -4.78387 -5.40819 1.89858
    norm -5.84149 4.85291 6.50586
    pos -3.49371 -4.82625 2.27745
    norm -3.80578 5.76134 7.23347
    pos -4.95038 -4.76278 1.42374
    norm -5.38059 3.36811 7.7269
  }
  poly {
    numVertices 3
    pos 10.1191 -15.3859 7.4543
    norm -5.10142 -3.25194 7.96243
    pos 10.7132 -15.3275 7.90101
    norm -5.44395 -0.289947 8.38328
    pos 10.2423 -14.7218 7.58383
    norm -4.23151 0.303576 9.0555
  }
  poly {
    numVertices 3
    pos -3.93149 -4.04676 -2.38923
    norm 4.04495 3.39763 -8.49085
    pos -4.72565 -3.46355 -2.63385
    norm 3.78939 1.23274 -9.17174
    pos -3.91682 -3.02274 -2.01956
    norm 5.91153 2.82406 -7.55504
  }
  poly {
    numVertices 3
    pos 9.21038 -13.7496 7.20827
    norm -1.84563 0.888925 9.78792
    pos 10.14 -13.7587 7.4528
    norm -2.9087 1.48174 9.45219
    pos 9.68214 -12.6627 7.02945
    norm -0.82539 3.09652 9.47261
  }
  poly {
    numVertices 3
    pos 13.5226 -15.8573 10.5449
    norm -7.75747 -4.87038 4.01261
    pos 13.2549 -14.7931 10.3128
    norm -8.0047 3.59806 4.79363
    pos 12.5064 -15.4412 9.36332
    norm -7.26255 -2.16443 6.52462
  }
  poly {
    numVertices 3
    pos -3.49371 -4.82625 2.27745
    norm -3.80578 5.76134 7.23347
    pos -2.58326 -5.15962 2.93761
    norm -1.51553 6.20177 7.69684
    pos -4.15879 -4.103 1.45667
    norm -3.14014 5.28119 7.88978
  }
  poly {
    numVertices 3
    pos 10.1191 -15.3859 7.4543
    norm -5.10142 -3.25194 7.96243
    pos 8.55083 -15.1088 6.68321
    norm -4.25555 -5.58175 7.12282
    pos 8.86135 -15.5277 6.3165
    norm -4.56015 -8.37189 3.01935
  }
  poly {
    numVertices 3
    pos -4.15879 -4.103 1.45667
    norm -3.14014 5.28119 7.88978
    pos -5.16035 -3.39729 0.676894
    norm -2.36296 3.29657 9.14051
    pos -5.74712 -3.53181 0.511146
    norm -5.86776 2.13709 7.8104
  }
  poly {
    numVertices 3
    pos -2.58326 -5.15962 2.93761
    norm -1.51553 6.20177 7.69684
    pos -3.7561 -3.66817 1.18189
    norm -1.12644 5.95345 7.95535
    pos -4.15879 -4.103 1.45667
    norm -3.14014 5.28119 7.88978
  }
  poly {
    numVertices 3
    pos -6.73434 -1.6125 -1.00367
    norm -9.57322 -0.188188 2.88409
    pos -6.37764 -4.77125 -0.192311
    norm -9.64338 -0.808735 2.52017
    pos -6.32544 -2.85629 -0.131951
    norm -7.90495 0.333222 6.11562
  }
  poly {
    numVertices 3
    pos -6.37764 -4.77125 -0.192311
    norm -9.64338 -0.808735 2.52017
    pos -5.74712 -3.53181 0.511146
    norm -5.86776 2.13709 7.8104
    pos -6.32544 -2.85629 -0.131951
    norm -7.90495 0.333222 6.11562
  }
  poly {
    numVertices 3
    pos -4.15879 -4.103 1.45667
    norm -3.14014 5.28119 7.88978
    pos -3.7561 -3.66817 1.18189
    norm -1.12644 5.95345 7.95535
    pos -5.16035 -3.39729 0.676894
    norm -2.36296 3.29657 9.14051
  }
  poly {
    numVertices 3
    pos -3.93149 -4.04676 -2.38923
    norm 4.04495 3.39763 -8.49085
    pos -3.91682 -3.02274 -2.01956
    norm 5.91153 2.82406 -7.55504
    pos -3.06933 -3.75821 -1.76762
    norm 5.86742 4.02452 -7.02685
  }
  poly {
    numVertices 3
    pos -4.41575 -5.39906 -2.72035
    norm 0.304998 0.0797159 -9.99504
    pos -5.48056 -2.86297 -2.79063
    norm -0.0272999 0.0652342 -9.99975
    pos -4.72565 -3.46355 -2.63385
    norm 3.78939 1.23274 -9.17174
  }
  poly {
    numVertices 3
    pos -5.16035 -3.39729 0.676894
    norm -2.36296 3.29657 9.14051
    pos -3.7561 -3.66817 1.18189
    norm -1.12644 5.95345 7.95535
    pos -4.47609 -2.48377 0.540099
    norm 0.315882 2.7505 9.60911
  }
  poly {
    numVertices 3
    pos 7.25101 -13.1962 6.91231
    norm -3.06769 -1.87857 9.33061
    pos 6.49444 -13.181 6.57407
    norm -3.31623 -3.50608 8.75842
    pos 7.42283 -14.3698 6.54049
    norm -4.24943 -4.21384 8.01161
  }
  poly {
    numVertices 3
    pos -6.32544 -2.85629 -0.131951
    norm -7.90495 0.333222 6.11562
    pos -5.74712 -3.53181 0.511146
    norm -5.86776 2.13709 7.8104
    pos -5.87569 -2.37263 0.248807
    norm -4.55692 1.03449 8.84106
  }
  poly {
    numVertices 3
    pos -5.16035 -3.39729 0.676894
    norm -2.36296 3.29657 9.14051
    pos -4.47609 -2.48377 0.540099
    norm 0.315882 2.7505 9.60911
    pos -5.04454 -1.96564 0.431794
    norm -1.1306 1.1066 9.87407
  }
  poly {
    numVertices 3
    pos 14.0665 -14.8584 12.9211
    norm -9.77944 -1.60865 1.33222
    pos 14.3549 -13.8269 13.5641
    norm -4.00888 9.13736 -0.661526
    pos 14.6149 -14.0403 13.3443
    norm 4.62677 8.73814 1.49593
  }
  poly {
    numVertices 3
    pos -5.87569 -2.37263 0.248807
    norm -4.55692 1.03449 8.84106
    pos -6.18769 -1.09847 -0.130771
    norm -6.52266 0.131418 7.57877
    pos -6.32544 -2.85629 -0.131951
    norm -7.90495 0.333222 6.11562
  }
  poly {
    numVertices 3
    pos -5.87569 -2.37263 0.248807
    norm -4.55692 1.03449 8.84106
    pos -5.76876 -0.84932 0.16111
    norm -4.3658 0.1777 8.9949
    pos -6.18769 -1.09847 -0.130771
    norm -6.52266 0.131418 7.57877
  }
  poly {
    numVertices 3
    pos 6.46578 -13.7723 3.93901
    norm 3.91257 -9.19225 -0.440992
    pos 5.88352 -14.1734 4.86874
    norm -0.798628 -9.82047 1.70898
    pos 6.13391 -14.1456 4.45689
    norm 4.24323 -8.88238 1.76021
  }
  poly {
    numVertices 3
    pos -5.04454 -1.96564 0.431794
    norm -1.1306 1.1066 9.87407
    pos -4.50336 -1.00268 0.358761
    norm 2.33573 0.553308 9.70764
    pos -4.97643 -0.57256 0.3864
    norm -1.27418 0.0279944 9.91845
  }
  poly {
    numVertices 3
    pos -3.23906 -2.74681 -1.22788
    norm 7.77713 3.54821 -5.18909
    pos -3.798 -1.41512 -1.41323
    norm 8.64647 1.8449 -4.67279
    pos -3.48307 -2.10146 -0.874187
    norm 9.14499 3.8158 -1.34493
  }
  poly {
    numVertices 3
    pos -6.53347 -0.893021 -0.548512
    norm -8.48769 -0.0512964 5.2874
    pos -6.18769 -1.09847 -0.130771
    norm -6.52266 0.131418 7.57877
    pos -6.38285 1.25007 -0.232028
    norm -7.90629 0.119105 6.12178
  }
  poly {
    numVertices 3
    pos -4.97643 -0.57256 0.3864
    norm -1.27418 0.0279944 9.91845
    pos -5.51697 0.50327 0.346612
    norm -3.18266 -0.484049 9.46765
    pos -5.76876 -0.84932 0.16111
    norm -4.3658 0.1777 8.9949
  }
  poly {
    numVertices 3
    pos -6.38285 1.25007 -0.232028
    norm -7.90629 0.119105 6.12178
    pos -6.70153 0.32487 -0.818477
    norm -9.77228 0.453305 2.0729
    pos -6.53347 -0.893021 -0.548512
    norm -8.48769 -0.0512964 5.2874
  }
  poly {
    numVertices 3
    pos -5.51697 0.50327 0.346612
    norm -3.18266 -0.484049 9.46765
    pos -5.16347 1.65372 0.514513
    norm -1.3543 -0.579078 9.89093
    pos -5.87396 1.74018 0.199279
    norm -5.63666 -0.143113 8.25879
  }
  poly {
    numVertices 3
    pos -5.57262 10.1447 0.780347
    norm -9.5592 0.382919 2.91118
    pos -5.70448 8.23418 0.42774
    norm -9.06602 0.144092 4.21741
    pos -5.4013 9.20118 1.04275
    norm -7.09335 -0.913391 6.98928
  }
  poly {
    numVertices 3
    pos -5.4013 9.20118 1.04275
    norm -7.09335 -0.913391 6.98928
    pos -5.00519 10.6159 1.59781
    norm -4.66419 -1.49476 8.71843
    pos -5.57262 10.1447 0.780347
    norm -9.5592 0.382919 2.91118
  }
  poly {
    numVertices 3
    pos 6.65273 -17.2559 2.73433
    norm 6.34607 3.39605 6.94221
    pos 6.14551 -17.6639 3.10086
    norm 0.518151 1.5958 9.85824
    pos 6.64033 -18.4669 3.05128
    norm 4.54137 -1.31627 8.81155
  }
  poly {
    numVertices 3
    pos 1.77574 -18.4491 5.91374
    norm 9.02425 -1.17894 4.14403
    pos 1.88938 -17.1928 5.6789
    norm 9.71842 -0.930545 2.16478
    pos 1.11266 -17.1594 6.36618
    norm 3.1409 -0.394874 9.48572
  }
  poly {
    numVertices 3
    pos 14.0665 -14.8584 12.9211
    norm -9.77944 -1.60865 1.33222
    pos 14.6149 -14.0403 13.3443
    norm 4.62677 8.73814 1.49593
    pos 14.2153 -13.7597 12.7611
    norm -2.9103 9.56188 -0.317319
  }
  poly {
    numVertices 3
    pos 1.67222 -17.6491 4.94128
    norm 9.29694 -0.492403 -3.65028
    pos 1.81833 -16.2318 4.58615
    norm 9.02872 -2.04429 -3.78195
    pos 1.88938 -17.1928 5.6789
    norm 9.71842 -0.930545 2.16478
  }
  poly {
    numVertices 3
    pos 5.88241 -17.9389 0.617416
    norm -2.21668 2.20694 -9.4982
    pos 5.03338 -16.9736 0.343904
    norm -2.88769 -4.37162 -8.51764
    pos 6.48723 -18.1052 0.690953
    norm 3.23443 0.65688 -9.43965
  }
  poly {
    numVertices 3
    pos 1.11266 -17.1594 6.36618
    norm 3.1409 -0.394874 9.48572
    pos 1.88938 -17.1928 5.6789
    norm 9.71842 -0.930545 2.16478
    pos 1.67285 -14.3322 6.3714
    norm 4.82088 -3.01384 8.22653
  }
  poly {
    numVertices 3
    pos -4.99445 -9.69996 -2.84098
    norm -8.56445 -1.05053 -5.05437
    pos -4.90419 -8.06049 -2.56106
    norm -8.05375 1.27667 -5.78853
    pos -4.70895 -8.53872 -3.14765
    norm -7.4631 2.37566 -6.21758
  }
  poly {
    numVertices 3
    pos 14.6805 -15.7524 11.8333
    norm 0.69947 -9.1607 3.94872
    pos 15.0791 -15.5421 11.6782
    norm 8.5558 -5.16098 -0.403146
    pos 15.3589 -14.9015 12.4757
    norm 9.70648 -1.07606 2.1509
  }
  poly {
    numVertices 3
    pos -4.90419 -8.06049 -2.56106
    norm -8.05375 1.27667 -5.78853
    pos -5.22259 -8.05642 -2.08366
    norm -8.27872 0.101808 -5.60825
    pos -4.99141 -6.75002 -2.38271
    norm -5.32882 -1.11321 -8.38835
  }
  poly {
    numVertices 3
    pos -4.03784 0.88588 0.0193272
    norm 8.05782 0.248429 5.9169
    pos -4.50336 -1.00268 0.358761
    norm 2.33573 0.553308 9.70764
    pos -4.10131 -1.57873 0.24944
    norm 4.88343 2.51075 8.35752
  }
  poly {
    numVertices 3
    pos 15.3589 -14.9015 12.4757
    norm 9.70648 -1.07606 2.1509
    pos 14.9597 -14.3451 12.6113
    norm 6.04957 7.51608 2.62892
    pos 15.0512 -15.3047 12.6233
    norm 6.92159 -6.49921 3.13877
  }
  poly {
    numVertices 3
    pos 15.0512 -15.3047 12.6233
    norm 6.92159 -6.49921 3.13877
    pos 14.9597 -14.3451 12.6113
    norm 6.04957 7.51608 2.62892
    pos 15.2959 -14.58 13.1373
    norm 9.41907 2.89557 1.70202
  }
  poly {
    numVertices 3
    pos -3.85952 10.602 1.11018
    norm 9.17845 -0.808337 3.88624
    pos -3.83307 9.29778 0.392387
    norm 9.9715 0.277973 -0.701367
    pos -3.8764 10.6555 0.360523
    norm 9.34649 0.484279 -3.52259
  }
  poly {
    numVertices 3
    pos -5.65267 -6.6997 -1.84277
    norm -8.19825 -2.69973 -5.04977
    pos -5.78898 -5.3049 -2.60849
    norm -4.80425 -2.17328 -8.49683
    pos -4.99141 -6.75002 -2.38271
    norm -5.32882 -1.11321 -8.38835
  }
  poly {
    numVertices 3
    pos -4.99141 -6.75002 -2.38271
    norm -5.32882 -1.11321 -8.38835
    pos -4.41575 -5.39906 -2.72035
    norm 0.304998 0.0797159 -9.99504
    pos -4.33039 -6.57474 -2.704
    norm -4.34739 1.7048 -8.84273
  }
  poly {
    numVertices 3
    pos -3.94628 2.11456 -0.763765
    norm 9.93457 0.42033 -1.0619
    pos -3.88598 4.77008 -0.422515
    norm 9.42815 0.236647 -3.32476
    pos -3.99182 4.45988 0.161384
    norm 9.51856 -0.555067 3.01477
  }
  poly {
    numVertices 3
    pos -3.94628 2.11456 -0.763765
    norm 9.93457 0.42033 -1.0619
    pos -4.33288 2.84798 -1.3933
    norm 7.4742 1.04776 -6.56038
    pos -3.88598 4.77008 -0.422515
    norm 9.42815 0.236647 -3.32476
  }
  poly {
    numVertices 3
    pos -5.65267 -6.6997 -1.84277
    norm -8.19825 -2.69973 -5.04977
    pos -6.23098 -4.79782 -2.29041
    norm -8.30328 -2.83787 -4.79604
    pos -5.78898 -5.3049 -2.60849
    norm -4.80425 -2.17328 -8.49683
  }
  poly {
    numVertices 3
    pos 1.64301 -18.8177 4.6335
    norm 9.08146 -0.664226 -4.13349
    pos 1.34481 -17.6313 4.32421
    norm 7.75381 -1.02879 -6.23058
    pos 1.67222 -17.6491 4.94128
    norm 9.29694 -0.492403 -3.65028
  }
  poly {
    numVertices 3
    pos -2.05895 -13.9549 -4.22049
    norm 5.54484 -0.426677 -8.311
    pos -1.62787 -14.1294 -3.74326
    norm 8.44954 -1.83973 -5.02204
    pos -2.43726 -15.3994 -3.93026
    norm 8.14933 -4.94443 -3.02343
  }
  poly {
    numVertices 3
    pos -4.14416 6.34258 0.769167
    norm 5.7389 -1.36193 8.07528
    pos -3.95827 8.53838 0.818801
    norm 9.01917 -0.740401 4.25517
    pos -4.31311 8.69838 1.23407
    norm 4.55417 -1.47168 8.78031
  }
  poly {
    numVertices 3
    pos -5.22259 -8.05642 -2.08366
    norm -8.27872 0.101808 -5.60825
    pos -5.65267 -6.6997 -1.84277
    norm -8.19825 -2.69973 -5.04977
    pos -4.99141 -6.75002 -2.38271
    norm -5.32882 -1.11321 -8.38835
  }
  poly {
    numVertices 3
    pos 11.2811 -14.4095 8.1446
    norm -5.26387 3.75025 7.63069
    pos 10.702 -14.262 7.7392
    norm -3.33812 2.62744 9.05282
    pos 10.7132 -15.3275 7.90101
    norm -5.44395 -0.289947 8.38328
  }
  poly {
    numVertices 3
    pos -3.3886 -12.9942 -4.04817
    norm -6.60869 1.92688 -7.25344
    pos -3.78867 -11.5941 -3.7573
    norm -7.40648 -1.66693 -6.50888
    pos -3.61009 -10.4979 -4.00697
    norm -3.01526 0.0475826 -9.53446
  }
  poly {
    numVertices 3
    pos -2.77505 -15.7261 -3.26141
    norm 6.56821 -6.21017 4.27696
    pos -1.79687 -14.7058 -2.95384
    norm 7.30355 -5.18284 4.4493
    pos -2.19534 -14.5856 -2.5191
    norm 5.02069 -4.6074 7.31879
  }
  poly {
    numVertices 3
    pos 13.833 -14.9394 11.7083
    norm -9.77509 0.805147 1.94915
    pos 14.0665 -14.8584 12.9211
    norm -9.77944 -1.60865 1.33222
    pos 14.2199 -14.1732 12.2989
    norm -5.06096 8.6173 0.359022
  }
  poly {
    numVertices 3
    pos -3.40532 -16.8933 -4.54874
    norm 9.26184 -1.48681 -3.4652
    pos -2.73953 -15.3335 -4.33695
    norm 6.40496 -1.51863 -7.52797
    pos -2.43726 -15.3994 -3.93026
    norm 8.14933 -4.94443 -3.02343
  }
  poly {
    numVertices 3
    pos 5.03743 -7.43863 5.19513
    norm 1.90716 6.02343 7.7512
    pos 5.00063 -8.51581 5.97394
    norm 2.33725 5.28274 8.16272
    pos 6.07394 -8.68997 5.81693
    norm 2.05825 5.0029 8.41039
  }
  poly {
    numVertices 3
    pos 11.087 -15.991 7.88525
    norm -5.75047 -4.4346 6.87505
    pos 12.431 -16.2295 9.06171
    norm -5.26734 -6.52142 5.45218
    pos 12.5064 -15.4412 9.36332
    norm -7.26255 -2.16443 6.52462
  }
  poly {
    numVertices 3
    pos 6.07394 -8.68997 5.81693
    norm 2.05825 5.0029 8.41039
    pos 5.00063 -8.51581 5.97394
    norm 2.33725 5.28274 8.16272
    pos 6.52046 -10.2486 6.39982
    norm 2.01654 3.13315 9.27992
  }
  poly {
    numVertices 3
    pos 0.916356 -19.1587 6.45121
    norm 1.08535 -2.93586 9.49751
    pos 1.34363 -19.5344 6.11335
    norm 5.19898 -5.25268 6.73646
    pos 1.11266 -17.1594 6.36618
    norm 3.1409 -0.394874 9.48572
  }
  poly {
    numVertices 3
    pos 14.7716 -14.9307 10.6684
    norm 8.60765 3.63719 -3.56081
    pos 13.9479 -14.3227 10.2476
    norm -0.137379 9.99617 0.240422
    pos 14.4181 -14.159 11.098
    norm 0.628696 9.89623 -1.29207
  }
  poly {
    numVertices 3
    pos -4.03784 0.88588 0.0193272
    norm 8.05782 0.248429 5.9169
    pos -4.65145 0.78429 0.426296
    norm 1.93167 -0.466882 9.80054
    pos -4.50336 -1.00268 0.358761
    norm 2.33573 0.553308 9.70764
  }
  poly {
    numVertices 3
    pos 8.76807 -14.6156 7.03628
    norm -3.17875 -2.74516 9.07522
    pos 7.25101 -13.1962 6.91231
    norm -3.06769 -1.87857 9.33061
    pos 7.42283 -14.3698 6.54049
    norm -4.24943 -4.21384 8.01161
  }
  poly {
    numVertices 3
    pos -3.09565 -11.3845 -4.19898
    norm -1.19905 0.884341 -9.88839
    pos -3.3886 -12.9942 -4.04817
    norm -6.60869 1.92688 -7.25344
    pos -3.61009 -10.4979 -4.00697
    norm -3.01526 0.0475826 -9.53446
  }
  poly {
    numVertices 3
    pos -5.40614 -10.1019 -2.23332
    norm -8.29182 -1.4815 -5.38988
    pos -4.51294 -11.9106 -1.79198
    norm -6.63337 -7.44736 -0.731678
    pos -5.40757 -11.031 -1.79466
    norm -7.23615 -4.23949 -5.44654
  }
  poly {
    numVertices 3
    pos 4.18665 -18.8733 1.83591
    norm -9.04527 2.70881 3.29324
    pos 4.7818 -18.0847 2.10223
    norm -9.96826 0.236757 0.760023
    pos 4.78829 -18.1591 1.58407
    norm -8.49068 3.96241 -3.49395
  }
  poly {
    numVertices 3
    pos 5.8825 -11.6913 6.83246
    norm 0.0785027 -0.309978 9.99488
    pos 6.52046 -10.2486 6.39982
    norm 2.01654 3.13315 9.27992
    pos 5.15808 -11.259 7.02667
    norm 1.60188 -1.11773 9.80738
  }
  poly {
    numVertices 3
    pos -3.99182 4.45988 0.161384
    norm 9.51856 -0.555067 3.01477
    pos -4.03784 0.88588 0.0193272
    norm 8.05782 0.248429 5.9169
    pos -3.94628 2.11456 -0.763765
    norm 9.93457 0.42033 -1.0619
  }
  poly {
    numVertices 3
    pos 14.1917 -14.694 14.1011
    norm -6.84043 -5.78602 4.4419
    pos 14.8238 -14.6449 14.4049
    norm 3.75364 -5.77145 7.25262
    pos 14.3959 -13.9987 14.4834
    norm -0.196616 4.91428 8.70696
  }
  poly {
    numVertices 3
    pos -3.95827 8.53838 0.818801
    norm 9.01917 -0.740401 4.25517
    pos -3.76933 8.38318 0.0669733
    norm 9.7092 0.543715 -2.33146
    pos -3.83307 9.29778 0.392387
    norm 9.9715 0.277973 -0.701367
  }
  poly {
    numVertices 3
    pos -4.31311 8.69838 1.23407
    norm 4.55417 -1.47168 8.78031
    pos -3.95827 8.53838 0.818801
    norm 9.01917 -0.740401 4.25517
    pos -3.85952 10.602 1.11018
    norm 9.17845 -0.808337 3.88624
  }
  poly {
    numVertices 3
    pos -2.34586 -12.94 -4.21181
    norm 3.45429 0.986163 -9.33249
    pos -3.0696 -12.7332 -4.27864
    norm -2.75791 1.24789 -9.53083
    pos -3.09565 -11.3845 -4.19898
    norm -1.19905 0.884341 -9.88839
  }
  poly {
    numVertices 3
    pos 13.2549 -14.7931 10.3128
    norm -8.0047 3.59806 4.79363
    pos 13.9479 -14.3227 10.2476
    norm -0.137379 9.99617 0.240422
    pos 12.4158 -14.8994 9.2069
    norm -6.3765 4.03786 6.56018
  }
  poly {
    numVertices 3
    pos -1.08883 -4.78023 2.76778
    norm 0.0327969 7.6055 6.49271
    pos -1.78565 -5.33574 3.16769
    norm -1.93798 5.95125 7.79915
    pos 1.13881 -6.01673 4.42871
    norm -0.628232 6.48513 7.58607
  }
  poly {
    numVertices 3
    pos 5.03743 -7.43863 5.19513
    norm 1.90716 6.02343 7.7512
    pos 6.07394 -8.68997 5.81693
    norm 2.05825 5.0029 8.41039
    pos 6.07528 -7.88127 5.21381
    norm 4.44789 6.86933 5.74706
  }
  poly {
    numVertices 3
    pos -3.99182 4.45988 0.161384
    norm 9.51856 -0.555067 3.01477
    pos -4.2911 4.37588 0.509789
    norm 4.66363 -0.868751 8.80317
    pos -4.03784 0.88588 0.0193272
    norm 8.05782 0.248429 5.9169
  }
  poly {
    numVertices 3
    pos -4.31311 8.69838 1.23407
    norm 4.55417 -1.47168 8.78031
    pos -3.85952 10.602 1.11018
    norm 9.17845 -0.808337 3.88624
    pos -4.30081 10.6338 1.62579
    norm 2.44105 -1.9551 9.49835
  }
  poly {
    numVertices 3
    pos 7.70443 -10.2055 4.42118
    norm 7.85518 5.2371 -3.2968
    pos 6.62089 -9.35893 2.58673
    norm 8.27734 4.20574 -3.7145
    pos 6.24673 -8.40136 3.8262
    norm 7.35641 6.3507 -2.35625
  }
  poly {
    numVertices 3
    pos -4.50336 -1.00268 0.358761
    norm 2.33573 0.553308 9.70764
    pos -5.04454 -1.96564 0.431794
    norm -1.1306 1.1066 9.87407
    pos -4.47609 -2.48377 0.540099
    norm 0.315882 2.7505 9.60911
  }
  poly {
    numVertices 3
    pos 6.89534 -18.8223 0.449338
    norm 7.27302 0.864053 -6.80858
    pos 7.00204 -18.0865 0.909847
    norm 5.40778 3.8668 -7.4702
    pos 7.42725 -18.6062 1.34154
    norm 9.37498 0.705162 -3.40773
  }
  poly {
    numVertices 3
    pos 6.02434 -19.4702 -0.272587
    norm 0.540327 -4.5337 -8.89683
    pos 6.33275 -18.7768 0.0730906
    norm 3.32945 4.45133 -8.31267
    pos 6.89534 -18.8223 0.449338
    norm 7.27302 0.864053 -6.80858
  }
  poly {
    numVertices 3
    pos -4.1396 -14.962 -4.7755
    norm -2.63305 3.36237 -9.04221
    pos -4.79786 -16.1441 -4.88718
    norm -6.05979 2.91969 -7.39962
    pos -5.41685 -15.8033 -4.18426
    norm -8.46398 3.58729 -3.93604
  }
  poly {
    numVertices 3
    pos 6.33275 -18.7768 0.0730906
    norm 3.32945 4.45133 -8.31267
    pos 7.00204 -18.0865 0.909847
    norm 5.40778 3.8668 -7.4702
    pos 6.89534 -18.8223 0.449338
    norm 7.27302 0.864053 -6.80858
  }
  poly {
    numVertices 3
    pos -6.77095 -2.81837 -2.24608
    norm -9.63537 -1.14348 -2.41912
    pos -6.37764 -4.77125 -0.192311
    norm -9.64338 -0.808735 2.52017
    pos -6.73434 -1.6125 -1.00367
    norm -9.57322 -0.188188 2.88409
  }
  poly {
    numVertices 3
    pos -4.53285 -19.0919 -6.25921
    norm 3.24862 -1.65157 -9.31229
    pos -4.58881 -18.4664 -5.96442
    norm -1.03224 3.63142 -9.25998
    pos -3.85971 -18.3206 -5.59526
    norm 5.88002 2.64735 -7.64309
  }
  poly {
    numVertices 3
    pos 13.8352 -15.4399 11.3672
    norm -8.20566 -4.45987 3.57445
    pos 13.8867 -14.3911 11.2997
    norm -7.42869 6.68461 0.361266
    pos 13.4294 -15.0981 10.7541
    norm -9.20901 0.456894 3.87109
  }
  poly {
    numVertices 3
    pos -5.6559 -17.7624 1.87307
    norm -9.8892 0.938324 -1.15032
    pos -6.12839 -18.9616 2.31741
    norm -9.08452 0.785912 4.10534
    pos -5.46907 -18.1557 2.47566
    norm -8.77681 2.5456 4.06048
  }
  poly {
    numVertices 3
    pos -5.0548 -19.1499 -0.108273
    norm 3.25779 -3.50773 -8.77967
    pos -4.17299 -19.0353 0.497947
    norm 6.1323 -0.272282 -7.89436
    pos -4.61753 -19.4945 0.453298
    norm 3.03687 -8.53415 -4.23624
  }
  poly {
    numVertices 3
    pos -4.99445 -9.69996 -2.84098
    norm -8.56445 -1.05053 -5.05437
    pos -4.44626 -9.80535 -3.71284
    norm -6.99382 -0.865093 -7.09493
    pos -4.45284 -11.0302 -2.99362
    norm -8.65514 -2.57609 -4.29561
  }
  poly {
    numVertices 3
    pos -5.65118 -19.1634 -0.121674
    norm -6.26051 -4.53746 -6.34172
    pos -5.27899 -18.8243 -0.162918
    norm 1.72187 3.43433 -9.23258
    pos -5.0548 -19.1499 -0.108273
    norm 3.25779 -3.50773 -8.77967
  }
  poly {
    numVertices 3
    pos 6.63421 -17.1719 0.818538
    norm 7.58243 1.68217 -6.29897
    pos 7.00204 -18.0865 0.909847
    norm 5.40778 3.8668 -7.4702
    pos 6.48723 -18.1052 0.690953
    norm 3.23443 0.65688 -9.43965
  }
  poly {
    numVertices 3
    pos -3.56044 -18.5697 -5.15075
    norm 9.07698 -2.52043 -3.35499
    pos -3.85971 -18.3206 -5.59526
    norm 5.88002 2.64735 -7.64309
    pos -3.66628 -17.5727 -5.07279
    norm 6.98266 1.91832 -6.89656
  }
  poly {
    numVertices 3
    pos -3.85971 -18.3206 -5.59526
    norm 5.88002 2.64735 -7.64309
    pos -4.58881 -18.4664 -5.96442
    norm -1.03224 3.63142 -9.25998
    pos -4.29227 -17.5202 -5.39181
    norm 1.52838 3.381 -9.28617
  }
  poly {
    numVertices 3
    pos -4.58881 -18.4664 -5.96442
    norm -1.03224 3.63142 -9.25998
    pos -5.39593 -19.2514 -6.0843
    norm -4.47909 -1.31291 -8.84386
    pos -4.29227 -17.5202 -5.39181
    norm 1.52838 3.381 -9.28617
  }
  poly {
    numVertices 3
    pos 1.64301 -18.8177 4.6335
    norm 9.08146 -0.664226 -4.13349
    pos 1.06354 -18.3718 4.20986
    norm 5.13237 0.0353872 -8.5824
    pos 1.34481 -17.6313 4.32421
    norm 7.75381 -1.02879 -6.23058
  }
  poly {
    numVertices 3
    pos 0.615579 -18.4413 3.97877
    norm 1.41317 2.72626 -9.51685
    pos 0.508093 -17.5007 3.96244
    norm -2.8014 -0.75583 -9.56979
    pos 1.06354 -18.3718 4.20986
    norm 5.13237 0.0353872 -8.5824
  }
  poly {
    numVertices 3
    pos 0.615579 -18.4413 3.97877
    norm 1.41317 2.72626 -9.51685
    pos -0.132075 -18.0928 4.40838
    norm -6.58332 4.50687 -6.02893
    pos 0.508093 -17.5007 3.96244
    norm -2.8014 -0.75583 -9.56979
  }
  poly {
    numVertices 3
    pos -3.85971 -18.3206 -5.59526
    norm 5.88002 2.64735 -7.64309
    pos -4.29227 -17.5202 -5.39181
    norm 1.52838 3.381 -9.28617
    pos -3.66628 -17.5727 -5.07279
    norm 6.98266 1.91832 -6.89656
  }
  poly {
    numVertices 3
    pos -5.64277 -18.6945 0.0114914
    norm -5.64334 6.91606 -4.50787
    pos -5.27899 -18.8243 -0.162918
    norm 1.72187 3.43433 -9.23258
    pos -5.65118 -19.1634 -0.121674
    norm -6.26051 -4.53746 -6.34172
  }
  poly {
    numVertices 3
    pos -3.8676 -7.09659 -3.12022
    norm -4.37957 4.03862 -8.03174
    pos -2.99029 -5.97223 -2.84402
    norm -1.02474 5.49437 -8.29227
    pos -3.74502 -7.58815 -3.6016
    norm -4.07146 4.11782 -8.15272
  }
  poly {
    numVertices 3
    pos 3.38326 -14.2725 1.69782
    norm -7.40815 -6.63655 1.03705
    pos 3.98159 -14.4435 2.56982
    norm -4.78334 -7.88491 3.86624
    pos 2.5949 -13.6132 1.74831
    norm -3.32224 -9.38673 -0.923094
  }
  poly {
    numVertices 3
    pos -4.29227 -17.5202 -5.39181
    norm 1.52838 3.381 -9.28617
    pos -3.76136 -16.9466 -5.05756
    norm 6.25336 0.271389 -7.79883
    pos -3.66628 -17.5727 -5.07279
    norm 6.98266 1.91832 -6.89656
  }
  poly {
    numVertices 3
    pos -5.94909 4.15499 -1.51447
    norm -5.55342 2.42297 -7.95543
    pos -5.48677 8.83448 -0.682687
    norm -6.53133 2.0409 -7.29221
    pos -5.23633 4.51948 -1.50026
    norm 0.50282 2.06563 -9.77141
  }
  poly {
    numVertices 3
    pos -3.83874 -18.0208 0.935175
    norm 6.8127 0.416185 -7.30849
    pos -4.73778 -18.4412 0.38361
    norm 1.70897 3.82109 -9.08178
    pos -4.30699 -17.3974 0.595372
    norm 3.62504 0.203236 -9.3176
  }
  poly {
    numVertices 3
    pos -4.33039 -6.57474 -2.704
    norm -4.34739 1.7048 -8.84273
    pos -3.8676 -7.09659 -3.12022
    norm -4.37957 4.03862 -8.03174
    pos -4.70895 -8.53872 -3.14765
    norm -7.4631 2.37566 -6.21758
  }
  poly {
    numVertices 3
    pos -5.8034 -7.88741 -1.12425
    norm -9.08584 -0.153234 -4.17421
    pos -5.65267 -6.6997 -1.84277
    norm -8.19825 -2.69973 -5.04977
    pos -5.22259 -8.05642 -2.08366
    norm -8.27872 0.101808 -5.60825
  }
  poly {
    numVertices 3
    pos 4.60674 -19.3379 2.15178
    norm -4.37939 -7.62477 4.76276
    pos 4.68402 -18.8672 2.53061
    norm -6.64904 -2.30815 7.10371
    pos 4.18665 -18.8733 1.83591
    norm -9.04527 2.70881 3.29324
  }
  poly {
    numVertices 3
    pos -4.73778 -18.4412 0.38361
    norm 1.70897 3.82109 -9.08178
    pos -5.019 -16.8262 0.536212
    norm -4.15483 0.123568 -9.09517
    pos -4.30699 -17.3974 0.595372
    norm 3.62504 0.203236 -9.3176
  }
  poly {
    numVertices 3
    pos -4.30699 -17.3974 0.595372
    norm 3.62504 0.203236 -9.3176
    pos -4.43158 -16.409 0.551847
    norm 2.92083 0.433753 -9.55409
    pos -3.83874 -18.0208 0.935175
    norm 6.8127 0.416185 -7.30849
  }
  poly {
    numVertices 3
    pos -5.019 -16.8262 0.536212
    norm -4.15483 0.123568 -9.09517
    pos -4.43158 -16.409 0.551847
    norm 2.92083 0.433753 -9.55409
    pos -4.30699 -17.3974 0.595372
    norm 3.62504 0.203236 -9.3176
  }
  poly {
    numVertices 3
    pos 6.081 -16.2644 0.324269
    norm 6.38897 0.561773 -7.67239
    pos 4.97961 -15.3822 -0.338328
    norm 2.2475 -1.2381 -9.66518
    pos 5.60475 -15.3098 0.0731382
    norm 6.35995 0.365823 -7.70826
  }
  poly {
    numVertices 3
    pos -3.83874 -18.0208 0.935175
    norm 6.8127 0.416185 -7.30849
    pos -4.43158 -16.409 0.551847
    norm 2.92083 0.433753 -9.55409
    pos -3.98486 -15.7726 0.775604
    norm 5.25154 0.418081 -8.49979
  }
  poly {
    numVertices 3
    pos 6.80751 -16.8562 2.14021
    norm 8.21816 4.10041 3.95582
    pos 6.65273 -17.2559 2.73433
    norm 6.34607 3.39605 6.94221
    pos 7.37924 -18.6891 2.15138
    norm 9.23978 -1.9749 3.27511
  }
  poly {
    numVertices 3
    pos 4.65564 -13.6955 -0.393571
    norm 0.1017 -0.68103 -9.97626
    pos 4.97961 -15.3822 -0.338328
    norm 2.2475 -1.2381 -9.66518
    pos 4.38104 -15.3573 -0.19007
    norm -3.86855 -2.08298 -8.98307
  }
  poly {
    numVertices 3
    pos -3.6129 -16.5015 1.63802
    norm 9.68138 -1.39526 2.07945
    pos -3.63079 -16.1815 1.1078
    norm 9.03447 -0.0437649 -4.28677
    pos -3.47869 -15.0434 1.36022
    norm 8.26886 -0.899591 -5.55128
  }
  poly {
    numVertices 3
    pos -3.68464 -15.8654 -4.90982
    norm 2.47642 1.428 -9.5827
    pos -3.27777 -14.6089 -4.68069
    norm 2.48977 0.722908 -9.65808
    pos -2.73953 -15.3335 -4.33695
    norm 6.40496 -1.51863 -7.52797
  }
  poly {
    numVertices 3
    pos 10.2669 -15.3472 5.29908
    norm 2.26622 -5.84393 -7.79184
    pos 10.4382 -14.8406 5.17183
    norm 4.54151 -1.39927 -8.79868
    pos 12.0257 -15.4419 6.29163
    norm 5.76813 -1.79928 -7.96815
  }
  poly {
    numVertices 3
    pos -3.40532 -16.8933 -4.54874
    norm 9.26184 -1.48681 -3.4652
    pos -3.59295 -18.7918 -4.53027
    norm 9.00954 -4.2537 0.856904
    pos -3.56044 -18.5697 -5.15075
    norm 9.07698 -2.52043 -3.35499
  }
  poly {
    numVertices 3
    pos -4.3253 -1.34326 -2.01213
    norm 6.76499 1.32773 -7.24377
    pos -3.798 -1.41512 -1.41323
    norm 8.64647 1.8449 -4.67279
    pos -3.23906 -2.74681 -1.22788
    norm 7.77713 3.54821 -5.18909
  }
  poly {
    numVertices 3
    pos -2.73953 -15.3335 -4.33695
    norm 6.40496 -1.51863 -7.52797
    pos -3.27777 -14.6089 -4.68069
    norm 2.48977 0.722908 -9.65808
    pos -2.05895 -13.9549 -4.22049
    norm 5.54484 -0.426677 -8.311
  }
  poly {
    numVertices 3
    pos -5.05599 2.40479 -1.97649
    norm 3.02521 1.52326 -9.40893
    pos -6.03612 2.2523 -2.00852
    norm -1.11556 2.14083 -9.70424
    pos -5.23633 4.51948 -1.50026
    norm 0.50282 2.06563 -9.77141
  }
  poly {
    numVertices 3
    pos -6.03612 2.2523 -2.00852
    norm -1.11556 2.14083 -9.70424
    pos -5.94909 4.15499 -1.51447
    norm -5.55342 2.42297 -7.95543
    pos -5.23633 4.51948 -1.50026
    norm 0.50282 2.06563 -9.77141
  }
  poly {
    numVertices 3
    pos 10.4382 -14.8406 5.17183
    norm 4.54151 -1.39927 -8.79868
    pos 10.376 -14.037 5.24704
    norm 6.03574 2.03773 -7.70827
    pos 12.0257 -15.4419 6.29163
    norm 5.76813 -1.79928 -7.96815
  }
  poly {
    numVertices 3
    pos 2.80769 -13.2753 0.903531
    norm -5.46502 -7.46703 -3.7917
    pos 3.38326 -14.2725 1.69782
    norm -7.40815 -6.63655 1.03705
    pos 2.28293 -13.2826 1.18152
    norm -0.148885 -8.9958 -4.36503
  }
  poly {
    numVertices 3
    pos 5.98871 -13.3607 0.632285
    norm 7.67601 0.280815 -6.40312
    pos 6.24481 -13.7521 0.908654
    norm 8.93794 -0.54909 -4.45105
    pos 6.09135 -15.1202 0.594855
    norm 8.4985 0.981692 -5.17801
  }
  poly {
    numVertices 3
    pos -4.02677 9.63628 -0.007707
    norm 8.11428 1.02779 -5.75344
    pos -3.83307 9.29778 0.392387
    norm 9.9715 0.277973 -0.701367
    pos -3.76933 8.38318 0.0669733
    norm 9.7092 0.543715 -2.33146
  }
  poly {
    numVertices 3
    pos 8.62872 -14.367 4.41651
    norm 1.84261 -4.41137 -8.7832
    pos 10.4382 -14.8406 5.17183
    norm 4.54151 -1.39927 -8.79868
    pos 10.2669 -15.3472 5.29908
    norm 2.26622 -5.84393 -7.79184
  }
  poly {
    numVertices 3
    pos 4.60674 -19.3379 2.15178
    norm -4.37939 -7.62477 4.76276
    pos 5.44222 -18.8925 2.97636
    norm -1.541 -4.79986 8.63635
    pos 4.68402 -18.8672 2.53061
    norm -6.64904 -2.30815 7.10371
  }
  poly {
    numVertices 3
    pos -1.60103 -14.4594 -3.38552
    norm 9.60254 -2.66033 -0.844921
    pos -1.39263 -12.5652 -3.23511
    norm 9.35587 -1.879 -2.9895
    pos -1.62737 -12.7661 -2.13916
    norm 8.26146 -4.79473 2.95955
  }
  poly {
    numVertices 3
    pos -4.99141 -6.75002 -2.38271
    norm -5.32882 -1.11321 -8.38835
    pos -5.78898 -5.3049 -2.60849
    norm -4.80425 -2.17328 -8.49683
    pos -4.41575 -5.39906 -2.72035
    norm 0.304998 0.0797159 -9.99504
  }
  poly {
    numVertices 3
    pos 0.37187 -12.7397 -0.903383
    norm 0.748865 -7.68155 -6.35869
    pos 2.28293 -13.2826 1.18152
    norm -0.148885 -8.9958 -4.36503
    pos -0.357896 -13.2263 0.0601258
    norm -0.146638 -9.46451 -3.22515
  }
  poly {
    numVertices 3
    pos -2.05895 -13.9549 -4.22049
    norm 5.54484 -0.426677 -8.311
    pos -3.27777 -14.6089 -4.68069
    norm 2.48977 0.722908 -9.65808
    pos -3.08106 -13.6066 -4.46041
    norm -0.742782 2.22322 -9.7214
  }
  poly {
    numVertices 3
    pos 5.36113 -14.5588 -0.166576
    norm 6.22493 0.314549 -7.81993
    pos 5.07755 -13.7669 -0.265191
    norm 4.77252 -0.149132 -8.7864
    pos 5.98871 -13.3607 0.632285
    norm 7.67601 0.280815 -6.40312
  }
  poly {
    numVertices 3
    pos -3.6129 -16.5015 1.63802
    norm 9.68138 -1.39526 2.07945
    pos -2.98619 -14.333 1.92776
    norm 9.58291 -1.88899 -2.14466
    pos -3.00445 -13.7999 2.7631
    norm 9.2791 -1.71494 3.31018
  }
  poly {
    numVertices 3
    pos -5.97654 -6.55009 -1.04053
    norm -9.68913 -2.09191 -1.32087
    pos -6.50666 -4.65668 -1.68746
    norm -9.48676 -2.6625 -1.70659
    pos -5.65267 -6.6997 -1.84277
    norm -8.19825 -2.69973 -5.04977
  }
  poly {
    numVertices 3
    pos -5.54138 -18.7444 0.488878
    norm -9.15698 3.38027 -2.17337
    pos -5.65118 -19.1634 -0.121674
    norm -6.26051 -4.53746 -6.34172
    pos -5.51585 -19.4426 0.605064
    norm -4.25677 -8.80978 -2.06585
  }
  poly {
    numVertices 3
    pos -3.47869 -15.0434 1.36022
    norm 8.26886 -0.899591 -5.55128
    pos -3.15492 -14.1057 1.49503
    norm 8.18651 -2.4407 -5.19846
    pos -2.98619 -14.333 1.92776
    norm 9.58291 -1.88899 -2.14466
  }
  poly {
    numVertices 3
    pos -2.05895 -13.9549 -4.22049
    norm 5.54484 -0.426677 -8.311
    pos -2.34586 -12.94 -4.21181
    norm 3.45429 0.986163 -9.33249
    pos -1.62321 -13.1215 -3.66567
    norm 8.23487 0.150715 -5.67135
  }
  poly {
    numVertices 3
    pos -3.08106 -13.6066 -4.46041
    norm -0.742782 2.22322 -9.7214
    pos -3.0696 -12.7332 -4.27864
    norm -2.75791 1.24789 -9.53083
    pos -2.34586 -12.94 -4.21181
    norm 3.45429 0.986163 -9.33249
  }
  poly {
    numVertices 3
    pos -4.33288 2.84798 -1.3933
    norm 7.4742 1.04776 -6.56038
    pos -5.05599 2.40479 -1.97649
    norm 3.02521 1.52326 -9.40893
    pos -4.70929 5.13828 -1.26631
    norm 4.614 1.41352 -8.7586
  }
  poly {
    numVertices 3
    pos -2.98619 -14.333 1.92776
    norm 9.58291 -1.88899 -2.14466
    pos -3.15492 -14.1057 1.49503
    norm 8.18651 -2.4407 -5.19846
    pos -2.8119 -13.0048 2.23074
    norm 9.28423 -2.69607 2.55625
  }
  poly {
    numVertices 3
    pos -0.283818 -19.0143 3.46991
    norm 0.589034 3.23168 -9.44507
    pos -1.08242 -19.3609 3.54029
    norm -5.95052 -5.48093 -5.87798
    pos -1.04649 -18.8133 3.66466
    norm -6.08121 5.77567 -5.44614
  }
  poly {
    numVertices 3
    pos 14.1917 -14.694 14.1011
    norm -6.84043 -5.78602 4.4419
    pos 14.72 -15.1718 13.5718
    norm 1.0773 -8.42496 5.27821
    pos 15.0179 -14.4384 14.0639
    norm 9.34882 2.94242 1.98538
  }
  poly {
    numVertices 3
    pos 9.04117 -13.1318 4.51572
    norm 6.25962 1.81214 -7.58507
    pos 10.0688 -13.0327 5.42226
    norm 6.5949 3.87326 -6.44245
    pos 10.376 -14.037 5.24704
    norm 6.03574 2.03773 -7.70827
  }
  poly {
    numVertices 3
    pos -2.34586 -12.94 -4.21181
    norm 3.45429 0.986163 -9.33249
    pos -1.71931 -11.2007 -3.74338
    norm 5.91182 0.907191 -8.0142
    pos -1.62321 -13.1215 -3.66567
    norm 8.23487 0.150715 -5.67135
  }
  poly {
    numVertices 3
    pos 4.76982 -12.6031 -0.487966
    norm 1.88983 -0.0460285 -9.8197
    pos 5.41775 -12.2746 -0.164942
    norm 5.81577 0.64092 -8.10963
    pos 5.07755 -13.7669 -0.265191
    norm 4.77252 -0.149132 -8.7864
  }
  poly {
    numVertices 3
    pos 14.1432 -15.9577 11.0633
    norm -3.28274 -8.75119 3.55532
    pos 13.4294 -15.0981 10.7541
    norm -9.20901 0.456894 3.87109
    pos 13.5226 -15.8573 10.5449
    norm -7.75747 -4.87038 4.01261
  }
  poly {
    numVertices 3
    pos -4.90419 -8.06049 -2.56106
    norm -8.05375 1.27667 -5.78853
    pos -4.33039 -6.57474 -2.704
    norm -4.34739 1.7048 -8.84273
    pos -4.70895 -8.53872 -3.14765
    norm -7.4631 2.37566 -6.21758
  }
  poly {
    numVertices 3
    pos 9.04117 -13.1318 4.51572
    norm 6.25962 1.81214 -7.58507
    pos 9.2347 -12.0919 5.1644
    norm 7.0686 3.64175 -6.06403
    pos 10.0688 -13.0327 5.42226
    norm 6.5949 3.87326 -6.44245
  }
  poly {
    numVertices 3
    pos 8.02126 -13.0395 3.8613
    norm 4.5653 -0.643587 -8.87377
    pos 7.98066 -12.4196 3.77291
    norm 4.55169 -0.147997 -8.90282
    pos 9.04117 -13.1318 4.51572
    norm 6.25962 1.81214 -7.58507
  }
  poly {
    numVertices 3
    pos 7.34488 -13.0766 3.5858
    norm 5.73292 -3.69174 -7.31469
    pos 7.98066 -12.4196 3.77291
    norm 4.55169 -0.147997 -8.90282
    pos 8.02126 -13.0395 3.8613
    norm 4.5653 -0.643587 -8.87377
  }
  poly {
    numVertices 3
    pos -5.46907 -18.1557 2.47566
    norm -8.77681 2.5456 4.06048
    pos -5.74922 -17.0399 2.18766
    norm -9.74152 0.168909 2.2526
    pos -5.6559 -17.7624 1.87307
    norm -9.8892 0.938324 -1.15032
  }
  poly {
    numVertices 3
    pos 5.41775 -12.2746 -0.164942
    norm 5.81577 0.64092 -8.10963
    pos 5.76414 -11.6978 0.288584
    norm 7.71584 1.35467 -6.21535
    pos 5.98871 -13.3607 0.632285
    norm 7.67601 0.280815 -6.40312
  }
  poly {
    numVertices 3
    pos -4.61753 -19.4945 0.453298
    norm 3.03687 -8.53415 -4.23624
    pos -5.51585 -19.4426 0.605064
    norm -4.25677 -8.80978 -2.06585
    pos -5.65118 -19.1634 -0.121674
    norm -6.26051 -4.53746 -6.34172
  }
  poly {
    numVertices 3
    pos -1.62787 -14.1294 -3.74326
    norm 8.44954 -1.83973 -5.02204
    pos -2.05895 -13.9549 -4.22049
    norm 5.54484 -0.426677 -8.311
    pos -1.62321 -13.1215 -3.66567
    norm 8.23487 0.150715 -5.67135
  }
  poly {
    numVertices 3
    pos 11.2811 -14.4095 8.1446
    norm -5.26387 3.75025 7.63069
    pos 12.9284 -14.287 9.09242
    norm -1.95407 8.78251 4.36453
    pos 11.8032 -13.8701 8.00983
    norm -1.52941 7.19719 6.77211
  }
  poly {
    numVertices 3
    pos 10.0688 -13.0327 5.42226
    norm 6.5949 3.87326 -6.44245
    pos 10.0073 -12.2825 5.97696
    norm 7.2667 5.21924 -4.46705
    pos 13.4579 -15.1211 7.84139
    norm 7.34013 4.49935 -5.08707
  }
  poly {
    numVertices 3
    pos 5.98871 -13.3607 0.632285
    norm 7.67601 0.280815 -6.40312
    pos 6.17638 -11.7776 0.996549
    norm 7.92142 0.766966 -6.05498
    pos 6.24481 -13.7521 0.908654
    norm 8.93794 -0.54909 -4.45105
  }
  poly {
    numVertices 3
    pos 4.76982 -12.6031 -0.487966
    norm 1.88983 -0.0460285 -9.8197
    pos 4.53682 -11.5524 -0.407543
    norm -0.730066 2.46174 -9.66472
    pos 5.41775 -12.2746 -0.164942
    norm 5.81577 0.64092 -8.10963
  }
  poly {
    numVertices 3
    pos 9.04117 -13.1318 4.51572
    norm 6.25962 1.81214 -7.58507
    pos 7.81209 -10.9002 3.95792
    norm 7.99863 2.98298 -5.20805
    pos 9.2347 -12.0919 5.1644
    norm 7.0686 3.64175 -6.06403
  }
  poly {
    numVertices 3
    pos -3.61009 -10.4979 -4.00697
    norm -3.01526 0.0475826 -9.53446
    pos -4.44626 -9.80535 -3.71284
    norm -6.99382 -0.865093 -7.09493
    pos -4.16097 -8.86538 -3.81439
    norm -3.83121 1.62248 -9.09337
  }
  poly {
    numVertices 3
    pos 6.24481 -13.7521 0.908654
    norm 8.93794 -0.54909 -4.45105
    pos 6.17638 -11.7776 0.996549
    norm 7.92142 0.766966 -6.05498
    pos 6.75368 -11.3013 1.63419
    norm 7.89281 1.31516 -5.99784
  }
  poly {
    numVertices 3
    pos 6.89534 -18.8223 0.449338
    norm 7.27302 0.864053 -6.80858
    pos 7.42725 -18.6062 1.34154
    norm 9.37498 0.705162 -3.40773
    pos 6.87304 -19.4288 0.605895
    norm 6.27903 -7.50803 -2.05021
  }
  poly {
    numVertices 3
    pos 6.87304 -19.4288 0.605895
    norm 6.27903 -7.50803 -2.05021
    pos 7.42725 -18.6062 1.34154
    norm 9.37498 0.705162 -3.40773
    pos 7.37924 -18.6891 2.15138
    norm 9.23978 -1.9749 3.27511
  }
  poly {
    numVertices 3
    pos 6.87304 -19.4288 0.605895
    norm 6.27903 -7.50803 -2.05021
    pos 6.02434 -19.4702 -0.272587
    norm 0.540327 -4.5337 -8.89683
    pos 6.89534 -18.8223 0.449338
    norm 7.27302 0.864053 -6.80858
  }
  poly {
    numVertices 3
    pos 7.98066 -12.4196 3.77291
    norm 4.55169 -0.147997 -8.90282
    pos 7.83166 -11.7518 3.74877
    norm 7.28161 0.351777 -6.84503
    pos 9.04117 -13.1318 4.51572
    norm 6.25962 1.81214 -7.58507
  }
  poly {
    numVertices 3
    pos 5.76414 -11.6978 0.288584
    norm 7.71584 1.35467 -6.21535
    pos 5.69516 -10.6303 0.601429
    norm 7.27045 2.189 -6.5076
    pos 6.17638 -11.7776 0.996549
    norm 7.92142 0.766966 -6.05498
  }
  poly {
    numVertices 3
    pos 5.41775 -12.2746 -0.164942
    norm 5.81577 0.64092 -8.10963
    pos 4.53682 -11.5524 -0.407543
    norm -0.730066 2.46174 -9.66472
    pos 5.16651 -11.1526 -0.0483241
    norm 4.80206 2.64467 -8.36337
  }
  poly {
    numVertices 3
    pos 6.56015 -13.7083 2.5919
    norm 8.45985 -5.23714 -1.00163
    pos 6.28983 -14.3377 1.44391
    norm 9.67517 -2.50103 0.368631
    pos 7.13286 -12.2295 2.65568
    norm 9.39183 -1.88633 -2.86973
  }
  poly {
    numVertices 3
    pos 5.76414 -11.6978 0.288584
    norm 7.71584 1.35467 -6.21535
    pos 5.41775 -12.2746 -0.164942
    norm 5.81577 0.64092 -8.10963
    pos 5.16651 -11.1526 -0.0483241
    norm 4.80206 2.64467 -8.36337
  }
  poly {
    numVertices 3
    pos 6.17638 -11.7776 0.996549
    norm 7.92142 0.766966 -6.05498
    pos 5.69516 -10.6303 0.601429
    norm 7.27045 2.189 -6.5076
    pos 6.75368 -11.3013 1.63419
    norm 7.89281 1.31516 -5.99784
  }
  poly {
    numVertices 3
    pos 5.76414 -11.6978 0.288584
    norm 7.71584 1.35467 -6.21535
    pos 5.16651 -11.1526 -0.0483241
    norm 4.80206 2.64467 -8.36337
    pos 5.69516 -10.6303 0.601429
    norm 7.27045 2.189 -6.5076
  }
  poly {
    numVertices 3
    pos -2.34586 -12.94 -4.21181
    norm 3.45429 0.986163 -9.33249
    pos -3.09565 -11.3845 -4.19898
    norm -1.19905 0.884341 -9.88839
    pos -2.32546 -10.8304 -4.03467
    norm 3.46307 1.71881 -9.22241
  }
  poly {
    numVertices 3
    pos 2.05608 -11.3913 -1.31843
    norm 5.36314 -4.74617 -6.9793
    pos 1.83164 -10.7767 -1.80439
    norm 5.52848 -2.72112 -7.87601
    pos 2.78563 -10.6028 -1.07071
    norm 6.83545 -2.12318 -6.98346
  }
  poly {
    numVertices 3
    pos 2.05608 -11.3913 -1.31843
    norm 5.36314 -4.74617 -6.9793
    pos 1.11372 -11.2381 -2.00032
    norm 3.4597 -4.65003 -8.14909
    pos 1.83164 -10.7767 -1.80439
    norm 5.52848 -2.72112 -7.87601
  }
  poly {
    numVertices 3
    pos -2.32546 -10.8304 -4.03467
    norm 3.46307 1.71881 -9.22241
    pos -1.29724 -10.813 -3.30765
    norm 6.55269 0.325613 -7.54693
    pos -1.71931 -11.2007 -3.74338
    norm 5.91182 0.907191 -8.0142
  }
  poly {
    numVertices 3
    pos -5.0833 0.98698 -2.12313
    norm 3.99763 1.4455 -9.05149
    pos -5.40552 0.16046 -2.46657
    norm 2.65352 2.18544 -9.39056
    pos -5.52957 1.33959 -2.16328
    norm 0.737567 1.89886 -9.79032
  }
  poly {
    numVertices 3
    pos 1.11372 -11.2381 -2.00032
    norm 3.4597 -4.65003 -8.14909
    pos 1.24592 -10.3963 -2.27277
    norm 4.52717 -1.62262 -8.76766
    pos 1.83164 -10.7767 -1.80439
    norm 5.52848 -2.72112 -7.87601
  }
  poly {
    numVertices 3
    pos 4.53682 -11.5524 -0.407543
    norm -0.730066 2.46174 -9.66472
    pos 4.38278 -10.4601 -0.0408829
    norm 0.470627 1.85455 -9.81525
    pos 5.16651 -11.1526 -0.0483241
    norm 4.80206 2.64467 -8.36337
  }
  poly {
    numVertices 3
    pos 0.25531 -11.6454 -1.93218
    norm 0.465279 -5.38191 -8.41537
    pos 0.538156 -10.3933 -2.53274
    norm 1.8206 -1.91168 -9.64525
    pos 1.11372 -11.2381 -2.00032
    norm 3.4597 -4.65003 -8.14909
  }
  poly {
    numVertices 3
    pos -0.28981 -9.73938 -2.63913
    norm 3.64756 -2.20968 -9.04503
    pos 0.538156 -10.3933 -2.53274
    norm 1.8206 -1.91168 -9.64525
    pos 0.25531 -11.6454 -1.93218
    norm 0.465279 -5.38191 -8.41537
  }
  poly {
    numVertices 3
    pos -0.677277 -11.281 -2.11471
    norm 5.59542 -5.69032 -6.0259
    pos -0.28981 -9.73938 -2.63913
    norm 3.64756 -2.20968 -9.04503
    pos 0.25531 -11.6454 -1.93218
    norm 0.465279 -5.38191 -8.41537
  }
  poly {
    numVertices 3
    pos -0.677277 -11.281 -2.11471
    norm 5.59542 -5.69032 -6.0259
    pos -1.39263 -12.5652 -3.23511
    norm 9.35587 -1.879 -2.9895
    pos -0.773334 -10.1339 -2.91499
    norm 6.91569 -1.80504 -6.99394
  }
  poly {
    numVertices 3
    pos -1.39263 -12.5652 -3.23511
    norm 9.35587 -1.879 -2.9895
    pos -1.29724 -10.813 -3.30765
    norm 6.55269 0.325613 -7.54693
    pos -0.773334 -10.1339 -2.91499
    norm 6.91569 -1.80504 -6.99394
  }
  poly {
    numVertices 3
    pos -3.09565 -11.3845 -4.19898
    norm -1.19905 0.884341 -9.88839
    pos -2.97614 -10.0888 -3.96776
    norm 1.18365 1.17877 -9.85948
    pos -2.32546 -10.8304 -4.03467
    norm 3.46307 1.71881 -9.22241
  }
  poly {
    numVertices 3
    pos -3.09565 -11.3845 -4.19898
    norm -1.19905 0.884341 -9.88839
    pos -3.61009 -10.4979 -4.00697
    norm -3.01526 0.0475826 -9.53446
    pos -2.97614 -10.0888 -3.96776
    norm 1.18365 1.17877 -9.85948
  }
  poly {
    numVertices 3
    pos 3.67478 -11.0294 0.137297
    norm 0.86657 -0.833314 -9.92747
    pos 3.3456 -10.5321 -0.356067
    norm 5.52992 -3.63531 -7.49696
    pos 3.82521 -11.0628 0.0781074
    norm -1.34017 -0.867301 -9.87176
  }
  poly {
    numVertices 3
    pos 2.78563 -10.6028 -1.07071
    norm 6.83545 -2.12318 -6.98346
    pos 1.83164 -10.7767 -1.80439
    norm 5.52848 -2.72112 -7.87601
    pos 2.08434 -9.85147 -1.75287
    norm 6.14109 -0.0169408 -7.89219
  }
  poly {
    numVertices 3
    pos -1.29724 -10.813 -3.30765
    norm 6.55269 0.325613 -7.54693
    pos -2.32546 -10.8304 -4.03467
    norm 3.46307 1.71881 -9.22241
    pos -2.15269 -9.90045 -3.74557
    norm 3.69494 0.52175 -9.27768
  }
  poly {
    numVertices 3
    pos 3.82521 -11.0628 0.0781074
    norm -1.34017 -0.867301 -9.87176
    pos 3.94706 -10.1116 -0.065452
    norm 3.99705 0.144965 -9.16529
    pos 4.38278 -10.4601 -0.0408829
    norm 0.470627 1.85455 -9.81525
  }
  poly {
    numVertices 3
    pos 5.16651 -11.1526 -0.0483241
    norm 4.80206 2.64467 -8.36337
    pos 4.38278 -10.4601 -0.0408829
    norm 0.470627 1.85455 -9.81525
    pos 4.6701 -9.61115 0.223488
    norm 2.92811 3.16245 -9.02359
  }
  poly {
    numVertices 3
    pos 3.3456 -10.5321 -0.356067
    norm 5.52992 -3.63531 -7.49696
    pos 3.94706 -10.1116 -0.065452
    norm 3.99705 0.144965 -9.16529
    pos 3.82521 -11.0628 0.0781074
    norm -1.34017 -0.867301 -9.87176
  }
  poly {
    numVertices 3
    pos 2.78563 -10.6028 -1.07071
    norm 6.83545 -2.12318 -6.98346
    pos 2.08434 -9.85147 -1.75287
    norm 6.14109 -0.0169408 -7.89219
    pos 2.91566 -9.60734 -1.01569
    norm 6.774 0.522784 -7.33754
  }
  poly {
    numVertices 3
    pos 1.40846 -9.1765 -2.19909
    norm 4.33043 0.853549 -8.97323
    pos 1.24592 -10.3963 -2.27277
    norm 4.52717 -1.62262 -8.76766
    pos 0.538156 -10.3933 -2.53274
    norm 1.8206 -1.91168 -9.64525
  }
  poly {
    numVertices 3
    pos -1.29724 -10.813 -3.30765
    norm 6.55269 0.325613 -7.54693
    pos -1.27279 -9.92793 -3.33552
    norm 5.18182 -0.992654 -8.49491
    pos -0.773334 -10.1339 -2.91499
    norm 6.91569 -1.80504 -6.99394
  }
  poly {
    numVertices 3
    pos -2.15269 -9.90045 -3.74557
    norm 3.69494 0.52175 -9.27768
    pos -1.27279 -9.92793 -3.33552
    norm 5.18182 -0.992654 -8.49491
    pos -1.29724 -10.813 -3.30765
    norm 6.55269 0.325613 -7.54693
  }
  poly {
    numVertices 3
    pos 3.66145 -9.71634 -0.365146
    norm 7.14836 -0.106065 -6.99212
    pos 2.78563 -10.6028 -1.07071
    norm 6.83545 -2.12318 -6.98346
    pos 2.91566 -9.60734 -1.01569
    norm 6.774 0.522784 -7.33754
  }
  poly {
    numVertices 3
    pos 4.38278 -10.4601 -0.0408829
    norm 0.470627 1.85455 -9.81525
    pos 3.94706 -10.1116 -0.065452
    norm 3.99705 0.144965 -9.16529
    pos 4.6701 -9.61115 0.223488
    norm 2.92811 3.16245 -9.02359
  }
  poly {
    numVertices 3
    pos 3.3456 -10.5321 -0.356067
    norm 5.52992 -3.63531 -7.49696
    pos 3.66145 -9.71634 -0.365146
    norm 7.14836 -0.106065 -6.99212
    pos 3.94706 -10.1116 -0.065452
    norm 3.99705 0.144965 -9.16529
  }
  poly {
    numVertices 3
    pos 0.538156 -10.3933 -2.53274
    norm 1.8206 -1.91168 -9.64525
    pos 0.10746 -8.88475 -2.57265
    norm 3.99296 1.57889 -9.03124
    pos 1.40846 -9.1765 -2.19909
    norm 4.33043 0.853549 -8.97323
  }
  poly {
    numVertices 3
    pos -0.28981 -9.73938 -2.63913
    norm 3.64756 -2.20968 -9.04503
    pos 0.10746 -8.88475 -2.57265
    norm 3.99296 1.57889 -9.03124
    pos 0.538156 -10.3933 -2.53274
    norm 1.8206 -1.91168 -9.64525
  }
  poly {
    numVertices 3
    pos -2.97614 -10.0888 -3.96776
    norm 1.18365 1.17877 -9.85948
    pos -3.61009 -10.4979 -4.00697
    norm -3.01526 0.0475826 -9.53446
    pos -3.41935 -9.16366 -3.98414
    norm -0.0921583 0.99474 -9.94997
  }
  poly {
    numVertices 3
    pos 5.16651 -11.1526 -0.0483241
    norm 4.80206 2.64467 -8.36337
    pos 5.26463 -8.91549 0.894319
    norm 5.9096 3.98031 -7.01669
    pos 5.69516 -10.6303 0.601429
    norm 7.27045 2.189 -6.5076
  }
  poly {
    numVertices 3
    pos 4.6701 -9.61115 0.223488
    norm 2.92811 3.16245 -9.02359
    pos 5.26463 -8.91549 0.894319
    norm 5.9096 3.98031 -7.01669
    pos 5.16651 -11.1526 -0.0483241
    norm 4.80206 2.64467 -8.36337
  }
  poly {
    numVertices 3
    pos 3.94706 -10.1116 -0.065452
    norm 3.99705 0.144965 -9.16529
    pos 4.00977 -9.22015 0.217357
    norm 5.27793 3.58148 -7.70171
    pos 4.6701 -9.61115 0.223488
    norm 2.92811 3.16245 -9.02359
  }
  poly {
    numVertices 3
    pos 1.24592 -10.3963 -2.27277
    norm 4.52717 -1.62262 -8.76766
    pos 1.40846 -9.1765 -2.19909
    norm 4.33043 0.853549 -8.97323
    pos 2.08434 -9.85147 -1.75287
    norm 6.14109 -0.0169408 -7.89219
  }
  poly {
    numVertices 3
    pos -4.72565 -3.46355 -2.63385
    norm 3.78939 1.23274 -9.17174
    pos -5.48056 -2.86297 -2.79063
    norm -0.0272999 0.0652342 -9.99975
    pos -5.35082 -1.32634 -2.74761
    norm 2.54944 1.17083 -9.59841
  }
  poly {
    numVertices 3
    pos -2.15269 -9.90045 -3.74557
    norm 3.69494 0.52175 -9.27768
    pos -0.897379 -9.00866 -3.28279
    norm 5.1314 -0.470598 -8.57014
    pos -1.27279 -9.92793 -3.33552
    norm 5.18182 -0.992654 -8.49491
  }
  poly {
    numVertices 3
    pos -2.97614 -10.0888 -3.96776
    norm 1.18365 1.17877 -9.85948
    pos -2.3899 -8.41909 -3.7683
    norm 2.526 1.41984 -9.57097
    pos -2.15269 -9.90045 -3.74557
    norm 3.69494 0.52175 -9.27768
  }
  poly {
    numVertices 3
    pos -3.41935 -9.16366 -3.98414
    norm -0.0921583 0.99474 -9.94997
    pos -2.3899 -8.41909 -3.7683
    norm 2.526 1.41984 -9.57097
    pos -2.97614 -10.0888 -3.96776
    norm 1.18365 1.17877 -9.85948
  }
  poly {
    numVertices 3
    pos 3.66145 -9.71634 -0.365146
    norm 7.14836 -0.106065 -6.99212
    pos 4.00977 -9.22015 0.217357
    norm 5.27793 3.58148 -7.70171
    pos 3.94706 -10.1116 -0.065452
    norm 3.99705 0.144965 -9.16529
  }
  poly {
    numVertices 3
    pos -3.41935 -9.16366 -3.98414
    norm -0.0921583 0.99474 -9.94997
    pos -3.61009 -10.4979 -4.00697
    norm -3.01526 0.0475826 -9.53446
    pos -4.16097 -8.86538 -3.81439
    norm -3.83121 1.62248 -9.09337
  }
  poly {
    numVertices 3
    pos 2.91566 -9.60734 -1.01569
    norm 6.774 0.522784 -7.33754
    pos 2.08434 -9.85147 -1.75287
    norm 6.14109 -0.0169408 -7.89219
    pos 2.58401 -8.83772 -1.23766
    norm 6.57392 1.7515 -7.3291
  }
  poly {
    numVertices 3
    pos -0.28981 -9.73938 -2.63913
    norm 3.64756 -2.20968 -9.04503
    pos -0.462525 -8.21891 -2.89676
    norm 5.85536 1.72956 -7.9198
    pos 0.10746 -8.88475 -2.57265
    norm 3.99296 1.57889 -9.03124
  }
  poly {
    numVertices 3
    pos -0.28981 -9.73938 -2.63913
    norm 3.64756 -2.20968 -9.04503
    pos -0.897379 -9.00866 -3.28279
    norm 5.1314 -0.470598 -8.57014
    pos -0.462525 -8.21891 -2.89676
    norm 5.85536 1.72956 -7.9198
  }
  poly {
    numVertices 3
    pos -4.02677 9.63628 -0.007707
    norm 8.11428 1.02779 -5.75344
    pos -3.8764 10.6555 0.360523
    norm 9.34649 0.484279 -3.52259
    pos -3.83307 9.29778 0.392387
    norm 9.9715 0.277973 -0.701367
  }
  poly {
    numVertices 3
    pos 1.80498 -8.38954 -1.75554
    norm 5.61535 2.10119 -8.0033
    pos 2.58401 -8.83772 -1.23766
    norm 6.57392 1.7515 -7.3291
    pos 2.08434 -9.85147 -1.75287
    norm 6.14109 -0.0169408 -7.89219
  }
  poly {
    numVertices 3
    pos 0.10746 -8.88475 -2.57265
    norm 3.99296 1.57889 -9.03124
    pos -0.462525 -8.21891 -2.89676
    norm 5.85536 1.72956 -7.9198
    pos -0.273659 -8.40599 -2.69356
    norm 6.7042 2.36737 -7.03201
  }
  poly {
    numVertices 3
    pos 2.58401 -8.83772 -1.23766
    norm 6.57392 1.7515 -7.3291
    pos 1.70578 -7.6199 -1.54277
    norm 5.26075 3.983 -7.51401
    pos 3.15852 -8.38935 -0.523185
    norm 7.1048 2.70633 -6.49596
  }
  poly {
    numVertices 3
    pos 1.80498 -8.38954 -1.75554
    norm 5.61535 2.10119 -8.0033
    pos 1.70578 -7.6199 -1.54277
    norm 5.26075 3.983 -7.51401
    pos 2.58401 -8.83772 -1.23766
    norm 6.57392 1.7515 -7.3291
  }
  poly {
    numVertices 3
    pos 8.92635 -10.9024 5.56255
    norm 7.64951 5.54286 -3.2805
    pos 10.6095 -12.6927 6.73842
    norm 6.83096 7.30238 0.115532
    pos 10.0073 -12.2825 5.97696
    norm 7.2667 5.21924 -4.46705
  }
  poly {
    numVertices 3
    pos 3.15852 -8.38935 -0.523185
    norm 7.1048 2.70633 -6.49596
    pos 4.12718 -8.40835 0.706444
    norm 6.23654 4.09118 -6.66093
    pos 4.00977 -9.22015 0.217357
    norm 5.27793 3.58148 -7.70171
  }
  poly {
    numVertices 3
    pos 2.68289 -7.22944 -0.250937
    norm 6.72592 4.86208 -5.57873
    pos 4.12718 -8.40835 0.706444
    norm 6.23654 4.09118 -6.66093
    pos 3.15852 -8.38935 -0.523185
    norm 7.1048 2.70633 -6.49596
  }
  poly {
    numVertices 3
    pos -0.897379 -9.00866 -3.28279
    norm 5.1314 -0.470598 -8.57014
    pos -2.3899 -8.41909 -3.7683
    norm 2.526 1.41984 -9.57097
    pos -0.462525 -8.21891 -2.89676
    norm 5.85536 1.72956 -7.9198
  }
  poly {
    numVertices 3
    pos 5.15589 -8.26437 1.34689
    norm 5.86433 5.32345 -6.10496
    pos 4.90674 -7.51158 2.10966
    norm 5.82643 7.12747 -3.90537
    pos 5.76159 -8.37016 2.21232
    norm 7.33373 5.97791 -3.23743
  }
  poly {
    numVertices 3
    pos 1.80498 -8.38954 -1.75554
    norm 5.61535 2.10119 -8.0033
    pos 0.939623 -8.53884 -2.27839
    norm 3.72696 2.48527 -8.94054
    pos 1.70578 -7.6199 -1.54277
    norm 5.26075 3.983 -7.51401
  }
  poly {
    numVertices 3
    pos -0.273659 -8.40599 -2.69356
    norm 6.7042 2.36737 -7.03201
    pos -0.302616 -7.1779 -2.58596
    norm 5.3647 2.43245 -8.08104
    pos 0.511418 -7.91169 -2.21703
    norm 4.25657 2.49436 -8.69827
  }
  poly {
    numVertices 3
    pos -0.462525 -8.21891 -2.89676
    norm 5.85536 1.72956 -7.9198
    pos -0.302616 -7.1779 -2.58596
    norm 5.3647 2.43245 -8.08104
    pos -0.273659 -8.40599 -2.69356
    norm 6.7042 2.36737 -7.03201
  }
  poly {
    numVertices 3
    pos 6.11378 -15.1697 1.76768
    norm 7.82517 1.79214 5.96281
    pos 6.49118 -15.5662 1.5058
    norm 9.47453 2.22726 2.2962
    pos 6.28983 -14.3377 1.44391
    norm 9.67517 -2.50103 0.368631
  }
  poly {
    numVertices 3
    pos 6.49118 -15.5662 1.5058
    norm 9.47453 2.22726 2.2962
    pos 6.10929 -14.7403 0.973346
    norm 9.68675 0.815143 -2.34573
    pos 6.28983 -14.3377 1.44391
    norm 9.67517 -2.50103 0.368631
  }
  poly {
    numVertices 3
    pos 0.939623 -8.53884 -2.27839
    norm 3.72696 2.48527 -8.94054
    pos 0.511418 -7.91169 -2.21703
    norm 4.25657 2.49436 -8.69827
    pos 1.70578 -7.6199 -1.54277
    norm 5.26075 3.983 -7.51401
  }
  poly {
    numVertices 3
    pos -0.462525 -8.21891 -2.89676
    norm 5.85536 1.72956 -7.9198
    pos -2.3899 -8.41909 -3.7683
    norm 2.526 1.41984 -9.57097
    pos -2.01924 -7.02041 -3.31201
    norm 2.89212 2.64906 -9.19881
  }
  poly {
    numVertices 3
    pos 6.10929 -14.7403 0.973346
    norm 9.68675 0.815143 -2.34573
    pos 6.09135 -15.1202 0.594855
    norm 8.4985 0.981692 -5.17801
    pos 6.24481 -13.7521 0.908654
    norm 8.93794 -0.54909 -4.45105
  }
  poly {
    numVertices 3
    pos -6.18026 4.11301 -1.09287
    norm -9.43279 1.84354 -2.76114
    pos -5.48677 8.83448 -0.682687
    norm -6.53133 2.0409 -7.29221
    pos -5.94909 4.15499 -1.51447
    norm -5.55342 2.42297 -7.95543
  }
  poly {
    numVertices 3
    pos 2.68289 -7.22944 -0.250937
    norm 6.72592 4.86208 -5.57873
    pos 3.90304 -7.66766 1.0278
    norm 5.89429 5.85771 -5.56279
    pos 4.12718 -8.40835 0.706444
    norm 6.23654 4.09118 -6.66093
  }
  poly {
    numVertices 3
    pos -4.26404 4.96568 -0.938556
    norm 7.22135 0.74888 -6.87686
    pos -4.21069 6.00638 -0.841693
    norm 6.98346 0.861646 -7.10555
    pos -3.88598 4.77008 -0.422515
    norm 9.42815 0.236647 -3.32476
  }
  poly {
    numVertices 3
    pos 1.70578 -7.6199 -1.54277
    norm 5.26075 3.983 -7.51401
    pos 2.68289 -7.22944 -0.250937
    norm 6.72592 4.86208 -5.57873
    pos 3.15852 -8.38935 -0.523185
    norm 7.1048 2.70633 -6.49596
  }
  poly {
    numVertices 3
    pos 1.54411 -6.69758 -0.977446
    norm 5.52338 5.84929 -5.93954
    pos 2.68289 -7.22944 -0.250937
    norm 6.72592 4.86208 -5.57873
    pos 1.70578 -7.6199 -1.54277
    norm 5.26075 3.983 -7.51401
  }
  poly {
    numVertices 3
    pos 4.90674 -7.51158 2.10966
    norm 5.82643 7.12747 -3.90537
    pos 3.90304 -7.66766 1.0278
    norm 5.89429 5.85771 -5.56279
    pos 3.91675 -7.13478 1.77387
    norm 5.88737 6.63754 -4.61323
  }
  poly {
    numVertices 3
    pos 2.66949 -6.38225 0.685115
    norm 6.27073 6.46775 -4.34123
    pos 3.91675 -7.13478 1.77387
    norm 5.88737 6.63754 -4.61323
    pos 3.90304 -7.66766 1.0278
    norm 5.89429 5.85771 -5.56279
  }
  poly {
    numVertices 3
    pos 1.70578 -7.6199 -1.54277
    norm 5.26075 3.983 -7.51401
    pos 0.511418 -7.91169 -2.21703
    norm 4.25657 2.49436 -8.69827
    pos 0.740215 -7.15194 -1.76874
    norm 5.19378 4.05653 -7.52126
  }
  poly {
    numVertices 3
    pos -0.302616 -7.1779 -2.58596
    norm 5.3647 2.43245 -8.08104
    pos 0.740215 -7.15194 -1.76874
    norm 5.19378 4.05653 -7.52126
    pos 0.511418 -7.91169 -2.21703
    norm 4.25657 2.49436 -8.69827
  }
  poly {
    numVertices 3
    pos -2.3899 -8.41909 -3.7683
    norm 2.526 1.41984 -9.57097
    pos -3.21034 -7.7722 -3.72271
    norm 0.0363636 2.49318 -9.68415
    pos -2.01924 -7.02041 -3.31201
    norm 2.89212 2.64906 -9.19881
  }
  poly {
    numVertices 3
    pos 11.293 -14.9098 8.29398
    norm -6.13054 0.29173 7.89502
    pos 12.4158 -14.8994 9.2069
    norm -6.3765 4.03786 6.56018
    pos 11.2811 -14.4095 8.1446
    norm -5.26387 3.75025 7.63069
  }
  poly {
    numVertices 3
    pos 2.68289 -7.22944 -0.250937
    norm 6.72592 4.86208 -5.57873
    pos 2.66949 -6.38225 0.685115
    norm 6.27073 6.46775 -4.34123
    pos 3.90304 -7.66766 1.0278
    norm 5.89429 5.85771 -5.56279
  }
  poly {
    numVertices 3
    pos -2.01924 -7.02041 -3.31201
    norm 2.89212 2.64906 -9.19881
    pos -3.21034 -7.7722 -3.72271
    norm 0.0363636 2.49318 -9.68415
    pos -2.75459 -6.57729 -3.30717
    norm 0.287488 4.43187 -8.95968
  }
  poly {
    numVertices 3
    pos -2.75459 -6.57729 -3.30717
    norm 0.287488 4.43187 -8.95968
    pos -2.99029 -5.97223 -2.84402
    norm -1.02474 5.49437 -8.29227
    pos -2.04694 -5.77759 -2.72298
    norm 1.0199 6.1023 -7.85632
  }
  poly {
    numVertices 3
    pos 0.692984 -6.27446 -1.1957
    norm 5.30086 5.54441 -6.41565
    pos 1.4807 -6.14113 -0.368454
    norm 5.5266 6.55219 -5.15029
    pos 1.54411 -6.69758 -0.977446
    norm 5.52338 5.84929 -5.93954
  }
  poly {
    numVertices 3
    pos 14.0515 -14.4601 13.5955
    norm -9.92904 -0.230176 1.16675
    pos 14.3549 -13.8269 13.5641
    norm -4.00888 9.13736 -0.661526
    pos 14.0665 -14.8584 12.9211
    norm -9.77944 -1.60865 1.33222
  }
  poly {
    numVertices 3
    pos 0.740215 -7.15194 -1.76874
    norm 5.19378 4.05653 -7.52126
    pos -0.89814 -5.35791 -2.07046
    norm 4.42295 5.83826 -6.80824
    pos 0.692984 -6.27446 -1.1957
    norm 5.30086 5.54441 -6.41565
  }
  poly {
    numVertices 3
    pos -2.04694 -5.77759 -2.72298
    norm 1.0199 6.1023 -7.85632
    pos -2.99029 -5.97223 -2.84402
    norm -1.02474 5.49437 -8.29227
    pos -1.93983 -4.95494 -1.93562
    norm 2.37489 6.51852 -7.202
  }
  poly {
    numVertices 3
    pos -3.53988 -5.77013 -2.67183
    norm -0.0518293 3.00429 -9.5379
    pos -4.33039 -6.57474 -2.704
    norm -4.34739 1.7048 -8.84273
    pos -4.41575 -5.39906 -2.72035
    norm 0.304998 0.0797159 -9.99504
  }
  poly {
    numVertices 3
    pos -3.22086 -2.49356 0.0420619
    norm 7.05189 6.2924 3.26751
    pos -3.7529 -1.36371 -0.192882
    norm 9.0451 2.36578 3.54812
    pos -4.10131 -1.57873 0.24944
    norm 4.88343 2.51075 8.35752
  }
  poly {
    numVertices 3
    pos 0.692984 -6.27446 -1.1957
    norm 5.30086 5.54441 -6.41565
    pos -0.487816 -5.06965 -1.22605
    norm 4.79827 6.76325 -5.58884
    pos 1.4807 -6.14113 -0.368454
    norm 5.5266 6.55219 -5.15029
  }
  poly {
    numVertices 3
    pos 14.2199 -14.1732 12.2989
    norm -5.06096 8.6173 0.359022
    pos 14.4033 -14.0152 11.9425
    norm -0.259256 9.99522 -0.168179
    pos 13.833 -14.9394 11.7083
    norm -9.77509 0.805147 1.94915
  }
  poly {
    numVertices 3
    pos 10.7132 -15.3275 7.90101
    norm -5.44395 -0.289947 8.38328
    pos 11.293 -14.9098 8.29398
    norm -6.13054 0.29173 7.89502
    pos 11.2811 -14.4095 8.1446
    norm -5.26387 3.75025 7.63069
  }
  poly {
    numVertices 3
    pos 14.7615 -14.2546 11.6964
    norm 6.6062 7.35563 -1.50098
    pos 15.3589 -14.9015 12.4757
    norm 9.70648 -1.07606 2.1509
    pos 15.0791 -15.5421 11.6782
    norm 8.5558 -5.16098 -0.403146
  }
  poly {
    numVertices 3
    pos -1.39952 -6.08959 -2.76028
    norm 3.11769 4.21128 -8.51734
    pos -2.04694 -5.77759 -2.72298
    norm 1.0199 6.1023 -7.85632
    pos -0.89814 -5.35791 -2.07046
    norm 4.42295 5.83826 -6.80824
  }
  poly {
    numVertices 3
    pos 7.0604 -11.1212 2.16792
    norm 9.17089 1.40861 -3.72968
    pos 7.1117 -10.7235 2.64221
    norm 9.10711 1.52615 -3.83816
    pos 7.13286 -12.2295 2.65568
    norm 9.39183 -1.88633 -2.86973
  }
  poly {
    numVertices 3
    pos -0.89814 -5.35791 -2.07046
    norm 4.42295 5.83826 -6.80824
    pos -2.04694 -5.77759 -2.72298
    norm 1.0199 6.1023 -7.85632
    pos -1.93983 -4.95494 -1.93562
    norm 2.37489 6.51852 -7.202
  }
  poly {
    numVertices 3
    pos 9.68214 -12.6627 7.02945
    norm -0.82539 3.09652 9.47261
    pos 9.22152 -11.3098 6.48667
    norm 3.88048 6.24039 6.78228
    pos 8.83447 -11.2836 6.62974
    norm 1.89934 4.28739 8.83237
  }
  poly {
    numVertices 3
    pos -2.99029 -5.97223 -2.84402
    norm -1.02474 5.49437 -8.29227
    pos -3.53988 -5.77013 -2.67183
    norm -0.0518293 3.00429 -9.5379
    pos -2.26993 -4.73804 -1.96651
    norm 3.27523 6.54553 -6.81387
  }
  poly {
    numVertices 3
    pos -3.53988 -5.77013 -2.67183
    norm -0.0518293 3.00429 -9.5379
    pos -3.93149 -4.04676 -2.38923
    norm 4.04495 3.39763 -8.49085
    pos -2.26993 -4.73804 -1.96651
    norm 3.27523 6.54553 -6.81387
  }
  poly {
    numVertices 3
    pos 9.45757 -14.5149 7.30453
    norm -3.4148 -1.1364 9.32994
    pos 8.76807 -14.6156 7.03628
    norm -3.17875 -2.74516 9.07522
    pos 8.55083 -15.1088 6.68321
    norm -4.25555 -5.58175 7.12282
  }
  poly {
    numVertices 3
    pos 10.702 -14.262 7.7392
    norm -3.33812 2.62744 9.05282
    pos 11.8032 -13.8701 8.00983
    norm -1.52941 7.19719 6.77211
    pos 10.3878 -12.8744 7.21761
    norm 1.7332 6.56232 7.34384
  }
  poly {
    numVertices 3
    pos -3.93149 -4.04676 -2.38923
    norm 4.04495 3.39763 -8.49085
    pos -2.85128 -4.57989 -2.09792
    norm 4.82665 7.31818 -4.81122
    pos -2.26993 -4.73804 -1.96651
    norm 3.27523 6.54553 -6.81387
  }
  poly {
    numVertices 3
    pos -0.487816 -5.06965 -1.22605
    norm 4.79827 6.76325 -5.58884
    pos -2.26993 -4.73804 -1.96651
    norm 3.27523 6.54553 -6.81387
    pos -0.89717 -4.52626 -0.804798
    norm 4.9225 7.68979 -4.07876
  }
  poly {
    numVertices 3
    pos -0.487816 -5.06965 -1.22605
    norm 4.79827 6.76325 -5.58884
    pos -1.93983 -4.95494 -1.93562
    norm 2.37489 6.51852 -7.202
    pos -2.26993 -4.73804 -1.96651
    norm 3.27523 6.54553 -6.81387
  }
  poly {
    numVertices 3
    pos -2.26993 -4.73804 -1.96651
    norm 3.27523 6.54553 -6.81387
    pos -2.04646 -4.13963 -1.17775
    norm 4.5813 6.94476 -5.54814
    pos -0.89717 -4.52626 -0.804798
    norm 4.9225 7.68979 -4.07876
  }
  poly {
    numVertices 3
    pos -3.28285 -12.4916 -1.3556
    norm -3.19206 -6.31825 7.06332
    pos -2.46167 -12.6482 -0.746944
    norm -2.10803 -9.73919 -0.83927
    pos -4.51294 -11.9106 -1.79198
    norm -6.63337 -7.44736 -0.731678
  }
  poly {
    numVertices 3
    pos -2.85128 -4.57989 -2.09792
    norm 4.82665 7.31818 -4.81122
    pos -3.06933 -3.75821 -1.76762
    norm 5.86742 4.02452 -7.02685
    pos -2.04646 -4.13963 -1.17775
    norm 4.5813 6.94476 -5.54814
  }
  poly {
    numVertices 3
    pos -4.26404 4.96568 -0.938556
    norm 7.22135 0.74888 -6.87686
    pos -3.88598 4.77008 -0.422515
    norm 9.42815 0.236647 -3.32476
    pos -4.33288 2.84798 -1.3933
    norm 7.4742 1.04776 -6.56038
  }
  poly {
    numVertices 3
    pos 14.3362 -13.8803 14
    norm -4.81193 8.69132 -1.14297
    pos 14.3959 -13.9987 14.4834
    norm -0.196616 4.91428 8.70696
    pos 15.0179 -14.4384 14.0639
    norm 9.34882 2.94242 1.98538
  }
  poly {
    numVertices 3
    pos -3.93149 -4.04676 -2.38923
    norm 4.04495 3.39763 -8.49085
    pos -3.06933 -3.75821 -1.76762
    norm 5.86742 4.02452 -7.02685
    pos -2.85128 -4.57989 -2.09792
    norm 4.82665 7.31818 -4.81122
  }
  poly {
    numVertices 3
    pos -3.93149 -4.04676 -2.38923
    norm 4.04495 3.39763 -8.49085
    pos -4.41575 -5.39906 -2.72035
    norm 0.304998 0.0797159 -9.99504
    pos -4.72565 -3.46355 -2.63385
    norm 3.78939 1.23274 -9.17174
  }
  poly {
    numVertices 3
    pos 14.2153 -13.7597 12.7611
    norm -2.9103 9.56188 -0.317319
    pos 14.2199 -14.1732 12.2989
    norm -5.06096 8.6173 0.359022
    pos 14.0665 -14.8584 12.9211
    norm -9.77944 -1.60865 1.33222
  }
  poly {
    numVertices 3
    pos 3.87057 -19.2953 1.09091
    norm -8.81646 -3.40422 -3.26824
    pos 4.32077 -19.1593 0.92638
    norm -5.76429 -3.76151 -7.25424
    pos 4.91991 -19.4177 1.19631
    norm -0.823745 -9.91614 -0.995827
  }
  poly {
    numVertices 3
    pos -5.65118 -19.1634 -0.121674
    norm -6.26051 -4.53746 -6.34172
    pos -5.54138 -18.7444 0.488878
    norm -9.15698 3.38027 -2.17337
    pos -5.64277 -18.6945 0.0114914
    norm -5.64334 6.91606 -4.50787
  }
  poly {
    numVertices 3
    pos 7.10629 -13.6283 3.83632
    norm 2.082 -7.3437 -6.46029
    pos 9.16125 -15.5749 5.49072
    norm -1.40995 -8.29987 -5.39669
    pos 6.46578 -13.7723 3.93901
    norm 3.91257 -9.19225 -0.440992
  }
  poly {
    numVertices 3
    pos -5.64277 -18.6945 0.0114914
    norm -5.64334 6.91606 -4.50787
    pos -5.54138 -18.7444 0.488878
    norm -9.15698 3.38027 -2.17337
    pos -5.05231 -18.2437 0.666347
    norm -7.018 5.13608 -4.93644
  }
  poly {
    numVertices 3
    pos -6.01756 -19.1983 0.864143
    norm -8.31338 -3.23897 -4.51629
    pos -5.37705 -17.985 1.3533
    norm -8.60207 2.27413 -4.5643
    pos -5.54138 -18.7444 0.488878
    norm -9.15698 3.38027 -2.17337
  }
  poly {
    numVertices 3
    pos -5.40552 0.16046 -2.46657
    norm 2.65352 2.18544 -9.39056
    pos -6.51485 0.20576 -2.34626
    norm -3.54166 2.09553 -9.11402
    pos -5.52957 1.33959 -2.16328
    norm 0.737567 1.89886 -9.79032
  }
  poly {
    numVertices 3
    pos -5.65267 -6.6997 -1.84277
    norm -8.19825 -2.69973 -5.04977
    pos -6.50666 -4.65668 -1.68746
    norm -9.48676 -2.6625 -1.70659
    pos -6.23098 -4.79782 -2.29041
    norm -8.30328 -2.83787 -4.79604
  }
  poly {
    numVertices 3
    pos 0.426079 -19.5653 6.04336
    norm -0.223378 -9.64842 2.61882
    pos 0.916356 -19.1587 6.45121
    norm 1.08535 -2.93586 9.49751
    pos 0.453254 -19.2285 6.45999
    norm -1.44472 -4.54158 8.7913
  }
  poly {
    numVertices 3
    pos -5.37705 -17.985 1.3533
    norm -8.60207 2.27413 -4.5643
    pos -5.05231 -18.2437 0.666347
    norm -7.018 5.13608 -4.93644
    pos -5.54138 -18.7444 0.488878
    norm -9.15698 3.38027 -2.17337
  }
  poly {
    numVertices 3
    pos -4.15299 -19.3924 -5.7299
    norm 3.34049 -8.72104 -3.57554
    pos -4.7385 -19.2777 -5.96447
    norm -1.84915 -5.05636 -8.42698
    pos -4.53285 -19.0919 -6.25921
    norm 3.24862 -1.65157 -9.31229
  }
  poly {
    numVertices 3
    pos -6.76337 -1.47748 -2.52411
    norm -6.89324 0.413042 -7.23276
    pos -6.5942 1.11603 -1.93166
    norm -9.33229 1.76466 -3.12959
    pos -6.51485 0.20576 -2.34626
    norm -3.54166 2.09553 -9.11402
  }
  poly {
    numVertices 3
    pos -5.6559 -17.7624 1.87307
    norm -9.8892 0.938324 -1.15032
    pos -5.77866 -16.6824 1.20658
    norm -8.83549 -0.652013 -4.63778
    pos -5.37705 -17.985 1.3533
    norm -8.60207 2.27413 -4.5643
  }
  poly {
    numVertices 3
    pos -5.37705 -17.985 1.3533
    norm -8.60207 2.27413 -4.5643
    pos -5.77866 -16.6824 1.20658
    norm -8.83549 -0.652013 -4.63778
    pos -5.019 -16.8262 0.536212
    norm -4.15483 0.123568 -9.09517
  }
  poly {
    numVertices 3
    pos -5.91348 -16.3229 1.89824
    norm -9.84757 0.11928 1.73527
    pos -5.33984 -13.5362 2.54091
    norm -8.93817 1.05547 4.35833
    pos -5.46645 -13.3129 1.79579
    norm -9.88526 -0.608969 -1.38229
  }
  poly {
    numVertices 3
    pos -5.77866 -16.6824 1.20658
    norm -8.83549 -0.652013 -4.63778
    pos -5.12193 -13.6635 1.05269
    norm -7.38051 0.233406 -6.74342
    pos -5.019 -16.8262 0.536212
    norm -4.15483 0.123568 -9.09517
  }
  poly {
    numVertices 3
    pos -5.434 -12.9887 2.25442
    norm -8.85879 -0.99168 4.53194
    pos -5.90258 -11.7864 2.19089
    norm -8.17213 -2.99435 4.92445
    pos -5.86864 -12.1499 1.57719
    norm -9.09612 -4.15328 -0.104921
  }
  poly {
    numVertices 3
    pos -4.94828 -19.5758 1.99663
    norm 0.481895 -9.91623 1.19837
    pos -5.58623 -19.4082 2.71577
    norm -2.11591 -7.41205 6.37059
    pos -6.1455 -19.4069 1.54821
    norm -9.17673 -3.6539 -1.56097
  }
  poly {
    numVertices 3
    pos -4.42753 -18.8147 -3.34335
    norm 4.87093 -4.5342 7.46425
    pos -3.8027 -18.8281 -4.01741
    norm 7.55102 -5.20987 3.97988
    pos -3.74297 -16.894 -3.39166
    norm 6.85428 -3.50839 6.38045
  }
  poly {
    numVertices 3
    pos -4.33866 -16.9699 -2.98265
    norm 3.10092 -3.56512 8.8133
    pos -2.19534 -14.5856 -2.5191
    norm 5.02069 -4.6074 7.31879
    pos -2.88157 -13.809 -1.92653
    norm 2.27185 -3.31637 9.15644
  }
  poly {
    numVertices 3
    pos -5.48677 8.83448 -0.682687
    norm -6.53133 2.0409 -7.29221
    pos -5.8495 7.29778 -0.182433
    norm -9.89567 1.10332 -0.926523
    pos -5.42746 10.2895 -0.392424
    norm -5.64918 2.11673 -7.97536
  }
  poly {
    numVertices 3
    pos -5.42746 10.2895 -0.392424
    norm -5.64918 2.11673 -7.97536
    pos -5.8495 7.29778 -0.182433
    norm -9.89567 1.10332 -0.926523
    pos -5.57262 10.1447 0.780347
    norm -9.5592 0.382919 2.91118
  }
  poly {
    numVertices 3
    pos -5.019 -16.8262 0.536212
    norm -4.15483 0.123568 -9.09517
    pos -4.57349 -14.8283 0.72303
    norm -0.778585 0.263387 -9.96616
    pos -4.43158 -16.409 0.551847
    norm 2.92083 0.433753 -9.55409
  }
  poly {
    numVertices 3
    pos -2.75314 -10.7816 4.20168
    norm -6.37077 -2.93335 7.12804
    pos -3.5269 -10.4585 3.5318
    norm -3.71794 -3.3034 8.67552
    pos -2.83174 -11.4541 3.35851
    norm -4.15278 -6.81769 6.02275
  }
  poly {
    numVertices 3
    pos -5.8495 7.29778 -0.182433
    norm -9.89567 1.10332 -0.926523
    pos -5.70448 8.23418 0.42774
    norm -9.06602 0.144092 4.21741
    pos -5.57262 10.1447 0.780347
    norm -9.5592 0.382919 2.91118
  }
  poly {
    numVertices 3
    pos -5.41685 -15.8033 -4.18426
    norm -8.46398 3.58729 -3.93604
    pos -5.13742 -15.1904 -3.4014
    norm -8.75339 4.09711 2.56746
    pos -4.12682 -13.5606 -3.34324
    norm -8.73727 4.02173 -2.73602
  }
  poly {
    numVertices 3
    pos -4.57349 -14.8283 0.72303
    norm -0.778585 0.263387 -9.96616
    pos -5.019 -16.8262 0.536212
    norm -4.15483 0.123568 -9.09517
    pos -5.12193 -13.6635 1.05269
    norm -7.38051 0.233406 -6.74342
  }
  poly {
    numVertices 3
    pos -0.666849 -19.4694 4.75092
    norm -3.70606 -9.28357 -0.283884
    pos -1.27763 -19.0498 4.83098
    norm -9.98953 -0.388803 0.240663
    pos -1.13676 -19.0025 4.13924
    norm -9.76427 1.88834 -1.04558
  }
  poly {
    numVertices 3
    pos -4.21837 -13.639 -2.81587
    norm -9.71203 2.22499 0.851998
    pos -4.12682 -13.5606 -3.34324
    norm -8.73727 4.02173 -2.73602
    pos -5.13742 -15.1904 -3.4014
    norm -8.75339 4.09711 2.56746
  }
  poly {
    numVertices 3
    pos -5.41685 -15.8033 -4.18426
    norm -8.46398 3.58729 -3.93604
    pos -4.12682 -13.5606 -3.34324
    norm -8.73727 4.02173 -2.73602
    pos -3.3886 -12.9942 -4.04817
    norm -6.60869 1.92688 -7.25344
  }
  poly {
    numVertices 3
    pos -2.75314 -10.7816 4.20168
    norm -6.37077 -2.93335 7.12804
    pos -2.08653 -11.8198 4.08392
    norm -5.37258 -6.81002 4.97583
    pos -1.41203 -11.4555 4.84936
    norm -3.54584 -5.00315 7.89908
  }
  poly {
    numVertices 3
    pos -4.21837 -13.639 -2.81587
    norm -9.71203 2.22499 0.851998
    pos -4.01789 -12.4093 -3.13516
    norm -9.02195 -0.298631 -4.30294
    pos -4.12682 -13.5606 -3.34324
    norm -8.73727 4.02173 -2.73602
  }
  poly {
    numVertices 3
    pos -4.21837 -13.639 -2.81587
    norm -9.71203 2.22499 0.851998
    pos -4.163 -12.6847 -2.66679
    norm -9.92912 -1.18267 0.117671
    pos -4.01789 -12.4093 -3.13516
    norm -9.02195 -0.298631 -4.30294
  }
  poly {
    numVertices 3
    pos -4.57349 -14.8283 0.72303
    norm -0.778585 0.263387 -9.96616
    pos -5.12193 -13.6635 1.05269
    norm -7.38051 0.233406 -6.74342
    pos -5.11027 -13.1041 0.859035
    norm -6.99962 -3.89051 -5.98909
  }
  poly {
    numVertices 3
    pos -4.12682 -13.5606 -3.34324
    norm -8.73727 4.02173 -2.73602
    pos -4.01789 -12.4093 -3.13516
    norm -9.02195 -0.298631 -4.30294
    pos -3.3886 -12.9942 -4.04817
    norm -6.60869 1.92688 -7.25344
  }
  poly {
    numVertices 3
    pos -3.78867 -11.5941 -3.7573
    norm -7.40648 -1.66693 -6.50888
    pos -3.3886 -12.9942 -4.04817
    norm -6.60869 1.92688 -7.25344
    pos -4.01789 -12.4093 -3.13516
    norm -9.02195 -0.298631 -4.30294
  }
  poly {
    numVertices 3
    pos -3.73905 -18.7716 2.41767
    norm 7.38283 -3.70313 5.63743
    pos -4.07782 -16.7707 2.7645
    norm 6.10066 0.127016 7.92249
    pos -4.42361 -18.4625 3.06688
    norm 1.2685 -1.31164 9.83212
  }
  poly {
    numVertices 3
    pos -5.46645 -13.3129 1.79579
    norm -9.88526 -0.608969 -1.38229
    pos -5.86864 -12.1499 1.57719
    norm -9.09612 -4.15328 -0.104921
    pos -5.11027 -13.1041 0.859035
    norm -6.99962 -3.89051 -5.98909
  }
  poly {
    numVertices 3
    pos -3.24709 -12.7552 0.506168
    norm 0.419805 -8.9403 -4.46036
    pos -5.21528 -12.174 -0.517622
    norm -3.31427 -8.18459 -4.69341
    pos -4.7626 -11.8623 -1.08569
    norm -3.18104 -8.93423 -3.17183
  }
  poly {
    numVertices 3
    pos 6.80751 -16.8562 2.14021
    norm 8.21816 4.10041 3.95582
    pos 5.78605 -16.2582 2.61357
    norm 3.53917 3.53024 8.66092
    pos 6.65273 -17.2559 2.73433
    norm 6.34607 3.39605 6.94221
  }
  poly {
    numVertices 3
    pos 8.86135 -15.5277 6.3165
    norm -4.56015 -8.37189 3.01935
    pos 10.4504 -16.0691 6.22518
    norm -0.833738 -9.45258 -3.15492
    pos 12.5146 -16.6051 8.32545
    norm -2.50109 -9.50222 1.85805
  }
  poly {
    numVertices 3
    pos -4.29227 -17.5202 -5.39181
    norm 1.52838 3.381 -9.28617
    pos -4.90629 -17.784 -5.39786
    norm -4.52652 3.6587 -8.1317
    pos -4.1396 -14.962 -4.7755
    norm -2.63305 3.36237 -9.04221
  }
  poly {
    numVertices 3
    pos -0.283818 -19.0143 3.46991
    norm 0.589034 3.23168 -9.44507
    pos -1.04649 -18.8133 3.66466
    norm -6.08121 5.77567 -5.44614
    pos -0.132075 -18.0928 4.40838
    norm -6.58332 4.50687 -6.02893
  }
  poly {
    numVertices 3
    pos -3.78867 -11.5941 -3.7573
    norm -7.40648 -1.66693 -6.50888
    pos -4.01789 -12.4093 -3.13516
    norm -9.02195 -0.298631 -4.30294
    pos -4.45284 -11.0302 -2.99362
    norm -8.65514 -2.57609 -4.29561
  }
  poly {
    numVertices 3
    pos 1.99368 -13.5825 3.13191
    norm -1.53619 -9.86583 -0.552767
    pos 2.31774 -13.6426 2.28765
    norm -2.74809 -9.60543 0.428646
    pos 4.73871 -14.2177 4.37438
    norm -2.11038 -9.6019 1.83024
  }
  poly {
    numVertices 3
    pos -4.47069 -11.6215 -2.42129
    norm -8.93755 -3.98278 -2.0634
    pos -5.40614 -10.1019 -2.23332
    norm -8.29182 -1.4815 -5.38988
    pos -4.99445 -9.69996 -2.84098
    norm -8.56445 -1.05053 -5.05437
  }
  poly {
    numVertices 3
    pos -3.63079 -16.1815 1.1078
    norm 9.03447 -0.0437649 -4.28677
    pos -3.83874 -18.0208 0.935175
    norm 6.8127 0.416185 -7.30849
    pos -3.98486 -15.7726 0.775604
    norm 5.25154 0.418081 -8.49979
  }
  poly {
    numVertices 3
    pos -5.40374 -17.5601 -3.37764
    norm -7.27576 1.06843 6.77657
    pos -5.64811 -16.6603 -3.81808
    norm -9.99552 0.231984 0.188913
    pos -5.83976 -18.1071 -4.05654
    norm -8.73339 4.82933 0.636729
  }
  poly {
    numVertices 3
    pos 7.81209 -10.9002 3.95792
    norm 7.99863 2.98298 -5.20805
    pos 7.1117 -10.7235 2.64221
    norm 9.10711 1.52615 -3.83816
    pos 6.62089 -9.35893 2.58673
    norm 8.27734 4.20574 -3.7145
  }
  poly {
    numVertices 3
    pos 6.63421 -17.1719 0.818538
    norm 7.58243 1.68217 -6.29897
    pos 6.081 -16.2644 0.324269
    norm 6.38897 0.561773 -7.67239
    pos 6.09135 -15.1202 0.594855
    norm 8.4985 0.981692 -5.17801
  }
  poly {
    numVertices 3
    pos 7.1117 -10.7235 2.64221
    norm 9.10711 1.52615 -3.83816
    pos 7.83166 -11.7518 3.74877
    norm 7.28161 0.351777 -6.84503
    pos 7.13286 -12.2295 2.65568
    norm 9.39183 -1.88633 -2.86973
  }
  poly {
    numVertices 3
    pos -1.71931 -11.2007 -3.74338
    norm 5.91182 0.907191 -8.0142
    pos -1.29724 -10.813 -3.30765
    norm 6.55269 0.325613 -7.54693
    pos -1.62321 -13.1215 -3.66567
    norm 8.23487 0.150715 -5.67135
  }
  poly {
    numVertices 3
    pos -3.52944 -3.05024 0.736041
    norm 1.87693 6.12494 7.67869
    pos -3.22086 -2.49356 0.0420619
    norm 7.05189 6.2924 3.26751
    pos -4.10131 -1.57873 0.24944
    norm 4.88343 2.51075 8.35752
  }
  poly {
    numVertices 3
    pos -4.17299 -19.0353 0.497947
    norm 6.1323 -0.272282 -7.89436
    pos -3.62061 -19.0037 1.10979
    norm 8.19336 -4.03503 -4.07276
    pos -4.61753 -19.4945 0.453298
    norm 3.03687 -8.53415 -4.23624
  }
  poly {
    numVertices 3
    pos 6.65273 -17.2559 2.73433
    norm 6.34607 3.39605 6.94221
    pos 5.78605 -16.2582 2.61357
    norm 3.53917 3.53024 8.66092
    pos 6.14551 -17.6639 3.10086
    norm 0.518151 1.5958 9.85824
  }
  poly {
    numVertices 3
    pos -3.62061 -19.0037 1.10979
    norm 8.19336 -4.03503 -4.07276
    pos -4.17299 -19.0353 0.497947
    norm 6.1323 -0.272282 -7.89436
    pos -3.83874 -18.0208 0.935175
    norm 6.8127 0.416185 -7.30849
  }
  poly {
    numVertices 3
    pos -3.62061 -19.0037 1.10979
    norm 8.19336 -4.03503 -4.07276
    pos -3.41743 -18.6993 1.71869
    norm 9.81143 -1.91974 -0.224945
    pos -3.95442 -19.3788 1.48969
    norm 4.60156 -8.83616 0.864803
  }
  poly {
    numVertices 3
    pos 7.83166 -11.7518 3.74877
    norm 7.28161 0.351777 -6.84503
    pos 7.1117 -10.7235 2.64221
    norm 9.10711 1.52615 -3.83816
    pos 7.81209 -10.9002 3.95792
    norm 7.99863 2.98298 -5.20805
  }
  poly {
    numVertices 3
    pos -2.34291 -3.79958 -1.0176
    norm 7.08379 4.76794 -5.20448
    pos -3.48307 -2.10146 -0.874187
    norm 9.14499 3.8158 -1.34493
    pos -2.47359 -3.30754 -0.0466174
    norm 6.20974 7.66014 -1.66176
  }
  poly {
    numVertices 3
    pos -3.66628 -17.5727 -5.07279
    norm 6.98266 1.91832 -6.89656
    pos -3.40532 -16.8933 -4.54874
    norm 9.26184 -1.48681 -3.4652
    pos -3.56044 -18.5697 -5.15075
    norm 9.07698 -2.52043 -3.35499
  }
  poly {
    numVertices 3
    pos -3.83874 -18.0208 0.935175
    norm 6.8127 0.416185 -7.30849
    pos -3.41743 -18.6993 1.71869
    norm 9.81143 -1.91974 -0.224945
    pos -3.62061 -19.0037 1.10979
    norm 8.19336 -4.03503 -4.07276
  }
  poly {
    numVertices 3
    pos 4.28666 -6.41083 4.51174
    norm 2.28514 7.5741 6.11647
    pos 2.18633 -5.28753 3.29878
    norm 1.23168 8.57469 4.99576
    pos 2.7402 -6.02309 4.17817
    norm 0.669737 7.72844 6.31052
  }
  poly {
    numVertices 3
    pos 12.9711 -16.5308 7.91567
    norm 3.64796 -8.60927 -3.54583
    pos 13.8795 -15.9158 8.35377
    norm 7.85484 -2.88631 -5.47456
    pos 14.6449 -15.9389 9.7333
    norm 8.68951 -3.85237 -3.10672
  }
  poly {
    numVertices 3
    pos -3.83874 -18.0208 0.935175
    norm 6.8127 0.416185 -7.30849
    pos -3.63079 -16.1815 1.1078
    norm 9.03447 -0.0437649 -4.28677
    pos -3.41743 -18.6993 1.71869
    norm 9.81143 -1.91974 -0.224945
  }
  poly {
    numVertices 3
    pos 11.491 -16.0655 6.38224
    norm 2.99165 -7.05107 -6.42903
    pos 12.7463 -15.9303 7.07573
    norm 6.23089 -3.66144 -6.91158
    pos 12.9711 -16.5308 7.91567
    norm 3.64796 -8.60927 -3.54583
  }
  poly {
    numVertices 3
    pos 6.89929 -16.9192 1.47205
    norm 9.5297 2.97301 -0.588242
    pos 6.63421 -17.1719 0.818538
    norm 7.58243 1.68217 -6.29897
    pos 6.09135 -15.1202 0.594855
    norm 8.4985 0.981692 -5.17801
  }
  poly {
    numVertices 3
    pos 6.24481 -13.7521 0.908654
    norm 8.93794 -0.54909 -4.45105
    pos 6.75368 -11.3013 1.63419
    norm 7.89281 1.31516 -5.99784
    pos 6.92175 -12.6385 1.96847
    norm 9.27388 -1.3204 -3.50026
  }
  poly {
    numVertices 3
    pos -3.40532 -16.8933 -4.54874
    norm 9.26184 -1.48681 -3.4652
    pos -2.43726 -15.3994 -3.93026
    norm 8.14933 -4.94443 -3.02343
    pos -3.47146 -17.0699 -3.9623
    norm 8.9259 -3.66105 2.63157
  }
  poly {
    numVertices 3
    pos -3.66628 -17.5727 -5.07279
    norm 6.98266 1.91832 -6.89656
    pos -3.76136 -16.9466 -5.05756
    norm 6.25336 0.271389 -7.79883
    pos -3.40532 -16.8933 -4.54874
    norm 9.26184 -1.48681 -3.4652
  }
  poly {
    numVertices 3
    pos 14.6449 -15.9389 9.7333
    norm 8.68951 -3.85237 -3.10672
    pos 13.8795 -15.9158 8.35377
    norm 7.85484 -2.88631 -5.47456
    pos 14.2157 -15.1248 9.03148
    norm 8.28858 3.27812 -4.53357
  }
  poly {
    numVertices 3
    pos 12.6226 -15.1059 6.90908
    norm 6.91303 1.83304 -6.98928
    pos 13.2641 -15.5252 7.47795
    norm 7.69544 0.144981 -6.38429
    pos 12.7463 -15.9303 7.07573
    norm 6.23089 -3.66144 -6.91158
  }
  poly {
    numVertices 3
    pos -2.47359 -3.30754 -0.0466174
    norm 6.20974 7.66014 -1.66176
    pos -3.22086 -2.49356 0.0420619
    norm 7.05189 6.2924 3.26751
    pos -2.13791 -3.39392 0.722582
    norm 2.8227 9.03571 3.2231
  }
  poly {
    numVertices 3
    pos 12.0257 -15.4419 6.29163
    norm 5.76813 -1.79928 -7.96815
    pos 12.7463 -15.9303 7.07573
    norm 6.23089 -3.66144 -6.91158
    pos 11.491 -16.0655 6.38224
    norm 2.99165 -7.05107 -6.42903
  }
  poly {
    numVertices 3
    pos 10.0688 -13.0327 5.42226
    norm 6.5949 3.87326 -6.44245
    pos 9.2347 -12.0919 5.1644
    norm 7.0686 3.64175 -6.06403
    pos 10.0073 -12.2825 5.97696
    norm 7.2667 5.21924 -4.46705
  }
  poly {
    numVertices 3
    pos 12.0257 -15.4419 6.29163
    norm 5.76813 -1.79928 -7.96815
    pos 12.6226 -15.1059 6.90908
    norm 6.91303 1.83304 -6.98928
    pos 12.7463 -15.9303 7.07573
    norm 6.23089 -3.66144 -6.91158
  }
  poly {
    numVertices 3
    pos 14.2157 -15.1248 9.03148
    norm 8.28858 3.27812 -4.53357
    pos 13.4579 -15.1211 7.84139
    norm 7.34013 4.49935 -5.08707
    pos 12.1298 -13.7766 7.55099
    norm 4.79951 8.77215 0.118892
  }
  poly {
    numVertices 3
    pos 15.0791 -15.5421 11.6782
    norm 8.5558 -5.16098 -0.403146
    pos 14.7716 -14.9307 10.6684
    norm 8.60765 3.63719 -3.56081
    pos 15.0734 -15.0724 11.3027
    norm 9.40422 2.44227 -2.36558
  }
  poly {
    numVertices 3
    pos 12.0257 -15.4419 6.29163
    norm 5.76813 -1.79928 -7.96815
    pos 10.376 -14.037 5.24704
    norm 6.03574 2.03773 -7.70827
    pos 12.6226 -15.1059 6.90908
    norm 6.91303 1.83304 -6.98928
  }
  poly {
    numVertices 3
    pos 14.2157 -15.1248 9.03148
    norm 8.28858 3.27812 -4.53357
    pos 12.1298 -13.7766 7.55099
    norm 4.79951 8.77215 0.118892
    pos 13.9092 -14.4893 9.48218
    norm 5.2732 8.28882 -1.86783
  }
  poly {
    numVertices 3
    pos 1.67285 -14.3322 6.3714
    norm 4.82088 -3.01384 8.22653
    pos 1.88938 -17.1928 5.6789
    norm 9.71842 -0.930545 2.16478
    pos 2.41752 -14.6786 5.48116
    norm 7.30528 -6.28082 2.68034
  }
  poly {
    numVertices 3
    pos 1.88938 -17.1928 5.6789
    norm 9.71842 -0.930545 2.16478
    pos 1.81833 -16.2318 4.58615
    norm 9.02872 -2.04429 -3.78195
    pos 2.41752 -14.6786 5.48116
    norm 7.30528 -6.28082 2.68034
  }
  poly {
    numVertices 3
    pos 12.1298 -13.7766 7.55099
    norm 4.79951 8.77215 0.118892
    pos 13.4579 -15.1211 7.84139
    norm 7.34013 4.49935 -5.08707
    pos 10.6095 -12.6927 6.73842
    norm 6.83096 7.30238 0.115532
  }
  poly {
    numVertices 3
    pos -2.13791 -3.39392 0.722582
    norm 2.8227 9.03571 3.2231
    pos -3.52944 -3.05024 0.736041
    norm 1.87693 6.12494 7.67869
    pos -2.40707 -3.70086 1.36944
    norm -0.239843 7.79161 6.26365
  }
  poly {
    numVertices 3
    pos 14.6149 -14.0403 13.3443
    norm 4.62677 8.73814 1.49593
    pos 14.3549 -13.8269 13.5641
    norm -4.00888 9.13736 -0.661526
    pos 15.0179 -14.4384 14.0639
    norm 9.34882 2.94242 1.98538
  }
  poly {
    numVertices 3
    pos 13.9479 -14.3227 10.2476
    norm -0.137379 9.99617 0.240422
    pos 13.2549 -14.7931 10.3128
    norm -8.0047 3.59806 4.79363
    pos 14.4181 -14.159 11.098
    norm 0.628696 9.89623 -1.29207
  }
  poly {
    numVertices 3
    pos 13.4294 -15.0981 10.7541
    norm -9.20901 0.456894 3.87109
    pos 14.4181 -14.159 11.098
    norm 0.628696 9.89623 -1.29207
    pos 13.2549 -14.7931 10.3128
    norm -8.0047 3.59806 4.79363
  }
  poly {
    numVertices 3
    pos 14.4181 -14.159 11.098
    norm 0.628696 9.89623 -1.29207
    pos 14.7615 -14.2546 11.6964
    norm 6.6062 7.35563 -1.50098
    pos 15.0734 -15.0724 11.3027
    norm 9.40422 2.44227 -2.36558
  }
  poly {
    numVertices 3
    pos 14.4181 -14.159 11.098
    norm 0.628696 9.89623 -1.29207
    pos 13.8867 -14.3911 11.2997
    norm -7.42869 6.68461 0.361266
    pos 14.7615 -14.2546 11.6964
    norm 6.6062 7.35563 -1.50098
  }
  poly {
    numVertices 3
    pos -0.89717 -4.52626 -0.804798
    norm 4.9225 7.68979 -4.07876
    pos 2.5073 -6.01438 1.25491
    norm 5.8757 7.35017 -3.38395
    pos 2.66949 -6.38225 0.685115
    norm 6.27073 6.46775 -4.34123
  }
  poly {
    numVertices 3
    pos -1.41203 -11.4555 4.84936
    norm -3.54584 -5.00315 7.89908
    pos -0.141278 -10.8745 5.46159
    norm -3.66977 -2.60014 8.93153
    pos -1.54057 -9.76134 5.1475
    norm -3.87715 -0.467208 9.20594
  }
  poly {
    numVertices 3
    pos 2.50165 -6.55031 4.875
    norm -0.209473 6.89509 7.23973
    pos 2.7402 -6.02309 4.17817
    norm 0.669737 7.72844 6.31052
    pos 1.13881 -6.01673 4.42871
    norm -0.628232 6.48513 7.58607
  }
  poly {
    numVertices 3
    pos 2.5073 -6.01438 1.25491
    norm 5.8757 7.35017 -3.38395
    pos -0.89717 -4.52626 -0.804798
    norm 4.9225 7.68979 -4.07876
    pos 4.16085 -6.57047 3.02101
    norm 6.06509 7.2655 -3.22909
  }
  poly {
    numVertices 3
    pos 10.6095 -12.6927 6.73842
    norm 6.83096 7.30238 0.115532
    pos 9.22152 -11.3098 6.48667
    norm 3.88048 6.24039 6.78228
    pos 10.3878 -12.8744 7.21761
    norm 1.7332 6.56232 7.34384
  }
  poly {
    numVertices 3
    pos 6.24673 -8.40136 3.8262
    norm 7.35641 6.3507 -2.35625
    pos 4.42427 -6.98383 2.47132
    norm 6.16395 7.06011 -3.48719
    pos 4.16085 -6.57047 3.02101
    norm 6.06509 7.2655 -3.22909
  }
  poly {
    numVertices 3
    pos 15.0179 -14.4384 14.0639
    norm 9.34882 2.94242 1.98538
    pos 14.3959 -13.9987 14.4834
    norm -0.196616 4.91428 8.70696
    pos 14.8238 -14.6449 14.4049
    norm 3.75364 -5.77145 7.25262
  }
  poly {
    numVertices 3
    pos 7.24279 -9.05532 5.38874
    norm 6.98985 7.02064 1.36114
    pos 9.22152 -11.3098 6.48667
    norm 3.88048 6.24039 6.78228
    pos 8.92635 -10.9024 5.56255
    norm 7.64951 5.54286 -3.2805
  }
  poly {
    numVertices 3
    pos -2.40707 -3.70086 1.36944
    norm -0.239843 7.79161 6.26365
    pos -3.52944 -3.05024 0.736041
    norm 1.87693 6.12494 7.67869
    pos -3.7561 -3.66817 1.18189
    norm -1.12644 5.95345 7.95535
  }
  poly {
    numVertices 3
    pos -2.40707 -3.70086 1.36944
    norm -0.239843 7.79161 6.26365
    pos -3.7561 -3.66817 1.18189
    norm -1.12644 5.95345 7.95535
    pos -2.58326 -5.15962 2.93761
    norm -1.51553 6.20177 7.69684
  }
  poly {
    numVertices 3
    pos 13.8867 -14.3911 11.2997
    norm -7.42869 6.68461 0.361266
    pos 14.4033 -14.0152 11.9425
    norm -0.259256 9.99522 -0.168179
    pos 14.7615 -14.2546 11.6964
    norm 6.6062 7.35563 -1.50098
  }
  poly {
    numVertices 3
    pos 14.7615 -14.2546 11.6964
    norm 6.6062 7.35563 -1.50098
    pos 14.4033 -14.0152 11.9425
    norm -0.259256 9.99522 -0.168179
    pos 15.3589 -14.9015 12.4757
    norm 9.70648 -1.07606 2.1509
  }
  poly {
    numVertices 3
    pos -4.47069 -11.6215 -2.42129
    norm -8.93755 -3.98278 -2.0634
    pos -4.163 -12.6847 -2.66679
    norm -9.92912 -1.18267 0.117671
    pos -3.8959 -13.1433 -2.06948
    norm -8.49338 -1.94664 4.90644
  }
  poly {
    numVertices 3
    pos -2.40707 -3.70086 1.36944
    norm -0.239843 7.79161 6.26365
    pos -2.58326 -5.15962 2.93761
    norm -1.51553 6.20177 7.69684
    pos -1.6586 -4.1407 2.12365
    norm -0.0900502 7.82454 6.22644
  }
  poly {
    numVertices 3
    pos -2.73953 -15.3335 -4.33695
    norm 6.40496 -1.51863 -7.52797
    pos -3.40532 -16.8933 -4.54874
    norm 9.26184 -1.48681 -3.4652
    pos -3.76136 -16.9466 -5.05756
    norm 6.25336 0.271389 -7.79883
  }
  poly {
    numVertices 3
    pos 7.24279 -9.05532 5.38874
    norm 6.98985 7.02064 1.36114
    pos 8.83447 -11.2836 6.62974
    norm 1.89934 4.28739 8.83237
    pos 9.22152 -11.3098 6.48667
    norm 3.88048 6.24039 6.78228
  }
  poly {
    numVertices 3
    pos 7.24279 -9.05532 5.38874
    norm 6.98985 7.02064 1.36114
    pos 5.47074 -7.06492 4.41854
    norm 6.9719 7.1489 -0.534642
    pos 6.07528 -7.88127 5.21381
    norm 4.44789 6.86933 5.74706
  }
  poly {
    numVertices 3
    pos 14.7716 -14.9307 10.6684
    norm 8.60765 3.63719 -3.56081
    pos 14.4181 -14.159 11.098
    norm 0.628696 9.89623 -1.29207
    pos 15.0734 -15.0724 11.3027
    norm 9.40422 2.44227 -2.36558
  }
  poly {
    numVertices 3
    pos 15.0791 -15.5421 11.6782
    norm 8.5558 -5.16098 -0.403146
    pos 15.0734 -15.0724 11.3027
    norm 9.40422 2.44227 -2.36558
    pos 14.7615 -14.2546 11.6964
    norm 6.6062 7.35563 -1.50098
  }
  poly {
    numVertices 3
    pos 15.3589 -14.9015 12.4757
    norm 9.70648 -1.07606 2.1509
    pos 14.4033 -14.0152 11.9425
    norm -0.259256 9.99522 -0.168179
    pos 14.9597 -14.3451 12.6113
    norm 6.04957 7.51608 2.62892
  }
  poly {
    numVertices 3
    pos 3.94738 -5.90251 3.77839
    norm 4.66748 8.84387 -0.0253589
    pos 4.28666 -6.41083 4.51174
    norm 2.28514 7.5741 6.11647
    pos 5.47074 -7.06492 4.41854
    norm 6.9719 7.1489 -0.534642
  }
  poly {
    numVertices 3
    pos 13.9092 -14.4893 9.48218
    norm 5.2732 8.28882 -1.86783
    pos 13.9479 -14.3227 10.2476
    norm -0.137379 9.99617 0.240422
    pos 14.7716 -14.9307 10.6684
    norm 8.60765 3.63719 -3.56081
  }
  poly {
    numVertices 3
    pos 12.9284 -14.287 9.09242
    norm -1.95407 8.78251 4.36453
    pos 13.9479 -14.3227 10.2476
    norm -0.137379 9.99617 0.240422
    pos 13.9092 -14.4893 9.48218
    norm 5.2732 8.28882 -1.86783
  }
  poly {
    numVertices 3
    pos 2.18633 -5.28753 3.29878
    norm 1.23168 8.57469 4.99576
    pos 4.28666 -6.41083 4.51174
    norm 2.28514 7.5741 6.11647
    pos 3.94738 -5.90251 3.77839
    norm 4.66748 8.84387 -0.0253589
  }
  poly {
    numVertices 3
    pos -2.13791 -3.39392 0.722582
    norm 2.8227 9.03571 3.2231
    pos 2.18633 -5.28753 3.29878
    norm 1.23168 8.57469 4.99576
    pos 3.94738 -5.90251 3.77839
    norm 4.66748 8.84387 -0.0253589
  }
  poly {
    numVertices 3
    pos -5.86864 -12.1499 1.57719
    norm -9.09612 -4.15328 -0.104921
    pos -5.90258 -11.7864 2.19089
    norm -8.17213 -2.99435 4.92445
    pos -6.52836 -10.8655 1.39294
    norm -9.40618 -3.11557 1.34795
  }
  poly {
    numVertices 3
    pos 1.06354 -18.3718 4.20986
    norm 5.13237 0.0353872 -8.5824
    pos 1.06416 -14.8908 3.66895
    norm 1.99546 -1.27894 -9.71506
    pos 1.49221 -15.8261 4.04828
    norm 6.69793 -2.07483 -7.12971
  }
  poly {
    numVertices 3
    pos -1.36154 -13.2043 0.290559
    norm -2.03814 -9.71714 -1.19297
    pos -2.46167 -12.6482 -0.746944
    norm -2.10803 -9.73919 -0.83927
    pos -0.357896 -13.2263 0.0601258
    norm -0.146638 -9.46451 -3.22515
  }
  poly {
    numVertices 3
    pos -4.94828 -19.5758 1.99663
    norm 0.481895 -9.91623 1.19837
    pos -6.01756 -19.1983 0.864143
    norm -8.31338 -3.23897 -4.51629
    pos -5.51585 -19.4426 0.605064
    norm -4.25677 -8.80978 -2.06585
  }
  poly {
    numVertices 3
    pos -4.94828 -19.5758 1.99663
    norm 0.481895 -9.91623 1.19837
    pos -5.51585 -19.4426 0.605064
    norm -4.25677 -8.80978 -2.06585
    pos -3.95442 -19.3788 1.48969
    norm 4.60156 -8.83616 0.864803
  }
  poly {
    numVertices 3
    pos -0.677277 -11.281 -2.11471
    norm 5.59542 -5.69032 -6.0259
    pos 0.25531 -11.6454 -1.93218
    norm 0.465279 -5.38191 -8.41537
    pos 0.37187 -12.7397 -0.903383
    norm 0.748865 -7.68155 -6.35869
  }
  poly {
    numVertices 3
    pos 4.78829 -18.1591 1.58407
    norm -8.49068 3.96241 -3.49395
    pos 4.32077 -19.1593 0.92638
    norm -5.76429 -3.76151 -7.25424
    pos 3.87057 -19.2953 1.09091
    norm -8.81646 -3.40422 -3.26824
  }
  poly {
    numVertices 3
    pos -5.51585 -19.4426 0.605064
    norm -4.25677 -8.80978 -2.06585
    pos -4.61753 -19.4945 0.453298
    norm 3.03687 -8.53415 -4.23624
    pos -3.95442 -19.3788 1.48969
    norm 4.60156 -8.83616 0.864803
  }
  poly {
    numVertices 3
    pos -0.677277 -11.281 -2.11471
    norm 5.59542 -5.69032 -6.0259
    pos -1.17107 -12.2054 -1.40582
    norm 4.76051 -8.33617 -2.80104
    pos -1.62737 -12.7661 -2.13916
    norm 8.26146 -4.79473 2.95955
  }
  poly {
    numVertices 3
    pos 3.98159 -14.4435 2.56982
    norm -4.78334 -7.88491 3.86624
    pos 2.31774 -13.6426 2.28765
    norm -2.74809 -9.60543 0.428646
    pos 2.5949 -13.6132 1.74831
    norm -3.32224 -9.38673 -0.923094
  }
  poly {
    numVertices 3
    pos -4.45782 -19.3487 -3.93444
    norm 3.18647 -9.29505 1.85698
    pos -4.49025 -19.4335 -5.45155
    norm 3.35634 -9.41696 0.236478
    pos -3.8027 -18.8281 -4.01741
    norm 7.55102 -5.20987 3.97988
  }
  poly {
    numVertices 3
    pos 2.5949 -13.6132 1.74831
    norm -3.32224 -9.38673 -0.923094
    pos 2.28293 -13.2826 1.18152
    norm -0.148885 -8.9958 -4.36503
    pos 3.38326 -14.2725 1.69782
    norm -7.40815 -6.63655 1.03705
  }
  poly {
    numVertices 3
    pos 4.37221 -15.1109 2.26629
    norm -6.46204 -3.07488 6.98479
    pos 3.38326 -14.2725 1.69782
    norm -7.40815 -6.63655 1.03705
    pos 3.74017 -14.8183 1.58109
    norm -8.79283 -3.26142 3.47121
  }
  poly {
    numVertices 3
    pos -1.17107 -12.2054 -1.40582
    norm 4.76051 -8.33617 -2.80104
    pos -0.677277 -11.281 -2.11471
    norm 5.59542 -5.69032 -6.0259
    pos 0.37187 -12.7397 -0.903383
    norm 0.748865 -7.68155 -6.35869
  }
  poly {
    numVertices 3
    pos 12.5146 -16.6051 8.32545
    norm -2.50109 -9.50222 1.85805
    pos 11.087 -15.991 7.88525
    norm -5.75047 -4.4346 6.87505
    pos 8.86135 -15.5277 6.3165
    norm -4.56015 -8.37189 3.01935
  }
  poly {
    numVertices 3
    pos 4.37221 -15.1109 2.26629
    norm -6.46204 -3.07488 6.98479
    pos 3.98159 -14.4435 2.56982
    norm -4.78334 -7.88491 3.86624
    pos 3.38326 -14.2725 1.69782
    norm -7.40815 -6.63655 1.03705
  }
  poly {
    numVertices 3
    pos -4.0326 -14.5984 0.816786
    norm 4.98562 -0.562871 -8.65025
    pos -4.57349 -14.8283 0.72303
    norm -0.778585 0.263387 -9.96616
    pos -3.92176 -13.6286 0.731951
    norm 2.62178 -2.20689 -9.39446
  }
  poly {
    numVertices 3
    pos 1.95109 -12.8602 0.366746
    norm 2.67699 -7.63905 -5.87185
    pos 2.41549 -12.4938 0.186379
    norm 0.631879 -6.67923 -7.41543
    pos 2.80769 -13.2753 0.903531
    norm -5.46502 -7.46703 -3.7917
  }
  poly {
    numVertices 3
    pos -3.95442 -19.3788 1.48969
    norm 4.60156 -8.83616 0.864803
    pos -4.61753 -19.4945 0.453298
    norm 3.03687 -8.53415 -4.23624
    pos -3.62061 -19.0037 1.10979
    norm 8.19336 -4.03503 -4.07276
  }
  poly {
    numVertices 3
    pos -1.72534 -12.5177 -1.18808
    norm 0.78716 -9.81368 -1.75274
    pos -1.17107 -12.2054 -1.40582
    norm 4.76051 -8.33617 -2.80104
    pos 0.37187 -12.7397 -0.903383
    norm 0.748865 -7.68155 -6.35869
  }
  poly {
    numVertices 3
    pos -0.334905 -14.7387 4.80715
    norm -9.80808 0.379854 -1.9124
    pos -0.283247 -16.1208 5.31343
    norm -9.98621 0.0295973 0.524017
    pos -0.368209 -14.303 5.31731
    norm -9.65013 0.450162 2.58308
  }
  poly {
    numVertices 3
    pos 1.81833 -16.2318 4.58615
    norm 9.02872 -2.04429 -3.78195
    pos 2.35422 -14.6418 4.83468
    norm 7.92009 -4.98631 -3.52262
    pos 2.41752 -14.6786 5.48116
    norm 7.30528 -6.28082 2.68034
  }
  poly {
    numVertices 3
    pos 1.49221 -15.8261 4.04828
    norm 6.69793 -2.07483 -7.12971
    pos 2.35422 -14.6418 4.83468
    norm 7.92009 -4.98631 -3.52262
    pos 1.81833 -16.2318 4.58615
    norm 9.02872 -2.04429 -3.78195
  }
  poly {
    numVertices 3
    pos 0.25531 -11.6454 -1.93218
    norm 0.465279 -5.38191 -8.41537
    pos 1.10714 -12.3829 -1.0688
    norm 3.98915 -6.74336 -6.21399
    pos 0.37187 -12.7397 -0.903383
    norm 0.748865 -7.68155 -6.35869
  }
  poly {
    numVertices 3
    pos -3.61009 -10.4979 -4.00697
    norm -3.01526 0.0475826 -9.53446
    pos -3.78867 -11.5941 -3.7573
    norm -7.40648 -1.66693 -6.50888
    pos -4.44626 -9.80535 -3.71284
    norm -6.99382 -0.865093 -7.09493
  }
  poly {
    numVertices 3
    pos 1.49221 -15.8261 4.04828
    norm 6.69793 -2.07483 -7.12971
    pos 2.16127 -14.3865 4.19042
    norm 6.62226 -4.64314 -5.88108
    pos 2.35422 -14.6418 4.83468
    norm 7.92009 -4.98631 -3.52262
  }
  poly {
    numVertices 3
    pos 1.60151 -14.2125 3.6987
    norm 2.20372 -3.99881 -8.89681
    pos 2.16127 -14.3865 4.19042
    norm 6.62226 -4.64314 -5.88108
    pos 1.49221 -15.8261 4.04828
    norm 6.69793 -2.07483 -7.12971
  }
  poly {
    numVertices 3
    pos 1.34363 -19.5344 6.11335
    norm 5.19898 -5.25268 6.73646
    pos 0.916356 -19.1587 6.45121
    norm 1.08535 -2.93586 9.49751
    pos 0.426079 -19.5653 6.04336
    norm -0.223378 -9.64842 2.61882
  }
  poly {
    numVertices 3
    pos 1.67222 -17.6491 4.94128
    norm 9.29694 -0.492403 -3.65028
    pos 1.34481 -17.6313 4.32421
    norm 7.75381 -1.02879 -6.23058
    pos 1.81833 -16.2318 4.58615
    norm 9.02872 -2.04429 -3.78195
  }
  poly {
    numVertices 3
    pos -5.11027 -13.1041 0.859035
    norm -6.99962 -3.89051 -5.98909
    pos -4.31979 -12.7319 0.377351
    norm -1.72291 -7.06117 -6.86815
    pos -3.92176 -13.6286 0.731951
    norm 2.62178 -2.20689 -9.39446
  }
  poly {
    numVertices 3
    pos -2.88157 -13.809 -1.92653
    norm 2.27185 -3.31637 9.15644
    pos -3.46427 -13.6093 -1.77623
    norm -2.8395 -2.96263 9.11921
    pos -3.67895 -14.6637 -2.20106
    norm -1.54476 -2.89539 9.44619
  }
  poly {
    numVertices 3
    pos -3.92176 -13.6286 0.731951
    norm 2.62178 -2.20689 -9.39446
    pos -3.09029 -13.3382 1.11748
    norm 4.12123 -7.0672 -5.75066
    pos -3.15492 -14.1057 1.49503
    norm 8.18651 -2.4407 -5.19846
  }
  poly {
    numVertices 3
    pos 2.24058 -13.866 3.83001
    norm 2.74746 -8.13723 -5.1222
    pos 3.96296 -13.845 5.31131
    norm 1.68375 -9.50935 2.59563
    pos 3.25728 -14.1647 5.3891
    norm 5.63874 -8.12302 -1.49034
  }
  poly {
    numVertices 3
    pos -3.09029 -13.3382 1.11748
    norm 4.12123 -7.0672 -5.75066
    pos -1.99347 -12.9008 1.58513
    norm -1.46428 -9.82497 1.15144
    pos -2.62799 -12.7573 1.64045
    norm 3.08013 -8.67321 3.91002
  }
  poly {
    numVertices 3
    pos -3.92176 -13.6286 0.731951
    norm 2.62178 -2.20689 -9.39446
    pos -4.31979 -12.7319 0.377351
    norm -1.72291 -7.06117 -6.86815
    pos -3.09029 -13.3382 1.11748
    norm 4.12123 -7.0672 -5.75066
  }
  poly {
    numVertices 3
    pos 2.16127 -14.3865 4.19042
    norm 6.62226 -4.64314 -5.88108
    pos 2.24058 -13.866 3.83001
    norm 2.74746 -8.13723 -5.1222
    pos 3.25728 -14.1647 5.3891
    norm 5.63874 -8.12302 -1.49034
  }
  poly {
    numVertices 3
    pos 1.60151 -14.2125 3.6987
    norm 2.20372 -3.99881 -8.89681
    pos 2.24058 -13.866 3.83001
    norm 2.74746 -8.13723 -5.1222
    pos 2.16127 -14.3865 4.19042
    norm 6.62226 -4.64314 -5.88108
  }
  poly {
    numVertices 3
    pos -1.99347 -12.9008 1.58513
    norm -1.46428 -9.82497 1.15144
    pos -1.12156 -13.2487 1.61868
    norm -1.99203 -9.76177 0.859989
    pos -1.20858 -12.8907 3.09074
    norm -3.10844 -8.89057 3.36085
  }
  poly {
    numVertices 3
    pos 12.431 -16.2295 9.06171
    norm -5.26734 -6.52142 5.45218
    pos 14.1432 -15.9577 11.0633
    norm -3.28274 -8.75119 3.55532
    pos 12.5064 -15.4412 9.36332
    norm -7.26255 -2.16443 6.52462
  }
  poly {
    numVertices 3
    pos -6.4463 -18.8541 -4.66278
    norm -7.95525 2.36024 -5.58062
    pos -6.10561 -19.4817 -4.4047
    norm -3.17217 -9.46973 -0.51153
    pos -6.68188 -19.2314 -4.35633
    norm -8.526 -5.14793 -0.89787
  }
  poly {
    numVertices 3
    pos -0.0581061 -16.5159 4.3851
    norm -8.90835 -0.622618 -4.50041
    pos -0.153755 -14.7952 4.09062
    norm -7.99686 -0.275063 -5.99788
    pos 0.508093 -17.5007 3.96244
    norm -2.8014 -0.75583 -9.56979
  }
  poly {
    numVertices 3
    pos 0.0136405 -12.2344 4.57136
    norm -8.80427 -3.52215 3.1748
    pos -0.440669 -11.6301 4.8394
    norm -3.31797 -6.12626 7.17356
    pos -1.5737 -12.207 3.74263
    norm -3.70792 -7.70746 5.18136
  }
  poly {
    numVertices 3
    pos 1.60151 -14.2125 3.6987
    norm 2.20372 -3.99881 -8.89681
    pos 0.899806 -13.3662 3.1669
    norm -2.47061 -9.61816 -1.17773
    pos 2.24058 -13.866 3.83001
    norm 2.74746 -8.13723 -5.1222
  }
  poly {
    numVertices 3
    pos -6.20664 -19.3608 -4.92499
    norm -4.21599 -8.80304 -2.17529
    pos -6.10561 -19.4817 -4.4047
    norm -3.17217 -9.46973 -0.51153
    pos -6.4463 -18.8541 -4.66278
    norm -7.95525 2.36024 -5.58062
  }
  poly {
    numVertices 3
    pos 0.899806 -13.3662 3.1669
    norm -2.47061 -9.61816 -1.17773
    pos 1.99368 -13.5825 3.13191
    norm -1.53619 -9.86583 -0.552767
    pos 2.24058 -13.866 3.83001
    norm 2.74746 -8.13723 -5.1222
  }
  poly {
    numVertices 3
    pos 9.16125 -15.5749 5.49072
    norm -1.40995 -8.29987 -5.39669
    pos 10.4504 -16.0691 6.22518
    norm -0.833738 -9.45258 -3.15492
    pos 8.86135 -15.5277 6.3165
    norm -4.56015 -8.37189 3.01935
  }
  poly {
    numVertices 3
    pos 1.99368 -13.5825 3.13191
    norm -1.53619 -9.86583 -0.552767
    pos 0.899806 -13.3662 3.1669
    norm -2.47061 -9.61816 -1.17773
    pos -1.12156 -13.2487 1.61868
    norm -1.99203 -9.76177 0.859989
  }
  poly {
    numVertices 3
    pos -2.83174 -11.4541 3.35851
    norm -4.15278 -6.81769 6.02275
    pos -1.5737 -12.207 3.74263
    norm -3.70792 -7.70746 5.18136
    pos -2.08653 -11.8198 4.08392
    norm -5.37258 -6.81002 4.97583
  }
  poly {
    numVertices 3
    pos 1.06416 -14.8908 3.66895
    norm 1.99546 -1.27894 -9.71506
    pos 1.06354 -18.3718 4.20986
    norm 5.13237 0.0353872 -8.5824
    pos 0.508093 -17.5007 3.96244
    norm -2.8014 -0.75583 -9.56979
  }
  poly {
    numVertices 3
    pos 0.899806 -13.3662 3.1669
    norm -2.47061 -9.61816 -1.17773
    pos 1.60151 -14.2125 3.6987
    norm 2.20372 -3.99881 -8.89681
    pos 0.634711 -13.1459 3.57186
    norm -5.46732 -6.82679 -4.84804
  }
  poly {
    numVertices 3
    pos 14.5757 -15.3658 12.6944
    norm -0.132056 -8.98233 4.3932
    pos 14.0665 -14.8584 12.9211
    norm -9.77944 -1.60865 1.33222
    pos 14.3834 -15.5749 12.1345
    norm -2.79924 -9.01807 3.2922
  }
  poly {
    numVertices 3
    pos -3.0696 -12.7332 -4.27864
    norm -2.75791 1.24789 -9.53083
    pos -3.08106 -13.6066 -4.46041
    norm -0.742782 2.22322 -9.7214
    pos -3.3886 -12.9942 -4.04817
    norm -6.60869 1.92688 -7.25344
  }
  poly {
    numVertices 3
    pos -3.8959 -13.1433 -2.06948
    norm -8.49338 -1.94664 4.90644
    pos -3.95801 -13.9431 -2.18399
    norm -7.31147 0.0977299 6.82151
    pos -3.46427 -13.6093 -1.77623
    norm -2.8395 -2.96263 9.11921
  }
  poly {
    numVertices 3
    pos 13.8352 -15.4399 11.3672
    norm -8.20566 -4.45987 3.57445
    pos 14.3834 -15.5749 12.1345
    norm -2.79924 -9.01807 3.2922
    pos 13.833 -14.9394 11.7083
    norm -9.77509 0.805147 1.94915
  }
  poly {
    numVertices 3
    pos -5.86864 -12.1499 1.57719
    norm -9.09612 -4.15328 -0.104921
    pos -6.52836 -10.8655 1.39294
    norm -9.40618 -3.11557 1.34795
    pos -6.31301 -10.9601 0.0356826
    norm -8.95624 -4.11627 -1.68584
  }
  poly {
    numVertices 3
    pos 14.6805 -15.7524 11.8333
    norm 0.69947 -9.1607 3.94872
    pos 14.3834 -15.5749 12.1345
    norm -2.79924 -9.01807 3.2922
    pos 13.8352 -15.4399 11.3672
    norm -8.20566 -4.45987 3.57445
  }
  poly {
    numVertices 3
    pos -4.47069 -11.6215 -2.42129
    norm -8.93755 -3.98278 -2.0634
    pos -4.51294 -11.9106 -1.79198
    norm -6.63337 -7.44736 -0.731678
    pos -5.40614 -10.1019 -2.23332
    norm -8.29182 -1.4815 -5.38988
  }
  poly {
    numVertices 3
    pos -6.20664 -19.3608 -4.92499
    norm -4.21599 -8.80304 -2.17529
    pos -4.45782 -19.3487 -3.93444
    norm 3.18647 -9.29505 1.85698
    pos -6.10561 -19.4817 -4.4047
    norm -3.17217 -9.46973 -0.51153
  }
  poly {
    numVertices 3
    pos 6.56405 -19.4502 1.91472
    norm 2.91858 -9.36404 1.94848
    pos 6.87304 -19.4288 0.605895
    norm 6.27903 -7.50803 -2.05021
    pos 7.37924 -18.6891 2.15138
    norm 9.23978 -1.9749 3.27511
  }
  poly {
    numVertices 3
    pos 5.03338 -16.9736 0.343904
    norm -2.88769 -4.37162 -8.51764
    pos 5.11581 -17.9895 1.08666
    norm -7.47975 -1.60475 -6.44036
    pos 3.84096 -15.6113 0.261495
    norm -7.84306 -3.62031 -5.03783
  }
  poly {
    numVertices 3
    pos 14.1432 -15.9577 11.0633
    norm -3.28274 -8.75119 3.55532
    pos 13.8352 -15.4399 11.3672
    norm -8.20566 -4.45987 3.57445
    pos 13.4294 -15.0981 10.7541
    norm -9.20901 0.456894 3.87109
  }
  poly {
    numVertices 3
    pos 14.6805 -15.7524 11.8333
    norm 0.69947 -9.1607 3.94872
    pos 13.8352 -15.4399 11.3672
    norm -8.20566 -4.45987 3.57445
    pos 14.1432 -15.9577 11.0633
    norm -3.28274 -8.75119 3.55532
  }
  poly {
    numVertices 3
    pos -3.0696 -12.7332 -4.27864
    norm -2.75791 1.24789 -9.53083
    pos -3.3886 -12.9942 -4.04817
    norm -6.60869 1.92688 -7.25344
    pos -3.09565 -11.3845 -4.19898
    norm -1.19905 0.884341 -9.88839
  }
  poly {
    numVertices 3
    pos 6.56405 -19.4502 1.91472
    norm 2.91858 -9.36404 1.94848
    pos 6.74407 -19.04 2.64974
    norm 3.60163 -7.088 6.06535
    pos 5.70841 -19.3793 2.33548
    norm -0.0113854 -9.29228 3.69503
  }
  poly {
    numVertices 3
    pos -6.68188 -19.2314 -4.35633
    norm -8.526 -5.14793 -0.89787
    pos -6.10561 -19.4817 -4.4047
    norm -3.17217 -9.46973 -0.51153
    pos -6.34574 -19.2106 -3.95814
    norm -4.83054 -5.13626 7.09118
  }
  poly {
    numVertices 3
    pos -6.10561 -19.4817 -4.4047
    norm -3.17217 -9.46973 -0.51153
    pos -5.16662 -19.3946 -3.58875
    norm 0.214517 -8.2453 5.65412
    pos -6.34574 -19.2106 -3.95814
    norm -4.83054 -5.13626 7.09118
  }
  poly {
    numVertices 3
    pos -4.45782 -19.3487 -3.93444
    norm 3.18647 -9.29505 1.85698
    pos -4.42753 -18.8147 -3.34335
    norm 4.87093 -4.5342 7.46425
    pos -5.16662 -19.3946 -3.58875
    norm 0.214517 -8.2453 5.65412
  }
  poly {
    numVertices 3
    pos 4.60674 -19.3379 2.15178
    norm -4.37939 -7.62477 4.76276
    pos 4.18665 -18.8733 1.83591
    norm -9.04527 2.70881 3.29324
    pos 3.87057 -19.2953 1.09091
    norm -8.81646 -3.40422 -3.26824
  }
  poly {
    numVertices 3
    pos 8.55083 -15.1088 6.68321
    norm -4.25555 -5.58175 7.12282
    pos 8.76807 -14.6156 7.03628
    norm -3.17875 -2.74516 9.07522
    pos 7.42283 -14.3698 6.54049
    norm -4.24943 -4.21384 8.01161
  }
  poly {
    numVertices 3
    pos -5.16662 -19.3946 -3.58875
    norm 0.214517 -8.2453 5.65412
    pos -4.42753 -18.8147 -3.34335
    norm 4.87093 -4.5342 7.46425
    pos -4.9726 -18.8315 -3.15757
    norm -0.342929 -2.75362 9.60729
  }
  poly {
    numVertices 3
    pos -0.153755 -14.7952 4.09062
    norm -7.99686 -0.275063 -5.99788
    pos -0.0581061 -16.5159 4.3851
    norm -8.90835 -0.622618 -4.50041
    pos -0.334905 -14.7387 4.80715
    norm -9.80808 0.379854 -1.9124
  }
  poly {
    numVertices 3
    pos -1.27763 -19.0498 4.83098
    norm -9.98953 -0.388803 0.240663
    pos -0.666849 -19.4694 4.75092
    norm -3.70606 -9.28357 -0.283884
    pos -0.799095 -19.4412 5.65921
    norm -5.07439 -7.78489 3.69407
  }
  poly {
    numVertices 3
    pos 0.157518 -12.7358 3.98195
    norm -9.20241 -3.80209 -0.9273
    pos 0.634711 -13.1459 3.57186
    norm -5.46732 -6.82679 -4.84804
    pos -0.153755 -14.7952 4.09062
    norm -7.99686 -0.275063 -5.99788
  }
  poly {
    numVertices 3
    pos -5.58623 -19.4082 2.71577
    norm -2.11591 -7.41205 6.37059
    pos -6.12839 -18.9616 2.31741
    norm -9.08452 0.785912 4.10534
    pos -6.1455 -19.4069 1.54821
    norm -9.17673 -3.6539 -1.56097
  }
  poly {
    numVertices 3
    pos 5.73481 -13.7659 5.83379
    norm -4.08864 -7.10352 5.72914
    pos 7.42283 -14.3698 6.54049
    norm -4.24943 -4.21384 8.01161
    pos 6.49444 -13.181 6.57407
    norm -3.31623 -3.50608 8.75842
  }
  poly {
    numVertices 3
    pos 13.833 -14.9394 11.7083
    norm -9.77509 0.805147 1.94915
    pos 14.3834 -15.5749 12.1345
    norm -2.79924 -9.01807 3.2922
    pos 14.0665 -14.8584 12.9211
    norm -9.77944 -1.60865 1.33222
  }
  poly {
    numVertices 3
    pos -2.19534 -14.5856 -2.5191
    norm 5.02069 -4.6074 7.31879
    pos -2.1929 -13.0381 -1.98032
    norm 4.27925 -5.69345 7.01945
    pos -2.88157 -13.809 -1.92653
    norm 2.27185 -3.31637 9.15644
  }
  poly {
    numVertices 3
    pos -0.666849 -19.4694 4.75092
    norm -3.70606 -9.28357 -0.283884
    pos 0.426079 -19.5653 6.04336
    norm -0.223378 -9.64842 2.61882
    pos -0.799095 -19.4412 5.65921
    norm -5.07439 -7.78489 3.69407
  }
  poly {
    numVertices 3
    pos -5.6559 -17.7624 1.87307
    norm -9.8892 0.938324 -1.15032
    pos -6.1455 -19.4069 1.54821
    norm -9.17673 -3.6539 -1.56097
    pos -6.12839 -18.9616 2.31741
    norm -9.08452 0.785912 4.10534
  }
  poly {
    numVertices 3
    pos 3.74017 -14.8183 1.58109
    norm -8.79283 -3.26142 3.47121
    pos 3.71647 -15.4835 0.993134
    norm -9.11882 -3.74517 1.67953
    pos 4.51454 -17.2297 1.45603
    norm -9.2191 -3.76161 -0.926524
  }
  poly {
    numVertices 3
    pos -3.8959 -13.1433 -2.06948
    norm -8.49338 -1.94664 4.90644
    pos -3.93253 -12.4792 -1.74255
    norm -7.42124 -4.34416 5.10426
    pos -4.47069 -11.6215 -2.42129
    norm -8.93755 -3.98278 -2.0634
  }
  poly {
    numVertices 3
    pos 4.37221 -15.1109 2.26629
    norm -6.46204 -3.07488 6.98479
    pos 5.01767 -14.9434 2.7632
    norm 0.414586 -5.90578 8.05915
    pos 3.98159 -14.4435 2.56982
    norm -4.78334 -7.88491 3.86624
  }
  poly {
    numVertices 3
    pos -5.16662 -19.3946 -3.58875
    norm 0.214517 -8.2453 5.65412
    pos -6.10561 -19.4817 -4.4047
    norm -3.17217 -9.46973 -0.51153
    pos -4.45782 -19.3487 -3.93444
    norm 3.18647 -9.29505 1.85698
  }
  poly {
    numVertices 3
    pos 4.40375 -16.8088 2.12267
    norm -8.71396 -2.20582 4.38192
    pos 4.49541 -16.1976 2.44063
    norm -6.32529 -0.408662 7.73459
    pos 3.74017 -14.8183 1.58109
    norm -8.79283 -3.26142 3.47121
  }
  poly {
    numVertices 3
    pos -3.46427 -13.6093 -1.77623
    norm -2.8395 -2.96263 9.11921
    pos -3.93253 -12.4792 -1.74255
    norm -7.42124 -4.34416 5.10426
    pos -3.8959 -13.1433 -2.06948
    norm -8.49338 -1.94664 4.90644
  }
  poly {
    numVertices 3
    pos 4.49541 -16.1976 2.44063
    norm -6.32529 -0.408662 7.73459
    pos 4.37221 -15.1109 2.26629
    norm -6.46204 -3.07488 6.98479
    pos 3.74017 -14.8183 1.58109
    norm -8.79283 -3.26142 3.47121
  }
  poly {
    numVertices 3
    pos -3.08106 -13.6066 -4.46041
    norm -0.742782 2.22322 -9.7214
    pos -4.1396 -14.962 -4.7755
    norm -2.63305 3.36237 -9.04221
    pos -3.3886 -12.9942 -4.04817
    norm -6.60869 1.92688 -7.25344
  }
  poly {
    numVertices 3
    pos -5.40374 -17.5601 -3.37764
    norm -7.27576 1.06843 6.77657
    pos -5.70136 -16.3119 -3.53508
    norm -9.17219 1.17268 3.80733
    pos -5.64811 -16.6603 -3.81808
    norm -9.99552 0.231984 0.188913
  }
  poly {
    numVertices 3
    pos -5.54138 -18.7444 0.488878
    norm -9.15698 3.38027 -2.17337
    pos -5.51585 -19.4426 0.605064
    norm -4.25677 -8.80978 -2.06585
    pos -6.01756 -19.1983 0.864143
    norm -8.31338 -3.23897 -4.51629
  }
  poly {
    numVertices 3
    pos 4.38104 -15.3573 -0.19007
    norm -3.86855 -2.08298 -8.98307
    pos 4.23647 -12.5242 -0.434443
    norm -2.77891 -0.109656 -9.6055
    pos 4.65564 -13.6955 -0.393571
    norm 0.1017 -0.68103 -9.97626
  }
  poly {
    numVertices 3
    pos 4.23647 -12.5242 -0.434443
    norm -2.77891 -0.109656 -9.6055
    pos 4.53682 -11.5524 -0.407543
    norm -0.730066 2.46174 -9.66472
    pos 4.76982 -12.6031 -0.487966
    norm 1.88983 -0.0460285 -9.8197
  }
  poly {
    numVertices 3
    pos 4.38104 -15.3573 -0.19007
    norm -3.86855 -2.08298 -8.98307
    pos 3.48108 -12.0348 0.0176834
    norm -2.71987 -1.45157 -9.5129
    pos 4.23647 -12.5242 -0.434443
    norm -2.77891 -0.109656 -9.6055
  }
  poly {
    numVertices 3
    pos -4.7385 -19.2777 -5.96447
    norm -1.84915 -5.05636 -8.42698
    pos -5.39593 -19.2514 -6.0843
    norm -4.47909 -1.31291 -8.84386
    pos -4.58881 -18.4664 -5.96442
    norm -1.03224 3.63142 -9.25998
  }
  poly {
    numVertices 3
    pos 15.0791 -15.5421 11.6782
    norm 8.5558 -5.16098 -0.403146
    pos 13.937 -16.4061 9.72636
    norm 1.69619 -9.73679 1.52245
    pos 14.6449 -15.9389 9.7333
    norm 8.68951 -3.85237 -3.10672
  }
  poly {
    numVertices 3
    pos 6.13391 -14.1456 4.45689
    norm 4.24323 -8.88238 1.76021
    pos 5.18419 -14.3767 3.39361
    norm -0.20731 -9.29047 3.6938
    pos 6.46578 -13.7723 3.93901
    norm 3.91257 -9.19225 -0.440992
  }
  poly {
    numVertices 3
    pos 4.78829 -18.1591 1.58407
    norm -8.49068 3.96241 -3.49395
    pos 4.7818 -18.0847 2.10223
    norm -9.96826 0.236757 0.760023
    pos 4.51454 -17.2297 1.45603
    norm -9.2191 -3.76161 -0.926524
  }
  poly {
    numVertices 3
    pos 14.3834 -15.5749 12.1345
    norm -2.79924 -9.01807 3.2922
    pos 15.3589 -14.9015 12.4757
    norm 9.70648 -1.07606 2.1509
    pos 15.0512 -15.3047 12.6233
    norm 6.92159 -6.49921 3.13877
  }
  poly {
    numVertices 3
    pos -2.67378 -12.6316 -1.64741
    norm 1.19233 -7.34456 6.68101
    pos -3.46427 -13.6093 -1.77623
    norm -2.8395 -2.96263 9.11921
    pos -2.88157 -13.809 -1.92653
    norm 2.27185 -3.31637 9.15644
  }
  poly {
    numVertices 3
    pos 3.25728 -14.1647 5.3891
    norm 5.63874 -8.12302 -1.49034
    pos 4.95033 -12.8112 6.32751
    norm 1.34532 -6.21054 7.72135
    pos 2.41752 -14.6786 5.48116
    norm 7.30528 -6.28082 2.68034
  }
  poly {
    numVertices 3
    pos 5.73481 -13.7659 5.83379
    norm -4.08864 -7.10352 5.72914
    pos 6.49444 -13.181 6.57407
    norm -3.31623 -3.50608 8.75842
    pos 5.28919 -13.2799 6.08438
    norm -1.80188 -6.628 7.26794
  }
  poly {
    numVertices 3
    pos -2.1929 -13.0381 -1.98032
    norm 4.27925 -5.69345 7.01945
    pos -2.67378 -12.6316 -1.64741
    norm 1.19233 -7.34456 6.68101
    pos -2.88157 -13.809 -1.92653
    norm 2.27185 -3.31637 9.15644
  }
  poly {
    numVertices 3
    pos 4.11606 -13.9358 4.64945
    norm -1.08573 -9.62851 2.47243
    pos 5.28919 -13.2799 6.08438
    norm -1.80188 -6.628 7.26794
    pos 3.96296 -13.845 5.31131
    norm 1.68375 -9.50935 2.59563
  }
  poly {
    numVertices 3
    pos 14.3834 -15.5749 12.1345
    norm -2.79924 -9.01807 3.2922
    pos 14.6805 -15.7524 11.8333
    norm 0.69947 -9.1607 3.94872
    pos 15.3589 -14.9015 12.4757
    norm 9.70648 -1.07606 2.1509
  }
  poly {
    numVertices 3
    pos 12.5146 -16.6051 8.32545
    norm -2.50109 -9.50222 1.85805
    pos 13.937 -16.4061 9.72636
    norm 1.69619 -9.73679 1.52245
    pos 12.431 -16.2295 9.06171
    norm -5.26734 -6.52142 5.45218
  }
  poly {
    numVertices 3
    pos 12.5146 -16.6051 8.32545
    norm -2.50109 -9.50222 1.85805
    pos 12.9711 -16.5308 7.91567
    norm 3.64796 -8.60927 -3.54583
    pos 13.937 -16.4061 9.72636
    norm 1.69619 -9.73679 1.52245
  }
  poly {
    numVertices 3
    pos 5.73658 -14.7578 2.50278
    norm 7.11003 -3.92935 5.83162
    pos 6.34665 -13.8848 3.30822
    norm 6.26657 -7.77736 0.492668
    pos 5.01767 -14.9434 2.7632
    norm 0.414586 -5.90578 8.05915
  }
  poly {
    numVertices 3
    pos -2.67378 -12.6316 -1.64741
    norm 1.19233 -7.34456 6.68101
    pos -3.28285 -12.4916 -1.3556
    norm -3.19206 -6.31825 7.06332
    pos -3.46427 -13.6093 -1.77623
    norm -2.8395 -2.96263 9.11921
  }
  poly {
    numVertices 3
    pos 14.0665 -14.8584 12.9211
    norm -9.77944 -1.60865 1.33222
    pos 14.5757 -15.3658 12.6944
    norm -0.132056 -8.98233 4.3932
    pos 14.72 -15.1718 13.5718
    norm 1.0773 -8.42496 5.27821
  }
  poly {
    numVertices 3
    pos 3.96296 -13.845 5.31131
    norm 1.68375 -9.50935 2.59563
    pos 5.28919 -13.2799 6.08438
    norm -1.80188 -6.628 7.26794
    pos 4.95033 -12.8112 6.32751
    norm 1.34532 -6.21054 7.72135
  }
  poly {
    numVertices 3
    pos 2.64476 -12.8334 6.95851
    norm -0.118875 -1.26298 9.91921
    pos 2.76759 -13.5402 6.69144
    norm 2.18427 -5.17082 8.27597
    pos 5.15808 -11.259 7.02667
    norm 1.60188 -1.11773 9.80738
  }
  poly {
    numVertices 3
    pos 6.34665 -13.8848 3.30822
    norm 6.26657 -7.77736 0.492668
    pos 7.34488 -13.0766 3.5858
    norm 5.73292 -3.69174 -7.31469
    pos 7.10629 -13.6283 3.83632
    norm 2.082 -7.3437 -6.46029
  }
  poly {
    numVertices 3
    pos 4.73871 -14.2177 4.37438
    norm -2.11038 -9.6019 1.83024
    pos 5.88352 -14.1734 4.86874
    norm -0.798628 -9.82047 1.70898
    pos 4.11606 -13.9358 4.64945
    norm -1.08573 -9.62851 2.47243
  }
  poly {
    numVertices 3
    pos -4.07782 -16.7707 2.7645
    norm 6.10066 0.127016 7.92249
    pos -3.82645 -16.9752 2.05766
    norm 9.18596 -0.487118 3.92185
    pos -3.65413 -15.3926 2.68235
    norm 6.94342 -2.63732 6.69577
  }
  poly {
    numVertices 3
    pos 4.95033 -12.8112 6.32751
    norm 1.34532 -6.21054 7.72135
    pos 5.8825 -11.6913 6.83246
    norm 0.0785027 -0.309978 9.99488
    pos 5.15808 -11.259 7.02667
    norm 1.60188 -1.11773 9.80738
  }
  poly {
    numVertices 3
    pos 5.18419 -14.3767 3.39361
    norm -0.20731 -9.29047 3.6938
    pos 6.13391 -14.1456 4.45689
    norm 4.24323 -8.88238 1.76021
    pos 4.62076 -14.2961 3.4172
    norm -1.57245 -9.64242 2.13335
  }
  poly {
    numVertices 3
    pos -3.25182 -13.8052 3.20214
    norm 7.13934 -1.76287 6.77659
    pos -3.57977 -13.548 3.56998
    norm 2.22492 -0.0265895 9.74931
    pos -4.09451 -15.121 3.07477
    norm 2.64513 -3.03033 9.15534
  }
  poly {
    numVertices 3
    pos -3.65413 -15.3926 2.68235
    norm 6.94342 -2.63732 6.69577
    pos -3.25182 -13.8052 3.20214
    norm 7.13934 -1.76287 6.77659
    pos -4.09451 -15.121 3.07477
    norm 2.64513 -3.03033 9.15534
  }
  poly {
    numVertices 3
    pos 5.01767 -14.9434 2.7632
    norm 0.414586 -5.90578 8.05915
    pos 6.34665 -13.8848 3.30822
    norm 6.26657 -7.77736 0.492668
    pos 6.46578 -13.7723 3.93901
    norm 3.91257 -9.19225 -0.440992
  }
  poly {
    numVertices 3
    pos 15.0791 -15.5421 11.6782
    norm 8.5558 -5.16098 -0.403146
    pos 14.6805 -15.7524 11.8333
    norm 0.69947 -9.1607 3.94872
    pos 14.1432 -15.9577 11.0633
    norm -3.28274 -8.75119 3.55532
  }
  poly {
    numVertices 3
    pos 2.76759 -13.5402 6.69144
    norm 2.18427 -5.17082 8.27597
    pos 2.41752 -14.6786 5.48116
    norm 7.30528 -6.28082 2.68034
    pos 4.95033 -12.8112 6.32751
    norm 1.34532 -6.21054 7.72135
  }
  poly {
    numVertices 3
    pos 5.18419 -14.3767 3.39361
    norm -0.20731 -9.29047 3.6938
    pos 5.01767 -14.9434 2.7632
    norm 0.414586 -5.90578 8.05915
    pos 6.46578 -13.7723 3.93901
    norm 3.91257 -9.19225 -0.440992
  }
  poly {
    numVertices 3
    pos 7.10629 -13.6283 3.83632
    norm 2.082 -7.3437 -6.46029
    pos 6.46578 -13.7723 3.93901
    norm 3.91257 -9.19225 -0.440992
    pos 6.34665 -13.8848 3.30822
    norm 6.26657 -7.77736 0.492668
  }
  poly {
    numVertices 3
    pos -3.25182 -13.8052 3.20214
    norm 7.13934 -1.76287 6.77659
    pos -3.00445 -13.7999 2.7631
    norm 9.2791 -1.71494 3.31018
    pos -3.07857 -11.868 2.95479
    norm 3.02265 -2.34406 9.23953
  }
  poly {
    numVertices 3
    pos 4.11606 -13.9358 4.64945
    norm -1.08573 -9.62851 2.47243
    pos 3.96296 -13.845 5.31131
    norm 1.68375 -9.50935 2.59563
    pos 2.24058 -13.866 3.83001
    norm 2.74746 -8.13723 -5.1222
  }
  poly {
    numVertices 3
    pos 5.47606 -15.4799 2.53888
    norm 4.71801 -0.115056 8.8163
    pos 5.73658 -14.7578 2.50278
    norm 7.11003 -3.92935 5.83162
    pos 5.01767 -14.9434 2.7632
    norm 0.414586 -5.90578 8.05915
  }
  poly {
    numVertices 3
    pos -3.00445 -13.7999 2.7631
    norm 9.2791 -1.71494 3.31018
    pos -2.8119 -13.0048 2.23074
    norm 9.28423 -2.69607 2.55625
    pos -3.07857 -11.868 2.95479
    norm 3.02265 -2.34406 9.23953
  }
  poly {
    numVertices 3
    pos -2.8119 -13.0048 2.23074
    norm 9.28423 -2.69607 2.55625
    pos -2.83174 -11.4541 3.35851
    norm -4.15278 -6.81769 6.02275
    pos -3.07857 -11.868 2.95479
    norm 3.02265 -2.34406 9.23953
  }
  poly {
    numVertices 3
    pos -2.8119 -13.0048 2.23074
    norm 9.28423 -2.69607 2.55625
    pos -2.62799 -12.7573 1.64045
    norm 3.08013 -8.67321 3.91002
    pos -2.83174 -11.4541 3.35851
    norm -4.15278 -6.81769 6.02275
  }
  poly {
    numVertices 3
    pos 4.73871 -14.2177 4.37438
    norm -2.11038 -9.6019 1.83024
    pos 2.31774 -13.6426 2.28765
    norm -2.74809 -9.60543 0.428646
    pos 4.62076 -14.2961 3.4172
    norm -1.57245 -9.64242 2.13335
  }
  poly {
    numVertices 3
    pos 14.5757 -15.3658 12.6944
    norm -0.132056 -8.98233 4.3932
    pos 15.0512 -15.3047 12.6233
    norm 6.92159 -6.49921 3.13877
    pos 15.2959 -14.58 13.1373
    norm 9.41907 2.89557 1.70202
  }
  poly {
    numVertices 3
    pos -2.75314 -10.7816 4.20168
    norm -6.37077 -2.93335 7.12804
    pos -2.83174 -11.4541 3.35851
    norm -4.15278 -6.81769 6.02275
    pos -2.08653 -11.8198 4.08392
    norm -5.37258 -6.81002 4.97583
  }
  poly {
    numVertices 3
    pos 13.937 -16.4061 9.72636
    norm 1.69619 -9.73679 1.52245
    pos 14.1432 -15.9577 11.0633
    norm -3.28274 -8.75119 3.55532
    pos 12.431 -16.2295 9.06171
    norm -5.26734 -6.52142 5.45218
  }
  poly {
    numVertices 3
    pos 1.34481 -17.6313 4.32421
    norm 7.75381 -1.02879 -6.23058
    pos 1.49221 -15.8261 4.04828
    norm 6.69793 -2.07483 -7.12971
    pos 1.81833 -16.2318 4.58615
    norm 9.02872 -2.04429 -3.78195
  }
  poly {
    numVertices 3
    pos -6.68188 -19.2314 -4.35633
    norm -8.526 -5.14793 -0.89787
    pos -6.34574 -19.2106 -3.95814
    norm -4.83054 -5.13626 7.09118
    pos -6.6304 -18.4853 -4.01595
    norm -8.33235 3.20496 4.50557
  }
  poly {
    numVertices 3
    pos 3.87057 -19.2953 1.09091
    norm -8.81646 -3.40422 -3.26824
    pos 4.18665 -18.8733 1.83591
    norm -9.04527 2.70881 3.29324
    pos 4.78829 -18.1591 1.58407
    norm -8.49068 3.96241 -3.49395
  }
  poly {
    numVertices 3
    pos -0.132075 -18.0928 4.40838
    norm -6.58332 4.50687 -6.02893
    pos -1.13676 -19.0025 4.13924
    norm -9.76427 1.88834 -1.04558
    pos -0.347828 -17.881 5.04525
    norm -8.72187 4.83423 -0.747798
  }
  poly {
    numVertices 3
    pos 3.48108 -12.0348 0.0176834
    norm -2.71987 -1.45157 -9.5129
    pos 4.38104 -15.3573 -0.19007
    norm -3.86855 -2.08298 -8.98307
    pos 3.84096 -15.6113 0.261495
    norm -7.84306 -3.62031 -5.03783
  }
  poly {
    numVertices 3
    pos -1.79687 -14.7058 -2.95384
    norm 7.30355 -5.18284 4.4493
    pos -2.43726 -15.3994 -3.93026
    norm 8.14933 -4.94443 -3.02343
    pos -1.60103 -14.4594 -3.38552
    norm 9.60254 -2.66033 -0.844921
  }
  poly {
    numVertices 3
    pos -4.39043 0.99754 -1.6567
    norm 6.19548 1.38771 -7.72596
    pos -5.40552 0.16046 -2.46657
    norm 2.65352 2.18544 -9.39056
    pos -5.0833 0.98698 -2.12313
    norm 3.99763 1.4455 -9.05149
  }
  poly {
    numVertices 3
    pos 1.99368 -13.5825 3.13191
    norm -1.53619 -9.86583 -0.552767
    pos 4.11606 -13.9358 4.64945
    norm -1.08573 -9.62851 2.47243
    pos 2.24058 -13.866 3.83001
    norm 2.74746 -8.13723 -5.1222
  }
  poly {
    numVertices 3
    pos 4.11606 -13.9358 4.64945
    norm -1.08573 -9.62851 2.47243
    pos 1.99368 -13.5825 3.13191
    norm -1.53619 -9.86583 -0.552767
    pos 4.73871 -14.2177 4.37438
    norm -2.11038 -9.6019 1.83024
  }
  poly {
    numVertices 3
    pos -4.33866 -16.9699 -2.98265
    norm 3.10092 -3.56512 8.8133
    pos -2.88157 -13.809 -1.92653
    norm 2.27185 -3.31637 9.15644
    pos -3.67895 -14.6637 -2.20106
    norm -1.54476 -2.89539 9.44619
  }
  poly {
    numVertices 3
    pos -5.72269 -12.0342 0.203904
    norm -6.6302 -6.71224 -3.31457
    pos -5.11027 -13.1041 0.859035
    norm -6.99962 -3.89051 -5.98909
    pos -6.31301 -10.9601 0.0356826
    norm -8.95624 -4.11627 -1.68584
  }
  poly {
    numVertices 3
    pos -5.82201 4.33718 0.299859
    norm -6.71597 -0.248429 7.40501
    pos -5.22285 4.31418 0.629247
    norm -2.87914 -0.928193 9.53148
    pos -5.22402 6.57518 0.862218
    norm -4.59514 -0.820964 8.84369
  }
  poly {
    numVertices 3
    pos -4.42361 -18.4625 3.06688
    norm 1.2685 -1.31164 9.83212
    pos -4.73337 -17.0081 2.82271
    norm -1.32038 -0.222874 9.90994
    pos -4.98335 -18.4228 2.99007
    norm -2.65841 1.1534 9.57092
  }
  poly {
    numVertices 3
    pos -6.5942 1.11603 -1.93166
    norm -9.33229 1.76466 -3.12959
    pos -6.38869 3.01368 -0.701171
    norm -9.91581 1.2354 0.387893
    pos -6.18026 4.11301 -1.09287
    norm -9.43279 1.84354 -2.76114
  }
  poly {
    numVertices 3
    pos -5.64277 -18.6945 0.0114914
    norm -5.64334 6.91606 -4.50787
    pos -4.73778 -18.4412 0.38361
    norm 1.70897 3.82109 -9.08178
    pos -5.27899 -18.8243 -0.162918
    norm 1.72187 3.43433 -9.23258
  }
  poly {
    numVertices 3
    pos -1.12623 -18.9312 5.58733
    norm -8.76816 0.530275 4.77893
    pos -0.799095 -19.4412 5.65921
    norm -5.07439 -7.78489 3.69407
    pos -0.527417 -19.112 6.20347
    norm -5.13615 -0.573981 8.56099
  }
  poly {
    numVertices 3
    pos 1.24592 -10.3963 -2.27277
    norm 4.52717 -1.62262 -8.76766
    pos 2.08434 -9.85147 -1.75287
    norm 6.14109 -0.0169408 -7.89219
    pos 1.83164 -10.7767 -1.80439
    norm 5.52848 -2.72112 -7.87601
  }
  poly {
    numVertices 3
    pos 0.556453 -13.4349 1.53279
    norm -0.476012 -9.90126 -1.3185
    pos -0.357896 -13.2263 0.0601258
    norm -0.146638 -9.46451 -3.22515
    pos 2.28293 -13.2826 1.18152
    norm -0.148885 -8.9958 -4.36503
  }
  poly {
    numVertices 3
    pos 7.70443 -10.2055 4.42118
    norm 7.85518 5.2371 -3.2968
    pos 8.92635 -10.9024 5.56255
    norm 7.64951 5.54286 -3.2805
    pos 7.81209 -10.9002 3.95792
    norm 7.99863 2.98298 -5.20805
  }
  poly {
    numVertices 3
    pos -6.76337 -1.47748 -2.52411
    norm -6.89324 0.413042 -7.23276
    pos -5.35082 -1.32634 -2.74761
    norm 2.54944 1.17083 -9.59841
    pos -5.48056 -2.86297 -2.79063
    norm -0.0272999 0.0652342 -9.99975
  }
  poly {
    numVertices 3
    pos 11.2811 -14.4095 8.1446
    norm -5.26387 3.75025 7.63069
    pos 12.4158 -14.8994 9.2069
    norm -6.3765 4.03786 6.56018
    pos 12.9284 -14.287 9.09242
    norm -1.95407 8.78251 4.36453
  }
  poly {
    numVertices 3
    pos -4.45284 -11.0302 -2.99362
    norm -8.65514 -2.57609 -4.29561
    pos -4.47069 -11.6215 -2.42129
    norm -8.93755 -3.98278 -2.0634
    pos -4.99445 -9.69996 -2.84098
    norm -8.56445 -1.05053 -5.05437
  }
  poly {
    numVertices 3
    pos -3.93149 -4.04676 -2.38923
    norm 4.04495 3.39763 -8.49085
    pos -3.53988 -5.77013 -2.67183
    norm -0.0518293 3.00429 -9.5379
    pos -4.41575 -5.39906 -2.72035
    norm 0.304998 0.0797159 -9.99504
  }
  poly {
    numVertices 3
    pos 9.16125 -15.5749 5.49072
    norm -1.40995 -8.29987 -5.39669
    pos 7.10629 -13.6283 3.83632
    norm 2.082 -7.3437 -6.46029
    pos 8.62872 -14.367 4.41651
    norm 1.84261 -4.41137 -8.7832
  }
  poly {
    numVertices 3
    pos 1.11993 -19.3723 3.91824
    norm 4.53941 -5.72818 -6.82508
    pos -1.08242 -19.3609 3.54029
    norm -5.95052 -5.48093 -5.87798
    pos -0.283818 -19.0143 3.46991
    norm 0.589034 3.23168 -9.44507
  }
  poly {
    numVertices 3
    pos 6.07528 -7.88127 5.21381
    norm 4.44789 6.86933 5.74706
    pos 8.83447 -11.2836 6.62974
    norm 1.89934 4.28739 8.83237
    pos 7.24279 -9.05532 5.38874
    norm 6.98985 7.02064 1.36114
  }
  poly {
    numVertices 3
    pos -4.10131 -1.57873 0.24944
    norm 4.88343 2.51075 8.35752
    pos -4.50336 -1.00268 0.358761
    norm 2.33573 0.553308 9.70764
    pos -4.47609 -2.48377 0.540099
    norm 0.315882 2.7505 9.60911
  }
  poly {
    numVertices 3
    pos -4.56752 7.83438 -0.79817
    norm 3.64294 1.59626 -9.17502
    pos -4.24376 10.6749 -0.148825
    norm 6.01758 1.29344 -7.88135
    pos -4.02677 9.63628 -0.007707
    norm 8.11428 1.02779 -5.75344
  }
  poly {
    numVertices 3
    pos -6.03612 2.2523 -2.00852
    norm -1.11556 2.14083 -9.70424
    pos -5.52957 1.33959 -2.16328
    norm 0.737567 1.89886 -9.79032
    pos -6.51485 0.20576 -2.34626
    norm -3.54166 2.09553 -9.11402
  }
  poly {
    numVertices 3
    pos -2.3899 -8.41909 -3.7683
    norm 2.526 1.41984 -9.57097
    pos -0.897379 -9.00866 -3.28279
    norm 5.1314 -0.470598 -8.57014
    pos -2.15269 -9.90045 -3.74557
    norm 3.69494 0.52175 -9.27768
  }
  poly {
    numVertices 3
    pos -2.97614 -10.0888 -3.96776
    norm 1.18365 1.17877 -9.85948
    pos -2.15269 -9.90045 -3.74557
    norm 3.69494 0.52175 -9.27768
    pos -2.32546 -10.8304 -4.03467
    norm 3.46307 1.71881 -9.22241
  }
  poly {
    numVertices 3
    pos -5.70448 8.23418 0.42774
    norm -9.06602 0.144092 4.21741
    pos -6.24211 2.74096 -0.200528
    norm -8.29056 0.385716 5.57834
    pos -5.82201 4.33718 0.299859
    norm -6.71597 -0.248429 7.40501
  }
  poly {
    numVertices 3
    pos -4.70929 5.13828 -1.26631
    norm 4.614 1.41352 -8.7586
    pos -5.05599 2.40479 -1.97649
    norm 3.02521 1.52326 -9.40893
    pos -5.23633 4.51948 -1.50026
    norm 0.50282 2.06563 -9.77141
  }
  poly {
    numVertices 3
    pos 1.51897 -11.4031 6.89312
    norm -1.99592 0.663922 9.77627
    pos 0.740866 -12.1068 6.69971
    norm -4.56911 0.0836025 8.89473
    pos 1.3167 -12.7386 6.82562
    norm -2.05511 -0.887939 9.74619
  }
  poly {
    numVertices 3
    pos 1.49221 -15.8261 4.04828
    norm 6.69793 -2.07483 -7.12971
    pos 1.06416 -14.8908 3.66895
    norm 1.99546 -1.27894 -9.71506
    pos 1.60151 -14.2125 3.6987
    norm 2.20372 -3.99881 -8.89681
  }
  poly {
    numVertices 3
    pos -1.79687 -14.7058 -2.95384
    norm 7.30355 -5.18284 4.4493
    pos -1.60103 -14.4594 -3.38552
    norm 9.60254 -2.66033 -0.844921
    pos -1.62737 -12.7661 -2.13916
    norm 8.26146 -4.79473 2.95955
  }
  poly {
    numVertices 3
    pos -2.26993 -4.73804 -1.96651
    norm 3.27523 6.54553 -6.81387
    pos -2.85128 -4.57989 -2.09792
    norm 4.82665 7.31818 -4.81122
    pos -2.04646 -4.13963 -1.17775
    norm 4.5813 6.94476 -5.54814
  }
  poly {
    numVertices 3
    pos 15.2959 -14.58 13.1373
    norm 9.41907 2.89557 1.70202
    pos 14.2199 -14.1732 12.2989
    norm -5.06096 8.6173 0.359022
    pos 14.2153 -13.7597 12.7611
    norm -2.9103 9.56188 -0.317319
  }
  poly {
    numVertices 3
    pos -6.1455 -19.4069 1.54821
    norm -9.17673 -3.6539 -1.56097
    pos -5.37705 -17.985 1.3533
    norm -8.60207 2.27413 -4.5643
    pos -6.01756 -19.1983 0.864143
    norm -8.31338 -3.23897 -4.51629
  }
  poly {
    numVertices 3
    pos -5.37705 -17.985 1.3533
    norm -8.60207 2.27413 -4.5643
    pos -6.1455 -19.4069 1.54821
    norm -9.17673 -3.6539 -1.56097
    pos -5.6559 -17.7624 1.87307
    norm -9.8892 0.938324 -1.15032
  }
  poly {
    numVertices 3
    pos -0.132075 -18.0928 4.40838
    norm -6.58332 4.50687 -6.02893
    pos 0.615579 -18.4413 3.97877
    norm 1.41317 2.72626 -9.51685
    pos -0.283818 -19.0143 3.46991
    norm 0.589034 3.23168 -9.44507
  }
  poly {
    numVertices 3
    pos 13.8867 -14.3911 11.2997
    norm -7.42869 6.68461 0.361266
    pos 13.8352 -15.4399 11.3672
    norm -8.20566 -4.45987 3.57445
    pos 13.833 -14.9394 11.7083
    norm -9.77509 0.805147 1.94915
  }
  poly {
    numVertices 3
    pos 4.38104 -15.3573 -0.19007
    norm -3.86855 -2.08298 -8.98307
    pos 5.03338 -16.9736 0.343904
    norm -2.88769 -4.37162 -8.51764
    pos 3.84096 -15.6113 0.261495
    norm -7.84306 -3.62031 -5.03783
  }
  poly {
    numVertices 3
    pos 3.71647 -15.4835 0.993134
    norm -9.11882 -3.74517 1.67953
    pos 3.84096 -15.6113 0.261495
    norm -7.84306 -3.62031 -5.03783
    pos 4.51454 -17.2297 1.45603
    norm -9.2191 -3.76161 -0.926524
  }
  poly {
    numVertices 3
    pos -6.52836 -10.8655 1.39294
    norm -9.40618 -3.11557 1.34795
    pos -6.70611 -10.0692 0.710534
    norm -9.76272 -0.789839 -2.01628
    pos -6.31301 -10.9601 0.0356826
    norm -8.95624 -4.11627 -1.68584
  }
  poly {
    numVertices 3
    pos 13.4579 -15.1211 7.84139
    norm 7.34013 4.49935 -5.08707
    pos 13.2641 -15.5252 7.47795
    norm 7.69544 0.144981 -6.38429
    pos 12.6226 -15.1059 6.90908
    norm 6.91303 1.83304 -6.98928
  }
  poly {
    numVertices 3
    pos 10.1191 -15.3859 7.4543
    norm -5.10142 -3.25194 7.96243
    pos 9.45757 -14.5149 7.30453
    norm -3.4148 -1.1364 9.32994
    pos 8.55083 -15.1088 6.68321
    norm -4.25555 -5.58175 7.12282
  }
  poly {
    numVertices 3
    pos -5.11027 -13.1041 0.859035
    norm -6.99962 -3.89051 -5.98909
    pos -3.92176 -13.6286 0.731951
    norm 2.62178 -2.20689 -9.39446
    pos -4.57349 -14.8283 0.72303
    norm -0.778585 0.263387 -9.96616
  }
  poly {
    numVertices 3
    pos -4.61753 -19.4945 0.453298
    norm 3.03687 -8.53415 -4.23624
    pos -5.65118 -19.1634 -0.121674
    norm -6.26051 -4.53746 -6.34172
    pos -5.0548 -19.1499 -0.108273
    norm 3.25779 -3.50773 -8.77967
  }
  poly {
    numVertices 3
    pos -3.59295 -18.7918 -4.53027
    norm 9.00954 -4.2537 0.856904
    pos -3.47146 -17.0699 -3.9623
    norm 8.9259 -3.66105 2.63157
    pos -3.8027 -18.8281 -4.01741
    norm 7.55102 -5.20987 3.97988
  }
  poly {
    numVertices 3
    pos -3.47146 -17.0699 -3.9623
    norm 8.9259 -3.66105 2.63157
    pos -3.59295 -18.7918 -4.53027
    norm 9.00954 -4.2537 0.856904
    pos -3.40532 -16.8933 -4.54874
    norm 9.26184 -1.48681 -3.4652
  }
  poly {
    numVertices 3
    pos 1.10714 -12.3829 -1.0688
    norm 3.98915 -6.74336 -6.21399
    pos 0.25531 -11.6454 -1.93218
    norm 0.465279 -5.38191 -8.41537
    pos 1.11372 -11.2381 -2.00032
    norm 3.4597 -4.65003 -8.14909
  }
  poly {
    numVertices 3
    pos -3.89908 6.96278 -0.342101
    norm 9.45917 0.280449 -3.23195
    pos -3.76933 8.38318 0.0669733
    norm 9.7092 0.543715 -2.33146
    pos -3.99182 4.45988 0.161384
    norm 9.51856 -0.555067 3.01477
  }
  poly {
    numVertices 3
    pos 13.4294 -15.0981 10.7541
    norm -9.20901 0.456894 3.87109
    pos 13.8867 -14.3911 11.2997
    norm -7.42869 6.68461 0.361266
    pos 14.4181 -14.159 11.098
    norm 0.628696 9.89623 -1.29207
  }
  poly {
    numVertices 3
    pos -5.90258 -11.7864 2.19089
    norm -8.17213 -2.99435 4.92445
    pos -6.41234 -10.2989 1.92819
    norm -8.64858 -1.11023 4.89586
    pos -6.52836 -10.8655 1.39294
    norm -9.40618 -3.11557 1.34795
  }
  poly {
    numVertices 3
    pos 1.06354 -18.3718 4.20986
    norm 5.13237 0.0353872 -8.5824
    pos 1.49221 -15.8261 4.04828
    norm 6.69793 -2.07483 -7.12971
    pos 1.34481 -17.6313 4.32421
    norm 7.75381 -1.02879 -6.23058
  }
  poly {
    numVertices 3
    pos 14.3362 -13.8803 14
    norm -4.81193 8.69132 -1.14297
    pos 14.1917 -14.694 14.1011
    norm -6.84043 -5.78602 4.4419
    pos 14.3959 -13.9987 14.4834
    norm -0.196616 4.91428 8.70696
  }
  poly {
    numVertices 3
    pos 14.1917 -14.694 14.1011
    norm -6.84043 -5.78602 4.4419
    pos 14.3362 -13.8803 14
    norm -4.81193 8.69132 -1.14297
    pos 14.0515 -14.4601 13.5955
    norm -9.92904 -0.230176 1.16675
  }
  poly {
    numVertices 3
    pos -3.94479 -9.40631 3.45026
    norm -3.72836 -0.432315 9.26889
    pos -4.43964 -10.526 3.38932
    norm -1.6898 -0.93405 9.81184
    pos -3.5269 -10.4585 3.5318
    norm -3.71794 -3.3034 8.67552
  }
  poly {
    numVertices 3
    pos -0.283247 -16.1208 5.31343
    norm -9.98621 0.0295973 0.524017
    pos -0.334905 -14.7387 4.80715
    norm -9.80808 0.379854 -1.9124
    pos -0.0581061 -16.5159 4.3851
    norm -8.90835 -0.622618 -4.50041
  }
  poly {
    numVertices 3
    pos 13.4294 -15.0981 10.7541
    norm -9.20901 0.456894 3.87109
    pos 13.2549 -14.7931 10.3128
    norm -8.0047 3.59806 4.79363
    pos 13.5226 -15.8573 10.5449
    norm -7.75747 -4.87038 4.01261
  }
  poly {
    numVertices 3
    pos -3.8027 -18.8281 -4.01741
    norm 7.55102 -5.20987 3.97988
    pos -4.49025 -19.4335 -5.45155
    norm 3.35634 -9.41696 0.236478
    pos -3.59295 -18.7918 -4.53027
    norm 9.00954 -4.2537 0.856904
  }
  poly {
    numVertices 3
    pos -3.68464 -15.8654 -4.90982
    norm 2.47642 1.428 -9.5827
    pos -2.73953 -15.3335 -4.33695
    norm 6.40496 -1.51863 -7.52797
    pos -3.76136 -16.9466 -5.05756
    norm 6.25336 0.271389 -7.79883
  }
  poly {
    numVertices 3
    pos 2.80769 -13.2753 0.903531
    norm -5.46502 -7.46703 -3.7917
    pos 3.15246 -14.0169 1.1708
    norm -8.84922 -4.5932 -0.770525
    pos 3.38326 -14.2725 1.69782
    norm -7.40815 -6.63655 1.03705
  }
  poly {
    numVertices 3
    pos 6.11378 -15.1697 1.76768
    norm 7.82517 1.79214 5.96281
    pos 6.28983 -14.3377 1.44391
    norm 9.67517 -2.50103 0.368631
    pos 5.73658 -14.7578 2.50278
    norm 7.11003 -3.92935 5.83162
  }
  poly {
    numVertices 3
    pos 7.13286 -12.2295 2.65568
    norm 9.39183 -1.88633 -2.86973
    pos 6.28983 -14.3377 1.44391
    norm 9.67517 -2.50103 0.368631
    pos 6.92175 -12.6385 1.96847
    norm 9.27388 -1.3204 -3.50026
  }
  poly {
    numVertices 3
    pos 0.939623 -8.53884 -2.27839
    norm 3.72696 2.48527 -8.94054
    pos 1.80498 -8.38954 -1.75554
    norm 5.61535 2.10119 -8.0033
    pos 1.40846 -9.1765 -2.19909
    norm 4.33043 0.853549 -8.97323
  }
  poly {
    numVertices 3
    pos 7.34488 -13.0766 3.5858
    norm 5.73292 -3.69174 -7.31469
    pos 6.56015 -13.7083 2.5919
    norm 8.45985 -5.23714 -1.00163
    pos 7.13286 -12.2295 2.65568
    norm 9.39183 -1.88633 -2.86973
  }
  poly {
    numVertices 3
    pos -6.37764 -4.77125 -0.192311
    norm -9.64338 -0.808735 2.52017
    pos -5.97654 -6.55009 -1.04053
    norm -9.68913 -2.09191 -1.32087
    pos -5.9866 -6.63685 -0.256173
    norm -9.85918 -0.491795 -1.59838
  }
  poly {
    numVertices 3
    pos -5.97654 -6.55009 -1.04053
    norm -9.68913 -2.09191 -1.32087
    pos -6.37764 -4.77125 -0.192311
    norm -9.64338 -0.808735 2.52017
    pos -6.50666 -4.65668 -1.68746
    norm -9.48676 -2.6625 -1.70659
  }
  poly {
    numVertices 3
    pos 14.0515 -14.4601 13.5955
    norm -9.92904 -0.230176 1.16675
    pos 14.72 -15.1718 13.5718
    norm 1.0773 -8.42496 5.27821
    pos 14.1917 -14.694 14.1011
    norm -6.84043 -5.78602 4.4419
  }
  poly {
    numVertices 3
    pos 14.72 -15.1718 13.5718
    norm 1.0773 -8.42496 5.27821
    pos 14.0515 -14.4601 13.5955
    norm -9.92904 -0.230176 1.16675
    pos 14.0665 -14.8584 12.9211
    norm -9.77944 -1.60865 1.33222
  }
  poly {
    numVertices 3
    pos 2.16127 -14.3865 4.19042
    norm 6.62226 -4.64314 -5.88108
    pos 3.25728 -14.1647 5.3891
    norm 5.63874 -8.12302 -1.49034
    pos 2.35422 -14.6418 4.83468
    norm 7.92009 -4.98631 -3.52262
  }
  poly {
    numVertices 3
    pos 12.5064 -15.4412 9.36332
    norm -7.26255 -2.16443 6.52462
    pos 11.293 -14.9098 8.29398
    norm -6.13054 0.29173 7.89502
    pos 11.087 -15.991 7.88525
    norm -5.75047 -4.4346 6.87505
  }
  poly {
    numVertices 3
    pos 11.293 -14.9098 8.29398
    norm -6.13054 0.29173 7.89502
    pos 12.5064 -15.4412 9.36332
    norm -7.26255 -2.16443 6.52462
    pos 12.4158 -14.8994 9.2069
    norm -6.3765 4.03786 6.56018
  }
  poly {
    numVertices 3
    pos -5.40552 0.16046 -2.46657
    norm 2.65352 2.18544 -9.39056
    pos -5.35082 -1.32634 -2.74761
    norm 2.54944 1.17083 -9.59841
    pos -6.51485 0.20576 -2.34626
    norm -3.54166 2.09553 -9.11402
  }
  poly {
    numVertices 3
    pos -5.11027 -13.1041 0.859035
    norm -6.99962 -3.89051 -5.98909
    pos -5.72269 -12.0342 0.203904
    norm -6.6302 -6.71224 -3.31457
    pos -4.31979 -12.7319 0.377351
    norm -1.72291 -7.06117 -6.86815
  }
  poly {
    numVertices 3
    pos -1.93983 -4.95494 -1.93562
    norm 2.37489 6.51852 -7.202
    pos -0.487816 -5.06965 -1.22605
    norm 4.79827 6.76325 -5.58884
    pos -0.89814 -5.35791 -2.07046
    norm 4.42295 5.83826 -6.80824
  }
  poly {
    numVertices 3
    pos 5.73481 -13.7659 5.83379
    norm -4.08864 -7.10352 5.72914
    pos 4.11606 -13.9358 4.64945
    norm -1.08573 -9.62851 2.47243
    pos 5.88352 -14.1734 4.86874
    norm -0.798628 -9.82047 1.70898
  }
  poly {
    numVertices 3
    pos 4.11606 -13.9358 4.64945
    norm -1.08573 -9.62851 2.47243
    pos 5.73481 -13.7659 5.83379
    norm -4.08864 -7.10352 5.72914
    pos 5.28919 -13.2799 6.08438
    norm -1.80188 -6.628 7.26794
  }
  poly {
    numVertices 3
    pos 4.91991 -19.4177 1.19631
    norm -0.823745 -9.91614 -0.995827
    pos 6.87304 -19.4288 0.605895
    norm 6.27903 -7.50803 -2.05021
    pos 6.56405 -19.4502 1.91472
    norm 2.91858 -9.36404 1.94848
  }
  poly {
    numVertices 3
    pos 3.18722 -12.7883 0.396544
    norm -4.30866 -3.80774 -8.18148
    pos 3.84096 -15.6113 0.261495
    norm -7.84306 -3.62031 -5.03783
    pos 3.15246 -14.0169 1.1708
    norm -8.84922 -4.5932 -0.770525
  }
  poly {
    numVertices 3
    pos 3.84096 -15.6113 0.261495
    norm -7.84306 -3.62031 -5.03783
    pos 3.18722 -12.7883 0.396544
    norm -4.30866 -3.80774 -8.18148
    pos 3.48108 -12.0348 0.0176834
    norm -2.71987 -1.45157 -9.5129
  }
  poly {
    numVertices 3
    pos -4.21069 6.00638 -0.841693
    norm 6.98346 0.861646 -7.10555
    pos -4.26404 4.96568 -0.938556
    norm 7.22135 0.74888 -6.87686
    pos -4.70929 5.13828 -1.26631
    norm 4.614 1.41352 -8.7586
  }
  poly {
    numVertices 3
    pos 12.431 -16.2295 9.06171
    norm -5.26734 -6.52142 5.45218
    pos 11.087 -15.991 7.88525
    norm -5.75047 -4.4346 6.87505
    pos 12.5146 -16.6051 8.32545
    norm -2.50109 -9.50222 1.85805
  }
  poly {
    numVertices 3
    pos -0.134512 -12.5962 5.47859
    norm -9.74782 1.36904 1.76229
    pos -0.368209 -14.303 5.31731
    norm -9.65013 0.450162 2.58308
    pos 0.0206174 -13.8208 5.93032
    norm -7.14273 0.0755825 6.99827
  }
  poly {
    numVertices 3
    pos 0.191765 -8.83552 5.61098
    norm -2.88706 1.92562 9.37853
    pos 1.50041 -8.76085 6.05027
    norm -3.71296 3.35072 8.65948
    pos 1.40944 -7.87463 5.54854
    norm -2.27225 4.64026 8.56183
  }
  poly {
    numVertices 3
    pos -0.121226 -15.4517 5.7487
    norm -8.02474 -0.205263 5.96334
    pos 0.255151 -15.9748 6.21275
    norm -6.12898 0.0820215 7.90119
    pos 0.0206174 -13.8208 5.93032
    norm -7.14273 0.0755825 6.99827
  }
  poly {
    numVertices 3
    pos 14.7716 -14.9307 10.6684
    norm 8.60765 3.63719 -3.56081
    pos 15.0791 -15.5421 11.6782
    norm 8.5558 -5.16098 -0.403146
    pos 14.6449 -15.9389 9.7333
    norm 8.68951 -3.85237 -3.10672
  }
  poly {
    numVertices 3
    pos -6.52836 -10.8655 1.39294
    norm -9.40618 -3.11557 1.34795
    pos -6.74061 -9.87395 1.32155
    norm -9.66247 0.589177 2.50792
    pos -6.70611 -10.0692 0.710534
    norm -9.76272 -0.789839 -2.01628
  }
  poly {
    numVertices 3
    pos 7.83166 -11.7518 3.74877
    norm 7.28161 0.351777 -6.84503
    pos 7.98066 -12.4196 3.77291
    norm 4.55169 -0.147997 -8.90282
    pos 7.34488 -13.0766 3.5858
    norm 5.73292 -3.69174 -7.31469
  }
  poly {
    numVertices 3
    pos 6.07528 -7.88127 5.21381
    norm 4.44789 6.86933 5.74706
    pos 5.47074 -7.06492 4.41854
    norm 6.9719 7.1489 -0.534642
    pos 4.28666 -6.41083 4.51174
    norm 2.28514 7.5741 6.11647
  }
  poly {
    numVertices 3
    pos -5.46645 -13.3129 1.79579
    norm -9.88526 -0.608969 -1.38229
    pos -5.11027 -13.1041 0.859035
    norm -6.99962 -3.89051 -5.98909
    pos -5.12193 -13.6635 1.05269
    norm -7.38051 0.233406 -6.74342
  }
  poly {
    numVertices 3
    pos 5.15808 -11.259 7.02667
    norm 1.60188 -1.11773 9.80738
    pos 2.76759 -13.5402 6.69144
    norm 2.18427 -5.17082 8.27597
    pos 4.95033 -12.8112 6.32751
    norm 1.34532 -6.21054 7.72135
  }
  poly {
    numVertices 3
    pos 0.538156 -10.3933 -2.53274
    norm 1.8206 -1.91168 -9.64525
    pos 1.24592 -10.3963 -2.27277
    norm 4.52717 -1.62262 -8.76766
    pos 1.11372 -11.2381 -2.00032
    norm 3.4597 -4.65003 -8.14909
  }
  poly {
    numVertices 3
    pos 2.41549 -12.4938 0.186379
    norm 0.631879 -6.67923 -7.41543
    pos 3.15246 -14.0169 1.1708
    norm -8.84922 -4.5932 -0.770525
    pos 2.80769 -13.2753 0.903531
    norm -5.46502 -7.46703 -3.7917
  }
  poly {
    numVertices 3
    pos 3.18722 -12.7883 0.396544
    norm -4.30866 -3.80774 -8.18148
    pos 3.15246 -14.0169 1.1708
    norm -8.84922 -4.5932 -0.770525
    pos 2.41549 -12.4938 0.186379
    norm 0.631879 -6.67923 -7.41543
  }
  poly {
    numVertices 3
    pos -2.67378 -12.6316 -1.64741
    norm 1.19233 -7.34456 6.68101
    pos -1.72534 -12.5177 -1.18808
    norm 0.78716 -9.81368 -1.75274
    pos -2.46167 -12.6482 -0.746944
    norm -2.10803 -9.73919 -0.83927
  }
  poly {
    numVertices 3
    pos 10.6095 -12.6927 6.73842
    norm 6.83096 7.30238 0.115532
    pos 13.4579 -15.1211 7.84139
    norm 7.34013 4.49935 -5.08707
    pos 10.0073 -12.2825 5.97696
    norm 7.2667 5.21924 -4.46705
  }
  poly {
    numVertices 3
    pos 3.98159 -14.4435 2.56982
    norm -4.78334 -7.88491 3.86624
    pos 5.18419 -14.3767 3.39361
    norm -0.20731 -9.29047 3.6938
    pos 4.62076 -14.2961 3.4172
    norm -1.57245 -9.64242 2.13335
  }
  poly {
    numVertices 3
    pos 5.18419 -14.3767 3.39361
    norm -0.20731 -9.29047 3.6938
    pos 3.98159 -14.4435 2.56982
    norm -4.78334 -7.88491 3.86624
    pos 5.01767 -14.9434 2.7632
    norm 0.414586 -5.90578 8.05915
  }
  poly {
    numVertices 3
    pos -4.33039 -6.57474 -2.704
    norm -4.34739 1.7048 -8.84273
    pos -4.90419 -8.06049 -2.56106
    norm -8.05375 1.27667 -5.78853
    pos -4.99141 -6.75002 -2.38271
    norm -5.32882 -1.11321 -8.38835
  }
  poly {
    numVertices 3
    pos 6.48723 -18.1052 0.690953
    norm 3.23443 0.65688 -9.43965
    pos 6.081 -16.2644 0.324269
    norm 6.38897 0.561773 -7.67239
    pos 6.63421 -17.1719 0.818538
    norm 7.58243 1.68217 -6.29897
  }
  poly {
    numVertices 3
    pos 6.081 -16.2644 0.324269
    norm 6.38897 0.561773 -7.67239
    pos 6.48723 -18.1052 0.690953
    norm 3.23443 0.65688 -9.43965
    pos 4.97961 -15.3822 -0.338328
    norm 2.2475 -1.2381 -9.66518
  }
  poly {
    numVertices 3
    pos 15.0791 -15.5421 11.6782
    norm 8.5558 -5.16098 -0.403146
    pos 14.1432 -15.9577 11.0633
    norm -3.28274 -8.75119 3.55532
    pos 13.937 -16.4061 9.72636
    norm 1.69619 -9.73679 1.52245
  }
  poly {
    numVertices 3
    pos -3.24709 -12.7552 0.506168
    norm 0.419805 -8.9403 -4.46036
    pos -1.99347 -12.9008 1.58513
    norm -1.46428 -9.82497 1.15144
    pos -3.09029 -13.3382 1.11748
    norm 4.12123 -7.0672 -5.75066
  }
  poly {
    numVertices 3
    pos 5.36113 -14.5588 -0.166576
    norm 6.22493 0.314549 -7.81993
    pos 6.09135 -15.1202 0.594855
    norm 8.4985 0.981692 -5.17801
    pos 5.60475 -15.3098 0.0731382
    norm 6.35995 0.365823 -7.70826
  }
  poly {
    numVertices 3
    pos 6.09135 -15.1202 0.594855
    norm 8.4985 0.981692 -5.17801
    pos 5.36113 -14.5588 -0.166576
    norm 6.22493 0.314549 -7.81993
    pos 5.98871 -13.3607 0.632285
    norm 7.67601 0.280815 -6.40312
  }
  poly {
    numVertices 3
    pos 1.58199 -19.4485 5.60605
    norm 6.96701 -7.1643 -0.365442
    pos 1.77574 -18.4491 5.91374
    norm 9.02425 -1.17894 4.14403
    pos 1.34363 -19.5344 6.11335
    norm 5.19898 -5.25268 6.73646
  }
  poly {
    numVertices 3
    pos -1.17107 -12.2054 -1.40582
    norm 4.76051 -8.33617 -2.80104
    pos -2.1929 -13.0381 -1.98032
    norm 4.27925 -5.69345 7.01945
    pos -1.62737 -12.7661 -2.13916
    norm 8.26146 -4.79473 2.95955
  }
  poly {
    numVertices 3
    pos -4.1396 -14.962 -4.7755
    norm -2.63305 3.36237 -9.04221
    pos -4.90629 -17.784 -5.39786
    norm -4.52652 3.6587 -8.1317
    pos -4.79786 -16.1441 -4.88718
    norm -6.05979 2.91969 -7.39962
  }
  poly {
    numVertices 3
    pos -4.90397 4.0578 0.6206
    norm 0.628265 -1.13764 9.9152
    pos -4.14416 6.34258 0.769167
    norm 5.7389 -1.36193 8.07528
    pos -4.75588 6.33448 0.975947
    norm -0.537424 -1.34268 9.89487
  }
  poly {
    numVertices 3
    pos -5.40614 -10.1019 -2.23332
    norm -8.29182 -1.4815 -5.38988
    pos -5.40757 -11.031 -1.79466
    norm -7.23615 -4.23949 -5.44654
    pos -5.67109 -9.58226 -1.54096
    norm -8.73711 0.174782 -4.86131
  }
  poly {
    numVertices 3
    pos -0.290922 -17.9807 5.59832
    norm -8.3273 2.79946 4.77694
    pos -0.283247 -16.1208 5.31343
    norm -9.98621 0.0295973 0.524017
    pos -0.347828 -17.881 5.04525
    norm -8.72187 4.83423 -0.747798
  }
  poly {
    numVertices 3
    pos -0.283247 -16.1208 5.31343
    norm -9.98621 0.0295973 0.524017
    pos -0.290922 -17.9807 5.59832
    norm -8.3273 2.79946 4.77694
    pos -0.0845011 -16.56 5.88587
    norm -7.63868 -0.0609581 6.45344
  }
  poly {
    numVertices 3
    pos -0.666849 -19.4694 4.75092
    norm -3.70606 -9.28357 -0.283884
    pos -1.13676 -19.0025 4.13924
    norm -9.76427 1.88834 -1.04558
    pos -1.08242 -19.3609 3.54029
    norm -5.95052 -5.48093 -5.87798
  }
  poly {
    numVertices 3
    pos 1.60151 -14.2125 3.6987
    norm 2.20372 -3.99881 -8.89681
    pos 1.06416 -14.8908 3.66895
    norm 1.99546 -1.27894 -9.71506
    pos 0.634711 -13.1459 3.57186
    norm -5.46732 -6.82679 -4.84804
  }
  poly {
    numVertices 3
    pos -5.22285 4.31418 0.629247
    norm -2.87914 -0.928193 9.53148
    pos -5.61575 2.92003 0.423229
    norm -4.51975 -0.407768 8.91098
    pos -4.90397 4.0578 0.6206
    norm 0.628265 -1.13764 9.9152
  }
  poly {
    numVertices 3
    pos -5.61575 2.92003 0.423229
    norm -4.51975 -0.407768 8.91098
    pos -5.22285 4.31418 0.629247
    norm -2.87914 -0.928193 9.53148
    pos -5.82201 4.33718 0.299859
    norm -6.71597 -0.248429 7.40501
  }
  poly {
    numVertices 3
    pos -5.48614 -7.78082 2.73145
    norm -5.5324 1.47646 8.19833
    pos -5.96826 -7.88366 2.26762
    norm -7.97156 2.15979 5.63823
    pos -5.59577 -8.97342 2.66893
    norm -5.65369 0.982106 8.1897
  }
  poly {
    numVertices 3
    pos 1.06354 -18.3718 4.20986
    norm 5.13237 0.0353872 -8.5824
    pos 1.64301 -18.8177 4.6335
    norm 9.08146 -0.664226 -4.13349
    pos 1.11993 -19.3723 3.91824
    norm 4.53941 -5.72818 -6.82508
  }
  poly {
    numVertices 3
    pos -3.8676 -7.09659 -3.12022
    norm -4.37957 4.03862 -8.03174
    pos -3.53988 -5.77013 -2.67183
    norm -0.0518293 3.00429 -9.5379
    pos -2.99029 -5.97223 -2.84402
    norm -1.02474 5.49437 -8.29227
  }
  poly {
    numVertices 3
    pos 6.07394 -8.68997 5.81693
    norm 2.05825 5.0029 8.41039
    pos 8.83447 -11.2836 6.62974
    norm 1.89934 4.28739 8.83237
    pos 6.07528 -7.88127 5.21381
    norm 4.44789 6.86933 5.74706
  }
  poly {
    numVertices 3
    pos 8.83447 -11.2836 6.62974
    norm 1.89934 4.28739 8.83237
    pos 6.07394 -8.68997 5.81693
    norm 2.05825 5.0029 8.41039
    pos 6.52046 -10.2486 6.39982
    norm 2.01654 3.13315 9.27992
  }
  poly {
    numVertices 3
    pos 0.556453 -13.4349 1.53279
    norm -0.476012 -9.90126 -1.3185
    pos -1.36154 -13.2043 0.290559
    norm -2.03814 -9.71714 -1.19297
    pos -0.357896 -13.2263 0.0601258
    norm -0.146638 -9.46451 -3.22515
  }
  poly {
    numVertices 3
    pos 6.56405 -19.4502 1.91472
    norm 2.91858 -9.36404 1.94848
    pos 5.70841 -19.3793 2.33548
    norm -0.0113854 -9.29228 3.69503
    pos 4.91991 -19.4177 1.19631
    norm -0.823745 -9.91614 -0.995827
  }
  poly {
    numVertices 3
    pos 2.18633 -5.28753 3.29878
    norm 1.23168 8.57469 4.99576
    pos -0.728564 -4.24 2.03679
    norm 0.892517 8.28634 5.5263
    pos 2.7402 -6.02309 4.17817
    norm 0.669737 7.72844 6.31052
  }
  poly {
    numVertices 3
    pos 6.80751 -16.8562 2.14021
    norm 8.21816 4.10041 3.95582
    pos 6.11378 -15.1697 1.76768
    norm 7.82517 1.79214 5.96281
    pos 5.78605 -16.2582 2.61357
    norm 3.53917 3.53024 8.66092
  }
  poly {
    numVertices 3
    pos -2.47359 -3.30754 -0.0466174
    norm 6.20974 7.66014 -1.66176
    pos -3.48307 -2.10146 -0.874187
    norm 9.14499 3.8158 -1.34493
    pos -3.22086 -2.49356 0.0420619
    norm 7.05189 6.2924 3.26751
  }
  poly {
    numVertices 3
    pos -5.46645 -13.3129 1.79579
    norm -9.88526 -0.608969 -1.38229
    pos -5.434 -12.9887 2.25442
    norm -8.85879 -0.99168 4.53194
    pos -5.86864 -12.1499 1.57719
    norm -9.09612 -4.15328 -0.104921
  }
  poly {
    numVertices 3
    pos -3.3886 -12.9942 -4.04817
    norm -6.60869 1.92688 -7.25344
    pos -4.1396 -14.962 -4.7755
    norm -2.63305 3.36237 -9.04221
    pos -5.41685 -15.8033 -4.18426
    norm -8.46398 3.58729 -3.93604
  }
  poly {
    numVertices 3
    pos 5.26463 -8.91549 0.894319
    norm 5.9096 3.98031 -7.01669
    pos 4.6701 -9.61115 0.223488
    norm 2.92811 3.16245 -9.02359
    pos 4.00977 -9.22015 0.217357
    norm 5.27793 3.58148 -7.70171
  }
  poly {
    numVertices 3
    pos -2.75314 -10.7816 4.20168
    norm -6.37077 -2.93335 7.12804
    pos -3.94479 -9.40631 3.45026
    norm -3.72836 -0.432315 9.26889
    pos -3.5269 -10.4585 3.5318
    norm -3.71794 -3.3034 8.67552
  }
  poly {
    numVertices 3
    pos -3.94479 -9.40631 3.45026
    norm -3.72836 -0.432315 9.26889
    pos -2.75314 -10.7816 4.20168
    norm -6.37077 -2.93335 7.12804
    pos -3.63628 -9.06782 3.69973
    norm -5.65925 0.263433 8.24036
  }
  poly {
    numVertices 3
    pos 7.0604 -11.1212 2.16792
    norm 9.17089 1.40861 -3.72968
    pos 7.13286 -12.2295 2.65568
    norm 9.39183 -1.88633 -2.86973
    pos 6.92175 -12.6385 1.96847
    norm 9.27388 -1.3204 -3.50026
  }
  poly {
    numVertices 3
    pos -4.50336 -1.00268 0.358761
    norm 2.33573 0.553308 9.70764
    pos -4.65145 0.78429 0.426296
    norm 1.93167 -0.466882 9.80054
    pos -4.97643 -0.57256 0.3864
    norm -1.27418 0.0279944 9.91845
  }
  poly {
    numVertices 3
    pos -6.50666 -4.65668 -1.68746
    norm -9.48676 -2.6625 -1.70659
    pos -6.77095 -2.81837 -2.24608
    norm -9.63537 -1.14348 -2.41912
    pos -6.23098 -4.79782 -2.29041
    norm -8.30328 -2.83787 -4.79604
  }
  poly {
    numVertices 3
    pos 3.92788 -7.08466 5.09628
    norm 1.54171 6.71857 7.24458
    pos 5.00063 -8.51581 5.97394
    norm 2.33725 5.28274 8.16272
    pos 5.03743 -7.43863 5.19513
    norm 1.90716 6.02343 7.7512
  }
  poly {
    numVertices 3
    pos 2.41752 -14.6786 5.48116
    norm 7.30528 -6.28082 2.68034
    pos 2.76759 -13.5402 6.69144
    norm 2.18427 -5.17082 8.27597
    pos 1.67285 -14.3322 6.3714
    norm 4.82088 -3.01384 8.22653
  }
  poly {
    numVertices 3
    pos 2.78563 -10.6028 -1.07071
    norm 6.83545 -2.12318 -6.98346
    pos 3.03443 -11.4539 -0.388693
    norm 5.80919 -3.96849 -7.10665
    pos 2.05608 -11.3913 -1.31843
    norm 5.36314 -4.74617 -6.9793
  }
  poly {
    numVertices 3
    pos 4.95033 -12.8112 6.32751
    norm 1.34532 -6.21054 7.72135
    pos 3.25728 -14.1647 5.3891
    norm 5.63874 -8.12302 -1.49034
    pos 3.96296 -13.845 5.31131
    norm 1.68375 -9.50935 2.59563
  }
  poly {
    numVertices 3
    pos 3.91675 -7.13478 1.77387
    norm 5.88737 6.63754 -4.61323
    pos 2.5073 -6.01438 1.25491
    norm 5.8757 7.35017 -3.38395
    pos 4.42427 -6.98383 2.47132
    norm 6.16395 7.06011 -3.48719
  }
  poly {
    numVertices 3
    pos 2.5073 -6.01438 1.25491
    norm 5.8757 7.35017 -3.38395
    pos 3.91675 -7.13478 1.77387
    norm 5.88737 6.63754 -4.61323
    pos 2.66949 -6.38225 0.685115
    norm 6.27073 6.46775 -4.34123
  }
  poly {
    numVertices 3
    pos -6.31301 -10.9601 0.0356826
    norm -8.95624 -4.11627 -1.68584
    pos -5.11027 -13.1041 0.859035
    norm -6.99962 -3.89051 -5.98909
    pos -5.86864 -12.1499 1.57719
    norm -9.09612 -4.15328 -0.104921
  }
  poly {
    numVertices 3
    pos -5.50148 -5.66874 1.22353
    norm -7.83408 2.50715 5.68695
    pos -4.95038 -4.76278 1.42374
    norm -5.38059 3.36811 7.7269
    pos -5.91146 -4.64372 0.615297
    norm -7.7944 1.00732 6.18325
  }
  poly {
    numVertices 3
    pos 4.60674 -19.3379 2.15178
    norm -4.37939 -7.62477 4.76276
    pos 5.70841 -19.3793 2.33548
    norm -0.0113854 -9.29228 3.69503
    pos 5.44222 -18.8925 2.97636
    norm -1.541 -4.79986 8.63635
  }
  poly {
    numVertices 3
    pos -4.33039 -6.57474 -2.704
    norm -4.34739 1.7048 -8.84273
    pos -3.53988 -5.77013 -2.67183
    norm -0.0518293 3.00429 -9.5379
    pos -3.8676 -7.09659 -3.12022
    norm -4.37957 4.03862 -8.03174
  }
  poly {
    numVertices 3
    pos -4.73778 -18.4412 0.38361
    norm 1.70897 3.82109 -9.08178
    pos -5.64277 -18.6945 0.0114914
    norm -5.64334 6.91606 -4.50787
    pos -5.05231 -18.2437 0.666347
    norm -7.018 5.13608 -4.93644
  }
  poly {
    numVertices 3
    pos -2.04646 -4.13963 -1.17775
    norm 4.5813 6.94476 -5.54814
    pos -1.10285 -3.98427 0.285084
    norm 5.66868 7.07042 -4.22792
    pos -0.89717 -4.52626 -0.804798
    norm 4.9225 7.68979 -4.07876
  }
  poly {
    numVertices 3
    pos -3.82645 -16.9752 2.05766
    norm 9.18596 -0.487118 3.92185
    pos -3.6129 -16.5015 1.63802
    norm 9.68138 -1.39526 2.07945
    pos -3.65413 -15.3926 2.68235
    norm 6.94342 -2.63732 6.69577
  }
  poly {
    numVertices 3
    pos -5.74712 -3.53181 0.511146
    norm -5.86776 2.13709 7.8104
    pos -4.95038 -4.76278 1.42374
    norm -5.38059 3.36811 7.7269
    pos -4.15879 -4.103 1.45667
    norm -3.14014 5.28119 7.88978
  }
  poly {
    numVertices 3
    pos -4.95038 -4.76278 1.42374
    norm -5.38059 3.36811 7.7269
    pos -3.49371 -4.82625 2.27745
    norm -3.80578 5.76134 7.23347
    pos -4.15879 -4.103 1.45667
    norm -3.14014 5.28119 7.88978
  }
  poly {
    numVertices 3
    pos -0.347828 -17.881 5.04525
    norm -8.72187 4.83423 -0.747798
    pos -1.13676 -19.0025 4.13924
    norm -9.76427 1.88834 -1.04558
    pos -1.27763 -19.0498 4.83098
    norm -9.98953 -0.388803 0.240663
  }
  poly {
    numVertices 3
    pos -6.09022 -9.17202 2.34186
    norm -7.40475 0.743075 6.67963
    pos -6.74061 -9.87395 1.32155
    norm -9.66247 0.589177 2.50792
    pos -6.41234 -10.2989 1.92819
    norm -8.64858 -1.11023 4.89586
  }
  poly {
    numVertices 3
    pos -5.77866 -16.6824 1.20658
    norm -8.83549 -0.652013 -4.63778
    pos -5.6559 -17.7624 1.87307
    norm -9.8892 0.938324 -1.15032
    pos -5.91348 -16.3229 1.89824
    norm -9.84757 0.11928 1.73527
  }
  poly {
    numVertices 3
    pos -6.09022 -9.17202 2.34186
    norm -7.40475 0.743075 6.67963
    pos -5.96826 -7.88366 2.26762
    norm -7.97156 2.15979 5.63823
    pos -6.74061 -9.87395 1.32155
    norm -9.66247 0.589177 2.50792
  }
  poly {
    numVertices 3
    pos -5.9866 -6.63685 -0.256173
    norm -9.85918 -0.491795 -1.59838
    pos -5.97654 -6.55009 -1.04053
    norm -9.68913 -2.09191 -1.32087
    pos -5.8034 -7.88741 -1.12425
    norm -9.08584 -0.153234 -4.17421
  }
  poly {
    numVertices 3
    pos 3.74017 -14.8183 1.58109
    norm -8.79283 -3.26142 3.47121
    pos 3.15246 -14.0169 1.1708
    norm -8.84922 -4.5932 -0.770525
    pos 3.71647 -15.4835 0.993134
    norm -9.11882 -3.74517 1.67953
  }
  poly {
    numVertices 3
    pos 3.15246 -14.0169 1.1708
    norm -8.84922 -4.5932 -0.770525
    pos 3.74017 -14.8183 1.58109
    norm -8.79283 -3.26142 3.47121
    pos 3.38326 -14.2725 1.69782
    norm -7.40815 -6.63655 1.03705
  }
  poly {
    numVertices 3
    pos 5.36113 -14.5588 -0.166576
    norm 6.22493 0.314549 -7.81993
    pos 5.60475 -15.3098 0.0731382
    norm 6.35995 0.365823 -7.70826
    pos 4.97961 -15.3822 -0.338328
    norm 2.2475 -1.2381 -9.66518
  }
  poly {
    numVertices 3
    pos -1.12156 -13.2487 1.61868
    norm -1.99203 -9.76177 0.859989
    pos -1.36154 -13.2043 0.290559
    norm -2.03814 -9.71714 -1.19297
    pos 0.556453 -13.4349 1.53279
    norm -0.476012 -9.90126 -1.3185
  }
  poly {
    numVertices 3
    pos -5.019 -16.8262 0.536212
    norm -4.15483 0.123568 -9.09517
    pos -5.05231 -18.2437 0.666347
    norm -7.018 5.13608 -4.93644
    pos -5.37705 -17.985 1.3533
    norm -8.60207 2.27413 -4.5643
  }
  poly {
    numVertices 3
    pos -5.46645 -13.3129 1.79579
    norm -9.88526 -0.608969 -1.38229
    pos -5.77866 -16.6824 1.20658
    norm -8.83549 -0.652013 -4.63778
    pos -5.91348 -16.3229 1.89824
    norm -9.84757 0.11928 1.73527
  }
  poly {
    numVertices 3
    pos -4.90629 -17.784 -5.39786
    norm -4.52652 3.6587 -8.1317
    pos -4.29227 -17.5202 -5.39181
    norm 1.52838 3.381 -9.28617
    pos -5.39593 -19.2514 -6.0843
    norm -4.47909 -1.31291 -8.84386
  }
  poly {
    numVertices 3
    pos 0.0206174 -13.8208 5.93032
    norm -7.14273 0.0755825 6.99827
    pos 0.702972 -14.1291 6.34669
    norm -4.11256 -0.320562 9.10956
    pos 0.740866 -12.1068 6.69971
    norm -4.56911 0.0836025 8.89473
  }
  poly {
    numVertices 3
    pos -4.2911 4.37588 0.509789
    norm 4.66363 -0.868751 8.80317
    pos -3.99182 4.45988 0.161384
    norm 9.51856 -0.555067 3.01477
    pos -4.14416 6.34258 0.769167
    norm 5.7389 -1.36193 8.07528
  }
  poly {
    numVertices 3
    pos -6.51485 0.20576 -2.34626
    norm -3.54166 2.09553 -9.11402
    pos -5.35082 -1.32634 -2.74761
    norm 2.54944 1.17083 -9.59841
    pos -6.76337 -1.47748 -2.52411
    norm -6.89324 0.413042 -7.23276
  }
  poly {
    numVertices 3
    pos -3.52944 -3.05024 0.736041
    norm 1.87693 6.12494 7.67869
    pos -4.47609 -2.48377 0.540099
    norm 0.315882 2.7505 9.60911
    pos -3.7561 -3.66817 1.18189
    norm -1.12644 5.95345 7.95535
  }
  poly {
    numVertices 3
    pos -1.39263 -12.5652 -3.23511
    norm 9.35587 -1.879 -2.9895
    pos -0.677277 -11.281 -2.11471
    norm 5.59542 -5.69032 -6.0259
    pos -1.62737 -12.7661 -2.13916
    norm 8.26146 -4.79473 2.95955
  }
  poly {
    numVertices 3
    pos -4.30081 10.6338 1.62579
    norm 2.44105 -1.9551 9.49835
    pos -4.87718 9.30128 1.3301
    norm -2.26571 -1.63239 9.60218
    pos -4.31311 8.69838 1.23407
    norm 4.55417 -1.47168 8.78031
  }
  poly {
    numVertices 3
    pos 4.49541 -16.1976 2.44063
    norm -6.32529 -0.408662 7.73459
    pos 4.40375 -16.8088 2.12267
    norm -8.71396 -2.20582 4.38192
    pos 4.96878 -17.7485 2.58786
    norm -8.08089 0.867681 5.82636
  }
  poly {
    numVertices 3
    pos -4.02677 9.63628 -0.007707
    norm 8.11428 1.02779 -5.75344
    pos -4.24376 10.6749 -0.148825
    norm 6.01758 1.29344 -7.88135
    pos -3.8764 10.6555 0.360523
    norm 9.34649 0.484279 -3.52259
  }
  poly {
    numVertices 3
    pos 5.88352 -14.1734 4.86874
    norm -0.798628 -9.82047 1.70898
    pos 4.62076 -14.2961 3.4172
    norm -1.57245 -9.64242 2.13335
    pos 6.13391 -14.1456 4.45689
    norm 4.24323 -8.88238 1.76021
  }
  poly {
    numVertices 3
    pos 4.62076 -14.2961 3.4172
    norm -1.57245 -9.64242 2.13335
    pos 5.88352 -14.1734 4.86874
    norm -0.798628 -9.82047 1.70898
    pos 4.73871 -14.2177 4.37438
    norm -2.11038 -9.6019 1.83024
  }
  poly {
    numVertices 3
    pos -3.52944 -3.05024 0.736041
    norm 1.87693 6.12494 7.67869
    pos -2.13791 -3.39392 0.722582
    norm 2.8227 9.03571 3.2231
    pos -3.22086 -2.49356 0.0420619
    norm 7.05189 6.2924 3.26751
  }
  poly {
    numVertices 3
    pos 9.2347 -12.0919 5.1644
    norm 7.0686 3.64175 -6.06403
    pos 8.92635 -10.9024 5.56255
    norm 7.64951 5.54286 -3.2805
    pos 10.0073 -12.2825 5.97696
    norm 7.2667 5.21924 -4.46705
  }
  poly {
    numVertices 3
    pos 8.92635 -10.9024 5.56255
    norm 7.64951 5.54286 -3.2805
    pos 9.2347 -12.0919 5.1644
    norm 7.0686 3.64175 -6.06403
    pos 7.81209 -10.9002 3.95792
    norm 7.99863 2.98298 -5.20805
  }
  poly {
    numVertices 3
    pos -6.38285 1.25007 -0.232028
    norm -7.90629 0.119105 6.12178
    pos -5.87396 1.74018 0.199279
    norm -5.63666 -0.143113 8.25879
    pos -6.24211 2.74096 -0.200528
    norm -8.29056 0.385716 5.57834
  }
  poly {
    numVertices 3
    pos -1.10285 -3.98427 0.285084
    norm 5.66868 7.07042 -4.22792
    pos 4.16085 -6.57047 3.02101
    norm 6.06509 7.2655 -3.22909
    pos -0.89717 -4.52626 -0.804798
    norm 4.9225 7.68979 -4.07876
  }
  poly {
    numVertices 3
    pos 0.606488 -16.9722 6.3572
    norm -3.58062 -0.587752 9.31846
    pos -0.0845011 -16.56 5.88587
    norm -7.63868 -0.0609581 6.45344
    pos 0.564647 -18.3093 6.31246
    norm -3.23203 1.75189 9.29973
  }
  poly {
    numVertices 3
    pos 5.03338 -16.9736 0.343904
    norm -2.88769 -4.37162 -8.51764
    pos 5.88241 -17.9389 0.617416
    norm -2.21668 2.20694 -9.4982
    pos 5.11581 -17.9895 1.08666
    norm -7.47975 -1.60475 -6.44036
  }
  poly {
    numVertices 3
    pos -6.22047 -9.19181 -0.776008
    norm -9.05972 0.412972 -4.2132
    pos -5.9866 -6.63685 -0.256173
    norm -9.85918 -0.491795 -1.59838
    pos -5.8034 -7.88741 -1.12425
    norm -9.08584 -0.153234 -4.17421
  }
  poly {
    numVertices 3
    pos -1.72534 -12.5177 -1.18808
    norm 0.78716 -9.81368 -1.75274
    pos 0.37187 -12.7397 -0.903383
    norm 0.748865 -7.68155 -6.35869
    pos -0.357896 -13.2263 0.0601258
    norm -0.146638 -9.46451 -3.22515
  }
  poly {
    numVertices 3
    pos -4.31311 8.69838 1.23407
    norm 4.55417 -1.47168 8.78031
    pos -4.75588 6.33448 0.975947
    norm -0.537424 -1.34268 9.89487
    pos -4.14416 6.34258 0.769167
    norm 5.7389 -1.36193 8.07528
  }
  poly {
    numVertices 3
    pos -4.9726 -18.8315 -3.15757
    norm -0.342929 -2.75362 9.60729
    pos -5.40374 -17.5601 -3.37764
    norm -7.27576 1.06843 6.77657
    pos -5.09561 -18.4673 -3.07797
    norm -0.377619 0.392749 9.98515
  }
  poly {
    numVertices 3
    pos 1.11993 -19.3723 3.91824
    norm 4.53941 -5.72818 -6.82508
    pos 1.64301 -18.8177 4.6335
    norm 9.08146 -0.664226 -4.13349
    pos 1.58199 -19.4485 5.60605
    norm 6.96701 -7.1643 -0.365442
  }
  poly {
    numVertices 3
    pos 2.66949 -6.38225 0.685115
    norm 6.27073 6.46775 -4.34123
    pos 1.4807 -6.14113 -0.368454
    norm 5.5266 6.55219 -5.15029
    pos -0.89717 -4.52626 -0.804798
    norm 4.9225 7.68979 -4.07876
  }
  poly {
    numVertices 3
    pos 1.11266 -17.1594 6.36618
    norm 3.1409 -0.394874 9.48572
    pos 1.34363 -19.5344 6.11335
    norm 5.19898 -5.25268 6.73646
    pos 1.77574 -18.4491 5.91374
    norm 9.02425 -1.17894 4.14403
  }
  poly {
    numVertices 3
    pos 6.02434 -19.4702 -0.272587
    norm 0.540327 -4.5337 -8.89683
    pos 5.87754 -18.655 0.110617
    norm 0.105673 5.38323 -8.42673
    pos 6.33275 -18.7768 0.0730906
    norm 3.32945 4.45133 -8.31267
  }
  poly {
    numVertices 3
    pos -5.68397 -18.1957 -4.65381
    norm -7.43067 4.28013 -5.14447
    pos -4.79786 -16.1441 -4.88718
    norm -6.05979 2.91969 -7.39962
    pos -4.90629 -17.784 -5.39786
    norm -4.52652 3.6587 -8.1317
  }
  poly {
    numVertices 3
    pos 4.65564 -13.6955 -0.393571
    norm 0.1017 -0.68103 -9.97626
    pos 4.76982 -12.6031 -0.487966
    norm 1.88983 -0.0460285 -9.8197
    pos 5.07755 -13.7669 -0.265191
    norm 4.77252 -0.149132 -8.7864
  }
  poly {
    numVertices 3
    pos 4.76982 -12.6031 -0.487966
    norm 1.88983 -0.0460285 -9.8197
    pos 4.65564 -13.6955 -0.393571
    norm 0.1017 -0.68103 -9.97626
    pos 4.23647 -12.5242 -0.434443
    norm -2.77891 -0.109656 -9.6055
  }
  poly {
    numVertices 3
    pos -6.70153 0.32487 -0.818477
    norm -9.77228 0.453305 2.0729
    pos -6.5942 1.11603 -1.93166
    norm -9.33229 1.76466 -3.12959
    pos -6.76337 -1.47748 -2.52411
    norm -6.89324 0.413042 -7.23276
  }
  poly {
    numVertices 3
    pos -1.79687 -14.7058 -2.95384
    norm 7.30355 -5.18284 4.4493
    pos -2.77505 -15.7261 -3.26141
    norm 6.56821 -6.21017 4.27696
    pos -2.43726 -15.3994 -3.93026
    norm 8.14933 -4.94443 -3.02343
  }
  poly {
    numVertices 3
    pos 6.10929 -14.7403 0.973346
    norm 9.68675 0.815143 -2.34573
    pos 6.24481 -13.7521 0.908654
    norm 8.93794 -0.54909 -4.45105
    pos 6.28983 -14.3377 1.44391
    norm 9.67517 -2.50103 0.368631
  }
  poly {
    numVertices 3
    pos 6.56405 -19.4502 1.91472
    norm 2.91858 -9.36404 1.94848
    pos 7.37924 -18.6891 2.15138
    norm 9.23978 -1.9749 3.27511
    pos 6.74407 -19.04 2.64974
    norm 3.60163 -7.088 6.06535
  }
  poly {
    numVertices 3
    pos 9.21038 -13.7496 7.20827
    norm -1.84563 0.888925 9.78792
    pos 8.76807 -14.6156 7.03628
    norm -3.17875 -2.74516 9.07522
    pos 9.45757 -14.5149 7.30453
    norm -3.4148 -1.1364 9.32994
  }
  poly {
    numVertices 3
    pos -3.971 0.991199 -1.18359
    norm 9.0112 1.12036 -4.18846
    pos -3.80574 -0.0732203 -0.647841
    norm 9.93586 1.06025 0.39308
    pos -3.8842 -0.17551 -1.42712
    norm 8.45929 1.22575 -5.19018
  }
  poly {
    numVertices 3
    pos -3.93253 -12.4792 -1.74255
    norm -7.42124 -4.34416 5.10426
    pos -3.28285 -12.4916 -1.3556
    norm -3.19206 -6.31825 7.06332
    pos -4.51294 -11.9106 -1.79198
    norm -6.63337 -7.44736 -0.731678
  }
  poly {
    numVertices 3
    pos -3.28285 -12.4916 -1.3556
    norm -3.19206 -6.31825 7.06332
    pos -3.93253 -12.4792 -1.74255
    norm -7.42124 -4.34416 5.10426
    pos -3.46427 -13.6093 -1.77623
    norm -2.8395 -2.96263 9.11921
  }
  poly {
    numVertices 3
    pos 1.06354 -18.3718 4.20986
    norm 5.13237 0.0353872 -8.5824
    pos 1.11993 -19.3723 3.91824
    norm 4.53941 -5.72818 -6.82508
    pos 0.615579 -18.4413 3.97877
    norm 1.41317 2.72626 -9.51685
  }
  poly {
    numVertices 3
    pos -4.73778 -18.4412 0.38361
    norm 1.70897 3.82109 -9.08178
    pos -5.05231 -18.2437 0.666347
    norm -7.018 5.13608 -4.93644
    pos -5.019 -16.8262 0.536212
    norm -4.15483 0.123568 -9.09517
  }
  poly {
    numVertices 3
    pos 14.72 -15.1718 13.5718
    norm 1.0773 -8.42496 5.27821
    pos 14.6149 -14.0403 13.3443
    norm 4.62677 8.73814 1.49593
    pos 15.0179 -14.4384 14.0639
    norm 9.34882 2.94242 1.98538
  }
  poly {
    numVertices 3
    pos 14.6149 -14.0403 13.3443
    norm 4.62677 8.73814 1.49593
    pos 14.72 -15.1718 13.5718
    norm 1.0773 -8.42496 5.27821
    pos 15.1075 -14.5324 13.4979
    norm 5.98485 2.23643 7.69285
  }
  poly {
    numVertices 3
    pos 2.7402 -6.02309 4.17817
    norm 0.669737 7.72844 6.31052
    pos 3.92788 -7.08466 5.09628
    norm 1.54171 6.71857 7.24458
    pos 4.28666 -6.41083 4.51174
    norm 2.28514 7.5741 6.11647
  }
  poly {
    numVertices 3
    pos -5.8034 -7.88741 -1.12425
    norm -9.08584 -0.153234 -4.17421
    pos -5.67109 -9.58226 -1.54096
    norm -8.73711 0.174782 -4.86131
    pos -6.22047 -9.19181 -0.776008
    norm -9.05972 0.412972 -4.2132
  }
  poly {
    numVertices 3
    pos -2.05895 -13.9549 -4.22049
    norm 5.54484 -0.426677 -8.311
    pos -2.43726 -15.3994 -3.93026
    norm 8.14933 -4.94443 -3.02343
    pos -2.73953 -15.3335 -4.33695
    norm 6.40496 -1.51863 -7.52797
  }
  poly {
    numVertices 3
    pos 7.37924 -18.6891 2.15138
    norm 9.23978 -1.9749 3.27511
    pos 6.89929 -16.9192 1.47205
    norm 9.5297 2.97301 -0.588242
    pos 6.80751 -16.8562 2.14021
    norm 8.21816 4.10041 3.95582
  }
  poly {
    numVertices 3
    pos 6.89929 -16.9192 1.47205
    norm 9.5297 2.97301 -0.588242
    pos 7.37924 -18.6891 2.15138
    norm 9.23978 -1.9749 3.27511
    pos 7.42725 -18.6062 1.34154
    norm 9.37498 0.705162 -3.40773
  }
  poly {
    numVertices 3
    pos -5.68397 -18.1957 -4.65381
    norm -7.43067 4.28013 -5.14447
    pos -6.4463 -18.8541 -4.66278
    norm -7.95525 2.36024 -5.58062
    pos -5.83976 -18.1071 -4.05654
    norm -8.73339 4.82933 0.636729
  }
  poly {
    numVertices 3
    pos -4.44626 -9.80535 -3.71284
    norm -6.99382 -0.865093 -7.09493
    pos -4.70895 -8.53872 -3.14765
    norm -7.4631 2.37566 -6.21758
    pos -4.16097 -8.86538 -3.81439
    norm -3.83121 1.62248 -9.09337
  }
  poly {
    numVertices 3
    pos 2.91566 -9.60734 -1.01569
    norm 6.774 0.522784 -7.33754
    pos 2.58401 -8.83772 -1.23766
    norm 6.57392 1.7515 -7.3291
    pos 3.15852 -8.38935 -0.523185
    norm 7.1048 2.70633 -6.49596
  }
  poly {
    numVertices 3
    pos 10.6095 -12.6927 6.73842
    norm 6.83096 7.30238 0.115532
    pos 8.92635 -10.9024 5.56255
    norm 7.64951 5.54286 -3.2805
    pos 9.22152 -11.3098 6.48667
    norm 3.88048 6.24039 6.78228
  }
  poly {
    numVertices 3
    pos -4.45782 -19.3487 -3.93444
    norm 3.18647 -9.29505 1.85698
    pos -6.20664 -19.3608 -4.92499
    norm -4.21599 -8.80304 -2.17529
    pos -4.49025 -19.4335 -5.45155
    norm 3.35634 -9.41696 0.236478
  }
  poly {
    numVertices 3
    pos -4.45782 -19.3487 -3.93444
    norm 3.18647 -9.29505 1.85698
    pos -3.8027 -18.8281 -4.01741
    norm 7.55102 -5.20987 3.97988
    pos -4.42753 -18.8147 -3.34335
    norm 4.87093 -4.5342 7.46425
  }
  poly {
    numVertices 3
    pos -6.37764 -4.77125 -0.192311
    norm -9.64338 -0.808735 2.52017
    pos -5.91146 -4.64372 0.615297
    norm -7.7944 1.00732 6.18325
    pos -5.74712 -3.53181 0.511146
    norm -5.86776 2.13709 7.8104
  }
  poly {
    numVertices 3
    pos 4.51454 -17.2297 1.45603
    norm -9.2191 -3.76161 -0.926524
    pos 5.11581 -17.9895 1.08666
    norm -7.47975 -1.60475 -6.44036
    pos 4.78829 -18.1591 1.58407
    norm -8.49068 3.96241 -3.49395
  }
  poly {
    numVertices 3
    pos 5.11581 -17.9895 1.08666
    norm -7.47975 -1.60475 -6.44036
    pos 4.51454 -17.2297 1.45603
    norm -9.2191 -3.76161 -0.926524
    pos 3.84096 -15.6113 0.261495
    norm -7.84306 -3.62031 -5.03783
  }
  poly {
    numVertices 3
    pos -3.89908 6.96278 -0.342101
    norm 9.45917 0.280449 -3.23195
    pos -3.88598 4.77008 -0.422515
    norm 9.42815 0.236647 -3.32476
    pos -4.21069 6.00638 -0.841693
    norm 6.98346 0.861646 -7.10555
  }
  poly {
    numVertices 3
    pos -3.88598 4.77008 -0.422515
    norm 9.42815 0.236647 -3.32476
    pos -3.89908 6.96278 -0.342101
    norm 9.45917 0.280449 -3.23195
    pos -3.99182 4.45988 0.161384
    norm 9.51856 -0.555067 3.01477
  }
  poly {
    numVertices 3
    pos -4.73778 -18.4412 0.38361
    norm 1.70897 3.82109 -9.08178
    pos -3.83874 -18.0208 0.935175
    norm 6.8127 0.416185 -7.30849
    pos -4.17299 -19.0353 0.497947
    norm 6.1323 -0.272282 -7.89436
  }
  poly {
    numVertices 3
    pos -5.21528 -12.174 -0.517622
    norm -3.31427 -8.18459 -4.69341
    pos -4.31979 -12.7319 0.377351
    norm -1.72291 -7.06117 -6.86815
    pos -5.72269 -12.0342 0.203904
    norm -6.6302 -6.71224 -3.31457
  }
  poly {
    numVertices 3
    pos -5.12193 -13.6635 1.05269
    norm -7.38051 0.233406 -6.74342
    pos -5.77866 -16.6824 1.20658
    norm -8.83549 -0.652013 -4.63778
    pos -5.46645 -13.3129 1.79579
    norm -9.88526 -0.608969 -1.38229
  }
  poly {
    numVertices 3
    pos 13.8867 -14.3911 11.2997
    norm -7.42869 6.68461 0.361266
    pos 13.833 -14.9394 11.7083
    norm -9.77509 0.805147 1.94915
    pos 14.4033 -14.0152 11.9425
    norm -0.259256 9.99522 -0.168179
  }
  poly {
    numVertices 3
    pos -3.91682 -3.02274 -2.01956
    norm 5.91153 2.82406 -7.55504
    pos -5.35082 -1.32634 -2.74761
    norm 2.54944 1.17083 -9.59841
    pos -4.3253 -1.34326 -2.01213
    norm 6.76499 1.32773 -7.24377
  }
  poly {
    numVertices 3
    pos -5.35082 -1.32634 -2.74761
    norm 2.54944 1.17083 -9.59841
    pos -3.91682 -3.02274 -2.01956
    norm 5.91153 2.82406 -7.55504
    pos -4.72565 -3.46355 -2.63385
    norm 3.78939 1.23274 -9.17174
  }
  poly {
    numVertices 3
    pos 14.3834 -15.5749 12.1345
    norm -2.79924 -9.01807 3.2922
    pos 15.0512 -15.3047 12.6233
    norm 6.92159 -6.49921 3.13877
    pos 14.5757 -15.3658 12.6944
    norm -0.132056 -8.98233 4.3932
  }
  poly {
    numVertices 3
    pos -3.7529 -1.36371 -0.192882
    norm 9.0451 2.36578 3.54812
    pos -4.03784 0.88588 0.0193272
    norm 8.05782 0.248429 5.9169
    pos -4.10131 -1.57873 0.24944
    norm 4.88343 2.51075 8.35752
  }
  poly {
    numVertices 3
    pos -3.41935 -9.16366 -3.98414
    norm -0.0921583 0.99474 -9.94997
    pos -3.21034 -7.7722 -3.72271
    norm 0.0363636 2.49318 -9.68415
    pos -2.3899 -8.41909 -3.7683
    norm 2.526 1.41984 -9.57097
  }
  poly {
    numVertices 3
    pos -4.70895 -8.53872 -3.14765
    norm -7.4631 2.37566 -6.21758
    pos -4.44626 -9.80535 -3.71284
    norm -6.99382 -0.865093 -7.09493
    pos -4.99445 -9.69996 -2.84098
    norm -8.56445 -1.05053 -5.05437
  }
  poly {
    numVertices 3
    pos 6.24673 -8.40136 3.8262
    norm 7.35641 6.3507 -2.35625
    pos 4.90674 -7.51158 2.10966
    norm 5.82643 7.12747 -3.90537
    pos 4.42427 -6.98383 2.47132
    norm 6.16395 7.06011 -3.48719
  }
  poly {
    numVertices 3
    pos -4.45284 -11.0302 -2.99362
    norm -8.65514 -2.57609 -4.29561
    pos -4.163 -12.6847 -2.66679
    norm -9.92912 -1.18267 0.117671
    pos -4.47069 -11.6215 -2.42129
    norm -8.93755 -3.98278 -2.0634
  }
  poly {
    numVertices 3
    pos -4.163 -12.6847 -2.66679
    norm -9.92912 -1.18267 0.117671
    pos -4.45284 -11.0302 -2.99362
    norm -8.65514 -2.57609 -4.29561
    pos -4.01789 -12.4093 -3.13516
    norm -9.02195 -0.298631 -4.30294
  }
  poly {
    numVertices 3
    pos -0.132075 -18.0928 4.40838
    norm -6.58332 4.50687 -6.02893
    pos -0.0581061 -16.5159 4.3851
    norm -8.90835 -0.622618 -4.50041
    pos 0.508093 -17.5007 3.96244
    norm -2.8014 -0.75583 -9.56979
  }
  poly {
    numVertices 3
    pos 2.7402 -6.02309 4.17817
    norm 0.669737 7.72844 6.31052
    pos -1.08883 -4.78023 2.76778
    norm 0.0327969 7.6055 6.49271
    pos 1.13881 -6.01673 4.42871
    norm -0.628232 6.48513 7.58607
  }
  poly {
    numVertices 3
    pos 2.50165 -6.55031 4.875
    norm -0.209473 6.89509 7.23973
    pos 3.92788 -7.08466 5.09628
    norm 1.54171 6.71857 7.24458
    pos 2.7402 -6.02309 4.17817
    norm 0.669737 7.72844 6.31052
  }
  poly {
    numVertices 3
    pos -4.90629 -17.784 -5.39786
    norm -4.52652 3.6587 -8.1317
    pos -5.39593 -19.2514 -6.0843
    norm -4.47909 -1.31291 -8.84386
    pos -5.68397 -18.1957 -4.65381
    norm -7.43067 4.28013 -5.14447
  }
  poly {
    numVertices 3
    pos 5.47606 -15.4799 2.53888
    norm 4.71801 -0.115056 8.8163
    pos 4.98477 -15.6632 2.63136
    norm -1.57615 0.274975 9.87118
    pos 5.78605 -16.2582 2.61357
    norm 3.53917 3.53024 8.66092
  }
  poly {
    numVertices 3
    pos 5.40388 -17.1397 2.85426
    norm -3.0169 1.39344 9.43168
    pos 6.14551 -17.6639 3.10086
    norm 0.518151 1.5958 9.85824
    pos 5.78605 -16.2582 2.61357
    norm 3.53917 3.53024 8.66092
  }
  poly {
    numVertices 3
    pos -6.38869 3.01368 -0.701171
    norm -9.91581 1.2354 0.387893
    pos -6.24211 2.74096 -0.200528
    norm -8.29056 0.385716 5.57834
    pos -5.70448 8.23418 0.42774
    norm -9.06602 0.144092 4.21741
  }
  poly {
    numVertices 3
    pos 2.08434 -9.85147 -1.75287
    norm 6.14109 -0.0169408 -7.89219
    pos 1.40846 -9.1765 -2.19909
    norm 4.33043 0.853549 -8.97323
    pos 1.80498 -8.38954 -1.75554
    norm 5.61535 2.10119 -8.0033
  }
  poly {
    numVertices 3
    pos -4.53457 -9.51144 3.39841
    norm -3.41061 0.998939 9.34718
    pos -5.59577 -8.97342 2.66893
    norm -5.65369 0.982106 8.1897
    pos -5.59569 -9.69985 2.82243
    norm -5.75759 0.908653 8.12555
  }
  poly {
    numVertices 3
    pos -6.06486 -6.60961 0.267663
    norm -9.84512 1.30847 1.16685
    pos -5.50148 -5.66874 1.22353
    norm -7.83408 2.50715 5.68695
    pos -5.91146 -4.64372 0.615297
    norm -7.7944 1.00732 6.18325
  }
  poly {
    numVertices 3
    pos -3.63628 -9.06782 3.69973
    norm -5.65925 0.263433 8.24036
    pos -2.97108 -8.36129 4.09266
    norm -5.79013 2.12898 7.87031
    pos -3.42154 -7.48252 3.49583
    norm -3.93015 1.20342 9.11624
  }
  poly {
    numVertices 3
    pos -2.98619 -14.333 1.92776
    norm 9.58291 -1.88899 -2.14466
    pos -3.6129 -16.5015 1.63802
    norm 9.68138 -1.39526 2.07945
    pos -3.47869 -15.0434 1.36022
    norm 8.26886 -0.899591 -5.55128
  }
  poly {
    numVertices 3
    pos 7.83166 -11.7518 3.74877
    norm 7.28161 0.351777 -6.84503
    pos 7.34488 -13.0766 3.5858
    norm 5.73292 -3.69174 -7.31469
    pos 7.13286 -12.2295 2.65568
    norm 9.39183 -1.88633 -2.86973
  }
  poly {
    numVertices 3
    pos 4.16085 -6.57047 3.02101
    norm 6.06509 7.2655 -3.22909
    pos 5.47074 -7.06492 4.41854
    norm 6.9719 7.1489 -0.534642
    pos 6.24673 -8.40136 3.8262
    norm 7.35641 6.3507 -2.35625
  }
  poly {
    numVertices 3
    pos -2.99029 -5.97223 -2.84402
    norm -1.02474 5.49437 -8.29227
    pos -2.26993 -4.73804 -1.96651
    norm 3.27523 6.54553 -6.81387
    pos -1.93983 -4.95494 -1.93562
    norm 2.37489 6.51852 -7.202
  }
  poly {
    numVertices 3
    pos -2.46167 -12.6482 -0.746944
    norm -2.10803 -9.73919 -0.83927
    pos -3.28285 -12.4916 -1.3556
    norm -3.19206 -6.31825 7.06332
    pos -2.67378 -12.6316 -1.64741
    norm 1.19233 -7.34456 6.68101
  }
  poly {
    numVertices 3
    pos 2.5949 -13.6132 1.74831
    norm -3.32224 -9.38673 -0.923094
    pos 0.556453 -13.4349 1.53279
    norm -0.476012 -9.90126 -1.3185
    pos 2.28293 -13.2826 1.18152
    norm -0.148885 -8.9958 -4.36503
  }
  poly {
    numVertices 3
    pos -5.91146 -4.64372 0.615297
    norm -7.7944 1.00732 6.18325
    pos -4.95038 -4.76278 1.42374
    norm -5.38059 3.36811 7.7269
    pos -5.74712 -3.53181 0.511146
    norm -5.86776 2.13709 7.8104
  }
  poly {
    numVertices 3
    pos -3.99182 4.45988 0.161384
    norm 9.51856 -0.555067 3.01477
    pos -3.95827 8.53838 0.818801
    norm 9.01917 -0.740401 4.25517
    pos -4.14416 6.34258 0.769167
    norm 5.7389 -1.36193 8.07528
  }
  poly {
    numVertices 3
    pos -3.95827 8.53838 0.818801
    norm 9.01917 -0.740401 4.25517
    pos -3.99182 4.45988 0.161384
    norm 9.51856 -0.555067 3.01477
    pos -3.76933 8.38318 0.0669733
    norm 9.7092 0.543715 -2.33146
  }
  poly {
    numVertices 3
    pos -4.41575 -5.39906 -2.72035
    norm 0.304998 0.0797159 -9.99504
    pos -5.78898 -5.3049 -2.60849
    norm -4.80425 -2.17328 -8.49683
    pos -5.48056 -2.86297 -2.79063
    norm -0.0272999 0.0652342 -9.99975
  }
  poly {
    numVertices 3
    pos -5.94909 4.15499 -1.51447
    norm -5.55342 2.42297 -7.95543
    pos -6.5942 1.11603 -1.93166
    norm -9.33229 1.76466 -3.12959
    pos -6.18026 4.11301 -1.09287
    norm -9.43279 1.84354 -2.76114
  }
  poly {
    numVertices 3
    pos 4.18665 -18.8733 1.83591
    norm -9.04527 2.70881 3.29324
    pos 4.96878 -17.7485 2.58786
    norm -8.08089 0.867681 5.82636
    pos 4.7818 -18.0847 2.10223
    norm -9.96826 0.236757 0.760023
  }
  poly {
    numVertices 3
    pos -5.59569 -9.69985 2.82243
    norm -5.75759 0.908653 8.12555
    pos -6.09022 -9.17202 2.34186
    norm -7.40475 0.743075 6.67963
    pos -5.91918 -10.9343 2.52807
    norm -7.05401 -1.02711 7.01328
  }
  poly {
    numVertices 3
    pos -6.09022 -9.17202 2.34186
    norm -7.40475 0.743075 6.67963
    pos -5.59569 -9.69985 2.82243
    norm -5.75759 0.908653 8.12555
    pos -5.59577 -8.97342 2.66893
    norm -5.65369 0.982106 8.1897
  }
  poly {
    numVertices 3
    pos -0.432926 -9.78983 5.41481
    norm -3.28229 -0.131218 9.44507
    pos -0.737537 -8.75539 5.35773
    norm -2.93786 1.25827 9.47553
    pos -1.54057 -9.76134 5.1475
    norm -3.87715 -0.467208 9.20594
  }
  poly {
    numVertices 3
    pos -4.33866 -16.9699 -2.98265
    norm 3.10092 -3.56512 8.8133
    pos -4.859 -16.7585 -2.9291
    norm -4.02286 -1.00849 9.09942
    pos -4.9726 -18.8315 -3.15757
    norm -0.342929 -2.75362 9.60729
  }
  poly {
    numVertices 3
    pos 14.1917 -14.694 14.1011
    norm -6.84043 -5.78602 4.4419
    pos 15.0179 -14.4384 14.0639
    norm 9.34882 2.94242 1.98538
    pos 14.8238 -14.6449 14.4049
    norm 3.75364 -5.77145 7.25262
  }
  poly {
    numVertices 3
    pos -4.42753 -18.8147 -3.34335
    norm 4.87093 -4.5342 7.46425
    pos -3.74297 -16.894 -3.39166
    norm 6.85428 -3.50839 6.38045
    pos -4.33866 -16.9699 -2.98265
    norm 3.10092 -3.56512 8.8133
  }
  poly {
    numVertices 3
    pos -6.70153 0.32487 -0.818477
    norm -9.77228 0.453305 2.0729
    pos -6.73434 -1.6125 -1.00367
    norm -9.57322 -0.188188 2.88409
    pos -6.53347 -0.893021 -0.548512
    norm -8.48769 -0.0512964 5.2874
  }
  poly {
    numVertices 3
    pos -1.20858 -12.8907 3.09074
    norm -3.10844 -8.89057 3.36085
    pos 0.899806 -13.3662 3.1669
    norm -2.47061 -9.61816 -1.17773
    pos 0.634711 -13.1459 3.57186
    norm -5.46732 -6.82679 -4.84804
  }
  poly {
    numVertices 3
    pos -5.28706 7.99038 0.975844
    norm -5.77012 -0.83039 8.12503
    pos -5.70448 8.23418 0.42774
    norm -9.06602 0.144092 4.21741
    pos -5.68235 6.67428 0.515124
    norm -7.4058 -0.233301 6.71563
  }
  poly {
    numVertices 3
    pos 3.15852 -8.38935 -0.523185
    norm 7.1048 2.70633 -6.49596
    pos 3.66145 -9.71634 -0.365146
    norm 7.14836 -0.106065 -6.99212
    pos 2.91566 -9.60734 -1.01569
    norm 6.774 0.522784 -7.33754
  }
  poly {
    numVertices 3
    pos 3.25728 -14.1647 5.3891
    norm 5.63874 -8.12302 -1.49034
    pos 2.41752 -14.6786 5.48116
    norm 7.30528 -6.28082 2.68034
    pos 2.35422 -14.6418 4.83468
    norm 7.92009 -4.98631 -3.52262
  }
  poly {
    numVertices 3
    pos -4.65145 0.78429 0.426296
    norm 1.93167 -0.466882 9.80054
    pos -4.03784 0.88588 0.0193272
    norm 8.05782 0.248429 5.9169
    pos -4.2911 4.37588 0.509789
    norm 4.66363 -0.868751 8.80317
  }
  poly {
    numVertices 3
    pos 7.25101 -13.1962 6.91231
    norm -3.06769 -1.87857 9.33061
    pos 8.76807 -14.6156 7.03628
    norm -3.17875 -2.74516 9.07522
    pos 9.21038 -13.7496 7.20827
    norm -1.84563 0.888925 9.78792
  }
  poly {
    numVertices 3
    pos -3.80574 -0.0732203 -0.647841
    norm 9.93586 1.06025 0.39308
    pos -3.7529 -1.36371 -0.192882
    norm 9.0451 2.36578 3.54812
    pos -3.48307 -2.10146 -0.874187
    norm 9.14499 3.8158 -1.34493
  }
  poly {
    numVertices 3
    pos -3.7529 -1.36371 -0.192882
    norm 9.0451 2.36578 3.54812
    pos -3.80574 -0.0732203 -0.647841
    norm 9.93586 1.06025 0.39308
    pos -4.03784 0.88588 0.0193272
    norm 8.05782 0.248429 5.9169
  }
  poly {
    numVertices 3
    pos 11.491 -16.0655 6.38224
    norm 2.99165 -7.05107 -6.42903
    pos 10.2669 -15.3472 5.29908
    norm 2.26622 -5.84393 -7.79184
    pos 12.0257 -15.4419 6.29163
    norm 5.76813 -1.79928 -7.96815
  }
  poly {
    numVertices 3
    pos -1.72534 -12.5177 -1.18808
    norm 0.78716 -9.81368 -1.75274
    pos -2.1929 -13.0381 -1.98032
    norm 4.27925 -5.69345 7.01945
    pos -1.17107 -12.2054 -1.40582
    norm 4.76051 -8.33617 -2.80104
  }
  poly {
    numVertices 3
    pos -2.1929 -13.0381 -1.98032
    norm 4.27925 -5.69345 7.01945
    pos -1.72534 -12.5177 -1.18808
    norm 0.78716 -9.81368 -1.75274
    pos -2.67378 -12.6316 -1.64741
    norm 1.19233 -7.34456 6.68101
  }
  poly {
    numVertices 3
    pos 5.03743 -7.43863 5.19513
    norm 1.90716 6.02343 7.7512
    pos 4.28666 -6.41083 4.51174
    norm 2.28514 7.5741 6.11647
    pos 3.92788 -7.08466 5.09628
    norm 1.54171 6.71857 7.24458
  }
  poly {
    numVertices 3
    pos 7.70443 -10.2055 4.42118
    norm 7.85518 5.2371 -3.2968
    pos 7.81209 -10.9002 3.95792
    norm 7.99863 2.98298 -5.20805
    pos 6.62089 -9.35893 2.58673
    norm 8.27734 4.20574 -3.7145
  }
  poly {
    numVertices 3
    pos -0.89814 -5.35791 -2.07046
    norm 4.42295 5.83826 -6.80824
    pos -0.487816 -5.06965 -1.22605
    norm 4.79827 6.76325 -5.58884
    pos 0.692984 -6.27446 -1.1957
    norm 5.30086 5.54441 -6.41565
  }
  poly {
    numVertices 3
    pos 5.98871 -13.3607 0.632285
    norm 7.67601 0.280815 -6.40312
    pos 5.76414 -11.6978 0.288584
    norm 7.71584 1.35467 -6.21535
    pos 6.17638 -11.7776 0.996549
    norm 7.92142 0.766966 -6.05498
  }
  poly {
    numVertices 3
    pos 5.03338 -16.9736 0.343904
    norm -2.88769 -4.37162 -8.51764
    pos 4.97961 -15.3822 -0.338328
    norm 2.2475 -1.2381 -9.66518
    pos 6.48723 -18.1052 0.690953
    norm 3.23443 0.65688 -9.43965
  }
  poly {
    numVertices 3
    pos -4.23926 -13.4181 3.47991
    norm -3.49412 0.851568 9.33092
    pos -4.76517 -12.5792 2.96131
    norm -5.98565 -0.453797 7.99788
    pos -4.83519 -13.6808 3.17317
    norm -6.09209 1.06364 7.85844
  }
  poly {
    numVertices 3
    pos -4.76517 -12.5792 2.96131
    norm -5.98565 -0.453797 7.99788
    pos -4.23926 -13.4181 3.47991
    norm -3.49412 0.851568 9.33092
    pos -4.22326 -12.3853 3.23113
    norm -1.65301 0.685175 9.83861
  }
  poly {
    numVertices 3
    pos -0.728564 -4.24 2.03679
    norm 0.892517 8.28634 5.5263
    pos -1.08883 -4.78023 2.76778
    norm 0.0327969 7.6055 6.49271
    pos 2.7402 -6.02309 4.17817
    norm 0.669737 7.72844 6.31052
  }
  poly {
    numVertices 3
    pos -1.08883 -4.78023 2.76778
    norm 0.0327969 7.6055 6.49271
    pos -0.728564 -4.24 2.03679
    norm 0.892517 8.28634 5.5263
    pos -1.6586 -4.1407 2.12365
    norm -0.0900502 7.82454 6.22644
  }
  poly {
    numVertices 3
    pos -5.51697 0.50327 0.346612
    norm -3.18266 -0.484049 9.46765
    pos -4.65145 0.78429 0.426296
    norm 1.93167 -0.466882 9.80054
    pos -5.16347 1.65372 0.514513
    norm -1.3543 -0.579078 9.89093
  }
  poly {
    numVertices 3
    pos -4.65145 0.78429 0.426296
    norm 1.93167 -0.466882 9.80054
    pos -5.51697 0.50327 0.346612
    norm -3.18266 -0.484049 9.46765
    pos -4.97643 -0.57256 0.3864
    norm -1.27418 0.0279944 9.91845
  }
  poly {
    numVertices 3
    pos 5.98871 -13.3607 0.632285
    norm 7.67601 0.280815 -6.40312
    pos 5.07755 -13.7669 -0.265191
    norm 4.77252 -0.149132 -8.7864
    pos 5.41775 -12.2746 -0.164942
    norm 5.81577 0.64092 -8.10963
  }
  poly {
    numVertices 3
    pos 7.81209 -10.9002 3.95792
    norm 7.99863 2.98298 -5.20805
    pos 9.04117 -13.1318 4.51572
    norm 6.25962 1.81214 -7.58507
    pos 7.83166 -11.7518 3.74877
    norm 7.28161 0.351777 -6.84503
  }
  poly {
    numVertices 3
    pos 5.60475 -15.3098 0.0731382
    norm 6.35995 0.365823 -7.70826
    pos 6.09135 -15.1202 0.594855
    norm 8.4985 0.981692 -5.17801
    pos 6.081 -16.2644 0.324269
    norm 6.38897 0.561773 -7.67239
  }
  poly {
    numVertices 3
    pos 7.10629 -13.6283 3.83632
    norm 2.082 -7.3437 -6.46029
    pos 7.34488 -13.0766 3.5858
    norm 5.73292 -3.69174 -7.31469
    pos 8.62872 -14.367 4.41651
    norm 1.84261 -4.41137 -8.7832
  }
  poly {
    numVertices 3
    pos -3.83307 9.29778 0.392387
    norm 9.9715 0.277973 -0.701367
    pos -3.85952 10.602 1.11018
    norm 9.17845 -0.808337 3.88624
    pos -3.95827 8.53838 0.818801
    norm 9.01917 -0.740401 4.25517
  }
  poly {
    numVertices 3
    pos -5.52638 -15.6086 2.63084
    norm -8.20097 0.228738 5.71767
    pos -5.33984 -13.5362 2.54091
    norm -8.93817 1.05547 4.35833
    pos -5.91348 -16.3229 1.89824
    norm -9.84757 0.11928 1.73527
  }
  poly {
    numVertices 3
    pos -3.15492 -14.1057 1.49503
    norm 8.18651 -2.4407 -5.19846
    pos -2.62799 -12.7573 1.64045
    norm 3.08013 -8.67321 3.91002
    pos -2.8119 -13.0048 2.23074
    norm 9.28423 -2.69607 2.55625
  }
  poly {
    numVertices 3
    pos -2.62799 -12.7573 1.64045
    norm 3.08013 -8.67321 3.91002
    pos -3.15492 -14.1057 1.49503
    norm 8.18651 -2.4407 -5.19846
    pos -3.09029 -13.3382 1.11748
    norm 4.12123 -7.0672 -5.75066
  }
  poly {
    numVertices 3
    pos -5.65267 -6.6997 -1.84277
    norm -8.19825 -2.69973 -5.04977
    pos -5.8034 -7.88741 -1.12425
    norm -9.08584 -0.153234 -4.17421
    pos -5.97654 -6.55009 -1.04053
    norm -9.68913 -2.09191 -1.32087
  }
  poly {
    numVertices 3
    pos -1.78565 -5.33574 3.16769
    norm -1.93798 5.95125 7.79915
    pos -2.84257 -6.72762 3.60283
    norm -3.14435 3.20493 8.93541
    pos -2.1032 -6.8853 4.14129
    norm -3.60766 4.55602 8.13803
  }
  poly {
    numVertices 3
    pos -2.84257 -6.72762 3.60283
    norm -3.14435 3.20493 8.93541
    pos -1.78565 -5.33574 3.16769
    norm -1.93798 5.95125 7.79915
    pos -2.58326 -5.15962 2.93761
    norm -1.51553 6.20177 7.69684
  }
  poly {
    numVertices 3
    pos 7.70443 -10.2055 4.42118
    norm 7.85518 5.2371 -3.2968
    pos 7.24279 -9.05532 5.38874
    norm 6.98985 7.02064 1.36114
    pos 8.92635 -10.9024 5.56255
    norm 7.64951 5.54286 -3.2805
  }
  poly {
    numVertices 3
    pos 4.00977 -9.22015 0.217357
    norm 5.27793 3.58148 -7.70171
    pos 3.66145 -9.71634 -0.365146
    norm 7.14836 -0.106065 -6.99212
    pos 3.15852 -8.38935 -0.523185
    norm 7.1048 2.70633 -6.49596
  }
  poly {
    numVertices 3
    pos 3.91675 -7.13478 1.77387
    norm 5.88737 6.63754 -4.61323
    pos 4.42427 -6.98383 2.47132
    norm 6.16395 7.06011 -3.48719
    pos 4.90674 -7.51158 2.10966
    norm 5.82643 7.12747 -3.90537
  }
  poly {
    numVertices 3
    pos -3.74502 -7.58815 -3.6016
    norm -4.07146 4.11782 -8.15272
    pos -2.75459 -6.57729 -3.30717
    norm 0.287488 4.43187 -8.95968
    pos -3.21034 -7.7722 -3.72271
    norm 0.0363636 2.49318 -9.68415
  }
  poly {
    numVertices 3
    pos -2.75459 -6.57729 -3.30717
    norm 0.287488 4.43187 -8.95968
    pos -3.74502 -7.58815 -3.6016
    norm -4.07146 4.11782 -8.15272
    pos -2.99029 -5.97223 -2.84402
    norm -1.02474 5.49437 -8.29227
  }
  poly {
    numVertices 3
    pos -0.273659 -8.40599 -2.69356
    norm 6.7042 2.36737 -7.03201
    pos 0.511418 -7.91169 -2.21703
    norm 4.25657 2.49436 -8.69827
    pos 0.10746 -8.88475 -2.57265
    norm 3.99296 1.57889 -9.03124
  }
  poly {
    numVertices 3
    pos -4.0326 -14.5984 0.816786
    norm 4.98562 -0.562871 -8.65025
    pos -3.15492 -14.1057 1.49503
    norm 8.18651 -2.4407 -5.19846
    pos -3.47869 -15.0434 1.36022
    norm 8.26886 -0.899591 -5.55128
  }
  poly {
    numVertices 3
    pos -3.15492 -14.1057 1.49503
    norm 8.18651 -2.4407 -5.19846
    pos -4.0326 -14.5984 0.816786
    norm 4.98562 -0.562871 -8.65025
    pos -3.92176 -13.6286 0.731951
    norm 2.62178 -2.20689 -9.39446
  }
  poly {
    numVertices 3
    pos 6.28983 -14.3377 1.44391
    norm 9.67517 -2.50103 0.368631
    pos 6.24481 -13.7521 0.908654
    norm 8.93794 -0.54909 -4.45105
    pos 6.92175 -12.6385 1.96847
    norm 9.27388 -1.3204 -3.50026
  }
  poly {
    numVertices 3
    pos 1.11266 -17.1594 6.36618
    norm 3.1409 -0.394874 9.48572
    pos 0.794315 -15.5449 6.52063
    norm -2.07978 -0.0475231 9.78122
    pos 0.606488 -16.9722 6.3572
    norm -3.58062 -0.587752 9.31846
  }
  poly {
    numVertices 3
    pos 0.794315 -15.5449 6.52063
    norm -2.07978 -0.0475231 9.78122
    pos 1.11266 -17.1594 6.36618
    norm 3.1409 -0.394874 9.48572
    pos 1.30906 -14.7929 6.48271
    norm 1.28867 -0.814554 9.8831
  }
  poly {
    numVertices 3
    pos -4.94828 -19.5758 1.99663
    norm 0.481895 -9.91623 1.19837
    pos -6.1455 -19.4069 1.54821
    norm -9.17673 -3.6539 -1.56097
    pos -6.01756 -19.1983 0.864143
    norm -8.31338 -3.23897 -4.51629
  }
  poly {
    numVertices 3
    pos -6.4463 -18.8541 -4.66278
    norm -7.95525 2.36024 -5.58062
    pos -6.6304 -18.4853 -4.01595
    norm -8.33235 3.20496 4.50557
    pos -5.83976 -18.1071 -4.05654
    norm -8.73339 4.82933 0.636729
  }
  poly {
    numVertices 3
    pos -6.6304 -18.4853 -4.01595
    norm -8.33235 3.20496 4.50557
    pos -6.4463 -18.8541 -4.66278
    norm -7.95525 2.36024 -5.58062
    pos -6.68188 -19.2314 -4.35633
    norm -8.526 -5.14793 -0.89787
  }
  poly {
    numVertices 3
    pos 0.10746 -8.88475 -2.57265
    norm 3.99296 1.57889 -9.03124
    pos 0.939623 -8.53884 -2.27839
    norm 3.72696 2.48527 -8.94054
    pos 1.40846 -9.1765 -2.19909
    norm 4.33043 0.853549 -8.97323
  }
  poly {
    numVertices 3
    pos 0.939623 -8.53884 -2.27839
    norm 3.72696 2.48527 -8.94054
    pos 0.10746 -8.88475 -2.57265
    norm 3.99296 1.57889 -9.03124
    pos 0.511418 -7.91169 -2.21703
    norm 4.25657 2.49436 -8.69827
  }
  poly {
    numVertices 3
    pos 4.98477 -15.6632 2.63136
    norm -1.57615 0.274975 9.87118
    pos 4.37221 -15.1109 2.26629
    norm -6.46204 -3.07488 6.98479
    pos 4.49541 -16.1976 2.44063
    norm -6.32529 -0.408662 7.73459
  }
  poly {
    numVertices 3
    pos 3.67478 -11.0294 0.137297
    norm 0.86657 -0.833314 -9.92747
    pos 3.48108 -12.0348 0.0176834
    norm -2.71987 -1.45157 -9.5129
    pos 3.03443 -11.4539 -0.388693
    norm 5.80919 -3.96849 -7.10665
  }
  poly {
    numVertices 3
    pos -2.97108 -8.36129 4.09266
    norm -5.79013 2.12898 7.87031
    pos -3.63628 -9.06782 3.69973
    norm -5.65925 0.263433 8.24036
    pos -2.70757 -9.17808 4.45397
    norm -5.76781 0.131912 8.16792
  }
  poly {
    numVertices 3
    pos -5.78898 -5.3049 -2.60849
    norm -4.80425 -2.17328 -8.49683
    pos -6.76337 -1.47748 -2.52411
    norm -6.89324 0.413042 -7.23276
    pos -5.48056 -2.86297 -2.79063
    norm -0.0272999 0.0652342 -9.99975
  }
  poly {
    numVertices 3
    pos -1.99347 -12.9008 1.58513
    norm -1.46428 -9.82497 1.15144
    pos -1.20858 -12.8907 3.09074
    norm -3.10844 -8.89057 3.36085
    pos -2.62799 -12.7573 1.64045
    norm 3.08013 -8.67321 3.91002
  }
  poly {
    numVertices 3
    pos 4.00977 -9.22015 0.217357
    norm 5.27793 3.58148 -7.70171
    pos 5.15589 -8.26437 1.34689
    norm 5.86433 5.32345 -6.10496
    pos 5.26463 -8.91549 0.894319
    norm 5.9096 3.98031 -7.01669
  }
  poly {
    numVertices 3
    pos 5.15589 -8.26437 1.34689
    norm 5.86433 5.32345 -6.10496
    pos 4.00977 -9.22015 0.217357
    norm 5.27793 3.58148 -7.70171
    pos 4.12718 -8.40835 0.706444
    norm 6.23654 4.09118 -6.66093
  }
  poly {
    numVertices 3
    pos 2.78563 -10.6028 -1.07071
    norm 6.83545 -2.12318 -6.98346
    pos 3.3456 -10.5321 -0.356067
    norm 5.52992 -3.63531 -7.49696
    pos 3.03443 -11.4539 -0.388693
    norm 5.80919 -3.96849 -7.10665
  }
  poly {
    numVertices 3
    pos 3.3456 -10.5321 -0.356067
    norm 5.52992 -3.63531 -7.49696
    pos 2.78563 -10.6028 -1.07071
    norm 6.83545 -2.12318 -6.98346
    pos 3.66145 -9.71634 -0.365146
    norm 7.14836 -0.106065 -6.99212
  }
  poly {
    numVertices 3
    pos -3.80574 -0.0732203 -0.647841
    norm 9.93586 1.06025 0.39308
    pos -3.94628 2.11456 -0.763765
    norm 9.93457 0.42033 -1.0619
    pos -4.03784 0.88588 0.0193272
    norm 8.05782 0.248429 5.9169
  }
  poly {
    numVertices 3
    pos -4.87718 9.30128 1.3301
    norm -2.26571 -1.63239 9.60218
    pos -4.30081 10.6338 1.62579
    norm 2.44105 -1.9551 9.49835
    pos -5.00519 10.6159 1.59781
    norm -4.66419 -1.49476 8.71843
  }
  poly {
    numVertices 3
    pos 4.74128 -19.0837 0.354702
    norm -6.12297 3.6265 -7.0255
    pos 5.11581 -17.9895 1.08666
    norm -7.47975 -1.60475 -6.44036
    pos 5.88241 -17.9389 0.617416
    norm -2.21668 2.20694 -9.4982
  }
  poly {
    numVertices 3
    pos 12.9284 -14.287 9.09242
    norm -1.95407 8.78251 4.36453
    pos 12.4158 -14.8994 9.2069
    norm -6.3765 4.03786 6.56018
    pos 13.9479 -14.3227 10.2476
    norm -0.137379 9.99617 0.240422
  }
  poly {
    numVertices 3
    pos 3.90304 -7.66766 1.0278
    norm 5.89429 5.85771 -5.56279
    pos 5.15589 -8.26437 1.34689
    norm 5.86433 5.32345 -6.10496
    pos 4.12718 -8.40835 0.706444
    norm 6.23654 4.09118 -6.66093
  }
  poly {
    numVertices 3
    pos 5.15589 -8.26437 1.34689
    norm 5.86433 5.32345 -6.10496
    pos 3.90304 -7.66766 1.0278
    norm 5.89429 5.85771 -5.56279
    pos 4.90674 -7.51158 2.10966
    norm 5.82643 7.12747 -3.90537
  }
  poly {
    numVertices 3
    pos -3.73905 -18.7716 2.41767
    norm 7.38283 -3.70313 5.63743
    pos -3.95442 -19.3788 1.48969
    norm 4.60156 -8.83616 0.864803
    pos -3.41743 -18.6993 1.71869
    norm 9.81143 -1.91974 -0.224945
  }
  poly {
    numVertices 3
    pos 6.70286 -11.8926 6.86067
    norm -1.18294 -0.156284 9.92856
    pos 7.25101 -13.1962 6.91231
    norm -3.06769 -1.87857 9.33061
    pos 9.21038 -13.7496 7.20827
    norm -1.84563 0.888925 9.78792
  }
  poly {
    numVertices 3
    pos -5.70136 -16.3119 -3.53508
    norm -9.17219 1.17268 3.80733
    pos -5.41685 -15.8033 -4.18426
    norm -8.46398 3.58729 -3.93604
    pos -5.64811 -16.6603 -3.81808
    norm -9.99552 0.231984 0.188913
  }
  poly {
    numVertices 3
    pos -5.41685 -15.8033 -4.18426
    norm -8.46398 3.58729 -3.93604
    pos -5.70136 -16.3119 -3.53508
    norm -9.17219 1.17268 3.80733
    pos -5.13742 -15.1904 -3.4014
    norm -8.75339 4.09711 2.56746
  }
  poly {
    numVertices 3
    pos 2.64476 -12.8334 6.95851
    norm -0.118875 -1.26298 9.91921
    pos 2.35778 -10.1694 6.88436
    norm -0.929721 1.62577 9.82306
    pos 1.51897 -11.4031 6.89312
    norm -1.99592 0.663922 9.77627
  }
  poly {
    numVertices 3
    pos -4.70929 5.13828 -1.26631
    norm 4.614 1.41352 -8.7586
    pos -4.26404 4.96568 -0.938556
    norm 7.22135 0.74888 -6.87686
    pos -4.33288 2.84798 -1.3933
    norm 7.4742 1.04776 -6.56038
  }
  poly {
    numVertices 3
    pos 3.67478 -11.0294 0.137297
    norm 0.86657 -0.833314 -9.92747
    pos 3.03443 -11.4539 -0.388693
    norm 5.80919 -3.96849 -7.10665
    pos 3.3456 -10.5321 -0.356067
    norm 5.52992 -3.63531 -7.49696
  }
  poly {
    numVertices 3
    pos -4.7626 -11.8623 -1.08569
    norm -3.18104 -8.93423 -3.17183
    pos -5.40757 -11.031 -1.79466
    norm -7.23615 -4.23949 -5.44654
    pos -4.51294 -11.9106 -1.79198
    norm -6.63337 -7.44736 -0.731678
  }
  poly {
    numVertices 3
    pos 7.34488 -13.0766 3.5858
    norm 5.73292 -3.69174 -7.31469
    pos 8.02126 -13.0395 3.8613
    norm 4.5653 -0.643587 -8.87377
    pos 8.62872 -14.367 4.41651
    norm 1.84261 -4.41137 -8.7832
  }
  poly {
    numVertices 3
    pos 5.88241 -17.9389 0.617416
    norm -2.21668 2.20694 -9.4982
    pos 6.48723 -18.1052 0.690953
    norm 3.23443 0.65688 -9.43965
    pos 5.87754 -18.655 0.110617
    norm 0.105673 5.38323 -8.42673
  }
  poly {
    numVertices 3
    pos 5.47074 -7.06492 4.41854
    norm 6.9719 7.1489 -0.534642
    pos 4.16085 -6.57047 3.02101
    norm 6.06509 7.2655 -3.22909
    pos 3.94738 -5.90251 3.77839
    norm 4.66748 8.84387 -0.0253589
  }
  poly {
    numVertices 3
    pos -4.57349 -14.8283 0.72303
    norm -0.778585 0.263387 -9.96616
    pos -3.98486 -15.7726 0.775604
    norm 5.25154 0.418081 -8.49979
    pos -4.43158 -16.409 0.551847
    norm 2.92083 0.433753 -9.55409
  }
  poly {
    numVertices 3
    pos -3.98486 -15.7726 0.775604
    norm 5.25154 0.418081 -8.49979
    pos -4.57349 -14.8283 0.72303
    norm -0.778585 0.263387 -9.96616
    pos -4.0326 -14.5984 0.816786
    norm 4.98562 -0.562871 -8.65025
  }
  poly {
    numVertices 3
    pos -3.47146 -17.0699 -3.9623
    norm 8.9259 -3.66105 2.63157
    pos -2.77505 -15.7261 -3.26141
    norm 6.56821 -6.21017 4.27696
    pos -3.74297 -16.894 -3.39166
    norm 6.85428 -3.50839 6.38045
  }
  poly {
    numVertices 3
    pos -2.77505 -15.7261 -3.26141
    norm 6.56821 -6.21017 4.27696
    pos -3.47146 -17.0699 -3.9623
    norm 8.9259 -3.66105 2.63157
    pos -2.43726 -15.3994 -3.93026
    norm 8.14933 -4.94443 -3.02343
  }
  poly {
    numVertices 3
    pos -0.487816 -5.06965 -1.22605
    norm 4.79827 6.76325 -5.58884
    pos -0.89717 -4.52626 -0.804798
    norm 4.9225 7.68979 -4.07876
    pos 1.4807 -6.14113 -0.368454
    norm 5.5266 6.55219 -5.15029
  }
  poly {
    numVertices 3
    pos -1.08242 -19.3609 3.54029
    norm -5.95052 -5.48093 -5.87798
    pos 1.11993 -19.3723 3.91824
    norm 4.53941 -5.72818 -6.82508
    pos -0.666849 -19.4694 4.75092
    norm -3.70606 -9.28357 -0.283884
  }
  poly {
    numVertices 3
    pos 1.13881 -6.01673 4.42871
    norm -0.628232 6.48513 7.58607
    pos 1.40944 -7.87463 5.54854
    norm -2.27225 4.64026 8.56183
    pos 2.50165 -6.55031 4.875
    norm -0.209473 6.89509 7.23973
  }
  poly {
    numVertices 3
    pos -4.47069 -11.6215 -2.42129
    norm -8.93755 -3.98278 -2.0634
    pos -3.93253 -12.4792 -1.74255
    norm -7.42124 -4.34416 5.10426
    pos -4.51294 -11.9106 -1.79198
    norm -6.63337 -7.44736 -0.731678
  }
  poly {
    numVertices 3
    pos -2.46167 -12.6482 -0.746944
    norm -2.10803 -9.73919 -0.83927
    pos -4.7626 -11.8623 -1.08569
    norm -3.18104 -8.93423 -3.17183
    pos -4.51294 -11.9106 -1.79198
    norm -6.63337 -7.44736 -0.731678
  }
  poly {
    numVertices 3
    pos 5.87754 -18.655 0.110617
    norm 0.105673 5.38323 -8.42673
    pos 7.00204 -18.0865 0.909847
    norm 5.40778 3.8668 -7.4702
    pos 6.33275 -18.7768 0.0730906
    norm 3.32945 4.45133 -8.31267
  }
  poly {
    numVertices 3
    pos 7.00204 -18.0865 0.909847
    norm 5.40778 3.8668 -7.4702
    pos 5.87754 -18.655 0.110617
    norm 0.105673 5.38323 -8.42673
    pos 6.48723 -18.1052 0.690953
    norm 3.23443 0.65688 -9.43965
  }
  poly {
    numVertices 3
    pos -6.73434 -1.6125 -1.00367
    norm -9.57322 -0.188188 2.88409
    pos -6.32544 -2.85629 -0.131951
    norm -7.90495 0.333222 6.11562
    pos -6.53347 -0.893021 -0.548512
    norm -8.48769 -0.0512964 5.2874
  }
  poly {
    numVertices 3
    pos -3.73905 -18.7716 2.41767
    norm 7.38283 -3.70313 5.63743
    pos -4.94828 -19.5758 1.99663
    norm 0.481895 -9.91623 1.19837
    pos -3.95442 -19.3788 1.48969
    norm 4.60156 -8.83616 0.864803
  }
  poly {
    numVertices 3
    pos -2.75459 -6.57729 -3.30717
    norm 0.287488 4.43187 -8.95968
    pos -1.39952 -6.08959 -2.76028
    norm 3.11769 4.21128 -8.51734
    pos -2.01924 -7.02041 -3.31201
    norm 2.89212 2.64906 -9.19881
  }
  poly {
    numVertices 3
    pos -1.39952 -6.08959 -2.76028
    norm 3.11769 4.21128 -8.51734
    pos -2.75459 -6.57729 -3.30717
    norm 0.287488 4.43187 -8.95968
    pos -2.04694 -5.77759 -2.72298
    norm 1.0199 6.1023 -7.85632
  }
  poly {
    numVertices 3
    pos -3.25182 -13.8052 3.20214
    norm 7.13934 -1.76287 6.77659
    pos -3.73572 -12.4283 3.1961
    norm 3.40211 1.349 9.30623
    pos -3.57977 -13.548 3.56998
    norm 2.22492 -0.0265895 9.74931
  }
  poly {
    numVertices 3
    pos -3.73572 -12.4283 3.1961
    norm 3.40211 1.349 9.30623
    pos -3.25182 -13.8052 3.20214
    norm 7.13934 -1.76287 6.77659
    pos -3.07857 -11.868 2.95479
    norm 3.02265 -2.34406 9.23953
  }
  poly {
    numVertices 3
    pos 15.0179 -14.4384 14.0639
    norm 9.34882 2.94242 1.98538
    pos 14.0515 -14.4601 13.5955
    norm -9.92904 -0.230176 1.16675
    pos 14.3362 -13.8803 14
    norm -4.81193 8.69132 -1.14297
  }
  poly {
    numVertices 3
    pos 14.0515 -14.4601 13.5955
    norm -9.92904 -0.230176 1.16675
    pos 15.0179 -14.4384 14.0639
    norm 9.34882 2.94242 1.98538
    pos 14.3549 -13.8269 13.5641
    norm -4.00888 9.13736 -0.661526
  }
  poly {
    numVertices 3
    pos 3.15246 -14.0169 1.1708
    norm -8.84922 -4.5932 -0.770525
    pos 3.84096 -15.6113 0.261495
    norm -7.84306 -3.62031 -5.03783
    pos 3.71647 -15.4835 0.993134
    norm -9.11882 -3.74517 1.67953
  }
  poly {
    numVertices 3
    pos -2.98619 -14.333 1.92776
    norm 9.58291 -1.88899 -2.14466
    pos -2.8119 -13.0048 2.23074
    norm 9.28423 -2.69607 2.55625
    pos -3.00445 -13.7999 2.7631
    norm 9.2791 -1.71494 3.31018
  }
  poly {
    numVertices 3
    pos -6.37764 -4.77125 -0.192311
    norm -9.64338 -0.808735 2.52017
    pos -6.77095 -2.81837 -2.24608
    norm -9.63537 -1.14348 -2.41912
    pos -6.50666 -4.65668 -1.68746
    norm -9.48676 -2.6625 -1.70659
  }
  poly {
    numVertices 3
    pos -0.283818 -19.0143 3.46991
    norm 0.589034 3.23168 -9.44507
    pos 0.615579 -18.4413 3.97877
    norm 1.41317 2.72626 -9.51685
    pos 1.11993 -19.3723 3.91824
    norm 4.53941 -5.72818 -6.82508
  }
  poly {
    numVertices 3
    pos -2.32546 -10.8304 -4.03467
    norm 3.46307 1.71881 -9.22241
    pos -1.71931 -11.2007 -3.74338
    norm 5.91182 0.907191 -8.0142
    pos -2.34586 -12.94 -4.21181
    norm 3.45429 0.986163 -9.33249
  }
  poly {
    numVertices 3
    pos 1.11372 -11.2381 -2.00032
    norm 3.4597 -4.65003 -8.14909
    pos 2.05608 -11.3913 -1.31843
    norm 5.36314 -4.74617 -6.9793
    pos 1.10714 -12.3829 -1.0688
    norm 3.98915 -6.74336 -6.21399
  }
  poly {
    numVertices 3
    pos -3.68464 -15.8654 -4.90982
    norm 2.47642 1.428 -9.5827
    pos -4.29227 -17.5202 -5.39181
    norm 1.52838 3.381 -9.28617
    pos -4.1396 -14.962 -4.7755
    norm -2.63305 3.36237 -9.04221
  }
  poly {
    numVertices 3
    pos -4.29227 -17.5202 -5.39181
    norm 1.52838 3.381 -9.28617
    pos -3.68464 -15.8654 -4.90982
    norm 2.47642 1.428 -9.5827
    pos -3.76136 -16.9466 -5.05756
    norm 6.25336 0.271389 -7.79883
  }
  poly {
    numVertices 3
    pos -1.13676 -19.0025 4.13924
    norm -9.76427 1.88834 -1.04558
    pos -1.04649 -18.8133 3.66466
    norm -6.08121 5.77567 -5.44614
    pos -1.08242 -19.3609 3.54029
    norm -5.95052 -5.48093 -5.87798
  }
  poly {
    numVertices 3
    pos -0.132075 -18.0928 4.40838
    norm -6.58332 4.50687 -6.02893
    pos -1.04649 -18.8133 3.66466
    norm -6.08121 5.77567 -5.44614
    pos -1.13676 -19.0025 4.13924
    norm -9.76427 1.88834 -1.04558
  }
  poly {
    numVertices 3
    pos 0.634711 -13.1459 3.57186
    norm -5.46732 -6.82679 -4.84804
    pos 1.06416 -14.8908 3.66895
    norm 1.99546 -1.27894 -9.71506
    pos 0.508093 -17.5007 3.96244
    norm -2.8014 -0.75583 -9.56979
  }
  poly {
    numVertices 3
    pos 5.76159 -8.37016 2.21232
    norm 7.33373 5.97791 -3.23743
    pos 6.24673 -8.40136 3.8262
    norm 7.35641 6.3507 -2.35625
    pos 6.62089 -9.35893 2.58673
    norm 8.27734 4.20574 -3.7145
  }
  poly {
    numVertices 3
    pos 6.24673 -8.40136 3.8262
    norm 7.35641 6.3507 -2.35625
    pos 5.76159 -8.37016 2.21232
    norm 7.33373 5.97791 -3.23743
    pos 4.90674 -7.51158 2.10966
    norm 5.82643 7.12747 -3.90537
  }
  poly {
    numVertices 3
    pos 8.02126 -13.0395 3.8613
    norm 4.5653 -0.643587 -8.87377
    pos 10.4382 -14.8406 5.17183
    norm 4.54151 -1.39927 -8.79868
    pos 8.62872 -14.367 4.41651
    norm 1.84261 -4.41137 -8.7832
  }
  poly {
    numVertices 3
    pos 3.22428 -7.81218 6.12013
    norm 0.340707 5.13796 8.57235
    pos 4.646 -9.84243 6.84369
    norm 1.07643 3.00155 9.47797
    pos 5.00063 -8.51581 5.97394
    norm 2.33725 5.28274 8.16272
  }
  poly {
    numVertices 3
    pos 4.646 -9.84243 6.84369
    norm 1.07643 3.00155 9.47797
    pos 3.22428 -7.81218 6.12013
    norm 0.340707 5.13796 8.57235
    pos 3.92801 -9.63744 6.72303
    norm -0.215369 2.67197 9.63402
  }
  poly {
    numVertices 3
    pos -5.51697 0.50327 0.346612
    norm -3.18266 -0.484049 9.46765
    pos -6.18769 -1.09847 -0.130771
    norm -6.52266 0.131418 7.57877
    pos -5.76876 -0.84932 0.16111
    norm -4.3658 0.1777 8.9949
  }
  poly {
    numVertices 3
    pos -6.18769 -1.09847 -0.130771
    norm -6.52266 0.131418 7.57877
    pos -6.53347 -0.893021 -0.548512
    norm -8.48769 -0.0512964 5.2874
    pos -6.32544 -2.85629 -0.131951
    norm -7.90495 0.333222 6.11562
  }
  poly {
    numVertices 3
    pos -4.33866 -16.9699 -2.98265
    norm 3.10092 -3.56512 8.8133
    pos -4.9726 -18.8315 -3.15757
    norm -0.342929 -2.75362 9.60729
    pos -4.42753 -18.8147 -3.34335
    norm 4.87093 -4.5342 7.46425
  }
  poly {
    numVertices 3
    pos -3.73905 -18.7716 2.41767
    norm 7.38283 -3.70313 5.63743
    pos -3.82645 -16.9752 2.05766
    norm 9.18596 -0.487118 3.92185
    pos -4.07782 -16.7707 2.7645
    norm 6.10066 0.127016 7.92249
  }
  poly {
    numVertices 3
    pos -3.6129 -16.5015 1.63802
    norm 9.68138 -1.39526 2.07945
    pos -3.41743 -18.6993 1.71869
    norm 9.81143 -1.91974 -0.224945
    pos -3.63079 -16.1815 1.1078
    norm 9.03447 -0.0437649 -4.28677
  }
  poly {
    numVertices 3
    pos 1.12225 -10.7054 6.58825
    norm -5.91535 2.71199 7.59301
    pos 0.190796 -12.0301 6.22176
    norm -8.04967 1.6415 5.70161
    pos 0.740866 -12.1068 6.69971
    norm -4.56911 0.0836025 8.89473
  }
  poly {
    numVertices 3
    pos 0.190796 -12.0301 6.22176
    norm -8.04967 1.6415 5.70161
    pos 1.12225 -10.7054 6.58825
    norm -5.91535 2.71199 7.59301
    pos 0.411964 -11.0887 5.99328
    norm -8.24775 2.2903 5.17002
  }
  poly {
    numVertices 3
    pos -4.79786 -16.1441 -4.88718
    norm -6.05979 2.91969 -7.39962
    pos -5.68397 -18.1957 -4.65381
    norm -7.43067 4.28013 -5.14447
    pos -5.41685 -15.8033 -4.18426
    norm -8.46398 3.58729 -3.93604
  }
  poly {
    numVertices 3
    pos -5.39593 -19.2514 -6.0843
    norm -4.47909 -1.31291 -8.84386
    pos -6.4463 -18.8541 -4.66278
    norm -7.95525 2.36024 -5.58062
    pos -5.68397 -18.1957 -4.65381
    norm -7.43067 4.28013 -5.14447
  }
  poly {
    numVertices 3
    pos -6.4463 -18.8541 -4.66278
    norm -7.95525 2.36024 -5.58062
    pos -5.39593 -19.2514 -6.0843
    norm -4.47909 -1.31291 -8.84386
    pos -6.20664 -19.3608 -4.92499
    norm -4.21599 -8.80304 -2.17529
  }
  poly {
    numVertices 3
    pos -0.334905 -14.7387 4.80715
    norm -9.80808 0.379854 -1.9124
    pos -0.134512 -12.5962 5.47859
    norm -9.74782 1.36904 1.76229
    pos 0.0136405 -12.2344 4.57136
    norm -8.80427 -3.52215 3.1748
  }
  poly {
    numVertices 3
    pos -0.134512 -12.5962 5.47859
    norm -9.74782 1.36904 1.76229
    pos -0.334905 -14.7387 4.80715
    norm -9.80808 0.379854 -1.9124
    pos -0.368209 -14.303 5.31731
    norm -9.65013 0.450162 2.58308
  }
  poly {
    numVertices 3
    pos 1.30906 -14.7929 6.48271
    norm 1.28867 -0.814554 9.8831
    pos 1.11266 -17.1594 6.36618
    norm 3.1409 -0.394874 9.48572
    pos 1.67285 -14.3322 6.3714
    norm 4.82088 -3.01384 8.22653
  }
  poly {
    numVertices 3
    pos -6.22047 -9.19181 -0.776008
    norm -9.05972 0.412972 -4.2132
    pos -6.06486 -6.60961 0.267663
    norm -9.84512 1.30847 1.16685
    pos -5.9866 -6.63685 -0.256173
    norm -9.85918 -0.491795 -1.59838
  }
  poly {
    numVertices 3
    pos -6.06486 -6.60961 0.267663
    norm -9.84512 1.30847 1.16685
    pos -6.22047 -9.19181 -0.776008
    norm -9.05972 0.412972 -4.2132
    pos -6.70611 -10.0692 0.710534
    norm -9.76272 -0.789839 -2.01628
  }
  poly {
    numVertices 3
    pos 6.75368 -11.3013 1.63419
    norm 7.89281 1.31516 -5.99784
    pos 6.59183 -10.0371 2.00844
    norm 7.87103 3.44094 -5.11926
    pos 7.0604 -11.1212 2.16792
    norm 9.17089 1.40861 -3.72968
  }
  poly {
    numVertices 3
    pos -5.87452 -11.0919 -1.04466
    norm -8.41081 -3.54622 -4.08445
    pos -5.72269 -12.0342 0.203904
    norm -6.6302 -6.71224 -3.31457
    pos -6.31301 -10.9601 0.0356826
    norm -8.95624 -4.11627 -1.68584
  }
  poly {
    numVertices 3
    pos -5.72269 -12.0342 0.203904
    norm -6.6302 -6.71224 -3.31457
    pos -5.87452 -11.0919 -1.04466
    norm -8.41081 -3.54622 -4.08445
    pos -5.21528 -12.174 -0.517622
    norm -3.31427 -8.18459 -4.69341
  }
  poly {
    numVertices 3
    pos -5.96826 -7.88366 2.26762
    norm -7.97156 2.15979 5.63823
    pos -6.16766 -7.5436 1.48804
    norm -9.17332 3.01716 2.59748
    pos -6.74061 -9.87395 1.32155
    norm -9.66247 0.589177 2.50792
  }
  poly {
    numVertices 3
    pos -2.01924 -7.02041 -3.31201
    norm 2.89212 2.64906 -9.19881
    pos -0.302616 -7.1779 -2.58596
    norm 5.3647 2.43245 -8.08104
    pos -0.462525 -8.21891 -2.89676
    norm 5.85536 1.72956 -7.9198
  }
  poly {
    numVertices 3
    pos -0.302616 -7.1779 -2.58596
    norm 5.3647 2.43245 -8.08104
    pos -2.01924 -7.02041 -3.31201
    norm 2.89212 2.64906 -9.19881
    pos -1.39952 -6.08959 -2.76028
    norm 3.11769 4.21128 -8.51734
  }
  poly {
    numVertices 3
    pos 2.5073 -6.01438 1.25491
    norm 5.8757 7.35017 -3.38395
    pos 4.16085 -6.57047 3.02101
    norm 6.06509 7.2655 -3.22909
    pos 4.42427 -6.98383 2.47132
    norm 6.16395 7.06011 -3.48719
  }
  poly {
    numVertices 3
    pos -5.22259 -8.05642 -2.08366
    norm -8.27872 0.101808 -5.60825
    pos -5.67109 -9.58226 -1.54096
    norm -8.73711 0.174782 -4.86131
    pos -5.8034 -7.88741 -1.12425
    norm -9.08584 -0.153234 -4.17421
  }
  poly {
    numVertices 3
    pos -5.67109 -9.58226 -1.54096
    norm -8.73711 0.174782 -4.86131
    pos -5.22259 -8.05642 -2.08366
    norm -8.27872 0.101808 -5.60825
    pos -5.40614 -10.1019 -2.23332
    norm -8.29182 -1.4815 -5.38988
  }
  poly {
    numVertices 3
    pos 2.50165 -6.55031 4.875
    norm -0.209473 6.89509 7.23973
    pos 2.34706 -7.76509 5.85884
    norm -2.15846 5.01183 8.3799
    pos 3.22428 -7.81218 6.12013
    norm 0.340707 5.13796 8.57235
  }
  poly {
    numVertices 3
    pos 2.34706 -7.76509 5.85884
    norm -2.15846 5.01183 8.3799
    pos 2.50165 -6.55031 4.875
    norm -0.209473 6.89509 7.23973
    pos 1.40944 -7.87463 5.54854
    norm -2.27225 4.64026 8.56183
  }
  poly {
    numVertices 3
    pos 12.7463 -15.9303 7.07573
    norm 6.23089 -3.66144 -6.91158
    pos 13.8795 -15.9158 8.35377
    norm 7.85484 -2.88631 -5.47456
    pos 12.9711 -16.5308 7.91567
    norm 3.64796 -8.60927 -3.54583
  }
  poly {
    numVertices 3
    pos 13.8795 -15.9158 8.35377
    norm 7.85484 -2.88631 -5.47456
    pos 12.7463 -15.9303 7.07573
    norm 6.23089 -3.66144 -6.91158
    pos 13.2641 -15.5252 7.47795
    norm 7.69544 0.144981 -6.38429
  }
  poly {
    numVertices 3
    pos -1.20858 -12.8907 3.09074
    norm -3.10844 -8.89057 3.36085
    pos -1.12156 -13.2487 1.61868
    norm -1.99203 -9.76177 0.859989
    pos 0.899806 -13.3662 3.1669
    norm -2.47061 -9.61816 -1.17773
  }
  poly {
    numVertices 3
    pos -5.41685 -15.8033 -4.18426
    norm -8.46398 3.58729 -3.93604
    pos -5.83976 -18.1071 -4.05654
    norm -8.73339 4.82933 0.636729
    pos -5.64811 -16.6603 -3.81808
    norm -9.99552 0.231984 0.188913
  }
  poly {
    numVertices 3
    pos -5.83976 -18.1071 -4.05654
    norm -8.73339 4.82933 0.636729
    pos -5.41685 -15.8033 -4.18426
    norm -8.46398 3.58729 -3.93604
    pos -5.68397 -18.1957 -4.65381
    norm -7.43067 4.28013 -5.14447
  }
  poly {
    numVertices 3
    pos 2.40176 -9.20136 6.5942
    norm -1.69627 3.79548 9.09489
    pos 1.40944 -7.87463 5.54854
    norm -2.27225 4.64026 8.56183
    pos 1.50041 -8.76085 6.05027
    norm -3.71296 3.35072 8.65948
  }
  poly {
    numVertices 3
    pos 1.40944 -7.87463 5.54854
    norm -2.27225 4.64026 8.56183
    pos 2.40176 -9.20136 6.5942
    norm -1.69627 3.79548 9.09489
    pos 2.34706 -7.76509 5.85884
    norm -2.15846 5.01183 8.3799
  }
  poly {
    numVertices 3
    pos -3.65413 -15.3926 2.68235
    norm 6.94342 -2.63732 6.69577
    pos -4.73337 -17.0081 2.82271
    norm -1.32038 -0.222874 9.90994
    pos -4.07782 -16.7707 2.7645
    norm 6.10066 0.127016 7.92249
  }
  poly {
    numVertices 3
    pos -4.73337 -17.0081 2.82271
    norm -1.32038 -0.222874 9.90994
    pos -3.65413 -15.3926 2.68235
    norm 6.94342 -2.63732 6.69577
    pos -4.09451 -15.121 3.07477
    norm 2.64513 -3.03033 9.15534
  }
  poly {
    numVertices 3
    pos -5.40614 -10.1019 -2.23332
    norm -8.29182 -1.4815 -5.38988
    pos -4.90419 -8.06049 -2.56106
    norm -8.05375 1.27667 -5.78853
    pos -4.99445 -9.69996 -2.84098
    norm -8.56445 -1.05053 -5.05437
  }
  poly {
    numVertices 3
    pos -4.90419 -8.06049 -2.56106
    norm -8.05375 1.27667 -5.78853
    pos -5.40614 -10.1019 -2.23332
    norm -8.29182 -1.4815 -5.38988
    pos -5.22259 -8.05642 -2.08366
    norm -8.27872 0.101808 -5.60825
  }
  poly {
    numVertices 3
    pos 6.62089 -9.35893 2.58673
    norm 8.27734 4.20574 -3.7145
    pos 5.15589 -8.26437 1.34689
    norm 5.86433 5.32345 -6.10496
    pos 5.76159 -8.37016 2.21232
    norm 7.33373 5.97791 -3.23743
  }
  poly {
    numVertices 3
    pos -4.9726 -18.8315 -3.15757
    norm -0.342929 -2.75362 9.60729
    pos -6.34574 -19.2106 -3.95814
    norm -4.83054 -5.13626 7.09118
    pos -5.16662 -19.3946 -3.58875
    norm 0.214517 -8.2453 5.65412
  }
  poly {
    numVertices 3
    pos 6.59183 -10.0371 2.00844
    norm 7.87103 3.44094 -5.11926
    pos 5.15589 -8.26437 1.34689
    norm 5.86433 5.32345 -6.10496
    pos 6.62089 -9.35893 2.58673
    norm 8.27734 4.20574 -3.7145
  }
  poly {
    numVertices 3
    pos 5.15589 -8.26437 1.34689
    norm 5.86433 5.32345 -6.10496
    pos 6.59183 -10.0371 2.00844
    norm 7.87103 3.44094 -5.11926
    pos 5.26463 -8.91549 0.894319
    norm 5.9096 3.98031 -7.01669
  }
  poly {
    numVertices 3
    pos -2.13791 -3.39392 0.722582
    norm 2.8227 9.03571 3.2231
    pos -0.728564 -4.24 2.03679
    norm 0.892517 8.28634 5.5263
    pos 2.18633 -5.28753 3.29878
    norm 1.23168 8.57469 4.99576
  }
  poly {
    numVertices 3
    pos 9.16125 -15.5749 5.49072
    norm -1.40995 -8.29987 -5.39669
    pos 8.62872 -14.367 4.41651
    norm 1.84261 -4.41137 -8.7832
    pos 10.2669 -15.3472 5.29908
    norm 2.26622 -5.84393 -7.79184
  }
  poly {
    numVertices 3
    pos 6.34665 -13.8848 3.30822
    norm 6.26657 -7.77736 0.492668
    pos 6.56015 -13.7083 2.5919
    norm 8.45985 -5.23714 -1.00163
    pos 7.34488 -13.0766 3.5858
    norm 5.73292 -3.69174 -7.31469
  }
  poly {
    numVertices 3
    pos -2.34586 -12.94 -4.21181
    norm 3.45429 0.986163 -9.33249
    pos -2.05895 -13.9549 -4.22049
    norm 5.54484 -0.426677 -8.311
    pos -3.08106 -13.6066 -4.46041
    norm -0.742782 2.22322 -9.7214
  }
  poly {
    numVertices 3
    pos 15.2959 -14.58 13.1373
    norm 9.41907 2.89557 1.70202
    pos 14.72 -15.1718 13.5718
    norm 1.0773 -8.42496 5.27821
    pos 14.5757 -15.3658 12.6944
    norm -0.132056 -8.98233 4.3932
  }
  poly {
    numVertices 3
    pos 14.72 -15.1718 13.5718
    norm 1.0773 -8.42496 5.27821
    pos 15.2959 -14.58 13.1373
    norm 9.41907 2.89557 1.70202
    pos 15.1075 -14.5324 13.4979
    norm 5.98485 2.23643 7.69285
  }
  poly {
    numVertices 3
    pos 6.75368 -11.3013 1.63419
    norm 7.89281 1.31516 -5.99784
    pos 5.26463 -8.91549 0.894319
    norm 5.9096 3.98031 -7.01669
    pos 6.59183 -10.0371 2.00844
    norm 7.87103 3.44094 -5.11926
  }
  poly {
    numVertices 3
    pos 5.26463 -8.91549 0.894319
    norm 5.9096 3.98031 -7.01669
    pos 6.75368 -11.3013 1.63419
    norm 7.89281 1.31516 -5.99784
    pos 5.69516 -10.6303 0.601429
    norm 7.27045 2.189 -6.5076
  }
  poly {
    numVertices 3
    pos -5.27899 -18.8243 -0.162918
    norm 1.72187 3.43433 -9.23258
    pos -4.17299 -19.0353 0.497947
    norm 6.1323 -0.272282 -7.89436
    pos -5.0548 -19.1499 -0.108273
    norm 3.25779 -3.50773 -8.77967
  }
  poly {
    numVertices 3
    pos -4.17299 -19.0353 0.497947
    norm 6.1323 -0.272282 -7.89436
    pos -5.27899 -18.8243 -0.162918
    norm 1.72187 3.43433 -9.23258
    pos -4.73778 -18.4412 0.38361
    norm 1.70897 3.82109 -9.08178
  }
  poly {
    numVertices 3
    pos -6.70153 0.32487 -0.818477
    norm -9.77228 0.453305 2.0729
    pos -6.38869 3.01368 -0.701171
    norm -9.91581 1.2354 0.387893
    pos -6.5942 1.11603 -1.93166
    norm -9.33229 1.76466 -3.12959
  }
  poly {
    numVertices 3
    pos -3.8842 -0.17551 -1.42712
    norm 8.45929 1.22575 -5.19018
    pos -3.48307 -2.10146 -0.874187
    norm 9.14499 3.8158 -1.34493
    pos -3.798 -1.41512 -1.41323
    norm 8.64647 1.8449 -4.67279
  }
  poly {
    numVertices 3
    pos -3.48307 -2.10146 -0.874187
    norm 9.14499 3.8158 -1.34493
    pos -3.8842 -0.17551 -1.42712
    norm 8.45929 1.22575 -5.19018
    pos -3.80574 -0.0732203 -0.647841
    norm 9.93586 1.06025 0.39308
  }
  poly {
    numVertices 3
    pos -1.12623 -18.9312 5.58733
    norm -8.76816 0.530275 4.77893
    pos -1.27763 -19.0498 4.83098
    norm -9.98953 -0.388803 0.240663
    pos -0.799095 -19.4412 5.65921
    norm -5.07439 -7.78489 3.69407
  }
  poly {
    numVertices 3
    pos -4.69667 -8.59835 3.17691
    norm -4.03678 0.617014 9.12818
    pos -5.48614 -7.78082 2.73145
    norm -5.5324 1.47646 8.19833
    pos -5.59577 -8.97342 2.66893
    norm -5.65369 0.982106 8.1897
  }
  poly {
    numVertices 3
    pos -5.48614 -7.78082 2.73145
    norm -5.5324 1.47646 8.19833
    pos -4.69667 -8.59835 3.17691
    norm -4.03678 0.617014 9.12818
    pos -4.83457 -7.89267 3.07841
    norm -3.74292 0.659117 9.24966
  }
  poly {
    numVertices 3
    pos 12.9711 -16.5308 7.91567
    norm 3.64796 -8.60927 -3.54583
    pos 10.4504 -16.0691 6.22518
    norm -0.833738 -9.45258 -3.15492
    pos 11.491 -16.0655 6.38224
    norm 2.99165 -7.05107 -6.42903
  }
  poly {
    numVertices 3
    pos 10.4504 -16.0691 6.22518
    norm -0.833738 -9.45258 -3.15492
    pos 12.9711 -16.5308 7.91567
    norm 3.64796 -8.60927 -3.54583
    pos 12.5146 -16.6051 8.32545
    norm -2.50109 -9.50222 1.85805
  }
  poly {
    numVertices 3
    pos 10.2669 -15.3472 5.29908
    norm 2.26622 -5.84393 -7.79184
    pos 10.4504 -16.0691 6.22518
    norm -0.833738 -9.45258 -3.15492
    pos 9.16125 -15.5749 5.49072
    norm -1.40995 -8.29987 -5.39669
  }
  poly {
    numVertices 3
    pos 10.4504 -16.0691 6.22518
    norm -0.833738 -9.45258 -3.15492
    pos 10.2669 -15.3472 5.29908
    norm 2.26622 -5.84393 -7.79184
    pos 11.491 -16.0655 6.38224
    norm 2.99165 -7.05107 -6.42903
  }
  poly {
    numVertices 3
    pos -5.21528 -12.174 -0.517622
    norm -3.31427 -8.18459 -4.69341
    pos -5.40757 -11.031 -1.79466
    norm -7.23615 -4.23949 -5.44654
    pos -4.7626 -11.8623 -1.08569
    norm -3.18104 -8.93423 -3.17183
  }
  poly {
    numVertices 3
    pos -5.40757 -11.031 -1.79466
    norm -7.23615 -4.23949 -5.44654
    pos -5.21528 -12.174 -0.517622
    norm -3.31427 -8.18459 -4.69341
    pos -5.87452 -11.0919 -1.04466
    norm -8.41081 -3.54622 -4.08445
  }
  poly {
    numVertices 3
    pos -1.10285 -3.98427 0.285084
    norm 5.66868 7.07042 -4.22792
    pos -3.06933 -3.75821 -1.76762
    norm 5.86742 4.02452 -7.02685
    pos -2.34291 -3.79958 -1.0176
    norm 7.08379 4.76794 -5.20448
  }
  poly {
    numVertices 3
    pos -3.06933 -3.75821 -1.76762
    norm 5.86742 4.02452 -7.02685
    pos -1.10285 -3.98427 0.285084
    norm 5.66868 7.07042 -4.22792
    pos -2.04646 -4.13963 -1.17775
    norm 4.5813 6.94476 -5.54814
  }
  poly {
    numVertices 3
    pos -6.24211 2.74096 -0.200528
    norm -8.29056 0.385716 5.57834
    pos -6.70153 0.32487 -0.818477
    norm -9.77228 0.453305 2.0729
    pos -6.38285 1.25007 -0.232028
    norm -7.90629 0.119105 6.12178
  }
  poly {
    numVertices 3
    pos -6.70153 0.32487 -0.818477
    norm -9.77228 0.453305 2.0729
    pos -6.24211 2.74096 -0.200528
    norm -8.29056 0.385716 5.57834
    pos -6.38869 3.01368 -0.701171
    norm -9.91581 1.2354 0.387893
  }
  poly {
    numVertices 3
    pos -1.10285 -3.98427 0.285084
    norm 5.66868 7.07042 -4.22792
    pos 3.94738 -5.90251 3.77839
    norm 4.66748 8.84387 -0.0253589
    pos 4.16085 -6.57047 3.02101
    norm 6.06509 7.2655 -3.22909
  }
  poly {
    numVertices 3
    pos -1.79687 -14.7058 -2.95384
    norm 7.30355 -5.18284 4.4493
    pos -2.1929 -13.0381 -1.98032
    norm 4.27925 -5.69345 7.01945
    pos -2.19534 -14.5856 -2.5191
    norm 5.02069 -4.6074 7.31879
  }
  poly {
    numVertices 3
    pos -2.1929 -13.0381 -1.98032
    norm 4.27925 -5.69345 7.01945
    pos -1.79687 -14.7058 -2.95384
    norm 7.30355 -5.18284 4.4493
    pos -1.62737 -12.7661 -2.13916
    norm 8.26146 -4.79473 2.95955
  }
  poly {
    numVertices 3
    pos -0.773334 -10.1339 -2.91499
    norm 6.91569 -1.80504 -6.99394
    pos -0.897379 -9.00866 -3.28279
    norm 5.1314 -0.470598 -8.57014
    pos -0.28981 -9.73938 -2.63913
    norm 3.64756 -2.20968 -9.04503
  }
  poly {
    numVertices 3
    pos -0.897379 -9.00866 -3.28279
    norm 5.1314 -0.470598 -8.57014
    pos -0.773334 -10.1339 -2.91499
    norm 6.91569 -1.80504 -6.99394
    pos -1.27279 -9.92793 -3.33552
    norm 5.18182 -0.992654 -8.49491
  }
  poly {
    numVertices 3
    pos -3.09029 -13.3382 1.11748
    norm 4.12123 -7.0672 -5.75066
    pos -5.21528 -12.174 -0.517622
    norm -3.31427 -8.18459 -4.69341
    pos -3.24709 -12.7552 0.506168
    norm 0.419805 -8.9403 -4.46036
  }
  poly {
    numVertices 3
    pos -5.21528 -12.174 -0.517622
    norm -3.31427 -8.18459 -4.69341
    pos -3.09029 -13.3382 1.11748
    norm 4.12123 -7.0672 -5.75066
    pos -4.31979 -12.7319 0.377351
    norm -1.72291 -7.06117 -6.86815
  }
  poly {
    numVertices 3
    pos 1.88938 -17.1928 5.6789
    norm 9.71842 -0.930545 2.16478
    pos 1.64301 -18.8177 4.6335
    norm 9.08146 -0.664226 -4.13349
    pos 1.67222 -17.6491 4.94128
    norm 9.29694 -0.492403 -3.65028
  }
  poly {
    numVertices 3
    pos 1.70578 -7.6199 -1.54277
    norm 5.26075 3.983 -7.51401
    pos 0.692984 -6.27446 -1.1957
    norm 5.30086 5.54441 -6.41565
    pos 1.54411 -6.69758 -0.977446
    norm 5.52338 5.84929 -5.93954
  }
  poly {
    numVertices 3
    pos 0.692984 -6.27446 -1.1957
    norm 5.30086 5.54441 -6.41565
    pos 1.70578 -7.6199 -1.54277
    norm 5.26075 3.983 -7.51401
    pos 0.740215 -7.15194 -1.76874
    norm 5.19378 4.05653 -7.52126
  }
  poly {
    numVertices 3
    pos 5.87754 -18.655 0.110617
    norm 0.105673 5.38323 -8.42673
    pos 4.74128 -19.0837 0.354702
    norm -6.12297 3.6265 -7.0255
    pos 5.88241 -17.9389 0.617416
    norm -2.21668 2.20694 -9.4982
  }
  poly {
    numVertices 3
    pos 4.74128 -19.0837 0.354702
    norm -6.12297 3.6265 -7.0255
    pos 5.87754 -18.655 0.110617
    norm 0.105673 5.38323 -8.42673
    pos 6.02434 -19.4702 -0.272587
    norm 0.540327 -4.5337 -8.89683
  }
  poly {
    numVertices 3
    pos -0.0299261 -7.59195 5.2301
    norm -2.19697 3.87407 8.95349
    pos -2.04821 -8.4586 4.76938
    norm -4.59977 2.26527 8.58549
    pos -0.737537 -8.75539 5.35773
    norm -2.93786 1.25827 9.47553
  }
  poly {
    numVertices 3
    pos -2.04821 -8.4586 4.76938
    norm -4.59977 2.26527 8.58549
    pos -0.0299261 -7.59195 5.2301
    norm -2.19697 3.87407 8.95349
    pos -2.1032 -6.8853 4.14129
    norm -3.60766 4.55602 8.13803
  }
  poly {
    numVertices 3
    pos 12.6226 -15.1059 6.90908
    norm 6.91303 1.83304 -6.98928
    pos 10.0688 -13.0327 5.42226
    norm 6.5949 3.87326 -6.44245
    pos 13.4579 -15.1211 7.84139
    norm 7.34013 4.49935 -5.08707
  }
  poly {
    numVertices 3
    pos 10.0688 -13.0327 5.42226
    norm 6.5949 3.87326 -6.44245
    pos 12.6226 -15.1059 6.90908
    norm 6.91303 1.83304 -6.98928
    pos 10.376 -14.037 5.24704
    norm 6.03574 2.03773 -7.70827
  }
  poly {
    numVertices 3
    pos -6.18769 -1.09847 -0.130771
    norm -6.52266 0.131418 7.57877
    pos -5.87396 1.74018 0.199279
    norm -5.63666 -0.143113 8.25879
    pos -6.38285 1.25007 -0.232028
    norm -7.90629 0.119105 6.12178
  }
  poly {
    numVertices 3
    pos -5.87396 1.74018 0.199279
    norm -5.63666 -0.143113 8.25879
    pos -6.18769 -1.09847 -0.130771
    norm -6.52266 0.131418 7.57877
    pos -5.51697 0.50327 0.346612
    norm -3.18266 -0.484049 9.46765
  }
  poly {
    numVertices 3
    pos 5.44222 -18.8925 2.97636
    norm -1.541 -4.79986 8.63635
    pos 6.74407 -19.04 2.64974
    norm 3.60163 -7.088 6.06535
    pos 6.64033 -18.4669 3.05128
    norm 4.54137 -1.31627 8.81155
  }
  poly {
    numVertices 3
    pos 6.74407 -19.04 2.64974
    norm 3.60163 -7.088 6.06535
    pos 5.44222 -18.8925 2.97636
    norm -1.541 -4.79986 8.63635
    pos 5.70841 -19.3793 2.33548
    norm -0.0113854 -9.29228 3.69503
  }
  poly {
    numVertices 3
    pos 6.11378 -15.1697 1.76768
    norm 7.82517 1.79214 5.96281
    pos 6.89929 -16.9192 1.47205
    norm 9.5297 2.97301 -0.588242
    pos 6.49118 -15.5662 1.5058
    norm 9.47453 2.22726 2.2962
  }
  poly {
    numVertices 3
    pos 6.89929 -16.9192 1.47205
    norm 9.5297 2.97301 -0.588242
    pos 6.11378 -15.1697 1.76768
    norm 7.82517 1.79214 5.96281
    pos 6.80751 -16.8562 2.14021
    norm 8.21816 4.10041 3.95582
  }
  poly {
    numVertices 3
    pos -4.859 -16.7585 -2.9291
    norm -4.02286 -1.00849 9.09942
    pos -3.95801 -13.9431 -2.18399
    norm -7.31147 0.0977299 6.82151
    pos -4.93657 -15.5553 -2.98597
    norm -6.98659 1.26198 7.04238
  }
  poly {
    numVertices 3
    pos -5.52638 -15.6086 2.63084
    norm -8.20097 0.228738 5.71767
    pos -5.6559 -17.7624 1.87307
    norm -9.8892 0.938324 -1.15032
    pos -5.74922 -17.0399 2.18766
    norm -9.74152 0.168909 2.2526
  }
  poly {
    numVertices 3
    pos -5.6559 -17.7624 1.87307
    norm -9.8892 0.938324 -1.15032
    pos -5.52638 -15.6086 2.63084
    norm -8.20097 0.228738 5.71767
    pos -5.91348 -16.3229 1.89824
    norm -9.84757 0.11928 1.73527
  }
  poly {
    numVertices 3
    pos 4.53682 -11.5524 -0.407543
    norm -0.730066 2.46174 -9.66472
    pos 3.82521 -11.0628 0.0781074
    norm -1.34017 -0.867301 -9.87176
    pos 4.38278 -10.4601 -0.0408829
    norm 0.470627 1.85455 -9.81525
  }
  poly {
    numVertices 3
    pos -6.06486 -6.60961 0.267663
    norm -9.84512 1.30847 1.16685
    pos -6.74061 -9.87395 1.32155
    norm -9.66247 0.589177 2.50792
    pos -6.16766 -7.5436 1.48804
    norm -9.17332 3.01716 2.59748
  }
  poly {
    numVertices 3
    pos -6.74061 -9.87395 1.32155
    norm -9.66247 0.589177 2.50792
    pos -6.06486 -6.60961 0.267663
    norm -9.84512 1.30847 1.16685
    pos -6.70611 -10.0692 0.710534
    norm -9.76272 -0.789839 -2.01628
  }
  poly {
    numVertices 3
    pos -5.78898 -5.3049 -2.60849
    norm -4.80425 -2.17328 -8.49683
    pos -6.77095 -2.81837 -2.24608
    norm -9.63537 -1.14348 -2.41912
    pos -6.76337 -1.47748 -2.52411
    norm -6.89324 0.413042 -7.23276
  }
  poly {
    numVertices 3
    pos -6.77095 -2.81837 -2.24608
    norm -9.63537 -1.14348 -2.41912
    pos -5.78898 -5.3049 -2.60849
    norm -4.80425 -2.17328 -8.49683
    pos -6.23098 -4.79782 -2.29041
    norm -8.30328 -2.83787 -4.79604
  }
  poly {
    numVertices 3
    pos 4.91991 -19.4177 1.19631
    norm -0.823745 -9.91614 -0.995827
    pos 4.60674 -19.3379 2.15178
    norm -4.37939 -7.62477 4.76276
    pos 3.87057 -19.2953 1.09091
    norm -8.81646 -3.40422 -3.26824
  }
  poly {
    numVertices 3
    pos 4.60674 -19.3379 2.15178
    norm -4.37939 -7.62477 4.76276
    pos 4.91991 -19.4177 1.19631
    norm -0.823745 -9.91614 -0.995827
    pos 5.70841 -19.3793 2.33548
    norm -0.0113854 -9.29228 3.69503
  }
  poly {
    numVertices 3
    pos -1.36154 -13.2043 0.290559
    norm -2.03814 -9.71714 -1.19297
    pos -1.99347 -12.9008 1.58513
    norm -1.46428 -9.82497 1.15144
    pos -3.24709 -12.7552 0.506168
    norm 0.419805 -8.9403 -4.46036
  }
  poly {
    numVertices 3
    pos -1.99347 -12.9008 1.58513
    norm -1.46428 -9.82497 1.15144
    pos -1.36154 -13.2043 0.290559
    norm -2.03814 -9.71714 -1.19297
    pos -1.12156 -13.2487 1.61868
    norm -1.99203 -9.76177 0.859989
  }
  poly {
    numVertices 3
    pos -0.132075 -18.0928 4.40838
    norm -6.58332 4.50687 -6.02893
    pos -0.283247 -16.1208 5.31343
    norm -9.98621 0.0295973 0.524017
    pos -0.0581061 -16.5159 4.3851
    norm -8.90835 -0.622618 -4.50041
  }
  poly {
    numVertices 3
    pos -0.283247 -16.1208 5.31343
    norm -9.98621 0.0295973 0.524017
    pos -0.132075 -18.0928 4.40838
    norm -6.58332 4.50687 -6.02893
    pos -0.347828 -17.881 5.04525
    norm -8.72187 4.83423 -0.747798
  }
  poly {
    numVertices 3
    pos -4.97643 -0.57256 0.3864
    norm -1.27418 0.0279944 9.91845
    pos -5.87569 -2.37263 0.248807
    norm -4.55692 1.03449 8.84106
    pos -5.04454 -1.96564 0.431794
    norm -1.1306 1.1066 9.87407
  }
  poly {
    numVertices 3
    pos -5.87569 -2.37263 0.248807
    norm -4.55692 1.03449 8.84106
    pos -4.97643 -0.57256 0.3864
    norm -1.27418 0.0279944 9.91845
    pos -5.76876 -0.84932 0.16111
    norm -4.3658 0.1777 8.9949
  }
  poly {
    numVertices 3
    pos -3.74502 -7.58815 -3.6016
    norm -4.07146 4.11782 -8.15272
    pos -4.70895 -8.53872 -3.14765
    norm -7.4631 2.37566 -6.21758
    pos -3.8676 -7.09659 -3.12022
    norm -4.37957 4.03862 -8.03174
  }
  poly {
    numVertices 3
    pos -4.70895 -8.53872 -3.14765
    norm -7.4631 2.37566 -6.21758
    pos -3.74502 -7.58815 -3.6016
    norm -4.07146 4.11782 -8.15272
    pos -4.16097 -8.86538 -3.81439
    norm -3.83121 1.62248 -9.09337
  }
  poly {
    numVertices 3
    pos -5.8495 7.29778 -0.182433
    norm -9.89567 1.10332 -0.926523
    pos -6.38869 3.01368 -0.701171
    norm -9.91581 1.2354 0.387893
    pos -5.70448 8.23418 0.42774
    norm -9.06602 0.144092 4.21741
  }
  poly {
    numVertices 3
    pos 2.76759 -13.5402 6.69144
    norm 2.18427 -5.17082 8.27597
    pos 1.41579 -13.7077 6.69625
    norm -0.879791 -2.42279 9.66209
    pos 1.67285 -14.3322 6.3714
    norm 4.82088 -3.01384 8.22653
  }
  poly {
    numVertices 3
    pos 1.41579 -13.7077 6.69625
    norm -0.879791 -2.42279 9.66209
    pos 2.76759 -13.5402 6.69144
    norm 2.18427 -5.17082 8.27597
    pos 2.64476 -12.8334 6.95851
    norm -0.118875 -1.26298 9.91921
  }
  poly {
    numVertices 3
    pos -4.859 -16.7585 -2.9291
    norm -4.02286 -1.00849 9.09942
    pos -3.67895 -14.6637 -2.20106
    norm -1.54476 -2.89539 9.44619
    pos -3.95801 -13.9431 -2.18399
    norm -7.31147 0.0977299 6.82151
  }
  poly {
    numVertices 3
    pos -3.73905 -18.7716 2.41767
    norm 7.38283 -3.70313 5.63743
    pos -3.6129 -16.5015 1.63802
    norm 9.68138 -1.39526 2.07945
    pos -3.82645 -16.9752 2.05766
    norm 9.18596 -0.487118 3.92185
  }
  poly {
    numVertices 3
    pos -3.6129 -16.5015 1.63802
    norm 9.68138 -1.39526 2.07945
    pos -3.73905 -18.7716 2.41767
    norm 7.38283 -3.70313 5.63743
    pos -3.41743 -18.6993 1.71869
    norm 9.81143 -1.91974 -0.224945
  }
  poly {
    numVertices 3
    pos 5.47606 -15.4799 2.53888
    norm 4.71801 -0.115056 8.8163
    pos 6.11378 -15.1697 1.76768
    norm 7.82517 1.79214 5.96281
    pos 5.73658 -14.7578 2.50278
    norm 7.11003 -3.92935 5.83162
  }
  poly {
    numVertices 3
    pos 6.11378 -15.1697 1.76768
    norm 7.82517 1.79214 5.96281
    pos 5.47606 -15.4799 2.53888
    norm 4.71801 -0.115056 8.8163
    pos 5.78605 -16.2582 2.61357
    norm 3.53917 3.53024 8.66092
  }
  poly {
    numVertices 3
    pos 7.42725 -18.6062 1.34154
    norm 9.37498 0.705162 -3.40773
    pos 6.63421 -17.1719 0.818538
    norm 7.58243 1.68217 -6.29897
    pos 6.89929 -16.9192 1.47205
    norm 9.5297 2.97301 -0.588242
  }
  poly {
    numVertices 3
    pos 6.63421 -17.1719 0.818538
    norm 7.58243 1.68217 -6.29897
    pos 7.42725 -18.6062 1.34154
    norm 9.37498 0.705162 -3.40773
    pos 7.00204 -18.0865 0.909847
    norm 5.40778 3.8668 -7.4702
  }
  poly {
    numVertices 3
    pos 10.702 -14.262 7.7392
    norm -3.33812 2.62744 9.05282
    pos 9.68214 -12.6627 7.02945
    norm -0.82539 3.09652 9.47261
    pos 10.14 -13.7587 7.4528
    norm -2.9087 1.48174 9.45219
  }
  poly {
    numVertices 3
    pos 3.92801 -9.63744 6.72303
    norm -0.215369 2.67197 9.63402
    pos 2.40176 -9.20136 6.5942
    norm -1.69627 3.79548 9.09489
    pos 2.35778 -10.1694 6.88436
    norm -0.929721 1.62577 9.82306
  }
  poly {
    numVertices 3
    pos 2.40176 -9.20136 6.5942
    norm -1.69627 3.79548 9.09489
    pos 3.92801 -9.63744 6.72303
    norm -0.215369 2.67197 9.63402
    pos 3.05568 -9.19868 6.57617
    norm -0.526782 3.67993 9.28336
  }
  poly {
    numVertices 3
    pos 0.702972 -14.1291 6.34669
    norm -4.11256 -0.320562 9.10956
    pos 0.255151 -15.9748 6.21275
    norm -6.12898 0.0820215 7.90119
    pos 0.794315 -15.5449 6.52063
    norm -2.07978 -0.0475231 9.78122
  }
  poly {
    numVertices 3
    pos 0.255151 -15.9748 6.21275
    norm -6.12898 0.0820215 7.90119
    pos 0.702972 -14.1291 6.34669
    norm -4.11256 -0.320562 9.10956
    pos 0.0206174 -13.8208 5.93032
    norm -7.14273 0.0755825 6.99827
  }
  poly {
    numVertices 3
    pos 1.54411 -6.69758 -0.977446
    norm 5.52338 5.84929 -5.93954
    pos 2.66949 -6.38225 0.685115
    norm 6.27073 6.46775 -4.34123
    pos 2.68289 -7.22944 -0.250937
    norm 6.72592 4.86208 -5.57873
  }
  poly {
    numVertices 3
    pos 2.66949 -6.38225 0.685115
    norm 6.27073 6.46775 -4.34123
    pos 1.54411 -6.69758 -0.977446
    norm 5.52338 5.84929 -5.93954
    pos 1.4807 -6.14113 -0.368454
    norm 5.5266 6.55219 -5.15029
  }
  poly {
    numVertices 3
    pos -2.97108 -8.36129 4.09266
    norm -5.79013 2.12898 7.87031
    pos -2.84257 -6.72762 3.60283
    norm -3.14435 3.20493 8.93541
    pos -3.42154 -7.48252 3.49583
    norm -3.93015 1.20342 9.11624
  }
  poly {
    numVertices 3
    pos -4.2911 4.37588 0.509789
    norm 4.66363 -0.868751 8.80317
    pos -5.16347 1.65372 0.514513
    norm -1.3543 -0.579078 9.89093
    pos -4.65145 0.78429 0.426296
    norm 1.93167 -0.466882 9.80054
  }
  poly {
    numVertices 3
    pos -5.16347 1.65372 0.514513
    norm -1.3543 -0.579078 9.89093
    pos -4.2911 4.37588 0.509789
    norm 4.66363 -0.868751 8.80317
    pos -4.90397 4.0578 0.6206
    norm 0.628265 -1.13764 9.9152
  }
  poly {
    numVertices 3
    pos 5.73481 -13.7659 5.83379
    norm -4.08864 -7.10352 5.72914
    pos 8.55083 -15.1088 6.68321
    norm -4.25555 -5.58175 7.12282
    pos 7.42283 -14.3698 6.54049
    norm -4.24943 -4.21384 8.01161
  }
  poly {
    numVertices 3
    pos -2.13791 -3.39392 0.722582
    norm 2.8227 9.03571 3.2231
    pos -1.6586 -4.1407 2.12365
    norm -0.0900502 7.82454 6.22644
    pos -0.728564 -4.24 2.03679
    norm 0.892517 8.28634 5.5263
  }
  poly {
    numVertices 3
    pos -1.6586 -4.1407 2.12365
    norm -0.0900502 7.82454 6.22644
    pos -2.13791 -3.39392 0.722582
    norm 2.8227 9.03571 3.2231
    pos -2.40707 -3.70086 1.36944
    norm -0.239843 7.79161 6.26365
  }
  poly {
    numVertices 3
    pos 1.64301 -18.8177 4.6335
    norm 9.08146 -0.664226 -4.13349
    pos 1.77574 -18.4491 5.91374
    norm 9.02425 -1.17894 4.14403
    pos 1.58199 -19.4485 5.60605
    norm 6.96701 -7.1643 -0.365442
  }
  poly {
    numVertices 3
    pos 1.77574 -18.4491 5.91374
    norm 9.02425 -1.17894 4.14403
    pos 1.64301 -18.8177 4.6335
    norm 9.08146 -0.664226 -4.13349
    pos 1.88938 -17.1928 5.6789
    norm 9.71842 -0.930545 2.16478
  }
  poly {
    numVertices 3
    pos -4.83519 -13.6808 3.17317
    norm -6.09209 1.06364 7.85844
    pos -5.434 -12.9887 2.25442
    norm -8.85879 -0.99168 4.53194
    pos -5.33984 -13.5362 2.54091
    norm -8.93817 1.05547 4.35833
  }
  poly {
    numVertices 3
    pos -5.434 -12.9887 2.25442
    norm -8.85879 -0.99168 4.53194
    pos -4.83519 -13.6808 3.17317
    norm -6.09209 1.06364 7.85844
    pos -4.76517 -12.5792 2.96131
    norm -5.98565 -0.453797 7.99788
  }
  poly {
    numVertices 3
    pos -4.7626 -11.8623 -1.08569
    norm -3.18104 -8.93423 -3.17183
    pos -1.36154 -13.2043 0.290559
    norm -2.03814 -9.71714 -1.19297
    pos -3.24709 -12.7552 0.506168
    norm 0.419805 -8.9403 -4.46036
  }
  poly {
    numVertices 3
    pos -1.36154 -13.2043 0.290559
    norm -2.03814 -9.71714 -1.19297
    pos -4.7626 -11.8623 -1.08569
    norm -3.18104 -8.93423 -3.17183
    pos -2.46167 -12.6482 -0.746944
    norm -2.10803 -9.73919 -0.83927
  }
  poly {
    numVertices 3
    pos -4.9726 -18.8315 -3.15757
    norm -0.342929 -2.75362 9.60729
    pos -6.11087 -18.8517 -3.75757
    norm -5.56098 0.461625 8.29834
    pos -6.34574 -19.2106 -3.95814
    norm -4.83054 -5.13626 7.09118
  }
  poly {
    numVertices 3
    pos -6.11087 -18.8517 -3.75757
    norm -5.56098 0.461625 8.29834
    pos -4.9726 -18.8315 -3.15757
    norm -0.342929 -2.75362 9.60729
    pos -5.09561 -18.4673 -3.07797
    norm -0.377619 0.392749 9.98515
  }
  poly {
    numVertices 3
    pos 4.74128 -19.0837 0.354702
    norm -6.12297 3.6265 -7.0255
    pos 4.78829 -18.1591 1.58407
    norm -8.49068 3.96241 -3.49395
    pos 5.11581 -17.9895 1.08666
    norm -7.47975 -1.60475 -6.44036
  }
  poly {
    numVertices 3
    pos 4.78829 -18.1591 1.58407
    norm -8.49068 3.96241 -3.49395
    pos 4.74128 -19.0837 0.354702
    norm -6.12297 3.6265 -7.0255
    pos 4.32077 -19.1593 0.92638
    norm -5.76429 -3.76151 -7.25424
  }
  poly {
    numVertices 3
    pos -0.582583 -18.4332 5.84974
    norm -6.51616 5.90666 4.75933
    pos -1.27763 -19.0498 4.83098
    norm -9.98953 -0.388803 0.240663
    pos -1.12623 -18.9312 5.58733
    norm -8.76816 0.530275 4.77893
  }
  poly {
    numVertices 3
    pos 0.157518 -12.7358 3.98195
    norm -9.20241 -3.80209 -0.9273
    pos -0.334905 -14.7387 4.80715
    norm -9.80808 0.379854 -1.9124
    pos 0.0136405 -12.2344 4.57136
    norm -8.80427 -3.52215 3.1748
  }
  poly {
    numVertices 3
    pos -0.334905 -14.7387 4.80715
    norm -9.80808 0.379854 -1.9124
    pos 0.157518 -12.7358 3.98195
    norm -9.20241 -3.80209 -0.9273
    pos -0.153755 -14.7952 4.09062
    norm -7.99686 -0.275063 -5.99788
  }
  poly {
    numVertices 3
    pos 7.0604 -11.1212 2.16792
    norm 9.17089 1.40861 -3.72968
    pos 6.62089 -9.35893 2.58673
    norm 8.27734 4.20574 -3.7145
    pos 7.1117 -10.7235 2.64221
    norm 9.10711 1.52615 -3.83816
  }
  poly {
    numVertices 3
    pos 6.62089 -9.35893 2.58673
    norm 8.27734 4.20574 -3.7145
    pos 7.0604 -11.1212 2.16792
    norm 9.17089 1.40861 -3.72968
    pos 6.59183 -10.0371 2.00844
    norm 7.87103 3.44094 -5.11926
  }
  poly {
    numVertices 3
    pos 14.7716 -14.9307 10.6684
    norm 8.60765 3.63719 -3.56081
    pos 14.2157 -15.1248 9.03148
    norm 8.28858 3.27812 -4.53357
    pos 13.9092 -14.4893 9.48218
    norm 5.2732 8.28882 -1.86783
  }
  poly {
    numVertices 3
    pos 14.2157 -15.1248 9.03148
    norm 8.28858 3.27812 -4.53357
    pos 14.7716 -14.9307 10.6684
    norm 8.60765 3.63719 -3.56081
    pos 14.6449 -15.9389 9.7333
    norm 8.68951 -3.85237 -3.10672
  }
  poly {
    numVertices 3
    pos -2.04821 -8.4586 4.76938
    norm -4.59977 2.26527 8.58549
    pos -2.84257 -6.72762 3.60283
    norm -3.14435 3.20493 8.93541
    pos -2.97108 -8.36129 4.09266
    norm -5.79013 2.12898 7.87031
  }
  poly {
    numVertices 3
    pos -2.84257 -6.72762 3.60283
    norm -3.14435 3.20493 8.93541
    pos -2.04821 -8.4586 4.76938
    norm -4.59977 2.26527 8.58549
    pos -2.1032 -6.8853 4.14129
    norm -3.60766 4.55602 8.13803
  }
  poly {
    numVertices 3
    pos 1.95109 -12.8602 0.366746
    norm 2.67699 -7.63905 -5.87185
    pos 1.10714 -12.3829 -1.0688
    norm 3.98915 -6.74336 -6.21399
    pos 2.41549 -12.4938 0.186379
    norm 0.631879 -6.67923 -7.41543
  }
  poly {
    numVertices 3
    pos 6.64033 -18.4669 3.05128
    norm 4.54137 -1.31627 8.81155
    pos 7.37924 -18.6891 2.15138
    norm 9.23978 -1.9749 3.27511
    pos 6.65273 -17.2559 2.73433
    norm 6.34607 3.39605 6.94221
  }
  poly {
    numVertices 3
    pos 7.37924 -18.6891 2.15138
    norm 9.23978 -1.9749 3.27511
    pos 6.64033 -18.4669 3.05128
    norm 4.54137 -1.31627 8.81155
    pos 6.74407 -19.04 2.64974
    norm 3.60163 -7.088 6.06535
  }
  poly {
    numVertices 3
    pos -5.94909 4.15499 -1.51447
    norm -5.55342 2.42297 -7.95543
    pos -6.51485 0.20576 -2.34626
    norm -3.54166 2.09553 -9.11402
    pos -6.5942 1.11603 -1.93166
    norm -9.33229 1.76466 -3.12959
  }
  poly {
    numVertices 3
    pos -6.51485 0.20576 -2.34626
    norm -3.54166 2.09553 -9.11402
    pos -5.94909 4.15499 -1.51447
    norm -5.55342 2.42297 -7.95543
    pos -6.03612 2.2523 -2.00852
    norm -1.11556 2.14083 -9.70424
  }
  poly {
    numVertices 3
    pos -5.42746 10.2895 -0.392424
    norm -5.64918 2.11673 -7.97536
    pos -4.56752 7.83438 -0.79817
    norm 3.64294 1.59626 -9.17502
    pos -5.48677 8.83448 -0.682687
    norm -6.53133 2.0409 -7.29221
  }
  poly {
    numVertices 3
    pos -4.56752 7.83438 -0.79817
    norm 3.64294 1.59626 -9.17502
    pos -5.42746 10.2895 -0.392424
    norm -5.64918 2.11673 -7.97536
    pos -4.24376 10.6749 -0.148825
    norm 6.01758 1.29344 -7.88135
  }
  poly {
    numVertices 3
    pos 3.48108 -12.0348 0.0176834
    norm -2.71987 -1.45157 -9.5129
    pos 4.53682 -11.5524 -0.407543
    norm -0.730066 2.46174 -9.66472
    pos 4.23647 -12.5242 -0.434443
    norm -2.77891 -0.109656 -9.6055
  }
  poly {
    numVertices 3
    pos 7.70443 -10.2055 4.42118
    norm 7.85518 5.2371 -3.2968
    pos 5.47074 -7.06492 4.41854
    norm 6.9719 7.1489 -0.534642
    pos 7.24279 -9.05532 5.38874
    norm 6.98985 7.02064 1.36114
  }
  poly {
    numVertices 3
    pos 5.47074 -7.06492 4.41854
    norm 6.9719 7.1489 -0.534642
    pos 7.70443 -10.2055 4.42118
    norm 7.85518 5.2371 -3.2968
    pos 6.24673 -8.40136 3.8262
    norm 7.35641 6.3507 -2.35625
  }
  poly {
    numVertices 3
    pos -1.62321 -13.1215 -3.66567
    norm 8.23487 0.150715 -5.67135
    pos -1.39263 -12.5652 -3.23511
    norm 9.35587 -1.879 -2.9895
    pos -1.60103 -14.4594 -3.38552
    norm 9.60254 -2.66033 -0.844921
  }
  poly {
    numVertices 3
    pos -1.39263 -12.5652 -3.23511
    norm 9.35587 -1.879 -2.9895
    pos -1.62321 -13.1215 -3.66567
    norm 8.23487 0.150715 -5.67135
    pos -1.29724 -10.813 -3.30765
    norm 6.55269 0.325613 -7.54693
  }
  poly {
    numVertices 3
    pos -3.25182 -13.8052 3.20214
    norm 7.13934 -1.76287 6.77659
    pos -3.6129 -16.5015 1.63802
    norm 9.68138 -1.39526 2.07945
    pos -3.00445 -13.7999 2.7631
    norm 9.2791 -1.71494 3.31018
  }
  poly {
    numVertices 3
    pos -3.6129 -16.5015 1.63802
    norm 9.68138 -1.39526 2.07945
    pos -3.25182 -13.8052 3.20214
    norm 7.13934 -1.76287 6.77659
    pos -3.65413 -15.3926 2.68235
    norm 6.94342 -2.63732 6.69577
  }
  poly {
    numVertices 3
    pos 4.7818 -18.0847 2.10223
    norm -9.96826 0.236757 0.760023
    pos 3.74017 -14.8183 1.58109
    norm -8.79283 -3.26142 3.47121
    pos 4.51454 -17.2297 1.45603
    norm -9.2191 -3.76161 -0.926524
  }
  poly {
    numVertices 3
    pos 3.74017 -14.8183 1.58109
    norm -8.79283 -3.26142 3.47121
    pos 4.7818 -18.0847 2.10223
    norm -9.96826 0.236757 0.760023
    pos 4.40375 -16.8088 2.12267
    norm -8.71396 -2.20582 4.38192
  }
  poly {
    numVertices 3
    pos 6.87304 -19.4288 0.605895
    norm 6.27903 -7.50803 -2.05021
    pos 4.91991 -19.4177 1.19631
    norm -0.823745 -9.91614 -0.995827
    pos 6.02434 -19.4702 -0.272587
    norm 0.540327 -4.5337 -8.89683
  }
  poly {
    numVertices 3
    pos -6.70611 -10.0692 0.710534
    norm -9.76272 -0.789839 -2.01628
    pos -5.87452 -11.0919 -1.04466
    norm -8.41081 -3.54622 -4.08445
    pos -6.31301 -10.9601 0.0356826
    norm -8.95624 -4.11627 -1.68584
  }
  poly {
    numVertices 3
    pos -5.87452 -11.0919 -1.04466
    norm -8.41081 -3.54622 -4.08445
    pos -6.70611 -10.0692 0.710534
    norm -9.76272 -0.789839 -2.01628
    pos -6.22047 -9.19181 -0.776008
    norm -9.05972 0.412972 -4.2132
  }
  poly {
    numVertices 3
    pos 5.07755 -13.7669 -0.265191
    norm 4.77252 -0.149132 -8.7864
    pos 4.97961 -15.3822 -0.338328
    norm 2.2475 -1.2381 -9.66518
    pos 4.65564 -13.6955 -0.393571
    norm 0.1017 -0.68103 -9.97626
  }
  poly {
    numVertices 3
    pos 4.97961 -15.3822 -0.338328
    norm 2.2475 -1.2381 -9.66518
    pos 5.07755 -13.7669 -0.265191
    norm 4.77252 -0.149132 -8.7864
    pos 5.36113 -14.5588 -0.166576
    norm 6.22493 0.314549 -7.81993
  }
  poly {
    numVertices 3
    pos -4.42361 -18.4625 3.06688
    norm 1.2685 -1.31164 9.83212
    pos -5.76928 -18.8956 2.82659
    norm -6.02046 0.817659 7.94264
    pos -5.58623 -19.4082 2.71577
    norm -2.11591 -7.41205 6.37059
  }
  poly {
    numVertices 3
    pos -5.76928 -18.8956 2.82659
    norm -6.02046 0.817659 7.94264
    pos -4.42361 -18.4625 3.06688
    norm 1.2685 -1.31164 9.83212
    pos -4.98335 -18.4228 2.99007
    norm -2.65841 1.1534 9.57092
  }
  poly {
    numVertices 3
    pos 9.22152 -11.3098 6.48667
    norm 3.88048 6.24039 6.78228
    pos 10.702 -14.262 7.7392
    norm -3.33812 2.62744 9.05282
    pos 10.3878 -12.8744 7.21761
    norm 1.7332 6.56232 7.34384
  }
  poly {
    numVertices 3
    pos 10.702 -14.262 7.7392
    norm -3.33812 2.62744 9.05282
    pos 9.22152 -11.3098 6.48667
    norm 3.88048 6.24039 6.78228
    pos 9.68214 -12.6627 7.02945
    norm -0.82539 3.09652 9.47261
  }
  poly {
    numVertices 3
    pos 6.52046 -10.2486 6.39982
    norm 2.01654 3.13315 9.27992
    pos 5.00063 -8.51581 5.97394
    norm 2.33725 5.28274 8.16272
    pos 4.646 -9.84243 6.84369
    norm 1.07643 3.00155 9.47797
  }
  poly {
    numVertices 3
    pos 1.13881 -6.01673 4.42871
    norm -0.628232 6.48513 7.58607
    pos -0.0299261 -7.59195 5.2301
    norm -2.19697 3.87407 8.95349
    pos 1.40944 -7.87463 5.54854
    norm -2.27225 4.64026 8.56183
  }
  poly {
    numVertices 3
    pos -0.0299261 -7.59195 5.2301
    norm -2.19697 3.87407 8.95349
    pos 1.13881 -6.01673 4.42871
    norm -0.628232 6.48513 7.58607
    pos 0.118262 -6.74988 4.74419
    norm -2.00505 5.46897 8.12836
  }
  poly {
    numVertices 3
    pos 8.86135 -15.5277 6.3165
    norm -4.56015 -8.37189 3.01935
    pos 5.73481 -13.7659 5.83379
    norm -4.08864 -7.10352 5.72914
    pos 5.88352 -14.1734 4.86874
    norm -0.798628 -9.82047 1.70898
  }
  poly {
    numVertices 3
    pos 5.73481 -13.7659 5.83379
    norm -4.08864 -7.10352 5.72914
    pos 8.86135 -15.5277 6.3165
    norm -4.56015 -8.37189 3.01935
    pos 8.55083 -15.1088 6.68321
    norm -4.25555 -5.58175 7.12282
  }
  poly {
    numVertices 3
    pos 3.48108 -12.0348 0.0176834
    norm -2.71987 -1.45157 -9.5129
    pos 2.41549 -12.4938 0.186379
    norm 0.631879 -6.67923 -7.41543
    pos 3.03443 -11.4539 -0.388693
    norm 5.80919 -3.96849 -7.10665
  }
  poly {
    numVertices 3
    pos 2.41549 -12.4938 0.186379
    norm 0.631879 -6.67923 -7.41543
    pos 3.48108 -12.0348 0.0176834
    norm -2.71987 -1.45157 -9.5129
    pos 3.18722 -12.7883 0.396544
    norm -4.30866 -3.80774 -8.18148
  }
  poly {
    numVertices 3
    pos -5.48677 8.83448 -0.682687
    norm -6.53133 2.0409 -7.29221
    pos -6.38869 3.01368 -0.701171
    norm -9.91581 1.2354 0.387893
    pos -5.8495 7.29778 -0.182433
    norm -9.89567 1.10332 -0.926523
  }
  poly {
    numVertices 3
    pos -6.38869 3.01368 -0.701171
    norm -9.91581 1.2354 0.387893
    pos -5.48677 8.83448 -0.682687
    norm -6.53133 2.0409 -7.29221
    pos -6.18026 4.11301 -1.09287
    norm -9.43279 1.84354 -2.76114
  }
  poly {
    numVertices 3
    pos 1.69011 -10.2507 6.73267
    norm -3.88336 2.71503 8.80615
    pos 0.740866 -12.1068 6.69971
    norm -4.56911 0.0836025 8.89473
    pos 1.51897 -11.4031 6.89312
    norm -1.99592 0.663922 9.77627
  }
  poly {
    numVertices 3
    pos 0.740866 -12.1068 6.69971
    norm -4.56911 0.0836025 8.89473
    pos 1.69011 -10.2507 6.73267
    norm -3.88336 2.71503 8.80615
    pos 1.12225 -10.7054 6.58825
    norm -5.91535 2.71199 7.59301
  }
  poly {
    numVertices 3
    pos -1.12156 -13.2487 1.61868
    norm -1.99203 -9.76177 0.859989
    pos 2.31774 -13.6426 2.28765
    norm -2.74809 -9.60543 0.428646
    pos 1.99368 -13.5825 3.13191
    norm -1.53619 -9.86583 -0.552767
  }
  poly {
    numVertices 3
    pos -1.12156 -13.2487 1.61868
    norm -1.99203 -9.76177 0.859989
    pos 2.5949 -13.6132 1.74831
    norm -3.32224 -9.38673 -0.923094
    pos 2.31774 -13.6426 2.28765
    norm -2.74809 -9.60543 0.428646
  }
  poly {
    numVertices 3
    pos 2.5949 -13.6132 1.74831
    norm -3.32224 -9.38673 -0.923094
    pos -1.12156 -13.2487 1.61868
    norm -1.99203 -9.76177 0.859989
    pos 0.556453 -13.4349 1.53279
    norm -0.476012 -9.90126 -1.3185
  }
  poly {
    numVertices 3
    pos -4.33866 -16.9699 -2.98265
    norm 3.10092 -3.56512 8.8133
    pos -3.67895 -14.6637 -2.20106
    norm -1.54476 -2.89539 9.44619
    pos -4.859 -16.7585 -2.9291
    norm -4.02286 -1.00849 9.09942
  }
  poly {
    numVertices 3
    pos 12.1298 -13.7766 7.55099
    norm 4.79951 8.77215 0.118892
    pos 12.9284 -14.287 9.09242
    norm -1.95407 8.78251 4.36453
    pos 13.9092 -14.4893 9.48218
    norm 5.2732 8.28882 -1.86783
  }
  poly {
    numVertices 3
    pos 12.9284 -14.287 9.09242
    norm -1.95407 8.78251 4.36453
    pos 12.1298 -13.7766 7.55099
    norm 4.79951 8.77215 0.118892
    pos 11.8032 -13.8701 8.00983
    norm -1.52941 7.19719 6.77211
  }
  poly {
    numVertices 3
    pos 14.2157 -15.1248 9.03148
    norm 8.28858 3.27812 -4.53357
    pos 13.2641 -15.5252 7.47795
    norm 7.69544 0.144981 -6.38429
    pos 13.4579 -15.1211 7.84139
    norm 7.34013 4.49935 -5.08707
  }
  poly {
    numVertices 3
    pos 13.2641 -15.5252 7.47795
    norm 7.69544 0.144981 -6.38429
    pos 14.2157 -15.1248 9.03148
    norm 8.28858 3.27812 -4.53357
    pos 13.8795 -15.9158 8.35377
    norm 7.85484 -2.88631 -5.47456
  }
  poly {
    numVertices 3
    pos -1.5737 -12.207 3.74263
    norm -3.70792 -7.70746 5.18136
    pos -2.62799 -12.7573 1.64045
    norm 3.08013 -8.67321 3.91002
    pos -1.20858 -12.8907 3.09074
    norm -3.10844 -8.89057 3.36085
  }
  poly {
    numVertices 3
    pos -2.62799 -12.7573 1.64045
    norm 3.08013 -8.67321 3.91002
    pos -1.5737 -12.207 3.74263
    norm -3.70792 -7.70746 5.18136
    pos -2.83174 -11.4541 3.35851
    norm -4.15278 -6.81769 6.02275
  }
  poly {
    numVertices 3
    pos 8.02126 -13.0395 3.8613
    norm 4.5653 -0.643587 -8.87377
    pos 10.376 -14.037 5.24704
    norm 6.03574 2.03773 -7.70827
    pos 10.4382 -14.8406 5.17183
    norm 4.54151 -1.39927 -8.79868
  }
  poly {
    numVertices 3
    pos 10.376 -14.037 5.24704
    norm 6.03574 2.03773 -7.70827
    pos 8.02126 -13.0395 3.8613
    norm 4.5653 -0.643587 -8.87377
    pos 9.04117 -13.1318 4.51572
    norm 6.25962 1.81214 -7.58507
  }
  poly {
    numVertices 3
    pos 3.67478 -11.0294 0.137297
    norm 0.86657 -0.833314 -9.92747
    pos 4.53682 -11.5524 -0.407543
    norm -0.730066 2.46174 -9.66472
    pos 3.48108 -12.0348 0.0176834
    norm -2.71987 -1.45157 -9.5129
  }
  poly {
    numVertices 3
    pos 4.53682 -11.5524 -0.407543
    norm -0.730066 2.46174 -9.66472
    pos 3.67478 -11.0294 0.137297
    norm 0.86657 -0.833314 -9.92747
    pos 3.82521 -11.0628 0.0781074
    norm -1.34017 -0.867301 -9.87176
  }
  poly {
    numVertices 3
    pos -1.5737 -12.207 3.74263
    norm -3.70792 -7.70746 5.18136
    pos 0.157518 -12.7358 3.98195
    norm -9.20241 -3.80209 -0.9273
    pos 0.0136405 -12.2344 4.57136
    norm -8.80427 -3.52215 3.1748
  }
  poly {
    numVertices 3
    pos 14.6149 -14.0403 13.3443
    norm 4.62677 8.73814 1.49593
    pos 15.2959 -14.58 13.1373
    norm 9.41907 2.89557 1.70202
    pos 14.2153 -13.7597 12.7611
    norm -2.9103 9.56188 -0.317319
  }
  poly {
    numVertices 3
    pos 15.2959 -14.58 13.1373
    norm 9.41907 2.89557 1.70202
    pos 14.6149 -14.0403 13.3443
    norm 4.62677 8.73814 1.49593
    pos 15.1075 -14.5324 13.4979
    norm 5.98485 2.23643 7.69285
  }
  poly {
    numVertices 3
    pos -5.96826 -7.88366 2.26762
    norm -7.97156 2.15979 5.63823
    pos -5.60496 -6.43976 1.71988
    norm -7.91846 4.27069 4.36567
    pos -6.16766 -7.5436 1.48804
    norm -9.17332 3.01716 2.59748
  }
  poly {
    numVertices 3
    pos -5.60496 -6.43976 1.71988
    norm -7.91846 4.27069 4.36567
    pos -5.96826 -7.88366 2.26762
    norm -7.97156 2.15979 5.63823
    pos -5.33063 -6.746 2.43856
    norm -6.81865 4.05886 6.08536
  }
  poly {
    numVertices 3
    pos 0.453254 -19.2285 6.45999
    norm -1.44472 -4.54158 8.7913
    pos -0.799095 -19.4412 5.65921
    norm -5.07439 -7.78489 3.69407
    pos 0.426079 -19.5653 6.04336
    norm -0.223378 -9.64842 2.61882
  }
  poly {
    numVertices 3
    pos -0.799095 -19.4412 5.65921
    norm -5.07439 -7.78489 3.69407
    pos 0.453254 -19.2285 6.45999
    norm -1.44472 -4.54158 8.7913
    pos -0.527417 -19.112 6.20347
    norm -5.13615 -0.573981 8.56099
  }
  poly {
    numVertices 3
    pos -1.78565 -5.33574 3.16769
    norm -1.93798 5.95125 7.79915
    pos -1.6586 -4.1407 2.12365
    norm -0.0900502 7.82454 6.22644
    pos -2.58326 -5.15962 2.93761
    norm -1.51553 6.20177 7.69684
  }
  poly {
    numVertices 3
    pos -1.6586 -4.1407 2.12365
    norm -0.0900502 7.82454 6.22644
    pos -1.78565 -5.33574 3.16769
    norm -1.93798 5.95125 7.79915
    pos -1.08883 -4.78023 2.76778
    norm 0.0327969 7.6055 6.49271
  }
  poly {
    numVertices 3
    pos -6.77095 -2.81837 -2.24608
    norm -9.63537 -1.14348 -2.41912
    pos -6.70153 0.32487 -0.818477
    norm -9.77228 0.453305 2.0729
    pos -6.76337 -1.47748 -2.52411
    norm -6.89324 0.413042 -7.23276
  }
  poly {
    numVertices 3
    pos -6.70153 0.32487 -0.818477
    norm -9.77228 0.453305 2.0729
    pos -6.77095 -2.81837 -2.24608
    norm -9.63537 -1.14348 -2.41912
    pos -6.73434 -1.6125 -1.00367
    norm -9.57322 -0.188188 2.88409
  }
  poly {
    numVertices 3
    pos -4.02677 9.63628 -0.007707
    norm 8.11428 1.02779 -5.75344
    pos -4.21069 6.00638 -0.841693
    norm 6.98346 0.861646 -7.10555
    pos -4.56752 7.83438 -0.79817
    norm 3.64294 1.59626 -9.17502
  }
  poly {
    numVertices 3
    pos -4.21069 6.00638 -0.841693
    norm 6.98346 0.861646 -7.10555
    pos -3.76933 8.38318 0.0669733
    norm 9.7092 0.543715 -2.33146
    pos -3.89908 6.96278 -0.342101
    norm 9.45917 0.280449 -3.23195
  }
  poly {
    numVertices 3
    pos -3.76933 8.38318 0.0669733
    norm 9.7092 0.543715 -2.33146
    pos -4.21069 6.00638 -0.841693
    norm 6.98346 0.861646 -7.10555
    pos -4.02677 9.63628 -0.007707
    norm 8.11428 1.02779 -5.75344
  }
  poly {
    numVertices 3
    pos 14.2199 -14.1732 12.2989
    norm -5.06096 8.6173 0.359022
    pos 14.9597 -14.3451 12.6113
    norm 6.04957 7.51608 2.62892
    pos 14.4033 -14.0152 11.9425
    norm -0.259256 9.99522 -0.168179
  }
  poly {
    numVertices 3
    pos 14.9597 -14.3451 12.6113
    norm 6.04957 7.51608 2.62892
    pos 14.2199 -14.1732 12.2989
    norm -5.06096 8.6173 0.359022
    pos 15.2959 -14.58 13.1373
    norm 9.41907 2.89557 1.70202
  }
  poly {
    numVertices 3
    pos 5.14061 -18.4373 2.96289
    norm -5.53908 0.70348 8.29601
    pos 4.18665 -18.8733 1.83591
    norm -9.04527 2.70881 3.29324
    pos 4.68402 -18.8672 2.53061
    norm -6.64904 -2.30815 7.10371
  }
  poly {
    numVertices 3
    pos 4.18665 -18.8733 1.83591
    norm -9.04527 2.70881 3.29324
    pos 5.14061 -18.4373 2.96289
    norm -5.53908 0.70348 8.29601
    pos 4.96878 -17.7485 2.58786
    norm -8.08089 0.867681 5.82636
  }
  poly {
    numVertices 3
    pos 6.28983 -14.3377 1.44391
    norm 9.67517 -2.50103 0.368631
    pos 6.34665 -13.8848 3.30822
    norm 6.26657 -7.77736 0.492668
    pos 5.73658 -14.7578 2.50278
    norm 7.11003 -3.92935 5.83162
  }
  poly {
    numVertices 3
    pos 6.34665 -13.8848 3.30822
    norm 6.26657 -7.77736 0.492668
    pos 6.28983 -14.3377 1.44391
    norm 9.67517 -2.50103 0.368631
    pos 6.56015 -13.7083 2.5919
    norm 8.45985 -5.23714 -1.00163
  }
  poly {
    numVertices 3
    pos -5.39593 -19.2514 -6.0843
    norm -4.47909 -1.31291 -8.84386
    pos -4.15299 -19.3924 -5.7299
    norm 3.34049 -8.72104 -3.57554
    pos -4.49025 -19.4335 -5.45155
    norm 3.35634 -9.41696 0.236478
  }
  poly {
    numVertices 3
    pos -4.15299 -19.3924 -5.7299
    norm 3.34049 -8.72104 -3.57554
    pos -5.39593 -19.2514 -6.0843
    norm -4.47909 -1.31291 -8.84386
    pos -4.7385 -19.2777 -5.96447
    norm -1.84915 -5.05636 -8.42698
  }
  poly {
    numVertices 3
    pos -6.22047 -9.19181 -0.776008
    norm -9.05972 0.412972 -4.2132
    pos -5.40757 -11.031 -1.79466
    norm -7.23615 -4.23949 -5.44654
    pos -5.87452 -11.0919 -1.04466
    norm -8.41081 -3.54622 -4.08445
  }
  poly {
    numVertices 3
    pos -5.40757 -11.031 -1.79466
    norm -7.23615 -4.23949 -5.44654
    pos -6.22047 -9.19181 -0.776008
    norm -9.05972 0.412972 -4.2132
    pos -5.67109 -9.58226 -1.54096
    norm -8.73711 0.174782 -4.86131
  }
  poly {
    numVertices 3
    pos 5.8825 -11.6913 6.83246
    norm 0.0785027 -0.309978 9.99488
    pos 8.83447 -11.2836 6.62974
    norm 1.89934 4.28739 8.83237
    pos 6.52046 -10.2486 6.39982
    norm 2.01654 3.13315 9.27992
  }
  poly {
    numVertices 3
    pos 8.83447 -11.2836 6.62974
    norm 1.89934 4.28739 8.83237
    pos 5.8825 -11.6913 6.83246
    norm 0.0785027 -0.309978 9.99488
    pos 6.70286 -11.8926 6.86067
    norm -1.18294 -0.156284 9.92856
  }
  poly {
    numVertices 3
    pos -4.53285 -19.0919 -6.25921
    norm 3.24862 -1.65157 -9.31229
    pos -3.56044 -18.5697 -5.15075
    norm 9.07698 -2.52043 -3.35499
    pos -4.15299 -19.3924 -5.7299
    norm 3.34049 -8.72104 -3.57554
  }
  poly {
    numVertices 3
    pos -3.56044 -18.5697 -5.15075
    norm 9.07698 -2.52043 -3.35499
    pos -4.53285 -19.0919 -6.25921
    norm 3.24862 -1.65157 -9.31229
    pos -3.85971 -18.3206 -5.59526
    norm 5.88002 2.64735 -7.64309
  }
  poly {
    numVertices 3
    pos 5.88352 -14.1734 4.86874
    norm -0.798628 -9.82047 1.70898
    pos 9.16125 -15.5749 5.49072
    norm -1.40995 -8.29987 -5.39669
    pos 8.86135 -15.5277 6.3165
    norm -4.56015 -8.37189 3.01935
  }
  poly {
    numVertices 3
    pos -0.89814 -5.35791 -2.07046
    norm 4.42295 5.83826 -6.80824
    pos -0.302616 -7.1779 -2.58596
    norm 5.3647 2.43245 -8.08104
    pos -1.39952 -6.08959 -2.76028
    norm 3.11769 4.21128 -8.51734
  }
  poly {
    numVertices 3
    pos -0.302616 -7.1779 -2.58596
    norm 5.3647 2.43245 -8.08104
    pos -0.89814 -5.35791 -2.07046
    norm 4.42295 5.83826 -6.80824
    pos 0.740215 -7.15194 -1.76874
    norm 5.19378 4.05653 -7.52126
  }
  poly {
    numVertices 3
    pos -3.66308 -6.70622 3.43613
    norm -3.22591 1.59914 9.32932
    pos -5.48614 -7.78082 2.73145
    norm -5.5324 1.47646 8.19833
    pos -4.83457 -7.89267 3.07841
    norm -3.74292 0.659117 9.24966
  }
  poly {
    numVertices 3
    pos -5.48614 -7.78082 2.73145
    norm -5.5324 1.47646 8.19833
    pos -3.66308 -6.70622 3.43613
    norm -3.22591 1.59914 9.32932
    pos -4.26162 -6.14556 2.99808
    norm -5.36902 4.23534 7.29626
  }
  poly {
    numVertices 3
    pos -5.60496 -6.43976 1.71988
    norm -7.91846 4.27069 4.36567
    pos -6.06486 -6.60961 0.267663
    norm -9.84512 1.30847 1.16685
    pos -6.16766 -7.5436 1.48804
    norm -9.17332 3.01716 2.59748
  }
  poly {
    numVertices 3
    pos -6.06486 -6.60961 0.267663
    norm -9.84512 1.30847 1.16685
    pos -5.60496 -6.43976 1.71988
    norm -7.91846 4.27069 4.36567
    pos -5.50148 -5.66874 1.22353
    norm -7.83408 2.50715 5.68695
  }
  poly {
    numVertices 3
    pos 4.32077 -19.1593 0.92638
    norm -5.76429 -3.76151 -7.25424
    pos 6.02434 -19.4702 -0.272587
    norm 0.540327 -4.5337 -8.89683
    pos 4.91991 -19.4177 1.19631
    norm -0.823745 -9.91614 -0.995827
  }
  poly {
    numVertices 3
    pos 6.02434 -19.4702 -0.272587
    norm 0.540327 -4.5337 -8.89683
    pos 4.32077 -19.1593 0.92638
    norm -5.76429 -3.76151 -7.25424
    pos 4.74128 -19.0837 0.354702
    norm -6.12297 3.6265 -7.0255
  }
  poly {
    numVertices 3
    pos 1.10714 -12.3829 -1.0688
    norm 3.98915 -6.74336 -6.21399
    pos 2.28293 -13.2826 1.18152
    norm -0.148885 -8.9958 -4.36503
    pos 0.37187 -12.7397 -0.903383
    norm 0.748865 -7.68155 -6.35869
  }
  poly {
    numVertices 3
    pos 2.28293 -13.2826 1.18152
    norm -0.148885 -8.9958 -4.36503
    pos 1.10714 -12.3829 -1.0688
    norm 3.98915 -6.74336 -6.21399
    pos 1.95109 -12.8602 0.366746
    norm 2.67699 -7.63905 -5.87185
  }
  poly {
    numVertices 3
    pos 10.3878 -12.8744 7.21761
    norm 1.7332 6.56232 7.34384
    pos 12.1298 -13.7766 7.55099
    norm 4.79951 8.77215 0.118892
    pos 10.6095 -12.6927 6.73842
    norm 6.83096 7.30238 0.115532
  }
  poly {
    numVertices 3
    pos 12.1298 -13.7766 7.55099
    norm 4.79951 8.77215 0.118892
    pos 10.3878 -12.8744 7.21761
    norm 1.7332 6.56232 7.34384
    pos 11.8032 -13.8701 8.00983
    norm -1.52941 7.19719 6.77211
  }
  poly {
    numVertices 3
    pos -1.5737 -12.207 3.74263
    norm -3.70792 -7.70746 5.18136
    pos -1.41203 -11.4555 4.84936
    norm -3.54584 -5.00315 7.89908
    pos -2.08653 -11.8198 4.08392
    norm -5.37258 -6.81002 4.97583
  }
  poly {
    numVertices 3
    pos -1.41203 -11.4555 4.84936
    norm -3.54584 -5.00315 7.89908
    pos -1.5737 -12.207 3.74263
    norm -3.70792 -7.70746 5.18136
    pos -0.440669 -11.6301 4.8394
    norm -3.31797 -6.12626 7.17356
  }
  poly {
    numVertices 3
    pos -3.68464 -15.8654 -4.90982
    norm 2.47642 1.428 -9.5827
    pos -3.08106 -13.6066 -4.46041
    norm -0.742782 2.22322 -9.7214
    pos -3.27777 -14.6089 -4.68069
    norm 2.48977 0.722908 -9.65808
  }
  poly {
    numVertices 3
    pos -3.08106 -13.6066 -4.46041
    norm -0.742782 2.22322 -9.7214
    pos -3.68464 -15.8654 -4.90982
    norm 2.47642 1.428 -9.5827
    pos -4.1396 -14.962 -4.7755
    norm -2.63305 3.36237 -9.04221
  }
  poly {
    numVertices 3
    pos 1.10714 -12.3829 -1.0688
    norm 3.98915 -6.74336 -6.21399
    pos 3.03443 -11.4539 -0.388693
    norm 5.80919 -3.96849 -7.10665
    pos 2.41549 -12.4938 0.186379
    norm 0.631879 -6.67923 -7.41543
  }
  poly {
    numVertices 3
    pos 3.03443 -11.4539 -0.388693
    norm 5.80919 -3.96849 -7.10665
    pos 1.10714 -12.3829 -1.0688
    norm 3.98915 -6.74336 -6.21399
    pos 2.05608 -11.3913 -1.31843
    norm 5.36314 -4.74617 -6.9793
  }
  poly {
    numVertices 3
    pos 6.89929 -16.9192 1.47205
    norm 9.5297 2.97301 -0.588242
    pos 6.10929 -14.7403 0.973346
    norm 9.68675 0.815143 -2.34573
    pos 6.49118 -15.5662 1.5058
    norm 9.47453 2.22726 2.2962
  }
  poly {
    numVertices 3
    pos 6.10929 -14.7403 0.973346
    norm 9.68675 0.815143 -2.34573
    pos 6.89929 -16.9192 1.47205
    norm 9.5297 2.97301 -0.588242
    pos 6.09135 -15.1202 0.594855
    norm 8.4985 0.981692 -5.17801
  }
  poly {
    numVertices 3
    pos 3.94738 -5.90251 3.77839
    norm 4.66748 8.84387 -0.0253589
    pos -2.47359 -3.30754 -0.0466174
    norm 6.20974 7.66014 -1.66176
    pos -2.13791 -3.39392 0.722582
    norm 2.8227 9.03571 3.2231
  }
  poly {
    numVertices 3
    pos -2.47359 -3.30754 -0.0466174
    norm 6.20974 7.66014 -1.66176
    pos 3.94738 -5.90251 3.77839
    norm 4.66748 8.84387 -0.0253589
    pos -1.10285 -3.98427 0.285084
    norm 5.66868 7.07042 -4.22792
  }
  poly {
    numVertices 3
    pos -5.16035 -3.39729 0.676894
    norm -2.36296 3.29657 9.14051
    pos -5.87569 -2.37263 0.248807
    norm -4.55692 1.03449 8.84106
    pos -5.74712 -3.53181 0.511146
    norm -5.86776 2.13709 7.8104
  }
  poly {
    numVertices 3
    pos -5.87569 -2.37263 0.248807
    norm -4.55692 1.03449 8.84106
    pos -5.16035 -3.39729 0.676894
    norm -2.36296 3.29657 9.14051
    pos -5.04454 -1.96564 0.431794
    norm -1.1306 1.1066 9.87407
  }
  poly {
    numVertices 3
    pos 1.41579 -13.7077 6.69625
    norm -0.879791 -2.42279 9.66209
    pos 0.740866 -12.1068 6.69971
    norm -4.56911 0.0836025 8.89473
    pos 0.702972 -14.1291 6.34669
    norm -4.11256 -0.320562 9.10956
  }
  poly {
    numVertices 3
    pos 0.740866 -12.1068 6.69971
    norm -4.56911 0.0836025 8.89473
    pos 1.41579 -13.7077 6.69625
    norm -0.879791 -2.42279 9.66209
    pos 1.3167 -12.7386 6.82562
    norm -2.05511 -0.887939 9.74619
  }
  poly {
    numVertices 3
    pos -4.73337 -17.0081 2.82271
    norm -1.32038 -0.222874 9.90994
    pos -5.18117 -14.6785 3.00876
    norm -5.47977 -0.386899 8.35599
    pos -5.52638 -15.6086 2.63084
    norm -8.20097 0.228738 5.71767
  }
  poly {
    numVertices 3
    pos -5.18117 -14.6785 3.00876
    norm -5.47977 -0.386899 8.35599
    pos -4.73337 -17.0081 2.82271
    norm -1.32038 -0.222874 9.90994
    pos -4.89572 -15.6929 2.96688
    norm -1.82954 -1.64476 9.69266
  }
  poly {
    numVertices 3
    pos 0.426079 -19.5653 6.04336
    norm -0.223378 -9.64842 2.61882
    pos 1.58199 -19.4485 5.60605
    norm 6.96701 -7.1643 -0.365442
    pos 1.34363 -19.5344 6.11335
    norm 5.19898 -5.25268 6.73646
  }
  poly {
    numVertices 3
    pos 3.92788 -7.08466 5.09628
    norm 1.54171 6.71857 7.24458
    pos 3.22428 -7.81218 6.12013
    norm 0.340707 5.13796 8.57235
    pos 5.00063 -8.51581 5.97394
    norm 2.33725 5.28274 8.16272
  }
  poly {
    numVertices 3
    pos 3.22428 -7.81218 6.12013
    norm 0.340707 5.13796 8.57235
    pos 3.92788 -7.08466 5.09628
    norm 1.54171 6.71857 7.24458
    pos 2.50165 -6.55031 4.875
    norm -0.209473 6.89509 7.23973
  }
  poly {
    numVertices 3
    pos -0.121226 -15.4517 5.7487
    norm -8.02474 -0.205263 5.96334
    pos -0.368209 -14.303 5.31731
    norm -9.65013 0.450162 2.58308
    pos -0.283247 -16.1208 5.31343
    norm -9.98621 0.0295973 0.524017
  }
  poly {
    numVertices 3
    pos -0.368209 -14.303 5.31731
    norm -9.65013 0.450162 2.58308
    pos -0.121226 -15.4517 5.7487
    norm -8.02474 -0.205263 5.96334
    pos -0.146719 -14.4176 5.83305
    norm -7.59837 0.152613 6.49934
  }
  poly {
    numVertices 3
    pos -5.58623 -19.4082 2.71577
    norm -2.11591 -7.41205 6.37059
    pos -3.73905 -18.7716 2.41767
    norm 7.38283 -3.70313 5.63743
    pos -4.42361 -18.4625 3.06688
    norm 1.2685 -1.31164 9.83212
  }
  poly {
    numVertices 3
    pos -3.73905 -18.7716 2.41767
    norm 7.38283 -3.70313 5.63743
    pos -5.58623 -19.4082 2.71577
    norm -2.11591 -7.41205 6.37059
    pos -4.94828 -19.5758 1.99663
    norm 0.481895 -9.91623 1.19837
  }
  poly {
    numVertices 3
    pos -3.21034 -7.7722 -3.72271
    norm 0.0363636 2.49318 -9.68415
    pos -4.16097 -8.86538 -3.81439
    norm -3.83121 1.62248 -9.09337
    pos -3.74502 -7.58815 -3.6016
    norm -4.07146 4.11782 -8.15272
  }
  poly {
    numVertices 3
    pos -4.16097 -8.86538 -3.81439
    norm -3.83121 1.62248 -9.09337
    pos -3.21034 -7.7722 -3.72271
    norm 0.0363636 2.49318 -9.68415
    pos -3.41935 -9.16366 -3.98414
    norm -0.0921583 0.99474 -9.94997
  }
  poly {
    numVertices 3
    pos -0.582583 -18.4332 5.84974
    norm -6.51616 5.90666 4.75933
    pos -0.347828 -17.881 5.04525
    norm -8.72187 4.83423 -0.747798
    pos -1.27763 -19.0498 4.83098
    norm -9.98953 -0.388803 0.240663
  }
  poly {
    numVertices 3
    pos -0.347828 -17.881 5.04525
    norm -8.72187 4.83423 -0.747798
    pos -0.582583 -18.4332 5.84974
    norm -6.51616 5.90666 4.75933
    pos -0.290922 -17.9807 5.59832
    norm -8.3273 2.79946 4.77694
  }
  poly {
    numVertices 3
    pos -2.77505 -15.7261 -3.26141
    norm 6.56821 -6.21017 4.27696
    pos -4.33866 -16.9699 -2.98265
    norm 3.10092 -3.56512 8.8133
    pos -3.74297 -16.894 -3.39166
    norm 6.85428 -3.50839 6.38045
  }
  poly {
    numVertices 3
    pos -4.33866 -16.9699 -2.98265
    norm 3.10092 -3.56512 8.8133
    pos -2.77505 -15.7261 -3.26141
    norm 6.56821 -6.21017 4.27696
    pos -2.19534 -14.5856 -2.5191
    norm 5.02069 -4.6074 7.31879
  }
  poly {
    numVertices 3
    pos -3.98486 -15.7726 0.775604
    norm 5.25154 0.418081 -8.49979
    pos -3.47869 -15.0434 1.36022
    norm 8.26886 -0.899591 -5.55128
    pos -3.63079 -16.1815 1.1078
    norm 9.03447 -0.0437649 -4.28677
  }
  poly {
    numVertices 3
    pos -3.47869 -15.0434 1.36022
    norm 8.26886 -0.899591 -5.55128
    pos -3.98486 -15.7726 0.775604
    norm 5.25154 0.418081 -8.49979
    pos -4.0326 -14.5984 0.816786
    norm 4.98562 -0.562871 -8.65025
  }
  poly {
    numVertices 3
    pos 0.426079 -19.5653 6.04336
    norm -0.223378 -9.64842 2.61882
    pos 1.11993 -19.3723 3.91824
    norm 4.53941 -5.72818 -6.82508
    pos 1.58199 -19.4485 5.60605
    norm 6.96701 -7.1643 -0.365442
  }
  poly {
    numVertices 3
    pos 1.11993 -19.3723 3.91824
    norm 4.53941 -5.72818 -6.82508
    pos 0.426079 -19.5653 6.04336
    norm -0.223378 -9.64842 2.61882
    pos -0.666849 -19.4694 4.75092
    norm -3.70606 -9.28357 -0.283884
  }
  poly {
    numVertices 3
    pos 0.634711 -13.1459 3.57186
    norm -5.46732 -6.82679 -4.84804
    pos -1.5737 -12.207 3.74263
    norm -3.70792 -7.70746 5.18136
    pos -1.20858 -12.8907 3.09074
    norm -3.10844 -8.89057 3.36085
  }
  poly {
    numVertices 3
    pos -1.5737 -12.207 3.74263
    norm -3.70792 -7.70746 5.18136
    pos 0.634711 -13.1459 3.57186
    norm -5.46732 -6.82679 -4.84804
    pos 0.157518 -12.7358 3.98195
    norm -9.20241 -3.80209 -0.9273
  }
  poly {
    numVertices 3
    pos -3.95801 -13.9431 -2.18399
    norm -7.31147 0.0977299 6.82151
    pos -4.163 -12.6847 -2.66679
    norm -9.92912 -1.18267 0.117671
    pos -4.21837 -13.639 -2.81587
    norm -9.71203 2.22499 0.851998
  }
  poly {
    numVertices 3
    pos -4.163 -12.6847 -2.66679
    norm -9.92912 -1.18267 0.117671
    pos -3.95801 -13.9431 -2.18399
    norm -7.31147 0.0977299 6.82151
    pos -3.8959 -13.1433 -2.06948
    norm -8.49338 -1.94664 4.90644
  }
}
poly_set {
  name ""
  numMaterials 1
  material {
    diffColor 0.139078 0.0374696 0.0374696
    ambColor 0.2 0.2 0.2
    specColor 0 0 0
    emisColor 0 0 0
    shininess 0.2
    ktran 0
  }
  type POLYSET_FACE_SET
  normType PER_FACE_NORMAL
  materialBinding PER_OBJECT_MATERIAL
  hasTextureCoords FALSE
  rowSize 0
  numPolys 6
  poly {
    numVertices 4
    pos -7.91866 10.6483 2.17569
    pos -7.62605 10.2379 2.20867
    pos -7.21445 10.5305 2.19799
    pos -7.50706 10.9409 2.16501
  }
  poly {
    numVertices 4
    pos -7.89365 10.5691 0.967308
    pos -7.48205 10.8617 0.956634
    pos -7.18944 10.4513 0.989611
    pos -7.60104 10.1587 1.00029
  }
  poly {
    numVertices 4
    pos -7.91866 10.6483 2.17569
    pos -7.89365 10.5691 0.967308
    pos -7.60104 10.1587 1.00029
    pos -7.62605 10.2379 2.20867
  }
  poly {
    numVertices 4
    pos -7.50706 10.9409 2.16501
    pos -7.21445 10.5305 2.19799
    pos -7.18944 10.4513 0.989611
    pos -7.48205 10.8617 0.956634
  }
  poly {
    numVertices 4
    pos -7.91866 10.6483 2.17569
    pos -7.50706 10.9409 2.16501
    pos -7.48205 10.8617 0.956634
    pos -7.89365 10.5691 0.967308
  }
  poly {
    numVertices 4
    pos -7.62605 10.2379 2.20867
    pos -7.60104 10.1587 1.00029
    pos -7.18944 10.4513 0.989611
    pos -7.21445 10.5305 2.19799
  }
}
sphere {
  name ""
  numMaterials 1
  material {
    diffColor 0.8 0.8 0.8
    ambColor 0.2 0.2 0.2
    specColor 0 0 0
    emisColor 0 0 0
    shininess 0.2
    ktran 0
  }
  origin -7.3805 11.659 2.54137
  radius 0.284694
  xaxis 1 0 0
  xlength 0.284694
  yaxis 0 1 0
  ylength 0.284694
  zaxis 0 0 1
  zlength 0.284694
}
sphere {
  name ""
  numMaterials 1
  material {
    diffColor 0.8 0.8 0.8
    ambColor 0.2 0.2 0.2
    specColor 0 0 0
    emisColor 0 0 0
    shininess 0.2
    ktran 0
  }
  origin -7.49789 12.021 0.594528
  radius 0.301258
  xaxis 1 0 0
  xlength 0.308174
  yaxis 0 1 0
  ylength 0.287426
  zaxis 0 0 1
  zlength 0.308174
}
sphere {
  name ""
  numMaterials 1
  material {
    diffColor 0.125405 0.0783784 0.0313514
    ambColor 0.2 0.2 0.2
    specColor 0 0 0
    emisColor 0 0 0
    shininess 0.2
    ktran 0
  }
  origin -6.98204 11.645 2.21658
  radius 0.688571
  xaxis 1 0 0
  xlength 0.688571
  yaxis 0 1 0
  ylength 0.688571
  zaxis 0 0 1
  zlength 0.688571
}
sphere {
  name ""
  numMaterials 1
  material {
    diffColor 0.0735135 0.0617911 0.0500687
    ambColor 0.2 0.2 0.2
    specColor 0 0 0
    emisColor 0 0 0
    shininess 0.2
    ktran 0
  }
  origin -7.04052 11.9415 0.543888
  radius 0.594069
  xaxis 1 0 0
  xlength 0.594069
  yaxis 0 1 0
  ylength 0.594069
  zaxis 0 0 1
  zlength 0.594069
}
sphere {
  name ""
  numMaterials 1
  material {
    diffColor 0.6 0.45 0.3
    ambColor 0.2 0.2 0.2
    specColor 0 0 0
    emisColor 0 0 0
    shininess 0.2
    ktran 0
  }
  origin -5.57544 11.1723 1.35784
  radius 2.13747
  xaxis 1 0 0
  xlength 2.13747
  yaxis 0 1 0
  ylength 2.13747
  zaxis 0 0 1
  zlength 2.13747
}";

	}
}

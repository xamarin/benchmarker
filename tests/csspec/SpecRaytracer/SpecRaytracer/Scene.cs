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
	private int LoadScene(String filename)
	{
		return LoadSceneOrig(filename);
	}

	private static String[] sceneLines = null;
	private static int scenePos = -1;

	private static String readString()
	{
		if(scenePos < sceneLines.Length)
		{
			String rp = sceneLines[scenePos];
			++scenePos;
			return rp;
		}
		else
			return null;
	}

	/**
	 * LoadSceneOrig
	 *
	 * @param filename
	 * @return int
	 */
	private int LoadSceneOrig(String filename)
	{
		sceneLines = File.ReadAllLines(filename);
		scenePos = 0;

		int numObj = 0, ObjID = 0;

		camera = null;
		lights = null;
		objects = null;
		materials = null;
		MaxX = MinX = MaxY = MinY = MaxZ = MinZ = 0.0f;
		String input;

		input = readString();

		while(input != null)
		{
			if(input.Equals("camera {"))
			{
				ReadCamera();
			}
			else if(input.Equals("point_light {"))
			{
				ReadLight();
			}
			else if(input.Equals("sphere {"))
			{
				numObj += ReadSphere(ObjID);
			}
			else if(input.Equals("poly_set {"))
			{
				numObj += ReadPoly(ObjID);
			}
			else
			{ ;}

			input = readString();
		}

		return (numObj);
	}

	/**
	 * ReadCamera
	 *
	 * @param infile
	 */
	private void ReadCamera()
	{
		String temp;
		double[] input = new double[3];
		int i;

		temp = readString();
		temp = temp.Substring(11);
		for(i = 0; i < 2; i++)
		{
			input[i] = (double)Double.Parse(temp.Substring(0, temp.IndexOf(' ')));
			temp = temp.Substring(temp.IndexOf(' ') + 1);
		}
		input[2] = (double)Double.Parse(temp);
		Point position = new Point(input[0], input[1], input[2]);
		MaxX = MinX = input[0];
		MaxY = MinY = input[1];
		MaxZ = MinZ = input[2];
		temp = readString();
		temp = temp.Substring(16);
		for(i = 0; i < 2; i++)
		{
			input[i] = (double)Double.Parse(temp.Substring(0, temp.IndexOf(' ')));
			temp = temp.Substring(temp.IndexOf(' ') + 1);
		}
		input[2] = (double)Double.Parse(temp);
		Vector viewDir = new Vector(input[0], input[1], input[2]);
		temp = readString();
		temp = temp.Substring(16);
		double focalDist = (double)Double.Parse(temp);
		temp = readString();
		temp = temp.Substring(10);
		for(i = 0; i < 2; i++)
		{
			input[i] = (double)Double.Parse(temp.Substring(0, temp.IndexOf(' ')));
			temp = temp.Substring(temp.IndexOf(' ') + 1);
		}
		input[2] = (double)Double.Parse(temp);
		Vector orthoUp = new Vector(input[0], input[1], input[2]);
		temp = readString();
		temp = temp.Substring(14);
		double FOV = (double)Double.Parse(temp);
		temp = readString();
		camera = new Camera(position, viewDir, focalDist, orthoUp, FOV);
	}

	/**
	 * ReadLight
	 *
	 * @param infile
	 */
	private void ReadLight()
	{
		String temp;
		double[] input = new double[3];
		int i;

		temp = readString();
		temp = temp.Substring(11);
		for(i = 0; i < 2; i++)
		{
			input[i] = (double)Double.Parse(temp.Substring(0, temp.IndexOf(' ')));
			temp = temp.Substring(temp.IndexOf(' ') + 1);
		}
		input[2] = (double)Double.Parse(temp);
		Point position = new Point(input[0], input[1], input[2]);
		temp = readString();
		temp = temp.Substring(8);
		for(i = 0; i < 2; i++)
		{
			input[i] = (double)Double.Parse(temp.Substring(0, temp.IndexOf(' ')));
			temp = temp.Substring(temp.IndexOf(' ') + 1);
		}
		input[2] = (double)Double.Parse(temp);
		Color color = new Color(input[0], input[1], input[2]);
		temp = readString();
		Light newlight = new Light(position, color);
		LightNode newnode = new LightNode(newlight, lights);
		lights = newnode;
	}

	/**
	 * ReadSphere
	 *
	 * @param infile
	 * @param ObjID
	 * @return int
	 */
	private int ReadSphere(int ObjID)
	{
		String temp;
		double[] input = new double[3];
		int i;
		double radius;
		Point max = new Point(MaxX, MaxY, MaxZ);
		Point min = new Point(MinX, MinY, MinZ);

		temp = readString();
		temp = readString();
		Material theMaterial = ReadMaterial();
		temp = readString();
		temp = temp.Substring(9);
		for(i = 0; i < 2; i++)
		{
			input[i] = (double)Double.Parse(temp.Substring(0, temp.IndexOf(' ')));
			temp = temp.Substring(temp.IndexOf(' ') + 1);
		}
		input[2] = (double)Double.Parse(temp);
		Point origin = new Point(input[0], input[1], input[2]);
		temp = readString();
		temp = temp.Substring(9);
		radius = (double)Double.Parse(temp);
		for(i = 0; i < 7; i++)
		{
			temp = readString();
		}
		SphereObj newsphere = new SphereObj(theMaterial, ++ObjID, origin, radius, max, min);
		ObjNode newnode = new ObjNode(newsphere, objects);
		objects = newnode;
		MaxX = max.GetX();
		MaxY = max.GetY();
		MaxZ = max.GetZ();
		MinX = min.GetX();
		MinY = min.GetY();
		MinZ = min.GetZ();

		return (1);
	}

	/**
	 * ReadPoly
	 *
	 * @param infile
	 * @param ObjID
	 * @return int
	 */
	private int ReadPoly(int ObjID)
	{
		String temp;
		double[] input = new double[3];
		int i, j, k;
		int numpolys = 0;
		int numverts;
		bool trimesh, vertnormal;
		Point max = new Point(MaxX, MaxY, MaxZ);
		Point min = new Point(MinX, MinY, MinZ);

		temp = readString();
		temp = readString();
		Material theMaterial = ReadMaterial();
		temp = readString();
		if(temp.Substring(7).Equals("POLYSET_TRI_MESH"))
		{
			trimesh = true;
		}
		else
		{
			trimesh = false;
		}
		temp = readString();
		if(temp.Substring(11).Equals("PER_VERTEX_NORMAL"))
		{
			vertnormal = true;
		}
		else
		{
			vertnormal = false;
		}
		for(i = 0; i < 4; i++)
		{
			temp = readString();
		}
		temp = temp.Substring(11);
		numpolys = Int32.Parse(temp);
		ObjID++;
		for(i = 0; i < numpolys; i++)
		{
			temp = readString();
			temp = readString();
			temp = temp.Substring(16);
			numverts = Int32.Parse(temp);
			Point[] vertices = new Point[numverts];
			for(j = 0; j < numverts; j++)
			{
				temp = readString();
				temp = temp.Substring(8);
				for(k = 0; k < 2; k++)
				{
					input[k] = (double)Double.Parse(temp.Substring(0, temp.IndexOf(' ')));
					temp = temp.Substring(temp.IndexOf(' ') + 1);
				}
				input[2] = (double)Double.Parse(temp);
				vertices[j] = new Point(input[0], input[1], input[2]);
				if(vertnormal)
				{
					temp = readString();
				}
			}
			temp = readString();
			TriangleObj newtriangle;
			PolygonObj newpoly;
			ObjNode newnode;
			if(trimesh)
			{
				newtriangle = new TriangleObj(theMaterial, ObjID, numverts, vertices, max, min);
				newnode = new ObjNode(newtriangle, objects);
			}
			else
			{
				newpoly = new PolygonObj(theMaterial, ObjID, numverts, vertices, max, min);
				newnode = new ObjNode(newpoly, objects);
			}
			objects = newnode;
		}
		temp = readString();
		MaxX = max.GetX();
		MaxY = max.GetY();
		MaxZ = max.GetZ();
		MinX = min.GetX();
		MinY = min.GetY();
		MinZ = min.GetZ();

		return (numpolys);
	}

	/**
	 * ReadMaterial
	 *
	 * @param infile
	 * @return Material
	 */
	private Material ReadMaterial()
	{
		String temp;
		double[] input = new double[3];
		Color[] colors = new Color[4];
		int i, j;
		double shininess, ktran;

		temp = readString();
		for(i = 0; i < 4; i++)
		{
			temp = readString();
			if(i != 1)
			{
				temp = temp.Substring(14);
			}
			else
			{
				temp = temp.Substring(13);
			}
			for(j = 0; j < 2; j++)
			{
				input[j] = (double)Double.Parse(temp.Substring(0, temp.IndexOf(' ')));
				temp = temp.Substring(temp.IndexOf(' ') + 1);
			}
			input[2] = (double)Double.Parse(temp);
			colors[i] = new Color(input[0], input[1], input[2]);
		}
		temp = readString();
		shininess = (double)Double.Parse(temp.Substring(14));
		temp = readString();
		ktran = (double)Double.Parse(temp.Substring(10));
		temp = readString();
		Material newmaterial = new Material(colors[0], colors[1], colors[2], colors[3], shininess, ktran);
		MaterialNode newnode = new MaterialNode(newmaterial, materials);
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
	private void Shade(OctNode tree, Ray eyeRay, Color color, double factor, int level, int originID)
	{
		Color lightColor = new Color(0.0f, 0.0f, 0.0f);
		Color reflectColor = new Color(0.0f, 0.0f, 0.0f);
		Color refractColor = new Color(0.0f, 0.0f, 0.0f);
		IntersectPt intersect = new IntersectPt();
		OctNode baseoctn = new OctNode();
		Vector normal = new Vector();
		Ray reflect = new Ray();
		Ray refract = new Ray();
		double mu;
		int current;

		if(intersect.FindNearestIsect(tree, eyeRay, originID, level, baseoctn))
		{
			intersect.GetIntersectObj().FindNormal(intersect.GetIntersection(), normal);
			GetLightColor(baseoctn, intersect.GetIntersection(), normal, intersect.GetIntersectObj(), lightColor);
			if(level < MaxLevel)
			{
				double check = factor * (1.0f - intersect.GetIntersectObj().GetMaterial().GetKTran()) * intersect.GetIntersectObj().GetMaterial().GetShininess();
				if(check > MinFactor)
				{
					reflect.SetOrigin(intersect.GetIntersection());
					reflect.GetDirection().Combine(eyeRay.GetDirection(), normal, 1.0f, -2.0f * normal.Dot(eyeRay.GetDirection()));
					reflect.SetID(RayID);
					this.RayID = this.RayID + 1;
					Shade(baseoctn, reflect, reflectColor, check, level + 1, originID);
					reflectColor.Scale((1.0f - intersect.GetIntersectObj().GetMaterial().GetKTran()) * intersect.GetIntersectObj().GetMaterial().GetShininess(),
						intersect.GetIntersectObj().GetMaterial().GetSpecColor());
				}
				check = factor * intersect.GetIntersectObj().GetMaterial().GetKTran();
				if(check > MinFactor)
				{
					if(intersect.GetEnter())
					{
						mu = 1.0f / intersect.GetIntersectObj().GetMaterial().GetRefIndex();
						current = intersect.GetIntersectObj().GetObjID();
					}
					else
					{
						mu = intersect.GetIntersectObj().GetMaterial().GetRefIndex();
						normal.Negate();
						current = 0;
					}
					double IdotN = normal.Dot(eyeRay.GetDirection());
					double TotIntReflect = 1.0f - mu * mu * (1.0f - IdotN * IdotN);
					if(TotIntReflect >= 0.0)
					{
						double gamma = -mu * IdotN - (double)Math.Sqrt(TotIntReflect);
						refract.SetOrigin(intersect.GetIntersection());
						refract.GetDirection().Combine(eyeRay.GetDirection(), normal, mu, gamma);
						refract.SetID(RayID);
						this.RayID = RayID + 1;
						Shade(baseoctn, refract, refractColor, check, level + 1, current);
						refractColor.Scale(intersect.GetIntersectObj().GetMaterial().GetKTran(), intersect.GetIntersectObj().GetMaterial().GetSpecColor());
					}
				}
			}
			color.Combine(intersect.GetIntersectObj().GetMaterial().GetEmissColor(), intersect.GetIntersectObj().GetMaterial().GetAmbColor(),
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
	private void GetLightColor(OctNode tree, Point point, Vector normal, ObjectType currentObj, Color color)
	{
		Ray shadow = new Ray();
		LightNode current = lights;
		double maxt;

		while(current != null)
		{
			shadow.SetOrigin(point);
			shadow.GetDirection().Sub(current.GetLight().GetPosition(), point);
			maxt = shadow.GetDirection().Length();
			shadow.GetDirection().Normalize();
			shadow.SetID(RayID);
			this.RayID = this.RayID + 1;
			if(!FindLightBlock(tree, shadow, maxt))
			{
				double factor = Math.Max(0.0f, normal.Dot(shadow.GetDirection()));
				if(factor != 0.0)
				{
					color.Mix(factor, current.GetLight().GetColor(), currentObj.GetMaterial().GetDiffColor());
				}
			}
			current = current.Next();
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
	private bool FindLightBlock(OctNode tree, Ray ray, double maxt)
	{
		OctNode current = tree.FindTreeNode(ray.GetOrigin());
		IntersectPt test = new IntersectPt();
		Point testpt = new Point();

		while(current != null)
		{
			ObjNode currentnode = current.GetList();
			while(currentnode != null)
			{
				bool found = false;
				if(currentnode.GetObj().GetCachePt().GetID() == ray.GetID())
				{
					found = true;
				}
				if(!found)
				{
					test.SetOrigID(0);
					if(currentnode.GetObj().Intersect(ray, test))
					{
						if(test.GetT() < maxt)
						{
							return (true);
						}
					}
				}
				currentnode = currentnode.Next();
			}
			OctNode adjacent = current.Intersect(ray, testpt, test.GetThreshold());
			if(adjacent == null)
			{
				current = null;
			}
			else
			{
				current = adjacent.FindTreeNode(testpt);
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
	public Scene(String filename)
	{
		int numObj = LoadScene(filename);
		octree = new OctNode(this, numObj);
	}

	/**
	 * RenderScene
	 */
	public void RenderScene(Canvas canvas, int width, int section, int nsections)
	{
		Vector view = camera.GetViewDir();
		Vector up = camera.GetOrthoUp();
		Vector plane = new Vector();
		Vector horIncr = new Vector();
		Vector vertIncr = new Vector();
		double ylen = camera.GetFocalDist() * (double)Math.Tan(0.5f * camera.GetFOV());
		double xlen = ylen * canvas.GetWidth() / canvas.GetHeight();
		Point upleft = new Point();
		Point upright = new Point();
		Point lowleft = new Point();
		Point basepoint = new Point();
		Point current;
		Ray eyeRay = new Ray();
		int ypixel, xpixel;

		RayID = 1;
		plane.Cross(view, up);
		view.Scale(camera.GetFocalDist());
		up.Scale(ylen);
		plane.Scale(-xlen);
		upleft.FindCorner(view, up, plane, camera.GetPosition());
		plane.Negate();
		upright.FindCorner(view, up, plane, camera.GetPosition());
		up.Negate();
		plane.Negate();
		lowleft.FindCorner(view, up, plane, camera.GetPosition());
		horIncr.Sub(upright, upleft);
		horIncr.Scale(horIncr.Length() / ((double)canvas.GetWidth()));
		vertIncr.Sub(lowleft, upleft);
		vertIncr.Scale(vertIncr.Length() / ((double)canvas.GetHeight()));
		basepoint.Set(upleft.GetX() + 0.5f * (horIncr.GetX() + vertIncr.GetX()), upleft.GetY() + 0.5f * (horIncr.GetY() + vertIncr.GetY()),
			upleft.GetZ() + 0.5f * (horIncr.GetZ() + vertIncr.GetZ()));
		eyeRay.SetOrigin(camera.GetPosition());

		int xstart = section * width / nsections;
		int xend = xstart + width / nsections;

		Console.WriteLine("+" + xstart + " to " + (xend - 1) + " by " + canvas.GetHeight());

		for(ypixel = 0; ypixel < canvas.GetHeight(); ypixel++)
		{
			current = new Point(basepoint);
			for(xpixel = 0; xpixel < canvas.GetWidth(); xpixel++)
			{
				if(xpixel >= xstart && xpixel < xend)
				{
					Color color = new Color(0.0f, 0.0f, 0.0f);
					eyeRay.GetDirection().Sub(current, eyeRay.GetOrigin());
					eyeRay.GetDirection().Normalize();
					eyeRay.SetID(RayID);
					this.RayID = this.RayID + 1;
					Shade(octree, eyeRay, color, 1.0f, 0, 0);
					canvas.Write(Brightness, xpixel, ypixel, color);
				}
				current.Add(horIncr);
			}
			basepoint.Add(vertIncr);
		}
		Console.WriteLine("-" + xstart + " to " + (xend - 1) + " by " + canvas.GetHeight());
	}

	/**
	 * GetObjects
	 *
	 * @return ObjNode
	 */
	public ObjNode GetObjects()
	{
		return (objects);
	}

	/**
	 * GetMaxX
	 *
	 * @return double
	 */
	public double GetMaxX()
	{
		return (MaxX);
	}

	/**
	 * GetMinX
	 *
	 * @return double
	 */
	public double GetMinX()
	{
		return (MinX);
	}

	/**
	 * GetMaxY
	 *
	 * @return double
	 */
	public double GetMaxY()
	{
		return (MaxY);
	}

	/**
	 * GetMinY
	 *
	 * @return double
	 */
	public double GetMinY()
	{
		return (MinY);
	}

	/**
	 * GetMaxZ
	 *
	 * @return double
	 */
	public double GetMaxZ()
	{
		return (MaxZ);
	}

	/**
	 * GetMinZ
	 *
	 * @return double
	 */
	public double GetMinZ()
	{
		return (MinZ);
	}
}



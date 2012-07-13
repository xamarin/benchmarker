/*
 * @(#)SphereObj.java	1.3 06/17/98
 *
 * SphereObj.java
 * The class for a sphere, holding its origin and radius. Implements the
 * intersection test of a ray with the sphere.
 *
 * Copyright (c) 1998 Standard Performance Evaluation Corporation (SPEC)
 *               All rights reserved.
 * Copyright (c) 1996,1997,1998 Sun Microsystems, Inc. All rights reserved.
 *
 * This source code is provided as is, without any express or implied warranty.
 */


/**
 * class SphereObj
 */
public class SphereObj : ObjectType
{
	private Point Origin;
	private double Radius;
	private double RadiusSquare;

	/**
	 * SphereObj
	 *
	 * @param objmaterial
	 * @param newobjID
	 * @param neworigin
	 * @param newradius
	 * @param MaxX
	 * @param MinX
	 * @param MaxY
	 * @param MinY
	 * @param MaxZ
	 * @param MinZ
	 */
	public SphereObj(Material objmaterial, int newobjID, Point neworigin, double newradius, Point max, Point min)
		: base(objmaterial, newobjID)
	{
		Origin = neworigin;
		Radius = newradius;

		RadiusSquare = Radius * Radius;
		GetMax().SetX(Origin.GetX() + Radius);
		GetMax().SetY(Origin.GetY() + Radius);
		GetMax().SetZ(Origin.GetZ() + Radius);
		GetMin().SetX(Origin.GetX() - Radius);
		GetMin().SetY(Origin.GetY() - Radius);
		GetMin().SetZ(Origin.GetZ() - Radius);
		if(GetMax().GetX() > max.GetX())
		{
			max.SetX(GetMax().GetX());
		}
		if(GetMax().GetY() > max.GetY())
		{
			max.SetY(GetMax().GetY());
		}
		if(GetMax().GetZ() > max.GetZ())
		{
			max.SetZ(GetMax().GetZ());
		}
		if(GetMin().GetX() < min.GetX())
		{
			min.SetX(GetMin().GetX());
		}
		if(GetMin().GetY() < min.GetY())
		{
			min.SetY(GetMin().GetY());
		}
		if(GetMin().GetZ() < min.GetZ())
		{
			min.SetZ(GetMin().GetZ());
		}
	}

	/**
	 * Intersect
	 *
	 * @param ray
	 * @param pt
	 * @return boolean
	 */
	public override bool Intersect(Ray ray, IntersectPt pt)
	{
		Vector OC = new Vector();
		double l2OC, tCA, t2HC;

		OC.Sub(Origin, ray.GetOrigin());
		l2OC = OC.SquaredLength();
		tCA = OC.Dot(ray.GetDirection());
		if(l2OC >= RadiusSquare && tCA <= 0)
		{
			return (false);
		}
		t2HC = RadiusSquare - l2OC + tCA * tCA;
		if(t2HC < 0)
		{
			return (false);
		}
		if(l2OC <= RadiusSquare)
		{
			pt.SetT(tCA + (double)System.Math.Sqrt(t2HC));
			if(pt.GetT() < pt.GetThreshold())
			{
				return (false);
			}
			pt.SetEnter(false);
			pt.GetIntersection().Combine(ray.GetOrigin(), ray.GetDirection(), 1.0f, pt.GetT());
		}
		else
		{
			pt.SetT(tCA - (double)System.Math.Sqrt(t2HC));
			pt.SetEnter(true);
			if(pt.GetT() < pt.GetThreshold())
			{
				pt.SetT(tCA + (double)System.Math.Sqrt(t2HC));
				pt.SetEnter(false);
			}
			pt.GetIntersection().Combine(ray.GetOrigin(), ray.GetDirection(), 1.0f, pt.GetT());
		}
		pt.SetIntersectObj(this);
		GetCachePt().Set(ray.GetID(), pt);
		return (true);
	}

	/**
	 * FindNormal
	 *
	 * @param point
	 * @param normal
	 */
	public override void FindNormal(Point point, Vector normal)
	{
		normal.Sub(point, Origin);
		normal.Normalize();
	}
}


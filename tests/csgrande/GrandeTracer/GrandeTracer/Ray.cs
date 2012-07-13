using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Ray
{
	public Vec P, D;

	public Ray(Vec pnt, Vec dir)
	{
		P = new Vec(pnt.x, pnt.y, pnt.z);
		D = new Vec(dir.x, dir.y, dir.z);
		D.normalize();
	}

	public Ray()
	{
		P = new Vec();
		D = new Vec();
	}

	public Vec point(double t)
	{
		return new Vec(P.x + D.x * t, P.y + D.y * t, P.z + D.z * t);
	}

	public override String ToString()
	{
		return "{" + P.ToString() + " -> " + D.ToString() + "}";
	}
}


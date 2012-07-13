using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


public abstract class Primitive
{
	public Surface surf = new Surface();

	public void setColor(double r, double g, double b)
	{
		surf.color = new Vec(r, g, b);
	}

	public abstract Vec normal(Vec pnt);
	public abstract Isect intersect(Ray ry);
	public abstract Vec getCenter();
	public abstract void setCenter(Vec c);
}


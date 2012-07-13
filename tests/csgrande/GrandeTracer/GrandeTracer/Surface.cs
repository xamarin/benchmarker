using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Surface
{
	public Vec color;
	public double kd;
	public double ks;
	public double shine;
	public double kt;
	public double ior;

	public Surface()
	{
		color = new Vec(1, 0, 0);
		kd = 1.0;
		ks = 0.0;
		shine = 0.0;
		kt = 0.0;
		ior = 1.0;
	}

	public String toString()
	{
		return "Surface { color=" + color + "}";
	}
}


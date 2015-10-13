using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Benchmarks.GrandeTracer
{
	public class Light
	{
		public Vec pos;
		public double brightness;

		public Light ()
		{
		}

		public Light (double x, double y, double z, double brightness)
		{
			this.pos = new Vec (x, y, z);
			this.brightness = brightness;
		}
	}
}

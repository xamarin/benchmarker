using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Benchmarks.GrandeTracer
{
	public class Interval
	{
		public int number;
		public int width;
		public int height;
		public int yfrom;
		public int yto;
		public int total;

		public Interval (int number, int width, int height, int yfrom, int yto, int total)
		{
			this.number = number;
			this.width = width;
			this.height = height;
			this.yfrom = yfrom;
			this.yto = yto;
			this.total = total;
		}
	}
}
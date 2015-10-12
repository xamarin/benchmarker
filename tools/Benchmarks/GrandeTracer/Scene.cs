using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Benchmarks.GrandeTracer
{
	public class Scene
	{
		public List<Light> lights;
		public List<Primitive> objects;
		private View view;

		public Scene ()
		{
			this.lights = new List<Light> ();
			this.objects = new List<Primitive> ();
		}

		public View RTView {
			get { return this.view; }
			set { this.view = value; }
		}

		public List<Light> Lights { get { return this.lights; } }

		public List<Primitive> Objects { get { return this.objects; } }
	}
}

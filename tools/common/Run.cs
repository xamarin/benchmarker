using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Benchmarker.Common
{
	public class Run
	{
		public DateTime DateTime { get; set; }
		public Benchmark Benchmark { get; set; }
		public Config Config { get; set; }
		public string Version { get; set; }
		public Time[] Times { get; set; }
		public bool Timedout { get; set; }

		public static Run LoadFrom (string filename)
		{
			using (var reader = new StreamReader (new FileStream (filename, FileMode.Open))) {
				return JsonConvert.DeserializeObject<Run> (reader.ReadToEnd ());
			}
		}

		public void StoreTo (string filename)
		{
			using (var writer = new StreamWriter (new FileStream (filename, FileMode.Create))) {
				writer.Write (JsonConvert.SerializeObject (this, Formatting.Indented));
			}
		}

		public class Time {
			public TimeSpan Value { get; set; }
			public string Output { get; set; }
			public string Error { get; set; }
		}
	}
}


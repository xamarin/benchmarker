using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Benchmarker.Common.Models
{
	public class Machine
	{
		public string Name { get; set; }
		public int DefaultTimeout { get; set; }
		public Dictionary<string, int> BenchmarkTimeouts { get; set; }
		public List<string> ExcludeBenchmarks { get; set; }

		public Machine ()
		{
		}

		public static Machine LoadFromString (string content)
		{
			return JsonConvert.DeserializeObject<Machine> (content);
		}
	}
}

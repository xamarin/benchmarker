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

		public static Machine LoadFrom (string machineName, string directory)
		{
			string filename = Path.Combine (directory, String.Format ("{0}.conf", machineName));

			try {
				using (var reader = new StreamReader (new FileStream (filename, FileMode.Open))) {
					return JsonConvert.DeserializeObject<Machine> (reader.ReadToEnd ());
				}
			} catch (FileNotFoundException) {
				return null;
			}
		}
			
		public static Machine LoadCurrentFrom (string directory)
		{
			return LoadFrom (Environment.MachineName, directory);
		}
	}
}

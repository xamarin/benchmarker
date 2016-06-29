using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using Benchmarker;

namespace Benchmarker.Models
{
    public class Benchmark
	{
		public string Name { get; set; }
		public string TestDirectory { get; set; }
		public bool OnlyExplicit {get; set; }
		public string[] CommandLine { get; set; }
		public string[] ClientCommandLine { get; set; }
		public string[] AOTAssemblies { get; set; }

		public Benchmark ()
		{
		}

		public static Benchmark LoadFromString (string jsonContent)
		{
			var benchmark = JsonConvert.DeserializeObject<Benchmark> (jsonContent);

			if (String.IsNullOrWhiteSpace (benchmark.TestDirectory))
				throw new InvalidDataException ("TestDirectory");
			if (benchmark.CommandLine == null || benchmark.CommandLine.Length == 0)
				throw new InvalidDataException ("CommandLine");

			return benchmark;
		}

		public override bool Equals (object other)
		{
			if (other == null)
				return false;

			var benchmark = other as Benchmark;
			if (benchmark == null)
				return false;

			return Name.Equals (benchmark.Name);
		}

		public override int GetHashCode ()
		{
			return Name.GetHashCode ();
		}
	}
}

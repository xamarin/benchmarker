using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using Parse;

namespace Benchmarker.Common.Models
{
	public class Benchmark
	{
		public string Name { get; set; }
		public string TestDirectory { get; set; }
		public string[] CommandLine { get; set; }

		public Benchmark ()
		{
		}

		public static Benchmark LoadFrom (string filename)
		{
			using (var reader = new StreamReader (new FileStream (filename, FileMode.Open))) {
				var benchmark = JsonConvert.DeserializeObject<Benchmark> (reader.ReadToEnd ());

				if (String.IsNullOrEmpty (benchmark.TestDirectory))
					throw new InvalidDataException ("TestDirectory");
				if (benchmark.CommandLine == null || benchmark.CommandLine.Length == 0)
					throw new InvalidDataException ("CommandLine");

				return benchmark;
			}
		}

		public static List<Benchmark> LoadAllFrom (string directory)
		{
			return LoadAllFrom (directory, new string[0]);
		}

		public static List<Benchmark> LoadAllFrom (string directory, string[] names)
		{
			return Directory.EnumerateFiles (directory)
				.Where (f => f.EndsWith (".benchmark"))
				.Where (f => names.Length == 0 ? true : names.Any (n => Path.GetFileNameWithoutExtension (f) == n))
				.Select (f => Benchmark.LoadFrom (f))
				.ToList ();
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

		ParseObject parseObject;

		public async Task<ParseObject> GetOrUploadToParse (List<ParseObject> saveList)
		{
			if (parseObject != null)
				return parseObject;

			var results = await ParseObject.GetQuery ("Benchmark").WhereEqualTo ("name", Name).FindAsync ();
			if (results.Count () > 0)
				return results.First ();
			var obj = ParseInterface.NewParseObject ("Benchmark");
			obj ["name"] = Name;
			saveList.Add (obj);

			parseObject = obj;

			return obj;
		}
	}
}

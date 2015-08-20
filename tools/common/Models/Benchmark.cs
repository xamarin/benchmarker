using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Parse;

namespace Benchmarker.Common.Models
{
    public class Benchmark
	{
		public string Name { get; set; }
		public string TestDirectory { get; set; }
		public string[] CommandLine { get; set; }

		static Dictionary<string, ParseObject> nameToParseObject;

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
			var allPaths = Directory.EnumerateFiles (directory)
				.Where (f => f.EndsWith (".benchmark"));
			if (names != null) {
				foreach (var name in names) {
					if (!allPaths.Any (p => Path.GetFileNameWithoutExtension (p) == name))
						return null;
				}
				allPaths = allPaths
					.Where (f => names.Any (n => Path.GetFileNameWithoutExtension (f) == n));
			}
			return allPaths
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

		static async Task FetchBenchmarks ()
		{
			if (nameToParseObject != null)
				return;

			nameToParseObject = new Dictionary<string, ParseObject> ();
			var results = await ParseInterface.RunWithRetry (() => ParseObject.GetQuery ("Benchmark").FindAsync ());
			//Console.WriteLine ("FindAsync Benchmark");
			foreach (var o in results)
				nameToParseObject [o.Get<string> ("name")] = o;
		}

		public static async Task<Benchmark> FromId (string id)
		{
			await FetchBenchmarks ();

			foreach (var kvp in nameToParseObject) {
				if (kvp.Value.ObjectId == id) {
					var benchmark = new Benchmark {
						Name = kvp.Value.Get<string> ("name")
					};

					return benchmark;
				}
			}

			throw new Exception ("Could not fetch benchmark.");
		}

		public async Task<ParseObject> GetOrUploadToParse (List<ParseObject> saveList)
		{
			await FetchBenchmarks ();

			if (nameToParseObject.ContainsKey (Name))
				return nameToParseObject [Name];

			var obj = ParseInterface.NewParseObject ("Benchmark");
			obj ["name"] = Name;
			saveList.Add (obj);

			nameToParseObject [Name] = obj;

			return obj;
		}
	}
}

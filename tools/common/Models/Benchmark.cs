using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Parse;
using System.IO;

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

		public static Benchmark LoadFromString (string jsonContent)
		{
			var benchmark = JsonConvert.DeserializeObject<Benchmark> (jsonContent);

			if (String.IsNullOrEmpty (benchmark.TestDirectory))
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

		static async Task FetchBenchmarks ()
		{
			if (nameToParseObject != null)
				return;

			nameToParseObject = new Dictionary<string, ParseObject> ();
			var results = await ParseInterface.RunWithRetry (() => ParseObject.GetQuery ("Benchmark").FindAsync ());
			Logging.GetLogging ().Info ("FindAsync Benchmark");
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

using System;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Collections.Generic;
using Parse;
using System.Threading.Tasks;
using System.Linq;

namespace Benchmarker.Common.Models
{
	public class Config
	{
		public string Name { get; set; }
		public int Count { get; set; }
		public bool NoMono {get; set; }
		public string Mono { get; set; }
		public string[] MonoOptions { get; set; }
		public Dictionary<string, string> MonoEnvironmentVariables { get; set; }
		public string ResultsDirectory { get; set; }

		public Config ()
		{
		}

		public static Config LoadFrom (string filename)
		{
			using (var reader = new StreamReader (new FileStream (filename, FileMode.Open))) {
				var config = JsonConvert.DeserializeObject<Config> (reader.ReadToEnd ());

				if (String.IsNullOrEmpty (config.Name))
					throw new InvalidDataException ("Name");

				if (config.NoMono) {
					Debug.Assert (config.MonoOptions == null || config.MonoOptions.Length == 0);
					Debug.Assert (config.MonoEnvironmentVariables == null || config.MonoEnvironmentVariables.Count == 0);
				}

				if (String.IsNullOrEmpty (config.Mono))
					config.Mono = String.Empty;

				if (config.Count < 1)
					config.Count = 10;

				if (config.MonoOptions == null)
					config.MonoOptions = new string[0];

				if (config.MonoEnvironmentVariables == null)
					config.MonoEnvironmentVariables = new Dictionary<string, string> ();

				if (String.IsNullOrEmpty (config.ResultsDirectory))
					config.ResultsDirectory = "results";

				return config;
			}
		}

		public override bool Equals (object other)
		{
			if (other == null)
				return false;

			var config = other as Config;
			if (config == null)
				return false;

			return Name.Equals (config.Name);
		}

		public override int GetHashCode ()
		{
			return Name.GetHashCode ();
		}

		static bool OptionsEqual (IEnumerable<string> native, IEnumerable<object> parse)
		{
			if (parse == null)
				return false;
			foreach (var s in native) {
				var found = false;
				foreach (var p in parse) {
					var ps = p as string;
					if (s == ps) {
						found = true;
						break;
					}
				}
				if (!found)
					return false;
			}
			return true;
		}

		static bool EnvironmentVariablesEqual (IDictionary<string, string> native, IDictionary<string, object> parse)
		{
			if (parse == null)
				return false;
			foreach (var kv in native) {
				if (!parse.ContainsKey (kv.Key))
					return false;
				var v = parse [kv.Key] as string;
				if (v != kv.Value)
					return false;
			}
			return true;
		}

		public async Task<ParseObject> GetOrUploadToParse ()
		{
			var executable = Path.GetFileName (Mono);

			var results = await ParseObject.GetQuery ("Config")
				.WhereEqualTo ("name", Name)
				.WhereEqualTo ("monoExecutable", executable)
				.FindAsync ();
			foreach (var o in results) {
				if (OptionsEqual (MonoOptions, o ["monoOptions"] as IEnumerable<object>)
				    && EnvironmentVariablesEqual (MonoEnvironmentVariables, o ["monoEnvironmentVariables"] as IDictionary<string, object>)) {
					Console.WriteLine ("found config " + o.ObjectId);
					return o;
				}
			}

			Console.WriteLine ("creating new config");

			var obj = new ParseObject ("Config");
			obj ["name"] = Name;
			obj ["monoExecutable"] = executable;
			obj ["monoOptions"] = MonoOptions;
			obj ["monoEnvironmentVariables"] = MonoEnvironmentVariables;
			await obj.SaveAsync ();
			return obj;
		}
	}
}

using System;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Collections.Generic;
using Parse;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections;
using System.Text;

namespace Benchmarker.Common.Models
{
    public class Config
	{
		const string rootVarString = "$ROOT";

		public string Name { get; set; }
		public int Count { get; set; }
		public bool NoMono {get; set; }
		public string Mono { get; set; }
		public string[] MonoOptions { get; set; }
		public Dictionary<string, string> MonoEnvironmentVariables { get; set; }
		public Dictionary<string, string> UnsavedMonoEnvironmentVariables { get; set; }

		public string MonoExecutable {
			get {
				return Path.GetFileName (Mono);
			}
		}

		public Dictionary<string, string> processedMonoEnvironmentVariables { get; set; }

		public Config ()
		{
		}

		static void ExpandRootInEnvironmentVariables (Dictionary<string, string> processedEnvVars, Dictionary<string, string> unexpandedEnvVars, string root)
		{
			foreach (var kvp in unexpandedEnvVars) {
				var key = kvp.Key;
				var unexpandedValue = kvp.Value;
				if (unexpandedValue.Contains (rootVarString)) {
					if (root != null)
						processedEnvVars [key] = unexpandedValue.Replace (rootVarString, root);
					else
						throw new InvalidDataException ("Configuration requires a root directory.");
				} else {
					processedEnvVars [key] = unexpandedValue;
				}
			}
		}

		public static Config LoadFromString (string content, string root)
		{
			var config = JsonConvert.DeserializeObject<Config> (content);

			if (String.IsNullOrEmpty (config.Name))
				throw new InvalidDataException ("Configuration does not have a `Name`.");

			if (config.NoMono) {
				Debug.Assert (config.MonoOptions == null || config.MonoOptions.Length == 0);
				Debug.Assert (config.MonoEnvironmentVariables == null || config.MonoEnvironmentVariables.Count == 0);
				Debug.Assert (config.UnsavedMonoEnvironmentVariables == null || config.UnsavedMonoEnvironmentVariables.Count == 0);
			}

			if (String.IsNullOrEmpty (config.Mono)) {
				config.Mono = String.Empty;
			} else if (root != null) {
				config.Mono = config.Mono.Replace (rootVarString, root);
			} else if (config.Mono.Contains (rootVarString)) {
				throw new InvalidDataException ("Configuration requires a root directory.");
			}

			if (config.Count < 1)
				config.Count = 10;

			if (config.MonoOptions == null)
				config.MonoOptions = new string[0];

			if (config.MonoEnvironmentVariables == null)
				config.MonoEnvironmentVariables = new Dictionary<string, string> ();
			if (config.UnsavedMonoEnvironmentVariables == null)
				config.UnsavedMonoEnvironmentVariables = new Dictionary<string, string> ();

			config.processedMonoEnvironmentVariables = new Dictionary<string, string> ();
			ExpandRootInEnvironmentVariables (config.processedMonoEnvironmentVariables, config.MonoEnvironmentVariables, root);
			ExpandRootInEnvironmentVariables (config.processedMonoEnvironmentVariables, config.UnsavedMonoEnvironmentVariables, root);

			return config;
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

		public bool EqualToParseObject (ParseObject o) {
			if (Name != o.Get<string> ("name"))
				return false;
			if (MonoExecutable != o.Get<string> ("monoExecutable"))
				return false;
			if (!OptionsEqual (MonoOptions, o ["monoOptions"] as IEnumerable<object>))
				return false;
			if (!EnvironmentVariablesEqual (MonoEnvironmentVariables, o ["monoEnvironmentVariables"] as IDictionary<string, object>))
				return false;
			return true;
		}

		public async Task<ParseObject> GetFromParse ()
		{
			var results = await ParseInterface.RunWithRetry (() => ParseObject.GetQuery ("Config")
				.WhereEqualTo ("name", Name)
				.WhereEqualTo ("monoExecutable", MonoExecutable)
				.FindAsync ());
			//Console.WriteLine ("FindAsync Config");
			foreach (var o in results) {
				if (EqualToParseObject (o)) {
					Logging.GetLogging ().Info ("found config " + o.ObjectId);
					return o;
				}
			}
			return null;
		}

		public async Task<ParseObject> GetOrUploadToParse (List<ParseObject> saveList)
		{
			var obj = await GetFromParse ();
			if (obj != null)
				return obj;

			Logging.GetLogging ().Info ("creating new config");

			obj = ParseInterface.NewParseObject ("Config");
			obj ["name"] = Name;
			obj ["monoExecutable"] = MonoExecutable;
			obj ["monoOptions"] = MonoOptions;
			obj ["monoEnvironmentVariables"] = MonoEnvironmentVariables;
			saveList.Add (obj);
			return obj;
		}
	}
}

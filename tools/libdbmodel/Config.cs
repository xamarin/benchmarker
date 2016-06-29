using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Collections;
using System.Text;
using System.Linq;
using Newtonsoft.Json.Linq;
using Benchmarker;

namespace Benchmarker.Models
{
	public class Config : ApiObject
	{
		const string rootVarString = "$ROOT";
		const string binaryProtocolString = "$BINPROT";

		public string Name { get; set; }

		public int Count { get; set; }

		public bool NoMono { get; set; }

		public string Mono { get; set; }

		public string[] AOTOptions { get; set; }

		public string[] MonoOptions { get; set; }

		public Dictionary<string, string> MonoEnvironmentVariables { get; set; }

		public Dictionary<string, string> UnsavedMonoEnvironmentVariables { get; set; }

		public List<string> Benchmarks { get; set; }

		private string monoRoot;

		public string MonoExecutable {
			get {
				return Path.GetFileName (Mono);
			}
		}

		public bool ProducesBinaryProtocol {
			get {
				var values = MonoEnvironmentVariables.Values.Concat (UnsavedMonoEnvironmentVariables.Values);
				return values.Any (v => v.Contains (binaryProtocolString));
			}
		}

		static void ExpandInEnvironmentVariables (Dictionary<string, string> processedEnvVars, Dictionary<string, string> unexpandedEnvVars,
		                                          string variableString, string variableValue)
		{
			var keys = unexpandedEnvVars.Keys.ToArray ();
			foreach (var key in keys) {
				var unexpandedValue = unexpandedEnvVars [key];
				if (unexpandedValue.Contains (variableString)) {
					if (variableValue != null)
						processedEnvVars [key] = unexpandedValue.Replace (variableString, variableValue);
					else
						throw new InvalidDataException ("Configuration requires a value for the variable " + variableString);
				} else {
					processedEnvVars [key] = unexpandedValue;
				}
			}
		}

		public Dictionary<string, string> ProcessMonoEnvironmentVariables (string binaryProtocolFile)
		{
			var hasBinProt = ProducesBinaryProtocol;
			if (hasBinProt && binaryProtocolFile == null)
				throw new Exception ("Configuration requires binary protocol file, but none is given");
			if (!hasBinProt && binaryProtocolFile != null)
				throw new Exception ("Binary protocol file is given, but none is required by config");

			var vars = new Dictionary<string, string> ();
			ExpandInEnvironmentVariables (vars, MonoEnvironmentVariables, rootVarString, monoRoot);
			ExpandInEnvironmentVariables (vars, UnsavedMonoEnvironmentVariables, rootVarString, monoRoot);

			if (hasBinProt)
				ExpandInEnvironmentVariables (vars, vars, binaryProtocolString, binaryProtocolFile);

			return vars;
		}

		public Config ()
		{
		}

		public static Config LoadFromString (string content, string root, bool expandRoot)
		{
			var config = JsonConvert.DeserializeObject<Config> (content);
			config.monoRoot = root == null ? null : Path.GetFullPath (root);

			if (String.IsNullOrWhiteSpace (config.Name))
				throw new InvalidDataException ("Configuration does not have a `Name`.");

			if (config.NoMono) {
				if (!(config.MonoOptions == null || config.MonoOptions.Length == 0)) {
					throw new Exception ("config error");
				}
				if (!(config.MonoEnvironmentVariables == null || config.MonoEnvironmentVariables.Count == 0)) {
					throw new Exception ("config error");
				}
				if (!(config.UnsavedMonoEnvironmentVariables == null || config.UnsavedMonoEnvironmentVariables.Count == 0)) {
					throw new Exception ("config error");
				}
			}

			if (String.IsNullOrWhiteSpace (config.Mono)) {
				config.Mono = String.Empty;
			} else if (expandRoot && root != null) {
				config.Mono = config.Mono.Replace (rootVarString, config.monoRoot);
			} else if (expandRoot && config.Mono.Contains (rootVarString)) {
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

		static bool EnvironmentVariablesEqual (IDictionary<string, string> native, JToken json)
		{
			if (native.Count != json.Count ())
				return false;
			foreach (var kv in native) {
				var jsonValue = json.Value<string> (kv.Key);
				if (jsonValue == null)
					return false;
				if (jsonValue != kv.Value)
					return false;
			}
			return true;
		}

		static bool OptionsEqual (IList<string> native, JToken json)
		{
			if (native.Count != json.Count ())
				return false;
			return native.All (opt => json.Any (j => opt == j.ToObject<string> ()));
		}

		public IDictionary<string, object> AsDict ()
		{
			var dict = new Dictionary<string, object> ();

			dict ["Name"] = Name;
			dict ["MonoExecutable"] = MonoExecutable;
			dict ["MonoEnvironmentVariables"] = new Dictionary<string, string> (MonoEnvironmentVariables);
			dict ["MonoOptions"] = new List<string> (MonoOptions);

			return dict;
		}

		public bool EqualsApiObject (JToken other)
		{
			return Name == other ["Name"].ToObject<string> () &&
			MonoExecutable == other ["MonoExecutable"].ToObject<string> () &&
			EnvironmentVariablesEqual (MonoEnvironmentVariables, other ["MonoEnvironmentVariables"]) &&
			OptionsEqual (MonoOptions, other ["MonoOptions"]);
		}
	}
}

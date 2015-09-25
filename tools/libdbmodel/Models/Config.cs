using System;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Collections;
using System.Text;
using System.Linq;
using Npgsql;
using Newtonsoft.Json.Linq;
using Benchmarker.Common;

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

		public bool EqualsPostgresObject (PostgresRow row, string prefix = "")
		{
			if (row.GetReference<string> (prefix + "name") != Name)
				return false;

			if (row.GetReference<string> (prefix + "monoExecutable") != MonoExecutable)
				return false;

			var envVars = row.GetReference<JToken> (prefix + "monoEnvironmentVariables");
			if (!EnvironmentVariablesEqual (MonoEnvironmentVariables, envVars))
				return false;
			
			if (!row.GetReference<string[]> (prefix + "monoOptions").SequenceEqual (MonoOptions))
				return false;

			return true;
		}

		public bool ExistsInPostgres (NpgsqlConnection conn)
		{
			var parameters = new PostgresRow ();
			parameters.Set ("name", NpgsqlTypes.NpgsqlDbType.Varchar, Name);
			var rows = PostgresInterface.Select (conn, "config", new string[] {
				"name",
				"monoExecutable",
				"monoEnvironmentVariables",
				"monoOptions"
			}, "name = :name", parameters);

			if (rows.Count () == 0)
				return false;

			var row = rows.First ();

			if (!EqualsPostgresObject (row))
				throw new Exception (string.Format ("Error: Config {0} exists but is not the same as the local config of the same name.", Name));

			return true;
		}

		public string GetOrUploadToPostgres (NpgsqlConnection conn)
		{
			if (ExistsInPostgres (conn))
				return Name;

			Logging.GetLogging ().Info ("config " + Name + " not found - inserting");

			var row = new PostgresRow ();
			row.Set ("name", NpgsqlTypes.NpgsqlDbType.Varchar, Name);
			row.Set ("monoExecutable", NpgsqlTypes.NpgsqlDbType.Varchar, MonoExecutable);
			row.Set ("monoEnvironmentVariables", NpgsqlTypes.NpgsqlDbType.Jsonb, MonoEnvironmentVariables);
			row.Set ("monoOptions", NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Text, MonoOptions);
			return PostgresInterface.Insert<string> (conn, "config", row, "name");
		}
	}
}

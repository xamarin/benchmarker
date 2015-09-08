using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Npgsql;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Parse2Postgres
{
	class MainClass
	{
		static Dictionary<string, Dictionary<string, long>> idMap = new Dictionary<string, Dictionary<string, long>> ();
		static Dictionary<string, Dictionary<string, string>> stringIdMap = new Dictionary<string, Dictionary<string, string>> ();

		static long LastVal (NpgsqlConnection conn)
		{
			using (var cmd = new NpgsqlCommand ()) {
				cmd.Connection = conn;

				cmd.CommandText = "select lastval()";
				using (var reader = cmd.ExecuteReader())
				{
					if (!reader.Read ())
						throw new Exception ("no result from lastval()");
					return reader.GetInt64 (0);
				}
			}
		}

		static long? Insert (NpgsqlCommand cmd, string table, JToken entry, string primaryStringKey, ISet<string> namesAlreadySet)
		{
			var namesWithPrefixes = new List<string> ();
			var names = new List<string> ();
			string primaryKeyValue = null;
			string postgresName = null;
			if (primaryStringKey != null) {
				var postgresNameValue = entry ["postgresName"];
				if (postgresNameValue != null)
					postgresName = postgresNameValue.ToObject<string> ();
			}
				
			foreach (NpgsqlParameter parameter in cmd.Parameters) {
				var nameWithPrefix = parameter.ParameterName;
				namesWithPrefixes.Add (nameWithPrefix);
				var name = nameWithPrefix.Substring (1);
				names.Add (name);

				if (namesAlreadySet != null && namesAlreadySet.Contains (name))
					continue;

				var value = entry [name];
				if (name == primaryStringKey) {
					primaryKeyValue = value.ToObject<string> ();
					if (postgresName != null) {
						primaryKeyValue = postgresName;
						value = postgresName;
					}
				}
				switch (parameter.NpgsqlDbType) {
				case NpgsqlTypes.NpgsqlDbType.Integer:
					parameter.Value = value.ToObject<int> ();
					break;
				case NpgsqlTypes.NpgsqlDbType.Varchar:
					parameter.Value = value.ToObject<string> ();
					break;
				case NpgsqlTypes.NpgsqlDbType.Boolean:
					parameter.Value = value.ToObject<bool> ();
					break;
				case NpgsqlTypes.NpgsqlDbType.TimestampTZ:
					parameter.Value = value ["iso"].ToObject<DateTime> ();
					break;
				case NpgsqlTypes.NpgsqlDbType.Jsonb:
					parameter.Value = value.ToString ();
					break;
				case NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Text:
					var children = value.Children<JToken> ();
					var arr = children.Select (jt => jt.ToObject<string> ()).ToArray ();
					parameter.Value = arr;
					break;
				default:
					throw new Exception ("unknown type");
				}
			}

			cmd.CommandText = string.Format ("insert into {0} ({1}) values ({2})", table, string.Join (",", names), string.Join (",", namesWithPrefixes));

			cmd.ExecuteNonQuery ();

			var objectId = entry ["objectId"].ToObject<string> ();

			if (primaryKeyValue != null) {
				if (!stringIdMap.ContainsKey (table))
					stringIdMap.Add (table, new Dictionary<string, string> ());
				stringIdMap [table].Add (objectId, primaryKeyValue);
				Console.WriteLine ("{0} {1} -> {2}", table, objectId, primaryKeyValue);
				return null;
			} else {
				var id = LastVal (cmd.Connection);
				if (!idMap.ContainsKey (table))
					idMap.Add (table, new Dictionary<string, long> ());
				idMap [table].Add (objectId, id);
				Console.WriteLine ("{0} {1} -> {2}", table, objectId, id);
				return id;
			}
		}

		struct ParameterInfo {
			public string name;
			public NpgsqlTypes.NpgsqlDbType type;
			public int size;
		}

		struct ForeignKeyInfo {
			public string name;
			public string table;
			public bool isArray;
		}

		static object GetForeignKey (JToken entry, string table, bool isString) {
			if (entry ["__type"].ToObject<string> () != "Pointer" || entry ["className"].ToObject<string> () != table)
				throw new Exception ("invalid pointer");

			var objectId = entry ["objectId"].ToObject<string> ();

			if (isString) {
				return stringIdMap [table] [objectId];
			} else {
				long value;
				if (idMap [table].TryGetValue (objectId, out value))
					return value;
				return null;
			}
		}

		static void ConvertTable (NpgsqlConnection conn, string exportDir, string tableName,
			ParameterInfo[] parameterInfos, string primaryStringKey = null, ForeignKeyInfo[] foreignKeyInfos = null,
			Func<JToken, bool> predicate = null)
		{
			var root = JObject.Parse (File.ReadAllText (Path.Combine (exportDir, string.Format ("{0}.json", tableName))));
			var results = root ["results"];

			foreach (var entry in results) {
				if (predicate != null && !predicate (entry))
					continue;

				using (var cmd = new NpgsqlCommand())
				{
					cmd.Connection = conn;

					if (parameterInfos != null) {
						foreach (var info in parameterInfos) {
							var value = entry [info.name];
							if (value == null || value.Type == JTokenType.Null)
								continue;
							cmd.Parameters.Add ("@" + info.name, info.type, info.size);
						}
					}

					var namesAlreadySet = new HashSet<string> ();

					if (foreignKeyInfos != null) {
						foreach (var info in foreignKeyInfos) {
							var value = entry [info.name];
							bool isString;
							object postgresValue;
							NpgsqlTypes.NpgsqlDbType postgresType;

							if (value == null || value.Type == JTokenType.Null)
								continue;

							if (idMap.ContainsKey (info.table))
								isString = false;
							else if (stringIdMap.ContainsKey (info.table))
								isString = true;
							else
								throw new Exception ("unknown table for foreign key");

							if (info.isArray) {
								if (!isString)
									throw new Exception ("only support string foreign key arrays");
								postgresValue = value.Select (e => (string)GetForeignKey (e, info.table, true)).ToArray ();
								postgresType = NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Varchar;
							} else {
								postgresValue = GetForeignKey (value, info.table, isString);
								if (postgresValue == null)
									goto NextEntry;
								if (isString)
									postgresType = NpgsqlTypes.NpgsqlDbType.Varchar;
								else
									postgresType = NpgsqlTypes.NpgsqlDbType.Integer;
							}

							cmd.Parameters.Add ("@" + info.name, postgresType).Value = postgresValue;
							namesAlreadySet.Add (info.name);
						}
					}

					Insert (cmd, tableName, entry, primaryStringKey, namesAlreadySet);
				}
				NextEntry:
				;
			}
		}

		static void InsertObjectIds (NpgsqlConnection conn)
		{
			using (var cmd = new NpgsqlCommand ()) {
				cmd.Connection = conn;
				cmd.CommandText = "insert into ParseObjectID (tableName, parseID, integerKey) values (@table, @parse, @key)";
				var tableParameter = cmd.Parameters.Add ("@table", NpgsqlTypes.NpgsqlDbType.Varchar, 32);
				var parseParameter = cmd.Parameters.Add ("@parse", NpgsqlTypes.NpgsqlDbType.Char, 10);
				var keyParameter = cmd.Parameters.Add ("@key", NpgsqlTypes.NpgsqlDbType.Integer);

				foreach (var tableKvp in idMap) {
					var table = tableKvp.Key;
					tableParameter.Value = table;
					foreach (var mapKvp in tableKvp.Value) {
						var parseObjectId = mapKvp.Key;
						var primaryKey = mapKvp.Value;
						parseParameter.Value = parseObjectId;
						keyParameter.Value = primaryKey;
						cmd.ExecuteNonQuery ();
					}
				}
			}	

			using (var cmd = new NpgsqlCommand ()) {
				cmd.Connection = conn;
				cmd.CommandText = "insert into ParseObjectID (tableName, parseID, varcharKey) values (@table, @parse, @key)";
				var tableParameter = cmd.Parameters.Add ("@table", NpgsqlTypes.NpgsqlDbType.Varchar, 32);
				var parseParameter = cmd.Parameters.Add ("@parse", NpgsqlTypes.NpgsqlDbType.Char, 10);
				var keyParameter = cmd.Parameters.Add ("@key", NpgsqlTypes.NpgsqlDbType.Varchar, 128);

				foreach (var tableKvp in stringIdMap) {
					var table = tableKvp.Key;
					tableParameter.Value = table;
					foreach (var mapKvp in tableKvp.Value) {
						var parseObjectId = mapKvp.Key;
						var primaryKey = mapKvp.Value;
						parseParameter.Value = parseObjectId;
						keyParameter.Value = primaryKey;
						cmd.ExecuteNonQuery ();
					}
				}
			}
		}

		static void ConvertBenchmarks (NpgsqlConnection conn, string exportDir)
		{
			ConvertTable (conn, exportDir, "Benchmark",
				new ParameterInfo[] {
					new ParameterInfo { name = "name", type = NpgsqlTypes.NpgsqlDbType.Varchar, size = 128 }
				},
				"name");
		}

		static void ConvertMachines (NpgsqlConnection conn, string exportDir)
		{
			ConvertTable (conn, exportDir, "Machine",
				new ParameterInfo[] {
					new ParameterInfo { name = "name", type = NpgsqlTypes.NpgsqlDbType.Varchar, size = 128 },
					new ParameterInfo { name = "architecture", type = NpgsqlTypes.NpgsqlDbType.Varchar, size = 128 },
					new ParameterInfo { name = "isDedicated", type = NpgsqlTypes.NpgsqlDbType.Boolean }
				},
				"name");
		}

		static void ConvertCommits (NpgsqlConnection conn, string exportDir)
		{
			ConvertTable (conn, exportDir, "Commit",
				new ParameterInfo[] {
					new ParameterInfo { name = "hash", type = NpgsqlTypes.NpgsqlDbType.Varchar, size = 40 },
					new ParameterInfo { name = "commitDate", type = NpgsqlTypes.NpgsqlDbType.TimestampTZ }
				},
				"hash");
		}

		static void ConvertConfigs (NpgsqlConnection conn, string exportDir)
		{
			ConvertTable (conn, exportDir, "Config",
				new ParameterInfo[] {
					new ParameterInfo { name = "name", type = NpgsqlTypes.NpgsqlDbType.Varchar, size = 128 },
					new ParameterInfo { name = "monoExecutable", type = NpgsqlTypes.NpgsqlDbType.Varchar, size = 128 },
					new ParameterInfo { name = "monoEnvironmentVariables", type = NpgsqlTypes.NpgsqlDbType.Jsonb },
					new ParameterInfo { name = "monoOptions", type = NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Text }
				},
				"name");
		}

		static void ConvertRunSet (NpgsqlConnection conn, string exportDir, bool withPullRequests)
		{
			ConvertTable (conn, exportDir, "RunSet",
				new ParameterInfo[] {
					new ParameterInfo { name = "startedAt", type = NpgsqlTypes.NpgsqlDbType.TimestampTZ },
					new ParameterInfo { name = "finishedAt", type = NpgsqlTypes.NpgsqlDbType.TimestampTZ },
					new ParameterInfo { name = "buildURL", type = NpgsqlTypes.NpgsqlDbType.Varchar, size = 256 },
					new ParameterInfo { name = "elapsedTimeAverages", type = NpgsqlTypes.NpgsqlDbType.Jsonb },
					new ParameterInfo { name = "elapsedTimeVariances", type = NpgsqlTypes.NpgsqlDbType.Jsonb },
					new ParameterInfo { name = "failed", type = NpgsqlTypes.NpgsqlDbType.Boolean },
					new ParameterInfo { name = "logURLs", type = NpgsqlTypes.NpgsqlDbType.Jsonb }
				},
				null,
				new ForeignKeyInfo[] {
					new ForeignKeyInfo { name = "commit", table = "Commit" },
					new ForeignKeyInfo { name = "machine", table = "Machine" },
					new ForeignKeyInfo { name = "config", table = "Config" },
					new ForeignKeyInfo { name = "timedOutBenchmarks", table = "Benchmark", isArray = true },
					new ForeignKeyInfo { name = "crashedBenchmarks", table = "Benchmark", isArray = true },
					new ForeignKeyInfo { name = "pullRequest", table = "PullRequest" }
				},
				e => e ["pullRequest"] == null ? !withPullRequests : withPullRequests);
		}

		static void ConvertRun (NpgsqlConnection conn, string exportDir)
		{
			ConvertTable (conn, exportDir, "Run",
				new ParameterInfo[] {
					new ParameterInfo { name = "elapsedMilliseconds", type = NpgsqlTypes.NpgsqlDbType.Integer }
				},
				null,
				new ForeignKeyInfo[] {
					new ForeignKeyInfo { name = "benchmark", table = "Benchmark" },
					new ForeignKeyInfo { name = "runSet", table = "RunSet" }
				});
		}

		static void ConvertRegressionWarnings (NpgsqlConnection conn, string exportDir)
		{
			ConvertTable (conn, exportDir, "RegressionWarnings",
				null,
				null,
				new ForeignKeyInfo[] {
					new ForeignKeyInfo { name = "runSet", table = "RunSet" },
					new ForeignKeyInfo { name = "fasterBenchmarks", table = "Benchmark", isArray = true },
					new ForeignKeyInfo { name = "slowerBenchmarks", table = "Benchmark", isArray = true }
				});
		}

		static void ConvertPullRequest (NpgsqlConnection conn, string exportDir)
		{
			ConvertTable (conn, exportDir, "PullRequest",
				new ParameterInfo[] {
					new ParameterInfo { name = "URL", type = NpgsqlTypes.NpgsqlDbType.Varchar, size = 256 }
				},
				null,
				new ForeignKeyInfo[] {
					new ForeignKeyInfo { name = "baselineRunSet", table = "RunSet" }
				});
		}

		public static void Main (string[] args)
		{
			if (args.Length != 1) {
				Console.Error.WriteLine ("Usage: Parse2Postgres EXPORT-DIR");
				Environment.Exit (1);
			}

			var exportDir = args [0];

			using (var conn = new NpgsqlConnection("Host=192.168.99.100;Port=32768;Username=postgres;Password=mysecretpassword;Database=performance"))
			{
				conn.Open();

				ConvertBenchmarks (conn, exportDir);
				ConvertMachines (conn, exportDir);
				ConvertCommits (conn, exportDir);
				ConvertConfigs (conn, exportDir);
				ConvertRunSet (conn, exportDir, false);
				ConvertRun (conn, exportDir);
				ConvertRegressionWarnings (conn, exportDir);
				ConvertPullRequest (conn, exportDir);
				ConvertRunSet (conn, exportDir, true);

				InsertObjectIds (conn);
			}
		}
	}
}

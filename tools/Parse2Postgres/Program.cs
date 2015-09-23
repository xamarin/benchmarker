using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Npgsql;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Benchmarker;
using Common.Logging;
using Common.Logging.Simple;
using Benchmarker.Common;

namespace Parse2Postgres
{
	class MainClass
	{
		static Dictionary<string, Dictionary<string, long>> idMap = new Dictionary<string, Dictionary<string, long>> ();
		static Dictionary<string, Dictionary<string, string>> stringIdMap = new Dictionary<string, Dictionary<string, string>> ();

		struct PostgresName {
			public string name;
			public NpgsqlTypes.NpgsqlDbType type;
		};

		static void SetColumnValues (string table, IEnumerable<PostgresName> columnNames, IDictionary<string, object> columnValues, JToken entry, string primaryStringKey)
		{
			string primaryKeyValue = null;
			string postgresName = null;
			if (primaryStringKey != null) {
				var postgresNameValue = entry ["postgresName"];
				if (postgresNameValue != null)
					postgresName = postgresNameValue.ToObject<string> ();
			}

			foreach (var pgName in columnNames) {
				var name = pgName.name;
				if (columnValues.ContainsKey (name))
					continue;

				var value = entry [name];

				if (value == null || value.Type == JTokenType.Null) {
					columnValues [name] = null;
					continue;
				}

				if (name == primaryStringKey) {
					primaryKeyValue = value.ToObject<string> ();
					if (postgresName != null) {
						primaryKeyValue = postgresName;
						value = postgresName;
					}
				}

				object pgValue;

				switch (pgName.type) {
				case NpgsqlTypes.NpgsqlDbType.Integer:
					pgValue = value.ToObject<int> ();
					break;
				case NpgsqlTypes.NpgsqlDbType.Varchar:
					pgValue = value.ToObject<string> ();
					break;
				case NpgsqlTypes.NpgsqlDbType.Boolean:
					pgValue = value.ToObject<bool> ();
					break;
				case NpgsqlTypes.NpgsqlDbType.TimestampTZ:
					pgValue = value ["iso"].ToObject<DateTime> ();
					break;
				case NpgsqlTypes.NpgsqlDbType.Jsonb:
					pgValue = value.ToString ();
					break;
				case NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Text:
					var children = value.Children<JToken> ();
					var arr = children.Select (jt => jt.ToObject<string> ()).ToArray ();
					pgValue = arr;
					break;
				default:
					throw new Exception ("unknown type");
				}

				columnValues [name] = pgValue;
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
				string value;
				if (stringIdMap [table].TryGetValue (objectId, out value))
					return value;
				return null;
			} else {
				long value;
				if (idMap [table].TryGetValue (objectId, out value))
					return value;
				return null;
			}
		}

		static NpgsqlTypes.NpgsqlDbType PostgresTypeForForeignKeyInfo (ForeignKeyInfo info) {
			bool isString;

			if (idMap.ContainsKey (info.table))
				isString = false;
			else if (stringIdMap.ContainsKey (info.table))
				isString = true;
			else
				throw new Exception ("unknown table for foreign key");
			
			if (info.isArray) {
				if (!isString)
					throw new Exception ("only support string foreign key arrays");
				return NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Varchar;
			} else {
				if (isString)
					return NpgsqlTypes.NpgsqlDbType.Varchar;
				else
					return NpgsqlTypes.NpgsqlDbType.Integer;
			}
		}

		static void ConvertTable (NpgsqlConnection conn, string exportDir, string tableName,
			ParameterInfo[] parameterInfos, string primaryStringKey = null, ForeignKeyInfo[] foreignKeyInfos = null,
			Func<JToken, bool> predicate = null)
		{
			var root = JObject.Parse (File.ReadAllText (Path.Combine (exportDir, string.Format ("{0}.json", tableName))));
			var results = root ["results"];
			var columnNames = parameterInfos.Select (pi => new PostgresName { name = pi.name, type = pi.type }).ToList ();
			if (foreignKeyInfos != null) {
				columnNames.AddRange (foreignKeyInfos.Select (fki => new PostgresName {
					name = fki.name,
					type = PostgresTypeForForeignKeyInfo (fki)
				}));
			}
			var copyCommand = string.Format ("copy {0} ({1}) from stdin binary", tableName, string.Join (",", columnNames.Select (n => n.name)));

			using (var writer = conn.BeginBinaryImport (copyCommand)) {
				foreach (var entry in results) {
					if (predicate != null && !predicate (entry))
						continue;
					
					var columnValues = new Dictionary<string, object> ();

					if (foreignKeyInfos != null) {
						foreach (var info in foreignKeyInfos) {
							var value = entry [info.name];
							bool isString;
							object postgresValue;

							if (value == null || value.Type == JTokenType.Null) {
								columnValues [info.name] = null;
								continue;
							}

							if (idMap.ContainsKey (info.table))
								isString = false;
							else if (stringIdMap.ContainsKey (info.table))
								isString = true;
							else
								throw new Exception ("unknown table for foreign key");

							if (info.isArray) {
								if (!isString)
									throw new Exception ("only support string foreign key arrays");
								var valueArray = value.Select (e => (string)GetForeignKey (e, info.table, true)).ToArray ();
								postgresValue = valueArray;
								if (valueArray.Any (s => s == null)) {
									Console.WriteLine ("foreign key in array not found - skipping");
									goto NextEntry;
								}
							} else {
								postgresValue = GetForeignKey (value, info.table, isString);
								if (postgresValue == null) {
									Console.WriteLine ("foreign key not found - skipping");
									goto NextEntry;
								}
							}

							columnValues [info.name] = postgresValue;
						}
					}

					SetColumnValues (tableName, columnNames, columnValues, entry, primaryStringKey);

					writer.StartRow();
					foreach (var pgName in columnNames) {
						var name = pgName.name;
						var value = columnValues [name];
						if (value == null)
							writer.WriteNull ();
						else
							writer.Write (value, pgName.type);
					}

					NextEntry:
					;
				}
			}

			using (var cmd = conn.CreateCommand ()) {
				cmd.CommandText = string.Format ("select objectID, {0} from {1}", primaryStringKey ?? "id", tableName);
				using (var reader = cmd.ExecuteReader())
				{
					while (reader.Read ()) {
						var objectId = reader.GetString (0);

						if (primaryStringKey != null) {
							var primaryKeyValue = reader.GetString (1);
							if (!stringIdMap.ContainsKey (tableName))
								stringIdMap.Add (tableName, new Dictionary<string, string> ());
							if (!stringIdMap [tableName].ContainsKey (objectId)) {
								stringIdMap [tableName].Add (objectId, primaryKeyValue);
								Console.WriteLine ("{0} {1} -> {2}", tableName, objectId, primaryKeyValue);
							}
						} else {
							var id = reader.GetInt64 (1);
							if (!idMap.ContainsKey (tableName))
								idMap.Add (tableName, new Dictionary<string, long> ());
							if (!idMap [tableName].ContainsKey (objectId)) {
								idMap [tableName].Add (objectId, id);
								Console.WriteLine ("{0} {1} -> {2}", tableName, objectId, id);
							}
						}
					}
				}
			}
		}

		static void ConvertBenchmarks (NpgsqlConnection conn, string exportDir)
		{
			ConvertTable (conn, exportDir, "Benchmark",
				new ParameterInfo[] {
					new ParameterInfo { name = "objectId", type = NpgsqlTypes.NpgsqlDbType.Varchar, size = 10 },
					new ParameterInfo { name = "name", type = NpgsqlTypes.NpgsqlDbType.Varchar, size = 128 },
					new ParameterInfo { name = "disabled", type = NpgsqlTypes.NpgsqlDbType.Boolean }
				},
				"name");
		}

		static void ConvertMachines (NpgsqlConnection conn, string exportDir)
		{
			ConvertTable (conn, exportDir, "Machine",
				new ParameterInfo[] {
					new ParameterInfo { name = "objectId", type = NpgsqlTypes.NpgsqlDbType.Varchar, size = 10 },
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
					new ParameterInfo { name = "objectId", type = NpgsqlTypes.NpgsqlDbType.Varchar, size = 10 },
					new ParameterInfo { name = "hash", type = NpgsqlTypes.NpgsqlDbType.Varchar, size = 40 },
					new ParameterInfo { name = "commitDate", type = NpgsqlTypes.NpgsqlDbType.TimestampTZ }
				},
				"hash");
		}

		static void ConvertConfigs (NpgsqlConnection conn, string exportDir)
		{
			ConvertTable (conn, exportDir, "Config",
				new ParameterInfo[] {
					new ParameterInfo { name = "objectId", type = NpgsqlTypes.NpgsqlDbType.Varchar, size = 10 },
					new ParameterInfo { name = "name", type = NpgsqlTypes.NpgsqlDbType.Varchar, size = 128 },
					new ParameterInfo { name = "monoExecutable", type = NpgsqlTypes.NpgsqlDbType.Varchar, size = 128 },
					new ParameterInfo { name = "monoEnvironmentVariables", type = NpgsqlTypes.NpgsqlDbType.Jsonb },
					new ParameterInfo { name = "monoOptions", type = NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Text }
				},
				"name");
		}

		static void ConvertRunSet (NpgsqlConnection conn, string exportDir, bool withPullRequests)
		{
			var foreignKeyInfos = new ForeignKeyInfo[] {
				new ForeignKeyInfo { name = "commit", table = "Commit" },
				new ForeignKeyInfo { name = "machine", table = "Machine" },
				new ForeignKeyInfo { name = "config", table = "Config" },
				new ForeignKeyInfo { name = "timedOutBenchmarks", table = "Benchmark", isArray = true },
				new ForeignKeyInfo { name = "crashedBenchmarks", table = "Benchmark", isArray = true }
			};

			if (withPullRequests) {
				foreignKeyInfos = foreignKeyInfos.Concat (new ForeignKeyInfo[] {
					new ForeignKeyInfo { name = "pullRequest", table = "PullRequest" }
				}).ToArray ();
			}

			ConvertTable (conn, exportDir, "RunSet",
				new ParameterInfo[] {
					new ParameterInfo { name = "objectId", type = NpgsqlTypes.NpgsqlDbType.Varchar, size = 10 },
					new ParameterInfo { name = "startedAt", type = NpgsqlTypes.NpgsqlDbType.TimestampTZ },
					new ParameterInfo { name = "finishedAt", type = NpgsqlTypes.NpgsqlDbType.TimestampTZ },
					new ParameterInfo { name = "buildURL", type = NpgsqlTypes.NpgsqlDbType.Varchar, size = 256 },
					new ParameterInfo { name = "elapsedTimeAverages", type = NpgsqlTypes.NpgsqlDbType.Jsonb },
					new ParameterInfo { name = "elapsedTimeVariances", type = NpgsqlTypes.NpgsqlDbType.Jsonb },
					new ParameterInfo { name = "failed", type = NpgsqlTypes.NpgsqlDbType.Boolean },
					new ParameterInfo { name = "logURLs", type = NpgsqlTypes.NpgsqlDbType.Jsonb }
				},
				null,
				foreignKeyInfos,
				e => e ["pullRequest"] == null ? !withPullRequests : withPullRequests);
		}

		static void ConvertRun (NpgsqlConnection conn, string exportDir)
		{
			ConvertTable (conn, exportDir, "Run",
				new ParameterInfo[] {
					new ParameterInfo { name = "objectId", type = NpgsqlTypes.NpgsqlDbType.Varchar, size = 10 },
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
				new ParameterInfo[] {
					new ParameterInfo { name = "objectId", type = NpgsqlTypes.NpgsqlDbType.Varchar, size = 10 }
				},
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
					new ParameterInfo { name = "objectId", type = NpgsqlTypes.NpgsqlDbType.Varchar, size = 10 },
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

			LogManager.Adapter = new ConsoleOutLoggerFactoryAdapter();
			Logging.SetLogging (LogManager.GetLogger<MainClass> ());

			using (var conn = PostgresInterface.Connect ())
			{
				ConvertBenchmarks (conn, exportDir);
				ConvertMachines (conn, exportDir);
				ConvertCommits (conn, exportDir);
				ConvertConfigs (conn, exportDir);
				ConvertRunSet (conn, exportDir, false);
				ConvertRegressionWarnings (conn, exportDir);
				ConvertPullRequest (conn, exportDir);
				ConvertRunSet (conn, exportDir, true);
				// do this last because of pull request run sets
				ConvertRun (conn, exportDir);
			}
		}
	}
}

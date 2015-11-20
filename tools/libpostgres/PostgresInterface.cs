using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Npgsql;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Benchmarker
{
	public class PostgresInterface
	{
		public static NpgsqlConnection Connect ()
		{
			var credentials = Accredit.GetCredentials ("benchmarkerPostgres");
			var host = credentials ["host"].ToObject<string> ();
			var port = credentials ["port"].ToObject<int> ();
			var database = credentials ["database"].ToObject<string> ();
			var user = credentials ["user"].ToObject<string> ();
			var password = credentials ["password"].ToObject<string> ();

			var connectionString = string.Format ("Host={0};Port={1};Username={2};Password={3};Database={4};SslMode=Require;TrustServerCertificate=true",
				host, port, user, password, database);

			var conn = new NpgsqlConnection (connectionString);
			conn.Open();

			return conn;
		}

		static void AddParameters (NpgsqlCommand cmd, PostgresRow row) {
			foreach (var column in row.Columns) {
				var type = row.ColumnType (column);
				var obj = row.GetReference<object> (column);
				if (obj != null && type == NpgsqlTypes.NpgsqlDbType.Unknown && obj.GetType () == typeof(string[]))
					type = NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Varchar;
				var param = cmd.Parameters.Add (column, type);
				if (obj == null)
					param.Value = DBNull.Value;
				else if (type == NpgsqlTypes.NpgsqlDbType.Jsonb)
					param.Value = JsonConvert.SerializeObject (obj);
				else
					param.Value = obj;
			}
		}


		static IEnumerable<PostgresRow> Select (NpgsqlConnection conn, string queryString, IEnumerable<string> columnsEnumerable, PostgresRow whereValues) {
			var columns = columnsEnumerable.ToArray ();
			using (var cmd = conn.CreateCommand ()) {
				cmd.CommandText = queryString;
				if (whereValues != null)
					AddParameters (cmd, whereValues);

				var rows = new List<PostgresRow> ();
				using (var reader = cmd.ExecuteReader ()) {
					while (reader.Read ()) {
						var row = new PostgresRow ();
						for (var i = 0; i < columns.Length; ++i) {
							var value = reader.GetValue (i);
							if (value.GetType () == typeof(DateTime))
								value = (object)TimeZoneInfo.ConvertTimeToUtc ((DateTime)value);
							row.Set (columns [i], NpgsqlTypes.NpgsqlDbType.Unknown, value);
						}
						rows.Add (row);
					}
				}
				return rows;
			}
		}

		public static IEnumerable<PostgresRow> Select (NpgsqlConnection conn, IDictionary<string, string> tableNames, IEnumerable<string> columnsEnumerable, string whereClause, PostgresRow whereValues) {
			var tables = string.Join (",", tableNames.Select (kvp => kvp.Value == null ? kvp.Key : kvp.Key + " " + kvp.Value));
			var columns = columnsEnumerable.ToArray ();
			var whereString = whereClause == null ? "" : string.Format ("where {0}", whereClause);
			var queryString = string.Format ("select {0} from {1} {2}", string.Join (",", columns), tables, whereString);
			return Select (conn, queryString, columns, whereValues);
		}

		public static IEnumerable<PostgresRow> Select (NpgsqlConnection conn, string table, IEnumerable<string> columnsEnumerable, string whereClause = null, PostgresRow whereValues = null) {
			return Select (conn, new Dictionary<string, string> { [table ] = null }, columnsEnumerable, whereClause, whereValues);
		}

		public static T Insert<T> (NpgsqlConnection conn, string table, PostgresRow row, string keyColumn) {
			var columns = row.Columns;
			var commandString = string.Format ("insert into {0} ({1}) values ({2}) returning {3}",
				table,
				string.Join (",", columns),
				string.Join (",", columns.Select (c => ":" + c)),
				keyColumn);
			using (var cmd = conn.CreateCommand ()) {
				cmd.CommandText = commandString;
				AddParameters (cmd, row);

				using (var reader = cmd.ExecuteReader ()) {
					if (!reader.Read ())
						throw new Exception ("Error: Failure inserting into " + table);

					return reader.GetFieldValue<T> (0);
				}
			}
		}

		public static void Update (NpgsqlConnection conn, string table, PostgresRow row, string keyColumn) {
			var columns = row.Columns;
			var commandString = string.Format ("update {0} set {1} where {2} = :{2}",
				table,
				string.Join (",", columns.Select (c => c + "=:" + c)),
				keyColumn);
			using (var cmd = conn.CreateCommand ()) {
				cmd.CommandText = commandString;
				AddParameters (cmd, row);
				var numRows = cmd.ExecuteNonQuery ();
				if (numRows != 1)
					throw new Exception ("Error: Could not update row in " + table);
			}
		}

		public static int Delete (NpgsqlConnection conn, string table, string whereClause, PostgresRow row) {
			var columns = row.Columns;
			var commandString = string.Format ("delete from {0} where {1}", table, whereClause);
			using (var cmd = conn.CreateCommand ()) {
				cmd.CommandText = commandString;
				AddParameters (cmd, row);
				return cmd.ExecuteNonQuery ();
			}
		}

		const int numRetries = 3;

		public static T RunInTransactionWithRetry<T> (NpgsqlConnection conn, Func<NpgsqlConnection, T> action, out bool success) {
			success = true;
			var retryTime = 10; // in seconds
			for (var i = 0; i < numRetries; ++i) {
				try {
					if (conn.State != System.Data.ConnectionState.Open) {
						Console.Error.WriteLine ("Database connection isn't open - reopening.");
						conn.Open ();
					}
						
					var transaction = conn.BeginTransaction ();
					var result = action (conn);
					transaction.Commit ();
					return result;
				} catch (Exception e) {
					if (i + 1 < numRetries) {
						Console.Error.WriteLine ("Error: Transaction failed.  Retrying after {0} seconds.  Exception: {1}", retryTime, e);
						Thread.Sleep (retryTime * 1000);
						retryTime *= 3;

					} else {
						Console.Error.WriteLine ("Error: Transaction failed.  Giving up.  Exception: {0}", e);
					}
				}
			}
			success = false;
			return default(T);
		}
	}

	public class PostgresRow
	{
		struct Value {
			public NpgsqlTypes.NpgsqlDbType type;
			public object v;
		}

		Dictionary<string, Value> values;

		public PostgresRow () {
			values = new Dictionary<string, Value> ();
		}
		
		public IEnumerable<string> Columns {
			get {
				return values.Keys;
			}
		}

		public T GetReference<T> (string column) where T : class {
			var value = values [column];
			if (value.v == null || value.v.GetType () == typeof (DBNull))
				return null;

			if (typeof (T) == typeof (JToken) || typeof (T) == typeof (JObject))
				return (T)JsonConvert.DeserializeObject ((string)value.v);

			return (T)value.v;
		}

		public T? GetValue<T> (string column) where T : struct {
			var value = values [column];
			if (value.v == null)
				return null;
			if (typeof(T) == typeof(long) && value.v.GetType () == typeof(int)) {
				var boxedLong = (object)(long)(int)value.v;
				return (T)boxedLong;
			}

			return (T)value.v;
		}

		public NpgsqlTypes.NpgsqlDbType ColumnType (string column) {
			var value = values [column];
			return value.type;
		}

		public void Set (string column, NpgsqlTypes.NpgsqlDbType type, object v) {
			values [column] = new Value { type = type, v = v };
		}

		public void TakeValuesFrom (PostgresRow row, string prefix) {
			foreach (var key in row.values.Keys) {
				if (key.StartsWith (prefix)) {
					var value = row.values [key];
					values.Add (key.Substring (prefix.Length), value);
				}
			}
		}
	}
}

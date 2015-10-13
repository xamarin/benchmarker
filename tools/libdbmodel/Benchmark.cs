using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using Benchmarker;
using Npgsql;

namespace Benchmarker.Models
{
    public class Benchmark
	{
		public string Name { get; set; }
		public string TestDirectory { get; set; }
		public string[] CommandLine { get; set; }
		public string[] ClientCommandLine { get; set; }

		static Dictionary<string, PostgresRow> nameToRow;

		public Benchmark ()
		{
		}

		public static Benchmark LoadFromString (string jsonContent)
		{
			var benchmark = JsonConvert.DeserializeObject<Benchmark> (jsonContent);

			if (String.IsNullOrWhiteSpace (benchmark.TestDirectory))
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

		static void FetchBenchmarks (NpgsqlConnection conn)
		{
			if (nameToRow != null)
				return;

			nameToRow = new Dictionary<string, PostgresRow> ();
			var rows = PostgresInterface.Select (conn, "Benchmark", new string[] { "name" });
			foreach (var r in rows)
				nameToRow [r.GetReference<string> ("name")] = r;
		}

		public string GetOrUploadToPostgres (NpgsqlConnection conn)
		{
			FetchBenchmarks (conn);

			if (nameToRow.ContainsKey (Name))
				return Name;

			var row = new PostgresRow ();
			row.Set ("name", NpgsqlTypes.NpgsqlDbType.Varchar, Name);
			return PostgresInterface.Insert<string> (conn, "Benchmark", row, "name");
		}
	}
}

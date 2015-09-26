using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Npgsql;

namespace Benchmarker.Models
{
	public class Result
	{
		public DateTime DateTime { get; set; }
		public Benchmark Benchmark { get; set; }
		public Config Config { get; set; }
		public string Version { get; set; }

		List<Run> runs;
		public List<Run> Runs { get { return runs; } }

		public Result ()
		{
			runs = new List<Run> ();
		}

		public static Result LoadFromString (string content)
		{
			return JsonConvert.DeserializeObject<Result> (content);
		}

		public class Run {
			public TimeSpan WallClockTime { get; set; }
			public string Output { get; set; }
			public string Error { get; set; }
		}

		public void UploadRunsToPostgres (NpgsqlConnection conn, long runSetId) {
			var b = Benchmark.GetOrUploadToPostgres (conn);
			foreach (var run in Runs) {
				var row = new PostgresRow ();
				row.Set ("benchmark", NpgsqlTypes.NpgsqlDbType.Varchar, b);
				row.Set ("runSet", NpgsqlTypes.NpgsqlDbType.Integer, runSetId);
				var runId = PostgresInterface.Insert<long> (conn, "Run", row, "id");

				var metricRow = new PostgresRow ();
				metricRow.Set ("run", NpgsqlTypes.NpgsqlDbType.Integer, runId);
				metricRow.Set ("metric", NpgsqlTypes.NpgsqlDbType.Varchar, "time");
				metricRow.Set ("result", NpgsqlTypes.NpgsqlDbType.Double, run.WallClockTime.TotalMilliseconds);
				PostgresInterface.Insert<long> (conn, "RunMetric", metricRow, "id");
			}
		}
	}
}

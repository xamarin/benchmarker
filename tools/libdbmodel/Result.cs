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

		public class RunMetric {
			public enum MetricType {
				Time,
				MemoryIntegral,
				Instructions,
				CacheMissRate,
				BranchMispredictionRate,
				CachegrindResults
			};

			public MetricType Metric { get; set; }
			public object Value { get; set; }

			public string MetricName {
				get {
					switch (Metric) {
					case MetricType.Time:
						return "time";
					case MetricType.MemoryIntegral:
						return "memory-integral";
					case MetricType.Instructions:
						return "instructions";
					case MetricType.CacheMissRate:
						return "cache-miss";
					case MetricType.BranchMispredictionRate:
						return "branch-mispred";
					case MetricType.CachegrindResults:
						return "cachegrind";
					default:
						throw new Exception ("unknown metric type");
					}
				}
			}

			public double PostgresValue {
				get {
					switch (Metric) {
					case MetricType.Time:
						return ((TimeSpan)Value).TotalMilliseconds;
					case MetricType.MemoryIntegral:
					case MetricType.CacheMissRate:
					case MetricType.BranchMispredictionRate:
						return (double)Value;
					case MetricType.Instructions:
						return (double)(long)Value;
					default:
						throw new Exception ("unknown metric type");
					}
				}
			}
		}

		public class Run {
			List<RunMetric> runMetrics;
			public List<RunMetric> RunMetrics { get { return runMetrics; } }
			public string BinaryProtocolFilename { get; set; }
			public long? PostgresId { get; set; }

			public Run () {
				runMetrics = new List<RunMetric> ();
			}
		}

		public void UploadRunsToPostgres (NpgsqlConnection conn, long runSetId) {
			var b = Benchmark.GetOrUploadToPostgres (conn);
			foreach (var run in Runs) {
				var row = new PostgresRow ();
				row.Set ("benchmark", NpgsqlTypes.NpgsqlDbType.Varchar, b);
				row.Set ("runSet", NpgsqlTypes.NpgsqlDbType.Integer, runSetId);
				var runId = PostgresInterface.Insert<long> (conn, "Run", row, "id");
				run.PostgresId = runId;

				foreach (var runMetric in run.RunMetrics) {
					var metricRow = new PostgresRow ();
					metricRow.Set ("run", NpgsqlTypes.NpgsqlDbType.Integer, runId);
					metricRow.Set ("metric", NpgsqlTypes.NpgsqlDbType.Varchar, runMetric.MetricName);
					if (runMetric.Metric == RunMetric.MetricType.CachegrindResults)
						metricRow.Set ("resultArray", NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Double, (double[])runMetric.Value);
					else
						metricRow.Set ("result", NpgsqlTypes.NpgsqlDbType.Double, runMetric.PostgresValue);
					PostgresInterface.Insert<long> (conn, "RunMetric", metricRow, "id");
				}
			}
		}
	}
}

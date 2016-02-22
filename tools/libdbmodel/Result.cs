using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Benchmarker.Models
{
	public class RunMetric
	{
		public enum MetricType
		{
			Time,
			MemoryIntegral,
			Instructions,
			CacheMissRate,
			BranchMispredictionRate,
			CachegrindResults,
			PauseTimes,
			PauseStarts
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
				case MetricType.PauseTimes:
					return "pause-times";
				case MetricType.PauseStarts:
					return "pause-starts";
				default:
					throw new Exception ("unknown metric type");
				}
			}
		}

		public object ApiValue {
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
				case MetricType.CachegrindResults:
				case MetricType.PauseTimes:
				case MetricType.PauseStarts:
					return (double[])Value;
				default:
					throw new Exception ("unknown metric type");
				}
			}
		}
	}

	public class Run : ApiObject
	{
		public Benchmark Benchmark { get; set; }

		List<RunMetric> runMetrics;

		public List<RunMetric> RunMetrics { get { return runMetrics; } }

		public string BinaryProtocolFilename { get; set; }

		public Run ()
		{
			runMetrics = new List<RunMetric> ();
		}

		public IDictionary<string, object> AsDict ()
		{
			var dict = new Dictionary<string, object> ();
			if (Benchmark != null)
				dict ["Benchmark"] = Benchmark.Name;
			var results = new Dictionary<string, object> ();
			foreach (var runMetric in RunMetrics) {
				results [runMetric.MetricName] = runMetric.ApiValue;
			}
			dict ["Results"] = results;
			return dict;
		}

		public async Task<bool> UploadForAmend (long id)
		{
			var responseBody = await HttpApi.PostRun (id, this);
			return responseBody != null;
		}
	}
}

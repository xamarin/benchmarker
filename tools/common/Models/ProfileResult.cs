using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.IO.Compression;

namespace Benchmarker.Common.Models
{
	public class ProfileResult
	{
		public DateTime DateTime { get; set; }
		public Benchmark Benchmark { get; set; }
		public Config Config { get; set; }
		public Revision Revision { get; set; }
		public bool Timedout { get; set; }
		public Run[] Runs { get; set; }

		public void StoreTo (string filename, bool compress = false)
		{
			using (var file = new FileStream (filename, FileMode.Create))
			using (var stream = compress ? (Stream) new GZipStream (file, CompressionMode.Compress) : (Stream) file)
			using (var writer = new StreamWriter (stream)) {
				writer.Write (JsonConvert.SerializeObject (this, Formatting.Indented));
			}
		}

		public static ProfileResult LoadFrom (string filename, bool compressed = false)
		{
			using (var file = new FileStream (filename, FileMode.Create))
			using (var stream = compressed ? (Stream) new GZipStream (file, CompressionMode.Decompress) : (Stream) file)
			using (var reader = new StreamReader (stream)) {
				return JsonConvert.DeserializeObject<ProfileResult> (reader.ReadToEnd ());
			}
		}

		public override string ToString ()
		{
			if (Benchmark == null)
				throw new ArgumentNullException ("Benchmark");
			if (Config == null)
				throw new ArgumentNullException ("Config");
			if (DateTime == default(DateTime))
				throw new ArgumentNullException ("DateTime");

			return String.Join ("_", new string [] { Benchmark.Name, Config.Name, DateTime.ToString ("s").Replace (':', '-') });
		}

		public class Run {
			public int Index { get; set; }
			public TimeSpan WallClockTime { get; set; }
			public string Output { get; set; }
			public string Error { get; set; }
			public string ProfilerOutput { get; set; }
			public Dictionary<LogProfiler.Counter, SortedDictionary<TimeSpan, object>> Counters { get; set; }

			public static Dictionary<LogProfiler.Counter, SortedDictionary<TimeSpan, object>> ParseCounters (string file)
			{
				if (!File.Exists (file))
					throw new InvalidDataException (String.Format ("File \"{0}\"  does not exists", file));

				var counters = new Dictionary<LogProfiler.Counter, SortedDictionary<TimeSpan, object>> ();

				var reader = new LogProfiler.Reader (file);

				reader.CountersDescription += (sender, e) => counters.Add (e.Counter, new SortedDictionary<TimeSpan, object> ());
				reader.CountersSample += (sender, e) => counters [e.Counter].Add (e.Timestamp, e.Value);
				reader.Run ();

				return counters;
			}
		}
	}
}

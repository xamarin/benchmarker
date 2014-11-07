using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Benchmarker.Common.Models
{
	public class Profile
	{
		public DateTime DateTime { get; set; }
		public Benchmark Benchmark { get; set; }
		public Config Config { get; set; }
		public Revision Revision { get; set; }
		public bool Timedout { get; set; }
		public Run[] Runs { get; set; }

		public void StoreTo (string filename)
		{
			using (var writer = new StreamWriter (new FileStream (filename, FileMode.Create))) {
				writer.Write (JsonConvert.SerializeObject (this, Formatting.Indented));
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

			return String.Join ("_", new string [] { Benchmark.Name, Config.Name, DateTime.ToString ("s") });
		}

		public class Run {
			public int Index { get; set; }
			public TimeSpan Time { get; set; }
			public string Output { get; set; }
			public string Error { get; set; }
			public string ProfilerOutput { get; set; }

			public Dictionary<LogProfiler.Counter, SortedDictionary<TimeSpan, object>> GetCounters (string directory)
			{
				if (String.IsNullOrEmpty (ProfilerOutput))
					throw new ArgumentNullException ("ProfilerOutput");

				var file = Path.Combine (directory, ProfilerOutput);
				if (!File.Exists (file))
					throw new InvalidDataException (String.Format ("ProfilerOutput file \"{0}\" in directory \"{1}\"  does not exists", ProfilerOutput, directory));

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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Caching;
using Newtonsoft.Json;
//using Benchmarker.Common.LogProfiler;

namespace Benchmarker.Common.Models
{
	public class ProfileResult
	{
		public DateTime DateTime { get; set; }
		public Benchmark Benchmark { get; set; }
		public Config Config { get; set; }
		public Revision Revision { get; set; }
		public bool Timedout { get; set; }

		List<Run> runs;
		public List<Run> Runs { get { return runs; } }

		public ProfileResult ()
		{
			runs = new List<Run> ();
		}

		public void StoreTo (string filename, bool compress = false)
		{
			using (var file = new FileStream (filename, FileMode.Create))
			using (var writer = new StreamWriter ((compress ? (Stream) new GZipStream (file, CompressionMode.Compress) : (Stream) file)))
			using (var jsonwriter = new JsonTextWriter (writer)) {
				new JsonSerializer { Formatting = Formatting.Indented }.Serialize (jsonwriter, this);
			}
		}

		public static ProfileResult LoadFrom (string filename, bool compressed = false)
		{
			using (var file = new FileStream (filename, FileMode.Open))
			using (var reader = new StreamReader (compressed ? (Stream) new GZipStream (file, CompressionMode.Decompress, true) : (Stream) file))
			using (var jsonreader = new JsonTextReader (reader)) {
				var profileresult = new JsonSerializer ().Deserialize<ProfileResult> (jsonreader);

				foreach (var run in profileresult.Runs)
					run.CountersDirectory = Directory.GetParent (filename).FullName;

				return profileresult;
			}
		}

		public class Run {
			public int Index { get; set; }
			public TimeSpan WallClockTime { get; set; }
			public string ProfilerOutput { get; set; }

			internal string CountersDirectory { get; set; }
			//public string CountersFile { get; set; }

			static private MemoryCache counterscache = MemoryCache.Default;
			static private object counterscachelock = new object ();

#if false
			private List<KeyValuePair<Counter, SortedDictionary<TimeSpan, object>>> counters;

			public List<KeyValuePair<Counter, SortedDictionary<TimeSpan, object>>> Counters {
				get {
					if (counters != null)
						return counters;

					if (String.IsNullOrEmpty (CountersDirectory) || String.IsNullOrEmpty (CountersFile))
						return null;

					List<KeyValuePair<Counter, SortedDictionary<TimeSpan, object>>> target;

					var path = Path.Combine (CountersDirectory, CountersFile);

					lock (counterscachelock) {
						target = counterscache [path] as List<KeyValuePair<Counter, SortedDictionary<TimeSpan, object>>>;
						if (target != null)
							return target;
					}

					target = LoadCountersFrom (path);

					lock (counterscachelock) {
						counterscache.Set (path, target, new CacheItemPolicy ());
					}

					return target;
				}
				set {
					counters = value;
				}
			}

			public static List<KeyValuePair<Counter, SortedDictionary<TimeSpan, object>>> ParseCounters (string file)
			{
				if (!File.Exists (file))
					throw new InvalidDataException (String.Format ("File \"{0}\"  does not exists", file));

				var counters = new Dictionary<Counter, SortedDictionary<TimeSpan, object>> ();

				var reader = new LogProfiler.Reader (file);

				reader.CountersDescription += (sender, e) => counters.Add (e.Counter, new SortedDictionary<TimeSpan, object> ());
				reader.CountersSample += (sender, e) => counters [e.Counter].Add (e.Timestamp, e.Value);
				reader.Run ();

				return counters.ToList ();
			}

			public void StoreCountersTo (string filename)
			{
				if (counters == null)
					throw new ArgumentNullException ("Counters");

				using (var file = new GZipStream (new FileStream (filename, FileMode.Create), CompressionMode.Compress))
				using (var writer = new StreamWriter (file))
				using (var jsonwriter = new JsonTextWriter (writer)) {
					new JsonSerializer { Formatting = Formatting.Indented }.Serialize (jsonwriter, counters);
				}
			}

			public static List<KeyValuePair<Counter, SortedDictionary<TimeSpan, object>>> LoadCountersFrom (string filename)
			{
				using (var file = new GZipStream (new FileStream (filename, FileMode.Open), CompressionMode.Decompress, true))
				using (var reader = new StreamReader (file))
				using (var jsonreader = new JsonTextReader (reader)) {
					return new JsonSerializer ().Deserialize<List<KeyValuePair<Counter, SortedDictionary<TimeSpan, object>>>> (jsonreader);
				}
			}

			public bool ShouldSerializeCounters ()
			{
				return String.IsNullOrWhiteSpace (CountersFile);
			}
#endif
		}
	}
}

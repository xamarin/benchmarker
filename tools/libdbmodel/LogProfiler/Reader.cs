#if false

using System;
using System.Collections.Generic;
using XamarinProfiler.Core;
using XamarinProfiler.Core.Reader;
using System.Linq;

namespace Benchmarker.Common.LogProfiler
{
	public class Reader
	{
		LogReader LogReader;
		Dictionary<Counter, object> Counters = new Dictionary<Counter, object> ();

		public delegate void CountersDescriptionEventHandler (object sender, CountersDescriptionEventArgs e);
		public event CountersDescriptionEventHandler CountersDescription;

		public delegate void CountersSampleEventHandler (object sender, CountersSampleEventArgs e);
		public event CountersSampleEventHandler CountersSample;

		public Reader (string filename)
		{
			LogReader = new LogReader (filename, true);
		}

		public void Run ()
		{
			if (!LogReader.OpenReader ())
				return;

			foreach (var e in LogReader.ReadEvents ()) {
				var countersdescevent = e as SampleCountersDescEvent;
				if (countersdescevent != null) {
					Counter counter;

					foreach (var t in countersdescevent.Counters) {
						Counters [counter = new Counter (t.Item1, t.Item2, t.Item3, t.Item4, t.Item5, t.Item6)] = null;

						if (CountersDescription != null) {
							CountersDescription (this, new CountersDescriptionEventArgs { Counter = counter });
						}
					}

					continue;
				}

				var countersevent = e as SampleCountersEvent;
				if (countersevent != null) {
					if (countersevent.Samples.Count == 0)
						return;

					foreach (var counter in new List<Counter> (Counters.Keys)) {
						var value = countersevent.Samples.FirstOrDefault (v => v.Item1 == counter.Index);

						if (value != null)
							Counters [counter] = value.Item3;

						if (CountersSample != null) {
							CountersSample (this, new CountersSampleEventArgs {
								Timestamp = TimeSpan.FromMilliseconds (countersevent.Timestamp), Counter = counter, Value = Counters [counter]
							});
						}
					}

					continue;
				}
			}
		}

		public class CountersDescriptionEventArgs : EventArgs
		{
			public Counter Counter  { get; internal set; }
		}

		public class CountersSampleEventArgs : EventArgs
		{
			public TimeSpan Timestamp { get; internal set; }
			public Counter Counter  { get; internal set; }
			public object Value { get; internal set; }
		}
	}
}

#endif
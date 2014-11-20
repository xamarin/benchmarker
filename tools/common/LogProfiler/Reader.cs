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
		ReaderEventListener Listener;

		public delegate void CountersDescriptionEventHandler (object sender, CountersDescriptionEventArgs e);
		public event CountersDescriptionEventHandler CountersDescription;

		public delegate void CountersSampleEventHandler (object sender, CountersSampleEventArgs e);
		public event CountersSampleEventHandler CountersSample;

		public Reader (string filename)
		{
			LogReader = new LogReader (filename, true);
			Listener = new ReaderEventListener (this);
		}

		public void Run ()
		{
			if (!LogReader.OpenReader ())
				return;

			foreach (var buf in LogReader.ReadBuffer (Listener)) {
				if (LogReader.IsStopping)
					break;
				if (buf == null)
					continue;
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

		class ReaderEventListener : EventListener
		{
			Reader Reader;

			Dictionary<Counter, object> Counters = new Dictionary<Counter, object> ();

			public ReaderEventListener (Reader reader)
			{
				Reader = reader;
			}

			public override void HandleSampleCountersDesc (List<Tuple<string, string, ulong, ulong, ulong, ulong>> counters)
			{
				Counter counter;

				foreach (var t in counters) {
					Counters [counter = new Counter (t.Item1, t.Item2, t.Item3, t.Item4, t.Item5, t.Item6)] = null;

					if (Reader.CountersDescription != null) {
						Reader.CountersDescription (this, new CountersDescriptionEventArgs { Counter = counter });
					}
				}
			}

			public override void HandleSampleCounters (ulong timestamp, List<Tuple<ulong, ulong, object>> values)
			{
				if (values.Count == 0)
					return;

				foreach (var counter in new List<Counter> (Counters.Keys)) {
					var value = values.FirstOrDefault (v => v.Item1 == counter.Index);

					if (value != null)
						Counters [counter] = value.Item3;

					if (Reader.CountersSample != null) {
						Reader.CountersSample (this, new CountersSampleEventArgs {
							Timestamp = TimeSpan.FromMilliseconds (timestamp), Counter = counter, Value = Counters [counter]
						});
					}
				}
			}
		}
	}
}

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

	public class Counter {
		public readonly string Section;
		public readonly string Name;
		public readonly CounterType Type;
		public readonly CounterUnit Unit;
		public readonly CounterVariance Variance;
		public readonly ulong Index;

		public Counter (string section, string name, ulong type, ulong unit, ulong variance, ulong counterID)
		{
			Section = section;
			Name = name;
			Type = (CounterType) type;
			Unit = (CounterUnit) unit;
			Variance = (CounterVariance) variance;
			Index = counterID;
		}

		public override string ToString ()
		{
			return String.Format ("{0} - {1} [{2}, {3}, {4}]", Section, Name, Type.ToString (), Unit.ToString (), Variance.ToString ());
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;

			var counter = obj as Counter;
			if (counter == null)
				return false;

			return Index == counter.Index;
		}

		public override int GetHashCode ()
		{
			return Index.GetHashCode ();
		}
	}

	public enum CounterType : ulong
	{
		Int = 0UL,
		UInt = 1UL,
		Word = 2UL,
		Long = 3UL,
		ULong = 4UL,
		Double = 5UL,
		String = 6UL,
		TimeInterval = 7UL,
	}

	public enum CounterUnit : ulong
	{
		Raw = 0UL << 24,
		Bytes = 1UL << 24,
		Time = 2UL << 24,
		Count = 3UL << 24,
		Percentage = 4UL << 24,
	}

	public enum CounterVariance : ulong
	{
		Monotonic = 1UL << 28,
		Constant = 1UL << 29,
		Variable = 1UL << 30,
	}
}

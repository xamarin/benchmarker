using System;
using System.Collections.Generic;
using XamarinProfiler.Core;
using XamarinProfiler.Core.Reader;

namespace Benchmarker.Common.LogProfiler
{
	public class Reader
	{
		LogReader LogReader;
		ReaderEventListener Listener;

		public delegate void SampleEventHandler (object sender, CountersSampleEventArgs e);
		public event SampleEventHandler CountersSample;
		public event SampleEventHandler CountersUpdatedSample;

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

		public class CountersSampleEventArgs : EventArgs
		{
			public ulong Timestamp { get; internal set; }
			public List<Counter> Counters  { get; internal set; }
		}

		class ReaderEventListener : EventListener
		{
			Reader Reader;

			Dictionary<ulong, Counter> Counters = new Dictionary<ulong, Counter> ();

			public ReaderEventListener (Reader reader)
			{
				Reader = reader;
			}

			public override void HandleSampleCountersDesc (List<Tuple<string, string, ulong, ulong, ulong, ulong>> counters)
			{
				foreach (var t in counters)
					Counters.Add (t.Item6, new Counter (t.Item1, t.Item2, t.Item3, t.Item4, t.Item5, t.Item6, null));
			}

			public override void HandleSampleCounters (ulong timestamp, List<Tuple<ulong, ulong, object>> values)
			{
				var counters = values.ConvertAll<Counter> (t => {
					return Counters [t.Item1] = new Counter (Counters [t.Item1], t.Item3);
				});

				if (Reader.CountersUpdatedSample != null)
					Reader.CountersUpdatedSample (this, new CountersSampleEventArgs () { Timestamp = timestamp, Counters = counters });

				if (Reader.CountersSample != null)
					Reader.CountersSample (this, new CountersSampleEventArgs { Timestamp = timestamp, Counters = new List<Counter> (Counters.Values) });
			}
		}
	}

	public class Counter {
		public readonly string Section;
		public readonly string Name;
		public readonly ulong Type;
		public readonly ulong Unit;
		public readonly ulong Variance;
		public readonly ulong CounterID;

		public readonly object Value;

		public string TypeName {
			get {
				switch (Type) {
				case 0: return "Int";
				case 1: return "UInt";
				case 2: return "Word";
				case 3: return "Long";
				case 4: return "ULong";
				case 5: return "Double";
				case 6: return "String";
				case 7: return "Time Interval";
				default: throw new NotImplementedException ();
				}
			}
		}

		public string UnitName {
			get {
				switch (Unit) {
				case 0 << 24: return "Raw";
				case 1 << 24: return "Bytes";
				case 2 << 24: return "Time";
				case 3 << 24: return "Count";
				case 4 << 24: return "Percentage";
				default: throw new NotImplementedException ();
				}
			}
		}

		public string VarianceName {
			get {
				switch (Variance) {
				case 1 << 28: return "Monotonic";
				case 1 << 29: return "Constant";
				case 1 << 30: return "Variable";
				default: throw new NotImplementedException ();
				}
			}
		}

		public Counter (string section, string name, ulong type, ulong unit, ulong variance, ulong counterID, object value)
		{
			Section = section;
			Name = name;
			Type = type;
			Unit = unit;
			Variance = variance;
			CounterID = counterID;

			Value = value;
		}

		public Counter (Counter o, object value)
		{
			Section = o.Section;
			Name = o.Name;
			Type = o.Type;
			Unit = o.Unit;
			Variance = o.Variance;
			CounterID = o.CounterID;

			Value = value;
		}

		public override string ToString ()
		{
			return String.Format ("{0} - {1} [{2}, {3}, {4}]", Section, Name, TypeName, UnitName, VarianceName);
		}
	}
}

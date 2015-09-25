#if false

using System;
using System.Collections.Generic;
using XamarinProfiler.Core;
using XamarinProfiler.Core.Reader;
using System.Linq;

namespace Benchmarker.Common.LogProfiler
{
	public class Counter {
		public string Section { get; set; }
		public string Name { get; set; }
		public CounterType Type { get; set; }
		public CounterUnit Unit { get; set; }
		public CounterVariance Variance { get; set; }
		public ulong Index { get; set; }

		public Counter ()
		{
		}

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

			return Name.Equals (counter.Name);
		}

		public override int GetHashCode ()
		{
			return Name.GetHashCode ();
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

#endif
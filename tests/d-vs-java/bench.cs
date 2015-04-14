/**
   bench.cs -
   Allocation/Garbage collection benchmark for C#
*/
using System;

public class List<T> {
        private const int _defaultCapacity = 4;

        private T[] _items;
        private int _size;

        static readonly T[]  _emptyArray = new T[0];

        public List() {
            _items = _emptyArray;
        }

        public List(int capacity) {
            if (capacity == 0)
		    _items = _emptyArray;
            else
		    _items = new T[capacity];
        }

        public int Count {
            get {
                return _size;
            }
        }

        public int Capacity {
		get {
			return _items.Length;
		}
		set {
			if (value != _items.Length) {
				if (value > 0) {
					T[] newItems = new T[value];
					if (_size > 0) {
						Array.Copy(_items, 0, newItems, 0, _size);
						Bench.numCopy += _size;
					}
					_items = newItems;
				}
				else {
					_items = _emptyArray;
				}
			}
		}
        }

        private void EnsureCapacity(int min) {
            if (_items.Length < min) {
                int newCapacity = _items.Length == 0? _defaultCapacity : _items.Length * 2;
                // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
                // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
                if (newCapacity < min) newCapacity = min;
                Capacity = newCapacity;
            }
        }

        public void Add(T item) {
		if (_size == _items.Length) EnsureCapacity(_size + 1);
		_items[_size++] = item;
        }

        public void RemoveAt(int index) {
		_size--;
		if (index < _size) {
			Array.Copy(_items, index + 1, _items, index, _size - index);
			Bench.numCopy += _size - index;
		}
		_items[_size] = default(T);
        }

        public void Clear() {
		if (_size > 0)
		{
			Array.Clear(_items, 0, _size); // Don't need to doc this but we clear the elements so that the gc can reclaim the references.
			_size = 0;
		}
        }
}

/**
 *
 * @author tommy
 */
public class Bench {

	public static long numCopy = 0;

	static readonly DateTime Jan1st1970 = new DateTime
		(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	static long CurrentTimeMillis()
	{
		return (long) (DateTime.UtcNow - Jan1st1970).TotalMilliseconds;
	}

	public static List<int> ascendingAlloc(int n, int max_size, bool warmup) {
		List<int> lens = new List<int>();
		// setup timer
		long startTime = CurrentTimeMillis();

		// microbenchmark code
		for (int i = 0; i < n; i++)
		{
			List<List<double>> x = new List<List<double>>();
			int ii = 0;
			while (ii++ < max_size)
			{
				List<double> y = new List<double>(ii);
				x.Add(y);
			}

			lens.Add(x.Count);

			ii = max_size;
			while (ii-- > 1)
			{
				x.RemoveAt(ii - 1);
			}

			x.Clear();
		}

		// stop timer
		long estimatedTime = CurrentTimeMillis() - startTime;

		// output results
		if (warmup == false)
		{
			Console.WriteLine("ascendingAllocCSharp,{0}", estimatedTime);
		}
		return lens;
	}

	public static List<int> descendingAlloc(int n, int max_size, bool warmup) {
		List<int> lens = new List<int>();

		// setup timer
		long startTime = CurrentTimeMillis();

		// microbenchmark code
		for (int i = 0; i < n; i++)
		{
			List<List<double>> x = new List<List<double>>();
			int ii = max_size;
			while (ii-- > 1)
			{
				List<double> y = new List<double>(ii);
				x.Add(y);
			}

			lens.Add(x.Count);

			ii = max_size;
			while (ii-- > 1)
			{
				x.RemoveAt(ii - 1);
			}

			x.Clear();
		}

		// stop timer
		long estimatedTime = CurrentTimeMillis() - startTime;

		// output results
		if (warmup == false)
		{
			Console.WriteLine("descendingAllocCSharp,{0}", estimatedTime);
		}
		return lens;
	}

	public static List<int> alternatingAlloc(int n, int max_size, bool warmup) {
		List<int> lens = new List<int>();
		// setup timer
		long startTime = CurrentTimeMillis();

		// microbenchmark code
		for (int i = 0; i < n; i++)
		{
			List<List<double>> x = new List<List<double>>();
			int ii = 0;
			int l1 = max_size / 2;
			int l2 = 0;
			int limit = l1;
			while (ii++ < limit)
			{
				List<double> y = new List<double>(++l1);
				List<double> y2 = new List<double>(++l2);
				x.Add(y);
				x.Add(y2);
			}

			lens.Add(x.Count);

			ii = limit;
			while (ii-- > 1)
			{
				x.RemoveAt(ii - 1);
			}

			x.Clear();
		}

		// stop timer
		long estimatedTime = CurrentTimeMillis() - startTime;

		// output results
		if (warmup == false)
		{
			Console.WriteLine("alternatingAllocCSharp,{0}", estimatedTime);
		}
		return lens;
	}


	/**
	 * @param args bench_iters times max_size
	 */
	public static void Main(String[] args) {
		int bench_iters = Int32.Parse(args[0]);
		int times = Int32.Parse(args[1]);
		int max_size = Int32.Parse(args[2]);

		// warmup
		List<int>  warmup = ascendingAlloc(times, max_size, true);
		warmup = ascendingAlloc(times, max_size, true);
		warmup = descendingAlloc(times, max_size, true);
		warmup = descendingAlloc(times, max_size, true);
		warmup = alternatingAlloc(times, max_size, true);
		warmup = alternatingAlloc(times, max_size, true);

		// run benchmarks
		while (bench_iters-- > 0)
		{
			List<int> result = ascendingAlloc(times, max_size, false);
			List<int> result2 = descendingAlloc(times, max_size, false);
			List<int> result3 = alternatingAlloc(times, max_size, false);
			List<int> result4 = descendingAlloc(times, max_size, false);
			List<int> result5 = ascendingAlloc(times, max_size, false);
			List<int> result6 = alternatingAlloc(times, max_size, false);
			List<int> result7 = alternatingAlloc(times, max_size, false);
			List<int> result8 = ascendingAlloc(times, max_size, false);
			List<int> result9 = descendingAlloc(times, max_size, false);
			max_size += 1000;
		}
		Console.WriteLine("copies {0}", numCopy);
	}
}

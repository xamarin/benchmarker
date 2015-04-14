/**
   Bench.java -
   Allocation/Garbage collection benchmark for Java
*/
import java.util.ArrayList;
import java.util.Collections;
import java.util.Arrays;

/**
 *
 * @author tommy
 */
public class Bench {
	static final int _defaultCapacity = 4;

	static long numCopy = 0;

	static void arraycopy (Object src, int srcIndex, Object dst, int dstIndex, int len) {
		System.arraycopy (src, srcIndex, dst, dstIndex, len);
		numCopy += len;
	}

	static class ObjectList {
		static final Object[] _emptyArray = new Object[0];

		private Object[] _items;
		private int _size;


		public ObjectList() {
			_items = _emptyArray;
		}

		public ObjectList(int capacity) {
			if (capacity == 0)
				_items = _emptyArray;
			else
				_items = new Object[capacity];
		}

		public int size () {
			return _size;
		}

		public void setCapacity (int value) {
			if (value != _items.length) {
				if (value > 0) {
					Object[] newItems = new Object[value];
					if (_size > 0) {
						arraycopy(_items, 0, newItems, 0, _size);
					}
					_items = newItems;
				}
				else {
					_items = _emptyArray;
				}
			}
		}

		private void EnsureCapacity(int min) {
			if (_items.length < min) {
				int newCapacity = _items.length == 0? _defaultCapacity : _items.length * 2;
				// Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
				// Note that this check works even when _items.Length overflowed thanks to the (uint) cast
				if (newCapacity < min) newCapacity = min;
				setCapacity (newCapacity);
			}
		}

		public void add(Object item) {
			if (_size == _items.length) EnsureCapacity(_size + 1);
			_items[_size++] = item;
		}

		public void remove(int index) {
			_size--;
			if (index < _size) {
				arraycopy(_items, index + 1, _items, index, _size - index);
			}
			_items[_size] = null;
		}

		public void clear() {
			if (_size > 0)
			{
				Arrays.fill(_items, 0); // Don't need to doc this but we clear the elements so that the gc can reclaim the references.
				_size = 0;
			}
		}
	}

	static class IntList {
		static final int[] _emptyArray = new int[0];

		private int[] _items;
		private int _size;


		public IntList() {
			_items = _emptyArray;
		}

		public IntList(int capacity) {
			if (capacity == 0)
				_items = _emptyArray;
			else
				_items = new int[capacity];
		}

		public int size () {
			return _size;
		}

		public void setCapacity (int value) {
			if (value != _items.length) {
				if (value > 0) {
					int[] newItems = new int[value];
					if (_size > 0) {
						arraycopy(_items, 0, newItems, 0, _size);
					}
					_items = newItems;
				}
				else {
					_items = _emptyArray;
				}
			}
		}

		private void EnsureCapacity(int min) {
			if (_items.length < min) {
				int newCapacity = _items.length == 0? _defaultCapacity : _items.length * 2;
				// Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
				// Note that this check works even when _items.Length overflowed thanks to the (uint) cast
				if (newCapacity < min) newCapacity = min;
				setCapacity (newCapacity);
			}
		}

		public void add(int item) {
			if (_size == _items.length) EnsureCapacity(_size + 1);
			_items[_size++] = item;
		}

		public void remove(int index) {
			_size--;
			if (index < _size) {
				arraycopy(_items, index + 1, _items, index, _size - index);
			}
			_items[_size] = 0;
		}

		public void clear() {
			if (_size > 0)
			{
				Arrays.fill(_items, 0); // Don't need to doc this but we clear the elements so that the gc can reclaim the references.
				_size = 0;
			}
		}
	}

	static class DoubleList {
		static final Double[] _emptyArray = new Double[0];

		private Double[] _items;
		private int _size;


		public DoubleList() {
			_items = _emptyArray;
		}

		public DoubleList(int capacity) {
			if (capacity == 0) {
				_items = _emptyArray;
			} else {
				_items = new Double[capacity];
				/* This makes it slow */
				for (int i = 0; i < capacity; ++i)
					_items [i] = 0.0;
			}
		}

		public int size () {
			return _size;
		}

		public void setCapacity (int value) {
			if (value != _items.length) {
				if (value > 0) {
					Double[] newItems = new Double[value];
					if (_size > 0) {
						arraycopy(_items, 0, newItems, 0, _size);
					}
					_items = newItems;
				}
				else {
					_items = _emptyArray;
				}
			}
		}

		private void EnsureCapacity(int min) {
			if (_items.length < min) {
				int newCapacity = _items.length == 0? _defaultCapacity : _items.length * 2;
				// Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
				// Note that this check works even when _items.Length overflowed thanks to the (uint) cast
				if (newCapacity < min) newCapacity = min;
				setCapacity (newCapacity);
			}
		}

		public void add(Double item) {
			if (_size == _items.length) EnsureCapacity(_size + 1);
			_items[_size++] = item;
		}

		public void remove(int index) {
			_size--;
			if (index < _size) {
				arraycopy(_items, index + 1, _items, index, _size - index);
			}
			_items[_size] = null;
		}

		public void clear() {
			if (_size > 0)
			{
				Arrays.fill(_items, 0); // Don't need to doc this but we clear the elements so that the gc can reclaim the references.
				_size = 0;
			}
		}
	}


    public static IntList ascendingAlloc(int n, int max_size, boolean warmup) {
        IntList lens = new IntList();
        // setup timer
        long startTime = System.currentTimeMillis();

        // microbenchmark code
        for (int i = 0; i < n; i++)
        {
            ObjectList x = new ObjectList();
            int ii = 0;
            while (ii++ < max_size)
            {
                DoubleList y = new DoubleList(ii);
                x.add(y);
            }

            lens.add(x.size());

            ii = max_size;
            while (ii-- > 1)
            {
                x.remove(ii - 1);
            }

            x.clear();
        }

        // stop timer
        long estimatedTime = System.currentTimeMillis() - startTime;

        // output results
        if (warmup == false)
        {
            System.out.print("ascendingAllocJava,");
            System.out.print(estimatedTime);
            System.out.print('\n');
        }
        return lens;
    }

    public static IntList descendingAlloc(int n, int max_size, boolean warmup) {
        IntList lens = new IntList();

        // setup timer
        long startTime = System.currentTimeMillis();

        // microbenchmark code
        for (int i = 0; i < n; i++)
        {
            ObjectList x = new ObjectList();
            int ii = max_size;
            while (ii-- > 1)
            {
                DoubleList y = new DoubleList(ii);
                x.add(y);
            }

            lens.add(x.size());

            ii = max_size;
            while (ii-- > 1)
            {
                x.remove(ii - 1);
            }

            x.clear();
        }

        // stop timer
        long estimatedTime = System.currentTimeMillis() - startTime;

        // output results
        if (warmup == false)
        {
            System.out.print("descendingAllocJava,");
            System.out.print(estimatedTime);
            System.out.print('\n');
        }
        return lens;
    }

    public static IntList alternatingAlloc(int n, int max_size, boolean warmup) {
        IntList lens = new IntList();
        // setup timer
        long startTime = System.currentTimeMillis();

        // microbenchmark code
        for (int i = 0; i < n; i++)
        {
            ObjectList x = new ObjectList();
            int ii = 0;
            int l1 = max_size / 2;
            int l2 = 0;
            int limit = l1;
            while (ii++ < limit)
            {
                DoubleList y = new DoubleList(++l1);
                DoubleList y2 = new DoubleList(++l2);
                x.add(y);
                x.add(y2);
            }

            lens.add(x.size());

            ii = limit;
            while (ii-- > 1)
            {
                x.remove(ii - 1);
            }

            x.clear();
        }

        // stop timer
        long estimatedTime = System.currentTimeMillis() - startTime;

        // output results
        if (warmup == false)
        {
            System.out.print("alternatingAllocJava,");
            System.out.print(estimatedTime);
            System.out.print('\n');
        }
        return lens;
    }


    /**
     * @param args bench_iters times max_size
     */
    public static void main(String[] args) {
        int bench_iters = Integer.parseInt(args[0]);
        int times = Integer.parseInt(args[1]);
        int max_size = Integer.parseInt(args[2]);

        // warmup
        IntList  warmup = ascendingAlloc(times, max_size, true);
        warmup = ascendingAlloc(times, max_size, true);
        warmup = descendingAlloc(times, max_size, true);
        warmup = descendingAlloc(times, max_size, true);
        warmup = alternatingAlloc(times, max_size, true);
        warmup = alternatingAlloc(times, max_size, true);

        // run benchmarks
        while (bench_iters-- > 0)
        {
            IntList result = ascendingAlloc(times, max_size, false);
            IntList result2 = descendingAlloc(times, max_size, false);
            IntList result3 = alternatingAlloc(times, max_size, false);
            IntList result4 = descendingAlloc(times, max_size, false);
            IntList result5 = ascendingAlloc(times, max_size, false);
            IntList result6 = alternatingAlloc(times, max_size, false);
            IntList result7 = alternatingAlloc(times, max_size, false);
            IntList result8 = ascendingAlloc(times, max_size, false);
            IntList result9 = descendingAlloc(times, max_size, false);
            max_size += 1000;
        }

	System.out.print("copies ");
	System.out.print(numCopy);
	System.out.print("\n");
    }
}

using System;

public class MyTypes {
	public class A {
		public virtual long foo () {
			return 1;
		}
	}

	public class B : A {
		public override long foo () {
			return 2;
		}
	}

	public class C : A {
		public override long foo () {
			return 3;
		}
	}

	static long bench (long iterations, object[] objs) {
		long checksum = 0;
		for (long i = 0; i < iterations; ++i) {
			foreach (object o in objs) {
				if (o is A) {
					checksum += ((A) o).foo ();
				}
				if (o is B) {
					checksum += ((B) o).foo ();
				}
				if (o is C) {
					checksum += ((C) o).foo ();
				}
			}
		}
		return checksum;
	}

	public static void Main (string []args) {
		long n =  25000000L;
		object[] objs = new object[4];
		objs [0] = new object ();
		objs [1] = new A ();
		objs [2] = new B ();
		objs [3] = new C ();
		if (args.Length > 0)
			n = Int64.Parse (args[0]);
		long checksum = bench (n, objs);
		Console.WriteLine ("iterations: " + n);
		Console.WriteLine ("checksum:   " + checksum);
		System.Environment.Exit (checksum == (11 * n) ? 0 : 1);
	}
}

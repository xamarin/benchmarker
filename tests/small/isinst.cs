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

	static bool bench (long iterations, object[] objs) {
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
		return checksum == (4 * iterations);
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
		System.Environment.Exit (bench (n, objs) ? 0 : 1);
	}
}

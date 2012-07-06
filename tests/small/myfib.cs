using System;

public class main {
    public static int fib (int n) {
	if (n < 2)
	    return n;
	return fib (n - 1) + fib (n - 2);
    }

    public static int Main () {
	fib (42);
	//Console.WriteLine ("result: " + fib (42));
	return 0;
    }
}

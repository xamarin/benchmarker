using System;

public class MyList
{
	MyList next;

	static MyList MakeList (int length)
	{
		MyList rest = null;
		for (int i = 0; i < length; ++i)
		{
			MyList first = new MyList ();
			first.next = rest;
			rest = first;
		}
		return rest;
	}

	public static void Main ()
	{
		MakeList (50000000);
	}
}

using System;
using System.Collections.Concurrent;
using System.Diagnostics;

public class ConcurrentDictionaryTest
{
	public static void Main(string[] args)
	{
		Profile();
	}

	private static void Profile()
	{
		Console.WriteLine("64bit: " + Environment.Is64BitProcess);
		var dict = new ConcurrentDictionary<string,string>();
		for (int i = 0; i < 0x5fffff; i++)
		{
			dict.GetOrAdd("1", (input) => "2");
		}
	}
}


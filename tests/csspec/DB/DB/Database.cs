/*
 * @(#)Database.java	1.18 06/17/98
 *
 * Database.java   Version 1.0 10/16/97 rrhi, kaivalya, salina
 * Randy Heisch       IBM Corp. - Austin, TX
 *
 * Tested Kaivalya  heap highwatermark = 13795320 Mar 24 15:26:52 CST 1998
 * Reduced the need for heap size to accomodate i64bit Arch.  rrh 3/24/98
 * Data files size reduced and workload increased. rrh 3/24/98
 *                                03/11/98 rrh - null (free) objects
 *         Check for CR also - rrh 2/18/98 make Unix & Windows happy - Randy
 *         Workload also is bigger to increase the run-time 02/18/98 Randy
 *
 * Copyright (c) 1998 Standard Performance Evaluation Corporation (SPEC)
 *               All rights reserved.
 * Copyright (c) 1996,1997,1998 IBM Corporation, Inc. All rights reserved.
 *
 * This source code is provided as is, without any express or implied warranty.
 *
 * Permission to use, copy, modify, and distribute this software
 * and its documentation for NON-COMMERCIAL purposes and without
 * fee is hereby granted provided that this copyright notice
 * appears in all copies.
 *
 * IBM MAKES NO REPRESENTATIONS OR WARRANTIES ABOUT THE SUITABILITY OF
 * THE SOFTWARE, EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
 * TO THE IMPLIED WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
 * PARTICULAR PURPOSE, OR NON-INFRINGEMENT. IBM SHALL NOT BE LIABLE FOR
 * ANY DAMAGES SUFFERED BY LICENSEE AS A RESULT OF USING, MODIFYING OR
 * DISTRIBUTING THIS SOFTWARE OR ITS DERIVATIVES.
 */

using System;
using System.Text;
using System.Collections.Generic;


public class Database
{
	private List<Entry> entries;
	private List<String> fmt;
	private Entry[] index;
	private int current_record;
	private String dbname = null;
	private int fnum = -1;

	public static bool printRecords = true;

	private void read_fmt(String filename)
	{

		String[] alltxt = System.IO.File.ReadAllLines(filename);
		for(int i = 0; i < alltxt.Length; ++i)
		{
			String vs = alltxt[i].Trim();
			if(String.IsNullOrEmpty(vs))
				continue;

			int prxi = vs.IndexOf('%');
			if(prxi == -1)
				fmt.Add(vs.Substring(1, vs.Length - 2));
			else
			{
				String pls = vs.Substring(0, prxi - 1).Trim();
				pls = pls.Substring(1, pls.Length - 2);
				fmt.Add(pls);
				fmt.Add(null);
			}
		}
	}

	public Database(String s)
	{
		entries = new List<Entry>();
		fmt = new List<String>();
		dbname = s;
		read_fmt(s + ".fmt");
		read_db(s + ".dat");
		index = null;
	}

	public int numRecords()
	{
		return entries.Count;
	}

	public void read_db(String filename)
	{
		Entry entry;
		int n = 0, e, s;

		Console.WriteLine("Reading database " + dbname + " ... ");

		String alltxt = null;
		alltxt = System.IO.File.ReadAllText(filename);

		entry = new Entry();

		Console.WriteLine("OK\nBuilding database ...");

		char[] buffer = alltxt.ToCharArray();
		n = buffer.Length;
		s = e = 0;
		while((e < n) && (s < n))
		{
			// Check for CR also - rrh 2/18/98
			while((e < n) && (buffer[e] != '\n') && (buffer[e] != '\r'))
			{
				e++;
			}

			if(e < n)
			{
				if(buffer[s] == '#')
				{
					add(entry);
					entry = new Entry();
				}
				else
				{
					entry.items.Add(new String(buffer, s, e - s));
				}

				// Discard CR & LF - rrh 2/18/98
				while((e < n) && ((buffer[e] == '\n') || (buffer[e] == '\r')))
				{
					e++;
				}

				s = e;
			}
		}

		buffer = null;     // 03/11/98 rrh

		Console.WriteLine("Done.");
	}


	public void write_db()
	{
		Entry entry;
		Console.WriteLine("Saving database " + dbname + " ... ");

		StringBuilder sb = new StringBuilder();
		for(int i = 0; i < this.entries.Count; ++i)
		{
			entry = this.entries[i];

			for(int j = 0; j < entry.items.Count; ++j)
				sb.Append(entry.items[j] + "\n");

			sb.Append("#\n");
		}

		System.IO.File.WriteAllText(dbname + ".dat", sb.ToString());
		Console.WriteLine("Done.");
	}


	private void set_index()
	{
		int i, n;

		n = entries.Count;

		index = null;
		index = new Entry[n];

		for(i = 0; i < n; ++i)
			index[i] = this.entries[i];
	}

	public void end()
	{
		if(index == null)
		{
			set_index();
		}
		current_record = index.Length - 1;
		printRec();
	}

	public void list()
	{
		current_record = 0;

		if(index == null)
		{
			set_index();
		}

		printRec();
	}


	public void gotoRec(int rec)
	{
		rec--;

		if(index == null)
		{
			set_index();
		}

		if((rec < index.Length) && (rec >= 0))
		{
			current_record = rec;

			printRec();
		}
		else
		{
			Console.WriteLine("Invalid record number (" + (rec + 1) + ")");
		}
	}


	public void next()
	{
		if(index == null)
		{
			set_index();
		}

		if(current_record < (index.Length - 1))
		{
			current_record = current_record + 1;

			printRec();
		}
	}


	public void previous()
	{
		if(index == null)
		{
			set_index();
		}

		if(current_record > 0)
		{
			current_record = current_record - 1;

			printRec();
		}
	}

	public int currentRec()
	{
		return current_record;
	}

	public void printRec()
	{
		String s;
		Entry entry;

		if(index == null)
		{
			set_index();
		}

		if((current_record >= index.Length) || (current_record < 0))
		{
			return;
		}

		Console.WriteLine("---- Record number " + (current_record + 1) + " ----");

		entry = index[current_record];

		int entrypos = 0;
		for(int fi = 0; fi < fmt.Count; ++fi)
		{
			s = fmt[fi];

			if(s != null)
			{
				Console.Write(s);
				if(fi < fmt.Count && fmt[fi + 1] != null)
					Console.WriteLine();
			}
			else
			{
				s = (String)entry.items[entrypos];
				Console.WriteLine(s);
				++entrypos;
			}
		}

		Console.WriteLine();
	}


	public void add(Entry entry)
	{
		entries.Add(entry);

		index = null;
		fnum = -1;
	}


	public Entry getEntry()
	{
		String s = null;
		String field;

		Entry entry = new Entry();

		for(int i = 0; i < fmt.Count; ++i)
		{
			field = fmt[i];

			if(field != null)
			{
				// These create too much output for benchmark - rrh
				//spec.harness.Context.out.print(field);
				//spec.harness.Context.out.flush();
			}
			else
			{
				s = Console.ReadLine();
				entry.items.Add(s);
			}
		}

		return entry;
	}


	public void modify()
	{
		String s = null;
		String field, os;
		int fn = 0;

		if(index == null)
		{
			return;
		}

		for(int i = 0; i < fmt.Count; ++i)
		{
			field = fmt[i];

			if(field != null)
			{ ;}
			else
			{
				os = index[current_record].items[fn];
				s = Console.ReadLine();

				if(s.Length > 0)
				{
					os = s;
				}

				index[current_record].items[fn] = os;

				fn++;
			}
		}

		fnum = -1;
	}


	public void status()
	{
		if(index == null)
		{
			set_index();
		}
		Console.WriteLine("Record " + (current_record + 1) + " of " + index.Length);
	}

	private String fieldValue;

	private int getfield()
	{
		String fs;
		int fn;


		if(index == null)
		{
			set_index();
		}

		Entry entry = new Entry();

		fn = 0;
		for(int i = 0; i < fmt.Count; ++i)
		{
			fs = fmt[i];

			if(fs != null)
			{ ;}
			else
			{
				fieldValue = Console.ReadLine();

				if(fieldValue.Length > 0)
				{
					break;
				}
				else
				{
					fn++;
				}
			}
		}

		if(fn >= index[0].items.Count)
		{
			return -1;
		}
		else
		{
			return fn;
		}
	}


	public void sort()
	{
		int fn;

		fn = getfield();

		if(fn < 0)
		{
			return;
		}

		if(fn != fnum)
		{
			shell_sort(fn);
		}
	}


	public void find()
	{
		int fn, rec;

		fn = getfield();

		if(fn != fnum)
		{
			shell_sort(fn);
		}

		if((rec = lookup(fieldValue, fnum)) < 0)
		{
			Console.WriteLine("NOT found");
		}
		else
		{
			Console.WriteLine();

			while(rec >= 0)
			{
				rec--;

				if(fieldValue.CompareTo(index[rec].items[fnum]) != 0)
				{
					break;
				}
			}

			current_record = rec + 1;
			printRec();
		}
	}


	// Binary search the alpha sorted index list
	public int lookup(String s, int fn)
	{
		int rc, i = 0, first, last;
		bool found;

		first = 0;
		last = index.Length - 1;
		found = false;

		while((first <= last) && !found)
		{
			i = (first + last) >> 1;

			rc = s.CompareTo(index[i].items[fn]);

			if(rc == 0)
			{
				found = true;
			}
			else if(rc < 0)
			{
				last = i - 1;
			}
			else
			{
				first = i + 1;
			}
		}

		if(found)
		{
			return i;
		}
		else
		{
			return -1;
		}
	}


	void shell_sort(int fn)
	{
		int i, j, gap;
		int n;
		String s1, s2;
		Entry e;

		if(index == null)
		{
			set_index();
		}

		n = index.Length;

		for(gap = n / 2; gap > 0; gap /= 2)
		{
			for(i = gap; i < n; i++)
			{
				for(j = i - gap; j >= 0; j -= gap)
				{
					s1 = index[j].items[fn];
					s2 = index[j + gap].items[fn];

					if(s1.CompareTo(s2) <= 0)
					{
						break;
					}

					e = index[j];
					index[j] = index[j + gap];
					index[j + gap] = e;
				}
			}
		}
		
		fnum = fn;
	}


	public void remove()
	{

		if(index == null)
		{
			set_index();
		}

		entries.Remove(index[current_record]);

		if(current_record == (index.Length - 1))
		{
			current_record = current_record - 1;
		}

		index = null;
		fnum = -1;
	}
}


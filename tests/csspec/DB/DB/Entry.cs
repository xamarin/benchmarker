/*
 * @(#)Entry.java	1.4 06/17/98
 *
 * Entry.java   Version 1.0 03/03/97 rrh
 * Randy Heisch       IBM Corp. - Austin, TX
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

public class Entry
{
	public List<String> items;

	public Entry()
	{
		items = new List<String>();
	}

	/*
	public override bool Equals(Object o)
	{
		Entry entry;

		if(!(o is Entry))
		{
			return false;
		}

		entry = (Entry)o;

		if(entry.items.Count != items.Count)
		{
			return false;
		}

		for(int i = 0; i < this.items.Count; ++i)
		{
			if(!this.items[i].Equals(entry.items[i]))
				return false;
		}

		return true;
	}

	public override int GetHashCode()
	{
		int hc = 0;

		for(int i = 0; i < this.items.Count; ++i)
			hc += this.items[i].GetHashCode();

		return hc;
	}
	*/ 
}



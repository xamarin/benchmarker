/*
 * @(#)Main.java	1.17 06/17/98
 *
 * Main.java   Version 2.0 03/24/98 rrh, kaivalya, salina
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


public class MainCL
{
	private bool standalone = true;

	static void help()
	{
		Console.WriteLine("a - add record");
		Console.WriteLine("b - show beginning record");
		Console.WriteLine("d - delete record");
		Console.WriteLine("e - show end record");
		Console.WriteLine("f - find record");
		Console.WriteLine("m - modify record");
		Console.WriteLine("n - next entry");
		Console.WriteLine("p - previous record");
		Console.WriteLine("q - quit");
		Console.WriteLine("w - write database");
		Console.WriteLine("s - sort");
		Console.WriteLine(". - current record number");
		Console.WriteLine("x - Total records");
		Console.WriteLine("num - goto record number 'num'");
	}

	public static void Main(String[] args)
	{
		String[] argsn = new String[2];
		//args[0] = "../src/otherSmallBench/_209_db/input/db6";
		//args[1] = "../src/otherSmallBench/_209_db/input/scr6";

		argsn[0] = @"..\input\db6";
		argsn[1] = @"..\input\scr6";

		//Database.printRecords = false; ?? do we want this ??

		new MainCL().run(args);
	}

	public void run(String[] arg)
	{
		String s;
		Database db;

		if(standalone)
			System.Console.SetIn(new System.IO.StreamReader(arg[1]));

		bool OK = true;
		bool changed = false;
		int rec;
		char cmd, last = ' ';


		db = new Database(arg[0]);


		while(OK)
		{
			s = System.Console.ReadLine();

			if(s.Length == 0)
			{
				cmd = last;
			}
			else
			{
				cmd = s[0];

				if((cmd <= '9') && (cmd >= '0'))
				{
					rec = Int32.Parse(s);
					db.gotoRec(rec);
				}
			}

			if(cmd == 'a')
			{
				db.add(db.getEntry());
				changed = true;
			}
			else if(cmd == 'h')
			{
				help();
			}
			else if(cmd == 'd')
			{
				s = System.Console.ReadLine();
				if(s.Length > 0)
				{
					if(s[0] == 'y')
					{
						db.remove();
						changed = true;
					}
				}

				cmd = ' ';
			}
			else if(cmd == 'b')
			{
				db.list();
			}
			else if(cmd == 'x')
			{
				db.status();
			}
			else if(cmd == 'e')
			{
				db.end();
			}
			else if(cmd == 'm')
			{
				db.modify();
				changed = true;
				cmd = ' ';
			}
			else if(cmd == 'n')
			{
				db.next();
			}
			else if(cmd == 'p')
			{
				db.previous();
			}
			else if(cmd == 's')
			{
				db.sort();
			}
			else if(cmd == 'f')
			{
				db.find();
			}
			else if(cmd == 'w')
			{
				db.write_db();
				changed = false;
				cmd = ' ';
			}
			else if(cmd == '.')
			{
				db.printRec();
			}
			else if(cmd == 'q')
			{
				OK = false;
			}
			else
			{ ;}

			last = cmd;
		}


		if(changed)
		{
			Console.WriteLine("Save database (y or n)? ");
			s = Console.ReadLine();

			if((s[0] != 'n') && standalone)
			{
				db.write_db();
			}
		}
	}
}

/*****************************************************************
Reading database input/db4 ... OK
Building database ...Done.
a - add record
b - show beginning record
d - delete record
e - show end record
f - find record
m - modify record
n - next entry
p - previous record
q - quit
w - write database
s - sort
. - current record number
x - Total records
num - goto record number 'num'
Record 1 of 50
---- Record number 1 ----
Full name
   Last: Richards
  First: Gena
     MI: E
Address
    Addr: P.O. Box 6055
    City: Hill City
   State: Wisconsin
     ZIP: 20959
 Phone #: (217) 541-3354

---- Record number 2 ----
Full name
   Last: Pearce
  First: Bobby
     MI: U
Address
    Addr: 8379 North Bull Run Crossing
    City: Galveston
   State: North Carolina
     ZIP: 31564
 Phone #: (193) 302-8320

---- Record number 3 ----
Full name
   Last: Wilkins
  First: Bonnie
     MI: O
Address
    Addr: P.O. Box 1420
    City: Marion
   State: North Dakota
     ZIP: 14912
 Phone #: (621) 569-9499

---- Record number 50 ----
Full name
   Last: Lopez
  First: Sara
     MI: S
Address
    Addr: P.O. Box 8255
    City: Bandera
   State: Vermont
     ZIP: 15229
 Phone #: (216) 860-0459

---- Record number 49 ----
Full name
   Last: Stevensen
  First: Terrence
     MI: M
Address
    Addr: 1060 East Wagon Wheel Street
    City: Bellaire
   State: North Carolina
     ZIP: 60054-9647
 Phone #: (437) 342-6493

---- Record number 48 ----
Full name
   Last: Kennedy
  First: Melanie
     MI: P
Address
    Addr: 6579 North Luna
    City: Lawrenceburg
   State: Louisiana
     ZIP: 53974
 Phone #: (442) 126-1627

---- Record number 10 ----
Full name
   Last: Carpenter
  First: Zane
     MI: P
Address
    Addr: P.O. Box 596
    City: Hartford
   State: Minnesota
     ZIP: 21544
 Phone #: (253) 143-4102

---- Record number 1 ----
Full name
   Last: Adams
  First: Melanie
     MI: A
Address
    Addr: P.O. Box 9404
    City: Baldwin
   State: South Dakota
     ZIP: 32234
 Phone #: (785) 442-4853

---- Record number 2 ----
Full name
   Last: Alvarado
  First: Marry
     MI: B
Address
    Addr: 514 South Bowser Av
    City: Hartford
   State: North Dakota
     ZIP: 29693
 Phone #: (116) 196-6806

---- Record number 3 ----
Full name
   Last: Bailey
  First: Pat
     MI: E
Address
    Addr: 2354 North Greenwood
    City: Benton
   State: Massachusetts
     ZIP: 09605
 Phone #: (937) 167-7049
*****************************************************************/

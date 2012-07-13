/*
 * %W% %G%
 *
 * Copyright (c) 1998 Standard Performance Evaluation Corporation (SPEC)
 *               All rights reserved.
 * Copyright (c) 1996,1997,1998 Sun Microsystems, Inc. All rights reserved.
 *
 * This source code is provided as is, without any express or implied warranty.
 */

using System;

public class MainCL
{
	public static void Main(String[] args)
	{

		if(args.Length == 0)
		{
			args = new String[3];
			args[0] = "20"; // + (200*spec.harness.Context.getSpeed())/100;
			args[1] = "200";
			args[2] = @"..\time-test.model";
		}

		(new RayTracer()).inst_main(args);
	}
}



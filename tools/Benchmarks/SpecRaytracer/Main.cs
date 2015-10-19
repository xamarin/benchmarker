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
using Common.Logging;

namespace Benchmarks.SpecRaytracer
{
	public class MainCL
	{
		public static ILog logger;
		public static void Main (String[] args, ILog ilog)
		{
			logger = ilog;
			if (args.Length == 0) {
				args = new String[3];
				args [0] = "20"; // + (200*spec.harness.Context.getSpeed())/100;
				args [1] = "200";
			}

			(new RayTracer ()).inst_main (args, ilog);
		}
	}
}

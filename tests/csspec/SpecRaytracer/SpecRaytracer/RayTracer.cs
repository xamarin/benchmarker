/*
 * @(#)RayTracer.java	1.14 06/17/98
 *
 * RayTracer.java
 * The RayTracer class is the starting class for the program. It creates the
 * Scene, renders it and draws it to the screen.
 *
 * Lines that are commented out can be uncommented to draw the resulting 
 * scene on the screen.
 *
 * Modified by Don McCauley - IBM 02/18/98 (DWM)
 *
 * Copyright (c) 1998 Standard Performance Evaluation Corporation (SPEC)
 *               All rights reserved.
 * Copyright (c) 1996,1997,1998 Sun Microsystems, Inc. All rights reserved.
 *
 * This source code is provided as is, without any express or implied warranty.
 */

using System;

/**
 * class RayTracer
 */
public class RayTracer
{
	int width = 20;
	int height = 30;
	String name = @"..\time-test.model";
	Canvas canvas;

	public void inst_main(String[] argv)
	{
		if(argv.Length >= 3)
		{
			width = Int32.Parse(argv[0]);
			height = Int32.Parse(argv[1]);
			name = argv[2];
		}

		canvas = new Canvas(width, height);
		new Scene(name).RenderScene(canvas, width, 0, 1);

		using (var writer = new System.IO.StreamWriter (System.IO.Stream.Null))
			canvas.WriteDiag(writer);
	}
}


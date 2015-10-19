/*
 * @(#)Light.java	1.4 06/17/98
 *
 * Light.java
 * Holds a light color and position.
 *
 * Copyright (c) 1998 Standard Performance Evaluation Corporation (SPEC)
 *               All rights reserved.
 * Copyright (c) 1996,1997,1998 Sun Microsystems, Inc. All rights reserved.
 *
 * This source code is provided as is, without any express or implied warranty.
 */

namespace Benchmarks.SpecRaytracer
{
	/**
 * class Light
 */
	public class Light
	{
		private Color lightColor;
		private Point lightPosition;

		/**
	 * Light
	 *
	 * @param newpos
	 * @param newcolor
	 */
		public Light (Point newpos, Color newcolor)
		{
			lightColor = newcolor;
			lightPosition = newpos;
		}

		/**
	 * GetPosition
	 *
	 * @return Point
	 */
		public Point GetPosition ()
		{
			return (lightPosition);
		}

		/**
	 * GetColor
	 *
	 * @return Color
	 */
		public Color GetColor ()
		{
			return (lightColor);
		}
	}
}

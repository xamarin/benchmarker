/*
 * @(#)LightNode.java	1.4 06/17/98
 *
 * LightNode.java
 * A linked list node of lights.
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
 * class LightNode
 */
	public class LightNode : LinkNode
	{
		private Light theLight;

		/**
	 * LightNode
	 */
		public LightNode ()
			: base (null)
		{
			theLight = null;
		}

		/**
	 * LightNode
	 *
	 * @param newLight
	 * @param nextlink
	 */
		public LightNode (Light newLight, LinkNode nextlink)
			: base (nextlink)
		{
			theLight = newLight;
		}

		/**
	 * Next
	 *
	 * @return LightNode
	 */
		public LightNode Next ()
		{
			return ((LightNode)GetNext ());
		}

		/**
	 * GetLight
	 *
	 * @return Light
	 */
		public Light GetLight ()
		{
			return (theLight);
		}
	}
}

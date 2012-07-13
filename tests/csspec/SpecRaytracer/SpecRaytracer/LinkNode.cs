/*
 * @(#)LinkNode.java	1.4 06/17/98
 *
 * LinkNode.java
 * The base class for all singly linked list nodes.
 *
 * Copyright (c) 1998 Standard Performance Evaluation Corporation (SPEC)
 *               All rights reserved.
 * Copyright (c) 1996,1997,1998 Sun Microsystems, Inc. All rights reserved.
 *
 * This source code is provided as is, without any express or implied warranty.
 */

/**
 * class LinkNode
 */
public class LinkNode
{
	private LinkNode NextLink;

	/**
	 * LinkNode
	 */
	public LinkNode()
	{
		NextLink = null;
	}

	/**
	 * LinkNode
	 *
	 * @param nextlink
	 */
	public LinkNode(LinkNode nextlink)
	{
		NextLink = nextlink;
	}

	/**
	 * Next
	 *
	 * @return LinkNode
	 */
	public LinkNode GetNext()
	{
		return (NextLink);
	}
}

/*
 * @(#)Ray.java	1.4 06/17/98
 *
 * Ray.java
 * Holds the ray origin and direction.
 *
 * Copyright (c) 1998 Standard Performance Evaluation Corporation (SPEC)
 *               All rights reserved.
 * Copyright (c) 1996,1997,1998 Sun Microsystems, Inc. All rights reserved.
 *
 * This source code is provided as is, without any express or implied warranty.
 */


/**
 * class Ray
 */
public class Ray
{
	Point Origin;
	Vector Direction;
	int ID;

	/**
	 * Ray
	 */
	public Ray()
	{
		ID = 0;
		Direction = new Vector();
	}

	/**
	 * SetOrigin
	 *
	 * @param neworigin
	 */
	public void SetOrigin(Point neworigin)
	{
		Origin = neworigin;
	}

	/**
	 * SetID
	 *
	 * @param newID
	 */
	public void SetID(int newID)
	{
		ID = newID;
	}

	/**
	 * GetOrigin
	 *
	 * @return Point
	 */
	public Point GetOrigin()
	{
		return (Origin);
	}

	/**
	 * GetDirection
	 *
	 * @return Vector
	 */
	public Vector GetDirection()
	{
		return (Direction);
	}

	/**
	 * GetID
	 *
	 * @return int
	 */
	public int GetID()
	{
		return (ID);
	}
}


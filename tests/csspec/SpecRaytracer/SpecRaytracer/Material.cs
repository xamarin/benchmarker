/*
 * @(#)Material.java	1.4 06/17/98
 *
 * Material.java
 * Holds the material characteristics of an object, including it's various
 * color components and shininess and transparent coefficients.
 *
 * Copyright (c) 1998 Standard Performance Evaluation Corporation (SPEC)
 *               All rights reserved.
 * Copyright (c) 1996,1997,1998 Sun Microsystems, Inc. All rights reserved.
 *
 * This source code is provided as is, without any express or implied warranty.
 */

/**
 * class Material
 */
public class Material
{
	private Color diffColor;
	private Color ambColor;
	private Color specColor;
	private Color emissColor;
	private double shininess;
	private double ktran;
	private double refractiveIndex;

	/**
	 * Material
	 *
	 * @param diff
	 * @param amb
	 * @param spec
	 * @param emiss
	 * @param newshininess
	 * @param newktran
	 */
	public Material(Color diff, Color amb, Color spec, Color emiss, double newshininess, double newktran)
	{
		diffColor = diff;
		ambColor = amb;
		specColor = spec;
		emissColor = emiss;
		shininess = newshininess;
		ktran = newktran;

		if(ktran != 0.0f)
		{
			refractiveIndex = 1.5f;
		}
		else
		{
			refractiveIndex = 0.0f;
		}
	}

	/**
	 * GetDiffColor
	 *
	 * @return Color
	 */
	public Color GetDiffColor()
	{
		return (diffColor);
	}

	/**
	 * GetAmbColor
	 *
	 * @return Color
	 */
	public Color GetAmbColor()
	{
		return (ambColor);
	}

	/**
	 * GetSpecColor
	 *
	 * @return Color
	 */
	public Color GetSpecColor()
	{
		return (specColor);
	}

	/**
	 * GetEmissColor
	 *
	 * @return Color
	 */
	public Color GetEmissColor()
	{
		return (emissColor);
	}

	/**
	 * GetKTran
	 *
	 * @return double
	 */
	public double GetKTran()
	{
		return (ktran);
	}

	/**
	 * GetShininess
	 *
	 * @return double
	 */
	public double GetShininess()
	{
		return (shininess);
	}

	/**
	 * GetRefIndex
	 *
	 * @return double
	 */
	public double GetRefIndex()
	{
		return (refractiveIndex);
	}
}

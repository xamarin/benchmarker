using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Euler
{
	class Euler : Tunnel
	{
		static void Main(string[] args)
		{
			Euler tt = new Euler(1);
			tt.initialise();
			tt.runiters();
			tt.validate();
		}

		public void validate()
		{
			double[] refval = { 0.0033831416599344965, 0.006812543658280322 };
			double dev = Math.Abs(error - refval[size]);
			if(dev > 1.0e-12)
			{
				Console.WriteLine("Validation failed");
				Console.WriteLine("Computed RMS pressure error = " + error);
				Console.WriteLine("Reference value = " + refval[size]);
			}
		}

		private Euler(int sz)
			: base()
		{
			this.size = sz;
		}
	}

	public class Tunnel
	{
		public int size;
		public int[] datasizes = { 8, 12 };

		public double machff = 0.7;    /* Inflow mach number */
		public double secondOrderDamping = 1.0;
		public double fourthOrderDamping = 1.0;
		public int ntime = 1; /* 0 = local timestep, 1 = time accurate */
		public int scale; /* Refine input grid by this factor */
		public double error;

		double[,] a; /* Grid cell area */
		double[,] deltat; /* Timestep */
		double[,] opg, pg, pg1; /* Pressure */
		double[,] sxi, seta;
		double[,] tg, tg1; /* Temperature */
		double[,] xnode, ynode; /* Storage of node coordinates */

		double[, ,] oldval, newval; /* Tepmoray arrays for interpolation */

		double cff, uff, vff, pff, rhoff, tff, jplusff, jminusff;
		/* Far field values */
		int iter = 100; /* Number of iterations */
		int imax, jmax;     /* Number of nodes in x and y direction*/
		int imaxin, jmaxin; /* Number of nodes in x and y direction in unscaled data */
		int nf = 6; /* Number of fields in data file */
		Statevector[,] d;   /* Damping coefficients */
		Statevector[,] f, g;   /* Flux Vectors */
		Statevector[,] r, ug1;
		Statevector[,] ug; /* Storage of data */

		const double Cp = 1004.5;      /* specific heat, const pres. */
		const double Cv = 717.5;      /* specific heat, const vol. */
		const double gamma = 1.4;   /* Ratio of specific heats */
		const double rgas = 287.0;       /* Gas Constant */
		const double fourthOrderNormalizer = 0.02; /* Damping coefficients */
		const double secondOrderNormalizer = 0.02;

		public void initialise()
		{
			int i, j, k;             /* Dummy counters */
			double scrap, scrap2;     /* Temporary storage */

			/* Set scale factor for interpolation */
			scale = datasizes[size];

			/* Open data file */
			String instr = System.IO.File.ReadAllText(@"../tunnel.dat").Trim();
			char[] spt = new char[4];
			spt[0] = ' ';
			spt[1] = '\n';
			spt[2] = '\t';
			spt[3] = '\r';
			String[] intokenstmp = instr.Split(spt);

			List<String> intokens = new List<String>();
			foreach(String ss in intokenstmp)
			{
				if(!String.IsNullOrEmpty(ss))
					intokens.Add(ss);
			}

			//we just assume the file is good and go for it
			imaxin = Int32.Parse(intokens[0]);
			jmaxin = Int32.Parse(intokens[1]);
			int pfloc = 2;
			
			// Read data into temporary array 
			// note: dummy extra row and column needed to make interpolation simple
			oldval = new double[nf, imaxin + 1, jmaxin + 1];

			for(i = 0; i < imaxin; i++)
			{
				for(j = 0; j < jmaxin; j++)
				{
					for(k = 0; k < nf; k++)
					{
						oldval[k, i, j] = Double.Parse(intokens[pfloc]);
						++pfloc;
					}
				}
			}

			//interpolate onto finer grid 
			imax = (imaxin - 1) * scale + 1;
			jmax = (jmaxin - 1) * scale + 1;
			newval = new double[nf, imax, jmax];

			for(k = 0; k < nf; k++)
			{
				for(i = 0; i < imax; i++)
				{
					for(j = 0; j < jmax; j++)
					{
						int iold = i / scale;
						int jold = j / scale;
						double xf = ((double)i % scale) / ((double)scale);
						double yf = ((double)j % scale) / ((double)scale);
						newval[k, i, j] = (1.0 - xf) * (1.0 - yf) * oldval[k, iold, jold] + (1.0 - xf) * yf * oldval[k, iold, jold + 1] + xf * (1.0 - yf) * oldval[k, iold + 1, jold] + xf * yf * oldval[k, iold + 1, jold + 1];
					}
				}
			}

			//create arrays 
			deltat = new double[imax + 1, jmax + 2];
			opg = new double[imax + 2, jmax + 2];
			pg = new double[imax + 2, jmax + 2];
			pg1 = new double[imax + 2, jmax + 2];
			sxi = new double[imax + 2, jmax + 2];
			seta = new double[imax + 2, jmax + 2];
			tg = new double[imax + 2, jmax + 2];
			tg1 = new double[imax + 2, jmax + 2];
			ug = new Statevector[imax + 2, jmax + 2];
			a = new double[imax, jmax];
			d = new Statevector[imax + 2, jmax + 2];
			f = new Statevector[imax + 2, jmax + 2];
			g = new Statevector[imax + 2, jmax + 2];
			r = new Statevector[imax + 2, jmax + 2];
			ug1 = new Statevector[imax + 2, jmax + 2];
			xnode = new double[imax, jmax];
			ynode = new double[imax, jmax];

			for(i = 0; i < imax + 2; ++i)
			{
				for(j = 0; j < jmax + 2; ++j)
				{
					d[i, j] = new Statevector();
					f[i, j] = new Statevector();
					g[i, j] = new Statevector();
					r[i, j] = new Statevector();
					ug[i, j] = new Statevector();
					ug1[i, j] = new Statevector();
				}
			}

			/* Set farfield values (we use normalized units for everything */
			cff = 1.0;
			vff = 0.0;
			pff = 1.0 / gamma;
			rhoff = 1.0;
			tff = pff / (rhoff * rgas);

			// Copy the interpolated data to arrays 
			for(i = 0; i < imax; i++)
			{
				for(j = 0; j < jmax; j++)
				{
					xnode[i, j] = newval[0, i, j];
					ynode[i, j] = newval[1, i, j];
					ug[i + 1, j + 1].a = newval[2, i, j];
					ug[i + 1, j + 1].b = newval[3, i, j];
					ug[i + 1, j + 1].c = newval[4, i, j];
					ug[i + 1, j + 1].d = newval[5, i, j];

					scrap = ug[i + 1, j + 1].c / ug[i + 1, j + 1].a;
					scrap2 = ug[i + 1, j + 1].b / ug[i + 1, j + 1].a;
					tg[i + 1, j + 1] = ug[i + 1, j + 1].d / ug[i + 1, j + 1].a - (0.5 * (scrap * scrap + scrap2 * scrap2));
					tg[i + 1, j + 1] = tg[i + 1, j + 1] / Cv;
					pg[i + 1, j + 1] = rgas * ug[i + 1, j + 1].a * tg[i + 1, j + 1];
				}
			}


			/* Calculate grid cell areas */
			for(i = 1; i < imax; ++i)
			{
				for(j = 1; j < jmax; ++j)
					a[i, j] = 0.5 * ((xnode[i, j] - xnode[i - 1, j - 1]) * (ynode[i - 1, j] - ynode[i, j - 1]) - (ynode[i, j] - ynode[i - 1, j - 1]) * (xnode[i - 1, j] - xnode[i, j - 1]));
			}
			// throw away temporary arrays 
			oldval = newval = null;
		}


		void doIteration()
		{
			double scrap;
			int i, j;

			/* Record the old pressure values */
			for(i = 1; i < imax; ++i)
			{
				for(j = 1; j < jmax; ++j)
				{
					opg[i, j] = pg[i, j];
				}
			}

			calculateDummyCells(pg, tg, ug);
			calculateDeltaT();

			calculateDamping(pg, ug);

			/* Do the integration */
			/* Step 1 */
			calculateF(pg, tg, ug);
			calculateG(pg, tg, ug);
			calculateR();

			for(i = 1; i < imax; ++i)
			{
				for(j = 1; j < jmax; ++j)
				{
					ug1[i, j].a = ug[i, j].a - 0.25 * deltat[i, j] / a[i, j] * (r[i, j].a - d[i, j].a);
					ug1[i, j].b = ug[i, j].b - 0.25 * deltat[i, j] / a[i, j] * (r[i, j].b - d[i, j].b);
					ug1[i, j].c = ug[i, j].c - 0.25 * deltat[i, j] / a[i, j] * (r[i, j].c - d[i, j].c);
					ug1[i, j].d = ug[i, j].d - 0.25 * deltat[i, j] / a[i, j] * (r[i, j].d - d[i, j].d);
				}
			}
			calculateStateVar(pg1, tg1, ug1);

			/* Step 2 */
			calculateDummyCells(pg1, tg1, ug1);
			calculateF(pg1, tg1, ug1);
			calculateG(pg1, tg1, ug1);
			calculateR();
			for(i = 1; i < imax; ++i)
			{
				for(j = 1; j < jmax; ++j)
				{
					ug1[i, j].a = ug[i, j].a - 0.33333 * deltat[i, j] / a[i, j] * (r[i, j].a - d[i, j].a);
					ug1[i, j].b = ug[i, j].b - 0.33333 * deltat[i, j] / a[i, j] * (r[i, j].b - d[i, j].b);
					ug1[i, j].c = ug[i, j].c - 0.33333 * deltat[i, j] / a[i, j] * (r[i, j].c - d[i, j].c);
					ug1[i, j].d = ug[i, j].d - 0.33333 * deltat[i, j] / a[i, j] * (r[i, j].d - d[i, j].d);
				}
			}
			calculateStateVar(pg1, tg1, ug1);

			/* Step 3 */
			calculateDummyCells(pg1, tg1, ug1);
			calculateF(pg1, tg1, ug1);
			calculateG(pg1, tg1, ug1);
			calculateR();
			for(i = 1; i < imax; ++i)
			{
				for(j = 1; j < jmax; ++j)
				{
					ug1[i, j].a = ug[i, j].a - 0.5 * deltat[i, j] / a[i, j] * (r[i, j].a - d[i, j].a);
					ug1[i, j].b = ug[i, j].b - 0.5 * deltat[i, j] / a[i, j] * (r[i, j].b - d[i, j].b);
					ug1[i, j].c = ug[i, j].c - 0.5 * deltat[i, j] / a[i, j] * (r[i, j].c - d[i, j].c);
					ug1[i, j].d = ug[i, j].d - 0.5 * deltat[i, j] / a[i, j] * (r[i, j].d - d[i, j].d);
				}
			}
			calculateStateVar(pg1, tg1, ug1);

			/* Step 4 (final step) */
			calculateDummyCells(pg1, tg1, ug1);
			calculateF(pg1, tg1, ug1);
			calculateG(pg1, tg1, ug1);
			calculateR();
			for(i = 1; i < imax; ++i)
			{
				for(j = 1; j < jmax; ++j)
				{
					ug[i, j].a -= deltat[i, j] / a[i, j] * (r[i, j].a - d[i, j].a);
					ug[i, j].b -= deltat[i, j] / a[i, j] * (r[i, j].b - d[i, j].b);
					ug[i, j].c -= deltat[i, j] / a[i, j] * (r[i, j].c - d[i, j].c);
					ug[i, j].d -= deltat[i, j] / a[i, j] * (r[i, j].d - d[i, j].d);
				}
			}
			calculateStateVar(pg, tg, ug);

			/* calculate RMS Pressure Error */
			error = 0.0;
			for(i = 1; i < imax; ++i)
			{
				for(j = 1; j < jmax; ++j)
				{
					scrap = pg[i, j] - opg[i, j];
					error += scrap * scrap;
				}
			}
			error = Math.Sqrt(error / (double)((imax - 1) * (jmax - 1)));
		}

		/* Calculates the new state values for range-kutta */
		private void calculateStateVar(double[,] localpg, double[,] localtg, Statevector[,] localug)
		{
			double temp, temp2;
			int i, j;

			for(i = 1; i < imax; ++i)
			{
				for(j = 1; j < jmax; ++j)
				{
					temp = localug[i, j].b;
					temp2 = localug[i, j].c;
					localtg[i, j] = localug[i, j].d / localug[i, j].a - 0.5 * (temp * temp + temp2 * temp2) / (localug[i, j].a * localug[i, j].a);

					localtg[i, j] = localtg[i, j] / Cv;
					localpg[i, j] = localug[i, j].a * rgas * localtg[i, j];
				}
			}
		}

		private void calculateR()
		{

			double deltax, deltay;
			double temp;
			int i, j;
			
			for(i = 1; i < imax; ++i)
			{
				for(j = 1; j < jmax; ++j)
				{

					/* Start by clearing R */
					r[i, j].a = 0.0;
					r[i, j].b = 0.0;
					r[i, j].c = 0.0;
					r[i, j].d = 0.0;

					/* East Face */
					deltay = (ynode[i, j] - ynode[i, j - 1]);
					deltax = (xnode[i, j] - xnode[i, j - 1]);
					temp = 0.5 * deltay;
					r[i, j].a += temp * (f[i, j].a + f[i + 1, j].a);
					r[i, j].b += temp * (f[i, j].b + f[i + 1, j].b);
					r[i, j].c += temp * (f[i, j].c + f[i + 1, j].c);
					r[i, j].d += temp * (f[i, j].d + f[i + 1, j].d);

					temp = -0.5 * deltax;
					r[i, j].a += temp * (g[i, j].a + g[i + 1, j].a);
					r[i, j].b += temp * (g[i, j].b + g[i + 1, j].b);
					r[i, j].c += temp * (g[i, j].c + g[i + 1, j].c);
					r[i, j].d += temp * (g[i, j].d + g[i + 1, j].d);

					/* South Face */
					deltay = (ynode[i, j - 1] - ynode[i - 1, j - 1]);
					deltax = (xnode[i, j - 1] - xnode[i - 1, j - 1]);

					temp = 0.5 * deltay;
					r[i, j].a += temp * (f[i, j].a + f[i, j - 1].a);
					r[i, j].b += temp * (f[i, j].b + f[i, j - 1].b);
					r[i, j].c += temp * (f[i, j].c + f[i, j - 1].c);
					r[i, j].d += temp * (f[i, j].d + f[i, j - 1].d);

					temp = -0.5 * deltax;
					r[i, j].a += temp * (g[i, j].a + g[i, j - 1].a);
					r[i, j].b += temp * (g[i, j].b + g[i, j - 1].b);
					r[i, j].c += temp * (g[i, j].c + g[i, j - 1].c);
					r[i, j].d += temp * (g[i, j].d + g[i, j - 1].d);

					/* West Face */
					deltay = (ynode[i - 1, j - 1] - ynode[i - 1, j]);
					deltax = (xnode[i - 1, j - 1] - xnode[i - 1, j]);

					temp = 0.5 * deltay;
					r[i, j].a += temp * (f[i, j].a + f[i - 1, j].a);
					r[i, j].b += temp * (f[i, j].b + f[i - 1, j].b);
					r[i, j].c += temp * (f[i, j].c + f[i - 1, j].c);
					r[i, j].d += temp * (f[i, j].d + f[i - 1, j].d);

					temp = -0.5 * deltax;
					r[i, j].a += temp * (g[i, j].a + g[i - 1, j].a);
					r[i, j].b += temp * (g[i, j].b + g[i - 1, j].b);
					r[i, j].c += temp * (g[i, j].c + g[i - 1, j].c);
					r[i, j].d += temp * (g[i, j].d + g[i - 1, j].d);


					/* North Face */
					deltay = (ynode[i - 1, j] - ynode[i, j]);
					deltax = (xnode[i - 1, j] - xnode[i, j]);

					temp = 0.5 * deltay;
					r[i, j].a += temp * (f[i, j].a + f[i + 1, j].a);
					r[i, j].b += temp * (f[i, j].b + f[i + 1, j].b);
					r[i, j].c += temp * (f[i, j].c + f[i + 1, j].c);
					r[i, j].d += temp * (f[i, j].d + f[i + 1, j].d);

					temp = -0.5 * deltax;
					r[i, j].a += temp * (g[i, j].a + g[i, j + 1].a);
					r[i, j].b += temp * (g[i, j].b + g[i, j + 1].b);
					r[i, j].c += temp * (g[i, j].c + g[i, j + 1].c);
					r[i, j].d += temp * (g[i, j].d + g[i, j + 1].d);
				}
			}
		}

		private void calculateG(double[,] localpg, double[,] localtg, Statevector[,] localug)
		{
			double temp, temp2, temp3;
			double v;
			int i, j;

			for(i = 0; i < imax + 1; ++i)
			{
				for(j = 0; j < jmax + 1; ++j)
				{
					v = localug[i, j].c / localug[i, j].a;
					g[i, j].a = localug[i, j].c;
					g[i, j].b = localug[i, j].b * v;
					g[i, j].c = localug[i, j].c * v + localpg[i, j];
					temp = localug[i, j].b * localug[i, j].b;
					temp2 = localug[i, j].c * localug[i, j].c;
					temp3 = localug[i, j].a * localug[i, j].a;
					g[i, j].d = localug[i, j].c * (Cp * localtg[i, j] + (0.5 * (temp + temp2) / (temp3)));
				}
			}
		}


		private void calculateF(double[,] localpg, double[,] localtg, Statevector[,] localug)
		{
			{
				double u;
				double temp1, temp2, temp3;
				int i, j;

				for(i = 0; i < imax + 1; ++i)
				{
					for(j = 0; j < jmax + 1; ++j)
					{
						u = localug[i, j].b / localug[i, j].a;
						f[i, j].a = localug[i, j].b;
						f[i, j].b = localug[i, j].b * u + localpg[i, j];
						f[i, j].c = localug[i, j].c * u;
						temp1 = localug[i, j].b * localug[i, j].b;
						temp2 = localug[i, j].c * localug[i, j].c;
						temp3 = localug[i, j].a * localug[i, j].a;
						f[i, j].d = localug[i, j].b * (Cp * localtg[i, j] + (0.5 * (temp1 + temp2) / (temp3)));
					}
				}
			}
		}

		private void calculateDamping(double[,] localpg, Statevector[,] localug)
		{
			double adt, sbar;
			double nu2;
			double nu4;
			double tempdouble;
			int i, j;
			Statevector temp = new Statevector();
			Statevector temp2 = new Statevector();
			Statevector scrap2 = new Statevector();
			Statevector scrap4 = new Statevector();

			nu2 = secondOrderDamping * secondOrderNormalizer;
			nu4 = fourthOrderDamping * fourthOrderNormalizer;

			/* First do the pressure switches */
			for(i = 1; i < imax; ++i)
			{
				for(j = 1; j < jmax; ++j)
				{
					sxi[i, j] = Math.Abs(localpg[i + 1, j] - 2.0 * localpg[i, j] + localpg[i - 1, j]) / localpg[i, j];
					seta[i, j] = Math.Abs(localpg[i, j + 1] - 2.0 * localpg[i, j] + localpg[i, j - 1]) / localpg[i, j];
				}
			}

			/* Then calculate the fluxes */
			for(i = 1; i < imax; ++i)
			{
				for(j = 1; j < jmax; ++j)
				{

					/* Clear values */
					/* East Face */
					if(i > 1 && i < imax - 1)
					{
						adt = (a[i, j] + a[i + 1, j]) / (deltat[i, j] + deltat[i + 1, j]);
						sbar = (sxi[i + 1, j] + sxi[i, j]) * 0.5;
					}
					else
					{
						adt = a[i, j] / deltat[i, j];
						sbar = sxi[i, j];
					}
					tempdouble = nu2 * sbar * adt;
					scrap2.a = tempdouble * (localug[i + 1, j].a - localug[i, j].a);
					scrap2.b = tempdouble * (localug[i + 1, j].b - localug[i, j].b);
					scrap2.c = tempdouble * (localug[i + 1, j].c - localug[i, j].c);
					scrap2.d = tempdouble * (localug[i + 1, j].d - localug[i, j].d);

					if(i > 1 && i < imax - 1)
					{
						temp = localug[i + 2, j].svect(localug[i - 1, j]);

						temp2.a = 3.0 * (localug[i, j].a - localug[i + 1, j].a);
						temp2.b = 3.0 * (localug[i, j].b - localug[i + 1, j].b);
						temp2.c = 3.0 * (localug[i, j].c - localug[i + 1, j].c);
						temp2.d = 3.0 * (localug[i, j].d - localug[i + 1, j].d);

						tempdouble = -nu4 * adt;
						scrap4.a = tempdouble * (temp.a + temp2.a);
						scrap4.b = tempdouble * (temp.a + temp2.b);
						scrap4.c = tempdouble * (temp.a + temp2.c);
						scrap4.d = tempdouble * (temp.a + temp2.d);
					}
					else
					{
						scrap4.a = 0.0;
						scrap4.b = 0.0;
						scrap4.c = 0.0;
						scrap4.d = 0.0;
					}

					temp.a = scrap2.a + scrap4.a;
					temp.b = scrap2.b + scrap4.b;
					temp.c = scrap2.c + scrap4.c;
					temp.d = scrap2.d + scrap4.d;
					d[i, j] = temp;

					/* West Face */
					if(i > 1 && i < imax - 1)
					{
						adt = (a[i, j] + a[i - 1, j]) / (deltat[i, j] + deltat[i - 1, j]);
						sbar = (sxi[i, j] + sxi[i - 1, j]) * 0.5;
					}
					else
					{
						adt = a[i, j] / deltat[i, j];
						sbar = sxi[i, j];
					}

					tempdouble = -nu2 * sbar * adt;
					scrap2.a = tempdouble * (localug[i, j].a - localug[i - 1, j].a);
					scrap2.b = tempdouble * (localug[i, j].b - localug[i - 1, j].b);
					scrap2.c = tempdouble * (localug[i, j].c - localug[i - 1, j].c);
					scrap2.d = tempdouble * (localug[i, j].d - localug[i - 1, j].d);


					if(i > 1 && i < imax - 1)
					{
						temp = localug[i + 1, j].svect(localug[i - 2, j]);
						temp2.a = 3.0 * (localug[i - 1, j].a - localug[i, j].a);
						temp2.b = 3.0 * (localug[i - 1, j].b - localug[i, j].b);
						temp2.c = 3.0 * (localug[i - 1, j].c - localug[i, j].c);
						temp2.d = 3.0 * (localug[i - 1, j].d - localug[i, j].d);

						tempdouble = nu4 * adt;
						scrap4.a = tempdouble * (temp.a + temp2.a);
						scrap4.b = tempdouble * (temp.a + temp2.b);
						scrap4.c = tempdouble * (temp.a + temp2.c);
						scrap4.d = tempdouble * (temp.a + temp2.d);
					}
					else
					{
						scrap4.a = 0.0;
						scrap4.b = 0.0;
						scrap4.c = 0.0;
						scrap4.d = 0.0;
					}

					d[i, j].a += scrap2.a + scrap4.a;
					d[i, j].b += scrap2.b + scrap4.b;
					d[i, j].c += scrap2.c + scrap4.c;
					d[i, j].d += scrap2.d + scrap4.d;

					/* North Face */
					if(j > 1 && j < jmax - 1)
					{
						adt = (a[i, j] + a[i, j + 1]) / (deltat[i, j] + deltat[i, j + 1]);
						sbar = (seta[i, j] + seta[i, j + 1]) * 0.5;
					}
					else
					{
						adt = a[i, j] / deltat[i, j];
						sbar = seta[i, j];
					}
					tempdouble = nu2 * sbar * adt;
					scrap2.a = tempdouble * (localug[i, j + 1].a - localug[i, j].a);
					scrap2.b = tempdouble * (localug[i, j + 1].b - localug[i, j].b);
					scrap2.c = tempdouble * (localug[i, j + 1].c - localug[i, j].c);
					scrap2.d = tempdouble * (localug[i, j + 1].d - localug[i, j].d);

					if(j > 1 && j < jmax - 1)
					{
						temp = localug[i, j + 2].svect(localug[i, j - 1]);
						temp2.a = 3.0 * (localug[i, j].a - localug[i, j + 1].a);
						temp2.b = 3.0 * (localug[i, j].b - localug[i, j + 1].b);
						temp2.c = 3.0 * (localug[i, j].c - localug[i, j + 1].c);
						temp2.d = 3.0 * (localug[i, j].d - localug[i, j + 1].d);

						tempdouble = -nu4 * adt;
						scrap4.a = tempdouble * (temp.a + temp2.a);
						scrap4.b = tempdouble * (temp.a + temp2.b);
						scrap4.c = tempdouble * (temp.a + temp2.c);
						scrap4.d = tempdouble * (temp.a + temp2.d);
					}
					else
					{
						scrap4.a = 0.0;
						scrap4.b = 0.0;
						scrap4.c = 0.0;
						scrap4.d = 0.0;
					}
					d[i, j].a += scrap2.a + scrap4.a;
					d[i, j].b += scrap2.b + scrap4.b;
					d[i, j].c += scrap2.c + scrap4.c;
					d[i, j].d += scrap2.d + scrap4.d;

					/* South Face */
					if(j > 1 && j < jmax - 1)
					{
						adt = (a[i, j] + a[i, j - 1]) / (deltat[i, j] + deltat[i, j - 1]);
						sbar = (seta[i, j] + seta[i, j - 1]) * 0.5;
					}
					else
					{
						adt = a[i, j] / deltat[i, j];
						sbar = seta[i, j];
					}
					tempdouble = -nu2 * sbar * adt;
					scrap2.a = tempdouble * (localug[i, j].a - localug[i, j - 1].a);
					scrap2.b = tempdouble * (localug[i, j].b - localug[i, j - 1].b);
					scrap2.c = tempdouble * (localug[i, j].c - localug[i, j - 1].c);
					scrap2.d = tempdouble * (localug[i, j].d - localug[i, j - 1].d);

					if(j > 1 && j < jmax - 1)
					{
						temp = localug[i, j + 1].svect(localug[i, j - 2]);
						temp2.a = 3.0 * (localug[i, j - 1].a - localug[i, j].a);
						temp2.b = 3.0 * (localug[i, j - 1].b - localug[i, j].b);
						temp2.c = 3.0 * (localug[i, j - 1].c - localug[i, j].c);
						temp2.d = 3.0 * (localug[i, j - 1].d - localug[i, j].d);

						tempdouble = nu4 * adt;
						scrap4.a = tempdouble * (temp.a + temp2.a);
						scrap4.b = tempdouble * (temp.a + temp2.b);
						scrap4.c = tempdouble * (temp.a + temp2.c);
						scrap4.d = tempdouble * (temp.a + temp2.d);
					}
					else
					{
						scrap4.a = 0.0;
						scrap4.b = 0.0;
						scrap4.c = 0.0;
						scrap4.d = 0.0;
					}
					d[i, j].a += scrap2.a + scrap4.a;
					d[i, j].b += scrap2.b + scrap4.b;
					d[i, j].c += scrap2.c + scrap4.c;
					d[i, j].d += scrap2.d + scrap4.d;
				}
			}
		}

		private void calculateDeltaT()
		{
			double xeta, yeta, xxi, yxi;              /* Local change in x and y */
			int i, j;
			double mint;
			double c, q, r;
			double safety_factor = 0.7;

			for(i = 1; i < imax; ++i)
			{
				for(j = 1; j < jmax; ++j)
				{
					xxi = (xnode[i, j] - xnode[i - 1, j] + xnode[i, j - 1] - xnode[i - 1, j - 1]) * 0.5;
					yxi = (ynode[i, j] - ynode[i - 1, j] + ynode[i, j - 1] - ynode[i - 1, j - 1]) * 0.5;
					xeta = (xnode[i, j] - xnode[i, j - 1] + xnode[i - 1, j] - xnode[i - 1, j - 1]) * 0.5;
					yeta = (ynode[i, j] - ynode[i, j - 1] + ynode[i - 1, j] - ynode[i - 1, j - 1]) * 0.5;

					q = (yeta * ug[i, j].b - xeta * ug[i, j].c);
					r = (-yxi * ug[i, j].b + xxi * ug[i, j].c);
					c = Math.Sqrt(gamma * rgas * tg[i, j]);

					deltat[i, j] = safety_factor * 2.8284 * a[i, j] / ((Math.Abs(q) + Math.Abs(r)) / ug[i, j].a + c * Math.Sqrt(xxi * xxi + yxi * yxi + xeta * xeta + yeta * yeta + 2.0 * Math.Abs(xeta * xxi + yeta * yxi)));
				}
			}

			/* If that's the user's choice, make it time accurate */
			if(ntime == 1)
			{
				mint = 100000.0;
				for(i = 1; i < imax; ++i)
				{
					for(j = 1; j < jmax; ++j)
					{
						if(deltat[i, j] < mint)
							mint = deltat[i, j];
					}
				}

				for(i = 1; i < imax; ++i)
				{
					for(j = 1; j < jmax; ++j)
						deltat[i, j] = mint;
				}
			}
		}

		private void calculateDummyCells(double[,] localpg, double[,] localtg, Statevector[,] localug)
		{
			double c;
			double jminus;
			double jplus;
			double s;
			double rho, temp, u, v;
			double scrap, scrap2;
			double theta;
			double uprime;
			int i, j;
			Vector2 norm = new Vector2();
			Vector2 tan = new Vector2();
			Vector2 u1 = new Vector2();

			uff = machff;
			jplusff = uff + 2.0 / (gamma - 1.0) * cff;
			jminusff = uff - 2.0 / (gamma - 1.0) * cff;

			for(i = 1; i < imax; ++i)
			{
				/* Bottom wall boundary cells */
				/* Routine checked by brute force for initial conditions, 4/9; 4:30 */
				/* Routine checked by brute force for random conditions, 4/13, 4:40 pm */
				/* Construct tangent vectors */
				tan.ihat = xnode[i, 0] - xnode[i - 1, 0];
				tan.jhat = ynode[i, 0] - ynode[i - 1, 0];
				norm.ihat = -(ynode[i, 0] - ynode[i - 1, 0]);
				norm.jhat = xnode[i, 0] - xnode[i - 1, 0];

				scrap = tan.magnitude();
				tan.ihat = tan.ihat / scrap;
				tan.jhat = tan.jhat / scrap;
				scrap = norm.magnitude();
				norm.ihat = norm.ihat / scrap;
				norm.jhat = norm.jhat / scrap;

				/* now set some state variables */
				rho = localug[i, 1].a;
				localtg[i, 0] = localtg[i, 1];
				u1.ihat = localug[i, 1].b / rho;
				u1.jhat = localug[i, 1].c / rho;

				u = u1.dot(tan) + u1.dot(norm) * tan.jhat / norm.jhat;
				u = u / (tan.ihat - (norm.ihat * tan.jhat / norm.jhat));

				v = -(u1.dot(norm) + u * norm.ihat) / norm.jhat;

				/* And construct the new state vector */
				localug[i, 0].a = localug[i, 1].a;
				localug[i, 0].b = rho * u;
				localug[i, 0].c = rho * v;
				localug[i, 0].d = rho * (Cv * localtg[i, 0] + 0.5 * (u * u + v * v));
				localpg[i, 0] = localpg[i, 1];

				/* Top Wall Boundary Cells */
				/* Checked numerically for default conditions, 4/9 at 5:30 pm */
				/* Construct normal and tangent vectors */
				/* This part checked and works; it produces the correct vectors */
				tan.ihat = xnode[i, jmax - 1] - xnode[i - 1, jmax - 1];
				tan.jhat = ynode[i, jmax - 1] - ynode[i - 1, jmax - 1];
				norm.ihat = ynode[i, jmax - 1] - ynode[i - 1, jmax - 1];
				norm.jhat = -(xnode[i, jmax - 1] - xnode[i - 1, jmax - 1]);

				scrap = tan.magnitude();
				tan.ihat = tan.ihat / scrap;
				tan.jhat = tan.jhat / scrap;
				scrap = norm.magnitude();
				norm.ihat = norm.ihat / scrap;
				norm.jhat = norm.jhat / scrap;

				/* now set some state variables */
				rho = localug[i, jmax - 1].a;
				temp = localtg[i, jmax - 1];
				u1.ihat = localug[i, jmax - 1].b / rho;
				u1.jhat = localug[i, jmax - 1].c / rho;

				u = u1.dot(tan) + u1.dot(norm) * tan.jhat / norm.jhat;
				u = u / (tan.ihat - (norm.ihat * tan.jhat / norm.jhat));

				v = -(u1.dot(norm) + u * norm.ihat) / norm.jhat;

				/* And construct the new state vector */
				localug[i, jmax].a = localug[i, jmax - 1].a;
				localug[i, jmax].b = rho * u;
				localug[i, jmax].c = rho * v;
				localug[i, jmax].d = rho * (Cv * temp + 0.5 * (u * u + v * v));
				localtg[i, jmax] = temp;
				localpg[i, jmax] = localpg[i, jmax - 1];
			}

			for(j = 1; j < jmax; ++j)
			{
				/* Inlet Boundary Cells: unchecked */
				/* Construct the normal vector; This works, 4/10, 2:00 pm */
				norm.ihat = ynode[0, j - 1] - ynode[0, j];
				norm.jhat = xnode[0, j] - xnode[0, j - 1];
				scrap = norm.magnitude();
				norm.ihat = norm.ihat / scrap;
				norm.jhat = norm.jhat / scrap;
				theta = Math.Acos((ynode[0, j - 1] - ynode[0, j]) /
				 Math.Sqrt((xnode[0, j] - xnode[0, j - 1]) * (xnode[0, j] - xnode[0, j - 1]) + (ynode[0, j - 1] - ynode[0, j]) * (ynode[0, j - 1] - ynode[0, j])));

				u1.ihat = localug[1, j].b / localug[1, j].a;
				u1.jhat = localug[1, j].c / localug[1, j].a;
				uprime = u1.ihat * Math.Cos(theta);
				c = Math.Sqrt(gamma * rgas * localtg[1, j]);
				/* Supersonic inflow; works on the initial cond, 4/10 at 3:10 pm */
				if(uprime < -c)
				{
					/* Use far field conditions */
					localug[0, j].a = rhoff;
					localug[0, j].b = rhoff * uff;
					localug[0, j].c = rhoff * vff;
					localug[0, j].d = rhoff * (Cv * tff + 0.5 * (uff * uff + vff * vff));
					localtg[0, j] = tff;
					localpg[0, j] = pff;
				}
				/* Subsonic inflow */
				/* This works on the initial conditions 4/10 @ 2:20 pm */
				else if(uprime < 0.0)
				{
					/* Calculate Riemann invarients here */
					jminus = u1.ihat - 2.0 / (gamma - 1.0) * c;
					s = Math.Log(pff) - gamma * Math.Log(rhoff);
					v = vff;

					u = (jplusff + jminus) / 2.0;
					scrap = (jplusff - u) * (gamma - 1.0) * 0.5;
					localtg[0, j] = (1.0 / (gamma * rgas)) * scrap * scrap;
					localpg[0, j] = Math.Exp(s) / Math.Pow((rgas * localtg[0, j]), gamma);
					localpg[0, j] = Math.Pow(localpg[0, j], 1.0 / (1.0 - gamma));

					/* And now: construct the new state vector */
					localug[0, j].a = localpg[0, j] / (rgas * localtg[0, j]);
					localug[0, j].b = localug[0, j].a * u;
					localug[0, j].c = localug[0, j].a * v;
					localug[0, j].d = localug[0, j].a * (Cv * tff + 0.5 * (u * u + v * v));
				}
				/* Other options */
				/* We should throw an exception here */
				else
				{
					Console.WriteLine("You have outflow at the inlet, which is not allowed.");
				}

				/* Outlet Boundary Cells */
				/* Construct the normal vector; works, 4/10 3:10 pm */
				norm.ihat = ynode[0, j] - ynode[0, j - 1];
				norm.jhat = xnode[0, j - 1] - xnode[0, j];
				scrap = norm.magnitude();
				norm.ihat = norm.ihat / scrap;
				norm.jhat = norm.jhat / scrap;
				scrap = xnode[0, j - 1] - xnode[0, j];
				scrap2 = ynode[0, j] - ynode[0, j - 1];
				theta = Math.Acos((ynode[0, j] - ynode[0, j - 1]) / Math.Sqrt(scrap * scrap + scrap2 * scrap2));

				u1.ihat = localug[imax - 1, j].b / localug[imax - 1, j].a;
				u1.jhat = localug[imax - 1, j].c / localug[imax - 1, j].a;
				uprime = u1.ihat * Math.Cos(theta);
				c = Math.Sqrt(gamma * rgas * localtg[imax - 1, j]);
				/* Supersonic outflow; works for defaults cond, 4/10: 3:10 pm */
				if(uprime > c)
				{
					/* Use a backward difference 2nd order derivative approximation */
					/* To set values at exit */
					localug[imax, j].a = 2.0 * localug[imax - 1, j].a - localug[imax - 2, j].a;
					localug[imax, j].b = 2.0 * localug[imax - 1, j].b - localug[imax - 2, j].b;
					localug[imax, j].c = 2.0 * localug[imax - 1, j].c - localug[imax - 2, j].c;
					localug[imax, j].d = 2.0 * localug[imax - 1, j].d - localug[imax - 2, j].d;
					localpg[imax, j] = 2.0 * localpg[imax - 1, j] - localpg[imax - 2, j];
					localtg[imax, j] = 2.0 * localtg[imax - 1, j] - localtg[imax - 2, j];
				}
				/* Subsonic Outflow; works for defaults cond, 4/10: 3:10 pm */
				else if(uprime < c && uprime > 0)
				{
					jplus = u1.ihat + 2.0 / (gamma - 1) * c;
					v = localug[imax - 1, j].c / localug[imax - 1, j].a;
					s = Math.Log(localpg[imax - 1, j]) - gamma * Math.Log(localug[imax - 1, j].a);

					u = (jplus + jminusff) / 2.0;
					scrap = (jplus - u) * (gamma - 1.0) * 0.5;
					localtg[imax, j] = (1.0 / (gamma * rgas)) * scrap * scrap;
					localpg[imax, j] = Math.Exp(s) / Math.Pow((rgas * localtg[imax, j]), gamma);
					localpg[imax, j] = Math.Pow(localpg[imax, j], 1.0 / (1.0 - gamma));
					rho = localpg[imax, j] / (rgas * localtg[imax, j]);

					/* And now, construct the new state vector */
					localug[imax, j].a = rho;
					localug[imax, j].b = rho * u;
					localug[imax, j].c = rho * v;
					localug[imax, j].d = rho * (Cv * localtg[imax, j] + 0.5 * (u * u + v * v));

				}
				/* Other cases that shouldn't have to be used. */
				else if(uprime < -c)
				{
					/* Supersonic inflow */
					/* Use far field conditions */
					localug[0, j].a = rhoff;
					localug[0, j].b = rhoff * uff;
					localug[0, j].c = rhoff * vff;
					localug[0, j].d = rhoff * (Cv * tff + 0.5 * (uff * uff + vff * vff));
					localtg[0, j] = tff;
					localpg[0, j] = pff;
				}
				/* Subsonic inflow */
				/* This works on the initial conditions 4/10 @ 2:20 pm */
				else if(uprime < 0.0)
				{
					/* Debug: throw exception here? */
					/* Calculate Riemann invarients here */
					jminus = u1.ihat - 2.0 / (gamma - 1.0) * c;
					s = Math.Log(pff) - gamma * Math.Log(rhoff);
					v = vff;

					u = (jplusff + jminus) / 2.0;
					scrap = (jplusff - u) * (gamma - 1.0) * 0.5;
					localtg[0, j] = (1.0 / (gamma * rgas)) * scrap * scrap;
					localpg[0, j] = Math.Exp(s) / Math.Pow((rgas * localtg[0, j]), gamma);
					localpg[0, j] = Math.Pow(localpg[0, j], 1.0 / (1.0 - gamma));

					/* And now: construct the new state vector */
					localug[0, j].a = localpg[0, j] / (rgas * localtg[0, j]);
					localug[0, j].b = localug[0, j].a * u;
					localug[0, j].c = localug[0, j].a * v;
					localug[0, j].d = localug[0, j].a * (Cv * tff + 0.5 * (u * u + v * v));
				}
				/* Other Options */
				/* Debug: throw exception here? */
				else
				{
					Console.WriteLine("You have inflow at the outlet, which is not allowed.");
				}
			}
			/* Do something with corners to avoid division by zero errors */
			/* What you do shouldn't matter */
			localug[0, 0] = localug[1, 0];
			localug[imax, 0] = localug[imax, 1];
			localug[0, jmax] = localug[1, jmax];
			localug[imax, jmax] = localug[imax, jmax - 1];
		}

		public void runiters()
		{

			for(int i = 0; i < iter; i++)
			{
				Console.WriteLine("Iteration: " + i.ToString() + "...");
				doIteration();
			}
		}

	}

	public class Statevector
	{
		public double a;   /* Storage for Statevectors */
		public double b;
		public double c;
		public double d;

		public Statevector()
		{
			a = 0.0;
			b = 0.0;
			c = 0.0;
			d = 0.0;
		}

		/* Most of these vector manipulation routines are not used in this program */
		/* because I inlined them for speed.  I leave them here because they may */
		/* be useful in the future. */
		public Statevector amvect(double m, Statevector that)
		{
			/* Adds statevectors multiplies the sum by scalar m */
			Statevector answer = new Statevector();

			answer.a = m * (this.a + that.a);
			answer.b = m * (this.b + that.b);
			answer.c = m * (this.c + that.c);
			answer.d = m * (this.d + that.d);

			return answer;
		}

		public Statevector avect(Statevector that)
		{
			Statevector answer = new Statevector();
			/* Adds two statevectors */
			answer.a = this.a + that.a;
			answer.b = this.b + that.b;
			answer.c = this.c + that.c;
			answer.d = this.d + that.d;

			return answer;
		}

		public Statevector mvect(double m)
		{
			Statevector answer = new Statevector();
			/* Multiplies statevector scalar m */
			answer.a = m * this.a;
			answer.b = m * this.b;
			answer.c = m * this.c;
			answer.d = m * this.d;

			return answer;
		}

		public Statevector svect(Statevector that)
		{
			Statevector answer = new Statevector();
			/* Subtracts vector that from this */
			answer.a = this.a - that.a;
			answer.b = this.b - that.b;
			answer.c = this.c - that.c;
			answer.d = this.d - that.d;

			return answer;
		}

		public Statevector smvect(double m, Statevector that)
		{
			Statevector answer = new Statevector();
			/* Subtracts statevector that from this and multiplies the */
			/* result by scalar m */
			answer.a = m * (this.a - that.a);
			answer.b = m * (this.b - that.b);
			answer.c = m * (this.c - that.c);
			answer.d = m * (this.d - that.d);

			return answer;
		}
	}

	public class Vector2
	{
		public double ihat;   /* Storage for 2-D vector */
		public double jhat;

		public Vector2()
		{
			ihat = 0.0;
			jhat = 0.0;
		}

		public double magnitude()
		{
			double mag;

			mag = Math.Sqrt(this.ihat * this.ihat + this.jhat * this.jhat);
			return mag;
		}

		public double dot(Vector2 that)
		{
			/* Calculates dot product of two 2-d vector */
			double answer;

			answer = this.ihat * that.ihat + this.jhat * that.jhat;

			return answer;
		}
	}
}


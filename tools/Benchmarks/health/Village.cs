
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace Benchmarks.Health
{
	/**
 * A class represnting a village in the Columbian health care system
 * simulation.
 */
	public class Village
	{
		private Village[] forward;
		public bool rootVillage;
		private List<Patient> returned;
		private Hospital hospital;
		private int label;
		public int seed;

		public static int IA;
		public static double IM;
		public static double AM;
		public static int IQ;
		public static int IR;
		public static int MASK;


		/**
	 * Construct an empty village.
	 *
	 * @param level the
	 * @param lab   the unique label for the village
	 * @param p     a reference to the "parent" village
	 * @param s     the user supplied seed value
	 */
		public Village (int level, int l, bool isRootV, int s)
		{
			rootVillage = isRootV;
			label = l;
			forward = new Village[4];
			seed = label * (IQ + s);
			hospital = new Hospital (level);
			returned = new List<Patient> ();
		}

		public static void initVillageStatic ()
		{
			Village.IA = 16807;
			Village.IM = 2147483647.0;
			Village.AM = (1.0 / IM);
			Village.IQ = 127773;
			Village.IR = 2836;
			Village.MASK = 123459876;
		}

		/**
	 * Return true if a patient should stay in this village or
	 * move up to the "parent" village.
	 *
	 * @return true if a patient says in this village
	 */
		public bool staysHere ()
		{
			double rand = myRand (seed);
			seed = (int)(rand * IM);
			return (rand > 0.1 || rootVillage);
		}

		/**
	 * Create a set of villages.  Villages are represented as a quad tree.
	 * Each village contains references to four other villages.  Users
	 * specify the number of levels.
	 *
	 * @param level the number of level of villages.
	 * @param label a unique label for the village
	 * @param back  a link to the "parent" village
	 * @param seed  the user supplied seed value.
	 * @return the village that was created
	 */
		public static Village createVillage (int level, int label, bool isRootV, int seed)
		{
			if (level == 0)
				return null;
			else {
				Village village = new Village (level, label, isRootV, seed);
				for (int i = 3; i >= 0; i--) {
					Village child = createVillage (level - 1, (label * 4) + i + 1, false, seed);
					village.forward [i] = child;
				}
				return village;
			}
		}

		/**
	 * Simulate the Columbian health care system for a village.
	 *
	 * @return a list of patients refered to the next village
	 */
		public List<Patient> simulate ()
		{
			// the list of patients refered from each child village
			Patient p;

			for (int i = 0; i < this.forward.Length; ++i) {
				Village v = this.forward [i];
				List<Patient> val = new List<Patient> ();

				if (v != null)
					val = v.simulate ();

				for (int j = 0; j < val.Count; ++j)
					hospital.putInHospital (val [j]);
			}

			hospital.checkPatientsInside (returned);
			List<Patient> up = hospital.checkPatientsAssess (this);
			hospital.checkPatientsWaiting ();

			// generate new patients
			p = generatePatient ();
			if (p != null)
				hospital.putInHospital (p);

			return up;
		}

		/**
	 * Summarize results of the simulation for the Village
	 *
	 * @return a summary of the simulation results for the village
	 */
		public Results getResults ()
		{
			List<Results> fval = new List<Results> ();
			for (int i = 0; i < this.forward.Length; ++i) {
				Village v = this.forward [i];
				if (v != null)
					fval.Add (v.getResults ());
			}

			Results r = new Results ();
			for (int i = 0; i < fval.Count; ++i) {
				r.totalHospitals = r.totalHospitals + fval [i].totalHospitals;
				r.totalPatients = r.totalPatients + fval [i].totalPatients;
				r.totalTime = r.totalTime + fval [i].totalTime;
			}

			for (int j = 0; j < this.returned.Count; ++j) {
				Patient p = this.returned [j];
				r.totalHospitals = r.totalHospitals + p.hospitalsVisited;
				r.totalTime = r.totalTime + p.time;
				r.totalPatients = r.totalPatients + 1;
			}
			return r;
		}

		/**
	 * Try to generate more patients for the village.
	 *
	 * @return a new patient or null if a new patient isn't created
	 */
		private Patient generatePatient ()
		{
			double rand = myRand (seed);
			seed = (int)(rand * IM);
			Patient p = null;
			if (rand > 0.666)
				p = Patient.makePatient (this);

			return p;
		}

		/**
	 * Random number generator.
	 */
		public static double myRand (int idum)
		{
			idum = idum ^ MASK;
			int k = idum / IQ;
			idum = IA * (idum - k * IQ) - IR * k;
			idum = idum ^ MASK;
			if (idum < 0)
				idum += (int)Math.Floor (IM);

			double answer = AM * ((double)idum);
			return answer;
		}
	}
}

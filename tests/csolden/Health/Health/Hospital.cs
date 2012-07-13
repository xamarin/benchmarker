
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

/**
 * A class representing a hospital in the Columbian health care system.
 */
public class Hospital
{
	private int zillesTime;
	private int personnel;
	private int freePersonnel;
	private List<Patient> waiting;
	private List<Patient> assess;
	private List<Patient> inside;
	private List<Patient> up;

	public Hospital(int level)
	{
		zillesTime = 0;
		personnel = 1 << (level - 1);
		freePersonnel = personnel;
		waiting = new List<Patient>();
		assess = new List<Patient>();
		inside = new List<Patient>();
		up = new List<Patient>();
	}


	/**
	 * Add a patient to this hospital
	 *
	 * @param p the patient.
	 */
	public void putInHospital(Patient p)
	{
		int num = p.hospitalsVisited;
		p.hospitalsVisited = p.hospitalsVisited + 1;
		if(freePersonnel > 0)
		{
			freePersonnel = freePersonnel - 1;
			assess.Add(p);
			p.timeLeft = 3;
			p.time += 3;
		}
		else
		{
			p.timeLeft = zillesTime;
			waiting.Add(p);
		}
	}

	/**
	 * Check the patients inside the hospital to see if any are finished.
	 * If so, then free up the personnel and and the patient to the returned
	 * list.
	 *
	 * @param returned a list of patients
	 */
	public void checkPatientsInside(List<Patient> returned)
	{
		int i = 0;
		while(i < this.inside.Count)
		{
			Patient p = this.inside[i];
			p.timeLeft = p.timeLeft - 1;
			if(p.timeLeft == 0)
			{
				freePersonnel = freePersonnel + 1;
				this.inside.RemoveAt(i);
				returned.Add(p);
			}
			else
				++i;
		}
	}

	/**
	 * Asses the patients in the village.
	 *
	 * @param v the village that owns the hospital
	 */
	public List<Patient> checkPatientsAssess(Village v)
	{
		double rand;
		bool stayhere;
		List<Patient> up = new List<Patient>();

		int i = 0;
		while(i < this.assess.Count)
		{
			Patient p = this.assess[i];
			p.timeLeft = p.timeLeft - 1;
			if(p.timeLeft == 0)
			{
				//inline stays here
				rand = Village.myRand(v.seed);
				v.seed = (int)(rand * Village.IM);
				stayhere = (rand > 0.1 || v.rootVillage);

				this.assess.RemoveAt(i);
				if(stayhere)
				{
					inside.Add(p);
					p.timeLeft = 10;
					p.time = p.timeLeft + 10;
				}
				else
				{
					freePersonnel = freePersonnel + 1;
					up.Add(p);
				}
			}
			else
				++i;
		}
		return up;
	}

	public void checkPatientsWaiting()
	{
		if(freePersonnel == 0)
			return;

		int i = 0;
		while(i < this.waiting.Count)
		{
			Patient p = this.waiting[i];
			if(freePersonnel > 0)
			{
				freePersonnel = freePersonnel - 1;
				p.time = p.time + (3 + this.zillesTime - p.timeLeft); //health harmful
				p.timeLeft = 3;
				this.waiting.RemoveAt(i);
				assess.Add(p);
			}
			else
				++i;
		}

		this.zillesTime = this.zillesTime + 1;
	}
}



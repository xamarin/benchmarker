
/**
 * A class that represents a patient in the health care system.
 */
public class Patient
{
	public int hospitalsVisited;
	public int time;
	public int timeLeft;
	//Village home;

	/**
	 * Construct a new patient that is from the specified village.
	 *
	 * @param v the home village of the patient.
	 */
	public static Patient makePatient(Village v)
	{
		Patient p = new Patient();

		//p.home = v;
		p.hospitalsVisited = 0;
		p.time = 0;
		p.timeLeft = 0;

		return p;
	}
}

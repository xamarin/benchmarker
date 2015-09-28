using System;
using System.Diagnostics;
using Benchmarker;
using Java.IO;

namespace AndroidAgent
{
	public class AndroidCPUManagment
	{
		public string[] cpus { get; }

		private string[] savedFrequency;
		private string[] savedGovenor;

		public AndroidCPUManagment ()
		{
			Debug.Assert (IsRooted ());
			cpus = AvailableCPUs ();
			savedFrequency = new string[cpus.Length];
			savedGovenor = new string[cpus.Length];

			for (int i = 0; i < cpus.Length; i++) {
				string cpu = cpus [i];

				Logging.GetLogging ().Info ("CPU: " + cpu);

				Logging.GetLogging ().Info ("\tAvailable Frequencies:");
				string freqs = "";
				foreach (string f in AvailableCPUFreuquencies (cpu)) {
					freqs += ", " + f;
				}
				Logging.GetLogging ().Info ("\t\t" + freqs);
				var freq = GetCPUFrequency (cpu);
				Logging.GetLogging ().Info ("\tCurrent Frequency: " + freq);
				savedFrequency [i] = freq;

				Logging.GetLogging ().Info ("\tAvailable Govenors:");
				string govs = "";
				foreach (string g in AvailableCPUGovenors (cpu)) {
					govs += ", " + g;
				}
				Logging.GetLogging ().Info ("\t\t" + govs);
				var gov = GetCPUGovenor (cpu);
				Logging.GetLogging ().Info ("\tCurrent Govenor: " + gov);
				savedGovenor [i] = gov;
			}
		}

		public void ConfigurePerformanceMode () {
			// TODO:  stop mpdecision on snpadragon SoCs
			for (int i = 0; i < cpus.Length; i++) {
				string cpu = cpus [i];
				string[] availFreqs = AvailableCPUFreuquencies (cpu);
				Debug.Assert (Array.IndexOf (AvailableCPUGovenors (cpu), "userspace") > -1);
				SetCPUGovenor (cpu, "userspace");
				SetCPUFrequency (cpu, availFreqs [availFreqs.Length - 1]);
			}
			PrintCurrentCPUState ();
		}

		public void RestoreCPUStates ()
		{
			for (int i = 0; i < cpus.Length; i++) {
				string cpu = cpus [i];
				SetCPUFrequency (cpu, savedFrequency [i]);
				SetCPUGovenor (cpu, savedGovenor [i]);
			}
			PrintCurrentCPUState ();
		}

		private void PrintCurrentCPUState ()
		{
			for (int i = 0; i < cpus.Length; i++) {
				string cpu = cpus [i];
				var gov = GetCPUGovenor (cpu);
				Logging.GetLogging ().Info ("\tCurrent Govenor(" + cpu + "): " + gov);
				var freq = GetCPUFrequency (cpu);
				Logging.GetLogging ().Info ("\tCurrent Frequency(" + cpu + "): " + freq);
			}
		}

		private static string SYS_CPU_PATH = "/sys/devices/system/cpu/";
		private static char[] DELIMITERS = { '\n', ' ' };

		public static Boolean IsRooted ()
		{
			try {
				Java.Lang.Process su = Java.Lang.Runtime.GetRuntime ().Exec ("su");
				Java.IO.DataOutputStream outSu = new Java.IO.DataOutputStream (su.OutputStream);
				outSu.WriteBytes ("exit\n");
				outSu.Flush ();
				su.WaitFor ();
				return su.ExitValue () == 0;
			} catch (Java.Lang.Exception) {
				return false;
			}
		}

		private static string[] AvailableCPUs ()
		{
			var ph = new ProcessHelper ();
			ph.Command ("cd " + SYS_CPU_PATH);
			ph.Command ("for i in cpu*[0-9] ; do echo $i; done");
			return ph.Output ().Trim (DELIMITERS).Split (DELIMITERS);
		}

		private static string[] AvailableCPUFreuquencies (string cpu)
		{
			var ph = new ProcessHelper ();
			ph.Command ("cd " + SYS_CPU_PATH + cpu + "/cpufreq");
			ph.Command ("cat scaling_available_frequencies");
			return ph.Output ().Trim (DELIMITERS).Split (DELIMITERS);
		}

		private static string GetCPUFrequency (string cpu)
		{
			var ph = new ProcessHelper ();
			ph.Command ("cd " + SYS_CPU_PATH + cpu + "/cpufreq");
			ph.Command ("cat scaling_cur_freq");
			return ph.Output ().Trim (DELIMITERS);
		}

		private static void SetCPUFrequency (string cpu, string frequency)
		{
			var ph = new ProcessHelper ();
			ph.Command ("cd " + SYS_CPU_PATH + cpu + "/cpufreq");
			ph.Command ("echo " + frequency + " > scaling_setspeed");
			ph.FinishSession ();
		}

		private static string[] AvailableCPUGovenors (string cpu)
		{
			var ph = new ProcessHelper ();
			ph.Command ("cd " + SYS_CPU_PATH + cpu + "/cpufreq");
			ph.Command ("cat scaling_available_governors");
			return ph.Output ().Trim (DELIMITERS).Split (DELIMITERS);
		}

		private static string GetCPUGovenor (string cpu)
		{
			var ph = new ProcessHelper ();
			ph.Command ("cd " + SYS_CPU_PATH + cpu + "/cpufreq");
			ph.Command ("cat scaling_governor");
			return ph.Output ().Trim (DELIMITERS);
		}

		private static void SetCPUGovenor (string cpu, string govenor)
		{
			var ph = new ProcessHelper ();
			ph.Command ("cd " + SYS_CPU_PATH + cpu + "/cpufreq");
			ph.Command ("echo " + govenor + " > scaling_governor");
			ph.FinishSession ();
		}
	}

	public class ProcessHelper
	{
		Java.IO.DataOutputStream outP;
		Java.IO.DataInputStream inP;
		Java.Lang.Process process;

		public ProcessHelper ()
		{
			process = Java.Lang.Runtime.GetRuntime ().Exec ("su");
			outP = new Java.IO.DataOutputStream (process.OutputStream);
			inP = new Java.IO.DataInputStream (process.InputStream);
		}

		public void Command (string cmd)
		{
			outP.WriteBytes (cmd + "\n");
			outP.Flush ();
		}

		public void FinishSession ()
		{
			Command ("exit");
			process.WaitFor ();
		}

		public string Output ()
		{
			FinishSession ();
			return ReadStreamOutput (inP);
		}

		private static string ReadStreamOutput (InputStream inputStream)
		{
			ByteArrayOutputStream baos = new ByteArrayOutputStream ();
			byte[] buffer = new byte[1024];
			int length = 0;
			while ((length = inputStream.Read (buffer)) != -1) {
				baos.Write (buffer, 0, length);
			}
			return baos.ToString ("UTF-8");
		}
	}
}

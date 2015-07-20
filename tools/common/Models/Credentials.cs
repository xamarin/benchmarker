using System;
using System.IO;
using Newtonsoft.Json;

namespace Benchmarker.Common.Models
{
	public class Credentials
	{
		public string Username { get; set; }
		public string Password { get; set; }

		public Credentials ()
		{
		}

		public static void WriteToFile (Credentials credentials, string filename)
		{
			try {
				using (FileStream fileStream = File.Open (filename, FileMode.Create, FileAccess.Write)) {
					using (var streamWriter = new StreamWriter (fileStream)) {
						streamWriter.Write (JsonConvert.SerializeObject (credentials));
					}
				}
			} catch (Exception) {
				Console.WriteLine ("Failed to save credentials to disk");
			}
		}

		public static Credentials LoadFromFile (string filename)
		{
			try {
				using (var reader = new StreamReader (new FileStream (filename, FileMode.Open))) {
					return Credentials.LoadFromString (reader.ReadToEnd ());
				}
			} catch (Exception) {
				return null;
			}
		}

		public static Credentials LoadFromString (string str)
		{
			try {
				return JsonConvert.DeserializeObject<Credentials> (str);
			} catch (Exception) {
				return null;
			}
		}
	}
}

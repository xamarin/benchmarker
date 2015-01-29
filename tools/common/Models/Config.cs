using System;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Benchmarker.Common.Models
{
	public class Config
	{
		public string Name { get; set; }
		public int Count { get; set; }
		public bool NoMono {get; set; }
		public string Mono { get; set; }
		public string[] MonoOptions { get; set; }
		public Dictionary<string, string> MonoEnvironmentVariables { get; set; }
		public string ResultsDirectory { get; set; }

		public Config ()
		{
		}

		public static Config LoadFrom (string filename)
		{
			using (var reader = new StreamReader (new FileStream (filename, FileMode.Open))) {
				var config = JsonConvert.DeserializeObject<Config> (reader.ReadToEnd ());

				if (String.IsNullOrEmpty (config.Name))
					throw new InvalidDataException ("Name");

				if (config.NoMono) {
					Debug.Assert (config.MonoOptions == null || config.MonoOptions.Length == 0);
					Debug.Assert (config.MonoEnvironmentVariables == null || config.MonoEnvironmentVariables.Count == 0);
				}

				if (String.IsNullOrEmpty (config.Mono))
					config.Mono = String.Empty;

				if (config.Count < 1)
					config.Count = 10;

				if (config.MonoOptions == null)
					config.MonoOptions = new string[0];

				if (config.MonoEnvironmentVariables == null)
					config.MonoEnvironmentVariables = new Dictionary<string, string> ();

				if (String.IsNullOrEmpty (config.ResultsDirectory))
					config.ResultsDirectory = "results";

				return config;
			}
		}

		public override bool Equals (object other)
		{
			if (other == null)
				return false;

			var config = other as Config;
			if (config == null)
				return false;

			return Name.Equals (config.Name);
		}

		public override int GetHashCode ()
		{
			return Name.GetHashCode ();
		}
	}
}

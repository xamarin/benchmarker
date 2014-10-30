using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Benchmarker.Common
{
	public class Config
	{
		public string Name { get; set; }
		public int Count { get; set; }
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

				if (String.IsNullOrEmpty (config.Mono))
					config.Mono = String.Empty;

				if (config.Count < 1)
					config.Count = 3;

				if (config.MonoOptions == null)
					config.MonoOptions = new string[0];

				if (config.MonoEnvironmentVariables == null)
					config.MonoEnvironmentVariables = new Dictionary<string, string> ();

				if (String.IsNullOrEmpty (config.ResultsDirectory))
					config.ResultsDirectory = "results";

				return config;
			}
		}
	}
}

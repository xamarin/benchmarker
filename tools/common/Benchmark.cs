using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

namespace Benchmarker.Common
{
	public class Benchmark
	{
		public string Name { get; set; }
		public string TestDirectory { get; set; }
		public string[] CommandLine { get; set; }

		public Benchmark ()
		{
		}

		public static Benchmark LoadFrom (string filename)
		{
			using (var reader = new StreamReader (new FileStream (filename, FileMode.Open))) {
				var benchmark = JsonConvert.DeserializeObject<Benchmark> (reader.ReadToEnd ());

				if (String.IsNullOrEmpty (benchmark.TestDirectory))
					throw new InvalidDataException ("TestDirectory");
				if (benchmark.CommandLine == null || benchmark.CommandLine.Length == 0)
					throw new InvalidDataException ("CommandLine");

				return benchmark;
			}
		}

		public static List<Benchmark> LoadAllFrom (string directory)
		{
			return LoadAllFrom (directory, new string[0]);
		}

		public static List<Benchmark> LoadAllFrom (string directory, string[] names)
		{
			return Directory.EnumerateFiles (directory)
				.Where (f => f.EndsWith (".benchmark"))
				.Where (f => names.Length == 0 ? true : names.Any (n => Path.GetFileNameWithoutExtension (f) == n))
				.Select (f => Benchmark.LoadFrom (f))
				.ToList ();
		}

		public Run Run (Config config, string testsdir = "tests", int timeout = Int32.MaxValue, string monoexe = null, bool pausetime = false)
		{
			Console.Out.WriteLine ("Running benchmark \"{0}\" with config \"{1}\"", Name, config.Name);

			var timedout = false;

			var info = new ProcessStartInfo () {
				FileName = !String.IsNullOrEmpty (monoexe) ? monoexe : !String.IsNullOrEmpty (config.Mono) ? config.Mono : "mono",
				WorkingDirectory = Path.Combine (testsdir, TestDirectory),
				UseShellExecute = false,
				RedirectStandardOutput = true,
				// RedirectStandardError = true,
			};

			foreach (var env in config.MonoEnvironmentVariables)
				info.EnvironmentVariables.Add (env.Key, env.Value);

			var envvar = String.Join (" ", config.MonoEnvironmentVariables.Select (kv => kv.Key + "=" + kv.Value));
			var arguments = String.Join (" ", config.MonoOptions.Union (CommandLine));

			/* Run without timing with --version */
			info.Arguments = "--version " + arguments;

			Console.Out.WriteLine ("\t$> {0} {1} {2}", envvar, info.FileName, info.Arguments);

			var process1 = Process.Start (info);
			var version = new StreamReader (process1.StandardOutput.BaseStream).ReadToEnd ();

			process1.WaitForExit ();

			/* Run without timing with --stats */
			info.Arguments = "--stats " + arguments;

			Console.Out.WriteLine ("\t$> {0} {1} {2}", envvar, info.FileName, info.Arguments);

			var process2 = Process.Start (info);
			var stdout = Task.Run (() => new StreamReader (process2.StandardOutput.BaseStream).ReadToEnd ());

			var success2 = process2.WaitForExit (timeout);
			if (!success2)
				process2.Kill ();


			/* Run with timing without --stats */
			info.Arguments = arguments;

			var sw = new Stopwatch ();
			var times = new TimeSpan [config.Count];

			for (var i = 0; i < times.Length; ++i) {
				Console.Out.Write ("\t$> {0} {1} {2} -> ({3}/{4}) ", envvar, info.FileName, info.Arguments, i + 1, times.Length);

				sw.Restart ();

				var process3 = Process.Start (info);
				var success3 = process3.WaitForExit (timeout);

				sw.Stop ();

				if (!success3)
					process3.Kill ();

				Console.Out.WriteLine (success3 ? sw.ElapsedMilliseconds.ToString () + "ms" : "timeout!");

				times [i] = new TimeSpan (success3 ? sw.ElapsedTicks : -1);
				timedout = timedout || !success3;
			}

			// FIXME: implement pausetime
			if (pausetime)
				throw new NotImplementedException ();

			return new Run () {
				DateTime = DateTime.Now,
				Benchmark = this,
				Config = config,
				Stdout = success2 ? stdout.Result : null,
				Version = version,
				Timedout = timedout,
				Times = times,
			};
		}
	}
}


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
				RedirectStandardError = true,
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

			/* Run with timing */
			info.Arguments = "--stats " + arguments;

			var sw = new Stopwatch ();
			var times = new Run.Time [config.Count];

			for (var i = 0; i < times.Length + 1; ++i) {
				Console.Out.WriteLine ("\t$> {0} {1} {2}", envvar, info.FileName, info.Arguments);
				Console.Out.Write ("\t\t-> ({0}/{1}) ...", i + 1, times.Length);

				sw.Restart ();

				var process2 = Process.Start (info);
				var stdout2 = Task.Factory.StartNew (() => new StreamReader (process2.StandardOutput.BaseStream).ReadToEnd (), TaskCreationOptions.LongRunning);
				var stderr2 = Task.Factory.StartNew (() => new StreamReader (process2.StandardError.BaseStream).ReadToEnd (), TaskCreationOptions.LongRunning);
				var success2 = process2.WaitForExit (timeout);

				sw.Stop ();

				if (!success2)
					process2.Kill ();

				Console.Out.WriteLine ("\r\t\t-> ({0}/{1}) {2}", i + 1, times.Length, success2 ? sw.ElapsedMilliseconds.ToString () + "ms" : "timeout!");

				if (i > 0) {
					times [i] = success2 ? new Run.Time { Value = TimeSpan.FromTicks (sw.ElapsedTicks), Output = stdout2.Result, Error = stderr2.Result } :
								new Run.Time { Value = TimeSpan.Zero, Output = null, Error = null };

					timedout = timedout || !success2;
				}
			}

			// FIXME: implement pausetime
			if (pausetime)
				throw new NotImplementedException ();

			return new Run () {
				DateTime = DateTime.Now,
				Benchmark = this,
				Config = config,
				Version = version,
				Timedout = timedout,
				Times = times,
			};
		}
	}
}


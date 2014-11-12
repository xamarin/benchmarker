using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

namespace Benchmarker.Common.Models
{
	public class Benchmark
	{
		public string Name { get; set; }
		public string TestDirectory { get; set; }
		public string[] CommandLine { get; set; }
		public int Timeout { get; set; }

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

		public Result Run (Config config, string testsdir = "tests", int timeout = Int32.MaxValue, string monoexe = null, bool pausetime = false)
		{
			Console.Out.WriteLine ("Running benchmark \"{0}\" with config \"{1}\"", Name, config.Name);

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

			var result = new Result () { DateTime = DateTime.Now, Benchmark = this, Config = config, Version = version, Timedout = false, Runs = new Result.Run [config.Count] };

			for (var i = 0; i < config.Count + 1; ++i) {
				var r = RunProcess (info, i == 0 ? "[dry run]" : String.Format ("({0}/{1})", i, config.Count), envvar, timeout);

				// skip first one
				if (i == 0)
					continue;

				result.Runs [i - 1] = new Result.Run { WallClockTime = r.Time, Output = r.Output, Error = r.Error };
				result.Timedout = result.Timedout || !r.Success;
			}

			// FIXME: implement pausetime
			if (pausetime)
				throw new NotImplementedException ();

			return result;
		}

		public ProfileResult Profile (Config config, Revision revision, string revisionfolder, string profilefolder, string testsdir = "tests", int timeout = Int32.MaxValue)
		{
			Console.Out.WriteLine ("Profiling benchmark \"{0}\" with config \"{1}\"", Name, config.Name);

			var timedout = false;

			var info = new ProcessStartInfo {
				FileName = Path.Combine (revisionfolder, "mono"),
				WorkingDirectory = Path.Combine (testsdir, TestDirectory),
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
			};

			foreach (var env in config.MonoEnvironmentVariables) {
				if (env.Key == "MONO_PATH" || env.Key == "LD_LIBRARY_PATH")
					continue;

				info.EnvironmentVariables.Add (env.Key, env.Value);
			}

			info.EnvironmentVariables.Add ("MONO_PATH", revisionfolder);
			info.EnvironmentVariables.Add ("LD_LIBRARY_PATH", revisionfolder);

			var envvar = String.Join (" ", config.MonoEnvironmentVariables.Union (new KeyValuePair<string, string>[] { new KeyValuePair<string, string> ("MONO_PATH", revisionfolder), new KeyValuePair<string, string> ("LD_LIBRARY_PATH", revisionfolder) })
				.Select (kv => kv.Key + "=" + kv.Value));

			var arguments = String.Join (" ", config.MonoOptions.Union (CommandLine));

			var result = new ProfileResult { DateTime = DateTime.Now, Benchmark = this, Config = config, Revision = revision, Timedout = timedout, Runs = new ProfileResult.Run [config.Count] };

			for (var i = 0; i < config.Count; ++i) {
				var profilefilename = String.Join ("_", new string [] { result.ToString (), i.ToString () }) + ".mlpd";

				info.Arguments = String.Format ("--profile=log:counters,nocalls,noalloc,output={0} ", Path.Combine (
					profilefolder, profilefilename)) + arguments;

				var r = RunProcess (info, String.Format ("({0}/{1})", i + 1, config.Count), envvar, timeout);

				result.Runs [i] = new Models.ProfileResult.Run { Index = i, WallClockTime = r.Time, Output = r.Output, Error = r.Error, ProfilerOutput = profilefilename };
				result.Timedout = result.Timedout || !r.Success;
			}

			return result;
		}

		struct RunProcessResult
		{
			public bool Success;
			public string Output;
			public string Error;
			public TimeSpan Time;

		}

		RunProcessResult RunProcess (ProcessStartInfo info, string step, string envvar, int timeout)
		{
			Console.Out.WriteLine ("\t$> {0} {1} {2}", envvar, info.FileName, info.Arguments);
			Console.Out.Write ("\t\t-> {0} ", step);

			timeout = Timeout > 0 ? Timeout : timeout;

			var sw = Stopwatch.StartNew ();

			var process = Process.Start (info);
			var stdout = Task.Factory.StartNew (() => new StreamReader (process.StandardOutput.BaseStream).ReadToEnd (), TaskCreationOptions.LongRunning);
			var stderr = Task.Factory.StartNew (() => new StreamReader (process.StandardError.BaseStream).ReadToEnd (), TaskCreationOptions.LongRunning);
			var success = process.WaitForExit (timeout < 0 ? -1 : (Math.Min (Int32.MaxValue / 1000, timeout) * 1000));

			sw.Stop ();

			if (!success)
				process.Kill ();

			Console.Out.WriteLine (success ? sw.ElapsedMilliseconds.ToString () + "ms" : "timeout!");

			return new RunProcessResult {
				Success = success,
				Time = success ? TimeSpan.FromTicks (sw.ElapsedTicks) : TimeSpan.Zero,
				Output = success ? stdout.Result : null,
				Error = success ? stderr.Result : null
			};
		}
	}
}


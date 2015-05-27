using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections;
using System.Threading.Tasks;
using Benchmarker.Common.Models;

namespace Benchmarker.Common
{
	public class Runner
	{
		ProcessStartInfo info;
		Config config;
		Benchmark benchmark;
		string arguments;
		int defaultTimeout;

		public Runner (string monoExecutable, string testsDirectory, Config _config, Benchmark _benchmark, int _timeout)
		{
			config = _config;
			benchmark = _benchmark;
			defaultTimeout = _timeout;

			info = new ProcessStartInfo {
				WorkingDirectory = Path.Combine (testsDirectory, benchmark.TestDirectory),
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
			};

			var commandLine = benchmark.CommandLine;

			if (config.NoMono) {
				info.FileName = Path.Combine (info.WorkingDirectory, commandLine [0]);
				commandLine = commandLine.Skip (1).ToArray ();
			} else {
				info.FileName = !String.IsNullOrEmpty (monoExecutable) ? monoExecutable : !String.IsNullOrEmpty (config.Mono) ? config.Mono : null;
			}

			if (info.FileName == null) {
				Console.Error.WriteLine ("Error: No mono executable specified.");
				Environment.Exit (1);
			}

			foreach (var env in config.MonoEnvironmentVariables)
				info.EnvironmentVariables.Add (env.Key, env.Value);

			arguments = String.Join (" ", config.MonoOptions.Concat (commandLine));
		}

		string PrintableEnvironmentVariables ()
		{
			var builder = new StringBuilder ();
			foreach (DictionaryEntry entry in info.EnvironmentVariables) {
				builder.Append (entry.Key.ToString ());
				builder.Append ("=");
				builder.Append (entry.Value.ToString ());
				builder.Append (" ");
			}
			return builder.ToString ();
		}

		public string GetEnvironmentVariable (string key)
		{
			var value = info.EnvironmentVariables [key];
			if (value == null)
				return String.Empty;
			return value;
		}

		public void SetEnvironmentVariable (string key, string value)
		{
			info.EnvironmentVariables.Add (key, value);
		}

		public string GetVersion ()
		{
			if (config.NoMono)
				return String.Empty;
			
			/* Run without timing with --version */
			info.Arguments = "--version " + arguments;

			Console.Out.WriteLine ("\t$> {0} {1} {2}", PrintableEnvironmentVariables (), info.FileName, info.Arguments);

			var process = Process.Start (info);
			var version = Task.Run (() => new StreamReader (process.StandardOutput.BaseStream).ReadToEnd ()).Result;
			var versionerror = Task.Run (() => new StreamReader (process.StandardError.BaseStream).ReadToEnd ());

			process.WaitForExit ();
			process.Close ();

			Console.WriteLine ("version is " + version);

			return version;
		}

		Result.Run Run (string profilesDirectory, string profileFilename)
		{
			try {
				if (profilesDirectory != null)
					Debug.Assert (profileFilename != null);
				else
					Debug.Assert (profileFilename == null);
				
				Console.Out.WriteLine ("\t$> {0} {1} {2}", PrintableEnvironmentVariables (), info.FileName, info.Arguments);

				/* Run with timing */
				if (config.NoMono)
					info.Arguments = arguments;
				else
					info.Arguments = "--stats " + arguments;

				if (profilesDirectory != null) {
					info.Arguments = String.Format ("--profile=log:counters,countersonly,nocalls,noalloc,output={0} ", Path.Combine (
						profilesDirectory, profileFilename)) + info.Arguments;
				}
				
				var timeout = benchmark.Timeout > 0 ? benchmark.Timeout : defaultTimeout;

				var sw = Stopwatch.StartNew ();

				using (var process = Process.Start (info)) {
					var stdout = Task.Factory.StartNew (() => new StreamReader (process.StandardOutput.BaseStream).ReadToEnd (), TaskCreationOptions.LongRunning);
					var stderr = Task.Factory.StartNew (() => new StreamReader (process.StandardError.BaseStream).ReadToEnd (), TaskCreationOptions.LongRunning);
					var success = process.WaitForExit (timeout < 0 ? -1 : (Math.Min (Int32.MaxValue / 1000, timeout) * 1000));

					sw.Stop ();

					if (success && process.ExitCode != 0)
						success = false;

					Console.Out.WriteLine (success ? sw.ElapsedMilliseconds.ToString () + "ms" : "failure!");

					if (!success) {
						process.Kill ();
						return null;
					}

					return new Result.Run {
						WallClockTime = TimeSpan.FromMilliseconds (sw.ElapsedMilliseconds),
						Output = stdout.Result,
						Error = stderr.Result,
					};
				}
			} catch (Exception) {
				return null;
			}
		}

		public Result.Run Run ()
		{
			return Run (null, null);
		}

		public ProfileResult.Run ProfilerRun (string profilesDirectory, string profileFilename)
		{
			var run = Run (profilesDirectory, profileFilename);
			if (run == null)
				return null;

			return new ProfileResult.Run {
				WallClockTime = run.WallClockTime,
				ProfilerOutput = profileFilename
			};
		}
	}
}

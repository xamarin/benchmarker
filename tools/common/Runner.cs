using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
		Machine machine;
		string arguments;
		int defaultTimeoutSeconds;

		public Runner (string testsDirectory, Config _config, Benchmark _benchmark, Machine _machine, int _timeoutSeconds)
		{
			config = _config;
			benchmark = _benchmark;
			machine = _machine;
			defaultTimeoutSeconds = _timeoutSeconds;

			info = config.NewProcessStartInfo ();

			info.WorkingDirectory = Path.Combine (testsDirectory, benchmark.TestDirectory);

			var commandLine = benchmark.CommandLine;

			if (config.NoMono) {
				info.FileName = Path.Combine (info.WorkingDirectory, commandLine [0]);
				commandLine = commandLine.Skip (1).ToArray ();
			}

			arguments = String.Join (" ", config.MonoOptions.Concat (commandLine));
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

		Result.Run Run (string profilesDirectory, string profileFilename, out bool timedOut)
		{
			timedOut = false;

			try {
				if (profilesDirectory != null)
					Debug.Assert (profileFilename != null);
				else
					Debug.Assert (profileFilename == null);
				
				Console.Out.WriteLine ("\t$> {0} {1} {2}", Config.PrintableEnvironmentVariables (info), info.FileName, info.Arguments);

				/* Run with timing */
				if (config.NoMono)
					info.Arguments = arguments;
				else
					info.Arguments = "--stats " + arguments;

				if (profilesDirectory != null) {
					info.Arguments = String.Format ("--profile=log:counters,countersonly,nocalls,noalloc,output={0} ", Path.Combine (
						profilesDirectory, profileFilename)) + info.Arguments;
				}

				int timeout;
				if (machine != null) {
					if (machine.BenchmarkTimeouts != null && machine.BenchmarkTimeouts.ContainsKey (benchmark.Name))
						timeout = machine.BenchmarkTimeouts [benchmark.Name];
					else
						timeout = machine.DefaultTimeout;
				} else {
					timeout = defaultTimeoutSeconds;
				}

				var sw = Stopwatch.StartNew ();

				using (var process = Process.Start (info)) {
					var stdout = Task.Factory.StartNew (() => new StreamReader (process.StandardOutput.BaseStream).ReadToEnd (), TaskCreationOptions.LongRunning);
					var stderr = Task.Factory.StartNew (() => new StreamReader (process.StandardError.BaseStream).ReadToEnd (), TaskCreationOptions.LongRunning);
					var success = process.WaitForExit (timeout < 0 ? -1 : (Math.Min (Int32.MaxValue / 1000, timeout) * 1000));

					sw.Stop ();

					if (!success) {
						Console.Out.WriteLine ("timed out!");
						timedOut = true;
					} else if (process.ExitCode != 0) {
						Console.Out.WriteLine ("failure!");
						success = false;
					} else {
						Console.Out.WriteLine (success ? sw.ElapsedMilliseconds.ToString () + "ms" : "failure!");
					}

					if (!success) {
						try {
							process.Kill ();
						} catch (InvalidOperationException) {
							// The process might have finished already, so we need to catch this.
						}
					}

					Console.Out.WriteLine ("stdout:\n{0}", stdout.Result);
					Console.Out.WriteLine ("stderr:\n{0}", stderr.Result);

					if (!success) {
						return null;
					}

					return new Result.Run {
						WallClockTime = TimeSpan.FromMilliseconds (sw.ElapsedMilliseconds),
						Output = stdout.Result,
						Error = stderr.Result,
					};
				}
			} catch (Exception exc) {
				Console.Out.WriteLine ("Exception: {0}", exc);
				return null;
			}
		}

		public Result.Run Run (out bool timedOut)
		{
			return Run (null, null, out timedOut);
		}
	}
}

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Benchmarker.Models;

namespace compare
{
	public class UnixRunner
	{
		public ProcessStartInfo Info { get; }
		public ProcessStartInfo ClientInfo { get; }
		public ProcessStartInfo InfoAot { get ; }

		Config config;
		Benchmark benchmark;
		Machine machine;
		string arguments;
		int defaultTimeoutSeconds;
		string fileName;
		string runTool;
		string runToolArguments;
		bool clientServer;

		public UnixRunner (string testsDirectory, Config _config, Benchmark _benchmark, Machine _machine, int _timeoutSeconds, string _runTool, string _runToolArguments)
		{
			config = _config;
			benchmark = _benchmark;
			machine = _machine;
			defaultTimeoutSeconds = _timeoutSeconds;
			runTool = _runTool;
			runToolArguments = _runToolArguments;

			var binaryProtocolFile = _config.ProducesBinaryProtocol ? "binprot.dummy" : null;
			Info = compare.Utils.NewProcessStartInfo (_config, binaryProtocolFile);

			Info.WorkingDirectory = Path.Combine (testsDirectory, benchmark.TestDirectory);

			if (config.AOTOptions != null) {
				if (benchmark.AOTAssemblies == null) {
					Console.Error.WriteLine("Error: benchmark {0} not configured to be executed in AOT mode.", benchmark.Name);
					Environment.Exit(1);
				}
				InfoAot = compare.Utils.NewProcessStartInfo (_config, binaryProtocolFile);
				InfoAot.WorkingDirectory = Path.Combine (testsDirectory, benchmark.TestDirectory);
				InfoAot.Arguments = String.Join (" ", config.AOTOptions.Concat (benchmark.AOTAssemblies));
			}

			var commandLine = benchmark.CommandLine;

			if (benchmark.ClientCommandLine != null) {
				clientServer = true;
				ClientInfo = compare.Utils.NewProcessStartInfo (_config, binaryProtocolFile);
				ClientInfo.WorkingDirectory = Info.WorkingDirectory;
				ClientInfo.Arguments = String.Join (" ", config.MonoOptions.Concat (benchmark.ClientCommandLine));
			} else {
				clientServer = false;
			}

			if (config.NoMono) {
				Info.FileName = Path.Combine (Info.WorkingDirectory, commandLine [0]);
				commandLine = commandLine.Skip (1).ToArray ();
			}

			fileName = Info.FileName;

			arguments = String.Join (" ", config.MonoOptions.Concat (commandLine));
			/* Run with timing */
			if (!config.NoMono)
				arguments = "--stats " + arguments;
		}

		public bool IsAot {
			get {
				return InfoAot != null;
			}
		}

		private int GetTimeout() {
			int timeout;
			if (machine != null) {
				if (machine.BenchmarkTimeouts != null && machine.BenchmarkTimeouts.ContainsKey(benchmark.Name))
					timeout = machine.BenchmarkTimeouts[benchmark.Name];
				else
					timeout = machine.DefaultTimeout;
			} else {
				timeout = defaultTimeoutSeconds;
			}
			return timeout;
		}

		private static void PrintCommandLine(String prefix, ProcessStartInfo info) {
			Console.WriteLine("$> {0}: \"{1} {2}\" in \"{3}\" with {4}", prefix, info.FileName, info.Arguments, info.WorkingDirectory, compare.Utils.PrintableEnvironmentVariables(info));
		}

		public long? RunAOT(out bool timedOut, out string stdoutOutput) {
			if (InfoAot == null)
				throw new ArgumentException ("config/benchmark isn't configured to be run in AOT mode");

			timedOut = false;
			stdoutOutput = null;

			int timeout = GetTimeout ();

			PrintCommandLine ("AOT command", InfoAot);
			var sw = Stopwatch.StartNew ();
			using (var aotProcess = Process.Start(InfoAot)) {
				var stdout = Task.Factory.StartNew (() => new StreamReader (aotProcess.StandardOutput.BaseStream).ReadToEnd (), TaskCreationOptions.LongRunning);
				var stderr = Task.Factory.StartNew (() => new StreamReader (aotProcess.StandardError.BaseStream).ReadToEnd (), TaskCreationOptions.LongRunning);
				var success = aotProcess.WaitForExit(timeout < 0 ? -1 : (Math.Min (Int32.MaxValue / 1000, timeout) * 1000));

				if (success) {
					if (aotProcess.ExitCode != 0) {
						Console.Out.WriteLine ("AOT failure!");
						success = false;
					}
				} else {
					Console.Out.WriteLine ("AOT timed out!");
					timedOut = true;
				}

				if (!success) {
					try {
						aotProcess.Kill ();
					} catch (InvalidOperationException) {
						// The process might have finished already, so we need to catch this.
					}
				}

				Console.Out.WriteLine ("aot-stdout:\n{0}", stdout.Result);
				stdoutOutput = stdout.Result;
				Console.Out.WriteLine ("aot-stderr:\n{0}", stderr.Result);

				if (success)
					return sw.ElapsedMilliseconds;
				else
					return null;
			}
		}

		public long? Run (string profilesDirectory, string profileFilename, string binaryProtocolFilename, out bool timedOut, out string stdoutOutput)
		{
			Utils.SetProcessStartEnvironmentVariables (Info, config, binaryProtocolFilename);

			timedOut = false;
			stdoutOutput = null;

			try {
				if (profilesDirectory != null) {
					if (profileFilename == null) {
						throw new Exception ("must have profile filename");
					}
				} else {
					if (profileFilename != null) {
						throw new Exception ("must have no profile filename");
					}
				}
				if (profilesDirectory == null) {
					Info.Arguments = arguments;
				} else {
					Info.Arguments = String.Format ("--profile=log:counters,countersonly,nocalls,noalloc,output={0} ", Path.Combine (
						profilesDirectory, profileFilename)) + arguments;
				}

				if (runTool == null) {
					Info.FileName = fileName;
				} else {
					Info.FileName = runTool;
					Info.Arguments = runToolArguments + " " + fileName + " " + Info.Arguments;
				}

				if (clientServer) {
					PrintCommandLine ("Server command", Info);
					PrintCommandLine ("Client command", ClientInfo);
				} else {
					PrintCommandLine ("Benchmark command", Info);
				}

				int timeout = GetTimeout ();
				using (var serverProcess = clientServer ? Process.Start (Info) : null) {

					if (clientServer)
						System.Threading.Thread.Sleep (5000);

					var sw = Stopwatch.StartNew ();
					using (var mainProcess = clientServer ? Process.Start (ClientInfo) : Process.Start (Info)) {
						var stdout = Task.Factory.StartNew (() => new StreamReader (mainProcess.StandardOutput.BaseStream).ReadToEnd (), TaskCreationOptions.LongRunning);
						var stderr = Task.Factory.StartNew (() => new StreamReader (mainProcess.StandardError.BaseStream).ReadToEnd (), TaskCreationOptions.LongRunning);

						var success = mainProcess.WaitForExit (timeout < 0 ? -1 : (Math.Min (Int32.MaxValue / 1000, timeout) * 1000));

						sw.Stop ();

						if (success) {
							if (mainProcess.ExitCode != 0) {
								Console.Out.WriteLine ("failure!");
								success = false;
							} else {
								Console.Out.WriteLine (sw.ElapsedMilliseconds.ToString () + "ms");
							}
						} else {
							Console.Out.WriteLine ("timed out!");
							timedOut = true;
						}

						if (clientServer)
							serverProcess.Kill ();

						if (!success) {
							try {
								mainProcess.Kill ();
							} catch (InvalidOperationException) {
								// The process might have finished already, so we need to catch this.
							}
						}

						Console.Out.WriteLine ("stdout:\n{0}", stdout.Result);
						stdoutOutput = stdout.Result;
						Console.Out.WriteLine ("stderr:\n{0}", stderr.Result);

						if (success)
							return sw.ElapsedMilliseconds;
						else
							return null;
					}
				}
			} catch (Exception exc) {
				Console.Out.WriteLine ("Exception: {0}", exc);
				return null;
			}
		}

		public long? Run (string binaryProtocolFilename, out bool timedOut, out string stdoutOutput) {
			return Run (null, null, binaryProtocolFilename, out timedOut, out stdoutOutput);
		}
	}
}

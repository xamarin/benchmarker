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
		ProcessStartInfo info, clientInfo;
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

			var binaryProtocolFile = _config.ProducesBinaryProtocol ? "/tmp/binprot.dummy" : null;
			info = compare.Utils.NewProcessStartInfo (_config, binaryProtocolFile);

			info.WorkingDirectory = Path.Combine (testsDirectory, benchmark.TestDirectory);

			var commandLine = benchmark.CommandLine;

			if (benchmark.ClientCommandLine != null) {
				clientServer = true;
				clientInfo = compare.Utils.NewProcessStartInfo (_config, binaryProtocolFile);
				clientInfo.WorkingDirectory = info.WorkingDirectory;
				clientInfo.Arguments = String.Join (" ", config.MonoOptions.Concat (benchmark.ClientCommandLine));
			} else {
				clientServer = false;
			}

			if (config.NoMono) {
				info.FileName = Path.Combine (info.WorkingDirectory, commandLine [0]);
				commandLine = commandLine.Skip (1).ToArray ();
			}

			fileName = info.FileName;

			arguments = String.Join (" ", config.MonoOptions.Concat (commandLine));
			/* Run with timing */
			if (!config.NoMono)
				arguments = "--stats " + arguments;
		}

		public long? Run (string profilesDirectory, string profileFilename, string binaryProtocolFilename, out bool timedOut)
		{
			Utils.SetProcessStartEnvironmentVariables (info, config, binaryProtocolFilename);

			timedOut = false;

			try {
				if (profilesDirectory != null)
					Debug.Assert (profileFilename != null);
				else
					Debug.Assert (profileFilename == null);
				
				if (profilesDirectory == null) {
					info.Arguments = arguments;
				} else {
					info.Arguments = String.Format ("--profile=log:counters,countersonly,nocalls,noalloc,output={0} ", Path.Combine (
						profilesDirectory, profileFilename)) + arguments;
				}

				if (runTool == null) {
					info.FileName = fileName;
				} else {
					info.FileName = runTool;
					info.Arguments = runToolArguments + " " + fileName + " " + info.Arguments;
				}

				Console.Out.WriteLine ("\t$> {0} {1} {2}", compare.Utils.PrintableEnvironmentVariables (info), info.FileName, info.Arguments);

				int timeout;
				if (machine != null) {
					if (machine.BenchmarkTimeouts != null && machine.BenchmarkTimeouts.ContainsKey (benchmark.Name))
						timeout = machine.BenchmarkTimeouts [benchmark.Name];
					else
						timeout = machine.DefaultTimeout;
				} else {
					timeout = defaultTimeoutSeconds;
				}

				using (var serverProcess = clientServer ? Process.Start (info) : null) {

					if (clientServer)
						System.Threading.Thread.Sleep (5000);

					var sw = Stopwatch.StartNew ();

					using (var mainProcess = clientServer ? Process.Start (clientInfo) : Process.Start (info)) {
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
							serverProcess.Kill();

						if (!success) {
							try {
								mainProcess.Kill ();
							} catch (InvalidOperationException) {
								// The process might have finished already, so we need to catch this.
							}
						}

						Console.Out.WriteLine ("stdout:\n{0}", stdout.Result);
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

		public long? Run (string binaryProtocolFilename, out bool timedOut)
		{
			return Run (null, null, binaryProtocolFilename, out timedOut);
		}
	}
}

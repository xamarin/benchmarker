using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.Test.Performance.Utilities
{
    public static class TestUtilities
    {
        /// <summary>
        /// The result of a ShellOut completing.
        /// </summary>
        public class ProcessResult
        {
            /// <summary>
            /// The path to the executable that was run.
            /// </summary>
            public string ExecutablePath { get; set; }
            /// <summary>
            /// The arguments that were passed to the process.
            /// </summary>
            public string Args { get; set; }
            /// <summary>
            /// The exit code of the process.
            /// </summary>
            public int Code { get; set; }
            /// <summary>
            /// The entire standard-out of the process.
            /// </summary>
            public string StdOut { get; set; }
            /// <summary>
            /// The entire standard-error of the process.
            /// </summary>
            public string StdErr { get; set; }

            /// <summary>
            /// True if the command returned an exit code other
            /// than zero.
            /// </summary>
            public bool Failed => Code != 0;
            /// <summary>
            /// True if the command returned an exit code of 0.
            /// </summary>
            public bool Succeeded => !Failed;
        }

        /// <summary>
        /// Shells out, and if the process fails, log the error and quit the script.
        /// </summary>
        public static void ShellOutVital(
                string file,
                string args,
                string workingDirectory = null,
                CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = ShellOut(file, args, workingDirectory, cancellationToken);
            if (result.Failed)
            {
                LogProcessResult(result);
                throw new Exception("Shelling out failed");
            }
        }

        /// <summary>
        /// Shells out and returns the string gathered from the stdout of the 
        /// executing process.
        /// 
        /// Throws an exception if the process fails.
        /// </summary>
        public static string StdoutFrom(string program, string args = "", string workingDirectory = null)
        {
            var result = ShellOut(program, args, workingDirectory);
            if (result.Failed)
            {
                LogProcessResult(result);
                throw new Exception("Shelling out failed");
            }
            return result.StdOut.Trim();
        }

        /// <summary>
        /// Logs the result of a finished process.
        /// </summary>
        public static void LogProcessResult(ProcessResult result)
        {
            var outcome = result.Failed ? "failed" : "succeeded";
            Console.WriteLine($"The process \"{result.ExecutablePath} {result.Args}\" {outcome} with code {result.Code}.");
            Console.WriteLine($"Standard Out:");
            Console.WriteLine(result.StdOut);
            Console.WriteLine($"Standard Error:");
            Console.WriteLine(result.StdErr);
        }

        /// <summary>
        /// Shells out, blocks, and returns the ProcessResult.
        /// </summary>
        public static ProcessResult ShellOut(
                string file,
                string args,
                string workingDirectory = null,
                CancellationToken cancellationToken = default(CancellationToken))
        {
            if (workingDirectory == null)
                workingDirectory = AppDomain.CurrentDomain.BaseDirectory;

            var tcs = new TaskCompletionSource<ProcessResult>();
            var startInfo = new ProcessStartInfo(file, args);
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            startInfo.WorkingDirectory = workingDirectory;
            var process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true,
            };

            if (cancellationToken != default(CancellationToken))
                cancellationToken.Register(() => process.Kill());

            if (RuntimeSettings.IsVerbose)
                Console.WriteLine($"Running \"{file}\" with arguments \"{args}\" from directory {workingDirectory}");

            process.Start();

            var output = new StringWriter();
            var error = new StringWriter();

            process.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    output.WriteLine(e.Data);
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    error.WriteLine(e.Data);
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            return new ProcessResult
            {
                ExecutablePath = file,
                Args = args,
                Code = process.ExitCode,
                StdOut = output.ToString(),
                StdErr = error.ToString(),
            };
        }
    }
}

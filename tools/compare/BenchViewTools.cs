using System;
using System.IO;
using System.Linq;
using Benchmarker.Models;
using Newtonsoft.Json;
using static System.Environment;
using static Xamarin.Test.Performance.Utilities.TestUtilities;

namespace Xamarin.Test.Performance.Utilities
{
    internal static class BenchViewTools
    {
        static BenchViewTools()
        {
            s_outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BenchView");
            s_scriptDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Microsoft.BenchView.JSONFormat", "tools");
            s_pythonProcessName = IsWindows ? "py.exe" : "python3.5";
            s_locateCommand = IsWindows ? "where.exe" : "which";
            s_validSubmissionTypes = new string[] { "rolling", "private", "local" };

            s_submissionMetadataPy = Path.Combine(s_scriptDirectory, "submission-metadata.py");
            s_buildPy = Path.Combine(s_scriptDirectory, "build.py");
            s_machinedataPy = Path.Combine(s_scriptDirectory, "machinedata.py");
            s_measurementPy = Path.Combine(s_scriptDirectory, "measurement.py");
            s_submissionPy = Path.Combine(s_scriptDirectory, "submission.py");
            s_uploadPy = Path.Combine(s_scriptDirectory, "upload.py");

            s_submissionMetadataJson = Path.Combine(s_outputDirectory, "submission-metadata.json");
            s_buildJson = Path.Combine(s_outputDirectory, "build.json");
            s_machinedataJson = Path.Combine(s_outputDirectory, "machinedata.json");
            s_measurementJson = Path.Combine(s_outputDirectory, "measurement.json");
            s_submissionJson = Path.Combine(s_outputDirectory, "submission.json");
            s_runSetJsonFileName = Path.Combine(s_outputDirectory, "xamarin-benchmarker-runset.json");
        }


        internal static string[] ValidSubmissionTypes
        {
            get
            {
                return s_validSubmissionTypes;
            }
        }

        internal static bool IsValidSubmissionType(string submissionType)
        {
            return s_validSubmissionTypes.Any(type => type == submissionType);
        }

        internal static bool CheckEnvironment()
        {
            Console.WriteLine ("Checking for valid environment for uploading to BenchView.");

            var sasToken = GetEnvironmentVariable (s_sasEnvironmentVar);
            if (string.IsNullOrEmpty(sasToken))
            {
                Console.Error.WriteLine ($"Error: {s_sasEnvironmentVar} was not defined");
                return false;
            }

            // TODO: Is it good enough for non-Windows platforms?
            var whereGit = ShellOut(s_locateCommand, "git");
            if (whereGit.Failed)
            {
                Console.Error.WriteLine("Error: git was not found on the PATH");
                return false;
            }

            // TODO: Is it good enough for non-Windows platforms?
            var wherePy = ShellOut(s_locateCommand, "py");
            if (wherePy.Failed)
            {
                Console.Error.WriteLine("Error: py was not found on the PATH");
                return false;
            }

            if (!Directory.Exists(s_scriptDirectory))
            {
                Console.Error.WriteLine ($"Error: BenchView Tools not found at {s_scriptDirectory}");
                return false;
            }

            return true;
        }

        internal static void GatherBenchViewData(string submissionType, string submissionName, string branch)
        {
            Console.WriteLine("Gathering BenchView data...");

            // Always start fresh for a new set of results.
            if (Directory.Exists(s_outputDirectory))
                Directory.Delete(s_outputDirectory, true);
            Directory.CreateDirectory(s_outputDirectory);

            var hash = StdoutFrom("git", "rev-parse HEAD");
            if (string.IsNullOrWhiteSpace(submissionName))
            {
                if (submissionType == "rolling")
                    submissionName = $"{s_group} {submissionType} {branch} {hash}";
                else
                    throw new Exception($"submissionName was not provided, but submission type is {submissionType}");
            }

            ShellOutVital(s_pythonProcessName, $"\"{s_submissionMetadataPy}\" --name=\"{submissionName}\" --user-email={s_userEmail} -o=\"{s_submissionMetadataJson}\"");
            ShellOutVital(s_pythonProcessName, $"\"{s_buildPy}\" git --type={submissionType} --branch=\"{branch}\" -o=\"{s_buildJson}\"");
            ShellOutVital(s_pythonProcessName, $"\"{s_machinedataPy}\" -o=\"{s_machinedataJson}\"");
        }

        internal static void CreateBenchviewReport(string submissionType, RunSet runSet)
        {
            Console.WriteLine("Creating benchview results...");

            // Serialize the xamarin/benchmarker object to a file.
            var jsonConvertedSerializedRunSet = JsonConvert.SerializeObject(runSet);
            using (var sw = new StreamWriter(s_runSetJsonFileName))
                sw.Write(jsonConvertedSerializedRunSet);

            var result = ConvertToMeasurement(s_runSetJsonFileName);
            CreateBenchViewSubmission(submissionType, runSet);
        }

        internal static void UploadBenchviewData()
        {
            Console.WriteLine("Uploading json to Azure blob storage...");
            ShellOutVital(s_pythonProcessName, $"\"{s_uploadPy}\" \"{s_submissionJson}\" --container xamarin");
            Console.WriteLine("Done uploading.");
        }

        private static bool ConvertToMeasurement(string runSetJsonFileName)
        {
            Console.WriteLine("Converting RunSet format to BenchView measurement json");

            if (!File.Exists(runSetJsonFileName))
            {
                Console.WriteLine($"File \"{runSetJsonFileName}\" does not exist.");
                return false;
            }

            ShellOutVital(s_pythonProcessName, $"\"{s_measurementPy}\" xamarin_benchmarker \"{runSetJsonFileName}\" -o=\"{s_measurementJson}\"");
            return true;
        }

        private static void CreateBenchViewSubmission(string submissionType, RunSet runSet)
        {
            Console.WriteLine("Creating BenchView submission json");

            // FIXME: What is the machine pool?
            var machinePoolName = "Mono TBD";
            var arguments = new string[]
            {
                $"\"{s_submissionPy}\"",
                $"\"{s_measurementJson}\"",
                $"--metadata=\"{s_submissionMetadataJson}\"",
                $"--build=\"{s_buildJson}\"",
                $"--machine-data=\"{s_machinedataJson}\"",
                $"--group=\"{s_group}\"",
                $"--type=\"{submissionType}\"",
                $"--config-name=\"{runSet.Config.Name}\"",
                $"--config MonoOptions \\\"{string.Join(" ", runSet.Config.MonoOptions)}\\\"",
                $"--architecture=\"{runSet.Machine.Architecture}\"",
                $"--machinepool=\"{machinePoolName}\"",
                $"-o=\"{s_submissionJson}\""
            };

            ShellOutVital(s_pythonProcessName, string.Join(" ", arguments));
        }

        private static bool IsWindows
        {
			get {
				return OSVersion.Platform == PlatformID.Win32NT;
            }
        }

        private const string s_group = "Xamarin";
        private const string s_sasEnvironmentVar = "BV_UPLOAD_SAS_TOKEN";
        private const string s_userEmail = "mono-pbot@microsoft.com";  // TODO: Is it the right user email to be used?

        private static readonly string s_scriptDirectory;
        private static readonly string s_outputDirectory;
        private static readonly string s_pythonProcessName;
        private static readonly string s_locateCommand;
        private static readonly string[] s_validSubmissionTypes;

        private static readonly string s_submissionMetadataPy;
        private static readonly string s_buildPy;
        private static readonly string s_machinedataPy;
        private static readonly string s_measurementPy;
        private static readonly string s_submissionPy;
        private static readonly string s_uploadPy;

        private static readonly string s_submissionMetadataJson;
        private static readonly string s_buildJson;
        private static readonly string s_machinedataJson;
        private static readonly string s_measurementJson;
        private static readonly string s_submissionJson;
        private static readonly string s_runSetJsonFileName;
    }
}

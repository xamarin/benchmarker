using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;

namespace Benchmarker.Common.Git
{
	public class Repository
	{
		string path;

		static string RunGit (string path, string command, params string[] args)
		{
			var startInfo = new ProcessStartInfo ("git");
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardOutput = true;
			startInfo.WorkingDirectory = path;
			startInfo.Arguments = command + " " + String.Join (" ", args);

			using (var process = Process.Start (startInfo)) {
				var stdout = Task.Run (() => new StreamReader (process.StandardOutput.BaseStream).ReadToEnd ()).Result;

				process.WaitForExit ();

				if (process.ExitCode != 0)
					return null;

				return stdout;
			}
		}

		public Repository (string _path)
		{
			path = RunGit (_path, "rev-parse", "--show-toplevel");
			if (path == null)
				throw new Exception ("Could not find Git repository in " + _path);
			path = path.TrimEnd ();
		}

		public string RevParse (string gitObject)
		{
			var sha = RunGit (path, "rev-parse", gitObject);
			if (sha == null)
				return null;
			return sha.TrimEnd ();
		}

		public string MergeBase (params string[] gitObjects)
		{
			var sha = RunGit (path, "merge-base", gitObjects);
			if (sha == null)
				return null;
			return sha.TrimEnd ();
		}

		public DateTime? CommitDate (string gitObject)
		{
			var dateString = RunGit (path, "show", "--pretty=format:%cD", "--no-patch", gitObject);
			if (dateString == null)
				return null;
			DateTime dateTime;
			if (DateTime.TryParse (dateString.TrimEnd (), out dateTime))
				return dateTime;
			return null;
		}

		public string Fetch (string repository, string branch)
		{
			if (RunGit (path, "fetch", repository, branch) == null)
				return null;
			return RevParse ("FETCH_HEAD");
		}

		public string[] RevList (string commit)
		{
			var output = RunGit (path, "rev-list", "--all", commit);
			if (output == null)
				return null;
			return output.Trim ().Split ('\n');
		}
	}
}

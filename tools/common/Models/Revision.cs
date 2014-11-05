using System;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace Benchmarker.Common.Models
{
	public class Revision
	{
		const string Storage = "nas.bos.xamarin.com/benchmarker";

		public string Project { get; private set; }
		public string Architecture { get; private set; }
		public string Commit { get; private set; }

		public static Revision Get (string project, string architecture, string commit)
		{
			// FIXME: check if it actually exists
			return new Revision () {
				Project = project,
				Architecture = architecture,
				Commit = commit,
			};
		}

		public static Revision Last (string project, string architecture)
		{
			throw new NotImplementedException ();
		}

		public void FetchInto (string folder)
		{
			Console.Out.WriteLine ("Fetch revision {0}/{1}/{2} in {3}", Project, Architecture, Commit, folder);

			Debug.Assert (!String.IsNullOrEmpty (Commit));

			var filename = Path.Combine (folder, "revision.tar.gz");

			using (var archive = HttpClient.GetStream (Storage, String.Format ("/binaries/{0}/{1}/{2}.tar.gz", Project, Architecture, Commit)))
			using (var file = new FileStream (filename, FileMode.Create, FileAccess.Write))
				archive.CopyTo (file);

			var process = Process.Start (new ProcessStartInfo () {
				FileName = "tar",
				Arguments = String.Format ("xvzf {0}", filename),
				WorkingDirectory = folder,
				UseShellExecute = true,
			});

			process.WaitForExit ();
		}
	}
}


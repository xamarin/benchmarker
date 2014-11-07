using System;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using System.Net;
using System.Linq;

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
			if (String.IsNullOrWhiteSpace (project))
				throw new ArgumentNullException ("project");
			if (String.IsNullOrWhiteSpace (architecture))
				throw new ArgumentNullException ("architecture");

			switch (project) {
			case "mono":
				string lane;
				switch (architecture) {
				case "amd64":
					lane = "mono-mac-master-64";
					break;
				case "x86":
					lane = "mono-mac-master-32";
					break;
				default:
					throw new ArgumentException (String.Format ("Unknown architecture \"{0}\" for project \"{1}\"", architecture, project));
				}

				var wrenchrequest = HttpClient.CreateRequest ("https://wrench.mono-project.com/Wrench/GetLatest.aspx?laneName=" + lane);
				wrenchrequest.AllowAutoRedirect = false;

				var wrenchresponse = HttpClient.Get (wrenchrequest);

				switch (wrenchresponse.StatusCode) {
				case HttpStatusCode.Redirect:
					var redirecturi = new Uri (wrenchresponse.GetResponseHeader ("Location"));
					var redirectpath = redirecturi.AbsolutePath;

					if (redirecturi.Host == "wrench.mono-project.com" && redirectpath == "/Wrench/index.aspx")
						return null;

					if (redirecturi.Host != "storage.bos.internalx.com")
						throw new InvalidDataException (String.Format ("Redirect Host is not storage, \"{0}\" instead", redirecturi.Host));

					var splits = redirectpath.Substring (1).Split ('/');

					if (splits.Length != 4 || splits [0] != lane || splits [3] != "manifest")
						throw new InvalidDataException (String.Format ("Unknown path format \"{0}\"", redirectpath));

					return new Revision { Project = project, Architecture = architecture, Commit = splits [2] };
				default:
					throw new InvalidDataException (String.Format ("Unknown return status code from Wrench \"{0}\"", wrenchresponse.StatusCode));
				}
			default:
				throw new NotImplementedException ();
			}
		}

		public void FetchInto (string folder)
		{
			Console.Out.WriteLine ("Fetch revision {0}/{1}/{2} in {3}", Project, Architecture, Commit, folder);

			Debug.Assert (!String.IsNullOrEmpty (Commit));

			var filename = Path.Combine (folder, "revision.tar.gz");

			using (var archive = HttpClient.GetStream (String.Format ("http://{0}/binaries/{1}/{2}/{3}.tar.gz", Storage, Project, Architecture, Commit)))
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


using System;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using System.Net;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Benchmarker.Common.Models
{
	public class Revision : IComparable<Revision>
	{
		const string Storage = "nas.bos.xamarin.com/benchmarker";

		public string Project { get; set; }
		public string Architecture { get; set; }
		public string Commit { get; set; }

		private DateTime commitDate;
		public DateTime CommitDate {
			get { return commitDate != default (DateTime) ? commitDate : (commitDate = GetCommitDate (Project, Commit)); }
			set { commitDate = value; }
		}

		public static Revision Get (string project, string architecture, string commit)
		{
			// FIXME: check if it actually exists
			return new Revision () {
				Project = project,
				Architecture = architecture,
				Commit = commit,
				CommitDate = GetCommitDate (project, commit),
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

					return new Revision {
						Project = project,
						Architecture = architecture,
						Commit = splits [2],
						CommitDate = GetCommitDate (project, splits [2]),
					};
				default:
					throw new InvalidDataException (String.Format ("Unknown return status code from Wrench \"{0}\"", wrenchresponse.StatusCode));
				}
			default:
				throw new NotImplementedException ();
			}
		}

		public bool FetchInto (string folder)
		{
			Console.Out.WriteLine ("Fetch revision {0}/{1}/{2} in {3}", Project, Architecture, Commit, folder);

			Debug.Assert (!String.IsNullOrEmpty (Commit));

			var filename = Path.Combine (folder, "revision.tar.gz");

			try {
				using (var archive = HttpClient.GetStream (String.Format ("http://{0}/binaries/{1}/{2}/{3}.tar.gz", Storage, Project, Architecture, Commit)))
				using (var file = new FileStream (filename, FileMode.Create, FileAccess.Write))
					archive.CopyTo (file);
			} catch (WebException e) {
				Console.Out.WriteLine (e.ToString ());
				return false;
			}

			var process = Process.Start (new ProcessStartInfo () {
				FileName = "tar",
				Arguments = String.Format ("xvzf {0}", filename),
				WorkingDirectory = folder,
				UseShellExecute = true,
			});

			process.WaitForExit ();

			return true;
		}

		public override bool Equals (object other)
		{
			if (other == null)
				return false;

			var revision = other as Revision;
			if (revision == null)
				return false;

			return Project.Equals (revision.Project)
				&& Architecture.Equals (revision.Architecture)
				&& Commit.Equals (revision.Commit);
		}

		public override int GetHashCode ()
		{
			return Project.GetHashCode () ^ Architecture.GetHashCode () ^ Commit.GetHashCode ();
		}

		int IComparable<Revision>.CompareTo (Revision other)
		{
			return CommitDate.CompareTo (other.CommitDate);
		}

		static Dictionary<string, DateTime> commitDateCache = new Dictionary<string, DateTime> ();

		private static DateTime GetCommitDate (string project, string commit)
		{
			if (project == null)
				throw new ArgumentNullException ("project");
			if (commit == null)
				throw new ArgumentNullException ("commit");

			var cacheKey = project + commit;
			if (commitDateCache.ContainsKey (cacheKey))
				return commitDateCache [cacheKey];

			var repo = "";

			switch (project) {
			case "mono":
				repo = "mono/mono";
				break;
			default:
				throw new NotImplementedException ();
			}

			var json = JObject.Parse (HttpClient.GetContent ("https://api.github.com/repos/" + repo + "/commits/" + commit));

			lock (commitDateCache) {
				return commitDateCache [cacheKey] = DateTime.Parse ((string)json ["commit"] ["committer"] ["date"]);
			}
		}
	}
}


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


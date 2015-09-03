using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Parse;

namespace Benchmarker.Common.Models
{
	public class Commit
	{
		public string Hash { get; set; }
		public string Branch { get; set; }
		public string MergeBaseHash { get; set; }
		public DateTime? CommitDate { get; set; }
		
		public Commit ()
		{
		}

		public async Task<ParseObject> GetOrUploadToParse (List<ParseObject> saveList)
		{
			var results = await ParseInterface.RunWithRetry (() => ParseObject.GetQuery ("Commit").WhereEqualTo ("hash", Hash).FindAsync ());
			Logging.GetLogging ().Info ("FindAsync Commit");
			if (results.Count () > 0)
				return results.First ();

			if (CommitDate == null)
				throw new Exception ("Cannot save a commit without a commit date");

			var obj = ParseInterface.NewParseObject ("Commit");
			obj ["hash"] = Hash;
			if (Branch != null)
				obj ["branch"] = Branch;
			if (MergeBaseHash != null)
				obj ["mergeBaseHash"] = MergeBaseHash;
			if (CommitDate != null)
				obj ["commitDate"] = CommitDate;
			saveList.Add (obj);
			return obj;
		}

	}
}

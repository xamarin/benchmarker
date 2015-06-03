using System;
using System.Threading.Tasks;
using System.Linq;
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

		public async Task<ParseObject> GetOrUploadToParse ()
		{
			var results = await ParseObject.GetQuery ("Commit").WhereEqualTo ("hash", Hash).FindAsync ();
			if (results.Count () > 0)
				return results.First ();

			if (CommitDate == null)
				throw new Exception ("Cannot save a commit without a commit date");

			var obj = new ParseObject ("Commit");
			obj ["hash"] = Hash;
			if (Branch != null)
				obj ["branch"] = Branch;
			if (MergeBaseHash != null)
				obj ["mergeBaseHash"] = MergeBaseHash;
			if (CommitDate != null)
				obj ["commitDate"] = CommitDate;
			await obj.SaveAsync ();
			return obj;
		}

	}
}

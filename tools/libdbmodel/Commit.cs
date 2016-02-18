using System;
using System.Collections.Generic;
using System.Linq;

namespace Benchmarker.Models
{
	public class Commit
	{
		public string Hash { get; set; }
		public Product Product { get; set; }
		public string Branch { get; set; }
		public string MergeBaseHash { get; set; }
		public DateTime? CommitDate { get; set; }
		
		public Commit ()
		{
		}

		public IDictionary<string, object> ApiObject {
			get {
				var dict = new Dictionary<string, object> ();
				dict ["Name"] = Product.Name;
				dict ["Commit"] = Hash;
				// FIXME: add MergeBaseHash to API
				if (MergeBaseHash != null)
					dict ["MergeBaseHash"] = MergeBaseHash;
				if (CommitDate.HasValue)
					dict ["CommitDate"] = CommitDate.Value.ToString (RunSet.DATETIME_FORMAT);
				return dict;
			}
		}
	}
}

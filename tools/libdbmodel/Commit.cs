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

		public IDictionary<string, string> ApiObject {
			get {
				var dict = new Dictionary<string, string> ();
				dict ["Name"] = Product.Name;
				dict ["Commit"] = Hash;
				// FIXME: add MergeBaseHash to API
				if (MergeBaseHash != null)
					dict ["MergeBaseHash"] = MergeBaseHash;
				return dict;
			}
		}
	}
}

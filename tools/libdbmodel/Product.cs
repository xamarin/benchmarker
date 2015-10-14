using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Benchmarker.Models
{
	public class Product
	{
		public string Name { get; set; }
		public string GitHubUser { get; set; }
		public string GitHubRepo { get; set; }

		public Product ()
		{
		}

		public string PullRequestRegexp {
			get {
				return string.Format (@"^https?://github\.com/{0}/{1}/pull/(\d+)/?$", GitHubUser, GitHubRepo);
			}
		}

		public string GitRepositoryUrl {
			get {
				return string.Format ("git@github.com:{0}/{1}.git", GitHubUser, GitHubRepo);
			}
		}

		public static Product LoadFromString (string content)
		{
			var product = JsonConvert.DeserializeObject<Product> (content);
			if (string.IsNullOrWhiteSpace (product.Name))
				throw new InvalidDataException ("Product does not have a `Name`.");
			if (string.IsNullOrWhiteSpace (product.GitHubUser) || string.IsNullOrWhiteSpace (product.GitHubRepo))
				throw new InvalidDataException ("Product does not have a `GitHubUser` or `GitHubRepo`.");

			return product;
		}
	}
}

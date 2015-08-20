using System;
using Octokit;

namespace Benchmarker.Common
{
	public class GitHubInterface
	{
		static GitHubClient gitHubClient;

		public static GitHubClient GitHubClient {
			get {
				if (gitHubClient != null)
					return gitHubClient;

				gitHubClient = new Octokit.GitHubClient (new Octokit.ProductHeaderValue ("XamarinBenchmark"));
				if (gitHubClient == null)
					throw new Exception ("Could not instantiate GitHub client");

				var creds = Accredit.GetCredentials ("gitHub") ["publicReadAccessToken"].ToString ();
				gitHubClient.Credentials = new Octokit.Credentials (creds);

				return gitHubClient;
			}
		}
	}
}

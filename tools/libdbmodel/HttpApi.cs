using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace Benchmarker.Models
{
	public class HttpApi
	{
		public class TrustAllCertificatePolicy : System.Net.ICertificatePolicy
		{
			public TrustAllCertificatePolicy ()
			{
			}

			public bool CheckValidationResult (ServicePoint sp, X509Certificate cert, WebRequest req, int problem)
			{
				return true;
			}
		}


		static HttpApi ()
		{
			// remove when we have a valid SSL cert
			System.Net.ServicePointManager.CertificatePolicy = new TrustAllCertificatePolicy ();
		}

		public static string AuthToken { get; set; }

		static string ApiUrl (string path, IDictionary<string, string> args)
		{
			string queryString;
			if (args == null)
				queryString = "";
			else
				queryString = "?" + String.Join ("&", args.Select (kvp => String.Format ("{0}={1}", kvp.Key, Uri.EscapeUriString (kvp.Value))));
			return String.Format ("https://performancebot.mono-project.com/api{0}{1}", path, queryString);
		}

		static async Task<string> Send (string method, string path, IDictionary<string, string> args, object contentObject) {
			HttpContent content = null;
			if (contentObject != null) {
				var body = JsonConvert.SerializeObject (contentObject);
				content = new StringContent (body, System.Text.UTF8Encoding.Default, "application/json");
			}
			using (var client = new HttpClient ()) {
				var url = ApiUrl (path, args);
				var message = new HttpRequestMessage (new HttpMethod (method), url);
				if (content != null)
					message.Content = content;
				client.DefaultRequestHeaders.Add ("Authorization", "token " + AuthToken);

				var response = await client.SendAsync (message);
				var responseBody = await response.Content.ReadAsStringAsync ();
				if (!response.IsSuccessStatusCode) {
					Console.Error.WriteLine ("Error: {0} to `{1}` not successful: {2}", method, url, responseBody);
					return null;
				}
				return responseBody;
			}
		}

		public static async Task<string> Get (string path, IDictionary<string, string> args) {
			return await Send ("GET", path, args, null);
		}

		public static async Task<string> Put (string path, IDictionary<string, string> args, object contentObject) {
			return await Send ("PUT", path, args, contentObject);
		}

		public static async Task<string> Post (string path, IDictionary<string, string> args, object contentObject) {
			return await Send ("POST", path, args, contentObject);
		}
	}
}

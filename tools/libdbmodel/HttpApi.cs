using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;

namespace Benchmarker.Models
{
	public class HttpApi
	{
		public static string AuthToken { get; set; }

		private static string ApiUrl (string path, IDictionary<string, string> args)
		{
			string queryString;
			if (args == null)
				queryString = "";
			else
				queryString = "?" + String.Join ("&", args.Select (kvp => String.Format ("{0}={1}", kvp.Key, Uri.EscapeUriString (kvp.Value))));
			return String.Format ("https://performancebot.mono-project.com/api{0}{1}", path, queryString);
		}

		private static async Task<string> Send (string method, string path, IDictionary<string, string> args, ApiObject contentObject)
		{
			HttpContent content = null;
			if (contentObject != null) {
				var body = JsonConvert.SerializeObject (contentObject.AsDict ());
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

		public static async Task<JObject> GetRunset (long runsetId)
		{
			string body = await HttpApi.Get (String.Format ("/runset/{0}", runsetId), null);
			if (body == null)
				return null;
			return JObject.Parse (body);
		}

		public static async Task<JArray> GetRunsets (string machine, string config)
		{
			var args = new Dictionary<string, string> {
				{ "machine", machine },
				{ "config", config }
			};
			var response = await HttpApi.Get ("/runsets", args);
			if (response == null) {
				return null;
			}
			return JArray.Parse (response);
		}

		private static async Task<string> Get (string path, IDictionary<string, string> args)
		{
			return await Send ("GET", path, args, null);
		}

		// new runset
		public static async Task<string> PutRunset (ApiObject contentObject)
		{
			return await Put ("/runset", null, contentObject);
		}

		private static async Task<string> Put (string path, IDictionary<string, string> args, ApiObject contentObject)
		{
			return await Send ("PUT", path, args, contentObject);
		}

		// amend existing runset
		public static async Task<string> AmendRunset (long runsetId, ApiObject contentObject)
		{
			return await Post (String.Format ("/runset/{0}", runsetId), null, contentObject);
		}

		public static async Task<string> PostRun (long runId, ApiObject contentObject)
		{
			return await HttpApi.Post (String.Format ("/run/{0}", runId), null, contentObject);
		}

		private static async Task<string> Post (string path, IDictionary<string, string> args, ApiObject contentObject)
		{
			return await Send ("POST", path, args, contentObject);
		}

		public static async Task<string> DeleteRunset (long runSetId)
		{
			return await Delete (String.Format ("/runset/{0}", runSetId), null, null);
		}

		private static async Task<string> Delete (string path, IDictionary<string, string> args, ApiObject contentObject)
		{
			return await Send ("DELETE", path, args, contentObject);
		}
	}
}

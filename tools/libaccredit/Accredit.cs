using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using Parse;
using System.Threading;
using System.Net;
using System.Text;
using Benchmarker.Common;

namespace Benchmarker
{
	public static class Accredit
	{
		const string CredentialsFilename = "benchmarkerCredentials";

		static Dictionary<string, JObject> cachedCredentials;

		static void InitializeParse (string applicationId, string dotNetKey)
		{
			ParseClient.Initialize (applicationId, dotNetKey);
			Logging.GetLogging ().Info ("InitializeParse: done");
			if (ParseUser.CurrentUser != null) {
				Logging.GetLogging ().Info ("InitializeParse: user -> " + ParseUser.CurrentUser.Username);
				try {
					ParseUser.LogOut ();
				} catch (Exception) {
					Logging.GetLogging ().Info ("InitializeParse: something fishy");
				}
			} else {
				Logging.GetLogging ().Info ("InitializeParse: nope");
			}
		}

		private static void WaitForConfirmation (string key)
		{
			Console.Error.WriteLine ("Log in on browser for access, confirmation key {0}", key);
			ParseQuery<ParseObject> query = ParseObject.GetQuery ("CredentialsResponse").WhereEqualTo ("key", key);
			while (true) {
				var task = query.FirstOrDefaultAsync ();
				//Console.WriteLine ("FindOrDefaultAsync CredentialsResponse");
				task.Wait ();

				var result = task.Result;
				if (result != null) {
					// FIXME: check that it's successful
					break;
				}
				Thread.Sleep (1000);
			}
			Console.Error.WriteLine ("Login successful");
		}

		private static string GetResponse (string url, string parameters)
		{
			WebRequest webRequest = WebRequest.Create(url);
			byte[] dataStream = Encoding.UTF8.GetBytes (parameters);
			webRequest.Method = "POST";
			webRequest.ContentType = "application/x-www-form-urlencoded";
			webRequest.ContentLength = dataStream.Length;
			using (Stream requestStream = webRequest.GetRequestStream ()) {
				requestStream.Write (dataStream, 0, dataStream.Length);
			}
			WebResponse webResponse = webRequest.GetResponse ();
			string response = new StreamReader(webResponse.GetResponseStream ()).ReadToEnd ();
			return response;
		}

		public static JObject GetCredentials (string serviceName)
		{
			if (cachedCredentials == null) {
				try {
					using (var reader = new StreamReader (new FileStream (CredentialsFilename, FileMode.Open))) {
						var text = reader.ReadToEnd ();
						var parsed = JsonConvert.DeserializeObject<Dictionary<string, JObject>> (text);
						cachedCredentials = parsed;
					}
				} catch (Exception) {
					cachedCredentials = new Dictionary<string, JObject> ();
				}
			}

			if (cachedCredentials.ContainsKey (serviceName))
				return cachedCredentials [serviceName];

			// Accredit
			InitializeParse ("RAePvLdkN2IHQNZRckrVXzeshpFZTgYif8qu5zuh", "giWKLzMOZa2nrgBjC9YPRF238CTVTpNsMlsIJkr3");

			string key = Guid.NewGuid ().ToString ();
			string secret = Guid.NewGuid ().ToString ();

			/* Get github OAuth authentication link */
			string oauthLink = GetResponse ("https://accredit.parseapp.com/requestCredentials", string.Format("service={0}&key={1}&secret={2}", serviceName, key, secret));

			/* Log in github OAuth */
			System.Diagnostics.Process.Start (oauthLink);

			/* Wait for login confirmation */
			WaitForConfirmation (key);

			/* Request the password */
			var response = GetResponse ("https://accredit.parseapp.com/getCredentials", string.Format ("key={0}&secret={1}", key, secret));
			var parsedResponse = JObject.Parse (response);

			cachedCredentials [serviceName] = parsedResponse;

			/* Cache it in the current folder for future use */
			try {
				using (FileStream fileStream = File.Open (CredentialsFilename, FileMode.Create, FileAccess.Write)) {
					using (var streamWriter = new StreamWriter (fileStream)) {
						streamWriter.Write (JsonConvert.SerializeObject (cachedCredentials));
					}
				}
			} catch (Exception) {
				Console.Error.WriteLine ("Warning: Failed to save credentials to file {0}", CredentialsFilename);
			}

			return parsedResponse;
		}
	}
}

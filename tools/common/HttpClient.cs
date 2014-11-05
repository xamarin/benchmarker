using System;
using System.IO;
using System.Net;

namespace Benchmarker.Common
{
	public class HttpClient
	{
		public static string GetContent (string server, string query)
		{
			if (String.IsNullOrEmpty (server))
				throw new Exception ("HttpClient.Server not set");

			try {
				return ReadResponseContent (HttpWebRequest.CreateHttp (String.Format ("http://{0}{1}", server, query)).GetResponse ());
			} catch (WebException e) {
				throw new WebException (String.Format ("GET {0} : {1}", query, ReadResponseContent (e.Response)), e);
			}
		}

		public static Stream GetStream (string server, string query)
		{
			try {
				return HttpWebRequest.CreateHttp (String.Format ("http://{0}{1}", server, query)).GetResponse ().GetResponseStream ();
			} catch (WebException e) {
				throw new WebException (String.Format ("GET {0} : {1}", query, ReadResponseContent (e.Response)), e);
			}
		}

		public static string PostStream (string server, string query, Stream content, string content_type = "application/octet-stream") {
			try {
				var request = HttpWebRequest.CreateHttp (String.Format ("http://{0}{1}", server, query));
				request.Method = "POST";
				request.ContentType = content_type;

				using (var s = request.GetRequestStream ())
					content.CopyTo (s);

				return ReadResponseContent (request.GetResponse ());
			} catch (WebException e) {
				throw new WebException (String.Format ("POST {0} : {1}", query, ReadResponseContent (e.Response)), e);
			}
		}

		static string ReadResponseContent (WebResponse response)
		{
			return new StreamReader (response.GetResponseStream ()).ReadToEnd ();
		}
	}
}


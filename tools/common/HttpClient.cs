using System;
using System.IO;
using System.Net;

namespace Benchmarker.Common
{
	public class HttpClient
	{
		public static HttpWebResponse Get (string url)
		{
			try {
				return (HttpWebResponse) CreateRequest (url).GetResponse ();
			} catch (WebException e) {
				throw new WebException (String.Format ("GET {0} : {1}", url, e.Response == null ? e.Message : ReadResponseContent (e.Response)), e, e.Status, e.Response);
			}
		}

		public static string GetContent (string url)
		{
			try {
				return ReadResponseContent (CreateRequest (url).GetResponse ());
			} catch (WebException e) {
				throw new WebException (String.Format ("GET {0} : {1}", url, e.Response == null ? e.Message : ReadResponseContent (e.Response)), e, e.Status, e.Response);
			}
		}

		public static Stream GetStream (string url)
		{
			try {
				return CreateRequest (url).GetResponse ().GetResponseStream ();
			} catch (WebException e) {
				throw new WebException (String.Format ("GET {0} : {1}", url, e.Response == null ? e.Message : ReadResponseContent (e.Response)), e, e.Status, e.Response);
			}
		}

		public static string PostStream (string url, Stream content, string content_type = "application/octet-stream")
		{
			if (content == null)
				throw new ArgumentNullException ("content");
			if (!content.CanRead)
				throw new ArgumentException ("Cannot read \"content\"");

			try {
				var request = CreateRequest (url);
				request.Method = "POST";
				request.ContentType = content_type;

				using (var s = request.GetRequestStream ())
					content.CopyTo (s);

				return ReadResponseContent (request.GetResponse ());
			} catch (WebException e) {
				throw new WebException (String.Format ("POST {0} : {1}", url, e.Response == null ? e.Message : ReadResponseContent (e.Response)), e, e.Status, e.Response);
			}
		}

		public static HttpWebRequest CreateRequest (string url, string useragent = "Xamarin Benchmarker")
		{
			if (String.IsNullOrWhiteSpace (url))
				throw new ArgumentNullException ("url");

			var request = (HttpWebRequest) HttpWebRequest.Create (new Uri (url));

			if (!String.IsNullOrWhiteSpace (useragent))
				request.UserAgent = useragent;

			return request;
		}

		static string ReadResponseContent (WebResponse response)
		{
			if (response == null)
				throw new ArgumentNullException ("response");

			return new StreamReader (response.GetResponseStream ()).ReadToEnd ();
		}
	}
}


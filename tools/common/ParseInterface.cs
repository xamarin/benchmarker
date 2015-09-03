using System;
using Parse;
using Nito.AsyncEx;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace Benchmarker.Common
{
    public class ParseInterface
	{
		public static ParseACL defaultACL;
		public static JObject benchmarkerCredentials;

		public static void InitializeParse (string applicationId, string dotNetKey)
		{
			ParseClient.Initialize (applicationId, dotNetKey);
			if (ParseUser.CurrentUser != null) {
				try {
					ParseUser.LogOut ();
				} catch (Exception) {
				}
			}
		}

		public static void InitializeParseForXamarinPerformance ()
		{
			InitializeParse ("7khPUBga9c7L1YryD1se1bp6VRzKKJESc0baS9ES", "FwqUX9gNQP5HmP16xDcZRoh0jJRCDvdoDpv8L87p");
		}

		public static bool Initialize ()
		{
			try {
				Debug.Assert (benchmarkerCredentials != null, "client must set benchmarkerCredentials");

				// Xamarin Performance
				InitializeParseForXamarinPerformance ();
				var username = benchmarkerCredentials ["username"].ToString ();
				var password = benchmarkerCredentials ["password"].ToString ();
				var user = AsyncContext.Run (() 
					=> ParseInterface.RunWithRetry (() 
						=> ParseUser.LogInAsync (username, password)));

				var acl = new ParseACL (user);
				acl.PublicReadAccess = true;
				acl.PublicWriteAccess = false;

				defaultACL = acl;
			} catch (Exception e) {
				Logging.GetLogging().Error ("Error: Exception when initializing Parse API: {0}", e);
				return false;
			}
			return true;
		}

		public static ParseObject NewParseObject (string className)
		{
			if (defaultACL == null)
				throw new Exception ("ParseInterface must be initialized before ParseObjects can be created.");
			var obj = new ParseObject (className);
			obj.ACL = defaultACL;
			return obj;
		}

		public static double NumberAsDouble (object o)
		{
			if (o.GetType () == typeof (long))
				return (double)(long)o;
			if (o.GetType () == typeof (double))
				return (double)o;
			throw new Exception ("Number is neither double nor long.");
		}

		public async static Task RunWithRetry (Func<Task> run, int numTries = 3) {
			for (var i = 0; i < numTries - 1; ++i) {
				try {
					await run ();
					return;
				} catch (Exception exc) {
					var seconds = (i == 0) ? 10 : 60 * i;
					Logging.GetLogging ().ErrorFormat ("Exception when running task - sleeping {0} seconds and retrying: {1}", seconds, exc);
					await Task.Delay (seconds * 1000);
				}
			}
			await run ();
		}

		public async static Task<T> RunWithRetry<T> (Func<Task<T>> run, int numTries = 3) {
			for (var i = 0; i < numTries - 1; ++i) {
				try {
					return await run ();
				} catch (Exception exc) {
					var seconds = (i == 0) ? 10 : 60 * i;
					Logging.GetLogging ().ErrorFormat ("Exception when running task - sleeping {0} seconds and retrying: {1}", seconds, exc);
					await Task.Delay (seconds * 1000);
				}
			}
			return await run ();
		}

		public static async Task<IEnumerable<T>> PageQueryWithRetry<T> (Func<ParseQuery<T>> makeQuery) where T : ParseObject
		{
			List<T> results = new List<T> ();
			const int limit = 100;
			for (var skip = 0;; skip += limit) {
				var page = await RunWithRetry (() => {
					var query = makeQuery ().Limit (limit).Skip (skip);
					return query.FindAsync ();
				});
				results.AddRange (page);
				if (page.Count () < limit)
					break;
			}
			return results;
		}
	}
}

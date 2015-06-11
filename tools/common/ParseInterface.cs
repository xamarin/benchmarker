using System;
using Parse;
using Nito.AsyncEx;

namespace Benchmarker.Common
{
	public class ParseInterface
	{
		static ParseACL defaultACL;

		public static bool Initialize ()
		{
			try {
				ParseClient.Initialize ("7khPUBga9c7L1YryD1se1bp6VRzKKJESc0baS9ES", "FwqUX9gNQP5HmP16xDcZRoh0jJRCDvdoDpv8L87p");
				var user = AsyncContext.Run (() => ParseUser.LogInAsync ("benchmarker", "WhammyJammy"));

				Console.WriteLine ("User authenticated: " + user.IsAuthenticated);

				var acl = new ParseACL (user);
				acl.PublicReadAccess = true;
				acl.PublicWriteAccess = false;

				defaultACL = acl;
			} catch (Exception) {
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
	}
}

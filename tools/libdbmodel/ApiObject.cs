using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Benchmarker.Models
{
	public interface ApiObject
	{
		IDictionary<string, object> AsDict ();
	}
}

 
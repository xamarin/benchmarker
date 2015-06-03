using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Parse;
using Mono.Unix.Native;
using System.Linq;

namespace Benchmarker.Common.Models
{
	public class RunSet
	{
		List<Result> results;
		public List<Result> Results { get { return results; } }
		public DateTime StartDateTime { get; set; }
		public DateTime FinishDateTime { get; set; }
		public Config Config { get; set; }
		public Commit Commit { get; set; }

		public RunSet ()
		{
			results = new List<Result> ();
		}

		async Task<ParseObject> GetOrUploadMachineToParse ()
		{
			Utsname utsname;
			var res = Syscall.uname (out utsname);
			string arch;
			string hostname;
			if (res != 0) {
				arch = "unknown";
				hostname = "unknown";
			} else {
				arch = utsname.machine;
				hostname = utsname.nodename;
			}

			var results = await ParseObject.GetQuery ("Machine").WhereEqualTo ("name", hostname).WhereEqualTo ("architecture", arch).FindAsync ();
			if (results.Count () > 0)
				return results.First ();
			var obj = new ParseObject ("Machine");
			obj ["name"] = hostname;
			obj ["architecture"] = arch;
			await obj.SaveAsync ();
			return obj;
		}

		public async Task<ParseObject> UploadToParse ()
		{
			var m = await GetOrUploadMachineToParse ();
			var c = await Config.GetOrUploadToParse ();
			var commit = await Commit.GetOrUploadToParse ();
			var obj = new ParseObject ("RunSet");
			obj ["machine"] = m;
			obj ["config"] = c;
			obj ["commit"] = commit;
			obj ["startedAt"] = StartDateTime;
			obj ["finishedAt"] = FinishDateTime;
			await obj.SaveAsync ();
			foreach (var result in results) {
				if (result.Config != Config)
					throw new Exception ("Results must have the same config as their RunSets");
				if (result.Timedout)
					continue;
				await result.UploadRunsToParse (obj);
			}
			return obj;
		}
	}
}

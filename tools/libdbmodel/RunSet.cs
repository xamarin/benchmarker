using System;
using System.Collections.Generic;
using System.Linq;
using Nito.AsyncEx;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Benchmarker.Models
{
	public class RunSet : ApiObject
	{
		public static string DATETIME_FORMAT = "yyyy-MM-ddTHH:mm:sszzz";

		public long? Id { get; set; }

		List<Run> runs;

		public List<Run> Runs { get { return runs; } }

		public DateTime StartDateTime { get; set; }

		public DateTime FinishDateTime { get; set; }

		public Machine Machine { get; set; }

		public Config Config { get; set; }

		public Commit Commit { get; set; }

		public List<Commit> SecondaryCommits { get; set; }

		public string BuildURL { get; set; }

		public string LogURL { get; set; }

		public string PullRequestURL { get; set; }

		public long? PullRequestBaselineRunSetId { get; set; }

		public List<string> TimedOutBenchmarks { get; set; }

		public List<string> CrashedBenchmarks { get; set; }

		public RunSet ()
		{
			runs = new List<Run> ();
			TimedOutBenchmarks = new List<string> ();
			CrashedBenchmarks = new List<string> ();
		}

		public static async Task<RunSet> FromId (Machine local_machine, long local_runsetid, Config local_config, Commit local_mainCommit, List<Commit> local_secondaryCommits, string local_buildURL, string local_logURL)
		{
			using (var client = new HttpClient ()) {
				JObject db_result = await HttpApi.GetRunset (local_runsetid);
				if (db_result == null) {
					return null;
				}

				var runSet = new RunSet {
					Id = local_runsetid,
					StartDateTime = db_result ["StartedAt"].ToObject<DateTime> (),
					FinishDateTime = db_result ["FinishedAt"].ToObject<DateTime> (),
					BuildURL = db_result ["BuildURL"].ToObject<string> (),
					Machine = local_machine,
					LogURL = local_logURL,
					Config = local_config,
					Commit = local_mainCommit,
					TimedOutBenchmarks = db_result ["TimedOutBenchmarks"].ToObject<List<string>> (),
					CrashedBenchmarks = db_result ["CrashedBenchmarks"].ToObject<List<string>> ()
				};

				var db_mainProductCommit = db_result ["MainProduct"] ["Commit"].ToObject<string> ();
				if (local_mainCommit.Hash != db_mainProductCommit)
					throw new Exception (String.Format ("Commit ({0}) does not match the one in the database ({1}).", local_mainCommit.Hash, db_mainProductCommit));

				var db_secondaryCommits = new List<Commit> ();
				foreach (var sc in db_result ["SecondaryProducts"]) {
					db_secondaryCommits.Add (new Commit {
						Hash = sc ["Commit"].ToObject<string> (),
						Product = new Product { Name = sc ["Name"].ToObject<string> () }
					});
				}
				if (local_secondaryCommits != null) {
					if (local_secondaryCommits.Count != db_secondaryCommits.Count)
						throw new Exception ("Secondary commits don't match the database.");
					foreach (var sc in db_secondaryCommits) {
						if (!local_secondaryCommits.Any (c => c.Hash == sc.Hash && c.Product.Name == sc.Product.Name))
							throw new Exception ("Secondary commits don't match the database.");
					}
					// local commits have more information (e.g. datetime)
					runSet.SecondaryCommits = local_secondaryCommits;
				} else {
					runSet.SecondaryCommits = db_secondaryCommits;
				}


				if (local_buildURL != null && local_buildURL != runSet.BuildURL)
					throw new Exception ("Build URL does not match the one in the database.");
				
				var db_machineName = db_result ["Machine"] ["Name"].ToObject<string> ();
				var db_machineArchitecture = db_result ["Machine"] ["Architecture"].ToObject<string> ();
				if (local_machine.Name != db_machineName || local_machine.Architecture != db_machineArchitecture)
					throw new Exception ("Machine does not match the one in the database. \"" + db_machineName + "\" vs. \"" + local_machine.Name + "\"");

				if (!local_config.EqualsApiObject (db_result ["Config"]))
					throw new Exception ("Config does not match the one in the database.");

				return runSet;
			}
		}

		public IDictionary<string, object> AsDict ()
		{
			var logURLs = new Dictionary<string, string> ();
			if (LogURL != null) {
				string defaultURL;
				logURLs.TryGetValue ("*", out defaultURL);
				if (defaultURL == null) {
					logURLs ["*"] = LogURL;
				} else if (defaultURL != LogURL) {
					foreach (var run in Runs)
						logURLs [run.Benchmark.Name] = LogURL;
				}
			}

			var dict = new Dictionary<string, object> ();
			dict ["MainProduct"] = Commit.AsDict ();
			dict ["SecondaryProducts"] = SecondaryCommits.Select (c => c.AsDict ()).ToList ();
			dict ["Machine"] = Machine.AsDict ();
			dict ["Config"] = Config.AsDict ();
			dict ["TimedOutBenchmarks"] = new List<string> (TimedOutBenchmarks);
			dict ["CrashedBenchmarks"] = new List<string> (CrashedBenchmarks);
			dict ["StartedAt"] = StartDateTime.ToString (DATETIME_FORMAT);
			dict ["FinishedAt"] = FinishDateTime.ToString (DATETIME_FORMAT);
			dict ["BuildURL"] = BuildURL;
			dict ["LogURLs"] = logURLs;
			dict ["Runs"] = Runs.Select (r => r.AsDict ()).ToList ();
			if (PullRequestBaselineRunSetId != null) {
				var prDict = new Dictionary<string, object> ();
				prDict ["BaselineRunSetID"] = PullRequestBaselineRunSetId.Value;
				prDict ["URL"] = PullRequestURL;
				dict ["PullRequest"] = prDict;
			}
			return dict;
		}

		public class UploadResult
		{
			[JsonProperty ("RunSetID")]
			public long RunSetId { get; set; }

			[JsonProperty ("RunIDs")]
			public long[] RunIds { get; set; }

			[JsonProperty ("PullRequestID")]
			public long? PullRequestId { get; set; }

			public UploadResult ()
			{
			}
		}

		public async Task<UploadResult> Upload ()
		{
			string responseBody;
			if (Id == null) {
				responseBody = await HttpApi.PutRunset (this);
			} else {
				responseBody = await HttpApi.AmendRunset (Id.Value, this);
			}
			if (responseBody == null)
				return null;
			return JsonConvert.DeserializeObject<UploadResult> (responseBody);
		}
	}
}

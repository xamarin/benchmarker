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
	public class RunSet
	{
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

		public static async Task<RunSet> FromId (Machine machine, long id, Config config, Commit mainCommit, List<Commit> secondaryCommits, string buildURL, string logURL)
		{
			using (var client = new HttpClient ()) {
				var body = await HttpApi.Get (String.Format ("/runset/{0}", id), null);
				if (body == null)
					return null;
				var result = JObject.Parse (body);

				var runSet = new RunSet {
					Id = id,
					StartDateTime = result ["StartedAt"].ToObject<DateTime> (),
					FinishDateTime = result ["FinishedAt"].ToObject<DateTime> (),
					BuildURL = result ["BuildURL"].ToObject<string> (),
					Machine = machine,
					LogURL = logURL,
					Config = config,
					Commit = mainCommit,
					TimedOutBenchmarks = result ["TimedOutBenchmarks"].ToObject<List<string>> (),
					CrashedBenchmarks = result ["CrashedBenchmarks"].ToObject<List<string>> ()
				};

				var mainProductCommit = result ["MainProduct"] ["Commit"].ToObject<string> ();
				if (mainCommit.Hash != mainProductCommit)
					throw new Exception (String.Format ("Commit ({0}) does not match the one in the database ({1}).", mainCommit.Hash, mainProductCommit));

				var secondaryCommitsFromDatabase = new List<Commit> ();
				foreach (var sc in result ["SecondaryProducts"]) {
					secondaryCommitsFromDatabase.Add (new Commit {
						Hash = sc ["Commit"].ToObject<string> (),
						Product = new Product { Name = sc ["Name"].ToObject<string> () }
					});
				}
				if (secondaryCommits != null) {
					if (secondaryCommits.Count != secondaryCommitsFromDatabase.Count)
						throw new Exception ("Secondary commits don't match the database.");
					foreach (var sc in secondaryCommitsFromDatabase) {
						if (!secondaryCommits.Any (c => c.Hash == sc.Hash && c.Product.Name == sc.Product.Name))
							throw new Exception ("Secondary commits don't match the database.");
					}
				}
				runSet.SecondaryCommits = secondaryCommitsFromDatabase;

				if (buildURL != null && buildURL != runSet.BuildURL)
					throw new Exception ("Build URL does not match the one in the database.");
				
				var machineName = result ["Machine"] ["Name"].ToObject<string> ();
				// The `StartsWith` case here is a weird exception we need for TestCloud devices,
				// which have a common prefix, and we treat them as the same machine.
				if ((machine.Name != machineName && !machineName.StartsWith (machine.Name)) || machine.Architecture != result ["Machine"]["Architecture"].ToObject<string> ())
					throw new Exception ("Machine does not match the one in the database.");

				if (!config.EqualsApiObject (result ["Config"]))
					throw new Exception ("Config does not match the one in the database.");

				return runSet;
			}
		}

		public Dictionary<string, object> ApiObject
		{
			get {
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
				dict ["MainProduct"] = Commit.ApiObject;
				dict ["SecondaryProducts"] = SecondaryCommits.Select(c => c.ApiObject).ToList ();
				dict ["Machine"] = Machine.ApiObject;
				dict ["Config"] = Config.ApiObject;
				dict ["TimedOutBenchmarks"] = new List<string> (TimedOutBenchmarks);
				dict ["CrashedBenchmarks"] = new List<string> (CrashedBenchmarks);
				dict ["StartedAt"] = StartDateTime;
				dict ["FinishedAt"] = FinishDateTime;
				dict ["BuildURL"] = BuildURL;
				dict ["LogURLs"] = logURLs;
				dict ["Runs"] = Runs.Select (r => r.ApiObject).ToList ();
				if (PullRequestBaselineRunSetId != null) {
					var prDict = new Dictionary<string, object> ();
					prDict ["BaselineRunSetID"] = PullRequestBaselineRunSetId.Value;
					prDict ["URL"] = PullRequestURL;
					dict ["PullRequest"] = prDict;
				}
				return dict;
			}
		}

		public class UploadResult {
			[JsonProperty ("RunSetID")]
			public long RunSetId { get; set; }

			[JsonProperty ("RunIDs")]
			public long[] RunIds { get; set; }

			[JsonProperty ("PullRequestID")]
			public long? PullRequestId { get; set; }

			public UploadResult () { }
		}

		public async Task<UploadResult> Upload () {
			string responseBody;
			if (Id == null) {
				responseBody = await HttpApi.Put ("/runset", null, ApiObject);
			} else {
				responseBody = await HttpApi.Post (String.Format ("/runset/{0}", Id.Value), null, ApiObject);
			}
			if (responseBody == null)
				return null;
			return JsonConvert.DeserializeObject<UploadResult> (responseBody);
		}
	}
}

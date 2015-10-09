using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Net;
using System.Linq;

namespace Xamarin.TestCloud.Api.V0
{

    /* To use the api, you'll need to add this file 
     * and the NewtonSoft.JSON NuGet package to your solution. */ 

    public class Client
    {
        
        public Apps Apps { get; set; }
        public TestRuns TestRuns { get; set; }
        public DeviceConfigurations DeviceConfigurations { get; set; }
        public Subscriptions Subscriptions { get; set; }

        public Client(string apiKey, String basePath = "https://testcloud.xamarin.com")
        {
            BasePath = basePath;
            apiInvoker.ApiKey = apiKey;

            
            Apps = new Apps(this);
            TestRuns = new TestRuns(this);
            DeviceConfigurations = new DeviceConfigurations(this);
            Subscriptions = new Subscriptions(this);
        }

        protected ApiInvoker apiInvoker = ApiInvoker.GetInstance();
        public ApiInvoker Invoker { get { return apiInvoker; } }
        public String BasePath { get; set; }
    }


    

    public class Apps
    {
        Client api;

        public Apps(Client api)
        {
            this.api = api;
        }
        

        // List apps owned by the team
        public async Task<List<App>> Find(int page = 1, int perPage = 100)
        {
            // create path and map variables
            string path = "/api/v0/apps";

            // query params
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, object>();
            
            queryParams.AddParam("page", page);
            queryParams.AddParam("per_page", perPage);

            try {
                var response = await api.Invoker.InvokeAPI(api.BasePath, path, "GET", queryParams, null, headerParams, formParams);
                if(response != null)
                {
                    var result = (AppCollection)ApiInvoker.Deserialize(response, typeof(AppCollection));
                    return result.Apps;
                }
                else
                {
                    return null;
                }
                
            }
            catch(ApiException ex)
            {
                if(ex.ErrorCode == 404)
                {
                    return null;
                }
                else
                {
                    throw ex;
                }
            }
        }

        // List test runs for an app
        public async Task<List<TestRun>> TestRuns(Guid appId, int page = 1, int perPage = 100)
        {
            // create path and map variables
            string path = string.Format("/api/v0/apps/{0}/test-runs", appId);

            // query params
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, object>();
            
            if (appId == default(Guid)) {
                throw new ApiException(400, "missing required params");
            }
            queryParams.AddParam("page", page);
            queryParams.AddParam("per_page", perPage);

            try {
                var response = await api.Invoker.InvokeAPI(api.BasePath, path, "GET", queryParams, null, headerParams, formParams);
                if(response != null)
                {
                    var result = (TestRunCollection)ApiInvoker.Deserialize(response, typeof(TestRunCollection));
                    return result.TestRuns;
                }
                else
                {
                    return null;
                }
                
            }
            catch(ApiException ex)
            {
                if(ex.ErrorCode == 404)
                {
                    return null;
                }
                else
                {
                    throw ex;
                }
            }
        }
    }

    public class TestRuns
    {
        Client api;

        public TestRuns(Client api)
        {
            this.api = api;
        }
        

        // Results of a single test run
        public async Task<ResultCollection> Results(Guid id)
        {
            // create path and map variables
            string path = string.Format("/api/v0/test-runs/{0}/results", id);

            // query params
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, object>();
            
            if (id == default(Guid)) {
                throw new ApiException(400, "missing required params");
            }

            try {
                var response = await api.Invoker.InvokeAPI(api.BasePath, path, "GET", queryParams, null, headerParams, formParams);
                if(response != null)
                {
                    var result = (ResultCollection)ApiInvoker.Deserialize(response, typeof(ResultCollection));
                    return result;
                }
                else
                {
                    return null;
                }
                
            }
            catch(ApiException ex)
            {
                if(ex.ErrorCode == 404)
                {
                    return null;
                }
                else
                {
                    throw ex;
                }
            }
        }

        // Metadata of a single test run
        public async Task<TestRun> Get(Guid id)
        {
            // create path and map variables
            string path = string.Format("/api/v0/test-runs/{0}", id);

            // query params
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, object>();
            
            if (id == default(Guid)) {
                throw new ApiException(400, "missing required params");
            }

            try {
                var response = await api.Invoker.InvokeAPI(api.BasePath, path, "GET", queryParams, null, headerParams, formParams);
                if(response != null)
                {
                    var result = (TestRun)ApiInvoker.Deserialize(response, typeof(TestRun));
                    return result;
                }
                else
                {
                    return null;
                }
                
            }
            catch(ApiException ex)
            {
                if(ex.ErrorCode == 404)
                {
                    return null;
                }
                else
                {
                    throw ex;
                }
            }
        }
    }

    public class DeviceConfigurations
    {
        Client api;

        public DeviceConfigurations(Client api)
        {
            this.api = api;
        }
        

        // List device configurations currently available
        public async Task<List<DeviceConfiguration>> Find(string q = default(string), string sort = default(string), string model = default(string), string manufacturer = default(string), string name = default(string), string os = default(string), string platform = default(string), int page = 1, int perPage = 100)
        {
            // create path and map variables
            string path = "/api/v0/device-configurations";

            // query params
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, object>();
            
            queryParams.AddParam("q", q);
            queryParams.AddParam("sort", sort);
            queryParams.AddParam("model", model);
            queryParams.AddParam("manufacturer", manufacturer);
            queryParams.AddParam("name", name);
            queryParams.AddParam("os", os);
            queryParams.AddParam("platform", platform);
            queryParams.AddParam("page", page);
            queryParams.AddParam("per_page", perPage);

            try {
                var response = await api.Invoker.InvokeAPI(api.BasePath, path, "GET", queryParams, null, headerParams, formParams);
                if(response != null)
                {
                    var result = (DeviceConfigurationCollection)ApiInvoker.Deserialize(response, typeof(DeviceConfigurationCollection));
                    return result.DeviceConfigurations;
                }
                else
                {
                    return null;
                }
                
            }
            catch(ApiException ex)
            {
                if(ex.ErrorCode == 404)
                {
                    return null;
                }
                else
                {
                    throw ex;
                }
            }
        }
    }

    public class Subscriptions
    {
        Client api;

        public Subscriptions(Client api)
        {
            this.api = api;
        }
        

        // Retrieve details of the team's current subscription
        public async Task<Subscription> Current()
        {
            // create path and map variables
            string path = "/api/v0/subscriptions/current";

            // query params
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, object>();
            

            try {
                var response = await api.Invoker.InvokeAPI(api.BasePath, path, "GET", queryParams, null, headerParams, formParams);
                if(response != null)
                {
                    var result = (Subscription)ApiInvoker.Deserialize(response, typeof(Subscription));
                    return result;
                }
                else
                {
                    return null;
                }
                
            }
            catch(ApiException ex)
            {
                if(ex.ErrorCode == 404)
                {
                    return null;
                }
                else
                {
                    throw ex;
                }
            }
        }
    }

    
    // The web pages related to a resource
    public class Link
    {
        
        public string Title { get; set; }
        
        public Uri Href { get; set; }
        

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Link");
            sb.AppendFormat("  {0}{1}\n", "Title".PadRight(20), Title);
            sb.AppendFormat("  {0}{1}\n", "Href".PadRight(20), Href);
            return sb.ToString();
        }
    }
    // A list of links
    public class LinkCollection
    {
        
        public List<Link> Links { get; set; }
        

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("LinkCollection");
            sb.AppendFormat("  {0}{1}\n", "Links".PadRight(20), "\n----\n" + String.Join(", \n", Links.Select(a => a.ToString()).ToArray()));
            
            return sb.ToString();
        }
    }
    // Summary single test run on Xamarin Test Cloud
    public class TestRun
    {
        
        public string AppName { get; set; }
        
        public Guid Id { get; set; }
        
        public DateTime? DateUploaded { get; set; }
        
        public string RelativeDate { get; set; }
        
        public string Platform { get; set; }
        
        public string AppVersion { get; set; }
        
        public Dictionary<string, object> TestParameters { get; set; }
        
        public string BinaryId { get; set; }
        
        public string TestSeriesName { get; set; }
        
        public string NameOfUploader { get; set; }
        
        public string EmailOfUploader { get; set; }
        
        public Uri Results { get; set; }
        

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("TestRun");
            sb.AppendFormat("  {0}{1}\n", "AppName".PadRight(20), AppName);
            sb.AppendFormat("  {0}{1}\n", "Id".PadRight(20), Id);
            sb.AppendFormat("  {0}{1}\n", "DateUploaded".PadRight(20), DateUploaded);
            sb.AppendFormat("  {0}{1}\n", "RelativeDate".PadRight(20), RelativeDate);
            sb.AppendFormat("  {0}{1}\n", "Platform".PadRight(20), Platform);
            sb.AppendFormat("  {0}{1}\n", "AppVersion".PadRight(20), AppVersion);
            sb.AppendFormat("  {0}{1}\n", "TestParameters".PadRight(20), TestParameters);
            sb.AppendFormat("  {0}{1}\n", "BinaryId".PadRight(20), BinaryId);
            sb.AppendFormat("  {0}{1}\n", "TestSeriesName".PadRight(20), TestSeriesName);
            sb.AppendFormat("  {0}{1}\n", "NameOfUploader".PadRight(20), NameOfUploader);
            sb.AppendFormat("  {0}{1}\n", "EmailOfUploader".PadRight(20), EmailOfUploader);
            sb.AppendFormat("  {0}{1}\n", "Results".PadRight(20), Results);
            return sb.ToString();
        }
    }
    // List of test runs
    public class TestRunCollection
    {
        
        public List<TestRun> TestRuns { get; set; }
        

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("TestRunCollection");
            sb.AppendFormat("  {0}{1}\n", "TestRuns".PadRight(20), "\n----\n" + String.Join(", \n", TestRuns.Select(a => a.ToString()).ToArray()));
            
            return sb.ToString();
        }
    }
    // Summary statistics for a single test across different devices in a single test run.
    public class TestStatistics
    {
        
        public int Passed { get; set; }
        
        public int Failed { get; set; }
        
        public int Skipped { get; set; }
        
        public int Total { get; set; }
        

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("TestStatistics");
            sb.AppendFormat("  {0}{1}\n", "Passed".PadRight(20), Passed);
            sb.AppendFormat("  {0}{1}\n", "Failed".PadRight(20), Failed);
            sb.AppendFormat("  {0}{1}\n", "Skipped".PadRight(20), Skipped);
            sb.AppendFormat("  {0}{1}\n", "Total".PadRight(20), Total);
            return sb.ToString();
        }
    }
    // Details of a single test run
    public class ResultCollection
    {
        
        public List<Result> Results { get; set; }

        public Logs Logs { get; set; }
        
        public bool Finished { get; set; }
        

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine ("ResultCollection");
            sb.AppendFormat ("  {0}{1}\n", "Results".PadRight(20), "\n----\n" + String.Join(", \n", Results.Select(a => a.ToString()).ToArray()));
            sb.AppendFormat ("  {0}{1}\n", "Logs".PadRight (20), Logs);
            sb.AppendFormat ("  {0}{1}\n", "Finished".PadRight(20), Finished);
            return sb.ToString ();
        }
    }
    // The result of one test on one device
    public class Result
    {
        
        public string TestGroup { get; set; }
        
        public string TestName { get; set; }
        
        public string DeviceConfigurationId { get; set; }
        
        public string Status { get; set; }
        

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Result");
            sb.AppendFormat("  {0}{1}\n", "TestGroup".PadRight(20), TestGroup);
            sb.AppendFormat("  {0}{1}\n", "TestName".PadRight(20), TestName);
            sb.AppendFormat("  {0}{1}\n", "DeviceConfigurationId".PadRight(20), DeviceConfigurationId);
            sb.AppendFormat("  {0}{1}\n", "Status".PadRight(20), Status);
            return sb.ToString();
        }
    }

	public class Logs
	{
		public List<Device> Devices { get; set; }
		public string NunitXmlZip { get; set; }

		public override string ToString ()
		{
			var sb = new StringBuilder ();
			sb.AppendLine ("Logs");
			sb.AppendFormat ("  {0}{1}\n", "Devices".PadRight(20), "\n----\n" + String.Join(", \n", Devices.Select(a => a.ToString()).ToArray()));
			sb.AppendFormat ("  {0}{1}\n", "NunitXmlZip".PadRight (20), NunitXmlZip);
			return sb.ToString ();
		}
	}

	public class Device
	{
		public string DeviceConfigurationId { get; set; }
		public string DeviceLog { get; set; }
		public string TestLog { get; set; }

		public override string ToString ()
		{
			var sb = new StringBuilder();
			sb.AppendLine("Device");
			sb.AppendFormat("  {0}{1}\n", "DeviceConfigurationId".PadRight(20), DeviceConfigurationId);
			sb.AppendFormat("  {0}{1}\n", "DeviceLog".PadRight(20), DeviceLog);
			sb.AppendFormat("  {0}{1}\n", "TestLog".PadRight(20), TestLog);
			return sb.ToString();
		}
	}
    // A mobile app uploaded to Xamarin Test Cloud
    public class App
    {
        
        public string Name { get; set; }
        
        public Guid Id { get; set; }
        
        public string BinaryId { get; set; }
        
        public string Platform { get; set; }
        
        public DateTime? CreatedDate { get; set; }
        
        public Uri TestRuns { get; set; }
        
        public List<Link> Links { get; set; }
        

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("App");
            sb.AppendFormat("  {0}{1}\n", "Name".PadRight(20), Name);
            sb.AppendFormat("  {0}{1}\n", "Id".PadRight(20), Id);
            sb.AppendFormat("  {0}{1}\n", "BinaryId".PadRight(20), BinaryId);
            sb.AppendFormat("  {0}{1}\n", "Platform".PadRight(20), Platform);
            sb.AppendFormat("  {0}{1}\n", "CreatedDate".PadRight(20), CreatedDate);
            sb.AppendFormat("  {0}{1}\n", "TestRuns".PadRight(20), TestRuns);
            sb.AppendFormat("  {0}{1}\n", "Links".PadRight(20), "\n----\n" + String.Join(", \n", Links.Select(a => a.ToString()).ToArray()));
            
            return sb.ToString();
        }
    }
    // A list of mobile apps uploaded to Xamarin Test Cloud
    public class AppCollection
    {
        
        public List<App> Apps { get; set; }
        

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("AppCollection");
            sb.AppendFormat("  {0}{1}\n", "Apps".PadRight(20), "\n----\n" + String.Join(", \n", Apps.Select(a => a.ToString()).ToArray()));
            
            return sb.ToString();
        }
    }
    // 
    public class DeviceConfiguration
    {
        
        public string Name { get; set; }
        
        public string Id { get; set; }
        
        public string Manufacturer { get; set; }
        
        public string Platform { get; set; }
        
        public string Os { get; set; }
        

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("DeviceConfiguration");
            sb.AppendFormat("  {0}{1}\n", "Name".PadRight(20), Name);
            sb.AppendFormat("  {0}{1}\n", "Id".PadRight(20), Id);
            sb.AppendFormat("  {0}{1}\n", "Manufacturer".PadRight(20), Manufacturer);
            sb.AppendFormat("  {0}{1}\n", "Platform".PadRight(20), Platform);
            sb.AppendFormat("  {0}{1}\n", "Os".PadRight(20), Os);
            return sb.ToString();
        }
    }
    // 
    public class DeviceConfigurationCollection
    {
        
        public List<DeviceConfiguration> DeviceConfigurations { get; set; }
        

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("DeviceConfigurationCollection");
            sb.AppendFormat("  {0}{1}\n", "DeviceConfigurations".PadRight(20), "\n----\n" + String.Join(", \n", DeviceConfigurations.Select(a => a.ToString()).ToArray()));
            
            return sb.ToString();
        }
    }
    // Xamarin Test Cloud Subscription information
    public class Subscription
    {
        
        public string Name { get; set; }
        
        public DateTime? PeriodStart { get; set; }
        
        public DateTime? NextUsageReset { get; set; }
        
        public float TotalHours { get; set; }
        
        public float UsedHours { get; set; }
        
        public float UsedPercent { get; set; }
        

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Subscription");
            sb.AppendFormat("  {0}{1}\n", "Name".PadRight(20), Name);
            sb.AppendFormat("  {0}{1}\n", "PeriodStart".PadRight(20), PeriodStart);
            sb.AppendFormat("  {0}{1}\n", "NextUsageReset".PadRight(20), NextUsageReset);
            sb.AppendFormat("  {0}{1}\n", "TotalHours".PadRight(20), TotalHours);
            sb.AppendFormat("  {0}{1}\n", "UsedHours".PadRight(20), UsedHours);
            sb.AppendFormat("  {0}{1}\n", "UsedPercent".PadRight(20), UsedPercent);
            return sb.ToString();
        }
    }

    public class ApiException : Exception {

        readonly int errorCode = 0;
        public ApiException() {}

        public int ErrorCode { 
            get
            {
                return errorCode;
            }
        }

        public ApiException(int errorCode, string message) : base(message)
        {
            this.errorCode = errorCode;
        }
    }

    public class ApiInvoker
    {
        static readonly ApiInvoker _instance = new ApiInvoker();
        Dictionary<String, String> defaultHeaderMap = new Dictionary<String, String>();
        string apiKey;

        public static ApiInvoker GetInstance()
        {
            return _instance;
        }


        public void AddDefaultHeader(string key, string value)
        {
            defaultHeaderMap.Add(key, value);
        }


        public string ApiKey { 
            set { apiKey = value; }
        }

        public static string EscapeString(string str)
        {
            return Uri.EscapeDataString(str);
        }


        public static object Deserialize(string json, Type type)
        {
            try
            {
                return JsonConvert.DeserializeObject(json, type);
            }
            catch(IOException e)
            {
                throw new ApiException(500, e.Message);
            }

        }

        public static string Serialize(object obj)
        {
            try
            {
                return obj != null ? JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }) : null;
            }
            catch(Exception e)
            {
                throw new ApiException(500, e.Message);
            }
        }


        public virtual async Task<string> InvokeAPI(string host, string path, string method, Dictionary<String, String> queryParams, object body, Dictionary<String, String> headerParams, Dictionary<String, object> formParams)
        {
            object value = await InvokeAPIInternal(host, path, method, false, queryParams, body, headerParams, formParams);
            return value as string;
        }


        public virtual async Task<byte[]> InvokeBinaryAPI(string host, string path, string method, Dictionary<String, String> queryParams, object body, Dictionary<String, String> headerParams, Dictionary<String, object> formParams)
        {
            object value = await InvokeAPIInternal(host, path, method, true, queryParams, body, headerParams, formParams);
            return value as byte[];
        }


        private async Task<object> InvokeAPIInternal (string host, string path, string method, bool binaryResponse, Dictionary<String, String> queryParams, object body, Dictionary<String, String> headerParams, Dictionary<String, object> formParams)
        {
            var b = new StringBuilder ();

            foreach (var queryParamItem in queryParams) {
                var value = queryParamItem.Value;
                if (value == null)
                    continue;
                b.Append (b.ToString ().Length == 0 ? "?" : "&");
                b.Append (EscapeString (queryParamItem.Key)).Append ("=").Append (EscapeString (value));
            }

            var querystring = b.ToString ();

            host = host.EndsWith ("/") ? host.Substring (0, host.Length - 1) : host;

            string contentType = "application/json";

            byte[] formData = null;

            if (formParams.Count > 0) {
                string formDataBoundary = String.Format ("----------{0:N}", Guid.NewGuid ());
                formData = GetMultipartFormData (formParams, formDataBoundary);

                contentType = "application/x-www-form-urlencoded";

                StringBuilder data = new StringBuilder ();
                foreach (String key in formParams.Keys) {
                    if (formParams [key] != null)
                        data.AppendFormat ("{0}={1}&", EscapeString (key), EscapeString (formParams [key].ToString ()));
                }
                formData = Encoding.UTF8.GetBytes (data.Remove (data.Length - 1, 1).ToString ());

            }

            HttpClientHandler httpClientHandler = new HttpClientHandler () {
                AllowAutoRedirect = false
            };

            using(var client = new HttpClient (httpClientHandler))
            {
                client.DefaultRequestHeaders.Accept.Add (new MediaTypeWithQualityHeaderValue (contentType));

                HttpMethod httpMethod = new HttpMethod (method);

                HttpRequestMessage request = new HttpRequestMessage (httpMethod, new Uri (host + path + querystring));

                switch (method) {
                case "GET":
                    break;
                case "POST":
                case "PUT":
                case "PATCH":
                case "DELETE":

                    request.Content = new StreamContent (new MemoryStream (formData));

                    break;
                default:
                    throw new ApiException (500, "unknown method type " + method);
                }

                // user agent
                client.DefaultRequestHeaders.Add ("UserAgent", "Xamarin/TestCloud/C#/Sdk/1.0");

                foreach (var headerParamsItem in headerParams) {
                    client.DefaultRequestHeaders.Add (headerParamsItem.Key, headerParamsItem.Value);
                }
                foreach (var defaultHeaderMapItem in defaultHeaderMap.Where(defaultHeaderMapItem => !headerParams.ContainsKey(defaultHeaderMapItem.Key))) {
                    client.DefaultRequestHeaders.Add (defaultHeaderMapItem.Key, defaultHeaderMapItem.Value);
                }
                if (!string.IsNullOrWhiteSpace (apiKey)) {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue ("token", apiKey);
                }

                try {
                    var response = await client.SendAsync (request);
                    int statusCode = (int)response.StatusCode;
                    if (statusCode == 302) {
                        request = new HttpRequestMessage (httpMethod, new Uri (response.Headers.GetValues ("Location").FirstOrDefault ()));
                        using(var client2 = new HttpClient (httpClientHandler))
                        {
                            response = await client2.SendAsync (request);
                            statusCode = (int) response.StatusCode;
                        }
                    }

                    if (!(statusCode >= 200 && statusCode <= 299)) {
                        throw new ApiException ((int)response.StatusCode, response.ReasonPhrase);
                    }

                    if (binaryResponse) {
                        using (var memoryStream = new MemoryStream ()) {
                            var stream = await response.Content.ReadAsStreamAsync ();
                            await stream.CopyToAsync (memoryStream);
                            return memoryStream.ToArray ();
                        }
                    } else {
                        var stream = await response.Content.ReadAsStreamAsync ();
                        using (var responseReader = new StreamReader (stream)) {
                            var responseData = responseReader.ReadToEnd ();
                            return responseData;
                        }
                    }
                } catch (WebException ex) {
                    using (var response = ex.Response as HttpWebResponse) {
                        int statusCode = 0;
                        if (response != null) {
                            statusCode = (int)response.StatusCode;
                        }
                        throw new ApiException (statusCode, ex.Message);
                    }
                }
            }
        }

        private static byte[] GetMultipartFormData(Dictionary<string, object> postParameters, string boundary)
		    {
            byte[] formData = new byte[0]; //Init empty byte[], will be assigned later

            using (Stream formDataStream = new System.IO.MemoryStream())
            {
                bool needsCLRF = false;

                foreach (var param in postParameters)
                {
                    // Thanks to feedback from commenters, add a CRLF to allow multiple parameters to be added.
                    // Skip it on the first parameter, add it to subsequent parameters.
                    if (needsCLRF)
                    formDataStream.Write(Encoding.UTF8.GetBytes("\r\n"), 0, Encoding.UTF8.GetByteCount("\r\n"));

                    needsCLRF = true;

                    if (param.Value is byte[])
                    {
                        string postData = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n",
                        boundary,
                        param.Key,
                        "application/octet-stream");
                        formDataStream.Write(Encoding.UTF8.GetBytes(postData), 0, Encoding.UTF8.GetByteCount(postData));

                        // Write the file data directly to the Stream, rather than serializing it to a string.
                        formDataStream.Write((param.Value as byte[]), 0, (param.Value as byte[]).Length);
                    }
                    else
                    {
                        string postData = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}",
                        boundary,
                        param.Key,
                        param.Value);
                        formDataStream.Write(Encoding.UTF8.GetBytes(postData), 0, Encoding.UTF8.GetByteCount(postData));
                    }
                }

                // Add the end of the request.  Start with a newline
                string footer = "\r\n--" + boundary + "--\r\n";
                formDataStream.Write(Encoding.UTF8.GetBytes(footer), 0, Encoding.UTF8.GetByteCount(footer));

                // Dump the Stream into a byte[]
                formDataStream.Position = 0;
                formData = new byte[formDataStream.Length];
                formDataStream.Read(formData, 0, formData.Length);
              }
              return formData;
        }
    }

    public static class ParamsExtension
    {
        public static void AddParam<T>(this Dictionary<string, string> paramsHash, string name, T value)
        {
            if ( ! EqualityComparer<T>.Default.Equals(value, default(T))) paramsHash.Add(name, ApiInvoker.EscapeString(value.ToString()));
        }

        public static void AddParam<T>(this Dictionary<string, string> paramsHash, string name, T value, T defaultValue)
        {
            if ( ! EqualityComparer<T>.Default.Equals(value, defaultValue)) paramsHash.Add(name, ApiInvoker.EscapeString(value.ToString()));
        }

        public static void AddParam<T>(this Dictionary<string, object> paramsHash, string name, T value)
        {
            if ( ! EqualityComparer<T>.Default.Equals(value, default(T))) paramsHash.Add(name, ApiInvoker.Serialize(value.ToString()));
        }

        public static void AddParam<T>(this Dictionary<string, object> paramsHash, string name, T value, T defaultValue)
        {
            if ( ! EqualityComparer<T>.Default.Equals(value, defaultValue)) paramsHash.Add(name, ApiInvoker.Serialize(value.ToString()));
        }
    }

}

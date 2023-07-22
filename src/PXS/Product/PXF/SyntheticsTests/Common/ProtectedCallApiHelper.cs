namespace Microsoft.Membership.MemberServices.PrivacyExperience.SyntheticsTests.Common
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Newtonsoft.Json;

    internal class ODataResponse<T>
    {
        public List<T> Value { get; set; }
    }

    /// <summary>
    /// Helper class to call a protected API and process its result
    /// </summary>
    public class ProtectedApiCallHelper
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="httpClient">HttpClient used to call the protected API</param>
        /// <param name="telemetryClient">Telemtry client used for logging</param>
        public ProtectedApiCallHelper(HttpClient httpClient, TelemetryClient telemetryClient)
        {
            HttpClient = httpClient;
            this.telemetryClient = telemetryClient;
        }

        protected HttpClient HttpClient { get; private set; }

        protected TelemetryClient telemetryClient { get; private set; }

        /// <summary>
        /// Calls the protected Web API and processes the result
        /// </summary>
        /// <param name="webApiUrl">Url of the Web API to call (supposed to return Json)</param>
        /// <param name="accessToken">Access token used as a bearer security token to call the Web API</param>
        public async Task<IEnumerable<T>> CallGetAsync<T>(string webApiUrl, string accessToken)
        {
            List<T> results = null;
            if (!string.IsNullOrEmpty(accessToken))
            {
                var defaultRequestHeaders = HttpClient.DefaultRequestHeaders;
                if (defaultRequestHeaders.Accept == null || !defaultRequestHeaders.Accept.Any(m => m.MediaType == "application/json"))
                {
                    HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                }
                defaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", accessToken);

                HttpResponseMessage response = await HttpClient.GetAsync(webApiUrl);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<ODataResponse<T>>(json);
                    results = result.Value;
                }
                else
                {
                    string content = await response.Content.ReadAsStringAsync();
                    telemetryClient.TrackTrace($"Failed to call the Web Api: {response.StatusCode}, Content: {content}");
                    
                    // Note that if you got reponse.Code == 403 and reponse.content.code == "Authorization_RequestDenied"
                    // this is because the tenant admin as not granted consent for the application to call the Web API
                }
            }

            return results;
        }

        /// <summary>
        /// Posts to the protected Web API and returns the result
        /// </summary>
        /// <param name="webApiUrl">Url of the Web API to call (supposed to return Json)</param>
        /// <param name="accessToken">Access token used as a bearer security token to call the Web API</param>
        /// <param name="postContent">Content to send in the body of the post call</param>
        public async Task<HttpResponseMessage> CallPostAsync<T>(string webApiUrl, string accessToken, T postContent)
        {
            string result = null;
            HttpResponseMessage response = null;
            if (!string.IsNullOrEmpty(accessToken))
            {
                var defaultRequestHeaders = HttpClient.DefaultRequestHeaders;
                if (defaultRequestHeaders.Accept == null || !defaultRequestHeaders.Accept.Any(m => m.MediaType == "application/json"))
                {
                    HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                }
                defaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", accessToken);

                HttpContent content = new StringContent(JsonConvert.SerializeObject(postContent), Encoding.UTF8, "application/json");
                response = await HttpClient.PostAsync(webApiUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    result = await response.Content.ReadAsStringAsync();
                    telemetryClient.TrackTrace($"Failed to call the Web Api: {response.StatusCode}, Content: {result}");

                    // Note that if you got reponse.Code == 403 and reponse.content.code == "Authorization_RequestDenied"
                    // this is because the tenant admin as not granted consent for the application to call the Web API
                }
            }

            return response;
        }
    }
}

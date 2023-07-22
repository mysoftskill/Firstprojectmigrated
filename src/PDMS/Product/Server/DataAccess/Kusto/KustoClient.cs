namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Kusto
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.DataManagement.Client;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Newtonsoft.Json;

    /// <summary>
    ///     An interface that communicates with database.
    /// </summary>
    public class KustoClient : IKustoClient
    {
        private readonly HttpClient httpClient;

        private readonly string kustoClusterUrl;

        private readonly IKustoClientConfig kustoConfig;

        private readonly IServiceTreeKustoConfiguration serviceTreeKustoConfiguration;

        private readonly ConfidentialCredential credsClient;

        private JsonSerializerSettings jsonSerializerSettings = SerializerSettings.Instance;

        /// <summary>
        ///     Initializes a new instance of the <see cref="KustoClient" /> class.
        /// </summary>
        /// <param name="kustoConfig">The database client.</param>
        /// <param name="httpClient">The http client.</param>
        /// <param name="credsClient">Confidential Credential</param>
        public KustoClient(IKustoClientConfig kustoConfig, HttpClient httpClient, ConfidentialCredential credsClient)
        {
            this.kustoConfig = kustoConfig;
            this.kustoClusterUrl = $@"https://{kustoConfig.KustoCluster}.kusto.windows.net";

            this.httpClient = httpClient;
            this.httpClient.BaseAddress = new Uri(this.kustoClusterUrl);
            this.httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            this.credsClient = credsClient;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="KustoClient" /> class.
        /// </summary>
        /// <param name="kustoConfig">The database client.</param>
        /// <param name="httpClient">The http client.</param>
        /// <param name="credsClient">Confidential Credential</param>
        public KustoClient(IServiceTreeKustoConfiguration kustoConfig, HttpClient httpClient, ConfidentialCredential credsClient)
        {
            this.serviceTreeKustoConfiguration = kustoConfig;
            this.kustoClusterUrl = $@"https://{serviceTreeKustoConfiguration.KustoCluster}.kusto.windows.net";

            this.httpClient = httpClient;
            this.httpClient.BaseAddress = new Uri(this.kustoClusterUrl);
            this.httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            this.credsClient = credsClient;
        }

        /// <summary>
        ///     Get s2s Access Token.
        /// </summary>
        /// <returns>Returns Access Token.</returns>
        public virtual async Task<string> GetS2SAccessToken()
        {
            var scopes = new[] { $"{this.kustoClusterUrl}/.default" };
            var result = await this.credsClient.GetTokenAsync(scopes);
            return result.AccessToken;
        }

        /// <inheritdoc />
        public async Task<IHttpResult<KustoResponse>> QueryAsync(string query)
        {
            var bearerToken = await this.GetS2SAccessToken().ConfigureAwait(false);
            this.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            var serializedObject = this.GetJsonBody(query);

            string url = "/v2/rest/query";
            using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, url))
            {
                requestMessage.Content = new StringContent(serializedObject, Encoding.UTF8, "application/json");

                Stopwatch stopwatch = Stopwatch.StartNew();

                // Send
                HttpResponseMessage httpResponseMessage = await this.httpClient.SendAsync(requestMessage, CancellationToken.None).ConfigureAwait(false);

                stopwatch.Stop();

                using (httpResponseMessage)
                {
                    string responseString = null;
                    if (httpResponseMessage.Content != null)
                    {
                        responseString = await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                    }

                    var responseStatusCode = httpResponseMessage.StatusCode;
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        // If query was successful, extract just the PrimaryResult table and return it
                        List<KustoResponse> response = KustoResponse.FromJson(responseString);
                        KustoResponse kustoResponse = response.FirstOrDefault(a => !string.IsNullOrEmpty(a.TableKind) && a.TableKind.Equals("PrimaryResult", StringComparison.OrdinalIgnoreCase));
                        if (kustoResponse != null)
                        {
                            return new HttpResult<KustoResponse>(
                                responseStatusCode,
                                KustoResponse.ToJson(kustoResponse),
                                httpResponseMessage.Headers,
                                requestMessage.Method,
                                url,
                                serializedObject,
                                stopwatch.ElapsedMilliseconds,
                                "KustoClient.QueryAsync",
                                this.jsonSerializerSettings);
                        }
                        else
                        {
                            // Got an unexpected response from Kusto; convert it to an error response
                            responseStatusCode = System.Net.HttpStatusCode.BadRequest;

                            var data = new Dictionary<string, object>();
                            data["code"] = "Unknown";
                            data["message"] = $"Unexpected output from Kusto: {responseString}.";

                            var responseObj = new Dictionary<string, Dictionary<string, object>>();
                            responseObj["error"] = data;

                            responseString = JsonConvert.SerializeObject(responseObj);
                        }
                    }

                    // Either there was an error calling Kusto, or Kusto returned some unexpected data
                    IHttpResult<ResponseError> result = new HttpResult<ResponseError>(
                                        responseStatusCode,
                                        responseString,
                                        httpResponseMessage.Headers,
                                        requestMessage.Method,
                                        url,
                                        serializedObject,
                                        stopwatch.ElapsedMilliseconds,
                                        "KustoClient.QueryAsync",
                                        this.jsonSerializerSettings);

                    if ((int)result.HttpStatusCode < 500)
                    {
                        throw CallerError.Create(result, 2);
                    }
                    else
                    {
                        throw new ServiceFault(result);
                    }
                }
            }
        }

        private string GetJsonBody(string query)
        {
            var db = this.kustoConfig?.KustoDatabase ?? this.serviceTreeKustoConfiguration?.KustoDatabase;
            var payload = new Dictionary<string, string>
            {
                { "db", db },
                { "csl", query }
            };

            return JsonConvert.SerializeObject(payload, this.jsonSerializerSettings);
        }
    }
}

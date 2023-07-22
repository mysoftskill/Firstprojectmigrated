namespace Microsoft.PrivacyServices.AzureFunctions.Common.DataAccessors
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.Common.Azure;
    using Newtonsoft.Json;

    /// <summary>
    /// Wrapper class for HttpClient
    /// </summary>
    public class HttpClientWrapper : IHttpClientWrapper
    {
        private const string ComponentName = nameof(HttpClientWrapper);

        private readonly string baseAddress;
        private readonly ILogger logger;
        private readonly HttpClient httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientWrapper"/> class.
        /// Construct a new HttpClientWrapper
        /// </summary>
        /// <param name="baseAddress">Base address for this client.</param>
        /// <param name="logger">Implementation of ILogger.</param>
        public HttpClientWrapper(ILogger logger, string baseAddress)
        : this(logger, baseAddress, new HttpClient())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientWrapper"/> class.
        /// </summary>
        /// <param name="baseAddress">Base address for this client.</param>
        /// <param name="client">Httpclient.</param>
        /// <param name="logger">Implementation of ILogger.</param>
        public HttpClientWrapper(ILogger logger, string baseAddress, HttpClient client)
        {
            this.logger = this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.baseAddress = baseAddress ?? throw new ArgumentNullException(nameof(baseAddress));
            this.httpClient = client ?? throw new ArgumentNullException(nameof(client));
            this.httpClient.BaseAddress = new Uri(baseAddress);

            var defaultRequestHeaders = this.httpClient.DefaultRequestHeaders;
            string httpAcceptHeaderValue = @"application/json;odata.metadata=minimal";

            defaultRequestHeaders.Add("Accept", httpAcceptHeaderValue);
        }

        /// <summary>
        /// Calls the protected Web API
        /// </summary>
        /// <param name="apiUrl">Url of the Web API to call (supposed to return Json).</param>
        /// <param name="accessTokenFunc">Function to get access token used as a bearer security token to call the Web API.</param>
        /// <typeparam name="T">Type of result.</typeparam>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<T> GetAsync<T>(string apiUrl, Func<Task<string>> accessTokenFunc)
        {
            T result = default;
            string accessToken = await accessTokenFunc().ConfigureAwait(false);
            if (!string.IsNullOrEmpty(accessToken))
            {
                this.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                this.logger.Information(ComponentName, $"{nameof(this.GetAsync)}: Calling Web Api: {this.baseAddress}/{apiUrl}");
                try
                {
                    using (HttpResponseMessage response = await this.httpClient.GetAsync(apiUrl).ConfigureAwait(false))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                            this.logger.Information(ComponentName, $"{nameof(this.GetAsync)}: Content: {content}");

                            result = JsonConvert.DeserializeObject<T>(content);

                            this.logger.Information(ComponentName, $"{nameof(this.GetAsync)}: json result: {result}");
                        }
                        else
                        {
                            // Note that if you get reponse.Code == 403 and reponse.content.code == "Authorization_RequestDenied"
                            // this is because the tenant admin has not granted consent for the application to call the Web API
                            this.logger.Information(ComponentName, $"{nameof(this.GetAsync)}: Call to Web Api failed: {response.StatusCode}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.logger.Error(ComponentName, $"{nameof(this.GetAsync)}: Exception: {ex}");
                    throw;
                }
            }

            return result;
        }

        /// <summary>
        /// Update the protected Web API
        /// </summary>
        /// <param name="httpMethod">HttpMethod to use for the update.).</param>
        /// <param name="apiUrl">Url of the Web API to call (supposed to return Json).</param>
        /// <param name="accessTokenFunc">Function to provide access token used as a bearer security token to call the Web API.</param>
        /// <param name="payload">The content to be updated.</param>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<T> UpdateAsync<T>(HttpMethod httpMethod, string apiUrl, Func<Task<string>> accessTokenFunc, T payload)
        {
            T result = default;
            string accessToken = await accessTokenFunc().ConfigureAwait(false);
            if (!string.IsNullOrEmpty(accessToken))
            {
                this.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                this.logger.Information(ComponentName, $"{nameof(this.UpdateAsync)}: Calling Web Api: {this.baseAddress}/{apiUrl}");
                try
                {
                    using (HttpRequestMessage requestMessage = new HttpRequestMessage(httpMethod, apiUrl))
                    {
                        string serializedObject = string.Empty;
                        if (payload != null)
                        {
                            serializedObject = JsonConvert.SerializeObject(payload);
                            requestMessage.Content = new StringContent(serializedObject, System.Text.Encoding.UTF8, "application/json");
                        }

                        using (HttpResponseMessage response = await this.httpClient.SendAsync(requestMessage).ConfigureAwait(false))
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                                this.logger.Information(ComponentName, $"{nameof(this.UpdateAsync)}: Content: {content}");

                                result = JsonConvert.DeserializeObject<T>(content);
                                this.logger.Information(ComponentName, $"{nameof(this.UpdateAsync)}: json result: {result}");
                            }
                            else
                            {
                                // Note that if you get reponse.Code == 403 and reponse.content.code == "Authorization_RequestDenied"
                                // this is because the tenant admin has not granted consent for the application to call the Web API
                                this.logger.Information(ComponentName, $"{nameof(this.UpdateAsync)}: Call to Web Api failed: {response.StatusCode}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.logger.Error(ComponentName, $"{nameof(this.UpdateAsync)}: Exception: {ex}");
                    throw;
                }
            }

            return result;
        }

        /// <inheritdoc/>
        public Task<HttpResponseMessage> PostAsync(string apiUrl, Func<Task<string>> accessTokenFunc, string etag)
        {
            return this.RequestWithEtagAsync(HttpMethod.Post, apiUrl, accessTokenFunc, etag);
        }

        /// <inheritdoc/>
        public Task<HttpResponseMessage> DeleteAsync(string apiUrl, Func<Task<string>> accessTokenFunc, string etag)
        {
            return this.RequestWithEtagAsync(HttpMethod.Delete, apiUrl, accessTokenFunc, etag);
        }

        private async Task<HttpResponseMessage> RequestWithEtagAsync(HttpMethod httpMethod, string apiUrl, Func<Task<string>> accessTokenFunc, string etag)
        {
            if (httpMethod == null)
            {
                throw new ArgumentException("HttpMethod missing");
            }

            if (apiUrl == null)
            {
                throw new ArgumentException("ApiUrl missing");
            }

            string accessToken = await accessTokenFunc().ConfigureAwait(false);
            if (!string.IsNullOrEmpty(accessToken))
            {
                this.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                this.logger.Information(ComponentName, $"{httpMethod.Method}Async: Calling Web Api: {this.baseAddress}/{apiUrl}");
                try
                {
                    HttpResponseMessage response;
                    using (HttpRequestMessage requestMessage = new HttpRequestMessage(httpMethod, apiUrl))
                    {
                        if (!string.IsNullOrWhiteSpace(etag))
                        {
                            requestMessage.Headers.Add("If-Match", etag);
                        }

                        response = await this.httpClient.SendAsync(requestMessage).ConfigureAwait(false);
                    }

                    return response;
                }
                catch (Exception ex)
                {
                    this.logger.Error(ComponentName, $"{httpMethod.Method}: Exception: {ex}");
                    throw;
                }
            }
            else
            {
                throw new ArgumentException("Unable to get access token");
            }
        }
    }
}

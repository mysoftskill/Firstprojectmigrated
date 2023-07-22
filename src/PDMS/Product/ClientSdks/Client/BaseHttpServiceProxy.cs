namespace Microsoft.PrivacyServices.DataManagement.Client
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Newtonsoft.Json;

    /// <summary>
    /// Establishes a set of base behaviors for all service proxy implementations.
    /// </summary>
    public abstract class BaseHttpServiceProxy : IHttpServiceProxy, IDisposable
    {
        private bool disposed = false; // To detect redundant dispose calls.
        private HttpClient httpClient;
        private readonly JsonSerializerSettings jsonSerializerSettings = SerializerSettings.Instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseHttpServiceProxy" /> class.
        /// </summary>
        /// <param name="httpClient">An http client to issue requests against.</param>
        public BaseHttpServiceProxy(HttpClient httpClient)
        {
            this.httpClient = httpClient;

            string httpAcceptHeaderValue = @"application/json;odata.metadata=minimal";

            this.httpClient.DefaultRequestHeaders.Add("Accept", httpAcceptHeaderValue);
        }

        /// <summary>
        /// Issues a GET call.
        /// </summary>
        /// <typeparam name="TResponse">The data type of the response.</typeparam>
        /// <param name="url">The url of the request, relative to the base address.</param>
        /// <param name="additionalHeaders">Additional headers specific for this request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response as an <see cref="IHttpResult{TResponse}" /></returns>
        public Task<IHttpResult<TResponse>> GetAsync<TResponse>(
            string url,
            IDictionary<string, Func<Task<string>>> additionalHeaders,
            CancellationToken cancellationToken)
        {
            return this.InvokeAsync<object, TResponse>(url, HttpMethod.Get, null, additionalHeaders, cancellationToken);
        }

        /// <summary>
        /// Issues a POST call.
        /// </summary>
        /// <typeparam name="TRequest">The type for the request data.</typeparam>
        /// <typeparam name="TResponse">The data type of the response.</typeparam>
        /// <param name="url">The url of the request, relative to the base address.</param>
        /// <param name="payload">The data to send to the service.</param>
        /// <param name="additionalHeaders">Additional headers specific for this request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response as an <see cref="IHttpResult{TResponse}" /></returns>
        public Task<IHttpResult<TResponse>> PostAsync<TRequest, TResponse>(
            string url,
            TRequest payload,
            IDictionary<string, Func<Task<string>>> additionalHeaders,
            CancellationToken cancellationToken)
        {
            return this.InvokeAsync<TRequest, TResponse>(url, HttpMethod.Post, payload, additionalHeaders, cancellationToken);
        }

        /// <summary>
        /// Issues a DELETE call.
        /// </summary>
        /// <param name="url">The url of the request, relative to the base address.</param>
        /// <param name="payload">The data to send to the service.</param>
        /// <param name="additionalHeaders">Additional headers specific for this request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response as an <see cref="IHttpResult" /></returns>
        public async Task<IHttpResult> DeleteAsync<TRequest>(
            string url,
            TRequest payload,
            IDictionary<string, Func<Task<string>>> additionalHeaders,
            CancellationToken cancellationToken)
        {
            return await this.InvokeAsync<object, object>(url, HttpMethod.Delete, payload, additionalHeaders, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Issues a DELETE call.
        /// </summary>
        /// <param name="url">The url of the request, relative to the base address.</param>
        /// <param name="additionalHeaders">Additional headers specific for this request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response as an <see cref="IHttpResult" /></returns>
        public async Task<IHttpResult> DeleteAsync(
            string url,
            IDictionary<string, Func<Task<string>>> additionalHeaders,
            CancellationToken cancellationToken)
        {
            return await this.InvokeAsync<object, object>(url, HttpMethod.Delete, null, additionalHeaders, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Issues a PUT call.
        /// </summary>
        /// <typeparam name="TRequest">The type for the request data.</typeparam>
        /// <typeparam name="TResponse">The data type of the response.</typeparam>
        /// <param name="url">The url of the request, relative to the base address.</param>
        /// <param name="payload">The data to send to the service.</param>
        /// <param name="additionalHeaders">Additional headers specific for this request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response as an <see cref="IHttpResult" /></returns>
        public Task<IHttpResult<TResponse>> PutAsync<TRequest, TResponse>(
            string url,
            TRequest payload,
            IDictionary<string, Func<Task<string>>> additionalHeaders,
            CancellationToken cancellationToken)
        {
            return this.InvokeAsync<TRequest, TResponse>(url, new HttpMethod("PUT"), payload, additionalHeaders, cancellationToken);
        }

        /// <summary>
        /// Disposes the object and it's dependencies.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>
        /// Disposes the object only once.
        /// </summary>
        /// <param name="disposing">Whether or not to dispose the dependencies.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    if (this.httpClient != null)
                    {
                        this.httpClient.Dispose();
                        this.httpClient = null;
                    }
                }

                this.disposed = true;
            }
        }

        /// <summary>
        /// Issues a call to the service.
        /// </summary>
        /// <typeparam name="TRequest">The type for the request data.</typeparam>
        /// <typeparam name="TResponse">The data type of the response.</typeparam>
        /// <param name="url">The url of the request, relative to the base address.</param>
        /// <param name="httpMethod">The http method for the request.</param>
        /// <param name="payload">The data to send to the service.</param>
        /// <param name="additionalHeaders">Additional headers specific for this request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response as an <see cref="IHttpResult{TResponse}" /></returns>
        private async Task<IHttpResult<TResponse>> InvokeAsync<TRequest, TResponse>(
            string url,
            HttpMethod httpMethod,
            TRequest payload,
            IDictionary<string, Func<Task<string>>> additionalHeaders,
            CancellationToken cancellationToken)
        {
            // If the base address has a portion of the path, 
            // then we need to concatenate the values manually.
            // Otherwise, the path value is stripped by the HttpRequestMessage.
            var finalUrl = new Uri(this.httpClient.BaseAddress.AbsoluteUri + url);

            if (this.httpClient.BaseAddress.AbsoluteUri.EndsWith("/") && url.StartsWith("/"))
            {
                finalUrl = new Uri(this.httpClient.BaseAddress.AbsoluteUri + url.TrimStart('/'));
            }

            using (HttpRequestMessage requestMessage = new HttpRequestMessage(httpMethod, finalUrl))
            {
                string operationName = OperationNameProvider.GetFromPathAndQuery(httpMethod.ToString(), url);
                requestMessage.Properties["OperationNameKey"] = operationName; // This is something we have agreed to do for the UX team.

                string serializedObject = string.Empty;
                
                if (payload != null)
                {
                    serializedObject = JsonConvert.SerializeObject(payload, this.jsonSerializerSettings);
                    requestMessage.Content = new StringContent(serializedObject, System.Text.Encoding.UTF8, "application/json");
                }
                
                if (additionalHeaders != null)
                {
                    foreach (var header in additionalHeaders)
                    {
                        string headerVaule = await header.Value().ConfigureAwait(false);
                        requestMessage.Headers.Add(header.Key, headerVaule);
                    }
                }

                Stopwatch stopwatch = Stopwatch.StartNew();

                // Send
                HttpResponseMessage httpResponseMessage = await this.httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);

                stopwatch.Stop();

                using (httpResponseMessage)
                {
                    string responseString = null;
                    if (httpResponseMessage.Content != null)
                    {
                        responseString = await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                    }

                    // TODO : Adding this condition here because adding this condition in ServiceTree Client will force us to change API models and return type.
                    // This needs to be moved to ServiceTree client in the future
                    if(httpResponseMessage.IsSuccessStatusCode && url.Contains("SearchServiceHierarchyByKeyword"))
                    {
                        responseString = SearchConverter.ConvertSearchV2ResponseToSearchV1Response(responseString);
                    }

                    return new HttpResult<TResponse>(
                        httpResponseMessage.StatusCode,
                        responseString,
                        httpResponseMessage.Headers,
                        requestMessage.Method,
                        url,
                        serializedObject,
                        stopwatch.ElapsedMilliseconds,
                        operationName,
                        this.jsonSerializerSettings);
                }
            }
        }
    }
}

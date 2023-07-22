// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.DataManagementAdapter
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers;
    using Microsoft.OSGS.HttpClientCommon;
    using Microsoft.PrivacyServices.DataManagement.Client;
    using Microsoft.Windows.Services.AuthN.Client.S2S;

    using Newtonsoft.Json;

    /// <summary>
    ///     DataManagement-HttpService-Proxy
    /// </summary>
    public class DataManagementHttpServiceProxy : IHttpServiceProxy, IDisposable
    {
        private const string PartnerId = "DataManagementService";

        private readonly JsonSerializerSettings jsonSerializerSettings = SerializerSettings.Instance;

        private readonly IS2SAuthClient s2sAuthClient;

        private readonly IAadAuthManager aadAuthManager;

        private readonly IPrivacyPartnerAdapterConfiguration partnerConfig;

        private IHttpClient httpClient;

        private bool disposed; // To detect redundant dispose calls.

        /// <summary>
        /// Initializes a new instance of the <see cref="DataManagementHttpServiceProxy" /> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="s2sAuthClient">The s2s authentication client.</param>
        /// <param name="partnerConfig">The partner configuration.</param>
        public DataManagementHttpServiceProxy(
            IHttpClient httpClient, 
            IS2SAuthClient s2sAuthClient, 
            IPrivacyPartnerAdapterConfiguration partnerConfig)
        {
            this.httpClient = httpClient;
            this.s2sAuthClient = s2sAuthClient;
            this.partnerConfig = partnerConfig;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataManagementHttpServiceProxy" /> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="aadAuthManager">The AAD application authentication client.</param>
        /// <param name="partnerConfig">The partner configuration.</param>
        public DataManagementHttpServiceProxy(
            IHttpClient httpClient, 
            IAadAuthManager aadAuthManager, 
            IPrivacyPartnerAdapterConfiguration partnerConfig)
        {
            this.httpClient = httpClient;
            this.aadAuthManager = aadAuthManager;
            this.partnerConfig = partnerConfig;
        }

        /// <summary>
        ///     Issues a DELETE call.
        /// </summary>
        /// <param name="url">The url of the request, relative to the base address.</param>
        /// <param name="additionalHeaders">Additional headers specific for this request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response as an <see cref="HttpResult" /></returns>
        public async Task<IHttpResult> DeleteAsync(
            string url, 
            IDictionary<string, 
            Func<Task<string>>> additionalHeaders, 
            CancellationToken cancellationToken)
        {
            return await this
                .InvokeAsync<object, object>(url, HttpMethod.Delete, null, additionalHeaders, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        ///     Issues a GET call.
        /// </summary>
        /// <typeparam name="TResponse">The data type of the response.</typeparam>
        /// <param name="url">The url of the request, relative to the base address.</param>
        /// <param name="additionalHeaders">Additional headers specific for this request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response as an <see cref="HttpResult{TResponse}" /></returns>
        public Task<IHttpResult<TResponse>> GetAsync<TResponse>(
            string url, 
            IDictionary<string, Func<Task<string>>> additionalHeaders, 
            CancellationToken cancellationToken)
        {
            return this.InvokeAsync<object, TResponse>(url, HttpMethod.Get, null, additionalHeaders, cancellationToken);
        }

        /// <summary>
        ///     Issues a POST call.
        /// </summary>
        /// <typeparam name="TRequest">The type for the request data.</typeparam>
        /// <typeparam name="TResponse">The data type of the response.</typeparam>
        /// <param name="url">The url of the request, relative to the base address.</param>
        /// <param name="payload">The data to send to the service.</param>
        /// <param name="additionalHeaders">Additional headers specific for this request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response as an <see cref="HttpResult{TResponse}" /></returns>
        public Task<IHttpResult<TResponse>> PostAsync<TRequest, TResponse>(
            string url,
            TRequest payload,
            IDictionary<string, Func<Task<string>>> additionalHeaders,
            CancellationToken cancellationToken)
        {
            return this.InvokeAsync<TRequest, TResponse>(url, HttpMethod.Post, payload, additionalHeaders, cancellationToken);
        }

        /// <summary>
        ///     Issues a PUT call.
        /// </summary>
        /// <typeparam name="TRequest">The type for the request data.</typeparam>
        /// <typeparam name="TResponse">The data type of the response.</typeparam>
        /// <param name="url">The url of the request, relative to the base address.</param>
        /// <param name="payload">The data to send to the service.</param>
        /// <param name="additionalHeaders">Additional headers specific for this request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response as an <see cref="HttpResult" /></returns>
        public Task<IHttpResult<TResponse>> PutAsync<TRequest, TResponse>(
            string url,
            TRequest payload,
            IDictionary<string, Func<Task<string>>> additionalHeaders,
            CancellationToken cancellationToken)
        {
            return this.InvokeAsync<TRequest, TResponse>(url, new HttpMethod("PUT"), payload, additionalHeaders, cancellationToken);
        }

        /// <summary>
        ///     Disposes the object and it's dependencies.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>
        ///     Disposes the object only once.
        /// </summary>
        /// <param name="disposing">Whether or not to dispose the dependencies.</param>
        private void Dispose(bool disposing)
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
        ///     Issues a call to the service.
        /// </summary>
        /// <typeparam name="TRequest">The type for the request data.</typeparam>
        /// <typeparam name="TResponse">The data type of the response.</typeparam>
        /// <param name="url">The url of the request, relative to the base address.</param>
        /// <param name="httpMethod">The http method for the request.</param>
        /// <param name="payload">The data to send to the service.</param>
        /// <param name="additionalHeaders">Additional headers specific for this request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        ///     The response as an <see cref="HttpResult{TResponse}" />
        /// </returns>
        private async Task<IHttpResult<TResponse>> InvokeAsync<TRequest, TResponse>(
            string url,
            HttpMethod httpMethod,
            TRequest payload,
            IDictionary<string, Func<Task<string>>> additionalHeaders,
            CancellationToken cancellationToken)
        {
            var absoluteUri = new Uri(this.httpClient.BaseAddress, url);

            string operationName = OperationNameProvider.GetFromPathAndQuery(httpMethod.ToString(), url);
            bool authHeaderProvided = false;

            OutgoingApiEventWrapper outgoingApiEvent = OutgoingApiEventWrapper.CreateHttpEventWithPuid(
                PartnerId,
                operationName,
                operationVersion: string.Empty,
                requestUri: absoluteUri,
                requestMethod: httpMethod,
                userPuid: null); // s2s only (no user context)

            using (HttpRequestMessage requestMessage = 
                HttpExtensions.CreateHttpRequestMessage(absoluteUri, httpMethod, outgoingApiEvent, null))
            {
                string serializedObject = string.Empty;

                if (payload != null)
                {
                    serializedObject = JsonConvert.SerializeObject(payload, this.jsonSerializerSettings);
                    requestMessage.Content = new StringContent(serializedObject, Encoding.UTF8, "application/json");
                }

                requestMessage.Headers.Add("Accept", "application/json; odata.metadata=none");

                if (additionalHeaders != null)
                {
                    foreach (KeyValuePair<string, Func<Task<string>>> header in additionalHeaders)
                    {
                        string headerValue = await header.Value().ConfigureAwait(false);
                        requestMessage.Headers.Add(header.Key, headerValue);

                        authHeaderProvided = authHeaderProvided || "Authorization".Equals(header.Key);
                    }
                }

                // if the extra headers already provided us with an auth header, then no need to generate another one
                if (authHeaderProvided == false)
                {
                    if (this.s2sAuthClient != null)
                    {
                        string appToken = await this.s2sAuthClient
                            .GetAccessTokenAsync(this.partnerConfig.MsaS2STargetSite, CancellationToken.None)
                            .ConfigureAwait(false);

                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("MSAAuth1.0", $"apptoken={appToken}");
                    }
                    else if (this.aadAuthManager != null)
                    {
                        string appToken = await this.aadAuthManager
                            .GetAccessTokenAsync(this.partnerConfig.AadTokenResourceId)
                            .ConfigureAwait(false);

                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", appToken);
                    }
                    else
                    {
                        const string Msg =
                            "DataManagementHttpServiceProxy was not passed an AAD or MSA S2S auth provider and the caller did " +
                            "not provide an auth header in the extra headers eleemnt";

                        throw new InvalidOperationException(Msg);
                    }
                }

                Stopwatch stopwatch = Stopwatch.StartNew();

                // Send
                HttpResponseMessage httpResponseMessage = await this.httpClient
                    .SendAsync(requestMessage, cancellationToken)
                    .ConfigureAwait(false);

                stopwatch.Stop();

                using (httpResponseMessage)
                {
                    string responseString = null;
                    if (httpResponseMessage.Content != null)
                    {
                        responseString = await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                    }

                    try
                    {
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
                    catch (JsonException e)
                    {
                        e.Data["DataManagmentClient.RawResponse"] = responseString;
                        throw;
                    }
                }
            }
        }

        // Not Implemented
        Task<IHttpResult> IHttpServiceProxy.DeleteAsync<TRequest>(string url, TRequest payload, IDictionary<string, Func<Task<string>>> additionalHeaders, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}

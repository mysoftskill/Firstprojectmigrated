// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Formatting;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;

    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Converters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Handlers;
    using Microsoft.OSGS.HttpClientCommon;
    using Microsoft.Windows.Services.AuthN.Client.S2S;

    using Newtonsoft.Json;

    /// <summary>
    ///     Http helper extension methods
    /// </summary>
    internal static class HttpExtensions
    {
        public const string FamilyJwtHeader = "X-Family-Json-Web-Token";

        public const string S2STokenHeader = "X-S2S-Access-Token";

        private const string ApplicationJson = "application/json";

        /// <summary>
        ///     Adds the query parameters to a new instance of Uri
        /// </summary>
        /// <param name="uri">The URI to add parameters to.</param>
        /// <param name="queryParameters">The query parameters.</param>
        /// <returns>A new Uri with the additional query parameters</returns>
        public static Uri AddQueryParameters(this Uri uri, IList<string> queryParameters)
        {
            if (queryParameters == null || !queryParameters.Any())
            {
                return uri;
            }

            NameValueCollection query = HttpUtility.ParseQueryString(uri.Query);
            foreach (string[] paramPair in queryParameters.Select(s => s.Split(new[] { '=' }, 2)))
            {
                query.Add(paramPair[0], paramPair[1]);
            }

            var builder = new UriBuilder(uri) { Query = query.ToString() };
            return builder.Uri;
        }

        /// <summary>
        ///     Expands a base URI with the relative path and query parameters
        /// </summary>
        /// <param name="baseUriInput">Base URI</param>
        /// <param name="relativePath">Relative URI</param>
        /// <param name="queryParameters">Query parameters</param>
        /// <returns>Uri</returns>
        public static Uri ExpandUri(this string baseUriInput, string relativePath, QueryStringCollection queryParameters)
        {
            // Ensure that there is a trailing slash in the base uri
            baseUriInput = baseUriInput.Trim();
            baseUriInput = baseUriInput.TrimEnd('/', '\\');
            baseUriInput = baseUriInput + "/";

            // Remove leading or trailing slash from relative path (new Uri has some non-obvious behaviors if these slashes are there)
            relativePath = relativePath.Trim(' ', '\t', '/');

            var baseUri = new Uri(baseUriInput);
            var sb = new StringBuilder(relativePath);
            if (queryParameters.Count > 0)
            {
                sb.Append("?");
            }

            bool first = true;
            foreach (string name in queryParameters.AllKeys)
            {
                if (!first)
                {
                    sb.Append("&");
                }

                sb.AppendFormat("{0}={1}", name, queryParameters[name]);
                first = false;
            }

            return new Uri(baseUri, sb.ToString());
        }

        /// <summary>
        ///     Performs a GET operation and handles the response
        /// </summary>
        /// <typeparam name="TResult">Expected response type</typeparam>
        /// <param name="httpClient">Http client</param>
        /// <param name="requestUri">Full request URI</param>
        /// <param name="requestContext">Request context</param>
        /// <param name="s2sAuthClient">S2S client</param>
        /// <param name="targetSite">Partner target site</param>
        /// <param name="partnerConfiguration"></param>
        /// <param name="outgoingApiEvent">The outgoing API event.</param>
        /// <param name="customHeaders">The custom headers.</param>
        /// <returns>Response object</returns>
        /// <exception cref="HttpException">Thrown on any errors</exception>
        public static async Task<TResult> GetAsync<TResult>(
            this IHttpClient httpClient,
            Uri requestUri,
            IPxfRequestContext requestContext,
            IS2SAuthClient s2sAuthClient,
            string targetSite,
            IPxfPartnerConfiguration partnerConfiguration,
            OutgoingApiEventWrapper outgoingApiEvent,
            IDictionary<string, string> customHeaders)
            where TResult : class
        {
            requestContext.ThrowOnNull("requestContext");

            HttpRequestMessage requestMessage = await CreateHttpRequestMessageAsync<object>(
                    requestUri,
                    requestContext,
                    s2sAuthClient,
                    targetSite,
                    HttpMethod.Get,
                    outgoingApiEvent,
                    customHeaders)
                .ConfigureAwait(false);

            HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead)
                .ConfigureAwait(false);

            return await HandleResponseAsync<TResult>(partnerConfiguration, responseMessage).ConfigureAwait(false);
        }

        /// <summary>
        ///     Performs a GET operation and handles the response
        /// </summary>
        /// <typeparam name="TResult">Expected response type</typeparam>
        /// <param name="httpClient">Http client</param>
        /// <param name="requestUri">Full request URI</param>
        /// <param name="aadTokenProvider">AAD token provider</param>
        /// <param name="aadTokenPartnerConfig">config for requesting AAD tokens for an adapter</param>
        /// <param name="requestContext">Request context</param>
        /// <param name="outgoingApiEvent">The outgoing API event.</param>
        /// <param name="partnerConfiguration">Partner configuration</param>
        /// <param name="customHeaders">The custom headers.</param>
        /// <param name="schema">
        ///     Part of the URL to be used in partner signature. For example, if the url is https://bing.com/p/i/v2/amc/api/userfeaturelist
        ///     then the schema will be "amc/api/userfeaturelist". API owner will provide this.
        /// </param>
        /// <returns>Response object</returns>
        /// <exception cref="HttpException">Thrown on any errors</exception>
        public static async Task<TResult> GetAsync<TResult>(
            this IHttpClient httpClient,
            Uri requestUri,
            IAadTokenProvider aadTokenProvider,
            AadTokenPartnerConfig aadTokenPartnerConfig,
            IPxfRequestContext requestContext,
            OutgoingApiEventWrapper outgoingApiEvent,
            IPxfPartnerConfiguration partnerConfiguration,
            IDictionary<string, string> customHeaders,
            string schema = "")
            where TResult : class
        {
            requestContext.ThrowOnNull("requestContext");

            HttpRequestMessage requestMessage = await CreateHttpRequestMessageAsync<object>(
                requestUri,
                aadTokenProvider,
                aadTokenPartnerConfig,
                requestContext,
                partnerConfiguration,
                HttpMethod.Get,
                outgoingApiEvent,
                customHeaders,
                schema).ConfigureAwait(false);

            HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead)
                .ConfigureAwait(false);

            return await HandleResponseAsync<TResult>(partnerConfiguration, responseMessage).ConfigureAwait(false);
        }

        /// <summary>
        ///     Performs a GET operation and handles the response, even if there is no content
        /// </summary>
        /// <param name="httpClient">Http client</param>
        /// <param name="requestUri">Full request URI</param>
        /// <param name="requestContext">Request context</param>
        /// <param name="outgoingApiEvent">The outgoing API event.</param>
        /// <param name="partnerConfiguration">Partner configuration</param>
        /// <param name="customHeaders">The custom headers.</param>
        /// <param name="schema"> </param>
        /// <returns>Response object</returns>
        /// <exception cref="HttpException">Thrown on any errors</exception>
        public static async Task<TResult> GetAsync<TResult>(
            this IHttpClient httpClient,
            Uri requestUri,
            IPxfRequestContext requestContext,
            OutgoingApiEventWrapper outgoingApiEvent,
            IPxfPartnerConfiguration partnerConfiguration,
            IDictionary<string, string> customHeaders,
            string schema = null)
            where TResult : class
        {
            requestContext.ThrowOnNull(nameof(requestContext));

            HttpRequestMessage requestMessage = CreateHttpRequestMessage(
                requestUri,
                requestContext,
                HttpMethod.Get,
                outgoingApiEvent,
                customHeaders,
                schema);

            HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead)
                .ConfigureAwait(false);

            if (responseMessage.IsSuccessStatusCode)
            {
                TResult response = null;
                if (responseMessage.Content != null)
                {
                    string responseAsString = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                    response = JsonConvert.DeserializeObject<TResult>(responseAsString);
                }
                return response;
            }

            await HandleResponseErrorAsync(partnerConfiguration, responseMessage).ConfigureAwait(false);

            return null;
        }

        /// <summary>
        ///     Performs a POST operation and handles the response
        /// </summary>
        /// <typeparam name="T">The type of the content to post.</typeparam>
        /// <param name="httpClient">Http client</param>
        /// <param name="requestUri">Full request URI</param>
        /// <param name="requestContext">Request context</param>
        /// <param name="partnerConfiguration"></param>
        /// <param name="s2sAuthClient">S2S client</param>
        /// <param name="targetSite">Partner target site</param>
        /// <param name="outgoingApiEvent">The outgoing API event.</param>
        /// <param name="customHeaders">The custom headers.</param>
        /// <param name="postObject">The post object.</param>
        /// <returns>Response object</returns>
        /// <exception cref="HttpException">Thrown if response was not success</exception>
        public static async Task PostAsync<T>(
            this IHttpClient httpClient,
            Uri requestUri,
            IPxfRequestContext requestContext,
            IPxfPartnerConfiguration partnerConfiguration,
            IS2SAuthClient s2sAuthClient,
            string targetSite,
            OutgoingApiEventWrapper outgoingApiEvent,
            IDictionary<string, string> customHeaders,
            T postObject)
            where T : class
        {
            requestContext.ThrowOnNull("requestContext");

            HttpRequestMessage requestMessage = await CreateHttpRequestMessageAsync(
                requestUri,
                requestContext,
                s2sAuthClient,
                targetSite,
                HttpMethod.Post,
                outgoingApiEvent,
                customHeaders,
                postObject).ConfigureAwait(false);

            await PostAsync(httpClient, partnerConfiguration, requestMessage).ConfigureAwait(false);
        }

        /// <summary>
        ///     Performs a POST operation and handles the response
        /// </summary>
        /// <typeparam name="T">The type of the content to post.</typeparam>
        /// <param name="httpClient">Http client</param>
        /// <param name="requestUri">Full request URI</param>
        /// <param name="aadTokenProvider">AAD token provider</param>
        /// <param name="aadTokenPartnerConfig">config for requesting AAD tokens for an adapter</param>
        /// <param name="requestContext">Request context</param>
        /// <param name="partnerConfiguration"></param>
        /// <param name="outgoingApiEvent">The outgoing API event.</param>
        /// <param name="customHeaders">The custom headers.</param>
        /// <param name="schema">
        ///     Part of the URL to be used in partner signature. For example, if the url is https://bing.com/p/i/v2/amc/api/userfeaturelist
        ///     then the schema will be "amc/api/userfeaturelist". API owner will provide this.
        /// </param>
        /// <param name="postObject">The post object.</param>
        /// <returns>Response object</returns>
        /// <exception cref="HttpException">Thrown if response was not success</exception>
        public static async Task PostAsync<T>(
            this IHttpClient httpClient,
            Uri requestUri,
            IAadTokenProvider aadTokenProvider,
            AadTokenPartnerConfig aadTokenPartnerConfig,
            IPxfRequestContext requestContext,
            IPxfPartnerConfiguration partnerConfiguration,
            OutgoingApiEventWrapper outgoingApiEvent,
            IDictionary<string, string> customHeaders,
            string schema = "",
            T postObject = null)
            where T : class
        {
            requestContext.ThrowOnNull("requestContext");

            HttpRequestMessage requestMessage = await CreateHttpRequestMessageAsync(
                requestUri,
                aadTokenProvider,
                aadTokenPartnerConfig,
                requestContext,
                partnerConfiguration,
                HttpMethod.Post,
                outgoingApiEvent,
                customHeaders,
                schema,
                postObject).ConfigureAwait(false);

            await PostAsync(httpClient, partnerConfiguration, requestMessage).ConfigureAwait(false);
        }

        internal static HttpRequestMessage CreateHttpRequestMessage(
            Uri requestUri,
            HttpMethod httpMethod,
            OutgoingApiEventWrapper outgoingApiEvent,
            IDictionary<string, string> customHeaders)
        {
            return CreateHttpRequestMessage<object>(requestUri, httpMethod, outgoingApiEvent, customHeaders, null);
        }

        internal static HttpRequestMessage CreateHttpRequestMessage<T>(
            Uri requestUri,
            HttpMethod httpMethod,
            OutgoingApiEventWrapper outgoingApiEvent,
            IDictionary<string, string> customHeaders,
            T postObject = null)
            where T : class
        {
            outgoingApiEvent.ThrowOnNull("outgoingApiEvent");

            var requestMessage = new HttpRequestMessage(httpMethod, requestUri);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(ApplicationJson));

            if (postObject != null)
            {
                requestMessage.Content = new ObjectContent<T>(postObject, new JsonMediaTypeFormatter());
            }

            // Note: CV is set and incremented in the OutgoingRequestHandler
            requestMessage.Properties.Add(OutgoingRequestHandler.CounterInstanceNameKey, outgoingApiEvent.DependencyOperationName);
            requestMessage.Properties.Add(OutgoingRequestHandler.ApiEventContextKey, outgoingApiEvent);

            // add custom headers
            if (customHeaders != null)
            {
                foreach (KeyValuePair<string, string> customHeader in customHeaders)
                {
                    if (customHeader.Key.Equals("Authorization"))
                    {
                        requestMessage.Headers.TryAddWithoutValidation(customHeader.Key, customHeader.Value);
                    }
                    else
                    {
                        requestMessage.Headers.Add(customHeader.Key, customHeader.Value);
                    }
                }
            }

            return requestMessage;
        }

        internal static async Task<HttpRequestMessage> CreateHttpRequestMessageAsync<T>(
            Uri requestUri,
            IPxfRequestContext requestContext,
            IS2SAuthClient s2sAuthClient,
            string targetSite,
            HttpMethod httpMethod,
            OutgoingApiEventWrapper outgoingApiEvent,
            IDictionary<string, string> customHeaders,
            T postObject = null)
            where T : class
        {
            s2sAuthClient.ThrowOnNull("s2sAuthClient");

            HttpRequestMessage requestMessage =
                CreateHttpRequestMessage(requestUri, httpMethod, outgoingApiEvent, customHeaders, postObject);

            string s2sToken = await s2sAuthClient.GetAccessTokenAsync(targetSite, CancellationToken.None).ConfigureAwait(false);
            requestMessage.Headers.Add(S2STokenHeader, s2sToken);

            if (requestContext != null)
            {
                if (requestContext.UserProxyTicket != null)
                {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("msa", requestContext.UserProxyTicket);
                }

                if (requestContext.FamilyJsonWebToken != null)
                {
                    requestMessage.Headers.Add(FamilyJwtHeader, requestContext.FamilyJsonWebToken);
                }
            }

            return requestMessage;
        }

        internal static HttpRequestMessage CreateHttpRequestMessage<T>(
            Uri requestUri,
            IPxfRequestContext requestContext,
            HttpMethod httpMethod,
            OutgoingApiEventWrapper outgoingApiEvent,
            IDictionary<string, string> customHeaders,
            T postObject = null)
            where T : class
        {
            outgoingApiEvent.ThrowOnNull("outgoingApiEvent");

            var requestMessage = new HttpRequestMessage(httpMethod, requestUri);

            // Note: CV is set and incremented in the OutgoingRequestHandler
            requestMessage.Properties.Add(OutgoingRequestHandler.CounterInstanceNameKey, outgoingApiEvent.DependencyOperationName);
            requestMessage.Properties.Add(OutgoingRequestHandler.ApiEventContextKey, outgoingApiEvent);

            return requestMessage;
        }

        /// <summary>
        ///     Creates an HttpRequestMessage for CortanaNotebookV1, PdpApiV2, Beacon.
        /// </summary>
        private static async Task<HttpRequestMessage> CreateHttpRequestMessageAsync<T>(
            Uri requestUri,
            IAadTokenProvider aadTokenProvider,
            AadTokenPartnerConfig aadTokenPartnerConfig,
            IPxfRequestContext requestContext,
            IPxfPartnerConfiguration partnerConfiguration,
            HttpMethod httpMethod,
            OutgoingApiEventWrapper outgoingApiEvent,
            IDictionary<string, string> customHeaders,
            string schema = "",
            T postObject = null)
            where T : class
        {
            HttpRequestMessage request = CreateHttpRequestMessage(
                requestUri,
                httpMethod,
                outgoingApiEvent,
                customHeaders,
                postObject);

            if (aadTokenPartnerConfig != null && aadTokenProvider != null)
            {
                IDictionary<string, string> popClaims = HttpExtensions.CreatePopClaims(requestContext, partnerConfiguration);
                AadPopTokenRequestType aadPopTokenRequestType;
                if (partnerConfiguration.PxfAdapterVersion == AdapterVersion.BeaconV1)
                {
                    // Beacon
                    aadPopTokenRequestType = AadPopTokenRequestType.MsaProxyTicket;
                }
                else
                {
                    // PdpApiV2 and CortanaNotebookV1
                    aadPopTokenRequestType = AadPopTokenRequestType.AppAssertedUserToken;
                }

                AadPopTokenRequest popRequest = new AadPopTokenRequest
                {
                    RequestUri = requestUri,
                    Resource = aadTokenPartnerConfig.Resource,
                    Scope = aadTokenPartnerConfig.Scope,
                    Claims = popClaims,
                    HttpMethod = httpMethod,
                    Type = aadPopTokenRequestType
                };

                string popToken = await aadTokenProvider
                    .GetPopTokenAsync(popRequest, CancellationToken.None)
                    .ConfigureAwait(false);

                request.Headers.Authorization =
                    new AuthenticationHeaderValue("MSAuth1.0", "popToken=\"" + popToken + "\", type=\"AT_POP\"");
            }

            return request;
        }

        /// <summary>
        /// Creates the pop claims from the user request context and the partner configuration.
        /// </summary>
        /// <param name="requestContext">The request context</param>
        /// <param name="partnerConfiguration">The partner configuration</param>
        /// <returns>Returns a dictionary with the claims</returns>
        private static IDictionary<string, string> CreatePopClaims(IPxfRequestContext requestContext, IPxfPartnerConfiguration partnerConfiguration)
        {
            var popClaims = new Dictionary<string, string>();

            if (partnerConfiguration.PxfAdapterVersion == AdapterVersion.BeaconV1)
            {
                // This is the standard claim implementation for delegate MSA proxy PoP tokens
                // Consult https://aadwiki.windows-int.net/index.php?title=Server-to-server_authentication for more information
                // msa_pt: MSA Proxy compact token
                popClaims["msa_pt"] = requestContext.UserProxyTicket;
            }
            else
            {
                // The claims implementation below is custom and unique to PdpApi
                // we're always consumer at this point
                popClaims["isconsumer"] = "true";

                // add in the claims based on the request context
                // PDAPI needs the PUID in hex, but not the CID.
                popClaims["puid"] = requestContext.TargetPuid.ToString("X");
                if (requestContext.TargetCid.HasValue)
                {
                    popClaims["cid"] = requestContext.TargetCid.Value.ToString();
                }

                if (!string.IsNullOrWhiteSpace(requestContext.Country))
                {
                    popClaims["country"] = requestContext.Country;
                }
            }

            return popClaims;
        }

        private static async Task<TResult> HandleResponseAsync<TResult>(
            IPxfPartnerConfiguration partnerConfiguration,
            HttpResponseMessage responseMessage)
            where TResult : class
        {
            if (responseMessage.IsSuccessStatusCode)
            {
                TResult response = null;
                if (responseMessage.Content != null)
                {
                    string responseAsString = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                    response = JsonConvert.DeserializeObject<TResult>(responseAsString);
                }

                if (response == null)
                {
                    throw new PxfAdapterException(
                        partnerConfiguration?.Id,
                        partnerConfiguration?.PartnerId,
                        (int)HttpStatusCode.OK,
                        "Unexpectedly empty response body received.");
                }

                return response;
            }

            // Must have an error
            await HandleResponseErrorAsync(partnerConfiguration, responseMessage).ConfigureAwait(false);

            return null;
        }

        private static async Task HandleResponseErrorAsync(IPxfPartnerConfiguration partnerConfiguration, HttpResponseMessage responseMessage)
        {
            if (responseMessage.Content == null)
            {
                throw new PxfAdapterException(
                    partnerConfiguration?.Id,
                    partnerConfiguration?.PartnerId,
                    (int)responseMessage.StatusCode,
                    string.Format(CultureInfo.InvariantCulture, "Error with no body and status code {0}", responseMessage.StatusCode));
            }

            string errorContent = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

            throw new PxfAdapterException(
                partnerConfiguration?.Id,
                partnerConfiguration?.PartnerId,
                (int)responseMessage.StatusCode,
                $"Error returned by partner is '{responseMessage.StatusCode}'. ResponseBody='{errorContent}'");
        }

        private static async Task PostAsync(this IHttpClient httpClient, IPxfPartnerConfiguration partnerConfiguration, HttpRequestMessage requestMessage)
        {
            HttpResponseMessage responseMessage =
                await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);

            if (responseMessage.IsSuccessStatusCode)
            {
                // do nothing on success
                return;
            }

            await HandleResponseErrorAsync(partnerConfiguration, responseMessage).ConfigureAwait(false);
        }
    }
}

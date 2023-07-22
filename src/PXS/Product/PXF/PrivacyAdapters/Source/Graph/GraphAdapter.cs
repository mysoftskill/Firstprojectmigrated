// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Graph
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Factory;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.OSGS.HttpClientCommon;

    using Newtonsoft.Json;

    /// <summary>
    ///     GraphAdapter.
    /// </summary>
    public class GraphAdapter : IGraphAdapter
    {
        private readonly string Scheme = "Bearer";

        private readonly IGraphAdapterConfiguration configuration;

        private readonly IHttpClient httpClient;

        private readonly IAadAuthManager authManager;

        /// <summary>
        ///     Create an instance of GraphAdapter.
        /// </summary>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="httpClientFactory">The HTTP client factory.</param>
        /// <param name="counterFactory">The counter factory.</param>
        /// <param name="authManager">The auth manager.</param>
        public GraphAdapter(
            IPrivacyConfigurationManager configurationManager,
            IHttpClientFactory httpClientFactory,
            ICounterFactory counterFactory,
            IAadAuthManager authManager)
        {
            if (httpClientFactory == null)
                throw new ArgumentNullException(nameof(httpClientFactory));
            if (counterFactory == null)
                throw new ArgumentNullException(nameof(counterFactory));

            this.configuration = configurationManager.AdaptersConfiguration.GraphAdapterConfiguration ?? throw new ArgumentNullException(nameof(configurationManager));
            this.httpClient = httpClientFactory.CreateHttpClient(configuration, counterFactory);
            this.authManager = authManager ?? throw new ArgumentNullException(nameof(authManager));
        }

        /// <inheritdoc />
        public async Task<AdapterResponse<IsMemberOfResponse>> IsMemberOfAsync(Guid memberId, Guid groupId)
        {
            Uri requestUri = new Uri(new Uri(this.configuration.BaseUrl), new Uri($"myorganization/isMemberOf?api-version={this.configuration.ApiVersion}", UriKind.Relative));

            OutgoingApiEventWrapper apiEvent = OutgoingApiEventWrapper.CreateBasicOutgoingEvent(
                this.configuration.PartnerId,
                "IsMemberOfAsync",
                targetUri: requestUri.ToString(),
                operationVersion: null,
                requestMethod: HttpMethod.Post,
                dependencyType: "WebService");

            IsMemberOfRequest isMemberOfRequest = new IsMemberOfRequest
            {
                GroupId = groupId.ToString(),
                MemberId = memberId.ToString()
            };

            HttpRequestMessage requestMessage =
                HttpExtensions.CreateHttpRequestMessage(requestUri, HttpMethod.Post, apiEvent, null, isMemberOfRequest);

            string accessToken = await this.authManager.GetAccessTokenAsync(this.configuration.AadGraphResource).ConfigureAwait(false);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue(Scheme, accessToken);

            HttpResponseMessage responseMessage = await this.httpClient.SendAsync(requestMessage).ConfigureAwait(false);

            return await HandleJsonResponseAsync<IsMemberOfResponse>(responseMessage).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<AdapterResponse<GetDirectoryRolesResponse>> GetDirectoryRolesAsync(Guid tenantId, Guid roleTemplateId)
        {
            Uri requestUri = new Uri(
                new Uri(this.configuration.BaseUrl), 
                new Uri($"{tenantId}/directoryRoles?$filter=roleTemplateId eq '{roleTemplateId}'&api-version={this.configuration.ApiVersion}", UriKind.Relative));

            OutgoingApiEventWrapper apiEvent = OutgoingApiEventWrapper.CreateBasicOutgoingEvent(
                this.configuration.PartnerId,
                "GetDirectoryRolesAsync",
                targetUri: requestUri.ToString(),
                operationVersion: null,
                requestMethod: HttpMethod.Get,
                dependencyType: "WebService");

            HttpRequestMessage requestMessage =
                HttpExtensions.CreateHttpRequestMessage(requestUri, HttpMethod.Get, apiEvent, null);

            string accessToken = await this.authManager.GetAccessTokenAsync(this.configuration.AadGraphResource).ConfigureAwait(false);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue(Scheme, accessToken);

            HttpResponseMessage responseMessage = await this.httpClient.SendAsync(requestMessage).ConfigureAwait(false);

            return await HandleJsonResponseAsync<GetDirectoryRolesResponse>(responseMessage).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<AdapterResponse<GetDirectoryRoleMemberResponse>> GetDirectoryRoleMembersAsync(Guid tenantId, Guid directoryRoleId)
        {
            Uri requestUri = new Uri(new Uri(this.configuration.BaseUrl), 
                new Uri($"{tenantId}/directoryRoles/{directoryRoleId}/$links/members?api-version={this.configuration.ApiVersion}", UriKind.Relative));

            OutgoingApiEventWrapper apiEvent = OutgoingApiEventWrapper.CreateBasicOutgoingEvent(
                this.configuration.PartnerId,
                "GetDirectoryRoleMembersAsync",
                targetUri: requestUri.ToString(),
                operationVersion: null,
                requestMethod: HttpMethod.Get,
                dependencyType: "WebService");

            HttpRequestMessage requestMessage =
                HttpExtensions.CreateHttpRequestMessage(requestUri, HttpMethod.Get, apiEvent, null);

            string accessToken = await this.authManager.GetAccessTokenAsync(this.configuration.AadGraphResource).ConfigureAwait(false);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue(Scheme, accessToken);

            HttpResponseMessage responseMessage = await this.httpClient.SendAsync(requestMessage).ConfigureAwait(false);

            return await HandleJsonResponseAsync<GetDirectoryRoleMemberResponse>(responseMessage).ConfigureAwait(false);
        }

        private static async Task<AdapterResponse<T>> HandleJsonResponseAsync<T>(HttpResponseMessage responseMessage)
            where T : class
        {
            if (!responseMessage.IsSuccessStatusCode)
            {
                return new AdapterResponse<T>
                {
                    Error = new AdapterError(AdapterErrorCode.Unknown, await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false), (int)responseMessage.StatusCode)
                };
            }

            return new AdapterResponse<T>
            {
                Result = JsonConvert.DeserializeObject<T>(await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false))
            };
        }
    }
}

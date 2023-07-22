// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.BeaconAdapter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.DataContracts.V2;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Converters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.V2;
    using Microsoft.OSGS.HttpClientCommon;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     Defines a Beacon service location data adapter
    /// </summary>
    public class BeaconAdapter : IPxfAdapter
    {

        private readonly IAadTokenProvider aadTokenProvider;
        private readonly IHttpClient httpClient;
        private readonly ILogger logger;
        private readonly IPxfPartnerConfiguration partnerConfiguration;

        private readonly AadTokenPartnerConfig aadTokenPartnerConfig;

        private const string LocationHistoryRelativePath = "v1/my/locationhistory";

        /// <summary>
        ///     Implements a Beacon Location data adapter
        /// </summary>
        public BeaconAdapter(
            IHttpClient httpClient,
            IPxfPartnerConfiguration partnerConfiguration,
            IAadTokenProvider aadTokenProvider,
            ILogger logger)
        {
            this.httpClient = httpClient;
            this.partnerConfiguration = partnerConfiguration;
            this.aadTokenProvider = aadTokenProvider;
            this.aadTokenPartnerConfig = new AadTokenPartnerConfig
            {
                Resource = partnerConfiguration.AadTokenResourceId,
                Scope = partnerConfiguration.AadTokenScope
            };

            this.logger = logger;
        }

        public IDictionary<string, string> CustomHeaders { get; set; }

        /// <inheritdoc/>
        public async Task<PagedResponse<LocationResource>> GetLocationHistoryAsync(IPxfRequestContext requestContext, OrderByType orderBy, DateOption? dateOption = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            string operationName = string.Join("_", "View_Location", this.partnerConfiguration.PartnerId);
            var resourceUri = new Uri(new Uri(this.partnerConfiguration.BaseUrl), LocationHistoryRelativePath);
            return await this.GetBeaconResourceAsync(requestContext, resourceUri, operationName).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<PagedResponse<LocationResource>> GetNextLocationPageAsync(IPxfRequestContext requestContext, Uri nextUri)
        {
            string operationName = string.Join("_", "View_NextPage_Location", this.partnerConfiguration.PartnerId);
            return await this.GetBeaconResourceAsync(requestContext, nextUri, operationName).ConfigureAwait(false);
        }

        private async Task<PagedResponse<LocationResource>> GetBeaconResourceAsync(
            IPxfRequestContext requestContext,
            Uri uri,
            string operationName)
        {
            OutgoingApiEventWrapper outgoingApiEvent = OutgoingApiEventWrapper.CreateHttpEventWithPuid(
                this.partnerConfiguration.PartnerId,
                operationName,
                this.partnerConfiguration.PxfAdapterVersion.ToString(),
                uri,
                HttpMethod.Get,
                requestContext.AuthorizingPuid);

            try
            {
                PagedResponseV2<LocationResourceV2> locationResource =
                    await this.httpClient.GetAsync<PagedResponseV2<LocationResourceV2>>(
                        uri,
                        this.aadTokenProvider,
                        this.aadTokenPartnerConfig,
                        requestContext,
                        outgoingApiEvent,
                        this.partnerConfiguration,
                        this.CustomHeaders).ConfigureAwait(false);

                this.logger.Information(nameof(BeaconAdapter), $"Received {locationResource?.Items?.Count().ToString() ?? "null"} location resource items.");

                var response = new PagedResponse<LocationResource>
                {
                    PartnerId = this.partnerConfiguration.PartnerId,
                    Items = locationResource?.Items?.Select(i => i.ToLocationResource(this.partnerConfiguration.PartnerId)).ToList() ?? new List<LocationResource>(),
                    NextLink = locationResource?.NextLink
                };

                return response;
            }
            catch (PxfAdapterException e)
            {
                this.logger.Error(nameof(BeaconAdapter), e, "An error occurred with the outbound request to target uri: {0}", uri);
                throw;
            }
        }

        #region Should not be implemented region
        public Task<DeleteResourceResponse> DeleteLocationAsync(IPxfRequestContext requestContext, params LocationV2Delete[] deletes)
        {
            throw new NotImplementedException();
        }

        public Task<DeleteResourceResponse> DeleteAppUsageAsync(IPxfRequestContext requestContext)
        {
            throw new NotImplementedException();
        }

        public Task<DeleteResourceResponse> DeleteBrowseHistoryAsync(IPxfRequestContext requestContext)
        {
            throw new NotImplementedException();
        }

        public Task<DeleteResourceResponse> DeleteLocationHistoryAsync(IPxfRequestContext requestContext)
        {
            throw new NotImplementedException();
        }

        public Task<DeleteResourceResponse> DeleteSearchHistoryAsync(IPxfRequestContext requestContext, bool disableThrottling)
        {
            throw new NotImplementedException();
        }

        public Task<DeleteResourceResponse> DeleteVoiceHistoryAsync(IPxfRequestContext requestContext)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResponse<AppUsageResource>> GetAppUsageAsync(IPxfRequestContext requestContext, OrderByType orderBy, DateOption? dateOption, DateTime? startDate, DateTime? endDate, string search)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResponse<BrowseResource>> GetBrowseHistoryAsync(IPxfRequestContext requestContext, OrderByType orderBy, DateOption? dateOption, DateTime? startDate, DateTime? endDate, string search)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResponse<AppUsageResource>> GetNextAppUsagePageAsync(IPxfRequestContext requestContext, Uri nextUri)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResponse<BrowseResource>> GetNextBrowsePageAsync(IPxfRequestContext requestContext, Uri nextUri)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResponse<SearchResource>> GetNextSearchPageAsync(IPxfRequestContext requestContext, Uri nextUri)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResponse<VoiceResource>> GetNextVoicePageAsync(IPxfRequestContext requestContext, Uri nextUri)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResponse<SearchResource>> GetSearchHistoryAsync(IPxfRequestContext requestContext, OrderByType orderBy, DateOption? dateOption, DateTime? startDate, DateTime? endDate, string search, bool disableThrottling)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResponse<VoiceResource>> GetVoiceHistoryAsync(IPxfRequestContext requestContext, OrderByType orderBy, DateOption? dateOption, DateTime? startDate, DateTime? endDate, string search)
        {
            throw new NotImplementedException();
        }

        public Task<VoiceAudioResource> GetVoiceHistoryAudioAsync(IPxfRequestContext requestContext, string id)
        {
            throw new NotImplementedException();
        }

        public Task<CountResourceResponse> GetAppUsageAggregateCountAsync(IPxfRequestContext requestContext)
        {
            throw new NotImplementedException();
        }

        public Task<CountResourceResponse> GetBrowseHistoryAggregateCountAsync(IPxfRequestContext requestContext)
        {
            throw new NotImplementedException();
        }

        public Task<CountResourceResponse> GetContentConsumptionAggregateCountAsync(IPxfRequestContext requestContext)
        {
            throw new NotImplementedException();
        }

        public Task<CountResourceResponse> GetLocationAggregateCountAsync(IPxfRequestContext requestContext)
        {
            throw new NotImplementedException();
        }

        public Task<CountResourceResponse> GetSearchHistoryAggregateCountAsync(IPxfRequestContext requestContext)
        {
            throw new NotImplementedException();
        }

        public Task<CountResourceResponse> GetVoiceHistoryAggregateCountAsync(IPxfRequestContext requestContext)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}

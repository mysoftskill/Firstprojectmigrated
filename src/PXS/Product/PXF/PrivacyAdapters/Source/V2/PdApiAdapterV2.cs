// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.V2
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.DataContracts.V2;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Converters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.OSGS.HttpClientCommon;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.Windows.Services.AuthN.Client.S2S;
    using AuthenticationType = Configuration.AuthenticationType;

    /// <summary>
    ///     Adapter for the V2 PDAPI
    /// </summary>
    public class PdApiAdapterV2 : IPxfAdapter,
        IContentConsumptionV2Adapter,
        ISearchHistoryV2Adapter,
        IAppUsageV2Adapter,
        IVoiceHistoryV2Adapter,
        IBrowseHistoryV2Adapter,
        ILocationV2Adapter,
        IWarmableAdapter
    {
        private const string AppUsagePathTemplate = "v2/{0}/appusage";

        private const string BrowseHistoryPathTemplate = "v2/{0}/browsehistory";

        private const string ContentConsumptionPathTemplate = "v2/{0}/contentconsumption";

        private const string CountAction = "/?$count";

        private const string DeleteAction = "/?delete";

        private const string LocationHistoryPathTemplate = "v2/{0}/locationhistory";

        private const string SearchHistoryPathTemplate = "v2/{0}/searchhistory";

        private const string TestRequestHeader = "X-TestRequest";

        private const string VoiceHistoryPathTemplate = "v2/{0}/voicehistory";

        private readonly AadTokenPartnerConfig aadTokenPartnerConfig;

        private readonly IAadTokenProvider aadTokenProvider;

        private readonly IHttpClient httpClient;

        private readonly ILogger logger;

        private readonly IPxfPartnerConfiguration partnerConfiguration;

        private readonly IS2SAuthClient s2sAuthClient;

        private readonly string WarmupSearch = $"Warmup_{Guid.NewGuid()}";

        /// <summary>
        ///     Gets the custom headers.
        /// </summary>
        public IDictionary<string, string> CustomHeaders { get; private set; }

        /// <summary>
        ///     Instantiates a PDAPI V2 Adapter
        /// </summary>
        /// <param name="httpClient">Http client</param>
        /// <param name="s2sAuthClient">S2S Auth Client</param>
        /// <param name="partnerConfig">Partner configuration</param>
        /// <param name="certProvider">The cert provider.</param>
        /// <param name="aadTokenProvider">AAD token provider</param>
        /// <param name="logger">The logger.</param>
        public PdApiAdapterV2(
            IHttpClient httpClient,
            IS2SAuthClient s2sAuthClient,
            IPxfPartnerConfiguration partnerConfig,
            ICertificateProvider certProvider,
            IAadTokenProvider aadTokenProvider,
            ILogger logger)
        {
            // Bug 532841: [PXS SF] Add init check during app start for DLL 'Wave2MP4.dll'

            this.httpClient = httpClient;

            // TODO: Un-dirty this.
            // **** DIRTY DIRTY DIRTY - Blame Patrick ****
            this.httpClient.Timeout = TimeSpan.FromSeconds(20.0);

            // **** DIRTY DIRTY DIRTY - Blame Patrick ****

            this.s2sAuthClient = s2sAuthClient;
            this.partnerConfiguration = partnerConfig;
            this.logger = logger;
            this.aadTokenProvider = aadTokenProvider;
            this.SetupAdapterCustomHeaders();

            if (this.partnerConfiguration.AuthenticationType == AuthenticationType.AadPopToken)
            {
                this.aadTokenProvider = aadTokenProvider;
                this.aadTokenPartnerConfig = new AadTokenPartnerConfig
                {
                    Resource = partnerConfig.AadTokenResourceId,
                    Scope = partnerConfig.AadTokenScope
                };
            }
        }

        /// <inheritdoc />
        public async Task WarmupAsync(IPxfRequestContext requestContext, ResourceType resourceType)
        {
            switch (resourceType)
            {
                case ResourceType.AppUsage:
                    await this.GetAppUsageAsync(requestContext, DateTimeOffset.UtcNow, null, this.WarmupSearch).ConfigureAwait(false);
                    break;
                case ResourceType.Browse:
                    await this.GetBrowseHistoryAsync(requestContext, DateTimeOffset.UtcNow, null, this.WarmupSearch).ConfigureAwait(false);
                    break;
                case ResourceType.ContentConsumption:
                    await this.GetContentConsumptionAsync(requestContext, DateTimeOffset.UtcNow, null, this.WarmupSearch).ConfigureAwait(false);
                    break;
                case ResourceType.Location:
                case ResourceType.MicrosoftHealthLocation:
                    await this.GetLocationAsync(requestContext, DateTimeOffset.UtcNow, null, this.WarmupSearch).ConfigureAwait(false);
                    break;
                case ResourceType.Search:
                    await this.GetSearchHistoryAsync(requestContext, DateTimeOffset.UtcNow, null, this.WarmupSearch).ConfigureAwait(false);
                    break;
                case ResourceType.Voice:
                    await this.GetVoiceHistoryAsync(requestContext, DateTimeOffset.UtcNow, null, this.WarmupSearch).ConfigureAwait(false);
                    break;
            }
        }

        public async Task<CountResourceResponse> GetAggregateCountAsync(IPxfRequestContext requestContext)
        {
            var resource = await this.GetCountAsync<CountResourceResponse>(
                this.partnerConfiguration.BaseUrl,
                BrowseHistoryPathTemplate,
                requestContext,
                string.Join("_", "View_Browse", this.partnerConfiguration.PartnerId));
            return resource;
        }

        private string ComputePrivacyIdSchema(Uri uri)
        {
            if (uri == null || uri.LocalPath == null)
                return null;

            string path = "/privacyapius/";
            int idx = uri.LocalPath.IndexOf(path, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
                return uri.LocalPath.Remove(idx, path.Length);

            path = "/privacyapi/";
            idx = uri.LocalPath.IndexOf(path, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
                return uri.LocalPath.Remove(idx, path.Length);

            return uri.LocalPath;
        }

        private async Task<DeleteResourceResponse> DeleteResourceAsync(
            string baseUrl,
            string pathTemplate,
            IPxfRequestContext requestContext,
            IEnumerable<string> filters,
            string operationName)
        {
            string relativePath = GetRelativePathFromTemplate(pathTemplate, requestContext.FamilyJsonWebToken != null);
            Uri uri = baseUrl.ExpandUri(relativePath + DeleteAction, new QueryStringCollection());
            var response = new DeleteResourceResponse { PartnerId = this.partnerConfiguration.PartnerId };

            try
            {
                OutgoingApiEventWrapper outgoingApiEvent = OutgoingApiEventWrapper.CreateHttpEventWithPuid(
                    this.partnerConfiguration.PartnerId,
                    operationName,
                    this.partnerConfiguration.PxfAdapterVersion.ToString(),
                    uri,
                    HttpMethod.Post,
                    requestContext.AuthorizingPuid);

                var deleteRequest = new DeleteRequestV2 { Filters = null };
                if (filters != null)
                {
                    deleteRequest.Filters = new List<string>(filters);
                    outgoingApiEvent.ExtraData?.Add("FiltersCount", deleteRequest.Filters.Count.ToString());
                }

                IDictionary<string, string> headers = this.CustomHeaders;
                if (requestContext.IsWatchdogRequest)
                {
                    headers = this.CustomHeaders == null ? new Dictionary<string, string>() : new Dictionary<string, string>(headers);
                    headers.Add(TestRequestHeader, "true");
                }

                if (this.partnerConfiguration.AuthenticationType == AuthenticationType.AadPopToken)
                {
                    await this.httpClient.PostAsync(
                        uri,
                        this.aadTokenProvider,
                        this.aadTokenPartnerConfig,
                        requestContext,
                        this.partnerConfiguration,
                        outgoingApiEvent,
                        headers,
                        this.ComputePrivacyIdSchema(uri),
                        deleteRequest).ConfigureAwait(false);
                }
                else
                {
                    await this.httpClient.PostAsync(
                        uri,
                        requestContext,
                        this.partnerConfiguration,
                        this.s2sAuthClient,
                        this.partnerConfiguration.MsaS2STargetSite,
                        outgoingApiEvent,
                        headers,
                        deleteRequest).ConfigureAwait(false);
                }

                response.Status = ResourceStatus.Deleted;
                return response;
            }
            catch (PxfAdapterException ex)
            {
                response.Status = ResourceStatus.Error;
                response.ErrorMessage = ex.Message;
                return response;
            }
        }

        private async Task<T> GetCountAsync<T>(
        string baseUrl, 
        string pathTemplate,
        IPxfRequestContext requestContext,
        string operationName
        )
                where T : class, new()
        {
            string relativePath = GetRelativePathFromTemplate(pathTemplate, requestContext.FamilyJsonWebToken != null);
            Uri uri = baseUrl.ExpandUri(relativePath + CountAction, new QueryStringCollection());

            OutgoingApiEventWrapper outgoingApiEvent = OutgoingApiEventWrapper.CreateHttpEventWithPuid(
                this.partnerConfiguration.PartnerId,
                operationName,
                this.partnerConfiguration.PxfAdapterVersion.ToString(),
                uri,
                HttpMethod.Get,
                requestContext.AuthorizingPuid);

            return await this.GetResourceAsync<T>(requestContext, uri, outgoingApiEvent, isWarmup: false).ConfigureAwait(false);

        }
        private async Task<T> GetResourceAsync<T>(
            string baseUrl,
            string pathTemplate,
            IPxfRequestContext requestContext,
            string filter,
            string search,
            string operationName)
            where T : class, new()
        {
            bool isWarmup = false;
            if (search == this.WarmupSearch)
            {
                isWarmup = true;
                search = null;
            }

            var queryParameters = new QueryStringCollection { { "$orderBy", "dateTime" } };
            if (!string.IsNullOrEmpty(filter))
                queryParameters.Add("$filter", WebUtility.UrlEncode(filter));
            if (!string.IsNullOrEmpty(search))
                queryParameters.Add("$search", WebUtility.UrlEncode(search));

            string relativePath = GetRelativePathFromTemplate(pathTemplate, requestContext.FamilyJsonWebToken != null);
            Uri uri = baseUrl.ExpandUri(relativePath, queryParameters);

            OutgoingApiEventWrapper outgoingApiEvent = OutgoingApiEventWrapper.CreateHttpEventWithPuid(
                this.partnerConfiguration.PartnerId,
                (isWarmup ? "Warmup_" : string.Empty) + operationName,
                this.partnerConfiguration.PxfAdapterVersion.ToString(),
                uri,
                HttpMethod.Get,
                requestContext.AuthorizingPuid);

            return await this.GetResourceAsync<T>(requestContext, uri, outgoingApiEvent, isWarmup).ConfigureAwait(false);
        }

        private async Task<T> GetResourceAsync<T>(IPxfRequestContext requestContext, Uri uri, OutgoingApiEventWrapper outgoingApiEvent, bool isWarmup)
            where T : class, new()
        {
            try
            {
                IDictionary<string, string> headers = this.CustomHeaders;
                if (requestContext.IsWatchdogRequest)
                {
                    headers = this.CustomHeaders == null ? new Dictionary<string, string>() : new Dictionary<string, string>(headers);
                    headers.Add(TestRequestHeader, "true");
                }

                if (isWarmup)
                {
                    headers = this.CustomHeaders == null ? new Dictionary<string, string>() : new Dictionary<string, string>(headers);
                    headers.Add("X-ScenarioTag", "warmup");
                }

                if (this.partnerConfiguration.AuthenticationType == AuthenticationType.AadPopToken)
                {
                    return await this.httpClient.GetAsync<T>(
                        uri,
                        this.aadTokenProvider,
                        this.aadTokenPartnerConfig,
                        requestContext,
                        outgoingApiEvent,
                        this.partnerConfiguration,
                        headers,
                        this.ComputePrivacyIdSchema(uri)).ConfigureAwait(false);
                }

                return await this.httpClient.GetAsync<T>(
                    uri,
                    requestContext,
                    this.s2sAuthClient,
                    this.partnerConfiguration.MsaS2STargetSite,
                    this.partnerConfiguration,
                    outgoingApiEvent,
                    headers).ConfigureAwait(false);
            }
            catch (PxfAdapterException e)
            {
                this.logger.Error(nameof(PdApiAdapterV2), e, "An error occurred with the outbound request to target uri: {0}", uri);
                throw;
            }
        }

        private void SetupAdapterCustomHeaders()
        {
            if (this.partnerConfiguration?.CustomHeaders != null && this.partnerConfiguration.CustomHeaders.Count > 0)
            {
                this.CustomHeaders = new Dictionary<string, string>();

                foreach (KeyValuePair<string, string> pxfHeader in this.partnerConfiguration.CustomHeaders)
                {
                    this.CustomHeaders.Add(pxfHeader.Key, pxfHeader.Value);
                }
            }
        }

        private static string GenerateGetFilter(DateTimeOffset startingAt, IList<string> sources)
        {
            var sb = new StringBuilder();

            // TODO: Not yet supported
            sb.Append($"dateTime le datetimeoffset'{startingAt:o}'");

            // TODO: Not yet supported
            //if (sources != null)
            //{
            //    if (sb.Length > 0)
            //        sb.Append(" and ");
            //    sb.Append("containsAny(sources,");
            //    sb.Append(string.Join(",", sources.Select(s => $"'{s}'")));
            //    sb.Append(")");
            //}

            return sb.ToString();
        }

        private static string GetRelativePathFromTemplate(string pathTemplate, bool isOnBehalfOfAuth)
        {
            // TODO: keithjac: /my/ or /my/child/ or /my/devices/<deviceid> --- device path not here yet.
            // HACK: should check is on behalf of flag for determining endpoint, at this time the child endpoint is not ready yet though
            string relativePath = string.Format(CultureInfo.InvariantCulture, pathTemplate, "my");
            return relativePath;
        }

        #region Browse

        /// <summary>
        ///     Get browse history
        /// </summary>
        /// <param name="requestContext">Context of the request</param>
        /// <param name="orderBy">Order by</param>
        /// <param name="dateOption">Date Option. Start/End date parameters may be required. (required)</param>
        /// <param name="startDate">State date/time (optional)</param>
        /// <param name="endDate">End date/time (optional)</param>
        /// <param name="search">Search term (optional)</param>
        /// <returns>Page of BrowseResource results</returns>
        public Task<PagedResponse<BrowseResource>> GetBrowseHistoryAsync(
            IPxfRequestContext requestContext,
            OrderByType orderBy,
            DateOption? dateOption = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string search = null)
        {
            if (orderBy != OrderByType.DateTime)
                throw new ArgumentOutOfRangeException(nameof(orderBy), orderBy, "orderBy must be DateTime");
            if (dateOption != DateOption.Between && dateOption != null)
                throw new ArgumentOutOfRangeException(nameof(dateOption), dateOption, "Only Between or null is supported");

            // start date is ignored since on a paging api there's no reason to set an end date. Just stop asking for results when you
            // feel like stopping.
            // end date is the date from which we start looking back in time from
            DateTimeOffset startingAt = endDate ?? DateTime.UtcNow;

            return this.GetBrowseHistoryAsync(requestContext, startingAt, null, search);
        }

        /// <summary>
        ///     Gets next page of browse history from partner
        /// </summary>
        /// <param name="requestContext">Context of the request</param>
        /// <param name="nextUri">Next link returned by previous page</param>
        /// <returns>Page of BrowseResource results</returns>
        public Task<PagedResponse<BrowseResource>> GetNextBrowsePageAsync(IPxfRequestContext requestContext, Uri nextUri)
        {
            return this.GetNextBrowseHistoryPageAsync(requestContext, nextUri);
        }

        /// <summary>
        ///     Deletes the browse history from partner
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <returns>Task of <see cref="DeleteResourceResponse" /></returns>
        public Task<DeleteResourceResponse> DeleteBrowseHistoryAsync(IPxfRequestContext requestContext)
        {
            return this.DeleteResourceAsync(
                this.partnerConfiguration.BaseUrl,
                BrowseHistoryPathTemplate,
                requestContext,
                null,
                string.Join("_", "Delete_Browse", this.partnerConfiguration.PartnerId));
        }

        /// <summary>
        ///     Delete a specific BrowseHistory entry.
        /// </summary>
        public Task<DeleteResourceResponse> DeleteBrowseHistoryAsync(IPxfRequestContext requestContext, params BrowseV2Delete[] deletes)
        {
            return this.DeleteResourceAsync(
                this.partnerConfiguration.BaseUrl,
                BrowseHistoryPathTemplate,
                requestContext,
                deletes?.Select(d => $"urlHash eq '{d.UriHash}' and dateTime eq datetimeoffset'{d.Timestamp:o}'"),
                string.Join("_", "Delete_BrowseHistoryId", this.partnerConfiguration.PartnerId));
        }

        /// <summary>
        ///     Gets next page of BrowseHistory from partner
        /// </summary>
        public async Task<PagedResponse<BrowseResource>> GetNextBrowseHistoryPageAsync(IPxfRequestContext requestContext, Uri nextUri)
        {
            string operationName = string.Join("_", "View_NextPage_Browse", this.partnerConfiguration.PartnerId);

            OutgoingApiEventWrapper outgoingApiEvent = OutgoingApiEventWrapper.CreateHttpEventWithPuid(
                this.partnerConfiguration.PartnerId,
                operationName,
                this.partnerConfiguration.PxfAdapterVersion.ToString(),
                nextUri,
                HttpMethod.Get,
                requestContext.AuthorizingPuid);

            PagedResponseV2<BrowseResourceV2> browseResourceV2 =
                await this.GetResourceAsync<PagedResponseV2<BrowseResourceV2>>(requestContext, nextUri, outgoingApiEvent, false).ConfigureAwait(false);

            var response = new PagedResponse<BrowseResource>
            {
                PartnerId = this.partnerConfiguration.PartnerId,
                Items = browseResourceV2?.Items.Select(i => i.ToBrowseResource(this.partnerConfiguration.PartnerId)).ToList(),
                NextLink = browseResourceV2?.NextLink
            };

            return response;
        }

        /// <summary>
        ///     Get BrowseHistory
        /// </summary>
        public async Task<PagedResponse<BrowseResource>> GetBrowseHistoryAsync(IPxfRequestContext requestContext, DateTimeOffset startingAt, IList<string> sources, string search)
        {
            PagedResponseV2<BrowseResourceV2> browseResourceV2 = await this.GetResourceAsync<PagedResponseV2<BrowseResourceV2>>(
                this.partnerConfiguration.BaseUrl,
                BrowseHistoryPathTemplate,
                requestContext,
                GenerateGetFilter(startingAt, sources),
                search,
                string.Join("_", "View_Browse", this.partnerConfiguration.PartnerId)).ConfigureAwait(false);

            var response = new PagedResponse<BrowseResource>
            {
                PartnerId = this.partnerConfiguration.PartnerId,
                Items = browseResourceV2?.Items?.Select(i => i.ToBrowseResource(this.partnerConfiguration.PartnerId)).ToList(),
                NextLink = browseResourceV2?.NextLink
            };

            return response;
        }

        /// <summary>
        ///     Gets the aggregate count of BrowseHistory from partner
        /// </summary>
        public async Task<CountResourceResponse> GetBrowseHistoryAggregateCountAsync(IPxfRequestContext requestContext)
        {
            var resource = await this.GetCountAsync<CountResourceResponse>(
                this.partnerConfiguration.BaseUrl,
                BrowseHistoryPathTemplate,
                requestContext,
                string.Join("_", "Count_Browse", this.partnerConfiguration.PartnerId));
            return resource;
        }

        #endregion // Browse

        #region AppUsage

        public async Task<PagedResponse<AppUsageResource>> GetAppUsageAsync(
            IPxfRequestContext requestContext,
            OrderByType orderBy,
            DateOption? dateOption = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string search = null)
        {
            if (orderBy != OrderByType.DateTime)
                throw new ArgumentOutOfRangeException(nameof(orderBy), orderBy, "orderBy must be DateTime");
            if (dateOption != DateOption.Between && dateOption != null)
                throw new ArgumentOutOfRangeException(nameof(dateOption), dateOption, "Only Between or null is supported");

            // start date is ignored since on a paging api there's no reason to set an end date. Just stop asking for results when you
            // feel like stopping.
            // end date is the date from which we start looking back in time from
            DateTimeOffset startingAt = endDate ?? DateTime.UtcNow;

            return await this.GetAppUsageAsync(requestContext, startingAt, null, search).ConfigureAwait(false);
        }

        public async Task<PagedResponse<AppUsageResource>> GetNextAppUsagePageAsync(
            IPxfRequestContext requestContext,
            Uri nextUri)
        {
            string operationName = string.Join("_", "View_NextPage_AppUsage", this.partnerConfiguration.PartnerId);

            OutgoingApiEventWrapper outgoingApiEvent = OutgoingApiEventWrapper.CreateHttpEventWithPuid(
                this.partnerConfiguration.PartnerId,
                operationName,
                this.partnerConfiguration.PxfAdapterVersion.ToString(),
                nextUri,
                HttpMethod.Get,
                requestContext.AuthorizingPuid);

            PagedResponseV2<AppUsageResourceV2> appUsageResourceV2 =
                await this.GetResourceAsync<PagedResponseV2<AppUsageResourceV2>>(requestContext, nextUri, outgoingApiEvent, false).ConfigureAwait(false);

            var response = new PagedResponse<AppUsageResource>
            {
                PartnerId = this.partnerConfiguration.PartnerId,
                Items = appUsageResourceV2?.Items.Select(i => i.ToAppUsageResource(this.partnerConfiguration.PartnerId)).ToList(),
                NextLink = appUsageResourceV2?.NextLink
            };

            return response;
        }

        public async Task<PagedResponse<AppUsageResource>> GetAppUsageAsync(IPxfRequestContext requestContext, DateTimeOffset startingAt, IList<string> sources, string search)
        {
            PagedResponseV2<AppUsageResourceV2> appUsageResourceV2 = await this.GetResourceAsync<PagedResponseV2<AppUsageResourceV2>>(
                this.partnerConfiguration.BaseUrl,
                AppUsagePathTemplate,
                requestContext,
                GenerateGetFilter(startingAt, sources),
                search,
                string.Join("_", "View_AppUsage", this.partnerConfiguration.PartnerId)).ConfigureAwait(false);

            var response = new PagedResponse<AppUsageResource>
            {
                PartnerId = this.partnerConfiguration.PartnerId,
                Items = appUsageResourceV2?.Items?.Select(i => i.ToAppUsageResource(this.partnerConfiguration.PartnerId)).ToList(),
                NextLink = appUsageResourceV2?.NextLink
            };

            return response;
        }

        public Task<DeleteResourceResponse> DeleteAppUsageAsync(IPxfRequestContext requestContext)
        {
            return this.DeleteAppUsageAsync(requestContext, null);
        }

        public Task<DeleteResourceResponse> DeleteAppUsageAsync(IPxfRequestContext requestContext, params AppUsageV2Delete[] deletes)
        {
            string operationName = "Delete_AppUsage";

            if (deletes?.Length > 0)
            {
                operationName += "Id";
            }

            return this.DeleteResourceAsync(
                this.partnerConfiguration.BaseUrl,
                AppUsagePathTemplate,
                requestContext,
                deletes?.Select(d => $"appId eq '{d.Id}' and dateTime eq datetimeoffset'{d.Timestamp:o}' and aggregation eq '{d.Aggregation}'"),
                string.Join("_", operationName, this.partnerConfiguration.PartnerId));
        }

        /// <summary>
        ///     Gets the aggregate count of AppUseage from partner
        /// </summary>
        public async Task<CountResourceResponse> GetAppUsageAggregateCountAsync(IPxfRequestContext requestContext)
        {
            var resource = await this.GetCountAsync<CountResourceResponse>(
                this.partnerConfiguration.BaseUrl,
                AppUsagePathTemplate,
                requestContext,
                string.Join("_", "Count_AppUsage", this.partnerConfiguration.PartnerId));
            return resource;
        }

        #endregion

        #region Voice

        /// <summary>
        ///     Get voice history
        /// </summary>
        /// <param name="requestContext">Context of the request</param>
        /// <param name="orderBy">Order by</param>
        /// <param name="dateOption">Date Option. Start/End date parameters may be required. (required)</param>
        /// <param name="startDate">State date/time (optional)</param>
        /// <param name="endDate">End date/time (optional)</param>
        /// <param name="search">Search term (optional)</param>
        /// <returns>Page of VoiceResource results</returns>
        public async Task<PagedResponse<VoiceResource>> GetVoiceHistoryAsync(
            IPxfRequestContext requestContext,
            OrderByType orderBy,
            DateOption? dateOption = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string search = null)
        {
            if (orderBy != OrderByType.DateTime)
                throw new ArgumentOutOfRangeException(nameof(orderBy), orderBy, "orderBy must be DateTime");
            if (dateOption != DateOption.Between && dateOption != null)
                throw new ArgumentOutOfRangeException(nameof(dateOption), dateOption, "Only Between or null is supported");

            // start date is ignored since on a paging api there's no reason to set an end date. Just stop asking for results when you
            // feel like stopping.
            // end date is the date from which we start looking back in time from
            DateTimeOffset startingAt = endDate ?? DateTime.UtcNow;

            return await this.GetVoiceHistoryAsync(requestContext, startingAt, null, search).ConfigureAwait(false);
        }

        /// <summary>
        ///     Gets next page of voice history from partner
        /// </summary>
        /// <param name="requestContext">Context of the request</param>
        /// <param name="nextUri">Next link returned by previous page</param>
        /// <returns>Page of VoiceResource results</returns>
        public Task<PagedResponse<VoiceResource>> GetNextVoicePageAsync(
            IPxfRequestContext requestContext,
            Uri nextUri)
        {
            return this.GetNextVoiceHistoryPageAsync(requestContext, nextUri);
        }

        /// <summary>
        ///     Get voice history audio
        /// </summary>
        /// <param name="requestContext">Context of the request</param>
        /// <param name="id">Id of the voice history audio to retrieve.</param>
        /// <returns>VoiceAudioResource result</returns>
        public async Task<VoiceAudioResource> GetVoiceHistoryAudioAsync(IPxfRequestContext requestContext, string id)
        {
            PagedResponseV2<VoiceAudioResourceV2> voiceAudioResourceV2 = await this.GetResourceAsync<PagedResponseV2<VoiceAudioResourceV2>>(
                this.partnerConfiguration.BaseUrl,
                VoiceHistoryPathTemplate,
                requestContext,
                $"id eq '{id}'",
                null,
                string.Join("_", "View_VoiceHistoryAudio", this.partnerConfiguration.PartnerId)).ConfigureAwait(false);

            return voiceAudioResourceV2?.Items.FirstOrDefault(i => i != null)?.ToVoiceAudioResource(this.partnerConfiguration.PartnerId);
        }

        public Task<DeleteResourceResponse> DeleteVoiceHistoryAsync(IPxfRequestContext requestContext, params VoiceV2Delete[] deletes)
        {
            string operationName = "Delete_VoiceHistory";

            if (deletes?.Length > 0)
            {
                operationName += "Id";
            }

            return this.DeleteResourceAsync(
                this.partnerConfiguration.BaseUrl,
                VoiceHistoryPathTemplate,
                requestContext,
                deletes?.Select(d => $"id eq '{d.VoiceId}' and dateTime eq datetimeoffset'{d.Timestamp:o}'"),
                string.Join("_", operationName, this.partnerConfiguration.PartnerId));
        }

        public async Task<PagedResponse<VoiceResource>> GetNextVoiceHistoryPageAsync(IPxfRequestContext requestContext, Uri nextUri)
        {
            string operationName = string.Join("_", "View_NextPage_Voice", this.partnerConfiguration.PartnerId);

            OutgoingApiEventWrapper outgoingApiEvent = OutgoingApiEventWrapper.CreateHttpEventWithPuid(
                this.partnerConfiguration.PartnerId,
                operationName,
                this.partnerConfiguration.PxfAdapterVersion.ToString(),
                nextUri,
                HttpMethod.Get,
                requestContext.AuthorizingPuid);

            PagedResponseV2<VoiceResourceV2> voiceResourceV2 =
                await this.GetResourceAsync<PagedResponseV2<VoiceResourceV2>>(requestContext, nextUri, outgoingApiEvent, false).ConfigureAwait(false);

            var response = new PagedResponse<VoiceResource>
            {
                PartnerId = this.partnerConfiguration.PartnerId,
                Items = voiceResourceV2?.Items.Select(i => i.ToVoiceResource(this.partnerConfiguration.PartnerId)).ToList(),
                NextLink = voiceResourceV2?.NextLink
            };

            return response;
        }

        public async Task<PagedResponse<VoiceResource>> GetVoiceHistoryAsync(IPxfRequestContext requestContext, DateTimeOffset startingAt, IList<string> sources, string search)
        {
            PagedResponseV2<VoiceResourceV2> voiceResourceV2 = await this.GetResourceAsync<PagedResponseV2<VoiceResourceV2>>(
                this.partnerConfiguration.BaseUrl,
                VoiceHistoryPathTemplate,
                requestContext,
                GenerateGetFilter(startingAt, null),
                search,
                string.Join("_", "View_VoiceHistory", this.partnerConfiguration.PartnerId)).ConfigureAwait(false);

            var response = new PagedResponse<VoiceResource>
            {
                PartnerId = this.partnerConfiguration.PartnerId,
                Items = voiceResourceV2?.Items?.Select(i => i.ToVoiceResource(this.partnerConfiguration.PartnerId)).ToList(),
                NextLink = voiceResourceV2?.NextLink
            };

            return response;
        }

        public async Task<VoiceAudioResource> GetVoiceHistoryAudioAsync(IPxfRequestContext requestContext, string id, DateTimeOffset timestamp)
        {
            PagedResponseV2<VoiceAudioResourceV2> voiceAudioResourceV2 = await this.GetResourceAsync<PagedResponseV2<VoiceAudioResourceV2>>(
                this.partnerConfiguration.BaseUrl,
                VoiceHistoryPathTemplate,
                requestContext,
                $"id eq '{id}' and dateTime eq datetimeoffset'{timestamp:o}'",
                null,
                string.Join("_", "View_VoiceHistoryAudio", this.partnerConfiguration.PartnerId)).ConfigureAwait(false);

            return voiceAudioResourceV2?.Items.FirstOrDefault(i => i != null)?.ToVoiceAudioResource(this.partnerConfiguration.PartnerId);
        }

        /// <summary>
        ///     Deletes the voice history from partner
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <returns>Task of <see cref="DeleteResourceResponse" /></returns>
        public Task<DeleteResourceResponse> DeleteVoiceHistoryAsync(IPxfRequestContext requestContext)
        {
            return this.DeleteVoiceHistoryAsync(requestContext, null);
        }

        public async Task<CountResourceResponse> GetVoiceHistoryAggregateCountAsync(IPxfRequestContext requestContext)
        {
            var resource = await this.GetCountAsync<CountResourceResponse>(
                this.partnerConfiguration.BaseUrl,
                VoiceHistoryPathTemplate,
                requestContext,
                string.Join("_", "Count_Voice", this.partnerConfiguration.PartnerId));
            return resource;
        }
        #endregion // Voice

        #region Location

        /// <summary>
        ///     Get location history
        /// </summary>
        /// <param name="requestContext">Context of the request</param>
        /// <param name="orderBy">Order by</param>
        /// <param name="dateOption">Date Option. Start/End date parameters may be required. (required)</param>
        /// <param name="startDate">State date/time (optional)</param>
        /// <param name="endDate">End date/time (optional)</param>
        /// <returns>Page of LocationResource results</returns>
        public async Task<PagedResponse<LocationResource>> GetLocationHistoryAsync(
            IPxfRequestContext requestContext,
            OrderByType orderBy,
            DateOption? dateOption = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            if (orderBy != OrderByType.DateTime)
                throw new ArgumentOutOfRangeException(nameof(orderBy), orderBy, "orderBy must be DateTime");
            if (dateOption != DateOption.Between && dateOption != null)
                throw new ArgumentOutOfRangeException(nameof(dateOption), dateOption, "Only Between or null is supported");

            // start date is ignored since on a paging api there's no reason to set an end date. Just stop asking for results when you
            // feel like stopping.
            // end date is the date from which we start looking back in time from
            DateTimeOffset startingAt = endDate ?? DateTime.UtcNow;

            return await this.GetLocationAsync(requestContext, startingAt, null, null).ConfigureAwait(false);
        }

        /// <summary>
        ///     Deletes a single location history entry
        /// </summary>
        public Task<DeleteResourceResponse> DeleteLocationAsync(IPxfRequestContext requestContext, params LocationV2Delete[] deletes)
        {
            string operationName = "Delete_Location";

            if (deletes?.Length > 0)
            {
                operationName += "Id";
            }

            return this.DeleteResourceAsync(
                this.partnerConfiguration.BaseUrl,
                LocationHistoryPathTemplate,
                requestContext,
                deletes?.Select(d => $"dateTime eq datetimeoffset'{d.Timestamp:o}'"),
                string.Join("_", operationName, this.partnerConfiguration.PartnerId));
        }

        /// <summary>
        ///     Gets location history from configured partners
        /// </summary>
        public async Task<PagedResponse<LocationResource>> GetLocationAsync(IPxfRequestContext requestContext, DateTimeOffset startingAt, IList<string> sources, string search)
        {
            PagedResponseV2<LocationResourceV2> locationResourceV2 = await this.GetResourceAsync<PagedResponseV2<LocationResourceV2>>(
                this.partnerConfiguration.BaseUrl,
                LocationHistoryPathTemplate,
                requestContext,
                "", // keithjac (PDAPI not supported yet) TODO: GenerateGetFilter(startingAt, sources),
                search,
                string.Join("_", "View_Location", this.partnerConfiguration.PartnerId)).ConfigureAwait(false);

            var response = new PagedResponse<LocationResource>
            {
                PartnerId = this.partnerConfiguration.PartnerId,
                Items = locationResourceV2?.Items?.Select(i => i.ToLocationResource(this.partnerConfiguration.PartnerId)).ToList(),
                NextLink = locationResourceV2?.NextLink
            };

            return response;
        }

        /// <summary>
        ///     Gets next page of location history from partner
        /// </summary>
        /// <param name="requestContext">Context of the request</param>
        /// <param name="nextUri">Next link returned by previous page</param>
        /// <returns>Page of LocationResource results</returns>
        public async Task<PagedResponse<LocationResource>> GetNextLocationPageAsync(
            IPxfRequestContext requestContext,
            Uri nextUri)
        {
            string operationName = string.Join("_", "View_NextPage_Location", this.partnerConfiguration.PartnerId);

            OutgoingApiEventWrapper outgoingApiEvent = OutgoingApiEventWrapper.CreateHttpEventWithPuid(
                this.partnerConfiguration.PartnerId,
                operationName,
                this.partnerConfiguration.PxfAdapterVersion.ToString(),
                nextUri,
                HttpMethod.Get,
                requestContext.AuthorizingPuid);

            PagedResponseV2<LocationResourceV2> resourceV2 =
                await this.GetResourceAsync<PagedResponseV2<LocationResourceV2>>(requestContext, nextUri, outgoingApiEvent, false).ConfigureAwait(false);

            var response = new PagedResponse<LocationResource>
            {
                PartnerId = this.partnerConfiguration.PartnerId,
                Items = resourceV2?.Items.Select(i => i.ToLocationResource(this.partnerConfiguration.PartnerId)).ToList(),
                NextLink = resourceV2?.NextLink
            };

            return response;
        }

        /// <summary>
        ///     Deletes the location history from partner
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <returns>Task of <see cref="DeleteResourceResponse" /></returns>
        public Task<DeleteResourceResponse> DeleteLocationHistoryAsync(IPxfRequestContext requestContext)
        {
            return this.DeleteLocationAsync(requestContext);
        }

        /// <summary>
        ///     Gets the aggregate count of LocationHistory from partner
        /// </summary>
        public async Task<CountResourceResponse> GetLocationAggregateCountAsync(IPxfRequestContext requestContext)
        {
            var resource = await this.GetCountAsync<CountResourceResponse>(
                this.partnerConfiguration.BaseUrl,
                LocationHistoryPathTemplate,
                requestContext,
                string.Join("_", "Count_Location", this.partnerConfiguration.PartnerId));
            return resource;
        }
        #endregion // Location

        #region Search

        public Task<DeleteResourceResponse> DeleteSearchHistoryAsync(IPxfRequestContext requestContext, params SearchV2Delete[] deletes)
        {
            string operationName = "Delete_SearchHistory";

            if (deletes?.Length > 0)
            {
                operationName += "Id";
            }

            return this.DeleteResourceAsync(
                this.partnerConfiguration.BaseUrl,
                SearchHistoryPathTemplate,
                requestContext,
                deletes?.Select(d => $"id eq '{d.ImpressionId}'"),
                string.Join("_", operationName, this.partnerConfiguration.PartnerId));
        }

        public Task<PagedResponse<SearchResource>> GetNextSearchHistoryPageAsync(IPxfRequestContext requestContext, Uri nextUri)
        {
            return this.GetNextSearchPageAsync(requestContext, nextUri);
        }

        public async Task<PagedResponse<SearchResource>> GetSearchHistoryAsync(IPxfRequestContext requestContext, DateTimeOffset startingAt, IList<string> sources, string search)
        {
            PagedResponseV2<SearchResourceV2> searchResourceV2 = await this.GetResourceAsync<PagedResponseV2<SearchResourceV2>>(
                this.partnerConfiguration.BaseUrl,
                SearchHistoryPathTemplate,
                requestContext,
                GenerateGetFilter(startingAt, sources),
                search,
                string.Join("_", "View_Search", this.partnerConfiguration.PartnerId)).ConfigureAwait(false);

            var response = new PagedResponse<SearchResource>
            {
                PartnerId = this.partnerConfiguration.PartnerId,
                Items = searchResourceV2?.Items?.Select(i => i.ToSeachResource(this.partnerConfiguration.PartnerId)).ToList(),
                NextLink = searchResourceV2?.NextLink
            };

            return response;
        }

        /// <summary>
        ///     Get search history
        /// </summary>
        /// <param name="requestContext">Context of the request</param>
        /// <param name="orderBy">Order by</param>
        /// <param name="dateOption">Date Option. Start/End date parameters may be required. (required)</param>
        /// <param name="startDate">State date/time (optional)</param>
        /// <param name="endDate">End date/time (optional)</param>
        /// <param name="search">Search query (optional)</param>
        /// <param name="disableThrottling">Disable throttling</param>
        /// <returns>Page of SearchResource results</returns>
        public async Task<PagedResponse<SearchResource>> GetSearchHistoryAsync(
            IPxfRequestContext requestContext,
            OrderByType orderBy,
            DateOption? dateOption = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string search = null,
            bool disableThrottling = false)
        {
            if (orderBy != OrderByType.DateTime)
                throw new ArgumentOutOfRangeException(nameof(orderBy), orderBy, "orderBy must be DateTime");
            if (dateOption != DateOption.Between && dateOption != null)
                throw new ArgumentOutOfRangeException(nameof(dateOption), dateOption, "Only Between or null is supported");

            // start date is ignored since on a paging api there's no reason to set an end date. Just stop asking for results when you
            // feel like stopping.
            // end date is the date from which we start looking back in time from
            DateTimeOffset startingAt = endDate ?? DateTime.UtcNow;

            return await this.GetSearchHistoryAsync(requestContext, startingAt, null, search).ConfigureAwait(false);
        }

        /// <summary>
        ///     Gets next page of search history from partner
        /// </summary>
        /// <param name="requestContext">Context of the request</param>
        /// <param name="nextUri">Next link returned by previous page</param>
        /// <returns>Page of SearchResource results</returns>
        public async Task<PagedResponse<SearchResource>> GetNextSearchPageAsync(
            IPxfRequestContext requestContext,
            Uri nextUri)
        {
            string operationName = string.Join("_", "View_NextPage_Search", this.partnerConfiguration.PartnerId);

            OutgoingApiEventWrapper outgoingApiEvent = OutgoingApiEventWrapper.CreateHttpEventWithPuid(
                this.partnerConfiguration.PartnerId,
                operationName,
                this.partnerConfiguration.PxfAdapterVersion.ToString(),
                nextUri,
                HttpMethod.Get,
                requestContext.AuthorizingPuid);

            PagedResponseV2<SearchResourceV2> resourceV2 =
                await this.GetResourceAsync<PagedResponseV2<SearchResourceV2>>(requestContext, nextUri, outgoingApiEvent, false).ConfigureAwait(false);

            var response = new PagedResponse<SearchResource>
            {
                PartnerId = this.partnerConfiguration.PartnerId,
                Items = resourceV2?.Items.Select(i => i.ToSeachResource(this.partnerConfiguration.PartnerId)).ToList(),
                NextLink = resourceV2?.NextLink
            };

            return response;
        }

        /// <summary>
        ///     Deletes the search history from partner
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="disableThrottling">Disable throttling.</param>
        /// <returns>Task of <see cref="DeleteResourceResponse" /></returns>
        public Task<DeleteResourceResponse> DeleteSearchHistoryAsync(IPxfRequestContext requestContext, bool disableThrottling)
        {
            return this.DeleteSearchHistoryAsync(requestContext, null);
        }

        /// <summary>
        ///     Gets the aggregate count of the search history from partner
        /// </summary>
        public async Task<CountResourceResponse> GetSearchHistoryAggregateCountAsync(IPxfRequestContext requestContext)
        {
            var resource = await this.GetCountAsync<CountResourceResponse>(
                this.partnerConfiguration.BaseUrl,
                SearchHistoryPathTemplate,
                requestContext,
                string.Join("_", "Count_Search", this.partnerConfiguration.PartnerId));
            return resource;
        }

        #endregion // Search

        #region ContentConsumption

        public Task<DeleteResourceResponse> DeleteContentConsumptionAsync(IPxfRequestContext requestContext, params ContentConsumptionV2Delete[] deletes)
        {
            string operationName = "Delete_ContentConsumption";

            if (deletes?.Length > 0)
            {
                operationName += "Id";
            }

            return this.DeleteResourceAsync(
                this.partnerConfiguration.BaseUrl,
                ContentConsumptionPathTemplate,
                requestContext,
                deletes?.Select(d => $"id eq '{d.Id}' and dateTime eq datetimeoffset'{d.Timestamp:o}'"),
                string.Join("_", operationName, this.partnerConfiguration.PartnerId));
        }

        public async Task<PagedResponse<ContentConsumptionResource>> GetContentConsumptionAsync(
            IPxfRequestContext requestContext,
            DateTimeOffset startingAt,
            IList<string> sources,
            string search)
        {
            PagedResponseV2<ContentConsumptionResourceV2> contentConsumptionPage = await this.GetResourceAsync<PagedResponseV2<ContentConsumptionResourceV2>>(
                this.partnerConfiguration.BaseUrl,
                ContentConsumptionPathTemplate,
                requestContext,
                GenerateGetFilter(startingAt, sources),
                search,
                string.Join("_", "View_ContentConsumption", this.partnerConfiguration.PartnerId)).ConfigureAwait(false);

            // TODO: REMOVE THIS
            // This is a temporary hack until PDAPI supports orderby, or paging, but hopefully one comes before the other. :)
            if (contentConsumptionPage.Items != null)
                contentConsumptionPage.Items = contentConsumptionPage.Items.OrderByDescending(e => e?.DateTime.ToUniversalTime().Ticks ?? 0);

            var response = new PagedResponse<ContentConsumptionResource>
            {
                PartnerId = this.partnerConfiguration.PartnerId,
                Items = contentConsumptionPage?.Items?.Select(i => i.ToContentConsumptionResource(this.partnerConfiguration.PartnerId)).ToList(),
                NextLink = contentConsumptionPage?.NextLink
            };

            return response;
        }

        public async Task<PagedResponse<ContentConsumptionResource>> GetNextContentConsumptionPageAsync(IPxfRequestContext requestContext, Uri nextUri)
        {
            string operationName = string.Join("_", "View_NextPage_ContentConsumption", this.partnerConfiguration.PartnerId);

            OutgoingApiEventWrapper outgoingApiEvent = OutgoingApiEventWrapper.CreateHttpEventWithPuid(
                this.partnerConfiguration.PartnerId,
                operationName,
                this.partnerConfiguration.PxfAdapterVersion.ToString(),
                nextUri,
                HttpMethod.Get,
                requestContext.AuthorizingPuid);

            PagedResponseV2<ContentConsumptionResourceV2> contentConsumptionResourceV2 =
                await this.GetResourceAsync<PagedResponseV2<ContentConsumptionResourceV2>>(requestContext, nextUri, outgoingApiEvent, false).ConfigureAwait(false);

            var response = new PagedResponse<ContentConsumptionResource>
            {
                PartnerId = this.partnerConfiguration.PartnerId,
                Items = contentConsumptionResourceV2?.Items.Select(i => i.ToContentConsumptionResource(this.partnerConfiguration.PartnerId)).ToList(),
                NextLink = contentConsumptionResourceV2?.NextLink
            };

            return response;
        }

        /// <summary>
        ///     Gets the aggregate count of the content consumption from partner
        /// </summary>
        public async Task<CountResourceResponse> GetContentConsumptionAggregateCountAsync(IPxfRequestContext requestContext)
        {
            var resource = await this.GetCountAsync<CountResourceResponse>(
                this.partnerConfiguration.BaseUrl,
                ContentConsumptionPathTemplate,
                requestContext,
                string.Join("_", "Count_ContentConsumption", this.partnerConfiguration.PartnerId));
            return resource;
        }
        #endregion

    }
}

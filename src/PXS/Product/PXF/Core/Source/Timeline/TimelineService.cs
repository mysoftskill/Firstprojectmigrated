// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Timeline
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Converters;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers;
    using Microsoft.Membership.MemberServices.Privacy.Core.PCF;
    using Microsoft.Membership.MemberServices.Privacy.Core.PrivacyCommand;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2;
    using Microsoft.Membership.MemberServices.PrivacyAdapters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.V2;
    using Microsoft.Membership.MemberServices.ScheduleDbClient;
    using Microsoft.Membership.MemberServices.ScheduleDbClient.Model;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using DateOption = Microsoft.Membership.MemberServices.PrivacyAdapters.DateOption;
    using OrderByType = Microsoft.Membership.MemberServices.PrivacyAdapters.OrderByType;

    /// <summary>
    ///     Timeline Service
    /// </summary>
    public class TimelineService : CoreService, ITimelineService
    {
        // This service is only used by MSA, so cloud instance doesn't apply in this case.
        private const string CloudInstance = null;

        private readonly IPcfProxyService pcfProxyService;

        private readonly Policy privacyPolicy;

        private readonly IRequestClassifier requestClassifier;
        private readonly IScheduleDbClient scheduleDbClient;
        private readonly IMsaIdentityServiceAdapter msaIdentityServiceAdapter;
        private readonly ILogger logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TimelineService" /> class.
        /// </summary>
        public TimelineService(
            IPxfDispatcher pxfDispatcher,
            Policy privacyPolicy,
            IPcfProxyService pcfProxyService,
            IRequestClassifier requestClassifier,
            IScheduleDbClient scheduleDbClient,
            IMsaIdentityServiceAdapter msaIdentityServiceAdapter,
            ILogger logger)
            : base(pxfDispatcher)
        {
            this.privacyPolicy = privacyPolicy ?? throw new ArgumentNullException(nameof(privacyPolicy));
            this.pcfProxyService = pcfProxyService ?? throw new ArgumentNullException(nameof(pcfProxyService));
            this.requestClassifier = requestClassifier;
            this.scheduleDbClient = scheduleDbClient;
            this.msaIdentityServiceAdapter = msaIdentityServiceAdapter;
            this.logger = logger;
        }

        /// <summary>
        ///     Deletes timeline cards by ids
        /// </summary>
        public async Task<ServiceResponse> DeleteAsync(IRequestContext requestContext, IList<string> ids)
        {
            if (requestContext == null)
                throw new ArgumentNullException(nameof(requestContext));
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));
            if (ids.Count <= 0)
                throw new ArgumentException("Value cannot be an empty collection.", nameof(ids));

            List<TimelineCard> cards = ids.Select(TimelineCard.DeserializeId).ToList();

            // Prevent bad data from being stored. While it's not the dashboards fault if something in here is bad (like a delete predicate), we can't act on it.
            // And storing a bad predicate is even worse for consumers of the feed.
            Error error = RequestValidation.ValidateIndividualDeleteRequest(cards);
            if (error != null)
                return new ServiceResponse { Error = error };

            ServiceResponse<IList<Guid>> pcfServiceResponse = await this.pcfProxyService.PostDeleteRequestsAsync(
                requestContext,
                cards.ToUserPcfDeleteRequests(
                    requestContext,
                    LogicalWebOperationContext.ServerActivityId,
                    Sll.Context.Vector.Value,
                    DateTimeOffset.UtcNow,
                    this.privacyPolicy,
                    cloudInstance: CloudInstance,
                    portal: Portals.Amc,
                    isTest: this.requestClassifier.IsTestRequest(Portals.Amc, requestContext.Identity)).ToList()).ConfigureAwait(false);
            if (pcfServiceResponse.Error != null)
                return pcfServiceResponse;

            IPxfRequestContext pxfRequestContext = requestContext.ToAdapterRequestContext();
            var deleteTasks = new List<Task<IList<DeleteResourceResponse>>>();
            Dictionary<ResourceType, List<TimelineCard>> cardsByType = cards.GroupBy(
                c =>
                {
                    if (c is AppUsageCard)
                        return ResourceType.AppUsage;
                    if (c is BrowseCard)
                        return ResourceType.Browse;
                    if (c is SearchCard)
                        return ResourceType.Search;
                    if (c is VoiceCard)
                        return ResourceType.Voice;
                    if (c is LocationCard)
                        return ResourceType.Location;
                    if (c is ContentConsumptionCard)
                        return ResourceType.ContentConsumption;

                    throw new ArgumentOutOfRangeException(nameof(c), $"Unknown card type: {c.GetType().FullName}");
                }).ToDictionary(g => g.Key, g => g.ToList());

            foreach (KeyValuePair<ResourceType, List<TimelineCard>> pair in cardsByType)
            {
                switch (pair.Key)
                {
                    case ResourceType.AppUsage:
                        deleteTasks.Add(
                            this.PxfDispatcher.ExecuteForProvidersAsync(
                                pxfRequestContext,
                                ResourceType.AppUsage,
                                PxfAdapterCapability.Delete,
                                async a =>
                                {
                                    if (!(a.Adapter is IAppUsageV2Adapter v2Adapter))
                                        throw new NotSupportedException("Cannot delete single row with old adapters");
                                    return await v2Adapter.DeleteAppUsageAsync(
                                            pxfRequestContext,
                                            pair.Value.Cast<AppUsageCard>().Select(c => new AppUsageV2Delete(c.AppId, c.Timestamp, c.Aggregation)).ToArray())
                                        .ConfigureAwait(false);
                                }));
                        break;
                    case ResourceType.Browse:
                        deleteTasks.Add(
                            this.PxfDispatcher.ExecuteForProvidersAsync(
                                pxfRequestContext,
                                ResourceType.Browse,
                                PxfAdapterCapability.Delete,
                                async a =>
                                {
                                    if (!(a.Adapter is IBrowseHistoryV2Adapter v2Adapter))
                                        throw new NotSupportedException("Cannot delete single row with old adapters");
                                    return await v2Adapter.DeleteBrowseHistoryAsync(
                                            pxfRequestContext,
                                            pair.Value.Cast<BrowseCard>()
                                                .SelectMany(c => c.Navigations.SelectMany(n => n.Timestamps.Select(t => new { n.UriHash, Timestamp = t })))
                                                .Select(n => new BrowseV2Delete(n.UriHash, n.Timestamp)).ToArray())
                                        .ConfigureAwait(false);
                                }));
                        break;
                    case ResourceType.Search:
                        deleteTasks.Add(
                            this.PxfDispatcher.ExecuteForProvidersAsync(
                                pxfRequestContext,
                                ResourceType.Search,
                                PxfAdapterCapability.Delete,
                                async a =>
                                {
                                    if (!(a.Adapter is ISearchHistoryV2Adapter v2Adapter))
                                        throw new NotSupportedException("Cannot delete single row with old adapters");
                                    return await v2Adapter.DeleteSearchHistoryAsync(
                                            pxfRequestContext,
                                            pair.Value.Cast<SearchCard>().SelectMany(c => c.ImpressionIds).Select(i => new SearchV2Delete(i)).ToArray())
                                        .ConfigureAwait(false);
                                }));
                        break;
                    case ResourceType.Voice:
                        deleteTasks.Add(
                            this.PxfDispatcher.ExecuteForProvidersAsync(
                                pxfRequestContext,
                                ResourceType.Voice,
                                PxfAdapterCapability.Delete,
                                async a =>
                                {
                                    if (!(a.Adapter is IVoiceHistoryV2Adapter v2Adapter))
                                        throw new NotSupportedException("Cannot delete single row with old adapters");
                                    return await v2Adapter.DeleteVoiceHistoryAsync(
                                            pxfRequestContext,
                                            pair.Value.Cast<VoiceCard>().Select(c => new VoiceV2Delete(c.VoiceId, c.Timestamp)).ToArray())
                                        .ConfigureAwait(false);
                                }));
                        break;
                    case ResourceType.Location:
                        deleteTasks.Add(
                            this.PxfDispatcher.ExecuteForProvidersAsync(
                                pxfRequestContext,
                                ResourceType.Location,
                                PxfAdapterCapability.Delete,
                                async a =>
                                {
                                    if (!(a.Adapter is ILocationV2Adapter v2Adapter))
                                        throw new NotSupportedException("Cannot delete single row with old adapters");
                                    return await v2Adapter.DeleteLocationAsync(
                                            pxfRequestContext,
                                            pair.Value.Cast<LocationCard>()
                                                .SelectMany(
                                                    c => new[] { new LocationV2Delete(c.Timestamp) }
                                                        .Concat(
                                                            (c.AdditionalLocations?.Select(x => x.Timestamp) ?? Enumerable.Empty<DateTimeOffset>()).Select(
                                                                t => new LocationV2Delete(t))))
                                                .ToArray())
                                        .ConfigureAwait(false);
                                }));
                        break;
                    case ResourceType.ContentConsumption:
                        deleteTasks.Add(
                            this.PxfDispatcher.ExecuteForProvidersAsync(
                                pxfRequestContext,
                                ResourceType.ContentConsumption,
                                PxfAdapterCapability.Delete,
                                async a =>
                                {
                                    if (!(a.Adapter is IContentConsumptionV2Adapter v2Adapter))
                                        throw new NotSupportedException("Cannot delete single row with old adapters");
                                    return await v2Adapter.DeleteContentConsumptionAsync(
                                            pxfRequestContext,
                                            pair.Value.Cast<ContentConsumptionCard>().Select(c => new ContentConsumptionV2Delete(c.MediaId, c.Timestamp)).ToArray())
                                        .ConfigureAwait(false);
                                }));
                        break;
                }
            }

            await Task.WhenAll(deleteTasks).ConfigureAwait(false);

            List<DeleteResourceResponse> errors = deleteTasks.Select(t => t.Result).SelectMany(r => r).Where(r => r.Status != ResourceStatus.Deleted).ToList();
            if (errors.Count > 0)
            {
                return new ServiceResponse
                {
                    Error = new Error(
                        ErrorCode.PartnerError,
                        string.Join(Environment.NewLine, errors.Select(e => $"[{e.PartnerId}] {e.Status}: {e.ErrorMessage}")))
                };
            }

            return new ServiceResponse();
        }

        /// <summary>
        ///     Deletes timeline cards by type for the last period of time
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="types">
        ///     Types to delete, these should come from <see cref="Microsoft.PrivacyServices.Policy.DataTypes.KnownIds" />, for example
        ///     <code>Policies.Current.DataTypes.Ids.ProductAndServiceUsage</code>
        /// </param>
        /// <param name="period">The time period of the delete request.</param>
        /// <param name="portal">The requesting portal eg. Portals.Amc, Portals.PCD.</param>
        public async Task<ServiceResponse<IList<Guid>>> DeleteAsync(IRequestContext requestContext, IList<string> types, TimeSpan period, string portal)
        {
            if (requestContext == null)
                throw new ArgumentNullException(nameof(requestContext));
            if (types == null)
                throw new ArgumentNullException(nameof(types));
            if (types.Count <= 0)
                throw new ArgumentException("Value cannot be an empty collection.", nameof(types));

            Error error = RequestValidation.ValidatePrivacyDataTypes(types, this.privacyPolicy);
            if (error != null)
            {
                return new ServiceResponse<IList<Guid>> { Error = error };
            }

            IPxfRequestContext pxfRequestContext = requestContext.ToAdapterRequestContext();

            DateTimeOffset endTime = DateTimeOffset.UtcNow;
            DateTimeOffset startTime = period == TimeSpan.MaxValue ? DateTimeOffset.MinValue.ToUniversalTime() : endTime - period;

            ServiceResponse<IList<Guid>> pcfServiceResponse = await this.pcfProxyService.PostDeleteRequestsAsync(
                requestContext,
                PrivacyRequestConverter.CreatePcfDeleteRequests(
                    PrivacyRequestConverter.CreateMsaSubjectFromContext(requestContext),
                    requestContext,
                    LogicalWebOperationContext.ServerActivityId,
                    Sll.Context.Vector.Value,
                    null,
                    DateTimeOffset.UtcNow,
                    types,
                    startTime,
                    endTime,
                    cloudInstance: CloudInstance,
                    portal: portal,
                    isTest: this.requestClassifier.IsTestRequest(portal, requestContext.Identity)).ToList()).ConfigureAwait(false);

            if (pcfServiceResponse.Error != null)
                return new ServiceResponse<IList<Guid>> { Error = pcfServiceResponse.Error };

            var deleteTasks = new List<Task<DeletionResponse<DeleteResourceResponse>>>();

            foreach (string type in types.Distinct())
            {
                var deletePolicyDataTypeTask = this.PxfDispatcher.CreateDeletePolicyDataTypeTask(type, pxfRequestContext);
                if (deletePolicyDataTypeTask != null)
                {
                    deleteTasks.Add(deletePolicyDataTypeTask);
                }
            }

            await Task.WhenAll(deleteTasks).ConfigureAwait(false);

            List<DeleteResourceResponse> errors = deleteTasks.Select(t => t.Result).SelectMany(r => r.Items).Where(r => r.Status != ResourceStatus.Deleted).ToList();
            if (errors.Count > 0)
            {
                return new ServiceResponse<IList<Guid>>
                {
                    Error = new Error(
                        ErrorCode.PartnerError,
                        string.Join(Environment.NewLine, errors.Select(e => $"[{e.PartnerId}] {e.Status}: {e.ErrorMessage}")))
                };
            }

            return pcfServiceResponse;
        }

        /// <summary>
        ///     Gets the timeline cards.
        /// </summary>
        /// <param name="requestContext"></param>
        /// <param name="cardTypes">These should come from <see cref="TimelineCard.CardTypes" /></param>
        /// <param name="count"></param>
        /// <param name="deviceIds"></param>
        /// <param name="sources"></param>
        /// <param name="search"></param>
        /// <param name="timeZoneOffset"></param>
        /// <param name="startingAt"></param>
        /// <param name="nextToken"></param>
        public async Task<ServiceResponse<ExperienceContracts.V2.PagedResponse<TimelineCard>>> GetAsync(
            IRequestContext requestContext,
            IList<string> cardTypes,
            int? count,
            IList<string> deviceIds,
            IList<string> sources,
            string search,
            TimeSpan timeZoneOffset,
            DateTimeOffset startingAt,
            string nextToken)
        {
            if (requestContext == null)
                throw new ArgumentNullException(nameof(requestContext));
            if (cardTypes == null)
                throw new ArgumentNullException(nameof(cardTypes));
            if (cardTypes.Count <= 0)
                throw new ArgumentException("Value cannot be an empty collection.", nameof(cardTypes));

            IPxfRequestContext pxfRequestContext = requestContext.ToAdapterRequestContext();

            // First, build the card type - resource type N:M mapping. Build it in both directions.
            Dictionary<string, ResourceType[]> cardTypeToResourceTypes = cardTypes.ToDictionary(
                c => c,
                c =>
                {
                    switch (c)
                    {
                        case TimelineCard.CardTypes.AppUsageCard:
                            return new[] { ResourceType.AppUsage };
                        case TimelineCard.CardTypes.BrowseCard:
                            return new[] { ResourceType.Browse };
                        case TimelineCard.CardTypes.SearchCard:
                            return new[] { ResourceType.Search };
                        case TimelineCard.CardTypes.VoiceCard:
                            return new[] { ResourceType.Voice };
                        case TimelineCard.CardTypes.LocationCard:
                            return new[] { ResourceType.Location };
                        case TimelineCard.CardTypes.BookConsumptionCard:
                        case TimelineCard.CardTypes.EpisodeConsumptionCard:
                        case TimelineCard.CardTypes.SongConsumptionCard:
                        case TimelineCard.CardTypes.SurroundVideoConsumptionCard:
                        case TimelineCard.CardTypes.VideoConsumptionCard:
                            return new[] { ResourceType.ContentConsumption };
                    }

                    throw new NotSupportedException($"Cannot map CardType {c} to ResourceType");
                });
            Dictionary<ResourceType, string[]> resourceTypeToCardTypes = cardTypeToResourceTypes.Values
                .SelectMany(rt => rt)
                .Distinct()
                .ToDictionary(
                    rt => rt,
                    rt => cardTypeToResourceTypes.Where(kvp => kvp.Value.Contains(rt)).Select(kvp => kvp.Key).Distinct().ToArray());

            // Then, grab the adapters for each resource type
            Dictionary<ResourceType, PartnerAdapter[]> resourceTypeToPartnerAdapters = resourceTypeToCardTypes.Keys
                .ToDictionary(
                    t => t,
                    t => this.PxfDispatcher.GetAdaptersForResourceType(pxfRequestContext, t, PxfAdapterCapability.View).ToArray());

            var resourceSources = new Dictionary<string, IResourceSource<TimelineCard>>();
            foreach (KeyValuePair<ResourceType, PartnerAdapter[]> pair in resourceTypeToPartnerAdapters)
            {
                ResourceType resourceType = pair.Key;
                PartnerAdapter[] adapters = pair.Value;

                switch (resourceType)
                {
                    case ResourceType.AppUsage:
                        foreach (PartnerAdapter adapter in adapters)
                        {
                            PagingResourceSource<AppUsageResource> pagingSource;
                            if (!(adapter.Adapter is IAppUsageV2Adapter auAdapter))
                            {
                                // TODO: This if case should go away and be replaced by an exception once we fully transition to PDAPI and the V2 adapter interfaces
                                pagingSource = new PagingResourceSource<AppUsageResource>(
                                    () =>
                                        adapter.Adapter.GetAppUsageAsync(
                                            pxfRequestContext,
                                            OrderByType.DateTime,
                                            DateOption.Between,
                                            DateTime.MinValue,
                                            startingAt.ToUniversalTime().DateTime,
                                            search),
                                    n => adapter.Adapter.GetNextAppUsagePageAsync(pxfRequestContext, n));
                            }
                            else
                            {
                                pagingSource = new PagingResourceSource<AppUsageResource>(
                                    () => auAdapter.GetAppUsageAsync(pxfRequestContext, startingAt.ToUniversalTime(), sources, search),
                                    n => auAdapter.GetNextAppUsagePageAsync(pxfRequestContext, n));
                            }

                            var transformSource = new TransformResourceSource<AppUsageResource, TimelineCard>(
                                pagingSource,
                                r =>
                                {
                                    Uri.TryCreate(r.AppIconUrl, UriKind.Absolute, out Uri appIconUri);
                                    return new AppUsageCard(
                                        r.AppId,
                                        r.Aggregation,
                                        r.AppIconBackground,
                                        appIconUri,
                                        r.AppName,
                                        r.AppPublisher,
                                        r.DateTime,
                                        r.EndDateTime,
                                        r.DeviceId != null ? new[] { r.DeviceId } : null,
                                        r.Sources,
                                        r.PropertyBag);
                                });
                            resourceSources.Add(adapter.PartnerId, transformSource);
                        }

                        break;
                    case ResourceType.Browse:
                        foreach (PartnerAdapter adapter in adapters)
                        {
                            PagingResourceSource<BrowseResource> pagingSource;
                            if (!(adapter.Adapter is IBrowseHistoryV2Adapter bhAdapter))
                            {
                                // TODO: This if case should go away and be replaced by an exception once we fully transition to PDAPI and the V2 adapter interfaces
                                // TODO: If the adapter could still be a PDP adapter, must provide DateTime Between Values. This is not required once PDP is replaced completely by PDAPI
                                pagingSource = new PagingResourceSource<BrowseResource>(
                                    () =>
                                        adapter.Adapter.GetBrowseHistoryAsync(
                                            pxfRequestContext,
                                            OrderByType.DateTime,
                                            DateOption.Between,
                                            DateTime.MinValue,
                                            startingAt.ToUniversalTime().DateTime,
                                            search),
                                    n => adapter.Adapter.GetNextBrowsePageAsync(pxfRequestContext, n));
                            }
                            else
                            {
                                pagingSource = new PagingResourceSource<BrowseResource>(
                                    () => bhAdapter.GetBrowseHistoryAsync(pxfRequestContext, startingAt.ToUniversalTime(), sources, search),
                                    n => bhAdapter.GetNextBrowseHistoryPageAsync(pxfRequestContext, n));
                            }

                            var transformSource = new TransformResourceSource<BrowseResource, TimelineCard>(
                                pagingSource,
                                r =>
                                {
                                    if (!Uri.TryCreate(r.NavigatedToUrl, UriKind.Absolute, out Uri navigatedUrl))
                                        Uri.TryCreate("https://" + r.NavigatedToUrl, UriKind.Absolute, out navigatedUrl);
                                    string domain = navigatedUrl?.DnsSafeHost.ToLowerInvariant() ?? r.NavigatedToUrl;
                                    return new BrowseCard(
                                        domain,
                                        new List<BrowseCard.Navigation>
                                        {
                                            new BrowseCard.Navigation(r.UrlHash, new List<DateTimeOffset> { r.DateTime }, r.PageTitle, navigatedUrl)
                                        },
                                        r.DateTime,
                                        r.DeviceId != null ? new[] { r.DeviceId } : null,
                                        r.Sources);
                                });
                            resourceSources.Add(adapter.PartnerId, transformSource);
                        }

                        break;
                    case ResourceType.Search:
                        foreach (PartnerAdapter adapter in adapters)
                        {
                            PagingResourceSource<SearchResource> pagingSource;
                            if (!(adapter.Adapter is ISearchHistoryV2Adapter shAdapter))
                            {
                                // TODO: This if case should go away and be replaced by an exception once we fully transition to PDAPI and the V2 adapter interfaces
                                pagingSource = new PagingResourceSource<SearchResource>(
                                    () =>
                                        adapter.Adapter.GetSearchHistoryAsync(
                                            pxfRequestContext,
                                            OrderByType.DateTime,
                                            DateOption.Between,
                                            DateTime.MinValue,
                                            startingAt.ToUniversalTime().DateTime,
                                            search),
                                    n => adapter.Adapter.GetNextSearchPageAsync(pxfRequestContext, n));
                            }
                            else
                            {
                                pagingSource = new PagingResourceSource<SearchResource>(
                                    () => shAdapter.GetSearchHistoryAsync(pxfRequestContext, startingAt.ToUniversalTime(), sources, search),
                                    n => shAdapter.GetNextSearchHistoryPageAsync(pxfRequestContext, n));
                            }

                            var transformSource = new TransformResourceSource<SearchResource, TimelineCard>(
                                pagingSource,
                                r =>
                                {
                                    return new SearchCard(
                                        r.SearchTerms,
                                        r.NavigatedToUrls.Select(
                                            n =>
                                            {
                                                Uri.TryCreate(n.Url, UriKind.Absolute, out Uri uri);
                                                return new SearchCard.Navigation(n.Title, uri, n.Time);
                                            }).ToList(),
                                        new[] { r.Id },
                                        r.DateTime,
                                        r.DeviceId != null ? new[] { r.DeviceId } : null,
                                        r.Sources);
                                });
                            resourceSources.Add(adapter.PartnerId, transformSource);
                        }

                        break;
                    case ResourceType.Voice:
                        foreach (PartnerAdapter adapter in adapters)
                        {
                            PagingResourceSource<VoiceResource> pagingSource;
                            if (!(adapter.Adapter is IVoiceHistoryV2Adapter vhAdapter))
                            {
                                // TODO: This if case should go away and be replaced by an exception once we fully transition to PDAPI and the V2 adapter interfaces
                                pagingSource = new PagingResourceSource<VoiceResource>(
                                    () =>
                                        adapter.Adapter.GetVoiceHistoryAsync(
                                            pxfRequestContext,
                                            OrderByType.DateTime,
                                            DateOption.Between,
                                            DateTime.MinValue,
                                            startingAt.ToUniversalTime().DateTime,
                                            search),
                                    n => adapter.Adapter.GetNextVoicePageAsync(pxfRequestContext, n));
                            }
                            else
                            {
                                pagingSource = new PagingResourceSource<VoiceResource>(
                                    () => vhAdapter.GetVoiceHistoryAsync(pxfRequestContext, startingAt.ToUniversalTime(), sources, search),
                                    n => vhAdapter.GetNextVoiceHistoryPageAsync(pxfRequestContext, n));
                            }

                            var transformSource = new TransformResourceSource<VoiceResource, TimelineCard>(
                                pagingSource,
                                r => new VoiceCard(
                                    r.Id,
                                    r.DisplayText,
                                    r.Application,
                                    r.DeviceType,
                                    r.DateTime,
                                    r.DeviceId == null ? null : new[] { r.DeviceId },
                                    r.Sources));
                            resourceSources.Add(adapter.PartnerId, transformSource);
                        }

                        break;
                    case ResourceType.Location:
                    case ResourceType.MicrosoftHealthLocation:
                        foreach (PartnerAdapter adapter in adapters)
                        {
                            PagingResourceSource<LocationResource> pagingSource;
                            if (!(adapter.Adapter is ILocationV2Adapter v2Adapter))
                            {
                                // TODO: This if case should go away and be replaced by an exception once we fully transition to PDAPI and the V2 adapter interfaces
                                pagingSource = new PagingResourceSource<LocationResource>(
                                    () =>
                                        adapter.Adapter.GetLocationHistoryAsync(
                                            pxfRequestContext,
                                            OrderByType.DateTime,
                                            DateOption.Between,
                                            DateTime.MinValue,
                                            startingAt.ToUniversalTime().DateTime),
                                    n => adapter.Adapter.GetNextLocationPageAsync(pxfRequestContext, n));
                            }
                            else
                            {
                                pagingSource = new PagingResourceSource<LocationResource>(
                                    () => v2Adapter.GetLocationAsync(pxfRequestContext, startingAt.ToUniversalTime(), sources, search),
                                    n => v2Adapter.GetNextLocationPageAsync(pxfRequestContext, n));
                            }

                            var transformSource = new TransformResourceSource<LocationResource, TimelineCard>(
                                pagingSource,
                                r =>
                                {
                                    Uri.TryCreate(r.Url, UriKind.Absolute, out Uri uri);
                                    return new LocationCard(
                                        r.Name,
                                        new LocationCard.GeographyPoint(r.Latitude, r.Longitude, 0.0),
                                        r.AccuracyRadius,
                                        r.ActivityType?.ToString(),
                                        r.EndDateTime,
                                        uri,
                                        r.Distance,
                                        r.DeviceType?.ToString(),
                                        new List<LocationCard.LocationImpression>(),
                                        r.DateTime,
                                        r.DeviceId == null ? null : new[] { r.DeviceId },
                                        r.Sources);
                                });
                            resourceSources.Add(adapter.PartnerId, transformSource);
                        }

                        break;
                    case ResourceType.ContentConsumption:
                        foreach (PartnerAdapter adapter in adapters)
                        {
                            if (!(adapter.Adapter is IContentConsumptionV2Adapter ccAdapter))
                                throw new Exception($"Expected content consumption adapter and got {adapter.Adapter?.GetType().FullName} ({adapter.PartnerId})");

                            var pagingSource = new PagingResourceSource<ContentConsumptionResource>(
                                () => ccAdapter.GetContentConsumptionAsync(pxfRequestContext, startingAt.ToUniversalTime(), sources, search),
                                n => ccAdapter.GetNextContentConsumptionPageAsync(pxfRequestContext, n));
                            var filterSource = new FilterResourceSource<ContentConsumptionResource>(
                                pagingSource,
                                r =>
                                {
                                    switch (r.MediaType)
                                    {
                                        case ContentConsumptionResource.ContentType.Book:
                                            return !cardTypes.Contains(TimelineCard.CardTypes.BookConsumptionCard);
                                        case ContentConsumptionResource.ContentType.Episode:
                                            return !cardTypes.Contains(TimelineCard.CardTypes.EpisodeConsumptionCard);
                                        case ContentConsumptionResource.ContentType.Song:
                                            return !cardTypes.Contains(TimelineCard.CardTypes.SongConsumptionCard);
                                        case ContentConsumptionResource.ContentType.SurroundVideo:
                                            return !cardTypes.Contains(TimelineCard.CardTypes.SurroundVideoConsumptionCard);
                                        case ContentConsumptionResource.ContentType.Video:
                                            return !cardTypes.Contains(TimelineCard.CardTypes.VideoConsumptionCard);
                                    }

                                    throw new InvalidDataException($"Unknown content consumption type from provider: {r.MediaType}");
                                });
                            var transformSource = new TransformResourceSource<ContentConsumptionResource, TimelineCard>(
                                filterSource,
                                r =>
                                {
                                    switch (r.MediaType)
                                    {
                                        case ContentConsumptionResource.ContentType.Book:
                                            return new BookConsumptionCard(
                                                r.Id,
                                                r.Title,
                                                r.Artist,
                                                r.ContainerName,
                                                r.IconUrl,
                                                r.ContentUrl,
                                                r.AppName,
                                                r.ConsumptionTime,
                                                r.DateTime,
                                                r.DeviceId == null ? null : new[] { r.DeviceId },
                                                r.Sources);
                                        case ContentConsumptionResource.ContentType.Episode:
                                            return new EpisodeConsumptionCard(
                                                r.Id,
                                                r.Title,
                                                r.ContainerName,
                                                r.Artist,
                                                r.IconUrl,
                                                r.ContentUrl,
                                                r.AppName,
                                                r.ConsumptionTime,
                                                r.DateTime,
                                                r.DeviceId == null ? null : new[] { r.DeviceId },
                                                r.Sources);
                                        case ContentConsumptionResource.ContentType.Song:
                                            return new SongConsumptionCard(
                                                r.Id,
                                                r.Title,
                                                r.Artist,
                                                r.ContainerName,
                                                r.IconUrl,
                                                r.ContentUrl,
                                                r.AppName,
                                                r.ConsumptionTime,
                                                r.DateTime,
                                                r.DeviceId == null ? null : new[] { r.DeviceId },
                                                r.Sources);
                                        case ContentConsumptionResource.ContentType.SurroundVideo:
                                            return new SurroundVideoConsumptionCard(
                                                r.Id,
                                                r.Title,
                                                r.ContainerName,
                                                r.Artist,
                                                r.IconUrl,
                                                r.ContentUrl,
                                                r.AppName,
                                                r.ConsumptionTime,
                                                r.DateTime,
                                                r.DeviceId == null ? null : new[] { r.DeviceId },
                                                r.Sources);
                                        case ContentConsumptionResource.ContentType.Video:
                                            return new VideoConsumptionCard(
                                                r.Id,
                                                r.Title,
                                                r.ContainerName,
                                                r.Artist,
                                                r.IconUrl,
                                                r.ContentUrl,
                                                r.AppName,
                                                r.ConsumptionTime,
                                                r.DateTime,
                                                r.DeviceId == null ? null : new[] { r.DeviceId },
                                                r.Sources);
                                    }

                                    throw new InvalidDataException($"Unknown content consumption type from provider: {r.MediaType}");
                                });
                            resourceSources.Add(adapter.PartnerId, transformSource);
                        }

                        break;
                }
            }

            var mergingSource = new MergingResourceSource<TimelineCard>(
                (a, b) =>
                {
                    if (a.Timestamp.Ticks > b.Timestamp.Ticks)
                        return -1;
                    if (a.Timestamp.Ticks < b.Timestamp.Ticks)
                        return 1;
                    return 0;
                },
                resourceSources);
            var aggregatingSource = new AggregatingResourceSource<TimelineCard>(
                mergingSource,
                (a, b) => TimelineCard.Aggregate(timeZoneOffset, a, b));
            aggregatingSource.SetNextToken(nextToken);

            return new ServiceResponse<ExperienceContracts.V2.PagedResponse<TimelineCard>>
            {
                Result = new ExperienceContracts.V2.PagedResponse<TimelineCard>
                {
                    Items = await aggregatingSource.FetchAsync(count ?? 100).ConfigureAwait(false),
                    NextLink = aggregatingSource.GetNextToken()?.BuildNextLink(requestContext.CurrentUri, "nextToken")
                }
            };
        }

        public async Task<ServiceResponse<AggregateCountResponse>> GetAggregateCountAsync(IRequestContext requestContext, IList<string> cardTypes)
        {
            if (cardTypes == null)
                throw new ArgumentNullException(nameof(cardTypes));

            IPxfRequestContext pxfRequestContext = requestContext.ToAdapterRequestContext();

            Dictionary<string, ResourceType[]> cardTypeToResourceTypes;

            // Creates a mapping of TimelineCard Types(AMC) to ResourceTypes(PDOS)
            try
            {
                cardTypeToResourceTypes = cardTypes.ToDictionary(
                    c => c,
                    c =>
                    {
                        switch (c)
                        {
                            case TimelineCard.CardTypes.AppUsageCard:
                                this.logger.Information(nameof(TimelineService), $"Mapping to AppUsage resource type.");
                                return new[] { ResourceType.AppUsage };
                            case TimelineCard.CardTypes.BrowseCard:
                                this.logger.Information(nameof(TimelineService), $"Mapping to Browse resource type.");
                                return new[] { ResourceType.Browse };
                            case TimelineCard.CardTypes.SearchCard:
                                this.logger.Information(nameof(TimelineService), $"Mapping to Search resource type.");
                                return new[] { ResourceType.Search };
                            case TimelineCard.CardTypes.VoiceCard:
                                this.logger.Information(nameof(TimelineService), $"Mapping to Voice resource type.");
                                return new[] { ResourceType.Voice };
                            case TimelineCard.CardTypes.LocationCard:
                                this.logger.Information(nameof(TimelineService), $"Mapping to Location resource type.");
                                return new[] { ResourceType.Location };
                            case TimelineCard.CardTypes.ContentConsumptionCount:
                                this.logger.Information(nameof(TimelineService), $"Mapping to ContentConsumption resource type.");
                                return new[] { ResourceType.ContentConsumption };
                            case TimelineCard.CardTypes.BookConsumptionCard:
                            case TimelineCard.CardTypes.EpisodeConsumptionCard:
                            case TimelineCard.CardTypes.SongConsumptionCard:
                            case TimelineCard.CardTypes.SurroundVideoConsumptionCard:
                            case TimelineCard.CardTypes.VideoConsumptionCard:
                                throw new NotSupportedException($"CardType {c} is not a valid ResourceType to count");
                        }

                        throw new NotSupportedException($"Cannot map CardType {c} to ResourceType");
                    });
            }
            catch (NotSupportedException ex)
            {
                // If any types are unsupported, returns a failed call
                return new ServiceResponse<AggregateCountResponse>()
                {
                    Error = new Error(ErrorCode.InvalidInput, $"An unsupported cardtype was called. Exception Message: {ex.Message}")
                };
            }
            // Creates a mapping of resource types to CardTypes, to know the list of all resource types used
            Dictionary<ResourceType, string[]> resourceTypeToCardTypes = cardTypeToResourceTypes.Values
                .SelectMany(rt => rt)
                .Distinct()
                .ToDictionary(
                    rt => rt,
                rt => cardTypeToResourceTypes.Where(kvp => kvp.Value.Contains(rt)).Select(kvp => kvp.Key).Distinct().ToArray());

            // Looks up the Adapter for each resourceType
            Dictionary<ResourceType, PartnerAdapter[]> resourceTypeToPartnerAdapters = resourceTypeToCardTypes.Keys
                .ToDictionary(
                    t => t,
                    t => this.PxfDispatcher.GetAdaptersForResourceType(pxfRequestContext, t, PxfAdapterCapability.View).ToArray());
            this.logger.Information(nameof(TimelineService), $"Get corresponding adapter succeeded.");

            Dictionary<string, int> resourceCounts = new Dictionary<string, int>();

            // This is used to determine if all calls to PDOS have failed
            HashSet<ResourceType> resourceTypesToRetrieve = resourceTypeToPartnerAdapters.Keys.ToHashSet<ResourceType>();

            //Store the last exception in case all calls fail
            List<Exception> thrownExceptions = new List<Exception>();

            foreach (KeyValuePair<ResourceType, PartnerAdapter[]> cardType in resourceTypeToPartnerAdapters)
            {
                var resource = cardType.Key;
                var adapters = cardType.Value;
                switch (resource)
                {
                    case ResourceType.AppUsage:
                        try
                        {
                            foreach (var adapter in adapters)
                            {
                                if (adapter.Adapter is IAppUsageV2Adapter auAdapter)
                                {
                                    var response = await auAdapter.GetAppUsageAggregateCountAsync(pxfRequestContext);
                                    resourceCounts.Add(TimelineCard.CardTypes.AppUsageCard, response.Count);
                                    this.logger.Information(nameof(TimelineService), $"Get aggregate count for AppUsage. Count is {response.Count}");

                                    break;
                                }
                            }

                            // Successfully retrieved aggregate count
                            resourceTypesToRetrieve.Remove(ResourceType.AppUsage);
                        }
                        catch (Exception ex)
                        {
                            // If one call fails we want to return the remaining results adding an out of range value to our output
                            resourceCounts.Add(TimelineCard.CardTypes.AppUsageCard, int.MinValue);

                            this.logger.Error(nameof(TimelineService), $"Error occurred while retrieving AppUsageCard: {ex.Message}");
                            // Stores the exception in case the call fails
                            thrownExceptions.Add(ex);
                        }
                        break;

                    case ResourceType.Browse:
                        try
                        {
                            foreach (var adapter in adapters)
                            {
                                if (adapter.Adapter is IBrowseHistoryV2Adapter bhAdapter)
                                {
                                    var response = await bhAdapter.GetBrowseHistoryAggregateCountAsync(pxfRequestContext);
                                    resourceCounts.Add(TimelineCard.CardTypes.BrowseCard, response.Count);
                                    this.logger.Information(nameof(TimelineService), $"Get aggregate count for Browse. Count is {response.Count}");
                                    break;
                                }
                            }
                            resourceTypesToRetrieve.Remove(ResourceType.Browse);
                        }
                        catch (Exception ex)
                        {
                            // If one call fails we want to return the remaining results adding an out of range value to our output
                            resourceCounts.Add(TimelineCard.CardTypes.BrowseCard, int.MinValue);

                            this.logger.Error(nameof(TimelineService), $"Error occurred while retrieving BrowseCard: {ex.Message}");
                            thrownExceptions.Add(ex);
                        }
                        break;
                    case ResourceType.ContentConsumption:
                        try
                        {
                            foreach (var adapter in adapters)
                            {
                                if (adapter.Adapter is IContentConsumptionV2Adapter ccAdapter)
                                {
                                    var response = await ccAdapter.GetContentConsumptionAggregateCountAsync(pxfRequestContext);
                                    resourceCounts.Add(TimelineCard.CardTypes.ContentConsumptionCount, response.Count);
                                    this.logger.Information(nameof(TimelineService), $"Get aggregate count for ContentConsumptionCount. Count is {response.Count}");

                                    break;
                                }
                            }
                            resourceTypesToRetrieve.Remove(ResourceType.ContentConsumption);
                        }
                        catch (Exception ex)
                        {
                            // If one call fails we want to return the remaining results adding an out of range value to our output
                            resourceCounts.Add(TimelineCard.CardTypes.ContentConsumptionCount, int.MinValue);

                            this.logger.Error(nameof(TimelineService), $"Error occurred while retrieving ContentConsumptionCard: {ex.Message}");
                            thrownExceptions.Add(ex);
                        }
                        break;
                    case ResourceType.Location:
                        try
                        {
                            foreach (var adapter in adapters)
                            {
                                if (adapter.Adapter is ILocationV2Adapter locAdapter)
                                {
                                    var response = await locAdapter.GetLocationAggregateCountAsync(pxfRequestContext);
                                    resourceCounts.Add(TimelineCard.CardTypes.LocationCard, response.Count);
                                    this.logger.Information(nameof(TimelineService), $"Get aggregate count for Location. Count is {response.Count}");

                                    break;
                                }
                            }
                            resourceTypesToRetrieve.Remove(ResourceType.Location);
                        }
                        catch (Exception ex)
                        {
                            // If one call fails we want to return the remaining results adding an out of range value to our output
                            resourceCounts.Add(TimelineCard.CardTypes.LocationCard, int.MinValue);

                            this.logger.Error(nameof(TimelineService), $"Error occurred while retrieving LocationCard: {ex.Message}");
                            thrownExceptions.Add(ex);
                        }
                        break;
                    case ResourceType.Search:
                        try
                        {
                            foreach (var adapter in adapters)
                            {
                                if (adapter.Adapter is ISearchHistoryV2Adapter shAdapter)
                                {
                                    var response = await shAdapter.GetSearchHistoryAggregateCountAsync(pxfRequestContext);
                                    resourceCounts.Add(TimelineCard.CardTypes.SearchCard, response.Count);
                                    this.logger.Information(nameof(TimelineService), $"Get aggregate count for Search. Count is {response.Count}");

                                    break;
                                }
                            }
                            resourceTypesToRetrieve.Remove(ResourceType.Search);
                        }
                        catch (Exception ex)
                        {
                            // If one call fails we want to return the remaining results adding an out of range value to our output
                            resourceCounts.Add(TimelineCard.CardTypes.SearchCard, int.MinValue);

                            this.logger.Error(nameof(TimelineService), $"Error occurred while retrieving SearchCard: {ex.Message}");
                            thrownExceptions.Add(ex);
                        }
                        break;
                    case ResourceType.Voice:
                        try
                        {
                            foreach (var adapter in adapters)
                            {
                                if (adapter.Adapter is IVoiceHistoryV2Adapter vhAdapter)
                                {
                                    var response = await vhAdapter.GetVoiceHistoryAggregateCountAsync(pxfRequestContext);
                                    resourceCounts.Add(TimelineCard.CardTypes.VoiceCard, response.Count);
                                    this.logger.Information(nameof(TimelineService), $"Get aggregate count for Voice. Count is {response.Count}");
                                    break;
                                }
                            }
                            resourceTypesToRetrieve.Remove(ResourceType.Voice);
                        }
                        catch (Exception ex)
                        {
                            // If one call fails we want to return the remaining results adding an out of range value to our output
                            resourceCounts.Add(TimelineCard.CardTypes.VoiceCard, int.MinValue);

                            this.logger.Error(nameof(TimelineService), $"Error occurred while retrieving VoiceCard: {ex.Message}");
                            thrownExceptions.Add(ex);
                        }
                        break;
                }
            }

            var numberResourcesRequested = resourceTypeToPartnerAdapters.Keys.Count;

            // This means no requests were succesfully completed
            if (resourceTypesToRetrieve.Count == numberResourcesRequested)
            {
                var aggregatedException = new AggregateException(thrownExceptions);
                StringBuilder sb = new StringBuilder();
                foreach (var exception in thrownExceptions)
                {
                    sb.Append(exception.Message);
                }
                return new ServiceResponse<AggregateCountResponse>()
                {
                    Error = new Error(ErrorCode.PartnerError, $"All calls to obtain aggregate counts failed.  This is the list of exceptions: {sb.ToString()}")
                };
            }

            if (resourceCounts.Count > 0)
            {
                foreach (KeyValuePair<string, int> kvp in resourceCounts)
                {
                    this.logger.Information(nameof(TimelineService), $"The key value pair info: {kvp.Key} and {kvp.Value}");
                }

                return new ServiceResponse<AggregateCountResponse>()
                {
                    Result = new AggregateCountResponse()
                    {
                        AggregateCounts = resourceCounts
                    }
                };
            }
            else
            {
                return new ServiceResponse<AggregateCountResponse>()
                {
                    Result = new AggregateCountResponse()
                    {
                        AggregateCounts = null
                    }
                };
            }
        }
        public async Task<ServiceResponse<VoiceCardAudio>> GetVoiceCardAudioAsync(IRequestContext requestContext, string id)
        {
            if (requestContext == null)
                throw new ArgumentNullException(nameof(requestContext));
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            if (!(TimelineCard.DeserializeId(id) is VoiceCard card))
                throw new ArgumentOutOfRangeException(nameof(id), "Id is not for a voice card");

            IPxfRequestContext pxfRequestContext = requestContext.ToAdapterRequestContext();
            IEnumerable<PartnerAdapter> adapters = this.PxfDispatcher.GetAdaptersForResourceType(
                pxfRequestContext,
                ResourceType.VoiceAudio,
                PxfAdapterCapability.View);

            foreach (PartnerAdapter adapter in adapters)
            {
                VoiceAudioResource result;

                if (adapter.Adapter is IVoiceHistoryV2Adapter vhAdapter)
                    result = await vhAdapter.GetVoiceHistoryAudioAsync(pxfRequestContext, card.VoiceId, card.Timestamp).ConfigureAwait(false);
                else
                    result = await adapter.Adapter.GetVoiceHistoryAudioAsync(pxfRequestContext, card.VoiceId).ConfigureAwait(false);

                if (result != null)
                {
                    return result.Audio == null
                        ? new ServiceResponse<VoiceCardAudio> { Error = new Error(ErrorCode.PartnerError, "No audio chunks") }
                        : new ServiceResponse<VoiceCardAudio> { Result = new VoiceCardAudio(result.Audio) };
                }
            }

            return new ServiceResponse<VoiceCardAudio> { Error = new Error(ErrorCode.PartnerError, "No audio found") };
        }

        /// <inheritdoc />
        public async Task<ServiceResponse> WarmupAsync(IRequestContext requestContext)
        {
            IPxfRequestContext pxfRequestContext = requestContext.ToAdapterRequestContext();

            try
            {
                List<Task> warmupTasks = new[]
                    {
                        // Arbitrary list of resource types to use to look for IWarmableAdapters
                        ResourceType.Browse,
                        ResourceType.Search,
                        ResourceType.AppUsage,
                        ResourceType.ContentConsumption,
                        ResourceType.Location,
                        ResourceType.Voice
                    }
                    .SelectMany(
                        rt =>
                        {
                            try
                            {
                                return this.PxfDispatcher.GetAdaptersForResourceType(pxfRequestContext, rt, PxfAdapterCapability.View)
                                    .Select(a => (resourceType: rt, adapter: a.Adapter as IWarmableAdapter));
                            }
                            catch (Exception)
                            {
                                return Enumerable.Empty<(ResourceType resourceType, IWarmableAdapter adapter)>();
                            }
                        })
                    .Where(a => a.adapter != null)
                    .Select(a => a.adapter.WarmupAsync(pxfRequestContext, a.resourceType))
                    .ToList();

                if (warmupTasks.Count <= 0)
                {
                    return new ServiceResponse
                    {
                        Error = new Error(ErrorCode.PartnerError, "No warmable adapters found")
                    };
                }

                // Chunk the tasks up to avoid concurrent call limits
                int chunkSize = 3;
                for (int i = 0; i < warmupTasks.Count; i += chunkSize)
                {
                    var chunk = warmupTasks.GetRange(i, Math.Min(chunkSize, warmupTasks.Count - i));
                    await Task.WhenAll(chunk).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    Error = new Error(ErrorCode.PartnerError, ex.ToString())
                };
            }

            return new ServiceResponse();
        }

        /// <inheritdoc />
        public async Task<ServiceResponse<IList<GetRecurringDeleteResponse>>> GetRecurringDeletesAsync(IRequestContext requestContext, int maxNumberOfRetries)
        {
            if (requestContext == null)
                throw new ArgumentNullException(nameof(requestContext));

            List<GetRecurringDeleteResponse> responseRecords = new List<GetRecurringDeleteResponse>();
            var records = await this.scheduleDbClient.GetRecurringDeletesScheduleDbAsync(requestContext.TargetPuid, CancellationToken.None).ConfigureAwait(false);

            if (records == null || records.Count() == 0)
            {
                this.logger.Information(nameof(GetRecurringDeletesAsync), $"No records found for requested puid.");
                return new ServiceResponse<IList<GetRecurringDeleteResponse>>() { Result = new List<GetRecurringDeleteResponse>() };
            }

            this.logger.Information(nameof(GetRecurringDeletesAsync), $"Found {records?.Count()} records.");

            // copy records to response
            foreach (var record in records)
            {
                responseRecords.Add(new GetRecurringDeleteResponse(
                    puidValue: record.Puid,
                    dataType: record.DataType,
                    createDate: record.CreateDateUtc.Value,
                    updateDate: record.UpdateDateUtc.Value,
                    lastDeleteOccurrence: record.LastDeleteOccurrenceUtc,
                    nextDeleteOccurrence: record.NextDeleteOccurrenceUtc,
                    lastSucceededDeleteOccurrence: record.LastSucceededDeleteOccurrenceUtc,
                    numberOfRetries: record.NumberOfRetries.Value,
                    maxNumberOfRetries: maxNumberOfRetries,
                    status: record.RecurrentDeleteStatus.Value,
                    recurringIntervalDays: record.RecurringIntervalDays.Value
                    ));
            }

            return new ServiceResponse<IList<GetRecurringDeleteResponse>>() { Result = responseRecords };
        }

        /// <inheritdoc />
        public async Task<ServiceResponse> DeleteRecurringDeletesAsync(IRequestContext requestContext, string dataType)
        {
            if (requestContext == null)
                throw new ArgumentNullException(nameof(requestContext));
            if (string.IsNullOrEmpty(dataType))
                throw new ArgumentNullException(nameof(dataType));

            await this.scheduleDbClient.DeleteRecurringDeletesScheduleDbAsync(requestContext.TargetPuid, dataType, CancellationToken.None).ConfigureAwait(false);

            return new ServiceResponse();
        }

        /// <inheritdoc />
        public async Task<ServiceResponse<GetRecurringDeleteResponse>> CreateOrUpdateRecurringDeletesAsync(
            IRequestContext requestContext,
            string dataType,
            DateTimeOffset nextDeleteDate,
            RecurringIntervalDays recurringIntervalDays,
            RecurrentDeleteStatus status,
            IRecurringDeleteWorkerConfiguration recurringDeleteWorkerConfiguration)
        {
            if (requestContext == null)
                throw new ArgumentNullException(nameof(requestContext));
            if (string.IsNullOrEmpty(dataType))
                throw new ArgumentNullException(nameof(dataType));

            // validate data type
            Error error = RequestValidation.ValidatePrivacyDataTypes(new List<string>() { dataType }, this.privacyPolicy);
            if (error != null)
            {
                return new ServiceResponse<GetRecurringDeleteResponse> { Error = error };
            }

            long puidValue = requestContext.TargetPuid;
            DateTimeOffset? preVerifierExpirationDateUtc = null;
            Guid? id = null;
            DateTimeOffset utcNow = DateTimeOffset.UtcNow;
            string preVerifier = null;
            RecurrentDeleteScheduleDbDocument recurrentDeleteScheduleDbDocument = null;

            // check if record already exists
            var exists = await this.scheduleDbClient.HasRecurringDeletesScheduleDbRecordAsync(puidValue, dataType, CancellationToken.None);

            var preVerifierResponse = await this.msaIdentityServiceAdapter
                .GetGdprUserDeleteVerifierWithRefreshClaimAsync(requestContext.ToAdapterRequestContext());

            if (!preVerifierResponse.IsSuccess)
            {
                this.logger.Error(
                    nameof(CreateOrUpdateRecurringDeletesAsync),
                    $"Fail to request pre-verifier: AdapterErrorCode={preVerifierResponse.Error.Code}, AdapterErrorMessage={preVerifierResponse.Error.Message}");
                return PcfProxyService.HandleMsaIdentityAdapterError<GetRecurringDeleteResponse>(preVerifierResponse.Error);
            }

            // always update preverifier when user call us
            // in case if fail to refresh preverifier it should help us to get a new pre-verifier
            preVerifier = preVerifierResponse.Result;
            preVerifierExpirationDateUtc = MsaIdentityServiceAdapter.GetExpiryTimeFromVerifier(preVerifier);

            if (!exists)
            {
                // Create a new record and get a pre-verifier
                id = Guid.NewGuid();

                recurrentDeleteScheduleDbDocument = new RecurrentDeleteScheduleDbDocument(
                    puidValue: puidValue,
                    dataType: dataType,
                    documentId: id.ToString(),
                    createDateUtc: utcNow,
                    updateDateUtc: utcNow,
                    numberOfRetries: 0);
            }
            else
            {
                // get existing record
                recurrentDeleteScheduleDbDocument = await this.scheduleDbClient.GetRecurringDeletesScheduleDbDocumentAsync(puidValue, dataType, CancellationToken.None).ConfigureAwait(false);
            }

            // update record
            recurrentDeleteScheduleDbDocument.RecurrentDeleteStatus = status;
            recurrentDeleteScheduleDbDocument.NextDeleteOccurrenceUtc = nextDeleteDate;
            recurrentDeleteScheduleDbDocument.RecurringIntervalDays = recurringIntervalDays;
            recurrentDeleteScheduleDbDocument.PreVerifier = preVerifier;
            recurrentDeleteScheduleDbDocument.PreVerifierExpirationDateUtc = preVerifierExpirationDateUtc;

            this.logger.Information(nameof(CreateOrUpdateRecurringDeletesAsync),
                $"Id={recurrentDeleteScheduleDbDocument.DocumentId}, Exists={exists}, PreVerifierExpirationDateUtc={recurrentDeleteScheduleDbDocument.PreVerifierExpirationDateUtc}.");

            var scheduleDbResponse = await this.scheduleDbClient.CreateOrUpdateRecurringDeletesScheduleDbAsync(recurrentDeleteScheduleDbDocument).ConfigureAwait(false);

            ServiceResponse<GetRecurringDeleteResponse> response = new ServiceResponse<GetRecurringDeleteResponse>()
            {
                Result = new GetRecurringDeleteResponse(
                        puidValue: scheduleDbResponse.Puid,
                        dataType: scheduleDbResponse.DataType,
                        createDate: scheduleDbResponse.CreateDateUtc.Value,
                        updateDate: scheduleDbResponse.UpdateDateUtc.Value,
                        lastDeleteOccurrence: scheduleDbResponse.LastDeleteOccurrenceUtc,
                        nextDeleteOccurrence: scheduleDbResponse.NextDeleteOccurrenceUtc.Value,
                        lastSucceededDeleteOccurrence: scheduleDbResponse.LastSucceededDeleteOccurrenceUtc,
                        numberOfRetries: scheduleDbResponse.NumberOfRetries.Value,
                        maxNumberOfRetries: recurringDeleteWorkerConfiguration.ScheduleDbConfig.MaxNumberOfRetries,
                        status: scheduleDbResponse.RecurrentDeleteStatus.Value,
                        recurringIntervalDays: scheduleDbResponse.RecurringIntervalDays.Value)
            };

            return response;
        }
    }
}

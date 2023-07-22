// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.V2;
    using Microsoft.OData.Client;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Policy;

    /// <summary>
    ///     Fans out resource calls to multiple providers and aggregates the result
    /// </summary>
    /// <seealso cref="Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher.IPxfDispatcher" />
    public class PxfDispatcher : IPxfDispatcher
    {
        private readonly RingType defaultRingType;

        private readonly IList<IFlightConfiguration> flightConfigurations;

        private readonly ILogger logger;

        private readonly IList<ResourceType> requiredResourceTypes;

        private readonly Policy privacyPolicy = Policies.Current;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PxfDispatcher" /> class.
        /// </summary>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="dataManagementConfig">The data management config.</param>
        /// <param name="adapterFactory">The adapter factory.</param>
        /// <param name="certProvider">The cert provider.</param>
        /// <param name="aadTokenProvider">The aad token provider.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="counterFactory">The counter factory.</param>
        public PxfDispatcher(
            IPrivacyConfigurationManager configurationManager,
            IDataManagementConfig dataManagementConfig,
            IPxfAdapterFactory adapterFactory,
            ICertificateProvider certProvider,
            IAadTokenProvider aadTokenProvider,
            ILogger logger,
            ICounterFactory counterFactory)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            logger.Information(
                nameof(PxfDispatcher),
                $"PxfDispatcher initializing with {nameof(dataManagementConfig)} entries count of: {dataManagementConfig?.RingPartnerConfigMapping?.Count}.");

            if (configurationManager == null)
            {
                throw new ArgumentNullException(nameof(configurationManager));
            }
            if (dataManagementConfig == null)
            {
                throw new ArgumentNullException(nameof(dataManagementConfig));
            }
            if (dataManagementConfig.RingPartnerConfigMapping == null)
            {
                throw new ArgumentNullException(nameof(dataManagementConfig.RingPartnerConfigMapping));
            }

            this.defaultRingType = configurationManager.AdaptersConfiguration.DefaultTargetRing;
            this.requiredResourceTypes = configurationManager.AdaptersConfiguration.RequiredResourceTypes;
            this.RingResourcePartnerMapping = new Dictionary<RingType, Dictionary<ResourceType, IList<string>>>();
            this.RingPartnerAdapterMapping = new Dictionary<RingType, Dictionary<string, PartnerAdapter>>();

            this.flightConfigurations = configurationManager.AdaptersConfiguration.PrivacyFlightConfigurations ?? new List<IFlightConfiguration>();
            var flightRings = new StringBuilder();
            foreach (IFlightConfiguration flightConfiguration in this.flightConfigurations)
            {
                flightRings.AppendLine($"{flightConfiguration.FlightName}:{flightConfiguration.Ring}");
            }
            logger.Information(nameof(PxfDispatcher), $"Supported flight configuration(s): '{flightRings}'");

            foreach (KeyValuePair<string, IRingPartnerConfigMapping> ringPartnerConfigMapping in dataManagementConfig.RingPartnerConfigMapping)
            {
                IRingPartnerConfigMapping ringConfig = ringPartnerConfigMapping.Value;

                var partnerAdapterMapping = new Dictionary<string, PartnerAdapter>();
                var resourcePartnerMapping = new Dictionary<ResourceType, IList<string>>();

                foreach (KeyValuePair<string, IPxfPartnerConfiguration> pxfPartnerConfiguration in ringConfig.PartnerConfigMapping)
                {
                    IPxfPartnerConfiguration config = pxfPartnerConfiguration.Value;

                    ValidatePartnerConfig(config);

                    if (partnerAdapterMapping.ContainsKey(config.Id))
                    {
                        throw new ArgumentException("partnerConfigurations contains duplicate entries with same PartnerId value.");
                    }

                    IMsaIdentityServiceConfiguration msaIdentityConfig = configurationManager.MsaIdentityServiceConfiguration;

                    PartnerAdapter adapter = adapterFactory.Create(
                        certProvider,
                        msaIdentityConfig,
                        config,
                        aadTokenProvider,
                        logger,
                        counterFactory);

                    partnerAdapterMapping.Add(config.Id, adapter);

                    MapGetPartnerResources(config, resourcePartnerMapping);
                }

                foreach (KeyValuePair<ResourceType, IList<string>> map in resourcePartnerMapping)
                {
                    logger.Information(nameof(PxfDispatcher), $"Ring: {ringConfig.Ring}, Resource Type: {map.Key}, # Adapters configured: {map.Value?.Count}");
                }

                this.RingResourcePartnerMapping.Add(ringConfig.Ring, resourcePartnerMapping);
                this.RingPartnerAdapterMapping.Add(ringConfig.Ring, partnerAdapterMapping);
            }
        }

        /// <summary>
        ///     Execute a function for each adapter and return all of the results
        /// </summary>
        public async Task<IList<T>> ExecuteForProvidersAsync<T>(
            IPxfRequestContext requestContext,
            ResourceType resourceType,
            PxfAdapterCapability capability,
            Func<PartnerAdapter, Task<T>> execFunc)
        {
            List<Task<T>> tasks = this.GetAdaptersForResourceType(requestContext, resourceType, capability).Select(execFunc).ToList();

            await Task.WhenAll(tasks).ConfigureAwait(false);

            return tasks.Select(t => t.GetAwaiter().GetResult()).ToList();
        }

        public IEnumerable<PartnerAdapter> GetAdaptersForResourceType(IPxfRequestContext context, ResourceType resourceType, PxfAdapterCapability capability)
        {
            RingType targetRingType = FlightRingHelper.CalculateTargetRingType(context, this.flightConfigurations, this.defaultRingType);
            this.ValidateAdapterMapping(resourceType, targetRingType);
            this.ValidateAtLeastOneAdapterCanPerformCapability(targetRingType, capability);

            Dictionary<ResourceType, IList<string>> resourceTypeDictionary;
            if (!this.RingResourcePartnerMapping.TryGetValue(targetRingType, out resourceTypeDictionary))
                return Enumerable.Empty<PartnerAdapter>();

            // Find adapters in the ring by resource-type
            IList<string> list;
            if (!resourceTypeDictionary.TryGetValue(resourceType, out list))
                return Enumerable.Empty<PartnerAdapter>();

            var results = new List<PartnerAdapter>();
            foreach (string partnerId in list)
            {
                if (!this.RingPartnerAdapterMapping.ContainsKey(targetRingType))
                {
                    // If this happens, we need a server error to investigate.
                    throw new NotSupportedException(
                        $"The ring type ({targetRingType}) this request maps to does not contain any valid configurations. " +
                        $"Supported rings include: {string.Join(",", this.RingPartnerAdapterMapping.Keys)}. " +
                        $"The request context flight headers are: {string.Join(",", context.Flights)}");
                }

                Dictionary<string, PartnerAdapter> partnerAdapterMapping = this.RingPartnerAdapterMapping[targetRingType];
                PartnerAdapter adapter = partnerAdapterMapping[partnerId];
                if (capability == PxfAdapterCapability.View && !adapter.RealTimeView)
                    continue;
                if (capability == PxfAdapterCapability.Delete && !adapter.RealTimeDelete)
                    continue;

                results.Add(partnerAdapterMapping[partnerId]);
            }

            IfxTraceLogger.Instance.Information(nameof(PxfDispatcher), $"GetAdaptersForResourceType({resourceType}): ringType={targetRingType}");

            IfxTraceLogger.Instance.Information(
                nameof(PxfDispatcher),
                $"GetAdaptersForResourceType({resourceType}): results: [{string.Join(",", results.Select(r => r.Adapter.GetType().Name + ":" + r.PartnerId))}]");
            return results;
        }

        internal Dictionary<RingType, Dictionary<string, PartnerAdapter>> RingPartnerAdapterMapping { get; }

        internal Dictionary<RingType, Dictionary<ResourceType, IList<string>>> RingResourcePartnerMapping { get; }

        private async Task<DeletionResponse<DeleteResourceResponse>> DeleteResourceAsync(
            ResourceType resourceType,
            Func<IPxfAdapter, IPxfRequestContext, Task<DeleteResourceResponse>> func,
            IPxfRequestContext requestContext)
        {
            // Get all adapters for this resourceType that can do real-time-deletes
            List<PartnerAdapter> partnerAdapters = this.GetAdaptersForResourceType(requestContext, resourceType, PxfAdapterCapability.Delete).ToList();

            // Call each of the partners in parallel and wait for all requests to complete
            var requestTasks = new List<Task<DeleteResourceResponse>>();
            foreach (PartnerAdapter partnerAdapter in partnerAdapters)
            {
                Task<DeleteResourceResponse> task = func(partnerAdapter.Adapter, requestContext);
                requestTasks.Add(task);
            }
            await Task.WhenAll(requestTasks).ConfigureAwait(false);

            var deletionResponse = new DeletionResponse<DeleteResourceResponse>(
                requestTasks.Where(response => response != null && response.Result != null).Select(response => response.Result)
            );

            return deletionResponse;
        }

        private void ValidateAdapterMapping(ResourceType resourceType, RingType ringType)
        {
            if (this.RingPartnerAdapterMapping == null || this.RingResourcePartnerMapping == null)
            {
                throw new NotSupportedException("Operation not supported until this instance has been initialized.");
            }

            // Need to make sure the dictionaries contain the target ring type first.
            if (!this.RingResourcePartnerMapping.ContainsKey(ringType))
            {
                string warningMessage =
                    $"Operation not supported until this instance has been initialized. Ring type not found: {ringType} in {nameof(this.RingResourcePartnerMapping)}";
                this.logger.Warning(nameof(PxfDispatcher), warningMessage);
                throw new NotSupportedException(warningMessage);
            }

            if (!this.RingPartnerAdapterMapping.ContainsKey(ringType))
            {
                string warningMessage =
                    $"Operation not supported until this instance has been initialized. Ring type not found: {ringType} in {nameof(this.RingPartnerAdapterMapping)}";
                this.logger.Warning(nameof(PxfDispatcher), warningMessage);
                throw new NotSupportedException(warningMessage);
            }

            // Then make sure they have the resource type.
            if (this.RingPartnerAdapterMapping[ringType]?.Values == null || this.RingResourcePartnerMapping[ringType]?.Values == null)
            {
                throw new NotSupportedException("Operation not supported until this instance has been initialized.");
            }

            if (this.requiredResourceTypes != null && !this.requiredResourceTypes.Contains(resourceType) && !this.RingResourcePartnerMapping[ringType].ContainsKey(resourceType))
            {
                this.logger.Warning(nameof(PxfDispatcher), $"The resource type: {resourceType} is not configured with any partner adapters for ring type: {ringType}.");
                return;
            }

            if (!this.RingResourcePartnerMapping[ringType].ContainsKey(resourceType))
            {
                throw new NotSupportedException($"No configured partners support resource type: {resourceType} for ring type: {ringType}");
            }
        }

        private void ValidateAtLeastOneAdapterCanPerformCapability(RingType ringType, PxfAdapterCapability capability)
        {
            string errorMessage = $"No configured partners support {capability} for ring: {ringType}.";

            // if the ring doesn't have any mapping, it has no adapters
            if (!this.RingPartnerAdapterMapping.ContainsKey(ringType))
            {
                this.logger.Error(nameof(PxfDispatcher), errorMessage);
                throw new NotSupportedException(errorMessage);
            }

            // if at least one adapter can support the capability, validation succeeds
            switch (capability)
            {
                case PxfAdapterCapability.View:
                    if (this.RingPartnerAdapterMapping[ringType].Any(pair => pair.Value != null && pair.Value.RealTimeView))
                    {
                        return;
                    }
                    break;
                case PxfAdapterCapability.Delete:
                    if (this.RingPartnerAdapterMapping[ringType].Any(pair => pair.Value != null && pair.Value.RealTimeDelete))
                    {
                        return;
                    }
                    break;
            }

            this.logger.Error(nameof(PxfDispatcher), errorMessage);
            throw new NotSupportedException(errorMessage);
        }

        #region Initialization

        private static void ValidatePartnerConfig(IPxfPartnerConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(config.Id))
            {
                throw new ArgumentException($"{nameof(config)} contains a bad input. Missing PartnerId.");
            }

            if (string.IsNullOrWhiteSpace(config.BaseUrl))
            {
                throw new ArgumentException($"{nameof(config)} contains a bad input. Missing BaseUrl.");
            }
        }

        private static void MapGetPartnerResources(IPxfPartnerConfiguration config, Dictionary<ResourceType, IList<string>> resourcePartnerMapping)
        {
            if (resourcePartnerMapping == null)
            {
                resourcePartnerMapping = new Dictionary<ResourceType, IList<string>>();
            }

            // For now, it is OK to have a partner with no supported resources. Eventually, this may be an initilazation error.
            if (config.SupportedResources == null)
            {
                return;
            }

            foreach (string resource in config.SupportedResources)
            {
                ResourceType resourceType;
                if (Enum.TryParse(resource, true, out resourceType))
                {
                    if (!resourcePartnerMapping.ContainsKey(resourceType))
                    {
                        resourcePartnerMapping[resourceType] = new List<string>();
                    }

                    resourcePartnerMapping[resourceType].Add(config.Id);
                }
                else
                {
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Partner '{0}' contains an unknown resource type '{1}'", config.Id, resource));
                }
            }
        }

        #endregion // Initialization

        #region Delete

        /// <summary>
        ///     Deletes the browse history from configured partners.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <returns>
        ///     A Collection of deletion responses from all of the partners for this resource.
        /// </returns>
        public async Task<DeletionResponse<DeleteResourceResponse>> DeleteBrowseHistoryAsync(IPxfRequestContext requestContext)
        {
            return await this.DeleteResourceAsync(ResourceType.Browse, (a, rc) => a.DeleteBrowseHistoryAsync(rc), requestContext).ConfigureAwait(false);
        }

        /// <summary>
        ///     Deletes the AppUsage from configured partners.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <returns>
        ///     A Collection of deletion responses from all of the partners for this resource.
        /// </returns>
        public async Task<DeletionResponse<DeleteResourceResponse>> DeleteAppUsageAsync(IPxfRequestContext requestContext)
        {
            return await this.DeleteResourceAsync(ResourceType.AppUsage, (a, rc) => a.DeleteAppUsageAsync(rc), requestContext).ConfigureAwait(false);
        }

        /// <summary>
        ///     Deletes the voice history from configured partners.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <returns>
        ///     A Collection of deletion responses from all of the partners for this resource.
        /// </returns>
        public async Task<DeletionResponse<DeleteResourceResponse>> DeleteVoiceHistoryAsync(IPxfRequestContext requestContext)
        {
            return await this.DeleteResourceAsync(ResourceType.Voice, (a, rc) => a.DeleteVoiceHistoryAsync(rc), requestContext).ConfigureAwait(false);
        }

        /// <summary>
        ///     Deletes the location history from configured partners.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <returns>
        ///     A Collection of deletion responses from all of the partners for this resource.
        /// </returns>
        public async Task<DeletionResponse<DeleteResourceResponse>> DeleteLocationHistoryAsync(IPxfRequestContext requestContext)
        {
            return await this.DeleteResourceAsync(ResourceType.Location, (a, rc) => a.DeleteLocationHistoryAsync(rc), requestContext).ConfigureAwait(false);
        }

        /// <summary>
        ///     Deletes the search history from configured partners.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="disableThrottling">if set to <c>true</c>, disable throttling for the request. Only test callerName is allowed to pass <c>true</c> for this.</param>
        /// <returns>A Collection of deletion responses from all of the partners for this resource.</returns>
        public async Task<DeletionResponse<DeleteResourceResponse>> DeleteSearchHistoryAsync(IPxfRequestContext requestContext, bool disableThrottling)
        {
            return await this.DeleteResourceAsync(ResourceType.Search, (a, rc) => a.DeleteSearchHistoryAsync(rc, disableThrottling), requestContext).ConfigureAwait(false);
        }

        /// inheritdoc
        public Task<DeletionResponse<DeleteResourceResponse>> CreateDeletePolicyDataTypeTask(string dataType, IPxfRequestContext pxfRequestContext)
        {
            Task<DeletionResponse<DeleteResourceResponse>> deleteTask = null;

            // There is a bit of asymmetry by design with the get calls and this delete call.
            // Here, we are deleting data types. In gets and single deletes, we are deleting cards.
            // Cards are a PXS/UX concept, but these data types are a bit wider reaching concept,
            // and when clicking to bulk delete in the UX, the list of options will be from this
            // set, and not by card types. Card types may not map 1 to 1 with these data types.
            if (string.Equals(dataType, this.privacyPolicy.DataTypes.Ids.ProductAndServiceUsage.Value))
            {
                deleteTask = this.DeleteAppUsageAsync(pxfRequestContext);
            }
            else if (string.Equals(dataType, this.privacyPolicy.DataTypes.Ids.InkingTypingAndSpeechUtterance.Value))
            {
                deleteTask = this.DeleteVoiceHistoryAsync(pxfRequestContext);
            }
            else if (string.Equals(dataType, this.privacyPolicy.DataTypes.Ids.BrowsingHistory.Value))
            {
                deleteTask = this.DeleteBrowseHistoryAsync(pxfRequestContext);
            }
            else if (string.Equals(dataType, this.privacyPolicy.DataTypes.Ids.SearchRequestsAndQuery.Value))
            {
                deleteTask = this.DeleteSearchHistoryAsync(pxfRequestContext, false);
            }
            else if (string.Equals(dataType, this.privacyPolicy.DataTypes.Ids.PreciseUserLocation.Value))
            {
                deleteTask = this.DeleteLocationHistoryAsync(pxfRequestContext);
            }
            else if (string.Equals(dataType, this.privacyPolicy.DataTypes.Ids.ContentConsumption.Value))
            {
                async Task<DeletionResponse<DeleteResourceResponse>> DeleteAsync()
                {
                    IList<DeleteResourceResponse> results = await this.ExecuteForProvidersAsync(
                            pxfRequestContext,
                            ResourceType.ContentConsumption,
                            PxfAdapterCapability.Delete,
                            async a =>
                            {
                                if (a.Adapter is IContentConsumptionV2Adapter ccAdapter)
                                {
                                    return await ccAdapter.DeleteContentConsumptionAsync(pxfRequestContext).ConfigureAwait(false);
                                }

                                return new DeleteResourceResponse
                                {
                                    PartnerId = a.PartnerId,
                                    ErrorMessage = "Unknown adapter type",
                                    Status = ResourceStatus.Error
                                };
                            })
                        .ConfigureAwait(false);
                    return new DeletionResponse<DeleteResourceResponse>(results);
                }

                deleteTask = DeleteAsync();
            }

            // Any type that doesn't have a corresponding PD API to call has no additional requests. Since at this point
            // the delete request has been logged to storage, that's all that's required.
            return deleteTask;
        }

        #endregion // Delete
    }
}

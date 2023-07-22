// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.QuickExportWorker.DataProcessingAgents
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.DataContracts.ExportTypes;
    using Microsoft.Membership.MemberServices.Privacy.QuickExportWorker.CsvSerializer;
    using Microsoft.Membership.MemberServices.PrivacyAdapters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.V2;
    using Microsoft.Practices.TransientFaultHandling;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Policy;

    /// <summary>
    /// Retrieves data from several PDP API resources and writes data to the output export staging container.
    /// </summary>
    public class PdApiDataResourceAgent
    {
        // public so that tests can access 
        public const string RawUserLocationsFileName = "RawUserLocations.csv";
        public const string UserVisitLocationsFileName = "UserVisitLocations.csv";

        private const string voiceAudioFormat = "mp4";
        private const string voiceAudioPrefix = "audio";
        private readonly ICounterFactory counterFactory;
        private readonly ILogger logger;
        private readonly IPrivacyExportConfiguration exportConfiguration;
        private readonly IPxfDispatcher pxfDispatcher;
        private readonly ISerializer serializer;
        private readonly RetryManager retryManager;

        public PdApiDataResourceAgent(
            IPxfDispatcher pxfDispatcher,
            ICounterFactory counterFactory,
            ISerializer serializer,
            IPrivacyConfigurationManager privacyConfigurationManager,
            ILogger logger)
        {
            this.pxfDispatcher = pxfDispatcher;
            this.counterFactory = counterFactory;
            this.serializer = serializer;
            this.logger = logger;

            this.exportConfiguration = privacyConfigurationManager.PrivacyExperienceServiceConfiguration.PrivacyExportConfiguration;
            IRetryStrategyConfiguration strategy = this.exportConfiguration.RetryStrategy;
            ITransientErrorDetectionStrategy errorDetectionStrategy = QuickExportTransientErrorDetector.Instance;
            this.retryManager = new RetryManager(strategy, logger, errorDetectionStrategy);
        }

        /// <summary>
        /// Processes a Quick export request and writes data to the export container.
        /// </summary>
        public async Task ProcessExportAsync(
            ExportStatusRecord statusRecord,
            ExportDataResourceStatus resourceStatus,
            IExportStagingStorageHelper stagingHelper)
        {
            resourceStatus.LastSessionStart = DateTime.UtcNow;
            DateTime startTime = statusRecord.StartTime == DateTimeOffset.MinValue ? new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc) : statusRecord.StartTime.UtcDateTime;
            DateTime endTime = statusRecord.EndTime == DateTimeOffset.MaxValue ? DateTime.UtcNow : statusRecord.EndTime.UtcDateTime;

            try
            {
                if (!ExportStatusRecord.ParseUserId(statusRecord.UserId, out long puid))
                {
                    this.logger.Information(
                        nameof(PdApiDataResourceAgent),
                        $"status record id is not a valid long, id {statusRecord.UserId} req {statusRecord.ExportId}");
                    throw new ArgumentException("status record id is not a valid long");
                }

                var requestContext = new PxfRequestContext(
                    statusRecord.Ticket,
                    null,
                    puid,
                    puid,
                    null,
                    "us",
                    false,
                    statusRecord.Flights ?? new string[0]);

                bool wroteAnything = false;
                bool needCommit = false;
                var savedVoiceAudioIds = new List<Tuple<string, DateTimeOffset>>();

                using (IExportStagingFile file1 = stagingHelper.GetStagingFile(resourceStatus.ResourceDataType + ".csv"))
                {
                    if (resourceStatus.ResourceDataType == Policies.Current.DataTypes.Ids.BrowsingHistory.Value)
                    {
                        string[] header = new string[] { "DateTime", "DeviceId", "NavigatedToUrl", "PageTitle", "SearchTerms" };
                        await this.WriteFileHeader(file1, header);
                        this.logger.Information(
                            nameof(PdApiDataResourceAgent),
                            $"About to get BrowseHistory for {puid} from {startTime} to {endTime} dispatcher {this.pxfDispatcher}");
                        needCommit = await this.WritePxfDataAsync(
                            requestContext,
                            file1,
                            ResourceType.Browse,
                            null,
                            a => a.Adapter.GetBrowseHistoryAsync(
                                requestContext,
                                OrderByType.DateTime,
                                DateOption.Between,
                                startTime,
                                endTime),
                            (a, n) => a.Adapter.GetNextBrowsePageAsync(requestContext, new Uri(n))).ConfigureAwait(false);
                    }
                    else if (resourceStatus.ResourceDataType == Policies.Current.DataTypes.Ids.SearchRequestsAndQuery.Value)
                    {
                        // Please ensure the csv column names match the field order listed in the CsvSerializer file
                        string[] header = new string[] { "DateTime", "DeviceId", "AccuracyRadius", "Latitude", "Longitude", "Time", "Title", "Url", "SearchTerms" };
                        await this.WriteFileHeader(file1, header);

                        this.logger.Information(
                            nameof(PdApiDataResourceAgent),
                            $"About to get SearchHistory for {puid} from {startTime} to {endTime} dispatcher {this.pxfDispatcher}");
                        needCommit = await this.WritePxfDataAsync(
                            requestContext,
                            file1,
                            ResourceType.Search,
                            null,
                            a => a.Adapter.GetSearchHistoryAsync(
                                requestContext,
                                OrderByType.DateTime,
                                DateOption.Between,
                                startTime,
                                endTime),
                            (a, n) => a.Adapter.GetNextSearchPageAsync(requestContext, new Uri(n))).ConfigureAwait(false);
                    }
                    else if (resourceStatus.ResourceDataType == Policies.Current.DataTypes.Ids.ProductAndServiceUsage.Value)
                    {
                        // Please ensure the csv column names match the field order listed in the CsvSerializer file
                        string[] header = new string[] { "DateTime", "EndDateTime", "DeviceId", "Aggregation", "AppName", "AppPublisher" };
                        await this.WriteFileHeader(file1, header);

                        this.logger.Information(
                            nameof(PdApiDataResourceAgent),
                            $"About to get AppUsage for {puid} from {startTime} to {endTime} dispatcher {this.pxfDispatcher}");

                        needCommit = await this.WritePxfDataAsync(
                            requestContext,
                            file1,
                            ResourceType.AppUsage,
                            null,
                            a => a.Adapter.GetAppUsageAsync(
                                requestContext,
                                OrderByType.DateTime,
                                DateOption.Between,
                                startTime,
                                endTime),
                            (a, n) => a.Adapter.GetNextAppUsagePageAsync(requestContext, new Uri(n))).ConfigureAwait(false);
                    }
                    else if (resourceStatus.ResourceDataType == Policies.Current.DataTypes.Ids.InkingTypingAndSpeechUtterance.Value)
                    {
                        // Please ensure the csv column names match the field order listed in the CsvSerializer file
                        string[] header = new string[] { "DateTime", "DeviceId", "Application", "DeviceType", "DisplayText" };
                        await this.WriteFileHeader(file1, header);

                        this.logger.Information(
                            nameof(PdApiDataResourceAgent),
                            $"About to get VoiceHistory for {puid} from {startTime} to {endTime} dispatcher {this.pxfDispatcher}");

                        needCommit = await this.WritePxfDataAsync(
                            requestContext,
                            file1,
                            ResourceType.Voice,
                            savedVoiceAudioIds,
                            a => a.Adapter.GetVoiceHistoryAsync(
                                requestContext,
                                OrderByType.DateTime,
                                DateOption.Between,
                                startTime,
                                endTime),
                            (a, n) => a.Adapter.GetNextVoicePageAsync(requestContext, new Uri(n))).ConfigureAwait(false);
                    }
                    else if (resourceStatus.ResourceDataType == Policies.Current.DataTypes.Ids.ContentConsumption.Value)
                    {
                        // Please ensure the csv column names match the field order listed in the CsvSerializer file
                        string[] header = new string[] { "DateTime", "DeviceId", "AppName", "Artist", "ConsumptionTime", "ContainerName", "ContentUrl","IconUrl", "MediaType", "Title"};
                        await this.WriteFileHeader(file1, header);

                        this.logger.Information(
                           nameof(PdApiDataResourceAgent),
                           $"About to get ContentConsumption for {puid} from {startTime} to {endTime} dispatcher {this.pxfDispatcher}");

                        needCommit = await this.WritePxfDataAsync(
                            requestContext,
                            file1,
                            ResourceType.ContentConsumption,
                            null,
                            a => ((IContentConsumptionV2Adapter)a.Adapter).GetContentConsumptionAsync(
                                requestContext,
                                endTime,
                                null,
                                null),
                            (a, n) => ((IContentConsumptionV2Adapter)a.Adapter).GetNextContentConsumptionPageAsync(requestContext, new Uri(n))).ConfigureAwait(false);
                    }
                    else if (resourceStatus.ResourceDataType == Policies.Current.DataTypes.Ids.PreciseUserLocation.Value)
                    {
                        wroteAnything |= await WriteLocationData(stagingHelper, startTime, endTime, requestContext).ConfigureAwait(false);
                    }
                    else
                    {
                        this.logger.Error(nameof(PdApiDataResourceAgent), "Unknown data type: " + resourceStatus.ResourceDataType);
                        throw new NotSupportedException($"Data type '{resourceStatus.ResourceDataType}' not supported");
                    }

                    if (needCommit)
                    {
                        wroteAnything = true;
                        await file1.CommitAsync().ConfigureAwait(false);
                    }
                }

                int fileNumber = 0;
                foreach (Tuple<string, DateTimeOffset> voiceAudioId in savedVoiceAudioIds)
                {
                    VoiceAudioResource vaResult =
                        await this.GetVoiceAudioAsync(requestContext, voiceAudioId.Item1, voiceAudioId.Item2).ConfigureAwait(false);
                    if (vaResult?.Audio != null)
                    {
                        wroteAnything = true;
                        fileNumber++;
                        using (IExportStagingFile file = stagingHelper.GetStagingFile(
                            $"{resourceStatus.ResourceDataType}/{voiceAudioPrefix}-{fileNumber.ToString("000")}-{voiceAudioId.Item1}.{ voiceAudioFormat}"))
                        {
                            await file.AddBlockAsync(vaResult.Audio).ConfigureAwait(false);
                            await file.CommitAsync().ConfigureAwait(false);
                        }
                    }
                }

                if (!wroteAnything)
                {
                    this.logger.Information(
                        nameof(PdApiDataResourceAgent),
                        $"No data for id {statusRecord.UserId} req {statusRecord.ExportId} resourceType {resourceStatus.ResourceDataType}");
                }

                resourceStatus.IsComplete = true;
                resourceStatus.LastSessionEnd = DateTime.UtcNow;

                this.logger.Information(
                    nameof(PdApiDataResourceAgent),
                    $"PDApiDataResourceAgent.ProcessExportAsync complete {resourceStatus.ResourceDataType} {resourceStatus.ResourceDataType}");
            }
            catch (Exception ex)
            {
                this.logger.Error(nameof(PdApiDataResourceAgent), "exception " + ex);
                throw;
            }
        }

        private async Task<bool> WriteLocationData(
            IExportStagingStorageHelper stagingHelper,
            DateTime startTime,
            DateTime endTime,
            PxfRequestContext requestContext)
        {
            bool wroteAnything = false;
            using (IExportStagingFile locationFile = stagingHelper.GetStagingFile(UserVisitLocationsFileName))
            {
                bool wroteLocationData = false;

                // Please ensure the csv column names match the field order listed in the CsvSerializer file
                string[] header = new string[] {
                    "DateTime", "DeviceId", "AccuracyRadius", "ActivityType", "AddressLine1", "AddressLine2", "AddressLine3","CountryRegion",
                    "FormattedAddress", "Locality", "PostalCode", "DeviceType", "Distance", "EndDateTime", "Latitude", "Longitude", "Name", "Url", "AppName"};
                await this.WriteFileHeader(locationFile, header);

                foreach (ResourceType locType in new[] { ResourceType.Location, ResourceType.MicrosoftHealthLocation })
                {
                    wroteLocationData |= await this.WritePxfDataAsync(
                       requestContext,
                       locationFile,
                       locType,
                       null,
                       a => a.Adapter.GetLocationHistoryAsync(
                           requestContext,
                           OrderByType.DateTime,
                           DateOption.Between,
                           startTime,
                           endTime),
                       (a, n) => a.Adapter.GetNextLocationPageAsync(requestContext, new Uri(n))).ConfigureAwait(false);
                }

                if (wroteLocationData)
                {
                    await locationFile.CommitAsync().ConfigureAwait(false);
                    locationFile.Dispose();

                    wroteAnything = true;
                }
            }

            try
            {
                using (IExportStagingFile beaconFile = stagingHelper.GetStagingFile(RawUserLocationsFileName))
                {
                    bool wroteBeaconData = false;

                    // Please ensure the csv column names match the field order listed in the CsvSerializer file
                    string[] header = new string[] {
                        "DateTime", "DeviceId", "AccuracyRadius", "ActivityType", "AddressLine1", "AddressLine2", "AddressLine3","CountryRegion",
                        "FormattedAddress", "Locality", "PostalCode", "DeviceType", "Distance", "EndDateTime", "Latitude", "Longitude", "Name", "Url", "AppName"};
                    await this.WriteFileHeader(beaconFile, header);

                    wroteBeaconData |= await this.WritePxfDataAsync(
                       requestContext,
                       beaconFile,
                       ResourceType.LocationTransit,
                       null,
                       a => a.Adapter.GetLocationHistoryAsync(
                           requestContext,
                           OrderByType.DateTime,
                           DateOption.Between,
                           startTime,
                           endTime),
                       (a, n) => a.Adapter.GetNextLocationPageAsync(requestContext, new Uri(n))).ConfigureAwait(false);

                    if (wroteBeaconData)
                    {
                        await beaconFile.CommitAsync().ConfigureAwait(false);
                        beaconFile.Dispose();
                        wroteAnything = true;
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.Error(nameof(PdApiDataResourceAgent), "Beacon export exception: " + ex);
                throw;
            }

            return wroteAnything;
        }

        private async Task<VoiceAudioResource> GetVoiceAudioAsync(PxfRequestContext requestContext, string id, DateTimeOffset timestamp)
        {
            IEnumerable<PartnerAdapter> adapters = this.pxfDispatcher.GetAdaptersForResourceType(
                requestContext,
                ResourceType.VoiceAudio,
                PxfAdapterCapability.View);

            string[] idParts = id.Split(new[] { ',' }, StringSplitOptions.None);
            id = idParts[0];
            if (idParts.Length > 1)
            {
                timestamp = DateTimeOffset.MinValue;
                if (long.TryParse(idParts[1], out long ticks))
                    timestamp = new DateTimeOffset(ticks, TimeSpan.Zero);
            }

            foreach (PartnerAdapter adapter in adapters)
            {
                var vhAdapter = (IVoiceHistoryV2Adapter)adapter.Adapter;
                VoiceAudioResource result = await this.retryManager.ExecuteAsync(
                    nameof(PdApiDataResourceAgent),
                    nameof(this.GetVoiceAudioAsync),
                    () => vhAdapter.GetVoiceHistoryAudioAsync(requestContext, id, timestamp)).ConfigureAwait(false);

                if (result != null)
                {
                    if (result.Audio == null)
                    {
                        // This used to throw in the conversion, which would fail exports. Now we log the scenario and skip it.
                        ICounter counter = this.counterFactory.GetCounter(nameof(PdApiDataResourceAgent), "NoAudioChunks", CounterType.Number);
                        counter.Increment();
                        this.logger.Error(nameof(PdApiDataResourceAgent), $"No audio chunks for impression '{id}'");
                    }
                    else
                    {
                        ICounter counter = this.counterFactory.GetCounter(nameof(PdApiDataResourceAgent), "GoodAudioChunks", CounterType.Number);
                        counter.Increment();
                    }

                    return result;
                }
            }

            return null;
        }

        private async Task<bool> WriteFileHeader(IExportStagingFile file, string[] header)
        {
            IEnumerable<string> headerList = new List<string>(header);
            await file.AddBlockAsync(this.serializer.WriteHeader(headerList));
            return true;
        }

        private async Task<bool> WritePxfDataAsync<T>(
            IPxfRequestContext context,
            IExportStagingFile file,
            ResourceType resourceType,
            IList<Tuple<string, DateTimeOffset>> saveIds,
            Func<PartnerAdapter, Task<PagedResponse<T>>> firstPageFunc,
            Func<PartnerAdapter, string, Task<PagedResponse<T>>> nextPageFunc)
            where T : Resource
        {
            const int blockSize = 10000;

            // Build the resource source
            var sources = new Dictionary<string, IResourceSource<T>>();
            foreach (PartnerAdapter adapter in this.pxfDispatcher.GetAdaptersForResourceType(context, resourceType, PxfAdapterCapability.View))
            {
                var pagingSource = new PagingResourceSource<T>(
                    () => this.retryManager.ExecuteAsync(
                        nameof(PdApiDataResourceAgent),
                        nameof(this.WritePxfDataAsync),
                        () => firstPageFunc(adapter)),
                    nextLink => this.retryManager.ExecuteAsync(
                        nameof(PdApiDataResourceAgent),
                        nameof(this.WritePxfDataAsync),
                        () => nextPageFunc(adapter, nextLink.ToString())));
                sources.Add(adapter.PartnerId, pagingSource);
            }
            var mergingResourceSource = new MergingResourceSource<T>(
                (a, b) =>
                {
                    if (a.DateTime.Ticks > b.DateTime.Ticks)
                        return -1;
                    if (a.DateTime.Ticks < b.DateTime.Ticks)
                        return 1;
                    return 0;
                },
                sources);

            bool wroteAnything = false;
            while (true)
            {
                IList<T> results = await mergingResourceSource.FetchAsync(blockSize).ConfigureAwait(false);
                if (results == null || results.Count <= 0)
                    break;

                if (saveIds != null)
                {
                    foreach (T res in results)
                    {
                        saveIds.Add(Tuple.Create(res.Id, res.DateTime));
                    }
                }

                IEnumerable<Resource> resources = results.Select(TransformResult);
                foreach (Resource resource in resources)
                {
                    string serialized = this.serializer.ConvertResource(resource);
                    await file.AddBlockAsync(serialized).ConfigureAwait(false);
                }

                wroteAnything = true;
            }

            return wroteAnything;
        }

        /// <summary>
        ///     This method transforms from the <see cref="Resource" /> objects retrieved from the adapters into the objects the user will see. Any data
        ///     returned from here WILL be visible to the user, including the names of fields. Microsoft confidential information should not be exposed.
        /// </summary>
        private static Resource TransformResult(object resource)
        {
            switch (resource)
            {
                case AppUsageResource appUsageResource:
                    return appUsageResource;
                case BrowseResource browseResource:
                    return browseResource;
                case LocationResource locationResource:
                    return locationResource;
                case SearchResource searchResource:
                    return searchResource;
                case VoiceResource voiceResource:
                    return voiceResource;
                case ContentConsumptionResource consumptionResource:
                    return consumptionResource;
            }

            throw new ArgumentOutOfRangeException(
                nameof(resource),
                $"{resource?.GetType().FullName} is an unknown type, see the {nameof(TransformResult)} method and make sure it is updated.");
        }
    }
}
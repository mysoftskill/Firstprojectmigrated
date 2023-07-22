// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Export
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Converters;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.PCF;
    using Microsoft.Membership.MemberServices.Privacy.DataContracts.ExportTypes;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Blob;
    using System.Diagnostics.Eventing.Reader;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter;

    /// <summary>
    ///     Export Service
    /// </summary>
    public class ExportService : IExportService
    {
        private const string CancellationErrorString = "cancelled by user";

        // TODO: This is awful to be here. Should probably get the actual expire time as part of the call to PCF, for now, this is just copied
        // TODO: from Config.Instance.Worker.Tasks.ExportStorageCleanupTask.MaxAgeDays in PCF configuration.
        private static readonly TimeSpan pcfLinksExpireIn = TimeSpan.FromDays(60);

        private readonly IPrivacyExportConfiguration exportConfig;

        private readonly IExportStorageProvider exportStorage;

        private readonly ILogger logger;

        private readonly IPcfProxyService pcfProxyService;

        private readonly ICounterFactory counterFactory;

        private readonly ICloudBlobFactory cloudBlobFactory;

        private const string ExportCounterCategoryName = "PXSExport";

        private const string ExportArchivesDeleteCounterCategoryName = "PXSExportArchivesDelete";

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExportService" /> class.
        /// </summary>
        public ExportService(IPrivacyConfigurationManager configManager, ICounterFactory counterFactory, ILogger logger, IExportStorageProvider exportStorage, IPcfProxyService pcfProxyService, ICloudBlobFactory cloudBlobFactory)
        {
            this.exportConfig = configManager?.PrivacyExperienceServiceConfiguration?.PrivacyExportConfiguration ??
                                throw new ArgumentNullException(nameof(configManager.PrivacyExperienceServiceConfiguration.PrivacyExportConfiguration));
            this.counterFactory = counterFactory ?? throw new ArgumentNullException(nameof(counterFactory));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.exportStorage = exportStorage ?? throw new ArgumentNullException(nameof(exportStorage));
            this.pcfProxyService = pcfProxyService ?? throw new ArgumentNullException(nameof(pcfProxyService));
            this.cloudBlobFactory = cloudBlobFactory ?? throw new ArgumentNullException(nameof(cloudBlobFactory));
        }

        /// <summary>
        ///     list history of export requests
        /// </summary>
        /// <param name="requestContext"></param>
        /// <returns></returns>
        public async Task<ServiceResponse<ListExportHistoryResponse>> ListExportHistoryAsync(IRequestContext requestContext)
        {
            var response = new ServiceResponse<ListExportHistoryResponse>
            {
                Result = new ListExportHistoryResponse
                {
                    Exports = new List<ExportStatus>()
                }
            };

            Task<ServiceResponse<IList<PrivacyRequestStatus>>> pcfExportsTask;

            // Kick off the call to PCF and to query quick exports.
            if (requestContext.IsWatchdogRequest)
            {
                // The WD doesn't have anything stored in PCF. Bypass that call because on PCF side it's expensive RU cost.
                pcfExportsTask = Task.FromResult(new ServiceResponse<IList<PrivacyRequestStatus>> { Result = new List<PrivacyRequestStatus>() });
            }
            else
            {
                pcfExportsTask = this.pcfProxyService.ListRequestsByCallerMsaAsync(requestContext, RequestType.Export);
            }

            Task<ExportStatusHistoryRecordCollection> quickExportsTask = new Func<Task<ExportStatusHistoryRecordCollection>>(
                async () =>
                {
                    IExportHistoryRecordHelper exportHistoryHelper =
                        await this.exportStorage.CreateStatusHistoryRecordHelperAsync(requestContext.TargetPuid.ToString()).ConfigureAwait(false);
                    return await exportHistoryHelper.GetHistoryRecordsAsync(true).ConfigureAwait(false);
                })();

            // Wait for both to complete
            await Task.WhenAll(pcfExportsTask, quickExportsTask).ConfigureAwait(false);

            // If pcf call errors out, bail with the error
            ServiceResponse<IList<PrivacyRequestStatus>> pcfResponse = await pcfExportsTask.ConfigureAwait(false);
            if (!pcfResponse.IsSuccess)
                return new ServiceResponse<ListExportHistoryResponse> { Error = pcfResponse.Error };

            // Transform and kick off the query to get each export size
            var pcfExports = (pcfResponse.Result ?? Enumerable.Empty<PrivacyRequestStatus>()).Select(
                e =>
                {
                    UriBuilder uriBuilder = null;
                    CloudBlob blob = null;
                    if (e.DestinationUri != null)
                    {
                        uriBuilder = new UriBuilder(e.DestinationUri);
                        uriBuilder.Path += $"/Export-{e.Id:n}.zip";
                        blob = cloudBlobFactory.GetCloudBlob(uriBuilder.Uri);
                    }

                    try
                    {
                        if (e.State == PrivacyRequestState.Completed && blob != null)
                        {
                            return new
                            {
                                Blob = blob,
                                BlobAttributesTask = blob.FetchAttributesAsync(),
                                ExportStatus = new ExportStatus
                                {
                                    DataTypes = e.DataTypes,
                                    ExpiresAt = e.CompletedTime + pcfLinksExpireIn,
                                    ExportId = e.Id.ToString(),
                                    IsComplete = true,
                                    LastError = null,
                                    RequestedAt = e.SubmittedTime,
                                    ZipFileUri = uriBuilder?.Uri
                                }
                            };
                        }
                        else
                        {
                            // If the export request is not yet in the completed state 
                            // then do not include a file zip URI (this would be a dead link)
                            return new
                            {
                                Blob = blob,
                                BlobAttributesTask = Task.CompletedTask,
                                ExportStatus = new ExportStatus
                                {
                                    DataTypes = e.DataTypes,
                                    ExpiresAt = e.CompletedTime + pcfLinksExpireIn,
                                    ExportId = e.Id.ToString(),
                                    IsComplete = false,
                                    LastError = null,
                                    RequestedAt = e.SubmittedTime,
                                    ZipFileUri = null
                                }
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        // Do not include any export requests with exceptions in the final list of results 
                        this.counterFactory.GetCounter(ExportCounterCategoryName, "PCFExportListCollectionFailed", CounterType.Number).Increment();
                        this.logger.Error(nameof(ExportService), ex, $"Failed to get PCF export blob attributes: {blob?.Name}");
                        return null;
                    }
                }).ToList();

            // Pull the blob length from the properties and add status to results
            foreach (var pcfExport in pcfExports)
            {
                // Skip any pcfexports that were collected with errors
                if (pcfExport == null)
                {
                    continue;
                }

                try
                {
                    // Check to see if the blob is not null
                    if (pcfExport.Blob != null)
                    {
                        await pcfExport.BlobAttributesTask.ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    this.counterFactory.GetCounter(ExportCounterCategoryName, "PCFExportListBlobAttributesFailed", CounterType.Number).Increment();
                    this.logger.Warning(nameof(ExportService), ex, $"Failed to asynchronously get PCF export blob attributes: {pcfExport.Blob?.Name}");
                }

                pcfExport.ExportStatus.ZipFileSize = pcfExport.Blob?.Properties?.Length ?? -1;
                pcfExport.ExportStatus.ExportType = ExperienceContracts.ExportType.Full;
                response.Result.Exports.Add(pcfExport.ExportStatus);
            }

            // Add quick exports to the results
            ExportStatusHistoryRecordCollection exportStatusHistoryRecordCollection = await quickExportsTask.ConfigureAwait(false);
            foreach (ExportStatusHistoryRecord rec in exportStatusHistoryRecordCollection?.HistoryRecords ?? Enumerable.Empty<ExportStatusHistoryRecord>())
            {
                rec.ExportType = DataContracts.ExportTypes.ExportType.Quick;
                response.Result.Exports.Add(ExportStatusConverter.ToExportStatus(rec));
            }

            // All done.
            return response;
        }

        /// <summary>
        ///     Post export cancel
        /// </summary>
        public async Task<ServiceResponse> PostExportCancelAsync(IRequestContext requestContext, string exportId)
        {
            var response = new ServiceResponse();
            IExportStatusRecordHelper exportStatusHelper = await this.exportStorage.CreateExportStatusHelperAsync(exportId).ConfigureAwait(false);
            ExportStatusRecord statusRecord = await exportStatusHelper.GetStatusRecordAsync(true).ConfigureAwait(false);
            if (statusRecord == null)
            {
                response.Error = new Error(ErrorCode.InvalidInput, "status record not found");
            }
            else
            {
                statusRecord.IsComplete = true;
                statusRecord.Ticket = string.Empty;
                statusRecord.LastError = CancellationErrorString;
                await exportStatusHelper.UpsertStatusRecordAsync(statusRecord).ConfigureAwait(false);
            }

            IExportHistoryRecordHelper exportHistoryHelper =
                await this.exportStorage.CreateStatusHistoryRecordHelperAsync(requestContext.TargetPuid.ToString()).ConfigureAwait(false);
            await exportHistoryHelper.CompleteHistoryRecordAsync(exportId, DateTimeOffset.UtcNow, CancellationErrorString, null, 0, DateTimeOffset.MinValue).ConfigureAwait(false);
            return response;
        }

        /// <summary>
        ///     Delete export archive
        /// </summary>
        public async Task<ServiceResponse> DeleteExportArchivesAsync(RequestContext requestContext, string exportId, string exportType)
        {
            var response = new ServiceResponse();
            DataContracts.ExportTypes.ExportType type;
            Enum.TryParse(exportType, out type);

            // Export record found in PXS i.e. requested export is a quick export
            if ( type == DataContracts.ExportTypes.ExportType.Quick)
            {
                IExportStatusRecordHelper exportStatusHelper = await this.exportStorage.CreateExportStatusHelperAsync(exportId).ConfigureAwait(false);
                ExportStatusRecord statusRecord = await exportStatusHelper.GetStatusRecordAsync(true).ConfigureAwait(false);
                //  adding some delay to make sure that worker would not pick message from queue before record status updation
                TimeSpan? delay = TimeSpan.FromSeconds(10);
                if (statusRecord.UserId != requestContext.TargetPuid.ToString())
                {
                    response.Error = new Error(ErrorCode.Unauthorized, "Requestor's PUID and the PUID in export records are not matching");
                    return response;
                }

                var baseQueueMsg = new BaseQueueMessage
                {
                    Action = "DeleteExportArchivesTask",
                    RequestId = exportId
                };

                await this.exportStorage.ExportArchiveDeletionQueue.AddMessageAsync(baseQueueMsg, delay).ConfigureAwait(false);

                try
                {
                    IExportHistoryRecordHelper exportHistoryHelper =
                            await this.exportStorage.CreateStatusHistoryRecordHelperAsync(requestContext.TargetPuid.ToString()).ConfigureAwait(false);
                    ExportStatusHistoryRecordCollection exportStatusHistoryRecordCollection = await exportHistoryHelper.GetHistoryRecordsAsync(true).ConfigureAwait(false);
                    ExportStatusHistoryRecord historyRecord = exportStatusHistoryRecordCollection.HistoryRecords.Find(h =>
                    {
                        return h.ExportId.Equals(exportId);
                    });

                    historyRecord.ExportArchiveDeleteStatus = DataContracts.ExportTypes.ExportArchivesDeleteStatus.DeleteInProgress;
                    historyRecord.ExportArchiveDeleteRequestedTime = DateTimeOffset.UtcNow;
                    historyRecord.ExportArchiveDeleteRequesterId = requestContext.TargetPuid.ToString();

                    await exportHistoryHelper.UpsertHistoryCollectionAsync(exportStatusHistoryRecordCollection).ConfigureAwait(false);
                }
                catch (StorageException storageEx)
                {
                    this.logger.Error(nameof(ExportService), "failed to write the history record " + storageEx);
                    this.counterFactory.GetCounter(ExportArchivesDeleteCounterCategoryName, "PCFExportArchivesDeleteRequestFailed", CounterType.Number).Increment();
                    
                    response.Error = new Error(ErrorCode.CreateConflict, ErrorMessages.RequestAlreadyInProgress);
                    return response;
                }

            }
            // Pass the request to PCF
            else 
            {
                var param = new DeleteExportArchiveParameters(exportId, requestContext.TargetPuid);
                Task<ServiceResponse> pcfResponse = this.pcfProxyService.DeleteExportsAsync(param);

                ServiceResponse pcfServiceResponse = await pcfResponse.ConfigureAwait(false);
                if (!pcfServiceResponse.IsSuccess)
                    return new ServiceResponse { Error = pcfServiceResponse.Error };
            }

            return response;
        }

        /// <summary>
        ///     Post export request
        /// </summary>
        public async Task<ServiceResponse<PostExportResponse>> PostExportRequestAsync(
            IRequestContext requestContext,
            IList<string> dataTypes,
            DateTimeOffset startTime,
            DateTimeOffset endTime)
        {
            var response = new ServiceResponse<PostExportResponse>();
            Error error = this.ValidateDataTypes(dataTypes);
            if (error != null)
            {
                response.Error = error;
                return response;
            }

            var baseQueueMsg = new BaseQueueMessage
            {
                Action = "ExportTask",
                RequestId = ExportStorageProvider.GetNewRequestId()
            };

            ExportStatusRecord statusRecord = ExportStatusConverter.FromExportRequest(
                requestContext.TargetPuid.ToString(),
                baseQueueMsg.RequestId,
                requestContext.RequireIdentity<MsaSelfIdentity>().UserProxyTicket,
                dataTypes,
                startTime,
                endTime,
                requestContext.Flights);
            ExportStatusHistoryRecord historyRecord = ExportStatusConverter.CreateHistoryRecordFromStatus(statusRecord, DateTimeOffset.UtcNow);
            IExportStatusRecordHelper exportStatusHelper = await this.exportStorage.CreateExportStatusHelperAsync(statusRecord.ExportId).ConfigureAwait(false);
            IExportHistoryRecordHelper exportHistoryHelper =
                await this.exportStorage.CreateStatusHistoryRecordHelperAsync(requestContext.TargetPuid.ToString()).ConfigureAwait(false);
            if (this.exportConfig.ExportRequestThrottleEnabled)
            {
                DateTimeOffset beginThrottleWindow = DateTimeOffset.UtcNow.AddHours(this.exportConfig.ExportRequestThrottleWindowInHours * -1);
                ExportThrottleState throttleState = await exportHistoryHelper.CheckRequestThrottlingAsync(
                    historyRecord,
                    beginThrottleWindow,
                    this.exportConfig.ExportRequestThrottleMaxCompleted,
                    this.exportConfig.ExportRequestThrottleMaxCancelled).ConfigureAwait(false);
                switch (throttleState)
                {
                    case ExportThrottleState.RequestInProgress:
                        response.Error = new Error(ErrorCode.CreateConflict, ErrorMessages.RequestAlreadyInProgress);
                        return response;
                    case ExportThrottleState.TooManyRequests:
                        response.Error = new Error(ErrorCode.TooManyRequests, ErrorMessages.TooManyRequests);
                        return response;
                }
            }

            await exportStatusHelper.UpsertStatusRecordAsync(statusRecord).ConfigureAwait(false);
            try
            {
                await exportHistoryHelper.CreateHistoryRecordAsync(historyRecord).ConfigureAwait(false);
            }
            catch (StorageException storageEx)
            {
                // TODO: This exception is a bad way to handle this case. Should be something more specific.
                this.logger.Error(nameof(ExportService), "failed to write the history record " + storageEx);
                this.counterFactory.GetCounter(ExportCounterCategoryName, "PCFExportRequestFailed", CounterType.Number).Increment();
                response.Error = new Error(ErrorCode.CreateConflict, ErrorMessages.RequestAlreadyInProgress);
                return response;
            }

            await this.exportStorage.ExportCreationQueue.AddMessageAsync(baseQueueMsg).ConfigureAwait(false);

            response.Result = new PostExportResponse
            {
                ExportId = statusRecord.ExportId
            };
            return response;
        }

        private Error ValidateDataTypes(IList<string> dataTypes)
        {
            ILookup<string, KeyValuePair<DataTypeId, DataType>> lookup = Policies.Current.DataTypes.Map.ToLookup(k => k.Key.Value);
            foreach (string dt in dataTypes)
            {
                if (!lookup.Contains(dt))
                    return new Error(ErrorCode.InvalidInput, $"Export Request contains an invalid data type {dt}");
            }

            return null;
        }
    }
}

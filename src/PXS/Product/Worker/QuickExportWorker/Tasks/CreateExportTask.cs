// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.QuickExportWorker.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Cloud.InstrumentationFramework;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.DataContracts.ExportTypes;
    using Microsoft.Membership.MemberServices.Privacy.QuickExportWorker.CsvSerializer;
    using Microsoft.Membership.MemberServices.Privacy.QuickExportWorker.DataProcessingAgents;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher;
    using Microsoft.Practices.Unity;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.Azure.Storage.Blob;

    public class CreateExportTask
    {
        private readonly TimeSpan exportTaskExpiration;

        private readonly IPrivacyConfigurationManager privacyConfigurationManager;

        private readonly ICounterFactory counterFactory;

        private readonly IDependencyManager dependencyManager;

        private readonly IExportStorageProvider exportStorageProvider;

        private readonly ILogger logger;

        private readonly BaseQueueMessage message;

        private IExportStagingStorageHelper stagingHelper;

        public CreateExportTask(
            IExportStorageProvider exportStorageProvider,
            BaseQueueMessage message,
            IDependencyManager dependencyManager,
            ILogger logger)
        {
            this.exportTaskExpiration = TimeSpan.FromHours(5);

            this.dependencyManager = dependencyManager ?? throw new ArgumentNullException(nameof(dependencyManager));
            this.privacyConfigurationManager = (IPrivacyConfigurationManager)this.dependencyManager.GetService(typeof(IPrivacyConfigurationManager));
            this.counterFactory = (ICounterFactory)this.dependencyManager.GetService(typeof(ICounterFactory));
            
            this.logger = logger;
            this.message = message;
            this.exportStorageProvider = exportStorageProvider;
        }

        public async Task ProcessAsync()
        {
            const string exportStatusLogKey = "ExportStatus";

            IExportStatusRecordHelper exportStatusHelper = null;
            ExportStatusRecord statusRecord = null;

            var apiEvent = new IncomingApiEventWrapper();
            apiEvent.Start("ExportTask");
            apiEvent.RequestMethod = "POST";

            try
            {
                this.counterFactory.GetCounter(ExportDequeuer.ExportCounterCategoryName, "ProcessExportStart", CounterType.Number).Increment();

                exportStatusHelper = await this.exportStorageProvider.CreateExportStatusHelperAsync(this.message.RequestId).ConfigureAwait(false);
                statusRecord = await exportStatusHelper.GetStatusRecordAsync(false).ConfigureAwait(false);

                foreach (string dataType in statusRecord.DataTypes ?? Enumerable.Empty<string>())
                    this.counterFactory.GetCounter(ExportDequeuer.ExportCounterCategoryName, "ProcessExportStart", CounterType.Number).Increment(dataType);

                if (long.TryParse(statusRecord.UserId, out long puid))
                    apiEvent.SetUserId(puid);
                apiEvent.ExtraData["ExportId"] = statusRecord.ExportId ?? string.Empty;
                apiEvent.ExtraData["ExportTypes"] = string.Join(",", statusRecord.DataTypes ?? Enumerable.Empty<string>());

                if (statusRecord.Resources == null)
                {
                    statusRecord.Resources = new List<ExportDataResourceStatus>();
                }
                statusRecord.LastSessionStart = DateTime.UtcNow;

                if (statusRecord.IsComplete)
                {
                    await this.CompleteAsync().ConfigureAwait(false);
                    apiEvent.ExtraData[exportStatusLogKey] = "AlreadyCompleted";
                    apiEvent.Success = true;
                    return;
                }

                this.InitializeDataResources(statusRecord);

                this.stagingHelper = await this.exportStorageProvider.CreateStagingStorageHelperAsync(statusRecord.UserId, statusRecord.ExportId).ConfigureAwait(false);

                if (this.message.InsertionTime.HasValue &&
                    this.message.InsertionTime.Value.Add(this.exportTaskExpiration) < DateTimeOffset.UtcNow)
                {
                    this.counterFactory.GetCounter(ExportDequeuer.ExportCounterCategoryName, "ProcessExportExpired", CounterType.Number).Increment();
                    this.logger.Warning(nameof(CreateExportTask), $"Export request expired {statusRecord.ExportId}");
                    await this.CleanupStagingDataAsync().ConfigureAwait(false);
                    await this.CompleteStatusRecordAsync(exportStatusHelper, statusRecord, "Export request expired").ConfigureAwait(false);
                    apiEvent.ExtraData[exportStatusLogKey] = "Expired";
                    apiEvent.ErrorMessage = "Export request expired";
                    apiEvent.Success = false;
                    return;
                }

                await this.HandleAgentsAsync(statusRecord.Resources, statusRecord).ConfigureAwait(false);

                if (statusRecord.LastError != null)
                {
                    // TODO: retries?
                    await this.CleanupStagingDataAsync().ConfigureAwait(false);
                    await this.CompleteStatusRecordAsync(exportStatusHelper, statusRecord, statusRecord.LastError).ConfigureAwait(false);
                    apiEvent.ErrorMessage = statusRecord.LastError;
                    apiEvent.ExtraData[exportStatusLogKey] = "Error";
                    apiEvent.Success = false;
                    this.logger.Error(nameof(CreateExportTask), $"The following export request had an error processing: {statusRecord.ExportId}");
                    this.counterFactory.GetCounter(ExportDequeuer.ExportCounterCategoryName, "ProcessExportError", CounterType.Number).Increment();
                }
                else if (this.AllResourcesDone(statusRecord))
                {
                    await this.ZipStagingFilesAsync(statusRecord).ConfigureAwait(false);
                    await this.CleanupStagingDataAsync().ConfigureAwait(false);
                    await this.CompleteStatusRecordAsync(exportStatusHelper, statusRecord, null).ConfigureAwait(false);
                    apiEvent.ExtraData[exportStatusLogKey] = "Success";
                    if (statusRecord.ZipFileSize >= int.MinValue && statusRecord.ZipFileSize <= int.MaxValue)
                        apiEvent.ExtraData["ExportSize"] = statusRecord.ZipFileSize.ToString();
                    apiEvent.Success = true;
                    this.counterFactory.GetCounter(ExportDequeuer.ExportCounterCategoryName, "ProcessExportSuccess", CounterType.Number).Increment();
                }
                else
                {
                    // This state shouldn't happen, since removing cosmos path, there is no intermediate state here anymore.
                    await this.UpdateIntermediateStatusAsync(exportStatusHelper, statusRecord).ConfigureAwait(false);
                    apiEvent.ExtraData[exportStatusLogKey] = "Intermediate";
                    apiEvent.Success = false;
                    this.counterFactory.GetCounter(ExportDequeuer.ExportCounterCategoryName, "ProcessExportIntermediate", CounterType.Number).Increment();
                }
            }
            catch (Exception ex)
            {
                this.counterFactory.GetCounter(ExportDequeuer.ExportCounterCategoryName, "ProcessExportException", CounterType.Number).Increment();
                this.logger.Error(
                    nameof(CreateExportTask),
                    $"ExportTask ProcessAsync exception for {this.message.RequestId}, {ex}");
                apiEvent.ErrorMessage = ex.ToString();
                apiEvent.ExtraData[exportStatusLogKey] = "Exception";
                apiEvent.Success = false;
                if (exportStatusHelper != null && statusRecord != null)
                {
                    // TODO: retries?
                    await this.CleanupStagingDataAsync().ConfigureAwait(false);
                    await this.CompleteStatusRecordAsync(exportStatusHelper, statusRecord, statusRecord.LastError).ConfigureAwait(false);
                }
                throw;
            }
            finally
            {
                apiEvent.Finish();
                this.counterFactory.GetCounter(ExportDequeuer.ExportCounterCategoryName, "ProcessExportEnd", CounterType.Number).Increment();
            }
        }

        public async Task CompleteAsync()
        {
            this.logger.Log(IfxTracingLevel.Informational, nameof(CreateExportTask), "CompleteAsync " + this.message);
            await this.exportStorageProvider.ExportCreationQueue.CompleteMessageAsync(this.message).ConfigureAwait(false);
        }

        public override string ToString()
        {
            return this.message.ToString();
        }

        private bool AllResourcesDone(ExportStatusRecord statusRecord)
        {
            return statusRecord.Resources.All(resourceStatus => resourceStatus.IsComplete);
        }

        private async Task CleanupStagingDataAsync()
        {
            await this.stagingHelper.DeleteStagingContainerAsync().ConfigureAwait(false);
        }

        private async Task CompleteStatusRecordAsync(IExportStatusRecordHelper exportStatusHelper, ExportStatusRecord statusRecord, string error)
        {
            IExportHistoryRecordHelper historyHelper = await this.exportStorageProvider.CreateStatusHistoryRecordHelperAsync(statusRecord.UserId).ConfigureAwait(false);
            await historyHelper.CompleteHistoryRecordAsync(
                statusRecord.ExportId,
                DateTimeOffset.UtcNow,
                error,
                statusRecord.ZipFileUri,
                statusRecord.ZipFileSize,
                statusRecord.ZipFileExpires).ConfigureAwait(false);
            statusRecord.IsComplete = true;
            statusRecord.LastError = error;
            statusRecord.LastSessionEnd = DateTime.UtcNow;
            statusRecord.Ticket = null;
            await exportStatusHelper.UpsertStatusRecordAsync(statusRecord).ConfigureAwait(false);
            await this.CompleteAsync().ConfigureAwait(false);
        }

        private async Task HandleAgentsAsync(
            IEnumerable<ExportDataResourceStatus> resources,
            ExportStatusRecord statusRecord)
        {
            try
            {
                foreach (ExportDataResourceStatus resourceStatus in resources)
                {
                    if (resourceStatus.IsComplete)
                    {
                        continue;
                    }

                    var agent = new PdApiDataResourceAgent(
                        this.dependencyManager.Container.Resolve<IPxfDispatcher>(),
                        this.dependencyManager.Container.Resolve<ICounterFactory>(),
                        this.dependencyManager.Container.Resolve<ISerializer>(),
                        (IPrivacyConfigurationManager)this.dependencyManager.GetService(typeof(IPrivacyConfigurationManager)),
                        this.logger);

                    await agent.ProcessExportAsync(statusRecord, resourceStatus, this.stagingHelper).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                statusRecord.LastError = ex.ToString();
            }
        }

        private void InitializeDataResources(ExportStatusRecord statusRecord)
        {
            foreach (string dataType in statusRecord.DataTypes)
            {
                var resourceStatus = new ExportDataResourceStatus
                {
                    ResourceDataType = dataType
                };
                statusRecord.Resources.Add(resourceStatus);
            }
        }

        private async Task UpdateIntermediateStatusAsync(IExportStatusRecordHelper exportStatusHelper, ExportStatusRecord statusRecord)
        {
            statusRecord.LastSessionEnd = DateTime.UtcNow;
            await exportStatusHelper.UpsertStatusRecordAsync(statusRecord).ConfigureAwait(false);
        }

        private async Task ZipStagingFilesAsync(ExportStatusRecord statusRecord)
        {
            CloudBlob blob = await this.stagingHelper.ZipStagingAsync(this.counterFactory, ExportDequeuer.ExportCounterCategoryName).ConfigureAwait(false);

            DateTimeOffset expirationTime = DateTimeOffset.UtcNow + TimeSpan.FromDays(this.privacyConfigurationManager.ExportConfiguration.MaxStorageAgeForCleanupInDays);

            CloudBlobClient client = new CloudBlobClient(blob.Uri);

            // User token can only last 7 days
            var delegationKey = await this.stagingHelper.GetUserDelegationKey(DateTimeOffset.UtcNow, expirationTime);

            string signature = blob.GetUserDelegationSharedAccessSignature(delegationKey,
                policy: new SharedAccessBlobPolicy
                {
                    Permissions = SharedAccessBlobPermissions.Read,
                    SharedAccessExpiryTime = expirationTime
                });

            var uriBuilder = new UriBuilder(new Uri(blob.Uri, signature)) { Scheme = "https" };
            statusRecord.ZipFileUri = uriBuilder.Uri;
            statusRecord.ZipFileSize = blob.Properties.Length;
            statusRecord.ZipFileExpires = expirationTime;
        }
    }
}

// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.QuickExportWorker.Tasks
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Cloud.InstrumentationFramework;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.DataContracts.ExportTypes;
    using Microsoft.PrivacyServices.Common.Azure;

    public class DeleteExportArchivesTask
    {
        private TimeSpan exportTaskExpiration;
        private IDependencyManager dependencyManager;
        private IPrivacyConfigurationManager privacyConfigurationManager;
        private ICounterFactory counterFactory;
        private ILogger logger;
        private BaseQueueMessage message;
        private IExportStorageProvider exportStorageProvider;

        public DeleteExportArchivesTask(
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
            var apiEvent = new IncomingApiEventWrapper();
            apiEvent.Start("DeleteExportArchivesTask");
            apiEvent.RequestMethod = "DELETE";
            var exportId = this.message.RequestId;
            apiEvent.ExtraData["ExportId"] = exportId;

            try
            {
                this.counterFactory.GetCounter(ExportDequeuer.ExportCounterCategoryName, "DeleteExportArchivesStart", CounterType.Number).Increment();


                IExportStatusRecordHelper exportStatusHelper = await this.exportStorageProvider.CreateExportStatusHelperAsync(exportId).ConfigureAwait(false);
                ExportStatusRecord statusRecord = await exportStatusHelper.GetStatusRecordAsync(true).ConfigureAwait(false);
                if (long.TryParse(statusRecord.UserId, out long puid))
                    apiEvent.SetUserId(puid);
                IExportZipStorageHelper zipHelper = await this.exportStorageProvider.CreateZipStorageHelperAsync(puid).ConfigureAwait(false);
                IExportHistoryRecordHelper statusHistoryHelper = await this.exportStorageProvider.CreateStatusHistoryRecordHelperAsync(puid.ToString()).ConfigureAwait(false);
                IExportStagingStorageHelper stagingStorageHelper = await this.exportStorageProvider.CreateStagingStorageHelperAsync(puid.ToString(), exportId).ConfigureAwait(false);

                ExportStatusHistoryRecordCollection exportStatusHistoryRecordCollection = await statusHistoryHelper.GetHistoryRecordsAsync(true).ConfigureAwait(false);
                ExportStatusHistoryRecord exportStatusHistoryRecord = exportStatusHistoryRecordCollection.HistoryRecords.Find(h =>
                {
                    return h.ExportId.Equals(exportId);
                });

                if (exportStatusHistoryRecord != null && exportStatusHistoryRecord.ExportArchiveDeleteStatus == ExportArchivesDeleteStatus.DeleteInProgress)
                {
                    bool zipDeleted, statusHistoryDeleted, stagingDeleted, statusDeleted;
                    
                    zipDeleted = await zipHelper.DeleteZipStorageAsync(exportId).ConfigureAwait(false);

                    statusHistoryDeleted = await statusHistoryHelper.DeleteExportByIdAsync(exportId);

                    stagingDeleted = await stagingStorageHelper.DeleteStagingContainerAsync(puid, exportId).ConfigureAwait(false);

                    statusDeleted = await exportStatusHelper.InitializeAndDeleteAsync(exportId).ConfigureAwait(false);


                    if (zipDeleted && statusDeleted && statusHistoryDeleted && stagingDeleted)
                    {
                        this.logger.Information(nameof(DeleteExportArchivesTask), $"The following export archives has been deleted from all the tables: {exportId}");
                        this.logger.Log(IfxTracingLevel.Informational, nameof(DeleteExportArchivesTask), "CompleteAsync " + this.message);
                        await this.exportStorageProvider.ExportArchiveDeletionQueue.CompleteMessageAsync(this.message).ConfigureAwait(false);
                        apiEvent.Success = true;
                    }
                    else
                    {
                        if(!zipDeleted)
                            this.logger.Error(nameof(DeleteExportArchivesTask), $"The following export archive had an error while deleting zip storage: {exportId}");
                        else if(!statusHistoryDeleted)
                            this.logger.Error(nameof(DeleteExportArchivesTask), $"The following export archive had an error while deleting the history record: {exportId}");
                        else if(!stagingDeleted)
                            this.logger.Error(nameof(DeleteExportArchivesTask), $"The following export archive had an error while deleting staging storage: {exportId}");
                        else
                            this.logger.Error(nameof(DeleteExportArchivesTask), $"The following export archive had an error while deleting: {exportId}");
                        apiEvent.Success = false;
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.Error(nameof(DeleteExportArchivesTask), $"The following export archive had an error while deleting: {exportId} "+ex.Message);
                apiEvent.Success = false;
            }
            finally
            {
                apiEvent.Finish();
                this.counterFactory.GetCounter(DeleteExportArchivesDequeuer.ExportArchivesDeleteCounterCategoryName, "DeleteExportArchivesEnd", CounterType.Number).Increment();
            }
        }
    }
}

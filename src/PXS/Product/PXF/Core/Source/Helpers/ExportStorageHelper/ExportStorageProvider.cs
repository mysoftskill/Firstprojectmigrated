// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.DataContracts.ExportTypes;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <inheritdoc />
    public class ExportStorageProvider : IExportStorageProvider
    {
        private const string ExportIdCentury = "20";

        private const string ExportIdDateTimeFormat = "yyMMddHHmmssffffff";

        private const string ExportQueueName = "v2exportqueue";

        private const string ExportArchivesDeleteQueueName = "v2exportarchivesdeletequeue";

        private readonly ILogger logger;

        private int maxHistoryRecords;

        /// <summary>
        ///     Converts a exportId to a DateTime
        ///     The export request ids are made up of a datetime related string plus a guid
        /// </summary>
        /// <param name="exportId"></param>
        /// <returns></returns>
        public static DateTime ConvertRequestIdToDateTime(string exportId)
        {
            var dateString = new StringBuilder();
            dateString.Append(ExportIdCentury);
            dateString.Append(exportId.Substring(0, 2));
            dateString.Append("/");
            dateString.Append(exportId.Substring(2, 2));
            dateString.Append("/");
            dateString.Append(exportId.Substring(4, 2));
            dateString.Append(" ");
            dateString.Append(exportId.Substring(6, 2));
            dateString.Append(":");
            dateString.Append(exportId.Substring(8, 2));
            dateString.Append(":");
            dateString.Append(exportId.Substring(10, 2));
            dateString.Append(".");
            dateString.Append(exportId.Substring(12, 6));
            if (!DateTime.TryParse(dateString.ToString(), out DateTime requestDateTime))
            {
                throw new InvalidDataException("exportId is invalid:" + exportId + " date " + dateString);
            }
            requestDateTime = DateTime.SpecifyKind(requestDateTime, DateTimeKind.Utc);
            return requestDateTime;
        }

        /// <summary>
        ///     Generate an exportId
        /// </summary>
        public static string GenerateExportId(DateTime dt)
        {
            return dt.ToString(ExportIdDateTimeFormat) + Guid.NewGuid().ToString("N").Substring(0, 10).ToLowerInvariant();
        }

        /// <summary>
        ///     Get the hash of a puid
        /// </summary>
        /// <param name="puid"></param>
        /// <returns></returns>
        public static string GetIdHash(long puid)
        {
            return LiveIdUtils.ToAnonymousHex(puid).ToLowerInvariant();
        }

        /// <summary>
        ///     Get a new request id
        /// </summary>
        /// <returns></returns>
        public static string GetNewRequestId()
        {
            return GenerateExportId(DateTime.UtcNow);
        }

        /// <summary>
        ///     Gets or sets the CreateExportQueue interface
        /// </summary>
        public IExportQueue ExportCreationQueue { get; private set; }

        /// <summary>
        ///     Gets or sets the ExportArchivesDeleteQueue interface
        /// </summary>
        public IExportQueue ExportArchiveDeletionQueue { get; private set; }

        public ExportStorageProvider(ILogger log, AzureStorageProvider storage)
        {
            this.logger = log;
            this.StorageProvider = storage;
        }

        /// <summary>
        ///     Clean up a batch of old status records and the storage associated with them
        /// </summary>
        /// <param name="olderThan"></param>
        /// <param name="top"></param>
        /// <returns></returns>
        public async Task<IList<ExportStatusRecord>> CleanupBatchAsync(DateTime olderThan, int top)
        {
            var deleted = new List<ExportStatusRecord>();
            IExportStatusRecordHelper statusHelper = await this.CreateExportStatusHelperAsync(null).ConfigureAwait(false);
            IList<ExportStatusRecord> records = await statusHelper.ListStatusRecordsAscendingAsync(top).ConfigureAwait(false);
            foreach (ExportStatusRecord rec in records)
            {
                DateTime requestTime = ConvertRequestIdToDateTime(rec.ExportId);
                if (DateTime.Compare(requestTime, olderThan) >= 0)
                {
                    // Break because the results are in ascending order
                    break;
                }

                if (!ExportStatusRecord.ParseUserId(rec.UserId, out long puid))
                {
                    this.logger.Error(nameof(ExportStorageProvider), "storage cleanup found status record with bad UserId " + rec.UserId + " req " + rec.ExportId);
                    continue;
                }

                IExportZipStorageHelper zipHelper = await this.CreateZipStorageHelperAsync(puid).ConfigureAwait(false);
                bool zipDeleted = await zipHelper.DeleteZipStorageAsync(rec.ExportId).ConfigureAwait(false);

                IExportHistoryRecordHelper statusHistoryHelper = await this.CreateStatusHistoryRecordHelperAsync(puid).ConfigureAwait(false);
                bool statusHistoryDeleted = await statusHistoryHelper.CleanupAsync(olderThan).ConfigureAwait(false);

                var stagingStorageHelper = new ExportStagingStorageHelper(this.StorageProvider.CreateCloudBlobClient(), this.logger);
                bool staging = await stagingStorageHelper.DeleteStagingContainerAsync(puid, rec.ExportId).ConfigureAwait(false);

                bool statusDeleted = await statusHelper.InitializeAndDeleteAsync(rec.ExportId).ConfigureAwait(false);

                if (zipDeleted || statusDeleted || statusHistoryDeleted || staging)
                {
                    deleted.Add(rec);
                }
            }
            return deleted;
        }

        /// <summary>
        ///     Clean up old storage
        /// </summary>
        /// <param name="oldestStorage">export storage older than this is deleted</param>
        /// <param name="maxSeconds">max seconds before loop is forced to end</param>
        /// <param name="maxCleanupIterations">max number of iterations</param>
        /// <param name="maxStatusRecordsToCleanupPerIteration">max status records (and associated storage) to cleanup per iteration</param>
        /// <param name="cleanupIterationDelayMilliseconds">delay injected at the end of each loop</param>
        /// <param name="cancellationToken">thread cancellation token</param>
        /// <returns></returns>
        public async Task<int> CleanupOldStorageAsync(
            DateTime oldestStorage,
            int maxSeconds,
            int maxCleanupIterations,
            int maxStatusRecordsToCleanupPerIteration,
            int cleanupIterationDelayMilliseconds,
            CancellationToken cancellationToken)
        {
            DateTime startTime = DateTime.UtcNow;
            DateTime forceEnd = startTime.AddSeconds(maxSeconds);
            this.logger.Information(
                nameof(ExportStorageProvider),
                "Start Export Cleanup " + startTime + " to clean up storage older than " + oldestStorage);
            int totalDeleted = 0;
            try
            {
                for (int i = 0; i < maxCleanupIterations; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        this.logger.Warning(nameof(ExportStorageProvider), "Export Cleanup canceled");
                        break;
                    }

                    if (DateTime.Compare(DateTime.UtcNow, forceEnd) >= 0)
                    {
                        this.logger.Information(nameof(ExportStorageProvider), "Export Cleanup Time is expired, ending loop,  total deleted " + totalDeleted);
                        break;
                    }

                    IList<ExportStatusRecord> deleted = await this.CleanupBatchAsync(oldestStorage, maxStatusRecordsToCleanupPerIteration).ConfigureAwait(false);
                    totalDeleted += deleted.Count;

                    if (deleted.Count == 0 || deleted.Count < maxStatusRecordsToCleanupPerIteration)
                    {
                        break;
                    }
                    totalDeleted += deleted.Count;

                    // ReSharper disable once MethodSupportsCancellation
                    await Task.Delay(cleanupIterationDelayMilliseconds).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                this.logger.Error(nameof(ExportStorageProvider), "Export Cleanup failed. totalDeleted " + totalDeleted + " exception:" + ex);
                throw;
            }

            this.logger.Information(nameof(ExportStorageProvider), "End Export Cleanup.  TotalDeleted:" + totalDeleted);
            return totalDeleted;
        }

        /// <summary>
        ///     Creates a status helper class to perform CRUD on a status record
        /// </summary>
        /// <param name="exportId"></param>
        /// <returns></returns>
        public async Task<IExportStatusRecordHelper> CreateExportStatusHelperAsync(string exportId)
        {
            var exportStatusRecordHelper = new ExportStatusRecordHelper(this.StorageProvider.CreateCloudBlobClient(), this.logger);
            await exportStatusRecordHelper.InitializeAsync(exportId).ConfigureAwait(false);
            return exportStatusRecordHelper;
        }

        /// <summary>
        ///     Creates a helper class that facilitates writing/reading files to the export request staging container
        /// </summary>
        /// <param name="puid"></param>
        /// <param name="exportId"></param>
        /// <returns></returns>
        public async Task<IExportStagingStorageHelper> CreateStagingStorageHelperAsync(string puid, string exportId)
        {
            var stagingStorageHelper = new ExportStagingStorageHelper(this.StorageProvider.CreateCloudBlobClient(), this.logger);
            await stagingStorageHelper.InitializeStagingAsync(puid, exportId).ConfigureAwait(false);
            return stagingStorageHelper;
        }

        /// <summary>
        ///     Creates a helper to manage history records for a user
        /// </summary>
        /// <param name="puidStr"></param>
        /// <returns></returns>
        public async Task<IExportHistoryRecordHelper> CreateStatusHistoryRecordHelperAsync(string puidStr)
        {
            if (!long.TryParse(puidStr, out long puid))
            {
                throw new InvalidCastException("can not parse this string into a puid " + puidStr);
            }
            return await this.CreateStatusHistoryRecordHelperAsync(puid).ConfigureAwait(false);
        }

        /// <summary>
        ///     Creates a helper to manage history records for a user
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Task<IExportHistoryRecordHelper> CreateStatusHistoryRecordHelperAsync(long id)
        {
            var historyHelper = new ExportHistoryRecordHelper(
                new SingleRecordBlobHelper<ExportStatusHistoryRecordCollection>(
                    this.StorageProvider.CreateCloudBlobClient(),
                    GetIdHash(id),
                    this.logger),
                this.maxHistoryRecords,
                this.logger);
            return Task.FromResult<IExportHistoryRecordHelper>(historyHelper);
        }

        /// <summary>
        ///     Creates a helper to manage reading/writing to the export zip file
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IExportZipStorageHelper> CreateZipStorageHelperAsync(long id)
        {
            var zipHelper = new ExportZipStorageHelper(this.StorageProvider.CreateCloudBlobClient(), this.logger);
            await zipHelper.InitializeAsync(id).ConfigureAwait(false);
            return zipHelper;
        }

        /// <summary>
        ///     initializes the storage helper
        /// </summary>
        /// <param name="serviceConfig"></param>
        /// <returns></returns>
        public Task InitializeAsync(IPrivacyExperienceServiceConfiguration serviceConfig)
        {
            return this.InitializeAsync(
                serviceConfig.AzureStorageConfiguration,
                TimeSpan.FromHours(serviceConfig.PrivacyExportConfiguration.ExportQueueTimeToLiveHours),
                TimeSpan.FromSeconds(serviceConfig.PrivacyExportConfiguration.ExportQueueMessageInitialVisibilitySeconds),
                TimeSpan.FromSeconds(serviceConfig.PrivacyExportConfiguration.ExportQueueMessageSubsequentVisibilitySeconds),
                serviceConfig.PrivacyExportConfiguration.ListExportHistoryMax);
        }

        public async Task InitializeAsync(
            IAzureStorageConfiguration azureStorageConfiguration,
            TimeSpan msgTimeToLive,
            TimeSpan initialMsgVisibility,
            TimeSpan subsequentMsgVisibility,
            int maxHistoryRecords)
        {
            this.maxHistoryRecords = maxHistoryRecords;
            await this.StorageProvider.InitializeAsync(azureStorageConfiguration).ConfigureAwait(false);
            
            var createExportQueue = new ExportQueue(this.StorageProvider.CreateCloudQueueClient(), ExportQueueName);
            await createExportQueue.InitializeAsync(msgTimeToLive, initialMsgVisibility, subsequentMsgVisibility).ConfigureAwait(false);
            this.ExportCreationQueue = createExportQueue;

            var exportArchivesDeleteQueue = new ExportQueue(this.StorageProvider.CreateCloudQueueClient(), ExportArchivesDeleteQueueName);
            await exportArchivesDeleteQueue.InitializeAsync(msgTimeToLive, initialMsgVisibility, subsequentMsgVisibility).ConfigureAwait(false);
            this.ExportArchiveDeletionQueue = exportArchivesDeleteQueue;
        }

        private AzureStorageProvider StorageProvider { get; }
    }
}

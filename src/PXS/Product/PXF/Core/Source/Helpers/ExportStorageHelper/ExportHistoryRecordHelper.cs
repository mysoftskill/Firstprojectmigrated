// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Privacy.DataContracts.ExportTypes;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     Export History Record Helper provides CRUD on the history status records within the user's container.
    /// </summary>
    public class ExportHistoryRecordHelper : IExportHistoryRecordHelper
    {
        /// <summary>
        ///     Name of the history blob
        /// </summary>
        private const string HistoryBlobName = "History";

        private readonly int maxHistoryRecords;

        private readonly ISingleRecordBlobHelper<ExportStatusHistoryRecordCollection> writer;

        public ILogger Logger { get; }

        /// <summary>
        ///     constructor with a SingleRecordBlobHelper pointing to the user's container.
        ///     Within that container, there is one history blob that contains a collection of history records.
        /// </summary>
        /// <param name="writer">single record blob</param>
        /// <param name="maxHistoryRecords">configurable max of history records intended to accommodate throttle limits</param>
        /// <param name="log">logger</param>
        public ExportHistoryRecordHelper(ISingleRecordBlobHelper<ExportStatusHistoryRecordCollection> writer, int maxHistoryRecords, ILogger log)
        {
            this.maxHistoryRecords = maxHistoryRecords;
            this.writer = writer;
            this.writer.InitializeAsync(HistoryBlobName).Wait();
            this.Logger = log;
        }

        /// <summary>
        ///     Check to see if the post export request should be throttled for this user
        /// </summary>
        /// <param name="incomingRequest">the incoming request used to evaluate throttling</param>
        /// <param name="beginPeriod">begin period for throttling</param>
        /// <param name="maxCompleted">max number of completed requests</param>
        /// <param name="maxCanceled">max number of canceled requests</param>
        /// <returns></returns>
        public async Task<ExportThrottleState> CheckRequestThrottlingAsync(ExportStatusHistoryRecord incomingRequest, DateTimeOffset beginPeriod, int maxCompleted, int maxCanceled)
        {
            ExportStatusHistoryRecordCollection historyCollection = await this.writer.GetRecordAsync(true).ConfigureAwait(false);
            if (historyCollection?.HistoryRecords == null || historyCollection.HistoryRecords.Count == 0)
            {
                return ExportThrottleState.NotThrottled;
            }
            if (!historyCollection.HistoryRecords[historyCollection.HistoryRecords.Count - 1].Completed.HasValue)
            {
                return ExportThrottleState.RequestInProgress;
            }
            int completed = 0;
            int canceled = 0;
            foreach (ExportStatusHistoryRecord rec in historyCollection.HistoryRecords)
            {
                if (DateTimeOffset.Compare(beginPeriod, rec.RequestedAt) <= 0)
                {
                    if (rec.Completed.HasValue && string.IsNullOrWhiteSpace(rec.Error))
                    {
                        completed++;
                    }
                    else
                    {
                        canceled++;
                    }
                }
            }
            if (completed >= maxCompleted || canceled >= maxCanceled)
            {
                this.Logger.Information(
                    nameof(ExportHistoryRecordHelper),
                    $"Throttled Completed={completed} Canceled={canceled} MaxCompleted={maxCompleted} MaxCanceled={maxCanceled} BeginPeriod={beginPeriod}");
                return ExportThrottleState.TooManyRequests;
            }

            return ExportThrottleState.NotThrottled;
        }

        /// <summary>
        ///     Cleanup old history records for a user
        /// </summary>
        /// <param name="olderThan">clean up records older than this date</param>
        /// <returns></returns>
        public async Task<bool> CleanupAsync(DateTime olderThan)
        {
            int recordsDeleted = 0;
            ExportStatusHistoryRecordCollection historyCollection = await this.writer.GetRecordAsync(true).ConfigureAwait(false);
            if (historyCollection?.HistoryRecords != null && historyCollection.HistoryRecords.Count > 0)
            {
                recordsDeleted = historyCollection.HistoryRecords.RemoveAll(
                    h =>
                    {
                        // Not sure why this case happens. Getting a nullref if try to convert to datetime on next function.
                        // Regardless, if the exportId is null, there's nothing to be done with it other than remove it.
                        // Likely, this is due to old code. We should log this situation and if it continues to happen we need
                        // to look more into it.
                        if (h.ExportId == null)
                        {
                            this.Logger.Error(nameof(ExportHistoryRecordHelper), $"Export CleanupAsync: found null ExportId, deleting from container: {this.writer.ContainerName}");
                            return true;
                        }

                        DateTime requestDt = ExportStorageProvider.ConvertRequestIdToDateTime(h.ExportId);
                        return (DateTime.Compare(requestDt, olderThan) <= 0);
                    });
                if (recordsDeleted > 0)
                {
                    await this.writer.UpsertRecordAsync(historyCollection).ConfigureAwait(false);
                }
            }
            return recordsDeleted > 0;
        }

        /// <inheritdoc />
        public async Task<ExportStatusHistoryRecordCollection> CompleteHistoryRecordAsync(string exportId, DateTimeOffset completed, string error, Uri zipFileUri, long zipFileSize, DateTimeOffset zipFileExpires)
        {
            ExportStatusHistoryRecordCollection historyCollection = await this.writer.GetRecordAsync(true).ConfigureAwait(false);
            if (historyCollection?.HistoryRecords == null)
            {
                this.Logger.Warning(nameof(ExportHistoryRecordHelper), $"unable to get history for export id {exportId}");
                return historyCollection;
            }

            ExportStatusHistoryRecord history = string.IsNullOrWhiteSpace(exportId)
                ? historyCollection.HistoryRecords.FirstOrDefault(h => !h.Completed.HasValue)
                : historyCollection.HistoryRecords.FirstOrDefault(h => h.ExportId == exportId);

            if (history == null)
            {
                this.Logger.Warning(nameof(ExportHistoryRecordHelper), $"history record for request id {exportId} not found");
            }
            else
            {
                history.Completed = DateTimeOffset.UtcNow;
                history.Error = error;
                history.ZipFileUri = zipFileUri;
                history.ZipFileExpires = zipFileExpires;
                history.ZipFileSize = zipFileSize;
                await this.UpsertHistoryCollectionAsync(historyCollection).ConfigureAwait(false);
            }

            return historyCollection;
        }

        /// <summary>
        ///     create a new history record for a request
        /// </summary>
        /// <param name="record">history record to create</param>
        /// <returns></returns>
        public async Task<ExportStatusHistoryRecordCollection> CreateHistoryRecordAsync(ExportStatusHistoryRecord record)
        {
            ExportStatusHistoryRecordCollection historyCollection = await this.writer.GetRecordAsync(true).ConfigureAwait(false) ?? new ExportStatusHistoryRecordCollection();
            if (historyCollection.HistoryRecords == null)
            {
                historyCollection.HistoryRecords = new List<ExportStatusHistoryRecord>();
            }
            if (historyCollection.HistoryRecords.All(h => h.ExportId != record.ExportId))
            {
                EnforceRollingLimits(historyCollection, this.maxHistoryRecords);
                historyCollection.HistoryRecords.Add(record);
            }
            else
            {
                throw new ArgumentException("history record can not be created because it already exists");
            }

            await this.writer.UpsertRecordAsync(historyCollection).ConfigureAwait(false);
            return historyCollection;
        }

        /// <summary>
        ///     Get the collection of history records
        /// </summary>
        /// <param name="allowNotFound">false will throw if not found</param>
        /// <returns></returns>
        public async Task<ExportStatusHistoryRecordCollection> GetHistoryRecordsAsync(bool allowNotFound)
        {
            return await this.writer.GetRecordAsync(allowNotFound).ConfigureAwait(false);
        }

        /// <summary>
        ///     Insert or update the history record collection
        /// </summary>
        /// <param name="historyCollection">takes the whole collection</param>
        /// <returns></returns>
        public async Task<ExportStatusHistoryRecordCollection> UpsertHistoryCollectionAsync(ExportStatusHistoryRecordCollection historyCollection)
        {
            await this.writer.UpsertRecordAsync(historyCollection).ConfigureAwait(false);
            return historyCollection;
        }

        /// <summary>
        ///     Delete export history by Id
        /// </summary>
        /// <param name="id">clean up export record with matching id</param>
        /// <returns></returns>
        public async Task<bool> DeleteExportByIdAsync(string id)
        {
            int recordsDeleted = 0;
            ExportStatusHistoryRecordCollection historyCollection = await this.writer.GetRecordAsync(true).ConfigureAwait(false);
            if (historyCollection?.HistoryRecords != null && historyCollection.HistoryRecords.Count > 0)
            {
                recordsDeleted = historyCollection.HistoryRecords.RemoveAll(
                    h =>
                    {
                        // Not sure why this case happens. Getting a nullref if try to convert to datetime on next function.
                        // Regardless, if the exportId is null, there's nothing to be done with it other than remove it.
                        // Likely, this is due to old code. We should log this situation and if it continues to happen we need
                        // to look more into it.
                        if (h.ExportId == null)
                        {
                            this.Logger.Error(nameof(ExportHistoryRecordHelper), $"Export CleanupAsync: found null ExportId, deleting from container: {this.writer.ContainerName}");
                            return true;
                        }

                        return h.ExportId.Equals(id);
                    });
                if (recordsDeleted > 0)
                {
                    await this.writer.UpsertRecordAsync(historyCollection).ConfigureAwait(false);
                }
            }
            return recordsDeleted > 0;
        }

        /// <summary>
        ///     trim the history record collection to conform to a max number of records.
        ///     in the case of test requests, trim them first so that the non-test requests can be kept for longer.
        /// </summary>
        /// <param name="historyCollection"></param>
        /// <param name="maxHistoryRecords"></param>
        private static void EnforceRollingLimits(ExportStatusHistoryRecordCollection historyCollection, int maxHistoryRecords)
        {
            if (historyCollection?.HistoryRecords == null)
            {
                throw new ArgumentNullException(nameof(historyCollection), "attempt to add null history collection");
            }

            // if we still are at the limit, remove the oldest record.
            while (historyCollection.HistoryRecords.Count >= maxHistoryRecords)
            {
                historyCollection.HistoryRecords.RemoveAt(0);
            }
        }
    }
}

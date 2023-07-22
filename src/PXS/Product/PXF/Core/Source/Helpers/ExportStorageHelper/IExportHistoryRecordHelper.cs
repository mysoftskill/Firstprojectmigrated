// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.DataContracts.ExportTypes;

    /// <summary>
    ///     Export History Record Helpder provides CRUD on the history status records within the user's container.
    /// </summary>
    public interface IExportHistoryRecordHelper
    {
        /// <summary>
        ///     Check to see if the post export request should be throttled for this user
        /// </summary>
        /// <param name="incomingStatusRecord">the incoming request used to evaluate throttling</param>
        /// <param name="beginPeriod">begin period for throttling</param>
        /// <param name="maxCompleted">max number of completed requests</param>
        /// <param name="maxCanceled">max number of cancelled requests</param>
        /// <returns></returns>
        Task<ExportThrottleState> CheckRequestThrottlingAsync(ExportStatusHistoryRecord incomingStatusRecord, DateTimeOffset beginPeriod, int maxCompleted, int maxCanceled);

        /// <summary>
        ///     Cleanup old history records for a user
        /// </summary>
        /// <param name="olderThan">clean up records older than this date</param>
        /// <returns></returns>
        Task<bool> CleanupAsync(DateTime olderThan);

        /// <summary>
        ///     Complete the export request status in the history record collection
        /// </summary>
        Task<ExportStatusHistoryRecordCollection> CompleteHistoryRecordAsync(string exportId, DateTimeOffset completed, string error, Uri zipFileUri, long zipFileSize, DateTimeOffset zipFileExpires);

        /// <summary>
        ///     create a new history record for a request
        /// </summary>
        /// <param name="record">history record to create</param>
        /// <returns></returns>
        Task<ExportStatusHistoryRecordCollection> CreateHistoryRecordAsync(ExportStatusHistoryRecord record);

        /// <summary>
        ///     Get the collection of history records
        /// </summary>
        /// <param name="allowNotFound">false will throw if not found</param>
        /// <returns></returns>
        Task<ExportStatusHistoryRecordCollection> GetHistoryRecordsAsync(bool allowNotFound);

        /// <summary>
        ///     Insert or update the history record collection
        /// </summary>
        /// <param name="history">takes the whole collection</param>
        /// <returns></returns>
        Task<ExportStatusHistoryRecordCollection> UpsertHistoryCollectionAsync(ExportStatusHistoryRecordCollection history);

        /// <summary>
        ///     Delete export history by Id
        /// </summary>
        /// <param name="id">clean up export record with matching id</param>
        /// <returns></returns>
        Task<bool> DeleteExportByIdAsync(string id);

    }
}

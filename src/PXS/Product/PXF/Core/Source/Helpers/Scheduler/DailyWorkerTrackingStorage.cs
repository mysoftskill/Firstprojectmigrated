// -------------------------------------------------------------------------
// <copyright>
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler
{
    using System;
    using System.Net;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler.Interfaces;

    /// <summary>
    /// Storage operations for daily worker tracking
    /// </summary>
    internal class DailyWorkerTrackingStorage : IDailyWorkerTrackingStorage
    {
        private ICloudTable dailyWorkerTrackingTable;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dailyworkerTrackingTable">Cloud table for the daily worker tracking table</param>
        public DailyWorkerTrackingStorage(ICloudTable dailyworkerTrackingTable)
        {
            this.dailyWorkerTrackingTable = dailyworkerTrackingTable;
        }

        /// <summary>
        /// Retrieves the current state for the worker for the specified date
        /// </summary>
        /// <param name="workItemName">Work item name</param>
        /// <param name="date">Date</param>
        /// <returns>Tracking row for the work/date tuple</returns>
        public async Task<DailyWorkerTracking> RetrieveAsync(string workItemName, DateTime date)
        {
            var tableResult = await this.dailyWorkerTrackingTable.QuerySingleRowAsync(
                workItemName,
                EntityFactory.QualifyRowKey(DailyWorkerTracking.RowQualifier, DailyWorkerTracking.ConvertDateTimeToRowKey(date)));

            var dailyTracker = EntityFactory.FromTableResult<DailyWorkerTracking>(tableResult, DailyWorkerTracking.RowQualifier);
            return dailyTracker;
        }

        /// <summary>
        /// Attempts to insert a new work tracking row
        /// </summary>
        /// <param name="tracker">Work tracking row</param>
        /// <returns>Result</returns>
        /// <remarks>This method does not throw if the row already exists in the table. InsertOrUpdateSuccess property of the result is false instead.</remarks>
        public async Task<DailyTrackerResult> InsertAsync(DailyWorkerTracking tracker)
        {
            var result = new DailyTrackerResult { InsertOrUpdateSuccess = false };
            var tableResult = await this.dailyWorkerTrackingTable.InsertAsync(tracker.Entity, true);
            if (tableResult.HttpStatusCode != (int)HttpStatusCode.Conflict)
            {
                result.InsertOrUpdateSuccess = true;
                result.CurrentRowValue = tracker;
                return result;
            }

            // If we did hit a conflict during insert, retrieve and return the current row value
            result.CurrentRowValue = await RetrieveAsync(tracker.WorkItemName, tracker.Date.Value);
            return result;
        }

        /// <summary>
        /// Attempts to update the work tracking row
        /// </summary>
        /// <param name="tracker">Work tracking row</param>
        /// <returns>Result</returns>
        /// <remarks>This method does not throw there is an update concurrency conflict. InsertOrUpdateSuccess property of the result is false instead.</remarks>
        public async Task<DailyTrackerResult> UpdateAsync(DailyWorkerTracking tracker)
        {
            var result = new DailyTrackerResult { InsertOrUpdateSuccess = false };
            var tableResult = await this.dailyWorkerTrackingTable.ReplaceAsync(tracker.Entity, true);
            if (tableResult.HttpStatusCode != (int)HttpStatusCode.PreconditionFailed)
            {
                result.InsertOrUpdateSuccess = true;
                result.CurrentRowValue = tracker;
                return result;
            }

            // If we did hit a conflict during insert, retrieve and return the current row value
            result.CurrentRowValue = await RetrieveAsync(tracker.WorkItemName, tracker.Date.Value);
            return result;
        }
    }
}

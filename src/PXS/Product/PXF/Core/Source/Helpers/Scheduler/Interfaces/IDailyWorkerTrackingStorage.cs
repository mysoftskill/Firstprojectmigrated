// -------------------------------------------------------------------------
// <copyright>
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -------------------------------------------------------------------------


namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler.Interfaces
{
    using System;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Result from a Daily work tracking storage operation
    /// </summary>
    public sealed class DailyTrackerResult
    {
        /// <summary>
        /// Indicates if the insert or update operation was a success with no concurrency conflicts
        /// </summary>
        public bool InsertOrUpdateSuccess { get; set; }

        /// <summary>
        /// Current value of the row after the requested operation completed
        /// </summary>
        public DailyWorkerTracking CurrentRowValue { get; set; }
    }

    /// <summary>
    /// Storage operations for daily worker tracking
    /// </summary>
    public interface IDailyWorkerTrackingStorage
    {
        /// <summary>
        /// Retrieves the current state for the worker for the specified date
        /// </summary>
        /// <param name="workItemName">Work item name</param>
        /// <param name="date">Date</param>
        /// <returns>Tracking row for the work/date tuple</returns>
        Task<DailyWorkerTracking> RetrieveAsync(string workItemName, DateTime date);

        /// <summary>
        /// Attempts to insert a new work tracking row
        /// </summary>
        /// <param name="tracker">Work tracking row</param>
        /// <returns>Result</returns>
        /// <remarks>This method does not throw if the row already exists in the table. InsertOrUpdateSuccess property of the result is false instead.</remarks>
        Task<DailyTrackerResult> InsertAsync(DailyWorkerTracking tracker);

        /// <summary>
        /// Attempts to update the work tracking row
        /// </summary>
        /// <param name="tracker">Work tracking row</param>
        /// <returns>Result</returns>
        /// <remarks>This method does not throw there is an update concurrency conflict. InsertOrUpdateSuccess property of the result is false instead.</remarks>
        Task<DailyTrackerResult> UpdateAsync(DailyWorkerTracking tracker);
    }
}

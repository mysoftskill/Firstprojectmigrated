// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler.Interfaces
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    ///     Result from a work tracking storage operation
    /// </summary>
    public sealed class WorkerTrackerResult
    {
        /// <summary>
        ///     Current value of the row after the requested operation completed
        /// </summary>
        public WorkerTracking CurrentRowValue { get; set; }

        /// <summary>
        ///     Indicates if the insert or update operation was a success with no concurrency conflicts
        /// </summary>
        public bool InsertOrUpdateSuccess { get; set; }
    }

    /// <summary>
    /// </summary>
    public interface IWorkerTrackingStorage
    {
        /// <summary>
        ///     Attempts to insert a new work tracking row
        /// </summary>
        /// <param name="tracker">Work tracking row</param>
        /// <returns>Result</returns>
        /// <remarks>This method does not throw if the row already exists in the table. InsertOrUpdateSuccess property of the result is false instead.</remarks>
        Task<WorkerTrackerResult> InsertAsync(WorkerTracking tracker);

        /// <summary>
        ///     Retrieves the current state for the worker
        /// </summary>
        /// <param name="workItemName">Work item name</param>
        /// <returns>Tracking row for the work</returns>
        Task<WorkerTracking> RetrieveAsync(string workItemName);

        /// <summary>
        ///     Attempts to update the work tracking row
        /// </summary>
        /// <param name="tracker">Work tracking row</param>
        /// <returns>Result</returns>
        /// <remarks>This method does not throw there is an update concurrency conflict. InsertOrUpdateSuccess property of the result is false instead.</remarks>
        Task<WorkerTrackerResult> UpdateAsync(WorkerTracking tracker);
    }
}

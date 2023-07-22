// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler
{
    using System;
    using System.Net;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler.Interfaces;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    ///     WorkerTrackingStorage
    /// </summary>
    public class WorkerTrackingStorage : IWorkerTrackingStorage
    {
        private readonly ICloudTable workerTrackingTable;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="workerTrackingTable">Cloud table for the worker tracking table</param>
        public WorkerTrackingStorage(ICloudTable workerTrackingTable)
        {
            this.workerTrackingTable = workerTrackingTable;
        }

        /// <inheritdoc />
        public async Task<WorkerTrackerResult> InsertAsync(WorkerTracking tracker)
        {
            var result = new WorkerTrackerResult { InsertOrUpdateSuccess = false };
            TableResult tableResult = await this.workerTrackingTable.InsertAsync(tracker.Entity, true).ConfigureAwait(false);
            if (tableResult.HttpStatusCode != (int)HttpStatusCode.Conflict)
            {
                result.InsertOrUpdateSuccess = true;
                result.CurrentRowValue = tracker;
                return result;
            }

            // If we did hit a conflict during insert, retrieve and return the current row value
            result.CurrentRowValue = await this.RetrieveAsync(tracker.WorkItemName).ConfigureAwait(false);
            return result;
        }

        /// <inheritdoc />
        public async Task<WorkerTracking> RetrieveAsync(string workItemName)
        {
            TableResult tableResult = await this.workerTrackingTable.QuerySingleRowAsync(
                partitionKey: workItemName,
                rowKey: EntityFactory.QualifyRowKey(WorkerTracking.RowQualifier, workItemName)).ConfigureAwait(false);

            var tracker = EntityFactory.FromTableResult<WorkerTracking>(tableResult, WorkerTracking.RowQualifier);
            return tracker;
        }

        /// <inheritdoc />
        public async Task<WorkerTrackerResult> UpdateAsync(WorkerTracking tracker)
        {
            var result = new WorkerTrackerResult { InsertOrUpdateSuccess = false };
            tracker.WorkerMachineName = WorkerTracking.EnvironmentMachineName;
            TableResult tableResult = await this.workerTrackingTable.ReplaceAsync(tracker.Entity, true).ConfigureAwait(false);
            if (tableResult.HttpStatusCode != (int)HttpStatusCode.PreconditionFailed)
            {
                result.InsertOrUpdateSuccess = true;
                result.CurrentRowValue = tracker;
                return result;
            }

            // If we did hit a conflict during insert, retrieve and return the current row value
            result.CurrentRowValue = await this.RetrieveAsync(tracker.WorkItemName).ConfigureAwait(false);
            return result;
        }
    }
}

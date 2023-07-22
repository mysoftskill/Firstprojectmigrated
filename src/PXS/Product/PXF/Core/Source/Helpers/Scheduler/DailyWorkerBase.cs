// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler.Interfaces;

    using Microsoft.PrivacyServices.Common.Azure;

    public abstract class DailyWorkerBase : WorkerBase
    {
        protected readonly ILogger logger;

        private readonly IDailyWorkerTrackingStorage workTrackingStorage;

        /// <summary>
        ///     The name of the work item being processed
        /// </summary>
        /// <remarks>This is intended to only be set in CheckForWorkInternal after verifying the work item is available.</remarks>
        public string DailyWorkItemName { get; internal set; }

        /// <summary>
        ///     The date the work item is processing for
        /// </summary>
        public DateTime WorkDate { get; protected set; }

        /// <summary>
        ///     Maximum time that this work item is expected to take to process
        /// </summary>
        /// <remarks>After this amount of time, it is assumed that the work failed if not complete and new instance may be started.</remarks>
        public abstract TimeSpan WorkExpirationTimeSpan { get; }

        /// <summary>
        ///     Time added to the current UTC time to derive the work date
        /// </summary>
        public abstract TimeSpan WorkUtcOffset { get; }

        /// <inheritdoc />
        public override async Task<WorkResult> CheckForWorkAsync(DateTime startTime, WorkOperationEvent workEvent, CancellationToken cancellationToken)
        {
            this.WorkDate = this.ConvertStartTimeToWorkDate(startTime);
            WorkResult result = await base.CheckForWorkAsync(startTime, workEvent, cancellationToken).ConfigureAwait(false);

            // If the base class fails or finds that work is not ready, return immediately
            if (!result.Success || !result.WorkReady)
            {
                return result;
            }

            // Uses the class WorkItemName property as the work item to check in the default case
            // To use more granular work item names, you must override this method.
            return await this.CheckForWorkInternalAsync(startTime, this.WorkItemName).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public override async Task<WorkResult> MarkWorkCompletedAsync(DateTime startTime, WorkOperationEvent workEvent, CancellationToken cancellationToken)
        {
            await base.MarkWorkCompletedAsync(startTime, workEvent, cancellationToken).ConfigureAwait(false);

            DateTime workDate = this.ConvertStartTimeToWorkDate(startTime);
            DailyWorkerTracking tracker = await this.workTrackingStorage.RetrieveAsync(this.DailyWorkItemName, workDate).ConfigureAwait(false);
            if (tracker == null)
            {
                throw new WorkerOperationAbortedException("Tracker row not found while marking work complete");
            }

            // We don't expect that another job already marked the work complete.
            if (tracker.WorkCompleted == true)
            {
                throw new WorkerOperationAbortedException("Operation has already been completed on another instance - output artifacts may be duplicated or corrupted.");
            }

            tracker.WorkCompleted = true;
            await this.workTrackingStorage.UpdateAsync(tracker).ConfigureAwait(false);

            return WorkResult.Succeeded;
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="workTrackingStorage">Daily work tracking storage</param>
        /// <param name="logger">The logger.</param>
        protected DailyWorkerBase(IDailyWorkerTrackingStorage workTrackingStorage, ILogger logger)
        {
            this.workTrackingStorage = workTrackingStorage ?? throw new ArgumentNullException(nameof(workTrackingStorage));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        ///     Checks for availability of a specific work item name (ensures no other worker instance currently operating on the same workload)
        /// </summary>
        /// <param name="startTime">Time of the start of work</param>
        /// <param name="dailyWorkItemName">Name of the work item</param>
        /// <returns>Result of the check</returns>
        protected async Task<WorkResult> CheckForWorkInternalAsync(
            DateTime startTime,
            string dailyWorkItemName)
        {
            // First try inserting a new row
            var workTracker = new DailyWorkerTracking(dailyWorkItemName, this.WorkDate)
            {
                WorkStartedTime = startTime,
                WorkCompleted = false
            };
            DailyTrackerResult insertResult = await this.workTrackingStorage.InsertAsync(workTracker).ConfigureAwait(false);

            // If we were able to insert, then we are the first instance to run for this date
            if (insertResult.InsertOrUpdateSuccess)
            {
                // We didn't have a conflict writing the new row for today's date, so proceed
                this.DailyWorkItemName = dailyWorkItemName;
                return WorkResult.Succeeded;
            }

            if (insertResult.CurrentRowValue == null)
            {
                throw new NotSupportedException("Insert failed, but null row returned - this shouldn't happen.");
            }

            // If the work is done for today, or started by another role but not past expiration time,
            // then just return no work for now.
            if (insertResult.CurrentRowValue.WorkCompleted == true)
            {
                this.logger.Information(
                    nameof(DailyWorkerBase),
                    $"Work already completed for Date: {insertResult.CurrentRowValue?.Date}, WorkItemName: {insertResult.CurrentRowValue?.WorkItemName}");
                return WorkResult.NoWork;
            }

            TimeSpan? timeSinceStart;
            if ((timeSinceStart = (startTime - insertResult.CurrentRowValue.WorkStartedTime)) < this.WorkExpirationTimeSpan)
            {
                this.logger.Information(
                    nameof(DailyWorkerBase),
                    $"WorkItemName: {insertResult.CurrentRowValue?.WorkItemName}, Work is already started on another instance for Date: {insertResult.CurrentRowValue?.Date}. Expiration time remaining is: {this.WorkExpirationTimeSpan - timeSinceStart}");
                return WorkResult.NoWork;
            }

            // Let's try to update the row with a new time
            DailyWorkerTracking update = insertResult.CurrentRowValue;
            update.WorkStartedTime = startTime;
            DailyTrackerResult updateResult = await this.workTrackingStorage.UpdateAsync(update).ConfigureAwait(false);
            if (updateResult.InsertOrUpdateSuccess)
            {
                this.DailyWorkItemName = dailyWorkItemName;
                return WorkResult.Succeeded;
            }
            return WorkResult.NoWork;
        }

        protected DateTime ConvertStartTimeToWorkDate(DateTime startTime)
        {
            return startTime.ToUniversalTime().Add(this.WorkUtcOffset).Date;
        }

        /// <summary>
        ///     Call this periodically from DoWork for long running operations. It checks cancellation and also
        ///     refreshes the work started time to prevent another instance from starting on a long running job.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        protected async Task RefreshWorkStartedTimeAsync(CancellationToken cancellationToken)
        {
            // Check for cancellation
            cancellationToken.ThrowIfCancellationRequested();

            DailyWorkerTracking tracker = await this.workTrackingStorage.RetrieveAsync(this.DailyWorkItemName, this.WorkDate).ConfigureAwait(false);
            if (tracker == null)
            {
                throw new WorkerOperationAbortedException("Tracker row not found while refreshing start time.");
            }

            // We don't expect that another job already marked the work complete.
            if (tracker.WorkCompleted == true)
            {
                throw new WorkerOperationAbortedException("Operation has already been completed on another instance, while refreshing start time");
            }

            tracker.WorkStartedTime = DateTime.UtcNow;
            await this.workTrackingStorage.UpdateAsync(tracker).ConfigureAwait(false);
        }

        internal void SetWorkDate(DateTime workDate)
        {
            this.WorkDate = workDate;
        }
    }
}

// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;

    public enum RunIntervalType
    {
        /// <summary>
        ///     The next schedule run occurs 'RunInterval' after the last instance completed.
        /// </summary>
        FromLastFinish
    }

    /// <summary>
    ///     State of the worker instance
    /// </summary>
    public enum WorkerState
    {
        NotStarted,

        CheckingForWork,

        DoingWork,

        WaitingForNextRun,

        Stopped
    }

    /// <summary>
    ///     Base class for workers
    /// </summary>
    public abstract class WorkerBase
    {
        /// <summary>
        ///     How the RunInterval is applied
        /// </summary>
        public abstract RunIntervalType IntervalType { get; }

        /// <summary>
        ///     Name of the operation (this appears in telemetry events as the 'operation')
        /// </summary>
        public abstract string OperationName { get; }

        /// <summary>
        ///     How often the scheduler should run this work
        /// </summary>
        public abstract TimeSpan RunInterval { get; }

        /// <summary>
        ///     Current state of the worker
        /// </summary>
        public WorkerState State { get; internal set; }

        /// <summary>
        ///     Name of the work item (this appears in QoS as the "Partner")
        /// </summary>
        public abstract string WorkItemName { get; }

        /// <summary>
        ///     Checks to see if there is work for this instance to do right now
        /// </summary>
        /// <param name="startTime">Time of the start of the run</param>
        /// <param name="workEvent">Telemetry event</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the check</returns>
        /// <remarks>It is optional to overload this.</remarks>
        public virtual Task<WorkResult> CheckForWorkAsync(
            DateTime startTime,
            WorkOperationEvent workEvent,
            CancellationToken cancellationToken)
        {
            // Check for cancellation
            cancellationToken.ThrowIfCancellationRequested();

            // Default implementation assumes that work is ready now
            return Task.FromResult(WorkResult.Succeeded);
        }

        /// <summary>
        ///     Abstract work item. Do work specific business logic here.
        /// </summary>
        /// <param name="startTime">Time of the start of the run</param>
        /// <param name="workEvent">Telemetry event</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the work</returns>
        public abstract Task<WorkResult> DoWorkAsync(
            DateTime startTime,
            WorkOperationEvent workEvent,
            CancellationToken cancellationToken);

        /// <summary>
        ///     Mark work completed
        /// </summary>
        /// <param name="startTime">Time of the start of the run</param>
        /// <param name="workEvent">Telemetry event</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the update</returns>
        /// <remarks>Note: this is called only after successful completion of DoWork() by the scheduler.</remarks>
        public virtual Task<WorkResult> MarkWorkCompletedAsync(
            DateTime startTime,
            WorkOperationEvent workEvent,
            CancellationToken cancellationToken)
        {
            // Check for cancellation
            cancellationToken.ThrowIfCancellationRequested();

            // Default implementation assumes that work is completed
            return Task.FromResult(WorkResult.Succeeded);
        }
    }
}

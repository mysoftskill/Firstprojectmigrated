// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.Runner.Tasks
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Locks;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.Exceptions;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Data;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Utility;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     task to execute queued tasks
    /// </summary>
    public class JobRunnerTask : TrackCountersBaseTask<IDataActionJobRunnerConfig>
    {
        private static readonly TimeSpan DequeueTimeout = TimeSpan.MaxValue;
        
        private readonly IQueue<JobWorkItem> actionQueue;
        private readonly IContextFactory execCtxFactory;
        private readonly ILockManager lockMgr;
        private readonly TimeSpan delayOnIncomplete;
        private readonly TimeSpan queueLeaseTime;
        private readonly TimeSpan emptyQueuePause;

        /// <summary>
        ///     Initializes a new instance of the JobRunnerTask class
        /// </summary>
        /// <param name="config">task config</param>
        /// <param name="actionQueue">action queue</param>
        /// <param name="counterFactory">perf counter factory</param>
        /// <param name="execCtxFactory">execute context factory</param>
        /// <param name="lockManager">lock manager</param>
        /// <param name="logger">trace logger</param>
        public JobRunnerTask(
            IDataActionJobRunnerConfig config,
            IQueue<JobWorkItem> actionQueue,
            ICounterFactory counterFactory,
            IContextFactory execCtxFactory,
            ILockManager lockManager,
            ILogger logger) :
            base(config, counterFactory, logger)
        {
            this.execCtxFactory = execCtxFactory ?? throw new ArgumentNullException(nameof(execCtxFactory));
            this.actionQueue = actionQueue ?? throw new ArgumentNullException(nameof(actionQueue));
            this.lockMgr = lockManager ?? throw new ArgumentNullException(nameof(lockManager));

            this.delayOnIncomplete = TimeSpan.FromMinutes(config.DelayIfCouldNotCompleteMinutes);
            this.emptyQueuePause = TimeSpan.FromSeconds(config.DelayOnEmptyQueueSeconds);
            this.queueLeaseTime = TimeSpan.FromMinutes(config.LeaseMinutes);
        }

        /// <summary>
        ///     Starts up the set of tasks used by the task to execute work
        /// </summary>
        /// <param name="ctx">operation context</param>
        /// <returns>non-null time span to indicate a wait time before the next run or null to run again immediately</returns>
        protected override async Task<TimeSpan?> RunOnceAsync(OperationContext ctx)
        {
            IQueueItem<JobWorkItem> item;
            JobWorkItem data;
            bool shouldComplete = false;

            ctx.Op = "dequeue data manifest file work item";

            item = await this.actionQueue
                .DequeueAsync(this.queueLeaseTime, JobRunnerTask.DequeueTimeout, null, this.CancelToken)
                .ConfigureAwait(false);

            data = item?.Data;

            if (data == null)
            {
                // wait 10 before next attempt if we failed to dequeue
                return this.emptyQueuePause;
            }

            ctx.Item = $"[ref id: {data.RefId ?? "UNKNOWN"}][action: {data.ActionRef?.Tag ?? "UNKNOWN"}]";

            // if we process this item without success too many times, abandon it and wait for the cosmos monitor to re-enqueue it
            //  later. This will keep us from processing the same items over and over and starving other items
            if (item.DequeueCount > this.Config.MaxDequeueCount)
            {
                const string Fmt =
                    "Dequeued item {0} for processing, but its previous dequeue count of {1} exceeds " +
                    "the max allowed dequeue count of {2}. Abandoning";

                this.TraceWarning(Fmt, ctx.Item, item.DequeueCount, this.Config.MaxDequeueCount);

                await item.CompleteAsync();
                return null;
            }

            //
            // acquire the lock for the action reference.  If we can't then another thread / process owns it, so we can just ignore
            //  it- if they fail processing it'll get appended to the queue again later by the file system monitor.
            // 

            try
            {
                IExecuteContext execCtx;
                ILockLease lease;
                bool isSimulation = data.IsSimulation || this.Config.ForceSimulationMode;

                ctx.Op = "acquire action ref lease";

                lease = await this.lockMgr.AttemptAcquireAsync(
                    Constants.JobRunnerLockGroup,
                    data.RefId,
                    ctx.TaskId,
                    data.TaskLeaseTime,
                    false).ConfigureAwait(false);
                if (lease == null)
                {
                    // someone else owns it; keeping calm and carrying on.
                    // We'll mark it complete because even if the owner fails AND runs out of dequeue attempts, the scheduler task
                    //  will pick it up and create another queue item if applicable.
                    shouldComplete = true;

                    return null;
                }

                this.CancelToken.ThrowIfCancellationRequested();

                ctx.Op = "Generating execution context";

                execCtx = this.execCtxFactory.Create<IExecuteContext>(
                    this.CancelToken, 
                    isSimulation, 
                    data.ExtensionProperties,
                    this.TaskCounterCategory);

                execCtx.OnActionStart(ActionType.Execute, data.RefId);

                this.TraceInfo("Processing action ref queue item " + ctx.Item);

                try
                {
                    ICounter execAttemptsCounter = this.GetCounter("Action Execute Attempts", CounterType.Number);

                    execAttemptsCounter.Increment(ctx.Item);
                    execAttemptsCounter.Increment();

                    ctx.Op = "Executing action";

                    try
                    {
                        await data.Executor.ExecuteActionAsync(execCtx, data.ActionRef).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        ICounter execErrorCounter = this.GetCounter("Action Execute Errors", CounterType.Number);

                        execErrorCounter.Increment(ctx.Item);
                        execErrorCounter.Increment();

                        execCtx.LogError(e, "Exception occurred");

                        this.TraceError(execCtx.GetLogs(EntryTypes.All));
                        throw;
                    }

                    if (execCtx.HasErrors)
                    {
                        ICounter execErrorCounter = this.GetCounter("Action Execute Errors", CounterType.Number);

                        execErrorCounter.Increment(ctx.Item);
                        execErrorCounter.Increment();

                        this.TraceError(execCtx.GetLogs(EntryTypes.All));

                        throw new ActionExecuteException(
                            $"Failed to execute action {ctx.Item}: {execCtx.GetLogs(EntryTypes.Error)}");
                    }
                    else if (this.Config.ForceVerboseLogOnSuccess || data.EmitVerboseLogging)
                    {
                        this.TraceInfo(execCtx.GetLogs(EntryTypes.All));
                    }
                    else
                    {
                        this.TraceInfo(execCtx.GetLogs(EntryTypes.Title));
                    }

                    shouldComplete = true;

                    this.TraceInfo("Completed action ref queue item " + ctx.Item);

                    this.CancelToken.ThrowIfCancellationRequested();
                }
                finally
                {
                    ctx.PushOp();
                    ctx.Op = "release action ref queue item lease";

                    await lease.ReleaseAsync(false).ConfigureAwait(false);

                    ctx.PopOp();
                }
            }
            finally
            {
                ctx.PushOp();
                ctx.Op = "release queue item";

                // a release is basically a renew with a short time span.  In this case, if we don't complete the 
                //  command, we want to wait a few minutes before trying again to ensure that we give any 
                //  transient issues time to resolve (such as a missing command feed command)
                await (shouldComplete ? item.CompleteAsync() : item.RenewLeaseAsync(this.delayOnIncomplete)).ConfigureAwait(false);

                ctx.PopOp();
            }

            return null;
        }
    }
}

namespace Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.DistributedLocking;
    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Base task for async worker operations.
    /// </summary>
    public abstract class WorkerTaskBase<TParameters, TLockState> where TLockState : class
    {
        private readonly string taskName;
        private readonly Service.Common.ConfigGen.ITaskConfiguration taskConfig;
        private readonly DistributedLock<TLockState> distributedLock;

        /// <summary>
        /// Task name
        /// </summary>
        public string TaskName => this.taskName;

        /// <summary>
        /// Ctor for base task. Accepts a configuration and a task name, and optional lock primitives.
        /// </summary>
        protected WorkerTaskBase(
            Service.Common.ConfigGen.ITaskConfiguration taskConfig, 
            string taskName,
            IDistributedLockPrimitives<TLockState> distributedLockPrimitives)
        {
            this.taskConfig = taskConfig;
            this.taskName = taskName;

            if (distributedLockPrimitives == null)
            {
                this.distributedLock = new DistributedLock<TLockState>(
                    taskName,
                    Config.Instance.DistributedLocks.BlobConnectionString,
                    Config.Instance.DistributedLocks.BlobContainerName,
                    EventLogger.Instance);
            }
            else
            {
                this.distributedLock = new DistributedLock<TLockState>(taskName, distributedLockPrimitives);
            }
        }

        /// <summary>
        /// Periodically start the task based on task config
        /// </summary>
        [ExcludeFromCodeCoverage] // Justification: runs as a periodic background operation. Not really coverable.
        public async Task StartAsync(Func<TParameters> parameterFunc, CancellationToken cancellationToken)
        {
            await Task.Yield();

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    int delaySeconds = RandomHelper.Next(
                        this.taskConfig.MinSleepTimeSeconds,
                        this.taskConfig.MaxSleepTimeSeconds);

                    DualLogger.Instance.Verbose(nameof(WorkerTaskBase<TParameters, TLockState>), $"[{this.TaskName}] Sleeping for {delaySeconds} seconds");

                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);

                    if (this.taskConfig.Enabled)
                    {
                        DualLogger.Instance.Verbose(nameof(WorkerTaskBase<TParameters, TLockState>), $"[{this.TaskName}] Sleep over, starting the task.");

                        await Logger.InstrumentAsync(
                            new IncomingEvent(SourceLocation.Here()),
                            ev => this.RunAsync(ev, parameterFunc()));
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance?.UnexpectedException(ex);
                }
            }
        }

        /// <summary>
        /// Runs the work item once and returns. If the lock is unavailable, then the method simply returns.
        /// </summary>
        public Task RunOnceAsync(TParameters parameters)
        {
            return this.RunAsync(IncomingEvent.Current ?? new IncomingEvent(SourceLocation.Here()), parameters);
        }

        private async Task RunAsync(IncomingEvent ev, TParameters parameters)
        {
            ev.OperationName = $"{this.TaskName}.{nameof(this.RunAsync)}";

            var acquireLockResult = await this.distributedLock.TryAcquireAsync(TimeSpan.FromMinutes(this.taskConfig.LockDurationMinutes), DualLogger.Instance);
            ev.SetProperty("LockAvailable", acquireLockResult.Succeeded.ToString());

            if (!acquireLockResult.Succeeded)
            {
                // Not able to acquire the lock. Bail.
                DualLogger.Instance.Information(nameof(WorkerTaskBase<TParameters, TLockState>), $"[{this.TaskName}] Lock is unavailable");
                ev.StatusCode = HttpStatusCode.OK;
                return;
            }

            bool shouldRun = this.ShouldRun(acquireLockResult.Status);
            ev.SetProperty("ShouldRun", shouldRun.ToString());

            if (!shouldRun)
            {
                DualLogger.Instance.Information(nameof(WorkerTaskBase<TParameters, TLockState>), $"[{this.TaskName}] It is NOT time to run");
                ev.StatusCode = HttpStatusCode.OK;
                return;
            }

            DualLogger.Instance.Information(nameof(WorkerTaskBase<TParameters, TLockState>), $"[{this.TaskName}] It's time to run and we own the lock. What luck!");

            IEnumerable<Func<Task>> allTasks = this.GetTasksAsync(acquireLockResult.Status, parameters);
            List<Task> pendingTasks = new List<Task>();

            while (pendingTasks.Any() || allTasks.Any())
            {
                while (pendingTasks.Count < this.taskConfig.BatchSize && allTasks.Any())
                {
                    Func<Task> factory = allTasks.First();
                    pendingTasks.Add(factory());
                    allTasks = allTasks.Skip(1);
                }

                Task extendLeaseTask = Task.Delay(TimeSpan.FromMinutes(1));
                pendingTasks.Add(extendLeaseTask);

                Task completedTask = await Task.WhenAny(pendingTasks);

                if (completedTask != extendLeaseTask)
                {
                    // Allow any exceptions to be thrown.
                    await completedTask;
                    pendingTasks.Remove(completedTask);
                }

                pendingTasks.Remove(extendLeaseTask);

                if (this.distributedLock.RemainingTime <= TimeSpan.FromMinutes(this.taskConfig.LockExtensionThresholdMinutes))
                {
                    DualLogger.Instance.Information(nameof(WorkerTaskBase<TParameters, TLockState>), $"[{this.TaskName}] Extending lock for {this.taskConfig.LockDurationMinutes} minutes, Remaining Pairs = {allTasks.Count()}");
                    bool extended = await this.distributedLock.TryExtendAsync(TimeSpan.FromMinutes(this.taskConfig.LockDurationMinutes), acquireLockResult.Status, DualLogger.Instance);
                    if (!extended)
                    {
                        DualLogger.Instance.Information(nameof(WorkerTaskBase<TParameters, TLockState>), $"[{this.TaskName}] Failed to extend lock in middle of operation. Bailing!");
                        ev.SetProperty("FailedToExtendLock", "true");
                        ev.StatusCode = HttpStatusCode.OK;
                        return;
                    }
                }
            }

            TLockState finalLockState = this.GetFinalLockState(out TimeSpan leaseTime);

            // Finally, update the lock state to reflect the current time we finished. Extend the lease such that it won't expire again
            // until roughly the time that this task should run again.
            await this.distributedLock.TryExtendAsync(leaseTime, finalLockState, DualLogger.Instance);

            var nextTime = DateTimeOffset.UtcNow + leaseTime;
            DualLogger.Instance.Information(nameof(WorkerTaskBase<TParameters, TLockState>), $"[{this.TaskName}] All done! Lock will next be available in {leaseTime.ToString()}.");
            DualLogger.Instance.Information(nameof(WorkerTaskBase<TParameters, TLockState>), $"[{this.TaskName}] Next available time: {nextTime.ToString("o")}.");
            ev.SetProperty("NextAvailableTime", nextTime.ToString());

            ev.StatusCode = HttpStatusCode.OK;
        }
        
        protected abstract bool ShouldRun(TLockState lockState);

        protected abstract IEnumerable<Func<Task>> GetTasksAsync(TLockState state, TParameters parameters);

        protected abstract TLockState GetFinalLockState(out TimeSpan leaseTime);
    }
}

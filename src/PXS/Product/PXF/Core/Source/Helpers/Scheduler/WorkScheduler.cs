// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    
    using Ms.Qos;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     Scheduler for Workers.
    /// </summary>
    public sealed class WorkScheduler : IDisposable
    {
        private readonly ILogger logger;

        private readonly TimeSpan minimumWait = TimeSpan.FromSeconds(1);

        private readonly CancellationTokenSource cancellationSource = new CancellationTokenSource();

        private bool disposed;

        private bool started;

        private readonly Dictionary<int, Func<WorkerBase>> workerFactories = new Dictionary<int, Func<WorkerBase>>();

        /// <summary>
        ///     Instantiates an instance of a scheduler
        /// </summary>
        /// <param name="workers">List of workers this schedule manages</param>
        /// <param name="logger">The logger</param>
        public WorkScheduler(IEnumerable<Func<WorkerBase>> workers, ILogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            int i = 0;
            foreach (Func<WorkerBase> worker in workers)
            {
                this.workerFactories.Add(i, worker);
                i++;
            }
        }

        /// <summary>
        ///     Disposes the scheduler. This stops all workers.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "cancellationSource")]
        public void Dispose()
        {
            this.disposed = true;
            this.cancellationSource.Cancel();
        }

        /// <summary>
        ///     Starts all workers
        /// </summary>
        public void Start()
        {
            if (this.started)
            {
                throw new InvalidOperationException("Scheduler cannot be started more than once.");
            }

            if (this.disposed)
            {
                throw new InvalidOperationException("Scheduler cannot be started after it has been disposed.");
            }

            foreach (KeyValuePair<int, Func<WorkerBase>> kvp in this.workerFactories)
            {
                this.StartWork(kvp.Value, kvp.Key);
            }

            this.started = true;
        }

        private async void CompleteOperationAndScheduleNext(
            WorkerBase worker,
            OutgoingApiEventWrapper op,
            WorkResult workResult,
            int workerFactoryIndex,
            WorkOperationEvent qosEvent
        )
        {
            this.logger.MethodEnter(nameof(WorkScheduler), nameof(this.CompleteOperationAndScheduleNext));

            // Complete the logical operation
            qosEvent.WorkReady = workResult.WorkReady;
            qosEvent.baseData.requestStatus = workResult.Success ? ServiceRequestStatus.Success : ServiceRequestStatus.ServiceError;
            if (workResult.ErrorDetails != null)
            {
                qosEvent.ErrorDetails = workResult.ErrorDetails;
            }
            else if (workResult.Exception != null)
            {
                qosEvent.LogException(workResult.Exception, true);
            }

            op.Success = workResult.Success;
            op.ProtocolStatusCode = workResult.Success ? "200" : "500";
            op.Finish();

            // Start waiting for the next run
            worker.State = WorkerState.WaitingForNextRun;
            try
            {
                await Task.Delay(workResult.RescheduleImmediate ? this.minimumWait : worker.RunInterval, this.cancellationSource.Token);
                this.StartWork(this.workerFactories[workerFactoryIndex], workerFactoryIndex);
            }
            catch (OperationCanceledException)
            {
                worker.State = WorkerState.Stopped;
            }

            this.logger.MethodExit(nameof(WorkScheduler), nameof(this.CompleteOperationAndScheduleNext));
        }

        private async void StartWork(Func<WorkerBase> workerFactory, int workerFactoryIndex)
        {
            if (!this.disposed)
            {
                WorkerBase worker = workerFactory();
                //// var logicalOpFactory = new LogicalOperationFactory<PublisherIncomingServiceRequest, WorkOperationEvent>(worker.OperationName, worker.WorkItemName);
                //// var op = logicalOpFactory.StartLogicalDependencyOperation();
                var op = new OutgoingApiEventWrapper();
                var qosEvent = new WorkOperationEvent();
                op.DependencyOperationName = worker.OperationName;
                op.DependencyName = "WorkScheduler";
                op.Start();
                try
                {
                    DateTime startTime = DateTime.UtcNow;

                    // Check if we are ready to do work
                    worker.State = WorkerState.CheckingForWork;
                    WorkResult checkForWorkResult = await worker.CheckForWorkAsync(startTime, qosEvent, this.cancellationSource.Token);
                    if (checkForWorkResult.WorkReady)
                    {
                        // Do the work
                        worker.State = WorkerState.DoingWork;
                        Task<WorkResult> workTask = worker.DoWorkAsync(startTime, qosEvent, this.cancellationSource.Token);

                        // Schedule next run when the above task completes
                        // Intentionally does not await on the continuation task to complete
                        Task<Task> continuationTask = workTask.ContinueWith(
                            async t =>
                            {
                                try
                                {
                                    if (t.Result.Success)
                                    {
                                        await worker.MarkWorkCompletedAsync(startTime, qosEvent, this.cancellationSource.Token);
                                    }

                                    this.CompleteOperationAndScheduleNext(worker, op, t.Result, workerFactoryIndex, qosEvent);
                                }
                                catch (Exception ex)
                                {
                                    this.CompleteOperationAndScheduleNext(worker, op, WorkResult.Failed(ex), workerFactoryIndex, qosEvent);
                                }
                            });
                    }
                    else
                    {
                        this.CompleteOperationAndScheduleNext(worker, op, checkForWorkResult, workerFactoryIndex, qosEvent);
                    }
                }
                catch (OperationCanceledException tce)
                {
                    this.logger.Error(nameof(WorkScheduler), tce, "Worker task could not complete due to exception");

                    // If this happened because the scheduler was disposed, then just go to a stopped state and don't schedule a new run
                    if (this.disposed)
                    {
                        worker.State = WorkerState.Stopped;
                        this.CompleteOperationAndScheduleNext(worker, op, WorkResult.NoWork, workerFactoryIndex, qosEvent);
                    }
                    else
                    {
                        // If this exception happened when we are not disposed,
                        // then the implemention must have let an exception from a different cancel escape
                        qosEvent.LogException(tce, true);
                        this.CompleteOperationAndScheduleNext(worker, op, WorkResult.Failed(tce, false), workerFactoryIndex, qosEvent);
                    }
                }
                catch (Exception ex)
                {
                    this.logger.Error(nameof(WorkScheduler), ex, "Worker task could not complete due to unhandled exception");
                    this.CompleteOperationAndScheduleNext(worker, op, WorkResult.Failed(ex), workerFactoryIndex, qosEvent);
                }
            }
        }
    }
}

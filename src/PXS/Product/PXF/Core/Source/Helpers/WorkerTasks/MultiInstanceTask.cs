// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.PrivacyServices.Common.Telemetry;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     Provides the boilerplate code for a background task that runs the same code in a loop
    /// </summary>
    /// <typeparam name="TIConfig">type of configuration interface used by the derived class instance</typeparam>
    /// <remarks>
    ///     Provided functionality includes:
    ///     - Start and run forever / Stop a 'forever' run / Run once
    ///     - Spawning multiple instances of the same task
    ///     - looping logic to perform a single invocation, catch and log errors, and (optionally) wait a period of time before running
    ///        the next invocation
    ///     - Task instance context tracking and common logging methods that insert context information into trace messages automatically
    /// </remarks>
    public abstract class MultiInstanceTask<TIConfig> :
        IBackgroundTask
        where TIConfig : class, ITaskConfig
    {
        private const string ErrorsCounter = "Task Errors";

        private readonly ITelemetryLogger telemetryLogger;

        private readonly TimeSpan? delayOnException;

        private readonly ILogger traceLogger;

        private readonly object startStopLock = new object();

        private readonly string component;
        
        private volatile bool allowBackgroundTaskSpawn;

        private CancellationTokenSource canceler;

        private ICollection<Task> waiters;

        /// <summary>
        ///     Initializes a new instance of the MultiInstanceTask class
        /// </summary>
        /// <param name="config">task config</param>
        /// <param name="counterFactory">performance counter factory</param>
        /// <param name="telemetryLogger">telemetry logger</param>
        /// <param name="traceLogger">Geneva trace logger</param>
        protected MultiInstanceTask(
            TIConfig config,
            ICounterFactory counterFactory,
            ITelemetryLogger telemetryLogger,
            ILogger traceLogger)
        {
            this.CounterFactory = counterFactory ?? throw new ArgumentNullException(nameof(counterFactory));
            this.Config = config ?? throw new ArgumentNullException(nameof(config));

            this.traceLogger = traceLogger ?? throw new ArgumentNullException(nameof(traceLogger));

            // the case of this being null is explicitly handled below
            this.telemetryLogger = telemetryLogger;
            
            this.Context = new AsyncLocal<string>();

            this.component = this.GetType().Name;

            if (config.DelayOnExceptionMinutes > 0)
            {
                this.delayOnException = TimeSpan.FromMinutes(config.DelayOnExceptionMinutes);
            }
        }

        /// <summary>
        ///     Initializes a new instance of the MultiInstanceTask class
        /// </summary>
        /// <param name="config">task config</param>
        /// <param name="counterFactory">performance counter factory</param>
        /// <param name="traceLogger">Geneva trace logger</param>
        protected MultiInstanceTask(
            TIConfig config,
            ICounterFactory counterFactory,
            ILogger traceLogger) :
            this(config, counterFactory, null, traceLogger)
        {
        }

        /// <summary>
        ///     Gets the task error counter category
        /// </summary>
        protected virtual string TaskCounterCategory => null;

        /// <summary>
        ///     Gets the cancellation token for the task
        /// </summary>
        protected CancellationToken CancelToken => this.canceler.Token;

        /// <summary>
        ///     Gets the trace logger
        /// </summary>
        protected AsyncLocal<string> Context { get; }

        /// <summary>
        ///     Gets the trace logger
        /// </summary>
        protected ICounterFactory CounterFactory { get; }

        /// <summary>
        ///     Gets the config for the task
        /// </summary>
        protected TIConfig Config { get; }

        /// <summary>
        ///     Starts the set of tasks
        /// </summary>
        public void Start()
        {
            lock (this.startStopLock)
            {
                string traceCtxBase = this.Config.Tag + ".";
                string taskIdBase = Environment.MachineName + "." + this.Config.Tag + ".";

                if (this.waiters != null || this.canceler != null)
                {
                    throw new InvalidOperationException($"Task {this.Config.Tag} is already started");
                }

                this.canceler = new CancellationTokenSource();
                this.waiters = new List<Task>();

                if (this.Config.InstanceCount <= 0)
                {
                    this.TraceWarning("WARNING: Task is configured to have no instances running");
                    return;
                }

                // setup the global state & tasks so they are present and running before the workers
                //  startup
                this.SetupGlobalState();

                for (int i = 0; i < this.Config.InstanceCount; ++i)
                {
                    string traceCtx = traceCtxBase + i.ToStringInvariant();
                    string taskId = taskIdBase + Guid.NewGuid().ToString("N");
                    int index = i;

                    this.waiters.Add(Task.Run(() => this.SpawnWorkerAsync(taskId, traceCtx, false, index), CancellationToken.None));
                }

                try
                {
                    this.allowBackgroundTaskSpawn = true;
                    this.RegisterGlobalBackgroundTasks();
                }
                finally
                {
                    this.allowBackgroundTaskSpawn = false;
                }
            }
        }

        /// <summary>
        ///     Stops the set of tasks
        /// </summary>
        /// <returns>resulting value</returns>
        public async Task StopAsync()
        {
            IEnumerable<Task> tasksLocal;

            lock (this.startStopLock)
            {
                this.canceler?.Cancel();
                tasksLocal = this.waiters ?? throw new InvalidOperationException($"Task {this.Config.Tag} is already stopped");
            }
            
            await Task.WhenAll(tasksLocal).ConfigureAwait(false);

            lock (this.startStopLock)
            {
                this.canceler?.Dispose();
                this.canceler = null;
                this.waiters = null;
            }
        }

        /// <summary>
        ///     Starts a single instance and runs one operation
        /// </summary>
        /// <param name="taskId">task id</param>
        /// <param name="traceContext">trace context</param>
        /// <returns>resulting value</returns>
        /// <remarks>StopAsync must be called to reset state and allow this to be called a second time</remarks>
        public Task RunSingleInstanceOnePassAsync(
            string taskId,
            string traceContext)
        {
            lock (this.startStopLock)
            {
                if (this.waiters != null)
                {
                    throw new InvalidOperationException($"Task {this.Config.Tag} is already started");
                }

                this.canceler = new CancellationTokenSource();
                this.waiters = new List<Task>();

                this.SetupGlobalState();

                this.waiters.Add(Task.Run(() => this.SpawnWorkerAsync(taskId, traceContext, true, 0)));

                try
                {
                    this.allowBackgroundTaskSpawn = true;
                    this.RegisterGlobalBackgroundTasks();
                }
                finally
                {
                    this.allowBackgroundTaskSpawn = false;
                }
            }

            return Task.WhenAll(this.waiters);
        }

        /// <summary>
        ///     Performs a single task operation
        /// </summary>
        /// <param name="ctx">operation context</param>
        /// <returns>resulting value</returns>
        /// <remarks>
        ///     'a single operation' is highly task dependent. It could mean processing a single queue item or enumerating and 
        ///      processing a directory of files
        /// </remarks>
        protected abstract Task<TimeSpan?> RunOnceAsync(OperationContext ctx);

        /// <summary>
        ///     Sets up global state
        /// </summary>
        /// <returns>resulting value</returns>
        /// <remarks>
        ///     this method is called before any instances of the primary task have started
        ///     this method should not spawn any background tasks- the RegisterGlobalBackgroundTasks() callback must be used for that
        /// </remarks>
        protected virtual void SetupGlobalState()
        {
        }

        /// <summary>
        ///     Starts the global background tasks
        /// </summary>
        /// <returns>resulting value</returns>
        /// <remarks>
        ///     this method is called after all instances of the primary task have started
        ///     this method should not setup global state- the SetupGlobalState() callback must be used for that
        /// </remarks>
        protected virtual void RegisterGlobalBackgroundTasks()
        {
        }

        /// <summary>
        ///     Emits an error trace
        /// </summary>
        /// <param name="format">format string</param>
        /// <param name="args">replacement arguments</param>
        protected void TraceError(
            string format,
            params object[] args)
        {
            this.traceLogger.Error(this.component, this.MakeTraceMessage(format, args));
        }

        /// <summary>
        ///     Emits a error trace
        /// </summary>
        /// <param name="format">format string</param>
        /// <param name="args">replacement arguments</param>
        protected void TraceWarning(
            string format,
            params object[] args)
        {
            this.traceLogger.Warning(this.component, this.MakeTraceMessage(format, args));
        }

        /// <summary>
        ///     Emits an information trace
        /// </summary>
        /// <param name="format">format string</param>
        /// <param name="args">replacement arguments</param>
        protected void TraceInfo(
            string format,
            params object[] args)
        {
            this.traceLogger.Information(this.component, this.MakeTraceMessage(format, args));
        }

        /// <summary>
        ///     Emits a warning trace
        /// </summary>
        /// <param name="format">format string</param>
        /// <param name="args">replacement arguments</param>
        protected void TraceVerbose(
            string format,
            params object[] args)
        {
            this.traceLogger.Verbose(this.component, this.MakeTraceMessage(format, args));
        }

        /// <summary>
        ///     Emits an error event to the telemetry stream
        /// </summary>
        /// <typeparam name="T">event type derived from MultiInstanceTaskEvent</typeparam>
        /// <param name="ctx">operation context</param>
        /// <param name="event">event to log</param>
        protected void LogEventError<T>(
            OperationContext ctx,
            T @event)
            where T : TaskTelemetryEvent
        {
            this.telemetryLogger?.LogError(this.PopulateEventBaseInfo(ctx, @event));
        }

        /// <summary>
        ///     Emits a warning event to the telemetry stream
        /// </summary>
        /// <typeparam name="T">event type derived from MultiInstanceTaskEvent</typeparam>
        /// <param name="ctx">operation context</param>
        /// <param name="event">event to log</param>
        protected void LogEventWarning<T>(
            OperationContext ctx,
            T @event)
            where T : TaskTelemetryEvent
        {
            this.telemetryLogger?.LogWarning(this.PopulateEventBaseInfo(ctx, @event));
        }

        /// <summary>
        ///     Emits an informational event to the telemetry stream
        /// </summary>
        /// <typeparam name="T">event type derived from MultiInstanceTaskEvent</typeparam>
        /// <param name="ctx">operation context</param>
        /// <param name="event">event to log</param>
        protected void LogEventInfo<T>(
            OperationContext ctx,
            T @event)
            where T : TaskTelemetryEvent
        {
            this.telemetryLogger?.LogInfo(this.PopulateEventBaseInfo(ctx, @event));
        }

        /// <summary>
        ///      Used by derived classes to start up task-specific background tasks for monitoring by this class
        /// </summary>
        /// <param name="taskFunc">task function</param>
        /// <param name="taskId">task id</param>
        /// <param name="taskTag">trace context</param>
        /// <returns>resulting value</returns>
        protected void SpawnGlobalTask(
            Func<OperationContextBasic, Task<TimeSpan?>> taskFunc,
            string taskId,
            string taskTag)
        {
            if (this.allowBackgroundTaskSpawn == false)
            {
                throw new InvalidOperationException(
                    "Can only start global tasks while RegisterGlobalBackgroundTasks() is being called");
            }

            this.waiters.Add(Task.Run(() => this.RunGlobalTaskAsync(taskFunc, taskId, taskTag), CancellationToken.None));
        }

        /// <summary>
        ///     Constructs a trace message
        /// </summary>
        /// <param name="format">format</param>
        /// <param name="args">arguments</param>
        /// <returns>resulting value</returns>
        private string MakeTraceMessage(
            string format,
            params object[] args)
        {
            string message = format;

            if (args.Length > 0)
            {
                try
                {
                    message = string.Format(format, args);
                }
                catch (FormatException)
                {
                    message = "INVALID FORMAT: [" + format + "]";
                }
            }

            return "[Ctx:" + this.Context.Value + "] " + message;
        }

        /// <summary>
        ///     Populates the base information of the event if it is not yet populated
        /// </summary>
        /// <typeparam name="T">event type derived from MultiInstanceTaskEvent</typeparam>
        /// <param name="ctx">operation context</param>
        /// <param name="event">event to populate</param>
        /// <returns>populated event oject</returns>
        private T PopulateEventBaseInfo<T>(
            OperationContext ctx,
            T @event)
            where T : TaskTelemetryEvent
        {
            if (string.IsNullOrWhiteSpace(@event.TaskId))
            {
                @event.TaskId = $"{ctx.TaskId}[{ctx.WorkerIndex}]";
            }

            if (string.IsNullOrWhiteSpace(@event.Operation))
            {
                @event.Operation = ctx.Op;
            }

            if (string.IsNullOrWhiteSpace(@event.Item))
            {
                @event.Item = ctx.Item;
            }

            return @event;
        }

        /// <summary>
        ///      Starts up the set of tasks used by the task to execute work
        /// </summary>
        /// <param name="taskId">task id</param>
        /// <param name="traceContext">trace context</param>
        /// <param name="singlePass">true to execute a single pass of the worker only; false to run continuously</param>
        /// <param name="workerIndex">worker index</param>
        /// <returns>resulting value</returns>
        private async Task SpawnWorkerAsync(
            string taskId,
            string traceContext,
            bool singlePass,
            int workerIndex)
        {
            this.Context.Value = traceContext;

            this.TraceVerbose("Starting worker task");

            try
            {
                OperationContext ctx = new OperationContext(taskId, workerIndex);
                TimeSpan? nextPassDelay = null;

                while (this.CancelToken.IsCancellationRequested == false)
                {
                    ctx.Item = string.Empty;
                    ctx.Op = string.Empty;

                    try
                    {
                        if (singlePass == false &&
                            nextPassDelay > TimeSpan.Zero &&
                            this.canceler.IsCancellationRequested == false)
                        {
                            await Task.Delay(nextPassDelay.Value, this.CancelToken).ConfigureAwait(false);
                        }

                        nextPassDelay = await this.RunOnceAsync(ctx).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // no need to do anything as this will get handled by the while loop condition check
                    }
                    catch (Exception e)
                    {
                        TaskTelemetryException telemetryEx = e as TaskTelemetryException;
                        TaskTelemetryEvent @event = telemetryEx?.Event;
                        bool isFatal = MultiInstanceTask<TIConfig>.IsFatalError(e);
                        string prefix = isFatal ? "Fatal error" : "Error";

                        this.TraceError($"{prefix} processing work item {ctx.Item} while {ctx.Op}: {e}");

                        if (string.IsNullOrWhiteSpace(this.TaskCounterCategory) == false)
                        {
                             ICounter counter = this.CounterFactory.GetCounter(
                                this.TaskCounterCategory, MultiInstanceTask<TIConfig>.ErrorsCounter, CounterType.Rate);

                            counter?.Increment();
                            counter?.Increment(this.Config.Tag);
                        }

                        if (@event == null)
                        {
                            @event = new TaskTelemetryEvent { Details = e.GetMessageAndInnerMessages() };
                        }

                        this.LogEventError(ctx, @event);

                        // we'll eat the exception in the looping or non-fatal case, but for the single run case, just throw it
                        if (singlePass || isFatal)
                        {
                            this.canceler.Cancel();
                            throw;
                        }

                        nextPassDelay = this.delayOnException;
                    }

                    // if we only want to make one pass (largely for testing purposes), then immediately trigger the
                    //  cancellation token once the first pass is done. This will then exit the while loop.
                    if (singlePass)
                    {
                        this.canceler.Cancel();
                    }
                }
            }
            finally
            {
                this.TraceVerbose("Terminating worker task");
            }
        }

        /// <summary>
        ///      Starts up the set of tasks used by the task to execute work
        /// </summary>
        /// <param name="taskFunc">task function</param>
        /// <param name="taskId">task id</param>
        /// <param name="taskTag">trace context</param>
        /// <returns>resulting value</returns>
        private async Task RunGlobalTaskAsync(
            Func<OperationContextBasic, Task<TimeSpan?>> taskFunc,
            string taskId,
            string taskTag)
        {
            this.Context.Value = taskTag;

            this.TraceVerbose("Starting global task");

            try
            {
                OperationContextBasic ctx = new OperationContextBasic(taskId);
                TimeSpan? nextPassDelay = null;

                while (this.CancelToken.IsCancellationRequested == false)
                {
                    ctx.Op = string.Empty;

                    try
                    {
                        if (nextPassDelay > TimeSpan.Zero && this.canceler.IsCancellationRequested == false)
                        {
                            await Task.Delay(nextPassDelay.Value, this.CancelToken).ConfigureAwait(false);
                        }

                        nextPassDelay = await taskFunc(ctx);
                    }
                    catch (OperationCanceledException)
                    {
                        // no need to do anything as this will get handled by the while loop condition check
                    }
                    catch (Exception e)
                    {
                        bool isFatal = MultiInstanceTask<TIConfig>.IsFatalError(e);
                        string prefix = isFatal ? "Fatal error" : "Error";

                        this.TraceError($"{prefix} running global task {taskTag} while {ctx.Op}: {e}");

                        if (string.IsNullOrWhiteSpace(this.TaskCounterCategory) == false)
                        {
                            ICounter counter = this.CounterFactory.GetCounter(
                                this.TaskCounterCategory, MultiInstanceTask<TIConfig>.ErrorsCounter, CounterType.Rate);

                            counter.Increment();
                            counter.Increment(taskTag);
                        }

                        if (isFatal)
                        {
                            throw;
                        }

                        nextPassDelay = this.delayOnException;
                    }
                }
            }
            finally
            {
                this.TraceVerbose("Terminating global task");
            }
        }

        /// <summary>
        ///      determines whether the specified exception should be considered fatal to the process
        /// </summary>
        /// <param name="e">exception to check</param>
        /// <returns>true if fatal; false otherwise</returns>
        private static bool IsFatalError(Exception e)
        {
            return
                e is OutOfMemoryException ||
                e is StackOverflowException ||
                e is NullReferenceException ||
                e is SEHException ||
                e is AccessViolationException ||
                e is ThreadAbortException;
        }
    }
}

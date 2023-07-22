// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem.Cosmos;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Locks;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Data;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility.ProgressTracker;
    using Microsoft.PrivacyServices.Common.CosmosExport.Telemetry;
    using Microsoft.Azure.Storage;

    using OperationContext = Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.OperationContext;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     processes a single file and splits it into individual streams
    /// </summary>
    public class FileProcessor : TrackCountersBaseTask<IFileProcessorConfig>
    {
        private static readonly Task<TimeSpan?> CounterUpdateFreq = Task.FromResult<TimeSpan?>(TimeSpan.FromSeconds(30));

        private static readonly TimeSpan AssumeTransientDefault = TimeSpan.FromDays(3);
        private static readonly TimeSpan StatsWriterFlushFreq = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan StatsWriterPeriod = TimeSpan.FromHours(1);
        private static readonly TimeSpan EmptyQueuePause = TimeSpan.FromSeconds(15);

        private const string DefaultProductId = "0";

        private const string DataRowBufferWriteCounter = "Row Write With Buffering Time (ms)";
        private const string DataRowErrorWriteCounter = "Row Write With Failure Time (ms)";
        private const string DataRowFlushWriteCounter = "Row Write With Flush Time (ms)";
        private const string DataRowReadCounter = "Row Read Time (ms)";
        
        private const string DataWriterFlushErrorCounter = "Writer Flush With Error Time (ms)";
        private const string DataWriterCreateCounter = "Writer Create Time (ms)";
        private const string DataWriterFlushCounter = "Writer Flush Time (ms)";

        private const string TrackerGlobalStatsUpdateCounter = "Tracker And Global Stats Update (ms)";
        private const string CommandStatsUpdateCounter = "Command Stats Update (ms)";

        private const string DataWritersCounter = "Active Data Writers";
        private const string InProgressCounter = "Data Files In Progress";
        private const string BytePerSecCounter = "Data File Bytes Per Second";
        private const string RowPerSecCounter = "Data File Rows Per Second";

        private const string DataReadAllCounter = "Data File Read Time (seconds)";
        private const string ProcessingCounter = "Data File Total Time (seconds)";
        private const string CompletedCounter = "Data Files Completed";

        private const string StatsFileTimeFormat = "_yyyy_MM_dd_HH";
        private const string DataFileStatsFile = "FileStats";
        private const string CommandStatsFile = "CommandStats";

        private readonly IPartitionedQueue<PendingDataFile, FileSizePartition> pendingQueue;
        private readonly IFileProgressTrackerFactory trackerFactory;
        private readonly IPeriodicFileWriterFactory periodicWriterFactory;
        private readonly ICommandDataWriterFactory writerFactory;
        private readonly IQueue<CompleteDataFile> doneQueue;
        private readonly ITable<CommandFileState> commandFileState;
        private readonly IFileSystemManager fileSystemManager;
        private readonly ILockManager lockMgr;
        private readonly TimeSpan assumeTransientThresold;
        private readonly TimeSpan delayOnIncomplete;
        private readonly TimeSpan leaseRenewFreq;
        private readonly TimeSpan holdingExpiry;
        private readonly TimeSpan leaseTime;
        private readonly IClock clock;
        private readonly string holdingPath;

        private IReadOnlyList<FileSizePartition>[] taskSizePartitions;
        private IPeriodicFileWriter dataFileStatsWriter;
        private IPeriodicFileWriter commandStatsWriter;
        private PeriodicCounters[] taskCounters;

        private enum ProcessingResult
        {
            Incomplete,
            NeedsProcessing,
            CompleteProcessed,
            CompleteQueued,
        }

        /// <summary>
        ///     Initializes a new instance of the FileProcessor class
        /// </summary>
        /// <param name="trackerFactory">file progress tracker factory</param>
        /// <param name="periodicWriterFactory">periodic writer factory</param>
        /// <param name="writerFactory">writer factory</param>
        /// <param name="doneQueue">complete file queue</param>
        /// <param name="commandFileState">command file state</param>
        /// <param name="pendingQueue">pending file queue</param>
        /// <param name="config">configuration</param>
        /// <param name="fileSystemManager">file system manager</param>
        /// <param name="telemetryLogger">telemetry logger</param>
        /// <param name="counterFactory">perf counter factory</param>
        /// <param name="lockManager">lock manager</param>
        /// <param name="traceLogger">trace logger</param>
        /// <param name="clock">clock</param>
        /// <param name="appConfig">appConfig to read lease multiple</param>
        public FileProcessor(
            IFileProgressTrackerFactory trackerFactory,
            IPeriodicFileWriterFactory periodicWriterFactory,
            ICommandDataWriterFactory writerFactory,
            IQueue<CompleteDataFile> doneQueue,
            ITable<CommandFileState> commandFileState,
            IPartitionedQueue<PendingDataFile, FileSizePartition> pendingQueue,
            IFileProcessorConfig config,
            IFileSystemManager fileSystemManager,
            ITelemetryLogger telemetryLogger,
            ICounterFactory counterFactory,
            ILockManager lockManager,
            ILogger traceLogger,
            IClock clock,
            IAppConfiguration appConfig) :
            base(config, counterFactory, telemetryLogger, traceLogger)
        {
            this.periodicWriterFactory = periodicWriterFactory ?? throw new ArgumentNullException(nameof(periodicWriterFactory));

            this.fileSystemManager = fileSystemManager ?? throw new ArgumentNullException(nameof(fileSystemManager));
            this.commandFileState = commandFileState ?? throw new ArgumentNullException(nameof(commandFileState));
            this.trackerFactory = trackerFactory ?? throw new ArgumentNullException(nameof(trackerFactory));
            this.writerFactory = writerFactory ?? throw new ArgumentNullException(nameof(writerFactory));
            this.pendingQueue = pendingQueue ?? throw new ArgumentNullException(nameof(pendingQueue));
            this.doneQueue = doneQueue ?? throw new ArgumentNullException(nameof(doneQueue));
            this.lockMgr = lockManager ?? throw new ArgumentNullException(nameof(lockManager));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));

            this.delayOnIncomplete = TimeSpan.FromMinutes(config.DelayIfCouldNotCompleteMinutes);
            this.leaseRenewFreq = TimeSpan.FromMinutes(config.MinimumRenewMinutes);
            this.holdingExpiry = TimeSpan.FromHours(fileSystemManager.CosmosPathsAndExpiryTimes.HoldingExpiryHours);
            this.leaseTime = TimeSpan.FromMinutes(Math.Max(1, appConfig.GetConfigValue<int>(ConfigNames.PXS.CosmosWorkerWorkItemLeaseTimeMultiple, 1)) * config.LeaseMinutes);

            this.holdingPath =
                Utility.EnsureHasTrailingSlashButNoLeadingSlash(fileSystemManager.CosmosPathsAndExpiryTimes.PostProcessHolding);

            this.assumeTransientThresold = string.IsNullOrWhiteSpace(config.AssumeNonTransientThreshold) ?
                FileProcessor.AssumeTransientDefault :
                TimeSpan.Parse(config.AssumeNonTransientThreshold);

            this.TraceVerbose($"Lease time in minutes = {this.leaseTime.TotalMinutes}");
        }

        /// <summary>
        ///     Starts the global background tasks
        /// </summary>
        /// <remarks>
        ///     this method is called before any instances of the primary task have started
        ///     this method should not spawn any background tasks- the RegisterGlobalBackgroundTasks() callback must be used for that
        /// </remarks>
        protected override void SetupGlobalState()
        {
            this.taskSizePartitions = new IReadOnlyList<FileSizePartition>[this.Config.InstanceCount];
            this.taskCounters = new PeriodicCounters[this.Config.InstanceCount];

            if (this.Config.InstanceCount == 0)
            {
                return;
            }

            // instance count of 1 would generally be used locally for testing, but in that case, allow the one queue to process
            //  everything, starting from smallest to largest
            if (this.Config.InstanceCount == 1)
            {
                this.taskSizePartitions = new IReadOnlyList<FileSizePartition>[1];
                this.taskSizePartitions[0] = 
                    new[] 
                    {
                        FileSizePartition.Empty,
                        FileSizePartition.Small,
                        FileSizePartition.Medium,
                        FileSizePartition.Large,
                        FileSizePartition.Oversize
                    };
            }
            else
            {
                IReadOnlyList<FileSizePartition> allowedSet;
                int thresholdOversize = this.Config.EmptyFileInstances;
                int thresholdLarge = thresholdOversize + this.Config.OversizedFileInstances;
                int thresholdMedium = thresholdLarge + this.Config.LargeFileInstances;
                int thresholdSmall = thresholdMedium + this.Config.MediumFileInstances;

                allowedSet = new ReadOnlyCollection<FileSizePartition>(new[] { FileSizePartition.Empty });

                for (int i = 0; i < thresholdOversize; ++i)
                {
                    this.taskSizePartitions[i] = allowedSet;
                }

                allowedSet = new ReadOnlyCollection<FileSizePartition>(
                    new[] { FileSizePartition.Oversize, FileSizePartition.Large, FileSizePartition.Medium, FileSizePartition.Small });

                for (int i = thresholdOversize; i < thresholdLarge; ++i)
                {
                    this.taskSizePartitions[i] = allowedSet;
                }

                allowedSet = new ReadOnlyCollection<FileSizePartition>(
                    new[] { FileSizePartition.Medium, FileSizePartition.Small, FileSizePartition.Large });

                for (int i = thresholdLarge; i < thresholdMedium; ++i)
                {
                    this.taskSizePartitions[i] = allowedSet;
                }

                allowedSet = new ReadOnlyCollection<FileSizePartition>(new[] { FileSizePartition.Medium, FileSizePartition.Small });

                for (int i = thresholdMedium; i < thresholdSmall; ++i)
                {
                    this.taskSizePartitions[i] = allowedSet;
                }

                allowedSet = new ReadOnlyCollection<FileSizePartition>(new[] { FileSizePartition.Small });

                for (int i = thresholdSmall; i < this.Config.InstanceCount; ++i)
                {
                    this.taskSizePartitions[i] = allowedSet;
                }
            }

            this.dataFileStatsWriter = this.periodicWriterFactory.Create(
                this.fileSystemManager.StatsLog,
                null,
                d => FileProcessor.DataFileStatsFile + d.ToString(FileProcessor.StatsFileTimeFormat) + ".tsv",
                FileProcessor.StatsWriterPeriod);

            this.commandStatsWriter = this.periodicWriterFactory.Create(
                this.fileSystemManager.StatsLog,
                null,
                d => FileProcessor.CommandStatsFile + d.ToString(FileProcessor.StatsFileTimeFormat) + ".tsv",
                FileProcessor.StatsWriterPeriod);
        }

        /// <summary>
        ///     Starts the global background tasks
        /// </summary>
        /// <remarks>
        ///     this method is called after all instances of the primary task have started
        ///     this method should not setup global state- the SetupGlobalState() callback must be used for that
        /// </remarks>
        protected override void RegisterGlobalBackgroundTasks()
        {
            const string TagAndIdStatsFlush = "FileProcessor.FlushStatsFile";
            const string TagAndIdCounters = "FileProcessor.UpdatePeriodicCounters";

            this.SpawnGlobalTask(this.UpdatePeriodicCounters, TagAndIdCounters, TagAndIdCounters);
            this.SpawnGlobalTask(this.FlushStatsFiles, TagAndIdStatsFlush, TagAndIdStatsFlush);
        }

        /// <summary>
        ///     Runs the task
        /// </summary>
        /// <param name="ctx">operation context</param>
        /// <returns>resulting value</returns>
        protected override async Task<TimeSpan?> RunOnceAsync(OperationContext ctx)
        {
            PartitionedQueueItem<PendingDataFile, FileSizePartition> fullItem;
            IQueueItem<PendingDataFile> item;
            PendingDataFile data;
            bool isComplete = false;

            // acquire a queue workItem

            this.CancelToken.ThrowIfCancellationRequested();

            ctx.Item = "none";
            ctx.Op = "dequeueing item";

            fullItem = await this.pendingQueue
                .DequeueAsync(
                    this.taskSizePartitions[ctx.WorkerIndex],
                    this.leaseTime, 
                    Constants.DequeueTimeout, 
                    null, 
                    this.CancelToken)
                .ConfigureAwait(false);

            item = fullItem?.Item;
            data = item?.Data;

            // no work to perform
            if (data == null)
            {
                // wait 10 seconds before trying to dequeue again
                return FileProcessor.EmptyQueuePause;
            }

            ctx.Item = "[dataFile: " + data.DataFileTag + "]";

            this.TraceVerbose($"File processor dequeued pending file {ctx.Item} for processing");

            // if we process this item without success too many times, abandon it and wait for the manifest processor to re-enqueue 
            //  it later. This will keep us from processing the same items over and over and starving other items
            if (item.DequeueCount > this.Config.MaxDequeueCount)
            {
                const string Fmt =
                    "File processor dequeued pending file {0} for processing, but its previous dequeue count of {1} exceeds the " +
                    "max allowed dequeue count of {2}. Abandoning and waiting for it to be enqueued again by the manifest processor";

                this.TraceWarning(Fmt, ctx.Item, item.DequeueCount, this.Config.MaxDequeueCount);

                await item.CompleteAsync();
                return null;
            }
            
            try
            {
                IFileProgressTracker tracker;
                ILockLease lease;
                string agentId;

                this.CancelToken.ThrowIfCancellationRequested();

                ctx.Op = "acquring data file lease";

                lease = await this.lockMgr.AttemptAcquireAsync(
                    data.AgentId,
                    data.DataFilePath,
                    ctx.TaskId,
                    this.leaseTime,
                    false).ConfigureAwait(false);
                if (lease == null)
                {
                    // someone else owns it; keeping calm and carrying on.
                    this.TraceVerbose("File processor failed to acquire lease for item " + ctx.Item + ". Abandoning");
                    return null;
                }

                this.taskCounters[ctx.WorkerIndex].SetInProgress(fullItem.PartitionId);

                tracker = this.trackerFactory.Create(
                    data.AgentId,
                    CosmosFileSystemUtility.SplitNameAndPath(data.DataFilePath).Name,
                    this.TraceError);

                this.TraceInfo($"File processor processing pending {fullItem.PartitionId} file {ctx.Item}");

                agentId = data.AgentId;
                
                try
                {
                    FileSizePartition newPartition;
                    ProcessingResult result;
                    ILeaseRenewer renewer;
                    Stopwatch timer = new Stopwatch();

                    timer.Start();

                    renewer = new LeaseRenewer(
                        new List<Func<Task<bool>>>
                        {
                            () => lease.RenewAsync(this.leaseTime),
                            () => item.RenewLeaseAsync(this.leaseTime)
                        },
                        this.leaseRenewFreq,
                        this.clock,
                        s => this.TraceError(s),
                        ctx.Item);

                    (result, newPartition) = await this
                        .ProcessRequestAsync(tracker, ctx, renewer, fullItem)
                        .ConfigureAwait(false);
                    
                    if (result == ProcessingResult.CompleteProcessed)
                    {
                        ICounter counter;
                        ulong elapsed;
                            
                        elapsed = (ulong)Math.Round(
                            TimeSpan.FromTicks(timer.ElapsedTicks).TotalSeconds, MidpointRounding.AwayFromZero);

                        counter = this.GetCounter(FileProcessor.ProcessingCounter);
                        counter.SetValue(elapsed);
                        counter.SetValue(elapsed, fullItem.PartitionId.ToString());

                        counter = this.GetCounter(FileProcessor.CompletedCounter);
                        counter.Increment();
                        counter.Increment(fullItem.PartitionId.ToString());

                        this.TraceInfo($"File processor completed processing {fullItem.PartitionId} pending file {ctx.Item}");

                        isComplete = true;
                    }
                    else if (result == ProcessingResult.CompleteQueued)
                    {
                        this.TraceInfo(
                            $"Pending empty pending file {ctx.Item} found to be non-empty and queued to {newPartition} queue");

                        isComplete = true;
                    }
                    else if (result == ProcessingResult.Incomplete)
                    {
                        this.TraceInfo(
                            $"Pending empty  file {ctx.Item} found to be non-empty but cosmos is still reporting empty. Will retry.");
                    }
                }
                catch (Exception e)
                {
                    tracker.AddMessage(
                        TrackerTypes.GeneralError,
                        $"Unexpected error ({e.Message ?? string.Empty}) occurred during processing. Will retry later.");

                    if (e is ChunkedReadException exSpecific && exSpecific.ErrorCode == ChunkedReadErrorCode.EarlyStreamEnd)
                    {
                        this.LogEventError(
                            ctx,
                            new CosmosReturnedEarlyEmptyStreamEvent
                            {
                                AgentId = agentId,
                                Details = e.Message,

                                ReadOffset = exSpecific.RequestOffset,
                                ReadSize = exSpecific.RequestedSize,

                                FileName = exSpecific.FileName,
                                FileSize = exSpecific.FileSize,
                            });
                    }

                    throw;
                }
                finally
                {
                    ctx.PushOp();
                    ctx.Op = "release data file lease";

                    this.taskCounters[ctx.WorkerIndex].Reset();

                    await lease.ReleaseAsync(isComplete).ConfigureAwait(false);

                    ctx.Op = "persisting progress tracking data";

                    await tracker.PersistAsync();

                    ctx.PopOp();
                }
            }
            finally
            {
                ctx.PushOp();
                ctx.Op = "release queue item";

                // a release is basically a renew with a short time span.  In this case, if we don't complete the 
                //  command, we want to wait a few minuntes before trying again to ensure that we give any 
                //  transient issues time to resolve (such as a missing command feed command)
                await (isComplete ? item.CompleteAsync() : item.RenewLeaseAsync(this.delayOnIncomplete)).ConfigureAwait(false);

                ctx.PopOp();
            }

            return null;
        }

        /// <summary>
        ///     Updates a set of counters periodically
        /// </summary>
        /// <param name="ctx">work item context</param>
        /// <returns>a task whose result is the amount of time to wait before running the method again</returns>
        private Task<TimeSpan?> UpdatePeriodicCounters(OperationContextBasic ctx)
        {
            IDictionary<FileSizePartition, PartitionCounterData> partitionData;

            ICounter counterCount;
            ICounter counterRps;
            ICounter counterBps;

            ulong totalSecs;
            ulong totalTicks;
            ulong totalBytes;
            ulong totalRows;
            uint totalCount;

            uint deadLetter = 0;
            uint abandoned = 0;
            uint real = 0;

            partitionData = new Dictionary<FileSizePartition, PartitionCounterData>
            {
                { FileSizePartition.Empty, new PartitionCounterData() },
                { FileSizePartition.Small, new PartitionCounterData() },
                { FileSizePartition.Medium, new PartitionCounterData() },
                { FileSizePartition.Large, new PartitionCounterData() },
                { FileSizePartition.Oversize, new PartitionCounterData() },
            };

            for (int i = 0; i < this.taskCounters.Length; ++i)
            {
                this.taskCounters[i].AddCounts(partitionData, ref deadLetter, ref abandoned, ref real);
            }

            counterCount = this.GetCounter(FileProcessor.DataWritersCounter);
            counterCount.SetValue(real + deadLetter + abandoned);
            counterCount.SetValue(deadLetter, "DeadLetter");
            counterCount.SetValue(abandoned, "AbandonedData");
            counterCount.SetValue(real, "BlobStore");

            // there is no Sum() for ulongs :-(
            totalCount = partitionData.Values.Select(o => o.Count).Aggregate<uint, uint>(0, (current, v) => current + v);
            totalTicks = partitionData.Values.Select(o => o.Ticks).Aggregate<ulong, ulong>(0, (current, v) => current + v);
            totalSecs = Convert.ToUInt64(TimeSpan.FromTicks(Convert.ToInt64(totalTicks)).TotalSeconds);
            totalBytes = partitionData.Values.Select(o => o.Bytes).Aggregate<ulong, ulong>(0, (current, v) => current + v);
            totalRows = partitionData.Values.Select(o => o.Rows).Aggregate<ulong, ulong>(0, (current, v) => current + v);

            counterCount = this.GetCounter(FileProcessor.InProgressCounter);
            counterBps = this.GetCounter(FileProcessor.BytePerSecCounter);
            counterRps = this.GetCounter(FileProcessor.RowPerSecCounter);

            counterCount.SetValue(totalCount);
            counterBps.SetValue(totalSecs > 0 ? Convert.ToUInt64(totalBytes / totalSecs) : 0);
            counterRps.SetValue(totalSecs > 0 ? Convert.ToUInt64(totalRows / totalSecs) : 0);

            foreach (KeyValuePair<FileSizePartition, PartitionCounterData> kvp in partitionData)
            {
                PartitionCounterData val = kvp.Value;
                string partition = kvp.Key.ToString();
                ulong secs = Convert.ToUInt64(TimeSpan.FromTicks(Convert.ToInt64(val.Ticks)).TotalSeconds);

                counterCount.SetValue(val.Count, partition);

                // no point in emitting bps & rps for the empty file category
                if (kvp.Key != FileSizePartition.Empty)
                {
                    counterBps.SetValue(secs > 0 ? Convert.ToUInt64(val.Bytes / secs) : 0);
                    counterRps.SetValue(secs > 0 ? Convert.ToUInt64(val.Rows / secs) : 0);
                }
            }

            return FileProcessor.CounterUpdateFreq;
        }

        /// <summary>
        ///     Updates a set of counters periodically
        /// </summary>
        /// <param name="ctx">work item context</param>
        /// <returns>a task whose result is the amount of time to wait before running the method again</returns>
        private async Task<TimeSpan?> FlushStatsFiles(OperationContextBasic ctx)
        {
            await Task
                .WhenAll(
                    this.dataFileStatsWriter.FlushQueueAsync(CancellationToken.None),
                    this.commandStatsWriter.FlushQueueAsync(CancellationToken.None))
                .ConfigureAwait(false);

            return FileProcessor.StatsWriterFlushFreq;
        }

        /// <summary>
        /// Processes a single data row
        /// </summary>
        /// <param name="ctx">operation context</param>
        /// <param name="counters">perf counters</param>
        /// <param name="writers">writers collecton</param>
        /// <param name="workItem">work item</param>
        /// <param name="renewer">lease renewer</param>
        /// <param name="commandId">command id</param>
        /// <param name="productId">product id</param>
        /// <param name="data">data to write</param>
        /// <param name="row">row number</param>
        /// <param name="assumeNonTransient">
        ///     true to assume that possibly transient errors are non-transient; false to treat them as transient
        /// </param>
        /// <returns>number of bytes added to or subtracted from the writer's pending byte count</returns>
        private async Task<long> ProcessSingleRow(
            OperationContext ctx,
            CounterSet counters,
            IDictionary<string, ICommandDataWriter> writers,
            PendingDataFile workItem,
            ILeaseRenewer renewer,
            string commandId,
            string productId,
            string data,
            ulong row,
            bool assumeNonTransient)
        {
            ICommandDataWriter writer;
            Stopwatch timer = new Stopwatch();
            long currentPending = 0;

            if (writers.TryGetValue(commandId, out writer) == false)
            {
                string description;

                ctx.Op = "creating writer factory";

                timer.Start();

                writer = await this.writerFactory
                    .CreateAsync(this.CancelToken, workItem.AgentId, commandId, workItem.ExportFileName)
                    .ConfigureAwait(false);

                writer.TransientFailureMode = assumeNonTransient ? 
                    TransientFailureMode.AssumeNonTransient :
                    TransientFailureMode.AssumeTransient;
                
                writers.Add(commandId, writer);

                description = WriterDescriptionGenerator.GenerateWriterDescription(writer);

                counters.DataWriterCreate.SetValue(Convert.ToUInt64(timer.ElapsedMilliseconds));

                this.TraceVerbose($"Opening file writer ({description}) for {commandId} and data file {ctx.Item} at row {row}");
            }

            // if we have a null writer, then a previous attempt to write data failed with a non-transient error, so just discard
            //  the data
            if (writer == null)
            {
                return 0;
            }

            ctx.Op = "sending row to writer";
            
            try
            {
                Task<long> writerTask;
                long result;

                currentPending = writer.PendingSize;

                timer.Restart();

                writerTask = writer.WriteAsync(productId, data, this.Config.CommandPendingByteThreshold);

                await Task.WhenAll(writerTask, renewer.RenewAsync()).ConfigureAwait(false);

                result = writerTask.Result;

                (result <= 0 ? counters.DataRowFlushWrite : counters.DataRowBufferWrite)
                    .SetValue(Convert.ToUInt64(timer.ElapsedMilliseconds));

                return result;
            }
            catch (NonTransientStorageException e)
            {
                counters.DataRowErrorWrite.SetValue(Convert.ToUInt64(timer.ElapsedMilliseconds));

                this.TraceWarning(
                    "Non-transient storage exception {0} writing for for {1} and data file {2}. Abandoning command data: {3}",
                    e.Message,
                    commandId,
                    ctx.Item,
                    e.GetMessageAndInnerMessages());

                // if we had any pending data, report back that we've dealt with it all as we're abandoning it
                return -1 * currentPending;
            }
        }

        /// <summary>
        /// Processes the work workItem (while owning the work workItem)
        /// </summary>
        /// <param name="tracker">file progress tracker</param>
        /// <param name="ctx">operation context</param>
        /// <param name="counters">perf counters</param>
        /// <param name="writers">collection of export writers</param>
        /// <param name="data">data stream of source data file</param>
        /// <param name="renewer">lease renewer</param>
        /// <param name="workItem">queue work workItem</param>
        /// <param name="partition">file size partition</param>
        /// <param name="assumeNonTransient">
        ///     true to assume that possibly transient errors are non-transient; false to treat them as transient
        /// </param>
        /// <returns>resulting value</returns>
        private async Task ProcessFile(
            IFileProgressTracker tracker,
            OperationContext ctx,
            CounterSet counters,
            IDictionary<string, ICommandDataWriter> writers,
            Stream data,
            ILeaseRenewer renewer,
            PendingDataFile workItem,
            FileSizePartition partition,
            bool assumeNonTransient)
        {
            const string FinalTraceFmt =
                "Heartbeat final: at {0}, {1:n0} bytes and {2:n0} rows processed at {3:f2} rps & {4:f2} bps [DataFile: {5}]" +
                "[Manifest: {6}][Writers: {7} real @ {8:n0} bytes; {9} dead-letter @ {10:n0} bytes; {11} abandoned @ " +
                "{12:n0} bytes]";

            const string ExpectedColumnCountText = "3";

            const int ExpectedColumnCount = 3;
            const int IdxCommand = 0;
            const int IdxProductId = 1;
            const int IdxData = 2;

            using (StreamReader reader = new StreamReader(data))
            {
                DateTimeOffset nextHeartbeat = DateTimeOffset.MinValue;
                Stopwatch timer = Stopwatch.StartNew();
                double elapsedSecs;
                double rps;
                double bps;
                string trackerMsg;
                ulong elapsed;
                ulong dataSizeProcessed = 0;
                ulong row = 0;
                long elapsedTicks;
                long pendingBytes = 0;
                long deadLetterBytes;
                long abandonedBytes;
                long realBytes;
                uint deadLetter;
                uint abandoned;
                uint real;

                while (reader.EndOfStream == false)
                {
                    Stopwatch innerTimer = new Stopwatch();
                    Task<string> nextLineTask;
                    string[] columns;
                    string commandId;

                    this.CancelToken.ThrowIfCancellationRequested();

                    ++row;

                    ctx.Op = "reading line from the source data";

                    innerTimer.Start();

                    nextLineTask = reader.ReadLineAsync();
                    await Task.WhenAll(nextLineTask, renewer.RenewAsync()).ConfigureAwait(false);

                    counters.DataRowRead.SetValue(Convert.ToUInt64(innerTimer.ElapsedMilliseconds));

                    // should only happen on End-of-Stream
                    if (nextLineTask.Result == null)
                    {
                        break;
                    }

                    columns = nextLineTask.Result.Split(new[] { '\t' }, ExpectedColumnCount, StringSplitOptions.None);
                    if (columns.Length < ExpectedColumnCount)
                    {
                        this.TraceError(
                            "[file: {0}][row: {1}]: {2} columns found instead of the expected {3}",
                            workItem.DataFileTag,
                            row,
                            columns.Length,
                            ExpectedColumnCountText);

                        tracker.AddMessage(
                            TrackerTypes.DataFileError,
                            "On {0} of data file, found {1} columns instead of the expected {2}. Skipping row.",
                            row.ToStringInvariant(),
                            columns.Length.ToStringInvariant(),
                            ExpectedColumnCountText);

                        continue;
                    }

                    commandId = Utility.CanonicalizeCommandId(columns[IdxCommand]);

                    // if we have no data, no point in submitting anything (also don't care about an empty command id if we have
                    //  no data, so can do this check first)
                    if (string.IsNullOrWhiteSpace(columns[IdxData]))
                    {
                        this.TraceInfo(
                            "[file: {0}][row: {1}][command: {2}]: empty payload column found, skipping",
                            workItem.DataFileTag,
                            row,
                            commandId);

                        tracker.AddMessage(
                            TrackerTypes.DataFileError, 
                            "On " + row.ToStringInvariant() + " of data file, found empty payload. Skipping row.");

                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(commandId))
                    {
                        this.TraceError(
                            $"[file: {workItem.DataFileTag}][row: {row}][command: {commandId}] missing or invalid command id");

                        tracker.AddMessage(
                            TrackerTypes.DataFileError, 
                            "On " + row.ToStringInvariant() + " of data file, found empty command id. Skipping row.");

                        continue;
                    }

                    // if no file id is specified, just use the default
                    if (string.IsNullOrWhiteSpace(columns[IdxProductId]))
                    {
                        tracker.AddMessage(
                            TrackerTypes.DataFileError, 
                            "On " + row.ToStringInvariant() + " of data file, found empty export product id. Using default.");

                        columns[IdxProductId] = FileProcessor.DefaultProductId;
                    }

                    dataSizeProcessed += Convert.ToUInt64(columns[IdxData].Length);

                    pendingBytes += await this.ProcessSingleRow(
                        ctx, 
                        counters, 
                        writers, 
                        workItem, 
                        renewer, 
                        commandId, 
                        columns[IdxProductId], 
                        columns[IdxData], 
                        row, 
                        assumeNonTransient);

                    // if we hit the overall threshold of pending writes, then start flushing the writers until we get to at least
                    //  half of the limit
                    if (pendingBytes > this.Config.OverallPendingByteThreshold)
                    {
                        IList<ICommandDataWriter> pendingWriters = writers.Values
                            .Where(o => o.PendingSize > 0)
                            .OrderByDescending(o => o.PendingSize).ToList();

                        long limit = this.Config.OverallPendingByteThreshold / 2;

                        for (int i = 0; i < pendingWriters.Count && pendingBytes > limit; ++i)
                        {
                            long pendingDelta = pendingWriters[i].PendingSize;

                            this.CancelToken.ThrowIfCancellationRequested();

                            innerTimer.Reset();

                            try
                            {
                                pendingDelta = await pendingWriters[i].FlushAsync();

                                elapsed = Convert.ToUInt64(innerTimer.ElapsedMilliseconds);
                                counters.DataWriterFlush.SetValue(elapsed);
                                counters.DataWriterFlush.SetValue(elapsed, partition.ToString());
                            }
                            catch (NonTransientStorageException e)
                            {
                                const string Fmt =
                                    "Non-transient storage exception writing data for command {0} and data file {1}. " +
                                    "Abandoned command data: {2}";

                                elapsed = Convert.ToUInt64(innerTimer.ElapsedMilliseconds);
                                counters.DataWriterFlushError.SetValue(elapsed);
                                counters.DataWriterFlushError.SetValue(elapsed, partition.ToString());

                                this.TraceWarning(Fmt, commandId, ctx.Item, e.GetMessageAndInnerMessages());
                            }

                            pendingBytes -= pendingDelta;
                        }
                    }

                    if (row % 25 == 0)
                    {
                        DateTimeOffset now = this.clock.UtcNow;
                        if (now > nextHeartbeat)
                        {
                            const string Fmt =
                                "Heartbeat: at {0}, {1:n0} of {2:n0} bytes processed at {3:f2} rps & {4:f2} bps [DataFile: {5}]" +
                                "[Manifest: {6}][Writers: {7} real; {8} dead-letter; {9} abandoned]";

                            elapsedTicks = timer.ElapsedTicks;
                            elapsedSecs = TimeSpan.FromTicks(elapsedTicks).TotalSeconds;
                            rps = (elapsedSecs > 0) ? (row / elapsedSecs) : 0.0;
                            bps = (elapsedSecs > 0) ? (dataSizeProcessed / elapsedSecs) : 0.0;

                            deadLetter = Convert.ToUInt32(writers.Values.Count(o => o.Statuses == WriterStatuses.DeadLetterWriter));
                            real = Convert.ToUInt32(writers.Values.Count(o => o.Statuses == WriterStatuses.NormalDataWriter));

                            abandoned = Convert.ToUInt32(writers.Count) - (real + deadLetter);

                            this.taskCounters[ctx.WorkerIndex].UpdateCounters(
                                Convert.ToUInt64(elapsedTicks), dataSizeProcessed, row, real, abandoned, real);

                            this.TraceInfo(
                                Fmt,
                                timer.Elapsed.ToString(@"d\.hh\:mm\:ss"),
                                data.Position,
                                data.Length,
                                rps,
                                bps,
                                workItem.DataFileTag,
                                workItem.ManifestTag,
                                real,
                                deadLetter,
                                abandoned);

                            nextHeartbeat = now.AddSeconds(this.Config.ProgressUpdateSeconds);
                        }
                    }
                }

                elapsedTicks = timer.ElapsedTicks;
                elapsedSecs = TimeSpan.FromTicks(elapsedTicks).TotalSeconds;
                rps = (elapsedSecs > 0) ? (row / elapsedSecs) : 0.0;
                bps = (elapsedSecs > 0) ? (dataSizeProcessed / elapsedSecs) : 0.0;

                deadLetter = Convert.ToUInt32(writers.Values.Count(o => o.Statuses == WriterStatuses.DeadLetterWriter));
                real = Convert.ToUInt32(writers.Values.Count(o => o.Statuses == WriterStatuses.NormalDataWriter));

                deadLetterBytes = writers.Values.Where(o => o.Statuses == WriterStatuses.DeadLetterWriter).Sum(o => o.Size);
                realBytes = writers.Values.Where(o => o.Statuses == WriterStatuses.NormalDataWriter).Sum(o => o.Size);

                abandonedBytes = writers.Values
                    .Where(o => o.Statuses != WriterStatuses.DeadLetterWriter && o.Statuses != WriterStatuses.NormalDataWriter)
                    .Sum(o => o.Size);

                abandoned = Convert.ToUInt32(writers.Count) - (real + deadLetter);

                this.taskCounters[ctx.WorkerIndex].UpdateCounters(
                    Convert.ToUInt64(elapsedTicks), dataSizeProcessed, row, real, abandoned, real);

                elapsed = Convert.ToUInt64(elapsedSecs);
                counters.DataReadAll.SetValue(elapsed);
                counters.DataReadAll.SetValue(elapsed, partition.ToString());

                this.TraceInfo(
                    FinalTraceFmt,
                    timer.Elapsed.ToString(@"d\.hh\:mm\:ss"),
                    data.Length,
                    row,
                    rps,
                    bps,
                    workItem.DataFileTag,
                    workItem.ManifestTag,
                    real,
                    realBytes,
                    deadLetter,
                    deadLetterBytes,
                    abandoned,
                    abandonedBytes);

                trackerMsg =
                    "Completed reading data file after {0} duration, {1:n0} bytes and {2:n0} rows processed. Command data summary:"
                        .FormatInvariant(timer.Elapsed.ToString(@"d\.hh\:mm\:ss"), data.Length, row);

                if (real > 0)
                {
                    trackerMsg += $" wrote {realBytes:n0} bytes for {real} commands to blob store;";
                }

                if (deadLetter > 0)
                {
                    trackerMsg += 
                        $" wrote {deadLetterBytes:n0} bytes for {deadLetter} commands to the missing command holding store;";
                }

                if (abandoned > 0)
                {
                    trackerMsg += $" discarded {abandonedBytes:n0} bytes for {abandoned} commands";
                }

                if (real + deadLetter + abandoned == 0)
                {
                    trackerMsg += " no writers wrote any data (data file is empty or has no valid data rows)";
                }

                tracker.AddMessage(TrackerTypes.DataFileComplete, trackerMsg);
            }
        }

        /// <summary>
        ///     Waits for the tasks to finish, renewing the lease while waiting
        /// </summary>
        /// <param name="ctx">operation context</param>
        /// <param name="item">file being processed</param>
        /// <param name="tasks">tasks to wait on</param>
        /// <param name="renewer">lease renewer</param>
        /// <returns>resulting value</returns>
        private async Task WaitAndRenewLease(
            OperationContext ctx,
            PendingDataFile item,
            IEnumerable<Task> tasks,
            ILeaseRenewer renewer)
        {
            Stopwatch timer = new Stopwatch();
            TimeSpan warningThreshold = TimeSpan.FromMinutes(this.leaseTime.TotalMinutes / 2);
            Task closerWaitAll = Task.WhenAll(tasks);

            // note that we assume that even half the lease time is significantly greater than the lease renewal frequency

            // while the closer tasks are in progress, renew the lease every this.config.MinimumRenewMinutes to ensure we 
            //  retain ownership while dumping all this data.
            // Note that we deliberately ignore cancel signals within this part of the method to avoid having to reprocess
            //  a stream after completing nearly all of it
            for (;;)
            {
                TimeSpan waitActual;
                Task renewTask = renewer.RenewAsync();

                if (closerWaitAll.IsCompleted || closerWaitAll.IsCanceled || closerWaitAll.IsFaulted)
                {
                    await renewTask.ConfigureAwait(false);
                    break;
                }

                timer.Restart();

                // deliberately ignore cancel signals while waiting to finish writing all the files
                await Task
                    .WhenAny(
                        closerWaitAll,
                        Task.Delay(this.leaseRenewFreq, CancellationToken.None))
                    .ConfigureAwait(false);

                if (closerWaitAll.IsCompleted || closerWaitAll.IsCanceled || closerWaitAll.IsFaulted)
                {
                    break;
                }

                waitActual = timer.Elapsed;

                if (waitActual > this.leaseTime || waitActual > warningThreshold)
                {
                    FileWriterLeaseRenewWaitedTooLong ev = new FileWriterLeaseRenewWaitedTooLong
                    {
                        AgentId = item.AgentId,
                        ManifestPath = item.ManifestPath,
                        FileName = item.DataFilePath,
                        MinutesActual = waitActual.TotalMinutes,
                        MinutesExpected = this.leaseRenewFreq.TotalMinutes,
                        MinutesLease = this.leaseTime.TotalMinutes
                    };

                    if (waitActual > this.leaseTime)
                    {
                        this.LogEventError(ctx, ev);
                    }
                    else
                    {
                        this.LogEventWarning(ctx, ev);
                    }
                }
            }
        }

        /// <summary>
        ///     Processes the work workItem (while owning the work workItem)
        /// </summary>
        /// <param name="tracker">file progress tracker</param>
        /// <param name="ctx">operation context</param>
        /// <param name="fullItem">work item</param>
        /// <param name="file">data file</param>
        /// <returns>true to allow the file to be processed normally as a zero byte file or</returns>
        private async Task<(ProcessingResult, FileSizePartition)> ProcessZeroQueueRequestAsync(
            IFileProgressTracker tracker,
            OperationContext ctx,
            PartitionedQueueItem<PendingDataFile, FileSizePartition> fullItem,
            IFile file)
        {
            PendingDataFile item = fullItem.Item.Data;

            ctx.Op = "determining file type";

            // if we have a size, just use it directly
            //  we assume that if Cosmos returns a non-zero size, it is the final non-zero size and will not increment later
            //  (assuming the file is not being updated, of couse, but data files should not be updated once the manifests
            //   are written)
            if (file?.Size > 0)
            {
                const string TrackerFmt =
                    "Cosmos is reporting size data for file [{0}] found at [{1}]. File size is {2} which classifies it as " + 
                    "'{3}'. Sending file to {3} data file queue for processing.";

                FileSizePartition type = Utility.GetPartition(this.fileSystemManager.FileSizeThresholds, file.Size);

                tracker.AddMessage(
                    TrackerTypes.BatchDataFiles,
                    TrackerFmt,
                    item.DataFileTag,
                    item.DataFilePath,
                    file.Size.ToStringInvariant(),
                    type.ToString());

                this.TraceInfo(
                    $"Enqueueing formerly Empty, but now {type} data file {item.DataFileTag} for manifest {item.ManifestTag}");

                ctx.Op = $"Enqueueing formerly empty, but now {type} item to queue";

                await this.pendingQueue.EnqueueAsync(type, fullItem.Item.Data, this.CancelToken);

                return (ProcessingResult.CompleteQueued, type);
            }

            // probe the file to see if it actually has any data in it
            if (file != null)
            {
                Stream data;

                ctx.Op = "probing zero byte file to see if it really is zero byte";

                data = await file.ReadFileChunkAsync(0, 512).ConfigureAwait(false);

                // assume if we ask for the first N bytes of the file and get zero bytes back, the file is truely zero bytes
                //  and we can just process the file inline (as this just entails queueing a item to the completion queue)
                if (data.Length > 0)
                {
                    const string FmtTracker =
                        "Empty data file {0} for manifest {1} is still reporting as empty, but has at least {2} bytes. Will " +
                        "retry later to see if Cosmos reports a non-zero size";

                    const string FmtTrace =
                        "Cosmos is still reporting incorrect size data for for file [{0}] found at [{1}], though it is at least " +
                        "{2} bytes in size. File will be retryed at a later point in time to see if Cosmos reports a non-zero " +
                        "size. A correct size is required to assign the file to a correct queue to ensure large files do not " + 
                        "consume all processing time and starve smaller files";

                    this.TraceInfo(FmtTrace, item.DataFileTag, item.DataFilePath, data.Length.ToStringInvariant());

                    tracker.AddMessage(
                        TrackerTypes.BatchDataFiles,
                        FmtTracker, 
                        item.DataFileTag, 
                        item.DataFilePath, 
                        data.Length.ToStringInvariant());

                    return (ProcessingResult.Incomplete, fullItem.PartitionId);
                }

                this.TraceInfo($"Processing actually empty data file {item.DataFileTag} for manifest {item.ManifestTag}");
            }

            //  file doesn't exist so just process it inline (again, just queue item to completion queue)
            return (ProcessingResult.NeedsProcessing, fullItem.PartitionId);
        }

        /// <summary>
        ///     Closes the files
        /// </summary>
        /// <param name="ctx">operation context</param>
        /// <param name="item">work item</param>
        /// <param name="renewer">lease renewer</param>
        /// <param name="writerFlushCounter">writer flush counter</param>
        /// <param name="writers">writers to close</param>
        /// <returns>resulting value</returns>
        private async Task CloseFiles(
            OperationContext ctx,
            PendingDataFile item,
            ILeaseRenewer renewer,
            ICounter writerFlushCounter,
            ICollection<ICommandDataWriter> writers)
        {
            const int CloseCallBatchSize = 5;

            // local method to close / flush writers opened to send out found data
            async Task CloseFileAsync(ICommandDataWriter w)
            {
                try
                {
                    Stopwatch timer = new Stopwatch();

                    this.TraceVerbose($"Closing file writer for {w.CommandId} and data file {ctx.Item}");

                    timer.Start();
                    await w.CloseAsync().ConfigureAwait(false);
                    writerFlushCounter.SetValue(Convert.ToUInt64(timer.ElapsedMilliseconds));
                }
                catch (NonTransientStorageException e)
                {
                    this.TraceWarning(
                        "Non-transient storage exception {0} writing for for {1} and data file {2}. Abandoned command data: {3}",
                        e.Message,
                        w?.CommandId,
                        ctx.Item,
                        e.GetMessageAndInnerMessages());
                }
                catch (Exception e)
                {
                    this.TraceError(
                        $"failed to close file writer for command {w.CommandId} and data file {item.DataFileTag}: {e}");
                    throw;
                }
            }

            for (int i = 0; i < writers.Count; i += CloseCallBatchSize)
            {
                IEnumerable<Task> waiters = writers
                    .Skip(i)
                    .Take(CloseCallBatchSize)
                    .Select(CloseFileAsync);

                await this.WaitAndRenewLease(ctx, item, waiters, renewer).ConfigureAwait(false);
            }
        }

        /// <summary>
        ///     writes the per command row counts and error messages that we will later provide PCF
        /// </summary>
        /// <param name="ctx">operation context</param>
        /// <param name="item">work item</param>
        /// <param name="renewer">lease renewer</param>
        /// <param name="writers">writers to close</param>
        /// <returns>resulting value</returns>
        private async Task WriteRowCounts(
            OperationContext ctx,
            PendingDataFile item,
            ILeaseRenewer renewer,
            IEnumerable<ICommandDataWriter> writers)
        {
            const int RowCountCallBatchSize = 10;
            const int AzureStorageBatchSize = 99;

            // local method to write row count batches to table store for command feed to pick up later
            async Task WriteRowCountBatchAsync(ICollection<ICommandDataWriter> list)
            {
                try
                {
                    await this.commandFileState.InsertBatchAsync(
                        list.Select(
                            w =>
                            {
                                CommandFileState result = new CommandFileState
                                {
                                    DataFilePathAndCommand = w.CommandId + "&" + item.DataFilePath,
                                    CommandId = w.CommandId,
                                    AgentId = item.AgentId,
                                    FilePath = item.DataFilePath
                                };

                                if (w.LastErrorDetails == null)
                                {
                                    result.NonTransientErrorInfo = null;
                                    result.ByteCount = w.Size;
                                    result.RowCount = w.RowCount;
                                }
                                else
                                {
                                    result.NonTransientErrorInfo = w.LastErrorDetails;
                                    result.ByteCount = 0;
                                    result.RowCount = 0;
                                }

                                return result;
                            })
                        .ToList())
                    .ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    string cmdList = string.Join(",", list.Select(o => o.CommandId));

                    this.TraceError(
                        $"failed to create row count entires for commands {cmdList} and file {item.DataFileTag}: {e}");

                    // ignore all storage exceptions for this as we really don't want to have to reprocess a large file
                    //  just because we couldn't write the row count for (some of) its commands
                    if (e is StorageException == false)
                    {
                        throw;
                    }
                }
            }

            // this is kinda confusing, but what is does is create a list of tuples that have a writer and original list
            //  index of the writer.  It then groups the writers by the index / AzureStorageBatchSize (creating groups  
            //  of no more than AzureStorageBatchSize writers).
            // Note that we only want to log row counts when we have an actual command from command feed becuase no one
            //  will consume the row counts otherwise. This decision can be done based on the writer we used- the no-op 
            //  and command feed writers can get data logged while the delete feed writer will not; thus we can use a 
            //  property on the writer (LogForCommandFeed) to indicate whether or not we should write the row counts.
            List<List<ICommandDataWriter>> rowCountInsertWriters = writers
                .Where(o => o.LogForCommandFeed && (o.LastErrorDetails != null || o.RowCount > 0))
                .Select((w, i) => (Index: i, Writer: w))
                .GroupBy(tuple => tuple.Index / AzureStorageBatchSize)
                .Select(group => group.Select(o => o.Writer).ToList())
                .ToList();

            for (int i = 0; i < rowCountInsertWriters.Count; i += RowCountCallBatchSize)
            {
                IEnumerable<Task> waiters = rowCountInsertWriters
                    .Skip(i)
                    .Take(RowCountCallBatchSize)
                    .Select(WriteRowCountBatchAsync);

                await this.WaitAndRenewLease(ctx, item, waiters, renewer).ConfigureAwait(false);
            }
        }

        /// <summary>
        ///     Writes the tracker and stats data
        /// </summary>
        /// <param name="tracker">file progress tracker</param>
        /// <param name="item">work item</param>
        /// <param name="dataFileName">data file name</param>
        /// <param name="writers">data writers</param>
        /// <returns>resulting value</returns>
        private async Task WriteTrackerAndStatsData(
            IFileProgressTracker tracker,
            PendingDataFile item,
            string dataFileName,
            ICollection<ICommandDataWriter> writers)
        {
            IDictionary<WriterStatuses, StatsCounts> stats = new Dictionary<WriterStatuses, StatsCounts>();
            StringBuilder data = new StringBuilder();
            string manifestName = Utility.SplitFileTag(item.ManifestTag).Name;
            long rows = 0;
            long size = 0;

            stats.Add(WriterStatuses.NormalDataWriter, new StatsCounts());
            stats.Add(WriterStatuses.DeadLetterWriter, new StatsCounts());
            stats.Add(WriterStatuses.AbandonedGeneral, new StatsCounts());

            foreach (ICommandDataWriter writer in writers)
            {
                const string ErrorCaseFmt = "Command {0}: processed {1:n0} bytes from {2:n0} rows. {3}";
                const string NormalFmt = "Command {0}: wrote {1:n0} bytes from {2:n0} rows to blob store";

                WriterStatuses type;
                string abandonType;

                string description = writer.Statuses != WriterStatuses.NormalDataWriter ?
                    WriterDescriptionGenerator.GenerateWriterDescription(writer) :
                    string.Empty;

                tracker.AddMessage(
                    TrackerTypes.DataFileCommand,
                    writer.Statuses == WriterStatuses.NormalDataWriter ? NormalFmt : ErrorCaseFmt,
                    writer.CommandId,
                    writer.Size,
                    writer.RowCount,
                    description);

                abandonType = WriterDescriptionGenerator.GetAbandonedType(writer);
                type = WriterDescriptionGenerator.GetOriginalType(writer);
                stats[type].AddWriter(writer);

                foreach (IFileDetails details in writer.FileDetails)
                {
                    data.Append(item.AgentId);
                    data.Append('\t');
                    data.Append(writer.CommandId);
                    data.Append('\t');
                    data.Append(manifestName);
                    data.Append('\t');
                    data.Append(dataFileName);
                    data.Append('\t');
                    data.Append(type.ToString());
                    data.Append('\t');
                    data.Append(abandonType);
                    data.Append('\t');
                    data.Append(details.Size.ToStringInvariant());
                    data.Append('\t');
                    data.Append(details.RowCount.ToStringInvariant());
                    data.Append("\tv.2\t");
                    data.Append(details.FileName);
                    data.Append('\t');
                    data.Append(details.ProductId);
                    data.Append('\n');
                }

                rows += writer.RowCount;
                size += writer.Size;
            }

            await this.commandStatsWriter.QueueWriteAsync(data.ToString());

            data = new StringBuilder();
            data.Append(item.AgentId);
            data.Append('\t');
            data.Append(manifestName);
            data.Append('\t');
            data.Append(dataFileName);
            data.Append('\t');
            data.Append(writers.Count.ToStringInvariant());
            data.Append('\t');
            data.Append(rows.ToStringInvariant());
            data.Append('\t');
            data.Append(size.ToStringInvariant());
            stats[WriterStatuses.NormalDataWriter].AppendToStringBuilder(data);
            stats[WriterStatuses.DeadLetterWriter].AppendToStringBuilder(data);
            stats[WriterStatuses.AbandonedGeneral].AppendToStringBuilder(data);
            data.Append('\n');

            await this.dataFileStatsWriter.QueueWriteAsync(data.ToString());
        }

        /// <summary>
        ///     Processes the work workItem (while owning the work workItem)
        /// </summary>
        /// <param name="tracker">file progress tracker</param>
        /// <param name="ctx">operation context</param>
        /// <param name="renewer">lease renewer</param>
        /// <param name="item">work item</param>
        /// <param name="partition">file size partition</param>
        /// <param name="file">file to process</param>
        /// <returns>resulting value</returns>
        private async Task<ProcessingResult> ProcessDataFileAsync(
            IFileProgressTracker tracker,
            OperationContext ctx,
            ILeaseRenewer renewer,
            PendingDataFile item,
            FileSizePartition partition,
            IFile file)
        {
            IDictionary<string, ICommandDataWriter> writers =
                new Dictionary<string, ICommandDataWriter>(StringComparer.OrdinalIgnoreCase);

            CounterSet counters;

            ctx.Op = "fetching perf counters";

            counters = new CounterSet((s, t) => this.CounterFactory.GetCounter(this.TaskCounterCategory, s, t));

            if (file != null)
            {
                try
                {
                    ctx.Op = "fetching data file stream";

                    using (Stream contents = file.GetDataReader())
                    {
                        bool assumeNonTransient = file.Created.Add(this.assumeTransientThresold) <= this.clock.UtcNow;

                        if (contents != null)
                        {
                            await this
                                .ProcessFile(tracker, ctx, counters, writers, contents, renewer, item, partition, assumeNonTransient)
                                .ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception e)
                {
                    // log this here also in case we get a failure in the finally
                    this.TraceError($"Error processing file {ctx.Item} while {ctx.Op}: {e}");
                    throw;
                }
                finally
                {
                    Stopwatch timer = new Stopwatch();
                    ICounter counter;
                    ulong elapsed;

                    // since we're in a finally block, push the current op as we could have got here via an exception where we'd 
                    //  want to preserve the current context. If this finally block throws, we'll fail to pop later, so the correct
                    //  context is maintained
                    ctx.PushOp();

                    // do the closers first in case there are errors writing 

                    ctx.Op = "closing file writers";

                    timer.Start();
                    await this.CloseFiles(ctx, item, renewer, counters.DataWriterFlush, writers.Values).ConfigureAwait(false);

                    ctx.Op = "updating per command row counts";

                    timer.Restart();
                    await this.WriteRowCounts(ctx, item, renewer, writers.Values).ConfigureAwait(false);
                    elapsed = Convert.ToUInt64(timer.ElapsedMilliseconds);

                    counter = this.GetCounter(FileProcessor.CommandStatsUpdateCounter);
                    counter.SetValue(elapsed);
                    counter.SetValue(elapsed, partition.ToString());

                    ctx.Op = "adding messages to tracker for final command writer status";

                    timer.Restart();
                    await this.WriteTrackerAndStatsData(tracker, item, file.Name, writers.Values).ConfigureAwait(false);
                    elapsed = Convert.ToUInt64(timer.ElapsedMilliseconds);

                    counter = this.GetCounter(FileProcessor.TrackerGlobalStatsUpdateCounter);
                    counter.SetValue(elapsed);
                    counter.SetValue(elapsed, partition.ToString());

                    // restore the orignial context
                    ctx.PopOp();
                }
            }

            // if we fail to find a file, then assume the file has been completed and enqueue a work item to check if the batch
            //  is complete

            this.TraceVerbose($"File processor completed file {ctx.Item} for processing. Enqueuing for 'file complete' processing");

            ctx.Op = "enqueuing file completion work item";
            await this.doneQueue.EnqueueAsync(new CompleteDataFile(item), this.CancelToken);

            if (file != null)
            {
                string renamePath = this.holdingPath + Utility.EnsureTrailingSlash(item.AgentId);

                ctx.Op = "moving data file to holding";
                await file.MoveRelativeAsync(renamePath, true, true).ConfigureAwait(false);

                ctx.Op = "setting holding data file expiry";
                await file.SetLifetimeAsync(this.holdingExpiry, true).ConfigureAwait(false);
            }

            return ProcessingResult.CompleteProcessed;
        }

        /// <summary>
        ///     Processes the work workItem (while owning the work workItem) 
        /// </summary>
        /// <param name="tracker">file progress tracker</param>
        /// <param name="ctx">operation context</param>
        /// <param name="renewer">lease renewer</param>
        /// <param name="fullItem">work item</param>
        /// <returns>resulting value</returns>
        private async Task<(ProcessingResult Result, FileSizePartition NewPartition)> ProcessRequestAsync(
            IFileProgressTracker tracker,
            OperationContext ctx,
            ILeaseRenewer renewer,
            PartitionedQueueItem<PendingDataFile, FileSizePartition> fullItem)
        {
            FileSizePartition newPartition = fullItem.PartitionId;
            ProcessingResult result = ProcessingResult.NeedsProcessing;
            PendingDataFile item = fullItem.Item.Data;
            IFileSystem fileSource;
            string dataFilePathFixup = item.DataFilePath;
            IFile file;

            ctx.Op = "fetching file system";

            fileSource = this.fileSystemManager.GetFileSystem(item.CosmosTag);

            ctx.Op = "fixing up data filename based on manifest filename";

            if (ManfiestNameParser.RequiresFixup(dataFilePathFixup))
            {
                TemplateParseResult parseResult = ManfiestNameParser.ParseManifestForDataFileTemplate(item.ManifestPath);
                dataFilePathFixup = ManfiestNameParser.BuildOutputFilenameFromTemplate(dataFilePathFixup, parseResult);
            }

            ctx.Op = "opening data file";

            file = await fileSource.OpenExistingFileAsync(dataFilePathFixup).ConfigureAwait(false);

            if (file != null && fullItem.PartitionId == FileSizePartition.Empty)
            {
                (result, newPartition) = 
                    await this.ProcessZeroQueueRequestAsync(tracker, ctx, fullItem, file).ConfigureAwait(false);
            }

            if (result == ProcessingResult.NeedsProcessing)
            {
                if (file != null && newPartition == FileSizePartition.Oversize)
                {
                    this.LogEventError(
                        ctx,
                        new ExcessiveBatchFileSizeEvent
                        {
                            ManifestPath = item.ManifestPath,
                            AgentId = item.AgentId,
                            FileName = file.Name,
                            FileSize = file.Size
                        });
                }

                result = await this
                    .ProcessDataFileAsync(tracker, ctx, renewer, item, fullItem.PartitionId, file)
                    .ConfigureAwait(false);
            }

            return (result, newPartition);
        }

        /// <summary>
        ///     keeps track of statistic counts 
        /// </summary>
        private class StatsCounts
        {
            private long writerCount;
            private long rowCount;
            private long size;

            public void AddWriter(ICommandDataWriter writer)
            {
                ++this.writerCount;

                this.rowCount += writer.RowCount;
                this.size += writer.Size;
            }

            public void AppendToStringBuilder(StringBuilder data)
            {
                data.Append('\t');
                data.Append(this.writerCount.ToStringInvariant());
                data.Append('\t');
                data.Append(this.rowCount.ToStringInvariant());
                data.Append('\t');
                data.Append(this.size.ToStringInvariant());
            }
        }

        /// <summary>
        ///     manages a set of counters
        /// </summary>
        private class CounterSet
        {
            /// <summary>
            ///    Initializes a new instance of the CounterSet class
            /// </summary>
            /// <param name="counterCreator">counter creator</param>
            public CounterSet(Func<string, CounterType, ICounter> counterCreator)
            {
                this.DataRowBufferWrite = counterCreator(FileProcessor.DataRowBufferWriteCounter, CounterType.Number);
                this.DataRowErrorWrite = counterCreator(FileProcessor.DataRowErrorWriteCounter, CounterType.Number);
                this.DataRowFlushWrite = counterCreator(FileProcessor.DataRowFlushWriteCounter, CounterType.Number);

                this.DataRowRead = counterCreator(FileProcessor.DataRowReadCounter, CounterType.Number);
                
                this.DataWriterFlushError = counterCreator(FileProcessor.DataWriterFlushErrorCounter, CounterType.Number);
                this.DataWriterCreate = counterCreator(FileProcessor.DataWriterCreateCounter, CounterType.Number);
                this.DataWriterFlush = counterCreator(FileProcessor.DataWriterFlushCounter, CounterType.Number);

                this.DataReadAll = counterCreator(FileProcessor.DataReadAllCounter, CounterType.Number);
            }

            public ICounter DataRowBufferWrite { get; }
            public ICounter DataRowErrorWrite { get; }
            public ICounter DataRowFlushWrite { get; }

            public ICounter DataRowRead { get; }

            public ICounter DataWriterFlushError { get; }
            public ICounter DataWriterCreate { get; }
            public ICounter DataWriterFlush { get; }

            public ICounter DataReadAll { get; }
        }

        /// <summary>
        ///     Periodic task counters
        /// </summary>
        private struct PeriodicCounters
        {
            private FileSizePartition fileType;

            private ulong ticks;
            private ulong bytes;
            private ulong rows;

            private uint deadLetter;
            private uint abandoned;
            private uint real;

            public void Reset()
            {
                this.deadLetter = 0;
                this.abandoned = 0;
                this.real = 0;

                this.ticks = 0;
                this.bytes = 0;
                this.rows = 0;

                this.fileType = FileSizePartition.Invalid;
            }

            public void SetInProgress(FileSizePartition type)
            {
                this.fileType = type;
            }

            public void UpdateCounters(
                ulong ticks,
                ulong bytes,
                ulong rows,
                uint deadLetter,
                uint abandoned,
                uint real)
            {
                this.deadLetter = deadLetter;
                this.abandoned = abandoned;
                this.real = real;

                this.ticks = ticks;
                this.bytes = bytes;
                this.rows = rows;
            }

            public void AddCounts(
                IDictionary<FileSizePartition, PartitionCounterData> partitionData,
                ref uint deadLetter,
                ref uint abandoned,
                ref uint real)
            {
                if (this.fileType != FileSizePartition.Invalid)
                {
                    PartitionCounterData data = partitionData[this.fileType];

                    deadLetter += this.deadLetter;
                    abandoned += this.abandoned;
                    real += this.real;

                    data.Count += 1;
                    data.Ticks += this.ticks;
                    data.Bytes += this.bytes;
                    data.Rows += this.rows;
                }
            }
        }

        private class PartitionCounterData
        {
            public ulong Ticks { get; set; }
            public ulong Bytes { get; set; }
            public ulong Rows { get; set; }
            public uint Count { get; set; }
        }
    }
}
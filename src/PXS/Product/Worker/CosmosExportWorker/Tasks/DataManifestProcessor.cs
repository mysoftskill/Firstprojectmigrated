// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

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
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility.ManifestParsers;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility.ProgressTracker;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility.Tracer;
    using Microsoft.PrivacyServices.Common.CosmosExport.Telemetry;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     task to monitor the file set processor queue and process a file set
    /// </summary>
    public class DataManifestProcessor : TrackCountersBaseTask<IDataManifestProcessorConfig>
    {
        private static readonly TimeSpan EmptyQueuePause = TimeSpan.FromSeconds(15);

        private readonly IPartitionedQueue<PendingDataFile, FileSizePartition> pendingQueue;
        private readonly ITable<ManifestFileSetState> manifestFileSetState;
        private readonly IFileProgressTrackerFactory trackerFactory;
        private readonly IRequestCommandUtilities requestCmdUtils;
        private readonly IQueue<CompleteDataFile> completeQueue;
        private readonly IQueue<ManifestFileSet> manifestFileSetQueue;
        private readonly IFileSystemManager fileSystemManager;
        private readonly ILockManager lockMgr;  
        private readonly TaskTracer taskTracer;
        private readonly TimeSpan delayOnIncomplete;
        private readonly TimeSpan maxCommandWait;
        private readonly TimeSpan leaseRenewFreq;
        private readonly TimeSpan leaseTime;
        private readonly IClock clock;

        /// <summary>
        ///     Initializes a new instance of the DataManifestProcessor class
        /// </summary>
        /// <param name="config">task config</param>
        /// <param name="trackerFactory">file progress tracker factory</param>
        /// <param name="manifestFileSetState">file set state</param>
        /// <param name="completeQueue">complete queue</param>
        /// <param name="manifestFileSetQueue">file set queue</param>
        /// <param name="pendingQueue">pending queue</param>
        /// <param name="requestCmdUtils">request command utils</param>
        /// <param name="fileSystemManager">file system manager</param>
        /// <param name="telemetryLogger">telemetry logger</param>
        /// <param name="counterFactory">perf counter factory</param>
        /// <param name="lockManager">lock manager</param>
        /// <param name="traceLogger">trace logger</param>
        /// <param name="clock">time of day clock</param>
        public DataManifestProcessor(
            IDataManifestProcessorConfig config,
            IFileProgressTrackerFactory trackerFactory,
            ITable<ManifestFileSetState> manifestFileSetState,
            IQueue<CompleteDataFile> completeQueue,
            IQueue<ManifestFileSet> manifestFileSetQueue,
            IPartitionedQueue<PendingDataFile, FileSizePartition> pendingQueue,
            IRequestCommandUtilities requestCmdUtils,
            IFileSystemManager fileSystemManager,
            ITelemetryLogger telemetryLogger,
            ICounterFactory counterFactory,
            ILockManager lockManager,
            ILogger traceLogger,
            IClock clock) :
            base(config, counterFactory, telemetryLogger, traceLogger)
        {
            this.manifestFileSetQueue = manifestFileSetQueue ?? throw new ArgumentNullException(nameof(manifestFileSetQueue));
            this.manifestFileSetState = manifestFileSetState ?? throw new ArgumentNullException(nameof(manifestFileSetState));
            this.fileSystemManager = fileSystemManager ?? throw new ArgumentNullException(nameof(fileSystemManager));
            this.requestCmdUtils = requestCmdUtils ?? throw new ArgumentNullException(nameof(requestCmdUtils));
            this.trackerFactory = trackerFactory ?? throw new ArgumentNullException(nameof(trackerFactory));
            this.completeQueue = completeQueue ?? throw new ArgumentNullException(nameof(completeQueue));
            this.pendingQueue = pendingQueue ?? throw new ArgumentNullException(nameof(pendingQueue));
            this.lockMgr = lockManager ?? throw new ArgumentNullException(nameof(lockManager));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));

            this.delayOnIncomplete = TimeSpan.FromMinutes(config.DelayIfCouldNotCompleteMinutes);
            this.maxCommandWait = TimeSpan.FromMinutes(config.MaxCommandWaitTimeMinutes);
            this.leaseRenewFreq = TimeSpan.FromMinutes(config.MinimumRenewMinutes);
            this.leaseTime = TimeSpan.FromMinutes(config.LeaseMinutes);

            this.taskTracer = new TaskTracer(
                traceErr: this.TraceError,
                traceWarn: this.TraceWarning,
                traceInfo: this.TraceInfo,
                traceVerbose: this.TraceVerbose);
        }

        /// <summary>
        ///     Starts up the set of tasks used by the task to execute work
        /// </summary>
        /// <param name="ctx">operation context</param>
        /// <returns>resulting value</returns>
        protected override async Task<TimeSpan?> RunOnceAsync(OperationContext ctx)
        {
            IQueueItem<ManifestFileSet> item;
            ManifestFileSet data;
            bool shouldComplete = false;

            ctx.Op = "dequeue data manifest file work item";

            item = await this.manifestFileSetQueue
                .DequeueAsync(this.leaseTime, Constants.DequeueTimeout, null, this.CancelToken)
                .ConfigureAwait(false);

            data = item?.Data;

            if (data == null)
            {
                // wait 10 before next attempt if we failed to dequeue
                return DataManifestProcessor.EmptyQueuePause;
            }

            ctx.Item = "[dataManifest: " + data.DataManifestTag + "]";

            this.TraceInfo($"Manifest processor dequeued pending file {ctx.Item} for processing");

            // if we process this item without success too many times, abandon it and wait for the cosmos monitor to re-enqueue it later. This
            //  will keep us from processing the same items over and over and starving other items
            if (item.DequeueCount > this.Config.MaxDequeueCount)
            {
                const string Fmt =
                    "Manifest processor dequeued pending file {0} for processing, but its previous dequeue count of {1} exceeds " +
                    "the max allowed dequeue count of {2}. Abandoning and waiting for it to be enqueued again by the cosmos monitor";

                this.TraceWarning(Fmt, ctx.Item, item.DequeueCount, this.Config.MaxDequeueCount);

                await item.CompleteAsync();
                return null;
            }

            //
            // acquire the lock for the manifest.  If we can't then another process owns it, so we can just ignore it- if they
            //  fail processing it'll get appended to the queue again later by the file system monitor.
            // 

            try
            {
                IFileProgressTracker tracker;
                ILockLease lease;
                string agentId;

                ctx.Op = "acquire data manifest lease";

                lease = await this.lockMgr.AttemptAcquireAsync(
                    data.AgentId,
                    data.DataManifestPath,
                    ctx.TaskId,
                    this.leaseTime,
                    false).ConfigureAwait(false);
                if (lease == null)
                {
                    // someone else owns it; keeping calm and carrying on.
                    // We'll mark it complete becuase even if the owner fails AND runs out of dequeue attempts, the cosmos monitor 
                    //  task will pick it up and create another queue item shortly.
                    shouldComplete = true;

                    return null;
                }

                this.TraceInfo("Processing data manifest queue item " + ctx.Item);

                ctx.Op = "open data and request manifests";

                this.CancelToken.ThrowIfCancellationRequested();

                tracker = this.trackerFactory.Create(
                    data.AgentId,
                    CosmosFileSystemUtility.SplitNameAndPath(data.DataManifestPath).Name, 
                    this.TraceError);

                agentId = data.AgentId;

                try
                {
                    ILeaseRenewer renewer = new LeaseRenewer(
                        new List<Func<Task<bool>>>
                        {
                            () => lease.RenewAsync(this.leaseTime),
                            () => item.RenewLeaseAsync(this.leaseTime)
                        },
                        this.leaseRenewFreq,
                        this.clock,
                        s => this.TraceError(s),
                        ctx.Item);

                    shouldComplete = await this.ProcessRequestAsync(ctx, tracker, renewer, data).ConfigureAwait(false);

                    this.TraceInfo($"Completed processing data manifest queue item {ctx.Item}, shouldComplete: {shouldComplete}");

                    this.CancelToken.ThrowIfCancellationRequested();
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

                    if (e is OperationCanceledException)
                    {
                        this.TraceError(
                            $"Task cancelled exception processing manifest for batch {ctx.Item} while {ctx.Op}: {e}");
                    }

                    throw;
                }
                finally
                {
                    ctx.PushOp();
                    ctx.Op = "release data manifest lease";

                    await lease.ReleaseAsync(shouldComplete).ConfigureAwait(false);

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
                await (shouldComplete ? item.CompleteAsync() : item.RenewLeaseAsync(this.delayOnIncomplete)).ConfigureAwait(false);

                ctx.PopOp();
            }

            return null;
        }

        /// <summary>
        ///     Enqueues the data file items
        /// </summary>
        /// <param name="tracker">file progress tracker</param>
        /// <param name="fileSystem">file system</param>
        /// <param name="statsLog">stats log</param>
        /// <param name="agentId">agent id</param>
        /// <param name="cosmosTag">cosmos tag</param>
        /// <param name="manifest">manifest file</param>
        /// <param name="dataFileNames">data file names</param>
        /// <returns>resulting value</returns>
        private async Task EnqueueDataFileItems(
            IFileProgressTracker tracker,
            IFileSystem fileSystem,
            FileCommandLog statsLog,
            string agentId,
            string cosmosTag,
            IFileSystemObject manifest,
            IEnumerable<ManifestDataFile> dataFileNames)
        {
            await Task.WhenAll(
                dataFileNames
                    .Select(
                        item => new PendingDataFile(
                            manifest.Path,
                            manifest.ParentDirectory + "/" + item.CosmosName,
                            item.PackageName + Constants.PackageDataFileExtension,
                            cosmosTag,
                            agentId))
                    .Select(
                        async pending =>
                        {
                            FileSizePartition type = FileSizePartition.Invalid;

                            try
                            {
                                IFile file = await fileSystem.OpenExistingFileAsync(pending.DataFilePath);

                                // if we have a file, send it to the queue appropriate for its size
                                if (file != null)
                                {
                                    const string TrackerFmtNonZero =
                                        "Data file [{0}] found in cosmos at [{1}]. File size is {2} which classifies it as '{3}'. " +
                                        "Sending file to {3} data file queue for processing.";

                                    const string TrackerFmtZero =
                                        "Data file [{0}] found in cosmos at [{1}]. File size is currently reported as zero by " +
                                        "Cosmos, but Cosmos can take time to report size correctly. Sending file to queue " +
                                        "for determining accurate file size; see data file log for later updates.  A correct size " +
                                        "is required to assign the file to a correct queue to ensure large files do not comsume " +
                                        "all processing time and starve smaller files";

                                    type = Utility.GetPartition(this.fileSystemManager.FileSizeThresholds, file.Size);

                                    this.TraceInfo(
                                        $"Enqueueing {type} data file {pending.DataFileTag} for manifest {pending.ManifestTag}");

                                    tracker?.AddMessage(
                                        TrackerTypes.BatchDataFiles,
                                        file.Size > 0 ? TrackerFmtNonZero : TrackerFmtZero,
                                        pending.DataFileTag,
                                        pending.DataFilePath,
                                        file.Size.ToStringInvariant(),
                                        type.ToString());

                                    await this.pendingQueue.EnqueueAsync(type, pending, this.CancelToken);

                                    statsLog?.AddValidDataFile();
                                }

                                // if the file doesn't exist, then no need to go through an extra queue- send it directly to the
                                //  complete queue.
                                else
                                {
                                    const string Fmt =
                                        "Data file [{0}] was NOT found in cosmos at [{1}]. If this is the first mention of this " +
                                        "data file in the log, a missing file indicates that a data file with the specified name " +
                                        "was not emitted by your job";
                                    
                                    tracker?.AddMessage(TrackerTypes.BatchDataFiles, Fmt, pending.DataFileTag, pending.DataFilePath);

                                    CompleteDataFile complete = new CompleteDataFile
                                    {
                                        DataFilePath = pending.DataFilePath,
                                        ManifestPath = pending.ManifestPath,
                                        CosmosTag = pending.CosmosTag,
                                        AgentId = pending.AgentId,
                                    };

                                    await this.completeQueue.EnqueueAsync(complete, this.CancelToken);

                                    statsLog?.AddMissingDataFile();
                                }
                            }
                            catch (Exception e)
                            {
                                this.TraceError(
                                    $"Enqueue of {type} file {pending.DataFileTag} for manifest {pending.ManifestTag} failed: {e}");
                                throw;
                            }
                        })).ConfigureAwait(false);
        }

        /// <summary>
        ///     Reads the data file manifest
        /// </summary>
        /// <param name="ctx">operation context</param>
        /// <param name="item">queue work item</param>
        /// <param name="manifest">data file manifest</param>
        /// <returns>processed contents of the manifest file</returns>
        private async Task<ICollection<ManifestDataFile>> ReadDataFileManifestAsync(
            OperationContext ctx,
            ManifestFileSet item,
            IFile manifest)
        {
            ICollection<ManifestDataFile> result;
            TemplateParseResult manifestParseResult;

            manifestParseResult = ManfiestNameParser.ParseManifestForDataFileTemplate(manifest.Name);

            result = await DataFileManifestReader
                .GetDataFileNamesAsync(manifest, manifestParseResult, this.TraceError)
                .ConfigureAwait(false);
            if (result == null)
            {
                // if we can't get the data file set, abandon and assume we'll try again later if it still needs processing
                this.TraceWarning("Failed to open data file manifest " + item.DataManifestTag + " despite owning lock for it");
                return null;
            }

            ctx.Op = "Generating file tags and building valid file list";

            foreach (ManifestDataFile file in result)
            {
                file.PopulateTag(item.CosmosTag, item.AgentId);
            }

            return result;
        }

        /// <summary>
        ///     Inserts a new state object or updates the existing one and returns the current state
        /// </summary>
        /// <param name="item">queue work item</param>
        /// <param name="namesValid">names valid</param>
        /// <param name="dataManifestHash">order indepenedent hash of the data manifest contents</param>
        /// <param name="reqManifestHash">order indepenedent hash of the request manifest contents</param>
        /// <param name="dataManifest">data manifest</param>
        /// <param name="reqManifest">request manifest</param>
        /// <returns>
        /// current state,
        /// true if it was inserted or false if it was updated,
        /// true if the manifest file list and the state file list should be merged; false otherwise
        /// </returns>
        private async Task<(ManifestFileSetState State, bool Inserted)> InsertOrUpdateManifestStateAsync(
            ManifestFileSet item,
            ICollection<ManifestDataFile> namesValid,
            int dataManifestHash,
            int reqManifestHash,
            IFile dataManifest,
            IFile reqManifest)
        {
            ManifestFileSetState resultState;
            bool inserted;

            resultState = new ManifestFileSetState
            {
                AgentId = item.AgentId,
                ManifestPath = item.DataManifestPath,

                RequestManifestPath = item.RequestManifestPath,

                DataFileTags = namesValid.Select(o => o.Tag).ToList(),

                DataFileManifestCreateTime = dataManifest.Created,
                RequestManifestCreateTime = reqManifest.Created,
                FirstProcessingTime = this.clock.UtcNow,

                DataFileManifestHash = dataManifestHash,
                RequestManifestHash = reqManifestHash,

                Counter = 0,
            };

            // don't care if this fails non-fatally because the only non-fatal error we care about is that the row already
            //  exists (and we don't want to overwrite it in that case)
            inserted = await this.manifestFileSetState.InsertAsync(resultState).ConfigureAwait(false);
            if (inserted == false)
            {
                ManifestFileSetState currentState;

                this.TraceInfo("Data file manifest state object" + item.DataManifestTag + " already exists, so failed to insert");

                // update the counter property on the manifest as the cosmos monitor will use the last modified timestamp
                //  to determine if it should enqueue a new work item
                currentState = await this.manifestFileSetState
                    .GetItemAsync(resultState.PartitionKey, resultState.RowKey)
                    .ConfigureAwait(false);

                if (currentState != null)
                {
                    if (currentState.Counter >= 0)
                    {
                        currentState.Counter++;
                        // this is really just an optimization, so do not really care if it succeeds or not
                        await this.manifestFileSetState.ReplaceAsync(currentState);
                        resultState = currentState;
                    }
                    else
                    {
                        this.TraceInfo("Existing manifestFilestate was found with Counter < 0, replacing it");
                        await this.manifestFileSetState.ReplaceAsync(resultState);
                    }
                }
                else
                {
                    this.TraceInfo("Failed to fetch the result state");
                    resultState = currentState;
                }
            }

            return (resultState, inserted);
        }

        /// <summary>
        ///     Computes the pending file list from the valid file list
        /// </summary>
        /// <param name="ctx">operation context</param>
        /// <param name="tracker">file progress tracker</param>
        /// <param name="state">manifest file state</param>
        /// <param name="namesValid">names valid</param>
        /// <param name="dataManifestHash">data manifest hash</param>
        /// <param name="reqManifestHash">request manifest hash</param>
        /// <param name="dataManifest">data manifest</param>
        /// <param name="reqManifest">request manifest</param>
        /// <returns>resulting value</returns>
        private bool AreStateAndMainfestsConsistent(
            OperationContext ctx,
            IFileProgressTracker tracker,
            ManifestFileSetState state,
            ICollection<ManifestDataFile> namesValid,
            int dataManifestHash,
            int reqManifestHash,
            IFile dataManifest,
            IFile reqManifest)
        {
            const string ExtraDataManifestFmt =
                "  The {0} valid file names listed in the current manifest will be merged with the {1} names recorded " +
                "when the original data file manifest was first processed";

            const string ExtraReqManifest =
                "  Commands may not be marked as complete or extra commands may be marked complete for which data may " +
                "not have been copied to the export package.";

            string dateProcessed = null;
            bool result = true;

            void CheckManifestTimeConsistency(
                IFile file,
                DateTimeOffset? expectedDate,
                int? expectedHash,
                DateTimeOffset foundDate,
                int foundHash,
                string type,
                string extra)
            {
                bool hasError = expectedHash.HasValue ?
                    expectedHash.Value != foundHash :
                    expectedDate.HasValue && expectedDate.Value != foundDate;

                if (hasError)
                {
                    const string ChangedManifestMessageFmt =
                        "When the batch was first processed at {0}, the {1} manfiest had a create time of {2} (hash: {3}), but " +
                        "it currently has a create time of {4} (hash: {5}). This indicates the file was changed after being " +
                        "written, which is not a supported operation.{6}";

                    const string ChangedManifestTraceFmt =
                        "Batch {0} {1} manifest first processed at {1}: create time then was {2} (hash: {3}, but current create time is {3}";

                    string currentHash = foundHash.ToStringInvariant();
                    string currentTime = foundDate.ToString("yyyy-MM-dd HH:mm:ss");
                    string origHash = expectedHash?.ToStringInvariant() ?? "<not recorded>";
                    string origTime = expectedDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "<not recorded>";

                    dateProcessed =
                        dateProcessed ??
                        state.FirstProcessingTime?.ToString("yyyy-MM-dd HH:mm:ss") ??
                        "<not recorded>";

                    tracker.AddMessage(
                        TrackerTypes.BatchDataFileNames,
                        ChangedManifestMessageFmt,
                        dateProcessed,
                        type,
                        origTime,
                        origHash,
                        currentTime,
                        currentHash,
                        extra);

                    this.TraceError(ChangedManifestTraceFmt, ctx.Item, type, dateProcessed, origTime, currentTime);

                    this.LogEventError(
                        ctx,
                        new BatchChangedEvent
                        {
                            OriginalTime = origTime,
                            OriginalHash = origHash,
                            CurrentTime = currentTime,
                            CurrentHash = currentHash,
                            FileName = file.Name,
                            AgentId = state.AgentId,
                        });

                    result = false;
                }
            }

            CheckManifestTimeConsistency(
                reqManifest,
                state.RequestManifestCreateTime,
                state.RequestManifestHash,
                reqManifest.Created,
                reqManifestHash,
                "request",
                ExtraReqManifest);

            CheckManifestTimeConsistency(
                dataManifest,
                state.DataFileManifestCreateTime,
                state.DataFileManifestHash,
                dataManifest.Created,
                dataManifestHash,
                "data file",
                ExtraDataManifestFmt.FormatInvariant(namesValid.Count, state.DataFileTags?.Count ?? 0));

            return result;
        }

        /// <summary>
        ///     Computes the pending file list from the valid file list
        /// </summary>
        /// <param name="tracker">file progress tracker</param>
        /// <param name="state">manifest file state</param>
        /// <param name="inserted">true if the state object was from an insert; false if it was from an update</param>
        /// <param name="namesValid">collection of valid data file names</param>
        /// <returns>
        ///     a value indicating whether processing should continue (based on state / manifest consistency)
        ///     list of pending files if processing should continue,
        ///     a value indicating whether file data should be logged to the tracker log (based on whether we've processed
        ///      at least one data file and removed it from state)
        /// </returns>
        private (bool IsConsistent, ICollection<ManifestDataFile> Pending, bool LogFilesToTracking) ComputePendingFileList(
            IFileProgressTracker tracker,
            ManifestFileSetState state,
            bool inserted,
            ICollection<ManifestDataFile> namesValid)
        {
            ICollection<ManifestDataFile> pending = namesValid;

            if (inserted == false && state.DataFileTags != null)
            {
                // see if some files have already been removed from the existing manifest state. If so, no need to enqueue
                //  them again.
                pending = namesValid
                    .Where(o => state.DataFileTags.Contains(o.Tag, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                // this list of data files in the manifest state object must be a subset or equivalent set to the the list
                //  of data files in the manifest, so if all is good, the intersection of the two must have the same number
                //  of items as the pending list (the manifest reader should dedupe, so the list from the reader should be
                //  a set of distinct items)
                // however, when someone overwrites the manifest with a new manifest that has a different set of files listed
                //  (has happened, hence this hack- sigh), this will not match.  In that case, need to rebuild the filename 
                //  list from the list of pending items and toss the list we got from the manifest. Sigh again.
                if (pending.Count != state.DataFileTags.Count)
                {
                    const string Fmt =
                        "The data file manifest likely changed since it was first processed as the set of pending data " +
                        "files lists {0} files that are not present in the current manifest. Ignoring manifest files in " +
                        "favor of the pending set";

                    string msg = Fmt.FormatInvariant(state.DataFileTags.Count - pending.Count);

                    tracker.AddMessage(TrackerTypes.BatchDataFileNames, msg);
                    this.TraceError(msg);

                    // TODO: if we get here, file incident and/or log event

                    return (false, null, true);
                }
            }

            // if we've started processing data files (i.e. we have more data files in the manifest than pending) AND the manifest 
            //  hasn't changed, skip emitting tracker data. We must have emitted all the file data and enqueue info at least once 
            //  in order to have progressed to the point where we have processed data files, so we're just skipping redundant info
            //  and not losing data

            return (true, pending, pending.Count == namesValid.Count);
        }

        /// <summary>
        ///     Reports the missing commands
        /// </summary>
        /// <param name="tracker">file progress tracker</param>
        /// <param name="dataFiles">collection of data files to report on</param>
        private void ReportOnFilesFound(
            IFileProgressTracker tracker,
            IEnumerable<ManifestDataFile> dataFiles)
        {
            foreach (ManifestDataFile file in dataFiles)
            {
                const string Fmt =
                    "Data file [{0}] {1}. Cosmos name is expected to be [{2}] and output package filename will " +
                    "be [{3}].";

                string source = file.Source == DataFileSource.ManifestFile ?
                    "found in data file manifest" :
                    "NOT found in current data file manifest, but was found in previous processing of data file manifest";
                    
                string msg = Fmt.FormatInvariant(file.RawName, source, file.CosmosName, file.PackageName);

                if (file.CountFound > 1)
                {
                    msg += $" The file was listed {file.CountFound} times and it should have been listed only once.";
                }

                if (file.Invalid)
                {
                    msg += " The file name was found to be invalid and so the data file will be skipped.";
                }

                tracker?.AddMessage(TrackerTypes.BatchDataFileNames, msg);
            }
        }

        /// <summary>
        ///     Processes the work item (while owning the work item)
        /// </summary>
        /// <param name="ctx">operation context</param>
        /// <param name="tracker">file progress tracker</param>
        /// <param name="renewer">lease renewer</param>
        /// <param name="item">queue work item</param>
        /// <returns>resulting value</returns>
        private async Task<bool> ProcessRequestAsync(
            OperationContext ctx,
            IFileProgressTracker tracker,
            ILeaseRenewer renewer,
            ManifestFileSet item)
        {
            ICollection<ManifestDataFile> dataFilesPending;
            ICollection<ManifestDataFile> dataFilesValid;
            ICollection<ManifestDataFile> dataFilesAll;
            ManifestFileSetState state;
            ICollection<string> manifestCmds;
            RequestCommandsInfo cmdInfo;
            FileCommandLog statsLog = null;
            IFileSystem fileSystem;
            Task<IFile> dataManifestTask;
            Task<IFile> reqManifestTask;
            TimeSpan ageBatch;
            IFile dataManifest;
            IFile reqManifest;
            long manifestCmdRowCount;
            bool continueIfMissingCmds;
            bool logFilesToTracking;
            bool continueProcessing;
            bool inserted;
            int dataManifestHash;
            int reqManifestHash;

            this.CancelToken.ThrowIfCancellationRequested();

            ctx.Op = "fetching file system";

            fileSystem = this.fileSystemManager.GetFileSystem(item.CosmosTag);

            ctx.Op = $"Opening data file ({item.DataManifestTag}) and request ({item.RequestManifestTag}) manifests";

            await Task
                .WhenAll(
                    dataManifestTask = fileSystem.OpenExistingFileAsync(item.DataManifestPath),
                    reqManifestTask = fileSystem.OpenExistingFileAsync(item.RequestManifestPath))
                .ConfigureAwait(false);

            dataManifest = dataManifestTask.Result;
            reqManifest = reqManifestTask.Result;

            if (reqManifest == null || dataManifest == null)
            {
                return true;
            }

            this.CancelToken.ThrowIfCancellationRequested();

            ctx.Op = "Fetching data files from data file manifest";

            tracker.AddMessage(
                TrackerTypes.GeneralInfo,
                "Reading request manifest file " + item.RequestManifestPath);

            dataFilesAll = await this.ReadDataFileManifestAsync(ctx, item, dataManifest).ConfigureAwait(false);
            if (dataFilesAll == null)
            {
                // failed to read file
                return true;
            }

            dataFilesValid = dataFilesAll.Where(o => o.Invalid == false).ToList();

            this.CancelToken.ThrowIfCancellationRequested();

            ctx.Op = "Reading command ids from request manifest";

            (manifestCmds, manifestCmdRowCount) = await RequestManifestReader
                .ExtractCommandIdsFromManifestFileAsync(
                    reqManifest,
                    renewer.RenewAsync,
                    this.Config.CommandReaderLeaseUpdateRowCount,
                    errMsg => this.TraceError(errMsg))
                .ConfigureAwait(false);

            await renewer.RenewAsync().ConfigureAwait(false);

            this.CancelToken.ThrowIfCancellationRequested();

            ctx.Op = "Adding data file manifest to or updating data file manifest in state store";

            dataManifestHash = Utility.GetHashCodeForUnorderedCollection(dataFilesValid.Select(o => o.RawName).ToList());
            reqManifestHash = Utility.GetHashCodeForUnorderedCollection(manifestCmds);

            (state, inserted) = await this
                .InsertOrUpdateManifestStateAsync(item, dataFilesValid, dataManifestHash, reqManifestHash, dataManifest, reqManifest)
                .ConfigureAwait(false);

            if(state == null)
            {
                // Insertion or update failed.
                // Insertion/update make http calls, there could be transient issues causing this failure
                this.TraceError($"Failed to insert/update manifestFileSet table record, will retry");
                return false;
            }

            ctx.Op = "Validating manifests match stored state";

            continueProcessing = this.AreStateAndMainfestsConsistent(
                ctx, tracker, state, dataFilesValid, dataManifestHash, reqManifestHash, dataManifest, reqManifest);
            if (continueProcessing == false)
            {
                return true;
            }

            ctx.Op = "Computing list of pending files";

            (continueProcessing, dataFilesPending, logFilesToTracking) = 
                this.ComputePendingFileList(tracker, state, inserted, dataFilesValid);

            if (continueProcessing == false)
            {
                return true;
            }

            if (logFilesToTracking == false)
            {
                tracker = null;
            }

            ageBatch = this.clock.UtcNow - reqManifest.Created;
            continueIfMissingCmds = ageBatch > this.maxCommandWait;

            this.CancelToken.ThrowIfCancellationRequested();

            ctx.Op = "Determining status of commands from internal state and command feed";

            cmdInfo = await this.requestCmdUtils
                .DetermineCommandStatusAsync(
                    ctx, 
                    item.AgentId, 
                    manifestCmds, 
                    renewer, 
                    this.CancelToken, 
                    continueIfMissingCmds == false)
                .ConfigureAwait(false);

            ctx.Op = "Reporting on manifest status";

            // only log to the stats log if we have a tracker for consistency
            if (tracker != null)
            {
                const string Fmt =
                     "Request manifest created at {0} containing {1:n0} commands (Hash: {2:x8}). " +
                     "Data manifest created at {3} containing {4:n0} files (Hash: {5:x8})";
                
                statsLog = new FileCommandLog(dataFilesAll.Count, cmdInfo.Commands);

                tracker.AddMessage(
                    TrackerTypes.GeneralInfo,
                    Fmt,
                    reqManifest.Created.ToString("yyyy-MM-dd HH:mm:ss"),
                    cmdInfo.CommandCount,
                    reqManifestHash,
                    dataManifest.Created.ToString("yyyy-MM-dd HH:mm:ss"),
                    dataFilesAll.Count,
                    dataManifestHash);
            }

            if (manifestCmds.Count != manifestCmdRowCount)
            {
                const string Fmt =
                    "Request manifest contained {0:n0} command ids but {1:n0} rows, which means duplicate command ids were " +
                    "found. A request manifest MUST NOT contain duplicate command ids";

                string msg = Fmt.FormatInvariant(
                    manifestCmds.Count.ToStringInvariant(),
                    manifestCmdRowCount.ToStringInvariant());

                tracker?.AddMessage(TrackerTypes.GeneralError, msg);
                this.TraceWarning(msg);
            }
            
            ctx.Op = "Logging command summary";

            await this.requestCmdUtils.ReportCommandSummaryAsync(
                ctx,
                tracker,
                this.fileSystemManager.ActivityLog,
                this.taskTracer,
                item.AgentId,
                cmdInfo,
                reqManifest.Name,
                ageBatch,
                this.maxCommandWait,
                continueIfMissingCmds);

            if ((cmdInfo.HasMissing && continueIfMissingCmds == false) || 
                cmdInfo.HasNotAvailable ||
                cmdInfo.HasUndetermined)
            {
                this.TraceInfo($"CmdInfo is unavailable, for AgentId: {item.AgentId}");
                return false;
            }

            this.CancelToken.ThrowIfCancellationRequested();

            ctx.Op = "Logging data file status to tracker";

            this.ReportOnFilesFound(tracker, dataFilesAll);

            if (dataFilesPending.Count > 0)
            {
                ctx.Op = $"Enqueueing {dataFilesPending.Count} data files for processing for {item.DataManifestTag}";

                this.TraceInfo(ctx.Op);
                
                await this
                    .EnqueueDataFileItems(
                        tracker, fileSystem, statsLog, item.AgentId, item.CosmosTag, dataManifest, dataFilesPending)
                    .ConfigureAwait(false);
            }
            else
            {
                ctx.Op = "Data file manifest " + item.DataManifestTag + " contained no valid data files. Completing batch.";

                this.TraceWarning(ctx.Op);

                tracker?.AddMessage(
                    TrackerTypes.BatchDataFiles,
                    "No valid data files listed in data file manifest, so no data to process. Completing batch.");

                await this.completeQueue.EnqueueAsync(
                    new CompleteDataFile
                    {
                        ManifestPath = item.DataManifestPath,
                        DataFilePath = string.Empty,
                        CosmosTag = item.CosmosTag,
                        AgentId = item.AgentId,
                    },
                    this.CancelToken);
            }

            ctx.Op = "Emitting counters to stats log";

            statsLog?.EmitCounters(this.GetCounter);

            return true;
        }

        private class FileCommandLog
        {
            private const string DataFileCounts = "Batch Data File Counts";
            private const string CommandCounts = "Batch Command Counts";

            private readonly IDictionary<CommandStatusCode, ICollection<string>> codeCmdMap;

            private readonly int totalFiles;

            private int missingFiles;
            private int validFiles;

            public FileCommandLog(
                int totalFiles,
                IDictionary<CommandStatusCode, ICollection<string>> codeCmdMap)
            {
                this.totalFiles = totalFiles;
                this.codeCmdMap = codeCmdMap;
            }

            public void AddMissingDataFile()
            {
                Interlocked.Increment(ref this.missingFiles);
            }

            public void AddValidDataFile()
            {
                Interlocked.Increment(ref this.validFiles);
            }

            public void EmitCounters(Func<string, ICounter> counterFactory)
            {
                ICounter counter;
                ulong totalCommands = 0;

                counter = counterFactory(FileCommandLog.CommandCounts);

                foreach (KeyValuePair<CommandStatusCode, ICollection<string>> codeCmds in this.codeCmdMap)
                {
                    ulong count = Convert.ToUInt64(codeCmds.Value.Count);

                    counter.SetValue(count, FileCommandLog.GetValidityCodeText(codeCmds.Key));

                    totalCommands += count;
                }

                counter.SetValue(totalCommands);

                counter = counterFactory(FileCommandLog.DataFileCounts);
                counter.SetValue(Convert.ToUInt64(this.totalFiles));
                counter.SetValue(Convert.ToUInt64(this.missingFiles), "missing");
                counter.SetValue(Convert.ToUInt64(this.validFiles), "valid");
                counter.SetValue(
                    Convert.ToUInt64(Math.Max(0, this.totalFiles - (this.missingFiles + this.validFiles))), 
                    "invalid");
            }

            /// <summary>
            ///     Gets the text for the validity code
            /// </summary>
            /// <param name="code">code</param>
            /// <returns>resulting value</returns>
            private static string GetValidityCodeText(CommandStatusCode code)
            {
                // retain the original casing for valid and missing
                if (code == CommandStatusCode.Actionable)
                {
                    return "valid";
                }

                if (code == CommandStatusCode.Missing)
                {
                    return "missing";
                }

                return code.ToStringInvariant();
            }
        }
    }
}

// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Tasks
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem.Cosmos;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Data;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility.ManifestParsers;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility.ProgressTracker;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     FileCompleteProcessor class
    /// </summary>
    public class FileCompleteProcessor : TrackCountersBaseTask<IFileCompleteProcessorConfig>
    {
        private const string CompletedBatchCounter = "Batches Completed";

        private readonly IFileCompleteProcessorConfig config;
        private readonly ITable<ManifestFileSetState> manifestState;
        private readonly IFileProgressTrackerFactory trackerFactory;
        private readonly IQueue<CompleteDataFile> completeQueue;
        private readonly ITable<CommandState> commandState;
        private readonly IFileSystemManager fileSystemManager;
        private readonly TimeSpan leaseRenewFreq;
        private readonly TimeSpan holdingExpiry;
        private readonly TimeSpan leaseTime;
        private readonly IClock clock;
        private readonly string holdingPath;

        /// <summary>
        ///     Initializes a new instance of the FileCompleteProcessor class
        /// </summary>
        /// <param name="config">task config</param>
        /// <param name="manifestState">manifest state store</param>
        /// <param name="trackerFactory">file progress tracker factory</param>
        /// <param name="completeQueue">file set queue</param>
        /// <param name="commandState">command state store</param>
        /// <param name="fileSystemManager">file system manager</param>
        /// <param name="counterFactory">perf counter factory</param>
        /// <param name="logger">trace logger</param>
        /// <param name="clock">time of day clock</param>
        public FileCompleteProcessor(
            IFileCompleteProcessorConfig config,
            ITable<ManifestFileSetState> manifestState,
            IFileProgressTrackerFactory trackerFactory,
            IQueue<CompleteDataFile> completeQueue,
            ITable<CommandState> commandState,
            IFileSystemManager fileSystemManager,
            ICounterFactory counterFactory,
            ILogger logger,
            IClock clock) :
            base(config, counterFactory, logger)
        {
            this.fileSystemManager = fileSystemManager ?? throw new ArgumentNullException(nameof(fileSystemManager));
            this.trackerFactory = trackerFactory ?? throw new ArgumentNullException(nameof(trackerFactory));
            this.manifestState = manifestState ?? throw new ArgumentNullException(nameof(manifestState));
            this.completeQueue = completeQueue ?? throw new ArgumentNullException(nameof(completeQueue));
            this.commandState = commandState ?? throw new ArgumentNullException(nameof(commandState));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));

            this.leaseRenewFreq = TimeSpan.FromMinutes(config.MinimumRenewMinutes);
            this.holdingExpiry = TimeSpan.FromHours(fileSystemManager.CosmosPathsAndExpiryTimes.ManifestHoldingExpiryHours);
            this.leaseTime = TimeSpan.FromMinutes(config.LeaseMinutes);

            this.holdingPath =
                Utility.EnsureHasTrailingSlashButNoLeadingSlash(fileSystemManager.CosmosPathsAndExpiryTimes.PostProcessHolding);
        }

        /// <summary>
        ///     Runs the task
        /// </summary>
        /// <param name="ctx">operation context</param>
        /// <returns>resulting value</returns>
        protected override async Task<TimeSpan?> RunOnceAsync(OperationContext ctx)
        {
            IQueueItem<CompleteDataFile> item;
            IFileProgressTracker tracker;
            CompleteDataFile data;
            bool isComplete = false;

            // acquire a queue workItem

            this.CancelToken.ThrowIfCancellationRequested();

            ctx.Item = "none";
            ctx.Op = "dequeueing item";

            item = await this.completeQueue
                .DequeueAsync(this.leaseTime, Constants.DequeueTimeout, null, this.CancelToken)
                .ConfigureAwait(false);

            data = item?.Data;

            if (data == null)
            {
                // wait 10 before next attempt if we failed to dequeue
                return TimeSpan.FromSeconds(10);
            }

            ctx.Item = "[dataFile: " + data.DataFileTag + "]";

            this.TraceInfo($"File processor dequeued complete file {ctx.Item} for processing");

            tracker = this.trackerFactory.Create(
                data.AgentId,
                CosmosFileSystemUtility.SplitNameAndPath(data.ManifestPath).Name,
                this.TraceError);

            try
            {
                ILeaseRenewer renewer;

                renewer = new LeaseRenewer(
                    new List<Func<Task<bool>>> { () => item.RenewLeaseAsync(this.leaseTime) },
                    this.leaseRenewFreq,
                    this.clock,
                    s => this.TraceError(s),
                    ctx.Item);

                isComplete = await this.ProcessRequestAsync(ctx, tracker, renewer, data).ConfigureAwait(false);

                this.TraceVerbose(
                    isComplete ?
                        "Completed file completion processing for " + ctx.Item :
                        "Failed to process file completion file " + ctx.Item + ". Releasing for later retry.");

                this.CancelToken.ThrowIfCancellationRequested();
            }
            catch (Exception e)
            {
                string filler = string.IsNullOrWhiteSpace(data.DataFileTag) ? 
                    "processing completion of batch with empty data file manifest" : 
                    "removing [" + data.DataFileTag + "] data file from manifest's pending files";

                tracker.AddMessage(
                    TrackerTypes.GeneralError,
                    $"Unexpected error ({e.Message ?? string.Empty}) occurred while {filler}. Will retry later.",
                    data.DataFileTag);

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
                ctx.Op = "release queue item";

                await (isComplete ? item.CompleteAsync() : item.ReleaseAsync()).ConfigureAwait(false);

                ctx.Op = "persisting progress tracking data";

                await tracker.PersistAsync();

                ctx.PopOp();
            }

            return null;
        }

        /// <summary>
        ///     removes the data file from the manifest state
        /// </summary>
        /// <param name="ctx">operation context</param>
        /// <param name="tracker">file progress tracker</param>
        /// <param name="renewer">lease renewer</param>
        /// <param name="item">queue work workItem</param>
        /// <returns>resulting value</returns>
        private async Task<(ManifestFileSetState, bool)> RemoveDataFileFromManifestState(
            OperationContext ctx,
            IFileProgressTracker tracker,
            ILeaseRenewer renewer,
            CompleteDataFile item)
        {
            // setting these properties will automatically populate the partition and row keys with the escaped keys, so we don't
            //  have to figure out how to do it manually
            ManifestFileSetState stateQuery = new ManifestFileSetState { AgentId = item.AgentId, ManifestPath = item.ManifestPath };

            int pass;
            
            for (pass = 0; pass < this.config.MaxStateUpdateAttempts; ++pass)
            {
                Task<ManifestFileSetState> stateTask;
                ManifestFileSetState state;
                List<string> pendingItems;
                string dataFileName;
                string dataFileTag;

                this.CancelToken.ThrowIfCancellationRequested();

                ctx.Op = $"Fetching manifest state object (pass {pass})";

                await Task.WhenAll(
                    stateTask = this.manifestState.GetItemAsync(stateQuery.PartitionKey, stateQuery.RowKey),
                    renewer.RenewAsync()).ConfigureAwait(false);

                state = stateTask.Result;

                if (state == null)
                {
                    // if the state object doesn't exist, not much to do, so just exit.
                    this.TraceInfo("Manifest state object did not exist for work item " + ctx.Item);
                    return (null, true);
                }

                // this is a special case where the data file manifest contained no files. There would be nothing to update, so just
                //  return true
                if (string.IsNullOrWhiteSpace(item.DataFilePath))
                {
                    tracker.AddMessage(TrackerTypes.BatchDataFiles, "Marking empty batch as complete.");
                    this.TraceInfo("Marking empty batch as complete");
                    return (state, true);
                }

                (_, dataFileName) = CosmosFileSystemUtility.SplitNameAndPath(item.DataFilePath);
                dataFileTag = Utility.GenerateFileTag(item.CosmosTag, item.AgentId, dataFileName);

                pendingItems = state.DataFileTags.Where(o => dataFileTag.EqualsIgnoreCase(o) == false).ToList();
                if (pendingItems.Count == state.DataFileTags.Count)
                {
                    // if the state object's file list didn't change when we attempted to delete the current one, 
                    //  no need to update it
                    this.TraceInfo("Manifest state did not need to be updated for item " + ctx.Item);
                    return (state, true);
                }

                ctx.Op = $"Attempting to udpate manifest state (pass {pass})";

                state.DataFileTags = pendingItems;
                if (await this.manifestState.ReplaceAsync(state).ConfigureAwait(false))
                {
                    if (pendingItems.Count > 0)
                    {
                        const string Fmt =
                            "After marking data file [{0}] as complete, {1} data files are still pending completion. Waiting " +
                            "for those to finish.";

                        tracker.AddMessage(
                            TrackerTypes.BatchDataFiles, Fmt, item.DataFileTag, pendingItems.Count.ToStringInvariant());
                    }
                    else
                    {
                        tracker.AddMessage(
                            TrackerTypes.BatchDataFiles,
                            "After marking data file [" + item.DataFileTag + "] as complete, no data files are pending completion.");
                    }

                    return (state, true);
                }

                this.TraceInfo($"Could not update state for {ctx.Item} due to a conflict");
            }

            this.TraceInfo($"Failed to update state for {ctx.Item} after {pass - 1} passes. Abandoning.");

            return (null, false);
        }

        /// <summary>
        ///     Fetches the command state
        /// </summary>
        /// <param name="agentId">agent id</param>
        /// <param name="batchIds">batch ids</param>
        /// <returns>command state for those items that could be fetched and the list of ids that could not be fetched</returns>
        private async Task<(ICollection<CommandState>, ICollection<string>)> FetchCommandState(
            string agentId,
            ICollection<string> batchIds)
        {
            const string RowKeyFilterFmt = "RowKey eq '{0}'";
            const string QueryFmt = "PartitionKey eq '{0}' and ({1})";

            ICollection<CommandState> result;

            string query = string.Format(
                QueryFmt,
                agentId,
                string.Join(" or ", batchIds.Select(o => string.Format(RowKeyFilterFmt, o))));

            // no reason not to do the query and manifest manifestLease renewal in parallel
            result = await this.commandState.QueryAsync(query).ConfigureAwait(false);
            
            return 
            (
                // can skip any already marked as complete
                result, 
                batchIds.Except(result.Select(o => o.RowKey)).ToList()
            );
        }

        /// <summary>
        ///     removes the data file from the manifest state
        /// </summary>
        /// <param name="ctx">operation context</param>
        /// <param name="agentId">agent id</param>
        /// <param name="batchIds">batch ids</param>
        /// <param name="cmdLists">command lists</param>
        /// <returns>list of items that existed and could not be marked as complete</returns>
        private async Task<ICollection<string>> MarkCommandBatchAsComplete(
            OperationContext ctx,
            string agentId,
            CommandLists cmdLists,
            IEnumerable<string> batchIds)
        {
            HashSet<string> idsToProcess = new HashSet<string>(batchIds, StringComparer.OrdinalIgnoreCase);
            int pass;

            for (pass = 0; pass < this.config.MaxStateUpdateAttempts && idsToProcess.Count > 0; ++pass)
            {
                ICollection<CommandState> stateSet;
                ICollection<string> notFoundIds;
                List<CommandState> stateToUpdate = null;

                this.CancelToken.ThrowIfCancellationRequested();

                ctx.Op = $"Fetching batch of {idsToProcess.Count} command state objects to mark as complete";

                (stateSet, notFoundIds) = await this.FetchCommandState(agentId, idsToProcess).ConfigureAwait(false);

                // remove all the not found ids from the allIds collection
                foreach (string id in notFoundIds)
                {
                    idsToProcess.Remove(id);
                    cmdLists.NotFound.Add(id);
                }

                foreach (CommandState state in stateSet)
                {
                    if (state.IsComplete && state.IgnoreCommand == false)
                    {
                        idsToProcess.Remove(state.CommandId);
                    }
                    else
                    {
                        state.IsComplete = true;

                        stateToUpdate = stateToUpdate ?? new List<CommandState>();
                        stateToUpdate.Add(state);
                    }
                }

                if (stateToUpdate != null && stateToUpdate.Count > 0)
                {
                    ctx.Op = $"Updating {stateToUpdate.Count} non-complete command state objects";

                    List<Task<(string CommandId, bool IsSuccess)>> waiters = stateToUpdate
                        .Select(
                            async state =>
                            {
                                try
                                {
                                    if (state.IgnoreCommand)
                                    {
                                        await this.commandState.DeleteItemAsync(state).ConfigureAwait(false);
                                    }
                                    else
                                    {
                                        bool updated = await this.commandState.ReplaceAsync(state).ConfigureAwait(false);
                                        if (updated == false)
                                        {
                                            const string Fmt =
                                                "[agentId: [0][command: {1}][pass: {2}] Attempt to mark command complete failed " + 
                                                "due to precondition failure error: likely command was modified since we first " + 
                                                "read it. Will retry if any retries remain";

                                            this.TraceWarning(Fmt, state.AgentId, state.CommandId, pass.ToStringInvariant());

                                            cmdLists.Retried.Add(state.CommandId);

                                            return (state.CommandId, false);
                                        }
                                    }

                                    (state.IgnoreCommand ? cmdLists.Removed : cmdLists.Completed).Add(state.CommandId);

                                    return (state.CommandId, true);
                                }
                                catch (Exception e)
                                {
                                    this.TraceError(
                                        "[agent: {0}][command: {1}] Failed to {2} command state: {3}",
                                        state.AgentId,
                                        state.CommandId,
                                        state.IgnoreCommand ? "delete test" : "update",
                                        e);
                                    throw;
                                }
                            })
                        .ToList();

                    await Task.WhenAll(waiters).ConfigureAwait(false);

                    foreach (
                        string id in 
                        waiters
                            .Where(o => o.IsCompleted && o.Result.IsSuccess)
                            .Select(o => o.Result.CommandId))
                    {
                        idsToProcess.Remove(id);
                    }
                }

                if (stateToUpdate == null || stateToUpdate.Count == 0 || idsToProcess.Count == 0)
                {
                    return idsToProcess;
                }

                this.TraceInfo(
                    $"After pass {pass}, {idsToProcess.Count} command state items remain to be udpated for {ctx.Item}.");
            }

            if (idsToProcess.Count > 0)
            {
                this.TraceInfo(
                    $"Failed to update {idsToProcess.Count} command state items for {ctx.Item} after {pass - 1} passes. Abandoning.");
            }

            return idsToProcess;
        }

        /// <summary>
        ///      Waits for the tasks to finish, renewing the lease while waiting
        /// </summary>
        /// <param name="ctx">operation context</param>
        /// <param name="markCompleteTask">primary task to wait on</param>
        /// <param name="renewer">lease renewer</param>
        /// <returns>resulting value</returns>
        private async Task<bool> WaitAndRenewLease(
            OperationContext ctx,
            Task<ICollection<string>> markCompleteTask,
            ILeaseRenewer renewer)
        {
            // while the completion tasks are in progress, renew the lease every this.config.MinimumRenewMinutes to ensure we 
            //  retain ownership while dumping all this data.
            // Note that we deliberately ignore cancel signals within this part of the method to avoid having to reprocess
            //  a stream after completing nearly all of it
            for (;;)
            {
                Task renewTask = renewer.RenewAsync();

                // if writer tasks have completed in some way, wait for the renewer task to finish and bail
                if (markCompleteTask.IsCompleted || markCompleteTask.IsCanceled || markCompleteTask.IsFaulted)
                {
                    await renewTask.ConfigureAwait(false);
                    break;
                }

                await Task.WhenAny(
                    markCompleteTask,
                    Task.Delay(this.leaseRenewFreq, this.CancelToken)).ConfigureAwait(false);

                if (markCompleteTask.IsCompleted || markCompleteTask.IsCanceled || markCompleteTask.IsFaulted)
                {
                    break;
                }
            }

            // if we get any command ids back from the mark complete task, then 
            if (markCompleteTask.Result.Count != 0)
            {
                string nonComplete = string.Join(", ", markCompleteTask.Result);
                this.TraceVerbose($"Unable to mark commands as complete for item [{ctx.Item}]. Commands: [{nonComplete}]");
                return false;
            }

            return true;
        }

        /// <summary>
        ///     removes the data file from the manifest state
        /// </summary>
        /// <param name="ctx">operation context</param>
        /// <param name="renewer">lease renewer</param>
        /// <param name="item">queue work workItem</param>
        /// <param name="commandIds">command ids</param>
        /// <param name="cmdLists">command lists</param>
        /// <returns>resulting value</returns>
        private async Task<bool> MarkCommandsAsComplete(
            OperationContext ctx,
            ILeaseRenewer renewer,
            CompleteDataFile item,
            ICollection<string> commandIds,
            CommandLists cmdLists)
        {
            const int BatchSize = 10;

            bool finalResult = true;
            
            for (int start = 0; start < commandIds.Count; start += BatchSize)
            {
                async Task<ICollection<string>> UpdateBatch(
                    OperationContext outerCtx,
                    int index)
                {
                    OperationContext localCtx = new OperationContext(outerCtx.TaskId, ctx.WorkerIndex) { Item = outerCtx.Item };

                    try
                    {
                        return await this.MarkCommandBatchAsComplete(
                            localCtx,
                            item.AgentId,
                            cmdLists,
                            commandIds.Skip(index).Take(BatchSize)).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        this.TraceError(
                            "Failed to update command id batch ({0}-{1}) for item {2} while {3}: {4}",
                            index,
                            index + BatchSize - 1,
                            localCtx.Op,
                            localCtx.Item,
                            e);
                        throw;
                    }
                }

                finalResult = 
                    await this.WaitAndRenewLease(ctx, UpdateBatch(ctx, start), renewer) && 
                    finalResult;
            }

            return finalResult;
        }

        /// <summary>
        ///     Processes the work work item (while owning the work work item)
        /// </summary>
        /// <param name="ctx">operation context</param>
        /// <param name="tracker">file progress tracker</param>
        /// <param name="renewer">lease renewer</param>
        /// <param name="item">queue work workItem</param>
        /// <returns>resulting value</returns>
        private async Task<bool> ProcessRequestAsync(
            OperationContext ctx,
            IFileProgressTracker tracker,
            ILeaseRenewer renewer,
            CompleteDataFile item)
        {
            ManifestFileSetState state;
            IFileSystem fileSystem;
            string renamePath = this.holdingPath + Utility.EnsureTrailingSlash(item.AgentId);
            IFile requestManfiest;
            IFile dataManfiest;
            bool shouldComplete;

            (state, shouldComplete) = await this.RemoveDataFileFromManifestState(ctx, tracker, renewer, item).ConfigureAwait(false);

            // if we have no state object, everything else should have been deleted (because deleting the state object is the last 
            //  thing we do), so can exit
            // if we still have pending data files, then it's not time for the rest of the cleanup pieces, so also exit
            if (state == null || state.DataFileTags.Count > 0)
            {
                if (state != null)
                {
                    this.TraceInfo(
                        "Removed {0} from manfiest pending files for manifest {1}, but {2} data files remain pending",
                        ctx.Item,
                        item.ManifestTag,
                        state.DataFileTags.Count);
                }

                return shouldComplete;
            }

            this.TraceInfo(
                "No data files remain pending for manifest {0} after removing {1}. Beginning completion of manifest set.",
                item.ManifestTag,
                ctx.Item);

            ctx.Op = "fetching file system";
            fileSystem = this.fileSystemManager.GetFileSystem(item.CosmosTag);

            ctx.Op = "fetching request manifest file";
            requestManfiest = await fileSystem.OpenExistingFileAsync(state.RequestManifestPath).ConfigureAwait(false);
            if (requestManfiest != null)
            {
                CommandLists cmdLists = new CommandLists();
                bool result;

                ctx.Op = "extracting command ids from request manfiest file";

                (ICollection<string> commandIds, _) =
                    await RequestManifestReader.
                        ExtractCommandIdsFromManifestFileAsync(
                            requestManfiest,
                            renewer.RenewAsync,
                            this.Config.CommandReaderLeaseUpdateRowCount,
                            s => this.TraceError(s))
                        .ConfigureAwait(false);

                result = await this.MarkCommandsAsComplete(ctx, renewer, item, commandIds, cmdLists).ConfigureAwait(false);

                tracker.AddMessage(TrackerTypes.BatchCommands, $"Completed commands: [{string.Join(",", cmdLists.Completed)}]");
                tracker.AddMessage(TrackerTypes.BatchCommands, $"NotFound commands: [{string.Join(",", cmdLists.NotFound)}]");
                tracker.AddMessage(TrackerTypes.BatchCommands, $"Removed commands: [{string.Join(",", cmdLists.Removed)}]");

                if (result == false)
                {
                    return false;
                }

                ctx.Op = "moving request manifest file to holding";
                await requestManfiest.MoveRelativeAsync(renamePath, true, true).ConfigureAwait(false);

                ctx.Op = "setting holding request manifest file expiry";
                await requestManfiest.SetLifetimeAsync(this.holdingExpiry, true).ConfigureAwait(false);
            }
            else
            {
                this.TraceWarning(
                    "Unable to find request manifest [" + state.RequestManifestPath + "] so cannot mark commands as complete");
            }

            ctx.Op = "fetching data manifest file";
            dataManfiest = await fileSystem.OpenExistingFileAsync(state.ManifestPath).ConfigureAwait(false);
            if (dataManfiest != null)
            {
                ctx.Op = "moving data manifest file to holding";
                await dataManfiest.MoveRelativeAsync(renamePath, true, true).ConfigureAwait(false);

                ctx.Op = "setting holding data manifest file expiry";
                await dataManfiest.SetLifetimeAsync(this.holdingExpiry, true).ConfigureAwait(false);
            }
            else
            {
                this.TraceWarning(
                    "Unable to find data manifest [" + state.ManifestPath + "] so cannot archive data manifest");
            }

            ctx.Op = "deleting manifest state from table";

            // force deletion of the row even if it changed
            state.ETag = "*";
            if (await this.manifestState.DeleteItemAsync(state).ConfigureAwait(false))
            {
                // only set the counter to indicate a completed batch if we're the one that deleted the table row
                this.GetCounter(FileCompleteProcessor.CompletedBatchCounter).Increment();
            }

            tracker.AddMessage(TrackerTypes.BatchComplete, "Processing of this Cosmos export batch has completed");

            return true;
        }

        /// <summary>
        ///     A list of commands and their states
        /// </summary>
        private class CommandLists
        {
            /// <summary>
            ///     Gets the set of commands marked as completed
            /// </summary>
            public ConcurrentBag<string> Completed { get; } = new ConcurrentBag<string>();

            /// <summary>
            ///     Gets the set of commands not found in the state store
            /// </summary>
            public ConcurrentBag<string> NotFound { get; } = new ConcurrentBag<string>();

            /// <summary>
            ///     Gets the set of commands removed from the store during completion (because they are test commands aqnd should)
            ///      be ignored
            /// </summary>
            public ConcurrentBag<string> Removed { get; } = new ConcurrentBag<string>();

            /// <summary>
            ///     Gets the set of commands removed from the store during completion (because they are test commands aqnd should)
            ///      be ignored
            /// </summary>
            public ConcurrentBag<string> Retried { get; } = new ConcurrentBag<string>();
        }
    }
}
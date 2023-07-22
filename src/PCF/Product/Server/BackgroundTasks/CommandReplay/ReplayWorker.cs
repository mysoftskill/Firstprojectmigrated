namespace Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandReplay;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache;
    using Microsoft.PrivacyServices.CommandFeed.Validator;
    using Microsoft.PrivacyServices.Common.Azure;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// ReplayWorker is the core component of the self-serve replay feature.
    /// High-level flows for the replay worker are as below:
    /// 1. Wakes up and tries to get a replay job from the command replay repo.
    /// 2. If no available job, goes back to sleep, other wise start processing.
    /// 3. Replay job contains the target replay date, last completed hour and continuation token for cold storage from the last query.
    ///    Base on those information, build the query to get next 1000 commands from cold storage.
    /// 4. Then for each of the command, check the verifier, filtering, variants for each requested Agent/AssetGroup pair.
    /// 5. Batch the actionable destinations, which is a list of Commands with its list of applied Agent/AssetGroups pair
    ///    and publish them as Azure work items.
    /// 6. Once those commands got published, update the replay job with the new tokens and lease time.
    /// 7. If the whole day of the job is done, mark the job as completed.
    /// </summary>
    public class ReplayWorker
    {
        private readonly IDataAgentMapFactory dataAgentMapFactory;
        private readonly ICommandReplayJobRepository replayJobRepo;
        private readonly ICommandHistoryRepository commandHistory;
        private readonly IValidationService validationService;
        private readonly IAzureWorkItemQueuePublisher<EnqueueBatchReplayCommandsWorkItem> publisher;

        private int EnqueueReplayCommandBatchSize => FlightingUtilities.IsEnabled(FlightingNames.CommandReplayReduceEnqueueReplayCommandBatchSize) ? 10 : 50;

        public ReplayWorker(
            IDataAgentMapFactory dataAgentMapFactory,
            ICommandReplayJobRepository replayJobRepo,
            ICommandHistoryRepository commandHistory,
            IValidationService validationService,
            IAzureWorkItemQueuePublisher<EnqueueBatchReplayCommandsWorkItem> publisher)
        {
            this.dataAgentMapFactory = dataAgentMapFactory;
            this.replayJobRepo = replayJobRepo;
            this.validationService = validationService;
            this.publisher = publisher;
            this.commandHistory = commandHistory;
        }

        public async Task BeginProcessAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (FlightingUtilities.IsEnabled(FlightingNames.CommandReplayWorkerDisabled))
                    {
                        DualLogger.Instance.Information(nameof(ReplayWorker), $"Replay worker is disabled by flight. Sleep 10min and try again.");
                        await Task.Delay(TimeSpan.FromMinutes(10), token);
                        continue;
                    }

                    int delaySeconds = RandomHelper.Next(
                        Config.Instance.CommandReplay.MinReplayWorkerSleepSeconds, 
                        Config.Instance.CommandReplay.MaxReplayWorkerSleepSeconds);

                    DualLogger.Instance.Information(nameof(ReplayWorker), $"Sleeping for {delaySeconds} seconds");
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), token);

                    DualLogger.Instance.Information(nameof(ReplayWorker), $"Sleep over, starting the work");
                    await Logger.InstrumentAsync(
                        new IncomingEvent(SourceLocation.Here()),
                        ev => this.RunAsync(ev, token));
                }
                catch (Exception ex)
                {
                    Logger.Instance?.UnexpectedException(ex);
                }
            }
        }

        /// <summary>
        /// Runs the work item once and returns.
        /// </summary>
        public Task RunOnceAsync(int noJobDelaySeconds)
        {
            return this.RunAsync(IncomingEvent.Current ?? new IncomingEvent(SourceLocation.Here()), new CancellationTokenSource().Token, noJobDelaySeconds);
        }

        private async Task RunAsync(IncomingEvent ev, CancellationToken token, int noJobDelaySeconds = 600)
        {
            ev.OperationName = "ReplayWorker.BeginProcessAsync";

            // Worker randomly pick up a replay job
            ReplayJobDocument replayJob = await this.replayJobRepo.PopNextItemAsync(TimeSpan.FromMinutes(10));

            if (replayJob == null)
            {
                DualLogger.Instance.Information(nameof(ReplayWorker), $"No available job... Sleep {noJobDelaySeconds} seconds and try again...");
                await Task.Delay(TimeSpan.FromSeconds(noJobDelaySeconds));
                ev["JobStatus"] = "NoAvailableJob";
                ev.StatusCode = HttpStatusCode.OK;
                return;
            }

            DualLogger.Instance.Information(nameof(ReplayWorker), $"Get one job. Start Processing... Date: {replayJob.ReplayDate.ToString()}");
            await this.StartJobProcessingAsync(replayJob, ev, token);
        }

        private async Task StartJobProcessingAsync(ReplayJobDocument replayJob, IncomingEvent ev, CancellationToken token)
        {
            var eTag = replayJob.ETag;

            // There seems like due to the Read Region propagation time, sometime single job can be fetched by another process 
            // So process will wait few seconds and test the etag before start the real replay work.
            ev["JobNvt"] = DateTimeOffset.FromUnixTimeSeconds(replayJob.UnixNextVisibleTimeSeconds).ToString();
            await Task.Delay(TimeSpan.FromSeconds(RandomHelper.Next(3, 10)));
            try
            {
                eTag = await this.replayJobRepo.ReplaceAsync(replayJob, eTag);
            }
            catch (CommandFeedException ex)
            {
                if (ex.ErrorCode == CommandFeedInternalErrorCode.Conflict)
                {
                    DualLogger.Instance.Information(nameof(ReplayWorker), $"Job {replayJob.ReplayDate} is updated by another process");
                    ev["JobStatus"] = "LostJobLease";
                    ev.StatusCode = HttpStatusCode.OK;
                    return;
                }

                throw;
            }

            bool isJobCompleted = false;
            List<IAssetGroupInfo> exportAssetGroupInfos = new List<IAssetGroupInfo>();

            // get a list of assetGropuInfos from the assetgroupIds
            List<IAssetGroupInfo> assetGroupInfos = this.GetAssetGroupInfo(replayJob.AssetGroupIds.ToList(), PrivacyCommandType.Delete, ev);
            if (replayJob.AssetGroupIdsForExportCommands != null)
            {
                exportAssetGroupInfos = this.GetAssetGroupInfo(replayJob.AssetGroupIdsForExportCommands.ToList(), PrivacyCommandType.Export, ev);
            }

            if (!assetGroupInfos.Any() && !exportAssetGroupInfos.Any())
            {
                ev["JobSubStatus"] = "NoValidAssetGroupInfo";
                isJobCompleted = true;
            }
            else
            {
                // start work on the job
                var replayStartTime = replayJob.LastCompletedHour?.AddHours(1) ?? replayJob.ReplayDate;
                var replayEndTime = replayStartTime.AddHours(1);
                var continuationToken = replayJob.ContinuationToken;
                var includeExportCommands = exportAssetGroupInfos != null && exportAssetGroupInfos.Any();

                DualLogger.Instance.Information(nameof(ReplayWorker), $"Targeting Hour: {replayStartTime}");
                ev["TargetHour"] = replayStartTime.ToString();

                while (!token.IsCancellationRequested)
                {
                    // query cold storage to return 1000 commands each time
                    var result = await this.commandHistory.GetCommandsForReplayAsync(replayStartTime, replayEndTime, replayJob.SubjectType, includeExportCommands, continuationToken);

                    var items = await this.GetReplayDestinationsAsync(result.pxsCommands, assetGroupInfos, exportAssetGroupInfos, ev);
                    if (items.Count == 0)
                    {
                        DualLogger.Instance.Information(nameof(ReplayWorker), $"Found 0 actionable command. [Hour: {replayStartTime}]");
                    }
                    else
                    {
                        DualLogger.Instance.Information(nameof(ReplayWorker), $"Found {items.Count} actionable commands. [Hour: {replayStartTime}]");
                        await this.PublishReplayItemsAsync(items);
                    }

                    // update ctoken then update record.
                    continuationToken = result.continuationToken;
                    if (continuationToken == null)
                    {
                        // this target hour is completed
                        DualLogger.Instance.Information(nameof(ReplayWorker), $"Job is done for target hour. [Hour: {replayStartTime}]");
                        break;
                    }
                    else
                    {
                        // extend lease
                        replayJob.UnixNextVisibleTimeSeconds = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds();
                        replayJob.ContinuationToken = continuationToken;
                        eTag = await this.replayJobRepo.ReplaceAsync(replayJob, eTag);

                        // Logic to slow down based on flighting, if necessary.
                        int[] delaySeconds = { 2, 1 };
                        foreach (var i in delaySeconds)
                        {
                            if (FlightingUtilities.IsIntegerValueEnabled(FlightingNames.CommandReplayWorkerDelaySeconds, i))
                            {
                                await Task.Delay(TimeSpan.FromSeconds(i));
                                break;
                            }
                        }
                    }
                }

                replayJob.ContinuationToken = null;
                replayJob.LastCompletedHour = replayStartTime;
                replayJob.UnixNextVisibleTimeSeconds = DateTimeOffset.UtcNow.AddSeconds(-1).ToUnixTimeSeconds();

                // check if replay job for this target date is completed
                if (replayStartTime.ToUniversalTime().Hour == 23)
                {
                    isJobCompleted = true;
                }
            }

            if (isJobCompleted)
            {
                DualLogger.Instance.Information(nameof(ReplayWorker), $"Job Done. Date: {replayJob.ReplayDate}");
                ev["JobStatus"] = "Completed";
                replayJob.IsCompleted = true;
                replayJob.UnixNextVisibleTimeSeconds = DateTimeOffset.MaxValue.ToUnixTimeSeconds();
                replayJob.CompletedTime = DateTimeOffset.UtcNow;
            }

            await this.replayJobRepo.ReplaceAsync(replayJob, eTag);

            ev.StatusCode = HttpStatusCode.OK;
        }

        private List<IAssetGroupInfo> GetAssetGroupInfo(
            List<AssetGroupId> assetGroupIds, PrivacyCommandType commandType, IncomingEvent ev)
        {
            ev[$"AssetGroupIdsFromJob_{commandType}"] = string.Join(",", assetGroupIds);

            IDataAgentMap dataAgentMap = this.dataAgentMapFactory.GetDataAgentMap();
            List<IAssetGroupInfo> assetGroupInfos = new List<IAssetGroupInfo>();
            List<AssetGroupId> notFoundAssetGroupIds = new List<AssetGroupId>();

            foreach (AssetGroupId assetGroupId in assetGroupIds)
            {
                bool found = false;
                foreach (AgentId agentId in dataAgentMap.GetAgentIds())
                {
                    IDataAgentInfo dataAgentInfo = dataAgentMap[agentId];
                    if (dataAgentInfo.TryGetAssetGroupInfo(assetGroupId, out IAssetGroupInfo assetGroupInfo))
                    {
                        // In PPE, by design it will always return HackedAssetGroupInfo if assetgroupId not found
                        if (assetGroupInfo.IsFakePreProdAssetGroup)
                        {
                            // drop the hacked asset group.
                            continue;
                        }

                        // One asset group can belong to up to 2 agents based on different Command Type
                        // Here is to find the agent that support the right commandType DSR for this asset group id
                        if (assetGroupInfo.SupportedCommandTypes.Contains(commandType))
                        {
                            assetGroupInfos.Add(assetGroupInfo);
                            found = true;
                            break;
                        }
                    }
                }

                if (!found)
                {
                    DualLogger.Instance.Information(nameof(ReplayWorker), $"Failed to find AssetGroupInfo for assetGroupId:{assetGroupId}");
                    notFoundAssetGroupIds.Add(assetGroupId);
                }
            }

            ev[$"AssetGroupsNotFound_{commandType}"] = string.Join(",", notFoundAssetGroupIds);

            return assetGroupInfos;
        }

        private async Task<List<ReplayCommandDestinationPair>> GetReplayDestinationsAsync(
            IEnumerable<JObject> pxsCommands, List<IAssetGroupInfo> assetGroupInfos, List<IAssetGroupInfo> exportAssetGroupInfos, IncomingEvent ev)
        {
            var items = new List<ReplayCommandDestinationPair>();
            int droppedBadVerifier = 0;
            int droppedNotApplicable = 0;

            foreach (var pxsCommand in pxsCommands)
            {
                var matchedAssetGroups = new List<(AgentId agentId, AssetGroupId assetGroupId)>();
                
                bool verifierChecked = false;
                PrivacyCommand pcfCommand = null;
                List<Policy.DataTypeId> originalDataTypes = null;
                
                var parser = PxsCommandParser.DummyParser;
                (pcfCommand, _) = parser.Process(pxsCommand);
                originalDataTypes = pcfCommand.DataTypeIds.ToList();

                // Choose queue storage destination
                pcfCommand.ChooseQueueStorageType();

                List<IAssetGroupInfo> aptAssetGroupInfos;
                if (pcfCommand is ExportCommand)
                {
                    aptAssetGroupInfos = exportAssetGroupInfos;
                }
                else
                {
                    aptAssetGroupInfos = assetGroupInfos;
                }

                foreach (var assetGroupInfo in aptAssetGroupInfos)
                {
                    // just repoint the existing command at the current asset group. This is much cheaper than
                    // reparsing from JSON.
                    pcfCommand.AgentId = assetGroupInfo.AgentId;
                    pcfCommand.AssetGroupId = assetGroupInfo.AssetGroupId;
                    pcfCommand.AssetGroupQualifier = assetGroupInfo.AssetGroupQualifier;
                    pcfCommand.DataTypeIds = originalDataTypes.ToList(); //vsc: Check if this is correct for export command

                    // 1. check the verifier if we haven't done so yet for this command.
                    //    for perf reasons, we only check the verifier once per command, not
                    //    once per asset group.
                    if (!verifierChecked)
                    {
                        verifierChecked = true;
                        bool isValid = await pcfCommand.IsVerifierValidAsync(this.validationService);
                        if (!isValid)
                        {
                            droppedBadVerifier++;
                            ev["DroppedBadVerifier"] = droppedBadVerifier.ToString();
                            break;
                        }
                    }

                    // 2. apply filter logic here
                    if (assetGroupInfo.IsCommandActionable(pcfCommand, out var applicabilityResult))
                    {
                        matchedAssetGroups.Add((assetGroupInfo.AgentId, assetGroupInfo.AssetGroupId));
                    }
                    else
                    {
                        droppedNotApplicable++;
                        ev["DroppedNotApplicable"] = droppedNotApplicable.ToString();
                    }
                }

                if (matchedAssetGroups.Count > 0)
                {
                    items.Add(new ReplayCommandDestinationPair(pxsCommand, matchedAssetGroups));
                }
            }

            return items;
        }

        private async Task PublishReplayItemsAsync(List<ReplayCommandDestinationPair> replayItems)
        {
            // split into groups and insert into Azure Queue items
            var batchNumber = 0;
            var totalBatchesToPublish = Math.Ceiling((double)(replayItems.Count / this.EnqueueReplayCommandBatchSize));
            
            // Set batches to be visible on the queue every fixed interval, so that they're staggered in time and not processed at the same time. 
            // The total period to stagger over is set arbitrarily to 6 hours.
            var visibilityInterval = totalBatchesToPublish > 0 && FlightingUtilities.IsEnabled(FlightingNames.CommandReplayStaggerEnqueueReplayCommandBatches)
                ? (6 * 60) / totalBatchesToPublish
                : 0;

            var batchItems = new List<ReplayCommandDestinationPair>();

            foreach (var item in replayItems)
            {
                batchItems.Add(item);

                if (batchItems.Count == this.EnqueueReplayCommandBatchSize)
                {
                    DualLogger.Instance.Information(nameof(ReplayWorker), $"Publishing {batchItems.Count} Command-AssetGroups Pair items");
                                        
                    await this.publisher.PublishWithSplitAsync(
                        batchItems,
                        batch =>
                        {
                            return new EnqueueBatchReplayCommandsWorkItem
                            {
                                ReplayCommandsBatch = batch.ToList(),
                            };
                        },
                        x => TimeSpan.FromMinutes(visibilityInterval * batchNumber));

                    batchItems = new List<ReplayCommandDestinationPair>();
                    batchNumber++;
                }
            }

            if (batchItems.Count > 0)
            {
                DualLogger.Instance.Information(nameof(ReplayWorker), $"Publishing {batchItems.Count} Command/AssetGroup Map items");
                await this.publisher.PublishWithSplitAsync(
                    batchItems,
                    batch =>
                    {
                        return new EnqueueBatchReplayCommandsWorkItem
                        {
                            ReplayCommandsBatch = batch.ToList(),
                            IsApplicabilityVerified = true,
                        };
                    },
                    x => TimeSpan.FromMinutes(visibilityInterval * batchNumber));
            }
        }
    }
}
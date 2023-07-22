namespace Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue.QueueStorageCommandQueue;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Common.Azure;

    using PXSV1 = PXS.Command.Contracts.V1;

    public class EnqueueBatchReplayCommandsWorkItem
    {
        /// <summary>
        /// A list of commands and target destinations that needs to be enqueued.
        /// </summary>
        public List<ReplayCommandDestinationPair> ReplayCommandsBatch { get; set; }

        /// <summary>
        /// True if the commands and destination is filtered already.
        /// Replay by command Ids is not filtered.
        /// </summary>
        public bool? IsApplicabilityVerified { get; set; }
    }

    public class EnqueueBatchReplayCommandsWorkItemHandler : IAzureWorkItemQueueHandler<EnqueueBatchReplayCommandsWorkItem>
    {
        private readonly IDataAgentMapFactory dataAgentMapFactory;
        private readonly ICommandLifecycleEventPublisher publisher;
        private readonly ICommandQueueFactory queueFactory;
        private readonly AzureQueueStorageContext azureQueueStorageCommandContext;

        public EnqueueBatchReplayCommandsWorkItemHandler(
            IDataAgentMapFactory dataAgentMapFactory,
            ICommandQueueFactory queueFactory,
            ICommandLifecycleEventPublisher publisher,
            AzureQueueStorageContext azureQueueStorageCommandContext)
        {
            this.dataAgentMapFactory = dataAgentMapFactory;
            this.queueFactory = queueFactory;
            this.publisher = publisher;
            this.azureQueueStorageCommandContext = azureQueueStorageCommandContext;
        }

        public SemaphorePriority WorkItemPriority => SemaphorePriority.Background;

        public async Task<QueueProcessResult> ProcessWorkItemAsync(QueueWorkItemWrapper<EnqueueBatchReplayCommandsWorkItem> wrapper)
        {
            if (FlightingUtilities.IsEnabled(FlightingNames.CommandReplayBatchQueueDraining))
            {
                // Flight control to enable draining queues with our process.
                IncomingEvent.Current?.SetProperty("DrainingQueue", "true");
                return QueueProcessResult.Success();
            }
            else if (FlightingUtilities.IsEnabled(FlightingNames.CommandReplayBatchQueueDelayProcess))
            {
                // Flight control to delay process the item for 60min in case of bug or other issues
                // which will likely required a hotfix before retry.
                IncomingEvent.Current?.SetProperty("DelayItemProcess", "true");
                return QueueProcessResult.RetryAfter(TimeSpan.FromMinutes(60));
            }

            // Logic to slow down based on flighting, if necessary.
            int[] delaySeconds = { 2, 1 };
            foreach (var i in delaySeconds)
            {
                if (FlightingUtilities.IsIntegerValueEnabled(FlightingNames.CommandReplayEnqueueDelaySeconds, i))
                {
                    await Task.Delay(TimeSpan.FromSeconds(i));
                    break;
                }
            }

            var replayItems = wrapper.WorkItem.ReplayCommandsBatch;
            IDataAgentMap dataAgentMap = this.dataAgentMapFactory.GetDataAgentMap();

            IncomingEvent.Current?.SetProperty("ReplayCommandsCount", replayItems.Count.ToString());
            IncomingEvent.Current?.SetProperty("IsApplicabilityVerified", wrapper.WorkItem.IsApplicabilityVerified.ToString());
            var replayedCommandIds = new HashSet<CommandId>();
            var droppedCommandIds = new HashSet<CommandId>();

            foreach (var item in replayItems)
            {
                PrivacyCommand pcfCommand = null;
                PXSV1.PrivacyRequest pxsCommand = null;
                List<Policy.DataTypeId> originalDataTypes = null;

                foreach (var targetQueue in item.AgentQueueTargets)
                {
                    if (!dataAgentMap.TryGetAgent(targetQueue.agentId, out IDataAgentInfo dataAgentInfo))
                    {
                        DualLogger.Instance.Information(nameof(EnqueueBatchReplayCommandsWorkItem), $"Failed to find AgentId {targetQueue.agentId.ToString()}");
                        continue;
                    }

                    if (dataAgentInfo.TryGetAssetGroupInfo(targetQueue.assetGroupId, out IAssetGroupInfo assetGroupInfo))
                    {
                        if (pcfCommand == null)
                        {
                            // parse the command into pcf format
                            var parser = new PxsCommandParser(assetGroupInfo.AgentId, assetGroupInfo.AssetGroupId, assetGroupInfo.AssetGroupQualifier, QueueStorageType.AzureCosmosDb);
                            (pcfCommand, pxsCommand) = parser.Process(item.RawPxsCommand);

                            // TODO: replay should pass the QueueStorageType here so we don't have to choose
                            pcfCommand.ChooseQueueStorageType();
                            originalDataTypes = pcfCommand.DataTypeIds.ToList();
                        }
                        else
                        {
                            // Otherwise, just repoint the existing command at the current asset group. This is much cheaper than
                            // reparsing from JSON.
                            pcfCommand.AgentId = assetGroupInfo.AgentId;
                            pcfCommand.AssetGroupId = assetGroupInfo.AssetGroupId;
                            pcfCommand.AssetGroupQualifier = assetGroupInfo.AssetGroupQualifier;
                            pcfCommand.DataTypeIds = originalDataTypes.ToList();
                        }

                        if (wrapper.WorkItem.IsApplicabilityVerified == false)
                        {
                            if (!assetGroupInfo.IsCommandActionable(pcfCommand, out var applicabilityResult))
                            {
                                droppedCommandIds.Add(pcfCommand.CommandId);
                                continue;
                            }
                        }

                        var exportParameters = await CommandIngester.PreProcessExportCommandAsync(pcfCommand, pxsCommand, assetGroupInfo.SupportedDataTypes.ToList());
                        
                        string preferredMoniker = CommandMonikerHash.GetPreferredMoniker(pcfCommand.CommandId, pcfCommand.AssetGroupId, CommandMonikerHash.GetCurrentWeightedMonikers(pcfCommand.QueueStorageType));

                        replayedCommandIds.Add(pcfCommand.CommandId);

                        // Enqueue.
                        await this.EnqueueCommandAsync(preferredMoniker, pcfCommand);

                        // Let the world know.
                        await this.publisher.PublishCommandStartedAsync(
                            assetGroupInfo.AgentId,
                            assetGroupInfo.AssetGroupId,
                            assetGroupInfo.AssetGroupQualifier,
                            pcfCommand.CommandId,
                            pcfCommand.CommandType,
                            pcfCommand.Timestamp,
                            exportParameters?.finalContainerUri,
                            exportParameters?.stagingContainerUri,
                            exportParameters?.stagingContainerPath,
                            dataAgentMap.AssetGroupInfoStreamName,
                            dataAgentMap.VariantInfoStreamName);
                    }
                }
            }

            IncomingEvent.Current?.SetProperty("ReplayCommandIds", string.Join(";", replayedCommandIds));
            IncomingEvent.Current?.SetProperty("DroppedCommandIds", string.Join(";", droppedCommandIds));
            return QueueProcessResult.Success();
        }

        private async Task EnqueueCommandAsync(string moniker, PrivacyCommand command)
        {
            var queue = this.queueFactory.CreateQueue(command.AgentId, command.AssetGroupId, command.Subject.GetSubjectType(), command.QueueStorageType);

            int nextBackoffMs = 1000;
            int attemptCount = 0;
            while (true)
            {
                try
                {
                    attemptCount++;
                    await queue.UpsertAsync(moniker, command);
                    break;
                }
                catch (CommandFeedException ex)
                {
                    // Exponential backoff, with some jitter. Multiply the next backoff times
                    // a random value between 1.5 and 2.5. This keeps all machines from having the same backoff.
                    double multiplier = 1.5 + RandomHelper.NextDouble();
                    int retryAfter = (int)(nextBackoffMs * multiplier);

                    if (attemptCount >= 5)
                    {
                        throw;
                    }
                    
                    // Request rate too large.
                    if (ex.ErrorCode == CommandFeedInternalErrorCode.Throttle)
                    {
                        DualLogger.Instance.Information(nameof(EnqueueBatchReplayCommandsWorkItem), $"Throttling error to upsert replayed commands. Sleeping for {retryAfter}ms");
                        await Task.Delay(retryAfter);
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            Logger.Instance?.CommandIngested(command);
        }
    }
}

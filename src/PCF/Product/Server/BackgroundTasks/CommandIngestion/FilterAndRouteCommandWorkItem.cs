namespace Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.SignalApplicability;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Processes FilterAndRouteCommandWorkItems out of an Azure Queue.
    /// </summary>
    public class FilterAndRouteCommandWorkItemHandler : BaseFilterAndRouteCommandWorkItemHandler
    {
        private readonly ICommandHistoryRepository repository;
        private readonly ICommandLifecycleEventPublisher eventPublisher;
        private readonly IAzureWorkItemQueuePublisher<InsertIntoQueueWorkItem> insertIntoQueuePublisher;
        private readonly IRedisClient redisClient;

        public FilterAndRouteCommandWorkItemHandler(
            ICommandHistoryRepository repository,
            ICommandLifecycleEventPublisher publisher,
            IDataAgentMapFactory dataAgentMapFactory,
            IAzureWorkItemQueuePublisher<InsertIntoQueueWorkItem> insertIntoQueuePublisher,
            IRedisClient redisClient) : base(dataAgentMapFactory)
        {
            this.repository = repository;
            this.eventPublisher = publisher;
            this.insertIntoQueuePublisher = insertIntoQueuePublisher;
            this.redisClient = redisClient;
        }

        protected override bool IsWhatIfMode => false;

        /// <summary>
        /// This one is a verified adopted copy from WhatIfFilterAndRouteWorkItemHandler.
        /// Everytime when we make changes in WhatIfFilterAndRouteWorkItemHandler v-next, this must be updated accordingly.
        /// </summary>
        protected override PxsFilteredCommandDestination FilterCommandForDestination(
            AgentId agentId,
            IDataAgentMap dataAgentMap,
            IAssetGroupInfo assetGroupInfo,
            JObject rawPxsCommand,
            CommandLifecycleEventBatch eventBatch,
            CommandHistoryRecord commandHistoryRecord)
        {
            IDataAgentInfo dataAgentInfo = dataAgentMap[agentId];
            if (dataAgentInfo.IsV2Agent())
            {
                return null;
            }

            // Parse to PXS and PCF command formats.
            var parser = new PxsCommandParser(agentId, assetGroupInfo.AssetGroupId, assetGroupInfo.AssetGroupQualifier, commandHistoryRecord.Core.QueueStorageType);
            var (command, _) = parser.Process(rawPxsCommand);

            PxsFilteredCommandDestination result = null;
            CommandIngestionStatus ingestionStatus;

            if (assetGroupInfo.IsCommandActionable(command, out var applicabilityResult))
            {
                IReadOnlyList<string> weightedMonikerList;
                if (commandHistoryRecord.Core.QueueStorageType == QueueStorageType.AzureCosmosDb &&
                    this.redisClient != null &&
                    FlightingUtilities.IsEnabled(FlightingNames.EnableEnqueueWithPartitionSize))
                {
                    weightedMonikerList = CommandMonikerHash.GetWeightedMonikersByPartitionSize(
                        this.redisClient,
                        agentId,
                        command.AssetGroupId,
                        CosmosDbQueueCollection.GetQueueCollectionId(command.Subject.GetSubjectType()),
                        commandHistoryRecord.Core.WeightedMonikerList);
                }
                else
                {
                    weightedMonikerList = commandHistoryRecord.Core.WeightedMonikerList;
                }

                var preferredMoniker = CommandMonikerHash.GetPreferredMoniker(command.CommandId, command.AssetGroupId, weightedMonikerList);

                IncomingEvent.Current.SetProperty("PreferredMoniker", preferredMoniker);

                commandHistoryRecord.StatusMap[(agentId, assetGroupInfo.AssetGroupId)] = new CommandHistoryAssetGroupStatusRecord(agentId, assetGroupInfo.AssetGroupId)
                {
                    StorageAccountMoniker = preferredMoniker,
                };

                ingestionStatus = CommandIngestionStatus.SendingToAgent;

                result = new PxsFilteredCommandDestination
                {
                    AgentId = agentId,
                    AssetGroupId = assetGroupInfo.AssetGroupId,
                    AssetGroupQualifier = assetGroupInfo.AssetGroupQualifier,
                    ApplicableVariantIds = command.GetCommandApplicableVariants(assetGroupInfo.VariantInfosAppliedByAgents).Select(v => v.VariantId).ToList(),
                    DataTypes = assetGroupInfo.SupportedDataTypes.ToList(),
                    TargetMoniker = preferredMoniker,
                    QueueStorageType = commandHistoryRecord.Core.QueueStorageType
                };
            }
            else
            {
                if (applicabilityResult.ReasonCode == ApplicabilityReasonCode.FilteredByVariant)
                {
                    ingestionStatus = CommandIngestionStatus.DroppedByApplyingVariant;

                    // Publish started + completed.
                    eventBatch.AddCommandStartedEvent(
                        command.AgentId,
                        command.AssetGroupId,
                        command.AssetGroupQualifier,
                        command.CommandId,
                        command.CommandType,
                        command.Timestamp,
                        null,
                        null,
                        null,
                        dataAgentMap.AssetGroupInfoStreamName,
                        dataAgentMap.VariantInfoStreamName);

                    eventBatch.AddCommandCompletedEvent(
                        command.AgentId,
                        command.AssetGroupId,
                        command.AssetGroupQualifier,
                        command.CommandId,
                        command.CommandType,
                        command.Timestamp,
                        applicabilityResult.ApplicableVariantIds.Select(id => id.ToString()).ToArray(),
                        ignoredByVariant: true,
                        rowCount: 0,
                        delinked: false,
                        completedByPcf: true,
                        nonTransientExceptions: null);
                }
                else
                {
                    ingestionStatus = CommandIngestionStatus.DroppedDueToFiltering;

                    if (applicabilityResult.ReasonCode == ApplicabilityReasonCode.TipAgentIsNotOnline)
                    {
                        ingestionStatus = CommandIngestionStatus.DroppedDueToOfflineAgent;
                    }

                    if (command.CommandType == Client.PrivacyCommandType.Export
                        && !FlightingUtilities.IsEnabled(FlightingNames.CommandLifecycleEventPublishDroppedEventDisabled))
                    {
                        eventBatch.AddCommandDroppedEvent(
                        command.AgentId,
                        command.AssetGroupId,
                        command.AssetGroupQualifier,
                        command.CommandId,
                        command.CommandType,
                        applicabilityResult.ReasonCode.ToString(),
                        dataAgentMap.AssetGroupInfoStreamName,
                        dataAgentMap.VariantInfoStreamName);
                    }
                }
            }

            commandHistoryRecord.AuditMap[(agentId, assetGroupInfo.AssetGroupId)] = new CommandIngestionAuditRecord
            {
                ApplicabilityReasonCode = applicabilityResult.ReasonCode,
                DebugText = applicabilityResult.ReasonDescription,
                IngestionStatus = ingestionStatus,
            };

            return result;
        }

        protected override Task<bool> InsertCommandHistoryAsync(CommandHistoryRecord record)
        {
            return this.repository.TryInsertAsync(record);
        }

        protected override Task PublishEventBatchAsync(CommandLifecycleEventBatch batch)
        {
            return this.eventPublisher.PublishBatchAsync(batch);
        }

        protected override Task<CommandHistoryRecord> QueryCommandHistoryAsync(CommandId commandId, CommandHistoryFragmentTypes fragments)
        {
            return this.repository.QueryAsync(commandId, fragments);
        }

        /// <summary>
        /// Inserts the given request into an Azure queue, splitting if necessary.
        /// </summary>
        protected override Task PublishToQueueAsync(FilterAndRouteCommandWorkItem workItem, PxsFilteredCommandRequest request)
        {
            IncomingEvent.Current?.SetProperty("PushedToQueue", "true");

            return this.insertIntoQueuePublisher.PublishWithSplitAsync(
                request.Destinations,
                splitDestinations => new InsertIntoQueueWorkItem
                {
                    CommandId = workItem.CommandId,
                    CommandType = workItem.CommandType,
                    IsReplayCommand = workItem.IsReplayCommand,
                    Destinations = splitDestinations.ToList(),
                    PxsCommand = workItem.PxsCommand,
                    DataSetVersion = request.PdmsDataSetVersion
                },
                x => TimeSpan.Zero);
        }
    }
}

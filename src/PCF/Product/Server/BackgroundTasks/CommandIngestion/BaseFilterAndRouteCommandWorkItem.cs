namespace Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Applicability;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.SignalApplicability;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    
    /// <summary>
    /// Work item that is triggered to apply filtering and routing for a new command coming in from PXS.
    /// </summary>
    public class FilterAndRouteCommandWorkItem
    {
        /// <summary>
        /// The PXS command.
        /// </summary>
        public JObject PxsCommand { get; set; }

        /// <summary>
        /// The command ID.
        /// </summary>
        public CommandId CommandId { get; set; }

        /// <summary>
        /// A hint about how to parse the PXS command.
        /// </summary>
        public PrivacyCommandType CommandType { get; set; }

        /// <summary>
        /// The data set version to use. Set by the frontend that initially received this request.
        /// </summary>
        public long DataSetVersion { get; set; }

        /// <summary>
        /// If this work item is for ReplayForAll
        /// </summary>
        public bool IsReplayCommand { get; set; } = false;
    }

    /// <summary>
    /// A base class for FilterAndRouteWorkItem.
    /// </summary>
    public abstract class BaseFilterAndRouteCommandWorkItemHandler : IAzureWorkItemQueueHandler<FilterAndRouteCommandWorkItem>
    {
        private readonly IDataAgentMapFactory dataAgentMapFactory;

        protected BaseFilterAndRouteCommandWorkItemHandler(
            IDataAgentMapFactory dataAgentMapFactory)
        {
            this.dataAgentMapFactory = dataAgentMapFactory;
        }

        /// <summary>
        /// Gets the name of the queue for "whatif" items.
        /// </summary>
        public static string WhatIfQueueName => "WhatIf" + nameof(FilterAndRouteCommandWorkItem);

        /// <summary>
        /// Gets the name of the queue for standard queue items.
        /// </summary>
        public static string QueueName => nameof(FilterAndRouteCommandWorkItem);

        /// <summary>
        /// Priority of this work item.
        /// </summary>
        public SemaphorePriority WorkItemPriority => SemaphorePriority.Low;

        /// <summary>
        /// Indicates if we are running in read-only preview mode for new versions of SAL.
        /// </summary>
        protected abstract bool IsWhatIfMode { get; }

        /// <summary>
        /// Checks that a command has all of it's sub-items completed, and, if so, raises a notification.
        /// </summary>
        public async Task<QueueProcessResult> ProcessWorkItemAsync(QueueWorkItemWrapper<FilterAndRouteCommandWorkItem> wrapper)
        {
            var workItem = wrapper.WorkItem;

            // Pull the version of the data set indicated in the work item. This is important in cases where
            // this work item fails and another with a potentially different "current" data set picks it up.
            // This technique allows us to execute idempotently here, which is crucial.
            IDataAgentMap dataAgentMap = await this.dataAgentMapFactory.GetDataAgentMapAsync(workItem.DataSetVersion);

            PxsFilteredCommandRequest request = new PxsFilteredCommandRequest
            {
                CommandType = workItem.CommandType,
                RawPxsCommand = workItem.PxsCommand,
                Destinations = new List<PxsFilteredCommandDestination>(),
                PdmsDataSetVersion = dataAgentMap.Version,
            };

            IncomingEvent.Current?.SetProperty("CommandId", workItem.CommandId.Value);
            IncomingEvent.Current?.SetProperty("CommandType", workItem.CommandType.ToString());
            IncomingEvent.Current?.SetProperty("DataSetVersion", dataAgentMap.Version.ToString());
            IncomingEvent.Current?.SetProperty("IsReplayedCommand", workItem.IsReplayCommand.ToString());

            // Check to see if the record already exists.
            CommandHistoryRecord record = await this.QueryCommandHistoryAsync(workItem.CommandId, CommandHistoryFragmentTypes.All);
            bool tryInsert = false;

            if (record == null)
            {
                tryInsert = true;
                record = new CommandHistoryRecord(workItem.CommandId);

                var (dummyCommand, pxsCommand) = PxsCommandParser.DummyParser.Process(workItem.PxsCommand);
                var queueStorageType = new QueueStorageTypeSelector().Process(dummyCommand);

                IncomingEvent.Current?.SetProperty("SubjectType", dummyCommand.Subject.GetSubjectType().ToString());
                IncomingEvent.Current?.SetProperty("QueueStorageType", queueStorageType.ToString());

                record.Core.CreatedTime = DateTimeOffset.UtcNow;
                record.Core.IngestionAssemblyVersion = EnvironmentInfo.AssemblyVersion;
                record.Core.IngestionDataSetVersion = workItem.DataSetVersion;
                record.Core.RawPxsCommand = workItem.PxsCommand.ToString(Formatting.None);
                record.Core.TotalCommandCount = 0;
                record.Core.IngestedCommandCount = 0;
                record.Core.CompletedCommandCount = 0;
                record.Core.WeightedMonikerList = CommandMonikerHash.GetCurrentWeightedMonikers(queueStorageType);
                record.Core.CommandType = dummyCommand.CommandType;
                record.Core.Context = pxsCommand.Context;
                record.Core.Requester = pxsCommand.Requester;
                record.Core.Subject = dummyCommand.Subject;
                record.Core.IsSynthetic = pxsCommand.IsSyntheticRequest;
                record.Core.QueueStorageType = queueStorageType;
            }
            else
            {
                IncomingEvent.Current?.SetProperty("SubjectType", record.Core.Subject.GetSubjectType().ToString());

                // It's possible that PXS can call PCF twice for the same command (they use some queuing
                // internally as well), which means it's possible for us to try to insert the same command
                // twice using different versions of the PDMS data set. This breaks our atomic version model,
                // so we need to check to see if we are allowed to proceed.
                if (record.Core.IngestionDataSetVersion != workItem.DataSetVersion)
                {
                    // Another work item with a different data set version beat us to inserting. 
                    // Let's just complete this work item. Since we got this far, we are sure
                    // that there is another work item with a different version that owns
                    // getting this command inserted.
                    IncomingEvent.Current?.SetProperty("VersionConflict", $"{workItem.DataSetVersion},{record.Core.IngestionDataSetVersion}");
                    return QueueProcessResult.Success();
                }
            }

            // Prepare a batch of events to publish.
            CommandLifecycleEventBatch eventBatch = new CommandLifecycleEventBatch(workItem.CommandId);

            foreach (AgentId agentId in dataAgentMap.GetAgentIds())
            {
                IDataAgentInfo dataAgentInfo = dataAgentMap[agentId];

                foreach (IAssetGroupInfo assetGroup in dataAgentInfo.AssetGroupInfos)
                {
                    if (assetGroup.IsFakePreProdAssetGroup  
                        || FlightingUtilities.IsIngestionBlockedForAgentId(agentId) 
                        || FlightingUtilities.IsIngestionBlockedForAssetGroupId(assetGroup.AssetGroupId))
                    {
                        continue;
                    }

                    PxsFilteredCommandDestination destination = this.FilterCommandForDestination(
                        agentId,
                        dataAgentMap,
                        assetGroup,
                        workItem.PxsCommand,
                        eventBatch,
                        record);

                    if (destination != null)
                    {
                        if (Config.Instance.PPEHack.Enabled)
                        {
                            StringBuilder commandResult = new StringBuilder("ProcessWorkItemAsync => [ReadyToPublish]: ");
                            commandResult.Append($"Command: {workItem.CommandId.GuidValue}, AgentId:{agentId}, assetGroupId: {destination.AssetGroupId} ");
                            DualLogger.Instance.Information(nameof(BaseFilterAndRouteCommandWorkItemHandler), $"{commandResult}");
                        }
                        request.Destinations.Add(destination);
                    }
                }
            }

            record.Core.TotalCommandCount = record.StatusMap.Count;

            if (tryInsert && !workItem.IsReplayCommand)
            {
                // Attempt to insert the record. On conflict, we should abort and 
                // try reading again next time. The next run, "tryInsert" 
                // will be false. The reason we back off here is because the set of
                // storage account monikers may be inconsistent between machines,
                // and we want to route all commands to deterministic locations.
                bool insertSuccess = await this.InsertCommandHistoryAsync(record);
                if (!insertSuccess)
                {
                    IncomingEvent.Current.SetProperty("CommandHistoryInsertConflict", "true");
                    return QueueProcessResult.TransientFailureRandomBackoff();
                }
            }

            // Then push to event hub.
            await this.PublishEventBatchAsync(eventBatch);

            // Finally insert into queue.
            await this.PublishToQueueAsync(wrapper.WorkItem, request);

            // This is a relatively computationally expensive task, so we do it in the background.
            Task background = Task.Run(() => this.LogResultsAsync(workItem.CommandId, request, record, eventBatch, dataAgentMap));

            return QueueProcessResult.Success();
        }

        private async Task LogResultsAsync(
            CommandId commandId,
            PxsFilteredCommandRequest filteredCommands,
            CommandHistoryRecord commandHistory,
            CommandLifecycleEventBatch eventBatch,
            IDataAgentMap map)
        {
            using (await PrioritySemaphore.Instance.WaitAsync(SemaphorePriority.Background))
            {
                string typeName = this.GetType().Name;
                int count = 0;

                // Parse as a PCF command to get access to some fields for logging.
                var (pcfCommand, _) = PxsCommandParser.DummyParser.Process(JsonConvert.DeserializeObject<JObject>(commandHistory.Core.RawPxsCommand));

                string salVersion = FileVersionInfo.GetVersionInfo(typeof(ApplicabilityReasonCode).Assembly.Location).FileVersion;

                foreach (var agentId in map.GetAgentIds())
                {
                    var agentInfo = map[agentId];
                    foreach (var assetGroupInfo in agentInfo.AssetGroupInfos)
                    {
                        var key = (agentId, assetGroupInfo.AssetGroupId);
                        var destination = filteredCommands.Destinations.FirstOrDefault(x => x.AgentId == agentId && x.AssetGroupId == assetGroupInfo.AssetGroupId);

                        bool sentToAgent = destination != null;
                        if (sentToAgent && commandHistory.AuditMap.TryGetValue(key, out var auditMap))
                        {
                            var lifecycleEvents = eventBatch.Events.Where(x => x.AgentId == agentId && x.AssetGroupId == assetGroupInfo.AssetGroupId).ToList();
                            var variants = lifecycleEvents.OfType<CommandCompletedEvent>().SelectMany(x => x.ClaimedVariantIds);

                            Logger.Instance?.CommandFiltered(
                                sentToAgent: sentToAgent,
                                applicabilityCode: auditMap.ApplicabilityReasonCode ?? (ApplicabilityReasonCode)(-1),
                                variantsApplied: variants,
                                dataTypes: destination?.DataTypes,
                                commandLifecycleEventNames: lifecycleEvents.Select(e => e.GetType().ToString()),
                                subjectType: commandHistory.Core.Subject.GetSubjectType(),
                                commandType: commandHistory.Core.CommandType,
                                isWhatIfMode: this.IsWhatIfMode,
                                cloudInstance: pcfCommand.CloudInstance,
                                salVersion: salVersion,
                                pdmsVersion: map.Version.ToString(),
                                agentId: agentId,
                                assetGroupId: assetGroupInfo.AssetGroupId,
                                commandId: commandId,
                                commandCreationTimestamp: pcfCommand.Timestamp
                                );
                        }

                        count++;
                        if (count % 25 == 0)
                        {
                            Thread.Yield();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// If this agent/asset group combination is relevant for this command, a destination object is returned. Otherwise, null is returned.
        /// </summary>
        protected abstract PxsFilteredCommandDestination FilterCommandForDestination(
            AgentId agentId,
            IDataAgentMap dataAgentMap,
            IAssetGroupInfo assetGroupInfo,
            JObject rawPxsCommand,
            CommandLifecycleEventBatch eventBatch,
            CommandHistoryRecord commandHistoryRecord);

        /// <summary>
        /// Publishes a batch of command lifecycle events.
        /// </summary>
        protected abstract Task PublishEventBatchAsync(CommandLifecycleEventBatch batch);

        /// <summary>
        /// Queries for command history items for the given command ID.
        /// </summary>
        protected abstract Task<CommandHistoryRecord> QueryCommandHistoryAsync(CommandId commandId, CommandHistoryFragmentTypes fragments);

        /// <summary>
        /// Attempts to insert the given record into Command History.
        /// </summary>
        protected abstract Task<bool> InsertCommandHistoryAsync(CommandHistoryRecord record);

        /// <summary>
        /// Inserts the given request into an Azure queue, splitting if necessary.
        /// </summary>
        protected abstract Task PublishToQueueAsync(FilterAndRouteCommandWorkItem workItem, PxsFilteredCommandRequest request);
    }
}

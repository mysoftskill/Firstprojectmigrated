namespace Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.DistributedLocking;
    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeedv2.Common.Storage;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// A periodic task that cleans up export storage.
    /// </summary>
    public class OldExportForceCompleteTask : WorkerTaskBase<object, OldExportForceCompleteTask.LockState>
    {
        private readonly IDataAgentMapFactory dataAgentMapFactory;

        private readonly ICommandHistoryRepository commandHistoryRepository;

        private readonly ICommandLifecycleEventPublisher lifecycleEventPublisher;

        private readonly ICosmosDbClientFactory cosmosDbClientFactory;

        private readonly IAppConfiguration appConfiguration;

        private ICosmosDbClient<CompletedExportEventEntry> completedExportEventCosmosDbClient;

        public OldExportForceCompleteTask(
            ICommandHistoryRepository commandHistoryRepository,
            ICommandLifecycleEventPublisher lifecycleEventPublisher,
            IDataAgentMapFactory dataAgentMapFactory,
            ICosmosDbClientFactory cosmosDbClientFactory,
            IAppConfiguration appConfiguration)
            : this(commandHistoryRepository, lifecycleEventPublisher, dataAgentMapFactory, cosmosDbClientFactory, appConfiguration, null)
        {
        }

        public OldExportForceCompleteTask(
            ICommandHistoryRepository commandHistoryRepository,
            ICommandLifecycleEventPublisher lifecycleEventPublisher,
            IDataAgentMapFactory dataAgentMapFactory,
            ICosmosDbClientFactory cosmosDbClientFactory,
            IAppConfiguration appConfiguration,
            IDistributedLockPrimitives<LockState> lockPrimitives)
            : base(Config.Instance.Worker.Tasks.ForceCompleteOldExportCommandsTask.CommonConfig, nameof(OldExportForceCompleteTask), lockPrimitives)
        {
            this.commandHistoryRepository = commandHistoryRepository;
            this.lifecycleEventPublisher = lifecycleEventPublisher;
            this.dataAgentMapFactory = dataAgentMapFactory;
            this.cosmosDbClientFactory = cosmosDbClientFactory;
            this.appConfiguration = appConfiguration;
        }

#if INCLUDE_TEST_HOOKS
        /// <summary>
        /// A test-hook to scope the force completion to a single command ID.
        /// </summary>
        public CommandId ScopedCommandId { get; set; }
#endif

        protected override LockState GetFinalLockState(out TimeSpan leaseTime)
        {
            DateTimeOffset nextStartTime = DateTimeOffset.UtcNow.Date.AddDays(1).AddHours(Config.Instance.Worker.Tasks.ForceCompleteOldExportCommandsTask.TaskStartHour);
            leaseTime = nextStartTime - DateTimeOffset.UtcNow + TimeSpan.FromMinutes(1);
            return new LockState { NextStartTime = nextStartTime };
        }

        protected override IEnumerable<Func<Task>> GetTasksAsync(LockState state, object parameters)
        {
            yield return this.ForceCompleteTaskAsync;
        }

        private async Task ForceCompleteTaskAsync()
        {
            try
            {
                DateTimeOffset oldestRecord = DateTimeOffset.UtcNow.AddDays(-Config.Instance.Worker.Tasks.ForceCompleteOldExportCommandsTask.MaxCompletionAgeDays);
                DateTimeOffset newestRecord = DateTimeOffset.UtcNow.AddDays(-Config.Instance.Worker.Tasks.ForceCompleteOldExportCommandsTask.MinCompletionAgeDays);
                DateTimeOffset oldestAadRecord = DateTimeOffset.UtcNow.AddDays(-Config.Instance.Worker.Tasks.ForceCompleteOldExportCommandsTask.MaxCompletionAgeDaysAad);
                DateTimeOffset newestAadRecord = DateTimeOffset.UtcNow.AddDays(-Config.Instance.Worker.Tasks.ForceCompleteOldExportCommandsTask.MinCompletionAgeDaysAad);

                List<CommandHistoryRecord> results = new List<CommandHistoryRecord>();
                results.AddRange(await this.commandHistoryRepository.QueryIncompleteExportsAsync(oldestRecord, newestRecord, false, CommandHistoryFragmentTypes.Core | CommandHistoryFragmentTypes.Status));
                results.AddRange(await this.commandHistoryRepository.QueryIncompleteExportsAsync(oldestAadRecord, newestAadRecord, true, CommandHistoryFragmentTypes.Core | CommandHistoryFragmentTypes.Status));

                List<Task> publishTasks = new List<Task>();
                List<string> commandIds = new List<string>();

                IDataAgentMap dataAgentMap = this.dataAgentMapFactory.GetDataAgentMap();
                int count = 0;
                foreach (CommandHistoryRecord record in results)
                {
#if INCLUDE_TEST_HOOKS

                    if (this.ScopedCommandId != null && record.CommandId != this.ScopedCommandId)
                    {
                        continue;
                    }
#endif
                    commandIds.Add(record.CommandId.Value);
                    count++;

                    // If the completed commands flag is enabled then the cosmos db client will be used for writing command ids
                    if (await appConfiguration.IsFeatureFlagEnabledAsync(FeatureNames.PCFV2.WriteCompletedCommands).ConfigureAwait(false))
                    {
                        // Setup the cosmos db client if it hasn't been created yet
                        if (completedExportEventCosmosDbClient == null)
                        {
                            completedExportEventCosmosDbClient =
                                cosmosDbClientFactory.GetCosmosDbClient<CompletedExportEventEntry>(Config.Instance.PCFv2.CosmosDbCompletedCommandsContainerName,
                                Config.Instance.PCFv2.CosmosDbDatabaseName, Config.Instance.PCFv2.CosmosDbEndpointName);
                        }

                        // Convert the command id to guid to restore dashes then a string
                        string commandIdAsString = Guid.Parse(record.CommandId.Value).ToString();

                        // Add the command to the completed commands database for PCFV2
                        await completedExportEventCosmosDbClient.UpsertEntryAsync(new CompletedExportEventEntry()
                        {
                            Id = commandIdAsString
                        },
                        commandIdAsString).ConfigureAwait(false);
                    }

                    CommandLifecycleEventBatch batch = new CommandLifecycleEventBatch(record.CommandId);

                    // If the write force complete with null values flag is enabled then send one event for a command id and no agent id and asset group id
                    if (await appConfiguration.IsFeatureFlagEnabledAsync(FeatureNames.PCFV2.WriteForceCompleteToEventHubWithNullValues).ConfigureAwait(false))
                    {
                        batch.AddCommandCompletedEvent(
                            agentId: new AgentId(Guid.Empty),
                            assetGroupId: new AssetGroupId(Guid.Empty),
                            assetGroupQualifier: String.Empty,
                            commandId: record.CommandId,
                            commandType: PrivacyCommandType.Export,
                            commandCreationTime: record.Core.CreatedTime,
                            claimedVariants: new string[0],
                            ignoredByVariant: false,
                            rowCount: 0,
                            delinked: false,
                            nonTransientExceptions: "Command force completed: " + Logger.Instance?.CorrelationVector,
                            completedByPcf: true,
                            forceCompleteReasonCode: ForceCompleteReasonCode.ForceCompleteFromAgeoutTimer);

                        PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "DailyExportForceCompleteItem").Increment();
                    }

                    // Publish a bunch of lifecycle events so that the command appears completed.
                    foreach (var item in record.StatusMap)
                    {
                        // This agent+assetGroup completed the command or did not get the command.
                        if (item.Value.CompletedTime != null)
                        {
                            continue;
                        }
                        else if (item.Value.IngestionTime == null)
                        {
                            // The command wasn't successfully ingested. Log that fact but don't yell at the agent for it.
                            Logger.Instance?.LogNotReceivedForceCompleteCommandEvent(
                                record.CommandId,
                                item.Key.agentId,
                                item.Key.assetGroupId,
                                record.Core.CommandType,
                                record.Core.Subject?.GetSubjectType());
                            DualLogger.Instance.Warning(nameof(OldExportForceCompleteTask), $"Agent {item.Key.agentId} did not ingest {record.Core.CommandType} command {record.CommandId}");
                        }

                        // This agent+assetGroup has NOT completed the command.
                        string assetGroupQualifier = string.Empty;

                        if (dataAgentMap.TryGetAgent(item.Key.agentId, out IDataAgentInfo dataAgentInfo) &&
                            dataAgentInfo.TryGetAssetGroupInfo(item.Key.assetGroupId, out IAssetGroupInfo assetGroupInfo))
                        {
                            assetGroupQualifier = assetGroupInfo.AssetGroupQualifier;
                        }

                        batch.AddCommandCompletedEvent(
                            agentId: item.Key.agentId,
                            assetGroupId: item.Key.assetGroupId,
                            assetGroupQualifier: assetGroupQualifier,
                            commandId: record.CommandId,
                            commandType: PrivacyCommandType.Export,
                            commandCreationTime: record.Core.CreatedTime,
                            claimedVariants: new string[0],
                            ignoredByVariant: false,
                            rowCount: 0,
                            delinked: false,
                            nonTransientExceptions: "Command force completed: " + Logger.Instance?.CorrelationVector,
                            completedByPcf: true,
                            forceCompleteReasonCode: ForceCompleteReasonCode.ForceCompleteFromAgeoutTimer);

                        Logger.Instance?.LogForceCompleteCommandEvent(
                            record.CommandId,
                            item.Key.agentId,
                            item.Key.assetGroupId,
                            ForceCompleteReasonCode.ForceCompleteFromAgeoutTimer.ToString(),
                            record.Core.CommandType,
                            record.Core.Subject?.GetSubjectType());

                        PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "DailyExportForceCompleteItem").Increment();
                    }

                    publishTasks.Add(this.lifecycleEventPublisher.PublishBatchAsync(batch));
                    PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "DailyExportForceComplete").Increment();
                }

                await Task.WhenAll(publishTasks);
                IncomingEvent.Current?.SetProperty("CommandCount", count.ToString());
                IncomingEvent.Current?.SetProperty("CommandIds", string.Join(",", commandIds));
                PerfCounterUtility.GetOrCreate(PerformanceCounterType.Number, "DailyExportForceComplete").Set(count);
            }
            catch
            {
                PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "DailyExportForceComplete:Failed").Increment();
                PerfCounterUtility.GetOrCreate(PerformanceCounterType.Number, "DailyExportForceComplete:Failed").Set(1);
                throw;
            }
        }

        protected override bool ShouldRun(LockState lockState)
        {
            DateTimeOffset nextTimeToRun = lockState?.NextStartTime ?? DateTimeOffset.MinValue;
            return nextTimeToRun <= DateTime.UtcNow;
        }

        // State stored along with the lock.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public class LockState
        {
            public DateTimeOffset NextStartTime { get; set; }
        }
    }
}

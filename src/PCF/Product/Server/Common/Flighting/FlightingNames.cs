namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    /// <summary>
    /// PCF Flighting Names Catalog
    /// Naming convention: FeatureArea_FlightName
    /// </summary>
    public static class FlightingNames
    {
        // these are for any agent(s) or assetgroup(s) who are causing QOS issues due to malbehavior
        // add the agentId or assetgroupId in the flight
        public const string BlockedAgents = "BlockedAgents";
        public const string BlockedAssetGroups = "BlockedAssetGroups";

        // this blocks just inserting commands into specific agent/assetgroup queues
        // used in cases where the queue depth is so very high that it causes CosmosDb partition issue
        public const string IngestionBlockedForAgentId = "IngestionBlockedForAgentId_Enabled";
        public const string IngestionBlockedForAssetGroupId = "IngestionBlockedForAssetGroupId_Enabled";

        // To mitigate issues with QueueDepth Api
        public const string QueueDepthDrainTasks = "QueueDepth_DrainTasks";
        public const string QueueDepthDisableKusto = "QueueDepth_DisableKusto";
        public const string QueueDepthKustoFlushImmediately = "QueueDepth_KustoFlushImmediately";

        // QueueDepth is by agent opt-in only because its expensive. This is how we allow agents
        public const string AllowedAgentIdForQueueStatsApi = "QueueDepth_AllowedAgentIds";

        // This is to disable agent queue flush. Havent had to use agent queueflush in a long time
        // this was mostly used pre-gdpr and just during gdpr to clear agent queues and start afresh
        public const string AgentQueueFlushDrainAzureQueue = "AgentQueueFlush_DrainAzureQueue";
        public const string AgentQueueFlushDelayQueueProcess = "AgentQueueFlush_DelayQueueProcess";
        public const string AgentQueueFlushSprocExecDelay1000ms = "AgentQueueFlush_SprocExecDelay_1000ms";
        public const string AgentQueueFlushSprocExecDelay500ms = "AgentQueueFlush_SprocExecDelay_500ms";

        // Command Lifecycle Events
        public const string CommandLifecycleEventHubReceiverSlowdown = "CommandLifecycle_EventHubReceiverSlowDown";
        public const string CommandLifecycleEventPublishDroppedEventDisabled = "CommandLifecycle_DroppedEvents_Disabled";

        // Command Queues
        public const string DeleteFromQueueWorkItemSuppressInvalidLeaseReceipts = "DeleteFromQueueWorkItem_SuppressInvalidLeaseReceipts";
        public const string LogicalCommandQueueDelayBetweenItems = "LogicalCommandQueue_DelayBetweenItems";

        // Synthetic agents
        public const string SyntheticAgentDisableCompleteCommands = "SyntheticAgent_DisableCompleteCommands";
        public const string SyntheticAgentDisableBatchComplete = "SyntheticAgent_DisableBatchComplete";

        // Command History
        public const string CommandStatusBatchWorkItemAutoCompleteOnError = "CommandStatusBatchWorkItem_AutoCompleteOnError";

        // Block specific agentIds from calling Replay
        public const string CommandReplayDisallowedAgentIds = "CommandReplay_DisallowedAgentIds";
        public const string CommandReplayExtendedMaxReplayDates = "CommandReplay_ExtendedMaxReplayDates";
        public const string CommandReplayDisableProdReplayDelay = "CommandReplay_DisableProdReplayDelay";
        public const string CommandReplayBatchQueueDraining = "CommandReplay_BatchQueueDraining";
        public const string CommandReplayBatchQueueDelayProcess = "CommandReplay_BatchQueueDelayProcess";
        public const string CommandReplayEnqueueDelaySeconds = "CommandReplay_EnqueueDelaySeconds";
        public const string CommandReplayWorkerDisabled = "CommandReplay_WorkerDisabled";
        public const string CommandReplayWorkerDelaySeconds = "CommandReplay_WorkerDelaySeconds";
        public const string CommandReplayReduceEnqueueReplayCommandBatchSize = "CommandReplay_ReduceEnqueueReplayCommandBatchSize";
        public const string CommandReplayStaggerEnqueueReplayCommandBatches = "CommandReplay_StaggerEnqueueReplayCommandBatches";

        public const string RandomConnectionCloseEnable = "RandomConnectionClose_Enable";

        // Add test tenantIds for TIP agents
        public const string TestInProductionByTenantIdEnabled = "TestInProductionByTenantIdEnabled";

        public const string CosmosDbQueueDisabledDatabases = "CosmosDbQueue_DisabledDatabases";

        // CloudInstance configuration filtering
        public const string CloudInstanceConfigMissingFallbackDisabled = "CloudInstance_ConfigMissingFallback_Disabled";

        // Flights to temporary disable specific Azure resources for livesite mitigation
        public const string EventHubPublisherDisabled = "EventHubPublisher_Disabled";
        public const string AzureQueuePublisherDisabled = "AzureQueuePublisher_Disabled";
        public const string CommandHistoryBlobClientDisabled = "CommandHistoryBlobClient_Disabled";
        public const string CommandQueueEnqueueDisabled = "CommandQueueEnqueue_Disabled";

        // Delay processing the specific Azure Queue, to mitigate livesite issues with that queue
        public const string AzureWorkItemQueueDelayProcessing = "AzureWorkItemQueue_DelayProcessing";

        // IngestionRecoveryTask
        public const string IngestionRecoveryTaskDisabled = "IngestionRecoveryTask_Disabled";

        // GetCommands
        public const string GetCommandsPopErrorThreshold = "GetCommandsActionResult_PopErrorThreshold";

        // Disable/ scale down DeferredDelete in CheckpointActionResult
        public const string CheckpointCompleteDeferredDeleteDisabled = "CheckpointComplete_DeferredDelete_Disabled";

        // Stress environment forwarding
        public const string TrustedStressSSLThumbprints = "StressForwarding_TrustedSSLThumbprints";
        public const string StressForwardingPercentage = "StressForwarding_Percentage";

        // Disable GetCommands
        public const string GetCommandsDisabled = "GetCommands_Disabled";

        // Defender AV Scanning
        public const string AvScanEnabled = "AVScan_Enable";

        // Autoscaler
        public const string AutoScalerDisabled = "DocDbAutoscale_Disabled";

        // NonWindows Device Delete
        public const string NonWindowsDeviceDeleteDisabled = "NonWindowsDeviceDelete_Disabled";

        // Export to CSV for subject
        public const string ExportToCsvBySubjectEnabled = "ExportToCsvBySubject_Enabled";

        // Disable processing the specific Azure Queue, to mitigate livesite issues with that queue
        public const string AzureWorkItemQueueDisableProcessing = "AzureWorkItemQueueDisableProcessing";

        // PCF Worker EventHub retry queue handler feature flags 
        public const string EventHubRetryQueueHandlerEnabled = "EventHubRetryQueueHandlerEnabled";
        public const string CommandLifecycleEventReceiverDisableRetryQueue = "CommandLifecycleEventReceiverDisableRetryQueue";

        public const string EnableEnqueueWithPartitionSize = "EnableEnqueueWithPartitionSize";

        // Disable update message process, if message not exist, to mitigate livesite issue.
        public const string IgnoreMessageIfNotFoundEnabled = "IgnoreMessageIfNotFound_Enabled";

        // If ExportDestination URL is missing in commandhistory read it from queue and repopulate
        public const string RePopulateExportDestinationFromQueues = "RePopulateExportDestinationFromQueues";

        // check if we disabled ingestion recovery items processing.
        public const string IngestionRecoveryItemsProcessingDisabled = "IngestionRecoveryItemsProcessingDisabled";

        // check if we disabled ingestion repair items processing.
        public const string IngestionRepairItemsProcessingDisabled = "IngestionRepairItemsProcessingDisabled";

        // Enable logging for command lifecycle events processing.
        public const string PCFCommandLifeCycleLoggingEnabled = "PCF.CommandLifeCycleLoggingEnabled";

        // Do not limit eventhub processing..
        public const string PCFEventHubProcessingFullThrottle = "PCF.EventHubProcessingFullThrottle";

#if INCLUDE_TEST_HOOKS

        // Test hook
        public const string FlightingEnabledTestHook = "Tests_FlightingEnabled";

#endif
    }
}

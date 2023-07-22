
namespace Microsoft.Azure.ComplianceServices.Common
{
    /// <summary>
    /// Defines all config names that can be passed in IAppConfiguration.GetAppConfiguration
    /// </summary>
    public static class ConfigNames
    {
        public static class PXS
        {
            public const string VortexDeviceDeleteWorker_EnableDequeuing = "PXS.VortexDeviceDeleteWorker.EnableDequeuing";
            public const string VortexDeviceDeleteWorker_DelayPerMessageInMilliSeconds = "PXS.VortexDeviceDeleteWorker.DelayPerMessageInMilliSeconds";
            public const string DeviceDeleteMaxVisibilityTimeoutInMinutes = "PXS.DeviceDeleteMaxVisibilityTimeoutInMinutes";
            public const string VortexDeviceDeleteWorker_DequeueCount = "PXS.VortexDeviceDeleteWorker.DequeueCount";
            

            public const string DataActionRunner_EnableJobScheduler = "PXS.DataActionRunner.EnableJobScheduler";
            public const string DataActionRunner_ActionRefsOverrides = "PXS.DataActionRunner.ActionRefsOverrides";

            public const string AadAccountCloseWorker_EnableDequeuing = "PXS.AadAccountCloseWorker.EnableDequeuing";
            public const string AadAccountCloseWorker_EnableProcessing = "PXS.AadAccountCloseWorker.EnableProcessing";
            public const string AadAccountCloseWorker_TenantIdFilterWhiteList = "PXS.AadAccountCloseWorker.TenantIdFilterWhiteList";
            public const string AadAccountCloseWorker_DeadLetterTableReProcessingConfig = "PXS.AadAccountCloseWorker.DeadLetterTableReProcessingConfig";
            public const string ScopedDeleteService_DeleteRequestsBatchSize = "PXS.ScopedDeleteService.DeleteRequestsBatchSize";

            // AnaheimId Worker
            public const string AnaheimIdQueueWorkerDelayInMilliSeconds = "PXS.AnaheimIdQueueWorkerDelayInMilliSeconds";
            public const string AnaheimIdQueueWorkerMinVisibilityTimeoutInSeconds = "PXS.AnaheimIdQueueWorkerMinVisibilityTimeoutInSeconds";
            public const string AnaheimIdQueueWorkerMaxVisibilityTimeoutInSeconds = "PXS.AnaheimIdQueueWorkerMaxVisibilityTimeoutInSeconds";
            public const string AnaheimIdQueueWorkerMaxCount = "PXS.AnaheimIdQueueWorkerMaxCount";
            public const string AnaheimIdThrottledRequestsMaxVisibilityTimeoutInMinutes = "PXS.AnaheimIdThrottledRequestsMaxVisibilityTimeoutInMinutes";

            //feature flag to make only email mandatory in DSR export requests
            public const string PRCMakeEmailMandatory = "PCDPXS.PRCMakeEmailMandatory";

            // Cosmos Worker.
            // Default chunk size is 25 KB, new chunk size will be 25 * multiple KB.
            public const string CosmosWorkerChunkReadSizeMultiple = "PXS.CosmosWorkerChunkReadSizeMultiple";
            // Default lease duration is 20 mins, new lease duration will be 20 * multiple minutes.
            public const string CosmosWorkerWorkItemLeaseTimeMultiple = "PXS.CosmosWorkerWorkItemLeaseTimeMultiple";

            //PXS AggregateCount API
            public const string TimelineAggregateCountAPIEnabled = "PXS.TimelineAggregateCountAPIEnabled";

            // The max VisibilityTimeout value for DeviceDelete request. Used for even distribution for all DeviceDeletes
            public const string EvenlyDistributeDeviceDeleteRequestMaxTimeoutInMinutes = "PXS.EvenlyDistributeDeviceDeleteRequestMaxTimeoutInMinutes";
        }

        public static class PCF
        {
            public const string CommandLifecycleEventPublisher_MaxPublishBytes = "PCF.CommandLifecycleEventPublisher.MaxPublishBytes";

            // todo: PCF AuditReceiver Hotfix 
            public const string ProcessAuditLogQueueAsyncDelayInMs = "ProcessAuditLogQueueAsyncDelayInMs";
            public const string ProcessAuditLogQueueAsyncMaxMessageCount = "ProcessAuditLogQueueAsyncMaxMessageCount";

            public const string CommandLifecycleEventPublisher_PxsCommandsBatchSize = "PCF.CommandLifecycleEventPublisher.PxsCommandsBatchSize";

            // If queue insertion fails, wait random time and retry.
            public const string InsertIntoQueue_MaxRetryWaitTimeInSeconds = "PCF.InsertIntoQueue.MaxRetryWaitTimeInSeconds";

            public const string IngestionRecoveryWindowSizeInDays = "PCF.DebugUtitilities.IngestionRecoveryWindowSizeInDays";

            public const string IngestionRecoveryWorkItemSplitWindowInHours = "PCF.DebugUtitilities.IngestionRecoveryWorkItemSplitWindowInHours";

            // PCF event hub checkpoint frequency used by CommandHistoryAggregationReceiver
            public const string CommandHistoryAggregationReceiverEventHubCheckpointFrequencySeconds = "PCF.CommandHistoryAggregationReceiver.EventHubCheckpointFrequencySeconds";

            // PCF event hub checkpoint frequency used by CommandRawDataReceiver
            public const string CommandRawDataReceiverMaxCheckpointIntervalSecs = "PCF.CommandRawDataReceiver.MaxCheckpointIntervalSecs";

            // ApiTrafficThrottling: Get the percentage of the traffic allowed for each traffic (combination of API.AgentId.AssetGroupId)
            public const string ApiTrafficPercantage = "PCF.ApiTrafficPercantage";

            // PCF CommandHistory max batch size
            public const string CommandHistoryLifecycleBatchSize = "PCF.CommandHistoryAggregationReceiver.CommandHistoryLifecycleBatchSize";

            // PCF CommandHistoryAggregationReceiver CheckpointThreshold
            public const string CommandHistoryAggregationReceiverCheckpointThreshold = "PCF.CommandHistoryAggregationReceiver.CheckpointThreshold";

            // PCF CommandHistoryAggregationReceiver CheckpointMaxThreadCount
            public const string CommandHistoryAggregationReceiverCheckpointMaxThreadCount = "PCF.CommandHistoryAggregationReceiver.CheckpointMaxThreadCount";

            // PCF CommandHistoryAggregationReceiver checkpoint publish delay multiplier
            public const string CommandHistoryAggregationReceiverCheckpointPublishDelayMultiplier = "PCF.CommandHistoryAggregationReceiver.CheckpointPublishDelayMultiplier";
            
            // Max wait time for eventhub callback.
            public const string MaxWaitTimeForCheckpointInMinutes = "PCF.MaxWaitTimeForEventHubCallbackInMinutes";
        }

        public static class PAF
        {
            // The url for the ICM connector endpoint
            public const string PAF_AID_ICMConnectorUrl = "PAF_AID_ICMConnectorUrl";
            // Name of the entity submiting the ticket
            public const string PAF_AID_ICMConnectorName = "PAF_AID_ICMConnectorName";
            // ID for routing the ticket to the correct queue
            public const string PAF_AID_ICMConnectorID = "PAF_AID_ICMConnectorID";
            // Keyvault to retrieve the ICM connector certificate for portal authentication
            public const string PAF_AID_ICMConnectorKeyVaultUrl = "PAF_AID_ICMConnectorKeyVaultUrl";
            // The name of the client certificate for portal authentication
            public const string PAF_AID_ICMConnectorCertificateName = "PAF_AID_ICMConnectorCertificateName";
            // The max VisibilityTimeout value for Anaheimid request
            public const string PAF_AID_AnaheimIdRequestMaxVisibilityTimeoutInMinutes = "PAF_AID_AnaheimIdRequestMaxVisibilityTimeoutInMinutes";
        }

        public static class PDMS
        {
            
            public const string ServiceTreeMetadataWorker_Enabled = "PDMS.ServiceTreeMetadataWorker.WorkerEnabled";
            
            public const string ServiceTreeMetadataWorker_Frequency = "PDMS.ServiceTreeMetadataWorker.Frequency";
            public const string ServiceTreeMetadataWorker_GetServicesWithMetadataQuery = "PDMS.ServiceTreeMetadataWorker.GetServicesWithMetadataQuery";
            public const string ServiceTreeMetadataWorker_GetServicesUnderDivisionQuery = "PDMS.ServiceTreeMetadataWorker.GetServicesUnderDivisionQuery";
            public const string ServiceTreeMetadataWorker_WhiteListedServices_Divisions = "PDMS.ServiceTreeMetadataWorker.WhiteListedServices.Divisions";
            public const string ServiceTreeMetadataWorker_WhiteListedServices_Services = "PDMS.ServiceTreeMetadataWorker.WhiteListedServices.Services";
            public const string ServiceTreeMetadataWorker_BlackListedServices_Services = "PDMS.ServiceTreeMetadataWorker.BlackListedServices.Services";
            public const string NGPPowerBIUrlTemplate = "PDMS.ServiceTreeMetadataWorker.NGPPowerBIUrlTemplate";
            public const string PrivacyComplianceDashboardTemplate = "PDMS.ServiceTreeMetadataWorker.PrivacyComplianceDashboardTemplate";
        }
    }
}

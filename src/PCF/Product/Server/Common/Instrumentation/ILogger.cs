namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Telemetry;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.SignalApplicability;

    /// <summary>
    /// Default logger interface.
    /// </summary>
    public interface ILogger : IEventLogger
    {
        /// <summary>
        /// Logged when we return a batch of commands to an agent.
        /// </summary>
        void CommandsReturned(
             AgentId agentId,
             AssetGroupId assetGroupId,
             string assetGroupQualifier,
             IEnumerable<CommandId> commandIds,
             Dictionary<PrivacyCommandType, int> commandCounts);

        /// <summary>
        /// Logged when we ingest a command into an agent queue.
        /// </summary>
        void CommandIngested(PrivacyCommand command);

        /// <summary>
        /// Logged when PXS sends a delete command without a time range predicate.
        /// </summary>
        void NullPxsTimeRangePredicate(CommandId commandId);

        /// <summary>
        /// Logs the agent queue statistics event
        /// </summary>
        void AgentQueueStatisticsEvent(AgentQueueStatistics agentQueueStatistics);

        /// <summary>
        /// Logs that we received a command from Event Grid with an invalid verifier.
        /// </summary>
        void InvalidVerifierReceived(CommandId commandId, AgentId agentId, Exception ex);

        /// <summary>
        /// Logs the given incoming event.
        /// </summary>
        void Log(IncomingEvent incomingEvent);

        /// <summary>
        /// Logs the given incoming event.
        /// </summary>
        void Log(OutgoingEvent outgoingEvent);

        /// <summary>
        /// Logs the given operation event.
        /// </summary>
        void Log(OperationEvent operationEvent);

        /// <summary>
        /// Logs the given CosmosDB outgoing event.
        /// </summary>
        void Log(CosmosDbOutgoingEvent cosmosDbEvent);

        /// <summary>
        /// Logs the size of an export output in bytes.
        /// </summary>
        void LogExportFileSizeEvent(
            AgentId agentId,
            AssetGroupId assetGroupId,
            CommandId commandId,
            string fileName,
            long sizeInBytes,
            long compressedSizeInBytes,
            bool isSourceCompressed,
            SubjectType subjectType,
            AgentType agentType,
            string cloudInstance);

        /// <summary>
        /// Logs collection command queue depth.
        /// </summary>
        void LogQueueDepth(CollectionQueueDepth queueDepth);

        /// <summary>
        /// Log force completed command event.
        /// </summary>
        void LogForceCompleteCommandEvent(
            CommandId commandId, 
            AgentId agentId, 
            AssetGroupId assetGroupId, 
            string forceCompletedReason, 
            PrivacyCommandType commandType, 
            SubjectType? subjectType);

        /// <summary>
        /// Logs when a force-completed command has not been ingested by the given agent.
        /// </summary>
        void LogNotReceivedForceCompleteCommandEvent(
            CommandId commandId,
            AgentId agentId,
            AssetGroupId assetGroupId,
            PrivacyCommandType commandType,
            SubjectType? subjectType);

        /// <summary>
        /// Logs the current telemetry events buffer size.
        /// </summary>
        void LogTelemetryLifecycleCheckpointInfo(TelemetryLifecycleCheckpointInfo eventInfo);

        /// <summary>
        /// Logged when a command is filtered.
        /// </summary>
        void CommandFiltered(
            bool sentToAgent,
            ApplicabilityReasonCode applicabilityCode,
            IEnumerable<string> variantsApplied,
            IEnumerable<DataTypeId> dataTypes,
            IEnumerable<string> commandLifecycleEventNames,
            SubjectType subjectType,
            PrivacyCommandType commandType,
            bool isWhatIfMode,
            string cloudInstance,
            string salVersion,
            string pdmsVersion,
            AgentId agentId,
            AssetGroupId assetGroupId,
            CommandId commandId,
            DateTimeOffset commandCreationTimestamp);

        /// <summary>
        /// Logged when a command history query is truncated.
        /// </summary>
        void CommandHistoryQueryTooLarge(
            IPrivacySubject subject,
            string requester,
            IList<PrivacyCommandType> commandTypes,
            DateTimeOffset oldestRecord,
            CommandHistoryFragmentTypes fragmentsToRead);
    }
}

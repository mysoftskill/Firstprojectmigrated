namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications
{
    using System;
    using System.Collections.Generic;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    /// <summary>
    /// A batch of events for the same command ID.
    /// </summary>
    public class CommandLifecycleEventBatch
    {
        private readonly CommandId expectedId;
        private readonly List<CommandLifecycleEvent> events;

        /// <summary>
        /// Creates an instance of a batch of command lifecycle events.
        /// </summary>
        public CommandLifecycleEventBatch(CommandId commandId)
        {
            this.expectedId = commandId;
            this.events = new List<CommandLifecycleEvent>();
        }

        /// <summary>
        /// The current set of events to be published.
        /// </summary>
        public IEnumerable<CommandLifecycleEvent> Events => this.events;

        /// <summary>
        /// Adds a started event to the batch.
        /// </summary>
        public void AddCommandStartedEvent(
            AgentId agentId,
            AssetGroupId assetGroupId,
            string assetGroupQualifier,
            CommandId commandId,
            PrivacyCommandType commandType,
            DateTimeOffset? commandCreationTime,
            Uri finalExportDestinationUri,
            Uri stagingExportDestinationUri,
            string stagingExportDestinationPath,
            string assetGroupStreamName,
            string variantStreamName)
        {
            this.AddEvent(CommandLifecycleEventPublisher.CreateStartedEvent(
                agentId,
                assetGroupId,
                assetGroupQualifier,
                commandId,
                commandType,
                commandCreationTime,
                finalExportDestinationUri,
                stagingExportDestinationUri,
                stagingExportDestinationPath,
                assetGroupStreamName,
                variantStreamName));
        }

        /// <summary>
        /// Adds a completed event to the batch.
        /// </summary>
        public void AddCommandCompletedEvent(
            AgentId agentId,
            AssetGroupId assetGroupId,
            string assetGroupQualifier,
            CommandId commandId,
            PrivacyCommandType commandType,
            DateTimeOffset? commandCreationTime,
            string[] claimedVariants,
            bool ignoredByVariant,
            int rowCount,
            bool delinked,
            string nonTransientExceptions,
            bool completedByPcf,
            ForceCompleteReasonCode? forceCompleteReasonCode = null)
        {
            this.AddEvent(CommandLifecycleEventPublisher.CreateCompletedEvent(
                agentId,
                assetGroupId,
                assetGroupQualifier,
                commandId,
                commandType,
                commandCreationTime,
                claimedVariants,
                ignoredByVariant,
                rowCount,
                delinked,
                nonTransientExceptions,
                completedByPcf,
                forceCompleteReasonCode));
        }

        /// <summary>
        /// Adds a dropped event to the batch.
        /// </summary>
        public void AddCommandDroppedEvent(
            AgentId agentId,
            AssetGroupId assetGroupId,
            string assetGroupQualifier,
            CommandId commandId,
            PrivacyCommandType commandType,
            string notApplicableReasonCode,
            string assetGroupStreamName,
            string variantStreamName)
        {
            this.AddEvent(CommandLifecycleEventPublisher.CreateCommandDroppedEvent(
                agentId,
                assetGroupId,
                assetGroupQualifier,
                commandId,
                commandType,
                notApplicableReasonCode,
                assetGroupStreamName,
                variantStreamName));
        }

        private void AddEvent(CommandLifecycleEvent @event)
        {
            if (@event.CommandId != this.expectedId)
            {
                throw new InvalidOperationException($"All batch operations must be for the same command ID. Expected: {this.expectedId}, Actual = {@event.CommandId}");
            }

            this.events.Add(@event);
        }
    }
}

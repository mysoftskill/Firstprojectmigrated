namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// An interface that publishes command lifecycle events.
    /// </summary>
    public interface ICommandLifecycleEventPublisher
    {
        /// <summary>
        /// Invoked when PCF gets a new command from PXS
        /// </summary>
        Task PublishCommandRawDataAsync(IReadOnlyList<JObject> pxsCommands);

        /// <summary>
        /// Invoked when a command has been freshly created for the Agent/AssetGroup combination.
        /// </summary>
        Task PublishCommandStartedAsync(
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
            string variantStreamName);

        /// <summary>
        /// Invoked when a command's status is completed.
        /// </summary>
        Task PublishCommandCompletedAsync(
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
            ForceCompleteReasonCode? forceCompleteReasonCode = null);

        /// <summary>
        /// Invoked when a command is flagged as being soft-deleted.
        /// </summary>
        Task PublishCommandSoftDeletedAsync(
            AgentId agentId,
            AssetGroupId assetGroupId,
            string assetGroupQualifier,
            CommandId commandId,
            PrivacyCommandType commandType,
            DateTimeOffset? commandCreationTime,
            string nonTransientExceptions);

        /// <summary>
        /// Invoked when a command was sent to the agent.
        /// </summary>
        Task PublishCommandSentToAgentAsync(
            AgentId agentId,
            AssetGroupId assetGroupId,
            string assetGroupQualifier,
            CommandId commandId,
            PrivacyCommandType commandType);

        /// <summary>
        /// Invoked when a command is flagged as pending.
        /// </summary>
        Task PublishCommandPendingAsync(
            AgentId agentId,
            AssetGroupId assetGroupId,
            string assetGroupQualifier,
            CommandId commandId,
            PrivacyCommandType commandType);

        /// <summary>
        /// Invoked when a command is flagged as failed.
        /// </summary>
        Task PublishCommandFailedAsync(
            AgentId agentId,
            AssetGroupId assetGroupId,
            string assetGroupQualifier,
            CommandId commandId,
            PrivacyCommandType commandType);

        /// <summary>
        /// Invoked when a command is flagged as being unexpected.
        /// </summary>
        Task PublishCommandUnexpectedAsync(
            AgentId agentId,
            AssetGroupId assetGroupId,
            string assetGroupQualifier,
            CommandId commandId,
            PrivacyCommandType commandType);

        /// <summary>
        /// Invoked when a command is flagged as failing verification.
        /// </summary>
        Task PublishCommandVerificationFailedAsync(
            AgentId agentId,
            AssetGroupId assetGroupId,
            string assetGroupQualifier,
            CommandId commandId,
            PrivacyCommandType commandType);

        /// <summary>
        /// Invoked when a command is flagged as unexpectedly failing verification.
        /// </summary>
        Task PublishCommandUnexpectedVerificationFailureAsync(
            AgentId agentId,
            AssetGroupId assetGroupId,
            string assetGroupQualifier,
            CommandId commandId,
            PrivacyCommandType commandType);

        /// <summary>
        /// Atomically publishes the batch of events.
        /// </summary>
        Task PublishBatchAsync(CommandLifecycleEventBatch batch);
    }
}

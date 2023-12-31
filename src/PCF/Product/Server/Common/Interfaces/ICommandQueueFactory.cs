namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    /// <summary>
    /// A factory interface for creating command queues.
    /// </summary>
    public interface ICommandQueueFactory
    {
        /// <summary>
        /// Creates a command queue.
        /// </summary>
        ICommandQueue CreateQueue(
            AgentId agentId,
            AssetGroupId assetGroupId,
            SubjectType subjectType,
            QueueStorageType queueStorageType);
    }
}

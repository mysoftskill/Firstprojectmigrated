namespace Microsoft.PrivacyServices.CommandFeed.Service.Telemetry.Repository
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Telemetry;
    using Microsoft.PrivacyServices.CommandFeed.Service.Telemetry.BackgroundTasks;

    /// <summary>
    /// A telemetry repository wrapper.
    /// </summary>
    public interface ITelemetryRepository
    {
        /// <summary>
        /// Add command lifecycle event.
        /// </summary>
        Task AddAsync(List<LifecycleEventTelemetry> lifecycleEvents);

        /// <summary>
        /// Add agent Azure storage command queue depth into Kusto table
        /// </summary>
        Task AddAzureStorageQueueDepthAsync(List<AgentQueueStatistics> agentQueueStatistics);

        /// <summary>
        /// Add queue depth baseline.
        /// </summary>
        Task AddBaselineAsync(QueueDepthWorkItem workItem);

        /// <summary>
        /// Add cosmos db partition size.
        /// </summary>
        Task AddCosmosDbPartitionSizeAsync(List<CosmosDbPartitionSize> partitionSizes);

        /// <summary>
        /// Add task into Kusto table.
        /// </summary>
        Task AddTaskAsync(QueueDepthWorkItem workItem, TaskActionName action);

        /// <summary>
        /// Create tasks in Kusto table.
        /// </summary>
        Task AddTasksAsync(List<QueueDepthWorkItem> workItems, TaskActionName action);

        /// <summary>
        /// Appends aggregated statistics for specified agent.
        /// </summary>
        Task AppendAgentStatAsync(AgentId agentId);

        /// <summary>
        /// Gets the QueueStats for the agent
        /// </summary>
        /// <param name="dataAgentMap">IDataAgentMap to map AssetQualifier to AssetGroupId</param>
        /// <param name="agentId">AgentId</param>
        /// <param name="assetGroupId">AssetGroupId and if null all assetgroups</param>
        /// <param name="privacyCommandType">CommandType and if None, all command types</param>
        /// <returns>List of QueueStats</returns>
        Task<List<QueueStats>> GetAgentStats(IDataAgentMap dataAgentMap, AgentId agentId, AssetGroupId assetGroupId, PrivacyCommandType privacyCommandType);

        /// <summary>
        /// Insert data for agent stats that do not have lifecycle events.
        /// </summary>
        Task InterpolateAgentStatAsync(AgentId agentId);
    }
}

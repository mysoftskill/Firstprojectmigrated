namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.CommandFeed.Client.Commands;
    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;

    /// <summary>
    /// Defines a client that can fetch a batch of privacy commands from the server.
    /// </summary>
    public interface ICommandFeedClient
    {
        /// <summary>
        /// List of supported sovereign cloud configurations.
        /// </summary>
        List<KeyDiscoveryConfiguration> SovereignCloudConfigurations { get; set; }

        /// <summary>
        /// The amount of time for which leases are acquired.
        /// </summary>
        TimeSpan? RequestedLeaseDuration { get; set; }

        /// <summary>
        /// Fetches the next batch of commands from the server.
        /// </summary>
        /// <returns>The list of commands.</returns>
        Task<List<IPrivacyCommand>> GetCommandsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Updates the status of the given command.
        /// </summary>
        /// <returns>The new lease receipt for the command.</returns>
        Task<string> CheckpointAsync(
            string commandId,
            string agentState,
            CommandStatus commandStatus,
            int affectedRowCount,
            string leaseReceipt,
            TimeSpan? leaseExtension = null,
            IEnumerable<string> variantIds = null,
            IEnumerable<string> nonTransientFailures = null,
            IEnumerable<ExportedFileSizeDetails> exportedFileSizeDetails = null);

        /// <summary>
        /// Completes a batch of commands.
        /// </summary>
        /// <param name="processedCommands">The commands ready for completion.</param>
        /// <returns>Returns if operation is successful or if the collection is empty.</returns>
        /// <exception cref="ArgumentNullException">If the collection is null.</exception>
        /// <exception cref="HttpRequestException">If this request fails. Errors logged to <see cref="CommandFeedLogger"/> BatchCompleteError.</exception>
        Task BatchCheckpointCompleteAsync(IEnumerable<ProcessedCommand> processedCommands);

        /// <summary>
        /// Retrieves details about a Command previously received from GetCommandsAsync().
        /// </summary>
        /// <returns>The full command.</returns>
        Task<IPrivacyCommand> QueryCommandAsync(string leaseReceipt, CancellationToken cancellationToken);

        /// <summary>
        /// This is in BETA Testing. Please reach out to ngppoeng to have your agentId enabled to call this Api.
        /// Gets the stats on the agent's queue depth.
        /// Please do not use this API as part of the checkpoint logic.
        /// </summary>
        /// <param name="assetGroupQualifier">
        ///     AssetGroupQualifier to filter the stats on, all AssetGroups of the agent if null
        /// </param>
        /// <param name="commandType">PrivacyCommandType: AccountClose, Delete, or Export to optionally filter on</param>
        /// <returns>A list of queueStats</returns>
        Task<List<QueueStats>> GetQueueStatsAsync(string assetGroupQualifier = null, string commandType = null);

        /// <summary>
        /// Replay commands by a list of Command Ids.
        /// </summary>
        /// <param name="commandIds">The commands that needs to be replayed</param>
        /// <param name="assetGroupQualifiers">The specific asset groups that the commands should be replayed for. All asset groups of the agent if null</param>
        Task ReplayCommandsByIdAsync(IEnumerable<string> commandIds, IEnumerable<string> assetGroupQualifiers = null);

        /// <summary>
        /// Replay commands for specific dates.
        /// </summary>
        /// <param name="replayFromDate">Replay all commands from this date</param>
        /// <param name="replayToDate">Replay all commands to this date</param>
        /// <param name="assetGroupQualifiers">The specific asset groups that the commands should be replayed for. All asset groups of the agent if null</param>
        Task ReplayCommandsByDatesAsync(DateTimeOffset replayFromDate, DateTimeOffset replayToDate, IEnumerable<string> assetGroupQualifiers = null);

        /// <summary>
        /// Gets a page of delete commands within a given time window.
        /// </summary>
        /// <param name="assetGroupId">The Asset Group Id</param>
        /// <param name="startTime">The start time</param>
        /// <param name="endTime">The end time</param>
        /// <param name="returnOnlyTest">A boolean flag that when set to true will ensure only test commands are returned.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A GetBatchCommandResult object</returns>
        Task<GetBatchCommandResponse> GetBatchDeleteCommandAsync(Guid assetGroupId, DateTimeOffset startTime, DateTimeOffset endTime, bool returnOnlyTest, CancellationToken cancellationToken);

        /// <summary>
        /// Gets a page of delete commands within a given time window.
        /// </summary>
        /// <param name="startTime">The start time</param>
        /// <param name="endTime">The end time</param>
        /// <param name="maxResult">The maximum number of command pages this call can return</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A GetBatchCommandResult object</returns>
        Task<GetBatchCommandResponse> GetBatchDeleteCommandAsync(DateTimeOffset startTime, DateTimeOffset endTime, int maxResult, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the next page of delete commands.
        /// </summary>  
        /// <param name="nextPageUri">The url for the next page of delete commands</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A GetBatchCommandResult object</returns>
        Task<GetBatchCommandResponse> GetNextBatchDeleteCommandAsync(string nextPageUri, CancellationToken cancellationToken);

        /// <summary>
        /// Gets a page of export commands within a given time window.
        /// </summary>
        /// <param name="assetGroupId">The Asset Group Id</param>
        /// <param name="startTime">The start time</param>
        /// <param name="endTime">The end time</param>
        /// <param name="returnOnlyTest">A boolean flag that when set to true will ensure only test commands are returned.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A GetBatchCommandResult object</returns>
        Task<GetBatchCommandResponse> GetBatchExportCommandAsync(Guid assetGroupId, DateTimeOffset startTime, DateTimeOffset endTime, bool returnOnlyTest, CancellationToken cancellationToken);

        /// <summary>
        /// Gets a page of export commands within a given time window.
        /// </summary>
        /// <param name="startTime">The start time</param>
        /// <param name="endTime">The end time</param>
        /// <param name="maxResult">The maximum number of command pages this call can return</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A GetBatchCommandResult object</returns>
        Task<GetBatchCommandResponse> GetBatchExportCommandAsync(DateTimeOffset startTime, DateTimeOffset endTime, int maxResult, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the next page of export commands.
        /// </summary>
        /// <param name="nextPageUri">The url for the next page of delete commands</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A GetBatchCommandResult object</returns>
        Task<GetBatchCommandResponse> GetNextBatchExportCommandAsync(string nextPageUri, CancellationToken cancellationToken);

        /// <summary>
        /// Completes a batch of delete commands within a given time window.
        /// </summary>
        /// <param name="assetGroupId">The Asset Group Id</param>
        /// <param name="startTime">The start time</param>
        /// <param name="endTime">The end time</param>
        /// <param name="completionToken">The batch command completion token</param>
        /// <param name="cancellationToken">The cancellation token</param>
        Task CompleteBatchDeleteCommandAsync(Guid assetGroupId, DateTimeOffset startTime, DateTimeOffset endTime, string completionToken, CancellationToken cancellationToken);

        /// <summary>
        /// Completes a batch of delete commands within a given time window.
        /// </summary>
        /// <param name="assetGroupId">The Asset Group Id</param>
        /// <param name="startTime">The start time</param>
        /// <param name="endTime">The end time</param>
        /// <param name="completionToken">The batch command completion token</param>
        /// <param name="succeededAssetUris">The AssetUris that have been successfully completed</param>
        /// <param name="failedAssetUris">The AssetUris that failed to complete</param>
        /// <param name="cancellationToken">The cancellation token</param>
        Task CompleteBatchDeleteCommandWithAssetUrisAsync(Guid assetGroupId, DateTimeOffset startTime, DateTimeOffset endTime, string completionToken, string[] succeededAssetUris, string[] failedAssetUris, CancellationToken cancellationToken);

        /// <summary>
        /// Completes a batch of export commands within a given time window.
        /// </summary>
        /// <param name="assetGroupId">The Asset Group Id</param>
        /// <param name="startTime">The start time</param>
        /// <param name="endTime">The end time</param>
        /// <param name="completionToken">The batch command completion token</param>
        /// <param name="stagingContainer">The staging location for export result</param>
        /// <param name="stagingRootFolder">The name of the root export folder</param>
        /// <param name="cancellationToken">The cancellation token</param>
        Task CompleteBatchExportCommandAsync(Guid assetGroupId, DateTimeOffset startTime, DateTimeOffset endTime, string completionToken, Uri stagingContainer, string stagingRootFolder, CancellationToken cancellationToken);

        /// <summary>
        /// Completes a batch of export commands within a given time window.
        /// </summary>
        /// <param name="assetGroupId">The Asset Group Id</param>
        /// <param name="startTime">The start time</param>
        /// <param name="endTime">The end time</param>
        /// <param name="completionToken">The batch command completion token</param>
        /// <param name="stagingContainer">The staging location for export result</param>
        /// <param name="stagingRootFolder">The name of the root export folder</param>
        /// <param name="succeededAssetUris">The AssetUris that have been successfully completed</param>
        /// <param name="failedAssetUris">The AssetUris that failed to complete</param>
        /// <param name="cancellationToken">The cancellation token</param>
        Task CompleteBatchExportCommandWithAssetUrisAsync(Guid assetGroupId, DateTimeOffset startTime, DateTimeOffset endTime, string completionToken, Uri stagingContainer, string stagingRootFolder, string[] succeededAssetUris, string[] failedAssetUris, CancellationToken cancellationToken);

        /// <summary>
        /// Get AssetGroup details for an agent.
        /// </summary>
        /// <param name="assetGroupId">The AssetGroup ID.</param>
        /// <param name="version">The version of the method to run.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        Task<AssetGroupDetailsResponse> GetAssetGroupDetailsAsync(Guid assetGroupId,  Version version, CancellationToken cancellationToken);

        /// <summary>
        /// Get Resource URI map for an agent.
        /// </summary>
        /// <param name="assetGroupId">The AssetGroup ID.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        Task<ResourceUriMapResponse> GetResourceUriMapAsync(Guid assetGroupId, CancellationToken cancellationToken);

        /// <summary>
        /// Get the next page of AssetGroup details for a agent.
        /// </summary>
        /// <param name="nextPageUri">The url for the next page of AssetGroup details.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        Task<AssetGroupDetailsResponse> GetNextAssetGroupDetailsAsync(string nextPageUri, CancellationToken cancellationToken);

        /// <summary>
        /// Get the next page of the Resource Uri map for an agent.
        /// </summary>
        /// <param name="nextPageUri">The url for the next page of AssetGroup details.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        Task<ResourceUriMapResponse> GetNextResourceUriMapAsync(string nextPageUri, CancellationToken cancellationToken);

        /// <summary>
        /// Get the command configuration.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token</param>
        Task<string> GetCommandConfigurationAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Get the earliest available work item.
        /// </summary>
        /// <param name="assetGroupId">[Optional] The AssetGroup ID.</param>
        /// <param name="leaseDuration">[Optional] The lease duration in seconds. The default value is 900 (15 minutes).</param>
        /// <param name="returnOnlyTest">[Optional] A boolean flag that when set to true will ensure only test work items are returned.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        Task<Workitem> GetWorkitemAsync(Guid assetGroupId = default, int leaseDuration = 900, bool returnOnlyTest = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Query a workitem by its id.
        /// </summary>
        /// <param name="workitemId">The workitem ID.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        Task<Workitem> QueryWorkitemAsync(string workitemId, CancellationToken cancellationToken);

        /// <summary>
        /// Update an existing work item.
        /// </summary>
        /// <param name="workitemId">The workitem ID.</param>
        /// <param name="updateRequest">The UpdateWorkitemRequest object.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        Task UpdateWorkitemAsync(string workitemId, UpdateWorkitemRequest updateRequest, CancellationToken cancellationToken);
    }
}

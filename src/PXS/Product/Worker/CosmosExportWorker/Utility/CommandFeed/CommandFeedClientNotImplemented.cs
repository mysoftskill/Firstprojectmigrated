
namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility
{
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.CommandFeed.Client.Commands;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Contains all the NotImplemented methods from ICommandFeedClient.
    /// </summary>
    public abstract class CommandFeedClientNotImplemented
    {
        public Task<List<QueueStats>> GetQueueStatsAsync(string assetGroupQualifier = null, string commandType = null)
        {
            throw new NotSupportedException();
        }

        public Task BatchCheckpointCompleteAsync(IEnumerable<ProcessedCommand> processedCommands)
        {
            throw new NotSupportedException();
        }

        public Task ReplayCommandsByDatesAsync(DateTimeOffset replayFromDate, DateTimeOffset replayToDate, IEnumerable<string> assetGroupQualifiers = null)
        {
            throw new NotSupportedException();
        }

        public Task ReplayCommandsByIdAsync(IEnumerable<string> commandIds, IEnumerable<string> assetGroupQualifiers = null)
        {
            throw new NotSupportedException();
        }

        public Task<GetBatchCommandResponse> GetBatchDeleteCommandAsync(Guid assetGroupId, DateTimeOffset startTime, DateTimeOffset endTime, bool returnOnlyTest, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<GetBatchCommandResponse> GetBatchDeleteCommandAsync(DateTimeOffset startTime, DateTimeOffset endTime, int maxResult, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<GetBatchCommandResponse> GetNextBatchDeleteCommandAsync(string nextPageUri, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<GetBatchCommandResponse> GetBatchExportCommandAsync(Guid assetGroupId, DateTimeOffset startTime, DateTimeOffset endTime, bool returnOnlyTest, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<GetBatchCommandResponse> GetBatchExportCommandAsync(DateTimeOffset startTime, DateTimeOffset endTime, int maxResult, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<GetBatchCommandResponse> GetNextBatchExportCommandAsync(string nextPageUri, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task CompleteBatchDeleteCommandAsync(Guid assetGroupId, DateTimeOffset startTime, DateTimeOffset endTime, string completionToken, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task CompleteBatchDeleteCommandWithAssetUrisAsync(Guid assetGroupId, DateTimeOffset startTime, DateTimeOffset endTime, string completionToken, string[] succeededAssetUris, string[] failedAssetUris, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task CompleteBatchExportCommandAsync(Guid assetGroupId, DateTimeOffset startTime, DateTimeOffset endTime, string completionToken, Uri stagingContainer, string stagingRootFolder, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task CompleteBatchExportCommandWithAssetUrisAsync(Guid assetGroupId, DateTimeOffset startTime, DateTimeOffset endTime, string completionToken, Uri stagingContainer, string stagingRootFolder, string[] succeededAssetUris, string[] failedAssetUris, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<AssetGroupDetailsResponse> GetAssetGroupDetailsAsync(Guid assetGroupId, Version version, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<AssetGroupDetailsResponse> GetNextAssetGroupDetailsAsync(string nextPageUri, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<string> GetCommandConfigurationAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<Workitem> GetWorkitemAsync(Guid assetGroupId = default, int leaseDuration = 900, bool returnOnlyTest = false, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<Workitem> QueryWorkitemAsync(string workitemId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task UpdateWorkitemAsync(string workitemId, UpdateWorkitemRequest updateRequest, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<ResourceUriMapResponse> GetResourceUriMapAsync(Guid assetGroupId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<ResourceUriMapResponse> GetNextResourceUriMapAsync(string nextPageUri, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}

namespace Microsoft.PrivacyServices.CommandFeed.Client.Testing
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.CommandFeed.Client.Commands;
    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;

    /// <summary>
    /// An ICommandFeed client implementation that accepts arbitrary data and is hosted completely in memory.
    /// </summary>
    public sealed class InMemoryCommandFeedClient : ICommandFeedClient
    {
        private readonly object syncRoot = new object();
        private readonly ConcurrentQueue<PendingRequest> pendingRequests = new ConcurrentQueue<PendingRequest>();

        private List<IPrivacyCommand> pendingCommands;

        /// <inheritdoc />
        public List<string> SupportedCloudInstances { get; set; }

        /// <inheritdoc />
        public List<KeyDiscoveryConfiguration> SovereignCloudConfigurations { get; set; }

        /// <inheritdoc />
        public TimeSpan? RequestedLeaseDuration { get; set; }

        /// <summary>
        /// Adds the command to the queue. It will be returned by calling GetCommandsAsync.
        /// </summary>
        public void Enqueue(IPrivacyCommand command)
        {
            lock (this.syncRoot)
            {
                PendingRequest result;
                if (this.pendingRequests.TryDequeue(out result))
                {
                    result.Complete(new List<IPrivacyCommand> { command });
                }
                else
                {
                    this.pendingCommands = this.pendingCommands ?? new List<IPrivacyCommand>();
                    this.pendingCommands.Add(command);
                }
            }
        }

        /// <inheritdoc/>
        public async Task<string> CheckpointAsync(
            string commandId,
            string agentState,
            CommandStatus commandStatus,
            int affectedRowCount,
            string leaseReceipt,
            TimeSpan? leaseExtension = null,
            IEnumerable<string> variantIds = null,
            IEnumerable<string> nonTransientFailures = null,
            IEnumerable<ExportedFileSizeDetails> exportedFileSizeDetails = null)
        {
            await Task.Yield();
            return "new lease receipt";
        }

        /// <inheritdoc />
        public async Task BatchCheckpointCompleteAsync(IEnumerable<ProcessedCommand> processedCommands)
        {
            await Task.Yield();
        }

        /// <inheritdoc/>
        public Task<List<IPrivacyCommand>> GetCommandsAsync(CancellationToken cancellationToken)
        {
            TaskCompletionSource<List<IPrivacyCommand>> completionSource = new TaskCompletionSource<List<IPrivacyCommand>>();
            PendingRequest request = new PendingRequest(completionSource, cancellationToken);

            lock (this.syncRoot)
            {
                if (this.pendingCommands != null)
                {
                    request.Complete(this.pendingCommands);
                    this.pendingCommands = null;
                }
                else
                {
                    this.pendingRequests.Enqueue(request);
                }
            }

            return completionSource.Task;
        }

        /// <inheritdoc/>
        public Task<IPrivacyCommand> QueryCommandAsync(string leaseReceipt, CancellationToken cancellationToken)
        {
            IPrivacyCommand found = null;

            lock (this.syncRoot)
            {
                foreach (var command in this.pendingCommands)
                {
                    if (command.LeaseReceipt == leaseReceipt)
                    {
                        found = command;
                        break;
                    }
                }
            }

            return Task.FromResult<IPrivacyCommand>(found);
        }

        /// <inheritdoc/>
        public Task<List<QueueStats>> GetQueueStatsAsync(string assetGroupQualifier = null, string commandType = null)
        {
            return Task.FromResult<List<QueueStats>>(new List<QueueStats>() { new QueueStats() });
        }

        /// <inheritdoc/>
        public async Task ReplayCommandsByIdAsync(IEnumerable<string> commandIds, IEnumerable<string> assetGroupQualifiers = null)
        {
            await Task.Yield();
        }

        /// <inheritdoc/>
        public async Task ReplayCommandsByDatesAsync(DateTimeOffset replayFromDate, DateTimeOffset replayToDate, IEnumerable<string> assetGroupQualifiers = null)
        {
            await Task.Yield();
        }

        /// <inheritdoc />
        public Task<GetBatchCommandResponse> GetBatchDeleteCommandAsync(Guid assetGroupId, DateTimeOffset startTime, DateTimeOffset endTime, bool returnOnlyTest, CancellationToken cancellationToken)
        {
            return Task.FromResult(new GetBatchCommandResponse());
        }

        /// <inheritdoc />
        public Task<GetBatchCommandResponse> GetBatchDeleteCommandAsync(DateTimeOffset startTime, DateTimeOffset endTime, int maxResult, CancellationToken cancellationToken)
        {
            return Task.FromResult(new GetBatchCommandResponse());
        }

        /// <inheritdoc />
        public Task<GetBatchCommandResponse> GetNextBatchDeleteCommandAsync(string nextPageUri, CancellationToken cancellationToken)
        {
            return Task.FromResult(new GetBatchCommandResponse());
        }

        /// <inheritdoc />
        public Task<GetBatchCommandResponse> GetBatchExportCommandAsync(Guid assetGroupId, DateTimeOffset startTime, DateTimeOffset endTime, bool returnOnlyTest, CancellationToken cancellationToken)
        {
            return Task.FromResult(new GetBatchCommandResponse());
        }

        /// <inheritdoc />
        public Task<GetBatchCommandResponse> GetBatchExportCommandAsync(DateTimeOffset startTime, DateTimeOffset endTime, int maxResult, CancellationToken cancellationToken)
        {
            return Task.FromResult(new GetBatchCommandResponse());
        }

        /// <inheritdoc />
        public Task<GetBatchCommandResponse> GetNextBatchExportCommandAsync(string nextPageUri, CancellationToken cancellationToken)
        {
            return Task.FromResult(new GetBatchCommandResponse());
        }

        /// <inheritdoc />
        public async Task CompleteBatchDeleteCommandAsync(Guid assetGroupId, DateTimeOffset startTime, DateTimeOffset endTime, string completionToken, CancellationToken cancellationToken)
        {
            await Task.Yield();
        }

        /// <inheritdoc />
        public async Task CompleteBatchDeleteCommandWithAssetUrisAsync(Guid assetGroupId, DateTimeOffset startTime, DateTimeOffset endTime, string completionToken, string[] succeededAssetUris, string[] failedAssetUris, CancellationToken cancellationToken)
        {
            await Task.Yield();
        }

        /// <inheritdoc />
        public async Task CompleteBatchExportCommandAsync(Guid assetGroupId, DateTimeOffset startTime, DateTimeOffset endTime, string completionToken, Uri stagingContainer, string stagingRootFolder, CancellationToken cancellationToken)
        {
            await Task.Yield();
        }

        /// <inheritdoc />
        public async Task CompleteBatchExportCommandWithAssetUrisAsync(Guid assetGroupId, DateTimeOffset startTime, DateTimeOffset endTime, string completionToken, Uri stagingContainer, string stagingRootFolder, string[] succeededAssetUris, string[] failedAssetUris, CancellationToken cancellationToken)
        {
            await Task.Yield();
        }

        /// <inheritdoc />
        public Task<AssetGroupDetailsResponse> GetAssetGroupDetailsAsync(Guid assetGroupId, Version version, CancellationToken cancellationToken)
        {
            return Task.FromResult(new AssetGroupDetailsResponse());
        }

        /// <inheritdoc />
        public Task<AssetGroupDetailsResponse> GetNextAssetGroupDetailsAsync(string nextPageUri, CancellationToken cancellationToken)
        {
            return Task.FromResult(new AssetGroupDetailsResponse());
        }

        /// <inheritdoc />
        public Task<ResourceUriMapResponse> GetNextResourceUriMapAsync(string nextPageUri, CancellationToken cancellationToken)
        {
            return Task.FromResult(new ResourceUriMapResponse());
        }

        /// <inheritdoc />
        public Task<ResourceUriMapResponse> GetResourceUriMapAsync(Guid assetGroupId, CancellationToken cancellationToken)
        {
            return Task.FromResult(new ResourceUriMapResponse());
        }

        /// <inheritdoc />
        public Task<string> GetCommandConfigurationAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(string.Empty);
        }

        /// <inheritdoc />
        public Task<Workitem> GetWorkitemAsync(Guid assetGroupId = default, int leaseDuration = 900, bool returnOnlyTest = false, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new Workitem());
        }

        /// <inheritdoc />
        public Task<Workitem> QueryWorkitemAsync(string workitemId, CancellationToken cancellationToken)
        {
            return Task.FromResult(new Workitem());
        }

        /// <inheritdoc />
        public async Task UpdateWorkitemAsync(string workitemId, UpdateWorkitemRequest updateRequest, CancellationToken cancellationToken)
        {
            await Task.Yield();
        }

        private class PendingRequest
        {
            private CancellationTokenRegistration registration;
            private TaskCompletionSource<List<IPrivacyCommand>> taskCompletionSource;

            public PendingRequest(TaskCompletionSource<List<IPrivacyCommand>> tcs, CancellationToken token)
            {
                this.registration = token.Register(this.OnCanceled);
                this.taskCompletionSource = tcs;
            }

            public void Complete(List<IPrivacyCommand> commands)
            {
                this.taskCompletionSource.TrySetResult(commands);
                this.registration.Dispose();
            }

            private void OnCanceled()
            {
                this.taskCompletionSource.TrySetException(new TaskCanceledException());
                this.registration.Dispose();
            }
        }
    }
}

namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue
{
    using System;
    using System.Buffers;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Documents;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    /// <summary>
    /// Interacts with a single queue in a single collection on behalf of an agent.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    public class CosmosDbCommandQueue : CommandQueue, ICommandQueue
    {
        private readonly StorageCommandSerializer serializer = new StorageCommandSerializer();
        private readonly ICosmosDbQueueCollection queueCollection;
        private readonly StorageCommandParser parser;

        /// <inheritdoc />
        public CommandQueuePriority QueuePriority => CommandQueuePriority.High;

        /// <summary>
        /// Initializes the CosmosDbQueueCollection object based on the underlying collection, agent ID, and asset Group ID.
        /// </summary>
        public CosmosDbCommandQueue(
            ICosmosDbQueueCollection queueCollection,
            AgentId agentId,
            AssetGroupId assetGroupId)
        {
            this.AssetGroupId = assetGroupId;
            this.AgentId = agentId;
            this.queueCollection = queueCollection;
            this.PartitionKey = CreatePartitionKeyOptimized(this.AgentId, this.AssetGroupId);
            this.parser = new StorageCommandParser(agentId, assetGroupId, QueueStorageType.AzureCosmosDb);
        }

        /// <summary>
        /// Gets the partition key used for this queue.
        /// </summary>
        protected string PartitionKey { get; }

        /// <summary>
        /// The agent ID.
        /// </summary>
        protected AgentId AgentId { get; }

        /// <summary>
        /// The asset group ID.
        /// </summary>
        protected AssetGroupId AssetGroupId { get; }

        /// <summary>
        /// Enqueues the given command.
        /// </summary>
        public Task EnqueueAsync(string moniker, PrivacyCommand command)
        {
            if (this.queueCollection.DatabaseMoniker != moniker)
            {
                throw new InvalidOperationException($"Queue for moniker {this.queueCollection.DatabaseMoniker} does not work for moniker {moniker}");
            }

            StoragePrivacyCommand jsonCommand = this.serializer.Process(command);
            jsonCommand.PartitionKey = this.PartitionKey;
            jsonCommand.TimeToLive = DateTimeHelper.GetTimeToLiveSeconds(command.AbsoluteExpirationTime);

            return this.queueCollection.InsertAsync(jsonCommand);
        }

        /// <summary>
        /// Upsert the given command.
        /// </summary>
        public Task UpsertAsync(string moniker, PrivacyCommand command)
        {
            if (this.queueCollection.DatabaseMoniker != moniker)
            {
                throw new InvalidOperationException($"Queue for moniker {this.queueCollection.DatabaseMoniker} does not work for moniker {moniker}");
            }

            StoragePrivacyCommand jsonCommand = this.serializer.Process(command);
            jsonCommand.PartitionKey = this.PartitionKey;
            jsonCommand.TimeToLive = DateTimeHelper.GetTimeToLiveSeconds(command.AbsoluteExpirationTime);

            return this.queueCollection.UpsertAsync(this.PartitionKey, jsonCommand);
        }

        /// <summary>
        /// Pops the next batch of items off of the queue.
        /// </summary>
        public virtual async Task<CommandQueuePopResult> PopAsync(int maxToPop, TimeSpan? requestedLeaseDuration, CommandQueuePriority commandQueuePriority)
        {
            if (!requestedLeaseDuration.HasValue)
            {
                requestedLeaseDuration = Config.Instance.CosmosDBQueues.GetLeaseDuration(this.AgentId);
            }

            List<Document> response = await this.queueCollection.PopAsync(requestedLeaseDuration.Value, this.PartitionKey, maxToPop);
            List<PrivacyCommand> commands = this.HandleJsonResponse(response);

            return new CommandQueuePopResult(commands, null);
        }

        /// <summary>
        /// Adds statistics to the result bag.
        /// </summary>
        public async Task AddQueueStatisticsAsync(ConcurrentBag<AgentQueueStatistics> resultBag, bool getDetailedStatistics, CancellationToken token)
        {
            AgentQueueStatistics response = await this.queueCollection.GetQueueStatisticsAsync(this.PartitionKey, getDetailedStatistics, token);

            response.DbMoniker = this.queueCollection.DatabaseMoniker;
            response.SubjectType = this.queueCollection.SubjectType;
            response.AgentId = this.AgentId;
            response.AssetGroupId = this.AssetGroupId;
            response.QueryDate = DateTime.UtcNow.Date;

            resultBag.Add(response);
        }

        /// <summary>
        /// Converts the list of documents into a list of Privacy Commands.
        /// </summary>
        protected List<PrivacyCommand> HandleJsonResponse(List<Document> response)
        {
            List<PrivacyCommand> items = new List<PrivacyCommand>();
            foreach (var jsonItem in response)
            {
                PrivacyCommand command = this.parser.Process(jsonItem);

                LeaseReceipt leaseReceipt = new LeaseReceipt(
                    this.queueCollection.DatabaseMoniker,
                    jsonItem.ETag,
                    command,
                    QueueStorageType.AzureCosmosDb);

                command.LeaseReceipt = leaseReceipt;
                items.Add(command);
            }

            return items;
        }

        /// <summary>
        /// Indicates whether the given lease receipt was issued by this Command Queue.
        /// </summary>
        public override bool SupportsLeaseReceipt(LeaseReceipt leaseReceipt)
        {
            return leaseReceipt.DatabaseMoniker == this.queueCollection.DatabaseMoniker &&
                   this.AgentId == leaseReceipt.AgentId &&
                   this.AssetGroupId == leaseReceipt.AssetGroupId &&
                   this.queueCollection.SubjectType == leaseReceipt.SubjectType;
        }

        public bool SupportsQueueFlushByDate => true;

        /// <summary>
        /// Replaces the value of the given command in the queue with this value.
        /// </summary>
        /// <param name="leaseReceipt">The lease receipt (for routing)</param>
        /// <param name="command">The command.</param>
        /// <param name="commandReplaceOperations">The command replace operations. This is not applicable for CosmosDB.</param>
        public async Task<LeaseReceipt> ReplaceAsync(LeaseReceipt leaseReceipt, PrivacyCommand command, CommandReplaceOperations commandReplaceOperations)
        {
            this.CheckLeaseReceipt(leaseReceipt);

            StoragePrivacyCommand result = this.serializer.Process(command);
            result.PartitionKey = this.PartitionKey;
            result.TimeToLive = DateTimeHelper.GetTimeToLiveSeconds(command.AbsoluteExpirationTime);

            string etag = await this.queueCollection.ReplaceAsync(
                result, 
                leaseReceipt.Token);
            
            leaseReceipt = new LeaseReceipt(leaseReceipt.DatabaseMoniker, etag, command, QueueStorageType.AzureCosmosDb);

            return leaseReceipt;
        }

        /// <summary>
        /// Deletes the command identified by the given lease receipt.
        /// </summary>
        public Task DeleteAsync(LeaseReceipt leaseReceipt)
        {
            this.CheckLeaseReceipt(leaseReceipt);
            return this.queueCollection.DeleteAsync(this.PartitionKey, leaseReceipt.CommandId.Value);
        }

        /// <summary>
        /// Attempts to look up a command identified by the given lease receipt.
        /// </summary>
        public async Task<PrivacyCommand> QueryCommandAsync(LeaseReceipt leaseReceipt)
        {
            this.CheckLeaseReceipt(leaseReceipt);

            Document document = await this.queueCollection.QueryAsync(this.PartitionKey, leaseReceipt.CommandId.Value);
            if (document == null)
            {
                return null;
            }

            PrivacyCommand parsedCommand = this.parser.Process(document);

            LeaseReceipt currentLeaseReceipt = new LeaseReceipt(this.queueCollection.DatabaseMoniker, document.ETag, parsedCommand, QueueStorageType.AzureCosmosDb);
            parsedCommand.LeaseReceipt = currentLeaseReceipt;

            return parsedCommand;
        }

        /// <summary>
        /// Call the AgentQueueFlush storedProc to delete all the commands in the agent queues
        /// </summary>
        /// <param name="flushDate">The command creation date of the latest command that needs to be flushed</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>Task</returns>
        public async Task FlushAgentQueueAsync(DateTimeOffset flushDate, CancellationToken token)
        {
            await this.queueCollection.FlushAgentQueueAsync(this.PartitionKey, flushDate, token);
        }

        /// <summary>
        /// Method is used across the project to create command queue partition key.
        /// </summary>
        public static string CreatePartitionKey(AgentId agentId, AssetGroupId assetGroupId) => $"{agentId}.{assetGroupId}";

        /// <summary>
        /// Creates a concatenated string representation of the given objects <code>agentId</code> and <code>assetGrouId</code>, separated by a dot.
        /// This version of the <code>CreatePartitionKey</code> method uses ArrayPool<char> to rent a buffer, 
        /// which is then used to build the resulting string. This approach minimizes memory allocations by reusing a buffer from a pool, 
        /// which can be especially beneficial when dealing with large strings or when the Create method is called frequently. 
        /// The rented buffer is returned to the pool in the finally block to ensure that it is properly returned even if an exception occurs 
        /// during the method execution.
        /// <seealso cref="AgentId"/>
        /// <seealso cref="AssetGroupId"/>
        /// </summary>
        /// <param name="agentId">The agent ID to be included in the concatenated string.</param>
        /// <param name="assetGroupId">The asset group Id to be included in the concatenated string.</param>
        /// <returns>A new string with the format "agentId.assetGroupId", where a and b are the string representations of the input objects.</returns>
        public static string CreatePartitionKeyOptimized(AgentId agentId, AssetGroupId assetGroupId)
        {
            // Get a shared instance of ArrayPool<char>
            var arrayPool = ArrayPool<char>.Shared;
            // Store the string representations of the input objects in local variables
            var agentIdString = agentId.ToString();
            var assetGroupIdString = assetGroupId.ToString();
            // Calculate the maximum length needed for the buffer
            int maxLength = agentIdString.Length + assetGroupIdString.Length + 1;
            // Rent abuffer from the ArrayPool
            char[] buffer = arrayPool.Rent(maxLength);
            try
            {
                // Initialize an index for tracking the position in the buffer
                int index = 0;
                // Copy the first string (agentId.ToString()) to the buffer
                agentIdString.CopyTo(0, buffer, index, agentIdString.Length);
                // Increment the index by the length of the first string
                index += agentIdString.Length;
                // Add the '.' character to the buffer
                buffer[index++] = '.';
                // Copy the second string (assetGroupId.ToString()) to the buffer
                assetGroupIdString.CopyTo(0, buffer, index, assetGroupIdString.Length);
                // Create a new string from the buffer and the calculated length
                return new string(buffer, 0, maxLength);
            }
            finally
            {
                // Return the buffer to the ArrayPool
                arrayPool.Return(buffer);
            }
        }
    }
}

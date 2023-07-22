namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Documents;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    /// <summary>
    /// Provides a low level queue abstraction on top of a single CosmosDB collection.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    public interface ICosmosDbQueueCollection
    {
        /// <summary>
        /// The ID of this collection's database. We store data in many different cosmosDB instances, so it's useful to identify them.
        /// </summary>
        string DatabaseMoniker { get; }

        /// <summary>
        /// The ID of this collection.
        /// </summary>
        string CollectionId { get; }

        /// <summary>
        /// The type of subject that this collection handles.
        /// </summary>
        SubjectType SubjectType { get; }

        /// <summary>
        /// The relative weight of this collection.
        /// </summary>
        int Weight { get; }

        /// <summary>
        /// Initializes the collection.
        /// </summary>
        Task InitializeAsync();
        
        /// <summary>
        /// Inserts a new command.
        /// </summary>
        Task InsertAsync(Document command);

        /// <summary>
        /// Upsert a new command.
        /// </summary>
        Task UpsertAsync(string partitionKey, Document command);

        /// <summary>
        /// Queries for the given command.
        /// </summary>
        /// <returns>The document.</returns>
        Task<Document> QueryAsync(string partitionKey, string commandId);

        /// <summary>
        /// Replaces the given command using the given etag.
        /// </summary>
        /// <returns>The new etag.</returns>
        Task<string> ReplaceAsync(Document replacementDocument, string etag);

        /// <summary>
        /// Deletes the given document.
        /// </summary>
        Task DeleteAsync(string partitionKey, string commandId);

        /// <summary>
        /// Pops the next batch of items off of the queue.
        /// </summary>
        /// <returns>The list of documents popped off of the queue.</returns>
        Task<List<Document>> PopAsync(TimeSpan requestedLeaseDuration, string partitionKey, int maxToPop);

        /// <summary>
        /// Get the Queue statistic data
        /// </summary>
        /// <returns>The list of statistic data per CommandType and CommandStatus for the queue</returns>
        Task<AgentQueueStatistics> GetQueueStatisticsAsync(string partitionKey, bool getDetailedStatistics, CancellationToken token);

        /// <summary>
        /// Call the AgentQueueFlush for the named queue (partition key).
        /// </summary>
        /// <param name="partitionKey">The name of the queue.</param>
        /// <param name="maxFlushDate">Date upto which the commands need to be flushed</param>
        /// <param name="token">The cancellation token.</param>
        Task FlushAgentQueueAsync(string partitionKey, DateTimeOffset maxFlushDate, CancellationToken token);
    }
}
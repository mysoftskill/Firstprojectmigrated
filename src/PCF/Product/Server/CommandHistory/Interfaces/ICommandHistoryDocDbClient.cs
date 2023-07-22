namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandHistory
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.Documents;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    /// <summary>
    /// Interface for mocking DocDb interactions within cold storage v2.
    /// </summary>
    internal interface ICommandHistoryDocDbClient
    {
        /// <summary>
        /// Attempts to insert the given document.
        /// </summary>
        Task InsertAsync(CoreCommandDocument document);

        /// <summary>
        /// Attempts to replace the given document using the given etag.
        /// </summary>
        Task ReplaceAsync(CoreCommandDocument document, string etag);

        /// <summary>
        /// Performs a cross-partition query.
        /// </summary>
        Task<(IEnumerable<CoreCommandDocument> documents, string continuationToken)> CrossPartitionQueryAsync(SqlQuerySpec query, string continuationToken, int maxItemCount = 1000);

        /// <summary>
        /// Performs a max parallelism cross-partition query.
        /// </summary>
        Task<(IEnumerable<CoreCommandDocument> documents, string continuationToken)> MaxParallelismCrossPartitionQueryAsync(SqlQuerySpec query, string continuationToken);

        /// <summary>
        /// Queries a specific document by command ID.
        /// </summary>
        Task<CoreCommandDocument> PointQueryAsync(CommandId commandId);

        /// <summary>
        /// Initializes the client.
        /// </summary>
        Task InitializeAsync();
    }
}

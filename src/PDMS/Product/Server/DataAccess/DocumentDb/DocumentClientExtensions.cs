namespace Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.Documents.Linq;

    /// <summary>
    /// Extensions to the document client.
    /// </summary>
    public static class DocumentClientExtensions
    {
        /// <summary>
        /// Makes a document query asynchronous.
        /// </summary>
        /// <typeparam name="T">The response type.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="documentQueryFactory">The a factory to manipulate the query.</param>
        /// <returns>A task that executes the query.</returns>
        public static Task<IEnumerable<T>> QueryAsync<T>(this IQueryable<T> query, IDocumentQueryFactory documentQueryFactory)
        {
            return QueryAsync(query.AsDocumentQuery(), documentQueryFactory);
        }

        /// <summary>
        /// Makes a document query asynchronous.
        /// </summary>
        /// <typeparam name="T">The response type.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="documentQueryFactory">The a factory to manipulate the query.</param>
        /// <param name="queryAllBatches">Whether or not this method should continue to execute each batch if there is a continuation token.</param>
        /// <returns>A task that executes the query.</returns>
        public static async Task<IEnumerable<T>> QueryAsync<T>(this IDocumentQuery<T> query, IDocumentQueryFactory documentQueryFactory, bool queryAllBatches = true)
        {
            var docQuery = documentQueryFactory.Decorate(query);

            var batches = new List<IEnumerable<T>>();

            do
            {
                var batch = await docQuery.ExecuteNextAsync<T>().ConfigureAwait(false);

                batches.Add(batch);
            }
            while (docQuery.HasMoreResults && queryAllBatches);

            var docs = batches.SelectMany(b => b);

            return docs;
        }

        /// <summary>
        /// Makes a document query asynchronous.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="documentQueryFactory">The a factory to manipulate the query.</param>
        /// <returns>A task that executes the query.</returns>
        public static async Task<IEnumerable<dynamic>> QueryAsync(this IQueryable<dynamic> query, IDocumentQueryFactory documentQueryFactory)
        {
            var docQuery = documentQueryFactory.Decorate(query.AsDocumentQuery());

            var batches = new List<IEnumerable<dynamic>>();

            do
            {
                var batch = await docQuery.ExecuteNextAsync().ConfigureAwait(false);

                batches.Add(batch);
            }
            while (docQuery.HasMoreResults);

            var docs = batches.SelectMany(b => b);

            return docs;
        }
    }
}
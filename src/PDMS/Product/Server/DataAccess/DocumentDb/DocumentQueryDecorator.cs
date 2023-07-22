namespace Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Azure.Documents.Client;

    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Linq;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac.Sll;

    /// <summary>
    /// An decorator that instruments IDocumentQuery.    
    /// </summary>
    /// <typeparam name="T">The query type.</typeparam>
    public sealed class DocumentQueryDecorator<T> : IDocumentQuery<T>
    {
        private readonly string apiName = "DocumentDB.Query.ExecuteNextAsync";
        private readonly IDocumentQuery<T> original;
        private readonly ISessionFactory sessionFactory;
        private readonly string queryUri;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentQueryDecorator{T}"/> class.
        /// </summary>
        /// <param name="sessionFactory">The session factory used for instrumenting calls.</param>
        /// <param name="original">The original document query.</param>
        public DocumentQueryDecorator(ISessionFactory sessionFactory, IDocumentQuery<T> original)
        {
            this.sessionFactory = sessionFactory;
            this.original = original;
            this.queryUri = original.ToString();
        }

        /// <summary>
        /// Gets a value indicating whether or not the original has more results.
        /// </summary>
        public bool HasMoreResults
        {
            get
            {
                return this.original.HasMoreResults;
            }
        }

        /// <summary>
        /// Disposes the original query.
        /// </summary>
        public void Dispose()
        {
            this.original.Dispose();
        }

        /// <summary>
        /// Instrument the ExecuteNextAsync query.
        /// </summary>
        /// <param name="token">A token for the task.</param>
        /// <returns>The original value.</returns>
        public Task<FeedResponse<dynamic>> ExecuteNextAsync(CancellationToken token = default(CancellationToken))
        {
            return this.Instrument(() => this.original.ExecuteNextAsync(token));
        }

        /// <summary>
        /// Instrument the ExecuteNextAsync query.
        /// </summary>
        /// <typeparam name="TResult">The query type.</typeparam>
        /// <param name="token">A token for the task.</param>
        /// <returns>The original value.</returns>
        public Task<FeedResponse<TResult>> ExecuteNextAsync<TResult>(CancellationToken token = default(CancellationToken))
        {
            return this.Instrument(() => this.original.ExecuteNextAsync<TResult>(token));
        }

        /// <summary>
        /// Wraps a function with instrumentation and tuple it's response with the query url.
        /// This way we can log the query information in addition to response data.
        /// </summary>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <param name="func">The function to instrument.</param>
        /// <returns>The response value.</returns>
        private async Task<FeedResponse<TResponse>> Instrument<TResponse>(Func<Task<FeedResponse<TResponse>>> func)
        {
            var session = this.sessionFactory.StartSession(this.apiName, SessionType.Outgoing);
            try
            {
                var result = await func().ConfigureAwait(false);
                session.Success(new DocumentResult
                {
                    ActivityId = result.ActivityId,
                    RequestCharge = result.RequestCharge,
                    RequestUri = this.queryUri,
                    Count = result.Count
                });
                return result;
            }
            catch (DocumentClientException exn)
            {
                session.Error(Tuple.Create(this.queryUri, exn));
                throw;
            }
        }
    }
}
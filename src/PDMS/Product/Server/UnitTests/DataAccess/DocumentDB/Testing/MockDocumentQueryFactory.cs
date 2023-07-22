namespace Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;

    using Ploeh.AutoFixture;

    /// <summary>
    /// A mock IDocumentQueryFactory that overrides Decorate so that a mock IDocumentQuery can be returned.
    /// This is useful for mocking the response for IDocumentQuery.
    /// </summary>
    public class MockDocumentQueryFactory : IDocumentQueryFactory
    {
        private readonly IEnumerable<dynamic> response;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockDocumentQueryFactory"/> class.
        /// </summary>
        /// <param name="response">The response to return.</param>
        public MockDocumentQueryFactory(IEnumerable<dynamic> response = null)
        {
            this.response = response ?? Enumerable.Empty<dynamic>();
        }

        /// <summary>
        /// Freezes an instance of the mock class as IDocumentQueryFactory on the given fixture.
        /// This way any class created by AutoFixture that needs an IDocumentQueryFactory and IDocumentClient
        /// will use the associated mocked class.
        /// </summary>
        /// <param name="fixture">The fixture to update.</param>
        /// <param name="response">A response to return from the query when executed.</param>
        public static void FreezeOnFixture(IFixture fixture, IEnumerable<dynamic> response = null)
        {
            fixture.Inject<IDocumentClient>(new DocumentClient(new Uri("https://test"), string.Empty));
            fixture.Inject<IDocumentQueryFactory>(new MockDocumentQueryFactory(response));
        }

        /// <summary>
        /// Replace the query with a mock value.
        /// </summary>
        /// <typeparam name="T">The query type.</typeparam>
        /// <param name="original">The original query.</param>
        /// <returns>The decorated query.</returns>
        public IDocumentQuery<T> Decorate<T>(IDocumentQuery<T> original)
        {
            var x = original.ToString(); // This forces the query to be generated, which fails if it is invalid.
            return new MockDocumentQuery<T>(this.response);
        }

        private class MockDocumentQuery<T> : IDocumentQuery<T>
        {
            private readonly IEnumerable<dynamic> response;

            public MockDocumentQuery(IEnumerable<dynamic> response)
            {
                this.response = response;
            }

            public bool HasMoreResults
            {
                get
                {
                    return false;
                }
            }

            public void Dispose()
            {
            }

            public Task<FeedResponse<dynamic>> ExecuteNextAsync(CancellationToken token = default(CancellationToken))
            {
                return Task.FromResult(new FeedResponse<dynamic>(this.response));
            }

            public Task<FeedResponse<TResult>> ExecuteNextAsync<TResult>(CancellationToken token = default(CancellationToken))
            {
                return Task.FromResult(new FeedResponse<TResult>(this.response.Select(m => (TResult)m)));
            }
        }
    }
}
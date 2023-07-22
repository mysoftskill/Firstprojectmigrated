namespace Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb.UnitTest
{
    using System;
    using System.Net;
    using System.Threading.Tasks;

    using global::Autofac;

    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac;
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;

    public class DocumentClientInterceptorTest
    {
        [Theory(DisplayName = "When an IDocumentClient call succeeds, then log success."), AutoMoqData]
        public async Task VerifyLogSuccess(
            Mock<IDocumentClient> documentClient,
            [Frozen] Mock<ISession> session,
            Mock<ISessionFactory> sessionFactory,
            Uri documentUri)
        {
            var client = this.CreateClient(sessionFactory.Object, new MockDocumentClient(documentClient.Object));

            await client.ReadDocumentAsync(documentUri).ConfigureAwait(false);

            Action<Tuple<string, ResourceResponse<Document>>> verify = v => Assert.Equal(documentUri.ToString(), v.Item1);

            session.Verify(m => m.Done(SessionStatus.Success, Is.Value(verify)), Times.Once());
            sessionFactory.Verify(m => m.StartSession("DocumentDB.ReadDocumentAsync", SessionType.Outgoing));
        }

        [Theory(DisplayName = "When an IDocumentClient call has no url, then default to empty string."), AutoMoqData]
        public async Task VerifyEmptyUrl(
            Mock<IDocumentClient> documentClient,
            [Frozen] Mock<ISession> session,
            Mock<ISessionFactory> sessionFactory)
        {
            var client = this.CreateClient(sessionFactory.Object, new MockDocumentClient(documentClient.Object));

            await client.CreateDatabaseAsync(null).ConfigureAwait(false);

            Action<Tuple<string, ResourceResponse<Database>>> verify = v => Assert.Equal(string.Empty, v.Item1);

            session.Verify(m => m.Done(SessionStatus.Success, Is.Value(verify)), Times.Once());
        }

        [Theory(DisplayName = "When an IDocumentClient call is not async, then do not instrument."), AutoMoqData]
        public void VerifyNonAsync(Mock<IDocumentClient> documentClient, Uri documentUri)
        {
            var sessionFactory = new Mock<ISessionFactory>(MockBehavior.Strict); // Use strict to ensure no functions are ever called.

            var client = this.CreateClient(sessionFactory.Object, new MockDocumentClient(documentClient.Object));

            client.CreateDocumentQuery(documentUri);
        }

        [Theory(DisplayName = "When an IDocumentClient call fails, then log error."), AutoMoqData]
        public async Task VerifyLogError(
            Mock<IDocumentClient> documentClient,
            [Frozen] Mock<ISession> session,
            Mock<ISessionFactory> sessionFactory,
            string documentUri,
            HttpStatusCode statusCode)
        {
            var mockClient = new MockDocumentClient(documentClient.Object);
            mockClient.DocumentClient
                .Setup(m => m.ReadDatabaseAsync(It.IsAny<string>(), It.IsAny<RequestOptions>()))
                .ThrowsAsync(DocumentClientExceptionModule.Create(statusCode));

            var client = this.CreateClient(sessionFactory.Object, mockClient);

            await Assert.ThrowsAsync<DocumentClientException>(() => client.ReadDatabaseAsync(documentUri)).ConfigureAwait(false);

            Action<Tuple<string, DocumentClientException>> verify = v =>
            {
                Assert.Equal(documentUri, v.Item1);
                Assert.Equal(statusCode, v.Item2.StatusCode);
            };

            session.Verify(m => m.Done(SessionStatus.Error, Is.Value(verify)), Times.Once());
            sessionFactory.Verify(m => m.StartSession("DocumentDB.ReadDatabaseAsync", SessionType.Outgoing));
        }

        private IDocumentClient CreateClient(ISessionFactory sessionFactory, MockDocumentClient documentClient)
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new DocumentDbModule());
            builder.RegisterInstance(sessionFactory);
            builder.RegisterInstance(documentClient).Named<IDocumentClient>("Instance"); // Override the default registered value.

            return builder.Build().Resolve<IDocumentClient>();
        }
    }
}
namespace Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Threading;
    using System.Threading.Tasks;

    using global::Autofac;

    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Linq;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac.Sll;
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Xunit2;

    using Xunit;

    public class DocumentQueryDecoratorTest
    {
        [Theory(DisplayName = "When an IDocumentQuery call succeeds, then log success."), AutoMoqData]
        public async Task VerifyLogSuccess(
            [Frozen] Mock<ISession> session,
            Mock<ISessionFactory> sessionFactory,
            string activityId,
            double requestCharge,
            Fixture fixture)
        {
            var headers = new NameValueCollection();
            headers.Add("x-ms-activity-id", activityId);
            headers.Add("x-ms-request-charge", requestCharge.ToString());

            var expectedResponse = FeedResponseModule.Create<string>(fixture.Create<IEnumerable<string>>(), headers);
            
            var documentQuery = new Mock<IDocumentQuery<string>>();
            documentQuery.Setup(m => m.ExecuteNextAsync<string>(CancellationToken.None)).ReturnsAsync(expectedResponse);

            var query = this.CreateQuery(sessionFactory.Object, documentQuery.Object);

            var actualResponse = await query.ExecuteNextAsync<string>().ConfigureAwait(false);

            Assert.Equal(expectedResponse, actualResponse); // Response should be unchanged.

            Action<DocumentResult> verify = v => 
            {
                Assert.Equal(activityId, v.ActivityId);
                Assert.Equal(requestCharge, v.RequestCharge);
                Assert.Equal(documentQuery.Object.ToString(), v.RequestUri);
            };

            session.Verify(m => m.Done(SessionStatus.Success, Is.Value(verify)), Times.Once());
            sessionFactory.Verify(m => m.StartSession("DocumentDB.Query.ExecuteNextAsync", SessionType.Outgoing));
        }

        [Theory(DisplayName = "When an IDocumentQuery call fails, then log error."), AutoMoqData]
        public async Task VerifyLogError(
            [Frozen] Mock<ISession> session,
            Mock<ISessionFactory> sessionFactory,
            string message)
        {
            var documentQuery = new Mock<IDocumentQuery<string>>();
            documentQuery
                .Setup(m => m.ExecuteNextAsync<string>(CancellationToken.None))
                .ThrowsAsync(new DocumentQueryException(message));

            var query = this.CreateQuery(sessionFactory.Object, documentQuery.Object);

            await Assert.ThrowsAsync<DocumentQueryException>(() => query.ExecuteNextAsync<string>()).ConfigureAwait(false);
            
            Action<Tuple<string, DocumentClientException>> verify = v =>
            {
                Assert.Equal(documentQuery.Object.ToString(), v.Item1);
            };

            session.Verify(m => m.Done(SessionStatus.Error, Is.Value(verify)), Times.Once());
            sessionFactory.Verify(m => m.StartSession("DocumentDB.Query.ExecuteNextAsync", SessionType.Outgoing));
        }

        private IDocumentQuery<T> CreateQuery<T>(ISessionFactory sessionFactory, IDocumentQuery<T> original)
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new DocumentDbModule());
            builder.RegisterInstance(sessionFactory);
            
            return builder.Build().Resolve<IDocumentQueryFactory>().Decorate(original);
        }
    }
}
namespace Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb.UnitTest
{
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using DocumentDB.Models;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using DM = Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb.DocumentModule;
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;

    public class DocumentModuleTest
    {
        [Theory(DisplayName = "Verify Read queries the database."), TestData]
        public async Task VerifyReadWithoutHandler(TestData data, [Frozen] Mock<IDocumentClient> documentClient, DM.DocumentContext context)
        {
            var result = await DM.Read<TestData>(data.Id, context).ConfigureAwait(false);

            Assert.Equal(data.Id, result.Id);

            documentClient.Verify(m => m.ReadDocumentAsync(It.Is<Uri>(x => x.ToString().Contains(data.Id.ToString())), null, default(CancellationToken)), Times.Once);
        }

        [Theory(DisplayName = "When a document is not found on read, then return null."), TestData]
        public async Task When_DocumentNotFoundOnReady_Then_ConvertToNull([Frozen] Mock<IDocumentClient> documentClient, DM.DocumentContext context)
        {
            documentClient.Setup(m => m.ReadDocumentAsync(It.IsAny<Uri>(), null, default(CancellationToken))).ThrowsAsync(DocumentClientExceptionModule.Create(System.Net.HttpStatusCode.NotFound));

            var result = await DM.Read<TestData>(Guid.Empty, context).ConfigureAwait(false);

            Assert.Null(result);
        }

        [Theory(DisplayName = "When an exception occurs on read, then rethrow."), TestData]
        public async Task When_ExceptionOnReady_Then_Rethrow([Frozen] Mock<IDocumentClient> documentClient, DM.DocumentContext context)
        {
            documentClient.Setup(m => m.ReadDocumentAsync(It.IsAny<Uri>(), null, default(CancellationToken))).ThrowsAsync(DocumentClientExceptionModule.Create(HttpStatusCode.ExpectationFailed));

            var exn = await Assert.ThrowsAsync<DocumentClientException>(() => DM.Read<TestData>(Guid.Empty, context)).ConfigureAwait(false);

            Assert.Equal(HttpStatusCode.ExpectationFailed, exn.StatusCode);
        }

        [Theory(DisplayName = "When read handler returns null, then return null."), TestData]
        public async Task When_ReadHandlerReturnsNull_Then_ReturnNull(DM.DocumentContext context)
        {           
            var result = await DM.Read<TestData>(Guid.Empty, context, _ => null).ConfigureAwait(false);

            Assert.Null(result);
        }

        [Theory(DisplayName = "When read handler alters the data, then return that value."), TestData]
        public async Task When_ReadHandlerChangesData_Then_ReturnChanges(TestData data, DM.DocumentContext context)
        {
            var result = await DM.Read<TestData>(Guid.Empty, context, _ => data).ConfigureAwait(false);

            Assert.Equal(data, result);
        }

        [Theory(DisplayName = "Verify create calls the database."), TestData]
        public async Task VerifyCreate(TestData data, [Frozen] Mock<IDocumentClient> documentClient, DM.DocumentContext context)
        {
            var result = await DM.Create<TestData>(data, context).ConfigureAwait(false);

            Assert.Equal(data.Id, result.Id);

            documentClient.Verify(m => m.CreateDocumentAsync(It.Is<Uri>(x => x.ToString().Contains(context.CollectionName.ToString())), data, null, false, default(CancellationToken)), Times.Once);
        }

        [Theory(DisplayName = "When create is called with a null document, then throw exception."), TestData]
        public async Task When_NullDocumentGivenToCreate_Then_Fail(DM.DocumentContext context)
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => DM.Create<TestData>(null, context)).ConfigureAwait(false);
        }

        [Theory(DisplayName = "Verify update calls the database."), TestData]
        public async Task VerifyUpdate(TestData data, [Frozen] Mock<IDocumentClient> documentClient, DM.DocumentContext context)
        {
            var result = await DM.Update<TestData>(data, context).ConfigureAwait(false);

            Assert.Equal(data.Id, result.Id);

            documentClient.Verify(
                m => m.ReplaceDocumentAsync(
                    It.Is<Uri>(x => x.ToString().Contains(data.Id.ToString())), 
                    data,
                    It.Is<RequestOptions>(o => 
                        o.AccessCondition.Type == AccessConditionType.IfMatch &&
                        o.AccessCondition.Condition == data.ETag),
                    default(CancellationToken)),
                Times.Once);
        }

        [Theory(DisplayName = "When update is called with a null document, then throw exception."), TestData]
        public async Task When_NullDocumentGivenToUpdate_Then_Fail(DM.DocumentContext context)
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => DM.Update<TestData>(null, context)).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When not found exception occurs on update and no handler provided, then rethrow."), TestData]
        public async Task When_NotFoundUpdateWithNoHandler_Then_Rethrow(
            TestData data,
            [Frozen] Mock<IDocumentClient> documentClient, 
            DM.DocumentContext context)
        {
            documentClient.Setup(m => m.ReplaceDocumentAsync(It.IsAny<Uri>(), data, It.IsAny<RequestOptions>(), default(CancellationToken))).ThrowsAsync(DocumentClientExceptionModule.Create(HttpStatusCode.NotFound));

            var exn = await Assert.ThrowsAsync<DocumentClientException>(() => DM.Update<TestData>(data, context)).ConfigureAwait(false);

            Assert.Equal(HttpStatusCode.NotFound, exn.StatusCode);
        }
        
        [Theory(DisplayName = "When not found exception occurs on update and a handler is provided, then convert exception."), TestData]
        public async Task When_NotFoundUpdateWithandler_Then_ConvertException(
            TestData data,
            [Frozen] Mock<IDocumentClient> documentClient,
            DM.DocumentContext context)
        {
            documentClient.Setup(m => m.ReplaceDocumentAsync(It.IsAny<Uri>(), data, It.IsAny<RequestOptions>(), default(CancellationToken))).ThrowsAsync(DocumentClientExceptionModule.Create(HttpStatusCode.NotFound));

            var exn = await Assert.ThrowsAsync<Exception>(() => DM.Update<TestData>(data, context, _ => new Exception("test"))).ConfigureAwait(false);

            Assert.Equal("test", exn.Message);
        }

        [Theory(DisplayName = "When etag mismatch exception occurs on update and no handler provided, then rethrow."), TestData]
        public async Task When_ETagMismatchUpdateWithNoHandler_Then_Rethrow(
            TestData data,
            [Frozen] Mock<IDocumentClient> documentClient,
            DM.DocumentContext context)
        {
            documentClient.Setup(m => m.ReplaceDocumentAsync(It.IsAny<Uri>(), data, It.IsAny<RequestOptions>(), default(CancellationToken))).ThrowsAsync(DocumentClientExceptionModule.Create(HttpStatusCode.PreconditionFailed));

            var exn = await Assert.ThrowsAsync<DocumentClientException>(() => DM.Update<TestData>(data, context)).ConfigureAwait(false);

            Assert.Equal(HttpStatusCode.PreconditionFailed, exn.StatusCode);
        }

        [Theory(DisplayName = "When etag mismatch exception occurs on update and a handler is provided, then convert exception."), TestData]
        public async Task When_ETagMismatchUpdateWithandler_Then_ConvertException(
            TestData data,
            [Frozen] Mock<IDocumentClient> documentClient,
            DM.DocumentContext context)
        {
            documentClient.Setup(m => m.ReplaceDocumentAsync(It.IsAny<Uri>(), data, It.IsAny<RequestOptions>(), default(CancellationToken))).ThrowsAsync(DocumentClientExceptionModule.Create(HttpStatusCode.PreconditionFailed));

            var exn = await Assert.ThrowsAsync<Exception>(() => DM.Update<TestData>(data, context, etagMismatchHandler: _ => new Exception("test"))).ConfigureAwait(false);

            Assert.Equal("test", exn.Message);
        }

        [Theory(DisplayName = "When unknown exception occurs on update and a handler is provided, then rethrow."), TestData]
        public async Task When_UnknownExceptionUpdateWithandler_Then_Rethrow(
            TestData data,
            [Frozen] Mock<IDocumentClient> documentClient,
            DM.DocumentContext context)
        {
            documentClient.Setup(m => m.ReplaceDocumentAsync(It.IsAny<Uri>(), data, It.IsAny<RequestOptions>(), default(CancellationToken))).ThrowsAsync(DocumentClientExceptionModule.Create(HttpStatusCode.PaymentRequired));

            Func<Exception, Exception> convert = _ => new Exception();

            var exn = await Assert.ThrowsAsync<DocumentClientException>(() => DM.Update<TestData>(data, context, convert, convert)).ConfigureAwait(false);

            Assert.Equal(HttpStatusCode.PaymentRequired, exn.StatusCode);
        }
        
        public class TestData : DocumentBase<Guid>
        {
        }

        public class TestDataAttribute : AutoMoqDataAttribute
        {
            public TestDataAttribute()
                : base(true)
            {
                var data = this.Fixture.Freeze<TestData>();

                // Ensures something is created.
                this.Fixture.Inject(new ResourceResponse<Document>(DocumentModule.Create(data)));
            }
        }
    }
}
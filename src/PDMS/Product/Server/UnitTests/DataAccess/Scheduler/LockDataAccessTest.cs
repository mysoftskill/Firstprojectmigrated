namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Scheduler.UnitTest
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Azure.Documents.Client;

    using Common.Configuration;

    using Microsoft.Azure.Documents;
    using Microsoft.PrivacyServices.Testing;

    using Moq;

    using Ploeh.AutoFixture.Xunit2;

    using Xunit;

    public class LockDataAccessTest
    {
        [Theory(DisplayName = "When create locker state, the document DB create operation should be called."), AutoMoqData]
        public async Task VerifyCreateOperation(
            [Frozen] Mock<IDocumentClient> documentClientMock,
            [Frozen] Mock<IDocumentDatabaseConfig> configMock,
            Lock<string> state)
        {
            var access = new LockDataAccess<string>(documentClientMock.Object, configMock.Object);
            ResourceResponse<Document> response = new ResourceResponse<Document>(new Document());

            documentClientMock
                .Setup(m => m.CreateDocumentAsync(It.IsAny<Uri>(), It.IsAny<object>(), It.IsAny<RequestOptions>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));

            var lockState = await access.CreateAsync(state).ConfigureAwait(false);

            documentClientMock.Verify(m => m.CreateDocumentAsync(It.IsAny<Uri>(), It.IsAny<object>(), It.IsAny<RequestOptions>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.NotNull(lockState);
        }

        [Theory(DisplayName = "When read locker state, the document DB read operation should be called."), AutoMoqData]
        public async Task VerifyGetOperation(
            [Frozen] Mock<IDocumentClient> documentClientMock,
            [Frozen] Mock<IDocumentDatabaseConfig> configMock)
        {
            var access = new LockDataAccess<string>(documentClientMock.Object, configMock.Object);
            ResourceResponse<Document> response = new ResourceResponse<Document>(new Document());

            documentClientMock
                .Setup(m => m.ReadDocumentAsync(It.IsAny<Uri>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));

            var lockState = await access.GetAsync("lock").ConfigureAwait(false);

            documentClientMock.Verify(m => m.ReadDocumentAsync(It.IsAny<Uri>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.NotNull(lockState);
        }

        [Theory(DisplayName = "When read locker state with null name, the document DB read operation should throw exception."), AutoMoqData]
        public async Task VerifyGetOperationThrowsException(
            [Frozen] Mock<IDocumentClient> documentClientMock,
            [Frozen] Mock<IDocumentDatabaseConfig> configMock)
        {
            var access = new LockDataAccess<string>(documentClientMock.Object, configMock.Object);

            await Assert.ThrowsAsync<ArgumentNullException>(() => access.GetAsync(null)).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When update locker state, the document DB update operation should be called."), AutoMoqData]
        public async Task VerifyUpdateOperation(
            [Frozen] Mock<IDocumentClient> documentClientMock,
            [Frozen] Mock<IDocumentDatabaseConfig> configMock,
            Lock<string> state)
        {
            var access = new LockDataAccess<string>(documentClientMock.Object, configMock.Object);
            ResourceResponse<Document> response = new ResourceResponse<Document>(new Document());

            documentClientMock
                .Setup(m => m.ReplaceDocumentAsync(It.IsAny<Uri>(), It.IsAny<object>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));

            var lockState = await access.UpdateAsync(state).ConfigureAwait(false);

            documentClientMock.Verify(m => m.ReplaceDocumentAsync(It.IsAny<Uri>(), It.IsAny<object>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.NotNull(lockState);
        }
    }
}

namespace Microsoft.PrivacyServices.DataManagement.DataAccess.ActiveDirectory.UnitTest
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.ActiveDirectory;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb.UnitTest;
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Xunit2;

    using Xunit;

    public class ActiveDirectoryCacheTest
    {
        [Theory(DisplayName = "When the cache storage is read, then return document db data."), AutoMoqData]
        public async Task When_CacheRead_Then_ReturnDBData(
            [Frozen] Mock<IDocumentClient> documentClient,
            CacheData data,
            ActiveDirectoryCache cacheStorage, 
            AuthenticatedPrincipal principal,
            IFixture fixture)
        {
            fixture.Inject(this.CreateDocument(data));

            var databaseData = await cacheStorage.ReadDataAsync(principal).ConfigureAwait(false);

            documentClient.Verify(m => m.ReadDocumentAsync(It.IsAny<Uri>(), null, default(CancellationToken)), Times.Once);

            data.SecurityGroupIds.SequenceLike<Guid>(databaseData.SecurityGroupIds);
        }

        [Theory(DisplayName = "When the cache storage is read, then use proper id."), AutoMoqData]
        public async Task When_CacheRead_Then_UseProperId(
            [Frozen] IDataAccessConfiguration config,
            [Frozen] Mock<IDocumentClient> documentClient,
            CacheData data,
            ActiveDirectoryCache cacheStorage,
            AuthenticatedPrincipal principal,
            IFixture fixture)
        {
            fixture.Inject(this.CreateDocument(data));

            await cacheStorage.ReadDataAsync(principal).ConfigureAwait(false);

            Action<Uri> verify = uri =>
            {
                var value = uri.ToString().Split('/').Last();
                Assert.Equal($"{config.ActiveDirectoryCacheIdPrefix}_{principal.UserId.ToLowerInvariant()}", value);
            };

            documentClient.Verify(m => m.ReadDocumentAsync(Is.Value(verify), null, default(CancellationToken)), Times.Once);
        }

        [Theory(DisplayName = "When the cache storage is created, then use proper id."), AutoMoqData]
        public async Task When_CacheCreated_Then_UseProperId(
            [Frozen] IDataAccessConfiguration config,
            [Frozen] Mock<IDocumentClient> documentClient,
            CacheData cacheData,
            ActiveDirectoryCache cacheStorage,
            AuthenticatedPrincipal principal,
            IFixture fixture)
        {
            fixture.Inject(this.CreateDocument(cacheData));

            await cacheStorage.CreateDataAsync(principal, cacheData).ConfigureAwait(false);

            Action<CacheData> verify = v =>
            {
                Assert.Equal($"{config.ActiveDirectoryCacheIdPrefix}_{principal.UserId.ToLowerInvariant()}", v.Id);
            };

            documentClient.Verify(m => m.CreateDocumentAsync(It.IsAny<Uri>(), Is.Value(verify), null, false, default(CancellationToken)), Times.Once);
        }

        [Theory(DisplayName = "When the cache storage is updated, then use proper id."), AutoMoqData]
        public async Task When_CacheUpdated_Then_UseProperId(
            [Frozen] IDataAccessConfiguration config,
            [Frozen] Mock<IDocumentClient> documentClient,
            CacheData cacheData,
            ActiveDirectoryCache cacheStorage,
            AuthenticatedPrincipal principal,
            IFixture fixture)
        {
            fixture.Inject(this.CreateDocument(cacheData));

            await cacheStorage.UpdateDataAsync(principal, cacheData).ConfigureAwait(false);

            Action<CacheData> verify = v =>
            {
                Assert.Equal($"{config.ActiveDirectoryCacheIdPrefix}_{principal.UserId.ToLowerInvariant()}", v.Id);
            };

            documentClient.Verify(m => m.ReplaceDocumentAsync(It.IsAny<Uri>(), Is.Value(verify), It.IsAny<RequestOptions>(), default(CancellationToken)), Times.Once);
        }

        private ResourceResponse<Document> CreateDocument(CacheData data)
        {
            return new ResourceResponse<Document>(DocumentModule.Create(data));
        }
    }
}
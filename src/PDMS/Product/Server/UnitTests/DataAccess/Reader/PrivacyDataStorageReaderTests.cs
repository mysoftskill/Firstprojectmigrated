namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Reader.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb.UnitTest;
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;

    public class PrivacyDataStorageReaderTests
    {
        [Theory(DisplayName = "When GetDataOwnerAsync is called, then DocumentClient.ReadDocumentAsync is invoked."), AutoMoqData]
        public async Task VerifyDataOwnerGet(
            [Frozen] Mock<IDocumentClient> documentClientMock,
            PrivacyDataStorageReader reader)
        {
            await this.VerifyEntityGet<DataOwner>(documentClientMock, reader.GetDataOwnerAsync).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When GetDataOwnerAsync is called with invalid entity type, then DocumentClient.ReadDocumentAsync will return null."), AutoMoqData(DisableRecursionCheck = true)]
        public async Task VerifyDataOwnerGetWithInvalidEntityType(
            [Frozen] Mock<IDocumentClient> documentClientMock,
            PrivacyDataStorageReader reader,
            DeleteAgent dataAgent)
        {
            await this.VerifyEntityGetWithInvalidEntityType<DataOwner>(documentClientMock, reader.GetDataOwnerAsync, dataAgent).ConfigureAwait(false);
        }

        [Theory(DisplayName = "Verify document db filter conversions."), AutoMoqData]
        [InlineAutoMoqData(StringComparisonType.Equals)]
        [InlineAutoMoqData(StringComparisonType.Contains)]
        public async Task VerifyFilterConversionsForConfiguration(
            StringComparisonType stringComparison,
            Fixture fixture)
        {
            MockDocumentQueryFactory.FreezeOnFixture(fixture);

            var reader = fixture.Create<PrivacyDataStorageReader>();

            var filterCriteria = new DataOwnerFilterCriteria
            {
                Name = new StringFilter("test", stringComparison),
                Index = 0,
                Count = 10
            };

            await reader.GetDataOwnersAsync(filterCriteria, false, false).ConfigureAwait(false); // If this doesn't fail, then the query is valid.
        }

        [Theory(DisplayName = "Verify sql generation for GetDataOwnersBySecurityGroupsAsync."), AutoMoqData]
        public async Task VerifySqlGenerationForGetDataOwnersBySecurityGroupsAsync(Fixture fixture, IEnumerable<Guid> securityGroupIds)
        {
            MockDocumentQueryFactory.FreezeOnFixture(fixture);

            var reader = fixture.Create<PrivacyDataStorageReader>();
            
            await reader.GetDataOwnersBySecurityGroupsAsync(securityGroupIds, false, false).ConfigureAwait(false); // If this doesn't fail, then the query is valid.
        }

        [Theory(DisplayName = "Verify sql generation for GetSharingRequestsAsync."), AutoMoqData]
        public async Task VerifySqlGenerationForGetSharingRequestsAsync(Fixture fixture)
        {
            MockDocumentQueryFactory.FreezeOnFixture(fixture);

            var reader = fixture.Create<PrivacyDataStorageReader>();

            var filterCriteria = new SharingRequestFilterCriteria
            {
                DeleteAgentId = Guid.NewGuid(),
                OwnerId = Guid.NewGuid(),
                AssetGroupId = Guid.NewGuid(),
                Index = 0,
                Count = 10
            };
            
            await reader.GetSharingRequestsAsync(filterCriteria, false).ConfigureAwait(false); // If this doesn't fail, then the query is valid.
        }

        [Theory(DisplayName = "Verify sql generation for GetVariantRequestsAsync."), AutoMoqData]
        public async Task VerifySqlGenerationForGetVariantRequestsAsync(Fixture fixture)
        {
            MockDocumentQueryFactory.FreezeOnFixture(fixture);

            var reader = fixture.Create<PrivacyDataStorageReader>();

            var filterCriteria = new VariantRequestFilterCriteria
            {
                OwnerId = Guid.NewGuid(),
                AssetGroupId = Guid.NewGuid(),
                Index = 0,
                Count = 10
            };

            await reader.GetVariantRequestsAsync(filterCriteria, false).ConfigureAwait(false); // If this doesn't fail, then the query is valid.
        }

        /// <summary>
        /// Helper function to verify entity get method.
        /// </summary>
        /// <typeparam name="T">The parameter type.</typeparam>
        /// <param name="documentClientMock">Mock for IDocumentClient type.</param>
        /// <param name="getEntityAsyncMethod">The entity get method to be verified.</param>
        /// <returns>A task that runs the test.</returns>
        private async Task VerifyEntityGet<T>(
            Mock<IDocumentClient> documentClientMock,
            Func<Guid, bool, bool, Task<T>> getEntityAsyncMethod)
        {
            Guid entityId = Guid.NewGuid();

            ResourceResponse<Document> response = new ResourceResponse<Document>(new Document());
            Action<Uri> verify = uri =>
            {
                Assert.EndsWith(entityId.ToString(), uri.ToString());
            };

            // Return response is required since mock object creates Response but however Response.Resource 
            // throws NullReferenceException.
            documentClientMock
                .Setup(m => m.ReadDocumentAsync(Is.Value(verify), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));

            await getEntityAsyncMethod(entityId, false, false).ConfigureAwait(false);

            documentClientMock.Verify(m => m.ReadDocumentAsync(It.IsAny<Uri>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        private async Task VerifyEntityGetWithInvalidEntityType<T>(
            Mock<IDocumentClient> documentClientMock,
            Func<Guid, bool, bool, Task<T>> getEntityAsyncMethod,
            Entity responseEntity)
        {
            Guid entityId = Guid.NewGuid();

            ResourceResponse<Document> response = new ResourceResponse<Document>(DocumentModule.Create(responseEntity));

            // Return response is required since mock object creates Response but however Response.Resource 
            // throws NullReferenceException.
            documentClientMock
                .Setup(m => m.ReadDocumentAsync(It.IsAny<Uri>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));

            var result = await getEntityAsyncMethod(entityId, false, false).ConfigureAwait(false);

            Assert.Null(result);
        }
    }
}

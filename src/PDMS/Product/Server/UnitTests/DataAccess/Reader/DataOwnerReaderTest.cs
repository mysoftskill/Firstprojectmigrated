namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Reader.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;

    public class DataOwnerReaderTest
    {
        #region ReadById
        [Theory(DisplayName = "When ReadById returns null, then return null."), ValidData]
        public async Task When_ReadByIdIsNull_Then_ReturnNull(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            Guid id,
            DataOwnerReader reader)
        {
            storageReader.Setup(m => m.GetDataOwnerAsync(id, false, true)).ReturnsAsync(null as DataOwner);

            var result = await reader.ReadByIdAsync(id, ExpandOptions.None).ConfigureAwait(false);

            Assert.Null(result);
        }

        [Theory(DisplayName = "When ReadById is called without expansions, then data access GetDataOwnerAsync is invoked without tracking details and service tree."), ValidData]
        public async Task When_ReadById_Then_GetDataOwnerAsyncInvoked(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            [Frozen] DataOwner dataOwner,
            Guid id,
            DataOwnerReader reader)
        {
            var result = await reader.ReadByIdAsync(id, ExpandOptions.None).ConfigureAwait(false);

            Assert.Equal(dataOwner, result);

            storageReader.Verify(m => m.GetDataOwnerAsync(id, false, true), Times.Once);
        }

        [Theory(DisplayName = "When ReadById is called with tracking details expansion, then data access GetDataOwnerAsync is invoked with tracking details."), ValidData]
        public async Task When_ReadByIdWithTrackingDetails_Then_GetDataOwnerAsyncInvokedWithTrackingDetails(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            Guid id,
            DataOwnerReader reader)
        {
            await reader.ReadByIdAsync(id, ExpandOptions.TrackingDetails).ConfigureAwait(false);

            storageReader.Verify(m => m.GetDataOwnerAsync(id, true, true), Times.Once);
        }

        [Theory(DisplayName = "When ReadById is called with service tree expansion, then data access GetDataOwnerAsync is invoked with service trees."), ValidData]
        public async Task When_ReadByIdWithServiceTree_Then_GetDataOwnerAsyncInvokedWithServiceTree(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            Guid id,
            DataOwnerReader reader)
        {
            await reader.ReadByIdAsync(id, ExpandOptions.ServiceTree).ConfigureAwait(false);

            storageReader.Verify(m => m.GetDataOwnerAsync(id, false, true), Times.Once);
        }

        [Theory(DisplayName = "When ReadById is called with data agents expansion and page size is exceeded, then throw exception."), ValidData]
        public async Task When_ReadByIdWithDataAgentsExceedsLimit_Then_ThrowException(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            [Frozen] ICoreConfiguration coreConfiguration,
            FilterResult<DataAgent> dataAgents,
            Guid id,
            DataOwnerReader reader)
        {
            storageReader.Setup(m => m.GetDataAgentsAsync<DataAgent>(It.IsAny<DataAgentFilterCriteria>(), false)).ReturnsAsync(dataAgents);

            dataAgents.Total = coreConfiguration.MaxPageSize + 1;

            var result = await Assert.ThrowsAsync<ConflictException>(() => reader.ReadByIdAsync(id, ExpandOptions.DataAgents)).ConfigureAwait(false);

            Assert.Equal(ConflictType.MaxExpansionSizeExceeded, result.ConflictType);
            Assert.Equal("dataAgents", result.Target);
        }

        [Theory(DisplayName = "When ReadById is called with asset groups expansion, then data access GetAssetGroupsAsync is invoked with the proper filter."), ValidData]
        public async Task When_ReadByIdWithAssetGroups_Then_GetAssetGroupsAsyncInvokedWithFilter(
            [Frozen] ICoreConfiguration coreConfiguration,
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            [Frozen] DataOwner dataOwner,
            [Frozen] IEnumerable<AssetGroup> assetGroups,
            Guid id,
            DataOwnerReader reader)
        {
            var result = await reader.ReadByIdAsync(id, ExpandOptions.AssetGroups).ConfigureAwait(false);

            Assert.Same(assetGroups, result.AssetGroups);

            storageReader.Verify(m => m.GetDataOwnerAsync(id, false, true), Times.Once);

            Action<AssetGroupFilterCriteria> verify = f =>
            {
                var expected = new AssetGroupFilterCriteria { OwnerId = dataOwner.Id }.Initialize(coreConfiguration.MaxPageSize);
                expected
                .Likeness()
                .With(m => m.EntityType).EqualsWhen((src, dest) => src.EntityType.LikenessShouldEqual(dest.EntityType))
                .ShouldEqual(f);
            };

            storageReader.Verify(m => m.GetAssetGroupsAsync(Is.Value(verify), false), Times.Once);
        }

        [Theory(DisplayName = "When ReadById is called with asset groups expansion and page size is exceeded, then throw exception."), ValidData]
        public async Task When_ReadByIdWithAssetGroupsExceedsLimit_Then_ThrowException(
            [Frozen] ICoreConfiguration coreConfiguration,
            [Frozen] FilterResult<AssetGroup> assetGroups,
            Guid id,
            DataOwnerReader reader)
        {
            assetGroups.Total = coreConfiguration.MaxPageSize + 1;

            var result = await Assert.ThrowsAsync<ConflictException>(() => reader.ReadByIdAsync(id, ExpandOptions.AssetGroups)).ConfigureAwait(false);

            Assert.Equal(ConflictType.MaxExpansionSizeExceeded, result.ConflictType);
            Assert.Equal("assetGroups", result.Target);
        }
        #endregion

        #region ReadByFilters
        [Theory(DisplayName = "When ReadByFilters is called without expansions, then data access GetDataOwnersAsync is invoked without tracking details and service tree."), ValidData]
        public async Task When_ReadByFilters_Then_GetDataOwnersAsyncInvoked(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            [Frozen] FilterResult<DataOwner> dataOwners,
            DataOwnerFilterCriteria filterCriteria,
            DataOwnerReader reader)
        {
            var result = await reader.ReadByFiltersAsync(filterCriteria, ExpandOptions.None).ConfigureAwait(false);

            Assert.Equal(dataOwners, result);

            storageReader.Verify(m => m.GetDataOwnersAsync(filterCriteria, false, false), Times.Once);
        }
        
        [Theory(DisplayName = "When ReadByFilters is called with tracking details expansion, then data access GetDataOwnersAsync is invoked with tracking details."), ValidData]
        public async Task When_ReadByFiltersWithTrackingDetails_Then_GetDataOwnersAsyncInvokedWithTrackingDetails(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            DataOwnerFilterCriteria filterCriteria,
            DataOwnerReader reader)
        {
            await reader.ReadByFiltersAsync(filterCriteria, ExpandOptions.TrackingDetails).ConfigureAwait(false);
            
            storageReader.Verify(m => m.GetDataOwnersAsync(filterCriteria, true, false), Times.Once);
        }

        [Theory(DisplayName = "When ReadByFilters is called with service tree expansion, then data access GetDataOwnersAsync is invoked with service trees."), ValidData]
        public async Task When_ReadByFiltersWithServiceTree_Then_GetDataOwnersAsyncInvokedWithServiceTree(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            DataOwnerFilterCriteria filterCriteria,
            DataOwnerReader reader)
        {
            await reader.ReadByFiltersAsync(filterCriteria, ExpandOptions.ServiceTree).ConfigureAwait(false);

            storageReader.Verify(m => m.GetDataOwnersAsync(filterCriteria, false, true), Times.Once);
        }
        #endregion

        [Theory(DisplayName = "When FindByAuthenticatedUser is called, then user's groups are passed into storage call."), ValidData]
        public async Task VerifyCallFlowForFindByAuthenticatedUser(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            [Frozen] AuthenticatedPrincipal authPrincipal,
            [Frozen] Mock<IActiveDirectory> activeDirectory,
            IEnumerable<Guid> securityGroups,
            IEnumerable<DataOwner> ownersA,
            IEnumerable<DataOwner> ownersB,
            DataOwnerReader reader)
        {
            ownersB = ownersB.Concat(new[] { ownersA.First() });

            activeDirectory.Setup(m => m.GetSecurityGroupIdsAsync(authPrincipal)).ReturnsAsync(securityGroups);
            storageReader.Setup(m => m.GetDataOwnersBySecurityGroupsAsync(securityGroups, false, false)).ReturnsAsync(ownersA);
            storageReader.Setup(m => m.GetDataOwnersByServiceAdminAsync(authPrincipal.UserAlias, false, false)).ReturnsAsync(ownersB);

            var result = await reader.FindByAuthenticatedUserAsync(ExpandOptions.None).ConfigureAwait(false);
                        
            Assert.Equal(ownersA.Concat(ownersB.Take(ownersB.Count() - 1)), result);
        }

        [Theory(DisplayName = "Verify IsLinkedToAnyOtherEntities."), ValidData]
        public async void VerifyIsLinkedToAnyOtherEntities(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            Guid id,
            DataOwnerReader reader)
        {
            await reader.IsLinkedToAnyOtherEntities(id).ConfigureAwait(false);

            storageReader.Verify(m => m.IsDataOwnerLinkedToAnyOtherEntities(id), Times.Once);
        }

        [Theory(DisplayName = "HasPendingCommands returns false."), ValidData]
        public async void EnsurePendingCommandsReturnsFalse(
            DataOwnerReader reader)
        {
            var result = await reader.HasPendingCommands(It.IsAny<Guid>()).ConfigureAwait(false);

            Assert.False(result);
        }

        [Theory(DisplayName = "Verify DataOwnerReader.ReadByIds.")]
        [InlineValidData(ExpandOptions.None, false, DisableRecursionCheck = true)]
        [InlineValidData(ExpandOptions.TrackingDetails, true, DisableRecursionCheck = true)]
        [InlineValidData(ExpandOptions.ServiceTree, false, DisableRecursionCheck = true)]
        [InlineValidData(ExpandOptions.TrackingDetails | ExpandOptions.ServiceTree, true, DisableRecursionCheck = true)]
        public async void VerifyDataOwnerReaderReadByIds(
            ExpandOptions options,
            bool includeTrackingDetails,
            IEnumerable<Guid> ids,
            [Frozen] Mock<IPrivacyDataStorageReader> storage,
            DataOwnerReader reader)
        {
            await reader.ReadByIdsAsync(ids, options).ConfigureAwait(false);
            storage.Verify(m => m.GetDataOwnersAsync(ids, includeTrackingDetails, true), Times.Once);
        }

        public class ValidDataAttribute : AutoMoqDataAttribute
        {
            public ValidDataAttribute() : base(true)
            {
                this.Fixture.Customize<FilterResult<DataAgent>>(obj =>
                    obj.With(x => x.Total, 0)); // Ensure we never hit page limits.

                this.Fixture.Customize<FilterResult<AssetGroup>>(obj =>
                    obj.With(x => x.Total, 0)); // Ensure we never hit page limits.

                this.Fixture.Customize<Mock<ICoreConfiguration>>(obj =>
                    obj.Do(x => x.SetupGet(m => m.MaxPageSize).Returns(5)));
            }
        }

        public class InlineValidDataAttribute : InlineAutoMoqDataAttribute
        {
            public InlineValidDataAttribute(params object[] values) : base(new ValidDataAttribute(), values)
            {
            }
        }
    }
}
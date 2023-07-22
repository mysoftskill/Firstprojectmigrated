namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Reader.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;

    public class VariantRequestReaderTest
    {
        [Theory(DisplayName = "When ReadById returns null, then return null."), ValidData]
        public async Task When_ReadByIdIsNull_Then_ReturnNull(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            Guid id,
            VariantRequestReader reader)
        {
            storageReader.Setup(m => m.GetVariantRequestAsync(id, false)).ReturnsAsync(null as VariantRequest);

            var result = await reader.ReadByIdAsync(id, ExpandOptions.None).ConfigureAwait(false);

            Assert.Null(result);
        }

        [Theory(DisplayName = "When ReadById is called without expansions, then data access GetVariantRequestAsync is invoked without tracking details."), ValidData]
        public async Task When_ReadById_Then_GetVariantRequestAsyncInvoked(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            [Frozen] VariantRequest variantRequest,
            Guid id,
            VariantRequestReader reader)
        {
            var result = await reader.ReadByIdAsync(id, ExpandOptions.None).ConfigureAwait(false);

            Assert.Equal(variantRequest, result);

            storageReader.Verify(m => m.GetVariantRequestAsync(id, false), Times.Once);
        }

        [Theory(DisplayName = "When ReadById is called with tracking details expansion, then data access GetVariantRequestAsync is invoked with tracking details."), ValidData]
        public async Task When_ReadByIdWithTrackingDetails_Then_GetVariantRequestAsyncInvokedWithTrackingDetails(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            Guid id,
            VariantRequestReader reader)
        {
            await reader.ReadByIdAsync(id, ExpandOptions.TrackingDetails).ConfigureAwait(false);

            storageReader.Verify(m => m.GetVariantRequestAsync(id, true), Times.Once);
        }

        [Theory(DisplayName = "When ReadByFilters is called without expansions, then data access GetVariantRequestsAsync is invoked without tracking details."), ValidData]
        public async Task When_ReadByFilters_Then_GetVariantRequestsAsyncInvoked(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            [Frozen] FilterResult<VariantRequest> variantRequests,
            VariantRequestFilterCriteria filterCriteria,
            VariantRequestReader reader)
        {
            var result = await reader.ReadByFiltersAsync(filterCriteria, ExpandOptions.None).ConfigureAwait(false);

            Assert.Equal(variantRequests, result);

            storageReader.Verify(m => m.GetVariantRequestsAsync(filterCriteria, false), Times.Once);
        }

        [Theory(DisplayName = "When ReadByFilters is called with tracking details expansion, then data access GetVariantRequestsAsync is invoked with tracking details."), ValidData]
        public async Task When_ReadByFiltersWithTrackingDetails_Then_GetVariantRequestsAsyncInvokedWithTrackingDetails(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            VariantRequestFilterCriteria filterCriteria,
            VariantRequestReader reader)
        {
            await reader.ReadByFiltersAsync(filterCriteria, ExpandOptions.TrackingDetails).ConfigureAwait(false);

            storageReader.Verify(m => m.GetVariantRequestsAsync(filterCriteria, true), Times.Once);
        }

        [Theory(DisplayName = "When IsLinkedToAnyOtherEntities is called, return false."), ValidData]
        public async void When_IsLinkedToAnyOtherEntitiesIsCalled_Then_ReturnFalse(
            VariantRequestReader reader,
            Guid id)
        {
            var result = await reader.IsLinkedToAnyOtherEntities(id).ConfigureAwait(false);

            Assert.False(result);
        }

        [Theory(DisplayName = "HasPendingCommands returns false."), ValidData]
        public async void EnsurePendingCommandsReturnsFalse(
            VariantRequestReader reader,
            Guid id)
        {
            var result = await reader.HasPendingCommands(id).ConfigureAwait(false);

            Assert.False(result);
        }

        [Theory(DisplayName = "Verify VariantRequestReader.ReadByIds.")]
        [InlineValidData(ExpandOptions.None, false, DisableRecursionCheck = true)]
        [InlineValidData(ExpandOptions.TrackingDetails, true, DisableRecursionCheck = true)]
        public async void VerifyVariantRequestReaderReadByIds(
            ExpandOptions options,
            bool includeTrackingDetails,
            IEnumerable<Guid> ids,
            [Frozen] Mock<IPrivacyDataStorageReader> storage,
            VariantRequestReader reader)
        {
            await reader.ReadByIdsAsync(ids, options).ConfigureAwait(false);
            storage.Verify(m => m.GetVariantRequestsAsync(ids, includeTrackingDetails), Times.Once);
        }

        public class ValidDataAttribute : AutoMoqDataAttribute
        {
            public ValidDataAttribute() : base(true)
            {
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
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

    public class SharingRequestReaderTest
    {
        [Theory(DisplayName = "When ReadById returns null, then return null."), ValidData]
        public async Task When_ReadByIdIsNull_Then_ReturnNull(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            Guid id,
            SharingRequestReader reader)
        {
            storageReader.Setup(m => m.GetSharingRequestAsync(id, false)).ReturnsAsync(null as SharingRequest);

            var result = await reader.ReadByIdAsync(id, ExpandOptions.None).ConfigureAwait(false);

            Assert.Null(result);
        }

        [Theory(DisplayName = "When ReadById is called without expansions, then data access GetSharingRequestAsync is invoked without tracking details."), ValidData]
        public async Task When_ReadById_Then_GetSharingRequestAsyncInvoked(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            [Frozen] SharingRequest sharingRequest,
            Guid id,
            SharingRequestReader reader)
        {
            var result = await reader.ReadByIdAsync(id, ExpandOptions.None).ConfigureAwait(false);

            Assert.Equal(sharingRequest, result);

            storageReader.Verify(m => m.GetSharingRequestAsync(id, false), Times.Once);
        }

        [Theory(DisplayName = "When ReadById is called with tracking details expansion, then data access GetSharingRequestAsync is invoked with tracking details."), ValidData]
        public async Task When_ReadByIdWithTrackingDetails_Then_GetSharingRequestAsyncInvokedWithTrackingDetails(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            Guid id,
            SharingRequestReader reader)
        {
            await reader.ReadByIdAsync(id, ExpandOptions.TrackingDetails).ConfigureAwait(false);

            storageReader.Verify(m => m.GetSharingRequestAsync(id, true), Times.Once);
        }

        [Theory(DisplayName = "When ReadByFilters is called without expansions, then data access GetSharingRequestsAsync is invoked without tracking details."), ValidData]
        public async Task When_ReadByFilters_Then_GetSharingRequestsAsyncInvoked(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            [Frozen] FilterResult<SharingRequest> sharingRequests,
            SharingRequestFilterCriteria filterCriteria,
            SharingRequestReader reader)
        {
            var result = await reader.ReadByFiltersAsync(filterCriteria, ExpandOptions.None).ConfigureAwait(false);

            Assert.Equal(sharingRequests, result);

            storageReader.Verify(m => m.GetSharingRequestsAsync(filterCriteria, false), Times.Once);
        }

        [Theory(DisplayName = "When ReadByFilters is called with tracking details expansion, then data access GetSharingRequestsAsync is invoked with tracking details."), ValidData]
        public async Task When_ReadByFiltersWithTrackingDetails_Then_GetSharingRequestsAsyncInvokedWithTrackingDetails(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            SharingRequestFilterCriteria filterCriteria,
            SharingRequestReader reader)
        {
            await reader.ReadByFiltersAsync(filterCriteria, ExpandOptions.TrackingDetails).ConfigureAwait(false);

            storageReader.Verify(m => m.GetSharingRequestsAsync(filterCriteria, true), Times.Once);
        }

        [Theory(DisplayName = "When IsLinkedToAnyOtherEntities is called, return false."), ValidData]
        public async void When_IsLinkedToAnyOtherEntitiesIsCalled_Then_ReturnFalse(
            SharingRequestReader reader,
            Guid id)
        {
            var result = await reader.IsLinkedToAnyOtherEntities(id).ConfigureAwait(false);

            Assert.False(result);
        }

        [Theory(DisplayName = "HasPendingCommands returns false."), ValidData]
        public async void EnsurePendingCommandsReturnsFalse(
            SharingRequestReader reader,
            Guid id)
        {
            var result = await reader.HasPendingCommands(id).ConfigureAwait(false);

            Assert.False(result);
        }

        [Theory(DisplayName = "Verify SharingRequestReader.ReadByIds.")]
        [InlineValidData(ExpandOptions.None, false, DisableRecursionCheck = true)]
        [InlineValidData(ExpandOptions.TrackingDetails, true, DisableRecursionCheck = true)]
        public async void VerifySharingRequestReaderReadByIds(
            ExpandOptions options,
            bool includeTrackingDetails,
            IEnumerable<Guid> ids,
            [Frozen] Mock<IPrivacyDataStorageReader> storage,
            SharingRequestReader reader)
        {
            await reader.ReadByIdsAsync(ids, options).ConfigureAwait(false);
            storage.Verify(m => m.GetSharingRequestsAsync(ids, includeTrackingDetails), Times.Once);
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
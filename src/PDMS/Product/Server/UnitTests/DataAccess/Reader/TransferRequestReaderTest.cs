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

    public class TransferRequestReaderTest
    {
        [Theory(DisplayName = "When ReadById returns null, then return null."), ValidData]
        public async Task When_ReadByIdIsNull_Then_ReturnNull(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            Guid id,
            TransferRequestReader reader)
        {
            storageReader.Setup(m => m.GetTransferRequestAsync(id, false)).ReturnsAsync(null as TransferRequest);

            var result = await reader.ReadByIdAsync(id, ExpandOptions.None).ConfigureAwait(false);

            Assert.Null(result);
        }

        [Theory(DisplayName = "When ReadById is called without expansions, then data access GetTransferRequestAsync is invoked without tracking details."), ValidData]
        public async Task When_ReadById_Then_GetTransferRequestAsyncInvoked(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            [Frozen] TransferRequest transferRequest,
            Guid id,
            TransferRequestReader reader)
        {
            var result = await reader.ReadByIdAsync(id, ExpandOptions.None).ConfigureAwait(false);

            Assert.Equal(transferRequest, result);

            storageReader.Verify(m => m.GetTransferRequestAsync(id, false), Times.Once);
        }

        [Theory(DisplayName = "When ReadById is called with tracking details expansion, then data access GetTransferRequestAsync is invoked with tracking details."), ValidData]
        public async Task When_ReadByIdWithTrackingDetails_Then_GetTransferRequestAsyncInvokedWithTrackingDetails(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            Guid id,
            TransferRequestReader reader)
        {
            await reader.ReadByIdAsync(id, ExpandOptions.TrackingDetails).ConfigureAwait(false);

            storageReader.Verify(m => m.GetTransferRequestAsync(id, true), Times.Once);
        }

        [Theory(DisplayName = "When ReadByFilters is called without expansions, then data access GetTransferRequestsAsync is invoked without tracking details."), ValidData]
        public async Task When_ReadByFilters_Then_GetTransferRequestsAsyncInvoked(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            [Frozen] FilterResult<TransferRequest> transferRequests,
            TransferRequestFilterCriteria filterCriteria,
            TransferRequestReader reader)
        {
            var result = await reader.ReadByFiltersAsync(filterCriteria, ExpandOptions.None).ConfigureAwait(false);

            Assert.Equal(transferRequests, result);

            storageReader.Verify(m => m.GetTransferRequestsAsync(filterCriteria, false), Times.Once);
        }

        [Theory(DisplayName = "When ReadByFilters is called with tracking details expansion, then data access GetTransferRequestsAsync is invoked with tracking details."), ValidData]
        public async Task When_ReadByFiltersWithTrackingDetails_Then_GetTransferRequestsAsyncInvokedWithTrackingDetails(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            TransferRequestFilterCriteria filterCriteria,
            TransferRequestReader reader)
        {
            await reader.ReadByFiltersAsync(filterCriteria, ExpandOptions.TrackingDetails).ConfigureAwait(false);

            storageReader.Verify(m => m.GetTransferRequestsAsync(filterCriteria, true), Times.Once);
        }

        [Theory(DisplayName = "When IsLinkedToAnyOtherEntities is called, return false."), ValidData]
        public async void When_IsLinkedToAnyOtherEntitiesIsCalled_Then_ReturnFalse(
            TransferRequestReader reader,
            Guid id)
        {
            var result = await reader.IsLinkedToAnyOtherEntities(id).ConfigureAwait(false);

            Assert.False(result);
        }

        [Theory(DisplayName = "HasPendingCommands returns false."), ValidData]
        public async void EnsurePendingCommandsReturnsFalse(
            TransferRequestReader reader,
            Guid id)
        {
            var result = await reader.HasPendingCommands(id).ConfigureAwait(false);

            Assert.False(result);
        }

        [Theory(DisplayName = "Verify TransferRequestReader.ReadByIds.")]
        [InlineValidData(ExpandOptions.None, false, DisableRecursionCheck = true)]
        [InlineValidData(ExpandOptions.TrackingDetails, true, DisableRecursionCheck = true)]
        public async void VerifyTransferRequestReaderReadByIds(
            ExpandOptions options,
            bool includeTrackingDetails,
            IEnumerable<Guid> ids,
            [Frozen] Mock<IPrivacyDataStorageReader> storage,
            TransferRequestReader reader)
        {
            await reader.ReadByIdsAsync(ids, options).ConfigureAwait(false);
            storage.Verify(m => m.GetTransferRequestsAsync(ids, includeTrackingDetails), Times.Once);
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
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

    public class DeleteAgentReaderTest
    {
        [Theory(DisplayName = "When ReadById returns null, then return null."), ValidData]
        public async Task When_ReadByIdIsNull_Then_ReturnNull(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            Guid id,
            DeleteAgentReader reader)
        {
            storageReader.Setup(m => m.GetDataAgentAsync(id, false)).ReturnsAsync(null as DataAgent);

            var result = await reader.ReadByIdAsync(id, ExpandOptions.HasSharingRequests).ConfigureAwait(false);

            Assert.Null(result);
        }

        [Theory(DisplayName = "When ReadById with HasSharingRequest, then return correct value.")]
        [InlineValidData(0, false)]
        [InlineValidData(1, true)]
        public async Task When_ReadByIdWithHasSharingRequest_Then_ReturnCorrectValue(
            int count,
            bool shouldBeSet,
            Guid id,
            [Frozen] DeleteAgent agent,
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            [Frozen] Mock<ISharingRequestReader> requestStorage,
            FilterResult<SharingRequest> requestResult,
            DeleteAgentReader reader)
        {
            requestResult.Total = count;

            Action<SharingRequestFilterCriteria> verify = f =>
           {
               Assert.Equal(agent.Id, f.DeleteAgentId);
               Assert.Equal(0, f.Count);
           };

            storageReader.Setup(m => m.GetDataAgentAsync(id, false)).ReturnsAsync(agent);

            requestStorage
                .Setup(m => m.ReadByFiltersAsync(Is.Value(verify), ExpandOptions.None))
                .ReturnsAsync(requestResult);

            var result = await reader.ReadByIdAsync(id, ExpandOptions.HasSharingRequests).ConfigureAwait(false);

            Assert.Equal(shouldBeSet, result.HasSharingRequests);
        }

        [Theory(DisplayName = "When IsLinkedToAnyOtherEntities is called, then call storage layer."), ValidData]
        public async Task When_IsLinkedToAnyOtherEntities_Then_CallStorageLayer(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            Guid id,
            DeleteAgentReader reader)
        {
            await reader.IsLinkedToAnyOtherEntities(id).ConfigureAwait(false);

            storageReader.Verify(m => m.IsDataAgentLinkedToAnyOtherEntities(id), Times.Once);
        }

        [Theory(DisplayName = "When HasPendingCommands is called, then call storage layer."), ValidData]
        public async Task When_HasPendingCommands_Then_CallStorageLayer(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            Guid id,
            DeleteAgentReader reader)
        {
            await reader.HasPendingCommands(id).ConfigureAwait(false);

            storageReader.Verify(m => m.DataAgentHasPendingCommands(id), Times.Once);
        }

        [Theory(DisplayName = "Verify DeleteAgentReader.ReadByIds.")]
        [InlineValidData(ExpandOptions.None, false, DisableRecursionCheck = true)]
        [InlineValidData(ExpandOptions.TrackingDetails, true, DisableRecursionCheck = true)]
        public async void VerifyDeleteAgentReaderReadByIds(
            ExpandOptions options,
            bool includeTrackingDetails,
            IEnumerable<Guid> ids,
            [Frozen] Mock<IPrivacyDataStorageReader> storage,
            DeleteAgentReader reader)
        {
            await reader.ReadByIdsAsync(ids, options).ConfigureAwait(false);
            storage.Verify(m => m.GetDataAgentsAsync<DeleteAgent>(ids, includeTrackingDetails), Times.Once);
        }

        [Theory(DisplayName = "Verify DeleteAgentReader.ReadByIds with HasSharingRequests expansion."), ValidData]
        public async void VerifyDeleteAgentReaderReadByIdsithHasSharingRequests(
            IEnumerable<Guid> ids,
            IEnumerable<DeleteAgent> agents,
            [Frozen] Mock<IPrivacyDataStorageReader> storage,
            [Frozen] Mock<ISharingRequestReader> requestStorage,
            DeleteAgentReader reader)
        {
            storage.Setup(m => m.GetDataAgentsAsync<DeleteAgent>(ids, false)).ReturnsAsync(agents);

            await reader.ReadByIdsAsync(ids, ExpandOptions.HasSharingRequests).ConfigureAwait(false);

            foreach (var agent in agents)
            {
                requestStorage.Verify(m => m.ReadByFiltersAsync(It.Is<SharingRequestFilterCriteria>(f => agent.Id == f.DeleteAgentId), ExpandOptions.None), Times.Once);
            }
        }

        [Theory(DisplayName = "Verify DeleteAgentReader.ReadByFilters with HasSharingRequests expansion."), ValidData]
        public async void VerifyDeleteAgentReaderReadByFiltersithHasSharingRequests(
            DeleteAgentFilterCriteria filterCriteria,
            IEnumerable<DeleteAgent> agents,
            [Frozen] Mock<IPrivacyDataStorageReader> storage,
            [Frozen] Mock<ISharingRequestReader> requestStorage,
            DeleteAgentReader reader)
        {
            var filterResult = new FilterResult<DeleteAgent>
            {
                Values = agents
            };

            storage.Setup(m => m.GetDataAgentsAsync<DeleteAgent>(filterCriteria, false)).ReturnsAsync(filterResult);

            await reader.ReadByFiltersAsync(filterCriteria, ExpandOptions.HasSharingRequests).ConfigureAwait(false);

            foreach (var agent in agents)
            {
                requestStorage.Verify(m => m.ReadByFiltersAsync(It.Is<SharingRequestFilterCriteria>(f => agent.Id == f.DeleteAgentId), ExpandOptions.None), Times.Once);
            }
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
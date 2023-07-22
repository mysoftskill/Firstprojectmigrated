namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Reader.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;

    public class DataAgentReaderTest
    {
        [Theory(DisplayName = "When ReadById returns null, then return null."), ValidData]
        public async Task When_ReadByIdIsNull_Then_ReturnNull(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            Guid id,
            DataAgentReader reader)
        {
            storageReader.Setup(m => m.GetDataAgentAsync(id, false)).ReturnsAsync(null as DataAgent);

            var result = await reader.ReadByIdAsync(id, ExpandOptions.None).ConfigureAwait(false);

            Assert.Null(result);
        }

        [Theory(DisplayName = "When ReadById is called with/without expansions, then data access GetDataAgentAsync is invoked appropriately.")]
        [InlineValidData(true, true)]
        [InlineValidData(false, false)]
        public async Task When_ReadById_Then_GetDataOwnerAsyncInvoked(
            bool withTrackingDetail,
            bool expcectedCallValue,
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            [Frozen] DataAgent agent,
            Guid id,
            DataAgentReader reader)
        {
            var result = await reader.ReadByIdAsync(
                            id, 
                            withTrackingDetail ? ExpandOptions.TrackingDetails : ExpandOptions.None).ConfigureAwait(false);

            Assert.Equal(agent, result);

            storageReader.Verify(m => m.GetDataAgentAsync(id, expcectedCallValue), Times.Once);
        }

        [Theory(DisplayName = "When IsLinkedToAnyOtherEntities is called, then call storage layer."), ValidData]        
        public async Task When_IsLinkedToAnyOtherEntities_Then_CallStorageLayer(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            Guid id,
            DataAgentReader reader)
        {
            await reader.IsLinkedToAnyOtherEntities(id).ConfigureAwait(false);
            
            storageReader.Verify(m => m.IsDataAgentLinkedToAnyOtherEntities(id), Times.Once);
        }

        [Theory(DisplayName = "When HasPendingCommands is called, then call storage layer."), ValidData]
        public async Task When_HasPendingCommands_Then_CallStorageLayer(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            Guid id,
            DataAgentReader reader)
        {
            await reader.HasPendingCommands(id).ConfigureAwait(false);

            storageReader.Verify(m => m.DataAgentHasPendingCommands(id), Times.Once);
        }

        [Theory(DisplayName = "Verify DataAgentReader.ReadByIds.")]
        [InlineValidData(ExpandOptions.None, false, DisableRecursionCheck = true)]
        [InlineValidData(ExpandOptions.TrackingDetails, true, DisableRecursionCheck = true)]
        public async void VerifyDataAgentReaderReadByIds(
            ExpandOptions options,
            bool includeTrackingDetails,
            IEnumerable<Guid> ids,
            [Frozen] Mock<IPrivacyDataStorageReader> storage,
            DataAgentReader reader)
        {
            await reader.ReadByIdsAsync(ids, options).ConfigureAwait(false);
            storage.Verify(m => m.GetDataAgentsAsync<DataAgent>(ids, includeTrackingDetails), Times.Once);
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
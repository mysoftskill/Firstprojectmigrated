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

    public class VariantDefinitionReaderTest
    {
        #region ReadById
        [Theory(DisplayName = "When ReadById returns null, then return null."), ValidData]
        public async Task When_ReadByIdIsNull_Then_ReturnNull(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            Guid id,
            VariantDefinitionReader reader)
        {
            storageReader.Setup(m => m.GetVariantDefinitionAsync(id, false)).ReturnsAsync(null as VariantDefinition);

            var result = await reader.ReadByIdAsync(id, ExpandOptions.None).ConfigureAwait(false);

            Assert.Null(result);
        }

        [Theory(DisplayName = "When ReadById is called without expansions, then data access GetVariantDefinitionAsync is invoked without tracking details."), ValidData]
        public async Task When_ReadById_Then_GetVariantDefinitionAsyncInvoked(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            [Frozen] VariantDefinition variantDefinition,
            Guid id,
            VariantDefinitionReader reader)
        {
            var result = await reader.ReadByIdAsync(id, ExpandOptions.None).ConfigureAwait(false);

            Assert.Equal(variantDefinition, result);

            storageReader.Verify(m => m.GetVariantDefinitionAsync(id, false), Times.Once);
        }

        [Theory(DisplayName = "When ReadById is called with tracking details expansion, then data access GetVariantDefinitionAsync is invoked with tracking details."), ValidData]
        public async Task When_ReadByIdWithTrackingDetails_Then_GetVariantDefinitionAsyncInvokedWithTrackingDetails(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            Guid id,
            VariantDefinitionReader reader)
        {
            await reader.ReadByIdAsync(id, ExpandOptions.TrackingDetails).ConfigureAwait(false);

            storageReader.Verify(m => m.GetVariantDefinitionAsync(id, true), Times.Once);
        }
        #endregion

        #region ReadByFilters
        [Theory(DisplayName = "When ReadByFilters is called without expansions, then data access GetVariantDefinitionsAsync is invoked without tracking details."), ValidData]
        public async Task When_ReadByFilters_Then_GetVariantDefinitionsAsyncInvoked(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            [Frozen] FilterResult<VariantDefinition> variantDefinitions,
            VariantDefinitionFilterCriteria filterCriteria,
            VariantDefinitionReader reader)
        {
            var result = await reader.ReadByFiltersAsync(filterCriteria, ExpandOptions.None).ConfigureAwait(false);

            Assert.Equal(variantDefinitions, result);

            storageReader.Verify(m => m.GetVariantDefinitionsAsync(filterCriteria, false), Times.Once);
        }

        [Theory(DisplayName = "When ReadByFilters is called with tracking details expansion, then data access GetVariantDefinitionsAsync is invoked with tracking details."), ValidData]
        public async Task When_ReadByFiltersWithTrackingDetails_Then_GetVariantDefinitionsAsyncInvokedWithTrackingDetails(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            VariantDefinitionFilterCriteria filterCriteria,
            VariantDefinitionReader reader)
        {
            await reader.ReadByFiltersAsync(filterCriteria, ExpandOptions.TrackingDetails).ConfigureAwait(false);

            storageReader.Verify(m => m.GetVariantDefinitionsAsync(filterCriteria, true), Times.Once);
        }
        #endregion

        [Theory(DisplayName = "Verify IsLinkedToAnyOtherEntities calls storage layer."), ValidData]
        public async void VerifyIsLinkedToAnyOtherEntities(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            Guid id,
            VariantDefinitionReader reader)
        {
            await reader.IsLinkedToAnyOtherEntities(id).ConfigureAwait(false);

            storageReader.Verify(m => m.IsVariantDefinitionLinkedToAnyOtherEntities(id), Times.Once);
        }

        [Theory(DisplayName = "HasPendingCommands returns false."), ValidData]
        public async void EnsurePendingCommandsReturnsFalse(
            VariantDefinitionReader reader,
            Guid id)
        {
            var result = await reader.HasPendingCommands(id).ConfigureAwait(false);

            Assert.False(result);
        }

        [Theory(DisplayName = "Verify VariantDefinitionReader.ReadByIds.")]
        [InlineValidData(ExpandOptions.None, false, DisableRecursionCheck = true)]
        [InlineValidData(ExpandOptions.TrackingDetails, true, DisableRecursionCheck = true)]
        public async void VerifyVariantDefinitionReaderReadByIds(
            ExpandOptions options,
            bool includeTrackingDetails,
            IEnumerable<Guid> ids,
            [Frozen] Mock<IPrivacyDataStorageReader> storage,
            VariantDefinitionReader reader)
        {
            await reader.ReadByIdsAsync(ids, options).ConfigureAwait(false);
            storage.Verify(m => m.GetVariantDefinitionsAsync(ids, includeTrackingDetails), Times.Once);
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
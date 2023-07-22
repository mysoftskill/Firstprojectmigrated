namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Reader.UnitTest
{
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;

    public class HistoryItemReaderTest
    {
        #region ReadByFilters
        [Theory(DisplayName = "When ReadByFilters is called, then data access GetHistoryItemsAsync is invoked."), ValidData]
        public async Task When_ReadByFilters_Then_GetHistoryItemsAsyncInvoked(
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            [Frozen] FilterResult<HistoryItem> historyItems,
            HistoryItemFilterCriteria filterCriteria,
            HistoryItemReader reader)
        {
            var result = await reader.ReadByFiltersAsync(filterCriteria).ConfigureAwait(false);

            Assert.Equal(historyItems, result);

            storageReader.Verify(m => m.GetHistoryItemsAsync(filterCriteria), Times.Once);
        }
        #endregion

        public class ValidDataAttribute : AutoMoqDataAttribute
        {
            public ValidDataAttribute() : base(true)
            {
                this.Fixture.Customize<Mock<ICoreConfiguration>>(obj =>
                    obj.Do(x => x.SetupGet(m => m.MaxPageSize).Returns(5)));
            }
        }
    }
}
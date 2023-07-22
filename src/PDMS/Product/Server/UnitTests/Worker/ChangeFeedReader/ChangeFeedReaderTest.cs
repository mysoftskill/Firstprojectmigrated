namespace Microsoft.PrivacyServices.DataManagement.Worker.ChangeFeedReader.UnitTest
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb;
    using Microsoft.PrivacyServices.Testing;
    using Moq;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;

    public class ChangeFeedReaderTest
    {
        [Theory(DisplayName = "When continuation is null, then start from the beginning."), AutoMoqData(true)]
        public async Task When_ContinuationIsNull_Then_StartFromBeginning(            
            [Frozen] Mock<IDocumentClientAdapter> clientAdapter,
            ChangeFeedReader changeFeedReader)
        {
            var feed = new FeedResponse<PartitionKeyRange>(new[] { new PartitionKeyRange() });

            clientAdapter.Setup(m => m.ReadPartitionKeyRangeFeedAsync(It.IsAny<Uri>())).ReturnsAsync(feed);

            await changeFeedReader.ReadItemsAsync(null).ConfigureAwait(false);

            Action<ChangeFeedOptions> verify = m =>
            {
                Assert.True(m.StartFromBeginning);
                Assert.Null(m.RequestContinuation);
            };

            clientAdapter.Verify(m => m.CreateDocumentChangeFeedQuery(It.IsAny<Uri>(), Is.Value(verify)), Times.Once);
        }

        [Theory(DisplayName = "When continuation is provided, then use it."), AutoMoqData(true)]
        public async Task When_ContinuationIsNotNull_Then_UseIt(
            [Frozen] Mock<IDocumentClientAdapter> clientAdapter,
            ChangeFeedReader changeFeedReader)
        {
            var feed = new FeedResponse<PartitionKeyRange>(new[] { new PartitionKeyRange() });

            clientAdapter.Setup(m => m.ReadPartitionKeyRangeFeedAsync(It.IsAny<Uri>())).ReturnsAsync(feed);

            await changeFeedReader.ReadItemsAsync("value").ConfigureAwait(false);

            Action<ChangeFeedOptions> verify = m =>
            {
                Assert.False(m.StartFromBeginning);
                Assert.Equal("value", m.RequestContinuation);
            };

            clientAdapter.Verify(m => m.CreateDocumentChangeFeedQuery(It.IsAny<Uri>(), Is.Value(verify)), Times.Once);
        }
    }
}
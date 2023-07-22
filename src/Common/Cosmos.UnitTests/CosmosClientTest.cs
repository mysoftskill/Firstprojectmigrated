namespace Microsoft.Azure.ComplianceServices.Common.UnitTests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.DataLake.Store;
    using Microsoft.PrivacyServices.Common.Cosmos;
    using Moq;
    using Xunit;

    [Trait("Category", "UnitTest")]
    public class CosmosClientTest
    {

        private static readonly long FOUR_MB = 4 * 1024 * 1024;
        private readonly AdlsConfig testConfig = new AdlsConfig("pxscosmos15-prod-c15",
            "975e1332-de26-4ac9-b120-fe35fa68adf1",
            "azuredatalakestore.net",
            "33e01921-4d64-4f8c-a055-5bdaffd5e33d",
            null
        );

        [Fact]
        public void AdlsClientIsInitializedCorrectly()
        {
            var client = new CosmosAdlsClient(testConfig, "dummyToken", (testConfig) => Task.FromResult(""));
            Assert.True(client.ClientTechInUse() == ClientTech.Adls);
        }

        [Theory]
        [InlineData(4 * 1024 * 1024)]
        [InlineData(3 * 1024 * 1024)]
        public async Task AdlsClientCallsConcurrentWriteForDataWithinLimit(long dataSize)
        {
            var mockedAdlsClient = new Mock<AdlsClient>();
            var client = new CosmosAdlsClient(mockedAdlsClient.Object);
            var data = new byte[dataSize];
            var streamPath = "testStreamPath";
            await client.AppendAsync(streamPath, data).ConfigureAwait(false);
            if(dataSize <= FOUR_MB)
            {
                mockedAdlsClient.Verify(o => o.ConcurrentAppendAsync(streamPath, true, data, 0, (int)dataSize, new CancellationToken()), Times.Once);
            }
            else
            {
                mockedAdlsClient.Verify(o => o.GetAppendStreamAsync(streamPath, new CancellationToken()), Times.Once);
            }
        }

        [Fact]
        public void AdlsMapsDirectoryEntryCorrectly()
        {
            var entry = new DirectoryEntry(
                "name",
                "fullName",
                10,
                "dummy",
                "user",
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeMilliseconds(),
                "DIRECTORY",
                "770",
                true,
                DateTimeOffset.UtcNow.AddDays(2).ToUnixTimeMilliseconds());

            CosmosStreamInfo info = CosmosAdlsClient.ConvertToCosmosStreamInfo(entry);

            Assert.True(info.StreamName == entry.FullName);
            Assert.True(info.Length == entry.Length);
            Assert.True(info.PublishedUpdateTime == entry.LastModifiedTime);
            Assert.True(info.ExpireTime == entry.ExpiryTime);
            Assert.True(info.IsComplete);
            Assert.True(info.IsDirectory);

            entry = new DirectoryEntry(
                "name",
                "fullName",
                10,
                "dummy",
                "user",
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeMilliseconds(),
                "FILE",
                "770",
                true,
                DateTimeOffset.UtcNow.AddDays(2).ToUnixTimeMilliseconds());

            info = CosmosAdlsClient.ConvertToCosmosStreamInfo(entry);

            Assert.True(info.StreamName == entry.FullName);
            Assert.True(info.Length == entry.Length);
            Assert.True(info.PublishedUpdateTime == entry.LastModifiedTime);
            Assert.True(info.ExpireTime == entry.ExpiryTime);
            Assert.True(info.IsComplete);
            Assert.True(info.CosmosPath == entry.FullName);
            Assert.False(info.IsDirectory);
        }

        [Fact]
        public async Task AdlsSetsExpiryTimeCorrectly()
        {
            var mockedAdlsClient = new Mock<AdlsClient>();
            var client = new CosmosAdlsClient(mockedAdlsClient.Object);
            var streamPath = "testStreamPath";
            TimeSpan span = TimeSpan.FromDays(100);
            
            // check expiry time is converted to milliseconds.
            await client.SetLifetimeAsync(streamPath, span, true).ConfigureAwait(false);
            mockedAdlsClient.Verify(o => o.SetExpiryTime(streamPath, ExpiryOption.RelativeToCreationDate, (long)span.TotalMilliseconds, It.IsAny<CancellationToken>()), Times.Once);


            // Check that expiry time can be cleared.
            await client.SetLifetimeAsync(streamPath, null, true).ConfigureAwait(false);
            mockedAdlsClient.Verify(o => o.SetExpiryTime(streamPath, ExpiryOption.NeverExpire, It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Once);

            span = TimeSpan.FromDays(10);

            // Check that we are setting expiry time correctly during create
            await client.CreateAsync(streamPath, span, CosmosCreateStreamMode.CreateAlways).ConfigureAwait(false);
            mockedAdlsClient.Verify(o => o.SetExpiryTime(streamPath, ExpiryOption.RelativeToCreationDate, (long)span.TotalMilliseconds, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

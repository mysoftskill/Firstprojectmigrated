namespace PCF.UnitTests.Pdms
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache;
    using Microsoft.PrivacyServices.Identity;
    using Microsoft.PrivacyServices.Policy;

    using Moq;
    using Xunit;

    using SubjectType = Microsoft.PrivacyServices.CommandFeed.Service.Common.SubjectType;

    /// <summary>
    /// Tests for OnDiskAssetGroupInfoCollection.
    /// </summary>
    [Trait("Category", "UnitTest")]
    public class OnDiskAssetGroupInfoCollectionTests : INeedDataBuilders
    {
#if INCLUDE_TEST_HOOKS
        [Fact]
        public async Task EnsureLocalCaching()
        {
            OnDiskAssetGroupInfoCollection.ClearFiles();

            var innerMock = this.AMockOf<IAssetGroupInfoReader>();
            var onDiskCache = new OnDiskAssetGroupInfoCollection(innerMock.Object);

            innerMock.Setup(m => m.GetLatestVersionAsync()).ReturnsAsync(123);
            innerMock.Setup(m => m.ReadAsync()).Returns(() => innerMock.Object.ReadVersionAsync(123));

            int readCount = 0;

            innerMock
                .Setup(m => m.ReadVersionAsync(It.IsAny<long>()))
                .Callback(() => readCount++)
                .ReturnsAsync<long, IAssetGroupInfoReader, AssetGroupInfoCollectionReadResult>(
                    l => new AssetGroupInfoCollectionReadResult
                    {
                        AssetGroupInfoStream = l.ToString() + ".ss",
                        CreatedTime = DateTimeOffset.UtcNow,
                        AssetGroupInfos = new List<AssetGroupInfoDocument>(),
                        DataVersion = l,
                        VariantInfoStream = l.ToString() + ".ss",
                    });

            long latestVersion = await onDiskCache.GetLatestVersionAsync();
            Assert.Equal(123, latestVersion);

            var latestDataSet = await onDiskCache.ReadAsync();
            Assert.Equal(123, latestDataSet.DataVersion);
            Assert.Equal(1, readCount);

            latestDataSet = await onDiskCache.ReadAsync();
            Assert.Equal(123, latestDataSet.DataVersion);
            Assert.Equal(1, readCount);

            var olderDataSet = await onDiskCache.ReadVersionAsync(1);
            Assert.Equal(1, olderDataSet.DataVersion);
            Assert.Equal(2, readCount);
        }
#endif
    }
}

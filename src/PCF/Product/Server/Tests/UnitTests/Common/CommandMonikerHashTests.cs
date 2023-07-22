namespace PCF.UnitTests
{
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Helpers;
    using Moq;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    [Trait("Category", "UnitTest")]
    public class CommandMonikerHashTests : IClassFixture<MonikerFixture>
    {
        [Fact]
        public void GetAzureQueueWeightedMonikers()
        {
            var expectedMonikers = Config.Instance.AgentAzureQueues.StorageAccounts.Where(x => x.Weight > 0).ToList();
            var monikers = CommandMonikerHash.GetCurrentWeightedMonikers(QueueStorageType.AzureQueueStorage);

            Assert.Equal(expectedMonikers.Count, monikers.Count);
            Assert.True(expectedMonikers.Count > 0);
        }

        [Fact]
        public void GetCosmosDbWeightedMonikers()
        {
            var expectedMonikers = Config.Instance.CosmosDBQueues.Instances.Where(x => x.Weight > 0).ToList();

            using (new FlightDisabled(FlightingNames.CommandQueueEnqueueDisabled))
            {
                var monikers = CommandMonikerHash.GetCurrentWeightedMonikers(QueueStorageType.AzureCosmosDb);

                Assert.Equal(expectedMonikers.Count, monikers.Count);
            }
        }

        [Fact]
        public void GetAzureQueueAllMonikers()
        {
            var expectedMonikers = Config.Instance.AgentAzureQueues.StorageAccounts.ToList();
            var monikers = CommandMonikerHash.GetAllMonikers(QueueStorageType.AzureQueueStorage);

            Assert.Equal(expectedMonikers.Count, monikers.Count);
        }

        [Fact]
        public void GetCosmosDbAllMonikers()
        {
            var expectedMonikers = Config.Instance.CosmosDBQueues.Instances.ToList();
            var monikers = CommandMonikerHash.GetAllMonikers(QueueStorageType.AzureCosmosDb);

            Assert.Equal(expectedMonikers.Count, monikers.Count);
        }

        [Fact]
        public void GetWeightedMonikersByPartitionSize_NoRedisData()
        {
            var allMonikers = CommandMonikerHash.GetAllMonikers(QueueStorageType.AzureCosmosDb);

            var redisMock = new Mock<IRedisClient>();

            // Cache expired
            redisMock.Setup(m => m.GetDataTime(It.IsAny<string>())).Returns(DateTime.UtcNow - TimeSpan.FromDays(1));
            var weightedMonikerList = CommandMonikerHash.GetWeightedMonikersByPartitionSize(redisMock.Object, new AgentId(Guid.NewGuid()), new AssetGroupId(Guid.NewGuid()), "collection", allMonikers);
            Assert.Equal(allMonikers, weightedMonikerList);

            // No data
            redisMock.Setup(m => m.GetDataTime(It.IsAny<string>())).Returns(DateTime.UtcNow - TimeSpan.FromMinutes(1));
            redisMock.Setup(m => m.GetString(It.IsAny<string>())).Returns("");
            weightedMonikerList = CommandMonikerHash.GetWeightedMonikersByPartitionSize(redisMock.Object, new AgentId(Guid.NewGuid()), new AssetGroupId(Guid.NewGuid()), "collection", allMonikers);
            Assert.Equal(allMonikers, weightedMonikerList);
        }

        [Fact]
        public void GetWeightedMonikersByPartitionSize_Normal()
        {
            // cachedRecord has more entries than allMonikers, the extra ones should be removed
            var allMonikers = new List<string>() { "db1", "db2", "db3", "db4" };
            var cachedRecord = new PartitionSizeRedisHelper.PartitionSizeEntryValue[]
            {
                new PartitionSizeRedisHelper.PartitionSizeEntryValue { DbMoniker = "db1", PartitionSizeKb = 1 * 1000 },
                new PartitionSizeRedisHelper.PartitionSizeEntryValue { DbMoniker = "db2", PartitionSizeKb = 2 * 1000 },
                new PartitionSizeRedisHelper.PartitionSizeEntryValue { DbMoniker = "db3", PartitionSizeKb = 3 * 1000 },
                new PartitionSizeRedisHelper.PartitionSizeEntryValue { DbMoniker = "db4", PartitionSizeKb = 4 * 1000 },
            };

            var redisMock = new Mock<IRedisClient>();
            redisMock.Setup(m => m.GetDataTime(It.IsAny<string>())).Returns(DateTime.UtcNow - TimeSpan.FromMinutes(1));
            redisMock.Setup(m => m.GetString(It.IsAny<string>())).Returns(JsonConvert.SerializeObject(cachedRecord));

            var weightedMonikerList = CommandMonikerHash.GetWeightedMonikersByPartitionSize(redisMock.Object, new AgentId(Guid.NewGuid()), new AssetGroupId(Guid.NewGuid()), "collection", allMonikers);

            // all normal sized monikers, no weight calculations
            Assert.Equal(allMonikers, weightedMonikerList);
        }

        [Fact]
        public void GetWeightedMonikersByPartitionSize_Weight()
        {
            // cachedRecord has more entries than allMonikers, the extra ones should be removed
            var allMonikers = new List<string>() { "db1", "db2", "db3", "db4", "db5", "db6" };
            var cachedRecord = new PartitionSizeRedisHelper.PartitionSizeEntryValue[]
            {
                new PartitionSizeRedisHelper.PartitionSizeEntryValue { DbMoniker = "db1", PartitionSizeKb =  1 * 1000 * 1000 },
                new PartitionSizeRedisHelper.PartitionSizeEntryValue { DbMoniker = "db2", PartitionSizeKb =  6 * 1000 * 1000 },
                new PartitionSizeRedisHelper.PartitionSizeEntryValue { DbMoniker = "db3", PartitionSizeKb = 11 * 1000 * 1000 },
                new PartitionSizeRedisHelper.PartitionSizeEntryValue { DbMoniker = "db4", PartitionSizeKb = 16 * 1000 * 1000 },
                new PartitionSizeRedisHelper.PartitionSizeEntryValue { DbMoniker = "db5", PartitionSizeKb = 20 * 1000 * 1000 },
            };

            var redisMock = new Mock<IRedisClient>();
            redisMock.Setup(m => m.GetDataTime(It.IsAny<string>())).Returns(DateTime.UtcNow - TimeSpan.FromMinutes(1));
            redisMock.Setup(m => m.GetString(It.IsAny<string>())).Returns(JsonConvert.SerializeObject(cachedRecord));

            var weightedMonikerList = CommandMonikerHash.GetWeightedMonikersByPartitionSize(redisMock.Object, new AgentId(Guid.NewGuid()), new AssetGroupId(Guid.NewGuid()), "collection", allMonikers);

            Assert.Equal(17, weightedMonikerList.Count);
            Assert.Equal(8, weightedMonikerList.Where(x => x == "db1").Count());
            Assert.Equal(4, weightedMonikerList.Where(x => x == "db2").Count());
            Assert.Equal(2, weightedMonikerList.Where(x => x == "db3").Count());
            Assert.Equal(1, weightedMonikerList.Where(x => x == "db4").Count());
            Assert.Equal(0, weightedMonikerList.Where(x => x == "db5").Count());
            Assert.Equal(2, weightedMonikerList.Where(x => x == "db6").Count());
        }

        [Fact]
        public void GetWeightedMonikersByPartitionSize_Oversize()
        {
            // cachedRecord has more entries than allMonikers, the extra ones should be removed
            var allMonikers = new List<string>() { "db2", "db3", "db4", "db6", "db7" };
            var cachedRecord = new PartitionSizeRedisHelper.PartitionSizeEntryValue[]
            {
                new PartitionSizeRedisHelper.PartitionSizeEntryValue { DbMoniker = "db5", PartitionSizeKb =  1 * 1000 * 1000 },    // normal size, not in the list
                new PartitionSizeRedisHelper.PartitionSizeEntryValue { DbMoniker = "db7", PartitionSizeKb =  3 * 1000 * 1000 },
                new PartitionSizeRedisHelper.PartitionSizeEntryValue { DbMoniker = "db2", PartitionSizeKb =  8 * 1000 * 1000 },
                new PartitionSizeRedisHelper.PartitionSizeEntryValue { DbMoniker = "db3", PartitionSizeKb = 12 * 1000 * 1000 },
                new PartitionSizeRedisHelper.PartitionSizeEntryValue { DbMoniker = "db4", PartitionSizeKb = 18 * 1000 * 1000 },
                new PartitionSizeRedisHelper.PartitionSizeEntryValue { DbMoniker = "db1", PartitionSizeKb = 19 * 1000 * 1000 },    // oversize but already disabled
            };

            var redisMock = new Mock<IRedisClient>();
            redisMock.Setup(m => m.GetDataTime(It.IsAny<string>())).Returns(DateTime.UtcNow - TimeSpan.FromMinutes(1));
            redisMock.Setup(m => m.GetString(It.IsAny<string>())).Returns(JsonConvert.SerializeObject(cachedRecord));

            var weightedMonikerList = CommandMonikerHash.GetWeightedMonikersByPartitionSize(redisMock.Object, new AgentId(Guid.NewGuid()), new AssetGroupId(Guid.NewGuid()), "collection", allMonikers);

            Assert.Equal(17, weightedMonikerList.Count);
            Assert.Equal(4, weightedMonikerList.Where(x => x == "db2").Count());
            Assert.Equal(2, weightedMonikerList.Where(x => x == "db3").Count());
            Assert.Equal(1, weightedMonikerList.Where(x => x == "db4").Count());
            Assert.Equal(2, weightedMonikerList.Where(x => x == "db6").Count());
            Assert.Equal(8, weightedMonikerList.Where(x => x == "db7").Count());
        }

        [Fact]
        public void GetWeightedMonikersByPartitionSize_AllTooBig()
        {
            // cachedRecord has more entries than allMonikers, the extra ones should be removed
            var allMonikers = new List<string>() { "db2", "db3", "db4" };
            var cachedRecord = new PartitionSizeRedisHelper.PartitionSizeEntryValue[]
            {
                new PartitionSizeRedisHelper.PartitionSizeEntryValue { DbMoniker = "db2", PartitionSizeKb = 20 * 1000 * 1000 },
                new PartitionSizeRedisHelper.PartitionSizeEntryValue { DbMoniker = "db3", PartitionSizeKb = 20 * 1000 * 1000 },
                new PartitionSizeRedisHelper.PartitionSizeEntryValue { DbMoniker = "db4", PartitionSizeKb = 20 * 1000 * 1000 },
            };

            var redisMock = new Mock<IRedisClient>();
            redisMock.Setup(m => m.GetDataTime(It.IsAny<string>())).Returns(DateTime.UtcNow - TimeSpan.FromMinutes(1));
            redisMock.Setup(m => m.GetString(It.IsAny<string>())).Returns(JsonConvert.SerializeObject(cachedRecord));

            var weightedMonikerList = CommandMonikerHash.GetWeightedMonikersByPartitionSize(redisMock.Object, new AgentId(Guid.NewGuid()), new AssetGroupId(Guid.NewGuid()), "collection", allMonikers);

            Assert.Equal(3, weightedMonikerList.Count);
        }
    }

    public class MonikerFixture
    {
        public MonikerFixture()
        {
            var monikers = Config.Instance.AgentAzureQueues.StorageAccounts.ToList();
            monikers[0].ConnectionString =
                "DefaultEndpointsProtocol=https;AccountName=testname1;AccountKey=fakefakefakefakefakefakefakefakefakefakefakefakefakefakefakefakefakefakefakefakefake11==;";
            monikers[0].Weight = 1;
        }
    }
}

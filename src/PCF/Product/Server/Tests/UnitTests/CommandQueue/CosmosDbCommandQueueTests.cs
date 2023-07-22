namespace PCF.UnitTests
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.Documents;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Moq;
    using Xunit;
    using PcfCommon = Microsoft.PrivacyServices.CommandFeed.Service.Common;

    [Trait("Category", "UnitTest")]
    public class CosmosDbCommandQueueTests : INeedDataBuilders
    {
        [Fact]
        public async Task PopReturnsValidLeaseReceipts()
        {
            var assetGroupId = this.AnAssetGroupId();
            var agentId = this.AnAgentId();
            var privacyCommand = this.ADeleteCommand(agentId, assetGroupId).Build();
            var mockCollection = new Mock<ICosmosDbQueueCollection>();

            StorageCommandSerializer converter = new StorageCommandSerializer();
            List<Document> commands = new List<Document>
            {
                privacyCommand.AsDocument(),
            };

            mockCollection.SetupGet(m => m.SubjectType).Returns(SubjectType.Msa);
            mockCollection.Setup(m => m.PopAsync(It.IsAny<TimeSpan>(), It.IsAny<string>(), It.IsAny<int>()))
                          .ReturnsAsync(commands.ToList());
            
            CosmosDbCommandQueue commandQueue = new CosmosDbCommandQueue(
                mockCollection.Object,
                agentId,
                assetGroupId);

            var popResult = await commandQueue.PopAsync(1000, null, CommandQueuePriority.Default);
            Assert.Single(popResult.Commands);
            PcfCommon.PrivacyCommand command = popResult.Commands[0];

            Assert.Equal(command.LeaseReceipt.AssetGroupId, assetGroupId);
            Assert.Equal(command.LeaseReceipt.AgentId, agentId);
            Assert.Equal(command.LeaseReceipt.CommandId, command.CommandId);
            Assert.Equal(command.LeaseReceipt.DatabaseMoniker, mockCollection.Object.DatabaseMoniker);
            Assert.Equal(SubjectType.Msa, command.LeaseReceipt.SubjectType);
            Assert.True(commandQueue.SupportsLeaseReceipt(command.LeaseReceipt));
        }

        [Fact]
        public void InvalidLeaseReceiptsAreRejected()
        {
            var assetGroupId = this.AnAssetGroupId();
            var agentId = this.AnAgentId();
            var mockCollection = new Mock<ICosmosDbQueueCollection>();

            CosmosDbCommandQueue commandQueue = new CosmosDbCommandQueue(mockCollection.Object, agentId, assetGroupId);

            LeaseReceipt leaseReceipt = new LeaseReceipt(mockCollection.Object.DatabaseMoniker, new CommandId(Guid.NewGuid()), "etag", assetGroupId, agentId, SubjectType.Msa, DateTimeOffset.UtcNow, "fakequalifier", PrivacyCommandType.Delete, string.Empty, DateTimeOffset.UtcNow, QueueStorageType.AzureCosmosDb);
            
            Assert.True(commandQueue.SupportsLeaseReceipt(leaseReceipt));

            leaseReceipt.DatabaseMoniker = "null";
            Assert.False(commandQueue.SupportsLeaseReceipt(leaseReceipt));

            leaseReceipt.DatabaseMoniker = mockCollection.Object.DatabaseMoniker;
            leaseReceipt.SubjectType = SubjectType.Aad;
            Assert.False(commandQueue.SupportsLeaseReceipt(leaseReceipt));
            
            leaseReceipt.SubjectType = SubjectType.Msa;
            leaseReceipt.AssetGroupId = new AssetGroupId(Guid.NewGuid());
            Assert.False(commandQueue.SupportsLeaseReceipt(leaseReceipt));
        }

        [Theory]
        [InlineData("2b23e8c7-4a4c-4ee7-a323-86e8a7d92e25", "4f3ddc9e-c06d-4b42-ba34-410e702ce24d", "2b23e8c74a4c4ee7a32386e8a7d92e25.4f3ddc9ec06d4b42ba34410e702ce24d")]
        [InlineData("45eeb671-6580-46d1-82f9-1e0f5439b8a7", "2fc02e12-3c2e-43bf-8f29-267b43a79f45", "45eeb671658046d182f91e0f5439b8a7.2fc02e123c2e43bf8f29267b43a79f45")]
        [InlineData("2b23e8c7-4a4c-4ee7-a323-86e8a7d92e25", "07273a99-0859-4faa-9e14-c6d1977641c5", "2b23e8c74a4c4ee7a32386e8a7d92e25.07273a9908594faa9e14c6d1977641c5")]
        public void CreatePartitionKeyOptimized_ReturnsExpectedValue(string agentIdValue, string assetGroupIdValue, string expectedResult)
        {
            //Arrange
            AgentId agentId = new AgentId(agentIdValue);
            AssetGroupId assetGroupId = new AssetGroupId(assetGroupIdValue);

            //Act
            string result = CosmosDbCommandQueue.CreatePartitionKeyOptimized(agentId, assetGroupId);

            //Assert
            Assert.Equal(expectedResult, result);
        }
    }
}

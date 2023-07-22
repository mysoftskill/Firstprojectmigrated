namespace PCF.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Policy;
    using Moq;
    using Newtonsoft.Json.Linq;
    using Xunit;
    using Common = Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using PXSV1 = Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    [Trait("Category", "UnitTest")]
    public class InsertIntoQueueWorkItemHandlerTests : INeedDataBuilders
    {
        [Fact]
        public async Task BelowThresholdInsertsIntoQueue()
        {
            List<InsertIntoQueueWorkItem> publishedItems = new List<InsertIntoQueueWorkItem>();
            var publisherMock = this.AMockOf<IAzureWorkItemQueuePublisher<InsertIntoQueueWorkItem>>();

            publisherMock.Setup(m => m.PublishAsync(It.IsAny<InsertIntoQueueWorkItem>(), It.IsAny<TimeSpan?>()))
                         .Callback<InsertIntoQueueWorkItem, TimeSpan?>((a, b) => publishedItems.Add(a))
                         .Returns(Task.FromResult(true));

            List<Common.PrivacyCommand> publishedCommands = new List<Common.PrivacyCommand>();
            var commandQueueMock = this.AMockOf<ICommandQueue>();
            commandQueueMock.Setup(m => m.EnqueueAsync(It.IsAny<string>(), It.IsAny<Common.PrivacyCommand>()))
                            .Callback<string, Common.PrivacyCommand>((s, pc) => publishedCommands.Add(pc))
                            .Returns(Task.FromResult(string.Empty));
                        
            var commandQueueFactoryMock = this.AMockOf<ICommandQueueFactory>();
            commandQueueFactoryMock.Setup(m => m.CreateQueue(It.IsAny<AgentId>(), It.IsAny<AssetGroupId>(), It.IsAny<Microsoft.PrivacyServices.CommandFeed.Service.Common.SubjectType>(), It.IsAny<QueueStorageType>()))
                                   .Returns(commandQueueMock.Object);

            InsertIntoQueueWorkItemHandler handler = new InsertIntoQueueWorkItemHandler(
                publisherMock.Object,
                this.AMockOf<ICommandLifecycleEventPublisher>().Object,
                commandQueueFactoryMock.Object,
                this.AMockOf<IDataAgentMapFactory>().Object);

            var destinations = Enumerable.Range(1, 9).Select(x => new PxsFilteredCommandDestination
            {
                AgentId = this.AnAgentId(),
                AssetGroupId = this.AnAssetGroupId(),
                ApplicableVariantIds = null,
                AssetGroupQualifier = "qualifier",
                DataTypes = new[] { Policies.Current.DataTypes.Ids.BrowsingHistory, }
            }).ToList();

            var commandId = this.ACommandId();
            InsertIntoQueueWorkItem workItem = new InsertIntoQueueWorkItem
            {
                CommandId = this.ACommandId(),
                CommandType = PrivacyCommandType.Export,
                PxsCommand = JObject.FromObject(new PXSV1.DeleteRequest
                {
                    RequestId = commandId.GuidValue,
                    PrivacyDataType = "BrowsingHistory",
                    RequestType = PXSV1.RequestType.Delete,
                    TimeRangePredicate = new Microsoft.PrivacyServices.CommandFeed.Contracts.Predicates.TimeRangePredicate { StartTime = DateTimeOffset.UtcNow, EndTime = DateTimeOffset.UtcNow },
                    Subject = new MsaSubject { Anid = "a", Opid = "o", Cid = 1, Puid = 2, Xuid = null },
                }),
                IsReplayCommand = false,
                Destinations = destinations,
            };

            var result = await handler.ProcessWorkItemAsync(new QueueWorkItemWrapper<InsertIntoQueueWorkItem>(workItem, Mock.Of<IAzureCloudQueue>(), null, null));

            Assert.Empty(publishedItems);
            Assert.Equal(9, publishedCommands.Count);
            Assert.True(result.Complete);

            foreach (var item in destinations)
            {
                Assert.Contains(publishedCommands, x => x.AssetGroupId == item.AssetGroupId && x.AgentId == item.AgentId);
            }
        }
        
        [Fact]
        public async Task AboveThresholdSmallSplit()
        {
            List<InsertIntoQueueWorkItem> publishedItems = new List<InsertIntoQueueWorkItem>();
            var publisherMock = this.AMockOf<IAzureWorkItemQueuePublisher<InsertIntoQueueWorkItem>>();

            publisherMock.Setup(m => m.PublishAsync(It.IsAny<InsertIntoQueueWorkItem>(), It.IsAny<TimeSpan?>()))
                         .Callback<InsertIntoQueueWorkItem, TimeSpan?>((a, b) => publishedItems.Add(a))
                         .Returns(Task.FromResult(true));

            var publishedCommands = new List<Common.PrivacyCommand>();
            var commandQueueMock = this.AMockOf<ICommandQueue>();
            commandQueueMock.Setup(m => m.EnqueueAsync(It.IsAny<string>(), It.IsAny<Common.PrivacyCommand>()))
                            .Callback<string, Common.PrivacyCommand>((s, pc) => publishedCommands.Add(pc))
                            .Returns(Task.FromResult(string.Empty));

            var commandQueueFactoryMock = this.AMockOf<ICommandQueueFactory>();
            commandQueueFactoryMock.Setup(m => m.CreateQueue(It.IsAny<AgentId>(), It.IsAny<AssetGroupId>(), It.IsAny<Microsoft.PrivacyServices.CommandFeed.Service.Common.SubjectType>(), It.IsAny<QueueStorageType>()))
                                   .Returns(commandQueueMock.Object);

            InsertIntoQueueWorkItemHandler handler = new InsertIntoQueueWorkItemHandler(
                publisherMock.Object,
                this.AMockOf<ICommandLifecycleEventPublisher>().Object,
                commandQueueFactoryMock.Object,
                this.AMockOf<IDataAgentMapFactory>().Object);

            var destinations = Enumerable.Range(1, 15).Select(x => new PxsFilteredCommandDestination
            {
                AgentId = this.AnAgentId(),
                AssetGroupId = this.AnAssetGroupId(),
                ApplicableVariantIds = null,
                AssetGroupQualifier = "qualifier",
                DataTypes = new[] { Policies.Current.DataTypes.Ids.BrowsingHistory, }
            }).ToList();

            var commandId = this.ACommandId();
            InsertIntoQueueWorkItem workItem = new InsertIntoQueueWorkItem
            {
                CommandId = this.ACommandId(),
                CommandType = PrivacyCommandType.Export,
                PxsCommand = JObject.FromObject(new PXSV1.DeleteRequest
                {
                    RequestId = commandId.GuidValue,
                    PrivacyDataType = "BrowsingHistory",
                    RequestType = PXSV1.RequestType.Delete,
                    TimeRangePredicate = new Microsoft.PrivacyServices.CommandFeed.Contracts.Predicates.TimeRangePredicate { StartTime = DateTimeOffset.UtcNow, EndTime = DateTimeOffset.UtcNow },
                    Subject = new MsaSubject { Anid = "a", Opid = "o", Cid = 1, Puid = 2, Xuid = null },
                }),
                IsReplayCommand = false,
                Destinations = destinations,
            };

            var result = await handler.ProcessWorkItemAsync(new QueueWorkItemWrapper<InsertIntoQueueWorkItem>(workItem, Mock.Of<IAzureCloudQueue>(), null, null));

            Assert.Equal(2, publishedItems.Count);
            Assert.Empty(publishedCommands);
            Assert.True(result.Complete);

            int count = 0;
            foreach (var item in publishedItems)
            {
                count += item.Destinations.Count;
            }

            Assert.Equal(15, count);
        }
        
        [Fact]
        public async Task AboveThresholdBigSplit()
        {
            List<InsertIntoQueueWorkItem> publishedItems = new List<InsertIntoQueueWorkItem>();
            var publisherMock = this.AMockOf<IAzureWorkItemQueuePublisher<InsertIntoQueueWorkItem>>();

            publisherMock.Setup(m => m.PublishAsync(It.IsAny<InsertIntoQueueWorkItem>(), It.IsAny<TimeSpan?>()))
                         .Callback<InsertIntoQueueWorkItem, TimeSpan?>((a, b) => publishedItems.Add(a))
                         .Returns(Task.FromResult(true));

            var publishedCommands = new List<Common.PrivacyCommand>();
            var commandQueueMock = this.AMockOf<ICommandQueue>();
            commandQueueMock.Setup(m => m.EnqueueAsync(It.IsAny<string>(), It.IsAny<Common.PrivacyCommand>()))
                            .Callback<string, Common.PrivacyCommand>((s, pc) => publishedCommands.Add(pc))
                            .Returns(Task.FromResult(string.Empty));

            var commandQueueFactoryMock = this.AMockOf<ICommandQueueFactory>();
            commandQueueFactoryMock.Setup(m => m.CreateQueue(It.IsAny<AgentId>(), It.IsAny<AssetGroupId>(), It.IsAny<Microsoft.PrivacyServices.CommandFeed.Service.Common.SubjectType>(), It.IsAny<QueueStorageType>()))
                                   .Returns(commandQueueMock.Object);

            InsertIntoQueueWorkItemHandler handler = new InsertIntoQueueWorkItemHandler(
                publisherMock.Object,
                this.AMockOf<ICommandLifecycleEventPublisher>().Object,
                commandQueueFactoryMock.Object,
                this.AMockOf<IDataAgentMapFactory>().Object);

            var destinations = Enumerable.Range(1, 250).Select(x => new PxsFilteredCommandDestination
            {
                AgentId = this.AnAgentId(),
                AssetGroupId = this.AnAssetGroupId(),
                ApplicableVariantIds = null,
                AssetGroupQualifier = "qualifier",
                DataTypes = new[] { Policies.Current.DataTypes.Ids.BrowsingHistory, }
            }).ToList();

            var commandId = this.ACommandId();
            InsertIntoQueueWorkItem workItem = new InsertIntoQueueWorkItem
            {
                CommandId = this.ACommandId(),
                CommandType = PrivacyCommandType.Export,
                PxsCommand = JObject.FromObject(new PXSV1.DeleteRequest
                {
                    RequestId = commandId.GuidValue,
                    PrivacyDataType = "BrowsingHistory",
                    RequestType = PXSV1.RequestType.Delete,
                    TimeRangePredicate = new Microsoft.PrivacyServices.CommandFeed.Contracts.Predicates.TimeRangePredicate { StartTime = DateTimeOffset.UtcNow, EndTime = DateTimeOffset.UtcNow },
                    Subject = new MsaSubject { Anid = "a", Opid = "o", Cid = 1, Puid = 2, Xuid = null },
                }),
                IsReplayCommand = false,
                Destinations = destinations,
            };

            var result = await handler.ProcessWorkItemAsync(new QueueWorkItemWrapper<InsertIntoQueueWorkItem>(workItem, Mock.Of<IAzureCloudQueue>(), null, null));

            Assert.Equal(10, publishedItems.Count);
            Assert.Empty(publishedCommands);
            Assert.True(result.Complete);

            int count = 0;
            foreach (var item in publishedItems)
            {
                count += item.Destinations.Count;
            }

            Assert.Equal(250, count);
        }
    }
}

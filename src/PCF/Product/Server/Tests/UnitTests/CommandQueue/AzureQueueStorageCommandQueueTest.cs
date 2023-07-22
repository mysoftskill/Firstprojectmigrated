// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace PCF.UnitTests
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Storage.Queue;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue.QueueStorageCommandQueue;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    using Moq;

    using Newtonsoft.Json;

    using Xunit;

    using AgeOutCommand = Microsoft.PrivacyServices.CommandFeed.Service.Common.AgeOutCommand;

    [Trait("Category", "UnitTest")]
    public class AzureQueueStorageCommandQueueTest : INeedDataBuilders
    {
        Mock<IClock> mockClock = new Mock<IClock>(MockBehavior.Strict);

        private Mock<IAssetGroupAzureQueueTrackerCache> mockQueueTrackerCache = new Mock<IAssetGroupAzureQueueTrackerCache>(MockBehavior.Strict);

        public AzureQueueStorageCommandQueueTest()
        {
            this.mockClock.Setup(c => c.UtcNow).Returns(DateTimeOffset.Parse("2019-01-01").ToUniversalTime());
            this.mockQueueTrackerCache.Setup(c => c.QueueExists(It.IsAny<IAzureCloudQueue>())).Returns(true);
            this.mockQueueTrackerCache.Setup(c => c.StartQueueTracker(It.IsAny<IAzureCloudQueue>(), It.IsAny<PrivacyCommandType>()));
        }

        [Theory]
        [InlineData(PrivacyCommandType.AgeOut, true)]
        [InlineData(PrivacyCommandType.AccountClose, false)]
        [InlineData(PrivacyCommandType.Delete, false)]
        [InlineData(PrivacyCommandType.Export, false)]
        [InlineData(PrivacyCommandType.None, false)]
        public void ConstructorOnlyAllowsSupportedCommandTypes(PrivacyCommandType commandType, bool isSupported)
        {
            var assetGroupId = this.AnAssetGroupId();
            var agentId = this.AnAgentId();
            var mockAzureQueue = new Mock<IAzureCloudQueue>();
            mockAzureQueue.Setup(c => c.AccountName).Returns("accountnamegoeshere");

            if (isSupported)
            {
                var queue = new AzureQueueStorageCommandQueue(
                        mockAzureQueue.Object,
                        agentId,
                        assetGroupId,
                        commandType,
                        SubjectType.Msa,
                        this.mockClock.Object,
                        this.mockQueueTrackerCache.Object);
                Assert.NotNull(queue);
            }
            else
            {
                Assert.Throws<ArgumentOutOfRangeException>(
                    () => new AzureQueueStorageCommandQueue(
                        mockAzureQueue.Object,
                        agentId,
                        assetGroupId,
                        commandType,
                        SubjectType.Msa,
                        this.mockClock.Object,
                        this.mockQueueTrackerCache.Object));
            }
        }

        [Fact]
        public async Task GetQueueDepthValid()
        {
            var assetGroupId = this.AnAssetGroupId();
            var agentId = this.AnAgentId();
            string moniker = "fakemoniker";
            int queueDepth = 15;
            var commandType = PrivacyCommandType.AgeOut;
            var subjectType = SubjectType.Msa;
            var mockAzureQueue = new Mock<IAzureCloudQueue>();
            mockAzureQueue.Setup(c => c.AccountName).Returns(moniker);
            mockAzureQueue.Setup(c => c.GetCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(queueDepth);

            var result = new ConcurrentBag<AgentQueueStatistics>();
            AzureQueueStorageCommandQueue commandQueue = new AzureQueueStorageCommandQueue(
                mockAzureQueue.Object,
                agentId,
                assetGroupId,
                commandType,
                subjectType,
                this.mockClock.Object,
                this.mockQueueTrackerCache.Object);

            await commandQueue.AddQueueStatisticsAsync(result, true, new CancellationTokenSource().Token);

            Assert.Single(result);

            var queueStat = result.ToArray()[0];
            Assert.Equal(agentId, queueStat.AgentId);
            Assert.Equal(assetGroupId, queueStat.AssetGroupId);
            Assert.Equal(queueDepth, queueStat.PendingCommandCount.Value);
            Assert.Equal(moniker, queueStat.DbMoniker);
            Assert.Equal(subjectType, queueStat.SubjectType);
            Assert.Equal(commandType, queueStat.CommandType);
        }

        [Fact]
        public async Task GetQueueDepthReturnEmptyWhenQueueNotExists()
        {
            var assetGroupId = this.AnAssetGroupId();
            var agentId = this.AnAgentId();
            var commandType = PrivacyCommandType.AgeOut;
            var mockAzureQueue = new Mock<IAzureCloudQueue>();
            this.mockQueueTrackerCache.Setup(c => c.QueueExists(mockAzureQueue.Object)).Returns(false);

            var result = new ConcurrentBag<AgentQueueStatistics>();
            AzureQueueStorageCommandQueue commandQueue = new AzureQueueStorageCommandQueue(
                mockAzureQueue.Object,
                agentId,
                assetGroupId,
                commandType,
                SubjectType.Msa,
                this.mockClock.Object,
                this.mockQueueTrackerCache.Object);

            await commandQueue.AddQueueStatisticsAsync(result, true, new CancellationTokenSource().Token);

            Assert.Empty(result);
            mockAzureQueue.Verify(c => c.GetCountAsync(It.IsAny<CancellationToken>()), Times.Never);
            this.mockQueueTrackerCache.Verify(c => c.StartQueueTracker(mockAzureQueue.Object, commandType), Times.Once);
        }

        [Theory]
        [InlineData(SubjectType.Msa)]
        [InlineData(SubjectType.Aad)]
        [InlineData(SubjectType.Device)]
        [InlineData(SubjectType.NonWindowsDevice)]
        [InlineData(SubjectType.EdgeBrowser)]
        [InlineData(SubjectType.Demographic)]
        [InlineData(SubjectType.MicrosoftEmployee)]
        public async Task PopReturnsValidLeaseReceipts(SubjectType subjectType)
        {
            var assetGroupId = this.AnAssetGroupId();
            var agentId = this.AnAgentId();
            var privacyCommand = this.AnAgeOutCommand(agentId, assetGroupId, subjectType: subjectType).Build();
            var mockAzureQueue = new Mock<IAzureCloudQueue>();
            mockAzureQueue.Setup(c => c.ExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            mockAzureQueue.Setup(c => c.AccountName).Returns("accountnamegoeshere");

            QueueStorageCommandConverter converter = new QueueStorageCommandConverter();
            List<CloudQueueMessage> cloudQueueMessages = new List<CloudQueueMessage> { QueueStorageCommandConverter.ToCloudQueueMessage(new StorageCommandSerializer().Process(privacyCommand)) };

            mockAzureQueue
                .Setup(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan?>()))
                .ReturnsAsync(cloudQueueMessages);
            mockAzureQueue.Setup(c => c.QueueExists).Returns(true);

            AzureQueueStorageCommandQueue commandQueue = new AzureQueueStorageCommandQueue(
                mockAzureQueue.Object,
                agentId,
                assetGroupId,
                privacyCommand.CommandType,
                subjectType,
                this.mockClock.Object,
                this.mockQueueTrackerCache.Object);

            var popResult = await commandQueue.PopAsync(1000, null, CommandQueuePriority.Default);
            Assert.Single(popResult.Commands);
            Microsoft.PrivacyServices.CommandFeed.Service.Common.PrivacyCommand command = popResult.Commands[0];

            Assert.Equal(command.LeaseReceipt.AssetGroupId, assetGroupId);
            Assert.Equal(command.LeaseReceipt.AgentId, agentId);
            Assert.Equal(command.LeaseReceipt.CommandId, command.CommandId);
            Assert.Equal(command.LeaseReceipt.DatabaseMoniker, mockAzureQueue.Object.AccountName);
            Assert.Equal(QueueStorageType.AzureQueueStorage, command.LeaseReceipt.QueueStorageType);
            Assert.Equal(subjectType, command.LeaseReceipt.SubjectType);
            Assert.True(command.LeaseReceipt.Version >= LeaseReceipt.MinimumAzureQueueStorageVersion);
            Assert.True(commandQueue.SupportsLeaseReceipt(command.LeaseReceipt));
        }

        [Theory]
        [InlineData("a", "a", "8fa0d6d4-3861-441d-bee6-eb7e9f6a3630", "8fa0d6d4-3861-441d-bee6-eb7e9f6a3630", "dbb3d991-78da-4738-86ff-9b9fca2692a0", "dbb3d991-78da-4738-86ff-9b9fca2692a0", SubjectType.Msa, PrivacyCommandType.AgeOut, QueueStorageType.AzureQueueStorage, true)]
        // Unsupported StorageTypes
        [InlineData("a", "a", "8fa0d6d4-3861-441d-bee6-eb7e9f6a3630", "8fa0d6d4-3861-441d-bee6-eb7e9f6a3630", "dbb3d991-78da-4738-86ff-9b9fca2692a0", "dbb3d991-78da-4738-86ff-9b9fca2692a0", SubjectType.Msa, PrivacyCommandType.AgeOut, QueueStorageType.AzureCosmosDb, false)]
        // Unsupported CommandTypes
        [InlineData("a", "a", "8fa0d6d4-3861-441d-bee6-eb7e9f6a3630", "8fa0d6d4-3861-441d-bee6-eb7e9f6a3630", "dbb3d991-78da-4738-86ff-9b9fca2692a0", "dbb3d991-78da-4738-86ff-9b9fca2692a0", SubjectType.Msa, PrivacyCommandType.AccountClose, QueueStorageType.AzureQueueStorage, false)]
        [InlineData("a", "a", "8fa0d6d4-3861-441d-bee6-eb7e9f6a3630", "8fa0d6d4-3861-441d-bee6-eb7e9f6a3630", "dbb3d991-78da-4738-86ff-9b9fca2692a0", "dbb3d991-78da-4738-86ff-9b9fca2692a0", SubjectType.Msa, PrivacyCommandType.Delete, QueueStorageType.AzureQueueStorage, false)]
        [InlineData("a", "a", "8fa0d6d4-3861-441d-bee6-eb7e9f6a3630", "8fa0d6d4-3861-441d-bee6-eb7e9f6a3630", "dbb3d991-78da-4738-86ff-9b9fca2692a0", "dbb3d991-78da-4738-86ff-9b9fca2692a0", SubjectType.Msa, PrivacyCommandType.Export, QueueStorageType.AzureQueueStorage, false)]
        [InlineData("a", "a", "8fa0d6d4-3861-441d-bee6-eb7e9f6a3630", "8fa0d6d4-3861-441d-bee6-eb7e9f6a3630", "dbb3d991-78da-4738-86ff-9b9fca2692a0", "dbb3d991-78da-4738-86ff-9b9fca2692a0", SubjectType.Msa, PrivacyCommandType.None, QueueStorageType.AzureQueueStorage, false)]
        // AssetGroupId mismatch
        [InlineData("a", "a", "8fa0d6d4-3861-441d-bee6-eb7e9f6a3630", "8fa0d6d4-3861-441d-bee6-eb7e9f6a3630", "dbb3d991-78da-4738-86ff-9b9fca2692a0", "e62a6499-999d-4c6e-a1b8-480fc21a2a26", SubjectType.Msa, PrivacyCommandType.AgeOut, QueueStorageType.AzureQueueStorage, false)]
        // AgentId mismatch
        [InlineData("a", "a", "8fa0d6d4-3861-441d-bee6-eb7e9f6a3630", "a69db147-d9cc-40bc-9b8a-d2dbb116ef80", "dbb3d991-78da-4738-86ff-9b9fca2692a0", "dbb3d991-78da-4738-86ff-9b9fca2692a0", SubjectType.Msa, PrivacyCommandType.AgeOut, QueueStorageType.AzureQueueStorage, false)]
        // moniker mismatch
        [InlineData("a", "b", "8fa0d6d4-3861-441d-bee6-eb7e9f6a3630", "8fa0d6d4-3861-441d-bee6-eb7e9f6a3630", "dbb3d991-78da-4738-86ff-9b9fca2692a0", "dbb3d991-78da-4738-86ff-9b9fca2692a0", SubjectType.Msa, PrivacyCommandType.AgeOut, QueueStorageType.AzureQueueStorage, false)]
        public void InvalidLeaseReceiptsAreRejected(
            string monikerLeaseReceipt = "a",
            string monikerQueue = "a",
            string agentIdLeaseReceipt = "c60510c8-af14-4f0e-bfde-3cb0f2ac6616",
            string agentIdQueue = "c60510c8-af14-4f0e-bfde-3cb0f2ac6616",
            string assetGroupIdLeaseReceipt = "8e0c5fa8-b00c-475b-99d6-1aee952172d0",
            string assetGroupIdQueue = "8e0c5fa8-b00c-475b-99d6-1aee952172d0",
            SubjectType subjectType = SubjectType.Msa,
            PrivacyCommandType commandType = PrivacyCommandType.AgeOut,
            QueueStorageType queueStorageType = QueueStorageType.AzureQueueStorage,
            bool isValid = false)
        {
            var assetGroupId = new AssetGroupId(Guid.Parse(assetGroupIdQueue));
            var agentId = new AgentId(Guid.Parse(agentIdQueue));
            var mockAzureQueue = new Mock<IAzureCloudQueue>();
            mockAzureQueue.Setup(c => c.AccountName).Returns(monikerQueue);

            AzureQueueStorageCommandQueue commandQueue = new AzureQueueStorageCommandQueue(mockAzureQueue.Object, agentId, assetGroupId, PrivacyCommandType.AgeOut, subjectType, this.mockClock.Object, this.mockQueueTrackerCache.Object);

            LeaseReceipt leaseReceipt = new LeaseReceipt(
                monikerLeaseReceipt, 
                new CommandId(Guid.NewGuid()), 
                "etag", 
                new AssetGroupId(Guid.Parse(assetGroupIdLeaseReceipt)), 
                new AgentId(Guid.Parse(agentIdLeaseReceipt)), 
                subjectType, 
                DateTimeOffset.UtcNow, 
                "fakequalifier", 
                commandType, 
                string.Empty, 
                DateTimeOffset.UtcNow, 
                queueStorageType);

            if (isValid)
            {
                Assert.True(commandQueue.SupportsLeaseReceipt(leaseReceipt));
            }
            else
            {
                Assert.False(commandQueue.SupportsLeaseReceipt(leaseReceipt));
            }
        }

        [Fact]
        public void MalformedTokenInLeaseReceiptsThrowsJsonReaderException()
        {
            var assetGroupId = this.AnAssetGroupId();
            var agentId = this.AnAgentId();
            var mockAzureQueue = new Mock<IAzureCloudQueue>();
            AzureQueueStorageCommandQueue commandQueue = new AzureQueueStorageCommandQueue(mockAzureQueue.Object, agentId, assetGroupId, PrivacyCommandType.AgeOut, SubjectType.Msa, this.mockClock.Object, this.mockQueueTrackerCache.Object);

            LeaseReceipt leaseReceipt = new LeaseReceipt(mockAzureQueue.Object.AccountName, new CommandId(Guid.NewGuid()), "etag", assetGroupId, agentId, SubjectType.Msa, DateTimeOffset.UtcNow, "fakequalifier", PrivacyCommandType.AgeOut, string.Empty, DateTimeOffset.UtcNow, QueueStorageType.AzureQueueStorage);

            Assert.True(commandQueue.SupportsLeaseReceipt(leaseReceipt));
            Assert.Throws<JsonReaderException>(() => leaseReceipt.DeserializeToken());
        }

        [Fact]
        public void InvalidQueueStorageTypeInLeaseReceiptsThrowsInvalidOperationException()
        {
            var assetGroupId = this.AnAssetGroupId();
            var agentId = this.AnAgentId();
            var mockAzureQueue = new Mock<IAzureCloudQueue>();
            AzureQueueStorageCommandQueue commandQueue = new AzureQueueStorageCommandQueue(mockAzureQueue.Object, agentId, assetGroupId, PrivacyCommandType.AgeOut, SubjectType.Msa, this.mockClock.Object, this.mockQueueTrackerCache.Object);

            LeaseReceipt leaseReceipt = new LeaseReceipt(mockAzureQueue.Object.AccountName, new CommandId(Guid.NewGuid()), "etag", assetGroupId, agentId, SubjectType.Msa, DateTimeOffset.UtcNow, "fakequalifier", PrivacyCommandType.AgeOut, string.Empty, DateTimeOffset.UtcNow, QueueStorageType.AzureCosmosDb);

            Assert.False(commandQueue.SupportsLeaseReceipt(leaseReceipt));
            Assert.Throws<InvalidOperationException>(() => leaseReceipt.DeserializeToken());
        }

        [Fact]
        public void InvalidVersionInLeaseReceiptsThrowsInvalidOperationException()
        {
            var assetGroupId = this.AnAssetGroupId();
            var agentId = this.AnAgentId();
            var mockAzureQueue = new Mock<IAzureCloudQueue>();
            AzureQueueStorageCommandQueue commandQueue = new AzureQueueStorageCommandQueue(mockAzureQueue.Object, agentId, assetGroupId, PrivacyCommandType.AgeOut, SubjectType.Msa, this.mockClock.Object, this.mockQueueTrackerCache.Object);

            LeaseReceipt leaseReceipt = new LeaseReceipt(mockAzureQueue.Object.AccountName, new CommandId(Guid.NewGuid()), "etag", assetGroupId, agentId, SubjectType.Msa, DateTimeOffset.UtcNow, "fakequalifier", PrivacyCommandType.Delete, string.Empty, DateTimeOffset.UtcNow, QueueStorageType.AzureQueueStorage);
            leaseReceipt.Version = 2;

            Assert.Throws<InvalidOperationException>(() => leaseReceipt.DeserializeToken());
        }

        [Theory]
        [InlineData(CommandReplaceOperations.None, (MessageUpdateFields)0)]
        [InlineData(CommandReplaceOperations.CommandContent, MessageUpdateFields.Content)]
        [InlineData(CommandReplaceOperations.LeaseExtension, MessageUpdateFields.Visibility)]
        [InlineData(CommandReplaceOperations.LeaseExtension | CommandReplaceOperations.CommandContent, MessageUpdateFields.Visibility | MessageUpdateFields.Content)]
        public void ShouldConvertToMessageUpdateFields(CommandReplaceOperations operations, MessageUpdateFields messageUpdateFields)
        {
            Assert.Equal(messageUpdateFields, AzureQueueStorageCommandQueue.ConvertToMessageUpdateFields(operations));
        }

        [Theory]
        [InlineData("2000-01-01", "2000-01-15", 14)]
        [InlineData("3000-01-01", "2000-01-01", 0)]
        public void ShouldCreateVisibilityTimeout(string currentTime, string requestedNextVisibleTime, int expectedLeaseTimeDays)
        {
            TimeSpan time = AzureQueueStorageCommandQueue.CreateVisibilityTimeout(DateTimeOffset.Parse(currentTime), DateTimeOffset.Parse(requestedNextVisibleTime));
            Assert.Equal(TimeSpan.FromDays(expectedLeaseTimeDays), time);
        }

        [Theory]
        [InlineData("01:00:00", "01:05:00", 300)]
        [InlineData("01:00:00.123", "01:05:00.345", 300)]
        public void ShouldCreateVisibilityTimeoutWholeSeconds(string currentTime, string requestedNextVisibleTime, int expectedLeaseTimeDays)
        {
            // Make sure TimeSpan returned by CreateVisibilityTimeout does not have milliseconds part
            TimeSpan time = AzureQueueStorageCommandQueue.CreateVisibilityTimeout(DateTimeOffset.Parse(currentTime), DateTimeOffset.Parse(requestedNextVisibleTime));
            Assert.Equal(TimeSpan.FromSeconds(expectedLeaseTimeDays), time);
        }

        [Theory]
        // No results on first try. Don't try again. Return nothing.
        [InlineData(-5, new[] { 0 }, new[] { 0 }, 0, 0)]
        [InlineData(0, new[] { 0 }, new[] { 0 }, 0, 0)]
        [InlineData(31, new[] { 31 }, new[] { 0 }, 1, 0)]
        [InlineData(33, new[] { 32 }, new[] { 0 }, 1, 0)]
        [InlineData(50, new[] { 32 }, new[] { 0 }, 1, 0)]
        [InlineData(100, new[] { 32 }, new[] { 0 }, 1, 0)]

        // <=32 requested will only attempt to dequeue one time.
        [InlineData(5, new[] { 5 }, new[] { 5 }, 1, 5)]
        [InlineData(31, new[] { 31 }, new[] { 10 }, 1, 10)]
        [InlineData(32, new[] { 32 }, new[] { 0 }, 1, 0)]
        [InlineData(32, new[] { 32 }, new[] { 32 }, 1, 32)]

        // >32 will attempt, and results found, will attempt to dequeue multiple times.
        // 2 dequeue attempts
        [InlineData(33, new[] { 32, 1 }, new[] { 32, 0 }, 2, 32)]
        [InlineData(33, new[] { 32, 1 }, new[] { 32, 1 }, 2, 33)]
        [InlineData(50, new[] { 32, 18 }, new[] { 32, 10 }, 2, 42)]
        [InlineData(64, new[] { 32, 32 }, new[] { 32, 32 }, 2, 64)]

        // 3 dequeue attempts
        [InlineData(65, new[] { 32, 32, 1 }, new[] { 32, 32, 0 }, 3, 64)]
        [InlineData(65, new[] { 32, 32, 1 }, new[] { 32, 32, 1 }, 3, 65)]
        [InlineData(70, new[] { 32, 32, 6 }, new[] { 32, 32, 5 }, 3, 69)]
        [InlineData(70, new[] { 32, 32, 6 }, new[] { 32, 32, 6 }, 3, 70)]
        [InlineData(96, new[] { 32, 32, 32 }, new[] { 32, 32, 0 }, 3, 64)]
        [InlineData(96, new[] { 32, 32, 32 }, new[] { 32, 32, 16 }, 3, 80)]
        [InlineData(96, new[] { 32, 32, 32 }, new[] { 32, 32, 32 }, 3, 96)]

        // 4 dequeue attempts
        [InlineData(97, new[] { 32, 32, 32, 1 }, new[] { 32, 32, 32, 0 }, 4, 96)]
        [InlineData(97, new[] { 32, 32, 32, 1 }, new[] { 32, 32, 32, 1 }, 4, 97)]
        [InlineData(100, new[] { 32, 32, 32, 4 }, new[] { 32, 32, 32, 4 }, 4, 100)]

        // More dequeue attempts than 4 does not happen
        [InlineData(200, new[] { 32, 32, 32, 32, 0 }, new[] { 32, 32, 32, 4 }, 4, 100)]

        public async Task GetMessagesCountValid(int input, int[] expectedGetCount, int[] expectedGetMessageResponse, int expectedNumberGetMessageCalls, int numberResultsReturned)
        {
            // Arrange
            int MaxResults = 32;

            var assetGroupId = this.AnAssetGroupId();
            var agentId = this.AnAgentId();
            var mockAzureQueue = new Mock<IAzureCloudQueue>();
            mockAzureQueue.Setup(c => c.AccountName).Returns("name");
            mockAzureQueue.Setup(c => c.QueueExists).Returns(true);

            var messagesPerQueueResponse = new List<List<CloudQueueMessage>>()
            {
                new List<CloudQueueMessage>(),
                new List<CloudQueueMessage>(),
                new List<CloudQueueMessage>(),
                new List<CloudQueueMessage>(),
            };
            for (int i = 0; i < expectedGetMessageResponse.Length; i++)
            {
                List<CloudQueueMessage> messages = this.CreateCloudQueueMessages(Math.Min(Math.Max(expectedGetMessageResponse[i], 0), MaxResults));
                messagesPerQueueResponse[i] = messages;
            }

            mockAzureQueue
                .SetupSequence(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan?>()))
                .ReturnsAsync(messagesPerQueueResponse[0])
                .ReturnsAsync(messagesPerQueueResponse[1])
                .ReturnsAsync(messagesPerQueueResponse[2])
                .ReturnsAsync(messagesPerQueueResponse[3]);


            AzureQueueStorageCommandQueue commandQueue = new AzureQueueStorageCommandQueue(mockAzureQueue.Object, agentId, assetGroupId, PrivacyCommandType.AgeOut, SubjectType.Msa, this.mockClock.Object, this.mockQueueTrackerCache.Object);

            // Act
            var results = await commandQueue.PopAsync(input, null, CommandQueuePriority.Default);

            // Assert
            Assert.NotNull(results);
            Assert.Equal(numberResultsReturned, results.Commands.Count);

            foreach (int count in expectedGetCount)
            {
                if (count > 0)
                {
                    mockAzureQueue.Verify(c => c.GetMessagesAsync(count, It.IsAny<TimeSpan>()), Times.AtLeast(1));
                }
                else
                {
                    mockAzureQueue.Verify(c => c.GetMessagesAsync(count, It.IsAny<TimeSpan>()), Times.Never);
                }
            }

            mockAzureQueue.Verify(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>()), Times.Exactly(expectedNumberGetMessageCalls));
        }

        private List<CloudQueueMessage> CreateCloudQueueMessages(int numberResultsReturned)
        {
            var messages = new List<CloudQueueMessage>();
            for (int i = 0; i < numberResultsReturned; i++)
            {
                AgeOutCommand command = this.AnAgeOutCommand().With(x => x.Subject, this.AnMsaSubject().Build());
                StoragePrivacyCommand storageCommand = new StorageCommandSerializer().Process(command);
                CloudQueueMessage cloudQueueMessage = QueueStorageCommandConverter.ToCloudQueueMessage(storageCommand);
                messages.Add(cloudQueueMessage);
            }

            return messages;
        }
    }
}
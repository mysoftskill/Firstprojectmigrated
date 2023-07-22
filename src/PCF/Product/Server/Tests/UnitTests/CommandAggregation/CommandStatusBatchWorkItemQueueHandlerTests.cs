namespace PCF.UnitTests
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Microsoft.Azure.Storage.Queue;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks.CommandStatusAggregation;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Xunit2;
    using ScopeRuntime;
    using Xunit;

    [Trait("Category", "UnitTest")]
    public class CommandStatusBatchWorkItemQueueHandlerTests : INeedDataBuilders
    {
        [Fact]
        public async Task ProcessWorkItemInsert()
        {
            var commandId = this.ACommandId();
            var agentId = this.AnAgentId();
            var assetGroupId = this.AnAssetGroupId();

            var startedEvent = this.AStartedEvent(agentId, assetGroupId, commandId).Build();
            var softDeleteEvent = this.ASoftDeletedEvent(agentId, assetGroupId, commandId).Build();
            var completedEvent = this.ACompletedEvent(agentId, assetGroupId, commandId).Build();

            var coldStorage = new Mock<ICommandHistoryRepository>();
            var publisher = new Mock<IAzureWorkItemQueuePublisher<CheckCompletionWorkItem>>();

            var fixture = new Fixture();

            var handler = new CommandStatusBatchWorkItemQueueHandler(publisher.Object, coldStorage.Object);

            int publishCount = 0;

            publisher.Setup(m => m.PublishAsync(It.IsAny<CheckCompletionWorkItem>(), It.IsAny<TimeSpan?>()))
                     .Callback<CheckCompletionWorkItem, TimeSpan?>((a, b) =>
                     {
                         Assert.Equal(commandId, a.CommandId);
                         publishCount++;
                     })
                     .Returns(Task.FromResult(true));

            var key = (agentId, assetGroupId);
            var record = this.AColdStorageCommandRecord(commandId).Build();
            record.StatusMap[key] = new CommandHistoryAssetGroupStatusRecord(agentId, assetGroupId);
            record.AuditMap[key] = new CommandIngestionAuditRecord { IngestionStatus = CommandIngestionStatus.SendingToAgent };

            coldStorage.Setup(m => m.QueryAsync(commandId, It.IsAny<CommandHistoryFragmentTypes>())).ReturnsAsync(record);
            coldStorage.Setup(m => m.ReplaceAsync(It.IsAny<CommandHistoryRecord>(), It.IsAny<CommandHistoryFragmentTypes>())).Returns(Task.FromResult(true));

            var wrapper = new QueueWorkItemWrapper<CommandStatusBatchWorkItem>(
                new CommandStatusBatchWorkItem(commandId, null, new CommandLifecycleEvent[] { startedEvent, softDeleteEvent, completedEvent }),
                this.AMockOf<IAzureCloudQueue>().Object,
                this.AnInstanceOf<CloudQueueMessage>(),
                wi => new byte[0]);

            var result = await handler.ProcessWorkItemAsync(wrapper);

            Assert.Equal(1, publishCount);

            var assetGroupRecord = record.StatusMap[key];
            Assert.NotNull(assetGroupRecord.CompletedTime);
            Assert.NotNull(assetGroupRecord.SoftDeleteTime);
            Assert.NotNull(assetGroupRecord.IngestionTime);

            var auditRecord = record.AuditMap[key];
            Assert.Equal(CommandIngestionStatus.SentToAgent, auditRecord.IngestionStatus);
        }

        [Theory]
        [InlineData(false, -1, true)] 	// -1 indicates a null command creation time
        [InlineData(true, -1, true)]	// -1 indicates a null command creation time
        [InlineData(false, 1, true)]
        [InlineData(true, 1, false)]
        [InlineData(false, 61, true)]
        [InlineData(true, 61, true)]
        public async Task ProcessWorkItem_ExpiredRecords(bool isRecordNull, int commandAgeDays, bool isExpectedResultSuccess)
        {
            var commandId = this.ACommandId();
            var agentId = this.AnAgentId();
            var assetGroupId = this.AnAssetGroupId();

            var startedEvent = this.AStartedEvent(agentId, assetGroupId, commandId).Build();
            var softDeleteEvent = this.ASoftDeletedEvent(agentId, assetGroupId, commandId).Build();
            var completedEvent = this.ACompletedEvent(agentId, assetGroupId, commandId).Build();

            var coldStorage = new Mock<ICommandHistoryRepository>();
            var publisher = new Mock<IAzureWorkItemQueuePublisher<CheckCompletionWorkItem>>();

            var fixture = new Fixture();

            var handler = new CommandStatusBatchWorkItemQueueHandler(publisher.Object, coldStorage.Object);

            int publishCount = 0;

            publisher.Setup(m => m.PublishAsync(It.IsAny<CheckCompletionWorkItem>(), It.IsAny<TimeSpan?>()))
                     .Callback<CheckCompletionWorkItem, TimeSpan?>((a, b) =>
                     {
                         Assert.Equal(commandId, a.CommandId);
                         publishCount++;
                     })
                     .Returns(Task.FromResult(true));

            var key = (agentId, assetGroupId);
            var record = this.AColdStorageCommandRecord(commandId).Build();
            record.StatusMap[key] = new CommandHistoryAssetGroupStatusRecord(agentId, assetGroupId);
            record.AuditMap[key] = new CommandIngestionAuditRecord { IngestionStatus = CommandIngestionStatus.SendingToAgent };

            if (!isRecordNull)
            {
                coldStorage.Setup(m => m.QueryAsync(commandId, It.IsAny<CommandHistoryFragmentTypes>())).ReturnsAsync(record);
                coldStorage.Setup(m => m.ReplaceAsync(It.IsAny<CommandHistoryRecord>(), It.IsAny<CommandHistoryFragmentTypes>())).Returns(Task.FromResult(true));
            }

            DateTimeOffset? commandCreationTime = null;
            if (commandAgeDays > 0)
            {
                commandCreationTime = DateTimeOffset.UtcNow.AddDays(-commandAgeDays);
            }

            var wrapper = new QueueWorkItemWrapper<CommandStatusBatchWorkItem>(
                new CommandStatusBatchWorkItem(commandId, commandCreationTime, new CommandLifecycleEvent[] { startedEvent, softDeleteEvent, completedEvent }),
                this.AMockOf<IAzureCloudQueue>().Object,
                this.AnInstanceOf<CloudQueueMessage>(),
                wi => new byte[0]);

            bool isCompleted;

            try
            {
                QueueProcessResult result = await handler.ProcessWorkItemAsync(wrapper);
                isCompleted = result.Complete;
            }
            catch (InvalidOperationException)
            {
                isCompleted = false;
            }

            Assert.Equal(isExpectedResultSuccess, isCompleted);
        }
    }
}

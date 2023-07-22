namespace PCF.UnitTests.BackgroundTasks
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks.DeleteExportArchive;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.PXS.Command.CommandStatus;
    using Moq;
    using Xunit;

    [Trait("Category", "UnitTest")]
    public class DeleteFullExportArchiveHandlerTests
    {

        [Fact]
        /// <summary>
        /// Verify completion when no record is present for given commandId
        /// </summary>
        public async void CompleteWithNoHistoryRecord()
        {
            List<DeleteFullExportArchiveWorkItem> publishedItems = new List<DeleteFullExportArchiveWorkItem>();
            var publisherMock = new Mock<IAzureWorkItemQueuePublisher<DeleteFullExportArchiveWorkItem>>();
            var repository = new Mock<ICommandHistoryRepository>();
            repository.Setup(r => r.QueryAsync(It.IsAny<CommandId>(), It.IsAny<CommandHistoryFragmentTypes>()))
                .Returns(Task.FromResult<CommandHistoryRecord>(null));

            publisherMock.Setup(m => m.PublishAsync(It.IsAny<DeleteFullExportArchiveWorkItem>(), It.IsAny<TimeSpan?>()))
                         .Callback<DeleteFullExportArchiveWorkItem, TimeSpan?>((a, b) => publishedItems.Add(a))
                         .Returns(Task.FromResult(true));

            var handler = new DeleteFullExportArchiveWorkItemHandler(repository.Object);
            var result = await handler.ProcessWorkItemAsync(new QueueWorkItemWrapper<DeleteFullExportArchiveWorkItem>(
                new DeleteFullExportArchiveWorkItem(new CommandId(Guid.NewGuid())),
                Mock.Of<IAzureCloudQueue>(),
                null,
                null));

            Assert.True(result.Complete);
        }

        /// <summary>
        /// Verify completion when status of Delete is AlreadyDeleted.
        /// </summary>
        [Fact]
        public async void CompleteWithArchiveAlreadyDeleted()
        {
            List<DeleteFullExportArchiveWorkItem> publishedItems = new List<DeleteFullExportArchiveWorkItem>();
            var publisherMock = new Mock<IAzureWorkItemQueuePublisher<DeleteFullExportArchiveWorkItem>>();
            var commandId = new CommandId(Guid.NewGuid().ToString());
            var commandHistoryCore = new CommandHistoryCoreRecord(commandId);
            commandHistoryCore.ExportArchivesDeleteStatus = ExportArchivesDeleteStatus.DeleteCompleted;
            var commandHistory = new CommandHistoryRecord(commandId, commandHistoryCore, null, null, null, null); ;

            var repository = new Mock<ICommandHistoryRepository>();
            repository.Setup(r => r.QueryAsync(It.IsAny<CommandId>(), It.IsAny<CommandHistoryFragmentTypes>()))
                .Returns(Task.FromResult(commandHistory));

            publisherMock.Setup(m => m.PublishAsync(It.IsAny<DeleteFullExportArchiveWorkItem>(), It.IsAny<TimeSpan?>()))
                         .Callback<DeleteFullExportArchiveWorkItem, TimeSpan?>((a, b) => publishedItems.Add(a))
                         .Returns(Task.FromResult(true));

            var handler = new DeleteFullExportArchiveWorkItemHandler(repository.Object);
            var result = await handler.ProcessWorkItemAsync(new QueueWorkItemWrapper<DeleteFullExportArchiveWorkItem>(
                new DeleteFullExportArchiveWorkItem(new CommandId(Guid.NewGuid())),
                Mock.Of<IAzureCloudQueue>(),
                null,
                null));
            Assert.True(result.Complete);
        }

        /// <summary>
        /// Verify completion when status of Delete is DeleteInProgress.
        /// </summary>
        [Fact]
        public async void CompleteWithArchiveDeleteInProgress()
        {
            List<DeleteFullExportArchiveWorkItem> publishedItems = new List<DeleteFullExportArchiveWorkItem>();
            var publisherMock = new Mock<IAzureWorkItemQueuePublisher<DeleteFullExportArchiveWorkItem>>();
            var commandId = new CommandId(Guid.NewGuid().ToString());
            var commandHistoryCore = new CommandHistoryCoreRecord(commandId);
            commandHistoryCore.ExportArchivesDeleteStatus = ExportArchivesDeleteStatus.DeleteInProgress;
            var commandHistory = new CommandHistoryRecord(commandId, commandHistoryCore, null, null, null, null); ;

            var repository = new Mock<ICommandHistoryRepository>();
            repository.Setup(r => r.QueryAsync(It.IsAny<CommandId>(), It.IsAny<CommandHistoryFragmentTypes>()))
                .Returns(Task.FromResult(commandHistory));

            publisherMock.Setup(m => m.PublishAsync(It.IsAny<DeleteFullExportArchiveWorkItem>(), It.IsAny<TimeSpan?>()))
                         .Callback<DeleteFullExportArchiveWorkItem, TimeSpan?>((a, b) => publishedItems.Add(a))
                         .Returns(Task.FromResult(true));

            var handler = new DeleteFullExportArchiveWorkItemHandler(repository.Object);
            var result = await handler.ProcessWorkItemAsync(new QueueWorkItemWrapper<DeleteFullExportArchiveWorkItem>(
                new DeleteFullExportArchiveWorkItem(new CommandId(Guid.NewGuid())),
                Mock.Of<IAzureCloudQueue>(),
                null,
                null));
            Assert.True(result.Complete);
        }

    }
}

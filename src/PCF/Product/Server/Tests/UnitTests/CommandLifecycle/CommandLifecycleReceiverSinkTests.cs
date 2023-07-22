namespace PCF.UnitTests
{
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Moq;
    using PCF.UnitTests.CommandLifecycle;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;

    [Trait("Category", "UnitTest")]
    public class CommandLifecycleReceiverSinkTests : BaseCommandLifeCycleTests, INeedDataBuilders
    {
        [Fact]
        public async Task NoExceptions()
        {
            this.Initialize();
            bool checkpointCalled = false;
            List<JsonEventData> publishedRetries = new List<JsonEventData>();

            var checkpointProcessor = this.AMockOf<ICommandLifecycleCheckpointProcessor>();
            var retryPublisher = this.AMockOf<IAzureWorkItemQueuePublisher<JsonEventData[]>>();

            checkpointProcessor.Setup(m => m.ShouldCheckpoint()).Returns(true);
            checkpointProcessor
                .Setup(m => m.CheckpointAsync())
                .Callback(() =>
                {
                    checkpointCalled = true;
                })
                .Returns(Task.FromResult(true));

            retryPublisher
                .Setup(m => m.PublishWithSplitAsync(It.IsAny<IEnumerable<JsonEventData>>(), It.IsAny<Func<IEnumerable<JsonEventData>, JsonEventData[]>>(), It.IsAny<Func<int, TimeSpan>>()))
                .Callback<IEnumerable<JsonEventData>, Func<IEnumerable<JsonEventData>, JsonEventData[]>, Func<int, TimeSpan>>((a, b, c) =>
                {
                    publishedRetries.AddRange(a);
                })
                .Returns(Task.FromResult(true));

            CommandLifecycleReceiverSink sink = new CommandLifecycleReceiverSink(() => checkpointProcessor.Object, retryPublisher.Object, "foobar");

            bool eventHubCommitted = false;
            Func<Task> commitCallback = () =>
            {
                eventHubCommitted = true;
                return Task.FromResult(true);
            };

            var events = new CommandLifecycleEvent[] { this.ACompletedEvent(), this.ASoftDeletedEvent(), this.AStartedEvent() };
            var toPublish = CommandLifecycleEventParser.Serialize(events);

            await sink.HandleEventBatchAsync(toPublish, commitCallback);

            Assert.True(eventHubCommitted);
            Assert.Empty(publishedRetries);
            Assert.True(checkpointCalled);
        }

        /// <summary>
        /// Tests cases where an event fails to parse and an event fails to process.
        /// </summary>
        [Fact]
        public async Task EventParseAndProcessFailures()
        {
            this.Initialize();
            bool checkpointCalled = false;
            List<JsonEventData> publishedRetries = new List<JsonEventData>();

            var checkpointProcessor = this.AMockOf<ICommandLifecycleCheckpointProcessor>();
            var retryPublisher = this.AMockOf<IAzureWorkItemQueuePublisher<JsonEventData[]>>();

            checkpointProcessor.Setup(m => m.ShouldCheckpoint()).Returns(true);
            checkpointProcessor
                .Setup(m => m.CheckpointAsync())
                .Callback(() =>
                {
                    checkpointCalled = true;
                })
                .Returns(Task.FromResult(true));

            checkpointProcessor.Setup(m => m.Process(It.IsAny<CommandCompletedEvent>())).Throws<InvalidOperationException>();

            retryPublisher
                .Setup(m => m.PublishWithSplitAsync(It.IsAny<IEnumerable<JsonEventData>>(), It.IsAny<Func<IEnumerable<JsonEventData>, JsonEventData[]>>(), It.IsAny<Func<int, TimeSpan>>()))
                .Callback<IEnumerable<JsonEventData>, Func<IEnumerable<JsonEventData>, JsonEventData[]>, Func<int, TimeSpan>>((a, b, c) =>
                {
                    publishedRetries.AddRange(a);
                })
                .Returns(Task.FromResult(true));

            CommandLifecycleReceiverSink sink = new CommandLifecycleReceiverSink(() => checkpointProcessor.Object, retryPublisher.Object, "foobar");

            bool eventHubCommitted = false;
            Func<Task> commitCallback = () =>
            {
                eventHubCommitted = true;
                return Task.FromResult(true);
            };

            var events = new CommandLifecycleEvent[] { this.ACompletedEvent(), this.ASoftDeletedEvent(), this.AStartedEvent() };
            var toPublish = CommandLifecycleEventParser.Serialize(events).ToList();

            // Fake event.
            toPublish.Add(new JsonEventData { Data = new byte[] { 1, 2, 3, 4, 5 }, Properties = { ["EventName"] = "foobar" } });

            await sink.HandleEventBatchAsync(toPublish, commitCallback);

            Assert.True(eventHubCommitted);
            Assert.True(checkpointCalled);

            Assert.Equal(2, publishedRetries.Count);
        }

        /// <summary>
        /// Tests the case where the processor fails to checkpoint a batch of work.
        /// </summary>
        [Fact]
        public async Task ProcessorCheckpointFailure()
        {
            this.Initialize();
            List<JsonEventData> publishedRetries = new List<JsonEventData>();

            var checkpointProcessor = this.AMockOf<ICommandLifecycleCheckpointProcessor>();
            var retryPublisher = this.AMockOf<IAzureWorkItemQueuePublisher<JsonEventData[]>>();

            // only checkpoint on the 3rd batch.
            checkpointProcessor.SetupSequence(m => m.ShouldCheckpoint()).Returns(false).Returns(false).Returns(true);

            checkpointProcessor
                .Setup(m => m.CheckpointAsync())
                .Throws(new InvalidOperationException());

            retryPublisher
                .Setup(m => m.PublishWithSplitAsync(It.IsAny<IEnumerable<JsonEventData>>(), It.IsAny<Func<IEnumerable<JsonEventData>, JsonEventData[]>>(), It.IsAny<Func<int, TimeSpan>>()))
                .Callback<IEnumerable<JsonEventData>, Func<IEnumerable<JsonEventData>, JsonEventData[]>, Func<int, TimeSpan>>((a, b, c) =>
                {
                    publishedRetries.AddRange(a);
                })
                .Returns(Task.FromResult(true));

            CommandLifecycleReceiverSink sink = new CommandLifecycleReceiverSink(() => checkpointProcessor.Object, retryPublisher.Object, "foobar");

            bool eventHubCommitted = false;
            Func<Task> commitCallback = () =>
            {
                eventHubCommitted = true;
                return Task.FromResult(true);
            };

            Func<IEnumerable<JsonEventData>> getSerializedEvents = 
                () => CommandLifecycleEventParser.Serialize(new CommandLifecycleEvent[] { this.ACompletedEvent(), this.ASoftDeletedEvent(), this.AStartedEvent() }).ToList();

            // Publish 3 batches.
            for (int i = 0; i < 2; ++i)
            {
                await sink.HandleEventBatchAsync(getSerializedEvents(), commitCallback);

                Assert.False(eventHubCommitted);
                Assert.Empty(publishedRetries);
            }

            await sink.HandleEventBatchAsync(getSerializedEvents(), commitCallback);

            // Validate that we published all 9 events to the retry queue, and that we proceeded to checkpoint the underlying event hub.
            Assert.True(eventHubCommitted);
            Assert.Equal(3, publishedRetries.Count);
            Assert.Equal(9, publishedRetries.SelectMany(CommandLifecycleEventParser.ParseEvents).Count());
        }

        /// <summary>
        /// Tests the case where the processor fails to checkpoint a batch of work.
        /// </summary>
        [Fact(Skip = "Skip this since we don't throw exception in PublishRetriesAsync method anymore.")]
        public async Task ProcessorCheckpointFailureWithRetryPublishFailure()
        {
            this.Initialize();
            List<JsonEventData> publishedRetries = new List<JsonEventData>();

            var checkpointProcessor = this.AMockOf<ICommandLifecycleCheckpointProcessor>();
            var retryPublisher = this.AMockOf<IAzureWorkItemQueuePublisher<JsonEventData[]>>();
            
            // Checkpoint to the underlying processor always fails.
            checkpointProcessor.Setup(m => m.ShouldCheckpoint()).Returns(true);
            checkpointProcessor
                .Setup(m => m.CheckpointAsync())
                .Throws<InvalidOperationException>();

            // Retry queue fails the first time.
            bool retryFail = true;
            retryPublisher
                .Setup(m => m.PublishWithSplitAsync(It.IsAny<IEnumerable<JsonEventData>>(), It.IsAny<Func<IEnumerable<JsonEventData>, JsonEventData[]>>(), It.IsAny<Func<int, TimeSpan>>()))
                .Callback<IEnumerable<JsonEventData>, Func<IEnumerable<JsonEventData>, JsonEventData[]>, Func<int, TimeSpan>>((a, b, c) =>
                {
                    if (retryFail)
                    {
                        retryFail = false;
                        throw new InvalidOperationException();
                    }

                    publishedRetries.AddRange(a);
                })
                .Returns(Task.FromResult(true));

            CommandLifecycleReceiverSink sink = new CommandLifecycleReceiverSink(() => checkpointProcessor.Object, retryPublisher.Object, "foobar");

            bool eventHubCommitted = false;
            Func<Task> commitCallback = () =>
            {
                eventHubCommitted = true;
                return Task.FromResult(true);
            };


            Func<IEnumerable<JsonEventData>> getSerializedEvents =
                () => CommandLifecycleEventParser.Serialize(new CommandLifecycleEvent[] { this.ACompletedEvent(), this.ASoftDeletedEvent(), this.AStartedEvent() }).ToList();

            await Assert.ThrowsAsync<InvalidOperationException>(() => sink.HandleEventBatchAsync(getSerializedEvents(), commitCallback));

            Assert.False(eventHubCommitted);
            Assert.Empty(publishedRetries);

            await sink.HandleEventBatchAsync(getSerializedEvents(), commitCallback);

            // All 6 events should be published to the retry queue. 3 for the first batch, which double-failed,
            // and 3 for this batch.
            Assert.True(eventHubCommitted);
            Assert.Equal(2, publishedRetries.Count);
            Assert.Equal(6, publishedRetries.SelectMany(CommandLifecycleEventParser.ParseEvents).Count());
        }
    }
}

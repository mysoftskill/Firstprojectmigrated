namespace PCF.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks.CommandStatusAggregation;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    using Moq;

    using Xunit;

    [Trait("Category", "UnitTest")]
    public class CommandHistoryAggregationReceiverTests : INeedDataBuilders
    {
        /// <summary>
        /// Tests that a set of events with the same command ID are aggregated together.
        /// </summary>
        [Fact]
        public async Task EventsAreConsolidated()
        {
            var commandId = this.ACommandId();
            var startedEvent = this.AStartedEvent(commandId: commandId).Build();
            var sentToAgentEvent = this.ASentToAgentEvent(commandId: commandId).Build();
            var pendingEvent = this.APendingEvent(commandId: commandId).Build();
            var failedEvent = this.AFailedEvent(commandId: commandId).Build();
            var unexpectedCommandEvent = this.AUnexpectedCommandEvent(commandId: commandId).Build();
            var verificationFailedEvent = this.AVerificationFailedEvent(commandId: commandId).Build();
            var unexpectedVerificationFailureCommandEvent = this.AUnexpectedVerificationFailureEvent(commandId: commandId).Build();
            var softDeleteEvent = this.ASoftDeletedEvent(commandId: commandId).Build();
            var completedEvent = this.ACompletedEvent(commandId: commandId).Build();
            var rawdataEvent = this.ARawDataEvent(commandId: commandId).Build();
            var droppedEvent = this.ADroppedEvent(commandId: commandId).Build();

            var publisher = new Mock<IAzureWorkItemQueuePublisher<CommandStatusBatchWorkItem>>();

            CommandHistoryAggregationReceiver receiver = new CommandHistoryAggregationReceiver(publisher.Object);

            receiver.Process(startedEvent);
            receiver.Process(sentToAgentEvent);
            receiver.Process(pendingEvent);
            receiver.Process(failedEvent);
            receiver.Process(unexpectedCommandEvent);
            receiver.Process(verificationFailedEvent);
            receiver.Process(unexpectedVerificationFailureCommandEvent);
            receiver.Process(softDeleteEvent);
            receiver.Process(completedEvent);
            receiver.Process(rawdataEvent);
            receiver.Process(droppedEvent);

            int publishCount = 0;
            publisher.Setup(m => m.PublishWithSplitAsync(
                It.IsAny<IEnumerable<CommandLifecycleEvent>>(),
                It.IsAny<Func<IEnumerable<CommandLifecycleEvent>, CommandStatusBatchWorkItem>>(),
                It.IsAny<Func<int, TimeSpan>>()))
            .Callback<IEnumerable<CommandLifecycleEvent>, Func<IEnumerable<CommandLifecycleEvent>, CommandStatusBatchWorkItem>, Func<int, TimeSpan>>((a, b, c) =>
            {
                publishCount++;
                Assert.Equal(3, a.Count());
            })
            .Returns(Task.FromResult(true));

            await receiver.CheckpointAsync();
            Assert.Equal(1, publishCount);
        }

        /// <summary>
        /// Test event aggregation for Age Out command type
        /// </summary>
        [Fact]
        public async Task EventAggregationForAgeOut()
        {
            var commandId = this.ACommandId();
            var startedEvent = this.AStartedEvent(commandId: commandId).WithValue(ev => ev.CommandType, PrivacyCommandType.AgeOut).Build();
            var sentToAgentEvent = this.ASentToAgentEvent(commandId: commandId).WithValue(ev => ev.CommandType, PrivacyCommandType.AgeOut).Build();
            var pendingEvent = this.APendingEvent(commandId: commandId).WithValue(ev => ev.CommandType, PrivacyCommandType.AgeOut).Build();
            var failedEvent = this.AFailedEvent(commandId: commandId).WithValue(ev => ev.CommandType, PrivacyCommandType.AgeOut).Build();
            var unexpectedCommandEvent = this.AUnexpectedCommandEvent(commandId: commandId).WithValue(ev => ev.CommandType, PrivacyCommandType.AgeOut).Build();
            var verificationFailedEvent = this.AVerificationFailedEvent(commandId: commandId).WithValue(ev => ev.CommandType, PrivacyCommandType.AgeOut).Build();
            var unexpectedVerificationFailureCommandEvent = this.AUnexpectedVerificationFailureEvent(commandId: commandId).WithValue(ev => ev.CommandType, PrivacyCommandType.AgeOut).Build();
            var softDeleteEvent = this.ASoftDeletedEvent(commandId: commandId).WithValue(ev => ev.CommandType, PrivacyCommandType.AgeOut).Build();
            var completedEvent = this.ACompletedEvent(commandId: commandId).WithValue(ev => ev.CommandType, PrivacyCommandType.AgeOut).Build();
            var rawdataEvent = this.ARawDataEvent(commandId: commandId).WithValue(ev => ev.CommandType, PrivacyCommandType.AgeOut).Build();
            var droppedEvent = this.ADroppedEvent(commandId: commandId).WithValue(ev => ev.CommandType, PrivacyCommandType.AgeOut).Build();

            var publisher = new Mock<IAzureWorkItemQueuePublisher<CommandStatusBatchWorkItem>>();

            CommandHistoryAggregationReceiver receiver = new CommandHistoryAggregationReceiver(publisher.Object);

            receiver.Process(startedEvent);
            receiver.Process(sentToAgentEvent);
            receiver.Process(pendingEvent);
            receiver.Process(failedEvent);
            receiver.Process(unexpectedCommandEvent);
            receiver.Process(verificationFailedEvent);
            receiver.Process(unexpectedVerificationFailureCommandEvent);
            receiver.Process(softDeleteEvent);
            receiver.Process(completedEvent);
            receiver.Process(rawdataEvent);
            receiver.Process(droppedEvent);

            int publishCount = 0;
            publisher.Setup(m => m.PublishWithSplitAsync(
                It.IsAny<IEnumerable<CommandLifecycleEvent>>(),
                It.IsAny<Func<IEnumerable<CommandLifecycleEvent>, CommandStatusBatchWorkItem>>(),
                It.IsAny<Func<int, TimeSpan>>()))
            .Callback<IEnumerable<CommandLifecycleEvent>, Func<IEnumerable<CommandLifecycleEvent>, CommandStatusBatchWorkItem>, Func<int, TimeSpan>>((a, b, c) =>
            {
                publishCount++;
                Assert.Single(a);
            })
            .Returns(Task.FromResult(true));

            await receiver.CheckpointAsync();
            Assert.Equal(1, publishCount);
        }
    }
}

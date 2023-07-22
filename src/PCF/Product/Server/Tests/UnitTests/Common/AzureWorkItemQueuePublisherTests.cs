namespace PCF.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Storage.Queue;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks.CommandStatusAggregation;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Moq;
    using Xunit;

    [Trait("Category", "UnitTest")]
    public class AzureWorkItemQueuePublisherTests : INeedDataBuilders
    {
        [Fact]
        public async Task PublishWithRecursiveSplit()
        {
            List<CommandLifecycleEvent> events = new List<CommandLifecycleEvent>();

            var msaSubject = this.AnMsaSubject().Build();

            events.AddRange(Enumerable.Range(1, 10000).Select(x => this.AStartedEvent().Build()));

            var queueMock = this.AMockOf<IAzureCloudQueue>();

            var publisher = new AzureWorkItemQueue<CommandStatusBatchWorkItem>(new[] { queueMock.Object });
            
            List<TimeSpan> publishDelays = new List<TimeSpan>();
            List<CloudQueueMessage> publishedMessages = new List<CloudQueueMessage>();

            queueMock.Setup(m => m.AddMessageAsync(It.IsAny<CloudQueueMessage>(), It.IsAny<TimeSpan?>(), null, It.IsAny<CancellationToken>()))
                         .Callback<CloudQueueMessage, TimeSpan?, TimeSpan?, CancellationToken>((wi, ts, ttl, ct) =>
                         {
                             publishedMessages.Add(wi);
                             publishDelays.Add(ts ?? TimeSpan.Zero);
                         })
                         .Returns(Task.FromResult(true));

            var commandId = this.ACommandId();

            await publisher.PublishWithSplitAsync(
                events,
                splitEvents => new CommandStatusBatchWorkItem(commandId, null, splitEvents.ToArray()),
                x => TimeSpan.FromSeconds(x));

            publishDelays.Sort();

            // 10,000 events generally splits into ~32 groups. Let's make sure that there are at least 25.
            Assert.True(publishedMessages.Count > 25);

            for (int i = 1; i < publishDelays.Count; ++i)
            {
                // Make sure things are spaced apart.
                Assert.True(publishDelays[i - 1] + TimeSpan.FromSeconds(1) == publishDelays[i]);
            }
        }
    }
}

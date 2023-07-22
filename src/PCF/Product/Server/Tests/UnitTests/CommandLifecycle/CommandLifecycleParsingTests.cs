namespace PCF.UnitTests
{
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.EventHubs;
    using Microsoft.Azure.Storage.Queue;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Moq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using PCF.UnitTests.CommandLifecycle;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    [Trait("Category", "UnitTest")]
    public class CommandLifecycleParsingTests : BaseCommandLifeCycleTests, INeedDataBuilders
    {
        [Fact]
        public void EventDataConversionTests()
        {
            EventData data = new EventData(new byte[] { 1, 2, 3, 4, 5 })
            {
                Properties =
                {
                    ["foo"] = "bar",
                    ["bar"] = "baz",
                    ["baz"] = 6,
                }
            };

            JsonEventData jsonData = new JsonEventData(data);
            Assert.False(jsonData.Properties.ContainsKey("baz"));

            jsonData = JsonConvert.DeserializeObject<JsonEventData>(JsonConvert.SerializeObject(jsonData));

            EventData eventData = jsonData.ToEventData();

            Assert.Equal(data.Body.Array, eventData.Body.Array);
            Assert.Equal(data.Properties["foo"], eventData.Properties["foo"]);
            Assert.Equal(data.Properties["bar"], eventData.Properties["bar"]);

            Assert.False(eventData.Properties.ContainsKey("baz"));
        }

        [Fact]
        public void SingleEventTooLarge()
        {
            this.Initialize();
            byte[] randomData = new byte[1 * 1024 * 1024];
            new Random().NextBytes(randomData);

            var fakeThing = new
            {
                Bytes = randomData
            };

            List<(string, JObject)> bulkEvents = new List<(string, JObject)>();
            bulkEvents.Add(("RawCommand", JObject.FromObject(fakeThing)));

            Assert.Throws<InvalidOperationException>(() => CommandLifecycleEventParser.BuildBulkEventsRecursive(bulkEvents, 0, 1));
        }

        [Fact]
        public void RecursiveSplitWorks()
        {
            this.Initialize();
            List<(string, JObject)> bulkEvents = new List<(string, JObject)>();
            var random = new Random();

            for (int i = 0; i < 30; ++i)
            {
                byte[] randomData = new byte[30 * 1024];
                random.NextBytes(randomData);

                var fakeThing = new
                {
                    Bytes = randomData
                };

                bulkEvents.Add(("foobar", JObject.FromObject(fakeThing)));
            }

            var events = CommandLifecycleEventParser.BuildBulkEventsRecursive(bulkEvents, 0, bulkEvents.Count);
            Assert.Equal(2, events.Count()); // would require only 2 event as MaxPublishSizeBytes is 500k and events are compressed
            Assert.True(events.All(x => x.Data.Length < CommandLifecycleEventParser.MaxBulkMessageSize));
        }

        [Fact]
        public void UncompressedRecursiveSplitWorks()
        {
            this.Initialize();

            // Enable uncompression 
            using (new FlightEnabled(FeatureNames.PCF.PublishUncompressedMessage))
            {
                List<(string, JObject)> bulkEvents = new List<(string, JObject)>();
                var random = new Random();

                for (int i = 0; i < 30; ++i)
                {
                    byte[] randomData = new byte[30 * 1024];
                    random.NextBytes(randomData);

                    var fakeThing = new
                    {
                        Bytes = randomData
                    };

                    bulkEvents.Add(("foobar", JObject.FromObject(fakeThing)));
                }

                var events = CommandLifecycleEventParser.BuildBulkEventsRecursive(bulkEvents, 0, bulkEvents.Count);
                Assert.Equal(4, events.Count()); // would require only 4 event as MaxPublishSizeBytes is 500k and events are not compressed
                Assert.True(events.All(x => x.Data.Length < CommandLifecycleEventParser.MaxBulkMessageSize));
            }
        }

        [Fact]
        public async Task CanParseBigBulkEvent()
        {
            this.Initialize();

            var eventList = new List<CommandLifecycleEvent>();
            for (int i = 0; i < 10001; ++i)
            {
                eventList.Add(this.AStartedEvent());
            }

            int parsedCount = 0;
            int publishCount = 0;

            var bulkEvents = CommandLifecycleEventParser.Serialize(eventList);
            foreach (var bulkEvent in bulkEvents)
            {
                Assert.Equal(CommandLifecycleEventParser.BulkEventName, bulkEvent.Properties[CommandLifecycleEventParser.EventNameProperty]);
                Assert.True(bulkEvent.Data.Length <= CommandLifecycleEventParser.MaxBulkMessageSize);

                var parsedEvents = CommandLifecycleEventParser.ParseEvents(bulkEvent);
                parsedCount += parsedEvents.Count();
            }

            Assert.Equal(10001, parsedCount);

            // Ensure all messages fit in the queue.
            var azureMock = this.AMockOf<IAzureCloudQueue>();
            azureMock.Setup(m => m.AddMessageAsync(It.IsAny<CloudQueueMessage>(), It.IsAny<TimeSpan?>(), null, It.IsAny<CancellationToken>()))
                        .Callback<CloudQueueMessage, TimeSpan?, TimeSpan?, CancellationToken>((m, ts, ttl, ct) =>
                        {
                            publishCount++;
                            Assert.True(m.AsBytes.Length <= AzureWorkItemQueue<object>.MaxAzureQueueMessageSize);
                        })
                        .Returns(Task.FromResult(true));

            AzureWorkItemQueue<JsonEventData[]> queue = new AzureWorkItemQueue<JsonEventData[]>(new[] { azureMock.Object });

            await CommandLifecycleReceiverSink.PublishRetriesAsync(queue, bulkEvents);

            Assert.Equal(201, bulkEvents.Count());
        }

        [Fact]
        public async Task CanParseUncompressedBigBulkEvent()
        {
            this.Initialize();

            // Enable uncompression 
            using (new FlightEnabled(FeatureNames.PCF.PublishUncompressedMessage))
            {
                var eventList = new List<CommandLifecycleEvent>();
                for (int i = 0; i < 10001; ++i)
                {
                    eventList.Add(this.AStartedEvent());
                }

                int parsedCount = 0;
                int publishCount = 0;

                var bulkEvents = CommandLifecycleEventParser.Serialize(eventList);
                foreach (var bulkEvent in bulkEvents)
                {
                    Assert.Equal(CommandLifecycleEventParser.BulkEventName, bulkEvent.Properties[CommandLifecycleEventParser.EventNameProperty]);
                    Assert.True(bulkEvent.Data.Length <= CommandLifecycleEventParser.MaxBulkMessageSize);

                    var parsedEvents = CommandLifecycleEventParser.ParseEvents(bulkEvent);
                    parsedCount += parsedEvents.Count();
                }

                Assert.Equal(10001, parsedCount);

                // Ensure all messages fit in the queue.
                var azureMock = this.AMockOf<IAzureCloudQueue>();
                azureMock.Setup(m => m.AddMessageAsync(It.IsAny<CloudQueueMessage>(), It.IsAny<TimeSpan?>(), null, It.IsAny<CancellationToken>()))
                            .Callback<CloudQueueMessage, TimeSpan?, TimeSpan?, CancellationToken>((m, ts, ttl, ct) =>
                            {
                                publishCount++;
                                Assert.True(m.AsBytes.Length <= AzureWorkItemQueue<object>.MaxAzureQueueMessageSize);
                            })
                            .Returns(Task.FromResult(true));

                AzureWorkItemQueue<JsonEventData[]> queue = new AzureWorkItemQueue<JsonEventData[]>(new[] { azureMock.Object });

                await CommandLifecycleReceiverSink.PublishRetriesAsync(queue, bulkEvents);

                Assert.Equal(201, bulkEvents.Count());
            }
        }

        [Fact]
        public void CanParseCommandCompleted()
        {
            this.RunTest(this.ACompletedEvent().Build());
        }

        [Fact]
        public void CanParseCommandFailed()
        {
            this.RunTest(this.AFailedEvent().Build());
        }

        [Fact]
        public void CanParseCommandPending()
        {
            this.RunTest(this.APendingEvent().Build());
        }

        [Fact]
        public void CanParseCommandRawData()
        {
            this.RunTest(this.ARawDataEvent().Build());
        }

        [Fact]
        public void CanParseCommandDropped()
        {
            this.RunTest(this.ADroppedEvent().Build());
        }

        [Fact]
        public void CanParseCommandSentToAgent()
        {
            this.RunTest(this.ASentToAgentEvent().Build());
        }

        [Fact]
        public void CanParseCommandSoftDelete()
        {
            this.RunTest(this.ASoftDeletedEvent().Build());
        }

        [Fact]
        public void CanParseCommandStarted()
        {
            this.RunTest(this.AStartedEvent().Build());
        }

        [Fact]
        public void CanParseCommandUnexpected()
        {
            this.RunTest(this.AUnexpectedCommandEvent().Build());
        }

        [Fact]
        public void CanParseCommandUnexpectedVerficationFailure()
        {
            this.RunTest(this.AUnexpectedVerificationFailureEvent().Build());
        }

        [Fact]
        public void CanParseVerificationFailedEvent()
        {
            this.RunTest(this.AVerificationFailedEvent().Build());
        }

        private void RunTest<T>(T @event) where T : CommandLifecycleEvent
        {
            this.Initialize();
            IEnumerable<JsonEventData> items = CommandLifecycleEventParser.Serialize(new[] { @event });
            IEnumerable<CommandLifecycleEvent> parsedEvents = CommandLifecycleEventParser.ParseEvents(items.Single());

            var parsedEvent = parsedEvents.Single();

            // JSON.NET likes to use local time, so a direct string comparison won't work. However, we can set them both to the same before serializing.
            Assert.Equal(parsedEvent.Timestamp, @event.Timestamp);
            @event.Timestamp = DateTimeOffset.MinValue;
            parsedEvent.Timestamp = DateTimeOffset.MinValue;

            Assert.IsType<T>(parsedEvent);
            Assert.Equal(JsonConvert.SerializeObject(@event), JsonConvert.SerializeObject(parsedEvent));
        }
    }
}

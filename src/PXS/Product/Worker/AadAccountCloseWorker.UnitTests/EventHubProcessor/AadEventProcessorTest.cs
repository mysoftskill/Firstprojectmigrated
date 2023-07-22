// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.UnitTests.EventHubProcessor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Principal;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.EventHubProcessor;
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.QueueProcessor;
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.TableStorage;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.ServiceBus.Messaging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Newtonsoft.Json;

    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;


    /// <summary>
    ///     AadEventProcessorTest
    /// </summary>
    [TestClass]
    public class AadEventProcessorTest : SharedTestFunctions
    {
        private readonly ILogger logger = new ConsoleLogger();

        private readonly Mock<IClock> mockClock = CreateMockClock();

        private readonly Mock<ICounter> mockCounter = CreateMockCounter();

        private readonly Mock<ICounterFactory> mockCounterFactory = CreateMockCounterFactory();

        private readonly Mock<ITable<NotificationDeadLetterStorage>> mockDeadLetterTable = CreateMockTableNotificationDeadLetterStorage();

        private readonly Mock<IPartitionContext> mockPartitionContext = new Mock<IPartitionContext>(MockBehavior.Strict);

        private readonly Mock<IAccountCloseQueueManager> mockQueue = CreateMockAccountCloseQueueManager();

        private readonly Mock<IRequestClassifier> mockRequestClassifier = new Mock<IRequestClassifier>(MockBehavior.Strict);

        private readonly Mock<IAppConfiguration> mockAppConfiguration = new Mock<IAppConfiguration>(MockBehavior.Strict);

        private readonly string endpoint = "sb://endpoint";

        [TestInitialize]
        public void Init()
        {
            this.mockClock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);
            this.mockPartitionContext.Setup(c => c.Lease).Returns(new Lease());
            this.mockPartitionContext.Setup(c => c.CheckpointAsync()).Returns(Task.CompletedTask);
            this.mockCounterFactory.Setup(c => c.GetCounter(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CounterType>())).Returns(this.mockCounter.Object);
            this.mockQueue.Setup(c => c.EnqueueAsync(It.IsAny<IEnumerable<AccountCloseRequest>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            this.mockRequestClassifier.Setup(c => c.IsTestRequest(It.IsAny<string>(), It.IsAny<IIdentity>(), It.IsAny<string>())).Returns(false);
            this.mockDeadLetterTable.Setup(c => c.InsertAsync(It.IsAny<NotificationDeadLetterStorage>())).ReturnsAsync(true);
            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(It.IsAny<string>(), true)).ReturnsAsync(false);
            this.mockAppConfiguration.Setup(c => c.GetConfigValues<Guid>(It.IsAny<string>())).Returns((Guid[])null);
        }

        [DataTestMethod]
        [DataRow("I'm not a Guid", true)]
        [DataRow("8829C5DE-7E7F-4A9A-A95A-B510ED39ED35", false)]
        public void ShouldCheckFilterList(string filterValue, bool shouldThrow)
        {
            bool threw = false;
            try
            {
                var aadEventProcessor = new AadEventProcessor(
                    this.logger,
                    this.mockQueue.Object,
                    this.mockCounterFactory.Object,
                    this.mockClock.Object,
                    nameof(AadEventProcessorTest),
                    "gocloudgo",
                    new List<string> { filterValue },
                    this.mockRequestClassifier.Object,
                    this.mockDeadLetterTable.Object,
                    this.endpoint,
                    this.mockAppConfiguration.Object);
            }
            catch (ArgumentException)
            {
                threw = true;
            }

            Assert.AreEqual(shouldThrow, threw);
        }

        [TestMethod]
        public async Task ShouldCloseSuccess()
        {
            var eventProcessor = new AadEventProcessor(
                this.logger,
                this.mockQueue.Object,
                this.mockCounterFactory.Object,
                this.mockClock.Object,
                nameof(AadEventProcessorTest),
                "cloud!!!!!!",
                null,
                this.mockRequestClassifier.Object,
                this.mockDeadLetterTable.Object,
                this.endpoint,
                this.mockAppConfiguration.Object);

            await eventProcessor.CloseAsync(new PartitionContext(), CloseReason.LeaseLost).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ShouldDeadLetterEventsWithoutPreverifierToken()
        {
            var eventProcessor = new AadEventProcessor(
                this.logger,
                this.mockQueue.Object,
                this.mockCounterFactory.Object,
                this.mockClock.Object,
                nameof(AadEventProcessorTest),
                "cloudcloudcloud",
                null,
                this.mockRequestClassifier.Object,
                this.mockDeadLetterTable.Object,
                this.endpoint,
                this.mockAppConfiguration.Object);

            var notification = CreateNotification();
            notification.Token = null;

            List<EventData> events = CreateEventData(new List<Notification> { notification });

            // Act
            await eventProcessor.ProcessEventsAsync(this.mockPartitionContext.Object, events).ConfigureAwait(false);

            // Assert
            // Should have nothing in the batch when it doesn't have a preverifier token.
            this.mockQueue.Verify(c => c.EnqueueAsync(It.Is<IEnumerable<AccountCloseRequest>>(acr => acr.Count() == 0), It.IsAny<CancellationToken>()), Times.Once);
            this.mockCounterFactory.Verify(c => c.GetCounter(CounterCategoryNames.AadAccountClose, "DeadLetterCount", CounterType.Number), Times.Once);
            this.mockCounter.Verify(c => c.Increment(), Times.Once);

            // Should still checkpoint when it was written to dead letter storage successfully.
            this.mockPartitionContext.Verify(c => c.CheckpointAsync(), Times.Once);
            this.mockDeadLetterTable.Verify(c => c.InsertAsync(It.IsAny<NotificationDeadLetterStorage>()), Times.Once);
        }

        [DataTestMethod]
        [DataRow(false, 5, 0, 0)]
        [DataRow(true, 5, 0, 0)]
        [DataRow(true, 5, 0, 7)]
        [DataRow(true, 0, 7, 4)]
        [DataRow(true, 4, 7, 4)]
        public async Task ShouldFilterBasedOnTenantId(bool hasFilter, int nonFilteredEvents, int filter1Events, int filter2Events)
        {
            Guid filter1 = Guid.Parse("8829C5DE-7E7F-4A9A-A95A-B510ED39ED35");
            Guid filter2 = Guid.Parse("9829C5DE-7E7F-4A9A-A95A-B510ED39ED35");
            List<string> filterIds = hasFilter ? new List<string> { filter1.ToString().ToLower(), filter2.ToString() } : null;
            var eventProcessor = new AadEventProcessor(
                this.logger,
                this.mockQueue.Object,
                this.mockCounterFactory.Object,
                this.mockClock.Object,
                nameof(AadEventProcessorTest),
                "gocloudgo",
                filterIds,
                this.mockRequestClassifier.Object,
                this.mockDeadLetterTable.Object,
                this.endpoint,
                this.mockAppConfiguration.Object);

            var requests = new List<AccountCloseRequest>();
            this.mockQueue.Setup(c => c.EnqueueAsync(It.IsAny<IEnumerable<AccountCloseRequest>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask)
                .Callback<IEnumerable<AccountCloseRequest>, CancellationToken>((reqs, token) => requests.AddRange(reqs));

            ulong actualEventsRead = 0;

            var eventsReadCounter = new Mock<ICounter>();
            eventsReadCounter.Setup(c => c.IncrementBy(It.IsAny<ulong>())).Callback<ulong>(read => actualEventsRead += read);

            this.mockCounterFactory.Setup(cf => cf.GetCounter(CounterCategoryNames.AzureEventHub, "AadAccountCloseEventsRead", It.IsAny<CounterType>()))
                .Returns(eventsReadCounter.Object);

            var eventsFilteredCounter = new Mock<ICounter>(MockBehavior.Strict);

            ulong totalFiltered = 0;
            eventsFilteredCounter.Setup(c => c.IncrementBy(It.IsAny<ulong>())).Callback<ulong>(val => totalFiltered += val);

            ulong filtered1 = 0;
            eventsFilteredCounter.Setup(c => c.IncrementBy(It.IsAny<ulong>(), filter1.ToString())).Callback<ulong, string>((val, name) => filtered1 += val);

            ulong filtered2 = 0;
            eventsFilteredCounter.Setup(c => c.IncrementBy(It.IsAny<ulong>(), filter2.ToString())).Callback<ulong, string>((val, name) => filtered2 += val);

            this.mockCounterFactory.Setup(cf => cf.GetCounter(CounterCategoryNames.AzureEventHub, "AadAccountCloseTenantFilteredEvents", It.IsAny<CounterType>()))
                .Returns(eventsFilteredCounter.Object);

            await eventProcessor.ProcessEventsAsync(
                    this.mockPartitionContext.Object,
                    CreateEventData(nonFilteredEvents).Union(CreateEventData(filter1Events, 1, filter1)).Union(CreateEventData(1, filter2Events, filter2)))
                .ConfigureAwait(false);

            Assert.AreEqual(nonFilteredEvents, requests.Count);
            Assert.AreEqual(nonFilteredEvents + filter1Events + filter2Events, (int)actualEventsRead);

            Assert.AreEqual(filter1Events + filter2Events, (int)totalFiltered);

            Assert.AreEqual(filter1Events, (int)filtered1);
            Assert.AreEqual(filter2Events, (int)filtered2);
        }

        [TestMethod]
        public async Task ShouldLogErrorWhenFailToDeadLetterEventsWithoutPreverifierToken()
        {
            this.mockDeadLetterTable.Setup(c => c.InsertAsync(It.IsAny<NotificationDeadLetterStorage>())).ReturnsAsync(false);
            var eventProcessor = new AadEventProcessor(
                this.logger,
                this.mockQueue.Object,
                this.mockCounterFactory.Object,
                this.mockClock.Object,
                nameof(AadEventProcessorTest),
                "cloudcloudcloud",
                null,
                this.mockRequestClassifier.Object,
                this.mockDeadLetterTable.Object,
                this.endpoint,
                this.mockAppConfiguration.Object);

            var notification = CreateNotification();
            notification.Token = null;

            List<EventData> events = CreateEventData(new List<Notification> { notification });

            // Act
            await eventProcessor.ProcessEventsAsync(this.mockPartitionContext.Object, events).ConfigureAwait(false);

            // Assert
            // Nothing queued up when we cannot dead-letter.
            this.mockQueue.Verify(c => c.EnqueueAsync(It.IsAny<IEnumerable<AccountCloseRequest>>(), It.IsAny<CancellationToken>()), Times.Never);
            this.mockCounterFactory.Verify(c => c.GetCounter(CounterCategoryNames.AadAccountClose, "FailedToStoreDeadLetterCount", CounterType.Number), Times.Once);
            this.mockCounter.Verify(c => c.Increment(), Times.Once);

            // Checkpoint isn't called when we cannot write a failed dead letter to stroage.
            this.mockPartitionContext.Verify(c => c.CheckpointAsync(), Times.Never);
            this.mockDeadLetterTable.Verify(c => c.InsertAsync(It.IsAny<NotificationDeadLetterStorage>()), Times.Once);
        }

        [TestMethod]
        public async Task ShouldOpenSucces()
        {
            var eventProcessor = new AadEventProcessor(
                this.logger,
                this.mockQueue.Object,
                this.mockCounterFactory.Object,
                this.mockClock.Object,
                nameof(AadEventProcessorTest),
                "yaaacloud",
                null,
                this.mockRequestClassifier.Object,
                this.mockDeadLetterTable.Object,
                this.endpoint,
                this.mockAppConfiguration.Object);

            await eventProcessor.OpenAsync(new PartitionContext()).ConfigureAwait(false);
        }

        [TestMethod]
        [DataRow("3/9/2018 10:20:12 PM +00:00", "3/8/2018 10:20:12 PM +00:00", (ulong)24)] // test offset of event from current time 
        [DataRow("3/9/2018 10:20:12 PM +00:00", "3/9/2018 7:20:12 PM +00:00", (ulong)3)] // test offset of event from current time 
        [DataRow("3/9/2018 10:20:12 PM +00:00", "3/9/2018 10:20:12 PM +00:00", (ulong)0)] // test same time
        [DataRow("3/9/2018 10:20:12 PM +00:00", "4/9/2018 10:20:12 PM +00:00", (ulong)0)] // test time in the future (greater than current time)
        [DataRow("3/9/2018 10:20:12 PM +00:00", "1/9/2018 10:20:12 PM +00:00", (ulong)1416)] // test time exceeding a week
        [DataRow("3/9/2018 10:20:12 PM +00:00", "3/2/2018 10:20:13 PM +00:00", (ulong)167)] // test time 1 minute before 168 hours
        [DataRow("3/9/2018 10:20:12 PM +00:00", "3/2/2018 10:20:12 PM +00:00", (ulong)168)] // test time equal to a week (168 hours)
        [DataRow("3/9/2018 10:20:12 PM +00:00", "3/2/2018 10:20:11 PM +00:00", (ulong)168)] // test time exceeding a week
        public async Task ShouldProcessEventsAndCategorizePerfCounterTimeCorrectly(string currentTimeValue, string eventTimeValue, ulong hourDifference)
        {
            DateTimeOffset currentTime = DateTimeOffset.Parse(currentTimeValue);
            DateTimeOffset eventTime = DateTimeOffset.Parse(eventTimeValue);

            this.mockClock.Setup(c => c.UtcNow).Returns(currentTime);

            var eventProcessor = new AadEventProcessor(
                this.logger,
                this.mockQueue.Object,
                this.mockCounterFactory.Object,
                this.mockClock.Object,
                nameof(AadEventProcessorTest),
                "cloud123",
                null,
                this.mockRequestClassifier.Object,
                this.mockDeadLetterTable.Object,
                this.endpoint,
                this.mockAppConfiguration.Object);

            List<EventData> events = CreateEventAtTime(eventTime);

            var ageCounter = new Mock<ICounter>();
            this.mockCounterFactory.Setup(cf => cf.GetCounter(It.IsAny<string>(), "AadAccountCloseEventAge", It.IsAny<CounterType>())).Returns(ageCounter.Object);

            // Act
            await eventProcessor.ProcessEventsAsync(this.mockPartitionContext.Object, events).ConfigureAwait(false);

            // Assert
            this.mockQueue.Verify(c => c.EnqueueAsync(It.IsAny<IEnumerable<AccountCloseRequest>>(), It.IsAny<CancellationToken>()), Times.Once);
            ageCounter.Verify(c => c.SetValue(hourDifference), Times.Once);
            this.mockPartitionContext.Verify(c => c.CheckpointAsync(), Times.Once);
        }

        [TestMethod]
        public async Task ShouldProcessEventsSuccessWithMessagesToQueue()
        {
            var eventProcessor = new AadEventProcessor(
                this.logger,
                this.mockQueue.Object,
                this.mockCounterFactory.Object,
                this.mockClock.Object,
                nameof(AadEventProcessorTest),
                "cloudcloudcloud",
                null,
                this.mockRequestClassifier.Object,
                this.mockDeadLetterTable.Object,
                this.endpoint,
                this.mockAppConfiguration.Object);

            List<EventData> events = CreateEventData(1, 1);

            // Act
            await eventProcessor.ProcessEventsAsync(this.mockPartitionContext.Object, events).ConfigureAwait(false);

            // Assert
            this.mockQueue.Verify(c => c.EnqueueAsync(It.IsAny<IEnumerable<AccountCloseRequest>>(), It.IsAny<CancellationToken>()), Times.Once);
            this.mockCounter.Verify(c => c.SetValue(0), Times.Once);
            this.mockPartitionContext.Verify(c => c.CheckpointAsync(), Times.Once);
            this.mockDeadLetterTable.Verify(c => c.InsertAsync(It.IsAny<NotificationDeadLetterStorage>()), Times.Never);
        }

        [TestMethod]
        public async Task ShouldProcessEventsSuccessWithNothingToDo()
        {
            // Arrange
            var eventProcessor = new AadEventProcessor(
                this.logger,
                this.mockQueue.Object,
                this.mockCounterFactory.Object,
                this.mockClock.Object,
                nameof(AadEventProcessorTest),
                "gocloudgo",
                null,
                this.mockRequestClassifier.Object,
                this.mockDeadLetterTable.Object,
                this.endpoint,
                this.mockAppConfiguration.Object);

            // Act
            await eventProcessor.ProcessEventsAsync(this.mockPartitionContext.Object, Enumerable.Empty<EventData>()).ConfigureAwait(false);

            // Assert
            this.mockQueue.Verify(c => c.EnqueueAsync(It.IsAny<IEnumerable<AccountCloseRequest>>(), It.IsAny<CancellationToken>()), Times.Never);
            this.mockPartitionContext.Verify(c => c.CheckpointAsync(), Times.Once);
            this.mockDeadLetterTable.Verify(c => c.InsertAsync(It.IsAny<NotificationDeadLetterStorage>()), Times.Never);
        }

        [TestMethod]
        public async Task ShouldProcessEventsWithPreverifierTokenInTokenProperty()
        {
            var eventProcessor = new AadEventProcessor(
                this.logger,
                this.mockQueue.Object,
                this.mockCounterFactory.Object,
                this.mockClock.Object,
                nameof(AadEventProcessorTest),
                "cloudcloudcloud",
                null,
                this.mockRequestClassifier.Object,
                this.mockDeadLetterTable.Object,
                this.endpoint,
                this.mockAppConfiguration.Object);

            var notification = CreateNotification();
            notification.Token = Guid.NewGuid().ToString("N");
            List<EventData> events = CreateEventData(new List<Notification> { notification });

            // Act
            await eventProcessor.ProcessEventsAsync(this.mockPartitionContext.Object, events).ConfigureAwait(false);

            // Assert
            this.mockQueue.Verify(c => c.EnqueueAsync(It.IsAny<IEnumerable<AccountCloseRequest>>(), It.IsAny<CancellationToken>()), Times.Once);
            this.mockCounter.Verify(c => c.SetValue(0), Times.Once);
            this.mockPartitionContext.Verify(c => c.CheckpointAsync(), Times.Once);
            this.mockDeadLetterTable.Verify(c => c.InsertAsync(It.IsAny<NotificationDeadLetterStorage>()), Times.Never);
        }

        [TestMethod]
        public async Task ShouldProcessManyEventsSuccessWithMessagesToQueue()
        {
            const int BatchSize = 10;
            const int NotificationsPerBatch = 100;
            var eventProcessor = new AadEventProcessor(
                this.logger,
                this.mockQueue.Object,
                this.mockCounterFactory.Object,
                this.mockClock.Object,
                nameof(AadEventProcessorTest),
                "youlikecloudtoo",
                null,
                this.mockRequestClassifier.Object,
                this.mockDeadLetterTable.Object,
                this.endpoint,
                this.mockAppConfiguration.Object);

            List<EventData> events = CreateEventData(BatchSize, NotificationsPerBatch);

            // Act
            await eventProcessor.ProcessEventsAsync(this.mockPartitionContext.Object, events).ConfigureAwait(false);

            // Assert
            this.mockQueue.Verify(
                c => c.EnqueueAsync(It.Is<IEnumerable<AccountCloseRequest>>(w => w.Count() == BatchSize * NotificationsPerBatch), It.IsAny<CancellationToken>()),
                Times.Once);

            // Called an extra time for events filtered
            this.mockCounter.Verify(c => c.SetValue(0), Times.Exactly(BatchSize * NotificationsPerBatch));
            this.mockPartitionContext.Verify(c => c.CheckpointAsync(), Times.Once);
            this.mockDeadLetterTable.Verify(c => c.InsertAsync(It.IsAny<NotificationDeadLetterStorage>()), Times.Never);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task ProcessEventsAsyncFailWithNull()
        {
            try
            {
                var eventProcessor = new AadEventProcessor(
                    this.logger,
                    this.mockQueue.Object,
                    this.mockCounterFactory.Object,
                    this.mockClock.Object,
                    nameof(AadEventProcessorTest),
                    "youlikecloudtoo",
                    null,
                    this.mockRequestClassifier.Object,
                    this.mockDeadLetterTable.Object,
                    this.endpoint,
                    this.mockAppConfiguration.Object);

                await eventProcessor.ProcessEventsAsync(It.IsAny<PartitionContext>(), Enumerable.Empty<EventData>()).ConfigureAwait(false);
                Assert.Fail("shouldn't have executed this");
            }
            catch (ArgumentNullException exception)
            {
                string expectedMessage = "Value cannot be null.\r\nParameter name: context";
                Assert.AreEqual(expectedMessage, exception.Message);
                throw;
            }
        }

        [TestMethod]
        public async Task ProcessEventsAsyncSubjectType()
        {
            var requests = new List<AccountCloseRequest>();

            var notification1 = CreateNotification();
            var notification2 = CreateNotificationForResourceTenant();
            List<EventData> events = CreateEventData(new List<Notification> { notification1, notification2 });

            this.mockQueue.Setup(c => c.EnqueueAsync(It.IsAny<IEnumerable<AccountCloseRequest>>(), It.IsAny<CancellationToken>()))
                .Callback((IEnumerable<AccountCloseRequest> accountCloseRequests, CancellationToken cancellationToken) => 
                    { 
                        requests.AddRange(accountCloseRequests); 
                    })
                .Returns(Task.CompletedTask);

            var eventProcessor = new AadEventProcessor(
                this.logger,
                this.mockQueue.Object,
                this.mockCounterFactory.Object,
                this.mockClock.Object,
                nameof(AadEventProcessorTest),
                "dodo",
                null,
                this.mockRequestClassifier.Object,
                this.mockDeadLetterTable.Object,
                this.endpoint,
                this.mockAppConfiguration.Object);

            // Feature flag turned on
            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.MultiTenantCollaboration, true)).ReturnsAsync(true);
            await eventProcessor.ProcessEventsAsync(this.mockPartitionContext.Object, events).ConfigureAwait(false);

            Assert.AreEqual(2, requests.Count());

            // Should have AadSubject in the first request and AadSubject in the second
            Assert.IsInstanceOfType(requests[0].Subject, typeof(AadSubject));
            Assert.IsNotInstanceOfType(requests[0].Subject, typeof(AadSubject2));

            Assert.IsInstanceOfType(requests[1].Subject, typeof(AadSubject2));
        }

        [TestMethod]
        [DynamicData(nameof(CreateAadEventProcessorTestData), DynamicDataSourceType.Method)]
        [ExpectedException(typeof(ArgumentNullException))]
        public void HandleConstructorExceptionTest(ILogger logger,
            IAccountCloseQueueManager accountCloseQueueManager,
            ICounterFactory counterFactory,
            IClock clock,
            string hubId,
            IRequestClassifier requestClassifier,
            ITable<NotificationDeadLetterStorage> deadLetterTable,
            IAppConfiguration appConfiguration)
        {
            try
            {
                //Arrange
                const string cloudInstance = "cloudinstance";
                IList<string> tenantFilterList = new List<string>()
                {
                    Guid.NewGuid().ToString()
                };

                var aadEventProcessor = new AadEventProcessor(
                      logger,
                      accountCloseQueueManager,
                      counterFactory,
                      clock,
                      hubId,
                      cloudInstance,
                      tenantFilterList,
                      requestClassifier,
                      deadLetterTable,
                      this.endpoint,
                      appConfiguration);

                Assert.Fail("should have thrown exception");
            }
            catch (ArgumentNullException exception)
            {
                Assert.IsNotNull(exception.Message);
                throw;
            }
        }

        [TestMethod]
        public void CloseAndOpenAsyncNullHandling()
        {
            //Arrange
            Lease lease = new Lease
            {
                PartitionId = "PartitionId",
                Owner = "Microsoft"
            };

            PartitionContext partitionContext = null;

            void SetPartitionContext(bool withLease = false) => partitionContext = withLease ? new PartitionContext() { Lease = lease } : new PartitionContext();

            this.mockPartitionContext.Setup(l => l.Lease).Returns(lease);
            
            var eventProcessor = new AadEventProcessor(
                this.logger,
                this.mockQueue.Object,
                this.mockCounterFactory.Object,
                this.mockClock.Object,
                nameof(AadEventProcessorTest),
                "youlikecloudtoo",
                new List<string> { Guid.NewGuid().ToString() },
                this.mockRequestClassifier.Object,
                this.mockDeadLetterTable.Object,
                this.endpoint,
                this.mockAppConfiguration.Object);

            //Act
            var result0 = eventProcessor.CloseAsync(null, CloseReason.LeaseLost);
            var result4 = eventProcessor.OpenAsync(null);
            Assert.IsNotNull(result0);
            Assert.IsNotNull(result4);

            //Arrange
            SetPartitionContext();

            //Act
            var result1 = eventProcessor.CloseAsync(partitionContext, CloseReason.LeaseLost);
            var result5 = eventProcessor.OpenAsync(partitionContext);
            //Assert
            Assert.IsNotNull(result1);
            Assert.IsNotNull(result5);

            SetPartitionContext(true);
            var result2 = eventProcessor.CloseAsync(partitionContext, CloseReason.LeaseLost);
            var result6 = eventProcessor.OpenAsync(partitionContext);
            //Assert
            Assert.IsNotNull(result2);
            Assert.IsNotNull(result6);
        }

        private static IEnumerable<object[]> CreateAadEventProcessorTestData()
        {
            var mockLogger = CreateMockGenevaLogger();
            var mockAccountCloseQueueManager = CreateMockAccountCloseQueueManager();
            var mockCounterFactor = CreateMockCounterFactory();
            var mockClock = CreateMockClock();
            var hubId = "hubId";
            var mockRequestClassifier = CreateMockRequestClassifier();
            var mockNotificationDeadLetterStorage = CreateMockTableNotificationDeadLetterStorage();
            var mockAppConfiguration = new Mock<IAppConfiguration>();

            return new List<object[]>
            {
                new object[]
                {
                    null,
                    mockAccountCloseQueueManager.Object,
                    mockCounterFactor.Object,
                    mockClock.Object,
                    hubId,
                    mockRequestClassifier.Object,
                    mockNotificationDeadLetterStorage.Object,
                    mockAppConfiguration.Object
                },
                new object[]
                {
                    mockLogger.Object,
                    null,
                    mockCounterFactor.Object,
                    mockClock.Object,
                    hubId,
                    mockRequestClassifier.Object,
                    mockNotificationDeadLetterStorage.Object,
                    mockAppConfiguration.Object
                },
                new object[]
                {
                    mockLogger.Object,
                    mockAccountCloseQueueManager.Object,
                    null,
                    mockClock.Object,
                    hubId,
                    mockRequestClassifier.Object,
                    mockNotificationDeadLetterStorage.Object,
                    mockAppConfiguration.Object
                },
                new object[]
                {
                    mockLogger.Object,
                    mockAccountCloseQueueManager.Object,
                    mockCounterFactor.Object,
                    null,
                    hubId,
                    mockRequestClassifier.Object,
                    mockNotificationDeadLetterStorage.Object,
                    mockAppConfiguration.Object
                },
                new object[]
                {
                    mockLogger.Object,
                    mockAccountCloseQueueManager.Object,
                    mockCounterFactor.Object,
                    mockClock.Object,
                    null,
                    mockRequestClassifier.Object,
                    mockNotificationDeadLetterStorage.Object,
                    mockAppConfiguration.Object
                },
                new object[]
                {
                    mockLogger.Object,
                    mockAccountCloseQueueManager.Object,
                    mockCounterFactor.Object,
                    mockClock.Object,
                    hubId,
                    null,
                    mockNotificationDeadLetterStorage.Object,
                    mockAppConfiguration.Object
                },
                new object[]
                {
                    mockLogger.Object,
                    mockAccountCloseQueueManager.Object,
                    mockCounterFactor.Object,
                    mockClock.Object,
                    hubId,
                    mockRequestClassifier.Object,
                    null,
                    mockAppConfiguration.Object
                },
                new object[]
                {
                    mockLogger.Object,
                    mockAccountCloseQueueManager.Object,
                    mockCounterFactor.Object,
                    mockClock.Object,
                    hubId,
                    mockRequestClassifier.Object,
                    mockNotificationDeadLetterStorage.Object,
                    null
                },

            };
        }

        private static List<EventData> CreateEventAtTime(DateTimeOffset eventTime)
        {
            var events = new List<EventData>();
            Notification[] notifications =
            {
                new Notification
                {
                    ResourceData = new ResourceData { EventTime = eventTime },
                    Token = "i-am-a-jwt"
                }
            };
            var encoding = new UTF8Encoding();
            events.Add(new EventData(encoding.GetBytes(JsonConvert.SerializeObject(notifications))));
            return events;
        }

        private static List<EventData> CreateEventData(IEnumerable<Notification> notifications)
        {
            var events = new List<EventData>();
            var encoding = new UTF8Encoding();
            events.Add(new EventData(encoding.GetBytes(JsonConvert.SerializeObject(notifications))));
            return events;
        }

        private static List<EventData> CreateEventData(int numberEvents = 1, int numberNotificationsPerEvent = 1, Guid? tenantId = null)
        {
            var events = new List<EventData>();
            var notifications = new List<Notification>();
            for (int i = 0; i < numberEvents; i++)
            {
                for (int j = 0; j < numberNotificationsPerEvent; j++)
                {
                    notifications.Add(CreateNotification(tenantId));
                }
            }

            var encoding = new UTF8Encoding();
            events.Add(new EventData(encoding.GetBytes(JsonConvert.SerializeObject(notifications))));
            return events;
        }

        private static Notification CreateNotification(Guid? tenantId = null)
        {
            return new Notification
            {
                ResourceData = new ResourceData
                {
                    EventTime = DateTimeOffset.UtcNow,
                    TenantId = tenantId ?? Guid.NewGuid()
                },

                // Value is opaque to us. We simply pass thru our system.
                Token = Guid.NewGuid().ToString("N")
            };
        }

        private static Notification CreateNotificationForResourceTenant()
        {
            return new Notification
            {
                ResourceData = new ResourceData
                {
                    EventTime = DateTimeOffset.UtcNow,
                    TenantId = Guid.NewGuid(),
                    HomeTenantId = Guid.NewGuid()
                },

                // Value is opaque to us. We simply pass thru our system.
                Token = Guid.NewGuid().ToString("N")
            };
        }
    }
}

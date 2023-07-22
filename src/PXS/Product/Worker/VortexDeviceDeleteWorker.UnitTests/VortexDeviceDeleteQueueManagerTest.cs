// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.VortexDeviceDeleteWorker.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.Membership.MemberServices.Privacy.Core.Vortex;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Queue;
    using Microsoft.Azure.Storage.RetryPolicies;

    using Moq;

    using Newtonsoft.Json;

    using Microsoft.PrivacyServices.Common.Azure;
    

    /// <summary>
    ///     VortexDeviceDeleteQueueManagerTest
    /// </summary>
    [TestClass]
    public class VortexDeviceDeleteQueueManagerTest
    {
        private readonly ILogger logger = new ConsoleLogger();

        private readonly Mock<IVortexDeviceDeleteQueueProccessorConfiguration> mockConfig = new Mock<IVortexDeviceDeleteQueueProccessorConfiguration>();

        private readonly Mock<ICounterFactory> mockCounterFactory = new Mock<ICounterFactory>();

        private readonly IList<Mock<IAzureStorageProvider>> mockQueueStorageProviders = new List<Mock<IAzureStorageProvider>>();

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        public async Task EnqueueAsyncForMultipleDevicesWithCancellationAndTimeSpanSuccess(int time)
        {
            var manager = new VortexDeviceDeleteQueueManager(
                this.mockQueueStorageProviders.Select(c => c.Object).ToList(),
                this.logger,
                this.mockConfig.Object,
                new CustomOrderSelectionStrategyFactory<DeviceDeleteRequest>(CreateCustomGenerator()),
                this.mockCounterFactory.Object);
            try
            {
                var allDevices = new List<DeviceDeleteRequest>();
                for (int i = 0; i < 10; i++)
                {
                    allDevices.Add(new DeviceDeleteRequest());
                }

                await manager.EnqueueAsync(allDevices, TimeSpan.FromMinutes(time), new CancellationToken(true)).ConfigureAwait(false);
                Assert.Fail("Should have thrown");
            }
            catch (OperationCanceledException e)
            {
                Assert.AreEqual("The operation was canceled.", e.Message);
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public async Task EnqueueAsyncForMultipleDevicesWithCancellationSuccess()
        {
            var manager = new VortexDeviceDeleteQueueManager(
                this.mockQueueStorageProviders.Select(c => c.Object).ToList(),
                this.logger,
                this.mockConfig.Object,
                new CustomOrderSelectionStrategyFactory<DeviceDeleteRequest>(CreateCustomGenerator()),
                this.mockCounterFactory.Object);
            try
            {
                await manager.EnqueueAsync(new List<DeviceDeleteRequest> { new DeviceDeleteRequest() }, new CancellationToken(true)).ConfigureAwait(false);
                Assert.Fail("Should have thrown");
                Assert.IsNotNull(this.logger);
            }
            catch (OperationCanceledException e)
            {
                Assert.AreEqual("The operation was canceled.", e.Message);
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        [DataRow(1)]
        [DataRow(10)]
        [DataRow(20)]
        public async Task EnqueueAsyncForSingleDevicesWithCancellationAndTimeSpanSuccess(int time)
        {
            var manager = new VortexDeviceDeleteQueueManager(
                this.mockQueueStorageProviders.Select(c => c.Object).ToList(),
                this.logger,
                this.mockConfig.Object,
                new CustomOrderSelectionStrategyFactory<DeviceDeleteRequest>(CreateCustomGenerator()),
                this.mockCounterFactory.Object);
            try
            {
                await manager.EnqueueAsync(new DeviceDeleteRequest(), TimeSpan.FromSeconds(time), new CancellationToken(true)).ConfigureAwait(false);
                Assert.Fail("Should have thrown");
            }
            catch (OperationCanceledException e)
            {
                Assert.AreEqual("The operation was canceled.", e.Message);
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public async Task EnqueueAsyncForSingleDevicesWithCancellationSuccess()
        {
            var manager = new VortexDeviceDeleteQueueManager(
                this.mockQueueStorageProviders.Select(c => c.Object).ToList(),
                this.logger,
                this.mockConfig.Object,
                new CustomOrderSelectionStrategyFactory<DeviceDeleteRequest>(CreateCustomGenerator()),
                this.mockCounterFactory.Object);
            try
            {
                await manager.EnqueueAsync(new DeviceDeleteRequest(), new CancellationToken(true)).ConfigureAwait(false);
                Assert.Fail("Should have thrown");
            }
            catch (OperationCanceledException e)
            {
                Assert.AreEqual("The operation was canceled.", e.Message);
                throw;
            }
        }

        [DataTestMethod]
        [DataRow(1)]
        [DataRow(5)]
        [DataRow(50)]
        public async Task ShouldDequeueFromAllQueuesWithNoMessagesReturned(int numberOfQueues)
        {
            this.mockQueueStorageProviders.Clear();

            var mockQueues = new List<Mock<ICloudQueue>>();
            for (int i = 0; i < numberOfQueues; i++)
            {
                var mockQueueStorageProvider = new Mock<IAzureStorageProvider>(MockBehavior.Strict);
                mockQueueStorageProvider.Setup(c => c.QueueStorageUri).Returns(new StorageUri(new Uri("https://primary.com"), new Uri("https://secondardy.com")));

                var mockQueue = new Mock<ICloudQueue>(MockBehavior.Strict);
                mockQueue
                    .Setup(c => c.DequeueAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<IRetryPolicy>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<CloudQueueMessage>());
                mockQueues.Add(mockQueue);
                mockQueueStorageProvider.Setup(c => c.GetCloudQueueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(mockQueue.Object);
                this.mockQueueStorageProviders.Add(mockQueueStorageProvider);
            }

            var manager = new VortexDeviceDeleteQueueManager(
                this.mockQueueStorageProviders.Select(c => c.Object).ToList(),
                this.logger,
                this.mockConfig.Object,
                new CustomOrderSelectionStrategyFactory<DeviceDeleteRequest>(CreateCustomGenerator()),
                this.mockCounterFactory.Object);

            IList<IQueueItem<DeviceDeleteRequest>> result = await manager.GetMessagesAsync(1, CancellationToken.None).ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);

            foreach (Mock<ICloudQueue> mockQueue in mockQueues)
            {
                mockQueue.Verify(
                    c => c.DequeueAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<IRetryPolicy>(), It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        [TestMethod]
        public async Task ShouldDequeueSuccessFromSecondQueue()
        {
            for (int i = 0; i < 50; i++)
            {
                this.mockQueueStorageProviders.Clear();

                var mockQueues = new List<Mock<ICloudQueue>>();

                var mockQueueNoResults = new Mock<ICloudQueue>(MockBehavior.Strict);
                mockQueueNoResults
                    .Setup(c => c.DequeueAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<IRetryPolicy>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<CloudQueueMessage>());
                mockQueues.Add(mockQueueNoResults);

                var mockQueueContainsMessages = new Mock<ICloudQueue>(MockBehavior.Strict);
                mockQueueContainsMessages
                    .Setup(c => c.DequeueAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<IRetryPolicy>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<CloudQueueMessage> { new CloudQueueMessage(JsonConvert.SerializeObject(new DeviceDeleteRequest())) });
                mockQueues.Add(mockQueueContainsMessages);
                this.ConfigureQueueSizeMock(mockQueues);

                foreach (Mock<ICloudQueue> mockQueue in mockQueues)
                {
                    var mockQueueStorageProvider = new Mock<IAzureStorageProvider>(MockBehavior.Strict);
                    mockQueueStorageProvider.Setup(c => c.QueueStorageUri).Returns(new StorageUri(new Uri("https://primary.com"), new Uri("https://secondardy.com")));
                    mockQueueStorageProvider.Setup(c => c.GetCloudQueueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockQueue.Object);
                    this.mockQueueStorageProviders.Add(mockQueueStorageProvider);
                }

                var manager = new VortexDeviceDeleteQueueManager(
                    this.mockQueueStorageProviders.Select(c => c.Object).ToList(),
                    this.logger,
                    this.mockConfig.Object,
                    new CustomOrderSelectionStrategyFactory<DeviceDeleteRequest>(CreateCustomGenerator()),
                    this.mockCounterFactory.Object);

                // Act
                IList<IQueueItem<DeviceDeleteRequest>> result = await manager.GetMessagesAsync(1, CancellationToken.None).ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual(1, result.Count);

                // Due to round-robin strategy, the queue with no results might not be chosen, but it will at MOST once
                mockQueueNoResults.Verify(
                    c => c.DequeueAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<IRetryPolicy>(), It.IsAny<CancellationToken>()),
                    Times.AtMostOnce);

                // The queue with results must always be invoked
                mockQueueContainsMessages.Verify(
                    c => c.DequeueAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<IRetryPolicy>(), It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        [DataTestMethod]
        [DataRow(1, 1)]
        [DataRow(5, 5)]
        [DataRow(7, 10)]
        public async Task ShouldEnqueueException(int numQueues, int numTimes)
        {
            this.mockQueueStorageProviders.Clear();

            var mockQueues = new List<Mock<ICloudQueue>>();
            for (int i = 0; i < numQueues; i++)
            {
                var mockQueueStorageProvider = new Mock<IAzureStorageProvider>(MockBehavior.Strict);
                mockQueueStorageProvider.Setup(c => c.QueueStorageUri).Returns(new StorageUri(new Uri("https://primary.com"), new Uri("https://secondardy.com")));

                var mockQueue = new Mock<ICloudQueue>(MockBehavior.Strict);
                mockQueue
                    .Setup(c => c.EnqueueAsync(It.IsAny<CloudQueueMessage>(), It.IsInRange(TimeSpan.MinValue, TimeSpan.Zero - TimeSpan.FromTicks(1), Range.Inclusive), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                    .Throws(new YouAreMyException("test exception"));

                mockQueues.Add(mockQueue);

                mockQueueStorageProvider.Setup(c => c.GetCloudQueueAsync(nameof(DeviceDeleteRequest).ToLowerInvariant(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(mockQueue.Object);
                this.mockQueueStorageProviders.Add(mockQueueStorageProvider);
            }

            var manager = new VortexDeviceDeleteQueueManager(
                this.mockQueueStorageProviders.Select(c => c.Object).ToList(),
                this.logger,
                this.mockConfig.Object,
                new CustomOrderSelectionStrategyFactory<DeviceDeleteRequest>(CreateCustomGenerator()),
                this.mockCounterFactory.Object);

            for (int x = 0; x < numTimes; ++x)
            {
                try
                {
                    await manager.EnqueueAsync(new DeviceDeleteRequest(), TimeSpan.FromHours(1), CancellationToken.None).ConfigureAwait(false);
                }
                catch (YouAreMyException)
                {
                }
            }

            foreach (Mock<ICloudQueue> mockQueue in mockQueues)
            {
                mockQueue.Verify(c => c.EnqueueAsync(It.IsAny<CloudQueueMessage>(), It.IsInRange(TimeSpan.MinValue, TimeSpan.Zero - TimeSpan.FromTicks(1), Range.Inclusive), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Exactly(numTimes));
            }
        }

        [DataTestMethod]
        [DataRow(1)]
        [DataRow(5)]
        [DataRow(10)]
        public async Task ShouldEnqueueRetry(int numTimes)
        {
            this.mockQueueStorageProviders.Clear();

            var failureQueue = new Mock<ICloudQueue>(MockBehavior.Strict);
            {
                var failureProvider = new Mock<IAzureStorageProvider>(MockBehavior.Strict);
                failureProvider.Setup(c => c.QueueStorageUri).Returns(new StorageUri(new Uri("https://primary.com"), new Uri("https://secondardy.com")));
                failureQueue
                    .Setup(c => c.EnqueueAsync(It.IsAny<CloudQueueMessage>(), null, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                    .Throws(new Exception("test exception"));
                failureProvider.Setup(c => c.GetCloudQueueAsync(nameof(DeviceDeleteRequest).ToLowerInvariant(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(failureQueue.Object);
                this.mockQueueStorageProviders.Add(failureProvider);
            }

            var successQueue = new Mock<ICloudQueue>(MockBehavior.Strict);
            {
                var successProvider = new Mock<IAzureStorageProvider>(MockBehavior.Strict);
                successProvider.Setup(c => c.QueueStorageUri).Returns(new StorageUri(new Uri("https://primary.com"), new Uri("https://secondardy.com")));
                successQueue
                    .Setup(c => c.EnqueueAsync(It.IsAny<CloudQueueMessage>(), It.IsInRange(TimeSpan.MinValue, TimeSpan.Zero - TimeSpan.FromTicks(1), Range.Inclusive), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
                successProvider.Setup(c => c.GetCloudQueueAsync(nameof(DeviceDeleteRequest).ToLowerInvariant(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(successQueue.Object);
                this.mockQueueStorageProviders.Add(successProvider);
            }

            var manager = new VortexDeviceDeleteQueueManager(
                this.mockQueueStorageProviders.Select(c => c.Object).ToList(),
                this.logger,
                this.mockConfig.Object,
                new CustomOrderSelectionStrategyFactory<DeviceDeleteRequest>(CreateCustomGenerator()),
                this.mockCounterFactory.Object);

            for (int x = 0; x < numTimes; ++x)
            {
                await manager.EnqueueAsync(new DeviceDeleteRequest(), TimeSpan.FromHours(1), CancellationToken.None).ConfigureAwait(false);
            }

            failureQueue.Verify(c => c.EnqueueAsync(It.IsAny<CloudQueueMessage>(), It.IsInRange(TimeSpan.MinValue, TimeSpan.Zero - TimeSpan.FromTicks(1), Range.Inclusive), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Exactly(numTimes));
        }

        [DataTestMethod]
        [DataRow(1)]
        [DataRow(5)]
        [DataRow(10)]
        [DataRow(100)]
        public async Task ShouldEnqueueSuccess(int numTimes)
        {
            this.mockQueueStorageProviders.Clear();

            int queuedTimes = 0;

            var primaryQueue = new Mock<ICloudQueue>(MockBehavior.Strict);
            {
                var failureProvider = new Mock<IAzureStorageProvider>(MockBehavior.Strict);
                failureProvider.Setup(c => c.QueueStorageUri).Returns(new StorageUri(new Uri("https://primary.com"), new Uri("https://secondardy.com")));
                primaryQueue
                    .Setup(c => c.EnqueueAsync(It.IsAny<CloudQueueMessage>(), It.IsInRange(TimeSpan.MinValue, TimeSpan.Zero - TimeSpan.FromTicks(1), Range.Inclusive), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                    .Callback(() => ++queuedTimes)
                    .Returns(Task.CompletedTask);
                failureProvider.Setup(c => c.GetCloudQueueAsync(nameof(DeviceDeleteRequest).ToLowerInvariant(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(primaryQueue.Object);
                this.mockQueueStorageProviders.Add(failureProvider);
            }

            var secondaryQueue = new Mock<ICloudQueue>(MockBehavior.Strict);
            {
                var successProvider = new Mock<IAzureStorageProvider>(MockBehavior.Strict);
                successProvider.Setup(c => c.QueueStorageUri).Returns(new StorageUri(new Uri("https://primary.com"), new Uri("https://secondardy.com")));
                secondaryQueue
                    .Setup(c => c.EnqueueAsync(It.IsAny<CloudQueueMessage>(), It.IsInRange(TimeSpan.MinValue, TimeSpan.Zero - TimeSpan.FromTicks(1), Range.Inclusive), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                    .Callback(() => ++queuedTimes)
                    .Returns(Task.CompletedTask);
                successProvider.Setup(c => c.GetCloudQueueAsync(nameof(DeviceDeleteRequest).ToLowerInvariant(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(secondaryQueue.Object);
                this.mockQueueStorageProviders.Add(successProvider);
            }

            var manager = new VortexDeviceDeleteQueueManager(
                this.mockQueueStorageProviders.Select(c => c.Object).ToList(),
                this.logger,
                this.mockConfig.Object,
                new CustomOrderSelectionStrategyFactory<DeviceDeleteRequest>(CreateCustomGenerator()),
                this.mockCounterFactory.Object);

            for (int x = 0; x < numTimes; ++x)
            {
                await manager.EnqueueAsync(new DeviceDeleteRequest(), TimeSpan.FromHours(1), CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(numTimes, queuedTimes);
        }

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public async Task ShouldThrowIfCanceled()
        {
            var manager = new VortexDeviceDeleteQueueManager(
                this.mockQueueStorageProviders.Select(c => c.Object).ToList(),
                this.logger,
                this.mockConfig.Object,
                new CustomOrderSelectionStrategyFactory<DeviceDeleteRequest>(CreateCustomGenerator()),
                this.mockCounterFactory.Object);
            try
            {
                IList<IQueueItem<DeviceDeleteRequest>> result = await manager.GetMessagesAsync(1, new CancellationToken(true)).ConfigureAwait(false);
                Assert.Fail("Should have thrown");
            }
            catch (OperationCanceledException e)
            {
                Assert.AreEqual("The operation was canceled.", e.Message);
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldThrowIfNoStorageProvidersSpecified()
        {
            try
            {
                var manager = new VortexDeviceDeleteQueueManager(
                    new List<IAzureStorageProvider>(),
                    this.logger,
                    this.mockConfig.Object,
                    new CustomOrderSelectionStrategyFactory<DeviceDeleteRequest>(CreateCustomGenerator()),
                    this.mockCounterFactory.Object);
                Assert.Fail("Should have thrown");
            }
            catch (ArgumentOutOfRangeException e)
            {
                StringAssert.StartsWith(
                    e.Message,
                    "Must initialize with at least 1 queue. There were 0." + Environment.NewLine + "Parameter name: queueStorageProviders");
                throw;
            }
        }

        [TestInitialize]
        public void TestInit()
        {
            var mockServicePointConfig = new Mock<IServicePointConfiguration>(MockBehavior.Loose);
            mockServicePointConfig.Setup(c => c.ConnectionLimit).Returns(50);
            mockServicePointConfig.Setup(c => c.ConnectionLeaseTimeout).Returns(1000);
            mockServicePointConfig.Setup(c => c.UseNagleAlgorithm).Returns(false);
            mockServicePointConfig.Setup(c => c.MaxIdleTime).Returns(42);
            this.mockConfig.Setup(c => c.ServicePointConfiguration).Returns(mockServicePointConfig.Object);

            var mockQueueStorageProvider = new Mock<IAzureStorageProvider>(MockBehavior.Strict);
            mockQueueStorageProvider.Setup(c => c.QueueStorageUri).Returns(new StorageUri(new Uri("https://primary.com"), new Uri("https://secondardy.com")));
            var mockQueue = new Mock<ICloudQueue>(MockBehavior.Strict);
            mockQueueStorageProvider.Setup(c => c.GetCloudQueueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockQueue.Object);
            this.mockQueueStorageProviders.Add(mockQueueStorageProvider);

            var mockCounter = new Mock<ICounter>(MockBehavior.Strict);
            mockCounter.Setup(c => c.SetValue(It.IsAny<ulong>(), It.IsAny<string>()));
            this.mockCounterFactory.Setup(c => c.GetCounter(CounterCategoryNames.AzureQueue, It.IsAny<string>(), It.IsAny<CounterType>())).Returns(mockCounter.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void VortexDeviceDeleteQueueManagerExcHandlingWhenAzureStorageProviderIsNull()
        {
            try
            {
                var manager = new VortexDeviceDeleteQueueManager(
                    null,
                    this.logger,
                    this.mockConfig.Object,
                    new CustomOrderSelectionStrategyFactory<DeviceDeleteRequest>(CreateCustomGenerator()),
                    this.mockCounterFactory.Object);
            }
            catch (ArgumentNullException e)
            {
                Assert.AreEqual("Value cannot be null.\r\nParameter name: queueStorageProviders", e.Message);
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void VortexDeviceDeleteQueueManagerExHandlingWhenICounterFactoryNull()
        {
            try
            {
                var manager = new VortexDeviceDeleteQueueManager(
                    this.mockQueueStorageProviders.Select(c => c.Object).ToList(),
                    this.logger,
                    null,
                    new CustomOrderSelectionStrategyFactory<DeviceDeleteRequest>(CreateCustomGenerator()),
                    this.mockCounterFactory.Object);
            }
            catch (ArgumentNullException e)
            {
                Assert.AreEqual("Value cannot be null.\r\nParameter name: config", e.Message);
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void VortexDeviceDeleteQueueManagerExHandlingWhenVortexDeviceDeleteQueueProccessorConfigurationIsNull()
        {
            try
            {
                var manager = new VortexDeviceDeleteQueueManager(
                    this.mockQueueStorageProviders.Select(c => c.Object).ToList(),
                    this.logger,
                    this.mockConfig.Object,
                    new CustomOrderSelectionStrategyFactory<DeviceDeleteRequest>(CreateCustomGenerator()),
                    null);
            }
            catch (ArgumentNullException e)
            {
                Assert.AreEqual("Value cannot be null.\r\nParameter name: counterFactory", e.Message);
                throw;
            }
        }

        private void ConfigureQueueSizeMock(List<Mock<ICloudQueue>> mockQueues)
        {
            foreach (Mock<ICloudQueue> mockQueue in mockQueues)
            {
                mockQueue.Setup(c => c.GetQueueSizeAsync()).ReturnsAsync(42);
            }
        }

        private class YouAreMyException : Exception
        {
            public YouAreMyException(string message)
                : base(message)
            {
            }
        }

        private static CustomOrderSelectionStrategy<DeviceDeleteRequest>.IndexSelector CreateCustomGenerator(int start = 0)
        {
            return queues => start++ % queues.Count;
        }
    }
}

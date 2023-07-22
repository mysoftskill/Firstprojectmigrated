// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.UnitTests.QueueProcessor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.QueueProcessor;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Queue;
    using Microsoft.Azure.Storage.RetryPolicies;

    using Moq;

    using Newtonsoft.Json;

    using Microsoft.PrivacyServices.Common.Azure;
    

    /// <summary>
    ///     AadAccountCloseQueueManagerTest
    /// </summary>
    [TestClass]
    public class AadAccountCloseQueueManagerTest
    {
        private readonly ILogger logger = new ConsoleLogger();

        private readonly Mock<IAadAccountCloseQueueProccessorConfiguration> mockConfig = CreateMockAadAccountCloseQueueProcessorConfig();

        private readonly Mock<ICounterFactory> mockCounterFactory = CreateMockCounterFactory();

        private readonly IList<Mock<IAzureStorageProvider>> mockQueueStorageProviders = CreateListOfMockAzureStorageProvider();

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
            mockQueue.Setup(c => c.Name).Returns(nameof(AccountCloseRequest).ToLowerInvariant());
            mockQueue.Setup(c => c.EnqueueAsync(It.IsAny<CloudQueueMessage>())).Returns(Task.CompletedTask);
            mockQueueStorageProvider.Setup(c => c.GetCloudQueueAsync(nameof(AccountCloseRequest).ToLowerInvariant(), It.IsAny<CancellationToken>())).ReturnsAsync(mockQueue.Object);
            this.mockQueueStorageProviders.Add(mockQueueStorageProvider);

            var mockCounter = new Mock<ICounter>(MockBehavior.Strict);
            mockCounter.Setup(c => c.SetValue(It.IsAny<ulong>(), It.IsAny<string>()));
            this.mockCounterFactory.Setup(c => c.GetCounter(CounterCategoryNames.AzureQueue, It.IsAny<string>(), It.IsAny<CounterType>())).Returns(mockCounter.Object);
        }

        [TestMethod, ExpectedException(typeof(OperationCanceledException))]
        public async Task ShouldThrowIfCanceled()
        {
            var manager = new AadAccountCloseQueueManager(this.mockQueueStorageProviders.Select(c => c.Object).ToList(), this.logger, this.mockConfig.Object, this.mockCounterFactory.Object);
            try
            {
                var result = await manager.GetMessagesAsync(1, new CancellationToken(canceled: true)).ConfigureAwait(false);
                Assert.Fail("Should have thrown");
            }
            catch (OperationCanceledException e)
            {
                Assert.AreEqual("The operation was canceled.", e.Message);
                throw;
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
                    .ReturnsAsync(new List<CloudQueueMessage> { new CloudQueueMessage(JsonConvert.SerializeObject(new AccountCloseRequest())) });
                mockQueues.Add(mockQueueContainsMessages);
                this.ConfigureQueueSizeMock(mockQueues);

                foreach (Mock<ICloudQueue> mockQueue in mockQueues)
                {
                    var mockQueueStorageProvider = new Mock<IAzureStorageProvider>(MockBehavior.Strict);
                    mockQueueStorageProvider.Setup(c => c.QueueStorageUri).Returns(new StorageUri(new Uri("https://primary.com"), new Uri("https://secondardy.com")));
                    mockQueueStorageProvider.Setup(c => c.GetCloudQueueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockQueue.Object);
                    this.mockQueueStorageProviders.Add(mockQueueStorageProvider);
                }

                var manager = new AadAccountCloseQueueManager(this.mockQueueStorageProviders.Select(c => c.Object).ToList(), this.logger, this.mockConfig.Object, this.mockCounterFactory.Object);

                // Act
                var result = await manager.GetMessagesAsync(1, CancellationToken.None).ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual(1, result.Count);

                // Due to round-robin strategy, the queue with no results might not be chosen, but it will at MOST once
                mockQueueNoResults.Verify(c => c.DequeueAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<IRetryPolicy>(), It.IsAny<CancellationToken>()), Times.AtMostOnce);

                // The queue with results must always be invoked
                mockQueueContainsMessages.Verify(c => c.DequeueAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<IRetryPolicy>(), It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        private void ConfigureQueueSizeMock(List<Mock<ICloudQueue>> mockQueues)
        {
            foreach (Mock<ICloudQueue> mockQueue in mockQueues)
            {
                mockQueue.Setup(c => c.GetQueueSizeAsync()).ReturnsAsync(42);
            }
        }

        [TestMethod]
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
                mockQueueStorageProvider.Setup(c => c.GetCloudQueueAsync(nameof(AccountCloseRequest).ToLowerInvariant(), It.IsAny<CancellationToken>())).ReturnsAsync(mockQueue.Object);
                this.mockQueueStorageProviders.Add(mockQueueStorageProvider);
            }

            var manager = new AadAccountCloseQueueManager(this.mockQueueStorageProviders.Select(c => c.Object).ToList(), this.logger, this.mockConfig.Object, this.mockCounterFactory.Object);

            var result = await manager.GetMessagesAsync(1, CancellationToken.None).ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);

            foreach (var mockQueue in mockQueues)
            {
                mockQueue.Verify(c => c.DequeueAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<IRetryPolicy>(), It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        [TestMethod, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldThrowIfNoStorageProvidersSpecified()
        {
            try
            {
                var manager = new AadAccountCloseQueueManager(new List<IAzureStorageProvider>(), this.logger, this.mockConfig.Object, this.mockCounterFactory.Object);
                Assert.Fail("Should have thrown");
            }
            catch (ArgumentOutOfRangeException e)
            {
                StringAssert.StartsWith(
                    value: e.Message,
                    substring: "Must initialize with at least 1 queue. There were 0." + Environment.NewLine + "Parameter name: queueStorageProviders");
                throw;
            }
        }

        [TestMethod]
        public async Task ShouldEnqueueSuccessWithMaxTtl()
        {
            this.mockQueueStorageProviders.Clear();

            var mockQueue = new Mock<ICloudQueue>(MockBehavior.Strict);
            mockQueue
                .Setup(c => c.EnqueueAsync(It.IsAny<CloudQueueMessage>(), TimeSpan.MaxValue, TimeSpan.Zero, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var mockQueueStorageProvider = new Mock<IAzureStorageProvider>(MockBehavior.Strict);
            mockQueueStorageProvider.Setup(c => c.QueueStorageUri).Returns(new StorageUri(new Uri("https://primary.com"), new Uri("https://secondardy.com")));
            mockQueueStorageProvider.Setup(c => c.GetCloudQueueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockQueue.Object);
            this.mockQueueStorageProviders.Add(mockQueueStorageProvider);

            var manager = new AadAccountCloseQueueManager(this.mockQueueStorageProviders.Select(c => c.Object).ToList(), this.logger, this.mockConfig.Object, this.mockCounterFactory.Object);

            await manager.EnqueueAsync(new List<AccountCloseRequest> { new AccountCloseRequest() }, CancellationToken.None).ConfigureAwait(false);

            mockQueue
                .Verify(
                    c => c.EnqueueAsync(It.IsAny<CloudQueueMessage>(), TimeSpan.MaxValue, TimeSpan.Zero, It.IsAny<CancellationToken>()),
                    Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        [DynamicData(nameof(CreateAadAccountCloseQueueManagerTestData),DynamicDataSourceType.Method)]
        public void CreateNewManager(IList<IAzureStorageProvider> queueStorageProviders, IAadAccountCloseQueueProccessorConfiguration config, ICounterFactory counterFactory)
        {
            new AadAccountCloseQueueManager(
                queueStorageProviders,
                this.logger,
                config,
                counterFactory);
        }

        protected static Mock<List<IAzureStorageProvider>> CreateMockAzureStorageProvider()
        {
            return new Mock<List<IAzureStorageProvider>>();
        }
        protected static IList<Mock<IAzureStorageProvider>> CreateListOfMockAzureStorageProvider()
        {
            return new List<Mock<IAzureStorageProvider>>();
        }
        protected static Mock<ICounterFactory> CreateMockCounterFactory()
        {
            return new Mock<ICounterFactory>();
        }

        protected static Mock<IAadAccountCloseQueueProccessorConfiguration> CreateMockAadAccountCloseQueueProcessorConfig()
        {
            return new Mock<IAadAccountCloseQueueProccessorConfiguration>();
        }


        private static IEnumerable<object[]> CreateAadAccountCloseQueueManagerTestData()
        {
            var logger = new Mock<ILogger>();
            var counterFactory = CreateMockCounterFactory();
            var azureStorageProvider = CreateMockAzureStorageProvider();
            var aadAccountCloseConfig = CreateMockAadAccountCloseQueueProcessorConfig();

            var data = new List<object[]>() {
                     new object[]
                     {
                         null,
                         aadAccountCloseConfig.Object,
                         counterFactory.Object
                     },
                new object[]
                {
                    azureStorageProvider.Object,
                     null,
                    counterFactory.Object
                },
                new object[]
                {
                    azureStorageProvider.Object,
                    aadAccountCloseConfig.Object,
                    null
                },

            };

            return data;
        }
    }
}

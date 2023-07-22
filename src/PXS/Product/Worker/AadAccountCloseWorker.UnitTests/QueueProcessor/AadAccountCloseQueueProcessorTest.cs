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

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.QueueProcessor;
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.TableStorage;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAccountClose;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    /// <summary>
    /// </summary>
    [TestClass]
    public class AadAccountCloseQueueProcessorTest
    {
        private const int MaxDequeueCountBeforeRequeue = 10;

        private const int MaxDequeueCountForConflicts = 30;

        private const int MaxDequeueCountToDeadLetter = 10;

        private readonly ILogger logger = new ConsoleLogger();

        private readonly Random random = new Random(5);

        private Mock<ICounter> aadRVSConcurrencyConflicts;

        private Mock<ICounter> aadRVSTooManyRequests;

        private Mock<IAadAccountCloseQueueProccessorConfiguration> configuration;

        private Mock<IAadAccountCloseService> mockAadAccountCloseService;

        private Mock<ICounterFactory> mockCounterFactory;

        private Mock<ICounter> mockDeadLetterCounter;

        private Mock<ITable<AccountCloseDeadLetterStorage>> mockDeadLetterTable;

        private Mock<ICounter> mockItemAgeCounter;

        private Mock<IAccountCloseQueueManager> mockQueue;

        private List<Mock<IQueueItem<AccountCloseRequest>>> mockQueueItems;

        private List<ServiceResponse<IQueueItem<AccountCloseRequest>>> serviceResponses;

        private Mock<IAppConfiguration> mockAppConfiguration;

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        [DynamicData(nameof(AadAccountCloseQueueProcessorConstructorTestData), DynamicDataSourceType.Method)]
        public void AadAccountCloseQueueProcessorNullHandlingSuccess(
            ILogger logger,
            IAadAccountCloseQueueProccessorConfiguration configuration,
            IAccountCloseQueueManager queueManager,
            IAadAccountCloseService aadAccountCloseService,
            ITable<AccountCloseDeadLetterStorage> deadLetterTable,
            ICounterFactory counterFactory,
            IAppConfiguration appConfiguration)
        {
            new AadAccountCloseQueueProcessor(logger, configuration, queueManager, aadAccountCloseService, deadLetterTable, counterFactory, appConfiguration);
        }

        [TestMethod]
        public async Task ShouldFilterSingalExpectedToBeDropped()
        {
            var objectId = Guid.NewGuid();

            var mockQueueItem = new Mock<IQueueItem<AccountCloseRequest>>(MockBehavior.Strict);
            mockQueueItem.Setup(c => c.CompleteAsync()).Returns(Task.CompletedTask);
            mockQueueItem.Setup(c => c.RenewLeaseAsync(It.IsAny<TimeSpan>())).ReturnsAsync(true);
            mockQueueItem.Setup(c => c.InsertionTime).Returns(DateTimeOffset.UtcNow.AddHours(-this.random.Next(7 * 24)));

            mockQueueItem.Setup(c => c.Data).Returns(
                new AccountCloseRequest
                {
                    RequestId = Guid.NewGuid(),
                    Subject = new AadSubject { ObjectId = objectId }
                });
 
            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.PXS.DropAccountCloseSignalForUser, It.Is<ICustomOperatorContext>((value) => ((string)value.Value) == objectId.ToString()), true)).ReturnsAsync(true);

            this.mockQueueItems.Add(mockQueueItem);

            var accountCloseItems = this.mockQueueItems.Select(c => c.Object).ToList();
            var originalLength = accountCloseItems.Count;

            this.mockQueue.Setup(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(accountCloseItems);

            var queueProcessor = new AadAccountCloseQueueProcessor(
                   this.logger,
                   this.configuration.Object,
                   this.mockQueue.Object,
                   this.mockAadAccountCloseService.Object,
                   this.mockDeadLetterTable.Object,
                   this.mockCounterFactory.Object,
                   this.mockAppConfiguration.Object);

            this.mockAppConfiguration.Setup(c => c.GetConfigValue(ConfigNames.PXS.AadAccountCloseWorker_EnableDequeuing, It.IsAny<bool>())).Returns(true);

            Assert.IsTrue(await queueProcessor.DoWorkAsync().ConfigureAwait(false));

            // At this point the items added should have been removed.
            Assert.IsTrue(!accountCloseItems.Contains(mockQueueItem.Object));

            // confirm only 1 item is removed.
            Assert.IsTrue(accountCloseItems.Count == originalLength - 1);
        }

        [TestMethod]
        public async Task ShouldDeadLetterAfterExceedingMaxDequeueCountToDeadLetter()
        {
            this.mockAppConfiguration.Setup(c => c.GetConfigValue(ConfigNames.PXS.AadAccountCloseWorker_EnableDequeuing, It.IsAny<bool>())).Returns(true);
            this.mockAppConfiguration.Setup(c => c.GetConfigValue(ConfigNames.PXS.AadAccountCloseWorker_EnableProcessing, It.IsAny<bool>())).Returns(true);

            foreach (ServiceResponse<IQueueItem<AccountCloseRequest>> serviceResponse in this.serviceResponses)
            {
                serviceResponse.Error = new Error(ErrorCode.Unknown, "Something bad");
            }

            foreach (Mock<IQueueItem<AccountCloseRequest>> mockQueueItem in this.mockQueueItems)
            {
                mockQueueItem.Setup(c => c.DequeueCount).Returns(MaxDequeueCountToDeadLetter + 1);
            }

            this.mockQueue.Setup(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(this.mockQueueItems.Select(c => c.Object).ToList());

            this.mockDeadLetterTable.Setup(dlt => dlt.GetItemAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((AccountCloseDeadLetterStorage)null);
            this.mockDeadLetterTable.Setup(dlt => dlt.InsertAsync(It.IsAny<AccountCloseDeadLetterStorage>())).ReturnsAsync(true);

            var queueProcessor = new AadAccountCloseQueueProcessor(
                this.logger,
                this.configuration.Object,
                this.mockQueue.Object,
                this.mockAadAccountCloseService.Object,
                this.mockDeadLetterTable.Object,
                this.mockCounterFactory.Object,
                this.mockAppConfiguration.Object);

            // Verify release was renewed.
            Assert.IsTrue(await queueProcessor.DoWorkAsync().ConfigureAwait(false));
            this.mockQueue.Verify(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

            // Instead of dead-letter, they are deleted from the queue.
            this.mockDeadLetterTable.Verify(dlt => dlt.InsertAsync(It.IsAny<AccountCloseDeadLetterStorage>()));

            // Task 16153351: Increase AAD worker fault tolerance by writing non-retriable errors to dead-letter storage
            foreach (Mock<IQueueItem<AccountCloseRequest>> mockQueueItem in this.mockQueueItems)
            {
                // Should complete message once moved to Dead letter
                mockQueueItem.Verify(c => c.CompleteAsync(), Times.Once);
            }

            // PCF is called when processing enabled
            this.mockAadAccountCloseService.Verify(c => c.PostBatchAccountCloseAsync(It.IsAny<IList<IQueueItem<AccountCloseRequest>>>()), Times.Once);

            this.mockItemAgeCounter.Verify(c => c.SetValue(It.IsAny<ulong>()), Times.Exactly(this.mockQueueItems.Count));
            this.mockDeadLetterCounter.Verify(c => c.Increment(), Times.Exactly(this.mockQueueItems.Count));
        }

        [TestMethod]
        [DataRow(AdapterErrorCode.Forbidden, 1, false)]
        [DataRow(AdapterErrorCode.Forbidden, 2, true)]
        [DataRow(AdapterErrorCode.Unauthorized, 1, true)]
        [DataRow(AdapterErrorCode.Unauthorized, 2, true)]
        public async Task ShouldDeadLetterForbiddenAndUnAuthorized(AdapterErrorCode adapterErrorCode, int dequeueCount, bool shouldDeadLetter)
        {
            this.mockAppConfiguration.Setup(c => c.GetConfigValue(ConfigNames.PXS.AadAccountCloseWorker_EnableDequeuing, It.IsAny<bool>())).Returns(true);
            this.mockAppConfiguration.Setup(c => c.GetConfigValue(ConfigNames.PXS.AadAccountCloseWorker_EnableProcessing, It.IsAny<bool>())).Returns(true);

            // Set the response 
            foreach (ServiceResponse<IQueueItem<AccountCloseRequest>> serviceResponse in this.serviceResponses)
            {
                serviceResponse.Error = new Error(ErrorCode.PartnerError, "Partner Error")
                {
                    InnerError = new Error
                    {
                        Code = adapterErrorCode.ToString(),
                        Message = "Disallowed, discard message"
                    }
                };
            }

            foreach (Mock<IQueueItem<AccountCloseRequest>> mockQueueItem in this.mockQueueItems)
            {
                mockQueueItem.Setup(c => c.DequeueCount).Returns(dequeueCount);
            }

            this.mockQueue.Setup(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(this.mockQueueItems.Select(c => c.Object).ToList());

            this.mockDeadLetterTable.Setup(dlt => dlt.GetItemAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((AccountCloseDeadLetterStorage)null);
            this.mockDeadLetterTable.Setup(dlt => dlt.InsertAsync(It.IsAny<AccountCloseDeadLetterStorage>())).ReturnsAsync(true);

            var queueProcessor = new AadAccountCloseQueueProcessor(
                this.logger,
                this.configuration.Object,
                this.mockQueue.Object,
                this.mockAadAccountCloseService.Object,
                this.mockDeadLetterTable.Object,
                this.mockCounterFactory.Object,
                this.mockAppConfiguration.Object);

            // Verify message was processed.
            Assert.IsTrue(await queueProcessor.DoWorkAsync().ConfigureAwait(false));

            if (shouldDeadLetter)
            {
                // Verify that Message was never renewed but Completed (during dead letter)
                foreach (Mock<IQueueItem<AccountCloseRequest>> mockQueueItem in this.mockQueueItems)
                {
                    mockQueueItem.Verify(c => c.CompleteAsync(), Times.Once);
                    mockQueueItem.Verify(c => c.RenewLeaseAsync(TimeSpan.FromMinutes(5)), Times.Never);
                }

                // Should dead letter all messages
                this.mockDeadLetterCounter.Verify(c => c.Increment(), Times.Exactly(5));
            }
            else
            {
                // Verify that Message was never renewed but Completed (during dead letter)
                foreach (Mock<IQueueItem<AccountCloseRequest>> mockQueueItem in this.mockQueueItems)
                {
                    mockQueueItem.Verify(c => c.CompleteAsync(), Times.Never);
                    mockQueueItem.Verify(c => c.RenewLeaseAsync(It.IsAny<TimeSpan>()), Times.Never);
                }

                this.mockDeadLetterCounter.Verify(c => c.Increment(), Times.Never);
            }
        }

        [TestMethod]
        public async Task ShouldDeadLetterMessageUponConflictResponseAfterMaxTries()
        {
            this.mockAppConfiguration.Setup(c => c.GetConfigValue(ConfigNames.PXS.AadAccountCloseWorker_EnableDequeuing, It.IsAny<bool>())).Returns(true);
            this.mockAppConfiguration.Setup(c => c.GetConfigValue(ConfigNames.PXS.AadAccountCloseWorker_EnableProcessing, It.IsAny<bool>())).Returns(true);

            this.configuration.Setup(c => c.MaxDequeueCountForConflicts).Returns(MaxDequeueCountForConflicts);

            // Set the response as Conflict 
            foreach (ServiceResponse<IQueueItem<AccountCloseRequest>> serviceResponse in this.serviceResponses)
            {
                serviceResponse.Error = new Error(ErrorCode.PartnerError, "Partner Error")
                {
                    InnerError = new Error
                    {
                        Code = AdapterErrorCode.ConcurrencyConflict.ToString(),
                        Message = "Conflict, Try later"
                    }
                };
            }

            // Set the de queue count to max + 1
            foreach (Mock<IQueueItem<AccountCloseRequest>> mockQueueItem in this.mockQueueItems)
            {
                mockQueueItem.Setup(c => c.DequeueCount).Returns(MaxDequeueCountForConflicts + 1);
            }

            this.mockQueue.Setup(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(this.mockQueueItems.Select(c => c.Object).ToList());

            this.mockDeadLetterTable.Setup(dlt => dlt.GetItemAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((AccountCloseDeadLetterStorage)null);
            this.mockDeadLetterTable.Setup(dlt => dlt.InsertAsync(It.IsAny<AccountCloseDeadLetterStorage>())).ReturnsAsync(true);

            var queueProcessor = new AadAccountCloseQueueProcessor(
                this.logger,
                this.configuration.Object,
                this.mockQueue.Object,
                this.mockAadAccountCloseService.Object,
                this.mockDeadLetterTable.Object,
                this.mockCounterFactory.Object,
                this.mockAppConfiguration.Object);

            // Verify message was processed.
            Assert.IsTrue(await queueProcessor.DoWorkAsync().ConfigureAwait(false));

            // Verify that Message(with response Concurrency Conflict) completed but not renewed when MaxQueue Count is hit
            foreach (Mock<IQueueItem<AccountCloseRequest>> mockQueueItem in this.mockQueueItems)
            {
                mockQueueItem.Verify(c => c.CompleteAsync(), Times.Once);
                mockQueueItem.Verify(c => c.RenewLeaseAsync(TimeSpan.FromMinutes(5)), Times.Never);
            }

            // Should dead letter all the messages
            this.mockDeadLetterCounter.Verify(c => c.Increment(), Times.Exactly(this.mockQueueItems.Count));
        }

        [TestMethod]
        public void ShouldLoadCorrectLeaseSetFromConfig()
        {
            IList<TimeSpan> timeSpans1 = PrivacyConfigurationHelper.BuildFullLeaseExtensionSet(
                new List<string> { "1", "2", "20", "50" },
                PrivacyConfigurationHelper.LeaseExtensionTimeType.Minutes);

            Assert.AreEqual(timeSpans1[0], TimeSpan.FromMinutes(1));
            Assert.AreEqual(timeSpans1[3], TimeSpan.FromMinutes(50));

            // Validate if correct hours list is built
            IList<TimeSpan> timeSpans2 = PrivacyConfigurationHelper.BuildFullLeaseExtensionSet(
                new List<string> { "3", "4", "7", "8" },
                PrivacyConfigurationHelper.LeaseExtensionTimeType.Hours);

            Assert.AreEqual(timeSpans2[0], TimeSpan.FromHours(3));
            Assert.AreEqual(timeSpans2[2], TimeSpan.FromHours(7));
            Assert.AreEqual(timeSpans2[3], TimeSpan.FromHours(8));
        }

        [TestMethod]
        public async Task ShouldNotAttemptProcessWhenNoMessagesToProcess()
        {
            this.mockAppConfiguration.Setup(c => c.GetConfigValue(ConfigNames.PXS.AadAccountCloseWorker_EnableDequeuing, It.IsAny<bool>())).Returns(true);
            this.mockAppConfiguration.Setup(c => c.GetConfigValue(ConfigNames.PXS.AadAccountCloseWorker_EnableProcessing, It.IsAny<bool>())).Returns(true);

            // Test that null or empty list will make DoWorkAsync return false.
            var queueResponseTest = new List<List<IQueueItem<AccountCloseRequest>>>
            {
                null,
                new List<IQueueItem<AccountCloseRequest>>()
            };

            for (int i = 0; i < queueResponseTest.Count; i++)
            {
                List<IQueueItem<AccountCloseRequest>> queueResponse = queueResponseTest[i];
                this.mockQueue.Setup(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(queueResponse);
                var queueProcessor = new AadAccountCloseQueueProcessor(
                    this.logger,
                    this.configuration.Object,
                    this.mockQueue.Object,
                    this.mockAadAccountCloseService.Object,
                    this.mockDeadLetterTable.Object,
                    this.mockCounterFactory.Object,
                    this.mockAppConfiguration.Object);

                Assert.IsFalse(await queueProcessor.DoWorkAsync().ConfigureAwait(false));
                this.mockQueue.Verify(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(i + 1));
            }
        }

        [TestMethod]
        public async Task ShouldNotDeadLetterMessageUponConflictResponseBeforeMaxTries()
        {
            this.mockAppConfiguration.Setup(c => c.GetConfigValue(ConfigNames.PXS.AadAccountCloseWorker_EnableDequeuing, It.IsAny<bool>())).Returns(true);
            this.mockAppConfiguration.Setup(c => c.GetConfigValue(ConfigNames.PXS.AadAccountCloseWorker_EnableProcessing, It.IsAny<bool>())).Returns(true);

            // Set the response as Conflict 
            foreach (ServiceResponse<IQueueItem<AccountCloseRequest>> serviceResponse in this.serviceResponses)
            {
                serviceResponse.Error = new Error(ErrorCode.PartnerError, "Partner Error")
                {
                    InnerError = new Error
                    {
                        Code = AdapterErrorCode.ConcurrencyConflict.ToString(),
                        Message = "Conflict, Try later"
                    }
                };
            }

            // Set the de queue count to max 
            foreach (Mock<IQueueItem<AccountCloseRequest>> mockQueueItem in this.mockQueueItems)
            {
                mockQueueItem.Setup(c => c.DequeueCount).Returns(MaxDequeueCountBeforeRequeue);
            }

            this.mockQueue.Setup(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(this.mockQueueItems.Select(c => c.Object).ToList());

            this.mockDeadLetterTable.Setup(dlt => dlt.InsertAsync(It.IsAny<AccountCloseDeadLetterStorage>())).ReturnsAsync(true);

            var queueProcessor = new AadAccountCloseQueueProcessor(
                this.logger,
                this.configuration.Object,
                this.mockQueue.Object,
                this.mockAadAccountCloseService.Object,
                this.mockDeadLetterTable.Object,
                this.mockCounterFactory.Object,
                this.mockAppConfiguration.Object);

            // Verify message was processed.
            Assert.IsTrue(await queueProcessor.DoWorkAsync().ConfigureAwait(false));

            // Verify that Message was not completed nor renewed when MaxQueue Count is hit and response is Too Many requests
            foreach (Mock<IQueueItem<AccountCloseRequest>> mockQueueItem in this.mockQueueItems)
            {
                mockQueueItem.Verify(c => c.CompleteAsync(), Times.Never);
                mockQueueItem.Verify(c => c.RenewLeaseAsync(TimeSpan.FromMinutes(5)), Times.Never);
            }

            // Should not dead letter any message
            this.mockDeadLetterCounter.Verify(c => c.Increment(), Times.Exactly(0));
        }

        [TestMethod]
        public async Task ShouldNotDeadLetterMessageUponTooManyRequestsResponse()
        {
            this.mockAppConfiguration.Setup(c => c.GetConfigValue(ConfigNames.PXS.AadAccountCloseWorker_EnableDequeuing, It.IsAny<bool>())).Returns(true);
            this.mockAppConfiguration.Setup(c => c.GetConfigValue(ConfigNames.PXS.AadAccountCloseWorker_EnableProcessing, It.IsAny<bool>())).Returns(true);

            // Set the response as Too Many requests
            foreach (ServiceResponse<IQueueItem<AccountCloseRequest>> serviceResponse in this.serviceResponses)
            {
                serviceResponse.Error = new Error(ErrorCode.PartnerError, "Partner Error")
                {
                    InnerError = new Error
                    {
                        Code = AdapterErrorCode.TooManyRequests.ToString(),
                        Message = "Too Many Requests, Try later"
                    }
                };
            }

            // Set the de queue count to max 
            foreach (Mock<IQueueItem<AccountCloseRequest>> mockQueueItem in this.mockQueueItems)
            {
                mockQueueItem.Setup(c => c.DequeueCount).Returns(MaxDequeueCountBeforeRequeue);
            }

            this.mockQueue.Setup(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(this.mockQueueItems.Select(c => c.Object).ToList());

            this.mockDeadLetterTable.Setup(dlt => dlt.InsertAsync(It.IsAny<AccountCloseDeadLetterStorage>())).ReturnsAsync(true);

            var queueProcessor = new AadAccountCloseQueueProcessor(
                this.logger,
                this.configuration.Object,
                this.mockQueue.Object,
                this.mockAadAccountCloseService.Object,
                this.mockDeadLetterTable.Object,
                this.mockCounterFactory.Object,
                this.mockAppConfiguration.Object);

            // Verify message was processed.
            Assert.IsTrue(await queueProcessor.DoWorkAsync().ConfigureAwait(false));

            // Verify that Message was not completed nor renewed when MaxQueue Count is hit and response is Too Many requests
            foreach (Mock<IQueueItem<AccountCloseRequest>> mockQueueItem in this.mockQueueItems)
            {
                mockQueueItem.Verify(c => c.CompleteAsync(), Times.Never);
                mockQueueItem.Verify(c => c.RenewLeaseAsync(TimeSpan.FromMinutes(5)), Times.Never);
            }

            // Should not dead letter any message
            this.mockDeadLetterCounter.Verify(c => c.Increment(), Times.Exactly(0));

            // Should hit Too Many requests flow
            this.aadRVSTooManyRequests.Verify(c => c.Increment(), Times.Exactly(this.mockQueueItems.Count));
            this.aadRVSConcurrencyConflicts.Verify(c => c.Increment(), Times.Exactly(0));
        }

        [TestMethod]
        public async Task ShouldNotProcessWhenDequeuingDisabled()
        {
            this.mockAppConfiguration.Setup(c => c.GetConfigValue(ConfigNames.PXS.AadAccountCloseWorker_EnableDequeuing, It.IsAny<bool>())).Returns(false);

            var queueProcessor = new AadAccountCloseQueueProcessor(
                this.logger,
                this.configuration.Object,
                this.mockQueue.Object,
                this.mockAadAccountCloseService.Object,
                this.mockDeadLetterTable.Object,
                this.mockCounterFactory.Object,
                this.mockAppConfiguration.Object);

            Assert.IsFalse(await queueProcessor.DoWorkAsync().ConfigureAwait(false));
        }

        [TestMethod]
        public async Task ShouldNotProcessWhenProcessingDisabled()
        {
            this.mockAppConfiguration.Setup(c => c.GetConfigValue(ConfigNames.PXS.AadAccountCloseWorker_EnableDequeuing, It.IsAny<bool>())).Returns(true);
            this.mockAppConfiguration.Setup(c => c.GetConfigValue(ConfigNames.PXS.AadAccountCloseWorker_EnableProcessing, It.IsAny<bool>())).Returns(false);

            var mockQueueItem = new Mock<IQueueItem<AccountCloseRequest>>(MockBehavior.Strict);
            mockQueueItem.Setup(c => c.CompleteAsync()).Returns(Task.CompletedTask);
            mockQueueItem.Setup(c => c.Data).Returns(new AccountCloseRequest());

            this.mockQueue.Setup(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<IQueueItem<AccountCloseRequest>> { mockQueueItem.Object });
            var queueProcessor = new AadAccountCloseQueueProcessor(
                this.logger,
                this.configuration.Object,
                this.mockQueue.Object,
                this.mockAadAccountCloseService.Object,
                this.mockDeadLetterTable.Object,
                this.mockCounterFactory.Object,
                this.mockAppConfiguration.Object);

            Assert.IsTrue(await queueProcessor.DoWorkAsync().ConfigureAwait(false));
            this.mockQueue.Verify(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
            mockQueueItem.Verify(c => c.CompleteAsync(), Times.Once);

            // PCF is never called when processing disabled
            this.mockAadAccountCloseService.Verify(
                c => c.PostBatchAccountCloseAsync(It.IsAny<IList<IQueueItem<AccountCloseRequest>>>()),
                Times.Never);
        }

        [TestMethod]
        public async Task ShouldProcessSuccessfullBatch()
        {
            this.mockAppConfiguration.Setup(c => c.GetConfigValue(ConfigNames.PXS.AadAccountCloseWorker_EnableDequeuing, It.IsAny<bool>())).Returns(true);
            this.mockAppConfiguration.Setup(c => c.GetConfigValue(ConfigNames.PXS.AadAccountCloseWorker_EnableProcessing, It.IsAny<bool>())).Returns(true);

            this.mockQueue.Setup(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(this.mockQueueItems.Select(c => c.Object).ToList());
            var queueProcessor = new AadAccountCloseQueueProcessor(
                this.logger,
                this.configuration.Object,
                this.mockQueue.Object,
                this.mockAadAccountCloseService.Object,
                this.mockDeadLetterTable.Object,
                this.mockCounterFactory.Object,
                this.mockAppConfiguration.Object);

            Assert.IsTrue(await queueProcessor.DoWorkAsync().ConfigureAwait(false));
            this.mockQueue.Verify(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

            foreach (Mock<IQueueItem<AccountCloseRequest>> mockQueueItem in this.mockQueueItems)
            {
                mockQueueItem.Verify(c => c.CompleteAsync(), Times.Once);
            }

            // PCF is called when processing enabled
            this.mockAadAccountCloseService.Verify(c => c.PostBatchAccountCloseAsync(It.IsAny<IList<IQueueItem<AccountCloseRequest>>>()), Times.Once);
        }

        [TestMethod]
        public async Task ShouldRenewLease()
        {
            this.mockAppConfiguration.Setup(c => c.GetConfigValue(ConfigNames.PXS.AadAccountCloseWorker_EnableDequeuing, It.IsAny<bool>())).Returns(true);
            this.mockAppConfiguration.Setup(c => c.GetConfigValue(ConfigNames.PXS.AadAccountCloseWorker_EnableProcessing, It.IsAny<bool>())).Returns(true);

            foreach (ServiceResponse<IQueueItem<AccountCloseRequest>> serviceResponse in this.serviceResponses)
            {
                serviceResponse.Error = new Error(ErrorCode.PartnerError, "Something bad");
            }

            foreach (Mock<IQueueItem<AccountCloseRequest>> mockQueueItem in this.mockQueueItems)
            {
                mockQueueItem.Setup(c => c.DequeueCount).Returns(1);
            }

            this.mockQueue.Setup(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(this.mockQueueItems.Select(c => c.Object).ToList());
            var queueProcessor = new AadAccountCloseQueueProcessor(
                this.logger,
                this.configuration.Object,
                this.mockQueue.Object,
                this.mockAadAccountCloseService.Object,
                this.mockDeadLetterTable.Object,
                this.mockCounterFactory.Object,
                this.mockAppConfiguration.Object);

            // Verify release was renewed.
            Assert.IsTrue(await queueProcessor.DoWorkAsync().ConfigureAwait(false));
            this.mockQueue.Verify(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

            foreach (Mock<IQueueItem<AccountCloseRequest>> mockQueueItem in this.mockQueueItems)
            {
                mockQueueItem.Verify(c => c.RenewLeaseAsync(It.IsAny<TimeSpan>()), Times.Once);
            }

            // PCF is called when processing enabled
            this.mockAadAccountCloseService.Verify(c => c.PostBatchAccountCloseAsync(It.IsAny<IList<IQueueItem<AccountCloseRequest>>>()), Times.Once);
        }

        [TestMethod]
        public void ShouldReturnCorrectLeaseRenewalTime()
        {
            var queueProcessor = new AadAccountCloseQueueProcessor(
                this.logger,
                this.configuration.Object,
                this.mockQueue.Object,
                this.mockAadAccountCloseService.Object,
                this.mockDeadLetterTable.Object,
                this.mockCounterFactory.Object,
                this.mockAppConfiguration.Object);

            // Validate if correct lease minutes are returned based on de-queue count
            PrivateObject aadPrivateObject = new PrivateObject(queueProcessor);
            object[] argsMinutes = new object[2] { PrivacyConfigurationHelper.LeaseExtensionTimeType.Minutes, 2 };
            var timeSpan1 = (TimeSpan)aadPrivateObject.Invoke("GetLeaseExtension", argsMinutes);

            Assert.AreEqual(timeSpan1, TimeSpan.FromMinutes(5));

            // Validate if correct lease hours are returned based on de-queue count
            object[] argsHours = new object[2] { PrivacyConfigurationHelper.LeaseExtensionTimeType.Hours, 3 };
            var timeSpan2 = (TimeSpan)aadPrivateObject.Invoke("GetLeaseExtension", argsHours);

            Assert.AreEqual(timeSpan2, TimeSpan.FromHours(7));
        }

        [TestMethod]
        public async Task ShouldReturnFalseIfExceptionOccurs()
        {
            this.mockAppConfiguration.Setup(c => c.GetConfigValue(ConfigNames.PXS.AadAccountCloseWorker_EnableDequeuing, It.IsAny<bool>())).Returns(true);
            this.mockAppConfiguration.Setup(c => c.GetConfigValue(ConfigNames.PXS.AadAccountCloseWorker_EnableProcessing, It.IsAny<bool>())).Returns(true);

            this.mockQueue.Setup(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).Throws(new TaskCanceledException("This task got canceled."));
            var queueProcessor = new AadAccountCloseQueueProcessor(
                this.logger,
                this.configuration.Object,
                this.mockQueue.Object,
                this.mockAadAccountCloseService.Object,
                this.mockDeadLetterTable.Object,
                this.mockCounterFactory.Object,
                this.mockAppConfiguration.Object);

            Assert.IsFalse(await queueProcessor.DoWorkAsync().ConfigureAwait(false));
            this.mockQueue.Verify(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

            foreach (Mock<IQueueItem<AccountCloseRequest>> mockQueueItem in this.mockQueueItems)
            {
                mockQueueItem.Verify(c => c.CompleteAsync(), Times.Never);
            }

            this.mockAadAccountCloseService.Verify(
                c => c.PostBatchAccountCloseAsync(It.IsAny<IList<IQueueItem<AccountCloseRequest>>>()),
                Times.Never);
        }

        [TestMethod]
        public void StartSuccess()
        {
            var queueProcessor = new AadAccountCloseQueueProcessor(
                this.logger,
                this.configuration.Object,
                this.mockQueue.Object,
                this.mockAadAccountCloseService.Object,
                this.mockDeadLetterTable.Object,
                this.mockCounterFactory.Object,
                this.mockAppConfiguration.Object);

            queueProcessor.Start();
        }

        [TestInitialize]
        public void TestInit()
        {
            this.configuration = new Mock<IAadAccountCloseQueueProccessorConfiguration>(MockBehavior.Strict);
            this.configuration.SetupGet(c => c.WaitOnQueueEmptyMilliseconds).Returns(5000);
            this.configuration.SetupGet(c => c.GetMessagesDequeueCount).Returns(5);
            this.configuration.SetupGet(c => c.MaxDequeueCountToDeadLetter).Returns(MaxDequeueCountToDeadLetter);
            this.configuration.SetupGet(c => c.MaxDequeueCountForForbidden).Returns(2);
            this.configuration.SetupGet(c => c.MaxDequeueCountBeforeRequeue).Returns(MaxDequeueCountBeforeRequeue);
            this.configuration.Setup(c => c.LeaseExtensionMinuteSet).Returns(new List<string> { "4", "5", "6", "8" });
            this.configuration.Setup(c => c.LeaseExtensionHourSet).Returns(new List<string> { "1", "2", "7", "15" });

            this.mockQueue = new Mock<IAccountCloseQueueManager>(MockBehavior.Strict);
            this.mockAadAccountCloseService = new Mock<IAadAccountCloseService>(MockBehavior.Strict);

            this.mockItemAgeCounter = new Mock<ICounter>(MockBehavior.Strict);
            this.mockItemAgeCounter.Setup(c => c.SetValue(It.IsAny<ulong>()));

            this.mockDeadLetterCounter = new Mock<ICounter>(MockBehavior.Strict);
            this.mockDeadLetterCounter.Setup(c => c.Increment());

            this.aadRVSTooManyRequests = new Mock<ICounter>(MockBehavior.Strict);
            this.aadRVSTooManyRequests.Setup(c => c.Increment());

            this.aadRVSConcurrencyConflicts = new Mock<ICounter>(MockBehavior.Strict);
            this.aadRVSConcurrencyConflicts.Setup(c => c.Increment());

            this.mockCounterFactory = new Mock<ICounterFactory>(MockBehavior.Strict);
            this.mockCounterFactory.Setup(c => c.GetCounter(CounterCategoryNames.AzureQueue, "AadAccountCloseQueueItemAge", CounterType.Number))
                .Returns(this.mockItemAgeCounter.Object);
            this.mockCounterFactory.Setup(c => c.GetCounter(CounterCategoryNames.AadAccountClose, "DeadLetterCount", CounterType.Number))
                .Returns(this.mockDeadLetterCounter.Object);

            this.mockCounterFactory.Setup(c => c.GetCounter(CounterCategoryNames.AadAccountClose, "AadRVSTooManyRequests", CounterType.Number))
                .Returns(this.aadRVSTooManyRequests.Object);
            this.mockCounterFactory.Setup(c => c.GetCounter(CounterCategoryNames.AadAccountClose, "AadRVSConcurrencyConflicts", CounterType.Number))
                .Returns(this.aadRVSConcurrencyConflicts.Object);

            this.mockQueueItems = new List<Mock<IQueueItem<AccountCloseRequest>>>();

            for (int i = 0; i < 5; i++)
            {
                var mockQueueItem = new Mock<IQueueItem<AccountCloseRequest>>(MockBehavior.Strict);
                mockQueueItem.Setup(c => c.CompleteAsync()).Returns(Task.CompletedTask);
                mockQueueItem.Setup(c => c.RenewLeaseAsync(It.IsAny<TimeSpan>())).ReturnsAsync(true);
                mockQueueItem.Setup(c => c.InsertionTime).Returns(DateTimeOffset.UtcNow.AddHours(-this.random.Next(7 * 24)));

                mockQueueItem.Setup(c => c.Data).Returns(
                    new AccountCloseRequest
                    {
                        RequestId = Guid.NewGuid(),
                        Subject = new AadSubject { ObjectId = Guid.NewGuid() }
                    });

                this.mockQueueItems.Add(mockQueueItem);
            }

            this.serviceResponses = new List<ServiceResponse<IQueueItem<AccountCloseRequest>>>();

            foreach (Mock<IQueueItem<AccountCloseRequest>> mockQueueItem in this.mockQueueItems)
            {
                this.serviceResponses.Add(new ServiceResponse<IQueueItem<AccountCloseRequest>> { Result = mockQueueItem.Object });
            }

            this.mockAadAccountCloseService
                .Setup(c => c.PostBatchAccountCloseAsync(It.IsAny<IList<IQueueItem<AccountCloseRequest>>>()))
                .ReturnsAsync(this.serviceResponses);

            this.mockDeadLetterTable = new Mock<ITable<AccountCloseDeadLetterStorage>>(MockBehavior.Strict);

            this.mockAppConfiguration = new Mock<IAppConfiguration>();
        }

        #region Test data

        private static Mock<IAadAccountCloseQueueProccessorConfiguration> CreateMockAadAccountCloseQueueProccessorConfiguration()
        {
            var mockAadAccountCloseQueueProccessor = new Mock<IAadAccountCloseQueueProccessorConfiguration>();
            mockAadAccountCloseQueueProccessor.SetupGet(c => c.WaitOnQueueEmptyMilliseconds).Returns(5000);
            mockAadAccountCloseQueueProccessor.SetupGet(c => c.GetMessagesDequeueCount).Returns(5);
            mockAadAccountCloseQueueProccessor.SetupGet(c => c.MaxDequeueCountToDeadLetter).Returns(MaxDequeueCountToDeadLetter);
            return mockAadAccountCloseQueueProccessor;
        }

        public static IEnumerable<object[]> AadAccountCloseQueueProcessorConstructorTestData()
        {
            var mockLogger = new ConsoleLogger();
            var mockConfiguration = CreateMockAadAccountCloseQueueProccessorConfiguration();
            var mockQueueManager = new Mock<IAccountCloseQueueManager>(MockBehavior.Strict);
            var mockAadAccountCloseService = new Mock<IAadAccountCloseService>(MockBehavior.Strict);
            var mockDeadLetterTable = new Mock<ITable<AccountCloseDeadLetterStorage>>(MockBehavior.Strict);
            var mockCounterFactory = CreateMockCounterFactory();
            var mockAppConfiguration = new Mock<IAppConfiguration>(MockBehavior.Strict);

            var data = new List<object[]>
            {
                new object[]
                {
                    null,
                    mockConfiguration.Object,
                    mockQueueManager.Object,
                    mockAadAccountCloseService.Object,
                    mockDeadLetterTable.Object,
                    mockCounterFactory.Object,
                    mockAppConfiguration.Object,
                },
                new object[]
                {
                    mockLogger,
                    mockConfiguration.Object,
                    null,
                    mockAadAccountCloseService.Object,
                    mockDeadLetterTable.Object,
                    mockCounterFactory.Object,
                    mockAppConfiguration.Object,
                },
                new object[]
                {
                    mockLogger,
                    mockConfiguration.Object,
                    mockQueueManager.Object,
                    null,
                    mockDeadLetterTable.Object,
                    mockCounterFactory.Object,
                    mockAppConfiguration.Object,
                },
                new object[]
                {
                    mockLogger,
                    mockConfiguration.Object,
                    mockQueueManager.Object,
                    mockAadAccountCloseService.Object,
                    null,
                    mockCounterFactory.Object,
                    mockAppConfiguration.Object,
                },
                new object[]
                {
                    mockLogger,
                    mockConfiguration.Object,
                    mockQueueManager.Object,
                    mockAadAccountCloseService.Object,
                    mockDeadLetterTable.Object,
                    null,
                    mockAppConfiguration.Object,
                },
                new object[]
                {
                    mockLogger,
                    mockConfiguration.Object,
                    mockQueueManager.Object,
                    mockAadAccountCloseService.Object,
                    mockDeadLetterTable.Object,
                    mockCounterFactory.Object,
                    null,
                }
            };
            return data;
        }

        private static Mock<ICounterFactory> CreateMockCounterFactory()
        {
            return new Mock<ICounterFactory>();
        }

        #endregion
    }
}

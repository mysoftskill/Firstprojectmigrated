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

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.Membership.MemberServices.Privacy.Core.Vortex;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.VortexDeviceDeleteWorker.QueueProcessor;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    /// <summary>
    /// </summary>
    [TestClass]
    public class VortexDeviceDeleteQueueProcessorTest
    {
        private readonly ILogger logger = new ConsoleLogger();

        private readonly Random random = new Random(5);

        private Mock<IVortexDeviceDeleteQueueProccessorConfiguration> configuration;

        private Mock<ICounter> mockCounter;

        private Mock<ICounterFactory> mockCounterFactory;

        private Mock<IVortexDeviceDeleteQueueManager> mockQueue;

        private List<Mock<IQueueItem<DeviceDeleteRequest>>> mockQueueItems;

        private Mock<IVortexEventService> mockVortexDeviceDeleteService;

        private Mock<IAppConfiguration> mockAppConfiguration;

        private List<ServiceResponse<IQueueItem<DeviceDeleteRequest>>> serviceResponses;

        [TestMethod]
        public async Task ShouldDeadLetterAfterExceedingMaxDequeueCountToDeadLetter()
        {
            this.mockAppConfiguration.Setup(c => c.GetConfigValue(ConfigNames.PXS.VortexDeviceDeleteWorker_EnableDequeuing, It.IsAny<bool>())).Returns(true);
            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.PXS.DeleteDeviceRequestEnabled, It.IsAny<bool>())).ReturnsAsync(true);
            this.mockAppConfiguration.Setup(c => c.GetConfigValue<int>(ConfigNames.PXS.DeviceDeleteMaxVisibilityTimeoutInMinutes, It.IsAny<int>())).Returns(1);

            foreach (ServiceResponse<IQueueItem<DeviceDeleteRequest>> serviceResponse in this.serviceResponses)
            {
                serviceResponse.Error = new Error(ErrorCode.PartnerError, "Something bad");
            }

            foreach (Mock<IQueueItem<DeviceDeleteRequest>> mockQueueItem in this.mockQueueItems)
            {
                mockQueueItem.Setup(c => c.DequeueCount).Returns(1);
            }

            this.mockQueue.Setup(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(this.mockQueueItems.Select(c => c.Object).ToList());

            var queueProcessor = new VortexDeviceDeleteQueueProcessor(
                this.logger,
                this.configuration.Object,
                this.mockQueue.Object,
                this.mockVortexDeviceDeleteService.Object,
                this.mockCounterFactory.Object,
                this.mockAppConfiguration.Object);

            // Verify release was renewed.
            Assert.IsTrue(await queueProcessor.DoWorkAsync().ConfigureAwait(false));
            this.mockQueue.Verify(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

            foreach (Mock<IQueueItem<DeviceDeleteRequest>> mockQueueItem in this.mockQueueItems)
            {
                mockQueueItem.Verify(c => c.RenewLeaseAsync(TimeSpan.FromSeconds(2)), Times.Once);
            }

            // PCF is called when processing enabled
            this.mockVortexDeviceDeleteService.Verify(c => c.DeleteDevicesAsync(It.IsAny<IList<IQueueItem<DeviceDeleteRequest>>>()), Times.Once);

            this.mockCounter.Verify(c => c.SetValue(It.IsAny<ulong>()), Times.Exactly(this.mockQueueItems.Count));
        }

        [TestMethod]
        public async Task ShouldNotAttemptProcessWhenNoMessagesToProcess()
        {
            this.mockAppConfiguration.Setup(c => c.GetConfigValue(ConfigNames.PXS.VortexDeviceDeleteWorker_EnableDequeuing, It.IsAny<bool>())).Returns(true);

            // Test that null or empty list will make DoWorkAsync return false.
            var queueResponseTest = new List<List<IQueueItem<DeviceDeleteRequest>>>
            {
                null,
                new List<IQueueItem<DeviceDeleteRequest>>()
            };

            for (int i = 0; i < queueResponseTest.Count; i++)
            {
                List<IQueueItem<DeviceDeleteRequest>> queueResponse = queueResponseTest[i];
                this.mockQueue.Setup(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(queueResponse);
                var queueProcessor = new VortexDeviceDeleteQueueProcessor(
                    this.logger,
                    this.configuration.Object,
                    this.mockQueue.Object,
                    this.mockVortexDeviceDeleteService.Object,
                    this.mockCounterFactory.Object,
                    this.mockAppConfiguration.Object);

                Assert.IsFalse(await queueProcessor.DoWorkAsync().ConfigureAwait(false));
                this.mockQueue.Verify(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(i + 1));
            }
        }

        [TestMethod]
        public async Task ShouldNotProcessWhenDequeuingDisabled()
        {
            this.mockAppConfiguration.Setup(c => c.GetConfigValue(ConfigNames.PXS.VortexDeviceDeleteWorker_EnableDequeuing, It.IsAny<bool>())).Returns(false);

            var queueProcessor = new VortexDeviceDeleteQueueProcessor(
                this.logger,
                this.configuration.Object,
                this.mockQueue.Object,
                this.mockVortexDeviceDeleteService.Object,
                this.mockCounterFactory.Object,
                this.mockAppConfiguration.Object);

            Assert.IsFalse(await queueProcessor.DoWorkAsync().ConfigureAwait(false));
        }

        [TestMethod]
        public async Task ShouldNotProcessWhenProcessingDisabled()
        {
            this.mockAppConfiguration.Setup(c => c.GetConfigValue(ConfigNames.PXS.VortexDeviceDeleteWorker_EnableDequeuing, It.IsAny<bool>())).Returns(true);
            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.PXS.DeleteDeviceRequestEnabled, It.IsAny<bool>())).ReturnsAsync(false);
            this.mockAppConfiguration.Setup(c => c.GetConfigValue<int>(ConfigNames.PXS.DeviceDeleteMaxVisibilityTimeoutInMinutes, It.IsAny<int>())).Returns(1);

            var mockQueueItem = new Mock<IQueueItem<DeviceDeleteRequest>>(MockBehavior.Strict);
            mockQueueItem.Setup(c => c.CompleteAsync()).Returns(Task.CompletedTask);
            mockQueueItem.Setup(c => c.Data).Returns(new DeviceDeleteRequest
            {
                RequestInformation = new VortexRequestInformation
                {
                    RequestTime = DateTimeOffset.UtcNow
                }
            });
            mockQueueItem.Setup(c => c.RenewLeaseAsync(It.IsAny<TimeSpan>())).ReturnsAsync(true);
            mockQueueItem.Setup(c => c.DequeueCount).Returns(0);

            this.mockQueue.Setup(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<IQueueItem<DeviceDeleteRequest>> { mockQueueItem.Object });
            var queueProcessor = new VortexDeviceDeleteQueueProcessor(
                this.logger,
                this.configuration.Object,
                this.mockQueue.Object,
                this.mockVortexDeviceDeleteService.Object,
                this.mockCounterFactory.Object,
                this.mockAppConfiguration.Object);

            Assert.IsTrue(await queueProcessor.DoWorkAsync().ConfigureAwait(false));
            this.mockQueue.Verify(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

            // PCF is never called when processing disabled
            this.mockVortexDeviceDeleteService.Verify(
                c => c.DeleteDevicesAsync(It.IsAny<IList<IQueueItem<DeviceDeleteRequest>>>()),
                Times.Never);

            // AnaheimIdAdapter is never called when processing disabled
            this.mockVortexDeviceDeleteService.Verify(
                c => c.SendAnaheimDeviceDeleteIdRequestAsync(It.IsAny<DeviceDeleteRequest>()),
                Times.Never);
        }

        [TestMethod]
        public async Task ShouldProcessSuccessfullBatch()
        {
            this.mockAppConfiguration.Setup(c => c.GetConfigValue(ConfigNames.PXS.VortexDeviceDeleteWorker_EnableDequeuing, It.IsAny<bool>())).Returns(true);
            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.PXS.DeleteDeviceRequestEnabled, It.IsAny<bool>())).ReturnsAsync(true);
            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.PXS.AnaheimIdEventsPublishEnabled, It.IsAny<bool>())).ReturnsAsync(true);
            this.mockAppConfiguration.Setup(c => c.GetConfigValue<int>(ConfigNames.PXS.DeviceDeleteMaxVisibilityTimeoutInMinutes, It.IsAny<int>())).Returns(1);

            this.mockQueue.Setup(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(this.mockQueueItems.Select(c => c.Object).ToList());
            var queueProcessor = new VortexDeviceDeleteQueueProcessor(
                this.logger,
                this.configuration.Object,
                this.mockQueue.Object,
                this.mockVortexDeviceDeleteService.Object,
                this.mockCounterFactory.Object,
                this.mockAppConfiguration.Object);

            Assert.IsTrue(await queueProcessor.DoWorkAsync().ConfigureAwait(false));
            this.mockQueue.Verify(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

            foreach (Mock<IQueueItem<DeviceDeleteRequest>> mockQueueItem in this.mockQueueItems)
            {
                mockQueueItem.Verify(c => c.CompleteAsync(), Times.Once);
            }

            // PCF is called when processing enabled
            this.mockVortexDeviceDeleteService.Verify(c => c.DeleteDevicesAsync(It.IsAny<IList<IQueueItem<DeviceDeleteRequest>>>()), Times.Once);

            // AnaheimIdAdapter is called when processing enabled
            this.mockVortexDeviceDeleteService.Verify(
                c => c.SendAnaheimDeviceDeleteIdRequestAsync(It.IsAny<DeviceDeleteRequest>()),
                Times.Exactly(this.mockQueueItems.Count));
        }

        [TestMethod]
        public async Task ShouldRenewLeaseIfFailsToSendToPCF()
        {
            this.mockAppConfiguration.Setup(c => c.GetConfigValue(ConfigNames.PXS.VortexDeviceDeleteWorker_EnableDequeuing, It.IsAny<bool>())).Returns(true);
            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.PXS.DeleteDeviceRequestEnabled, It.IsAny<bool>())).ReturnsAsync(true);
            this.mockAppConfiguration.Setup(c => c.GetConfigValue<int>(ConfigNames.PXS.DeviceDeleteMaxVisibilityTimeoutInMinutes, It.IsAny<int>())).Returns(1);

            foreach (ServiceResponse<IQueueItem<DeviceDeleteRequest>> serviceResponse in this.serviceResponses)
            {
                serviceResponse.Error = new Error(ErrorCode.PartnerError, "Something bad");
            }

            foreach (Mock<IQueueItem<DeviceDeleteRequest>> mockQueueItem in this.mockQueueItems)
            {
                mockQueueItem.Setup(c => c.DequeueCount).Returns(1);
            }

            this.mockQueue.Setup(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(this.mockQueueItems.Select(c => c.Object).ToList());
            var queueProcessor = new VortexDeviceDeleteQueueProcessor(
                this.logger,
                this.configuration.Object,
                this.mockQueue.Object,
                this.mockVortexDeviceDeleteService.Object,
                this.mockCounterFactory.Object,
                this.mockAppConfiguration.Object);

            // Verify release was renewed.
            Assert.IsTrue(await queueProcessor.DoWorkAsync().ConfigureAwait(false));
            this.mockQueue.Verify(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

            foreach (Mock<IQueueItem<DeviceDeleteRequest>> mockQueueItem in this.mockQueueItems)
            {
                mockQueueItem.Verify(c => c.RenewLeaseAsync(It.IsAny<TimeSpan>()), Times.Once);
            }

            // PCF is called when processing enabled
            this.mockVortexDeviceDeleteService.Verify(c => c.DeleteDevicesAsync(It.IsAny<IList<IQueueItem<DeviceDeleteRequest>>>()), Times.Once);

            // AnaheimIdAdapter is never called
            this.mockVortexDeviceDeleteService.Verify(
                c => c.SendAnaheimDeviceDeleteIdRequestAsync(It.IsAny<DeviceDeleteRequest>()),
                Times.Never);
        }

        [TestMethod]
        public async Task ShouldUpdateMessageIfFailsToSendToAnaheim()
        {
            this.mockAppConfiguration.Setup(c => c.GetConfigValue(ConfigNames.PXS.VortexDeviceDeleteWorker_EnableDequeuing, It.IsAny<bool>())).Returns(true);
            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.PXS.DeleteDeviceRequestEnabled, It.IsAny<bool>())).ReturnsAsync(true);
            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.PXS.AnaheimIdEventsPublishEnabled, It.IsAny<bool>())).ReturnsAsync(true);
            this.mockAppConfiguration.Setup(c => c.GetConfigValue<int>(ConfigNames.PXS.DeviceDeleteMaxVisibilityTimeoutInMinutes, It.IsAny<int>())).Returns(1);

            foreach (Mock<IQueueItem<DeviceDeleteRequest>> mockQueueItem in this.mockQueueItems)
            {
                mockQueueItem.Setup(c => c.DequeueCount).Returns(1);
                mockQueueItem.Setup(c => c.UpdateAsync(It.IsAny<TimeSpan>())).ReturnsAsync(true);
            }

            this.mockQueue.Setup(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(this.mockQueueItems.Select(c => c.Object).ToList());

            this.mockVortexDeviceDeleteService.Setup(c => c.SendAnaheimDeviceDeleteIdRequestAsync(It.IsAny<DeviceDeleteRequest>())).ReturnsAsync(false);
            
            var queueProcessor = new VortexDeviceDeleteQueueProcessor(
                this.logger,
                this.configuration.Object,
                this.mockQueue.Object,
                this.mockVortexDeviceDeleteService.Object,
                this.mockCounterFactory.Object,
                this.mockAppConfiguration.Object);

            // Verify release was renewed.
            Assert.IsTrue(await queueProcessor.DoWorkAsync().ConfigureAwait(false));
            this.mockQueue.Verify(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

            foreach (Mock<IQueueItem<DeviceDeleteRequest>> mockQueueItem in this.mockQueueItems)
            {
                mockQueueItem.Verify(c => c.UpdateAsync(It.IsAny<TimeSpan>()), Times.Once);
            }

            // PCF is called when processing enabled
            this.mockVortexDeviceDeleteService.Verify(c => c.DeleteDevicesAsync(It.IsAny<IList<IQueueItem<DeviceDeleteRequest>>>()), Times.Once);

            // AnaheimIdAdapter is never called
            this.mockVortexDeviceDeleteService.Verify(
                c => c.SendAnaheimDeviceDeleteIdRequestAsync(It.IsAny<DeviceDeleteRequest>()),
                Times.Exactly(this.mockQueueItems.Count));
        }

        [TestMethod]
        public async Task ShouldReturnFalseIfExceptionOccurs()
        {
            this.mockAppConfiguration.Setup(c => c.GetConfigValue(ConfigNames.PXS.VortexDeviceDeleteWorker_EnableDequeuing, It.IsAny<bool>())).Returns(true);
            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.PXS.DeleteDeviceRequestEnabled, It.IsAny<bool>())).ReturnsAsync(true);
            this.mockAppConfiguration.Setup(c => c.GetConfigValue<int>(ConfigNames.PXS.DeviceDeleteMaxVisibilityTimeoutInMinutes, It.IsAny<int>())).Returns(1);

            this.mockQueue.Setup(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).Throws(new TaskCanceledException("This task got canceled."));
            var queueProcessor = new VortexDeviceDeleteQueueProcessor(
                this.logger,
                this.configuration.Object,
                this.mockQueue.Object,
                this.mockVortexDeviceDeleteService.Object,
                this.mockCounterFactory.Object,
                this.mockAppConfiguration.Object);

            Assert.IsFalse(await queueProcessor.DoWorkAsync().ConfigureAwait(false));
            this.mockQueue.Verify(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

            foreach (Mock<IQueueItem<DeviceDeleteRequest>> mockQueueItem in this.mockQueueItems)
            {
                mockQueueItem.Verify(c => c.CompleteAsync(), Times.Never);
            }

            this.mockVortexDeviceDeleteService.Verify(
                c => c.DeleteDevicesAsync(It.IsAny<IList<IQueueItem<DeviceDeleteRequest>>>()),
                Times.Never);

            // AnaheimIdAdapter is never called
            this.mockVortexDeviceDeleteService.Verify(
                c => c.SendAnaheimDeviceDeleteIdRequestAsync(It.IsAny<DeviceDeleteRequest>()),
                Times.Never);
        }

        [TestMethod]
        public async Task ShouldAllowTrafficWhenThrottlingFlagReturnsTrue()
        {
            this.mockAppConfiguration.Setup(c => c.GetConfigValue(ConfigNames.PXS.VortexDeviceDeleteWorker_EnableDequeuing, It.IsAny<bool>())).Returns(true);
            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.PXS.DeleteDeviceRequestEnabled, It.IsAny<bool>())).ReturnsAsync(true);
            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.PXS.AnaheimIdEventsPublishEnabled, It.IsAny<bool>())).ReturnsAsync(true);
            this.mockAppConfiguration.Setup(c => c.GetConfigValue<int>(ConfigNames.PXS.DeviceDeleteMaxVisibilityTimeoutInMinutes, It.IsAny<int>())).Returns(1);

            var mockQueueItem = new Mock<IQueueItem<DeviceDeleteRequest>>(MockBehavior.Strict);
            mockQueueItem.Setup(c => c.CompleteAsync()).Returns(Task.CompletedTask);
            mockQueueItem.Setup(c => c.Data).Returns(new DeviceDeleteRequest
            {
                RequestInformation = new VortexRequestInformation
                {
                    RequestTime = DateTimeOffset.UtcNow
                }
            });

            mockQueueItem.Setup(c => c.RenewLeaseAsync(It.IsAny<TimeSpan>())).ReturnsAsync(true);
            mockQueueItem.Setup(c => c.InsertionTime).Returns(DateTimeOffset.MinValue);

            var DeleteDevicesAsyncReturnValue = new List<ServiceResponse<IQueueItem<DeviceDeleteRequest>>> { new ServiceResponse<IQueueItem<DeviceDeleteRequest>> { Result = mockQueueItem.Object } };
            this.mockVortexDeviceDeleteService.Setup(v => v.DeleteDevicesAsync(It.IsAny<IEnumerable<IQueueItem<DeviceDeleteRequest>>>())).ReturnsAsync(DeleteDevicesAsyncReturnValue);

            this.mockQueue.Setup(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<IQueueItem<DeviceDeleteRequest>> { mockQueueItem.Object });
            var queueProcessor = new VortexDeviceDeleteQueueProcessor(
                this.logger,
                this.configuration.Object,
                this.mockQueue.Object,
                this.mockVortexDeviceDeleteService.Object,
                this.mockCounterFactory.Object,
                this.mockAppConfiguration.Object);

            Assert.IsTrue(await queueProcessor.DoWorkAsync().ConfigureAwait(false));
            this.mockQueue.Verify(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

            // PCF is called when throttling feature flag DeleteDeviceRequestEnabled returns true
            this.mockVortexDeviceDeleteService.Verify(
                c => c.DeleteDevicesAsync(It.IsAny<IEnumerable<IQueueItem<DeviceDeleteRequest>>>()),
                Times.Once);

            // AnaheimIdAdapter is called when throttling feature flag DeleteDeviceRequestEnabled returns true
            this.mockVortexDeviceDeleteService.Verify(
                c => c.SendAnaheimDeviceDeleteIdRequestAsync(It.IsAny<DeviceDeleteRequest>()),
                Times.Once);
        }

        [TestMethod]
        public async Task ShouldBlockTrafficWhenDeleteDeviceRequestDisabled()
        {
            this.mockAppConfiguration.Setup(c => c.GetConfigValue(ConfigNames.PXS.VortexDeviceDeleteWorker_EnableDequeuing, It.IsAny<bool>())).Returns(true);
            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.PXS.DeleteDeviceRequestEnabled, It.IsAny<bool>())).ReturnsAsync(false);
            this.mockAppConfiguration.Setup(c => c.GetConfigValue<int>(ConfigNames.PXS.DeviceDeleteMaxVisibilityTimeoutInMinutes, It.IsAny<int>())).Returns(1);

            var mockQueueItem = new Mock<IQueueItem<DeviceDeleteRequest>>();
            mockQueueItem.Setup(c => c.CompleteAsync()).Returns(Task.CompletedTask);
            mockQueueItem.Setup(c => c.Data).Returns(new DeviceDeleteRequest
            {
                RequestInformation = new VortexRequestInformation
                {
                    RequestTime = DateTimeOffset.UtcNow
                }
            });
            mockQueueItem.Setup(c => c.RenewLeaseAsync(It.IsAny<TimeSpan>())).ReturnsAsync(true);
            mockQueueItem.Setup(c => c.DequeueCount).Returns(0);

            this.mockQueue.Setup(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<IQueueItem<DeviceDeleteRequest>> { mockQueueItem.Object });
            var queueProcessor = new VortexDeviceDeleteQueueProcessor(
                this.logger,
                this.configuration.Object,
                this.mockQueue.Object,
                this.mockVortexDeviceDeleteService.Object,
                this.mockCounterFactory.Object,
                this.mockAppConfiguration.Object);

            Assert.IsTrue(await queueProcessor.DoWorkAsync().ConfigureAwait(false));
            this.mockQueue.Verify(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

            // PCF is never called when throttling feature flag DeleteDeviceRequestEnabled returns true
            this.mockVortexDeviceDeleteService.Verify(
                c => c.DeleteDevicesAsync(It.IsAny<IList<IQueueItem<DeviceDeleteRequest>>>()),
                Times.Never);

            // AnaheimIdAdapter is never called when throttling feature flag DeleteDeviceRequestEnabled returns true
            this.mockVortexDeviceDeleteService.Verify(
                c => c.SendAnaheimDeviceDeleteIdRequestAsync(It.IsAny<DeviceDeleteRequest>()),
                Times.Never);

            // Should extend lease of existing item
            mockQueueItem.Verify(c => c.RenewLeaseAsync(It.IsAny<TimeSpan>()), Times.Once);
        }

        [TestMethod]
        public void StartSuccess()
        {
            var queueProcessor = new VortexDeviceDeleteQueueProcessor(
                this.logger,
                this.configuration.Object,
                this.mockQueue.Object,
                this.mockVortexDeviceDeleteService.Object,
                this.mockCounterFactory.Object,
                this.mockAppConfiguration.Object);

            queueProcessor.Start();
        }

        [TestInitialize]
        public void TestInit()
        {
            this.configuration = new Mock<IVortexDeviceDeleteQueueProccessorConfiguration>(MockBehavior.Strict);
            this.configuration.SetupGet(c => c.WaitOnQueueEmptyMilliseconds).Returns(5000);

            this.mockQueue = new Mock<IVortexDeviceDeleteQueueManager>(MockBehavior.Strict);
            this.mockQueue.Setup(q => q.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<IQueueItem<DeviceDeleteRequest>>());
            this.mockQueue.Setup(q => q.EnqueueAsync(It.IsAny<DeviceDeleteRequest>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            this.mockVortexDeviceDeleteService = new Mock<IVortexEventService>(MockBehavior.Strict);
            this.mockVortexDeviceDeleteService.Setup(v => v.SendAnaheimDeviceDeleteIdRequestAsync(It.IsAny<DeviceDeleteRequest>())).ReturnsAsync(true);

            this.mockAppConfiguration = new Mock<IAppConfiguration>();

            this.mockCounter = new Mock<ICounter>(MockBehavior.Strict);

            this.mockCounter.Setup(c => c.SetValue(It.IsAny<ulong>()));
            this.mockCounter.Setup(c => c.Increment());
            this.mockCounter.Setup(c => c.IncrementBy(It.IsAny<ulong>()));

            this.mockCounterFactory = new Mock<ICounterFactory>(MockBehavior.Strict);

            this.mockCounterFactory
                .Setup(c => c.GetCounter(CounterCategoryNames.AzureQueue, It.IsAny<string>(), CounterType.Number))
                .Returns(this.mockCounter.Object);

            this.mockCounterFactory
                .Setup(c => c.GetCounter(CounterCategoryNames.VortexDeviceDelete, It.IsAny<string>(), CounterType.Rate))
                .Returns(this.mockCounter.Object);

            this.mockQueueItems = new List<Mock<IQueueItem<DeviceDeleteRequest>>>();

            for (int i = 0; i < 5; i++)
            {
                var mockQueueItem = new Mock<IQueueItem<DeviceDeleteRequest>>(MockBehavior.Strict);
                mockQueueItem.Setup(c => c.CompleteAsync()).Returns(Task.CompletedTask);
                mockQueueItem.Setup(c => c.RenewLeaseAsync(It.IsAny<TimeSpan>())).ReturnsAsync(true);
                mockQueueItem.Setup(c => c.Data).Returns(new DeviceDeleteRequest { 
                    RequestId = Guid.NewGuid(),
                    RequestInformation = new VortexRequestInformation
                    {
                        RequestTime = DateTimeOffset.UtcNow
                    }
                });
                mockQueueItem.Setup(c => c.InsertionTime).Returns(DateTimeOffset.UtcNow.AddHours(-this.random.Next(7 * 24)));
                this.mockQueueItems.Add(mockQueueItem);
            }

            this.serviceResponses = new List<ServiceResponse<IQueueItem<DeviceDeleteRequest>>>();

            foreach (Mock<IQueueItem<DeviceDeleteRequest>> mockQueueItem in this.mockQueueItems)
            {
                this.serviceResponses.Add(new ServiceResponse<IQueueItem<DeviceDeleteRequest>> { Result = mockQueueItem.Object });
            }

            this.mockVortexDeviceDeleteService
                .Setup(c => c.DeleteDevicesAsync(It.IsAny<IList<IQueueItem<DeviceDeleteRequest>>>()))
                .ReturnsAsync(this.serviceResponses);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        [DynamicData(nameof(VortexDeviceDeleteQueueProcessorConstructorTestData), DynamicDataSourceType.Method)]
        public void VortexDeviceDeleteQueueProcessorNullHandlingSuccess(
            ILogger logger,
            IVortexDeviceDeleteQueueProccessorConfiguration configuration,
            IVortexDeviceDeleteQueueManager queueManager,
            IVortexEventService vortexService,
            ICounterFactory counterFactory,
            IAppConfiguration appConfiguration)
        {
            //Act
            new VortexDeviceDeleteQueueProcessor(logger, configuration, queueManager, vortexService, counterFactory, appConfiguration);
        }

        #region Test Data

        private static Mock<IVortexDeviceDeleteQueueProccessorConfiguration> CreateMockVortexDeviceDeleteQueueProccessorConfiguration()
        {
            var mockVortexDeviceDeleteQueueProccessor = new Mock<IVortexDeviceDeleteQueueProccessorConfiguration>();
            mockVortexDeviceDeleteQueueProccessor.SetupGet(c => c.WaitOnQueueEmptyMilliseconds).Returns(5000);
            return mockVortexDeviceDeleteQueueProccessor;
        }

        public static IEnumerable<object[]> VortexDeviceDeleteQueueProcessorConstructorTestData()
        {
            var mockLogger = new ConsoleLogger();
            Mock<IVortexDeviceDeleteQueueProccessorConfiguration> mockConfiguration = CreateMockVortexDeviceDeleteQueueProccessorConfiguration();
            var mockQueueManager = new Mock<IVortexDeviceDeleteQueueManager>(MockBehavior.Strict);
            var mockVortexService = new Mock<IVortexEventService>(MockBehavior.Strict);
            Mock<ICounterFactory> mockCounterFactory = CreateMockCounterFactory();
            var mockAppConfiguration = new Mock<IAppConfiguration>();
            mockAppConfiguration.Setup(c => c.GetConfigValue<int>(ConfigNames.PXS.DeviceDeleteMaxVisibilityTimeoutInMinutes, It.IsAny<int>())).Returns(1);


            var data = new List<object[]>
            {
                new object[]
                {
                    null,
                    mockConfiguration.Object,
                    mockQueueManager.Object,
                    mockVortexService.Object,
                    mockCounterFactory.Object,
                    mockAppConfiguration.Object,
                },
                new object[]
                {
                    mockLogger,
                    mockConfiguration.Object,
                    null,
                    mockVortexService.Object,
                    mockCounterFactory.Object,
                    mockAppConfiguration.Object,
                },
                new object[]
                {
                    mockLogger,
                    mockConfiguration.Object,
                    mockQueueManager.Object,
                    null,
                    mockCounterFactory.Object,
                    mockAppConfiguration.Object,
                },
                new object[]
                {
                    mockLogger,
                    mockConfiguration.Object,
                    mockQueueManager.Object,
                    mockVortexService.Object,
                    null,
                    mockAppConfiguration.Object,
                },
                new object[]
                {
                    mockLogger,
                    mockConfiguration.Object,
                    mockQueueManager.Object,
                    mockVortexService.Object,
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

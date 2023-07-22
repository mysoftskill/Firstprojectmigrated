// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.VortexDeviceDeleteWorker.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.ComplianceServices.AnaheimIdLib.Schema;
    using Microsoft.ComplianceServices.Common.Queues;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.Privacy.VortexDeviceDeleteWorker.QueueProcessor;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class AnaheimIdQueueWorkerTest
    {
        private readonly ILogger logger = new ConsoleLogger();

        private Mock<ICloudQueue<AnaheimIdRequest>> mockQueue;

        private Mock<ICloudQueueItem<AnaheimIdRequest>> mockQueueItem;

        private Mock<IAppConfiguration> mockAppConfiguration;

        private Mock<IPcfAdapter> mockPcfAdapter;

        private Mock<IVortexDeviceDeleteQueueProccessorConfiguration> mockWorkerConfiguration;

        [TestMethod]
        public async Task ShouldNotProcessWhenWorkerDisabled()
        {
            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.PXS.AnaheimIdQueueWorker_Enabled, It.IsAny<bool>())).ReturnsAsync(false);

            var queueWorker = new AnaheimIdQueueWorker(
                this.mockQueue.Object,
                this.mockPcfAdapter.Object,
                this.mockWorkerConfiguration.Object,
                this.mockAppConfiguration.Object,
                this.logger);

            Assert.IsFalse(await queueWorker.DoWorkAsync().ConfigureAwait(false));
        }

        [TestMethod]
        public async Task ShouldNotContinueThrottleIfNoMessageFound()
        {
            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.PXS.AnaheimIdQueueWorker_Enabled, It.IsAny<bool>())).ReturnsAsync(true);

            this.mockQueue.Setup(c => c.DequeueBatchAsync(It.IsAny<TimeSpan>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<ICloudQueueItem<AnaheimIdRequest>>());

            var queueWorker = new AnaheimIdQueueWorker(
                this.mockQueue.Object,
                this.mockPcfAdapter.Object,
                this.mockWorkerConfiguration.Object,
                this.mockAppConfiguration.Object,
                this.logger);

            Assert.IsTrue(await queueWorker.DoWorkAsync().ConfigureAwait(false));

            this.mockAppConfiguration.Verify(c => c.IsFeatureFlagEnabledAsync(FeatureNames.PXS.AnaheimIdRequestToPcfEnabled, false), Times.Never);
        }

        [TestMethod]
        public async Task ShouldSendToPcfIfMessagesAreAllowed()
        {
            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.PXS.AnaheimIdQueueWorker_Enabled, It.IsAny<bool>())).ReturnsAsync(true);
            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.PXS.AnaheimIdRequestToPcfEnabled, It.IsAny<bool>())).ReturnsAsync(true);

            this.mockQueueItem.Setup(c => c.DeleteAsync()).Returns(Task.CompletedTask);

            this.mockQueue.Setup(c => c.DequeueBatchAsync(It.IsAny<TimeSpan>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<ICloudQueueItem<AnaheimIdRequest>> { mockQueueItem.Object });

            var queueWorker = new AnaheimIdQueueWorker(
                this.mockQueue.Object,
                this.mockPcfAdapter.Object,
                this.mockWorkerConfiguration.Object,
                this.mockAppConfiguration.Object,
                this.logger);

            Assert.IsTrue(await queueWorker.DoWorkAsync().ConfigureAwait(false));

            // Call to send to PCF
            this.mockPcfAdapter.Verify(c => c.PostCommandsAsync(It.IsAny<IList<PrivacyRequest>>()), Times.Once);

            // Will delete message
            mockQueueItem.Verify(c => c.DeleteAsync(), Times.Once);
        }

        [TestMethod]
        public async Task ShouldRenewLeaseIfMessagesAreThrottled()
        {
            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.PXS.AnaheimIdQueueWorker_Enabled, It.IsAny<bool>())).ReturnsAsync(true);
            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.PXS.AnaheimIdRequestToPcfEnabled, It.IsAny<bool>())).ReturnsAsync(false);

            this.mockQueueItem.Setup(c => c.UpdateAsync(It.IsAny<TimeSpan>())).Returns(Task.CompletedTask);

            this.mockQueue.Setup(c => c.DequeueBatchAsync(It.IsAny<TimeSpan>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<ICloudQueueItem<AnaheimIdRequest>> { mockQueueItem.Object });

            var queueWorker = new AnaheimIdQueueWorker(
                this.mockQueue.Object,
                this.mockPcfAdapter.Object,
                this.mockWorkerConfiguration.Object,
                this.mockAppConfiguration.Object,
                this.logger);

            Assert.IsTrue(await queueWorker.DoWorkAsync().ConfigureAwait(false));

            // Call to renew message lease
            mockQueueItem.Verify(c => c.UpdateAsync(It.IsAny<TimeSpan>()), Times.Once);

            //  Won't delete message
            mockQueueItem.Verify(c => c.DeleteAsync(), Times.Never);

            // Never calls PcfAdapter
            this.mockPcfAdapter.Verify(c => c.PostCommandsAsync(It.IsAny<IList<PrivacyRequest>>()), Times.Never);
        }

        [TestMethod]
        public async Task ShouldThrowExceptionIfFailsToGetMessage()
        {
            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.PXS.AnaheimIdQueueWorker_Enabled, It.IsAny<bool>())).ReturnsAsync(true);
            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.PXS.AnaheimIdRequestToPcfEnabled, It.IsAny<bool>())).ReturnsAsync(true);

            this.mockQueue.Setup(c => c.DequeueBatchAsync(It.IsAny<TimeSpan>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).Throws(new Exception("Can't get messages."));

            var queueWorker = new AnaheimIdQueueWorker(
                this.mockQueue.Object,
                this.mockPcfAdapter.Object,
                this.mockWorkerConfiguration.Object,
                this.mockAppConfiguration.Object,
                this.logger);

            Assert.IsFalse(await queueWorker.DoWorkAsync().ConfigureAwait(false));
        }

        [TestMethod]
        public async Task ShouldContinueProcessIfFailsToDeleteMessage()
        {
            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.PXS.AnaheimIdQueueWorker_Enabled, It.IsAny<bool>())).ReturnsAsync(true);
            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.PXS.AnaheimIdRequestToPcfEnabled, It.IsAny<bool>())).ReturnsAsync(true);

            this.mockQueueItem.Setup(c => c.DeleteAsync()).Throws(new Exception("Can't Delete this message"));
            var anotherMockQueueItem = new Mock<ICloudQueueItem<AnaheimIdRequest>>();
            anotherMockQueueItem.Setup(c => c.Data).Returns(
                new AnaheimIdRequest
                {
                    AnaheimIds = new long[] { 1 },
                    DeleteDeviceIdRequest = new DeleteDeviceIdRequest
                    {
                        AuthorizationId = "AuthorizationId",
                        CorrelationVector = "CorrelationVector",
                        GlobalDeviceId = 123456,
                        RequestId = Guid.NewGuid(),
                        CreateTime = DateTimeOffset.UtcNow,
                        TestSignal = true
                    }
                });
            anotherMockQueueItem.Setup(c => c.DeleteAsync()).Returns(Task.CompletedTask);

            this.mockQueue.Setup(c => c.DequeueBatchAsync(It.IsAny<TimeSpan>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<ICloudQueueItem<AnaheimIdRequest>> { this.mockQueueItem.Object, anotherMockQueueItem.Object });

            var queueWorker = new AnaheimIdQueueWorker(
                this.mockQueue.Object,
                this.mockPcfAdapter.Object,
                this.mockWorkerConfiguration.Object,
                this.mockAppConfiguration.Object,
                this.logger);

            Assert.IsTrue(await queueWorker.DoWorkAsync().ConfigureAwait(false));

            anotherMockQueueItem.Verify(c => c.DeleteAsync(), Times.Once);
        }

        [TestMethod]
        public async Task ShouldContinueProcessIfFailsToRenewMessage()
        {
            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.PXS.AnaheimIdQueueWorker_Enabled, It.IsAny<bool>())).ReturnsAsync(true);
            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.PXS.AnaheimIdRequestToPcfEnabled, It.IsAny<bool>())).ReturnsAsync(false);

            this.mockQueueItem.Setup(c => c.UpdateAsync(It.IsAny<TimeSpan>())).Throws(new Exception("Can't Renew this message"));
            var anotherMockQueueItem = new Mock<ICloudQueueItem<AnaheimIdRequest>>();
            anotherMockQueueItem.Setup(c => c.Data).Returns(
                new AnaheimIdRequest
                {
                    AnaheimIds = new long[] { 1 },
                    DeleteDeviceIdRequest = new DeleteDeviceIdRequest
                    {
                        AuthorizationId = "AuthorizationId",
                        CorrelationVector = "CorrelationVector",
                        GlobalDeviceId = 123456,
                        RequestId = Guid.NewGuid(),
                        CreateTime = DateTimeOffset.UtcNow,
                        TestSignal = true
                    }
                });
            anotherMockQueueItem.Setup(c => c.UpdateAsync(It.IsAny<TimeSpan>())).Returns(Task.CompletedTask);

            this.mockQueue.Setup(c => c.DequeueBatchAsync(It.IsAny<TimeSpan>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<ICloudQueueItem<AnaheimIdRequest>> { this.mockQueueItem.Object, anotherMockQueueItem.Object });

            var queueWorker = new AnaheimIdQueueWorker(
                this.mockQueue.Object,
                this.mockPcfAdapter.Object,
                this.mockWorkerConfiguration.Object,
                this.mockAppConfiguration.Object,
                this.logger);

            Assert.IsTrue(await queueWorker.DoWorkAsync().ConfigureAwait(false));

            anotherMockQueueItem.Verify(c => c.UpdateAsync(It.IsAny<TimeSpan>()), Times.Once);
        }

        [TestMethod]
        public void ShouldUpdateApiEventCorrectly()
        {
            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.PXS.AnaheimIdQueueWorker_Enabled, It.IsAny<bool>())).ReturnsAsync(true);
            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.PXS.AnaheimIdRequestToPcfEnabled, It.IsAny<bool>())).ReturnsAsync(true);

            OutgoingApiEventWrapper apiEvent = new OutgoingApiEventWrapper
            {
                DependencyOperationVersion = string.Empty,
                DependencyOperationName = "OperationName",
                DependencyName = "DependencyName",
                DependencyType = "DependencyType",
                PartnerId = "PartnerId",
                Success = false,
            };
            apiEvent.Start();

            var aRequestId = Guid.NewGuid();
            var aMockQueueItem = new Mock<ICloudQueueItem<AnaheimIdRequest>>();
            aMockQueueItem.Setup(c => c.Data).Returns(
                new AnaheimIdRequest
                {
                    AnaheimIds = new long[] { 1, 2, 3 },
                    DeleteDeviceIdRequest = new DeleteDeviceIdRequest
                    {
                        AuthorizationId = "AuthorizationId",
                        CorrelationVector = "CorrelationVector",
                        GlobalDeviceId = 123456,
                        RequestId = aRequestId,
                        CreateTime = DateTimeOffset.UtcNow,
                        TestSignal = true
                    }
                });

            aMockQueueItem.Setup(c => c.DeleteAsync()).Returns(Task.CompletedTask);
            this.mockQueue.Setup(c => c.DequeueBatchAsync(It.IsAny<TimeSpan>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<ICloudQueueItem<AnaheimIdRequest>> { aMockQueueItem.Object });

            var queueWorker = new AnaheimIdQueueWorker(
                this.mockQueue.Object,
                this.mockPcfAdapter.Object,
                this.mockWorkerConfiguration.Object,
                this.mockAppConfiguration.Object,
                this.logger);

            queueWorker.DeleteMessagesAsync(apiEvent, new List<ICloudQueueItem<AnaheimIdRequest>> { aMockQueueItem.Object }).Wait();

            Assert.IsTrue(apiEvent.ExtraData["CommandIds"] != String.Empty);
            Assert.AreEqual($"{aRequestId},", apiEvent.ExtraData["CommandIds"]);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        [DynamicData(nameof(AnaheimIdQueueWorkerConstructorTestData), DynamicDataSourceType.Method)]
        public void AnaheimIdQueueWorkerNullHandlingSuccess(
            ICloudQueue<AnaheimIdRequest> cloudQueue
            , IPcfAdapter pcfAdapter
            , IVortexDeviceDeleteQueueProccessorConfiguration vortexDeviceDeleteQueueProccessorConfiguration
            , IAppConfiguration appConfiguration
            , ILogger logger)
        {
            new AnaheimIdQueueWorker(cloudQueue, pcfAdapter, vortexDeviceDeleteQueueProccessorConfiguration, appConfiguration, logger);
        }

        [TestInitialize]
        public void TestInit()
        {
            this.mockQueueItem = new Mock<ICloudQueueItem<AnaheimIdRequest>>();
            this.mockQueueItem.Setup(c => c.Data).Returns(
                new AnaheimIdRequest
                {
                    AnaheimIds = new long[] { 1, 2, 3 },
                    DeleteDeviceIdRequest = new DeleteDeviceIdRequest
                    {
                        AuthorizationId = "AuthorizationId",
                        CorrelationVector = "CorrelationVector",
                        GlobalDeviceId = 123456,
                        RequestId = Guid.NewGuid(),
                        CreateTime = DateTimeOffset.UtcNow,
                        TestSignal = true
                    }
                });

            this.mockQueue = new Mock<ICloudQueue<AnaheimIdRequest>>();

            this.mockWorkerConfiguration = new Mock<IVortexDeviceDeleteQueueProccessorConfiguration>();
            this.mockAppConfiguration = new Mock<IAppConfiguration>();

            this.mockPcfAdapter = new Mock<IPcfAdapter>();
            this.mockPcfAdapter.Setup(c => c.PostCommandsAsync(It.IsAny<IList<PrivacyRequest>>())).ReturnsAsync(new AdapterResponse());
        }

        public static IEnumerable<object[]> AnaheimIdQueueWorkerConstructorTestData()
        {
            var mockCloudQueue = new Mock<ICloudQueue<AnaheimIdRequest>>();
            var mockPcfAdapter = new Mock<IPcfAdapter>();
            var mockWorkerConfiguration = new Mock<IVortexDeviceDeleteQueueProccessorConfiguration>();
            var mockAppConfiguration = new Mock<IAppConfiguration>();
            var mockLogger = new ConsoleLogger();

            var data = new List<object[]>
            {
                new object[]
                {
                    null,
                    mockPcfAdapter.Object,
                    mockWorkerConfiguration.Object,
                    mockAppConfiguration.Object,
                    mockLogger
                },
                new object[]
                {
                    mockCloudQueue.Object,
                    null,
                    mockWorkerConfiguration.Object,
                    mockAppConfiguration.Object,
                    mockLogger
                },
                new object[]
                {
                    mockCloudQueue.Object,
                    mockPcfAdapter.Object,
                    null,
                    mockAppConfiguration.Object,
                    mockLogger
                },
                new object[]
                {
                    mockCloudQueue.Object,
                    mockPcfAdapter.Object,
                    mockWorkerConfiguration.Object,
                    null,   
                    mockLogger
                },
                new object[]
                {
                    mockCloudQueue.Object,
                    mockPcfAdapter.Object,
                    mockWorkerConfiguration.Object,
                    mockAppConfiguration.Object,
                    null
                }
            };
            return data;
        }
    }
}

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
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.Privacy.Core.Vortex;
    using Microsoft.Membership.MemberServices.Privacy.VortexDeviceDeleteWorker.QueueProcessor;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class VortexDeviceDeleteQueueProcessorCollectionTests : TestBase
    {
        private readonly ILogger logger = new ConsoleLogger();

        private Mock<IPrivacyConfigurationManager> configuration;

        private Mock<ICounter> mockCounter;

        private Mock<ICounterFactory> mockCounterFactory;

        private Mock<IVortexDeviceDeleteQueueManager> mockQueueManager;

        private VortexDeviceDeleteQueueProcessorCollection processorCollection;

        private Mock<IVortexDeviceDeleteQueueProccessorConfiguration> queueProcessorConfig;

        private Mock<IVortexDeviceDeleteQueueProcessorFactory> queueProcessorFactory;

        private Mock<IVortexEventService> vortexEventService;

        private Mock<IAppConfiguration> mockAppConfiguration;

        [TestInitialize]
        public void Initialize()
        {
            this.queueProcessorConfig = CreateMockVortexDeviceDeleteQueueProccessorConfig();
            Mock<IVortexDeviceDeleteWorkerConfiguration> mockVortexDeviceDeleteWorkerConfiguration = CreateMockDeviceDeleteWorkerConfig(this.queueProcessorConfig);
            this.configuration = CreateMockPrivacyConfigManager(mockVortexDeviceDeleteWorkerConfiguration);
            this.mockQueueManager = this.CreateMockVortexDeviceDeleteQueueManager();
            this.mockQueueManager.Setup(qm => qm.GetMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<IQueueItem<DeviceDeleteRequest>>());
            this.vortexEventService = this.CreateMockVortexEventService();
            this.mockCounter = this.CreateMockCounter();
            this.queueProcessorFactory = new Mock<IVortexDeviceDeleteQueueProcessorFactory>();
            this.mockCounterFactory = this.CreateMockCounterFactory(this.mockCounter);
            this.mockAppConfiguration = CreateMockAppConfiguration();

            var vortexDeviceDeleteQueueProcessor = new VortexDeviceDeleteQueueProcessor(
                this.logger,
                this.queueProcessorConfig.Object,
                this.mockQueueManager.Object,
                this.vortexEventService.Object,
                this.mockCounterFactory.Object,
                this.mockAppConfiguration.Object);

            this.queueProcessorFactory
                .Setup(
                    c => c.Create(
                        this.logger,
                        this.configuration.Object,
                        this.mockQueueManager.Object,
                        this.vortexEventService.Object,
                        this.mockCounterFactory.Object,
                        this.mockAppConfiguration.Object))
                .Returns(vortexDeviceDeleteQueueProcessor);

            this.processorCollection = new VortexDeviceDeleteQueueProcessorCollection(
                this.logger,
                this.queueProcessorFactory.Object,
                this.configuration.Object,
                this.mockQueueManager.Object,
                this.vortexEventService.Object,
                this.mockCounterFactory.Object,
                this.mockAppConfiguration.Object);
        }

        [TestMethod]
        public void StartSuccess()
        {
            this.processorCollection.Start();
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(500)]
        public async Task StartSuccessWithTimeSpanDelay(double Milliseconds)
        {
            this.processorCollection.Start(TimeSpan.FromMilliseconds(Milliseconds));
            await this.processorCollection.StopAsync().ConfigureAwait(false);
        }
    }
}

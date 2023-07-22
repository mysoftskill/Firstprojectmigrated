// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.VortexDeviceDeleteWorker.UnitTests
{
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.Privacy.Core.Vortex;
    using Microsoft.PrivacyServices.Common.Azure;

    using Moq;

    public class TestBase
    {
        protected Mock<ICounter> CreateMockCounter()
        {
            var mockCounter = new Mock<ICounter>(MockBehavior.Strict);
            mockCounter.Setup(c => c.SetValue(It.IsAny<ulong>()));
            mockCounter.Setup(c => c.Increment());
            mockCounter.Setup(c => c.IncrementBy(It.IsAny<ulong>()));
            return mockCounter;
        }

        protected Mock<ICounterFactory> CreateMockCounterFactory(Mock<ICounter> mockCounter)
        {
            var mockCounterFactory = new Mock<ICounterFactory>(MockBehavior.Strict);
            mockCounterFactory.Setup(c => c.GetCounter(CounterCategoryNames.VortexDeviceDelete, It.IsAny<string>(), CounterType.Number))
                .Returns(mockCounter.Object);
            return mockCounterFactory;
        }

        protected Mock<ICounterFactory> CreateMockCounterFactory()
        {
            return this.CreateMockCounterFactory(new Mock<ICounter>());
        }

        protected Mock<ILogger> CreateMockGenevaLogger()
        {
            return new Mock<ILogger>();
        }

        protected Mock<IVortexDeviceDeleteQueueManager> CreateMockVortexDeviceDeleteQueueManager()
        {
            return new Mock<IVortexDeviceDeleteQueueManager>(MockBehavior.Strict);
        }

        protected Mock<IVortexEventService> CreateMockVortexEventService()
        {
            return new Mock<IVortexEventService>(MockBehavior.Strict);
        }

        protected static Mock<IVortexDeviceDeleteWorkerConfiguration> CreateMockDeviceDeleteWorkerConfig(
            Mock<IVortexDeviceDeleteQueueProccessorConfiguration> mockQueueProcessorConfig)
        {
            var mockVortexDeviceDeleteWorkerConfiguration = new Mock<IVortexDeviceDeleteWorkerConfiguration>();
            mockVortexDeviceDeleteWorkerConfiguration
                .Setup(c => c.QueueProccessorConfig)
                .Returns(mockQueueProcessorConfig.Object);

            return mockVortexDeviceDeleteWorkerConfiguration;
        }

        protected static Mock<IPrivacyConfigurationManager> CreateMockPrivacyConfigManager(Mock<IVortexDeviceDeleteWorkerConfiguration> mockVortexDeviceDeleteWorkerConfiguration)
        {
            var configuration = new Mock<IPrivacyConfigurationManager>(MockBehavior.Strict);
            configuration
                .Setup(c => c.VortexDeviceDeleteWorkerConfiguration)
                .Returns(mockVortexDeviceDeleteWorkerConfiguration.Object);

            return configuration;
        }

        protected static Mock<IPrivacyConfigurationManager> CreateMockPrivacyConfigManager()
        {
            var configuration = new Mock<IPrivacyConfigurationManager>(MockBehavior.Strict);
            var mockQueueProccessorConfig = CreateMockVortexDeviceDeleteQueueProccessorConfig();
            var mockDeleteWorkerConfig = CreateMockDeviceDeleteWorkerConfig(mockQueueProccessorConfig);
            configuration
                .Setup(c => c.VortexDeviceDeleteWorkerConfiguration)
                .Returns(mockDeleteWorkerConfig.Object);

            return configuration;
        }

        protected static Mock<IVortexDeviceDeleteQueueProccessorConfiguration> CreateMockVortexDeviceDeleteQueueProccessorConfig(
            int processorCount = 1,
            int MaxDequeueCountToDeadLetter = 5)
        {
            var queueProcessorConfig = new Mock<IVortexDeviceDeleteQueueProccessorConfiguration>();
            queueProcessorConfig.SetupGet(c => c.ProcessorCount).Returns(processorCount);
            queueProcessorConfig.SetupGet(c => c.WaitOnQueueEmptyMilliseconds).Returns(5000);

            return queueProcessorConfig;
        }

        protected static Mock<IAppConfiguration> CreateMockAppConfiguration()
        {
            var appConfig = new Mock<IAppConfiguration>();

            return appConfig;
        }
    }
}

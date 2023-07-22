// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.VortexDeviceDeleteWorker.UnitTests
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.Privacy.Core.Vortex;
    using Microsoft.Membership.MemberServices.Privacy.VortexDeviceDeleteWorker.QueueProcessor;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.PrivacyServices.Common.Azure;

    using Moq;

    [TestClass]
    public class VortexDeviceDeleteQueueProcessorFactoryTests : TestBase
    {
        private readonly ILogger logger = new ConsoleLogger();

        private VortexDeviceDeleteQueueProcessorFactory factory;

        private Mock<IPrivacyConfigurationManager> mockConfiguration;

        private Mock<ICounterFactory> mockCounterFactory;

        private Mock<IVortexEventService> mockEventService;

        private Mock<IVortexDeviceDeleteQueueManager> mockQueueManager;

        private Mock<IAppConfiguration> mockAppConfiguration;

        public static IEnumerable<object[]> CreateMockPrivacyConfigManagerTestdata()
        {
            var config1 = CreateMockPrivacyConfigManager();
            config1
                .Setup(c => c.VortexDeviceDeleteWorkerConfiguration)
                .Returns(new Mock<IVortexDeviceDeleteWorkerConfiguration>().Object);

            var config2 = CreateMockPrivacyConfigManager();
            config2
                .Setup(c => c.VortexDeviceDeleteWorkerConfiguration)
                .Returns(It.IsAny<IVortexDeviceDeleteWorkerConfiguration>());

            var data = new List<object[]>
            {
                new object[] { null },
                new object[] { config1.Object },
                new object[] { config2.Object }
            };
            return data;
        }

        [TestMethod]
        [DynamicData(nameof(CreateMockPrivacyConfigManagerTestdata), DynamicDataSourceType.Method)]
        public void CreateVortexDeviceDeleteQueueProcessorFactoryExpectedException(IPrivacyConfigurationManager config)
        {
            try
            {
                //Act
                this.factory.Create(
                    this.logger,
                    config,
                    this.mockQueueManager.Object,
                    this.mockEventService.Object,
                    this.mockCounterFactory.Object,
                    this.mockAppConfiguration.Object);
                Assert.Fail("should not get here");
            }
            catch (ArgumentNullException e)
            {
                Assert.AreEqual("Value cannot be null.\r\nParameter name: QueueProccessorConfig", e.Message);
            }
        }

        [TestMethod]
        public void CreateVortexDeviceDeleteQueueProcessorFactorySuccess()
        {
            //Act
            this.factory.Create(
                this.logger,
                this.mockConfiguration.Object,
                this.mockQueueManager.Object,
                this.mockEventService.Object,
                this.mockCounterFactory.Object,
                this.mockAppConfiguration.Object);
        }

        [TestInitialize]
        public void Init()
        {
            this.mockConfiguration = CreateMockPrivacyConfigManager();
            this.mockQueueManager = this.CreateMockVortexDeviceDeleteQueueManager();
            this.mockCounterFactory = this.CreateMockCounterFactory();
            this.mockEventService = this.CreateMockVortexEventService();
            this.mockAppConfiguration = CreateMockAppConfiguration();

            this.factory = new VortexDeviceDeleteQueueProcessorFactory();
        }
    }
}

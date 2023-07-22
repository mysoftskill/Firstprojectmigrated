// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.UnitTests.QueueProcessor
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.QueueProcessor;
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.TableStorage;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAccountClose;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class AadAccountCloseQueueProcessorFactoryTest : SharedTestFunctions
    {
        private AadAccountCloseQueueProcessorFactory aadAccountCloseQueueProcessorFactory;

        private Mock<IAadAccountCloseService> mockAadAccountCloseService;

        private Mock<IAccountCloseQueueManager> mockAccountCloseQueueManager;

        private Mock<ICounterFactory> mockCounterFactory;

        private Mock<ILogger> mockLogger;

        private Mock<ITable<AccountCloseDeadLetterStorage>> mockTableOfAccountCloseDeadLetterStorage;

        private Mock<IAppConfiguration> mockAppConfiguration;

        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        [DynamicData(nameof(CreateAadAccountCloseQueueProcessorFactoryTestData), DynamicDataSourceType.Method)]
        public void CreateExceptionHandling(IPrivacyConfigurationManager configuration)
        {
            this.aadAccountCloseQueueProcessorFactory.Create(
                this.mockLogger.Object,
                configuration,
                this.mockAccountCloseQueueManager.Object,
                this.mockAadAccountCloseService.Object,
                this.mockTableOfAccountCloseDeadLetterStorage.Object,
                this.mockCounterFactory.Object,
                this.mockAppConfiguration.Object);
        }

        [TestInitialize]
        public void Initialize()
        {
            this.mockLogger = CreateMockGenevaLogger();

            this.mockAccountCloseQueueManager = CreateMockAccountCloseQueueManager();

            this.mockAadAccountCloseService = CreateMockAadAccountCloseService();

            this.mockTableOfAccountCloseDeadLetterStorage = CreateMockAccountCloseDeadLetterStorage();

            this.mockCounterFactory = CreateMockCounterFactory();

            this.mockAppConfiguration = CreateMockAppConfiguration();

            this.aadAccountCloseQueueProcessorFactory = new AadAccountCloseQueueProcessorFactory();
        }

        [TestMethod]
        public void ShouldCreateFactorySuccess()
        {
            var privacyConfg = CreateMockPrivacyConfigManager();
            var result = this.aadAccountCloseQueueProcessorFactory.Create(
                this.mockLogger.Object,
                privacyConfg.Object,
                this.mockAccountCloseQueueManager.Object,
                this.mockAadAccountCloseService.Object,
                this.mockTableOfAccountCloseDeadLetterStorage.Object,
                this.mockCounterFactory.Object,
                this.mockAppConfiguration.Object);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.GetType() == typeof(AadAccountCloseQueueProcessor));
        }

        private static IEnumerable<object[]> CreateAadAccountCloseQueueProcessorFactoryTestData()
        {
            var config1 = CreateMockPrivacyConfigManager();
            config1.Setup(c => c.AadAccountCloseWorkerConfiguration).Returns((IAadAccountCloseWorkerConfiguration)null);
            var config2 = CreateMockPrivacyConfigManager();
            config2.Setup(c => c.AadAccountCloseWorkerConfiguration.QueueProccessorConfig).Returns((IAadAccountCloseQueueProccessorConfiguration)null);

            var data = new List<object[]>
            {
                new object[] { null },
                new object[] { config1.Object },
                new object[] { config2.Object }
            };

            return data;
        }
    }
}

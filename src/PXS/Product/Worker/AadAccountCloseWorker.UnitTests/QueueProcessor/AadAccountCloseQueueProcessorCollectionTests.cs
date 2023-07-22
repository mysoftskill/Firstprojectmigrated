// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.UnitTests.QueueProcessor
{
    using System;

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
    public class AadAccountCloseQueueProcessorCollectionTests : SharedTestFunctions

    {
        private AadAccountCloseQueueProcessorCollection aadAccountCloseQueueProcessorCollection;

        private Mock<IAadAccountCloseQueueProccessorConfiguration> mockAadAccountCloseQueueProcessorConfig;

        private Mock<IAadAccountCloseService> mockAadAccountCloseService;

        private Mock<IAccountCloseQueueManager> mockAccountCloseQueueManager;

        private Mock<ICounterFactory> mockCounterFactory;

        private Mock<ILogger> mockLogger;

        private Mock<IPrivacyConfigurationManager> mockPrivacyConfMananger;

        private Mock<IAadAccountCloseQueueProcessorFactory> mockProcessorFactory;

        private Mock<ITable<AccountCloseDeadLetterStorage>> mockTableOfAccountCloseDeadLetterStorage;

        private Mock<IAppConfiguration> mockAppConfiguration;

        [TestInitialize]
        public void Initialize()
        {
            this.mockLogger = CreateMockGenevaLogger();
            this.mockAccountCloseQueueManager = CreateMockAccountCloseQueueManager();
            this.mockAadAccountCloseService = CreateMockAadAccountCloseService();
            this.mockTableOfAccountCloseDeadLetterStorage = CreateMockAccountCloseDeadLetterStorage();
            this.mockCounterFactory = CreateMockCounterFactory();
            this.mockPrivacyConfMananger = CreateMockPrivacyConfigManager();

            this.mockAadAccountCloseQueueProcessorConfig = CreateMockAadAccountCloseQueueProcessorConfig();

            this.mockAppConfiguration = CreateMockAppConfiguration();

            this.mockProcessorFactory = new Mock<IAadAccountCloseQueueProcessorFactory>();
            this.mockProcessorFactory.Setup(
                    c => c.Create(
                        this.mockLogger.Object,
                        this.mockPrivacyConfMananger.Object,
                        this.mockAccountCloseQueueManager.Object,
                        this.mockAadAccountCloseService.Object,
                        this.mockTableOfAccountCloseDeadLetterStorage.Object,
                        this.mockCounterFactory.Object,
                        this.mockAppConfiguration.Object))
                .Returns(
                    new AadAccountCloseQueueProcessor(
                        this.mockLogger.Object,
                        this.mockAadAccountCloseQueueProcessorConfig.Object,
                        this.mockAccountCloseQueueManager.Object,
                        this.mockAadAccountCloseService.Object,
                        this.mockTableOfAccountCloseDeadLetterStorage.Object,
                        this.mockCounterFactory.Object,
                        this.mockAppConfiguration.Object))
                .Verifiable();

            this.aadAccountCloseQueueProcessorCollection = new AadAccountCloseQueueProcessorCollection(
                this.mockLogger.Object,
                this.mockProcessorFactory.Object,
                this.mockPrivacyConfMananger.Object,
                this.mockAccountCloseQueueManager.Object,
                this.mockAadAccountCloseService.Object,
                this.mockTableOfAccountCloseDeadLetterStorage.Object,
                this.mockCounterFactory.Object,
                this.mockAppConfiguration.Object);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(500)]
        public void StartDelaySuccess(long milliseconds)
        {
            this.aadAccountCloseQueueProcessorCollection.Start(TimeSpan.FromMilliseconds(milliseconds));
            this.Verify();
        }

        [TestMethod]
        public void StartSuccess()
        {
            this.aadAccountCloseQueueProcessorCollection.Start();
            this.Verify();
        }

        [TestMethod]
        public void StopAsyncSuccess()
        {
            this.aadAccountCloseQueueProcessorCollection.StopAsync();
            this.Verify();
        }

        private void Verify()
        {
            var count = this.mockAadAccountCloseQueueProcessorConfig.Object.ProcessorCount;
            this.mockProcessorFactory.Verify(
                c => c.Create(
                    this.mockLogger.Object,
                    this.mockPrivacyConfMananger.Object,
                    this.mockAccountCloseQueueManager.Object,
                    this.mockAadAccountCloseService.Object,
                    this.mockTableOfAccountCloseDeadLetterStorage.Object,
                    this.mockCounterFactory.Object,
                    this.mockAppConfiguration.Object
                ),
                Times.Exactly(count));
        }
    }
}

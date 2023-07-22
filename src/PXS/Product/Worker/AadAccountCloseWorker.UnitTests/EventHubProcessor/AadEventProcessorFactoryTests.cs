// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.UnitTests.EventHubProcessor
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.EventHubProcessor;
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.QueueProcessor;
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.TableStorage;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.ServiceBus.Messaging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Microsoft.PrivacyServices.Common.Azure;

    [TestClass]
    public class AadEventProcessorFactoryTests : SharedTestFunctions
    {
        private readonly string hubId = "hubId";

        private readonly string endpoint = "https://myeventhub.com";

        private Mock<ILogger> logger;

        private Mock<IClock> mockClock;

        private Mock<ICounterFactory> mockCounterFactory;

        private Mock<ITable<NotificationDeadLetterStorage>> mockDeadLetterTable;

        private Mock<IAccountCloseQueueManager> mockQueueManager;

        private Mock<IRequestClassifier> mockRequestClassifier;

        private Mock<IAppConfiguration> mockAppConfiguration = new Mock<IAppConfiguration>(MockBehavior.Strict);

        private AadEventProcessorFactory processorFactory;

        public static IEnumerable<object[]> CreateAadEventProcessorFactoryTestData()
        {
            var mockLogger = CreateMockGenevaLogger();
            var mockAccountCloseQueueManager = CreateMockAccountCloseQueueManager();
            var mockClock = CreateMockClock();
            var mockCounterFactory = CreateMockCounterFactory();
            var config = CreateMockPrivacyConfigManager(true, true);
            var mockAppConfiguration = new Mock<IAppConfiguration>(MockBehavior.Strict);

            return new List<object[]>
            {
                new object[]
                {
                    null,
                    mockAccountCloseQueueManager.Object,
                    mockClock.Object,
                    mockCounterFactory.Object,
                    config.Object,
                    mockAppConfiguration.Object
                },
                new object[]
                {
                    mockLogger.Object,
                    null,
                    mockClock.Object,
                    mockCounterFactory.Object,
                    config.Object,
                    mockAppConfiguration.Object
                },
                new object[]
                {
                    mockLogger.Object,
                    mockAccountCloseQueueManager.Object,
                    null,
                    mockCounterFactory.Object,
                    config.Object,
                    mockAppConfiguration.Object
                },
                new object[]
                {
                    mockLogger.Object,
                    mockAccountCloseQueueManager.Object,
                    mockClock.Object,
                    null,
                    config.Object,
                    mockAppConfiguration.Object
                },
                new object[]
                {
                    mockLogger.Object,
                    mockAccountCloseQueueManager.Object,
                    mockClock.Object,
                    mockCounterFactory.Object,
                    config.Object,
                    null
                }
            };
        }

        [TestMethod]
        [DynamicData(nameof(CreatePartitionContextTestData), DynamicDataSourceType.Method)]
        public void CreateEventProcessorSuccess(PartitionContext partitionContext)
        {
            //Act
            var result = this.processorFactory.CreateEventProcessor(partitionContext);

            //Assert
            Assert.IsNotNull(result);

            //Verify
            this.logger.Verify(
                c => c.Information(
                    nameof(AadEventProcessorFactory),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.AtLeastOnce);
        }

        [TestInitialize]
        public void Initialize()
        {
            this.logger = CreateMockGenevaLogger();
            this.logger.Setup(c => c.Information(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Verifiable();

            this.mockClock = CreateMockClock();
            this.mockClock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);
            this.mockCounterFactory = CreateMockCounterFactory();

            CancellationToken cancellationToken = new CancellationToken(true);
            this.mockQueueManager = new Mock<IAccountCloseQueueManager>(MockBehavior.Strict);
            this.mockQueueManager.Setup(c => c.EnqueueAsync(It.IsAny<IEnumerable<AccountCloseRequest>>(), cancellationToken)).Returns(Task.CompletedTask);
            this.mockRequestClassifier = new Mock<IRequestClassifier>(MockBehavior.Strict);
            this.mockDeadLetterTable = CreateMockTableNotificationDeadLetterStorage();
            this.mockAppConfiguration = new Mock<IAppConfiguration>(MockBehavior.Strict);
            this.mockAppConfiguration.Setup(c => c.GetConfigValues<Guid>(It.IsAny<string>())).Returns((Guid[])null);

            this.CreateAadEventProcessorFactory();
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(true, true)]
        public void ShouldCreateFactorySuccess(bool setupPxsConfig, bool setupAadAccountCloseConfig)
        {
            var privacyConfig = CreateMockPrivacyConfigManager(setupPxsConfig, setupAadAccountCloseConfig);
            new AadEventProcessorFactory(
                this.logger.Object,
                this.mockQueueManager.Object,
                this.mockCounterFactory.Object,
                this.mockClock.Object,
                this.hubId,
                privacyConfig.Object,
                this.mockRequestClassifier.Object,
                this.mockDeadLetterTable.Object,
                this.endpoint,
                this.mockAppConfiguration.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        [DynamicData(nameof(CreateAadEventProcessorFactoryTestData), DynamicDataSourceType.Method)]
        public void ShouldHandleArgumentNullConstructorExceptions(
            ILogger logger,
            IAccountCloseQueueManager queueManager,
            IClock clock,
            ICounterFactory counterFactory,
            IPrivacyConfigurationManager configurationManager,
            IAppConfiguration appConfiguration)
        {
            this.processorFactory = new AadEventProcessorFactory(
                logger,
                queueManager,
                counterFactory,
                clock,
                this.hubId,
                configurationManager,
                this.mockRequestClassifier.Object,
                this.mockDeadLetterTable.Object,
                this.endpoint,
                appConfiguration);
        }

        private void CreateAadEventProcessorFactory()
        {
            var privacyConfig = CreateMockPrivacyConfigManager(true, true);
            this.processorFactory = new AadEventProcessorFactory(
                this.logger.Object,
                this.mockQueueManager.Object,
                this.mockCounterFactory.Object,
                this.mockClock.Object,
                this.hubId,
                privacyConfig.Object,
                this.mockRequestClassifier.Object,
                this.mockDeadLetterTable.Object,
                this.endpoint,
                this.mockAppConfiguration.Object);
        }

        private static IEnumerable<object[]> CreatePartitionContextTestData()
        {
            var lease = new Lease();
            var lease2 = new Lease
            {
                Owner = "owner1",
                PartitionId = "part01",
                SequenceNumber = long.MaxValue,
                Token = "token1"
            };
            return new List<object[]>
            {
                new object[]
                {
                    null
                },
                new object[]
                {
                    new PartitionContext
                    {
                        Lease = lease
                    }
                },
                new object[]
                {
                    new PartitionContext
                    {
                        Lease = lease2
                    }
                }
            };
        }
    }
}

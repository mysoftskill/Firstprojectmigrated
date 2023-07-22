// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.UnitTests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.QueueProcessor;
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.TableStorage;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAccountClose;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.PrivacyServices.Common.Azure;

    using Moq;

    public class SharedTestFunctions
    {
        protected static Mock<IAadAccountCloseQueueProccessorConfiguration> CreateMockAadAccountCloseQueueProcessorConfig()
        {
            var iAadAccountCloseQueueProccessorConfiguration = new Mock<IAadAccountCloseQueueProccessorConfiguration>();
            iAadAccountCloseQueueProccessorConfiguration.Setup(c => c.ProcessorCount).Returns(3);
            return iAadAccountCloseQueueProccessorConfiguration;
        }

        protected static Mock<IAadAccountCloseService> CreateMockAadAccountCloseService()
        {
            return new Mock<IAadAccountCloseService>();
        }

        protected static Mock<IAadAccountCloseWorkerConfiguration> CreateMockAadAccountCloseWorkerConfiguration(bool useEmulator = false)
        {
            var mockEventHubProcessorConfig = CreateMockEventHubProcessorConfiguration(useEmulator);
            var iAadAccountCloseWorkerConfiguration = new Mock<IAadAccountCloseWorkerConfiguration>();
            iAadAccountCloseWorkerConfiguration.Setup(i => i.EventHubProcessorConfig).Returns(mockEventHubProcessorConfig.Object);
            return iAadAccountCloseWorkerConfiguration;
        }

        protected static Mock<ITable<AccountCloseDeadLetterStorage>> CreateMockAccountCloseDeadLetterStorage()
        {
            var mockDeadLetterStorage = new Mock<ITable<AccountCloseDeadLetterStorage>>();
            mockDeadLetterStorage.Setup(t => t.InsertAsync(It.IsAny<AccountCloseDeadLetterStorage>())).Returns(Task.FromResult(true));
            return mockDeadLetterStorage;
        }

        protected static Mock<IAccountCloseQueueManager> CreateMockAccountCloseQueueManager()
        {
            var mockAccountCloseQueueManager = new Mock<IAccountCloseQueueManager>();
            mockAccountCloseQueueManager.Setup(c => c.EnqueueAsync(It.IsAny<IEnumerable<AccountCloseRequest>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            return mockAccountCloseQueueManager;
        }

        protected static Mock<IAqsQueueProcessorConfiguration> CreateMockAqsQueueProcessorConfig()
        {
            var aqsConfig = new Mock<IAqsQueueProcessorConfiguration>(MockBehavior.Strict);
            aqsConfig.Setup(c => c.RequesterId).Returns("AqsUnitTests");
            aqsConfig.Setup(c => c.QueueName).Returns("UnitTestQueue");
            aqsConfig.Setup(c => c.GroupsToTake).Returns(50);
            aqsConfig.Setup(c => c.LeaseTimeoutSeconds).Returns(50);
            aqsConfig.Setup(c => c.ReleaseWaitIntervalSeconds).Returns(50);
            aqsConfig.Setup(c => c.WaitOnQueueEmptyMilliseconds).Returns(50);
            return aqsConfig;
        }

        protected static Mock<IAzureEventHubConfiguration> CreateMockAzureEventHubConfiguration()
        {
            var mock = new Mock<IAzureEventHubConfiguration>();
            mock.Setup(i => i.VaultBaseUrl).Returns("https://testvault.vault.azure.net");
            return mock;
        }

        protected static Mock<IAzureStorageConfiguration> CreateMockAzureStorageConfiguration(bool useEmulator)
        {
            Mock<IAzureStorageConfiguration> mockAzureStorageConfiguration = new Mock<IAzureStorageConfiguration>();
            mockAzureStorageConfiguration.Setup(i => i.UseEmulator).Returns(useEmulator);
            mockAzureStorageConfiguration.Setup(i => i.AccountName).Returns("dontcare");
            mockAzureStorageConfiguration.Setup(i => i.AuthKeyEncryptedFilePath).Returns("c:\file\auth1");
            mockAzureStorageConfiguration.Setup(i => i.StorageEndpointSuffix).Returns("endpoint.core.com");

            return mockAzureStorageConfiguration;
        }

        protected static Mock<IClock> CreateMockClock()
        {
            return new Mock<IClock>(MockBehavior.Strict);
        }

        protected static Mock<ICounter> CreateMockCounter()
        {
            return new Mock<ICounter>();
        }

        protected static Mock<ICounterFactory> CreateMockCounterFactory()
        {
            var counter = CreateMockCounter();
            var counterFactory = new Mock<ICounterFactory>(MockBehavior.Strict);
            counterFactory
                .Setup(cf => cf.GetCounter(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CounterType>()))
                .Returns(counter.Object);
            return counterFactory;
        }

        protected static Mock<IEventHubProcessorConfiguration> CreateMockEventHubProcessorConfiguration(bool useEmulator = false)
        {
            var mockAzureEventHubConfig = CreateMockAzureEventHubConfiguration();
            var mockAzureStorageConfig = CreateMockAzureStorageConfiguration(useEmulator);
            var mockEventHubConfig = new Mock<IEventHubProcessorConfiguration>();
            mockEventHubConfig.Setup(ie => ie.TenantFilter).Returns(It.IsAny<List<string>>());
            mockEventHubConfig.Setup(ie => ie.EventHubConfig).Returns(mockAzureEventHubConfig.Object);
            mockEventHubConfig.Setup(i => i.LeaseStorageConfig).Returns(mockAzureStorageConfig.Object);

            return mockEventHubConfig;
        }

        protected static Mock<ILogger> CreateMockGenevaLogger()
        {
            return new Mock<ILogger>();
        }

        protected static Mock<IPrivacyConfigurationManager> CreateMockPrivacyConfigManager(
            bool setupPxsConfig,
            bool setupAadAccountCloseConfig,
            bool useEmulator = false)
        {
            var config = CreateMockPrivacyConfigManager();
            if (setupPxsConfig)
            {
                var mockPxsConfig = CreateMockPrivacyExperienceServiceConfiguration();
                config.Setup(p => p.PrivacyExperienceServiceConfiguration)
                    .Returns(mockPxsConfig.Object);
            }

            if (setupAadAccountCloseConfig)
            {
                var mockAadAccountCloseWorkerConfig = CreateMockAadAccountCloseWorkerConfiguration(useEmulator);
                config.Setup(p => p.AadAccountCloseWorkerConfiguration).Returns(mockAadAccountCloseWorkerConfig.Object);
            }

            return config;
        }

        protected static Mock<IPrivacyConfigurationManager> CreateMockPrivacyConfigManager()
        {
            var config = CreateMockAqsQueueProcessorConfig();

            var aqsConfig = new Mock<IAqsConfiguration>(MockBehavior.Default);
            aqsConfig.Setup(a => a.AqsQueueProcessorConfiguration).Returns(config.Object);

            var workerConfig = new Mock<IPrivacyAqsWorkerConfiguration>(MockBehavior.Default);
            workerConfig.Setup(a => a.AqsConfiguration).Returns(new[] { aqsConfig.Object }.ToList());
            workerConfig.Setup(a => a.EnableExtraLogging).Returns(true);

            var iAadAccountCloseQueueProccessorConfiguration = CreateMockAadAccountCloseQueueProcessorConfig();

            var iAadAccountCloseWorkerConfiguration = new Mock<IAadAccountCloseWorkerConfiguration>();
            iAadAccountCloseWorkerConfiguration.Setup(c => c.QueueProccessorConfig).Returns(iAadAccountCloseQueueProccessorConfiguration.Object);

            var privacyConfig = new Mock<IPrivacyConfigurationManager>(MockBehavior.Default);
            privacyConfig.Setup(p => p.AqsWorkerConfiguration).Returns(workerConfig.Object);
            privacyConfig.Setup(p => p.AadAccountCloseWorkerConfiguration).Returns(iAadAccountCloseWorkerConfiguration.Object);

            return privacyConfig;
        }

        protected static Mock<IPrivacyExperienceServiceConfiguration> CreateMockPrivacyExperienceServiceConfiguration()
        {
            var mockPxsConfig = new Mock<IPrivacyExperienceServiceConfiguration>();
            mockPxsConfig.Setup(c => c.CloudInstance).Returns(It.IsAny<CloudInstanceType>());
            return mockPxsConfig;
        }

        protected static Mock<IRequestClassifier> CreateMockRequestClassifier()
        {
            return new Mock<IRequestClassifier>();
        }

        protected static Mock<ITable<NotificationDeadLetterStorage>> CreateMockTableNotificationDeadLetterStorage()
        {
            return new Mock<ITable<NotificationDeadLetterStorage>>();
        }

        protected static Mock<IAppConfiguration> CreateMockAppConfiguration()
        {
            return new Mock<IAppConfiguration>();
        }
    }
}

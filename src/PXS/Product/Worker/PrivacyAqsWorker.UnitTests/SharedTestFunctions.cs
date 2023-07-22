// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.UnitTests
{
    using System;
    using System.Linq;

    using Microsoft.Azure.ComplianceServices.Common.UnitTests;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.CosmosHelpers;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.EventProcessors;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.LeaseId;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.TableStorage;
    using Microsoft.Membership.MemberServices.Privacy.Core.AQS;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.Test.Common;

    using Moq;

    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Common.Cosmos;

    public class SharedTestFunctions
    {
        protected static Mock<IAccountDeleteWriter> CreateMockAccountDeleteWriter()
        {
            return new Mock<IAccountDeleteWriter>();
        }

        public static Mock<IAqsQueueProcessorConfiguration> CreateMockAqsQueueProcessorConfig(bool filter = true)
        {
            var aqsConfig = new Mock<IAqsQueueProcessorConfiguration>(MockBehavior.Strict);
            aqsConfig.Setup(c => c.RequesterId).Returns("AqsUnitTests");
            aqsConfig.Setup(c => c.QueueName).Returns("UnitTestQueue");
            aqsConfig.Setup(c => c.GroupsToTake).Returns(50);
            aqsConfig.Setup(c => c.LeaseTimeoutSeconds).Returns(50);
            aqsConfig.Setup(c => c.ReleaseWaitIntervalSeconds).Returns(50);
            aqsConfig.Setup(c => c.WaitOnQueueEmptyMilliseconds).Returns(50);
            aqsConfig.Setup(c => c.IgnoreVerifierErrors).Returns(false);

            return aqsConfig;
        }

        protected static IPrivacyConfigurationManager CreateMockConf()
        {
            var config = CreateMockAqsQueueProcessorConfig();

            var aqsConfig = new Mock<IAqsConfiguration>(MockBehavior.Strict);
            aqsConfig.Setup(a => a.AqsQueueProcessorConfiguration).Returns(config.Object);
            aqsConfig.Setup(a => a.ProcessorCount).Returns(1);
            aqsConfig.Setup(a => a.Endpoint).Returns("https://www.msn.com");
            aqsConfig.Setup(a => a.ConnectionLimit).Returns(98);

            Mock<ICertificateConfiguration> certificateConfiguration = new Mock<ICertificateConfiguration>();
            certificateConfiguration.SetupGet(c => c.Thumbprint).Returns(UnitTestData.UnitTestCertificate.Thumbprint);
            aqsConfig.Setup(a => a.CertificateConfiguration).Returns(certificateConfiguration.Object);

            var workerConfig = new Mock<IPrivacyAqsWorkerConfiguration>(MockBehavior.Strict);
            workerConfig.Setup(a => a.AqsConfiguration).Returns(new[] { aqsConfig.Object }.ToList());
            workerConfig.Setup(a => a.EnableExtraLogging).Returns(true);

            var privacyConfig = new Mock<IPrivacyConfigurationManager>(MockBehavior.Strict);
            privacyConfig.Setup(p => p.AqsWorkerConfiguration).Returns(workerConfig.Object);

            return privacyConfig.Object;
        }

        protected static Mock<ICounter> CreateMockCounter()
        {
            return new Mock<ICounter>();
        }

        protected static Mock<ICounterFactory> CreateMockCounterFactory()
        {
            var counter = new Mock<ICounter>();
            var counterFactory = new Mock<ICounterFactory>(MockBehavior.Strict);
            counterFactory
                .Setup(cf => cf.GetCounter(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CounterType>()))
                .Returns(counter.Object);
            return counterFactory;
        }

        protected static Mock<IUserCreateEventProcessor> CreateMockCreateEventProcessor()
        {
            return new Mock<IUserCreateEventProcessor>(MockBehavior.Strict);
        }

        protected static Mock<IAccountCreateWriter> CreateMockCreateWriter()
        {
            return new Mock<IAccountCreateWriter>();
        }

        protected static Mock<IUserDeleteEventProcessor> CreateMockDeleteEventProcessor()
        {
            return new Mock<IUserDeleteEventProcessor>(MockBehavior.Strict);
        }

        protected static Mock<IDistributedIdFactory> CreateMockDistributedIdFactory()
        {
            return new Mock<IDistributedIdFactory>();
        }

        protected static Mock<ILogger> CreateMockGenevaLogger()
        {
            return new Mock<ILogger>();
        }

        protected static Mock<ITable<MsaDeadLetterStorage>> CreateMockMsaDeadLetterStorage()
        {
            return new Mock<ITable<MsaDeadLetterStorage>>(MockBehavior.Strict);
        }

        protected static Mock<IPuidMappingConfig> CreateMockPuidMappingConfig()
        {
            var mockPuidConfig = new Mock<IPuidMappingConfig>(MockBehavior.Strict);
            mockPuidConfig.SetupGet(c => c.LogPath).Returns("C:/TEST");
            mockPuidConfig.SetupGet(c => c.RootDir).Returns("C:/TEST");
            mockPuidConfig.SetupGet(c => c.StreamExtension).Returns("txt");
            mockPuidConfig.SetupGet(c => c.StreamNamePrefix).Returns("TEST");
            return mockPuidConfig;
        }

        protected static Mock<IAsyncQueueService2> CreateMockQueueService2()
        {
            return new Mock<IAsyncQueueService2>();
        }

        protected static Mock<ICosmosClient> CreateMockCosmosClient()
        {
            var mockICosmosClient = new Mock<ICosmosClient>();
            mockICosmosClient.Setup(c => c.CreateAsync(It.IsAny<string>(), TimeSpan.MinValue, CosmosCreateStreamMode.CreateAlways));
            return mockICosmosClient;
        }

        protected static EventDataBaseProperty InvalidDeleteData(int reason = 2) => new EventDataBaseProperty
        {
            Name = "CredentialName",
            ExtendedData = $"UserDeleteReason:{reason},cid:123abc,GdprPreVerifier:gdpr,field"
        };

        protected static CDPEvent2 MakeEvent(string puid, EventData data)
        {
            return new CDPEvent2
            {
                AggregationKey = puid,
                EventData = data
            };
        }

        protected static EventDataBaseProperty ValidDeleteData(int reason = 2) => new EventDataBaseProperty
        {
            Name = "CredentialName",
            ExtendedData = $"UserDeleteReason:{reason},cid:123abc,GdprPreVerifier:gdpr{(reason == 2 ? ",LastSuccessSignIn:2000-01-01T14:05:00Z,Suspended:false" : "")}"
        };
    }
}

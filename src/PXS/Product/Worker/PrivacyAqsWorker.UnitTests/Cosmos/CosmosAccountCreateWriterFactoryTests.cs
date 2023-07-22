// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.UnitTests.Cosmos
{
    using System;
    using System.Linq;

    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.CosmosHelpers;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.Cosmos;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Common.Cosmos;

    [TestClass]
    public class CosmosAccountCreateWriterFactoryTests : SharedTestFunctions
    {
        private Mock<ICosmosClient> cosmosClient;

        private Mock<IPuidMappingConfig> cosmosConfig;

        private Mock<ILogger> logger;

        [TestMethod]
        public void CreateSuccess()
        {
            IAccountCreateWriter result = CosmosAccountCreateWriterFactory.Create(
                this.CreateMockConf(),
                this.cosmosClient.Object,
                this.logger.Object,
                CreateMockDistributedIdFactory().Object);
            Assert.IsNotNull(result);
        }

        [TestInitialize]
        public void Initialize()
        {
            this.logger = new Mock<ILogger>();
            this.cosmosClient = new Mock<ICosmosClient>();
            this.cosmosClient.Setup(c => c.CreateAsync("ABC", TimeSpan.MinValue, CosmosCreateStreamMode.CreateAlways));
            this.cosmosConfig = new Mock<IPuidMappingConfig>(MockBehavior.Strict);
            this.cosmosConfig.SetupGet(c => c.LogPath).Returns("C:/TEST");
            this.cosmosConfig.SetupGet(c => c.StreamExtension).Returns("txt");
            this.cosmosConfig.SetupGet(c => c.StreamNamePrefix).Returns("TEST");
        }

        protected new IPrivacyConfigurationManager CreateMockConf()
        {
            var config = new Mock<IAqsQueueProcessorConfiguration>(MockBehavior.Strict);
            config.Setup(c => c.RequesterId).Returns("AqsUnitTests");
            config.Setup(c => c.QueueName).Returns("UnitTestQueue");
            config.Setup(c => c.GroupsToTake).Returns(50);
            config.Setup(c => c.LeaseTimeoutSeconds).Returns(50);
            config.Setup(c => c.ReleaseWaitIntervalSeconds).Returns(50);
            var aqsConfig = new Mock<IAqsConfiguration>(MockBehavior.Strict);
            aqsConfig.Setup(a => a.AqsQueueProcessorConfiguration).Returns(config.Object);
            var workerConfig = new Mock<IPrivacyAqsWorkerConfiguration>();
            workerConfig.Setup(a => a.AqsConfiguration).Returns(new[] { aqsConfig.Object }.ToList());
            workerConfig.Setup(a => a.EnableExtraLogging).Returns(true);
            var privacyConfig = new Mock<IPrivacyConfigurationManager>(MockBehavior.Strict);
            privacyConfig.Setup(p => p.AqsWorkerConfiguration.MappingConfig).Returns(this.cosmosConfig.Object);
            privacyConfig.Setup(p => p.AqsWorkerConfiguration.MappingConfig.CosmosVcPath).Returns("https://doesnotmatter.com");
            privacyConfig.Setup(p => p.AqsWorkerConfiguration.CosmosConnectionLimit).Returns(2);
            return privacyConfig.Object;
        }
    }
}

// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.UnitTests.Cosmos
{
    using System;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.Common.Cosmos;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.CosmosHelpers;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.Cosmos;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class AqsCosmosClientFactoryTests
    {
        private Mock<X509Certificate> certificate;

        private Mock<ICertificateProvider> certProvider;

        private Mock<IPuidMappingConfig> cosmosConfig;

        [TestMethod]
        [ExpectedException(typeof(NullReferenceException))]
        public void CreateCosmosClientExpectedException()
        {
            var config = this.CreateMockConf();

            ICosmosClient result = AqsCosmosClientFactory.CreateCosmosClient(config, new AppConfiguration(@"local.settings.json"), new CosmosResourceFactory(), null);
        }

        [TestInitialize]
        public void Initialize()
        {
            this.certProvider = new Mock<ICertificateProvider>();

            this.cosmosConfig = new Mock<IPuidMappingConfig>();
            this.cosmosConfig.SetupGet(c => c.LogPath).Returns("C:/TEST");
            this.cosmosConfig.SetupGet(c => c.StreamExtension).Returns("txt");
            this.cosmosConfig.SetupGet(c => c.StreamNamePrefix).Returns("TEST");
            this.certificate = new Mock<X509Certificate>(MockBehavior.Strict);
        }

        protected IPrivacyConfigurationManager CreateMockConf()
        {
            var config = new Mock<IAqsQueueProcessorConfiguration>();
            config.Setup(c => c.RequesterId).Returns("AqsUnitTests");
            config.Setup(c => c.QueueName).Returns("UnitTestQueue");
            config.Setup(c => c.GroupsToTake).Returns(50);
            config.Setup(c => c.LeaseTimeoutSeconds).Returns(50);
            config.Setup(c => c.ReleaseWaitIntervalSeconds).Returns(50);
            var aqsConfig = new Mock<IAqsConfiguration>();
            aqsConfig.Setup(a => a.AqsQueueProcessorConfiguration).Returns(config.Object);

            var workerConfig = new Mock<IPrivacyAqsWorkerConfiguration>();
            workerConfig.Setup(a => a.AqsConfiguration).Returns(new[] { aqsConfig.Object }.ToList());
            workerConfig.Setup(a => a.EnableExtraLogging).Returns(true);

            var privacyConfig = new Mock<IPrivacyConfigurationManager>();
            privacyConfig.Setup(p => p.AqsWorkerConfiguration.MappingConfig).Returns(this.cosmosConfig.Object);
            privacyConfig.Setup(p => p.AqsWorkerConfiguration.MappingConfig.CosmosVcPath).Returns("https://doesnotmatter.com");
            return privacyConfig.Object;
        }
    }
}

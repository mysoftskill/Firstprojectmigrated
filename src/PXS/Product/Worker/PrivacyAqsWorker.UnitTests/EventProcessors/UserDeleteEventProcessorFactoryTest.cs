// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.UnitTests.EventProcessors
{
    using System;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.EventProcessors;
    using Microsoft.Membership.MemberServices.Privacy.Core.VerificationTokenValidation;
    using Microsoft.Membership.MemberServices.PrivacyAdapters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Microsoft.PrivacyServices.Common.Azure;

    [TestClass]
    public class UserDeleteEventProcessorFactoryTest : SharedTestFunctions
    {
        private CdpEvent2Helper cdpEvent2Helper;

        private Mock<ICounter> counter;

        private Mock<ICounterFactory> counterFactory;

        private Mock<IAqsQueueProcessorConfiguration> iAqsQueueProcessorConfiguration;

        private IUserDeleteEventProcessor iUserDeleteEventProcessor;

        private Mock<ILogger> logger;

        private Mock<IMsaIdentityServiceAdapter> msa;

        private Mock<IVerificationTokenValidationService> tokenVerifier;

        private Mock<IXboxAccountsAdapter> xbox;

        private Mock<IClock> clock = new Mock<IClock>();

        [TestMethod]
        public void CreateSuccess()
        {
            //Arrange
            var proxy = new UserDeleteEventProcessorFactory(
                this.cdpEvent2Helper,
                this.counterFactory.Object,
                this.logger.Object,
                this.clock.Object);

            //Act
            this.iUserDeleteEventProcessor = proxy.Create(this.iAqsQueueProcessorConfiguration.Object);
            
            //Assert
            Assert.IsNotNull(this.iUserDeleteEventProcessor);
        }

        [TestInitialize]
        public void Initialize()
        {
            Sll.ResetContext();

            this.counter = CreateMockCounter();
            this.counterFactory = CreateMockCounterFactory();
            this.msa = new Mock<IMsaIdentityServiceAdapter>(MockBehavior.Strict);
            this.msa.Setup(m => m.GetGdprAccountCloseVerifierAsync(It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(
                    new AdapterResponse<string>
                    {
                        Result = "abc123"
                    });
            this.xbox = new Mock<IXboxAccountsAdapter>(MockBehavior.Strict);
            this.xbox.Setup(x => x.GetXuidAsync(It.IsAny<IPxfRequestContext>())).ReturnsAsync(
                new AdapterResponse<string>
                {
                    Result = "abc123"
                });

            this.tokenVerifier = new Mock<IVerificationTokenValidationService>(MockBehavior.Strict);
            this.tokenVerifier.Setup(v => v.ValidateVerifierAsync(It.Is<PrivacyRequest>(req => req is AccountCloseRequest), "abc123"))
                .ReturnsAsync(new AdapterResponse());

            this.logger = CreateMockGenevaLogger();

            this.cdpEvent2Helper = new CdpEvent2Helper(CreateMockConf(), this.logger.Object);

            this.iAqsQueueProcessorConfiguration = CreateMockAqsQueueProcessorConfig();

            var aqsConfig = new Mock<IAqsConfiguration>(MockBehavior.Strict);
            aqsConfig.Setup(a => a.AqsQueueProcessorConfiguration).Returns(this.iAqsQueueProcessorConfiguration.Object);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Sll.ResetContext();
        }
    }
}

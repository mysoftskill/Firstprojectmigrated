// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.PCF
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.PCF;
    using Microsoft.Membership.MemberServices.Privacy.Core.VerificationTokenValidation;
    using Microsoft.Membership.MemberServices.PrivacyAdapters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Graph;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public abstract class PcfProxyServiceTestBase
    {
        protected readonly ILogger logger = new ConsoleLogger();

        protected Mock<IPrivacyConfigurationManager> configuration = new Mock<IPrivacyConfigurationManager>();

        protected Mock<IAadRequestVerificationServiceAdapter> mockAadRvsAdapter = new Mock<IAadRequestVerificationServiceAdapter>(MockBehavior.Strict);

        protected Mock<IGraphAdapter> mockGraphAdapter = new Mock<IGraphAdapter>(MockBehavior.Strict);

        protected Mock<IMsaIdentityServiceAdapter> mockMsaIdentityServiceAdapter = new Mock<IMsaIdentityServiceAdapter>(MockBehavior.Strict);

        protected Mock<IPcfAdapter> mockPcfAdapter = new Mock<IPcfAdapter>(MockBehavior.Strict);

        protected Mock<IVerificationTokenValidationService> mockVerificationTokenValidationService = new Mock<IVerificationTokenValidationService>(MockBehavior.Strict);

        protected Mock<IXboxAccountsAdapter> mockXboxAccountsAdapter = new Mock<IXboxAccountsAdapter>(MockBehavior.Strict);

        protected Mock<IPrivacyExperienceServiceConfiguration> privacyExperienceServiceConfiguration = new Mock<IPrivacyExperienceServiceConfiguration>();

        protected Mock<IAppConfiguration> mockAppconfiguration = new Mock<IAppConfiguration>();

        [TestInitialize]
        public void Initialize()
        {
            this.mockVerificationTokenValidationService
                .Setup(ver => ver.ValidateVerifierAsync(It.IsAny<PrivacyRequest>(), It.IsAny<string>()))
                .ReturnsAsync(new AdapterResponse());

            this.mockPcfAdapter
                .Setup(pcf => pcf.PostCommandsAsync(It.IsAny<IList<PrivacyRequest>>()))
                .ReturnsAsync(new AdapterResponse());

            this.mockGraphAdapter
                .Setup(ga => ga.IsMemberOfAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(new AdapterResponse<IsMemberOfResponse> { Result = new IsMemberOfResponse { Value = true } });

        }

        protected PcfProxyService CreatePcfProxyService()
        {
            var counterMock = new Mock<ICounter>(MockBehavior.Loose);
            var counterFactoryMock = new Mock<ICounterFactory>(MockBehavior.Strict);
            counterFactoryMock.Setup(f => f.GetCounter(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CounterType>()))
                .Returns(counterMock.Object);

            this.privacyExperienceServiceConfiguration.SetupGet(c => c.ThrottleConfigurations).Returns(new Dictionary<string, IPrivacyThrottleConfiguration>());
            this.configuration.SetupGet(c => c.PrivacyExperienceServiceConfiguration).Returns(this.privacyExperienceServiceConfiguration.Object);

            return new PcfProxyService(
                this.mockXboxAccountsAdapter.Object,
                this.mockPcfAdapter.Object,
                this.mockMsaIdentityServiceAdapter.Object,
                this.mockVerificationTokenValidationService.Object,
                this.mockAadRvsAdapter.Object,
                this.mockGraphAdapter.Object,
                this.configuration.Object,
                this.logger,
                counterFactoryMock.Object,
                Policies.Current,
                this.mockAppconfiguration.Object);
        }
    }
}

// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.AadAccountClose
{
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAccountClose;
    using Microsoft.Membership.MemberServices.Privacy.Core.VerificationTokenValidation;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter;
    using Microsoft.PrivacyServices.Common.Azure;

    using Moq;

    /// <summary>
    /// </summary>
    public abstract class AadAccountCloseServiceTestBase
    {
        protected ILogger logger = new ConsoleLogger();

        protected Mock<IAadRequestVerificationServiceAdapter> mockAadRvsAdapter = new Mock<IAadRequestVerificationServiceAdapter>(MockBehavior.Strict);

        protected Mock<IPcfAdapter> mockPcfAdapter = new Mock<IPcfAdapter>(MockBehavior.Strict);

        protected Mock<IVerificationTokenValidationService> mockVerificationTokenValidationService = new Mock<IVerificationTokenValidationService>(MockBehavior.Strict);

        protected Mock<IAppConfiguration> mockAppConfiguration = new Mock<IAppConfiguration>(MockBehavior.Strict);

        protected AadAccountCloseService CreateAadAccountCloseService()
        {
            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(It.IsAny<string>(), true)).ReturnsAsync(true);

            return new AadAccountCloseService(
                this.mockPcfAdapter.Object,
                this.mockVerificationTokenValidationService.Object,
                this.mockAadRvsAdapter.Object,
                this.logger,
                this.mockAppConfiguration.Object);
        }
    }
}

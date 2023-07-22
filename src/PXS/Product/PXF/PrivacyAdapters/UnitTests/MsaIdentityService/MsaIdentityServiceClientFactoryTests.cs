// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.UnitTests.MsaIdentityService
{
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class MsaIdentityServiceClientFactoryTests : TestBase
    {
        private Mock<ICertificateProvider> mockCertificateProvider;

        private Mock<IMsaIdentityServiceConfiguration> mockMsaIdentityConfig;

        private Mock<IPrivacyPartnerAdapterConfiguration> mockPrivacyAdapterConfig;

        private MsaIdentityServiceClientFactory msaFactory;

        [TestMethod]
        public void CreateCredentialServiceClientSuccess()
        {
            //Act
            this.msaFactory.CreateCredentialServiceClient(
                this.mockPrivacyAdapterConfig.Object,
                this.mockMsaIdentityConfig.Object,
                this.mockCertificateProvider.Object);

            //Assert
            Assert.IsNotNull(this.msaFactory);
        }

        [TestMethod]
        public void CreateProfileServiceClientSuccess()
        {
            //Act
            this.msaFactory.CreateProfileServiceClient(
                this.mockPrivacyAdapterConfig.Object,
                this.mockMsaIdentityConfig.Object,
                this.mockCertificateProvider.Object);

            //Assert
            Assert.IsNotNull(this.msaFactory);
        }

        [TestInitialize]
        public void Init()
        {
            this.mockCertificateProvider = CreateCertProviderMock();
            this.mockPrivacyAdapterConfig = new Mock<IPrivacyPartnerAdapterConfiguration>();
            this.mockMsaIdentityConfig = CreateMsaIdentityConfigMock();
            var certConfig = new Mock<ICertificateConfiguration>(MockBehavior.Strict);

            this.mockPrivacyAdapterConfig.SetupGet(c => c.BaseUrl).Returns("https://doesnotcare.com");
            this.msaFactory = new MsaIdentityServiceClientFactory();
        }
    }
}

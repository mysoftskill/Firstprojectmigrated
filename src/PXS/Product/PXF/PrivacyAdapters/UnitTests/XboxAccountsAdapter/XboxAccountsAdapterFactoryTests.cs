// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.UnitTests.XboxAccounts
{
    using System;
    using System.Security.Cryptography.X509Certificates;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Factory;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts;
    using Microsoft.OSGS.HttpClientCommon;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Microsoft.PrivacyServices.Common.Azure;

    [TestClass]
    public class XboxAccountsAdapterFactoryTests : TestBase
    {
        private Mock<IClock> mockClock;

        private XboxAccountsAdapterFactory factory;

        private Mock<ICertificateProvider> mockCertificateProvider;

        private Mock<IHttpClientFactory> mockClientFactory;

        private Mock<ILogger> mockLogger;

        private Mock<IPrivacyConfigurationManager> mockPrivacyConfigurationManager;

        [TestMethod]
        public void CreateXboxAccountsAdapter()
        {
            var privacyAdapter = this.factory.Create(
                this.mockCertificateProvider.Object,
                this.mockPrivacyConfigurationManager.Object,
                this.mockLogger.Object,
                this.mockCounterFactory.Object,
                this.mockClock.Object);

            Assert.IsNotNull(privacyAdapter);
        }

        [TestInitialize]
        public void Init()
        {
            this.mockClock = new Mock<IClock>(MockBehavior.Strict);
            this.mockClock.Setup(c => c.UtcNow).Returns(new DateTimeOffset(2018, 2, 3, 4, 5, 6, 7, TimeSpan.Zero));
            this.mockClientFactory = new Mock<IHttpClientFactory>();
            this.mockCertificateProvider = CreateCertProviderMock();
            this.mockPrivacyConfigurationManager = new Mock<IPrivacyConfigurationManager>();
            this.mockLogger = new Mock<ILogger>();
            var mockX509Certificate2 = new Mock<X509Certificate2>();
            var mockCertificateConfiguration = new Mock<ICertificateConfiguration>();

            var mockXboxAccountsAdapterConfiguration = new Mock<IXboxAccountsAdapterConfiguration>();
            mockXboxAccountsAdapterConfiguration.SetupGet(p => p.S2SCertificateConfiguration).Returns(mockCertificateConfiguration.Object);

            var mockHttpClient = new Mock<IHttpClient>();

            var mockPrivacyAdapterConfig = new Mock<IPrivacyPartnerAdapterConfiguration>();
            var mockAdaptersConfiguration = new Mock<IAdaptersConfiguration>();
            mockAdaptersConfiguration
                .Setup(c => c.XboxAccountsAdapterConfiguration)
                .Returns(mockXboxAccountsAdapterConfiguration.Object);

            var mockMsaIdentityServiceConfiguration = new Mock<IMsaIdentityServiceConfiguration>();
            mockMsaIdentityServiceConfiguration.SetupGet(p => p.CertificateConfiguration).Returns(mockCertificateConfiguration.Object);
            mockMsaIdentityServiceConfiguration.SetupGet(p => p.ClientId).Returns("295218");
            mockMsaIdentityServiceConfiguration.SetupGet(p => p.Endpoint).Returns("https://login.live.com/pksecure/oauth20_clientcredentials.srf");

            this.mockPrivacyConfigurationManager
                .Setup(c => c.MsaIdentityServiceConfiguration)
                .Returns(mockMsaIdentityServiceConfiguration.Object);

            this.mockPrivacyConfigurationManager
                .Setup(c => c.AdaptersConfiguration)
                .Returns(mockAdaptersConfiguration.Object);

            this.mockCertificateProvider
                .Setup(c => c.GetClientCertificate(mockCertificateConfiguration.Object))
                .Returns(mockX509Certificate2.Object);

            this.mockClientFactory
                .Setup(c => c.CreateHttpClient(mockXboxAccountsAdapterConfiguration.Object, mockX509Certificate2.Object, this.mockCounterFactory.Object))
                .Returns(mockHttpClient.Object);

            this.factory = new XboxAccountsAdapterFactory(this.mockClientFactory.Object);
        }
    }
}

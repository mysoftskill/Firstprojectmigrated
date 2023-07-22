// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.UnitTests.Factory
{
    using System.Net.Http;
    using System.Security.Cryptography.X509Certificates;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging.LoggingFilter;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Factory;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Microsoft.PrivacyServices.Common.Azure;

    [TestClass]
    public class HttpClientFactoryTests
    {
        private Mock<ICounterFactory> counterFactory;

        private HttpClientFactoryPublic httpClientFactory;

        private Mock<ILogger> logger;

        private Mock<ILoggingFilter> loggingFilter;

        private Mock<X509Certificate2> mockCertificate2;

        private Mock<IPrivacyPartnerAdapterConfiguration> partnerConfig;

        private Mock<WebRequestHandler> webRequestHandler;

        [TestMethod]
        public void CreateHttpClientSuccess()
        {
            //Act
            var result = this.httpClientFactory.CreateHttpClient(this.partnerConfig.Object, this.counterFactory.Object);

            //Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void CreateHttpClientWithCertificateSuccess()
        {
            //Act
            var result = this.httpClientFactory.CreateHttpClient(
                this.partnerConfig.Object,
                this.mockCertificate2.Object,
                this.counterFactory.Object);

            //Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void CreateHttpClientWitWebHandlerSuccess(bool includeOutgoingRequestHandler)
        {
            //Act
            var result = this.httpClientFactory.CreateHttpClient(
                this.partnerConfig.Object,
                this.webRequestHandler.Object,
                this.counterFactory.Object,
                includeOutgoingRequestHandler);

            //Assert
            Assert.IsNotNull(result);
        }

        [TestInitialize]
        public void Initialize()
        {
            this.partnerConfig = new Mock<IPrivacyPartnerAdapterConfiguration>();
            this.partnerConfig.SetupGet(c => c.BaseUrl).Returns("https://doesnotmatter.com");
            this.partnerConfig.SetupGet(c => c.PartnerId).Returns("doesnotmatter");
            this.partnerConfig.SetupGet(c => c.AadTokenResourceId).Returns("TokenResource");
            this.partnerConfig.SetupGet(c => c.TimeoutInMilliseconds).Returns(10000);

            var mockServicePointConfiguration = new Mock<IServicePointConfiguration>(MockBehavior.Strict);
            mockServicePointConfiguration.Setup(c => c.ConnectionLimit).Returns(42);
            mockServicePointConfiguration.Setup(c => c.ConnectionLeaseTimeout).Returns(39);
            mockServicePointConfiguration.Setup(c => c.MaxIdleTime).Returns(98);

            this.partnerConfig.SetupGet(c => c.ServicePointConfiguration).Returns(mockServicePointConfiguration.Object);

            this.counterFactory = new Mock<ICounterFactory>();
            this.mockCertificate2 = new Mock<X509Certificate2>();
            this.webRequestHandler = new Mock<WebRequestHandler>();
            this.loggingFilter = new Mock<ILoggingFilter>();
            this.logger = new Mock<ILogger>();
            this.httpClientFactory = new HttpClientFactoryPublic(this.loggingFilter.Object, this.logger.Object);
        }
    }
}

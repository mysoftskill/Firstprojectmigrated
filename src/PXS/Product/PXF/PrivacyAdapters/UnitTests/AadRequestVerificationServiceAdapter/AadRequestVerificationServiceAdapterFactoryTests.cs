// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.UnitTests.AadRequestVerificationServiceAdapter
{
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Factory;
    using Microsoft.OSGS.HttpClientCommon;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class AadRequestVerificationServiceAdapterFactoryTests
    {
        private Mock<IAadAuthManager> mockAadAuthManager;

        private Mock<IPrivacyConfigurationManager> mockConfiguration;

        private Mock<ICounterFactory> mockCounterFactory;

        private Mock<IHttpClientFactory> mockHttpClientFactory;

        [TestMethod]
        public void ConstructAadRequestVerificationServiceAdapterFactorySuccess()
        {
            var aadRequestFactory = this.CreateAadRequestVerificationServiceAdapterFactory();

            Assert.IsNotNull(aadRequestFactory);
        }

        [TestMethod]
        public void CreateSuccess()
        {
            var adapterFactory = this.CreateAadRequestVerificationServiceAdapterFactory();
            var adapter = adapterFactory.Create();
            Assert.IsNotNull(adapter);
        }

        [TestInitialize]
        public void Init()
        {
            this.mockHttpClientFactory = new Mock<IHttpClientFactory>();
            this.mockAadAuthManager = new Mock<IAadAuthManager>();
            this.mockConfiguration = new Mock<IPrivacyConfigurationManager>();
            var mockAdapterConfiguration = new Mock<IAdaptersConfiguration>();
            this.mockCounterFactory = new Mock<ICounterFactory>(MockBehavior.Loose);
            var mockCounter = new Mock<ICounter>(MockBehavior.Loose);
            var mockHttpClient = new Mock<IHttpClient>();

            var mockAadRequestVerificationServiceAdapterConfiguration = new Mock<IAadRequestVerificationServiceAdapterConfiguration>();

            mockAdapterConfiguration.SetupGet(c => c.AadRequestVerificationServiceAdapterConfiguration).Returns(mockAadRequestVerificationServiceAdapterConfiguration.Object);
            this.mockConfiguration
                .Setup(c => c.AdaptersConfiguration).Returns(mockAdapterConfiguration.Object);

            this.mockCounterFactory
                .Setup(c => c.GetCounter(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CounterType>()))
                .Returns(mockCounter.Object);

            this.mockHttpClientFactory
                .Setup(c => c.CreateHttpClient(mockAadRequestVerificationServiceAdapterConfiguration.Object, this.mockCounterFactory.Object))
                .Returns(mockHttpClient.Object);
        }

        private AadRequestVerificationServiceAdapterFactory CreateAadRequestVerificationServiceAdapterFactory()
        {
            return new AadRequestVerificationServiceAdapterFactory(
                this.mockHttpClientFactory.Object,
                this.mockConfiguration.Object,
                this.mockCounterFactory.Object,
                this.mockAadAuthManager.Object);
        }
    }
}

// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.UnitTests
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Configuration.Privacy;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.Test.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Newtonsoft.Json;

    using Microsoft.PrivacyServices.Common.Azure;

    public abstract class TestBase
    {
        protected readonly Mock<ICounterFactory> mockCounterFactory = TestMockFactory.CreateCounterFactory();

        protected void ExpectedException<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
                Assert.Fail("Expected exception of type " + typeof(TException).FullName);
            }
            catch (TException)
            {
                // This was expected
            }
        }

        protected static Mock<ICertificateProvider> CreateCertProviderMock()
        {
            var provider = new Mock<ICertificateProvider>(MockBehavior.Strict);
            provider
                .Setup(p => p.GetClientCertificate(It.IsAny<ICertificateConfiguration>()))
                .Returns(new X509Certificate2());

            return provider;
        }

        protected static Mock<IMsaIdentityServiceConfiguration> CreateMsaIdentityConfigMock()
        {
            var certConfig = new Mock<ICertificateConfiguration>(MockBehavior.Strict);

            var provider = new Mock<IMsaIdentityServiceConfiguration>(MockBehavior.Strict);
            provider.SetupGet(p => p.CertificateConfiguration).Returns(certConfig.Object);
            provider.SetupGet(p => p.ClientId).Returns("295218");
            provider.SetupGet(p => p.Endpoint).Returns("https://login.live.com/pksecure/oauth20_clientcredentials.srf");

            return provider;
        }

        protected static Mock<IPrivacyConfigurationManager> CreatePrivacyConfigurationManager(
            IMsaIdentityServiceConfiguration msaIdentityServiceConfiguration,
            IDataManagementConfig pxfAdaptersConfiguration,
            IPrivacyExperienceServiceConfiguration serviceConfig,
            IAdaptersConfiguration adaptersConfiguration)
        {
            Mock<IPrivacyConfigurationManager> configurationManager = new Mock<IPrivacyConfigurationManager>(MockBehavior.Strict);
            configurationManager.SetupGet(c => c.MsaIdentityServiceConfiguration).Returns(msaIdentityServiceConfiguration);
            configurationManager.SetupGet(c => c.DataManagementConfig).Returns(pxfAdaptersConfiguration);
            configurationManager.SetupGet(c => c.PrivacyExperienceServiceConfiguration).Returns(serviceConfig);
            configurationManager.SetupGet(c => c.AdaptersConfiguration).Returns(adaptersConfiguration);
            return configurationManager;
        }

        protected static Mock<ILogger> CreateLoggerMock()
        {
            return new Mock<ILogger>(MockBehavior.Loose);
        }

        protected HttpContent CreateHttpContent<T>(T contentValue, string mediaType = "application/json")
        {
            string serializedValue = JsonConvert.SerializeObject(contentValue);
            return this.CreateHttpContent(serializedValue, mediaType);
        }

        private HttpContent CreateHttpContent(string contentValue, string mediaType = null)
        {
            HttpContent result = new StringContent(contentValue);

            if (mediaType != null)
            {
                result.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
            }

            return result;
        }

        protected static void AreEqual(AdapterError expected, AdapterError actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual);
                return;
            }

            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.Code, actual.Code);
            Assert.AreEqual(expected.Message, actual.Message);
        }
    }
}

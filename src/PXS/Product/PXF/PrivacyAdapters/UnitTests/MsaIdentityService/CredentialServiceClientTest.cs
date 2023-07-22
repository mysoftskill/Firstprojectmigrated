// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.UnitTests.MsaIdentityService
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    /// <summary>
    ///     CredentialServiceClientTest
    /// </summary>
    [TestClass]
    public class CredentialServiceClientTest : TestBase
    {
        private Mock<ICertificateProvider> certProvider;

        private ICredentialServiceClient credentialServiceClient;

        private Mock<IPrivacyPartnerAdapterConfiguration> mockPrivacyPartnerAdapterConfig;

        private Mock<IMsaIdentityServiceConfiguration> msaServiceConfig;

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CredentialServiceClientExHandlingWhenIPrivacyPartnerAdapterConfigurationIsNull()
        {
            try
            {
                new CredentialServiceClient(null, this.msaServiceConfig.Object, this.certProvider.Object);
                Assert.Fail("Should have failed");
            }
            catch (ArgumentNullException e)
            {
                Assert.AreEqual("Value cannot be null.\r\nParameter name: adapterConfig", e.Message);
                throw;
            }
        }

        [TestMethod]
        public void GetGdprVerifierAsyncAccountCloseSuccess()
        {
            ConfiguredTaskAwaitable<string> result = this.credentialServiceClient.GetGdprVerifierAsync(
                    It.IsAny<tagPASSID>(),
                    eGDPR_VERIFIER_OPERATION.AccountClose,
                    It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>())
                .ConfigureAwait(false);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void GetGdprVerifierAsyncDeleteSuccess()
        {
            ConfiguredTaskAwaitable<string> result = this.credentialServiceClient.GetGdprVerifierAsync(
                    It.IsAny<tagPASSID>(),
                    eGDPR_VERIFIER_OPERATION.Delete,
                    It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>())
                .ConfigureAwait(false);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void GetGdprVerifierAsynExportSuccess()
        {
            ConfiguredTaskAwaitable<string> result = this.credentialServiceClient.GetGdprVerifierAsync(
                    It.IsAny<tagPASSID>(),
                    eGDPR_VERIFIER_OPERATION.Export,
                    It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>())
                .ConfigureAwait(false);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        [DataRow("1351235626262")]
        [DataRow("")]
        public void GetSigninNamesAndCidsForNetIdSuccess(string puid)
        {
            ConfiguredTaskAwaitable<string> result = this.credentialServiceClient.GetSigninNamesAndCidsForNetIdAsync(puid).ConfigureAwait(false);
            Assert.IsNotNull(result);
        }

        [TestInitialize]
        public void Initialize()
        {
            this.mockPrivacyPartnerAdapterConfig = new Mock<IPrivacyPartnerAdapterConfiguration>();
            this.mockPrivacyPartnerAdapterConfig.Setup(c => c.BaseUrl).Returns("https://www.msa.com");
            this.mockPrivacyPartnerAdapterConfig.Setup(c => c.PartnerId).Returns("Partnerid");
            this.mockPrivacyPartnerAdapterConfig.Setup(c => c.TimeoutInMilliseconds).Returns(10000);

            this.certProvider = CreateCertProviderMock();
            this.msaServiceConfig = CreateMsaIdentityConfigMock();
            this.credentialServiceClient = new CredentialServiceClient(
                this.mockPrivacyPartnerAdapterConfig.Object,
                this.msaServiceConfig.Object,
                this.certProvider.Object);
        }
    }
}

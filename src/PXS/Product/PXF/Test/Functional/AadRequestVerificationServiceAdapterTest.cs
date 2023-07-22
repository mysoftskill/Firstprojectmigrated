// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.FunctionalTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication;
    using Microsoft.Membership.MemberServices.Privacy.Core.PrivacyCommand;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.OSGS.HttpClientCommon;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    using ConsoleLogger = Microsoft.PrivacyServices.Common.Azure.ConsoleLogger;

    /// <summary>
    ///     AadRequestVerificationServiceAdapter Test
    /// </summary>
    [TestClass]
    public class AadRequestVerificationServiceAdapterTest
    {
        [Ignore, TestMethod, Description("This test is not meant to run in functional tests. It's meant only for debugging purposes.")]
        public async Task TestConstructAccountClose()
        {
            Guid targetObjectId = Guid.Parse("d9062efc-26b6-4945-8b2e-276c7510711d");
            Guid tenantId = Guid.Parse("7bdb2545-6702-490d-8d07-5cc0a5376dd9");
            long targetOrgIdPuid = 1153765932362723921;

            var accountCloseRequest = new AccountCloseRequest
            {
                Subject = new AadSubject { ObjectId = targetObjectId, TenantId = tenantId, OrgIdPUID = targetOrgIdPuid },
                RequestId = Guid.NewGuid(),
                RequestGuid = Guid.NewGuid()
            };

            Mock<IAadRequestVerificationServiceAdapterConfiguration> mockAadRvsConfiguration = CreateMocks(out Mock<IPrivacyConfigurationManager> mockConfiguration);

            var logger = new ConsoleLogger();
            var tokenManager = new InstrumentedAadTokenManager(new AadTokenManager());
            var mockCounterFactory = new Mock<ICounterFactory>(MockBehavior.Loose);
            var mockConfigurationManager = new Mock<IPrivacyConfigurationManager>(MockBehavior.Loose);
            var tokenHandler = new AadJwtSecurityTokenHandler(new ConsoleLogger(), mockConfigurationManager.Object);
            var miseTokenHandler = new MiseTokenValidationUtility(new ConsoleLogger(), mockConfigurationManager.Object);
            var mockAppConfiguration = new Mock<IAppConfiguration>();
            var adapter = new AadRequestVerificationServiceAdapter(
                new HttpClient(),
                mockAadRvsConfiguration.Object,
                new AadAuthManager(mockConfiguration.Object, new CertificateProvider(logger), logger, tokenManager, tokenHandler, miseTokenHandler, mockAppConfiguration.Object),
                mockCounterFactory.Object);

            AadRvsRequest rvsRequest = PrivacyRequestConverter.CreateAadRvsGdprRequestV2(accountCloseRequest, AadRequestVerificationServiceAdapter.AadRvsOperationType.AccountClose);
            AdapterResponse<AadRvsVerifiers> response = await adapter.ConstructAccountCloseAsync(rvsRequest).ConfigureAwait(false);

            Assert.IsNotNull(response);
            Assert.IsTrue(response.IsSuccess, $"Response was: {response?.Error}");
            Assert.IsTrue(Guid.TryParse(rvsRequest.CorrelationId, out Guid correlationId), $"Correlation id value was: {rvsRequest.CorrelationId}");
            Assert.AreNotEqual(default(Guid), correlationId, $"Correlation id value was: {rvsRequest.CorrelationId}");
        }

        private static Mock<IAadRequestVerificationServiceAdapterConfiguration> CreateMocks(out Mock<IPrivacyConfigurationManager> mockConfiguration)
        {
            var mockAadRvsConfiguration = new Mock<IAadRequestVerificationServiceAdapterConfiguration>(MockBehavior.Strict);
            mockAadRvsConfiguration.SetupGet(c => c.AadAppId).Returns("c728155f-7b2a-4502-a08b-b8af9b269319");
            mockAadRvsConfiguration.SetupGet(c => c.EnableAdapter).Returns(true);
            mockAadRvsConfiguration.SetupGet(c => c.BaseUrl).Returns("https://aadrvs-ppe.msidentity.com");
            mockAadRvsConfiguration.SetupGet(c => c.PartnerId).Returns("AADRequestVerificationService");

            var mockCertificateConfiguration = new Mock<ICertificateConfiguration>(MockBehavior.Strict);
            mockCertificateConfiguration.SetupGet(c => c.Thumbprint).Returns("FC3C0EF39391B30DCCF50952B536FE7EC1753D24");
            mockCertificateConfiguration.SetupGet(c => c.Subject).Returns("CN=pdos.aadclient.pxs.privacy.microsoft-int.com");
            mockCertificateConfiguration.SetupGet(c => c.CheckValidity).Returns(true);

            var mockAadTokenAuthConfiguration = new Mock<IAadTokenAuthConfiguration>(MockBehavior.Strict);
            mockAadTokenAuthConfiguration.SetupGet(c => c.RequestSigningCertificateConfiguration).Returns(mockCertificateConfiguration.Object);
            mockAadTokenAuthConfiguration.SetupGet(c => c.AadAppId).Returns("705363a0-5817-47fb-ba32-59f47ce80bb7");

            var mockJwtInboundPolicy = new Mock<IJwtInboundPolicyConfig>(MockBehavior.Strict);
            mockJwtInboundPolicy.SetupGet(c => c.Authority).Returns("https://sts.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47/");
            mockJwtInboundPolicy.SetupGet(c => c.Audiences).Returns(new List<string> { "https://graph.microsoft.com/" });
            mockJwtInboundPolicy.Setup(c => c.IssuerPrefixes).Returns(new[] { "https://sts.windows-ppe.net" });
            mockJwtInboundPolicy.SetupGet(c => c.ApplyPolicyForAllTenants).Returns(true);
            mockJwtInboundPolicy.SetupGet(c => c.ValidIncomingAppIds).Returns(new[] { "00000003-0000-0000-c000-000000000000" });
            mockAadTokenAuthConfiguration.SetupGet(c => c.JwtInboundPolicyConfig).Returns(mockJwtInboundPolicy.Object);

            var mockJwtOutboundPolicyAadRvsConstructAccountClose = new Mock<IJwtOutboundPolicyConfig>(MockBehavior.Strict);
            mockJwtOutboundPolicyAadRvsConstructAccountClose.SetupGet(c => c.AppId).Returns("c728155f-7b2a-4502-a08b-b8af9b269319");
            mockJwtOutboundPolicyAadRvsConstructAccountClose.SetupGet(c => c.Authority).Returns("https://login.windows-ppe.net/ea8a4392-515e-481f-879e-6571ff2a8a36/");
            mockJwtOutboundPolicyAadRvsConstructAccountClose.SetupGet(c => c.Resource).Returns("c728155f-7b2a-4502-a08b-b8af9b269319");

            IDictionary<string, IJwtOutboundPolicyConfig> dictionary = new Dictionary<string, IJwtOutboundPolicyConfig>
            {
                { OutboundPolicyName.AadRvsConstructAccountClose.ToString(), mockJwtOutboundPolicyAadRvsConstructAccountClose.Object }
            };
            mockAadTokenAuthConfiguration
                .SetupGet(c => c.JwtOutboundPolicyConfig)
                .Returns(dictionary);

            mockConfiguration = new Mock<IPrivacyConfigurationManager>(MockBehavior.Strict);
            mockConfiguration.SetupGet(c => c.AadTokenAuthGeneratorConfiguration).Returns(mockAadTokenAuthConfiguration.Object);
            return mockAadRvsConfiguration;
        }
    }
}

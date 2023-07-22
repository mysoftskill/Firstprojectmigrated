// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.AadAuthentication.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.IdentityModel.Protocols;
    using Microsoft.IdentityModel.Protocols.OpenIdConnect;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.Membership.MemberServices.AadAuthentication.UnitTests.Helpers;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;
       
    /// <summary>
    ///     AadJwtSecurityTokenHandlerTest
    /// </summary>
    [TestClass]
    public class AadJwtSecurityTokenHandlerTest
    {
        private const string ChinaAppId = "3B8D673D-F45D-4B72-9B0F-B631E82EDD46";

        private const string ChinaOpenIdConfigurationEndpoint = "https://login.partner.microsoftonline.cn/0b4a31a2-c1a0-475d-b363-5f26668660a3/.well-known/openid-configuration";

        // These app id's aren't real, they are just new guids generated for the purposes of unit testing.
        private const string DefaultAppId = "DEFADA4A-4F51-471E-820D-1BA08F6AC6DC";

        private const string DefaultAuthorityTenantId = "6131C9AA-740D-46F0-8039-B2C95D58BAB6";

        private const string DefaultLoginEndpoint = "default-login-endpoint.com";

        private const string DefaultOpenIdConfigurationEndpoint = "https://" + DefaultLoginEndpoint + "/" + DefaultAuthorityTenantId + "/.well-known/openid-configuration";

        private const string ProdAppId = "915B658F-CD9F-4D36-9D9E-75BF7988C1CA";

        private const string ProdOpenIdConfigurationEndpoint = "https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47/.well-known/openid-configuration";

        private const string UnitedStatesAppId = "EB98871D-DDFE-4D3E-86A3-0DCCF74AD0B8";

        private const string UnitedStatesOpenIdConfigurationEndpoint = "https://login.microsoftonline.us/f8cdef31-a31e-4b4a-93e4-5f571e91255a/.well-known/openid-configuration";

        private Mock<IAadTokenAuthConfiguration> mockAadTokenAuthGeneratorConfiguration;

        private Mock<IPrivacyConfigurationManager> mockConfigurationManager;

        [TestInitialize]
        public void Init()
        {
            this.CreateMockConfigurationManager();
        }

        [TestMethod]
        public void AadJwtSecurityTokenHandlerConstructor()
        {
            var tokenHandler = new AadJwtSecurityTokenHandler(
                new ConsoleLogger(), 
                mockConfigurationManager.Object);

            Assert.IsNotNull(tokenHandler);
        }

        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        [DynamicData(nameof(CreateAadJwtSecurityTokenHandlerTestData), DynamicDataSourceType.Method)]
        public void AadJwtSecurityTokenHandlerConstructorThrowOnNull(
            ILogger logger,
            IPrivacyConfigurationManager config,
            string errorMessage)
        {
            try
            {
                var authJwtSecurityTokenHandler = new AadJwtSecurityTokenHandler(logger, config);
                Assert.Fail("should not get here");
            }
            catch (ArgumentNullException e)
            {
                Assert.AreEqual(e.Message, errorMessage);
                throw e;
            }
        }

        [TestMethod]
        [DataRow("https://sts.chinacloudapi.cn")]
        public async Task GetConnectConfigurationAsyncTest(string issuer)
        {
            var jwtSecurityToken = AadAuthTestHelper.CreateUnitTestJwtSecurityToken(issuer);

            Mock<IConfigurationRetriever<OpenIdConnectConfiguration>> aadConfigRetrieverMock = CreateAadConfigRetrieverMock();

            var tokenHandler = new AadJwtSecurityTokenHandler(new ConsoleLogger(), mockConfigurationManager.Object);

            var result = await tokenHandler.GetConnectConfigurationAsync(jwtSecurityToken, aadConfigRetrieverMock.Object).ConfigureAwait(false);
            Assert.IsNotNull(result);
        }

        [TestMethod, ExpectedException(typeof(SecurityTokenException))]
        [DataRow("https://sts.chinacloudapi.cn")]
        public async Task GetConnectConfigurationAsyncThrowsSecurityTokenException(string issuer)
        {
            var jwtSecurityToken = AadAuthTestHelper.CreateUnitTestJwtSecurityToken(issuer);

            Mock<IConfigurationRetriever<OpenIdConnectConfiguration>> aadConfigRetrieverMock = CreateAadConfigRetrieverMock();
            aadConfigRetrieverMock.Setup(m => m.GetConfigurationAsync(It.IsAny<string>(), It.IsAny<IDocumentRetriever>(), CancellationToken.None))
                .Throws(new SecurityTokenException("Unable to get OpenIdConnectConfiguration"));

            var tokenHandler = new AadJwtSecurityTokenHandler(new ConsoleLogger(), mockConfigurationManager.Object);

            var result = await tokenHandler.GetConnectConfigurationAsync(jwtSecurityToken, aadConfigRetrieverMock.Object).ConfigureAwait(false);
            Assert.Fail("should not get here");
        }

        [TestMethod]
        public void MapIssuerToOpenIdConfigurationEndpointNullHandling()
        {
            var tokenHandler = new AadJwtSecurityTokenHandler(new ConsoleLogger(), mockConfigurationManager.Object);
            Assert.IsNotNull(tokenHandler.MapIssuerToOpenIdConfigurationEndpoint(null));
            Assert.IsNotNull(tokenHandler.MapTokenToAppId(null));
        }

        [TestMethod]
        [DataRow("foo", DefaultOpenIdConfigurationEndpoint)] // Ones that don't match should use the default.
        [DataRow("https://sts.windows.net/abc123", ProdOpenIdConfigurationEndpoint)]
        [DataRow("https://sts.chinacloudapi.cn", ChinaOpenIdConfigurationEndpoint)]
        [DataRow("https://sts.microsoftonline.us", UnitedStatesOpenIdConfigurationEndpoint)]
        [DataRow("https://sts.windows.net/f8cdef31-a31e-4b4a-93e4-5f571e91255a/", UnitedStatesOpenIdConfigurationEndpoint)]
        [DataRow("HTTPS://STS.WINDOWS.NET", ProdOpenIdConfigurationEndpoint)]
        [DataRow("HTTPS://STS.CHINACLOUDAPI.CN", ChinaOpenIdConfigurationEndpoint)]
        [DataRow("HTTPS://STS.MICROSOFTONLINE.US", UnitedStatesOpenIdConfigurationEndpoint)]
        public void ShouldMapIssuersCorrectly(string tokenIssuer, string expectedIssuer)
        {
            var jwtSecurityToken = AadAuthTestHelper.CreateUnitTestJwtSecurityToken(tokenIssuer);
            var tokenHandler = new AadJwtSecurityTokenHandler(new ConsoleLogger(), mockConfigurationManager.Object);

            Uri result = tokenHandler.MapIssuerToOpenIdConfigurationEndpoint(jwtSecurityToken);

            Assert.AreEqual(new Uri(expectedIssuer), result);
        }

        [TestMethod]
        [DataRow(null, DefaultLoginEndpoint, DefaultOpenIdConfigurationEndpoint)] // Ones that don't match should use the default.
        [DataRow("72f988bf-86f1-41af-91ab-2d7cd011db47", "login.microsoftonline.com", ProdOpenIdConfigurationEndpoint)]
        [DataRow("0b4a31a2-c1a0-475d-b363-5f26668660a3", "login.partner.microsoftonline.cn", ChinaOpenIdConfigurationEndpoint)]
        public void createOpenIdConfigurationEndpointCorrectly(string tenantId, string loginEndpoint, string openIdConfigurationEndpoint)
        {
            mockAadTokenAuthGeneratorConfiguration.Setup(c => c.AadLoginEndpointForOpenId).Returns(loginEndpoint);
            var tokenHandler = new AadJwtSecurityTokenHandler(new ConsoleLogger(), mockConfigurationManager.Object);
            
            Uri result = tokenHandler.CreateOpenIdConfigurationEndpoint(tenantId);

            Assert.AreEqual(new Uri(openIdConfigurationEndpoint), result);
        }

        [TestMethod]
        [DataRow("foo", DefaultAppId)] // Ones that don't match should use the default.
        [DataRow("https://sts.windows.net/abc123", ProdAppId)]
        [DataRow("https://sts.chinacloudapi.cn", ChinaAppId)]
        [DataRow("https://sts.microsoftonline.us", UnitedStatesAppId)]
        [DataRow("https://sts.windows.net/f8cdef31-a31e-4b4a-93e4-5f571e91255a/", UnitedStatesAppId)]
        [DataRow("HTTPS://STS.WINDOWS.NET", ProdAppId)]
        [DataRow("HTTPS://STS.CHINACLOUDAPI.CN", ChinaAppId)]
        [DataRow("HTTPS://STS.MICROSOFTONLINE.US", UnitedStatesAppId)]
        public void ShouldMapTokenToAppIdCorrectly(string tokenIssuer, string expectedAppId)
        {
            var jwtSecurityToken = AadAuthTestHelper.CreateUnitTestJwtSecurityToken(tokenIssuer);
            var tokenHandler = new AadJwtSecurityTokenHandler(new ConsoleLogger(), mockConfigurationManager.Object);

            string result = tokenHandler.MapTokenToAppId(jwtSecurityToken);

            Assert.AreEqual(expectedAppId, result);
        }

        [TestMethod,ExpectedException(typeof(ArgumentException))]
        [DataRow("https://sts.chinacloudapi.cn", ChinaAppId)]
        public void ValidateTokenFailsForInvalidToken(string tokenIssuer, string appId)
        {
            string token = "invalid_token";
            var tokenHandler = new AadJwtSecurityTokenHandler(new ConsoleLogger(), mockConfigurationManager.Object);

            Mock<IConnectConfigurationWrapper> mockIConnectConfigurationWrapper = new Mock<IConnectConfigurationWrapper>();
            mockIConnectConfigurationWrapper.Setup(c => c.Issuer).Returns(tokenIssuer);
            mockIConnectConfigurationWrapper.Setup(c => c.SigningKeys).Returns(new List<SecurityKey>());

            var result = tokenHandler.ValidateToken(token, appId, mockIConnectConfigurationWrapper.Object);
            Assert.Fail("should not get here");
        }

        private void CreateMockConfigurationManager()
        {
            var aadProd = new Mock<IIssuerAppIdConfig>(MockBehavior.Strict);
            aadProd.Setup(c => c.AppId).Returns(ProdAppId);
            aadProd.Setup(c => c.OpenIdConfigurationEndpoint).Returns(ProdOpenIdConfigurationEndpoint);
            aadProd.Setup(c => c.StsAuthorityEndpoint).Returns("https://sts.windows.net");

            var aadMooncake = new Mock<IIssuerAppIdConfig>(MockBehavior.Strict);
            aadMooncake.Setup(c => c.AppId).Returns(ChinaAppId);
            aadMooncake.Setup(c => c.OpenIdConfigurationEndpoint).Returns(ChinaOpenIdConfigurationEndpoint);
            aadMooncake.Setup(c => c.StsAuthorityEndpoint).Returns("https://sts.chinacloudapi.cn");

            var aadFairfax = new Mock<IIssuerAppIdConfig>(MockBehavior.Strict);
            aadFairfax.Setup(c => c.AppId).Returns(UnitedStatesAppId);
            aadFairfax.Setup(c => c.OpenIdConfigurationEndpoint).Returns(UnitedStatesOpenIdConfigurationEndpoint);
            aadFairfax.Setup(c => c.StsAuthorityEndpoint).Returns("https://sts.microsoftonline.us");

            var issuerAppIdConfigs = new Dictionary<string, IIssuerAppIdConfig>
            {
                { "AadProd", aadProd.Object },
                { "AadMooncake", aadMooncake.Object },
                { "AadFairfax", aadFairfax.Object }
            };

            var jwtInboundPolicyConfig = new Mock<IJwtInboundPolicyConfig>(MockBehavior.Strict);
            jwtInboundPolicyConfig.Setup(c => c.ValidIncomingAppIds).Returns( new List<string> { "00000003-0000-0000-c000-000000000000" });
            jwtInboundPolicyConfig.Setup(c => c.Audiences).Returns(new List<string> { "00000003-0000-0000-c000-000000000000" });
            jwtInboundPolicyConfig.Setup(c => c.IssuerPrefixes).Returns(new List<string> { "00000003-0000-0000-c000-000000000000" });
            

            mockAadTokenAuthGeneratorConfiguration = new Mock<IAadTokenAuthConfiguration>(MockBehavior.Strict);
            mockAadTokenAuthGeneratorConfiguration.Setup(c => c.AadAppId).Returns(DefaultAppId);
            mockAadTokenAuthGeneratorConfiguration.Setup(c => c.AadLoginEndpoint).Returns(DefaultLoginEndpoint);
            mockAadTokenAuthGeneratorConfiguration.Setup(c => c.AuthorityTenantId).Returns(DefaultAuthorityTenantId);
            mockAadTokenAuthGeneratorConfiguration.Setup(c => c.AadLoginEndpointForOpenId).Returns(DefaultLoginEndpoint);
            mockAadTokenAuthGeneratorConfiguration.Setup(c => c.AuthorityTenantIdForOpenId).Returns(DefaultAuthorityTenantId);
            mockAadTokenAuthGeneratorConfiguration.Setup(c => c.IssuerAppIdConfigs).Returns(issuerAppIdConfigs);
            mockAadTokenAuthGeneratorConfiguration.Setup(c => c.JwtInboundPolicyConfig).Returns(jwtInboundPolicyConfig.Object);
  
            mockConfigurationManager = new Mock<IPrivacyConfigurationManager>(MockBehavior.Strict);
            mockConfigurationManager.Setup(c => c.AadTokenAuthGeneratorConfiguration).Returns(mockAadTokenAuthGeneratorConfiguration.Object);
        }

        private static Mock<IConfigurationRetriever<OpenIdConnectConfiguration>> CreateAadConfigRetrieverMock()
        {
            var result = new OpenIdConnectConfiguration() { Issuer = "DummyIssuer" };
            
            var mock = new Mock<IConfigurationRetriever<OpenIdConnectConfiguration>>();
            mock.Setup(m => m.GetConfigurationAsync(It.IsAny<string>(), It.IsAny<IDocumentRetriever>(), CancellationToken.None))
                .Returns(Task.FromResult(result));;

            return mock;
        }

        private static IEnumerable<object[]> CreateAadJwtSecurityTokenHandlerTestData()
        {
            var privacyConfigurationManager = new Mock<IPrivacyConfigurationManager>();
            var logger = new Mock<ILogger>();

            return new List<object[]>
            {
                new object[]
                {
                    null,
                    privacyConfigurationManager.Object,
                    "Value cannot be null." + Environment.NewLine + "Parameter name: logger"
                },
                new object[]
                {
                    logger.Object,
                    null,
                    "Value cannot be null." + Environment.NewLine + "Parameter name: privacyConfigurationManager"
                }
            };
        }
    }
}

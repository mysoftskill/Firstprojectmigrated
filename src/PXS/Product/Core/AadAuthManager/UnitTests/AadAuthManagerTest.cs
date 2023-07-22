// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.AadAuthentication.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IdentityModel.Tokens.Jwt;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Security.Claims;
    using System.Security.Cryptography.X509Certificates;
    using System.Security.Principal;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.UnitTests;
    using Microsoft.IdentityModel.Protocols;
    using Microsoft.IdentityModel.Protocols.OpenIdConnect;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.Membership.MemberServices.AadAuthentication.UnitTests.Helpers;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class AadAuthManagerTest
    {
        private const string AppTokenValue = "app_token_value";

        // Get an expired test token from:
        // https://microsoftgraph.visualstudio.com/_git/onboarding?_a=preview&path=%2Fdocs%2Fonboarding%2Fpft-authorization-flow.md&version=GBmaster
        private const string ExpiredTestOnlyToken =
            @"actortoken=""Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6InFXMDAtQnM3b2ZzWS1YdmIzMVFMOEN5bUhPMCIsImtpZCI6InFXMDAtQnM3b2ZzWS1YdmIzMVFMOEN5bUhPMCJ9.eyJhdWQiOiIwMDAwMDAwMi0wMDAwLTAwMDAtYzAwMC0wMDAwMDAwMDAwMDAiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLXBwZS5uZXQvODgxOGFkOTctMjU0Ny00MTcxLWFkZjUtOGI0MTY2MjcyZjQwLyIsImlhdCI6MTUxMTgyOTU1OCwibmJmIjoxNTExODI5NTU4LCJleHAiOjE1MTE4MzM0NTgsImFpbyI6IjQyUmdZREFJUzF0WWU3YlhsdU9sNUV2YmJUWVBBUT09IiwiYXBwaWQiOiIwMDAwMDAwMy0wMDAwLTAwMDAtYzAwMC0wMDAwMDAwMDAwMDAiLCJhcHBpZGFjciI6IjIiLCJjbmYiOnsiandrIjp7Imt0eSI6IlJTQSIsIm4iOiIzMDgyMDEwQTAyODIwMTAxMDBCMTA5RTY2NzM2MDNCREYzODU3REE0QTYzQTczNTkwMkMxRDQ3QjY4MTAyQzc1QTQ1Q0IyRTY2Q0RDQzQ2RkQ1MjkxNkZDNzEyREFDRENEODBENTNDNjYwMTBDMkRDQkVEODI4NDAwNjJEMjVFMkZBQUMyMzJGRDMxRTg5M0E2REY2MjA4MDY5QUFCMTQ0OUQxRDZDMzBFNTAyNkNDQTMxMzc0NTA3NTIxMTgwRDJEQjdGOUVBNEUyM0Y0MEE5NDAzMUVGNjE5NEVBQkVCRTE2MTE3QzBEQjREQTJENTczQUZDNUEwOUZBQTlFNTZDMzc1MkNCNTlFNTk0ODlCNUNGMEREM0VGQkI5NzRDQzNBQzAxRThGOUI0QUE1M0I1NzAyRTI5QjY4RTY4NTlGRTNEMzM2NzVCMjM1QkVFQUYyNUVDNjcxNTMzNDI2QTc3NDFDQUY1REU5QTE4NDMyRTQ3MDU4QjAxOTBEODJDRjNEOEZFNzEzMDExNzBGQjQ5MjIwODE0MkE5QURCNTRGMTcxNzY0MTVGNzBCODdDOTE4RDY3ODBGQ0Y3NjBGQTE1N0RCOTY0QjE4MTg4N0QzN0RGNDg1NTZGNEQyQTk2MDIwRTgwODgwQTU1OERFN0EwNDA1NTI0QTIxRjE0ODcxRDNENkQ0RDdFMzI2QzgzODc4NTAyMDMwMTAwMDEiLCJlIjoiQVFBQiIsImFsZyI6IlJTMjU2Iiwia2lkIjoiMSJ9fSwiaWRwIjoiaHR0cHM6Ly9zdHMud2luZG93cy1wcGUubmV0Lzg4MThhZDk3LTI1NDctNDE3MS1hZGY1LThiNDE2NjI3MmY0MC8iLCJvaWQiOiIzZTQ1OTBmMi01YmE3LTRjM2EtODZiMy05NTJjNWM3ZmRlMjciLCJzdWIiOiIzZTQ1OTBmMi01YmE3LTRjM2EtODZiMy05NTJjNWM3ZmRlMjciLCJ0ZW5hbnRfcmVnaW9uX3Njb3BlIjoiTkEiLCJ0aWQiOiI4ODE4YWQ5Ny0yNTQ3LTQxNzEtYWRmNS04YjQxNjYyNzJmNDAiLCJ1dGkiOiJMTWhFWnVOcjNFeXBYWGhMeWh3RkFBIiwidmVyIjoiMS4wIn0.ESTSSIGNATURE"",accesstoken=""Bearer eyJ0eXAiOiJKV1QiLCJub25jZSI6ImZEcFN1U2ZzSWRZd2QtWWtidEp3blpsNUd2ZlZYODJMcXpnVEQzMF9BZzQiLCJhbGciOiJSUzI1NiIsIng1dCI6InFXMDAtQnM3b2ZzWS1YdmIzMVFMOEN5bUhPMCIsImtpZCI6InFXMDAtQnM3b2ZzWS1YdmIzMVFMOEN5bUhPMCJ9.eyJhdWQiOiJodHRwczovL2dyYXBoLm1pY3Jvc29mdC1wcGUuY29tIiwiaXNzIjoiaHR0cHM6Ly9zdHMud2luZG93cy1wcGUubmV0Lzg4MThhZDk3LTI1NDctNDE3MS1hZGY1LThiNDE2NjI3MmY0MC8iLCJpYXQiOjE1MTE4Mjk1NDksIm5iZiI6MTUxMTgyOTU0OSwiZXhwIjoxNTExODMzNDQ5LCJhY3IiOiIxIiwiYWlvIjoiQVNRQTIvOEpBQUFBaWN6WXVUbkJsYXhoeGk2K2Y3ZFJpWURDQ1FWVFhtd3k1TERXeEgvVUY5az0iLCJhbXIiOlsicHdkIl0sImFwcF9kaXNwbGF5bmFtZSI6InNkdGVzdC1BbGxBY2Nlc3MiLCJhcHBpZCI6ImVmOTY0ODNiLTRlN2YtNDY4MC05ZjAzLTk0MTJmZTkyZWZhZCIsImFwcGlkYWNyIjoiMSIsImZhbWlseV9uYW1lIjoiRG9lIiwiZ2l2ZW5fbmFtZSI6IkpvaG4iLCJpcGFkZHIiOiIxNjcuMjIwLjEuMTYyIiwibmFtZSI6IlRFU1RfVEVTVF9TUE9Qcm92SGVhcnRiZWF0X0UzXzE1XzE3MDgzMTIzMDBfMzQ2Iiwib2lkIjoiYjk2OTg0MmEtMzk2YS00NjVkLWI3NzQtNzk1NGQwMzEwYjE4IiwicGxhdGYiOiIxNCIsInB1aWQiOiIxMDAzM0ZGRkE0Nzk5QjZCIiwic2NwIjoiQ2FsZW5kYXJzLlJlYWQgQ2FsZW5kYXJzLlJlYWRXcml0ZSBDb250YWN0cy5SZWFkIENvbnRhY3RzLlJlYWRXcml0ZSBEaXJlY3RvcnkuQWNjZXNzQXNVc2VyLkFsbCBEaXJlY3RvcnkuUmVhZC5BbGwgRGlyZWN0b3J5LlJlYWRXcml0ZS5BbGwgZW1haWwgRmlsZXMuUmVhZCBGaWxlcy5SZWFkLkFsbCBGaWxlcy5SZWFkLlNlbGVjdGVkIEZpbGVzLlJlYWRXcml0ZSBGaWxlcy5SZWFkV3JpdGUuQWxsIEZpbGVzLlJlYWRXcml0ZS5BcHBGb2xkZXIgRmlsZXMuUmVhZFdyaXRlLlNlbGVjdGVkIEdyb3VwLlJlYWQuQWxsIEdyb3VwLlJlYWRXcml0ZS5BbGwgSWRlbnRpdHlSaXNrRXZlbnQuUmVhZC5BbGwgTWFpbC5SZWFkIE1haWwuUmVhZFdyaXRlIE1haWwuU2VuZCBNYWlsYm94U2V0dGluZ3MuUmVhZFdyaXRlIE5vdGVzLkNyZWF0ZSBOb3Rlcy5SZWFkIE5vdGVzLlJlYWQuQWxsIE5vdGVzLlJlYWRXcml0ZSBOb3Rlcy5SZWFkV3JpdGUuQWxsIE5vdGVzLlJlYWRXcml0ZS5DcmVhdGVkQnlBcHAgb2ZmbGluZV9hY2Nlc3Mgb3BlbmlkIFBlb3BsZS5SZWFkIFBlb3BsZS5SZWFkV3JpdGUgcHJvZmlsZSByZWNpcGllbnQubWFuYWdlIFNpdGVzLlJlYWQuQWxsIFRhc2tzLlJlYWRXcml0ZSBVc2VyLlJlYWQgVXNlci5SZWFkLkFsbCBVc2VyLlJlYWRCYXNpYy5BbGwgVXNlci5SZWFkV3JpdGUgVXNlci5SZWFkV3JpdGUuQWxsIiwic3ViIjoiSFpMNFJBU0hDMWtEY3hRckRTQk1qMG11UFNRZjBMc25KZGhwMDlUcGFlYyIsInRpZCI6Ijg4MThhZDk3LTI1NDctNDE3MS1hZGY1LThiNDE2NjI3MmY0MCIsInVuaXF1ZV9uYW1lIjoiYWRtaW5AYTgzMGVkYWQ5MDUwODQ5MzQ2RTE3MDgzMTIzLmNjc2N0cC5uZXQiLCJ1cG4iOiJhZG1pbkBhODMwZWRhZDkwNTA4NDkzNDZFMTcwODMxMjMuY2NzY3RwLm5ldCIsInV0aSI6IlZQZVVuTFo2MGs2UktRZ3FDYThFQUEiLCJ2ZXIiOiIxLjAiLCJ3aWRzIjpbIjYyZTkwMzk0LTY5ZjUtNDIzNy05MTkwLTAxMjE3NzE0NWUxMCJdfQ.ESTSSIGNATURE"",type=""PFAT""";

        private Mock<IAadTokenAuthConfiguration> mockAadTokenAuthGeneratorConfiguration;

        private Mock<ICertificateProvider> mockCertificateProvider;

        private Mock<IPrivacyConfigurationManager> mockConfig;

        private Mock<IJwtOutboundPolicyConfig> mockJwtOutboundPolicyConfig;

        private Mock<IAadJwtSecurityTokenHandler> mockTokenHandler;

        private Mock<ITokenManager> mockTokenManager;

        private Mock<IMiseTokenValidationUtility> mockMiseTokenHandler;

        private readonly Mock<IAppConfiguration> mockAppConfiguration = new Mock<IAppConfiguration>(MockBehavior.Strict);

        [TestInitialize]
        public void Init()
        {
            this.CreateMockConfiguration();
        }

        [TestMethod]
        public void AadAuthManagerConstructor()
        {
            var authManager = new AadAuthManager(
                this.mockConfig.Object,
                this.mockCertificateProvider.Object,
                new ConsoleLogger(),
                this.mockTokenManager.Object,
                this.mockTokenHandler.Object,
                this.mockMiseTokenHandler.Object,
                this.mockAppConfiguration.Object);

            Assert.IsNotNull(authManager);
        }

        [TestMethod, ExpectedException(typeof(KeyNotFoundException))]
        public void AadAuthManagerConstructorThrownOnJwtOutboundPolicyConfigInvalidEnumeration()
        {
            this.mockAadTokenAuthGeneratorConfiguration.Setup(c => c.JwtOutboundPolicyConfig)
                .Returns(
                    new Dictionary<string, IJwtOutboundPolicyConfig>
                    {
                        { "invalid_key", this.mockJwtOutboundPolicyConfig.Object }
                    });
            try
            {
                new AadAuthManager(this.mockConfig.Object, this.mockCertificateProvider.Object, new ConsoleLogger(), this.mockTokenManager.Object, this.mockTokenHandler.Object, this.mockMiseTokenHandler.Object, this.mockAppConfiguration.Object);
                Assert.Fail("should not get here");
            }
            catch (KeyNotFoundException e)
            {
                Assert.AreEqual(e.Message, "Invalid enumeration specified: invalid_key");
                throw e;
            }
        }

        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public void AadAuthManagerConstructorThrownOnJwtOutboundPolicyConfigNull()
        {
            this.mockAadTokenAuthGeneratorConfiguration.Setup(c => c.JwtOutboundPolicyConfig)
                .Returns((Dictionary<string, IJwtOutboundPolicyConfig>)null);
            try
            {
                new AadAuthManager(this.mockConfig.Object, this.mockCertificateProvider.Object, new ConsoleLogger(), this.mockTokenManager.Object, this.mockTokenHandler.Object, this.mockMiseTokenHandler.Object, this.mockAppConfiguration.Object);
                Assert.Fail("should not get here");
            }
            catch (ArgumentNullException e)
            {
                Assert.AreEqual(e.Message, "Value cannot be null." + Environment.NewLine + "Parameter name: JwtOutboundPolicyConfig");
                throw e;
            }
        }

        [TestMethod, ExpectedException(typeof(NullReferenceException))]
        public void AadAuthManagerConstructorThrowOnAadTokenAuthGeneratorConfigurationNull()
        {
            try
            {
                new AadAuthManager(
                    new Mock<IPrivacyConfigurationManager>().Object,
                    this.mockCertificateProvider.Object,
                    new ConsoleLogger(),
                    this.mockTokenManager.Object,
                    this.mockTokenHandler.Object,
                    this.mockMiseTokenHandler.Object,
                    this.mockAppConfiguration.Object);
                Assert.Fail("should not get here");
            }
            catch (NullReferenceException e)
            {
                Assert.AreEqual(e.Message, "AadTokenAuthGeneratorConfiguration was null.");
                throw e;
            }
        }

        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        [DynamicData(nameof(CreateAadAuthManagerTestData), DynamicDataSourceType.Method)]
        public void AadAuthManagerConstructorThrowOnNull(
            IPrivacyConfigurationManager config,
            ICertificateProvider certProvider,
            ILogger logger,
            ITokenManager tokenManager,
            IAadJwtSecurityTokenHandler tokenHandler,
            IMiseTokenValidationUtility miseTokenHandler,
            IAppConfiguration appConfiguration,
            string errorMessage)
        {
            try
            {
                var authManager = new AadAuthManager(config, certProvider, logger, tokenManager, tokenHandler, miseTokenHandler, appConfiguration);
                Assert.Fail("should not get here");
            }
            catch (ArgumentNullException e)
            {
                Assert.AreEqual(e.Message, errorMessage);
                throw e;
            }
        }

        [TestMethod]
        public async Task AadAuthManagerValidateIncomingPft_False()
        {
            var authManager = new AadAuthManager(
                this.mockConfig.Object,
                this.mockCertificateProvider.Object,
                new ConsoleLogger(),
                this.mockTokenManager.Object,
                this.mockTokenHandler.Object,
                this.mockMiseTokenHandler.Object,
                this.mockAppConfiguration.Object);

            var result = await authManager.ValidateInboundPftAsync(
                new AuthenticationHeaderValue("MSAuth1.0", ExpiredTestOnlyToken),
                Guid.NewGuid()).ConfigureAwait(false);

            Assert.IsFalse(result.Succeeded);
        }

        [TestMethod,
         Ignore,
         Description(
             "This test is intended to help debugging against test tokens to determine the right configuration. It's meant to be ignored so it won't run during checkin.")]
        public async Task AadAuthManagerValidateIncomingPft_Success()
        {
            var authManager = new AadAuthManager(
                this.mockConfig.Object,
                this.mockCertificateProvider.Object,
                new ConsoleLogger(),
                this.mockTokenManager.Object,
                this.mockTokenHandler.Object,
                this.mockMiseTokenHandler.Object,
                this.mockAppConfiguration.Object);
            var authHeader = new AuthenticationHeaderValue("MSAuth1.0", ExpiredTestOnlyToken);
            var result = await authManager.ValidateInboundPftAsync(
                authHeader,
                Guid.NewGuid()).ConfigureAwait(false);

            foreach (var diagnosticLog in result.DiagnosticLogs)
            {
                Console.WriteLine(diagnosticLog);
            }

            Assert.IsTrue(result.Succeeded);
        }

        [TestMethod]
        public void CoverAadLoginEndpointAndStsAuthorityEndpointProperty()
        {
            var authManager = new AadAuthManager(
                this.mockConfig.Object,
                this.mockCertificateProvider.Object,
                new ConsoleLogger(),
                this.mockTokenManager.Object,
                this.mockTokenHandler.Object,
                this.mockMiseTokenHandler.Object,
                this.mockAppConfiguration.Object);

            Assert.AreEqual("login.microsoftonline.com", authManager.AadLoginEndpoint);
            Assert.AreEqual("sts.com", authManager.StsAuthorityEndpoint);
        }

        [TestMethod]
        public void GetAccessTokenAsyncSuccess()
        {
            var authManager = new AadAuthManager(
                this.mockConfig.Object,
                this.mockCertificateProvider.Object,
                new ConsoleLogger(),
                this.mockTokenManager.Object,
                this.mockTokenHandler.Object,
                this.mockMiseTokenHandler.Object,
                this.mockAppConfiguration.Object);

            var result = authManager.GetAccessTokenAsync("This_is_a_resource");
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsFaulted);
            Assert.AreEqual(AppTokenValue, result.Result);
        }

        [TestMethod]
        public void GetAccessTokenAsyncRethrowsExceptionOnFailure()
        {
            var authManager = new AadAuthManager(
                this.mockConfig.Object,
                this.mockCertificateProvider.Object,
                new ConsoleLogger(),
                this.mockTokenManager.Object,
                this.mockTokenHandler.Object,
                this.mockMiseTokenHandler.Object,
                this.mockAppConfiguration.Object);

            string badResourceId = "bad_resource_id";
            this.mockTokenManager
                .Setup(c => c.GetAppTokenAsync(It.IsAny<string>(), It.IsAny<string>(), badResourceId, It.IsAny<X509Certificate2>(), It.IsAny<bool>(), It.IsAny<ILogger>()))
                .Throws<Exception>();

            var result = authManager.GetAccessTokenAsync(badResourceId);
            Assert.IsTrue(result.IsFaulted);
        }

        [TestMethod]
        [DataRow(OutboundPolicyName.AadRvsConstructAccountClose, "59MXn7hhGGXNb9KKL0gm")]
        public async Task ShouldErrorSetAppTokenIfOutboundPolicyDoesNotExist(OutboundPolicyName outboundPolicy, string accessToken)
        {
            this.mockAadTokenAuthGeneratorConfiguration
                .Setup(c => c.JwtOutboundPolicyConfig)
                .Returns(new Dictionary<string, IJwtOutboundPolicyConfig>());
            var authManager = new AadAuthManager(
                this.mockConfig.Object,
                this.mockCertificateProvider.Object,
                new ConsoleLogger(),
                this.mockTokenManager.Object,
                this.mockTokenHandler.Object,
                this.mockMiseTokenHandler.Object,
                this.mockAppConfiguration.Object);

            try
            {
                HttpRequestMessage message = new HttpRequestMessage();
                await authManager.SetAuthorizationHeaderProtectedForwardedTokenAsync(message.Headers, accessToken, outboundPolicy).ConfigureAwait(false);

                Assert.Fail("Should have thrown");
            }
            catch (ArgumentOutOfRangeException e)
            {
                Assert.AreEqual(
                    $"Target outbound poilcy not supported: {outboundPolicy}" + Environment.NewLine +
                    "Parameter name: targetOutboundPolicyName" + Environment.NewLine +
                    $"Actual value was {outboundPolicy}.",
                    e.Message);
            }
        }

        [TestMethod]
        public async Task ShouldSetAppTokenAuthorizationHeaderSuccess()
        {
            IJwtOutboundPolicy jwtOutboundPolicy = new JwtOutboundPolicy(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "login.com", "sts.com");

            var authManager = new AadAuthManager(
                this.mockConfig.Object,
                this.mockCertificateProvider.Object,
                new ConsoleLogger(),
                this.mockTokenManager.Object,
                this.mockTokenHandler.Object,
                this.mockMiseTokenHandler.Object,
                this.mockAppConfiguration.Object);
            HttpRequestMessage message = new HttpRequestMessage();

            // Act
            await authManager.SetAuthorizationHeaderAppTokenAsync(message.Headers, jwtOutboundPolicy).ConfigureAwait(false);

            Assert.AreEqual("Bearer", message.Headers.Authorization.Scheme);
            Assert.AreEqual(AppTokenValue, message.Headers.Authorization.Parameter);
        }

        [TestMethod]
        [DataRow(OutboundPolicyName.AadRvsConstructAccountClose, "59MXn7hhGGXNb9KKL0gm")]
        public async Task ShouldSetAuthorizationHeaderProtectedForwardedTokenSuccess(OutboundPolicyName outboundPolicy, string accessToken)
        {
            string expectedAuthHeader = $"actortoken=\"Bearer app_token_value\",accesstoken=\"Bearer {accessToken}\",type=\"PFAT\"";

            this.mockAadTokenAuthGeneratorConfiguration
                .Setup(c => c.JwtOutboundPolicyConfig)
                .Returns(new Dictionary<string, IJwtOutboundPolicyConfig> { { outboundPolicy.ToString(), this.mockJwtOutboundPolicyConfig.Object } });

            var authManager = new AadAuthManager(
                this.mockConfig.Object,
                this.mockCertificateProvider.Object,
                new ConsoleLogger(),
                this.mockTokenManager.Object,
                this.mockTokenHandler.Object,
                this.mockMiseTokenHandler.Object,
                this.mockAppConfiguration.Object);
            HttpRequestMessage message = new HttpRequestMessage();

            // Act
            await authManager.SetAuthorizationHeaderProtectedForwardedTokenAsync(message.Headers, accessToken, outboundPolicy).ConfigureAwait(false);

            Assert.AreEqual("MSAuth1.0", message.Headers.Authorization.Scheme);
            Assert.AreEqual(expectedAuthHeader, message.Headers.Authorization.Parameter);
        }

        [TestMethod]
        public async Task ShouldSetAuthorizationHeaderProtectedForwardedTokenSuccess_OutboundPolicyNameNone()
        {
            string expectedAuthHeader = $"actortoken=\"Bearer app_token_value\",accesstoken=\"Bearer accesstoken\",type=\"PFAT\"";

            var authManager = new AadAuthManager(
                this.mockConfig.Object,
                this.mockCertificateProvider.Object,
                new ConsoleLogger(),
                this.mockTokenManager.Object,
                this.mockTokenHandler.Object,
                this.mockMiseTokenHandler.Object,
                this.mockAppConfiguration.Object);
            HttpRequestMessage message = new HttpRequestMessage();

            // Act
            await authManager.SetAuthorizationHeaderProtectedForwardedTokenAsync(
                message.Headers,
                "accesstoken",
                new JwtOutboundPolicy(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "login.com", "sts.com")).ConfigureAwait(false);

            Assert.AreEqual("MSAuth1.0", message.Headers.Authorization.Scheme);
            Assert.AreEqual(expectedAuthHeader, message.Headers.Authorization.Parameter);
        }

        [TestMethod]
        [DataRow("", "", "", false)]
        [DataRow("97139e53-efe6-4d42-8f59-347ddab4a1e7", "", "", false)]
        [DataRow("97139e53-efe6-4d42-8f59-347ddab4a1e7", "359809b9-f0b5-41a8-b4b2-f3588c003aa8", "", false)]
        [DataRow("97139e53-efe6-4d42-8f59-347ddab4a1e7", "i.am.not.a.guid", "a41ff7b3-a92b-4387-a90e-9be782294e7f", true)]
        [DataRow("97139e53-efe6-4d42-8f59-347ddab4a1e7", "359809b9-f0b5-41a8-b4b2-f3588c003aa8", "i.am.not.a.guid", false)]
        [DataRow("97139e53-efe6-4d42-8f59-347ddab4a1e7", "359809b9-f0b5-41a8-b4b2-f3588c003aa8", "a41ff7b3-a92b-4387-a90e-9be782294e7f", true)]
        public async Task ShouldValidateClaimsCorrectlyMise(string appIdValue, string oidValue, string tidValue, bool shouldSucceed)
        {
            Mock<ClaimsPrincipal> claimsPrincipal = new Mock<ClaimsPrincipal>();
            Mock<IIdentity> mockIdentity = new Mock<IIdentity>();
            mockIdentity.SetupGet(i => i.IsAuthenticated).Returns(true);
            claimsPrincipal.SetupGet(p => p.Identity).Returns(mockIdentity.Object);
            claimsPrincipal.Setup(p => p.FindFirst("appid")).Returns(new Claim("appId", appIdValue));
            this.mockMiseTokenHandler.Setup(h => h.AuthenticateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(claimsPrincipal.Object);
            var authManager = new AadAuthManager(
                this.mockConfig.Object,
                this.mockCertificateProvider.Object,
                new ConsoleLogger(),
                this.mockTokenManager.Object,
                this.mockTokenHandler.Object,
                this.mockMiseTokenHandler.Object,
                this.mockAppConfiguration.Object);

            string encodedJwt = AadAuthTestHelper.CreateUnitTestJwt(objectId: oidValue, tenantId: tidValue);
            var result = await authManager.ValidateInboundJwtAsync($"Bearer {encodedJwt}").ConfigureAwait(false);

            Assert.IsNotNull(result);

            if (shouldSucceed)
            {
                Assert.IsTrue(result.Succeeded);
                Assert.AreEqual(appIdValue, result.InboundAppId);
                Assert.AreEqual(Guid.TryParse(oidValue, out Guid oid) ? oidValue : default(Guid).ToString(), result.ObjectId.ToString());
                Assert.AreEqual(tidValue, result.TenantId.ToString());
            }
            else
            {
                Assert.IsFalse(result.Succeeded);
                Assert.AreEqual(result.Exception?.Message, "Cannot parse identity information from the input JWT");
            }
        }

        [TestMethod]
        [DataRow("invalidToken")]
        [DataRow("inva,lid,Token")]
        [DataRow("inva,Bearer,Bearer")]
        public void TryParseAccessTokenFail(string token)
        {
            bool canParse = AadAuthManager.TryParseAccessToken(
                token,
                out string accessToken);

            Assert.IsFalse(canParse);
            Assert.IsNull(accessToken);
        }

        [TestMethod]
        public void TryParseAccessTokenSuccess()
        {
            bool canParse = AadAuthManager.TryParseAccessToken(
                ExpiredTestOnlyToken,
                out string accessToken);

            Assert.IsTrue(canParse);
            Assert.AreEqual(
                @"eyJ0eXAiOiJKV1QiLCJub25jZSI6ImZEcFN1U2ZzSWRZd2QtWWtidEp3blpsNUd2ZlZYODJMcXpnVEQzMF9BZzQiLCJhbGciOiJSUzI1NiIsIng1dCI6InFXMDAtQnM3b2ZzWS1YdmIzMVFMOEN5bUhPMCIsImtpZCI6InFXMDAtQnM3b2ZzWS1YdmIzMVFMOEN5bUhPMCJ9.eyJhdWQiOiJodHRwczovL2dyYXBoLm1pY3Jvc29mdC1wcGUuY29tIiwiaXNzIjoiaHR0cHM6Ly9zdHMud2luZG93cy1wcGUubmV0Lzg4MThhZDk3LTI1NDctNDE3MS1hZGY1LThiNDE2NjI3MmY0MC8iLCJpYXQiOjE1MTE4Mjk1NDksIm5iZiI6MTUxMTgyOTU0OSwiZXhwIjoxNTExODMzNDQ5LCJhY3IiOiIxIiwiYWlvIjoiQVNRQTIvOEpBQUFBaWN6WXVUbkJsYXhoeGk2K2Y3ZFJpWURDQ1FWVFhtd3k1TERXeEgvVUY5az0iLCJhbXIiOlsicHdkIl0sImFwcF9kaXNwbGF5bmFtZSI6InNkdGVzdC1BbGxBY2Nlc3MiLCJhcHBpZCI6ImVmOTY0ODNiLTRlN2YtNDY4MC05ZjAzLTk0MTJmZTkyZWZhZCIsImFwcGlkYWNyIjoiMSIsImZhbWlseV9uYW1lIjoiRG9lIiwiZ2l2ZW5fbmFtZSI6IkpvaG4iLCJpcGFkZHIiOiIxNjcuMjIwLjEuMTYyIiwibmFtZSI6IlRFU1RfVEVTVF9TUE9Qcm92SGVhcnRiZWF0X0UzXzE1XzE3MDgzMTIzMDBfMzQ2Iiwib2lkIjoiYjk2OTg0MmEtMzk2YS00NjVkLWI3NzQtNzk1NGQwMzEwYjE4IiwicGxhdGYiOiIxNCIsInB1aWQiOiIxMDAzM0ZGRkE0Nzk5QjZCIiwic2NwIjoiQ2FsZW5kYXJzLlJlYWQgQ2FsZW5kYXJzLlJlYWRXcml0ZSBDb250YWN0cy5SZWFkIENvbnRhY3RzLlJlYWRXcml0ZSBEaXJlY3RvcnkuQWNjZXNzQXNVc2VyLkFsbCBEaXJlY3RvcnkuUmVhZC5BbGwgRGlyZWN0b3J5LlJlYWRXcml0ZS5BbGwgZW1haWwgRmlsZXMuUmVhZCBGaWxlcy5SZWFkLkFsbCBGaWxlcy5SZWFkLlNlbGVjdGVkIEZpbGVzLlJlYWRXcml0ZSBGaWxlcy5SZWFkV3JpdGUuQWxsIEZpbGVzLlJlYWRXcml0ZS5BcHBGb2xkZXIgRmlsZXMuUmVhZFdyaXRlLlNlbGVjdGVkIEdyb3VwLlJlYWQuQWxsIEdyb3VwLlJlYWRXcml0ZS5BbGwgSWRlbnRpdHlSaXNrRXZlbnQuUmVhZC5BbGwgTWFpbC5SZWFkIE1haWwuUmVhZFdyaXRlIE1haWwuU2VuZCBNYWlsYm94U2V0dGluZ3MuUmVhZFdyaXRlIE5vdGVzLkNyZWF0ZSBOb3Rlcy5SZWFkIE5vdGVzLlJlYWQuQWxsIE5vdGVzLlJlYWRXcml0ZSBOb3Rlcy5SZWFkV3JpdGUuQWxsIE5vdGVzLlJlYWRXcml0ZS5DcmVhdGVkQnlBcHAgb2ZmbGluZV9hY2Nlc3Mgb3BlbmlkIFBlb3BsZS5SZWFkIFBlb3BsZS5SZWFkV3JpdGUgcHJvZmlsZSByZWNpcGllbnQubWFuYWdlIFNpdGVzLlJlYWQuQWxsIFRhc2tzLlJlYWRXcml0ZSBVc2VyLlJlYWQgVXNlci5SZWFkLkFsbCBVc2VyLlJlYWRCYXNpYy5BbGwgVXNlci5SZWFkV3JpdGUgVXNlci5SZWFkV3JpdGUuQWxsIiwic3ViIjoiSFpMNFJBU0hDMWtEY3hRckRTQk1qMG11UFNRZjBMc25KZGhwMDlUcGFlYyIsInRpZCI6Ijg4MThhZDk3LTI1NDctNDE3MS1hZGY1LThiNDE2NjI3MmY0MCIsInVuaXF1ZV9uYW1lIjoiYWRtaW5AYTgzMGVkYWQ5MDUwODQ5MzQ2RTE3MDgzMTIzLmNjc2N0cC5uZXQiLCJ1cG4iOiJhZG1pbkBhODMwZWRhZDkwNTA4NDkzNDZFMTcwODMxMjMuY2NzY3RwLm5ldCIsInV0aSI6IlZQZVVuTFo2MGs2UktRZ3FDYThFQUEiLCJ2ZXIiOiIxLjAiLCJ3aWRzIjpbIjYyZTkwMzk0LTY5ZjUtNDIzNy05MTkwLTAxMjE3NzE0NWUxMCJdfQ.ESTSSIGNATURE",
                accessToken);
        }

        [TestMethod]
        public void ValidateInboundJwtAsyncAndTryParseAuthorizationHeader()
        {
            var authManager = new AadAuthManager(
                this.mockConfig.Object,
                this.mockCertificateProvider.Object,
                new ConsoleLogger(),
                this.mockTokenManager.Object,
                this.mockTokenHandler.Object,
                this.mockMiseTokenHandler.Object,
                this.mockAppConfiguration.Object);

            var result = authManager.ValidateInboundJwtAsync("This");
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsCompleted);
        }

        [TestMethod]
        public async Task ValidateJwtCannotValidateJwtUnauthenticatedClaimsPrincipalMise()
        {
            Mock<ClaimsPrincipal> claimsPrincipal = new Mock<ClaimsPrincipal>();
            Mock<IIdentity> mockIdentity = new Mock<IIdentity>();

            // Setup: Unauthenticated Claims Principal
            mockIdentity.SetupGet(i => i.IsAuthenticated).Returns(false);
            claimsPrincipal.SetupGet(p => p.Identity).Returns(mockIdentity.Object);
            this.mockMiseTokenHandler.Setup(h => h.AuthenticateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(claimsPrincipal.Object);
            var authManager = new AadAuthManager(
                this.mockConfig.Object,
                this.mockCertificateProvider.Object,
                new ConsoleLogger(),
                this.mockTokenManager.Object,
                this.mockTokenHandler.Object,
                this.mockMiseTokenHandler.Object,
                this.mockAppConfiguration.Object);

            string encodedJwt = AadAuthTestHelper.CreateUnitTestJwt();
            var result = await authManager.ValidateInboundJwtAsync($"Bearer {encodedJwt}").ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(result.Exception?.Message, "Cannot validate the input JWT");
        }

        [TestMethod]
        public async Task ValidateJwtSuccessMise()
        {
            Mock<ClaimsPrincipal> claimsPrincipal = new Mock<ClaimsPrincipal>();
            Mock<IIdentity> mockIdentity = new Mock<IIdentity>();
            mockIdentity.SetupGet(i => i.IsAuthenticated).Returns(true);
            claimsPrincipal.SetupGet(p => p.Identity).Returns(mockIdentity.Object);

            Guid appId = Guid.NewGuid();
            claimsPrincipal.Setup(p => p.FindFirst("appid")).Returns(new Claim("appId", appId.ToString()));

            Guid oid = Guid.NewGuid();
            Guid tid = Guid.NewGuid();

            this.mockMiseTokenHandler.Setup(h => h.AuthenticateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(claimsPrincipal.Object);
            var authManager = new AadAuthManager(
                this.mockConfig.Object,
                this.mockCertificateProvider.Object,
                new ConsoleLogger(),
                this.mockTokenManager.Object,
                this.mockTokenHandler.Object,
                this.mockMiseTokenHandler.Object,
                this.mockAppConfiguration.Object);

            string encodedJwt = AadAuthTestHelper.CreateUnitTestJwt(objectId: oid.ToString(), tenantId: tid.ToString());
            var result = await authManager.ValidateInboundJwtAsync($"Bearer {encodedJwt}").ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(appId.ToString(), result.InboundAppId);
            Assert.AreEqual(oid, result.ObjectId);
            Assert.AreEqual(tid, result.TenantId);
        }

        [TestMethod]
        public async Task ValidateJwtCannotValidateJwtNullClaimsPrincipalMise()
        {
            // Setup: Null Claims Principal
            this.mockMiseTokenHandler.Setup(h => h.AuthenticateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ClaimsPrincipal) null);
            var authManager = new AadAuthManager(
                this.mockConfig.Object,
                this.mockCertificateProvider.Object,
                new ConsoleLogger(),
                this.mockTokenManager.Object,
                this.mockTokenHandler.Object,
                this.mockMiseTokenHandler.Object,
                this.mockAppConfiguration.Object);

            Guid oid = Guid.NewGuid();
            Guid tid = Guid.NewGuid();
            string encodedJwt = AadAuthTestHelper.CreateUnitTestJwt(objectId: oid.ToString(), tenantId: tid.ToString());
            var result = await authManager.ValidateInboundJwtAsync($"Bearer {encodedJwt}").ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(result.Exception?.Message, "Cannot validate the input JWT");
        }

        [TestMethod]
        public async Task ValidateJwtCannotValidateJwtBadOpenIdConfiguration()
        {
            // Setup: Bad openid configuration
            this.mockTokenHandler.Setup(m => m.GetConnectConfigurationAsync(It.IsAny<JwtSecurityToken>(), It.IsAny<IConfigurationRetriever<OpenIdConnectConfiguration>>()))
                .Throws(new SecurityTokenException("Unable to get OpenIdConnectConfiguration"));
            var authManager = new AadAuthManager(
                this.mockConfig.Object,
                this.mockCertificateProvider.Object,
                new ConsoleLogger(),
                this.mockTokenManager.Object,
                this.mockTokenHandler.Object,
                this.mockMiseTokenHandler.Object,
                this.mockAppConfiguration.Object);

            Guid oid = Guid.NewGuid();
            Guid tid = Guid.NewGuid();
            string encodedJwt = AadAuthTestHelper.CreateUnitTestJwt(objectId: oid.ToString(), tenantId: tid.ToString());
            var result = await authManager.ValidateInboundJwtAsync($"Bearer {encodedJwt}").ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(result.Exception?.Message, "Unable to get OpenIdConnectConfiguration");
        }

        private void CreateMockConfiguration()
        {
            this.mockConfig = new Mock<IPrivacyConfigurationManager>(MockBehavior.Strict);
            this.mockAadTokenAuthGeneratorConfiguration = new Mock<IAadTokenAuthConfiguration>(MockBehavior.Strict);
            this.mockTokenManager = new Mock<ITokenManager>(MockBehavior.Strict);
            this.mockTokenManager
                .Setup(c => c.GetAppTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<X509Certificate2>(), It.IsAny<bool>(), It.IsAny<ILogger>()))
                .ReturnsAsync(AppTokenValue);

            var mockConnectConfiguration = new Mock<IConnectConfigurationWrapper>(MockBehavior.Strict);
            mockConnectConfiguration.SetupGet(c => c.Issuer).Returns("I'm an issuer");
            mockConnectConfiguration.SetupGet(c => c.SigningKeys).Returns(new Collection<SecurityKey>(new List<SecurityKey>()));

            this.mockTokenHandler = new Mock<IAadJwtSecurityTokenHandler>();
            var mockCertConfiguration = new Mock<ICertificateConfiguration>(MockBehavior.Strict);
            mockCertConfiguration.Setup(c => c.Subject).Returns("aad-ame2.ppe.dpp.microsoft.com");

            var mockJwtInboundPolicyConfig = new Mock<IJwtInboundPolicyConfig>(MockBehavior.Strict);
            mockJwtInboundPolicyConfig.Setup(c => c.AadTenantName).Returns("PartnerTenantName");
            mockJwtInboundPolicyConfig.Setup(c => c.Authority).Returns("https://sts.windows-ppe.net/8818ad97-2547-4171-adf5-8b4166272f40/");
            mockJwtInboundPolicyConfig.Setup(c => c.IssuerPrefixes).Returns(new [] { "https://sts.windows-ppe.net" });
            mockJwtInboundPolicyConfig.Setup(c => c.ValidIncomingAppIds).Returns(new[] { "00000003-0000-0000-c000-000000000000" });
            mockJwtInboundPolicyConfig.Setup(c => c.Audiences).Returns(new[] { "https://www.something.com" });
            mockJwtInboundPolicyConfig.Setup(c => c.ApplyPolicyForAllTenants).Returns(true);

            this.mockJwtOutboundPolicyConfig = new Mock<IJwtOutboundPolicyConfig>(MockBehavior.Strict);
            this.mockJwtOutboundPolicyConfig.Setup(c => c.AppId).Returns("outbound app id");
            this.mockJwtOutboundPolicyConfig.Setup(c => c.Authority).Returns("authority outbound");
            this.mockJwtOutboundPolicyConfig.Setup(c => c.Resource).Returns("resource");

            this.mockAadTokenAuthGeneratorConfiguration.Setup(c => c.JwtInboundPolicyConfig).Returns(mockJwtInboundPolicyConfig.Object);
            this.mockAadTokenAuthGeneratorConfiguration.Setup(c => c.AadAppId).Returns("my app id");
            this.mockAadTokenAuthGeneratorConfiguration.Setup(c => c.RequestSigningCertificateConfiguration).Returns(mockCertConfiguration.Object);
            this.mockAadTokenAuthGeneratorConfiguration.Setup(c => c.AuthorityTenantId).Returns(Guid.NewGuid().ToString());
            this.mockAadTokenAuthGeneratorConfiguration.Setup(c => c.AadLoginEndpoint).Returns("login.microsoftonline.com");
            this.mockAadTokenAuthGeneratorConfiguration.Setup(c => c.StsAuthorityEndpoint).Returns("sts.com");
            this.mockAadTokenAuthGeneratorConfiguration.Setup(c => c.EnableShowPIIDiagnosticLogs).Returns(true);
            this.mockAadTokenAuthGeneratorConfiguration
                .Setup(c => c.JwtOutboundPolicyConfig)
                .Returns(
                    new Dictionary<string, IJwtOutboundPolicyConfig>
                    {
                        { OutboundPolicyName.AadRvsConstructAccountClose.ToString(), this.mockJwtOutboundPolicyConfig.Object }
                    });

            this.mockConfig.Setup(c => c.AadTokenAuthGeneratorConfiguration).Returns(this.mockAadTokenAuthGeneratorConfiguration.Object);

            this.mockCertificateProvider = new Mock<ICertificateProvider>(MockBehavior.Strict);
            this.mockCertificateProvider.Setup(c => c.GetClientCertificate(It.IsAny<string>(), It.IsAny<StoreLocation>())).Returns(UnitTestData.UnitTestCertificate);

            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.AuthenticationLogging, true)).ReturnsAsync(false);
            this.mockMiseTokenHandler = new Mock<IMiseTokenValidationUtility>();
            this.mockMiseTokenHandler.Setup(c => c.AuthenticateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()));
        }

        private static IEnumerable<object[]> CreateAadAuthManagerTestData()
        {
            var config = new Mock<IPrivacyConfigurationManager>();
            var certProvider = new Mock<ICertificateProvider>();
            var logger = new Mock<ILogger>();
            var tokenManager = new Mock<ITokenManager>();
            var tokenHandler = new Mock<IAadJwtSecurityTokenHandler>();
            var miseTokenHandler = new Mock<IMiseTokenValidationUtility>();
            var appConfiguration = new Mock<IAppConfiguration>();

            return new List<object[]>
            {
                new object[]
                {
                    null,
                    certProvider.Object,
                    logger.Object,
                    tokenManager.Object,
                    tokenHandler.Object,
                    miseTokenHandler.Object,
                    appConfiguration.Object,
                    "Value cannot be null." + Environment.NewLine + "Parameter name: config"
                },
                new object[]
                {
                    config.Object,
                    null,
                    logger.Object,
                    tokenManager.Object,
                    tokenHandler.Object,
                    miseTokenHandler.Object,
                    appConfiguration.Object,
                    "Value cannot be null." + Environment.NewLine + "Parameter name: certProvider"
                },
                new object[]
                {
                    config.Object,
                    certProvider.Object,
                    null,
                    tokenManager.Object,
                    tokenHandler.Object,
                    miseTokenHandler.Object,
                    appConfiguration.Object,
                    "Value cannot be null." + Environment.NewLine + "Parameter name: logger"
                },
                new object[]
                {
                    config.Object,
                    certProvider.Object,
                    logger.Object,
                    null,
                    tokenHandler.Object,
                    miseTokenHandler.Object,
                    appConfiguration.Object,
                    "Value cannot be null." + Environment.NewLine + "Parameter name: tokenManager"
                },
                new object[]
                {
                    config.Object,
                    certProvider.Object,
                    logger.Object,
                    tokenManager.Object,
                    null,
                    miseTokenHandler.Object,
                    appConfiguration.Object,
                    "Value cannot be null." + Environment.NewLine + "Parameter name: tokenHandler"
                },
                new object[]
                {
                    config.Object,
                    certProvider.Object,
                    logger.Object,
                    tokenManager.Object,
                    tokenHandler.Object,
                    null,
                    appConfiguration.Object,
                    "Value cannot be null." + Environment.NewLine + "Parameter name: miseTokenHandler"
                },
                new object[]
                {
                    config.Object,
                    certProvider.Object,
                    logger.Object,
                    tokenManager.Object,
                    tokenHandler.Object,
                    miseTokenHandler.Object,
                    null,
                    "Value cannot be null." + Environment.NewLine + "Parameter name: appConfiguration"
                }
            };
        }
    }
}

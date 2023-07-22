// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.UnitTests.Helpers.AadToken
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Security.Claims;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Factory;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers;
    using Microsoft.OSGS.HttpClientCommon;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class AadTokenProviderTests
    {
        private const string AadAppId = "AADAPPID";
        private const string AadIsser = "AADISSUER";
        private const string LoginUri = "https://www.example.org/login";
        private const int AadAppTokenExpirySeconds = 8;
        private const int MaxCacheAgeAadAppTokenExpirySeconds = 2;

        // the AadTokenProvider code below needs a cert to test with, so I reused one I had on my local machine generated via the
        //  CA on my dev machine (which in turn is only used to generate certs for local testing purposes and is not trusted by
        //  anything else); this cert is currently used for nothing except this test.
        private const string SelfSignedUnitTestCert =
            "MIILgAIBAzCCC0AGCSqGSIb3DQEHAaCCCzEEggstMIILKTCCBhIGCSqGSIb3DQEHAaCCBgMEggX/MIIF+zCCBfcGCyqGSIb3DQEMCgECoIIE9jCCBPIwH" +
            "AYKKoZIhvcNAQwBAzAOBAhoDf4a7CUtigICB9AEggTQ2EtnNYnHqNmYF97Br0d69IhIYBbgc90NsAWE/hqo4srL2kJJu8HiIyL6F+Dhx+3C3oDiUzx4fm" +
            "G7zH3XuHIb6okpHJuWlCfd61H82IxrwA2tbG6mRaDYIzymsUuxRX9sEoX6bz1x7YQI2DkPIXXSKD0AQy5Hkfl1ma9HSIziOGifagxTi1ga79qONKPCWBX" +
            "HWWjQ07X01IS3ao61wqKX/IfGgme3/YJwTnbLExtY1LByckk9V5u4o+kgBMZfeWbVX9vXcEAGJFSrIW7WGH5IAevvEgtEBrHGriJf/3vHUCTOEpS68TnZ" +
            "7889yrlkCOekjx5/+59k5BOea8epAhV11JPfMqPyofcL4v1llyNOhpy21OV514LBGdJKlZIV47y6TQXxeBJcDqhWNzMdTqiJPRXgY2mfLHkNxY6gXavoB" +
            "WQAY9ruzUW5wP0OaLSv7SV4cF8yhmM6oAuTk35wSyF4ceSWvz5uaudk6+mCDt3dhLaBfnmyMlPIV4JCga4gPv3YhK8DNommOX3ljTzXYR3qgwpXipwS+q" +
            "2OpkUFZFqzdxDNoteeFIPpiYLIwOfh0AroVBumNmY9dJ6VvlN7CXOheXDwpNHiyLeDBkEgKAQeUAgeu6tcez4p66iyTLasWe5sjxAHcXFj2yQ/3cnUxRq" +
            "AATEeyTM4RLy/03wcWVojiPfaMhqlE1C8ys81jD6m6GtShipyjxT7IpUkCCxc3PpnzMizqUJ3+VYcV4UPB5W8KDK27wbEdsVhQ++9dUHLdo5Rs17lPOa6" +
            "Pu0N3uzT6SunkbmoVMX7OMU2q7jVimuuZPQ2XcZL0KVray2r06d/6L/IxWdXhQaOQMYv3j1zOyfPcF9iTU8LM/KXJyOxGsOrOCDjU1wgXimBpCCqZda1o" +
            "f7DluknEUI9NK1JssswqxiiRZFItw6kv+aULM0zgIDcvF+vb+T6TycsEQaVhN2liOOB82dQdBK6nTMPZTyOkBIISCHCDqIV58YqECRBQJaYHfXGsJhEUr" +
            "kK51wmyQcVlhI/lgbf1sWPtuw2d1cZXGFV3bA1WSMJqyTbeD4AZN+NGFa1xu0SBr8H96dIvQTUwOApkVEjIbkBkZWRzskUCRUTq4JKBCUnGKfsAlItIx4" +
            "7hqc+Llp9rgR5QwVNifVamdUzvQ6FBDEwAtI90HA8WL7J2jdMjUHzfrvs3Stdifd9pKvYELEmOUMAkp7TsR3qdXUtPLI1pCy8NZSbAKBZxNPmPb5CUPh0" +
            "JgSiwPcbFsQ/ZYuEiXH6InlRJcg7SratduJJXUSrcJBcoX/5+KCo5LIKobgQRQbpYkcruMALu9Do/y5XyrQMPGPxPmzunhz2yF2+4FN8xxqyIFhlNEITj" +
            "uiVDkY/n0cpCaVuZytkWW47JS+DMJ7Q6m0nhZDuYSW5ZnFgGdifsy9YoO8zbGjODWYHsoPOe53dCdHq8ZIaSLNSIiAQjAk0NnJw3qL4jGbIgi34FFve0F" +
            "j0zuM1JLk9+tadflsFa1dZgjKXpjcLAc/5xS1C5tR7Y1VCN9GR5dtTBYO/Z+m/88Z46Og6/CsUs10ywuCYgC5mVNkYhKYHJAkHp35quiWHg2QPq5QiATM" +
            "MHStnKdLKR9ZS3RgWZST2/y6Q/lUTEnV5xeuIp6T92spTJi8xge0wEwYJKoZIhvcNAQkVMQYEBAEAAAAwWwYJKoZIhvcNAQkUMU4eTAB7ADUANwAzADMA" +
            "RgBFADQARgAtADUAQgBGAEYALQA0AEEAQQAyAC0AQQA0ADYAQgAtAEIARQAzADIARAA5ADgANAAxAEQAQQBEAH0weQYJKwYBBAGCNxEBMWweagBNAGkAY" +
            "wByAG8AcwBvAGYAdAAgAEUAbgBoAGEAbgBjAGUAZAAgAFIAUwBBACAAYQBuAGQAIABBAEUAUwAgAEMAcgB5AHAAdABvAGcAcgBhAHAAaABpAGMAIABQAH" +
            "IAbwB2AGkAZABlAHIwggUPBgkqhkiG9w0BBwagggUAMIIE/AIBADCCBPUGCSqGSIb3DQEHATAcBgoqhkiG9w0BDAEGMA4ECFAgiMdlRw2BAgIH0ICCBMi" +
            "kTxiQu6+Wc1dnbEsNPlBQeryUOHbwO2PvmdnRX37W6+WWyX123YwlIu2OhBZGZ07gQgD4FtZQsk60KlP5XhttHSJThbDIVioI1fygkpFIEpysI3hDVKZA" +
            "X2ClRKNROrJyBfT0fDW129h0egOwZLHrFFbHYmXWAinfJndTlCPC3v2JLPi4EdQzMkt87JsjhzdRdiDFadd+wkikKCvy9HDukmxmbs1r4lLwN1UyULnPU" +
            "tqKXWsBBhFyOdGIGGLudV5RPVfBFMQPULaN+WHO+0BUxPiwoyLw2BOxQeeHWbCC8sRVWaTWah0+jYas7aRUP6Hsozr1q2Yt1pCnosSXOg3N2c7odprDGC" +
            "gmj2IjwauoeF9UrJupB4Vg95F2bR0/2y4mS3uYSCnpKH9OvYfBCSYrWSmc+gTntPN5R0dfyLhMy4Xi8k9hZIP/9PncMgXB1fKJkmMGcXOLj7B5bNLZhhz" +
            "GY71Yc62bGA4G4Zie4uBMUQTwik8mivWHjNjsRnSKYcH6YRBulpw/fAS/h3YT3wukVTtvYAZnuMq5bRbGEx5xFl28gwCyCB3EVpx/9jyRTt8fYij7/IJk" +
            "hio2EuPA0wwViW4BUzvoY66RyRxoBxcDVCxBYXfBHN9QyyemxaG/Yl48B9CEuMlCwIxHnyyVpeU6IH1IbFMBIrAM50ayOu8Kxn0QoVhzslLpFYWx4dJB5" +
            "A+ESiOahMPYpGsTIyj/6oAxW9H7v5VMWkz6D5hAaZ0xSBYzXAX4rcCbYVmoiyOSbujL4dH+DmH/bWAogwk0IQ1teYLIEAwS13WXDFLaW8YrhhjysoiRyQ" +
            "bf5J8pTmLAjtlA4ISBDpouBKXbyv/0HhW95vlLsPbu5lXDx09Y1apHxfyTmt4YNQJnhbaHfPMiO98EnlweTQ3hJEv43xwzsbbyJNq3dBsMSYjBenkfDVo" +
            "R8I7IJ8FhT/NYX88fGEK6u61j4jGha6cU70xXTez9wwF5D5w+eBbxixLmxCkksLxPmgeuRD/tzRogFMEnv9eIpOEgvets5icmYSFs1b4NsTmnWyYGfuNS" +
            "dbpbsBDve6EBQnNVigMPcksBhJjTEIN0f77x4AzpIOQTaHTNWdUFifMz9gD9oBxdlwNPPcumyQu4TCoXqR5D+BHCCjBp9jkBaI2PDGucfwpP5vTEubSPQ" +
            "aTsBZjaS3A3pW/e5tvlbQMKOvLyWDHXeTrfsYnpV2hnQzqeB+3pAS9tolcFXkSdm7LUdNr4jlB/rTDIhuO/JTkHcjGt/FelfBxSKi/S6KLCfMYWdX4T8I" +
            "Mqsdu+zmVnzyWiXv7QqNXohCW+MNhYp6AnPy17iMuOk3fO2A2rrMWkB77jq/h+wu5RFwrKqDuhuDnCtMj25x7+HWJdxGKRso8Du3RWXyil4RroWtKiaZX" +
            "7O8BSbq0T+25+ohn2KMWaHPaC78jxccr+DMpryGULfexj1jtM2hrCNIhN8H9u/EGjJHY3SPsHQ9q+Kb3942sqQvTTaE/1G0mALxS4WwWIFIG4NJGUJvrD" +
            "i9cVqUtJUu2C4Ql2DxmZ9Y78sDsPbKOXqbM1Ix3xX17AifHAdXPkIf8eEmkh9TRbvgHYK/7mpTcjBhR5Jaso7j8ZaK8aUPcifOPII9zogDZLY9kwNzAfM" +
            "AcGBSsOAwIaBBT4qsiZHGeskpAftfjKepe4vNLs9AQUT/3SVNM44Y23tbky6hEwel1U0I0=";

        private readonly Mock<IAadTokenAuthConfiguration> mockAadConfig = new Mock<IAadTokenAuthConfiguration>();
        private readonly Mock<IAadPopTokenAuthConfiguration> mockAadPopConfig = new Mock<IAadPopTokenAuthConfiguration>();
        private readonly Mock<ICertificateConfiguration> mockCertConfig = new Mock<ICertificateConfiguration>();
        private readonly Mock<ICertificateProvider> mockCertProv = new Mock<ICertificateProvider>();
        private readonly Mock<IHttpClient> mockClient = new Mock<IHttpClient>();
        private readonly Mock<IHttpClientFactory> mockClientFactory = new Mock<IHttpClientFactory>();
        private readonly Mock<IClock> mockClock = new Mock<IClock>();
        private readonly Mock<IPrivacyConfigurationManager> mockConfig = new Mock<IPrivacyConfigurationManager>();
        private readonly Mock<ICounterFactory> mockCounterFactory = new Mock<ICounterFactory>();
        private readonly Mock<ILogger> mockLog = new Mock<ILogger>();
        private readonly Mock<IAppConfiguration> mockAppConfig = new Mock<IAppConfiguration>();
        private readonly string tenantId = Guid.NewGuid().ToString("D");

        private AadTokenProvider testObj;

        [TestInitialize]
        public void Init()
        {
            this.mockConfig.SetupGet(o => o.AadTokenAuthGeneratorConfiguration).Returns(this.mockAadConfig.Object);
            this.mockAadConfig.SetupGet(o => o.RequestSigningCertificateConfiguration).Returns(this.mockCertConfig.Object);
            this.mockCertProv
                .Setup(o => o.GetClientCertificate(It.IsAny<ICertificateConfiguration>()))
                .Returns(new X509Certificate2(Convert.FromBase64String(SelfSignedUnitTestCert)));

            this.mockClientFactory.Setup(
                    o => o.CreateHttpClient(
                        It.IsAny<IPrivacyPartnerAdapterConfiguration>(),
                        It.IsAny<ICounterFactory>()))
                .Returns(this.mockClient.Object);

            this.mockAadPopConfig.SetupGet(o => o.CacheAppTokens).Returns(true);
            this.mockAadPopConfig.SetupGet(o => o.AadAppTokenExpirySeconds).Returns(AadAppTokenExpirySeconds);
            this.mockAadPopConfig.SetupGet(o => o.MaxCacheAgeAadAppTokenExpirySeconds).Returns(MaxCacheAgeAadAppTokenExpirySeconds);
            this.mockAadConfig.SetupGet(o => o.AadPopTokenAuthConfig).Returns(this.mockAadPopConfig.Object);
            this.mockAadConfig.SetupGet(o => o.BaseUrl).Returns(LoginUri);
            this.mockAadConfig.SetupGet(o => o.AadAppId).Returns(AadAppId);

            this.mockClock.SetupGet(o => o.UtcNow).Returns(DateTimeOffset.UtcNow);

            this.mockClient
                .Setup(o => o.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .Returns((HttpRequestMessage msg, CancellationToken ct) => this.GenerateResponse(msg));

            this.testObj = new AadTokenProvider(
                this.mockConfig.Object,
                this.mockCertProv.Object,
                this.mockClientFactory.Object,
                this.mockCounterFactory.Object,
                this.mockLog.Object,
                this.mockClock.Object,
                new MemoryCache(new MemoryCacheOptions()),
                this.mockAppConfig.Object);
        }

        [TestMethod]
        public async Task CanRetrieveCachedAccessTokenWhenNotExpired()
        {
            AadPopTokenRequest req = CreateAadPopTokenRequest();

            // seed the cache 
            string result = await this.testObj.GetPopTokenAsync(req, CancellationToken.None).ConfigureAwait(false);
            Assert.IsNotNull(result);
            this.mockClient.Verify(o => o.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once);
            this.mockClient.Invocations.Clear();

            // test a cached value is used when it's not expired (http client not called)
            result = await this.testObj.GetPopTokenAsync(req, CancellationToken.None).ConfigureAwait(false);
            Assert.IsNotNull(result);
            this.mockClient.Verify(o => o.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task CanRetrieveCachedAccessTokensForDifferentResourcesWhenNotExpired()
        {
            const string Resource1 = "resource1";
            const string Resource2 = "resource2";
            const string Scope = "scope";

            AadPopTokenRequest req1 = new AadPopTokenRequest
            {
                Resource = Resource1,
                Scope = Scope,
                Claims = new Dictionary<string, string> { { "claimKey", "claimValue" } },
                RequestUri = new Uri("https://example1.com/path"),
                HttpMethod = HttpMethod.Post,
                Type = AadPopTokenRequestType.AppAssertedUserToken
            };

            AadPopTokenRequest req2 = new AadPopTokenRequest
            {
                Resource = Resource2,
                Scope = Scope,
                Claims = new Dictionary<string, string> { { "msa_pt", "MsaUserProxyToken" } },
                RequestUri = new Uri("https://example2.com/path"),
                HttpMethod = HttpMethod.Post,
                Type = AadPopTokenRequestType.MsaProxyTicket
            };

            // seed the cache 
            string result1 = await this.testObj.GetPopTokenAsync(req1, CancellationToken.None).ConfigureAwait(false);
            string result2 = await this.testObj.GetPopTokenAsync(req2, CancellationToken.None).ConfigureAwait(false);
            Assert.IsNotNull(result1);
            Assert.IsNotNull(result2);

            this.mockClient.Verify(o => o.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            this.mockClient.Invocations.Clear();

            // test a cached value is used when it's not expired (http client not called)
            result1 = await this.testObj.GetPopTokenAsync(req1, CancellationToken.None).ConfigureAwait(false);
            result2 = await this.testObj.GetPopTokenAsync(req2, CancellationToken.None).ConfigureAwait(false);
            Assert.IsNotNull(result1);
            Assert.IsNotNull(result2);

            this.mockClient.Verify(o => o.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task CanSuccessfullyGetAValidTokenWhenRequested()
        {
            const string ClaimVal = "c1v";
            const string ClaimKey = "c1";
            const string Resource = "rsrc";
            const string Scope = "scop";

            AadPopTokenRequest req = new AadPopTokenRequest
            {
                Resource = Resource,
                Scope = Scope,
                Claims = new Dictionary<string, string> { { ClaimKey, ClaimVal } },
                RequestUri = new Uri("https://example.com/path"),
                HttpMethod = HttpMethod.Post,
                Type = AadPopTokenRequestType.AppAssertedUserToken
            };

            // test 
            string result = await this.testObj.GetPopTokenAsync(req, CancellationToken.None).ConfigureAwait(false);

            // verify
            Assert.IsNotNull(result);

            // this will throw if it's not a valid token
            JwtSecurityToken resultToken = new JwtSecurityToken(result);

            JwtSecurityToken accessToken = new JwtSecurityToken(
                resultToken.Claims.First(o => o.Type.Equals("aat", StringComparison.OrdinalIgnoreCase)).Value);

            JwtSecurityToken aadToken = new JwtSecurityToken(
                resultToken.Claims.First(o => o.Type.Equals("at", StringComparison.OrdinalIgnoreCase)).Value);

            Assert.AreEqual(
                HttpMethod.Post.ToString().ToUpperInvariant(),
                resultToken.Claims.First(o => "m".Equals(o.Type, StringComparison.OrdinalIgnoreCase)).Value);

            Assert.AreEqual("JWT", aadToken.Header.Typ);
            Assert.AreEqual(AadIsser, aadToken.Issuer);
            Assert.AreEqual(Resource, aadToken.Payload.Aud.First());
            Assert.AreEqual(
                AadAppId,
                aadToken.Claims.First(o => "appid".Equals(o.Type, StringComparison.OrdinalIgnoreCase)).Value);
            Assert.AreEqual(
                this.tenantId,
                aadToken.Claims.First(o => "tid".Equals(o.Type, StringComparison.OrdinalIgnoreCase)).Value);

            Assert.AreEqual("JWT", accessToken.Header.Typ);
            Assert.AreEqual(
                "app_asserted_user_v1",
                accessToken.Claims.First(o => "ver".Equals(o.Type, StringComparison.OrdinalIgnoreCase)).Value);
            Assert.AreEqual(
                this.tenantId,
                accessToken.Claims.First(o => "tid".Equals(o.Type, StringComparison.OrdinalIgnoreCase)).Value);
            Assert.AreEqual(
                AadAppId,
                accessToken.Claims.First(o => "appid".Equals(o.Type, StringComparison.OrdinalIgnoreCase)).Value);
            Assert.AreEqual(
                Scope,
                accessToken.Claims.First(o => "scp".Equals(o.Type, StringComparison.OrdinalIgnoreCase)).Value);
            Assert.AreEqual(Resource, accessToken.Payload.Aud.First());
        }

        [TestMethod]
        public async Task CanSuccessfullyGetValidMsaIdProxyTicketWhenRequested()
        {
            const string ClaimVal = "FullTrustMsaUserProxyTicket";
            const string ClaimKey = "msa_pt";
            const string Resource = "rsrc";
            const string Scope = "scop";

            AadPopTokenRequest req = new AadPopTokenRequest
            {
                Resource = Resource,
                Scope = Scope,
                Claims = new Dictionary<string, string> { { ClaimKey, ClaimVal } },
                RequestUri = new Uri("https://example.com/path"),
                HttpMethod = HttpMethod.Post,
                Type = AadPopTokenRequestType.MsaProxyTicket
            };

            // test 
            string result = await this.testObj.GetPopTokenAsync(req, CancellationToken.None).ConfigureAwait(false);

            // verify
            Assert.IsNotNull(result);

            // this will throw if it's not a valid token
            JwtSecurityToken resultToken = new JwtSecurityToken(result);

            string msaProxyTicket = resultToken.Claims.First(o => o.Type.Equals("msa_pt", StringComparison.OrdinalIgnoreCase)).Value;

            JwtSecurityToken aadToken = new JwtSecurityToken(
                resultToken.Claims.First(o => o.Type.Equals("at", StringComparison.OrdinalIgnoreCase)).Value);

            Assert.AreEqual(
                HttpMethod.Post.ToString().ToUpperInvariant(),
                resultToken.Claims.First(o => "m".Equals(o.Type, StringComparison.OrdinalIgnoreCase)).Value);

            Assert.AreEqual("JWT", aadToken.Header.Typ);
            Assert.AreEqual(AadIsser, aadToken.Issuer);
            Assert.AreEqual(Resource, aadToken.Payload.Aud.First());
            Assert.AreEqual(
                AadAppId,
                aadToken.Claims.First(o => "appid".Equals(o.Type, StringComparison.OrdinalIgnoreCase)).Value);
            Assert.AreEqual(
                this.tenantId,
                aadToken.Claims.First(o => "tid".Equals(o.Type, StringComparison.OrdinalIgnoreCase)).Value);

            Assert.AreEqual(ClaimVal, msaProxyTicket);
        }

        [TestMethod]
        public async Task ShouldNotCacheIfCacheDisabled()
        {
            AadPopTokenRequest req = CreateAadPopTokenRequest();
            this.mockAadPopConfig.SetupGet(o => o.CacheAppTokens).Returns(false);

            // seed the cache 
            string result = await this.testObj.GetPopTokenAsync(req, CancellationToken.None).ConfigureAwait(false);
            Assert.IsNotNull(result);
            this.mockClient.Verify(o => o.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once);
            this.mockClient.Invocations.Clear();
            this.mockLog.Invocations.Clear();

            // test a cached value is NOT used when normally it would've been
            result = await this.testObj.GetPopTokenAsync(req, CancellationToken.None).ConfigureAwait(false);
            Assert.IsNotNull(result);
            this.mockClient.Verify(o => o.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task ShouldRefreshCacheWhenCacheIsExpired()
        {
            AadPopTokenRequest req = CreateAadPopTokenRequest();

            // seed the cache 
            Task<string> result = this.testObj.GetPopTokenAsync(req, CancellationToken.None);

            // Wait until cache is expired
            Task.WaitAll(result, Task.Delay(TimeSpan.FromSeconds(MaxCacheAgeAadAppTokenExpirySeconds + 1)));

            Assert.IsNotNull(result.Result);
            this.mockClient.Verify(o => o.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once);
            this.mockClient.Invocations.Clear();

            // test a cached value is refreshed when cache is expired (http client IS called)
            string result2 = await this.testObj.GetPopTokenAsync(req, CancellationToken.None).ConfigureAwait(false);
            Assert.IsNotNull(result2);
            this.mockClient.Verify(o => o.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task ShouldUseRegionalEstsEndpoint()
        {
            Uri estsEndpoint = null;

            this.mockAppConfig
                .Setup(o => o.IsFeatureFlagEnabledAsync(FeatureNames.PXS.EnableEstsrForPopToken, true))
                .ReturnsAsync(true);

            this.mockClient
                .Setup(o => o.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .Callback<HttpRequestMessage, CancellationToken>((httpRequestMessage, cancellationToken) =>
                {
                    estsEndpoint = httpRequestMessage.RequestUri;
                })
                .Returns((HttpRequestMessage msg, CancellationToken ct) => this.GenerateResponse(msg));

            AadPopTokenRequest req = CreateAadPopTokenRequest();

            var region = "westus2";
            Environment.SetEnvironmentVariable("MONITORING_DATACENTER", region);

            string result = await this.testObj.GetPopTokenAsync(req, CancellationToken.None).ConfigureAwait(false);
            Assert.IsNotNull(result);
            Assert.IsNotNull(estsEndpoint);
            Assert.AreEqual($"https://{region}.r.login.microsoftonline.com/login", estsEndpoint.ToString());
        }

        private async Task<HttpResponseMessage> GenerateResponse(HttpRequestMessage request)
        {
            NameValueCollection args = HttpUtility.ParseQueryString(await request.Content.ReadAsStringAsync().ConfigureAwait(false));
            JwtSecurityToken inputToken = new JwtSecurityToken(args["request"]);

            List<Claim> claims = new List<Claim>
            {
                new Claim("iss", AadIsser),
                new Claim("aud", args["resource"]),
                new Claim("tid", this.tenantId),
                new Claim("appid", inputToken.Issuer ?? "UNKNOWN"),
                new Claim(
                    "nbf",
                    inputToken.Payload?.Iat?.ToString() ?? DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds().ToString()),
                new Claim("exp", inputToken.Payload?.Exp?.ToString() ?? DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds().ToString())
            };

            JwtSecurityToken outputToken = new JwtSecurityToken(claims: claims);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"access_token\":\"" + outputToken.EncodedHeader + "." + outputToken.EncodedPayload + ".\"}")
            };
        }

        private static AadPopTokenRequest CreateAadPopTokenRequest()
        {
            const string ClaimVal = "c1v";
            const string ClaimKey = "c1";
            const string Resource = "rsrc";
            const string Scope = "scop";

            AadPopTokenRequest req = new AadPopTokenRequest
            {
                Resource = Resource,
                Scope = Scope,
                Claims = new Dictionary<string, string> { { ClaimKey, ClaimVal } },
                RequestUri = new Uri("https://example.com/path"),
                HttpMethod = HttpMethod.Post,
                Type = AadPopTokenRequestType.AppAssertedUserToken
            };
            return req;
        }
    }
}

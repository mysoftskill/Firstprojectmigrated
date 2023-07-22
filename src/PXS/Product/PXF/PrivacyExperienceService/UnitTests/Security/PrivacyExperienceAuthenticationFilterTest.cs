// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.UnitTests.Security
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using System.Web.Http;
    using System.Web.Http.Controllers;
    using System.Web.Http.Dependencies;
    using System.Web.Http.Filters;
    using System.Web.Http.Hosting;
    using System.Web.Http.Routing;

    using Microsoft.Azure.ComplianceServices.Common.UnitTests;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers;
    using ExperienceContracts = Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyAdapters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.CustomerMasterAdapter;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.ProfileIdentityService;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Security;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common;
    using Microsoft.Membership.MemberServices.Test.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Windows.Services.AuthN.Server;

    using Moq;

    using Error = Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.Error;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.Azure.ComplianceServices.Common;

    /// <summary>
    ///     PrivacyExperienceAuthenticationFilter Test
    /// </summary>
    [TestClass]
    public class PrivacyExperienceAuthenticationFilterTest
    {
        private const string TestSiteCallerName = "SiteCallerName";

        private readonly Mock<ICertificateValidator> mockCertValidator = new Mock<ICertificateValidator>(MockBehavior.Strict);

        private readonly Mock<ICustomerMasterAdapter> mockCustomerMasterAdapter = new Mock<ICustomerMasterAdapter>(MockBehavior.Strict);

        private readonly Mock<ILogger> mockLogger = TestMockFactory.CreateLogger();

        private readonly Mock<IMsaIdentityServiceAdapter> mockMsaIdentityServiceAdapter = new Mock<IMsaIdentityServiceAdapter>(MockBehavior.Strict);

        private readonly Mock<IRpsAuthServer> mockRpsServer = new Mock<IRpsAuthServer>(MockBehavior.Strict);

        private readonly Mock<IRpsTicket> mockTicket = new Mock<IRpsTicket>(MockBehavior.Strict);

        private readonly X509Certificate2 testClientAllowedListCert = UnitTestData.UnitTestCertificate;

        private readonly Mock<IAppConfiguration> mockAppConfiguration = new Mock<IAppConfiguration>(MockBehavior.Strict);

        private string accessToken, proxyTicket;

        private string countryRegion;

        private string expectedSiteCallerName;

        private Mock<IAadAuthManager> mockAadAuthManager;

        private Mock<IPrivacyConfigurationManager> mockConfiguration;

        private Mock<IDataManagementConfig> mockDataManagementConfig;

        private Mock<IPrivacyExperienceServiceConfiguration> mockServiceConfiguration;

        private long puid, cid, applicationId, applicationIdWithAllowAppList;

        [TestInitialize]
        public void ConfigureMocks()
        {
            this.puid = RequestFactory.GeneratePuid();
            this.cid = RequestFactory.GenerateCid();
            this.applicationId = RequestFactory.GeneratePuid();
            this.applicationIdWithAllowAppList = RequestFactory.GeneratePuid();
            this.accessToken = Guid.NewGuid().ToString();
            this.proxyTicket = Guid.NewGuid().ToString();
            this.countryRegion = "US";

            this.mockServiceConfiguration = TestPrivacyMockFactory.CreatePrivacyExperienceServiceConfiguration();
            string applicationIdString = this.applicationId.ToString(CultureInfo.InvariantCulture);
            string applicationIdWithAllowAppListString = this.applicationIdWithAllowAppList.ToString(CultureInfo.InvariantCulture);
            this.mockServiceConfiguration.SetupGet(c => c.SiteIdToCallerName).Returns(
                new Dictionary<string, string>
                {
                    { applicationIdString, TestSiteCallerName },
                    { applicationIdWithAllowAppListString, TestSiteCallerName }
                });
            this.mockServiceConfiguration.SetupGet(c => c.AppAllowList).Returns(
                new Dictionary<string, string>
                {
                    { applicationIdWithAllowAppListString, applicationIdString }
                });
            this.mockServiceConfiguration.SetupGet(c => c.VortexAllowedCertSubjects).Returns(
                new Dictionary<string, string>
                {
                    { this.testClientAllowedListCert.Subject, "parter.name.goes.here" }
                });
            this.mockServiceConfiguration.SetupGet(c => c.VortexAllowedCertIssuers).Returns(
                new List<string>
                {
                    this.testClientAllowedListCert.Issuer
                });
            this.expectedSiteCallerName = TestSiteCallerName + "_" + applicationIdString;

            this.mockDataManagementConfig = new Mock<IDataManagementConfig>(MockBehavior.Strict);

            this.mockConfiguration = new Mock<IPrivacyConfigurationManager>(MockBehavior.Strict);
            this.mockConfiguration.SetupGet(c => c.PrivacyExperienceServiceConfiguration).Returns(this.mockServiceConfiguration.Object);

            this.mockAadAuthManager = new Mock<IAadAuthManager>(MockBehavior.Strict);
            var mockAadS2SResult = new Mock<IAadS2SAuthResult>(MockBehavior.Strict);
            mockAadS2SResult.SetupGet(c => c.Succeeded).Returns(true);
            mockAadS2SResult.SetupGet(c => c.Exception).Returns((Exception)null);
            mockAadS2SResult.SetupGet(c => c.InboundAppId).Returns("i_am_the_app_id");
            mockAadS2SResult.SetupGet(c => c.AppDisplayName).Returns("i_am_the_app_display_name");
            mockAadS2SResult.SetupGet(c => c.SubjectTicket).Returns("i_am_the_ticket");
            mockAadS2SResult.SetupGet(c => c.ObjectId).Returns(Guid.NewGuid());
            mockAadS2SResult.SetupGet(c => c.TenantId).Returns(Guid.NewGuid());
            mockAadS2SResult.SetupGet(c => c.AccessToken).Returns("accesstoken");
            this.mockAadAuthManager
                .Setup(c => c.ValidateInboundPftAsync(It.IsAny<AuthenticationHeaderValue>(), It.IsAny<Guid>()))
                .ReturnsAsync(mockAadS2SResult.Object);
            this.mockAadAuthManager
                .Setup(c => c.ValidateInboundJwtAsync(It.IsAny<string>()))
                .ReturnsAsync(mockAadS2SResult.Object);

            // setup defaults
            this.mockTicket.Setup(m => m.GetTicketProperty(It.IsAny<string>())).Returns(null);
            this.mockTicket.Setup(m => m.GetProfileProperty(It.IsAny<string>())).Returns(null);

            this.mockCustomerMasterAdapter.Setup(m => m.GetOboPrivacyConsentSettingAsync(It.IsAny<IPxfRequestContext>())).ReturnsAsync(
                new AdapterResponse<bool?>
                {
                    Result = false
                });

            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.AuthenticationLogging, true)).ReturnsAsync(false);
        }

        [DataTestMethod]
        [DataRow(true, true)]
        [DataRow(true, null)] // If they're in a family but the consent bit isn't set, assume true
        [DataRow(false, false)]
        public async Task PrivacyExperienceAuthentication_OnBehalfOf_Success(bool expectedConsentValue, bool? customerMasterValue)
        {
            const string FamilyTicket = "family-ticket";

            const long TargetPuid = 0x1337;
            const long TargetCid = 0x7ac0dad5;

            this.SetupSiteAuth();
            this.SetupSelfAuth(this.puid, this.cid, this.countryRegion);

            var msaIdentityServiceAdapter = new Mock<IMsaIdentityServiceAdapter>(MockBehavior.Strict);
            msaIdentityServiceAdapter.Setup(msa => msa.GetSigninNameInformationAsync(TargetPuid)).ReturnsAsync(
                new AdapterResponse<ISigninNameInformation>
                {
                    Result = new SigninNameInformation
                    {
                        Cid = TargetCid
                    }
                });

            var userData = new Mock<IProfileAttributesUserData>(MockBehavior.Strict);
            userData.Setup(ud => ud.AgeGroup).Returns(LegalAgeGroup.MinorWithParentalConsent);
            userData.Setup(ud => ud.Birthdate).Returns(DateTime.UtcNow.AddYears(-13));
            userData.Setup(ud => ud.CountryCode).Returns("US");

            msaIdentityServiceAdapter.Setup(msa => msa.GetProfileAttributesAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<ProfileAttribute[]>())).ReturnsAsync(
                new AdapterResponse<IProfileAttributesUserData>
                {
                    Result = userData.Object
                });

            var customerMasterAdapter = new Mock<ICustomerMasterAdapter>(MockBehavior.Strict);
            customerMasterAdapter.Setup(c => c.GetOboPrivacyConsentSettingAsync(It.IsAny<IPxfRequestContext>())).ReturnsAsync(
                new AdapterResponse<bool?>
                {
                    Result = customerMasterValue
                });

            var familyClaims = new Mock<IFamilyClaims>(MockBehavior.Strict);
            familyClaims.Setup(fc => fc.CheckIsValid()).Returns(true);
            familyClaims.Setup(fc => fc.ParentChildRelationshipIsClaimed(It.IsAny<long>(), TargetPuid)).Returns(true);
            familyClaims.Setup(fc => fc.TargetPuid).Returns(() => TargetPuid);

            IFamilyClaims claims = familyClaims.Object;
            Assert.IsNotNull(claims);

            var familyClaimsParser = new Mock<IFamilyClaimsParser>(MockBehavior.Strict);
            familyClaimsParser.Setup(f => f.TryParse(FamilyTicket, out claims)).Returns(true);

            PrivacyExperienceAuthenticationFilter filter = this.CreateRequestMessageWithAuthFilter(
                HttpMethod.Get,
                out HttpRequestMessage request,
                out HttpAuthenticationContext context,
                msaIdentityServiceAdapter.Object,
                familyClaimsParser: familyClaimsParser.Object,
                customerMasterAdapter: customerMasterAdapter.Object);
            request.Headers.Add(ExperienceContracts.HeaderNames.FamilyTicket, FamilyTicket); // Add family ticket to request

            await filter.AuthenticateAsync(context, new CancellationToken(false)).ConfigureAwait(false);

            this.ValidateRequest(context, this.expectedSiteCallerName, typeof(MsaSelfIdentity), expectCid: true);
            this.ValidateS2SSiteAuth(Times.Once);
            this.ValidateAuthResult(Times.Once);

            var callerPrincipal = context.Principal as CallerPrincipal;
            var identity = callerPrincipal?.Identity as MsaSelfIdentity;

            Assert.IsNotNull(identity);
            Assert.AreEqual(TargetPuid, identity.TargetPuid);
            Assert.AreEqual(TargetCid, identity.TargetCid);
            Assert.AreEqual(AuthType.OnBehalfOf, identity.AuthType);
            Assert.IsTrue(identity.IsChildInFamily);
            Assert.AreEqual(expectedConsentValue, identity.IsFamilyConsentSet);

            msaIdentityServiceAdapter.Verify(msa => msa.GetSigninNameInformationAsync(TargetPuid), Times.Once);
        }

        [TestMethod]
        public async Task PrivacyExperienceAuthenticationFilter_AadAuth_Error()
        {
            var expectedDiagnosticsLog = new List<string> { "Extra log info #1", "Extra log info #2" };

            this.mockAadAuthManager = new Mock<IAadAuthManager>(MockBehavior.Strict);
            var mockAadS2SResult = new Mock<IAadS2SAuthResult>(MockBehavior.Strict);
            mockAadS2SResult.SetupGet(c => c.Succeeded).Returns(false);
            mockAadS2SResult.SetupGet(c => c.Exception).Returns(new Exception("security error!"));
            mockAadS2SResult.SetupGet(c => c.DiagnosticLogs).Returns(expectedDiagnosticsLog);
            mockAadS2SResult.SetupGet(c => c.InboundAppId).Returns("correctappid");
            this.mockAadAuthManager
                .Setup(c => c.ValidateInboundPftAsync(It.IsAny<AuthenticationHeaderValue>(), It.IsAny<Guid>()))
                .ReturnsAsync(mockAadS2SResult.Object);

            const string RelativePath = @"users('20328eb1-9839-4edd-a95e-04a0e7e6d783')/exportPersonalData";
            PrivacyExperienceAuthenticationFilter filter = this.CreateAadRequestMessageWithAuthFilter(
                RelativePath,
                HttpMethod.Post,
                out HttpRequestMessage _,
                out HttpAuthenticationContext context,
                this.mockAadAuthManager.Object);

            await filter.AuthenticateAsync(context, CancellationToken.None).ConfigureAwait(false);
            Assert.IsNotNull(context.ErrorResult);

            var errorResult = context.ErrorResult as ErrorHttpActionResult;
            Assert.IsNotNull(errorResult);
            Assert.AreEqual(
                "Request is not authorized. Api RoutePath: /users('20328eb1-9839-4edd-a95e-04a0e7e6d783')/exportPersonalData",
                errorResult.Error.Message);
            Assert.AreEqual(string.Join(", ", expectedDiagnosticsLog), errorResult.Error.InnerError.Message);
        }

        [DataTestMethod]
        public async Task PrivacyExperienceAuthenticationFilter_AadAuth_FullyQualifiedOdataNamespace_Success()
        {
            try
            {
                const string RelativePath =
                    @"users('20328eb1-9839-4edd-a95e-04a0e7e6d783')/Microsoft.PrivacyServices.DataSubjectRight.Contracts.V1.exportPersonalData";
                PrivacyExperienceAuthenticationFilter filter = this.CreateAadRequestMessageWithAuthFilter(
                    RelativePath,
                    HttpMethod.Post,
                    out HttpRequestMessage _,
                    out HttpAuthenticationContext context,
                    this.mockAadAuthManager.Object);

                await filter.AuthenticateAsync(context, CancellationToken.None).ConfigureAwait(false);
                Assert.IsNull(context.ErrorResult);
                Assert.IsNotNull(context.Principal);
                var aadIdentity = context.Principal.Identity as AadIdentity;
                Assert.IsNotNull(aadIdentity);
                Assert.AreEqual(aadIdentity.CallerNameFormatted, "AAD_i_am_the_app_display_name");
            }
            finally
            {
                HttpContext.Current = null;
            }
        }

        [DataTestMethod]
        public async Task PrivacyExperienceAuthenticationFilter_AadAuth_Success()
        {
            var aadRoutes = new Dictionary<string, HttpMethod>
            {
                { @"users('1c7cf8c6-314c-4b69-a183-7b6be4931e99')/exportPersonalData", HttpMethod.Post },
                { @"directory/inboundSharedUserProfiles('1c7cf8c6-314c-4b69-a183-7b6be4931e99')/removePersonalData", HttpMethod.Post },
                { @"directory/inboundSharedUserProfiles('1c7cf8c6-314c-4b69-a183-7b6be4931e99')/exportPersonalData", HttpMethod.Post },
                { @"directory/outboundSharedUserProfiles('1c7cf8c6-314c-4b69-a183-7b6be4931e99')/tenants('20328eb1-9839-4edd-a95e-04a0e7e6d783')/removePersonalData", HttpMethod.Post },
                { @"dataPolicyOperations", HttpMethod.Get },
                { @"dataPolicyOperations('1c7cf8c6-314c-4b69-a183-7b6be4931e99')", HttpMethod.Get }
            };

            this.mockServiceConfiguration.SetupGet(c => c.SiteIdToCallerName).Returns(
                new Dictionary<string, string>
                {
                    { "i_am_the_app_id", "i_am_the_caller_name" }
                });

            foreach (KeyValuePair<string, HttpMethod> aadRoute in aadRoutes)
            {
                PrivacyExperienceAuthenticationFilter filter = this.CreateAadRequestMessageWithAuthFilter(
                    aadRoute.Key,
                    aadRoute.Value,
                    out HttpRequestMessage _,
                    out HttpAuthenticationContext context,
                    this.mockAadAuthManager.Object);

                await filter.AuthenticateAsync(context, CancellationToken.None).ConfigureAwait(false);

                string errorMessage = $"Route was not authenticated: {aadRoute}";

                Assert.IsNull(context.ErrorResult, errorMessage);
                Assert.IsNotNull(context.Principal);

                var aadIdentity = context.Principal.Identity as AadIdentity;
                Assert.IsNotNull(aadIdentity);
                Assert.AreEqual(aadIdentity.CallerNameFormatted, "AAD_i_am_the_caller_name");
            }
        }

        [TestMethod]
        public async Task PrivacyExperienceAuthenticationFilter_PcdAadAuthWithMsaUserProxyTicket_InvalidProxyTicket()
        {
            this.SetupSelfAuth(this.puid, this.cid, this.countryRegion, true);

            const string RelativePath = @"v1/privacyrequest/export";
            PrivacyExperienceAuthenticationFilter filter = this.CreateAadRequestMessageWithAuthFilter(
                RelativePath,
                HttpMethod.Post,
                out HttpRequestMessage _,
                out HttpAuthenticationContext context,
                this.mockAadAuthManager.Object,
                true,
                false);

            await filter.AuthenticateAsync(context, CancellationToken.None).ConfigureAwait(false);
            Assert.IsNotNull(context.ErrorResult);
            var errorHttpActionResult = context.ErrorResult as ErrorHttpActionResult;
            Assert.IsNotNull(errorHttpActionResult);
            Assert.AreEqual(errorHttpActionResult.Error.Code, ExperienceContracts.ErrorCode.InvalidClientCredentials.ToString());
        }

        [TestMethod]
        public async Task PrivacyExperienceAuthenticationFilter_PcdAadAuthWithMsaUserProxyTicket_Success()
        {
            this.SetupSelfAuth(this.puid, this.cid, this.countryRegion);
            this.mockServiceConfiguration.SetupGet(c => c.SiteIdToCallerName).Returns(
                new Dictionary<string, string>
                {
                    { "i_am_the_app_id", "i_am_the_caller_name" }
                });

            const string RelativePath = @"v1/privacyrequest/export";
            PrivacyExperienceAuthenticationFilter filter = this.CreateAadRequestMessageWithAuthFilter(
                RelativePath,
                HttpMethod.Post,
                out HttpRequestMessage _,
                out HttpAuthenticationContext context,
                this.mockAadAuthManager.Object,
                true,
                false);

            await filter.AuthenticateAsync(context, CancellationToken.None).ConfigureAwait(false);
            Assert.IsNull(context.ErrorResult);
            Assert.IsNotNull(context.Principal);
            Assert.IsTrue(context.Principal.Identity is AadIdentityWithMsaUserProxyTicket);
            var identity = context.Principal.Identity as AadIdentityWithMsaUserProxyTicket;
            Assert.AreEqual(this.puid, identity.TargetPuid);
            Assert.AreEqual(this.cid, identity.TargetCid);
            Assert.AreEqual("AAD_i_am_the_caller_name", identity.CallerNameFormatted);
            Assert.IsNotNull(identity.UserProxyTicket);
            this.mockServiceConfiguration.Verify(c => c.S2SUserLongSiteName, Times.AtLeastOnce());
            this.mockServiceConfiguration.Verify(c => c.S2SUserSiteName, Times.Never);
        }

        [TestMethod]
        public async Task PrivacyExperienceAuthenticationFilter_PcdAadAuthWithoutMsaUserProxyTicket_Success()
        {
            const string RelativePath = @"v1/privacyrequest/export";
            PrivacyExperienceAuthenticationFilter filter = this.CreateAadRequestMessageWithAuthFilter(
                RelativePath,
                HttpMethod.Post,
                out HttpRequestMessage _,
                out HttpAuthenticationContext context,
                this.mockAadAuthManager.Object,
                false,
                false);

            await filter.AuthenticateAsync(context, CancellationToken.None).ConfigureAwait(false);
            Assert.IsNull(context.ErrorResult);
            Assert.IsNotNull(context.Principal);
            Assert.IsTrue(context.Principal.Identity.GetType() == typeof(AadIdentity));
            var identity = context.Principal.Identity as AadIdentity;
            Assert.AreEqual("AAD_i_am_the_app_display_name", identity?.CallerNameFormatted);
        }

        [TestMethod]
        public async Task PrivacyExperienceAuthenticationFilter_SelfAuthTicket_FromAdditionalSite_Success()
        {
            this.SetupSiteAuth(this.applicationIdWithAllowAppList);
            this.SetupSelfAuth(this.puid, this.cid, this.countryRegion, true);

            PrivacyExperienceAuthenticationFilter filter = this.CreateRequestMessageWithAuthFilter(HttpMethod.Get, out HttpRequestMessage _, out HttpAuthenticationContext context);

            await filter.AuthenticateAsync(context, new CancellationToken(false)).ConfigureAwait(false);

            this.ValidateRequest(context, TestSiteCallerName + "_" + this.applicationIdWithAllowAppList, typeof(MsaSelfIdentity), expectCid: true);
            this.ValidateS2SSiteAuth(Times.Once);
            this.ValidateAuthResult(() => Times.Exactly(2));
        }

        [TestMethod]
        public async Task PrivacyExperienceAuthenticationFilter_SelfAuthTicket_MissingCid_Denied()
        {
            var expectedError = new Error(ExperienceContracts.ErrorCode.Unknown, "Error occurred when validating proxy ticket. Message: The Cid was null");
            this.SetupSiteAuth();
            this.SetupSelfAuth(this.puid, null, this.countryRegion);

            PrivacyExperienceAuthenticationFilter filter = this.CreateRequestMessageWithAuthFilter(HttpMethod.Get, out HttpRequestMessage _, out HttpAuthenticationContext context);

            await filter.AuthenticateAsync(context, new CancellationToken(false)).ConfigureAwait(false);

            this.ValidateError(context, expectedError);
            this.ValidateS2SSiteAuth(Times.Once);
            this.ValidateAuthResult(Times.Once);
        }

        [TestMethod]
        public async Task PrivacyExperienceAuthenticationFilter_SelfAuthTicket_MissingPuid_Denied()
        {
            var expectedError = new Error(ExperienceContracts.ErrorCode.Unknown, "Error occurred when validating proxy ticket. Message: The MemberId was null");
            this.SetupSiteAuth();
            this.SetupSelfAuth(null, this.cid, this.countryRegion);

            PrivacyExperienceAuthenticationFilter filter = this.CreateRequestMessageWithAuthFilter(HttpMethod.Get, out HttpRequestMessage _, out HttpAuthenticationContext context);

            await filter.AuthenticateAsync(context, new CancellationToken(false)).ConfigureAwait(false);

            this.ValidateError(context, expectedError);
            this.ValidateS2SSiteAuth(Times.Once);
            this.ValidateAuthResult(Times.Once);
        }

        /// <summary>
        ///     Validates only certain routes (ie PCD Export) can authenticate via MSA Site id with an optional MSA self
        /// </summary>
        [TestMethod]
        public async Task PrivacyExperienceAuthenticationFilter_SiteAuth_SelfOptionalIncluded_Success()
        {
            this.SetupSiteAuth();
            this.SetupSelfAuth(this.puid, this.cid, this.countryRegion);

            var msaIdentityServiceAdapter = new Mock<IMsaIdentityServiceAdapter>(MockBehavior.Strict);

            HttpRequestMessage request;
            HttpAuthenticationContext context;
            PrivacyExperienceAuthenticationFilter filter = this.CreateRequestMessageWithAuthFilter(HttpMethod.Get, out request, out context, msaIdentityServiceAdapter.Object);

            var route = new HttpRouteData(new HttpRoute(ExperienceContracts.RouteNames.PrivacyRequestApiExport));
            request.SetRouteData(route);

            await filter.AuthenticateAsync(context, new CancellationToken(false)).ConfigureAwait(false);

            this.ValidateRequest(context, this.expectedSiteCallerName, typeof(MsaSelfIdentity), expectCid: true);
            this.ValidateS2SSiteAuth(Times.Once);
            this.ValidateAuthResult(Times.Once);

            // should never call family for self auth
            msaIdentityServiceAdapter.Verify(m => m.GetSigninNameInformationAsync(It.IsAny<long>()), Times.Never);
        }

        /// <summary>
        ///     Validates only certain routes (ie PCD Export) can authenticate via MSA Site id with an optional MSA self
        /// </summary>
        [TestMethod]
        public async Task PrivacyExperienceAuthenticationFilter_SiteAuth_SelfOptionalNotIncluded_Success()
        {
            this.SetupSiteAuth();

            var msaIdentityServiceAdapter = new Mock<IMsaIdentityServiceAdapter>(MockBehavior.Strict);

            HttpRequestMessage request = this.CreateRequestMessage(
                httpMethod: HttpMethod.Get,
                includeCert: true,
                requestUriString: "https://pxs.com/",
                routeValue: ExperienceContracts.RouteNames.PrivacyRequestApiExport,
                addAccessToken: true,
                addProxyTicket: false);

            var route = new HttpRouteData(new HttpRoute(ExperienceContracts.RouteNames.PrivacyRequestApiExport));
            request.SetRouteData(route);

            HttpAuthenticationContext context;
            PrivacyExperienceAuthenticationFilter filter = this.CreateRequestMessageWithAuthFilter(request, out context, msaIdentityServiceAdapter.Object);

            await filter.AuthenticateAsync(context, new CancellationToken(false)).ConfigureAwait(false);

            this.ValidateRequestSiteAuth(context, this.expectedSiteCallerName);
            this.ValidateS2SSiteAuth(Times.Once);

            // Self auth is not called since this route is authenticated just by msa site.
            this.ValidateAuthResult(Times.Never);

            // should never call family for self auth
            msaIdentityServiceAdapter.Verify(m => m.GetSigninNameInformationAsync(It.IsAny<long>()), Times.Never);
        }

        /// <summary>
        ///     Validates only certain routes can authenticate via MSA Site id.
        /// </summary>
        [TestMethod]
        public async Task PrivacyExperienceAuthenticationFilter_SiteAuth_Success()
        {
            this.SetupSiteAuth(this.applicationId);
            this.mockServiceConfiguration
                .SetupGet(c => c.SiteIdToCallerName)
                .Returns(new Dictionary<string, string> { { this.applicationId.ToString(), TestSiteCallerName } });

            var msaIdentityServiceAdapter = new Mock<IMsaIdentityServiceAdapter>(MockBehavior.Strict);

            HttpRequestMessage request = this.CreateRequestMessage(
                httpMethod: HttpMethod.Get,
                includeCert: true,
                requestUriString: "https://pxs.com/",
                routeValue: ExperienceContracts.RouteNames.PrivacyRequestApiList,
                addAccessToken: true,
                addProxyTicket: false);


            var route = new HttpRouteData(new HttpRoute(ExperienceContracts.RouteNames.PrivacyRequestApiList));
            request.SetRouteData(route);

            HttpAuthenticationContext context;
            PrivacyExperienceAuthenticationFilter filter = this.CreateRequestMessageWithAuthFilter(request, out context, msaIdentityServiceAdapter.Object);

            await filter.AuthenticateAsync(context, new CancellationToken(false)).ConfigureAwait(false);

            this.ValidateRequestSiteAuth(context, this.expectedSiteCallerName);
            this.ValidateS2SSiteAuth(Times.Once);

            // Self auth is not called since this route is authenticated just by msa site.
            this.ValidateAuthResult(Times.Never);

            // should never call family for self auth
            msaIdentityServiceAdapter.Verify(m => m.GetSigninNameInformationAsync(It.IsAny<long>()), Times.Never);
        }

        [TestMethod]
        public async Task PrivacyExperienceAuthenticationFilterMissingAccessToken()
        {
            var expectedError = new Error(
                ExperienceContracts.ErrorCode.MissingClientCredentials,
                "An access token is required to access the requested resource.");

            PrivacyExperienceAuthenticationFilter filter = this.CreateRequestMessageWithAuthFilter(
                HttpMethod.Get,
                out HttpRequestMessage request,
                out HttpAuthenticationContext context);
            request.Headers.Remove(ExperienceContracts.HeaderNames.AccessToken);

            await filter.AuthenticateAsync(context, new CancellationToken(false)).ConfigureAwait(false);

            this.ValidateError(context, expectedError);
            this.ValidateS2SSiteAuth(Times.Never);
            this.ValidateAuthResult(Times.Never);
        }

        [TestMethod]
        public async Task PrivacyExperienceAuthenticationFilterMissingClientCert()
        {
            var expectedError = new Error(
                ExperienceContracts.ErrorCode.Unauthorized,
                "Authorization is null and client certificate is missing. API RoutePath /nonAllowedListroute");

            PrivacyExperienceAuthenticationFilter filter = this.CreateRequestMessageWithAuthFilter(
                HttpMethod.Get,
                out HttpRequestMessage _,
                out HttpAuthenticationContext context,
                includeClientCert: false);

            await filter.AuthenticateAsync(context, new CancellationToken(false)).ConfigureAwait(false);

            this.ValidateError(context, expectedError);
            this.ValidateS2SSiteAuth(Times.Never);
            this.ValidateAuthResult(Times.Never);
        }

        [TestMethod]
        public async Task PrivacyExperienceAuthenticationFilterNotAllowedSiteIdError()
        {
            const string AllowedSiteId = "12345678";
            const long InvalidSiteId = 1;

            this.applicationId = InvalidSiteId;
            this.mockServiceConfiguration
                .SetupGet(c => c.SiteIdToCallerName)
                .Returns(new Dictionary<string, string> { { AllowedSiteId, TestSiteCallerName } });

            string errorMessage = string.Format(CultureInfo.InvariantCulture, "Invalid caller name. The site id: {0} is not AllowedList.", this.applicationId);
            var expectedError = new Error(ExperienceContracts.ErrorCode.InvalidClientCredentials, errorMessage);

            this.SetupSiteAuth();
            this.SetupSelfAuth(this.puid, this.cid, this.countryRegion);

            PrivacyExperienceAuthenticationFilter filter = this.CreateRequestMessageWithAuthFilter(HttpMethod.Get, out HttpRequestMessage _, out HttpAuthenticationContext context);

            await filter.AuthenticateAsync(context, new CancellationToken(false)).ConfigureAwait(false);

            this.ValidateError(context, expectedError);
            this.ValidateS2SSiteAuth(Times.Once);

            // Self auth is not called since it failed before getting there.
            this.ValidateAuthResult(Times.Never);
        }

        [TestMethod]
        public async Task PrivacyExperienceAuthenticationFilterSelfAuthMissingProxyTicket()
        {
            var expectedError = new Error(ExperienceContracts.ErrorCode.MissingClientCredentials, "A user proxy ticket is required to access the requested resource.");
            this.SetupSiteAuth();

            PrivacyExperienceAuthenticationFilter filter = this.CreateRequestMessageWithAuthFilter(
                HttpMethod.Get,
                out HttpRequestMessage request,
                out HttpAuthenticationContext context);
            request.Headers.Remove(ExperienceContracts.HeaderNames.ProxyTicket);

            await filter.AuthenticateAsync(context, new CancellationToken(false)).ConfigureAwait(false);

            this.ValidateError(context, expectedError);
            this.ValidateS2SSiteAuth(Times.Once);
            this.ValidateAuthResult(Times.Never);
        }

        [TestMethod]
        public async Task PrivacyExperienceAuthenticationFilterSelfAuthReturnsNull()
        {
            var expectedError = new Error(ExperienceContracts.ErrorCode.Unknown, "Unknown error occurred when validating proxy ticket. Message: RpsAuthResult was null");
            this.SetupSiteAuth();
            this.mockRpsServer
                .Setup(m => m.GetAuthResult(It.IsAny<string>(), It.IsAny<string>(), RpsTicketType.Proxy, It.IsAny<RpsPropertyBag>()))
                .Returns((RpsAuthResult)null);

            PrivacyExperienceAuthenticationFilter filter = this.CreateRequestMessageWithAuthFilter(HttpMethod.Get, out HttpRequestMessage _, out HttpAuthenticationContext context);

            await filter.AuthenticateAsync(context, new CancellationToken(false)).ConfigureAwait(false);

            this.ValidateError(context, expectedError);
            this.ValidateS2SSiteAuth(Times.Once);
            this.ValidateAuthResult(Times.Once);
        }

        [TestMethod]
        public async Task PrivacyExperienceAuthenticationFilterSelfAuthThrowsAuthException()
        {
            var expectedError = new Error(
                ExperienceContracts.ErrorCode.InvalidClientCredentials,
                string.Format(CultureInfo.InvariantCulture, "Request contained an invalid or unauthorized proxy ticket. ErrorCode: {0}", AuthNErrorCode.AuthenticationFailed));
            this.SetupSiteAuth();
            this.mockRpsServer
                .Setup(m => m.GetAuthResult(It.IsAny<string>(), It.IsAny<string>(), RpsTicketType.Proxy, It.IsAny<RpsPropertyBag>()))
                .Throws(new AuthNException(AuthNErrorCode.AuthenticationFailed));

            PrivacyExperienceAuthenticationFilter filter = this.CreateRequestMessageWithAuthFilter(HttpMethod.Get, out HttpRequestMessage _, out HttpAuthenticationContext context);

            await filter.AuthenticateAsync(context, new CancellationToken(false)).ConfigureAwait(false);

            this.ValidateError(context, expectedError);
            this.ValidateS2SSiteAuth(Times.Once);
            this.ValidateAuthResult(Times.Once);
        }

        [TestMethod]
        public async Task PrivacyExperienceAuthenticationFilterSelfAuthThrowsUnknownException()
        {
            var unknownException = new Exception(Guid.NewGuid().ToString());
            var expectedError = new Error(
                ExperienceContracts.ErrorCode.Unknown,
                string.Format(CultureInfo.InvariantCulture, "Unknown error occurred when validating proxy ticket. Message: {0}", unknownException.Message));
            expectedError.ErrorDetails = unknownException.ToString();
            this.SetupSiteAuth();
            this.mockRpsServer
                .Setup(m => m.GetAuthResult(It.IsAny<string>(), It.IsAny<string>(), RpsTicketType.Proxy, It.IsAny<RpsPropertyBag>()))
                .Throws(unknownException);

            PrivacyExperienceAuthenticationFilter filter = this.CreateRequestMessageWithAuthFilter(HttpMethod.Get, out HttpRequestMessage _, out HttpAuthenticationContext context);

            await filter.AuthenticateAsync(context, new CancellationToken(false)).ConfigureAwait(false);

            this.ValidateError(context, expectedError);
            this.ValidateS2SSiteAuth(Times.Once);
            this.ValidateAuthResult(Times.Once);
        }

        [TestMethod]
        public async Task PrivacyExperienceAuthenticationFilterSelfAuthTicketFromAdditionalSite()
        {
            this.SetupSiteAuth(this.applicationIdWithAllowAppList);
            this.SetupSelfAuth(this.puid, this.cid, this.countryRegion, true);

            PrivacyExperienceAuthenticationFilter filter = this.CreateRequestMessageWithAuthFilter(HttpMethod.Get, out HttpRequestMessage _, out HttpAuthenticationContext context);

            await filter.AuthenticateAsync(context, new CancellationToken(false)).ConfigureAwait(false);

            this.ValidateRequest(context, TestSiteCallerName + "_" + this.applicationIdWithAllowAppList, typeof(MsaSelfIdentity), expectCid: true);
            this.ValidateS2SSiteAuth(Times.Once);
            this.ValidateAuthResult(() => Times.Exactly(2));
        }

        [TestMethod]
        public async Task PrivacyExperienceAuthenticationFilterSelfAuthTicketMissingCid()
        {
            var expectedError = new Error(ExperienceContracts.ErrorCode.Unknown, "Error occurred when validating proxy ticket. Message: The Cid was null");
            this.SetupSiteAuth();
            this.SetupSelfAuth(this.puid, null, this.countryRegion);

            PrivacyExperienceAuthenticationFilter filter = this.CreateRequestMessageWithAuthFilter(HttpMethod.Get, out HttpRequestMessage _, out HttpAuthenticationContext context);

            await filter.AuthenticateAsync(context, new CancellationToken(false)).ConfigureAwait(false);

            this.ValidateError(context, expectedError);
            this.ValidateS2SSiteAuth(Times.Once);
            this.ValidateAuthResult(Times.Once);
        }

        [TestMethod]
        public async Task PrivacyExperienceAuthenticationFilterSelfAuthTicketMissingPuid()
        {
            var expectedError = new Error(ExperienceContracts.ErrorCode.Unknown, "Error occurred when validating proxy ticket. Message: The MemberId was null");
            this.SetupSiteAuth();
            this.SetupSelfAuth(null, this.cid, this.countryRegion);

            PrivacyExperienceAuthenticationFilter filter = this.CreateRequestMessageWithAuthFilter(HttpMethod.Get, out HttpRequestMessage _, out HttpAuthenticationContext context);

            await filter.AuthenticateAsync(context, new CancellationToken(false)).ConfigureAwait(false);

            this.ValidateError(context, expectedError);
            this.ValidateS2SSiteAuth(Times.Once);
            this.ValidateAuthResult(Times.Once);
        }

        [TestMethod]
        public async Task PrivacyExperienceAuthenticationFilterSiteAuthReturnsNull()
        {
            var expectedError = new Error(
                ExperienceContracts.ErrorCode.Unknown,
                "Unknown error occurred when validating access token. Message: RpsAuthResult was null");

            this.mockRpsServer
                .Setup(m => m.GetS2SSiteAuthResult(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<RpsPropertyBag>()))
                .Returns((RpsAuthResult)null);

            PrivacyExperienceAuthenticationFilter filter = this.CreateRequestMessageWithAuthFilter(HttpMethod.Get, out HttpRequestMessage _, out HttpAuthenticationContext context);

            await filter.AuthenticateAsync(context, new CancellationToken(false)).ConfigureAwait(false);

            this.ValidateError(context, expectedError);
            this.ValidateS2SSiteAuth(Times.Once);
            this.ValidateAuthResult(Times.Never);
        }

        [TestMethod]
        public async Task PrivacyExperienceAuthenticationFilterSiteAuthThrowsAuthException()
        {
            var expectedError = new Error(
                ExperienceContracts.ErrorCode.InvalidClientCredentials,
                $"Request contained an invalid or unauthorized access token for sitename: s2sapp.pxs.api.account.microsoft.com. ErrorCode: {AuthNErrorCode.AuthenticationFailed}, Message: ");
            expectedError.ErrorDetails = "Microsoft.Windows.Services.AuthN.Server.AuthNException";

            this.mockRpsServer
                .Setup(m => m.GetS2SSiteAuthResult(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<RpsPropertyBag>()))
                .Throws(new AuthNException(AuthNErrorCode.AuthenticationFailed));

            PrivacyExperienceAuthenticationFilter filter = this.CreateRequestMessageWithAuthFilter(HttpMethod.Get, out HttpRequestMessage _, out HttpAuthenticationContext context);

            await filter.AuthenticateAsync(context, new CancellationToken(false)).ConfigureAwait(false);

            this.ValidateError(context, expectedError);
            this.ValidateS2SSiteAuth(Times.Once);
            this.ValidateAuthResult(Times.Never);
        }

        [TestMethod]
        public async Task PrivacyExperienceAuthenticationFilterSiteAuthThrowsUnknownException()
        {
            Guid errorGuid = Guid.NewGuid();
            var unknownException = new Exception(errorGuid.ToString());
            var expectedError = new Error(
                ExperienceContracts.ErrorCode.Unknown,
                string.Format(CultureInfo.InvariantCulture, "Unknown error occurred when validating access token. Message: {0}", unknownException.Message));
            expectedError.ErrorDetails = $"System.Exception: {errorGuid}";

            this.mockRpsServer
                .Setup(m => m.GetS2SSiteAuthResult(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<RpsPropertyBag>()))
                .Throws(unknownException);

            PrivacyExperienceAuthenticationFilter filter = this.CreateRequestMessageWithAuthFilter(HttpMethod.Get, out HttpRequestMessage _, out HttpAuthenticationContext context);

            await filter.AuthenticateAsync(context, new CancellationToken(false)).ConfigureAwait(false);

            this.ValidateError(context, expectedError);
            this.ValidateS2SSiteAuth(Times.Once);
            this.ValidateAuthResult(Times.Never);
        }

        [TestMethod]
        public async Task PrivacyExperienceAuthenticationFilterVortexAuthFailsOnUnknownCert()
        {
            var failedCertValidatorMock = new Mock<ICertificateValidator>(MockBehavior.Strict);
            failedCertValidatorMock.Setup(cv => cv.IsAuthorized(It.IsAny<X509Certificate2>())).Returns(false);

            PrivacyExperienceAuthenticationFilter filter = this.CreateVortexRequestMessageWithAuthFilter(
                out HttpRequestMessage _,
                out HttpAuthenticationContext context,
                failedCertValidatorMock.Object);

            await filter.AuthenticateAsync(context, new CancellationToken()).ConfigureAwait(false);
            Assert.IsNotNull(context.ErrorResult);

            failedCertValidatorMock.Verify(cv => cv.IsAuthorized(It.IsAny<X509Certificate2>()));
        }

        [TestMethod]
        public async Task PrivacyExperienceAuthenticationFilterVortexAuthSucceeds()
        {
            PrivacyExperienceAuthenticationFilter filter = this.CreateVortexRequestMessageWithAuthFilter(
                out HttpRequestMessage _,
                out HttpAuthenticationContext context,
                new CertificateSNIValidator(this.mockLogger.Object, this.mockServiceConfiguration.Object.VortexAllowedCertSubjects.Keys, this.mockServiceConfiguration.Object.VortexAllowedCertIssuers));

            await filter.AuthenticateAsync(context, new CancellationToken()).ConfigureAwait(false);
            Assert.IsNull(context.ErrorResult);
        }

        [TestMethod]
        public async Task PrivacyExperienceAuthenticationFilterVortexAuthSucceedsWithVerification()
        {
            var certValidatorMock = new Mock<ICertificateValidator>(MockBehavior.Strict);
            certValidatorMock.Setup(cv => cv.IsAuthorized(It.IsAny<X509Certificate2>())).Returns(true);

            PrivacyExperienceAuthenticationFilter filter = this.CreateVortexRequestMessageWithAuthFilter(
                out HttpRequestMessage _,
                out HttpAuthenticationContext context,
                certValidatorMock.Object);

            await filter.AuthenticateAsync(context, new CancellationToken()).ConfigureAwait(false);
            Assert.IsNull(context.ErrorResult);

            certValidatorMock.Verify(cv => cv.IsAuthorized(It.IsAny<X509Certificate2>()));
        }

        private PrivacyExperienceAuthenticationFilter CreateAadRequestMessageWithAuthFilter(
            string routeTemplate,
            HttpMethod httpMethod,
            out HttpRequestMessage request,
            out HttpAuthenticationContext context,
            IAadAuthManager aadAuthManager = null,
            bool addProxyTicket = false,
            bool usePft = true)
        {
            PrivacyExperienceAuthenticationFilter filter = this.CreateRequestMessageWithAuthFilter(
                httpMethod,
                out request,
                out context,
                false,
                certificateValidator: null,
                aadAuthManager: aadAuthManager ?? this.mockAadAuthManager.Object,
                routeValue: routeTemplate,
                addProxyTicket: addProxyTicket);

            // Overwrite template
            var route = new HttpRouteData(new HttpRoute(routeTemplate));
            request.SetRouteData(route);
            request.Headers.Authorization =
                usePft
                    ? new AuthenticationHeaderValue("MSAuth1.0", "actortoken=\"anactortoken\", accesstoken=\"anaccesstoken\", type=\"PFAT\"")
                    : new AuthenticationHeaderValue("Bearer", "tokenValueHere");

            return filter;
        }

        private HttpAuthenticationContext CreateAuthenticationContext(HttpRequestMessage request)
        {
            var controllerContext = new HttpControllerContext { Request = request };
            var actionContext = new HttpActionContext { ControllerContext = controllerContext };
            var authenticationContext = new HttpAuthenticationContext(actionContext, null);
            return authenticationContext;
        }

        private HttpRequestMessage CreateRequestMessage(
            HttpMethod httpMethod,
            bool includeCert = true,
            string requestUriString = "https://www.microsoft.com",
            string routeValue = "nonAllowedListroute",
            bool addAccessToken = true,
            bool addProxyTicket = true)
        {
            var routes = new HttpRouteCollection();
            var httpRoute = new HttpRoute(routeValue);
            routes.Add("routeName", httpRoute);

            var configuration = new HttpConfiguration(routes);

            var requestContext = new HttpRequestContext
            {
                ClientCertificate = includeCert ? this.testClientAllowedListCert
                    : null,
                Configuration = configuration
            };

            var mockResolver = new Mock<IDependencyResolver>(MockBehavior.Strict);
            mockResolver.Setup(m => m.BeginScope()).Returns(mockResolver.Object);
            mockResolver.Setup(m => m.GetService(typeof(IPrivacyConfigurationManager))).Returns(this.mockConfiguration.Object);
            mockResolver.Setup(m => m.GetService(typeof(IDataManagementConfig))).Returns(this.mockDataManagementConfig.Object);

            requestContext.Configuration.DependencyResolver = mockResolver.Object;
            var baseUri = new Uri(requestUriString);
            var relativePath = new Uri(routeValue, UriKind.Relative);
            var request = new HttpRequestMessage(httpMethod, new Uri(baseUri, relativePath));
            request.Properties[HttpPropertyKeys.RequestContextKey] = requestContext;
            if (addAccessToken)
            {
                request.Headers.Add(ExperienceContracts.HeaderNames.AccessToken, this.accessToken);
            }

            if (addProxyTicket)
            {
                request.Headers.Add(ExperienceContracts.HeaderNames.ProxyTicket, this.proxyTicket);
            }

            var route = new HttpRouteData(httpRoute);
            request.SetRouteData(route);
            request.SetConfiguration(configuration);

            return request;
        }

        private PrivacyExperienceAuthenticationFilter CreateRequestMessageWithAuthFilter(
            HttpMethod httpMethod,
            out HttpRequestMessage request,
            out HttpAuthenticationContext context,
            bool includeClientCert = true,
            ICertificateValidator certificateValidator = null,
            IAadAuthManager aadAuthManager = null,
            string routeValue = "nonAllowedListroute",
            bool addAccessToken = true,
            bool addProxyTicket = true,
            IFamilyClaimsParser familyClaimsParser = null,
            ICustomerMasterAdapter customerMasterAdapter = null)
        {
            return this.CreateRequestMessageWithAuthFilter(
                httpMethod,
                out request,
                out context,
                this.mockMsaIdentityServiceAdapter.Object,
                includeClientCert,
                certificateValidator,
                aadAuthManager,
                routeValue,
                addAccessToken: addAccessToken,
                addProxyTicket: addProxyTicket,
                familyClaimsParser: familyClaimsParser);
        }

        private PrivacyExperienceAuthenticationFilter CreateRequestMessageWithAuthFilter(
            HttpMethod httpMethod,
            out HttpRequestMessage request,
            out HttpAuthenticationContext context,
            IMsaIdentityServiceAdapter msaServiceAdapter,
            bool includeClientCert = true,
            ICertificateValidator certificateValidator = null,
            IAadAuthManager aadAuthManager = null,
            string routeValue = "nonAllowedListroute",
            bool addAccessToken = true,
            bool addProxyTicket = true,
            IFamilyClaimsParser familyClaimsParser = null,
            ICustomerMasterAdapter customerMasterAdapter = null)
        {
            request = this.CreateRequestMessage(
                httpMethod,
                includeClientCert,
                routeValue: routeValue,
                addAccessToken: addAccessToken,
                addProxyTicket: addProxyTicket);
            return this.CreateRequestMessageWithAuthFilter(
                request,
                out context,
                msaServiceAdapter,
                certificateValidator,
                aadAuthManager,
                familyClaimsParser,
                customerMasterAdapter);
        }

        private PrivacyExperienceAuthenticationFilter CreateRequestMessageWithAuthFilter(
            HttpRequestMessage request,
            out HttpAuthenticationContext context,
            IMsaIdentityServiceAdapter msaServiceAdapter,
            ICertificateValidator certificateValidator = null,
            IAadAuthManager aadAuthManager = null,
            IFamilyClaimsParser familyClaimsParser = null,
            ICustomerMasterAdapter customerMasterAdapter = null)
        {
            var filter =
                new PrivacyExperienceAuthenticationFilter(
                    this.mockRpsServer.Object,
                    this.mockLogger.Object,
                    msaServiceAdapter ?? this.mockMsaIdentityServiceAdapter.Object,
                    certificateValidator ?? this.mockCertValidator.Object,
                    aadAuthManager ?? this.mockAadAuthManager.Object,
                    familyClaimsParser ?? new FamilyClaimsParser(),
                    customerMasterAdapter ?? this.mockCustomerMasterAdapter.Object,
                    this.mockAppConfiguration.Object);
            context = this.CreateAuthenticationContext(request);
            return filter;
        }

        private PrivacyExperienceAuthenticationFilter CreateVortexRequestMessageWithAuthFilter(
            out HttpRequestMessage request,
            out HttpAuthenticationContext context,
            ICertificateValidator certValidator)
        {
            string targetRoute = ExperienceContracts.RouteNames.VortexIngestionDeviceDeleteV1;
            PrivacyExperienceAuthenticationFilter filter = this.CreateRequestMessageWithAuthFilter(
                HttpMethod.Post,
                out request,
                out context,
                true,
                certValidator,
                routeValue: targetRoute,
                addAccessToken: false,
                addProxyTicket: false);

            // Overwrite template to hit vortex endpoint
            var route = new HttpRouteData(new HttpRoute(targetRoute));
            request.SetRouteData(route);

            return filter;
        }

        private void SetupSelfAuth(long? puidValue, long? cidValue, string countryRegionValue, bool throwOnce = false)
        {
            if (puidValue.HasValue)
            {
                this.mockTicket
                    .Setup(m => m.GetTicketProperty(RpsTicketField.MemberIdLow.ToString()))
                    .Returns((uint)(puidValue.Value & uint.MaxValue));
                this.mockTicket
                    .Setup(m => m.GetTicketProperty(RpsTicketField.MemberIdHigh.ToString()))
                    .Returns((uint)(puidValue.Value >> 32));

                // Hex PUID in RPS ticket is upper case letters
                this.mockTicket
                    .Setup(m => m.GetTicketProperty("HexPUID"))
                    .Returns(puidValue.Value.ToString("X")); // The enum name is the not the same as the property key
            }

            if (cidValue.HasValue)
            {
                this.mockTicket
                    .Setup(m => m.GetTicketProperty(RpsTicketField.CidLow.ToString()))
                    .Returns((uint)(cidValue.Value & uint.MaxValue));
                this.mockTicket
                    .Setup(m => m.GetTicketProperty(RpsTicketField.CidHigh.ToString()))
                    .Returns((uint)(cidValue.Value >> 32));

                // Hex CID in RPS ticket is lower case letters
                this.mockTicket
                    .Setup(m => m.GetTicketProperty("HexCId"))
                    .Returns(cidValue.Value.ToString("x")); // The enum name is the not the same as the property key
            }

            this.mockTicket
                .Setup(m => m.GetProfileProperty("Country"))
                .Returns(countryRegionValue);

            this.mockTicket
                .Setup(m => m.GetTicketProperty(RpsTicketField.IsProxyTicket.ToString()))
                .Returns(true);

            this.mockTicket
                .Setup(m => m.GetTicketProperty(RpsTicketField.ProxyTicket.ToString()))
                .Returns(this.proxyTicket);

            this.mockTicket
                .Setup(m => m.Dispose());

            var result = new RpsAuthResult(this.mockTicket.Object);

            if (throwOnce)
            {
                bool firstCall = true;
                this.mockRpsServer
                    .Setup(m => m.GetAuthResult(It.IsAny<string>(), It.IsAny<string>(), RpsTicketType.Proxy, It.IsAny<RpsPropertyBag>()))
                    .Returns(
                        () =>
                        {
                            if (!firstCall)
                            {
                                return result;
                            }

                            firstCall = false;
                            throw new AuthNException(AuthNErrorCode.InvalidTicket, "Mock invalid ticket exception");
                        });
            }
            else
            {
                this.mockRpsServer
                    .Setup(m => m.GetAuthResult(It.IsAny<string>(), It.IsAny<string>(), RpsTicketType.Proxy, It.IsAny<RpsPropertyBag>()))
                    .Returns(result);
            }
        }

        private void SetupSiteAuth()
        {
            this.SetupSiteAuth(this.applicationId);
        }

        private void SetupSiteAuth(long applicationId)
        {
            this.mockTicket.Setup(m => m.GetTicketProperty(RpsTicketField.AppIdLow.ToString())).Returns((uint)(applicationId & uint.MaxValue));
            this.mockTicket.Setup(m => m.GetTicketProperty(RpsTicketField.AppIdHigh.ToString())).Returns((uint)(applicationId >> 32));
            this.mockTicket.Setup(m => m.Dispose());
            var result = new RpsAuthResult(this.mockTicket.Object);
            this.mockRpsServer
                .Setup(m => m.GetS2SSiteAuthResult(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<RpsPropertyBag>()))
                .Returns(result);
        }

        private void ValidateAuthResult(Func<Times> times)
        {
            this.mockRpsServer
                .Verify(
                    m =>
                        m.GetAuthResult(
                            this.mockServiceConfiguration.Object.S2SUserSiteName,
                            this.proxyTicket,
                            RpsTicketType.Proxy,
                            It.IsAny<RpsPropertyBag>()),
                    times);
        }

        private void ValidateError(HttpAuthenticationContext context, Error expectedError)
        {
            Assert.IsNull(context.Principal);
            var errorResult = context.ErrorResult as ErrorHttpActionResult;
            Assert.IsNotNull(errorResult);
            EqualityHelper.AreEqual(expectedError, errorResult.Error);
        }

        private void ValidateRequest(
            HttpAuthenticationContext context,
            string expectedCallerName = null,
            Type identityType = null,
            bool expectCid = false)
        {
            // added to debug unit test failures
            if (context.ErrorResult != null)
            {
                if (context.ErrorResult is ErrorHttpActionResult errorResult)
                {
                    Console.WriteLine(errorResult.Error.ToString());
                }
            }

            Assert.IsNull(context.ErrorResult, nameof(context.ErrorResult) + " should be null");
            Assert.IsNotNull(context.Principal);
            var callerPrincipal = context.Principal as CallerPrincipal;
            Assert.IsNotNull(callerPrincipal);
            Assert.IsNotNull(callerPrincipal.Identity);
            Assert.AreEqual(identityType, callerPrincipal.Identity.GetType());
            var identity = callerPrincipal.Identity as MsaSelfIdentity;
            Assert.IsNotNull(identity);

            Assert.AreEqual(this.puid, identity.AuthorizingPuid);

            if (expectCid)
            {
                Assert.IsTrue(identity.AuthorizingCid.HasValue);
                Assert.AreEqual(this.cid, identity.AuthorizingCid);
            }
            else
            {
                Assert.IsFalse(identity.AuthorizingCid.HasValue);
            }

            Assert.AreEqual(expectedCallerName, identity.CallerNameFormatted);
        }

        private void ValidateRequestSiteAuth(
            HttpAuthenticationContext context,
            string expectedCallerName = null)
        {
            // added to debug unit test failures
            if (context.ErrorResult != null)
            {
                if (context.ErrorResult is ErrorHttpActionResult errorResult)
                {
                    Console.WriteLine(errorResult.Error.ToString());
                }
            }

            Assert.IsNull(context.ErrorResult, nameof(context.ErrorResult) + " should be null");
            Assert.IsNotNull(context.Principal);
            var callerPrincipal = context.Principal as CallerPrincipal;
            Assert.IsNotNull(callerPrincipal);
            Assert.IsNotNull(callerPrincipal.Identity);
            var identity = callerPrincipal.Identity as MsaSiteIdentity;
            Assert.IsNotNull(identity);
            Assert.AreEqual(expectedCallerName, identity.CallerNameFormatted);
            Assert.AreEqual(AuthType.MsaSite.ToString(), identity.AuthType.ToString());
        }

        private void ValidateS2SSiteAuth(Func<Times> times)
        {
            this.mockRpsServer
                .Verify(
                    m =>
                        m.GetS2SSiteAuthResult(
                            this.mockServiceConfiguration.Object.S2SAppSiteName,
                            this.accessToken,
                            this.testClientAllowedListCert.RawData,
                            It.IsAny<RpsPropertyBag>()),
                    times);
        }

        private static string CalculateSignature(X509Certificate2 cert)
        {
            using (SHA256 hasher = SHA256.Create())
            {
                byte[] hash = hasher.ComputeHash(cert.RawData);
                return Convert.ToBase64String(hash);
            }
        }
    }
}

// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.FunctionalTests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging.LoggingFilter;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.CustomerMasterAdapter;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Factory;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Utilities;
    using Microsoft.Membership.MemberServices.Test.Common;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Osgs = Microsoft.OSGS.HttpClientCommon;
    using TestConfiguration = Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Config.TestConfiguration;

    [TestClass]
    public class PrivacyAdapterFcts : TestBase
    {
        protected const string PartnerS2STargetSite = "pxs.api.account.microsoft-int.com";
        protected const string AgentFriendlyName = "MockPartnerFriendlyName";
        protected const string PartnerId = "MockPartner";

        private Mock<ILoggingFilter> mockLoggingFilter = new Mock<ILoggingFilter>(MockBehavior.Strict);

        private IPxfAdapterFactory pxfAdapterFactory;

        [TestInitialize]
        public void Initialize()
        {
            Sll.ResetContext();
            Sll.Context.Vector = new CorrelationVector();

            this.mockLoggingFilter.Setup(o => o.ShouldLogDetailsForUser(It.IsAny<string>())).Returns(false);

            this.pxfAdapterFactory = new PxfAdapterFactory(new HttpClientFactoryPublic(this.mockLoggingFilter.Object, new ConsoleLogger()));
        }

        [TestMethod]
        [TestCategory("Private")]
        [Ignore]
        public async Task GetKeepAlive()
        {
            var httpClient = new Osgs.HttpClient();
            var result = await httpClient.GetAsync(TestConfiguration.MockBaseUrl.Value + "/keepalive", CancellationToken.None);
            Assert.IsTrue(result.IsSuccessStatusCode);
        }

        // TODO: fix CreateCustomerMasterAdapter() and re-enable test
        [TestMethod]
        [TestCategory("Private")]
        [Ignore]
        public async Task GetPrivacyProfile()
        {
            CustomerMasterAdapter adapter = this.CreateCustomerMasterAdapter();
            AdapterResponse<PrivacyProfile> profile = await adapter.GetPrivacyProfileAsync(CreatePxfRequestContext(TestUsers.ViewLocation0).Object);

            Assert.IsNotNull(profile);
            Assert.IsTrue(profile.IsSuccess, $"Error Code: {profile.Error.Code}, Message: {profile.Error.Message}");
        }

        [TestMethod]
        [TestCategory("Private")]
        [Ignore]
        public async Task GetMsaVerifier_User_StrongAuth_Success()
        {
            // In INT, the test account must be unprotected for this to work (bypasses TFA requirement)
            // MBI_SSL_SA is strong auth
            var requestContextMock = CreatePxfRequestContext(TestUsers.Verifier001, authPolicy: "MBI_SSL_SA");
            MsaIdentityServiceAdapter adapter = this.CreateMsaIdentityServiceAdapter();
            var result = await adapter.GetGdprUserDeleteVerifierAsync(new List<Guid> { Guid.NewGuid() }, requestContextMock.Object, null, null, null).ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(result.Result));
        }

        [TestMethod]
        [TestCategory("Private")]
        [Ignore]
        public async Task GetCidFromPuid()
        {
            MsaIdentityServiceAdapter adapter = this.CreateMsaIdentityServiceAdapter();
            const long puid = 985160683659105;
            AdapterResponse<ISigninNameInformation> result = await adapter.GetSigninNameInformationAsync(puid).ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.Result);
            Assert.AreEqual(puid, result.Result.Puid);
        }

        [TestMethod]
        [TestCategory("Private")]
        [Ignore]
        public async Task GetMsaVerifier_Device_StrongAuth_Success()
        {
            MsaIdentityServiceAdapter adapter = this.CreateMsaIdentityServiceAdapter();
            var result = await adapter.GetGdprDeviceDeleteVerifierAsync(Guid.NewGuid(), GlobalDeviceIdGenerator.Generate(), null).ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(result.Result));
        }

        [TestMethod]
        [TestCategory("Private")]
        [Ignore]
        public async Task GetMsaVerifier_User_AccessDenied()
        {
            // In INT, the test account must be unprotected for this to work (bypasses TFA requirement)
            // MBI_SSL is not strong auth and therefore gets an error from MSA
            var requestContextMock = CreatePxfRequestContext(TestUsers.Verifier001, authPolicy: "MBI_SSL");
            MsaIdentityServiceAdapter adapter = this.CreateMsaIdentityServiceAdapter();
            var result = await adapter.GetGdprUserDeleteVerifierAsync(new List<Guid> { Guid.NewGuid() }, requestContextMock.Object, null, null, null).ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(AdapterErrorCode.MsaCallerNotAuthorized, result.Error.Code);
        }

        [TestMethod]
        [TestCategory("Private")]
        [Ignore]
        public async Task GetMsaVerifier_Device()
        {
            MsaIdentityServiceAdapter adapter = this.CreateMsaIdentityServiceAdapter();
            
            // The global device id has to follow msa's logic on what's valid
            var deviceId = GlobalDeviceIdGenerator.Generate();
            Console.WriteLine(deviceId);
            var result = await adapter.GetGdprDeviceDeleteVerifierAsync(Guid.NewGuid(), deviceId, null).ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(result.Result));
        }

        [TestMethod]
        [TestCategory("Private")]
        [Ignore]
        public async Task GetLocationHistoryFromMock()
        {
            var partnerAdapter = CreateAdapterForMockService();
            var requestContextMock = CreatePxfRequestContext(TestUsers.ViewLocation0);

            // Make the GET call
            var pagedResponse = await partnerAdapter.Adapter.GetLocationHistoryAsync(
                requestContextMock.Object,
                OrderByType.DateTime,
                dateOption: DateOption.Between,
                startDate: DateTime.UtcNow.Subtract(TimeSpan.FromDays(15)).Date,
                endDate: DateTime.UtcNow.Subtract(TimeSpan.FromDays(1)).Date);
        }

        [TestMethod]
        [TestCategory("Private")]
        [Ignore]
        public async Task GetLocationHistoryForChildFromMock()
        {
            var parentUser = TestUsers.Parent1;
            var childUser = TestUsers.Child1;
            var partnerAdapter = CreateAdapterForMockService();
            var requestContextMock = CreatePxfRequestContext(parentUser);
            var familyModel = await FamilyModel.GetFamilyAsync(parentUser.Puid, requestContextMock.Object.UserProxyTicket);
            var childElement = familyModel.Members.Where(fmm => fmm.Id == childUser.Puid.ToString(CultureInfo.InvariantCulture)).First();
            requestContextMock.SetupGet(r => r.FamilyJsonWebToken).Returns(childElement.JsonWebToken);
            requestContextMock.SetupGet(r => r.TargetPuid).Returns(childUser.Puid);

            // Make the GET call
            var pagedResponse = await partnerAdapter.Adapter.GetLocationHistoryAsync(
                requestContextMock.Object,
                OrderByType.DateTime,
                dateOption: DateOption.Between,
                startDate: DateTime.UtcNow.Subtract(TimeSpan.FromDays(15)).Date,
                endDate: DateTime.UtcNow.Subtract(TimeSpan.FromDays(1)).Date);
        }

        [TestMethod]
        [TestCategory("Private")]
        [Ignore]
        public async Task GetLocationHistoryWithPaging()
        {
            var partnerAdapter = CreateAdapterForMockService();
            var requestContextMock = CreatePxfRequestContext(TestUsers.ViewLocation0);

            // Clear all values from the user using the test hook
            var response = await MockTestHooks.PostTestHookAsync(TestConfiguration.MockBaseUrl.Value, TestUsers.ViewLocation0.Puid, "locationhistory", "delete");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            // Make the initial GET call
            var allLocations = new List<LocationResource>();
            var pagedResponse = await partnerAdapter.Adapter.GetLocationHistoryAsync(
                requestContextMock.Object,
                OrderByType.DateTime,
                dateOption: DateOption.Between,
                startDate: DateTime.UtcNow.Subtract(TimeSpan.FromDays(15)).Date,
                endDate: DateTime.UtcNow.Subtract(TimeSpan.FromDays(1)).Date);
            allLocations = pagedResponse.Items.ToList();

            Console.WriteLine("User has {0} locations in history", allLocations.Count);
        }

        [TestMethod]
        [TestCategory("Private")]
        [Ignore]
        public async Task DeleteLocationHistoryFromMock()
        {
            var partnerAdapter = CreateAdapterForMockService();
            var requestContextMock = CreatePxfRequestContext(TestUsers.DeleteLocation0);

            // Make the delete call
            var deleteResponse = await partnerAdapter.Adapter.DeleteLocationHistoryAsync(
                requestContextMock.Object);
        }

        [TestMethod]
        [TestCategory("Private")]
        [Ignore]
        public async Task GetBrowseHistoryFromMock()
        {
            var partnerAdapter = CreateAdapterForMockService();
            var requestContextMock = CreatePxfRequestContext(TestUsers.ViewBrowse0);

            // Make the GET call
            var pagedResponse = await partnerAdapter.Adapter.GetBrowseHistoryAsync(
                requestContextMock.Object,
                OrderByType.DateTime,
                dateOption: DateOption.SingleDay,
                startDate: new DateTime(2016, 2, 4));
        }

        [TestMethod]
        [TestCategory("Private")]
        [Ignore]
        public async Task GetSearchHistoryFromMock()
        {
            var partnerAdapter = CreateAdapterForMockService();
            var requestContextMock = CreatePxfRequestContext(TestUsers.ViewSearch0);

            // Make the GET call
            var pagedResponse = await partnerAdapter.Adapter.GetSearchHistoryAsync(
                requestContextMock.Object,
                OrderByType.DateTime,
                dateOption: DateOption.SingleDay,
                startDate: new DateTime(2016, 2, 4));
        }

        [TestMethod]
        public async Task GetProxyTicketTest()
        {
            var ticket = await MockTestHooks.GetProxyTicketAsync(TestConfiguration.MockBaseUrl.Value, TestUsers.PrivacyTest12.UserName, TestUsers.PrivacyTest12.Password);
            Assert.IsFalse(string.IsNullOrWhiteSpace(ticket));
        }

        private static Mock<IPxfRequestContext> CreatePxfRequestContext(
            TestUser testUser,
            string familyJsonWebToken = null,
            string authPolicy = "MBI_SSL")
        {
            var proxyTicket = GetUserProxyTicketAndAssertSuccess(testUser.UserName, testUser.Password, authPolicy);
            var requestContextMock = new Mock<IPxfRequestContext>(MockBehavior.Strict);
            requestContextMock.SetupGet(r => r.UserProxyTicket).Returns(proxyTicket);
            requestContextMock.SetupGet(r => r.CV).Returns(new CorrelationVector());
            requestContextMock.SetupGet(r => r.FamilyJsonWebToken).Returns(familyJsonWebToken);
            requestContextMock.SetupGet(r => r.AuthorizingPuid).Returns(testUser.Puid);
            requestContextMock.SetupGet(r => r.TargetPuid).Returns(testUser.Puid);
            return requestContextMock;
        }

        /// <summary>
        /// Retrieves a user proxy ticket for a user and asserts that it was successfully retrieved.
        /// </summary>
        /// <param name="userName">The user name</param>
        /// <param name="password">The password</param>
        /// <param name="authPolicy">The auth policy</param>
        /// <returns>The user proxy ticket</returns>
        private static string GetUserProxyTicketAndAssertSuccess(string userName, string password, string authPolicy)
        {
            UserProxyTicketProvider userProxyTicketProvider = new UserProxyTicketProvider(TestData.IntUserTicketConfiguration(authPolicy));
            UserProxyTicketResult userProxyTicketResult = userProxyTicketProvider.GetTicket(userName, password).Result;
            Assert.IsNull(
                userProxyTicketResult.ErrorMessage,
                "UserProxyTicketProvider.GetTicket() returned error, but expected a ticket: ErrorMessage = {0}",
                userProxyTicketResult.ErrorMessage);
            return userProxyTicketResult.Ticket;
        }

        private PartnerAdapter CreateAdapterForMockService()
        {
            var partnerConfig = new Mock<IPxfPartnerConfiguration>(MockBehavior.Strict);
            partnerConfig.SetupGet(c => c.BaseUrl).Returns(TestConfiguration.MockBaseUrl.Value.ToString);
            partnerConfig.SetupGet(c => c.MsaS2STargetSite).Returns(PartnerS2STargetSite);
            partnerConfig.SetupGet(c => c.AgentFriendlyName).Returns(AgentFriendlyName);
            partnerConfig.SetupGet(c => c.PartnerId).Returns(PartnerId);
            partnerConfig.SetupGet(c => c.Id).Returns(Guid.NewGuid().ToString);
            partnerConfig.SetupGet(c => c.PxfAdapterVersion).Returns(AdapterVersion.PxfV1);
            partnerConfig.SetupGet(c => c.RealTimeDelete).Returns(true);
            partnerConfig.SetupGet(c => c.RealTimeView).Returns(true);
            partnerConfig.SetupGet(c => c.SkipServerCertValidation).Returns(true);
            partnerConfig.SetupGet(c => c.RetryStrategyConfiguration).Returns(TestMockFactory.CreateFixedIntervalRetryStrategyConfiguration().Object);
            partnerConfig.SetupGet(c => c.TimeoutInMilliseconds).Returns(10000);
            partnerConfig.SetupGet(c => c.CounterCategoryName).Returns("MockCounterCategoryName");
            partnerConfig.SetupGet(c => c.CustomHeaders).Returns((Dictionary<string,string>) null);

            var logger = new Mock<ILogger>(MockBehavior.Loose).Object;
            var certProvider = new CertificateProvider(logger);

            var clientCertificateConfig = new Mock<ICertificateConfiguration>(MockBehavior.Strict);
            var certInfo = TestConfiguration.S2SCert.Value;
            clientCertificateConfig.SetupGet(c => c.Subject).Returns(certInfo.Subject);
            clientCertificateConfig.SetupGet(c => c.Issuer).Returns(certInfo.Issuer);
            clientCertificateConfig.SetupGet(c => c.Thumbprint).Returns(certInfo.Thumbprint);
            clientCertificateConfig.SetupGet(c => c.CheckValidity).Returns(true);

            var msaIdentityServiceConfig = new Mock<IMsaIdentityServiceConfiguration>(MockBehavior.Strict);
            msaIdentityServiceConfig.SetupGet(c => c.Endpoint).Returns("https://login.live-int.com/pksecure/oauth20_clientcredentials.srf");
            msaIdentityServiceConfig.SetupGet(c => c.ClientId).Returns(TestData.TestSiteIdIntProd.ToString());
            msaIdentityServiceConfig.SetupGet(c => c.Policy).Returns("S2S_24HOURS_MUTUALSSL");
            msaIdentityServiceConfig.SetupGet(c => c.CertificateConfiguration).Returns(clientCertificateConfig.Object);

            Mock<IAadTokenProvider> aadTokenProvider = new Mock<IAadTokenProvider>(MockBehavior.Strict);
            aadTokenProvider
                .Setup(o => o.GetPopTokenAsync(It.IsAny<AadPopTokenRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("this_is_an_AAD_PoP_token");

            var partnerAdapter = this.pxfAdapterFactory.Create(
                certProvider, 
                msaIdentityServiceConfig.Object, 
                partnerConfig.Object, 
                aadTokenProvider.Object,
                logger, 
                this.mockCounterFactory.Object);
            return partnerAdapter;
        }

        private CustomerMasterAdapter CreateCustomerMasterAdapter()
        {
            var adapterFactory = new Mock<ICustomerMasterAdapterFactory>(MockBehavior.Strict).Object;
            var logger = new Mock<ILogger>(MockBehavior.Loose).Object;
            var certProvider = new CertificateProvider(logger);

            var clientCertificateConfig = new Mock<ICertificateConfiguration>(MockBehavior.Strict);
            var certInfo = TestConfiguration.S2SCert.Value;
            clientCertificateConfig.SetupGet(c => c.Subject).Returns(certInfo.Subject);
            clientCertificateConfig.SetupGet(c => c.Issuer).Returns(certInfo.Issuer);
            clientCertificateConfig.SetupGet(c => c.Thumbprint).Returns(certInfo.Thumbprint);
            clientCertificateConfig.SetupGet(c => c.CheckValidity).Returns(false);

            var msaIdentityServiceConfig = new Mock<IMsaIdentityServiceConfiguration>(MockBehavior.Strict);
            msaIdentityServiceConfig.SetupGet(c => c.Endpoint).Returns("https://login.live-int.com/pksecure/oauth20_clientcredentials.srf");
            msaIdentityServiceConfig.SetupGet(c => c.ClientId).Returns(TestData.TestSiteIdIntProd.ToString());
            msaIdentityServiceConfig.SetupGet(c => c.Policy).Returns("S2S_24HOURS_MUTUALSSL");
            msaIdentityServiceConfig.SetupGet(c => c.CertificateConfiguration).Returns(clientCertificateConfig.Object);

            var partnerConfig = new Mock<IPrivacyPartnerAdapterConfiguration>(MockBehavior.Strict);
            partnerConfig.SetupGet(c => c.BaseUrl).Returns("https://jcmsdf.account.microsoft-int.com");
            partnerConfig.SetupGet(c => c.CounterCategoryName).Returns("CustomerMaster");
            partnerConfig.SetupGet(c => c.MsaS2STargetSite).Returns("unistorefd-int.www.microsoft.com");
            partnerConfig.SetupGet(c => c.RetryStrategyConfiguration).Returns(TestMockFactory.CreateFixedIntervalRetryStrategyConfiguration().Object);
            partnerConfig.SetupGet(c => c.TimeoutInMilliseconds).Returns(10000);
            partnerConfig.SetupGet(c => c.SkipServerCertValidation).Returns(true);

            var configurationManager = new Mock<IPrivacyConfigurationManager>(MockBehavior.Strict);
            configurationManager.SetupGet(c => c.MsaIdentityServiceConfiguration).Returns(msaIdentityServiceConfig.Object);
            configurationManager.SetupGet(c => c.AdaptersConfiguration.CustomerMasterAdapterConfiguration).Returns(partnerConfig.Object);

            // TODO: this will always throw a MockException(). adapterFactory is a Mock<> created with 'Strict' option, but the Create()
            // TODO:  method has no definition provided. This should anyway just be doing 'new CustomerMasterAdapter()' with a bunch of 
            // TODO:  Mock<>ed objects because the definition of Create() would have to do that anyway so no point in going through a
            // TODO:  Mock<>.

            return adapterFactory.Create(certProvider, configurationManager.Object, logger, this.mockCounterFactory.Object);
        }

        private MsaIdentityServiceAdapter CreateMsaIdentityServiceAdapter()
        {
            var logger = new Mock<ILogger>(MockBehavior.Loose).Object;
            var certProvider = new CertificateProvider(logger);

            var clientCertificateConfig = new Mock<ICertificateConfiguration>(MockBehavior.Strict);
            var certInfo = TestConfiguration.S2SCert.Value;
            clientCertificateConfig.SetupGet(c => c.Subject).Returns(certInfo.Subject);
            clientCertificateConfig.SetupGet(c => c.Issuer).Returns(certInfo.Issuer);
            clientCertificateConfig.SetupGet(c => c.Thumbprint).Returns(certInfo.Thumbprint);
            clientCertificateConfig.SetupGet(c => c.CheckValidity).Returns(false);

            var msaIdentityServiceConfig = new Mock<IMsaIdentityServiceConfiguration>(MockBehavior.Strict);
            msaIdentityServiceConfig.SetupGet(c => c.Endpoint).Returns("https://login.live-int.com/pksecure/oauth20_clientcredentials.srf");
            msaIdentityServiceConfig.SetupGet(c => c.ClientId).Returns(TestData.TestSiteIdIntProd.ToString());
            msaIdentityServiceConfig.SetupGet(c => c.Policy).Returns("S2S_24HOURS_MUTUALSSL");
            msaIdentityServiceConfig.SetupGet(c => c.CertificateConfiguration).Returns(clientCertificateConfig.Object);

            var mockServicePointConfiguration = new Mock<IServicePointConfiguration>(MockBehavior.Strict);
            mockServicePointConfiguration.Setup(c => c.ConnectionLimit).Returns(42);
            mockServicePointConfiguration.Setup(c => c.ConnectionLeaseTimeout).Returns(39);
            mockServicePointConfiguration.Setup(c => c.MaxIdleTime).Returns(98);

            var partnerConfig = new Mock<IMsaIdentityServiceAdapterConfiguration>(MockBehavior.Strict);
            partnerConfig.SetupGet(c => c.BaseUrl).Returns("https://api.login.live-int.com");
            partnerConfig.SetupGet(c => c.CounterCategoryName).Returns("MSA");
            partnerConfig.SetupGet(c => c.PartnerId).Returns("MSA");
            partnerConfig.SetupGet(c => c.RetryStrategyConfiguration).Returns(TestMockFactory.CreateFixedIntervalRetryStrategyConfiguration().Object);
            partnerConfig.SetupGet(c => c.TimeoutInMilliseconds).Returns(10000);
            partnerConfig.SetupGet(c => c.ServicePointConfiguration).Returns(mockServicePointConfiguration.Object);
            partnerConfig.SetupGet(c => c.IgnoreErrors).Returns(false);
            partnerConfig.SetupGet(c => c.EnableAdapter).Returns(true);

            var configurationManager = new Mock<IPrivacyConfigurationManager>(MockBehavior.Strict);
            configurationManager.SetupGet(c => c.MsaIdentityServiceConfiguration).Returns(msaIdentityServiceConfig.Object);
            configurationManager.SetupGet(c => c.AdaptersConfiguration.MsaIdentityServiceAdapterConfiguration).Returns(partnerConfig.Object);

            var counterFactory = new Mock<ICounterFactory>(MockBehavior.Strict);
            counterFactory.Setup(c => c.GetCounter(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CounterType>())).Returns(new Mock<ICounter>(MockBehavior.Loose).Object);

            var msaIdentityServiceClientFactory = new MsaIdentityServiceClientFactory();
            return new MsaIdentityServiceAdapter(configurationManager.Object, certProvider, logger, counterFactory.Object, msaIdentityServiceClientFactory);
        }
    }
}

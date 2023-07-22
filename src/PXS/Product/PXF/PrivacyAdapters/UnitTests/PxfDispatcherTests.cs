// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    using Microsoft.PrivacyServices.Common.Azure;
    

    [TestClass]
    public class PxfDispatcherTests : TestBase
    {
        private Mock<IDataManagementConfig> pxfAdaptersConfiguration = new Mock<IDataManagementConfig>(MockBehavior.Strict);
        private Mock<IPrivacyExperienceServiceConfiguration> serviceConfig = new Mock<IPrivacyExperienceServiceConfiguration>(MockBehavior.Strict);
        private Mock<IAdaptersConfiguration> adaptersConfiguration = new Mock<IAdaptersConfiguration>(MockBehavior.Strict);

        public PxfDispatcherTests()
        {
            this.serviceConfig.SetupGet(c => c.AdapterConfigurationSource).Returns(AdapterConfigurationSource.ConfigurationIniFile);
            var mockPartnerSignature = new Mock<ICertificateConfiguration>(MockBehavior.Strict);
            this.adaptersConfiguration.SetupGet(c => c.PrivacyFlightConfigurations).Returns(new List<IFlightConfiguration>());
            this.adaptersConfiguration.SetupGet(c => c.DefaultTargetRing).Returns(RingType.Prod);
            this.adaptersConfiguration.SetupGet(c => c.RequiredResourceTypes).Returns((IList<ResourceType>)null);
        }

        [TestMethod]
        public void InitializeTestProdRing()
        {
            List<IPxfPartnerConfiguration> partnerConfigs = CreateDefaultPxfPartnerConfigurations();
            this.CreatePxfAdaptersConfiguration(partnerConfigs, new List<RingType> { RingType.Prod });

            var dispatcher = new PxfDispatcher(
                this.CreatePrivacyConfigurationManager().Object,
                this.pxfAdaptersConfiguration.Object,
                MockPxfAdapterFactory().Object, 
                CreateCertProviderMock().Object, 
                CreateAadTokenProviderMock().Object,
                new ConsoleLogger(), 
                this.mockCounterFactory.Object);

            // Verify number of rings
            Assert.AreEqual(1, dispatcher.RingPartnerAdapterMapping.Count);

            // Verify mapping tables
            Assert.AreEqual(7, dispatcher.RingPartnerAdapterMapping[RingType.Prod].Count);
            Assert.AreEqual(2, dispatcher.RingResourcePartnerMapping[RingType.Prod].Count);
            Assert.AreEqual(2, dispatcher.RingResourcePartnerMapping[RingType.Prod][ResourceType.Browse].Count);
            Assert.AreEqual(5, dispatcher.RingResourcePartnerMapping[RingType.Prod][ResourceType.Location].Count);
            Assert.AreEqual(true, dispatcher.RingPartnerAdapterMapping[RingType.Prod]["P1"].RealTimeDelete);
            Assert.AreEqual(false, dispatcher.RingPartnerAdapterMapping[RingType.Prod]["P2"].RealTimeDelete);
            Assert.AreEqual(false, dispatcher.RingPartnerAdapterMapping[RingType.Prod]["P3"].RealTimeDelete);
        }

        [TestMethod]
        public void InitializeTestManyRings()
        {
            List<IPxfPartnerConfiguration> partnerConfigs = CreateDefaultPxfPartnerConfigurations();
            this.CreatePxfAdaptersConfiguration(partnerConfigs, new List<RingType> { RingType.Prod, RingType.PreProd, RingType.Ring1 });

            var dispatcher = new PxfDispatcher(
                this.CreatePrivacyConfigurationManager().Object,
                this.pxfAdaptersConfiguration.Object,
                MockPxfAdapterFactory().Object,
                CreateCertProviderMock().Object,
                CreateAadTokenProviderMock().Object,
                new ConsoleLogger(),
                this.mockCounterFactory.Object);

            // Verify number of rings
            Assert.AreEqual(3, dispatcher.RingPartnerAdapterMapping.Count);

            // Verify mapping tables
            Assert.AreEqual(7, dispatcher.RingPartnerAdapterMapping[RingType.Prod].Count);
            Assert.AreEqual(2, dispatcher.RingResourcePartnerMapping[RingType.Prod].Count);
            Assert.AreEqual(2, dispatcher.RingResourcePartnerMapping[RingType.Prod][ResourceType.Browse].Count);
            Assert.AreEqual(5, dispatcher.RingResourcePartnerMapping[RingType.Prod][ResourceType.Location].Count);
            Assert.AreEqual(true, dispatcher.RingPartnerAdapterMapping[RingType.Prod]["P1"].RealTimeDelete);
            Assert.AreEqual(false, dispatcher.RingPartnerAdapterMapping[RingType.Prod]["P2"].RealTimeDelete);
            Assert.AreEqual(false, dispatcher.RingPartnerAdapterMapping[RingType.Prod]["P3"].RealTimeDelete);
        }

        [TestMethod]
        public void ValidateAdapterMappingHasRequiredResourceType()
        {
            // Test that a required resource exists in the adapters configuration passes the validation check

            List<IPxfPartnerConfiguration> partnerConfigs = CreateDefaultPxfPartnerConfigurations();
            this.CreatePxfAdaptersConfiguration(partnerConfigs, new List<RingType> { RingType.Prod });

            // Only Browse is required
            this.adaptersConfiguration.SetupGet(c => c.RequiredResourceTypes).Returns(new List<ResourceType> { ResourceType.Browse });

            var dispatcher = new PxfDispatcher(
                this.CreatePrivacyConfigurationManager().Object,
                this.pxfAdaptersConfiguration.Object,
                MockPxfAdapterFactory().Object,
                CreateCertProviderMock().Object,
                CreateAadTokenProviderMock().Object,
                new ConsoleLogger(),
                this.mockCounterFactory.Object);

            var adapters = dispatcher.GetAdaptersForResourceType(CreatePxfRequestContext(), ResourceType.Browse, PxfAdapterCapability.View).ToList();
            Assert.IsNotNull(adapters);
            Assert.AreEqual(2, adapters.Count);
        }

        [TestMethod]
        public void ValidateAdapterMappingMissingResourceType()
        {
            // Test that a resource type not listed in the configuration is not considered required.

            List<IPxfPartnerConfiguration> partnerConfigs = CreateDefaultPxfPartnerConfigurations();
            this.CreatePxfAdaptersConfiguration(partnerConfigs, new List<RingType> { RingType.Prod });

            // Only Browse is required here, so test some other type (AppUsage) doesn't throw.
            this.adaptersConfiguration.SetupGet(c => c.RequiredResourceTypes).Returns(new List<ResourceType> { ResourceType.Browse });

            var dispatcher = new PxfDispatcher(
                this.CreatePrivacyConfigurationManager().Object,
                this.pxfAdaptersConfiguration.Object,
                MockPxfAdapterFactory().Object,
                CreateCertProviderMock().Object,
                CreateAadTokenProviderMock().Object,
                new ConsoleLogger(),
                this.mockCounterFactory.Object);

            var adapters = dispatcher.GetAdaptersForResourceType(CreatePxfRequestContext(), ResourceType.AppUsage, PxfAdapterCapability.View).ToList();
            Assert.IsNotNull(adapters);
        }

        [TestMethod, ExpectedException(typeof(NotSupportedException))]
        public void ValidateAdapterMappingEnforcesRequiredResourceType()
        {
            // Arrange: Test that a resource type listed in the configuration is required.
            this.adaptersConfiguration.SetupGet(c => c.RequiredResourceTypes).Returns(new List<ResourceType> { ResourceType.AppUsage });

            List<IPxfPartnerConfiguration> partnerConfigs = CreateDefaultPxfPartnerConfigurations();
            this.CreatePxfAdaptersConfiguration(partnerConfigs, new List<RingType> { RingType.Prod });

            var dispatcher = new PxfDispatcher(
                this.CreatePrivacyConfigurationManager().Object,
                this.pxfAdaptersConfiguration.Object,
                MockPxfAdapterFactory().Object,
                CreateCertProviderMock().Object,
                CreateAadTokenProviderMock().Object,
                new ConsoleLogger(),
                this.mockCounterFactory.Object);

            // Act
            try
            {
                var adapters = dispatcher.GetAdaptersForResourceType(CreatePxfRequestContext(), ResourceType.AppUsage, PxfAdapterCapability.View).ToList();
                Assert.Fail("Should have thrown because this resource type isn't in the adapters list.");
            }
            catch(Exception e)
            {
                // Assert
                // Verify no adapters exist for this resource type
                IList<IPxfPartnerConfiguration> mockAdapters = this.pxfAdaptersConfiguration.Object.RingPartnerConfigMapping.Values.Select(c => c.PartnerConfigMapping).SelectMany(x => x.Values).ToList();
                Assert.AreEqual(0, mockAdapters.Count(c => c.SupportedResources.Contains(ResourceType.AppUsage.ToString())));

                // Verify the correct exception as thrown
                Assert.IsTrue(e is NotSupportedException);
                var notSupportedException = e as NotSupportedException;
                Assert.AreEqual("No configured partners support resource type: AppUsage for ring type: Prod", e.Message);
                throw;
            }
        }

        private static IPxfRequestContext CreatePxfRequestContext()
        {
            var mockPxfRequestContext = new Mock<IPxfRequestContext>(MockBehavior.Strict);
            mockPxfRequestContext.SetupGet(p => p.Flights).Returns((string[])null);
            return mockPxfRequestContext.Object;
        }

        [TestMethod]
        public void InitializeWithMissingPartnerId()
        {
            var partnerConfigs = new List<IPxfPartnerConfiguration>
            {
                SimplePartnerConfig.Create(string.Empty, "https://p1.microsoft.com", "p1.microsoft.com", new[] { ResourceType.Browse }, AdapterVersion.PxfV1, false),
            };
            this.CreatePxfAdaptersConfiguration(partnerConfigs, new List<RingType> { RingType.Prod });

            this.ExpectedException<ArgumentException>(
                () => new PxfDispatcher(
                    this.CreatePrivacyConfigurationManager().Object,  
                    this.pxfAdaptersConfiguration.Object, 
                    MockPxfAdapterFactory().Object, 
                    CreateCertProviderMock().Object, 
                    CreateAadTokenProviderMock().Object,
                    new ConsoleLogger(), 
                    this.mockCounterFactory.Object));
        }

        [TestMethod]
        public void InitializeWithMissingPartnerUrl()
        {
            var partnerConfigs = new List<IPxfPartnerConfiguration>
            {
                SimplePartnerConfig.Create("P1", null, "p1.microsoft.com", new[] { ResourceType.Browse }, AdapterVersion.PxfV1, false),
            };
            this.CreatePxfAdaptersConfiguration(partnerConfigs);

            this.ExpectedException<ArgumentException>(
                () => new PxfDispatcher(
                    this.CreatePrivacyConfigurationManager().Object, 
                    this.pxfAdaptersConfiguration.Object, 
                    MockPxfAdapterFactory().Object, 
                    CreateCertProviderMock().Object, 
                    CreateAadTokenProviderMock().Object,
                    new ConsoleLogger(), 
                    this.mockCounterFactory.Object));
        }

        [TestMethod]
        public void InitializeWithNoSupportedResources()
        {
            var partnerConfigs = new List<IPxfPartnerConfiguration>
            {
                SimplePartnerConfig.Create("P1", "https://p1.microsoft.com", "p1.microsoft.com", (IList<string>)null, AdapterVersion.PxfV1, false),
            };
            this.CreatePxfAdaptersConfiguration(partnerConfigs);

            var dispatcher = new PxfDispatcher(
                this.CreatePrivacyConfigurationManager().Object, 
                this.pxfAdaptersConfiguration.Object, 
                MockPxfAdapterFactory().Object, 
                CreateCertProviderMock().Object, 
                CreateAadTokenProviderMock().Object,
                new ConsoleLogger(), 
                this.mockCounterFactory.Object);

            // Verify mapping tables
            Assert.AreEqual(1, dispatcher.RingPartnerAdapterMapping.First().Value.Count);
            Assert.AreEqual(0, dispatcher.RingResourcePartnerMapping.First().Value.Count);
        }

        [TestMethod]
        public void InitializeWithUnknownSupportedResource()
        {
            var partnerConfigs = new List<IPxfPartnerConfiguration>
            {
                SimplePartnerConfig.Create("P1", "https://p1.microsoft.com", "p1.microsoft.com", new[] { "Widgets" }, AdapterVersion.PxfV1, false),
            };
            this.CreatePxfAdaptersConfiguration(partnerConfigs);

            this.ExpectedException<ArgumentException>(
                () => new PxfDispatcher(
                    this.CreatePrivacyConfigurationManager().Object, 
                    this.pxfAdaptersConfiguration.Object, 
                    MockPxfAdapterFactory().Object, 
                    CreateCertProviderMock().Object, 
                    CreateAadTokenProviderMock().Object,
                    new ConsoleLogger(),
                    this.mockCounterFactory.Object));
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public async Task DeleteBrowseHistoryWithNoPartnersForResource()
        {
            var partnerConfigs = new List<IPxfPartnerConfiguration>
            {
                SimplePartnerConfig.Create("P3", "https://p3.microsoft.com", "p3.microsoft.com", new[] { ResourceType.Location }, AdapterVersion.PxfV1, false),
            };
            this.CreatePxfAdaptersConfiguration(partnerConfigs);

            var dispatcher = new PxfDispatcher(
                this.CreatePrivacyConfigurationManager().Object, 
                this.pxfAdaptersConfiguration.Object, 
                MockPxfAdapterFactory().Object, 
                CreateCertProviderMock().Object, 
                CreateAadTokenProviderMock().Object,
                new ConsoleLogger(), 
                this.mockCounterFactory.Object);

            // Get BrowseHistory
            await dispatcher.DeleteBrowseHistoryAsync(MockRequestContext().Object).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeleteBrowseHistoryAsync()
        {
            // Create config containing three 'browse' resource partners, but P2 does not support realTimeDelete.
            var partnerConfigs = new List<IPxfPartnerConfiguration>
            {
                SimplePartnerConfig.Create("P1", "https://p1.microsoft.com", "p1.microsoft.com", new[] { ResourceType.Browse }, AdapterVersion.PxfV1, realTimeDelete: true),
                SimplePartnerConfig.Create("P2", "https://p2.microsoft.com", "p2.microsoft.com", new[] { ResourceType.Browse }, AdapterVersion.PxfV1, realTimeDelete: false),
                SimplePartnerConfig.Create("P3", "https://p2.microsoft.com", "p2.microsoft.com", new[] { ResourceType.Browse }, AdapterVersion.PxfV1, realTimeDelete: true),
                SimplePartnerConfig.Create("P4", "https://p3.microsoft.com", "p3.microsoft.com", new[] { ResourceType.Location }, AdapterVersion.PxfV1, false),
            };
            this.CreatePxfAdaptersConfiguration(partnerConfigs);

            var partnerResponses = new Dictionary<string, DeleteResourceResponse>
            {
                { "P1", CreateDeleteResponse("P1", ResourceStatus.Deleted) },
                { "P2", CreateDeleteResponse("P2", ResourceStatus.PendingDelete) },
                { "P3", CreateDeleteResponse("P3", ResourceStatus.PendingDelete) },
            };

            var dispatcher = new PxfDispatcher(
                this.CreatePrivacyConfigurationManager().Object, 
                this.pxfAdaptersConfiguration.Object, 
                MockPxfAdapterFactory(partnerDeleteResponseMapping: partnerResponses).Object, 
                CreateCertProviderMock().Object, 
                CreateAadTokenProviderMock().Object,
                new ConsoleLogger(), 
                this.mockCounterFactory.Object);

            // Delete Browse History
            var response = await dispatcher.DeleteBrowseHistoryAsync(MockRequestContext().Object).ConfigureAwait(false);

            // Verify response
            // Only two partners should have been called, according to this configuration.
            Assert.IsNotNull(response);
            Assert.IsInstanceOfType(response, typeof(DeletionResponse<DeleteResourceResponse>));
            Assert.AreEqual(2, response.Items.Count());
            Assert.AreEqual(ResourceStatus.Deleted, response.Items.Single(p => p.PartnerId == "P1").Status);
            Assert.AreEqual(ResourceStatus.PendingDelete, response.Items.Single(p => p.PartnerId == "P3").Status);
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public async Task DeleteBrowseHistoryAsyncNoRealTimeDeletePartners()
        {
            // Create config containing three 'browse' resource partners, but no browse partners support realTimeDelete.
            var partnerConfigs = new List<IPxfPartnerConfiguration>
            {
                SimplePartnerConfig.Create("P1", "https://p1.microsoft.com", "p1.microsoft.com", new[] { ResourceType.Browse }, AdapterVersion.PxfV1, realTimeDelete: false),
                SimplePartnerConfig.Create("P2", "https://p2.microsoft.com", "p2.microsoft.com", new[] { ResourceType.Browse }, AdapterVersion.PxfV1, realTimeDelete: false),
                SimplePartnerConfig.Create("P3", "https://p2.microsoft.com", "p2.microsoft.com", new[] { ResourceType.Browse }, AdapterVersion.PxfV1, realTimeDelete: false),
                SimplePartnerConfig.Create("P4", "https://p3.microsoft.com", "p3.microsoft.com", new[] { ResourceType.Location }, AdapterVersion.PxfV1, false),
            };
            this.CreatePxfAdaptersConfiguration(partnerConfigs);

            var partnerResponses = new Dictionary<string, DeleteResourceResponse>();

            var dispatcher = new PxfDispatcher(
                this.CreatePrivacyConfigurationManager().Object, 
                this.pxfAdaptersConfiguration.Object,
                MockPxfAdapterFactory(partnerDeleteResponseMapping: partnerResponses).Object, 
                CreateCertProviderMock().Object, 
                CreateAadTokenProviderMock().Object,
                new ConsoleLogger(), 
                this.mockCounterFactory.Object);

            // Delete Browse History
            var response = await dispatcher.DeleteBrowseHistoryAsync(MockRequestContext().Object).ConfigureAwait(false);
        }

        private static Mock<IPxfAdapterFactory> MockPxfAdapterFactory(
            Dictionary<string, PagedResponse<BrowseResource>> partnerViewResponseMapping = null,
            Dictionary<string, DeleteResourceResponse> partnerDeleteResponseMapping = null)
        {
            var mockAdapterFactory = new Mock<IPxfAdapterFactory>(MockBehavior.Strict);
            mockAdapterFactory
                .Setup(
                    af => af.Create(
                        It.IsAny<ICertificateProvider>(), 
                        It.IsAny<IMsaIdentityServiceConfiguration>(), 
                        It.IsAny<IPxfPartnerConfiguration>(), 
                        It.IsAny<IAadTokenProvider>(),
                        It.IsAny<ILogger>(), 
                        It.IsAny<ICounterFactory>()))
                .Returns<
                    ICertificateProvider, 
                    IMsaIdentityServiceConfiguration, 
                    IPxfPartnerConfiguration, 
                    IAadTokenProvider,
                    ILogger, 
                    ICounterFactory>((cp, msa, pc, aadtp, log, cf) =>
                    {
                        var mockAdapter = new Mock<IPxfAdapter>(MockBehavior.Strict);
                        var browseResponse = partnerViewResponseMapping != null && partnerViewResponseMapping.ContainsKey(pc.Id) ? partnerViewResponseMapping[pc.Id] : null;
                        mockAdapter
                            .Setup(a => a.GetBrowseHistoryAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<OrderByType>(), It.IsAny<DateOption?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>()))
                            .ReturnsAsync(browseResponse);
                        mockAdapter
                            .Setup(a => a.GetNextBrowsePageAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<Uri>()))
                            .ReturnsAsync(browseResponse);
                        var deleteResponse = partnerDeleteResponseMapping != null && partnerDeleteResponseMapping.ContainsKey(pc.PartnerId) ? partnerDeleteResponseMapping[pc.PartnerId] : null;
                        mockAdapter
                            .Setup(a => a.DeleteBrowseHistoryAsync(It.IsAny<IPxfRequestContext>()))
                            .ReturnsAsync(deleteResponse);

                        return new PartnerAdapter
                        {
                            Adapter = mockAdapter.Object,
                            PartnerId = pc.Id,
                            RealTimeDelete = pc.RealTimeDelete,
                            RealTimeView = pc.RealTimeView,
                        };
                    });
            return mockAdapterFactory;
        }

        private static List<IPxfPartnerConfiguration> CreateDefaultPxfPartnerConfigurations()
        {
            var partnerConfigs = new List<IPxfPartnerConfiguration>
            {
                //// PXF V1
                SimplePartnerConfig.Create("P1", "https://p1.microsoft.com", "p1.microsoft.com", new[] { ResourceType.Browse }, AdapterVersion.PxfV1, true),
                SimplePartnerConfig.Create("P2", "https://p2.microsoft.com", "p2.microsoft.com", new[] { ResourceType.Browse }, AdapterVersion.PxfV1, false),
                SimplePartnerConfig.Create("P3", "https://p3.microsoft.com", "p3.microsoft.com", new[] { ResourceType.Location }, AdapterVersion.PxfV1, false),
                //// PDP V1
                SimplePartnerConfig.Create("P4", "https://p3.microsoft.com", "p3.microsoft.com", new[] { ResourceType.Location }, AdapterVersion.PdApiV2, true),
                SimplePartnerConfig.Create("P5", "https://p3.microsoft.com", "p3.microsoft.com", new[] { ResourceType.Location }, AdapterVersion.PdApiV2, true),
                SimplePartnerConfig.Create("P6", "https://p3.microsoft.com", "p3.microsoft.com", new[] { ResourceType.Location }, AdapterVersion.PdApiV2, true),
                SimplePartnerConfig.Create("P7", "https://p3.microsoft.com", "p3.microsoft.com", new[] { ResourceType.Location }, AdapterVersion.PdApiV2, true)
            };
            return partnerConfigs;
        }

        private static DeleteResourceResponse CreateDeleteResponse(string partnerId, ResourceStatus status = ResourceStatus.Deleted)
        {
            return new DeleteResourceResponse
            {
                PartnerId = partnerId,
                Status = status
            };
        }

        private static Mock<IPxfRequestContext> MockRequestContext()
        {
            var requestContextMock = new Mock<IPxfRequestContext>(MockBehavior.Strict);
            requestContextMock.SetupGet(r => r.UserProxyTicket).Returns("{myuserproxyticketgoeshere}");
            requestContextMock.SetupGet(r => r.CV).Returns(new CorrelationVector());
            requestContextMock.SetupGet(r => r.Flights).Returns(Enumerable.Empty<string>().ToArray());

            return requestContextMock;
        }

        private void CreatePxfAdaptersConfiguration(List<IPxfPartnerConfiguration> partnerConfigs)
        {
            this.CreatePxfAdaptersConfiguration(partnerConfigs, new List<RingType> { RingType.Prod });
        }

        private void CreatePxfAdaptersConfiguration(List<IPxfPartnerConfiguration> partnerConfigs, List<RingType> ringTypes)
        {
            this.pxfAdaptersConfiguration = new Mock<IDataManagementConfig>(MockBehavior.Strict);
            var partnerConfigsDictionary = partnerConfigs.ToDictionary(p => p.Id);

            var ringPartnerConfigsDictionary = new Dictionary<string, IRingPartnerConfigMapping>();
            foreach (RingType ringType in ringTypes)
            {
                var mockRingPartnerConfigMapping = new Mock<IRingPartnerConfigMapping>();
                mockRingPartnerConfigMapping.SetupGet(c => c.Ring).Returns(ringType);
                mockRingPartnerConfigMapping.SetupGet(c => c.PartnerConfigMapping).Returns(partnerConfigsDictionary);
                ringPartnerConfigsDictionary.Add(ringType.ToString(), mockRingPartnerConfigMapping.Object);
            }

            this.pxfAdaptersConfiguration.SetupGet(p => p.RingPartnerConfigMapping).Returns(ringPartnerConfigsDictionary);
        }

        private static Mock<IAadTokenProvider> CreateAadTokenProviderMock()
        {
            Mock<IAadTokenProvider> aadTokenProvider = new Mock<IAadTokenProvider>(MockBehavior.Strict);
            aadTokenProvider
                .Setup(o => o.GetPopTokenAsync(It.IsAny<AadPopTokenRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("this_is_an_AAD_PoP_token");
            return aadTokenProvider;
        }

        private Mock<IPrivacyConfigurationManager> CreatePrivacyConfigurationManager()
        {
            return CreatePrivacyConfigurationManager(CreateMsaIdentityConfigMock().Object, this.pxfAdaptersConfiguration.Object, this.serviceConfig.Object, this.adaptersConfiguration.Object);
        }

        private class SimplePartnerConfig : IPxfPartnerConfiguration
        {
            public static IPxfPartnerConfiguration Create(
                string partnerId,
                string baseUrl,
                string targetSite,
                ResourceType[] resourceTypes,
                AdapterVersion version,
                bool realTimeDelete,
                FacetDomain facetDomain = FacetDomain.Unknown,
                bool realTimeView = true)
            {
                return new SimplePartnerConfig
                {
                    Id = partnerId,
                    PartnerId = partnerId,
                    BaseUrl = baseUrl,
                    MsaS2STargetSite = targetSite,
                    SupportedResources = resourceTypes.Select(rt => rt.ToString()).ToList(),
                    PxfAdapterVersion = version,
                    RealTimeDelete = realTimeDelete,
                    SkipServerCertValidation = false,
                    RetryStrategyConfiguration = CreateRetryStrategyConfiguration(),
                    FacetDomain = facetDomain,
                    RealTimeView = realTimeView,
                    AgentFriendlyName = partnerId,
                    AssetDefinitionFriendlyName = partnerId,
                    AuthenticationType = AuthenticationType.Unknown
                };
            }

            public static IPxfPartnerConfiguration Create(
                string partnerId,
                string baseUrl,
                string targetSite,
                IList<string> resourceTypes,
                AdapterVersion version,
                bool realTimeDelete)
            {
                return new SimplePartnerConfig
                {
                    Id = partnerId,
                    PartnerId = partnerId,
                    BaseUrl = baseUrl,
                    MsaS2STargetSite = targetSite,
                    SupportedResources = resourceTypes,
                    PxfAdapterVersion = version,
                    RealTimeDelete = realTimeDelete,
                    SkipServerCertValidation = false,
                    RetryStrategyConfiguration = CreateRetryStrategyConfiguration(),
                    RealTimeView = true,
                    AgentFriendlyName = partnerId,
                    AssetDefinitionFriendlyName = partnerId,
                    AuthenticationType = AuthenticationType.Unknown
                };
            }

            public string PartnerId { get; set; }

            public string BaseUrl { get; set; }

            public string ResetUrl { get; set; }

            public string MsaS2STargetSite { get; set; }

            public string CounterCategoryName { get; set; }

            public IList<string> SupportedResources { get; set; }

            public AdapterVersion PxfAdapterVersion { get; set; }

            public bool RealTimeDelete { get; set; }

            public bool SkipServerCertValidation { get; set; }

            public PxfLocationCategory LocationCategory { get; private set; }

            public IRetryStrategyConfiguration RetryStrategyConfiguration { get; private set; }

            public int TimeoutInMilliseconds { get; private set; }

            public FacetDomain FacetDomain { get; private set; }

            public ICertificateConfiguration PartnerSignatureCertificateConfiguration { get; private set; }

            public IDictionary<string, string> CustomHeaders { get; private set; }

            public bool RealTimeView { get; private set; }

            private static IRetryStrategyConfiguration CreateRetryStrategyConfiguration()
            {
                var fixedIntervalRetryConfiguration = new Mock<IFixedIntervalRetryConfiguration>(MockBehavior.Strict);
                fixedIntervalRetryConfiguration.SetupGet(f => f.RetryCount).Returns(3);
                fixedIntervalRetryConfiguration.SetupGet(f => f.RetryIntervalInMilliseconds).Returns(1000);

                var retryStrategy = new Mock<IRetryStrategyConfiguration>(MockBehavior.Strict);
                retryStrategy.SetupGet(r => r.RetryMode).Returns(RetryMode.FixedInterval);
                retryStrategy.SetupGet(r => r.FixedIntervalRetryConfiguration).Returns(fixedIntervalRetryConfiguration.Object);
                return retryStrategy.Object;
            }

            public string AssetDefinitionFriendlyName { get; set; }

            public string AgentFriendlyName { get; set; }

            public string Id { get; set; }

            public IServicePointConfiguration ServicePointConfiguration { get; }

            public AuthenticationType AuthenticationType { get; private set; }

            public string AadTokenResourceId { get; private set; }

            public string AadTokenScope { get; private set; }

            public IDictionary<string, string> AdditionalParameters { get; private set; }
        }
    }
}

// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common;
    using Microsoft.Membership.MemberServices.Test.Common;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    /// <summary>
    ///     CoreServiceTest Base
    /// </summary>
    public abstract class CoreServiceTestBase
    {
        private const int GroupLocationDistanceMetersConfiguration = 11;

        private const int MaxAccuracyRadiusMeters = 300;

        private static bool assembliesLoaded;

        [TestInitialize]
        public void TestInitialize()
        {
            if (!assembliesLoaded)
            {
                // TODO: Figure out how to do this optionally if not in VSO. It doesn't work there.
                //// SqlServerTypes.Utilities.LoadNativeAssemblies(AppDomain.CurrentDomain.BaseDirectory);
                assembliesLoaded = true;
            }

            Sll.ResetContext();

            this.TestPuid = RequestFactory.GeneratePuid();
            this.TestCid = RequestFactory.GenerateCid();
            this.TestCountry = "test_country";
            this.TestUserProxyTicket = "test_user_proxy_ticket";
            this.TestCorrelationVector = new CorrelationVector();
            this.TestFlights = new string[0];
            this.MockPxfDispatcher = new Mock<IPxfDispatcher>(MockBehavior.Strict);
            this.MockPxfAdaptersConfiguration = new Mock<IDataManagementConfig>(MockBehavior.Strict);
            this.MockAdaptersConfiguration = new Mock<IAdaptersConfiguration>(MockBehavior.Strict);
            this.MockPrivacyConfigurationManager = new Mock<IPrivacyConfigurationManager>(MockBehavior.Strict);
            this.MockDataManagementConfig = new Mock<IDataManagementConfig>(MockBehavior.Strict);

            IDictionary<string, IRingPartnerConfigMapping> mockRingPartnerConfigMappingDictionary = new Dictionary<string, IRingPartnerConfigMapping>
            {
                { RingType.PreProd.ToString(), CreateMockRingPartnerConfigMapping(RingType.PreProd) },
                { RingType.Prod.ToString(), CreateMockRingPartnerConfigMapping(RingType.Prod) }
            };
            this.MockDataManagementConfig
                .Setup(p => p.RingPartnerConfigMapping)
                .Returns(mockRingPartnerConfigMappingDictionary);
            this.MockAdaptersConfiguration
                .Setup(p => p.GroupLocationDistanceMeters)
                .Returns(GroupLocationDistanceMetersConfiguration);
            this.MockAdaptersConfiguration
                .Setup(p => p.MaxAccuracyRadiusMeters)
                .Returns(MaxAccuracyRadiusMeters);
            this.MockAdaptersConfiguration
                .Setup(p => p.DefaultTargetRing)
                .Returns(RingType.Prod);
            this.MockAdaptersConfiguration
                .Setup(p => p.PrivacyFlightConfigurations)
                .Returns(new List<IFlightConfiguration>());
            this.MockPrivacyConfigurationManager
                .SetupGet(p => p.AdaptersConfiguration)
                .Returns(this.MockAdaptersConfiguration.Object);
            this.MockPrivacyConfigurationManager
                .SetupGet(p => p.PrivacyExperienceServiceConfiguration)
                .Returns(CreateMockPrivacyExperienceServiceConfig().Object);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Sll.ResetContext();
        }

        private static IRingPartnerConfigMapping CreateMockRingPartnerConfigMapping(RingType ringType)
        {
            IDictionary<string, IPxfPartnerConfiguration> mockConfigMapping = new Dictionary<string, IPxfPartnerConfiguration>
            {
                { "PartnerId1", CreateMockPartnerConfig("PartnerId1").Object },
                { "PartnerId2", CreateMockPartnerConfig("PartnerId2").Object }
            };

            Mock<IRingPartnerConfigMapping> mockRingPartnerConfigMapping = new Mock<IRingPartnerConfigMapping>(MockBehavior.Strict);
            mockRingPartnerConfigMapping.Setup(c => c.PartnerConfigMapping).Returns(mockConfigMapping);
            mockRingPartnerConfigMapping.Setup(c => c.Ring).Returns(ringType);
            return mockRingPartnerConfigMapping.Object;
        }

        protected Mock<IAdaptersConfiguration> MockAdaptersConfiguration { get; set; }

        protected Mock<IDataManagementConfig> MockDataManagementConfig { get; set; }

        protected Mock<IPrivacyConfigurationManager> MockPrivacyConfigurationManager { get; set; }

        protected Mock<IDataManagementConfig> MockPxfAdaptersConfiguration { get; set; }

        protected Mock<IPxfDispatcher> MockPxfDispatcher { get; set; }

        protected Policy PrivacyPolicy => Policies.Current;

        protected long TestCid { get; set; }

        protected CorrelationVector TestCorrelationVector { get; set; }

        protected string TestCountry { get; set; }

        protected string[] TestFlights { get; set; }

        protected long TestPuid { get; set; }

        protected IRequestContext TestRequestContext => this.CreateRequestContext(null);

        protected string TestUserProxyTicket { get; set; }

        protected IRequestContext CreateRequestContext(string callerName, LegalAgeGroup legalAgeGroup = 0, string familyJwt = null)
        {
            return RequestContext.CreateOldStyle(new Uri("https://unittest"),
                this.TestUserProxyTicket,
                familyJwt,
                this.TestPuid,
                this.TestPuid,
                this.TestCid,
                this.TestCid,
                this.TestCountry,
                callerName,
                11,
                this.TestFlights,
                legalAgeGroup: legalAgeGroup);
        }

        private static Mock<IPxfPartnerConfiguration> CreateMockPartnerConfig(string partnerId)
        {
            var partnerConfig = new Mock<IPxfPartnerConfiguration>(MockBehavior.Strict);
            partnerConfig.SetupGet(c => c.PartnerId).Returns(partnerId);
            partnerConfig.SetupGet(c => c.PxfAdapterVersion).Returns(AdapterVersion.PxfV1);
            partnerConfig.SetupGet(c => c.RealTimeDelete).Returns(true);
            partnerConfig.SetupGet(c => c.SkipServerCertValidation).Returns(true);
            partnerConfig.SetupGet(c => c.LocationCategory).Returns(PxfLocationCategory.Unknown);
            partnerConfig.SetupGet(c => c.AgentFriendlyName).Returns("Agent");
            return partnerConfig;
        }

        private static Mock<IPrivacyExperienceServiceConfiguration> CreateMockPrivacyExperienceServiceConfig()
        {
            var privacyExperienceServiceConfig = new Mock<IPrivacyExperienceServiceConfiguration>(MockBehavior.Strict);
            return privacyExperienceServiceConfig;
        }
    }
}

// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.ScopedDelete
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.PCF;
    using Microsoft.Membership.MemberServices.Privacy.Core.ScopedDelete;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyAdapters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.Test.Common;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Predicates;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class ScopedDeleteServiceTests
    {
        private const string BingTenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
        private const string BingNonProdAppId = "f4dc8e70-44d5-4a71-a7be-8ff2a8943e8d";
        private const string BingNonProdSiteName = "Bing_NonProd";

        private Mock<IPxfDispatcher> mockPxfDispatcher = new Mock<IPxfDispatcher>();
        private Mock<IPrivacyExperienceServiceConfiguration> mockPrivacyExperienceServiceConfiguration = new Mock<IPrivacyExperienceServiceConfiguration>();
        private Mock<IPrivacyConfigurationManager> mockConfigurationManager = new Mock<IPrivacyConfigurationManager>();
        private Mock<IPcfProxyService> mockPcfProxyService = new Mock<IPcfProxyService>();
        private IRequestContext sampleRequestContext;
        private ScopedDeleteService service;

        [TestInitialize]
        public void Initialize()
        {
            this.mockPrivacyExperienceServiceConfiguration
                .SetupGet(p => p.SiteIdToCallerName)
                .Returns(new Dictionary<string, string>() { { BingNonProdAppId, BingNonProdSiteName } });

            this.mockPrivacyExperienceServiceConfiguration
                .SetupGet(p => p.CloudInstance)
                .Returns(CloudInstanceType.PublicProd);

            this.mockConfigurationManager
                .SetupGet(c => c.PrivacyExperienceServiceConfiguration)
                .Returns(mockPrivacyExperienceServiceConfiguration.Object);

            var sampleIdentity = new AadIdentityWithMsaUserProxyTicket(BingNonProdAppId, Guid.NewGuid(), Guid.Parse(BingTenantId), "accessToken", "appDisplayName", 12341234, "ticket", 123123);
            this.sampleRequestContext = new RequestContext(sampleIdentity, new Uri("https://test"), new Dictionary<string, string[]>());

            this.mockPcfProxyService
                .Setup(m => m.PostDeleteRequestsAsync(It.IsAny<IRequestContext>(), It.IsAny<List<DeleteRequest>>()))
                .ReturnsAsync(new ServiceResponse<IList<Guid>>());

            this.mockPxfDispatcher
                .Setup(m => m.DeleteSearchHistoryAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<bool>()))
                .ReturnsAsync(new DeletionResponse<DeleteResourceResponse>(Enumerable.Empty<DeleteResourceResponse>()));

            this.mockPxfDispatcher
                .Setup(m => m.ExecuteForProvidersAsync(
                    It.IsAny<IPxfRequestContext>(),
                    It.Is<ResourceType>(x => x == ResourceType.Search),
                    It.Is<PxfAdapterCapability>(x => x == PxfAdapterCapability.Delete),
                    It.IsAny<Func<PartnerAdapter, Task<DeleteResourceResponse>>>()))
                .ReturnsAsync(Enumerable.Empty<DeleteResourceResponse>().ToList());

            this.service = new ScopedDeleteService(this.mockPxfDispatcher.Object, this.mockPcfProxyService.Object, this.mockConfigurationManager.Object,
                CreateMockAppConfiguration(2).Object, TestMockFactory.CreateLogger().Object);
        }


        private static Mock<IAppConfiguration> CreateMockAppConfiguration(int batchSize)
        {
            var mockAppConfig = new Mock<IAppConfiguration>(MockBehavior.Strict);
            mockAppConfig.Setup(c => c.GetConfigValue<int>(ConfigNames.PXS.ScopedDeleteService_DeleteRequestsBatchSize, It.IsAny<int>())).Returns(batchSize);
            return mockAppConfig;
        }

        [TestMethod]
        public async Task ShouldValidateNullContext()
        {
            // Arrange
            IRequestContext requestContext = null;

            // Act
            ServiceResponse response = await service.SearchRequestsAndQueryScopedDeleteAsync(requestContext, null);

            // Assert
            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(ErrorCode.InvalidInput.ToString(), response.Error.Code);
        }

        [TestMethod]
        public async Task ShouldValidateMissingIdentityContext()
        {
            // Arrange
            IRequestContext aadRequestContext = new RequestContext(new AadIdentity("1", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));

            // Act
            ServiceResponse response = await service.SearchRequestsAndQueryScopedDeleteAsync(aadRequestContext, null);

            // Assert
            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(ErrorCode.InvalidClientCredentials.ToString(), response.Error.Code);
        }

        [TestMethod]
        public async Task ShouldValidateInvalidIdsInput()
        {
            // Arrange
            string badGuid = "notGuid";

            // Act
            ServiceResponse response = await service.SearchRequestsAndQueryScopedDeleteAsync(this.sampleRequestContext, new[] { badGuid });

            // Assert
            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(ErrorCode.InvalidInput.ToString(), response.Error.Code);
            Assert.AreEqual($"Id {badGuid} is not a guid.", response.Error.Message);
        }

        [TestMethod]
        public async Task ShouldValidateNullInvalidIdsInput()
        {
            // Arrange
            string badGuid = null;

            // Act
            ServiceResponse response = await service.SearchRequestsAndQueryScopedDeleteAsync(this.sampleRequestContext, new[] { badGuid });

            // Assert
            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(ErrorCode.InvalidInput.ToString(), response.Error.Code);
            Assert.AreEqual($"Id {badGuid} is not a guid.", response.Error.Message);
        }

        [TestMethod]
        public async Task ShouldProcessBulkDelete()
        {
            // Arrange
            string[] searchRequestsAndQueryIds = null;

            // Act
            ServiceResponse response = await service.SearchRequestsAndQueryScopedDeleteAsync(this.sampleRequestContext, searchRequestsAndQueryIds);

            // Assert
            Assert.IsTrue(response.IsSuccess);

            this.mockPcfProxyService.Verify(
                m => m.PostDeleteRequestsAsync(It.IsAny<IRequestContext>(), It.IsAny<List<DeleteRequest>>()),
                Times.Once);

            this.mockPxfDispatcher.Verify(
                m => m.DeleteSearchHistoryAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<bool>()),
                Times.Once);

            this.mockPxfDispatcher.Verify(
                m => m.ExecuteForProvidersAsync(
                    It.IsAny<IPxfRequestContext>(),
                    It.IsAny<ResourceType>(),
                    It.IsAny<PxfAdapterCapability>(),
                    It.IsAny<Func<PartnerAdapter, Task<DeleteResourceResponse>>>()),
                Times.Never);
        }

        [TestMethod]
        public async Task ShouldProcessSingleDeletes()
        {
            // Arrange
            Guid[] searchRequestsAndQueryGuids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            string[] inputSearchRequestsAndQueryIds = searchRequestsAndQueryGuids.Select(g => g.ToString()).ToArray();
            string[] expectedSearchRequestsAndQueryIds = searchRequestsAndQueryGuids.Select(g => g.ToString("N").ToUpperInvariant()).ToArray();

            // Act
            ServiceResponse response = await service.SearchRequestsAndQueryScopedDeleteAsync(this.sampleRequestContext, inputSearchRequestsAndQueryIds);

            // Assert
            Assert.IsTrue(response.IsSuccess);

            // Since we are mocking AppConfig with batchSize of 2 for UT, so for this test case, as we have a total of 3 searchRequestsAndQueryGuids, hence PXS should call PCF twice.
            this.mockPcfProxyService.Verify(
                m => m.PostDeleteRequestsAsync(
                    It.IsAny<IRequestContext>(),
                    It.Is<List<DeleteRequest>>(
                        l => l.All(
                            item => expectedSearchRequestsAndQueryIds.Contains(
                                ((SearchRequestsAndQueryPredicate)item.Predicate).ImpressionGuid
                                )))),
                Times.Exactly(2));

            this.mockPxfDispatcher.Verify(
                m => m.DeleteSearchHistoryAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<bool>()),
                Times.Never);

            this.mockPxfDispatcher.Verify(
                m => m.ExecuteForProvidersAsync(
                    It.IsAny<IPxfRequestContext>(),
                    It.IsAny<ResourceType>(),
                    It.IsAny<PxfAdapterCapability>(),
                    It.IsAny<Func<PartnerAdapter, Task<DeleteResourceResponse>>>()),
                Times.Once);

        }

        [TestMethod]
        public async Task PostDeleteRequestsAsyncCreatesNotTestSignal()
        {
            // Arrange
            Guid[] searchRequestsAndQueryGuids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            string[] inputSearchRequestsAndQueryIds = searchRequestsAndQueryGuids.Select(g => g.ToString()).ToArray();
            string[] expectedSearchRequestsAndQueryIds = searchRequestsAndQueryGuids.Select(g => g.ToString("N").ToUpperInvariant()).ToArray();

            // Act
            ServiceResponse response = await service.SearchRequestsAndQueryScopedDeleteAsync(this.sampleRequestContext, inputSearchRequestsAndQueryIds);

            // Assert
            Assert.IsTrue(response.IsSuccess);

            this.mockPcfProxyService.Verify(
                m => m.PostDeleteRequestsAsync(
                    It.IsAny<IRequestContext>(),
                    It.Is<List<DeleteRequest>>(
                        l => l.All(
                            item => !item.IsTestRequest))));

            this.mockPxfDispatcher.Verify(
                m => m.DeleteSearchHistoryAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<bool>()),
                Times.Never);

            this.mockPxfDispatcher.Verify(
                m => m.ExecuteForProvidersAsync(
                    It.IsAny<IPxfRequestContext>(),
                    It.IsAny<ResourceType>(),
                    It.IsAny<PxfAdapterCapability>(),
                    It.IsAny<Func<PartnerAdapter, Task<DeleteResourceResponse>>>()),
                Times.Once);

        }
    }
}

// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.UnitTests.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using System.Web.Http;
    using System.Web.Http.Results;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Aggregation;
    using Microsoft.Membership.MemberServices.Privacy.Core.PCF;
    using Microsoft.Membership.MemberServices.Privacy.Core.TestMsa;
    using Microsoft.Membership.MemberServices.Privacy.Core.Timeline;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Controllers;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Security;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.PrivacyOperation.Contracts.PrivacySubject;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Ms.Qos;

    [TestClass]
    public class PrivacyRequestApiControllerV2Tests : ControllerTestBase
    {
        private static readonly string appId = Guid.NewGuid().ToString();

        private readonly Mock<IPrivacyExperienceServiceConfiguration> mockConfiguration = new Mock<IPrivacyExperienceServiceConfiguration>(MockBehavior.Strict);

        private readonly Mock<DataTypesClassifier> mockDataTypeClassifier = new Mock<DataTypesClassifier>(MockBehavior.Strict, Policies.Current);

        private readonly Mock<ITimelineService> mockTimelineService = new Mock<ITimelineService>(MockBehavior.Strict);

        private Mock<IPrivacyConfigurationManager> mockConfigManager;

        private Mock<IPcfProxyService> mockPcfProxyService;

        private Mock<ITestMsaService> mockTestMsaService;

        private readonly Mock<IRequestClassifier> mockRequestClassifier = new Mock<IRequestClassifier>(MockBehavior.Strict);

        private readonly Mock<IAppConfiguration> mockAppConfiguration = new Mock<IAppConfiguration>(MockBehavior.Loose);


        [TestInitialize]
        public void TestInit()
        {
            Sll.ResetContext();
            this.mockPcfProxyService = new Mock<IPcfProxyService>();
            this.mockTestMsaService = new Mock<ITestMsaService>();
            this.mockConfigManager = new Mock<IPrivacyConfigurationManager>();
            this.mockConfigManager.Setup(c => c.PrivacyExperienceServiceConfiguration).Returns(this.mockConfiguration.Object);
            this.mockConfigManager.Setup(c => c.PrivacyExperienceServiceConfiguration.CloudInstance).Returns(CloudInstanceType.PublicProd);
            this.mockConfigManager.Setup(c => c.PrivacyExperienceServiceConfiguration.SiteIdToCallerName).Returns(new Dictionary<string, string> { { "Test", "Test" } });
            this.mockRequestClassifier.Setup(c => c.IsTestRequest(It.IsAny<string>(), It.IsAny<IIdentity>(), It.IsAny<string>())).Returns(false);
            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(ConfigNames.PXS.PRCMakeEmailMandatory, false)).ReturnsAsync(false);
        }

        [TestMethod]
        public void PrivacyRequestDeleteV2_Success()
        {
            var request = new HttpRequestMessage();
            request.RequestUri = new Uri("https://unittest");

            // Complete list of data types which PRC sends to PXS
            var dataTypes =
                "BrowsingHistory,SearchRequestsAndQuery,PreciseUserLocation,InkingTypingAndSpeechUtterance,ContentConsumption,ProductAndServicePerformance,ProductAndServiceUsage,SoftwareSetupAndInventory,DeviceConnectivityAndConfiguration,DemographicInformation,FeedbackAndRatings,FitnessAndActivity,SupportContent,SupportInteraction,EnvironmentalSensor";
            DateTimeOffset startTime = DateTimeOffset.MinValue;
            DateTimeOffset endTime = DateTimeOffset.MaxValue;

            // Guids expected from PCF calls (Non timeline supported data types)
            Guid id1 = Guid.NewGuid();
            Guid id2 = Guid.NewGuid();
            List<Guid> expectedRequestIdsFromPcf = new List<Guid>
            {
                id1,
                id2
            };
            this.mockPcfProxyService
                .Setup(x => x.PostDeleteRequestsAsync(It.IsAny<IRequestContext>(), It.IsAny<List<DeleteRequest>>()))
                .ReturnsAsync(new ServiceResponse<IList<Guid>> { Result = expectedRequestIdsFromPcf.ToList() });

            // Guids expected from time line response
            Guid id3 = Guid.NewGuid();
            Guid id4 = Guid.NewGuid();
            List<Guid> expectedRequestIdsFromTimeLineResponse = new List<Guid>
            {
                id3,
                id4
            };
            this.mockTimelineService.Setup(c => c.DeleteAsync(It.IsAny<IRequestContext>(), It.IsAny<IList<string>>(), It.IsAny<TimeSpan>(), It.IsAny<string>()))
                .Returns(Task.FromResult(new ServiceResponse<IList<Guid>> { Result = expectedRequestIdsFromTimeLineResponse.ToList() }));

            var privacyRequestApiController = new PrivacyRequestApiController(
                this.mockPcfProxyService.Object,
                this.mockTestMsaService.Object,
                this.mockConfigManager.Object,
                this.mockTimelineService.Object,
                this.mockDataTypeClassifier.Object,
                this.mockRequestClassifier.Object,
                this.mockAppConfiguration.Object)
            {
                Request = request
            };

            Mock<IAadS2SAuthResult> mockAuthResult = CreateMockAuthResult(appId);
            var identity = new AadIdentityWithMsaUserProxyTicket(
                mockAuthResult.Object.InboundAppId,
                mockAuthResult.Object.ObjectId,
                mockAuthResult.Object.TenantId,
                mockAuthResult.Object.AccessToken,
                mockAuthResult.Object.AppDisplayName,
                It.IsAny<long>(),
                this.TestUserProxyTicket,
                It.IsAny<long>());
            privacyRequestApiController.RequestContext.Principal = new CallerPrincipal(identity);

            var deleteRequest = new DeleteOperationRequest
            {
                Subject = new MsaSelfAuthSubject(this.TestUserProxyTicket)
            };

            var incomingApiEvent = new IncomingApiEvent();
            incomingApiEvent.baseData.operationName = "DeleteOperation_Test";
            Sll.Context.Vector = new CorrelationVector();
            Sll.Context.ChangeIncomingEvent(incomingApiEvent);
            Sll.Context.Incoming.baseData = new IncomingServiceRequest();
            IHttpActionResult actual = privacyRequestApiController.DeleteAsyncV2(dataTypes, startTime, endTime, deleteRequest).Result;

            var conNegResult = actual as OkNegotiatedContentResult<OperationResponse>;

            this.mockPcfProxyService.Verify(
                o => o.PostDeleteRequestsAsync(It.IsAny<IRequestContext>(), It.IsAny<List<DeleteRequest>>()),
                Times.Once);
            this.mockTimelineService.Verify(
                o => o.DeleteAsync(It.IsAny<IRequestContext>(), It.IsAny<IList<string>>(), It.IsAny<TimeSpan>(), It.IsAny<string>()),
                Times.Once);

            Assert.IsNotNull(actual);
            Assert.AreEqual(4, conNegResult.Content.Ids.Count);

            // Should contain PCF request Ids - for non timeline supported data types
            CollectionAssert.Contains(conNegResult.Content.Ids.ToList(), id1);
            CollectionAssert.Contains(conNegResult.Content.Ids.ToList(), id2);

            //Should contain PCF request Ids from timeline response
            CollectionAssert.Contains(conNegResult.Content.Ids.ToList(), id3);
            CollectionAssert.Contains(conNegResult.Content.Ids.ToList(), id4);
        }

        [TestMethod]
        public void PrivacyRequestExportV2_Success()
        {
            var request = new HttpRequestMessage();
            request.RequestUri = new Uri("https://unittest");

            // Complete list of data types which PRC sends to PXS
            var dataTypes =
                "BrowsingHistory,SearchRequestsAndQuery,PreciseUserLocation,InkingTypingAndSpeechUtterance,ContentConsumption,ProductAndServicePerformance,ProductAndServiceUsage,SoftwareSetupAndInventory,DeviceConnectivityAndConfiguration,DemographicInformation,FeedbackAndRatings,FitnessAndActivity,SupportContent,SupportInteraction,EnvironmentalSensor";
            DateTimeOffset startTime = DateTimeOffset.MinValue;
            DateTimeOffset endTime = DateTimeOffset.MaxValue;

            // Guid expected from PCF calls (Non timeline supported data types)
            Guid expectedRequestIdFromPcf = Guid.NewGuid();
            this.mockPcfProxyService
                .Setup(x => x.PostExportRequestAsync(It.IsAny<IRequestContext>(), It.IsAny<ExportRequest>()))
                .ReturnsAsync(new ServiceResponse<Guid> { Result = expectedRequestIdFromPcf });

            var privacyRequestApiController = new PrivacyRequestApiController(
                this.mockPcfProxyService.Object,
                this.mockTestMsaService.Object,
                this.mockConfigManager.Object,
                this.mockTimelineService.Object,
                this.mockDataTypeClassifier.Object,
                this.mockRequestClassifier.Object,
                this.mockAppConfiguration.Object)
            {
                Request = request
            };

            Mock<IAadS2SAuthResult> mockAuthResult = CreateMockAuthResult(appId);
            var identity = new AadIdentityWithMsaUserProxyTicket(
                mockAuthResult.Object.InboundAppId,
                mockAuthResult.Object.ObjectId,
                mockAuthResult.Object.TenantId,
                mockAuthResult.Object.AccessToken,
                mockAuthResult.Object.AppDisplayName,
                It.IsAny<long>(),
                this.TestUserProxyTicket,
                It.IsAny<long>());
            privacyRequestApiController.RequestContext.Principal = new CallerPrincipal(identity);

            var exportRequest = new ExportOperationRequest
            {
                Subject = new MsaSelfAuthSubject(this.TestUserProxyTicket)
            };

            var incomingApiEvent = new IncomingApiEvent();
            incomingApiEvent.baseData.operationName = "ExportOperation_Test";
            Sll.Context.Vector = new CorrelationVector();
            Sll.Context.ChangeIncomingEvent(incomingApiEvent);
            Sll.Context.Incoming.baseData = new IncomingServiceRequest();
            IHttpActionResult actual = privacyRequestApiController.ExportAsyncV2(dataTypes, startTime, endTime, exportRequest).Result;

            var conNegResult = actual as OkNegotiatedContentResult<OperationResponse>;

            this.mockPcfProxyService.Verify(
                o => o.PostExportRequestAsync(It.IsAny<IRequestContext>(), It.IsAny<ExportRequest>()),
                Times.Once);

            Assert.IsNotNull(actual);
            Assert.AreEqual(1, conNegResult.Content.Ids.Count);
            Assert.AreEqual(expectedRequestIdFromPcf, conNegResult.Content.Ids[0]);
        }

        private static Mock<IAadS2SAuthResult> CreateMockAuthResult(string appId)
        {
            var mockAuthResult = new Mock<IAadS2SAuthResult>(MockBehavior.Strict);
            mockAuthResult.Setup(c => c.ObjectId).Returns(Guid.NewGuid());
            mockAuthResult.Setup(c => c.TenantId).Returns(Guid.NewGuid());
            mockAuthResult.Setup(c => c.InboundAppId).Returns(appId);
            mockAuthResult.Setup(c => c.AppDisplayName).Returns("Test App");
            mockAuthResult.Setup(c => c.AccessToken).Returns("Test Access Token");
            return mockAuthResult;
        }
    }
}

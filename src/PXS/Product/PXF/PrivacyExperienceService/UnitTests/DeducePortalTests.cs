// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.UnitTests.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http.Results;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Aggregation;
    using Microsoft.Membership.MemberServices.Privacy.Core.PCF;
    using Microsoft.Membership.MemberServices.Privacy.Core.TestMsa;
    using Microsoft.Membership.MemberServices.Privacy.Core.Timeline;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.PrivacySubject;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Controllers;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Security;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class DeducePortalTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            Sll.ResetContext();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Sll.ResetContext();
        }

        [TestMethod]
        public async Task TestDeducePortalAmc()
        {
            var timelineServiceMock = new Mock<ITimelineService>(MockBehavior.Loose);
            var loggerMock = new Mock<ILogger>(MockBehavior.Loose);
            var configurationMock = new Mock<IPrivacyConfigurationManager>(MockBehavior.Loose);
            var pxsConfigurationMock = new Mock<IPrivacyExperienceServiceConfiguration>(MockBehavior.Loose);
            var appConfigMock = new Mock<IAppConfiguration>(MockBehavior.Loose);
            pxsConfigurationMock.SetupGet(c => c.SiteIdToCallerName).Returns(new Dictionary<string, string> { { "0", "MEEPortal_INT_PROD" } });
            configurationMock.SetupGet(c => c.PrivacyExperienceServiceConfiguration).Returns(pxsConfigurationMock.Object);
            var controller = new TimelineV2Controller(timelineServiceMock.Object, loggerMock.Object, configurationMock.Object, appConfigMock.Object);
            var identity = new MsaSelfIdentity("ticket", null, 0, 0, 0, "caller", 0, 0, "country/region", null, false, AuthType.MsaSelf, LegalAgeGroup.Undefined, null);
            controller.User = new CallerPrincipal(identity);
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://testuri");
            controller.Request = requestMessage;

            timelineServiceMock
                .Setup(s => s.DeleteAsync(It.IsAny<IRequestContext>(), It.IsAny<IList<string>>(), It.IsAny<TimeSpan>(), It.Is<string>(p => p == Portals.Amc)))
                .ReturnsAsync(new ServiceResponse<IList<Guid>> { Result = new[] { Guid.Empty } })
                .Verifiable();
            await controller.DeleteAsync(Policies.Current.DataTypes.Ids.BrowsingHistory.Value, TimeSpan.MaxValue).ConfigureAwait(false);
            timelineServiceMock.Verify();
        }

        [TestMethod]
        public async Task TestDeducePortalPartnerTestPage()
        {
            Sll.ResetContext();
            Sll.Context.ChangeIncomingEvent(new IncomingApiEvent());
            Sll.Context.Vector = new CorrelationVector();

            var pcfProxyServiceMock = new Mock<IPcfProxyService>(MockBehavior.Loose);
            pcfProxyServiceMock.Setup(
                    p => p.PostDeleteRequestsAsync(It.IsAny<IRequestContext>(), Match.Create<List<DeleteRequest>>(requests => requests.Single().Portal == Portals.PartnerTestPage)))
                .ReturnsAsync(new ServiceResponse<IList<Guid>> { Result = new List<Guid> { Guid.Empty } })
                .Verifiable();
            var testMsaServiceMock = new Mock<ITestMsaService>(MockBehavior.Loose);
            var configurationMock = new Mock<IPrivacyConfigurationManager>(MockBehavior.Loose);
            var pxsConfigurationMock = new Mock<IPrivacyExperienceServiceConfiguration>(MockBehavior.Loose);
            var mockAppConfiguration = new Mock<IAppConfiguration>(MockBehavior.Loose);
            mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.AuthenticationLogging, true)).ReturnsAsync(false);
            pxsConfigurationMock.SetupGet(c => c.SiteIdToCallerName).Returns(new Dictionary<string, string> { { "0", "MEEPortal_INT_PROD" } });
            configurationMock.SetupGet(c => c.PrivacyExperienceServiceConfiguration).Returns(pxsConfigurationMock.Object);
            var timelineServiceMock = new Mock<ITimelineService>(MockBehavior.Loose);
            var requestClassifierMock = new Mock<IRequestClassifier>(MockBehavior.Loose);
            var controller = new PrivacyRequestApiController(
                pcfProxyServiceMock.Object,
                testMsaServiceMock.Object,
                configurationMock.Object,
                timelineServiceMock.Object,
                new DataTypesClassifier(Policies.Current),
                requestClassifierMock.Object,
                mockAppConfiguration.Object);
            var identity = new MsaSelfIdentity("ticket", null, 0, 0, 0, "caller", 0, 0, "country/region", null, false, AuthType.MsaSelf, LegalAgeGroup.Undefined, null);
            controller.User = new CallerPrincipal(identity);
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://testuri");
            controller.Request = requestMessage;

            await controller.DeleteAsync(
                Policies.Current.DataTypes.Ids.BrowsingHistory.Value,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                new DeleteOperationRequest
                {
                    Subject = new MsaSelfAuthSubject("proxy")
                }).ConfigureAwait(false);
            timelineServiceMock.Verify();
        }

        [TestMethod]
        public async Task TestDeducePortalPcd()
        {
            Sll.ResetContext();
            Sll.Context.ChangeIncomingEvent(new IncomingApiEvent());
            Sll.Context.Vector = new CorrelationVector();

            var pcfProxyServiceMock = new Mock<IPcfProxyService>(MockBehavior.Loose);
            pcfProxyServiceMock.Setup(
                    p => p.PostDeleteRequestsAsync(It.IsAny<IRequestContext>(), Match.Create<List<DeleteRequest>>(requests => requests.Single().Portal == Portals.Pcd)))
                .ReturnsAsync(new ServiceResponse<IList<Guid>> { Result = new List<Guid> { Guid.Empty } })
                .Verifiable();
            var testMsaServiceMock = new Mock<ITestMsaService>(MockBehavior.Loose);
            var configurationMock = new Mock<IPrivacyConfigurationManager>(MockBehavior.Loose);
            var pxsConfigurationMock = new Mock<IPrivacyExperienceServiceConfiguration>(MockBehavior.Loose);
            var mockAppConfiguration = new Mock<IAppConfiguration>(MockBehavior.Loose);
            mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(ConfigNames.PXS.PRCMakeEmailMandatory, false)).ReturnsAsync(false);
            pxsConfigurationMock.SetupGet(c => c.SiteIdToCallerName).Returns(new Dictionary<string, string> { { "appId", "PCD_PROD" } });
            configurationMock.SetupGet(c => c.PrivacyExperienceServiceConfiguration).Returns(pxsConfigurationMock.Object);
            var timelineServiceMock = new Mock<ITimelineService>(MockBehavior.Loose);
            var requestClassifierMock = new Mock<IRequestClassifier>(MockBehavior.Loose);
            var controller = new PrivacyRequestApiController(
                pcfProxyServiceMock.Object,
                testMsaServiceMock.Object,
                configurationMock.Object,
                timelineServiceMock.Object,
                new DataTypesClassifier(Policies.Current),
                requestClassifierMock.Object,
                mockAppConfiguration.Object);
            var identity = new AadIdentityWithMsaUserProxyTicket("appId", Guid.Empty, Guid.Empty, "accessToken", "appDisplayName", 0, "ticket", 0);
            controller.User = new CallerPrincipal(identity);
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://testuri");
            controller.Request = requestMessage;

            await controller.DeleteAsyncV2(
                Policies.Current.DataTypes.Ids.CloudServiceProvider.Value,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                new PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.DeleteOperationRequest
                {
                    Subject = new PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.MsaSelfAuthSubject("ticket")
                }).ConfigureAwait(false);
            timelineServiceMock.Verify();
        }

        [TestMethod]
        public void TestCallerBlock()
        {
            var timelineServiceMock = new Mock<ITimelineService>(MockBehavior.Loose);
            var loggerMock = new Mock<ILogger>(MockBehavior.Loose);
            var configurationMock = new Mock<IPrivacyConfigurationManager>(MockBehavior.Loose);
            var pxsConfigurationMock = new Mock<IPrivacyExperienceServiceConfiguration>(MockBehavior.Loose);
            var appConfigMock = new Mock<IAppConfiguration>(MockBehavior.Loose);

            // This will block any callers
            appConfigMock
                .Setup(s => s.IsFeatureFlagEnabledAsync<ICustomOperatorContext>(It.IsAny<string>(), It.IsAny<ICustomOperatorContext>(), It.IsAny<bool>()))
                .ReturnsAsync(true);

            pxsConfigurationMock.SetupGet(c => c.SiteIdToCallerName).Returns(new Dictionary<string, string> { { "0", "MEEPortal_INT_PROD" } });
            configurationMock.SetupGet(c => c.PrivacyExperienceServiceConfiguration).Returns(pxsConfigurationMock.Object);
            var controller = new TimelineV2Controller(timelineServiceMock.Object, loggerMock.Object, configurationMock.Object, appConfigMock.Object);
            var identity = new MsaSelfIdentity("ticket", null, 0, 0, 0, "caller", 0, 0, "country/region", null, false, AuthType.MsaSelf, LegalAgeGroup.Undefined, null);
            controller.User = new CallerPrincipal(identity);
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://testuri");
            controller.Request = requestMessage;

            var response = controller.WarmupV1() as ResponseMessageResult;
            Assert.AreEqual(HttpStatusCode.Unauthorized, response.Response.StatusCode);
        }
    }
}

// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.UnitTests.Security
{
    using System.Security.Authentication;
    using System.Security.Principal;
    using System.Web.Http;
    using System.Web.Http.Controllers;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.IdentityModel.S2S;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Aggregation;
    using Microsoft.Membership.MemberServices.Privacy.Core.Timeline;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Controllers;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Security;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    /// <summary>
    ///     PrivacyExperienceMsaAadAuthorizationAttributeTests
    /// </summary>
    [TestClass]
    public class PrivacyExperienceMsaAadAuthorizationAttributeTests : AuthorizationAttributeTestBase
    {
        private readonly Mock<DataTypesClassifier> dataTypeClassifierMock = new Mock<DataTypesClassifier>(MockBehavior.Strict, Policies.Current);

        private readonly Mock<ITimelineService> timelineServiceMock = new Mock<ITimelineService>(MockBehavior.Strict);

        private readonly Mock<IRequestClassifier> requestClassifierMock = new Mock<IRequestClassifier>(MockBehavior.Strict);

        private readonly Mock<IAppConfiguration> mockAppConfiguration = new Mock<IAppConfiguration>(MockBehavior.Loose);

        [TestInitialize]
        public void TestInitialize()
        {
            this.requestClassifierMock.Setup(c => c.IsTestRequest(It.IsAny<string>(), It.IsAny<IIdentity>(), It.IsAny<string>())).Returns(false);
            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(ConfigNames.PXS.PRCMakeEmailMandatory, false)).ReturnsAsync(false);
        }

        [TestMethod]
        [ExpectedException(typeof(AuthenticationException))]
        public void PrivacyExperienceMsaAadAuthorizationAttributeFailControllerType()
        {
            HttpActionContext context = CreateHttpActionContext(new MsaOnlyPrivacyController());
            var s2SResult = new S2SAuthenticationResult();
            var result = new AadS2SAuthResult(s2SResult, "accessToken");
            var identity = new AadIdentity(result.InboundAppId, result.ObjectId, result.TenantId, result.AccessToken, result.AppDisplayName);
            ((ApiController)context.ControllerContext.Controller).User = new CallerPrincipal(identity);

            var attribute = new PrivacyExperienceIdentityAuthorizationAttribute(typeof(MsaSelfIdentity));
            attribute.OnAuthorization(context);
        }

        [TestMethod]
        [ExpectedException(typeof(AuthenticationException))]
        public void PrivacyExperienceMsaAadAuthorizationAttributeFailIdenityType()
        {
            HttpActionContext context = CreateHttpActionContext(new MsaOnlyPrivacyController());
            var identity = new MsaSiteIdentity("callerName", 123456);
            ((ApiController)context.ControllerContext.Controller).User = new CallerPrincipal(identity);

            var attribute = new PrivacyExperienceIdentityAuthorizationAttribute(typeof(MsaSelfIdentity));
            attribute.OnAuthorization(context);
        }

        [TestMethod]
        public void PrivacyExperienceMsaAadAuthorizationAttributeSuccess()
        {
            HttpActionContext context =
                CreateHttpActionContext(new PrivacyRequestApiController(null, null, null, this.timelineServiceMock.Object, this.dataTypeClassifierMock.Object, this.requestClassifierMock.Object,this.mockAppConfiguration.Object));
            var s2SResult = new S2SAuthenticationResult();
            var result = new AadS2SAuthResult(s2SResult, "accessToken");
            var identity = new AadIdentity(result.InboundAppId, result.ObjectId, result.TenantId, result.AccessToken, result.AppDisplayName);
            ((ApiController)context.ControllerContext.Controller).User = new CallerPrincipal(identity);

            var attribute = new PrivacyExperienceIdentityAuthorizationAttribute(typeof(AadIdentity));
            attribute.OnAuthorization(context);

            Assert.IsNull(context.Response);
        }

        [TestMethod]
        [ExpectedException(typeof(AuthenticationException))]
        public void PrivacyExperienceMsaAadWithUserProxyTicketAuthorizationAttributeFail()
        {
            HttpActionContext context =
                CreateHttpActionContext(new PrivacyRequestApiController(null, null, null, this.timelineServiceMock.Object, this.dataTypeClassifierMock.Object, this.requestClassifierMock.Object, this.mockAppConfiguration.Object));
            var s2SResult = new S2SAuthenticationResult();
            var result = new AadS2SAuthResult(s2SResult, "accessToken");
            var identity = new AadIdentity(result.InboundAppId, result.ObjectId, result.TenantId, result.AccessToken, result.AppDisplayName);
            ((ApiController)context.ControllerContext.Controller).User = new CallerPrincipal(identity);

            var attribute = new PrivacyExperienceIdentityAuthorizationAttribute(typeof(AadIdentityWithMsaUserProxyTicket));
            attribute.OnAuthorization(context);

            Assert.IsNull(context.Response);
        }

        [TestMethod]
        public void PrivacyExperienceMsaAadWithUserProxyTicketAuthorizationAttributeSuccess()
        {
            HttpActionContext context =
                CreateHttpActionContext(new PrivacyRequestApiController(null, null, null, this.timelineServiceMock.Object, this.dataTypeClassifierMock.Object, this.requestClassifierMock.Object, this.mockAppConfiguration.Object));
            var s2SResult = new S2SAuthenticationResult();
            var result = new AadS2SAuthResult(s2SResult, "accessToken");
            var identity = new AadIdentityWithMsaUserProxyTicket(
                result.InboundAppId,
                result.ObjectId,
                result.TenantId,
                result.AccessToken,
                result.AppDisplayName,
                1234567,
                "proxyticket",
                7654321);
            ((ApiController)context.ControllerContext.Controller).User = new CallerPrincipal(identity);

            var attribute = new PrivacyExperienceIdentityAuthorizationAttribute(typeof(AadIdentityWithMsaUserProxyTicket));
            attribute.OnAuthorization(context);

            Assert.IsNull(context.Response);
        }
    }
}

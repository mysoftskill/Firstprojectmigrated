// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.UnitTests.Security
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Formatting;
    using System.Security.Authentication;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using System.Web.Http;
    using System.Web.Http.Controllers;
    using System.Web.Http.Dependencies;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Controllers;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Security;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common;
    using Microsoft.Membership.MemberServices.Test.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    /// <summary>
    ///     PrivacyExperience AuthorizationAttribute Test
    /// </summary>
    [TestClass]
    public class PrivacyExperienceAuthorizationAttributeTest : AuthorizationAttributeTestBase
    {
        private readonly Mock<IPrivacyExperienceServiceConfiguration> serviceConfig = new Mock<IPrivacyExperienceServiceConfiguration>(MockBehavior.Strict);

        private readonly Mock<IDependencyResolver> resolver = new Mock<IDependencyResolver>(MockBehavior.Strict);

        private readonly Mock<IAppConfiguration> appConfig = new Mock<IAppConfiguration>(MockBehavior.Strict);

        [TestInitialize]
        public void Initialize()
        {
            var oboConf = new Mock<IOnBehalfOfConfiguration>(MockBehavior.Strict);
            oboConf.Setup(c => c.EnforceAgeAuthZRules).Returns(true);
            this.serviceConfig.Setup(c => c.OnBehalfOfConfiguration).Returns(oboConf.Object);

            var mockConfigurationManager = new Mock<IPrivacyConfigurationManager>(MockBehavior.Strict);
            mockConfigurationManager.Setup(c => c.PrivacyExperienceServiceConfiguration).Returns(this.serviceConfig.Object);

            this.resolver.Setup(m => m.BeginScope()).Returns(this.resolver.Object);
            this.resolver.Setup(m => m.GetService(typeof(IPrivacyConfigurationManager))).Returns(mockConfigurationManager.Object);
            this.resolver.Setup(m => m.GetService(typeof(IContentNegotiator))).Returns(new DefaultContentNegotiator());

            this.appConfig.Setup(m => m.IsFeatureFlagEnabledAsync(It.IsAny<string>(), true)).Returns(new ValueTask<bool>(false));
        }

        private static IAgeAuthZRules CreateMockAuthorizationAgeRules(bool returnSuccess)
        {
            var mockAuthRules = new Mock<IAgeAuthZRules>(MockBehavior.Strict);
            mockAuthRules.Setup(c => c.CanView(It.IsAny<MsaSelfIdentity>())).Returns(returnSuccess);
            mockAuthRules.Setup(c => c.CanDelete(It.IsAny<MsaSelfIdentity>())).Returns(returnSuccess);
            return mockAuthRules.Object;
        }

        #region View

        [TestMethod]
        public void PrivacyExperienceAuthorizationAttributeSuccessForMsaSelfView()
        {
            long puid = RequestFactory.GeneratePuid();
            var identity = new MsaSelfIdentity("proxy ticket", null, puid, puid, RequestFactory.GenerateCid(), "caller name", 123, 456, "country/region", null, true);
            HttpActionContext context = CreateHttpActionContext(CreateController());
            ((ApiController)context.ControllerContext.Controller).User = new CallerPrincipal(identity);
            context.RequestContext.Configuration.DependencyResolver = this.resolver.Object;

            var attribute = new PrivacyExperienceAgeAuthZAuthorizationAttribute(CreateMockAuthorizationAgeRules(true), PrivacyAction.View);
            attribute.OnAuthorization(context);

            Assert.IsNull(context.Response);
        }

        [TestMethod]
        public async Task PrivacyExperienceAuthorizationAttributeErrorForMsaSelfView()
        {
            long puid = RequestFactory.GeneratePuid();
            var identity = new MsaSelfIdentity(
                "proxy ticket",
                null,
                puid,
                puid,
                RequestFactory.GenerateCid(),
                "caller name",
                123,
                456,
                "US",
                DateTimeOffset.Parse("2015/01/01"),
                true);

            string errorMessage =
                $"User is unauthorized by majority age rules. Auth-type: {identity.AuthType}. IsChildInFamily: {identity.IsChildInFamily}";
            var expectedError = new Error(ErrorCode.UnauthorizedMajorityAge, errorMessage);

            HttpActionContext context = CreateHttpActionContext(CreateController());
            ((ApiController)context.ControllerContext.Controller).User = new CallerPrincipal(identity);
            context.RequestContext.Configuration.DependencyResolver = this.resolver.Object;

            var attribute = new PrivacyExperienceAgeAuthZAuthorizationAttribute(CreateMockAuthorizationAgeRules(false), PrivacyAction.View);
            attribute.OnAuthorization(context);

            Assert.IsNotNull(context.Response);

            Error actualError = await context.Response.Content.ReadAsAsync<Error>().ConfigureAwait(false);
            Assert.IsNotNull(actualError);
            EqualityHelper.AreEqual(expectedError, actualError);
        }

        [TestMethod]
        public void PrivacyExperienceAuthorizationAttributeSuccessForMsaOboView()
        {
            long puid = RequestFactory.GeneratePuid();
            var identity = new MsaSelfIdentity(
                "proxy ticket",
                null,
                puid,
                puid,
                RequestFactory.GenerateCid(),
                "caller name",
                123,
                456,
                "country/region",
                null,
                true,
                AuthType.OnBehalfOf);
            HttpActionContext context = CreateHttpActionContext(CreateController());
            ((ApiController)context.ControllerContext.Controller).User = new CallerPrincipal(identity);
            context.RequestContext.Configuration.DependencyResolver = this.resolver.Object;

            var attribute = new PrivacyExperienceAgeAuthZAuthorizationAttribute(CreateMockAuthorizationAgeRules(true), PrivacyAction.View);
            attribute.OnAuthorization(context);

            Assert.IsNull(context.Response);
        }

        [TestMethod]
        public async Task PrivacyExperienceAuthorizationAttributeErrorForMsaOboView()
        {
            long puid = RequestFactory.GeneratePuid();
            var identity = new MsaSelfIdentity(
                "proxy ticket",
                null,
                puid,
                puid,
                RequestFactory.GenerateCid(),
                "caller name",
                123,
                456,
                "US",
                DateTimeOffset.Parse("2015/01/01"),
                true,
                AuthType.OnBehalfOf);

            string errorMessage =
                $"User is unauthorized by majority age rules. Auth-type: {identity.AuthType}. IsChildInFamily: {identity.IsChildInFamily}";
            var expectedError = new Error(ErrorCode.UnauthorizedMajorityAge, errorMessage);

            HttpActionContext context = CreateHttpActionContext(CreateController());
            ((ApiController)context.ControllerContext.Controller).User = new CallerPrincipal(identity);
            context.RequestContext.Configuration.DependencyResolver = this.resolver.Object;

            var attribute = new PrivacyExperienceAgeAuthZAuthorizationAttribute(CreateMockAuthorizationAgeRules(false), PrivacyAction.View);
            attribute.OnAuthorization(context);

            Assert.IsNotNull(context.Response);

            Error actualError = await context.Response.Content.ReadAsAsync<Error>().ConfigureAwait(false);
            Assert.IsNotNull(actualError);
            EqualityHelper.AreEqual(expectedError, actualError);
        }

        [TestMethod]
        public async Task PrivacyExperienceAuthorizationAttributeErrorAuthTypeNoneView()
        {
            long puid = RequestFactory.GeneratePuid();
            var identity = new MsaSelfIdentity(
                "proxy ticket",
                null,
                puid,
                puid,
                RequestFactory.GenerateCid(),
                "caller name",
                123,
                456,
                "US",
                DateTimeOffset.Parse("2015/01/01"),
                true,
                AuthType.None);

            string errorMessage = $"User is unauthorized with Auth-type: {identity.AuthType}";
            var expectedError = new Error(ErrorCode.InvalidClientCredentials, errorMessage);

            HttpActionContext context = CreateHttpActionContext(CreateController());
            ((ApiController)context.ControllerContext.Controller).User = new CallerPrincipal(identity);
            context.RequestContext.Configuration.DependencyResolver = this.resolver.Object;

            var attribute = new PrivacyExperienceAgeAuthZAuthorizationAttribute(CreateMockAuthorizationAgeRules(false), PrivacyAction.View);
            attribute.OnAuthorization(context);

            Assert.IsNotNull(context.Response);

            Error actualError = await context.Response.Content.ReadAsAsync<Error>().ConfigureAwait(false);
            Assert.IsNotNull(actualError);
            EqualityHelper.AreEqual(expectedError, actualError);
        }

        #endregion

        #region Delete

        [TestMethod]
        public void PrivacyExperienceAuthorizationAttributeSuccessForMsaSelfDelete()
        {
            long puid = RequestFactory.GeneratePuid();
            var identity = new MsaSelfIdentity("proxy ticket", null, puid, puid, RequestFactory.GenerateCid(), "caller name", 123, 456, "country", null, true);
            HttpActionContext context = CreateHttpActionContext(CreateController());
            ((ApiController)context.ControllerContext.Controller).User = new CallerPrincipal(identity);
            context.RequestContext.Configuration.DependencyResolver = this.resolver.Object;

            var attribute = new PrivacyExperienceAgeAuthZAuthorizationAttribute(CreateMockAuthorizationAgeRules(true), PrivacyAction.Delete);
            attribute.OnAuthorization(context);

            Assert.IsNull(context.Response);
        }

        [TestMethod]
        public async Task PrivacyExperienceAuthorizationAttributeErrorForMsaSelfDelete()
        {
            long puid = RequestFactory.GeneratePuid();
            var identity = new MsaSelfIdentity(
                "proxy ticket",
                null,
                puid,
                puid,
                RequestFactory.GenerateCid(),
                "caller name",
                123,
                456,
                "US",
                DateTimeOffset.Parse("2015/01/01"),
                true);

            string errorMessage =
                $"User is unauthorized by majority age rules. Auth-type: {identity.AuthType}. IsChildInFamily: {identity.IsChildInFamily}";
            var expectedError = new Error(ErrorCode.UnauthorizedMajorityAge, errorMessage);

            HttpActionContext context = CreateHttpActionContext(CreateController());
            ((ApiController)context.ControllerContext.Controller).User = new CallerPrincipal(identity);
            context.RequestContext.Configuration.DependencyResolver = this.resolver.Object;

            var attribute = new PrivacyExperienceAgeAuthZAuthorizationAttribute(CreateMockAuthorizationAgeRules(false), PrivacyAction.Delete);
            attribute.OnAuthorization(context);

            Assert.IsNotNull(context.Response);

            Error actualError = await context.Response.Content.ReadAsAsync<Error>().ConfigureAwait(false);
            Assert.IsNotNull(actualError);
            EqualityHelper.AreEqual(expectedError, actualError);
        }

        [TestMethod]
        public void PrivacyExperienceAuthorizationAttributeSuccessForMsaOboDelete()
        {
            long puid = RequestFactory.GeneratePuid();
            var identity = new MsaSelfIdentity(
                "proxy ticket",
                null,
                puid,
                puid,
                RequestFactory.GenerateCid(),
                "caller name",
                123,
                456,
                "country",
                null,
                true,
                AuthType.OnBehalfOf);
            HttpActionContext context = CreateHttpActionContext(CreateController());
            ((ApiController)context.ControllerContext.Controller).User = new CallerPrincipal(identity);

            var attribute = new PrivacyExperienceAgeAuthZAuthorizationAttribute(CreateMockAuthorizationAgeRules(true), PrivacyAction.Delete);
            attribute.OnAuthorization(context);

            Assert.IsNull(context.Response);
        }

        [TestMethod]
        public async Task PrivacyExperienceAuthorizationAttributeErrorForMsaOboDelete()
        {
            long puid = RequestFactory.GeneratePuid();
            var identity = new MsaSelfIdentity(
                "proxy ticket",
                null,
                puid,
                puid,
                RequestFactory.GenerateCid(),
                "caller name",
                123,
                456,
                "US",
                DateTimeOffset.Parse("2015/01/01"),
                true,
                AuthType.OnBehalfOf);

            string errorMessage =
                $"User is unauthorized by majority age rules. Auth-type: {identity.AuthType}. IsChildInFamily: {identity.IsChildInFamily}";
            var expectedError = new Error(ErrorCode.UnauthorizedMajorityAge, errorMessage);

            HttpActionContext context = CreateHttpActionContext(CreateController());
            ((ApiController)context.ControllerContext.Controller).User = new CallerPrincipal(identity);
            context.RequestContext.Configuration.DependencyResolver = this.resolver.Object;

            var attribute = new PrivacyExperienceAgeAuthZAuthorizationAttribute(CreateMockAuthorizationAgeRules(false), PrivacyAction.Delete);
            attribute.OnAuthorization(context);

            Assert.IsNotNull(context.Response);

            Error actualError = await context.Response.Content.ReadAsAsync<Error>().ConfigureAwait(false);
            Assert.IsNotNull(actualError);
            EqualityHelper.AreEqual(expectedError, actualError);
        }

        [TestMethod]
        public async Task PrivacyExperienceAuthorizationAttributeErrorAuthTypeNoneDelete()
        {
            long puid = RequestFactory.GeneratePuid();
            var identity = new MsaSelfIdentity(
                "proxy ticket",
                null,
                puid,
                puid,
                RequestFactory.GenerateCid(),
                "caller name",
                123,
                456,
                "US",
                DateTimeOffset.Parse("2015/01/01"),
                true,
                AuthType.None);

            string errorMessage = $"User is unauthorized with Auth-type: {identity.AuthType}";
            var expectedError = new Error(ErrorCode.InvalidClientCredentials, errorMessage);

            HttpActionContext context = CreateHttpActionContext(CreateController());
            ((ApiController)context.ControllerContext.Controller).User = new CallerPrincipal(identity);
            context.RequestContext.Configuration.DependencyResolver = this.resolver.Object;

            var attribute = new PrivacyExperienceAgeAuthZAuthorizationAttribute(CreateMockAuthorizationAgeRules(false), PrivacyAction.Delete);
            attribute.OnAuthorization(context);

            Assert.IsNotNull(context.Response);

            Error actualError = await context.Response.Content.ReadAsAsync<Error>().ConfigureAwait(false);
            Assert.IsNotNull(actualError);
            EqualityHelper.AreEqual(expectedError, actualError);
        }

        #endregion

        #region Default

        [TestMethod]
        public void PrivacyExperienceAuthorizationAttributeSuccess()
        {
            long puid = RequestFactory.GeneratePuid();
            var identity = new MsaSelfIdentity("proxy ticket", null, puid, puid, RequestFactory.GenerateCid(), "caller name", 123, 456, "country/region", null, true);
            HttpActionContext context = CreateHttpActionContext(CreateController());
            ((ApiController)context.ControllerContext.Controller).User = new CallerPrincipal(identity);
            context.RequestContext.Configuration.DependencyResolver = this.resolver.Object;

            var attribute = new PrivacyExperienceAgeAuthZAuthorizationAttribute(typeof(AgeAuthZAlwaysTrue), PrivacyAction.Default);
            attribute.OnAuthorization(context);

            Assert.IsNull(context.Response);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidCastException))]
        public void PrivacyExperienceAuthorizationAttributeNotCalledFromApiController()
        {
            HttpActionContext context = CreateHttpActionContext(CreateController());
            context.ControllerContext.Controller = new Mock<IHttpController>().Object;
            context.RequestContext.Configuration.DependencyResolver = this.resolver.Object;

            var attribute = new PrivacyExperienceAgeAuthZAuthorizationAttribute(typeof(AgeAuthZAlwaysTrue), PrivacyAction.Default);
            attribute.OnAuthorization(context);
        }

        [TestMethod]
        [ExpectedException(typeof(AuthenticationException))]
        public void PrivacyExperienceAuthorizationAttributeUserNull()
        {
            HttpActionContext context = CreateHttpActionContext(CreateController());
            ((ApiController)context.ControllerContext.Controller).User = null;
            context.RequestContext.Configuration.DependencyResolver = this.resolver.Object;

            var attribute = new PrivacyExperienceAgeAuthZAuthorizationAttribute(typeof(AgeAuthZAlwaysTrue), PrivacyAction.Default);
            attribute.OnAuthorization(context);
        }

        [TestMethod]
        [ExpectedException(typeof(AuthenticationException))]
        public void PrivacyExperienceAuthorizationAttributeIdentityNull()
        {
            HttpActionContext context = CreateHttpActionContext(CreateController());
            ((ApiController)context.ControllerContext.Controller).User = new CallerPrincipal(null);
            context.RequestContext.Configuration.DependencyResolver = this.resolver.Object;

            var attribute = new PrivacyExperienceAgeAuthZAuthorizationAttribute(typeof(AgeAuthZAlwaysTrue), PrivacyAction.Default);
            attribute.OnAuthorization(context);
        }

        [TestMethod]
        [ExpectedException(typeof(AuthenticationException))]
        public void PrivacyExperienceAuthorizationAttributeInvalidIdentity()
        {
            HttpActionContext context = CreateHttpActionContext(CreateController());
            ((ApiController)context.ControllerContext.Controller).User = new CallerPrincipal(new GenericIdentity("generic name"));
            context.RequestContext.Configuration.DependencyResolver = this.resolver.Object;

            var attribute = new PrivacyExperienceAgeAuthZAuthorizationAttribute(typeof(AgeAuthZAlwaysTrue), PrivacyAction.Default);
            attribute.OnAuthorization(context);
        }

        [TestMethod]
        public async Task PrivacyExperienceAuthorizationAttributeMissingProxyTicket()
        {
            var expectedError = new Error(ErrorCode.MissingClientCredentials, "User proxy ticket must be provided for this API through S2S header.");
            long puid = RequestFactory.GeneratePuid();
            var identity = new MsaSelfIdentity(string.Empty, null, puid, puid, RequestFactory.GenerateCid(), "caller name", 123, 456, "test_countryRegion", null, true);

            HttpActionContext context = CreateHttpActionContext(CreateController());
            ((ApiController)context.ControllerContext.Controller).User = new CallerPrincipal(identity);
            context.RequestContext.Configuration.DependencyResolver = this.resolver.Object;

            var attribute = new PrivacyExperienceAgeAuthZAuthorizationAttribute(typeof(AgeAuthZAlwaysTrue), PrivacyAction.Default);
            attribute.OnAuthorization(context);

            Assert.IsNotNull(context.Response);

            Error actualError = await context.Response.Content.ReadAsAsync<Error>().ConfigureAwait(false);
            Assert.IsNotNull(actualError);
            EqualityHelper.AreEqual(expectedError, actualError);
        }

        #endregion

        private KeepAliveController CreateController()
        {
            return new KeepAliveController(this.appConfig.Object);
        }
    }
}

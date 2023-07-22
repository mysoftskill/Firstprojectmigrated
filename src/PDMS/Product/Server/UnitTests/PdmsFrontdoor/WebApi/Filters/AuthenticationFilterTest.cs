namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.UnitTest
{
    using System;
    using System.Net.Http;
    using System.Security.Principal;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http.Controllers;

    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.Filters;
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Ploeh.AutoFixture;
    using Xunit;

    public class AuthenticationFilterTest
    {
        private Func<Task<HttpResponseMessage>> continuation = () => Task.FromResult(default(HttpResponseMessage));

        [Theory(DisplayName = "Verify that set principal is only called for enabled providers."), AutoMoqData]
        public async Task VerifyOnlyEnabledProvidersAreCalled(
            IPrincipal requestPrincipal,
            AuthenticatedPrincipal principal,
            Mock<IAuthenticationProvider> enabledProvider, 
            Mock<IAuthenticationProvider> disabledProvider,
            Fixture fixture)
        {
            enabledProvider.Setup(m => m.Enabled).Returns(true);
            disabledProvider.Setup(m => m.Enabled).Returns(false);

            var filter = new AuthenticationFilter(principal, new[] { enabledProvider.Object, disabledProvider.Object });
            var actionContext = this.CreateContext(requestPrincipal, fixture);

            await filter.ExecuteActionFilterAsync(actionContext, CancellationToken.None, this.continuation).ConfigureAwait(false);

            enabledProvider.Verify(m => m.SetPrincipal(requestPrincipal, principal), Times.Once);
            disabledProvider.Verify(m => m.SetPrincipal(requestPrincipal, principal), Times.Never);
        }

        [Theory(DisplayName = "Verify that authentication principal has valuse if all authentication is disabled."), AutoMoqData]
        public async Task VerifyAuthenticationDisabledBehavior(
            IPrincipal requestPrincipal,
            AuthenticatedPrincipal principal,
            Fixture fixture)
        {
            var filter = new AuthenticationFilter(principal, new IAuthenticationProvider[0]);
            var actionContext = this.CreateContext(requestPrincipal, fixture);

            await filter.ExecuteActionFilterAsync(actionContext, CancellationToken.None, this.continuation).ConfigureAwait(false);

            Assert.Equal("Disabled", principal.ApplicationId);
            Assert.Null(principal.ClaimsPrincipal);
            Assert.Equal("Disabled", principal.UserId);
        }

        private HttpActionContext CreateContext(IPrincipal requestPrincipal, Fixture fixture)
        {
            fixture.DisableRecursionCheck();

            var requestContext = new HttpRequestContext();
            requestContext.Principal = requestPrincipal;

            var controller = fixture.Create<IHttpController>();
            var controllerContext = new HttpControllerContext(requestContext, new HttpRequestMessage(), new HttpControllerDescriptor(), controller);

            var descriptor = fixture.Create<HttpActionDescriptor>();
            var actionContext = new HttpActionContext(controllerContext, descriptor);

            return actionContext;
        }
    }
}
namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.Filters;
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Ploeh.AutoFixture;
    using Xunit;

    public class AuthorizationFilterTest
    {
        private Func<Task<HttpResponseMessage>> continuation = () => Task.FromResult(default(HttpResponseMessage));

        [Theory(DisplayName = "When ApplicationId is not set for the authenticated principal, then fail the call as not authenticated.")]
        [InlineAutoMoqData(null)]
        [InlineAutoMoqData("")]
        [InlineAutoMoqData(" ")]
        public Task When_ApplicationIdEmpty_Then_NotAuthenticated(string appId, AuthenticatedPrincipal principal, Mock<IAuthenticationProvider> provider)
        {
            principal.ApplicationId = appId;
            return this.AssertNotAuthenticated(principal, provider);
        }

        [Theory(DisplayName = "When UserName is not set for the authenticated principal, then proceed with the request.")]
        [InlineAutoMoqData(null)]
        [InlineAutoMoqData("")]
        [InlineAutoMoqData(" ")]
        public Task When_UserNameEmpty_Then_Authenticated(string userName, AuthenticatedPrincipal principal, Mock<IAuthenticationProvider> provider)
        {
            principal.UserId = userName;
            return this.AssertAuthenticated(principal, new[] { provider.Object });
        }

        [Theory(DisplayName = "When ClaimsPrincipal is not set for the authenticated principal, then fail the call as not authenticated."), AutoMoqData]
        public Task When_ClaimsPrincipalEmpty_Then_NotAuthenticated(AuthenticatedPrincipal principal, Mock<IAuthenticationProvider> provider)
        {
            principal.ClaimsPrincipal = null;
            return this.AssertNotAuthenticated(principal, provider);
        }

        [Theory(DisplayName = "When all properties set for authenticated principal, then proceed with the request."), AutoMoqData]
        public Task When_AllPropertiesSet_Then_AllowRequest(AuthenticatedPrincipal principal, Mock<IAuthenticationProvider> provider)
        {
            provider.Setup(m => m.Enabled).Returns(true);

            return this.AssertAuthenticated(principal, new[] { provider.Object });
        }

        [Theory(DisplayName = "When all providers are disabled, then skip authorization."), AutoMoqData]
        public async Task When_NoProviderEnabled_Then_SkipAuthorization(IEnumerable<Mock<IAuthenticationProvider>> mockProviders, SessionProperties sessionProperties, Mock<IOperationAccessProvider> operationAccessProvider)
        {
            var principal = new AuthenticatedPrincipal(); // This would normally cause an exception.
            
            var providers = mockProviders.Select(provider =>
            {
                provider.Setup(m => m.Enabled).Returns(false);
                return provider.Object;
            });

            var filter = new AuthorizationFilter(principal, sessionProperties, providers, operationAccessProvider.Object);

            await filter.ExecuteActionFilterAsync(null, CancellationToken.None, this.continuation).ConfigureAwait(false);

            operationAccessProvider.Verify(m => m.GetAccessPermissions(It.IsAny<string>()), Times.Never);
        }

        [Theory(DisplayName = "When there is no access permission configuration for the specified application, then fail the call as not authorized."), AutoMoqData]
        public Task When_NullConfiguration_Then_NotAuthorized(AuthenticatedPrincipal principal, SessionProperties sessionProperties, Mock<IAuthenticationProvider> provider, Mock<IOperationAccessProvider> operationAccessProvider)
        {
            provider.Setup(m => m.Enabled).Returns(true);

            operationAccessProvider.Setup(m => m.GetAccessPermissions(principal.ApplicationId)).Returns<OperationAccessPermission>(null);

            var filter = new AuthorizationFilter(principal, sessionProperties, new[] { provider.Object }, operationAccessProvider.Object);

            return Assert.ThrowsAsync<ApplicationNotAuthorizedError>(() =>
                filter.ExecuteActionFilterAsync(null, CancellationToken.None, this.continuation));
        }

        [Theory(DisplayName = "When the required operation is not allowed for this application, then fail the call as not authorized."), AutoMoqData]
        public Task When_OperationNotAllowed_Then_NotAuthorized(
            AuthenticatedPrincipal principal,
            SessionProperties sessionProperties,
            OperationAccessPermission operationAccessPermission,
            Mock<IAuthenticationProvider> provider,
            Mock<IOperationAccessProvider> operationAccessProvider)
        {
            provider.Setup(m => m.Enabled).Returns(true);

            operationAccessProvider.Setup(m => m.GetAccessPermissions(principal.ApplicationId)).Returns(operationAccessPermission);

            var filter = new AuthorizationFilter(principal, sessionProperties, new[] { provider.Object }, operationAccessProvider.Object);

            return Assert.ThrowsAsync<ApplicationNotAuthorizedError>(() =>
                filter.ExecuteActionFilterAsync(null, CancellationToken.None, this.continuation));
        }

        [Theory(DisplayName = "When all operations are allowed for this application, then proceed with the request."), AutoMoqData]
        public async Task When_AllOperationsAllowed_Then_Authorized(
            AuthenticatedPrincipal principal,
            SessionProperties sessionProperties,
            OperationAccessPermission operationAccessPermission,
            Mock<IAuthenticationProvider> provider,
            Mock<IOperationAccessProvider> operationAccessProvider)
        {
            provider.Setup(m => m.Enabled).Returns(true);
            
            operationAccessPermission.AllowedOperations = new[] { "*" };

            operationAccessProvider.Setup(m => m.GetAccessPermissions(principal.ApplicationId)).Returns(operationAccessPermission);

            var filter = new AuthorizationFilter(principal, sessionProperties, new[] { provider.Object }, operationAccessProvider.Object);

            await filter.ExecuteActionFilterAsync(null, CancellationToken.None, this.continuation).ConfigureAwait(false);

            operationAccessProvider.Verify(m => m.GetAccessPermissions(It.IsAny<string>()), Times.Once);
        }

        [Theory(DisplayName = "When all providers are disabled, then PartnerName set as ApplicationId"), AutoMoqData]
        public async Task When_NoProviderEnabled_Then_SetPartnerNameAsApplicationId(
            AuthenticatedPrincipal principal,
            SessionProperties sessionProperties,
            Mock<IOperationAccessProvider> operationAccessProvider,
            Mock<IAuthenticationProvider> provider)
        {
            principal.ClaimsPrincipal = null;

            provider.Setup(m => m.Enabled).Returns(false);

            var filter = new AuthorizationFilter(principal, sessionProperties, new[] { provider.Object }, operationAccessProvider.Object);

            await filter.ExecuteActionFilterAsync(null, CancellationToken.None, this.continuation).ConfigureAwait(false);

            Assert.Equal(principal.ApplicationId, sessionProperties.PartnerName);

            Assert.Equal(principal.UserId, sessionProperties.User);
        }

        [Theory(DisplayName = "When not authenticated, then PartnerName set as ApplicationId"), AutoMoqData]
        public async Task When_NotAuthenticated_Then_SetPartnerNameAsApplicationId(
            AuthenticatedPrincipal principal,
            SessionProperties sessionProperties,
            Mock<IOperationAccessProvider> operationAccessProvider,
            Mock<IAuthenticationProvider> provider)
        {
            principal.ClaimsPrincipal = null;

            provider.Setup(m => m.Enabled).Returns(true);

            var filter = new AuthorizationFilter(principal, sessionProperties, new[] { provider.Object }, operationAccessProvider.Object);

            await Assert.ThrowsAsync<NotAuthenticatedError>(() => filter.ExecuteActionFilterAsync(null, CancellationToken.None, this.continuation)).ConfigureAwait(false);

            Assert.Equal(principal.ApplicationId, sessionProperties.PartnerName);

            Assert.Equal(principal.UserId, sessionProperties.User);
        }

        [Theory(DisplayName = "When there is no access permission configuration for the specified application, then PartnerName set as ApplicationId."), AutoMoqData]
        public async Task When_NullConfiguration_Then_SetPartnerNameAsApplicationId(
            AuthenticatedPrincipal principal,
            SessionProperties sessionProperties,
            Mock<IAuthenticationProvider> provider,
            Mock<IOperationAccessProvider> operationAccessProvider)
        {
            provider.Setup(m => m.Enabled).Returns(true);

            operationAccessProvider.Setup(m => m.GetAccessPermissions(principal.ApplicationId)).Returns<OperationAccessPermission>(null);

            var filter = new AuthorizationFilter(principal, sessionProperties, new[] { provider.Object }, operationAccessProvider.Object);

            await Assert.ThrowsAsync<ApplicationNotAuthorizedError>(() => filter.ExecuteActionFilterAsync(null, CancellationToken.None, this.continuation)).ConfigureAwait(false);

            Assert.Equal(principal.ApplicationId, sessionProperties.PartnerName);

            Assert.Equal(principal.UserId, sessionProperties.User);
        }

        [Theory(DisplayName = "When the required operation is not allowed for this application, then PartnerName set as FriendlyName."), AutoMoqData]
        public async Task When_OperationNotAllowed_Then_SetPartnerNameAsFriendlyName(
            AuthenticatedPrincipal principal,
            SessionProperties sessionProperties,
            OperationAccessPermission operationAccessPermission,
            Mock<IAuthenticationProvider> provider,
            Mock<IOperationAccessProvider> operationAccessProvider)
        {
            provider.Setup(m => m.Enabled).Returns(true);

            operationAccessProvider.Setup(m => m.GetAccessPermissions(principal.ApplicationId)).Returns(operationAccessPermission);

            var filter = new AuthorizationFilter(principal, sessionProperties, new[] { provider.Object }, operationAccessProvider.Object);

            var exn = await Assert.ThrowsAsync<ApplicationNotAuthorizedError>(() => filter.ExecuteActionFilterAsync(null, CancellationToken.None, this.continuation)).ConfigureAwait(false);

            Assert.Equal(operationAccessPermission.FriendlyName, sessionProperties.PartnerName);

            Assert.Equal(principal.UserId, sessionProperties.User);
            Assert.Equal(principal.ApplicationId, ((ApplicationNotAuthorizedError.InnerError)exn.ServiceError.InnerError).ApplicationId);
        }

        [Theory(DisplayName = "When the required operation is allowed for this application, then PartnerName set as FriendlyName."), AutoMoqData]
        public async Task When_OperationAllowed_Then_SetPartnerNameAsFriendlyName(
            AuthenticatedPrincipal principal,
            SessionProperties sessionProperties,
            OperationAccessPermission operationAccessPermission,
            Mock<IAuthenticationProvider> provider,
            Mock<IOperationAccessProvider> operationAccessProvider)
        {
            provider.Setup(m => m.Enabled).Returns(true);

            operationAccessPermission.AllowedOperations = new[] { principal.OperationName };

            operationAccessProvider.Setup(m => m.GetAccessPermissions(principal.ApplicationId)).Returns(operationAccessPermission);

            var filter = new AuthorizationFilter(principal, sessionProperties, new[] { provider.Object }, operationAccessProvider.Object);

            await filter.ExecuteActionFilterAsync(null, CancellationToken.None, this.continuation).ConfigureAwait(false);

            Assert.Equal(operationAccessPermission.FriendlyName, sessionProperties.PartnerName);

            Assert.Equal(principal.UserId, sessionProperties.User);
        }

        private Task AssertNotAuthenticated(AuthenticatedPrincipal principal, Mock<IAuthenticationProvider> provider)
        {
            var fixture = new Fixture().EnableAutoMoq();

            var sessionProperties = fixture.Create<SessionProperties>();

            var operationAccessProvider = new Mock<IOperationAccessProvider>();

            provider.Setup(m => m.Enabled).Returns(true);

            var filter = new AuthorizationFilter(principal, sessionProperties, new[] { provider.Object }, operationAccessProvider.Object);

            return Assert.ThrowsAsync<NotAuthenticatedError>(() => 
                filter.ExecuteActionFilterAsync(null, CancellationToken.None, this.continuation));
        }

        private async Task AssertAuthenticated(AuthenticatedPrincipal principal, IEnumerable<IAuthenticationProvider> providers)
        {
            var fixture = new Fixture().EnableAutoMoq();

            var sessionProperties = fixture.Create<SessionProperties>();

            var operationAccessPermission =
                fixture
                .Build<OperationAccessPermission>()
                .With(m => m.AllowedOperations, new[] { principal.OperationName })
                .Create();

            var operationAccessProvider = new Mock<IOperationAccessProvider>();
            operationAccessProvider.Setup(m => m.GetAccessPermissions(principal.ApplicationId)).Returns(operationAccessPermission);

            var continuationCalled = false;
            var filter = new AuthorizationFilter(principal, sessionProperties, providers, operationAccessProvider.Object);

            await filter
                .ExecuteActionFilterAsync(null, CancellationToken.None, () => { continuationCalled = true; return this.continuation(); })
                .ConfigureAwait(false);

            Assert.True(continuationCalled);
        }
    }
}
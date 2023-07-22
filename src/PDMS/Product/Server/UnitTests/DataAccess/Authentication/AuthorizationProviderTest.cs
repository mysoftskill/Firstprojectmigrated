namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Authentication.UnitTest
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.ActiveDirectory;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authorization;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.UnitTest;
    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;

    public class AuthorizationProviderTest
    {
        [Theory(DisplayName = "Verify the code passes if the static roles are set for the user.")]
        [InlineValidData(AuthorizationRole.ServiceAdmin, AuthorizationRole.ServiceAdmin)]
        [InlineValidData(AuthorizationRole.ServiceAdmin, AuthorizationRole.ServiceEditor)]
        [InlineValidData(AuthorizationRole.VariantEditor, AuthorizationRole.VariantEditor)]
        [InlineValidData(AuthorizationRole.VariantEditor, AuthorizationRole.VariantEditor | AuthorizationRole.ApplicationAccess)]
        [InlineValidData(AuthorizationRole.ServiceAdmin, AuthorizationRole.ServiceEditor | AuthorizationRole.VariantEditor)]
        [InlineValidData(AuthorizationRole.VariantEditor, AuthorizationRole.ServiceEditor | AuthorizationRole.VariantEditor)]
        [InlineValidData(AuthorizationRole.ServiceAdmin | AuthorizationRole.VariantEditor, AuthorizationRole.ServiceEditor | AuthorizationRole.VariantEditor)]
        public async Task VerifyStaticRoleMappingSuccess(AuthorizationRole requiredRole, AuthorizationProvider sut)
        {
            await sut.AuthorizeAsync(requiredRole, null).ConfigureAwait(false);
        }

        [Theory(DisplayName = "Verify the code throws an exception if the static roles are not set for the user.")]
        [InlineValidData(AuthorizationRole.None, AuthorizationRole.None, "None")]
        [InlineValidData(AuthorizationRole.None, AuthorizationRole.ServiceAdmin, "ServiceAdmin")]
        [InlineValidData(AuthorizationRole.None, AuthorizationRole.VariantEditor, "VariantEditor")]
        public async Task VerifyStaticRoleMappingFail(AuthorizationRole requiredRole, string roleValue, AuthorizationProvider sut)
        {
            var exn = await Assert.ThrowsAsync<MissingWritePermissionException>(() => sut.AuthorizeAsync(requiredRole, null)).ConfigureAwait(false);
            Assert.Equal(roleValue, exn.Role);
        }

        [Theory(DisplayName = "When there is no user context, then fail")]
        [ValidData(AuthorizationRole.None)]
        public async Task When_NoUserId_Then_Fail([Frozen]AuthenticatedPrincipal principal, AuthorizationProvider sut)
        {
            principal.UserId = string.Empty;

            var exn = await Assert.ThrowsAsync<MissingWritePermissionException>(() => sut.AuthorizeAsync(AuthorizationRole.NoCachedSecurityGroups, null)).ConfigureAwait(false);

            Assert.Equal("ApplicationAccess", exn.Role);
        }

        [Theory(DisplayName = "When there is no user context and the API supports app access, then pass")]
        [ValidData(AuthorizationRole.None)]
        public async Task When_NoUserIdWithApplicationAccess_Then_Pass([Frozen]AuthenticatedPrincipal principal, AuthorizationProvider sut)
        {
            principal.UserId = string.Empty;

            await sut.AuthorizeAsync(AuthorizationRole.ApplicationAccess | AuthorizationRole.ServiceAdmin, null).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When there is no user context, the AppId is VariantEditorApplicationId and has VariantEditor access, then pass")]
        [ValidData(AuthorizationRole.None, treatAsVariantEditorApplication: true, applicationId: "00000000-0000-0000-0000-000000000001")]
        public async Task When_NoUserIdWithVariantEditorApplicationIdAndVariantEditorRole_Then_Pass(
            [Frozen] AuthenticatedPrincipal principal, 
            AuthorizationProvider sut)
        {
            principal.UserId = string.Empty;
            principal.ApplicationId = "00000000-0000-0000-0000-000000000001";
            await sut.AuthorizeAsync(AuthorizationRole.VariantEditor, null).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When there is no user context, the AppId is VariantEditorApplicationId and has VariantEditor+ServiceEditor access, then pass")]
        [ValidData(AuthorizationRole.None, treatAsVariantEditorApplication: true, applicationId: "00000000-0000-0000-0000-000000000001")]
        public async Task When_NoUserIdWithVariantEditorApplicationIdAndVariantEditorAndServiceEditorRole_Then_Pass(
            [Frozen] AuthenticatedPrincipal principal,
            AuthorizationProvider sut)
        {
            principal.UserId = string.Empty;
            principal.ApplicationId = "00000000-0000-0000-0000-000000000001";
            await sut.AuthorizeAsync(AuthorizationRole.ServiceEditor | AuthorizationRole.VariantEditor, null).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When there is no user context, the AppId is VariantEditorApplicationId and has ApplicationAccess access, then pass")]
        [ValidData(AuthorizationRole.None, treatAsVariantEditorApplication: true, applicationId: "00000000-0000-0000-0000-000000000001")]
        public async Task When_NoUserIdWithVariantEditorApplicationIdAndApplicationAccess_Then_Pass(
            [Frozen] AuthenticatedPrincipal principal,
            AuthorizationProvider sut)
        {
            principal.UserId = string.Empty;
            principal.ApplicationId = "00000000-0000-0000-0000-000000000001";
            await sut.AuthorizeAsync(AuthorizationRole.ApplicationAccess, null).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When there is no user context, the AppId is VariantEditorApplicationId and has ServiceAdmin access, then fail")]
        [ValidData(AuthorizationRole.None, treatAsVariantEditorApplication: true, applicationId: "00000000-0000-0000-0000-000000000001")]
        public async Task When_NoUserIdWithVariantEditorApplicationIdAndServiceAdminRole_Then_Fail(
            [Frozen] AuthenticatedPrincipal principal,
            AuthorizationProvider sut)
        {
            principal.UserId = string.Empty;
            principal.ApplicationId = "00000000-0000-0000-0000-000000000001";

            var exn = await Assert.ThrowsAsync<MissingWritePermissionException>(() => sut.AuthorizeAsync(AuthorizationRole.ServiceAdmin, null)).ConfigureAwait(false);

            Assert.Equal("ApplicationAccess", exn.Role);
        }

        [Theory(DisplayName = "When there is no user context, the AppId is not VariantEditorApplicationId and has VariantEditor access, then fail")]
        [ValidData(AuthorizationRole.None, treatAsVariantEditorApplication: true, applicationId: "00000000-0000-0000-0000-000000000001")]
        public async Task When_NoUserIdWithNonVariantEditorApplicationIdAndVariantEditorRole_Then_Fail(
            [Frozen] AuthenticatedPrincipal principal,
            AuthorizationProvider sut)
        {
            principal.UserId = string.Empty;
            principal.ApplicationId = "00000000-0000-0000-0000-000000000002";
            var exn = await Assert.ThrowsAsync<MissingWritePermissionException>(() => sut.AuthorizeAsync(AuthorizationRole.VariantEditor, null)).ConfigureAwait(false);

            Assert.Equal("ApplicationAccess", exn.Role);
        }

        [Theory(DisplayName = "When there is no user context, the AppId is not VariantEditorApplicationId and has VariantEditor+ServiceEditor access, then fail")]
        [ValidData(AuthorizationRole.None, treatAsVariantEditorApplication: true, applicationId: "00000000-0000-0000-0000-000000000001")]
        public async Task When_NoUserIdWithNonVariantEditorApplicationIdAndVariantEditorAndServiceEditorRole_Then_Fail(
            [Frozen] AuthenticatedPrincipal principal,
            AuthorizationProvider sut)
        {
            principal.UserId = string.Empty;
            principal.ApplicationId = "00000000-0000-0000-0000-000000000002";
            var exn = await Assert.ThrowsAsync<MissingWritePermissionException>(() => sut.AuthorizeAsync(AuthorizationRole.VariantEditor | AuthorizationRole.ServiceEditor, null)).ConfigureAwait(false);

            Assert.Equal("ApplicationAccess", exn.Role);
        }

        [Theory(DisplayName = "When NoCachedSecurityGroups, then force refresh the cache.")]
        [ValidData(AuthorizationRole.ServiceEditor)]
        public async Task When_NoCachedSecurityGroups_Then_PassAlongCorrectFlag(
            [Frozen] Mock<ICachedActiveDirectory> activeDirectory, 
            AuthorizationProvider sut)
        {
            await sut.AuthorizeAsync(AuthorizationRole.NoCachedSecurityGroups, null).ConfigureAwait(false);

            activeDirectory.VerifySet(m => m.ForceRefreshCache = true, Times.Exactly(2));
        }

        [Theory(DisplayName = "When first call does not have NoCache, but second call does, then refresh the cache.")]
        [ValidData(AuthorizationRole.ServiceAdmin)]
        public async Task When_NoCachedSecurityGroupsOnSecondCall_Then_PassAlongCorrectFlag(
            [Frozen] Mock<ICachedActiveDirectory> activeDirectory,
            AuthorizationProvider sut)
        {
            // This should cache them in the class.
            await sut.AuthorizeAsync(AuthorizationRole.ServiceAdmin, null).ConfigureAwait(false);

            // This should trigger the correct ForceRefreshCache and ignore the class cache.
            await sut.AuthorizeAsync(AuthorizationRole.NoCachedSecurityGroups, null).ConfigureAwait(false);

            // This should use the class cache.
            await sut.AuthorizeAsync(AuthorizationRole.ServiceAdmin, null).ConfigureAwait(false);

            activeDirectory.VerifySet(m => m.ForceRefreshCache = true, Times.Exactly(2));
            activeDirectory.Verify(m => m.GetSecurityGroupIdsAsync(It.IsAny<AuthenticatedPrincipal>()), Times.Exactly(2));
        }

        [Theory(DisplayName = "When authorize is called multiple times, then read security groups only once.")]
        [ValidData(AuthorizationRole.VariantEditor)]
        public async Task When_AuthorizeCalledRepeatedly_Then_ReadSecurityGroupsOnce(
            [Frozen] Mock<ICachedActiveDirectory> activeDirectory,
            AuthorizationProvider sut)
        {
            await sut.AuthorizeAsync(AuthorizationRole.NoCachedSecurityGroups, null).ConfigureAwait(false);
            await sut.AuthorizeAsync(AuthorizationRole.VariantEditor, null).ConfigureAwait(false);

            activeDirectory.Verify(m => m.GetSecurityGroupIdsAsync(It.IsAny<AuthenticatedPrincipal>()), Times.Once);
        }

        [Theory(DisplayName = "When authorize is called multiple times with NoCacheSecurityGroups, then read security groups with cache only once.")]
        [ValidData(AuthorizationRole.VariantEditor)]
        public async Task When_AuthorizeCalledRepeatedlyWithNoCachedSecurityGroups_Then_ReadSecurityGroupsOnce(
            [Frozen] Mock<ICachedActiveDirectory> activeDirectory,
            AuthorizationProvider sut)
        {
            var forceRefresh = false;
            activeDirectory.SetupGet(x => x.ForceRefreshCache).Returns(() => forceRefresh);
            activeDirectory.SetupSet(x => x.ForceRefreshCache = It.IsAny<bool>()).Callback<bool>(x => forceRefresh = x);

            await sut.AuthorizeAsync(AuthorizationRole.NoCachedSecurityGroups, null).ConfigureAwait(false);
            await sut.AuthorizeAsync(AuthorizationRole.NoCachedSecurityGroups, null).ConfigureAwait(false);

            activeDirectory.VerifySet(m => m.ForceRefreshCache = true, Times.Exactly(2));
            activeDirectory.Verify(m => m.GetSecurityGroupIdsAsync(It.IsAny<AuthenticatedPrincipal>()), Times.Once);
        }

        public class ValidDataAttribute : AutoMoqDataAttribute
        {
            public ValidDataAttribute(
                AuthorizationRole roles, 
                bool treatAsVariantEditorApplication = false,
                string applicationId = null) : base(true)
            {
                var securityGroups = this.Fixture.CreateMany<Guid>();

                var treatAsAdmin = roles.HasFlag(AuthorizationRole.ServiceAdmin);
                var treatAsVariantEditor = roles.HasFlag(AuthorizationRole.VariantEditor);

                this.Fixture.RegisterAuthorizationClasses(
                    securityGroups,
                    treatAsAdmin: treatAsAdmin,
                    treatAsVariantEditor: treatAsVariantEditor,
                    treatAsVariantEditorApplication: treatAsVariantEditorApplication,
                    applicationId: applicationId);
            }
        }

        public class InlineValidDataAttribute : InlineAutoMoqDataAttribute
        {
            public InlineValidDataAttribute(AuthorizationRole roles, params object[] values) : base(new ValidDataAttribute(roles), values)
            {
            }
        }
    }
}
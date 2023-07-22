namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Controllers.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    using global::Autofac;
    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.ActiveDirectory;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Reader;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Writer;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.UnitTests;
    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.Identity;
    using Microsoft.PrivacyServices.Testing;
    using Moq;
    using Newtonsoft.Json;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;

    using Core = Microsoft.PrivacyServices.DataManagement.Models.V2;

    public class UsersControllerTest
    {
        [Theory(DisplayName = "When ReadById is called, then cached active directory is called.")]
        [AutofixtureCustomizations.InlineTypeCorrections("")]
        public async Task When_ReadByIdWithExpansions_Then_ParseAppropriately(
            string queryParameters,
            Mock<AuthenticatedPrincipal> authenticatedPrincipal,
            Mock<ICachedActiveDirectory> cachedActiveDirectory)
        {
            // Arrange
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(cachedActiveDirectory.Object).As<ICachedActiveDirectory>();
                cb.RegisterInstance(authenticatedPrincipal.Object).As<AuthenticatedPrincipal>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                // Act
                HttpResponseMessage response =
                    await
                        server.HttpClient.GetAsync(
                            $"api/v2/users('me'){queryParameters}").ConfigureAwait(false);
                var actual = await TestHelper.Deserialize<User>(response).ConfigureAwait(false);

                // Assert
                cachedActiveDirectory.Verify(m => m.GetSecurityGroupIdsAsync(It.IsAny<AuthenticatedPrincipal>()), Times.Once);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.NotNull(actual.SecurityGroups);
                Assert.Null(actual.TrackingDetails);
            }
        }
    }
}
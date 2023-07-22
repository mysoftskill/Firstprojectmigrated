namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Authentication.UnitTest
{
    using System;
    using System.Linq;
    using System.Security.Claims;
    using System.Security.Principal;
    using Autofac;
    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;

    public class AzureActiveDirectoryProviderTest
    {
        private const string ObjectIdClaim = "http://schemas.microsoft.com/identity/claims/objectidentifier";
        private const string microsoftIssuer = "https://sts.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47/";
        private const string ameIssuer = "https://sts.windows.net/33e01921-4d64-4f8c-a055-5bdaffd5e33d/";

        [Theory(DisplayName = "When set principal is called, then assign the proper values."), AutoMoqData]
        public void VerifySetPrincipal(
            Mock<IAzureActiveDirectoryProviderConfig> configuration,
            string appId,
            string userName)
        {
            var claims = new[]
            {
                new Claim("appId", appId),
                new Claim(ObjectIdClaim, userName),
                new Claim(ClaimTypes.Upn, "userName")
            };

            var requestPrincipal = this.CreatePrincipal(claims);

            configuration.Setup(m => m.EnableIntegrationTestOverrides).Returns(false);
            configuration.Setup(m => m.ValidIssuers).Returns(new [] { microsoftIssuer, ameIssuer });

            var principal = new AuthenticatedPrincipal();
            var fixture = new Fixture().EnableAutoMoq();
            var eventFactory = fixture.Freeze<Mock<IEventWriterFactory>>();
            var provider = new AzureActiveDirectoryProvider(configuration.Object, null, eventFactory.Object);

            provider.SetPrincipal(requestPrincipal, principal);

            Assert.Equal(appId, principal.ApplicationId);
            Assert.Equal("a:" + userName, principal.UserId); // Ensure Asimov format.
            Assert.Equal(requestPrincipal, principal.ClaimsPrincipal);
            Assert.Equal("userName", principal.UserAlias);
        }

        [Theory(DisplayName = "When set principal is called and ips is different from iss, then treat as guest."), AutoMoqData]
        public void VerifySetPrincipalIpdDifferentFromIss(
            Mock<IAzureActiveDirectoryProviderConfig> configuration, string userName)
        {
            var principal = new AuthenticatedPrincipal();

            var claims = new[]
            {
                new Claim(ObjectIdClaim, userName),
                new Claim("idp", "value"),
                new Claim("iss", microsoftIssuer)
            };

            var requestPrincipal = this.CreatePrincipal(claims);

            var fixture = new Fixture().EnableAutoMoq();
            var eventFactory = fixture.Freeze<Mock<IEventWriterFactory>>();
            var provider = new AzureActiveDirectoryProvider(configuration.Object, null, eventFactory.Object);

            provider.SetPrincipal(requestPrincipal, principal);

            // Ensure not authenticated.
            Assert.Null(principal.ApplicationId);
            Assert.Null(principal.UserId);
            Assert.Null(principal.ClaimsPrincipal);
        }

        [Theory(DisplayName = "When set principal is called and ips is same as iss, then do not treat as guest."), AutoMoqData]
        public void VerifySetPrincipalIpdSameAsIss(
            Mock<IAzureActiveDirectoryProviderConfig> configuration,
            AuthenticatedPrincipal principal, 
            string userName)
        {
            var claims = new[]
            {
                new Claim(ObjectIdClaim, userName),
                new Claim("idp", microsoftIssuer),
                new Claim("iss", microsoftIssuer),
                new Claim(ClaimTypes.Upn, "userName")
            };

            var requestPrincipal = this.CreatePrincipal(claims);

            configuration.Setup(m => m.ValidIssuers).Returns(new[] { microsoftIssuer, ameIssuer });
            var fixture = new Fixture().EnableAutoMoq();
            var eventFactory = fixture.Freeze<Mock<IEventWriterFactory>>();
            var provider = new AzureActiveDirectoryProvider(configuration.Object, null, eventFactory.Object);
            provider.SetPrincipal(requestPrincipal, principal);

            provider.SetPrincipal(requestPrincipal, principal);

            Assert.Null(principal.ApplicationId);
            Assert.Equal("a:" + userName, principal.UserId);
        }

        [Theory(DisplayName = "When set principal is called and appId is missing, then set application id to null."), AutoMoqData]
        public void VerifySetPrincipalMissingAppId(
            Mock<IAzureActiveDirectoryProviderConfig> configuration, 
            AuthenticatedPrincipal principal, 
            string userName)
        {
            var claims = new[]
            {
                new Claim(ObjectIdClaim, userName),
                new Claim(ClaimTypes.Upn, "userName")
            };

            var requestPrincipal = this.CreatePrincipal(claims);

            configuration.Setup(m => m.ValidIssuers).Returns(new[] { microsoftIssuer, ameIssuer });
            var fixture = new Fixture().EnableAutoMoq();
            var eventFactory = fixture.Freeze<Mock<IEventWriterFactory>>();
            var provider = new AzureActiveDirectoryProvider(configuration.Object, null, eventFactory.Object);

            provider.SetPrincipal(requestPrincipal, principal);

            Assert.Null(principal.ApplicationId);
            Assert.Equal("a:" + userName, principal.UserId);
            Assert.Equal(requestPrincipal, principal.ClaimsPrincipal);
            Assert.Equal("userName", principal.UserAlias);
        }

        [Theory(DisplayName = "When set principal is called and user alias is missing, then set user alias to null."), AutoMoqData]
        public void VerifySetPrincipalMissingUserAlias(
            Mock<IAzureActiveDirectoryProviderConfig> configuration,
            AuthenticatedPrincipal principal, 
            string appId, 
            string userName)
        {
            var claims = new[]
            {
                new Claim("appId", appId),
                new Claim(ObjectIdClaim, userName)
            };

            var requestPrincipal = this.CreatePrincipal(claims);

            configuration.Setup(m => m.EnableIntegrationTestOverrides).Returns(false);
            configuration.Setup(m => m.ValidIssuers).Returns(new[] { microsoftIssuer, ameIssuer });
            var fixture = new Fixture().EnableAutoMoq();
            var eventFactory = fixture.Freeze<Mock<IEventWriterFactory>>();
            var provider = new AzureActiveDirectoryProvider(configuration.Object, null, eventFactory.Object);

            provider.SetPrincipal(requestPrincipal, principal);

            Assert.Equal(appId, principal.ApplicationId);
            Assert.Equal(requestPrincipal, principal.ClaimsPrincipal);
            Assert.Null(principal.UserAlias);
        }

        [Theory(DisplayName = "When set principal is called with an email user alias, then parse out the user portion."), AutoMoqData]
        public void VerifySetPrincipalUserAliasValue(
            Mock<IAzureActiveDirectoryProviderConfig> configuration,
            AuthenticatedPrincipal principal)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Upn, "petersc@microsoft.com")
            };

            var requestPrincipal = this.CreatePrincipal(claims);

            configuration.Setup(m => m.ValidIssuers).Returns(new[] { microsoftIssuer, ameIssuer });
            var fixture = new Fixture().EnableAutoMoq();
            var eventFactory = fixture.Freeze<Mock<IEventWriterFactory>>();
            var provider = new AzureActiveDirectoryProvider(configuration.Object, null, eventFactory.Object);
            provider.SetPrincipal(requestPrincipal, principal);

            provider.SetPrincipal(requestPrincipal, principal);

            Assert.Equal("petersc", principal.UserAlias);
        }

        [Theory(DisplayName = "When set principal is called and username is missing, then set user id to null."), AutoMoqData]
        public void VerifySetPrincipalMissingUserName(
            [Frozen] Mock<IAzureActiveDirectoryProviderConfig> configuration,
            AuthenticatedPrincipal principal, 
            string appId)
        {
            configuration.Setup(m => m.EnableIntegrationTestOverrides).Returns(false);
            configuration.Setup(m => m.ValidIssuers).Returns(new[] { microsoftIssuer, ameIssuer });

            var requestPrincipal = this.CreatePrincipal(new Claim("appId", appId));

            var fixture = new Fixture().EnableAutoMoq();
            var eventFactory = fixture.Freeze<Mock<IEventWriterFactory>>();
            var provider = new AzureActiveDirectoryProvider(configuration.Object, null, eventFactory.Object);

            provider.SetPrincipal(requestPrincipal, principal);

            Assert.Null(principal.UserId);
            Assert.Equal(appId, principal.ApplicationId);
            Assert.Equal(requestPrincipal, principal.ClaimsPrincipal);
        }

        [Theory(DisplayName = "When set principal is called and user name is not set, then assign the proper values.")]
        [InlineAutoMoqData(true, "IntegrationTest")]
        [InlineAutoMoqData(false, null)]
        public void VerifyUserNameOverride(
            bool overrideEnabled,
            string expectedUserName,
            Mock<IAzureActiveDirectoryProviderConfig> configuration)
        {
            configuration.Setup(m => m.EnableIntegrationTestOverrides).Returns(overrideEnabled);
            configuration.Setup(m => m.IntegrationTestUserName).Returns("IntegrationTest");
            configuration.Setup(m => m.ValidIssuers).Returns(new[] { microsoftIssuer, ameIssuer });

            var principal = new AuthenticatedPrincipal();
            var fixture = new Fixture().EnableAutoMoq();
            var eventFactory = fixture.Freeze<Mock<IEventWriterFactory>>();
            var provider = new AzureActiveDirectoryProvider(configuration.Object, null, eventFactory.Object);

            provider.SetPrincipal(this.CreatePrincipal(), principal);

            Assert.Equal(expectedUserName, principal.UserId);
            Assert.Equal(expectedUserName, principal.UserAlias);
        }

        [Theory(DisplayName = "When SetPrincipal is called with null destination, then throw exception."), AutoMoqData]
        public void VerifyNullDestination(Mock<IAzureActiveDirectoryProviderConfig> configuration)
        {
            var fixture = new Fixture().EnableAutoMoq();
            var eventFactory = fixture.Freeze<Mock<IEventWriterFactory>>();
            var provider = new AzureActiveDirectoryProvider(configuration.Object, null, eventFactory.Object);

            Assert.Throws<ArgumentNullException>(() => provider.SetPrincipal(null, null));
        }

        [Theory(DisplayName = "When SetPrincipal is called with null source, then do nothing."), AutoMoqData]
        public void VerifyNullSource(Mock<IAzureActiveDirectoryProviderConfig> configuration, AuthenticatedPrincipal destination)
        {
            var initialPrincipal = destination.ClaimsPrincipal;

            var fixture = new Fixture().EnableAutoMoq();
            var eventFactory = fixture.Freeze<Mock<IEventWriterFactory>>();
            var provider = new AzureActiveDirectoryProvider(configuration.Object, null, eventFactory.Object);

            provider.SetPrincipal(null, destination);

            Assert.Equal(initialPrincipal, destination.ClaimsPrincipal);
        }

        [Theory(DisplayName = "When SetPrincipal is called with unknown source type, then do nothing."), AutoMoqData]
        public void VerifyUnknownSource(Mock<IAzureActiveDirectoryProviderConfig> configuration, AuthenticatedPrincipal destination, IPrincipal principal)
        {
            var initialPrincipal = destination.ClaimsPrincipal;

            var fixture = new Fixture().EnableAutoMoq();
            var eventFactory = fixture.Freeze<Mock<IEventWriterFactory>>();
            var provider = new AzureActiveDirectoryProvider(configuration.Object, null, eventFactory.Object);

            provider.SetPrincipal(principal, destination);

            Assert.Equal(initialPrincipal, destination.ClaimsPrincipal);
        }

        [Theory(DisplayName = "When principal is for the wrong authentication method, then do not set app id."), AutoMoqData]
        public void VerifyWrongAuthenticationType(Mock<IAzureActiveDirectoryProviderConfig> configuration, AuthenticatedPrincipal destination)
        {
            var initialApplicationId = destination.ApplicationId;

            // Arrange.
            var identity = new ClaimsIdentity(new Claim[0]);
            var principal = new ClaimsPrincipal(identity);

            var fixture = new Fixture().EnableAutoMoq();
            var eventFactory = fixture.Freeze<Mock<IEventWriterFactory>>();
            var provider = new AzureActiveDirectoryProvider(configuration.Object, null, eventFactory.Object);

            // Act.
            provider.SetPrincipal(principal, destination);

            // Assert.
            Assert.Equal(initialApplicationId, destination.ApplicationId);
        }

        private ClaimsPrincipal CreatePrincipal(params Claim[] claims)
        {
            claims = claims ?? new Claim[0];

            claims = claims.Concat(new[]
            {
                new Claim("http://schemas.microsoft.com/identity/claims/tenantid", "tenantid"),
                new Claim("iss", microsoftIssuer),
                new Claim("oid", Guid.NewGuid().ToString()),
            }).ToArray();

            var requestPrincipal = new ClaimsPrincipal();
            requestPrincipal.AddIdentity(new ClaimsIdentity(claims));

            return requestPrincipal;
        }
    }
}

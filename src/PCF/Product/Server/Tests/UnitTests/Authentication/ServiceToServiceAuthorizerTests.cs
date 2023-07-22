namespace PCF.UnitTests
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor;
    using Microsoft.Windows.Services.AuthN.Server;

    using Moq;

    using Xunit;

    [Trait("Category", "UnitTest")]
    public class ServiceToServiceAuthorizerTests
    {
        [Fact]
        public async void CheckAuthorizedAsyncThrowsIfAgentNotFound()
        {
            var dataAgentMapFactoryMock = new Mock<IDataAgentMapFactory>();
            var dataAgentMapMock = new Mock<IDataAgentMap>();
            var stsAuthenticator = new Mock<IAuthenticator>();

            IDataAgentInfo agentInfo;
            dataAgentMapMock.Setup(x => x.TryGetAgent(It.IsAny<AgentId>(), out agentInfo)).Returns(false);
            dataAgentMapFactoryMock.Setup(x => x.GetDataAgentMap()).Returns(dataAgentMapMock.Object);

            var authorizer = new ServiceAuthorizer(dataAgentMapFactoryMock.Object, stsAuthenticator.Object);

            bool isException = false;

            try
            {
                await authorizer.CheckAuthorizedAsync(new HttpRequestMessage(), new AgentId(Guid.NewGuid()));
            }
            catch (AuthNException)
            {
                isException = true;
            }

            Assert.True(isException);
        }

        [Fact]
        public async void CheckAuthorizedAsyncThrowsIfAuthenticatorThrows()
        {
            var dataAgentMapFactoryMock = new Mock<IDataAgentMapFactory>();
            var authenticator = new Mock<IAuthenticator>();

            var request = new HttpRequestMessage();

            authenticator.Setup(x => x.AuthenticateAsync(It.IsAny<HttpRequestHeaders>(), It.IsAny<X509Certificate2>())).Throws(new AuthNException(AuthNErrorCode.InvalidTicket));
            var authorizer = new ServiceAuthorizer(dataAgentMapFactoryMock.Object, authenticator.Object);

            bool isException = false;

            try
            {
                await authorizer.CheckAuthorizedAsync(request, AuthenticationScope.TestHooks);
            }
            catch (AuthNException)
            {
                isException = true;
            }

            Assert.True(isException);

            authenticator.Verify(x => x.AuthenticateAsync(It.IsAny<HttpRequestHeaders>(), It.IsAny<X509Certificate2>()), Times.Once);
        }

        [Fact]
        public async void CheckAuthorizedAsyncThrowsIfNoAuthenticatedIds()
        {
            var dataAgentMapFactoryMock = new Mock<IDataAgentMapFactory>();
            var dataAgentMapMock = new Mock<IDataAgentMap>();
            var stsAuthenticator = new Mock<IAuthenticator>();
            var agentInfoMock = new Mock<IDataAgentInfo>();
            var agentInfo = agentInfoMock.Object;

            dataAgentMapMock.Setup(x => x.TryGetAgent(It.IsAny<AgentId>(), out agentInfo)).Returns(true);
            dataAgentMapFactoryMock.Setup(x => x.GetDataAgentMap()).Returns(dataAgentMapMock.Object);

            var authContext = new PcfAuthenticationContext();
            stsAuthenticator.Setup(x => x.AuthenticateAsync(It.IsAny<HttpRequestHeaders>(), It.IsAny<X509Certificate2>())).Returns(Task.FromResult(authContext));

            var request = new HttpRequestMessage();
            var authorizer = new ServiceAuthorizer(dataAgentMapFactoryMock.Object, stsAuthenticator.Object);

            var authError = new AuthNException(AuthNErrorCode.None);
            try
            {
                await authorizer.CheckAuthorizedAsync(request, new AgentId(Guid.NewGuid()));
            }
            catch (AuthNException ex)
            {
                authError = ex;
            }

            Assert.Equal(AuthNErrorCode.InvalidTicket, authError.ErrorCode);
            Assert.Equal("Invalid authorization scheme", authError.Message);

            authError = new AuthNException(AuthNErrorCode.None);
            try
            {
                await authorizer.CheckAuthorizedAsync(request, AuthenticationScope.TestHooks);
            }
            catch (AuthNException ex)
            {
                authError = ex;
            }

            Assert.Equal(AuthNErrorCode.InvalidTicket, authError.ErrorCode);
            Assert.Equal("Invalid authorization scheme", authError.Message);
        }

        [Fact]
        public async void CheckAuthorizedAsyncThrowsIfPartnerIdDoesNotMatch()
        {
            var dataAgentMapFactoryMock = new Mock<IDataAgentMapFactory>();
            var stsAuthenticator = new Mock<IAuthenticator>();

            var request = new HttpRequestMessage();
            var appId = Guid.NewGuid();
            var siteId = 54321L;

            stsAuthenticator.Setup(x => x.AuthenticateAsync(It.IsAny<HttpRequestHeaders>(), It.IsAny<X509Certificate2>()))
                .Returns(Task.FromResult(new PcfAuthenticationContext { AuthenticatedAadAppId = appId, AuthenticatedMsaSiteId = siteId }));
            var authorizer = new ServiceAuthorizer(dataAgentMapFactoryMock.Object, stsAuthenticator.Object);

            var authError = new AuthNException(AuthNErrorCode.None);
            try
            {
                await authorizer.CheckAuthorizedAsync(request, AuthenticationScope.TestHooks);
            }
            catch (AuthNException ex)
            {
                authError = ex;
            }

            Assert.Equal(AuthNErrorCode.InvalidTicket, authError.ErrorCode);
            Assert.Contains("AuthenticatedIdMismatch", authError.Message);
        }

        [Fact]
        public async void CheckAuthorizedAsyncThrowsIfAgentIdDoesNotMatch()
        {
            var dataAgentMapFactoryMock = new Mock<IDataAgentMapFactory>();
            var dataAgentMapMock = new Mock<IDataAgentMap>();
            var agentInfoMock = new Mock<IDataAgentInfo>();
            var stsAuthenticator = new Mock<IAuthenticator>();

            agentInfoMock.Setup(x => x.MatchesAadAppId(It.IsAny<Guid>())).Returns(false);
            agentInfoMock.Setup(x => x.MatchesMsaSiteId(It.IsAny<long>())).Returns(false);
            var agentInfo = agentInfoMock.Object;
            dataAgentMapMock.Setup(x => x.TryGetAgent(It.IsAny<AgentId>(), out agentInfo)).Returns(true);
            dataAgentMapFactoryMock.Setup(x => x.GetDataAgentMap()).Returns(dataAgentMapMock.Object);

            var request = new HttpRequestMessage();
            var appId = Guid.NewGuid();

            stsAuthenticator.Setup(x => x.AuthenticateAsync(It.IsAny<HttpRequestHeaders>(), It.IsAny<X509Certificate2>()))
                .Returns(Task.FromResult(new PcfAuthenticationContext { AuthenticatedAadAppId = appId }));
            var authorizer = new ServiceAuthorizer(dataAgentMapFactoryMock.Object, stsAuthenticator.Object);


            var authError = new AuthNException(AuthNErrorCode.None);
            try
            {
                await authorizer.CheckAuthorizedAsync(request, new AgentId(Guid.NewGuid()));
            }
            catch (AuthNException ex)
            {
                authError = ex;
            }

            Assert.Equal(AuthNErrorCode.InvalidTicket, authError.ErrorCode);
            Assert.Contains("AuthenticatedIdMismatch", authError.Message);
        }

        [Fact]
        public async void CheckAuthorizedAsyncSucceedsIfPartnerAppIdIsKnown()
        {
            var dataAgentMapFactoryMock = new Mock<IDataAgentMapFactory>();
            var stsAuthenticator = new Mock<IAuthenticator>();

            var request = new HttpRequestMessage();
            var appId = new Guid("7819dd7c-2f73-4787-9557-0e342743f34b");

            stsAuthenticator.Setup(x => x.AuthenticateAsync(It.IsAny<HttpRequestHeaders>(), It.IsAny<X509Certificate2>())).Returns(Task.FromResult(new PcfAuthenticationContext { AuthenticatedAadAppId = appId }));
            var authorizer = new ServiceAuthorizer(dataAgentMapFactoryMock.Object, stsAuthenticator.Object);
            
            var authContext = await authorizer.CheckAuthorizedAsync(request, AuthenticationScope.TestHooks);

            stsAuthenticator.Verify(x => x.AuthenticateAsync(It.IsAny<HttpRequestHeaders>(), It.IsAny<X509Certificate2>()), Times.Once);
            Assert.Equal(appId, authContext.AuthenticatedAadAppId);
        }

        [Fact]
        public async void CheckAuthorizedAsyncSucceedsIfPartnerSiteIdIsKnown()
        {
            var dataAgentMapFactoryMock = new Mock<IDataAgentMapFactory>();
            var stsAuthenticator = new Mock<IAuthenticator>();

            var request = new HttpRequestMessage();
            long siteId = 296170L;

            stsAuthenticator.Setup(x => x.AuthenticateAsync(It.IsAny<HttpRequestHeaders>(), It.IsAny<X509Certificate2>())).Returns(Task.FromResult(new PcfAuthenticationContext { AuthenticatedMsaSiteId = siteId }));
            var authorizer = new ServiceAuthorizer(dataAgentMapFactoryMock.Object, stsAuthenticator.Object);

            var authContext = await authorizer.CheckAuthorizedAsync(request, AuthenticationScope.TestHooks);

            stsAuthenticator.Verify(x => x.AuthenticateAsync(It.IsAny<HttpRequestHeaders>(), It.IsAny<X509Certificate2>()), Times.Once);
            Assert.Equal(siteId, authContext.AuthenticatedMsaSiteId);
        }

        [Fact]
        public async void CheckAuthorizedAsyncSucceedsIfAgentAppIdIsKnown()
        {
            var dataAgentMapFactoryMock = new Mock<IDataAgentMapFactory>();
            var dataAgentMapMock = new Mock<IDataAgentMap>();
            var agentInfoMock = new Mock<IDataAgentInfo>();
            var stsAuthenticator = new Mock<IAuthenticator>();

            agentInfoMock.Setup(x => x.MatchesAadAppId(It.IsAny<Guid>())).Returns(true);
            var agentInfo = agentInfoMock.Object;
            dataAgentMapMock.Setup(x => x.TryGetAgent(It.IsAny<AgentId>(), out agentInfo)).Returns(true);
            dataAgentMapFactoryMock.Setup(x => x.GetDataAgentMap()).Returns(dataAgentMapMock.Object);

            var request = new HttpRequestMessage();

            var appId = Guid.NewGuid();

            stsAuthenticator.Setup(x => x.AuthenticateAsync(It.IsAny<HttpRequestHeaders>(), It.IsAny<X509Certificate2>())).Returns(Task.FromResult(new PcfAuthenticationContext { AuthenticatedAadAppId = appId }));
            var authorizer = new ServiceAuthorizer(dataAgentMapFactoryMock.Object, stsAuthenticator.Object);

            var authContext = await authorizer.CheckAuthorizedAsync(request, new AgentId(Guid.NewGuid()));

            stsAuthenticator.Verify(x => x.AuthenticateAsync(It.IsAny<HttpRequestHeaders>(), It.IsAny<X509Certificate2>()), Times.Once);
            Assert.Equal(appId, authContext.AuthenticatedAadAppId);
        }

        [Fact]
        public async void CheckAuthorizedAsyncSucceedsIfAgentSiteIdIsKnown()
        {
            var dataAgentMapFactoryMock = new Mock<IDataAgentMapFactory>();
            var dataAgentMapMock = new Mock<IDataAgentMap>();
            var agentInfoMock = new Mock<IDataAgentInfo>();
            var stsAuthenticator = new Mock<IAuthenticator>();

            agentInfoMock.Setup(x => x.MatchesMsaSiteId(It.IsAny<long>())).Returns(true);
            var agentInfo = agentInfoMock.Object;
            dataAgentMapMock.Setup(x => x.TryGetAgent(It.IsAny<AgentId>(), out agentInfo)).Returns(true);
            dataAgentMapFactoryMock.Setup(x => x.GetDataAgentMap()).Returns(dataAgentMapMock.Object);

            var request = new HttpRequestMessage();

            long siteId = 12345L;

            stsAuthenticator.Setup(x => x.AuthenticateAsync(It.IsAny<HttpRequestHeaders>(), It.IsAny<X509Certificate2>())).Returns(Task.FromResult(new PcfAuthenticationContext { AuthenticatedMsaSiteId = siteId }));
            var authorizer = new ServiceAuthorizer(dataAgentMapFactoryMock.Object, stsAuthenticator.Object);

            var authContext = await authorizer.CheckAuthorizedAsync(request, new AgentId(Guid.NewGuid()));

            stsAuthenticator.Verify(x => x.AuthenticateAsync(It.IsAny<HttpRequestHeaders>(), It.IsAny<X509Certificate2>()), Times.Once);
            Assert.Equal(siteId, authContext.AuthenticatedMsaSiteId);
        }
    }
}

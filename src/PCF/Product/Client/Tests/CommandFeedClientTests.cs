using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.PrivacyServices.CommandFeed.Client.Test
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Predicates;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Validator;
    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;
    using Microsoft.PrivacyServices.Policy;

    using Moq;

    using Newtonsoft.Json;

    [TestClass]
    public class CommandFeedClientTests
    {
        [TestMethod]
        public async Task GetCommandsAsyncDoesNotCallValidatorInPpeIfNoVerifier()
        {
            var validationServiceMock = new Mock<IValidationService>(MockBehavior.Strict);
            validationServiceMock.Setup(service => service.SovereignCloudConfigurations).Returns(new List<KeyDiscoveryConfiguration>());

            CommandFeedClient client = this.GetCommandFeedClient(string.Empty, null, CommandFeedEndpointConfiguration.Preproduction);

            client.ValidationService = validationServiceMock.Object;
            var result = await client.GetCommandsAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public async Task GetCommandsAsyncCallsValidatorInPpeIfVerifierExists()
        {
            const string verifier = "verifier";
            
            var validationServiceMock = new Mock<IValidationService>(MockBehavior.Strict);
            validationServiceMock.Setup(service => service.EnsureValidAsync(verifier, It.IsAny<CommandClaims>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            validationServiceMock.Setup(service => service.SovereignCloudConfigurations).Returns(new List<KeyDiscoveryConfiguration>());

            CommandFeedClient client = this.GetCommandFeedClient(verifier, verifier, CommandFeedEndpointConfiguration.Preproduction);

            client.ValidationService = validationServiceMock.Object;
            var result = await client.GetCommandsAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public async Task GetCommandsAsyncCallsValidator()
        {
            const string verifier = "verifier";
            var validationServiceMock = new Mock<IValidationService>(MockBehavior.Strict);
            validationServiceMock.Setup(service => service.EnsureValidAsync(verifier, It.IsAny<CommandClaims>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            validationServiceMock.Setup(service => service.SovereignCloudConfigurations).Returns(new List<KeyDiscoveryConfiguration>());

            CommandFeedClient client = this.GetCommandFeedClient(verifier, verifier, CommandFeedEndpointConfiguration.Production);

            client.ValidationService = validationServiceMock.Object;
            var result = await client.GetCommandsAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public async Task GetCommandsAsyncDoesNotAddInvalidCommandToList()
        {
            const string verifier1 = "verifier1";
            const string verifier2 = "verifier2";
            var validationServiceMock = new Mock<IValidationService>(MockBehavior.Strict);

            validationServiceMock.Setup(service => service.EnsureValidAsync(verifier1, It.IsAny<CommandClaims>(), It.IsAny<CancellationToken>())).Throws<InvalidPrivacyCommandException>();
            validationServiceMock.Setup(service => service.EnsureValidAsync(verifier2, It.IsAny<CommandClaims>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            validationServiceMock.Setup(service => service.SovereignCloudConfigurations).Returns(new List<KeyDiscoveryConfiguration>());

            CommandFeedClient client = this.GetCommandFeedClient(verifier1, verifier2, CommandFeedEndpointConfiguration.Production);

            client.ValidationService = validationServiceMock.Object;
            var result = await client.GetCommandsAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.AreEqual(1, result.Count);
        }

        [TestMethod]
        public async Task GetCommandsAsyncDoesNotAddCommandToListIfValidatorThrowsSecurityTokenRetrievalException()
        {
            const string verifier1 = "verifier1";
            const string verifier2 = "verifier2";

            var validationServiceMock = new Mock<IValidationService>(MockBehavior.Strict);
            validationServiceMock.Setup(service => service.EnsureValidAsync(verifier1, It.IsAny<CommandClaims>(), It.IsAny<CancellationToken>())).Throws<KeyDiscoveryException>();
            validationServiceMock.Setup(service => service.EnsureValidAsync(verifier2, It.IsAny<CommandClaims>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            validationServiceMock.Setup(service => service.SovereignCloudConfigurations).Returns(new List<KeyDiscoveryConfiguration>());

            CommandFeedClient client = this.GetCommandFeedClient(verifier1, verifier2, CommandFeedEndpointConfiguration.Production);

            client.ValidationService = validationServiceMock.Object;
            var result = await client.GetCommandsAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.AreEqual(1, result.Count);
        }

        [TestMethod]
        public async Task GetCommandsAsyncRequestHeadersAreSet()
        {
            var authClientMock = new Mock<IAuthClient>();
            authClientMock.Setup(authClient => authClient.GetAccessTokenAsync()).ReturnsAsync("token");
            authClientMock.SetupGet(authClient => authClient.Scheme).Returns("Bearer");

            HttpRequestMessage requestMessage = null;
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(new GetCommandsResponse()))
            };

            var httpClientMock = new Mock<IHttpClient>();
            httpClientMock.Setup(httpClient => httpClient.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .Callback<HttpRequestMessage, CancellationToken>((rq, tk) => requestMessage = rq)
                .ReturnsAsync(httpResponse);

            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(factory => factory.CreateHttpClient(null)).Returns(httpClientMock.Object);
            var requestedLeaseDuration = TimeSpan.FromSeconds(3600);
            var client = new CommandFeedClient(
                Guid.NewGuid(),
                authClientMock.Object,
                new ConsoleCommandFeedLogger(),
                factory: httpClientFactoryMock.Object,
                endpointConfiguration: CommandFeedEndpointConfiguration.Preproduction)
            {
                RequestedLeaseDuration = requestedLeaseDuration
            };

            await client.GetCommandsAsync(CancellationToken.None);
            httpClientMock.Verify(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once());

            var headers = requestMessage.Headers;
            Assert.IsNotNull(headers);
            var clientVersion = headers.GetValues("x-client-version").First();
            var supportedCommands = headers.GetValues("x-supported-commands").First();
            var leaseTimeStr = headers.GetValues("x-lease-duration-seconds").First();

            Assert.IsTrue(clientVersion.StartsWith("pcfsdk;"));
            Assert.AreEqual(supportedCommands, "AccountClose,Delete,Export");
            Assert.AreEqual(leaseTimeStr, requestedLeaseDuration.TotalSeconds.ToString(CultureInfo.InvariantCulture));
        }

        private CommandFeedClient GetCommandFeedClient(string verifier1, string verifier2, CommandFeedEndpointConfiguration configuration)
        {
            var commandResponse = new GetCommandsResponse
            {
                this.GetCommand(verifier1, "command1"),
                this.GetCommand(verifier2, "command2")
            };

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonConvert.SerializeObject(commandResponse)) };

            var authClientMock = new Mock<IAuthClient>();
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            var httpClientMock = new Mock<IHttpClient>();

            authClientMock.Setup(authClient => authClient.GetAccessTokenAsync()).ReturnsAsync("token");
            authClientMock.SetupGet(authClient => authClient.Scheme).Returns("Bearer");

            httpClientFactoryMock.Setup(factory => factory.CreateHttpClient(null)).Returns(httpClientMock.Object);
            httpClientMock.Setup(httpClient => httpClient.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(httpResponse);

            return new CommandFeedClient(
                Guid.NewGuid(),
                authClientMock.Object,
                new ConsoleCommandFeedLogger(),
                factory: httpClientFactoryMock.Object,
                endpointConfiguration: configuration);
        }

        private PrivacyCommand GetCommand(string verifier, string commandId)
        {
            return new DeleteCommand(
                commandId,
                "assetGroup1",
                "assetGroupQ1",
                verifier,
                "cv1",
                "lr1",
                DateTime.UtcNow.AddMinutes(5),
                DateTime.UtcNow,
                new MsaSubject
                {
                    Puid = 12345,
                    Anid = "12345",
                    Cid = 12345,
                    Opid = "12345",
                    Xuid = "12345",
                },
                "state",
                new BrowsingHistoryPredicate(),
                Policies.Current.DataTypes.Ids.BrowsingHistory,
                new TimeRangePredicate
                {
                    StartTime = DateTime.UtcNow.AddDays(30),
                    EndTime = DateTimeOffset.UtcNow,
                },
                null,
                Policies.Current.CloudInstances.Ids.Public.Value);
        }
    }
}

namespace Microsoft.PrivacyServices.CommandFeed.Client.Test
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    [TestClass]
    public class ReplayCommandsClientTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task ReplayCommandsByIds_NullCollection_ThrowsException()
        {
            CommandFeedClient client = this.GetClient();
            await client.ReplayCommandsByIdAsync(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task ReplayCommandsByIds_EmptyCollection_ThrowsException()
        {
            CommandFeedClient client = this.GetClient();
            await client.ReplayCommandsByIdAsync(new string[0]);
        }


        [TestMethod]
        [ExpectedException(typeof(HttpRequestException))]
        public async Task ReplayCommandsByIds_BadRequest_ThrowsException()
        {
            CommandFeedClient client = this.GetClient(new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("Errors") });
            await client.ReplayCommandsByIdAsync(new string[] { Guid.NewGuid().ToString() });
        }

        [TestMethod]
        public async Task ReplayCommands_ReturnEmpty()
        {
            CommandFeedClient client = this.GetClient(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) });
            await client.ReplayCommandsByIdAsync(new string[] { Guid.NewGuid().ToString() }, new string[] { "assetgroup1" });
            await client.ReplayCommandsByDatesAsync(DateTimeOffset.UtcNow.AddDays(-10), DateTimeOffset.UtcNow.AddDays(-1));
        }

        private CommandFeedClient GetClient(HttpResponseMessage httpResponse = null)
        {
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
                factory: httpClientFactoryMock.Object);
        }
    }
}

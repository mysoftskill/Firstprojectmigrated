namespace Microsoft.PrivacyServices.CommandFeed.Client.Test
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.CommandFeed.Client.Commands;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Newtonsoft.Json;

    [TestClass]
    public class BatchCompleteClientTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task BatchComplete_NullCollection_ReturnsEmpty()
        {
            CommandFeedClient client = this.GetClient();
            await client.BatchCheckpointCompleteAsync(null);
        }

        [TestMethod]
        public async Task BatchComplete_EmptyCollection_ReturnsTrue()
        {
            CommandFeedClient client = this.GetClient();
            await client.BatchCheckpointCompleteAsync(new ProcessedCommand[0]);
        }

        [TestMethod]
        public async Task BatchComplete_SuccessfullyComplete_ReturnsEmpty()
        {
            CommandFeedClient client = this.GetClient(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) });
            await client.BatchCheckpointCompleteAsync(new[] { this.GetProcessedCommand(), this.GetProcessedCommand() });
        }

        [TestMethod]
        [ExpectedException(typeof(HttpRequestException))]
        public async Task BatchComplete_BadRequest_ThrowsException()
        {
            var errorCheckpoint = new CheckpointCompleteResponse
            {
                CommandId = Guid.NewGuid().ToString(),
                Error = "ErrorMessage"
            };

            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(JsonConvert.SerializeObject(new[] { errorCheckpoint }), Encoding.UTF8, "application/json")
            };

            CommandFeedClient client = this.GetClient(response);
            await client.BatchCheckpointCompleteAsync(new[] { this.GetProcessedCommand() });
        }

        [TestMethod]
        [ExpectedException(typeof(HttpRequestException))]
        public async Task BatchComplete_BadRequest_ErrorMessage_ThrowsException()
        {
            CommandFeedClient client = this.GetClient(new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent(string.Empty) });
            await client.BatchCheckpointCompleteAsync(new[] { this.GetProcessedCommand() });
        }

        [TestMethod]
        [ExpectedException(typeof(JsonSerializationException))]
        public async Task BatchComplete_BadRequest_NullCommandId_ThrowsSerializationException()
        {
            CommandFeedClient client = this.GetClient(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) });
            await client.BatchCheckpointCompleteAsync(new[] { new ProcessedCommand(null, null, 0) });
        }

        [TestMethod]
        [ExpectedException(typeof(HttpRequestException))]
        public async Task BatchComplete_InternalServerError_ErrorMessage_ThrowsException()
        {
            CommandFeedClient client = this.GetClient(new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent(string.Empty) });
            await client.BatchCheckpointCompleteAsync(new[] { this.GetProcessedCommand() });
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

        private ProcessedCommand GetProcessedCommand(string id = null, string error = null)
        {
            id = id ?? Guid.NewGuid().ToString();

            return new ProcessedCommand(
                id,
                "receipt",
                10)
            {
                Error = error
            };
        }
    }
}

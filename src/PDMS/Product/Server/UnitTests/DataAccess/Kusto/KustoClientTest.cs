namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Kusto.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.DataManagement.Client;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;

    using Moq;
    using Moq.Protected;

    using Newtonsoft.Json;

    using Xunit;

    /// <summary>
    ///     Client tests.
    /// </summary>
    public class KustoClientTest
    {
        private readonly Mock<HttpMessageHandler> handlerMock;

        private readonly HttpClient mockHttpClient;

        private readonly Mock<KustoClient> mockKustoClient;

        private readonly Mock<IKustoClientConfig> mockKustoClientConfig;

        /// <summary>
        ///     Initializes a new instance of the <see cref="KustoClientTest" /> class.
        /// </summary>
        public KustoClientTest()
        {
            this.handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            // Real http client with mocked handler
            this.mockHttpClient = new HttpClient(this.handlerMock.Object)
            {
                BaseAddress = new Uri("https://kustocluster.kusto.windows.net")
            };

            this.mockKustoClientConfig = new Mock<IKustoClientConfig>();
            this.mockKustoClientConfig.Setup(a => a.Authority).Returns("https://someauthority.com/microsoft.com");
            this.mockKustoClientConfig.Setup(a => a.ClientId).Returns("489D9BA9-10AA-4125-B417-E4F31F4BC9C6");
            this.mockKustoClientConfig.Setup(a => a.KustoCluster).Returns("KustoCluster");
            this.mockKustoClientConfig.Setup(a => a.KustoDatabase).Returns("KustoDatabase");
            this.mockKustoClientConfig.Setup(a => a.KustoFunctionPendingCommands).Returns("KustoFunctionName");
            this.mockKustoClientConfig.Setup(a => a.KeyVaultCertificateName).Returns("KeyVaultCertificateName");

            var mockConfidentialCredential = new Mock<ConfidentialCredential>(MockBehavior.Loose);

            this.mockKustoClient = new Mock<KustoClient>(this.mockKustoClientConfig.Object, this.mockHttpClient, mockConfidentialCredential.Object);
            this.mockKustoClient.CallBase = true;
            this.mockKustoClient.Setup(a => a.GetS2SAccessToken()).Returns(Task.FromResult("abcd"));
        }

        /// <summary>
        ///     Test Query Success.
        /// </summary>
        /// <returns>A task that performs the checks.</returns>
        [Fact]
        public async Task ShouldSucceedUponValidKustoResponseAsync()
        {
            // Return valid kusto response
            this.handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(ValidKustoResponse()))
                .Verifiable();

            var result = await this.mockKustoClient.Object.QueryAsync("whatever").ConfigureAwait(false);

            var kustoResponse = result.Response;

            Assert.NotNull(kustoResponse.Rows);

            this.ExpectedCalls("https://kustocluster.kusto.windows.net/v2/rest/query", 1);
        }

        /// <summary>
        ///     Test Query Failure.
        /// </summary>
        /// <returns>A task that performs the checks.</returns>
        [Fact]
        public async Task ShouldThrowExceptionUponInvalidKustoResponseAsync()
        {
            // Return invalid kusto response
            this.handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>> (
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage> (),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(InvalidKustoResponse()))
                .Verifiable();

            var expectedUri = new Uri("https://kustocluster.kusto.windows.net/v2/rest/query");

            CallerError ex = await Assert.ThrowsAsync<CallerError>(() => this.mockKustoClient.Object.QueryAsync("whatever")).ConfigureAwait(false);

            Assert.NotNull(ex);
            Assert.NotNull(ex.ResponseError);
            Assert.NotNull(ex.Message);

            this.ExpectedCalls("https://kustocluster.kusto.windows.net/v2/rest/query", 1);
        }

        [Fact]
        public async Task ShouldThrowExceptionUponKustoResponseErrorAsync()
        {
            // Return invalid kusto response
            this.handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(KustoResponseError()))
                .Verifiable();

            var expectedUri = new Uri("https://kustocluster.kusto.windows.net/v2/rest/query");

            CallerError ex = await Assert.ThrowsAsync<CallerError>(() => this.mockKustoClient.Object.QueryAsync("whatever")).ConfigureAwait(false);

            Assert.NotNull(ex);
            Assert.NotNull(ex.ResponseError);
            Assert.NotNull(ex.Message);

            this.ExpectedCalls("https://kustocluster.kusto.windows.net/v2/rest/query", 1);
        }

        private static HttpResponseMessage ValidKustoResponse()
        {
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    "[{\"FrameType\":\"DataTable\",\"TableId\":1,\"TableKind\":\"PrimaryResult\",\"TableName\":\"PrimaryResult\",\"Columns\":[{\"ColumnName\":\"AgentId\",\"ColumnType\":\"string\"}],\"Rows\":[[\"f3d89dc9428e4823a64ca243b459de53\"]]}]")
            };
        }

        private static HttpResponseMessage InvalidKustoResponse()
        {
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[{'id':1,'value':'1'}]")
            };
        }

        private static HttpResponseMessage KustoResponseError()
        {
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\r\n    \"error\": {\r\n        \"code\": \"General_BadRequest\",\r\n        \"message\": \"Request is invalid and cannot be executed.\",\r\n        \"@type\": \"Kusto.Data.Exceptions.KustoBadRequestException\",\r\n        \"@message\": \"Semantic error: 'FnPendingCommandsForAgentTEST('04a4622b-5b51-4815-80a6-a07155df1d84')' has the following semantic error: SEM0260: Unknown function: 'FnPendingCommandsForAgentBAD'..\",\r\n }}")
            };
        }

        private void ExpectedCalls(string expectedUri, int times)
        {
            this.handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(times),
                ItExpr.Is<HttpRequestMessage>(
                    req =>
                        req.Method == HttpMethod.Post
                        && req.RequestUri == new Uri(expectedUri)),
                ItExpr.IsAny<CancellationToken>());
        }

        private HttpContent GetHttpBody(string query)
        {
            var payload = new Dictionary<string, string>
            {
                { "db", "KustoDatabase" },
                { "csl", query }
            };

            return new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
        }
    }
}

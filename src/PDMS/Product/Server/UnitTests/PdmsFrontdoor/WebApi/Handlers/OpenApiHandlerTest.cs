namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.UnitTest
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.Handlers;
    using Microsoft.PrivacyServices.Testing;

    using Xunit;

    public class OpenApiHandlerTest
    {
        [Theory(DisplayName = "When an http request is made to the default openapi api, then always return 200."), AutoMoqData]
        public async Task VerifyNoRedirectForOpenApi(HttpRequestMessage requestMessage)
        {
            requestMessage.RequestUri = new Uri("https://localhost/openapi");
            var messageHandler = new MockOpenApiHandler();

            var response = await messageHandler.SendAsync(requestMessage).ConfigureAwait(false);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType.ToString());
        }

        /// <summary>
        /// A mock class to inject a response message.
        /// </summary>
        public class MockOpenApiHandler : OpenApiHandler
        {
            public MockOpenApiHandler()
                : base()
            {
                this.BaseSendAsync = (request, token) => Task.FromException<HttpResponseMessage>(new InvalidOperationException());
            }

            public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
            {
                return base.SendAsync(request, CancellationToken.None);
            }
        }
    }
}
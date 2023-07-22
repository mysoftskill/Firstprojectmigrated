namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.UnitTest
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.Handlers;
    using Microsoft.PrivacyServices.Testing;

    using Ploeh.AutoFixture.Xunit2;
    using Xunit;

    public class HstsHandlerTest
    {
        [Theory(DisplayName = "When an http request is made, redirect to https."), AutoMoqData]
        public async Task VerifyHttpRedirect(
            HttpRequestMessage requestMessage,
            MockHstsHandler messageHandler)
        {
            requestMessage.RequestUri = new Uri("http://" + "localhost");

            var response = await messageHandler.SendAsync(requestMessage).ConfigureAwait(false);

            Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        }

        [Theory(DisplayName = "When an https request is made, then add the response header."), AutoMoqData]
        public async Task VerifyHttpsHeader(
            HttpRequestMessage requestMessage,
            MockHstsHandler messageHandler)
        {
            requestMessage.RequestUri = new Uri("https://" + "localhost");

            var response = await messageHandler.SendAsync(requestMessage).ConfigureAwait(false);
            Assert.Collection(response.Headers.GetValues("Strict-Transport-Security"), v => Assert.Equal("max-age=31536000; includeSubdomains", v));
        }

        [Theory(DisplayName = "When an http request is made to the probe api, then do not redirect."), AutoMoqData]
        public async Task VerifyNoRedirectForProbe(
            HttpRequestMessage requestMessage,
            [Frozen] HttpResponseMessage responseMessage,
            MockHstsHandler messageHandler)
        {
            requestMessage.RequestUri = new Uri("https://" + "localhost" + "/probe");

            var response = await messageHandler.SendAsync(requestMessage).ConfigureAwait(false);

            Assert.Equal(responseMessage.StatusCode, response.StatusCode);
        }

        /// <summary>
        /// A mock class to inject a response message.
        /// </summary>
        public class MockHstsHandler : HstsHandler
        {
            public MockHstsHandler(HttpResponseMessage response)
                : base()
            {
                this.BaseSendAsync = (request, token) => Task.Run(() => response);
            }

            public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
            {
                return base.SendAsync(request, CancellationToken.None);
            }
        }
    }
}
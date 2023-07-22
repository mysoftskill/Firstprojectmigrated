namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.UnitTest
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.Handlers;
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Xunit;

    public class ProbeHandlerTest
    {
        [Theory(DisplayName = "When an http request is made to the default probe api, then always return 200."), AutoMoqData]
        public async Task VerifyNoRedirectForProbe(HttpRequestMessage requestMessage)
        {
            requestMessage.RequestUri = new Uri("https://localhost/probe");            
            var messageHandler = new MockProbeHandler(new DefaultProbe());

            var response = await messageHandler.SendAsync(requestMessage).ConfigureAwait(false);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(8 * 1024, response.Content.Headers.ContentLength);
            Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType.ToString());
        }

        [Theory(DisplayName = "When an exception happens in the probe, then do not catch it."), AutoMoqData]
        public async Task VerifyExceptionForProbe(HttpRequestMessage requestMessage)
        {
            requestMessage.RequestUri = new Uri("https://localhost/probe");

            var probe = new Mock<IProbeMonitor>();
            Func<Task> failure = () => Task.FromException(new NotImplementedException());
            probe.Setup(m => m.ProbeAsync(It.IsAny<CancellationToken>())).Returns(failure);

            var messageHandler = new MockProbeHandler(probe.Object);

            await Assert.ThrowsAsync<NotImplementedException>(() => messageHandler.SendAsync(requestMessage)).ConfigureAwait(false);
        }

        /// <summary>
        /// A mock class to inject a response message.
        /// </summary>
        public class MockProbeHandler : ProbeHandler
        {
            public MockProbeHandler(IProbeMonitor probe)
                : base(probe)
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
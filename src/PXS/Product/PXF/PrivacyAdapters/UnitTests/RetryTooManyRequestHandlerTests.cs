using Microsoft.Membership.MemberServices.PrivacyAdapters.Handlers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.UnitTests
{
    [TestClass]
    public class RetryTooManyRequestHandlerTests
    {

        [TestMethod]
        public async Task Given429Error_ShouldRetry()
        {
            // Arrange
            var innerHandler = new TestHandler((HttpStatusCode)429);
            var httpClient = new HttpClient(new RetryTooManyRequestHandler(innerHandler));

            // Act
            var response = await httpClient.GetAsync("http://example.com");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsTrue(innerHandler.CallCount > 1); // At least one retry occured
        }

        [TestMethod]
        public async Task GivenNon429Error_ShouldNotRetry()
        {
            // Arrange
            var innerHandler = new TestHandler(HttpStatusCode.InternalServerError);
            var httpClient = new HttpClient(new RetryTooManyRequestHandler(innerHandler));

            // Act
            var response = await httpClient.GetAsync("http://example.com");

            // Assert
            Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.AreEqual(1, innerHandler.CallCount); // Only 1 request should be made
        }
    }

    public class TestHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCodeToReturn;
        public int CallCount { get; private set; }

        public TestHandler(HttpStatusCode statusCodeToReturn)
        {
            _statusCodeToReturn = statusCodeToReturn;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;

            if (CallCount == 1)
            {
                return new HttpResponseMessage(_statusCodeToReturn);
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}

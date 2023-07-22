namespace Microsoft.PrivacyServices.CommandFeed.Client.Test
{
    using System;
    using System.Diagnostics;
    using System.Net.Http.Headers;
    using System.Net.Http;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Client.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Moq.Protected;

    [TestClass]
    public class RetryHandlerTests
    {
        private IBackOff backoff;
        private HttpResponseMessage response200;
        private HttpResponseMessage response429;
        private HttpResponseMessage response429WithretryHeader;

        [TestInitialize]
        public void TestInitialize()
        {
            this.backoff = new ExponentialBackoff(delay: TimeSpan.FromSeconds(1), maxDelay: TimeSpan.FromSeconds(30));
            this.response200 = new HttpResponseMessage(HttpStatusCode.OK);
            this.response429 = new HttpResponseMessage((HttpStatusCode)429);
            this.response429WithretryHeader = new HttpResponseMessage
            {
                StatusCode = (HttpStatusCode)429,
                Headers = { RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(3)) }
            };
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ThrowsExcpetionWithNullArgument()
        {
            new RetryHandler(null, 1);
        }


        [TestMethod]
        public async Task SucceedsWithNoRetry()
        {
            // Arrange. Set the call to return a 200
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(this.response200);

            var handler = new RetryHandler(this.backoff, 3)
            {
                InnerHandler = mockHandler.Object,
            };

            var invoker = new HttpMessageInvoker(handler);

            // Act
            var result = await invoker.SendAsync(new HttpRequestMessage(), CancellationToken.None);

            // Assert. SendAsync should only be called once
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            mockHandler.Protected().Verify<Task<HttpResponseMessage>>("SendAsync", Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }

        [TestMethod]
        public async Task SucceedsWithOneExponentialRetry()
        {
            // Arrange. Set the first call to return a 429 and the second call to return a 200
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .SetupSequence<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(this.response429)
                .ReturnsAsync(this.response200);

            var handler = new RetryHandler(this.backoff, 3)
            {
                InnerHandler = mockHandler.Object,
            };

            var invoker = new HttpMessageInvoker(handler);

            // Act
            var result = await invoker.SendAsync(new HttpRequestMessage(), CancellationToken.None);

            // Assert. SendAsync should only be called twice.
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            mockHandler.Protected().Verify<Task<HttpResponseMessage>>("SendAsync", Times.Exactly(2),
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }

        [TestMethod]
        public async Task Returns429AfterMaxExponentialRetries()
        {
            // Arrange. Set the call to return 429 for 3 times
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .SetupSequence<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(this.response429)
                .ReturnsAsync(this.response429)
                .ReturnsAsync(this.response429);

            var maxRetries = 2;
            var handler = new RetryHandler(this.backoff, maxRetries)
            {
                InnerHandler = mockHandler.Object,
            };

            var invoker = new HttpMessageInvoker(handler);

            // Act
            var result = await invoker.SendAsync(new HttpRequestMessage(), CancellationToken.None);

            // Assert. SendAsync should be called maxRetries + 1 times.
            Assert.AreEqual((HttpStatusCode)429, result.StatusCode);
            mockHandler.Protected().Verify<Task<HttpResponseMessage>>("SendAsync", Times.Exactly(maxRetries + 1),
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }

        [TestMethod]
        public async Task SucceedsWithOneRetryHeaderRetry()
        {
            // Arrange. Set the call to return 429 with retry header for 2 times
            var response200 = new HttpResponseMessage(HttpStatusCode.OK);

            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .SetupSequence<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(this.response429WithretryHeader)
                .ReturnsAsync(this.response200);

            var mockBackoff = new Mock<IBackOff>();
            mockBackoff.Setup(m => m.Delay()).Returns(TimeSpan.FromSeconds(1));
            mockBackoff.Setup(m => m.Reset());
            var maxRetries = 2;
            var handler = new RetryHandler(mockBackoff.Object, maxRetries)
            {
                InnerHandler = mockHandler.Object,
            };

            var invoker = new HttpMessageInvoker(handler);

            // Act
            var stopWatch = Stopwatch.StartNew();
            var result = await invoker.SendAsync(new HttpRequestMessage(), CancellationToken.None);

            // Assert.
            // SendAsync should be called twice
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            mockHandler.Protected().Verify<Task<HttpResponseMessage>>("SendAsync", Times.Exactly(2),
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
            // Elapsed time should >= 3s
            Assert.IsTrue(stopWatch.Elapsed.TotalSeconds >= 3);
            // ExpontialBackoff should not be used
            mockBackoff.Verify(x => x.Delay(), Times.Never);
        }

        [TestMethod]
        public async Task Returns429AfterMaxRetryHeaderRetries()
        {
            // Arrange. Set the call to return 429 with retry header for 3 times
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .SetupSequence<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(this.response429WithretryHeader)
                .ReturnsAsync(this.response429WithretryHeader)
                .ReturnsAsync(this.response429WithretryHeader);

            var mockBackoff = new Mock<IBackOff>();
            mockBackoff.Setup(m => m.Delay()).Returns(TimeSpan.FromSeconds(1));
            mockBackoff.Setup(m => m.Reset());

            var maxRetries = 2;
            var handler = new RetryHandler(this.backoff, maxRetries)
            {
                InnerHandler = mockHandler.Object,
            };

            var invoker = new HttpMessageInvoker(handler);

            // Act
            var stopWatch = Stopwatch.StartNew();
            var result = await invoker.SendAsync(new HttpRequestMessage(), CancellationToken.None);

            // Assert.
            // SendAsync should be called maxRetries + 1 times.
            Assert.AreEqual((HttpStatusCode)429, result.StatusCode);
            mockHandler.Protected().Verify<Task<HttpResponseMessage>>("SendAsync", Times.Exactly(maxRetries + 1),
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
            // Elapsed time should >= 6s
            Assert.IsTrue(stopWatch.Elapsed.TotalSeconds >= 6);
            // ExpontialBackoff should not be used
            mockBackoff.Verify(x => x.Delay(), Times.Never);
        }
    }
}

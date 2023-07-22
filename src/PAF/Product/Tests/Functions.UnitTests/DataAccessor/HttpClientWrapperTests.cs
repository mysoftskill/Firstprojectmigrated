namespace Microsoft.PrivacyServices.AzureFunctions.UnitTests.DataAccessor
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.AzureFunctions.Common.DataAccessors;
    using Microsoft.PrivacyServices.AzureFunctions.Common.Models;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;
    using Moq.Protected;
    using Newtonsoft.Json;

    [TestClass]
    public class HttpClientWrapperTests
    {
        private readonly Mock<ILogger> loggerMock;
        private readonly VariantRequest variantRequest;
        private Guid variantRequestId;

        public HttpClientWrapperTests()
        {
            this.loggerMock = new Mock<ILogger>();
            this.variantRequestId = Guid.NewGuid();
            this.variantRequest = new VariantRequest()
            {
                Id = this.variantRequestId.ToString(),
                WorkItemUri = new Uri("https://someUrl")
            };
        }

        [TestMethod]
        public async Task PostSuccessAsync()
        {
            var messageHandlerMock = new Mock<HttpMessageHandler>();
            messageHandlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.Accepted
               })
               .Verifiable();

            var apiUrl = "https://test.com";
            var httpClient = new HttpClient(messageHandlerMock.Object);
            var etag = "*";
            var httpWrapper = new HttpClientWrapper(this.loggerMock.Object, apiUrl, httpClient);

            static Task<string> AccessFunction() => Task.FromResult("testtoken");
            var result = await httpWrapper.PostAsync(apiUrl, AccessFunction, etag).ConfigureAwait(false);
            Assert.IsTrue(result.IsSuccessStatusCode);
            messageHandlerMock.VerifyAll();
        }

        [TestMethod]
        [ExpectedException(typeof(NullReferenceException))]
        public async Task ApprovePendingVariantRequestNullAccessFunctionAsync()
        {
            var messageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            messageHandlerMock
               .Protected()

               // Setup the PROTECTED method to mock
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.Accepted
               })
               .Verifiable();

            var apiUrl = "https://test.com";
            var httpClient = new HttpClient(messageHandlerMock.Object);
            var etag = "*";
            var httpWrapper = new HttpClientWrapper(this.loggerMock.Object, apiUrl, httpClient);
            _ = await httpWrapper.PostAsync(apiUrl, null, etag).ConfigureAwait(false);
            messageHandlerMock.VerifyAll();
        }

        [TestMethod]
        public async Task PostAsyncMissingEtagAsync()
        {
            var messageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            messageHandlerMock
               .Protected()

               // Setup the PROTECTED method to mock
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.Accepted
               })
               .Verifiable();

            var apiUrl = "https://test.com";
            var httpClient = new HttpClient(messageHandlerMock.Object);
            var httpWrapper = new HttpClientWrapper(this.loggerMock.Object, apiUrl, httpClient);

            static Task<string> AccessFunction() => Task.FromResult("testtoken");

            // Still sends the request since ETag is optional
            var result = await httpWrapper.PostAsync(apiUrl, AccessFunction, null).ConfigureAwait(false);
            Assert.IsTrue(result.IsSuccessStatusCode);
            messageHandlerMock.VerifyAll();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task PostAsyncNullAccessTokenReturnedAsync()
        {
            var messageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            messageHandlerMock
               .Protected()

               // Setup the PROTECTED method to mock
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.Accepted
               })
               .Verifiable();

            var apiUrl = "https://test.com";
            var httpClient = new HttpClient(messageHandlerMock.Object);
            var httpWrapper = new HttpClientWrapper(this.loggerMock.Object, apiUrl, httpClient);
            var etag = "*";
            static Task<string> AccessFunction() => Task.FromResult((string)null);

            // Still sends the request since ETag is optional
            var result = await httpWrapper.PostAsync(apiUrl, AccessFunction, etag).ConfigureAwait(false);
            Assert.IsNull(result);
            messageHandlerMock.VerifyAll();
        }

        [TestMethod]
        public async Task PostCallFailsAsync()
        {
            var messageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            messageHandlerMock
               .Protected()

               // Setup the PROTECTED method to mock
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.BadRequest
               })
               .Verifiable();

            var apiUrl = "https://test.com";
            var httpClient = new HttpClient(messageHandlerMock.Object);
            var httpWrapper = new HttpClientWrapper(this.loggerMock.Object, apiUrl, httpClient);
            var etag = "*";

            static Task<string> AccessFunction() => Task.FromResult("testtoken");

            // Still sends the request since ETag is optional
            var result = await httpWrapper.PostAsync(apiUrl, AccessFunction, etag).ConfigureAwait(false);
            Assert.IsFalse(result.IsSuccessStatusCode);
            messageHandlerMock.VerifyAll();
        }

        [TestMethod]
        public async Task UpdateSucceedsAsync()
        {
            var serializedObject = JsonConvert.SerializeObject(this.variantRequest);
            var messageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            messageHandlerMock
               .Protected()

               // Setup the PROTECTED method to mock
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.Accepted,
                   Content = new StringContent(serializedObject, System.Text.Encoding.UTF8, "application/json")
               })
               .Verifiable();

            var apiUrl = "https://test.com";
            var httpClient = new HttpClient(messageHandlerMock.Object);
            var httpWrapper = new HttpClientWrapper(this.loggerMock.Object, apiUrl, httpClient);

            static Task<string> AccessFunction() => Task.FromResult("testtoken");

            VariantRequest result = await httpWrapper.UpdateAsync<VariantRequest>(HttpMethod.Put, apiUrl, AccessFunction, this.variantRequest).ConfigureAwait(false);
            Assert.AreEqual(this.variantRequestId.ToString(), result.Id);
            messageHandlerMock.VerifyAll();
        }

        [TestMethod]
        public async Task UpdateFailsAsync()
        {
            var serializedObject = JsonConvert.SerializeObject(this.variantRequest);
            var messageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            messageHandlerMock
               .Protected()

               // Setup the PROTECTED method to mock
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.BadRequest,
                   Content = new StringContent(serializedObject, System.Text.Encoding.UTF8, "application/json")
               })
               .Verifiable();

            var apiUrl = "https://test.com";
            var httpClient = new HttpClient(messageHandlerMock.Object);
            var httpWrapper = new HttpClientWrapper(this.loggerMock.Object, apiUrl, httpClient);

            static Task<string> AccessFunction() => Task.FromResult("testtoken");

            VariantRequest result = await httpWrapper.UpdateAsync<VariantRequest>(HttpMethod.Put, apiUrl, AccessFunction, this.variantRequest).ConfigureAwait(false);
            messageHandlerMock.VerifyAll();
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetSucceedsAsync()
        {
            var serializedObject = JsonConvert.SerializeObject(this.variantRequest);
            var messageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            messageHandlerMock
               .Protected()

               // Setup the PROTECTED method to mock
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.Accepted,
                   Content = new StringContent(serializedObject, System.Text.Encoding.UTF8, "application/json")
               })
               .Verifiable();

            var apiUrl = "https://test.com";
            var httpClient = new HttpClient(messageHandlerMock.Object);
            var httpWrapper = new HttpClientWrapper(this.loggerMock.Object, apiUrl, httpClient);

            static Task<string> AccessFunction() => Task.FromResult("testtoken");

            VariantRequest result = await httpWrapper.GetAsync<VariantRequest>(apiUrl, AccessFunction).ConfigureAwait(false);
            messageHandlerMock.VerifyAll();
            Assert.AreEqual(this.variantRequestId.ToString(), result.Id);
        }

        [TestMethod]
        public async Task GetFailsAsync()
        {
            var messageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            messageHandlerMock
               .Protected()

               // Setup the PROTECTED method to mock
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.BadRequest
               })
               .Verifiable();

            var apiUrl = "https://test.com";
            var httpClient = new HttpClient(messageHandlerMock.Object);
            var httpWrapper = new HttpClientWrapper(this.loggerMock.Object, apiUrl, httpClient);

            static Task<string> AccessFunction() => Task.FromResult("testtoken");

            VariantRequest result = await httpWrapper.GetAsync<VariantRequest>(apiUrl, AccessFunction).ConfigureAwait(false);
            messageHandlerMock.VerifyAll();
            Assert.IsNull(result);
        }
    }
}

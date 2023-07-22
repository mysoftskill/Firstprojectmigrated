// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Handlers;
    using Microsoft.Membership.MemberServices.Test.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Moq.Protected;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// OutgoingRequestHandler Test
    /// </summary>
    [TestClass]
    public class OutgoingRequestHandlerTest
    {
        private readonly Mock<DelegatingHandler> innerHandler = new Mock<DelegatingHandler>(MockBehavior.Strict);

        [TestInitialize]
        public void TestInitialize()
        {
            Sll.ResetContext();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Sll.ResetContext();
        }
        
        [TestMethod]
        public async Task OutgoingRequestHandlerSuccessNullResponse()
        {
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);

            ReturnsExtensions.ReturnsAsync(this.innerHandler.Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()), response);

            await this.TestSendAsync("OK", "200");
        }

        [TestMethod]
        public async Task OutgoingRequestHandlerErrorResponse()
        {
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.InternalServerError);

            ReturnsExtensions.ReturnsAsync(this.innerHandler.Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()), response);

            await this.TestSendAsync("Internal Server Error", "500");
        }

        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public async Task OutgoingRequestHandlerThrowOnMissingOutgoingApiEvent()
        {
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.InternalServerError);

            ReturnsExtensions.ReturnsAsync(this.innerHandler.Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()), response);

            // Create the request without the api event.
            HttpRequestMessage request = new HttpRequestMessage();

            var handler = new TestAdapterOutgoingRequestHandler();
            handler.InnerHandler = this.innerHandler.Object;

            try
            {
                await handler.TestSendAsync(request, CancellationToken.None);

                Assert.Fail("An exception should have been thrown.");
            }
            catch (KeyNotFoundException e)
            {
                string expectedErrorMessage = "The given key was not present in the dictionary.";
                Assert.AreEqual(expectedErrorMessage, e.Message);
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task OutgoingRequestHandlerThrowOnNullOutgoingApiEvent()
        {
            OutgoingApiEventWrapper apiEvent = null;

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.InternalServerError);

            ReturnsExtensions.ReturnsAsync(this.innerHandler.Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()), response);

            HttpRequestMessage request = new HttpRequestMessage();
            request.Properties.Add(OutgoingRequestHandler.ApiEventContextKey, apiEvent);

            var handler = new TestAdapterOutgoingRequestHandler();
            handler.InnerHandler = this.innerHandler.Object;

            try
            {
                await handler.TestSendAsync(request, CancellationToken.None);

                Assert.Fail("An exception should have been thrown.");
            }
            catch (InvalidOperationException e)
            {
                string expectedErrorMessage = "OutgoingApiEvent is null. It is required to enforce always logging outgoing api events.";
                Assert.AreEqual(expectedErrorMessage, e.Message);
                throw;
            }
        }

        private async Task TestSendAsync(string expectedStatusCodePhrase, string expectedProtocolStatusCode)
        {
            OutgoingApiEventWrapper apiEvent = new OutgoingApiEventWrapper();

            HttpRequestMessage request = new HttpRequestMessage();
            request.Properties.Add(OutgoingRequestHandler.ApiEventContextKey, apiEvent);

            var handler = new TestAdapterOutgoingRequestHandler();
            handler.InnerHandler = this.innerHandler.Object;

            await handler.TestSendAsync(request, CancellationToken.None);

            Assert.AreEqual(expectedProtocolStatusCode, apiEvent.ProtocolStatusCode);
            Assert.AreEqual(expectedStatusCodePhrase, apiEvent.ProtocolStatusCodePhrase);

            // Verify that the API event has the correct error message and success status
            if (expectedProtocolStatusCode.StartsWith("2"))
            {
                Assert.IsTrue(apiEvent.Success);
            }
            else
            {
                Assert.IsFalse(apiEvent.Success);
            }
        }

        public class TestAdapterOutgoingRequestHandler : OutgoingRequestHandler
        {
            public TestAdapterOutgoingRequestHandler() : 
                base(TestMockFactory.CreateCounterFactory().Object, TestMockFactory.CreateLogger().Object, "TestComponent", null)
            {
            }

            public TestAdapterOutgoingRequestHandler(ICounterFactory counterFactory, ILogger logger, string componentName) : 
                base(counterFactory, logger, componentName, null)
            {
            }

            public Task<HttpResponseMessage> TestSendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return this.SendAsync(request, cancellationToken);
            }
        }
    }
}
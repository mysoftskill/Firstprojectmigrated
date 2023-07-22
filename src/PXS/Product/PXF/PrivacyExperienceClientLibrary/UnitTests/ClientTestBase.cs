// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.UnitTests
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Formatting;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common.UnitTests;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Clients.Interfaces;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common;
    using Microsoft.Membership.MemberServices.Test.Common;
    using Microsoft.OSGS.HttpClientCommon;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    /// <summary>
    ///     Client TestBase
    /// </summary>
    public abstract class ClientTestBase
    {
        [TestInitialize]
        public void TestInitialize()
        {
            this.TestEndpoint = new Uri("https://testendpoint.com");
            this.TestCertificate = UnitTestData.UnitTestCertificate;

            this.TestPuid = RequestFactory.GeneratePuid();
            this.TestCid = RequestFactory.GenerateCid();
            this.TestUserProxyTicket = "test_user_proxy_ticket";
            this.TestAccessToken = "test_access_token";
            this.TestRequestId = "test_request_id";

            // HTTP client setup for a successful execution
            var mockHttpResponse = new HttpResponseMessage { Content = new ObjectContent(typeof(string), string.Empty, new JsonMediaTypeFormatter()) };

            // mock body to have empty json object

            this.MockHttpClient = new Mock<IHttpClient>();
            this.MockHttpClient.Setup(c => c.BaseAddress).Returns(this.TestEndpoint);
            this.MockHttpClient.SetupGet(c => c.DefaultRequestHeaders).Returns(new HttpRequestMessage().Headers);
            this.MockHttpClient.SetupGet(c => c.MessageHandler).Returns(new WebRequestHandler());
            this.MockHttpClient
                .Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), HttpCompletionOption.ResponseContentRead))
                .ReturnsAsync(mockHttpResponse);

            this.MockAuthClient = new Mock<IPrivacyAuthClient>();
            this.MockAuthClient.Setup(c => c.GetAccessTokenAsync(It.IsAny<CancellationToken>())).ReturnsAsync(this.TestAccessToken);
            this.MockAuthClient.SetupGet(c => c.ClientCertificate).Returns(this.TestCertificate);

            this.MockSpmConfig = new Mock<IServicePointManagerConfig>();
            this.MockSpmConfig.Setup(c => c.ConnectionLeaseTimeout).Returns(25000);
            this.MockSpmConfig.Setup(c => c.Expect100Continue).Returns(false);
            this.MockSpmConfig.Setup(c => c.MaxIdleTime).Returns(25000);
            this.MockSpmConfig.Setup(c => c.UseNagleAlgorithm).Returns(false);
        }

        protected Mock<IPrivacyAuthClient> MockAuthClient { get; set; }

        protected Mock<IHttpClient> MockHttpClient { get; set; }

        protected Mock<IServicePointManagerConfig> MockSpmConfig { get; set; }

        protected string TestAccessToken { get; set; }

        protected X509Certificate2 TestCertificate { get; set; }

        protected long TestCid { get; set; }

        protected Uri TestEndpoint { get; set; }

        protected long TestPuid { get; set; }

        protected string TestRequestId { get; set; }

        protected string TestUserProxyTicket { get; set; }

        protected PrivacyExperienceClient CreateBasicClient()
        {
            return new PrivacyExperienceClient(this.TestEndpoint, this.MockHttpClient.Object, this.MockAuthClient.Object);
        }

        /// <summary>
        ///     Executes the given <see cref="Func{Task}" /> and asserts the expected error matches the actual error in the exception.
        /// </summary>
        /// <typeparam name="T">The type T returned by the function.</typeparam>
        /// <param name="func">The function.</param>
        /// <param name="expectedError">The expected error.</param>
        /// <param name="expectedStatusCode">The expected status code.</param>
        /// <returns>A Task</returns>
        protected async Task ExecuteAndAssertExceptionMatchesAsync<T>(Func<Task<T>> func, Error expectedError, HttpStatusCode expectedStatusCode)
            where T : class
        {
            try
            {
                T actualResponse = await func().ConfigureAwait(false);
                Assert.IsNotNull(actualResponse);
            }
            catch (PrivacyExperienceTransportException e)
            {
                EqualityHelper.AreEqual(expectedError, e.Error);
                Assert.AreEqual(HttpStatusCode.BadRequest, e.HttpStatusCode);
                throw;
            }

            Assert.Fail("An exception should have been thrown.");
        }

        /// <summary>
        ///     Executes the given <see cref="Func{T}" /> and asserts the expected error matches the actual error in the exception.
        /// </summary>
        /// <param name="func">The function.</param>
        /// <param name="expectedError">The expected error.</param>
        /// <param name="expectedStatusCode">The expected status code.</param>
        /// <returns>A Task</returns>
        protected async Task ExecuteAndAssertExceptionMatchesAsync(Func<Task> func, Error expectedError, HttpStatusCode expectedStatusCode)
        {
            try
            {
                await func().ConfigureAwait(false);
            }
            catch (PrivacyExperienceTransportException e)
            {
                EqualityHelper.AreEqual(expectedError, e.Error);
                Assert.AreEqual(HttpStatusCode.BadRequest, e.HttpStatusCode);
                throw;
            }

            Assert.Fail("An exception should have been thrown.");
        }

        protected void ValidateHttpClientMockHasNoHeader(string headerName)
        {
            this.MockHttpClient.Verify(c => c.SendAsync(It.Is<HttpRequestMessage>(m => !m.Headers.Contains(headerName)), HttpCompletionOption.ResponseContentRead));
        }

        protected void ValidateHttpClientMockHeader(string headerName, string value)
        {
            this.MockHttpClient.Verify(
                c => c.SendAsync(
                    It.Is<HttpRequestMessage>(
                        m =>
                            m.Headers.Contains(headerName) &&
                            m.Headers.GetValues(headerName)
                                .FirstOrDefault()
                                .Equals(value)),
                    HttpCompletionOption.ResponseContentRead));
        }
    }
}

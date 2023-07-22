// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.UnitTests.XboxAccounts
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Formatting;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts.Models;
    using Microsoft.Membership.MemberServices.Test.Common;
    using Microsoft.OSGS.HttpClientCommon;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Windows.Services.AuthN.Client.S2S;

    using Moq;

    using Microsoft.PrivacyServices.Common.Azure;

    [TestClass]
    public class XboxAccountsAdapterTests
    {
        private Mock<IClock> mockClock;

        private readonly Mock<ICounterFactory> mockCounterFactory = TestMockFactory.CreateCounterFactory();

        private readonly Mock<ILogger> mockLogger = TestMockFactory.CreateLogger();

        private XboxAccountsAdapter adapter;

        private Mock<IXboxAccountsAdapterConfiguration> mockConfiguration;

        private Mock<IHttpClient> mockHttpClient;

        //private static readonly DateTimeOffset currentClockTime = new DateTimeOffset(2018, 2, 3, 4, 5, 6, 7, TimeSpan.Zero);
        private static readonly DateTimeOffset currentClockTime = DateTimeOffset.UtcNow;

        [TestMethod]
        public async Task GetUserInfoByPuidExpiredXassTokenShouldReturnError()
        {
            // Arrange
            var expectedResult = new XboxLiveUserLookupInfo
            {
                GamerTag = "blackperl",
                Email = "m@ms.com",
                Xuid = "999",
                Puid = 123
            };

            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ObjectContent<XboxLiveUserLookupInfo>(expectedResult, new JsonMediaTypeFormatter())
            };

            var xassToken = new XassToken();
            xassToken.Token = "xassTokenOK";
            xassToken.NotAfter = DateTime.UtcNow;
            xassToken.IssueInstant = DateTime.UtcNow;

            var xstsToken = new XstsToken();
            xstsToken.Token = "xstsTokenOK";
            xstsToken.NotAfter = DateTime.UtcNow.AddDays(1);
            xstsToken.IssueInstant = DateTime.UtcNow;

            this.SetupMockHttpClient(responseMessage, xassToken, xstsToken);

            // Act
            var requestContext = new PxfRequestContext(null, null, 123, 123, null, null, false, null);
            AdapterResponse<string> adapterResponse = await this.adapter.GetXuidAsync(requestContext).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(adapterResponse);
            Assert.IsNotNull(adapterResponse.Error);
            Assert.IsNull(adapterResponse.Result);
            Assert.IsTrue(adapterResponse.Error.Message.Contains("XASS token returned is not valid."));
        }

        [TestMethod]
        public async Task GetUserInfoByPuidExpiredXstsTokenShouldReturnError()
        {
            // Arrange
            var expectedResult = new XboxLiveUserLookupInfo
            {
                GamerTag = "blackperl",
                Email = "m@ms.com",
                Xuid = "999",
                Puid = 123
            };

            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ObjectContent<XboxLiveUserLookupInfo>(expectedResult, new JsonMediaTypeFormatter())
            };

            var xassToken = new XassToken();
            xassToken.Token = "xassTokenOK";
            xassToken.NotAfter = DateTime.UtcNow.AddDays(1);
            xassToken.IssueInstant = DateTime.UtcNow;

            var xstsToken = new XstsToken();
            xstsToken.Token = "xstsTokenOK";
            xstsToken.NotAfter = DateTime.UtcNow.AddDays(-1);
            xstsToken.IssueInstant = DateTime.UtcNow;

            this.SetupMockHttpClient(responseMessage, xassToken, xstsToken);

            // Act
            var requestContext = new PxfRequestContext(null, null, 123, 123, null, null, false, null);
            AdapterResponse<string> adapterResponse = await this.adapter.GetXuidAsync(requestContext).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(adapterResponse);
            Assert.IsNotNull(adapterResponse.Error);
            Assert.IsNull(adapterResponse.Result);
            Assert.IsTrue(adapterResponse.Error.Message.Contains("XSTS token returned is not valid."));
        }

        [TestMethod]
        public async Task GetUserInfoByPuidCannotParseXstsTokenResponseShouldReturnError()
        {
            // Arrange
            var expectedResult = new XboxLiveUserLookupInfo
            {
                GamerTag = "blackperl",
                Email = "m@ms.com",
                Xuid = "999",
                Puid = 123
            };

            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ObjectContent<XboxLiveUserLookupInfo>(expectedResult, new JsonMediaTypeFormatter())
            };

            var xassToken = new XassToken();
            xassToken.Token = "xassTokenOK";
            xassToken.NotAfter = DateTime.UtcNow.AddDays(1);
            xassToken.IssueInstant = DateTime.UtcNow;

            var xassResponse = new HttpResponseMessage(HttpStatusCode.OK);
            xassResponse.Content = new ObjectContent<XassToken>(xassToken, new JsonMediaTypeFormatter());

            var xstsResponse = new HttpResponseMessage(HttpStatusCode.OK);
            xstsResponse.Content = new ObjectContent<string>("Cannot be parsed into XboxError", new JsonMediaTypeFormatter());

            this.mockHttpClient
                .SetupSequence(
                    client => client.SendAsync(
                        It.IsAny<HttpRequestMessage>(),
                        It.IsAny<HttpCompletionOption>()))
                .Returns(Task.FromResult(xassResponse))
                .Returns(Task.FromResult(xstsResponse))
                .Returns(Task.FromResult(responseMessage));

            // Act
            var requestContext = new PxfRequestContext(null, null, 123, 123, null, null, false, null);
            AdapterResponse<string> adapterResponse = await this.adapter.GetXuidAsync(requestContext).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(adapterResponse);
            Assert.IsNotNull(adapterResponse.Error);
            Assert.IsNull(adapterResponse.Result);
            Assert.IsTrue(adapterResponse.Error.Message.Contains("Cannot be parsed into XboxError"));
        }

        [TestMethod]
        public async Task GetUserInfoByPuidNotFoundShouldReturnError()
        {
            // Arrange
            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            };

            var xassToken = new XassToken();
            xassToken.Token = "xassTokenOK";
            xassToken.NotAfter = DateTime.UtcNow.AddDays(1);
            xassToken.IssueInstant = DateTime.UtcNow;

            var xstsToken = new XstsToken();
            xstsToken.Token = "xstsTokenOK";
            xstsToken.NotAfter = DateTime.UtcNow.AddDays(1);
            xstsToken.IssueInstant = DateTime.UtcNow;

            this.SetupMockHttpClient(responseMessage, xassToken, xstsToken);

            // Act
            var requestContext = new PxfRequestContext(null, null, 123, 123, null, null, false, null);
            AdapterResponse<string> adapterResponse = await this.adapter.GetXuidAsync(requestContext).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(adapterResponse);
            Assert.IsNotNull(adapterResponse.Error);
            Assert.IsNull(adapterResponse.Result);
        }

        [TestMethod]
        public async Task GetUserInfoByPuidNullXassTokenShouldReturnError()
        {
            // Arrange
            var expectedResult = new XboxLiveUserLookupInfo
            {
                GamerTag = "blackperl",
                Email = "m@ms.com",
                Xuid = "999",
                Puid = 123
            };

            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ObjectContent<XboxLiveUserLookupInfo>(expectedResult, new JsonMediaTypeFormatter())
            };

            XassToken xassToken = null;

            var xstsToken = new XstsToken();
            xstsToken.Token = "xstsTokenOK";
            xstsToken.NotAfter = DateTime.UtcNow.AddDays(1);
            xstsToken.IssueInstant = DateTime.UtcNow;

            this.SetupMockHttpClient(responseMessage, xassToken, xstsToken);

            // Act
            var requestContext = new PxfRequestContext(null, null, 123, 123, null, null, false, null);
            AdapterResponse<string> adapterResponse = await this.adapter.GetXuidAsync(requestContext).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(adapterResponse);
            Assert.IsNotNull(adapterResponse.Error);
            Assert.IsNull(adapterResponse.Result);
            Assert.IsTrue(adapterResponse.Error.Message.Contains("Null response in HTTP XASS response body."));
        }

        [TestMethod]
        public async Task GetUserInfoByPuidNullXstsTokenShouldReturnError()
        {
            // Arrange
            var expectedResult = new XboxLiveUserLookupInfo
            {
                GamerTag = "blackperl",
                Email = "m@ms.com",
                Xuid = "999",
                Puid = 123
            };

            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ObjectContent<XboxLiveUserLookupInfo>(expectedResult, new JsonMediaTypeFormatter())
            };

            var xassToken = new XassToken();
            xassToken.Token = "xassTokenOK";
            xassToken.NotAfter = DateTime.UtcNow.AddDays(1);
            xassToken.IssueInstant = DateTime.UtcNow;

            XstsToken xstsToken = null;

            this.SetupMockHttpClient(responseMessage, xassToken, xstsToken);

            // Act
            var requestContext = new PxfRequestContext(null, null, 123, 123, null, null, false, null);
            AdapterResponse<string> adapterResponse = await this.adapter.GetXuidAsync(requestContext).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(adapterResponse);
            Assert.IsNotNull(adapterResponse.Error);
            Assert.IsNull(adapterResponse.Result);
            Assert.IsTrue(adapterResponse.Error.Message.Contains("Null response in HTTP XSTS response body."));
        }

        [TestMethod]
        public async Task GetUserInfoByPuidShouldSucceed()
        {
            // Arrange
            var expectedResult = new XboxLiveUserLookupInfo
            {
                GamerTag = "blackperl",
                Email = "m@ms.com",
                Xuid = "999",
                Puid = 123
            };

            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ObjectContent<XboxLiveUserLookupInfo>(expectedResult, new JsonMediaTypeFormatter())
            };

            var xassToken = new XassToken();
            xassToken.Token = "xassTokenOK";
            xassToken.NotAfter = DateTime.UtcNow.AddDays(1);
            xassToken.IssueInstant = DateTime.UtcNow;

            var xstsToken = new XstsToken();
            xstsToken.Token = "xstsTokenOK";
            xstsToken.NotAfter = DateTime.UtcNow.AddDays(1);
            xstsToken.IssueInstant = DateTime.UtcNow.AddMilliseconds(-1);

            this.SetupMockHttpClient(responseMessage, xassToken, xstsToken);

            // Act
            var requestContext = new PxfRequestContext(null, null, 123, 123, null, null, false, null);
            AdapterResponse<string> adapterResponse = await this.adapter.GetXuidAsync(requestContext).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(adapterResponse);
            Assert.IsNull(adapterResponse.Error);
            Assert.AreEqual(expectedResult.Xuid, adapterResponse.Result);
        }

        /// <summary>
        ///     This is when a PUID does not have an associated PUID
        /// </summary>
        [TestMethod]
        public async Task GetUserInfoByPuidWithProxyTicketXstsErrorShouldReturnEmptyXuid()
        {
            // Arrange
            var xassToken = new XassToken();
            xassToken.Token = "xassTokenOK";
            xassToken.NotAfter = DateTime.UtcNow.AddDays(1);
            xassToken.IssueInstant = DateTime.UtcNow;

            var xassResponse = new HttpResponseMessage(HttpStatusCode.OK);
            xassResponse.Content = new ObjectContent<XassToken>(xassToken, new JsonMediaTypeFormatter());

            var xstsResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            xstsResponse.Content = new ObjectContent<XstsError>(
                new XstsError
                {
                    XErr = 2148916233,
                    Identity = "identity",
                    Message = "message"
                },
                new JsonMediaTypeFormatter());

            this.mockHttpClient
                .SetupSequence(
                    client => client.SendAsync(
                        It.IsAny<HttpRequestMessage>(),
                        It.IsAny<HttpCompletionOption>()))
                .Returns(Task.FromResult(xassResponse))
                .Returns(Task.FromResult(xstsResponse));

            // Act
            var requestContext = new PxfRequestContext("userProxyTicket", null, 123, 123, null, null, false, null);
            AdapterResponse<string> adapterResponse = await this.adapter.GetXuidAsync(requestContext).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(adapterResponse);
            Assert.IsNull(adapterResponse.Error);
            Assert.IsNull(adapterResponse.Result);
        }

        [TestMethod]
        public async Task GetUserInfoByPuidWithProxyTicketEmptyResponseContent()
        {
            // Arrange
            var xassToken = new XassToken();
            xassToken.Token = "xassTokenOK";
            xassToken.NotAfter = DateTime.UtcNow.AddDays(1);
            xassToken.IssueInstant = DateTime.UtcNow;

            var xasuToken = new XasuToken();
            xasuToken.Token = "xasuTokenOK";
            xasuToken.NotAfter = DateTime.UtcNow.AddDays(1);
            xasuToken.IssueInstant = DateTime.UtcNow;

            var xassResponse = new HttpResponseMessage(HttpStatusCode.OK);
            xassResponse.Content = new ObjectContent<XassToken>(xassToken, new JsonMediaTypeFormatter());

            var xasuResponse = new HttpResponseMessage(HttpStatusCode.OK);
            xasuResponse.Content = new ObjectContent<XasuToken>(xasuToken, new JsonMediaTypeFormatter());

            var xstsResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            xstsResponse.Content = null;

            this.mockHttpClient
                .SetupSequence(
                    client => client.SendAsync(
                        It.IsAny<HttpRequestMessage>(),
                        It.IsAny<HttpCompletionOption>()))
                .Returns(Task.FromResult(xasuResponse))
                .Returns(Task.FromResult(xassResponse))
                .Returns(Task.FromResult(xstsResponse));

            // Act
            var requestContext = new PxfRequestContext("userProxyTicket", null, 123, 123, null, null, false, null);
            AdapterResponse<string> adapterResponse = await this.adapter.GetXuidAsync(requestContext).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(adapterResponse);
            Assert.IsFalse(adapterResponse.IsSuccess);
            Assert.IsNotNull(adapterResponse.Error);
        }

        [TestMethod]
        public async Task XassTokenShouldFetchNewWhenNothingIsCached()
        {
            var xassToken = new XassToken
            {
                Token = "xassTokenOK",
                NotAfter = currentClockTime.DateTime.AddDays(1),
                IssueInstant = currentClockTime.DateTime,
            };
            var xassResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ObjectContent<XassToken>(xassToken, new JsonMediaTypeFormatter()) };
            this.mockHttpClient
                .Setup(
                    client => client.SendAsync(
                        It.IsAny<HttpRequestMessage>(),
                        It.IsAny<HttpCompletionOption>()))
                .ReturnsAsync(xassResponse);

            XassToken xassTokenResponse = await this.adapter.GetXassTokenAsync().ConfigureAwait(false);
            Assert.IsNotNull(xassTokenResponse);
        }

        [TestMethod]
        public async Task XassTokenShouldUseCachedTokenWhenNotExpiry()
        {
            // Arrange - seed the cache.
            var xassToken = new XassToken
            {
                Token = "xassTokenOK",
                NotAfter = currentClockTime.DateTime.AddDays(1),
                IssueInstant = currentClockTime.DateTime,
            };
            var xassResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ObjectContent<XassToken>(xassToken, new JsonMediaTypeFormatter()) };
            this.mockHttpClient
                .Setup(
                    client => client.SendAsync(
                        It.IsAny<HttpRequestMessage>(),
                        It.IsAny<HttpCompletionOption>()))
                .ReturnsAsync(xassResponse)
                .Verifiable();

            XassToken xassTokenResponse = await this.adapter.GetXassTokenAsync().ConfigureAwait(false);
            Assert.IsNotNull(xassTokenResponse);
            this.mockHttpClient.Verify(client => client.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<HttpCompletionOption>()), Times.Once);
            this.mockHttpClient.Verify();
            this.mockHttpClient.Invocations.Clear();
            this.mockLogger.Verify(c => c.Information(nameof(XboxAccountsAdapter), "Fetching new Xass token for the first time."), Times.Once);
            this.mockLogger.Invocations.Clear();

            // Act - Attempt 2nd call, should use cached value
            xassTokenResponse = await this.adapter.GetXassTokenAsync().ConfigureAwait(false);

            // Verify
            Assert.IsNotNull(xassTokenResponse);
            this.mockHttpClient.Verify(client => client.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<HttpCompletionOption>()), Times.Never);
            this.mockLogger
                .Verify(
                    c => c.Information(nameof(XboxAccountsAdapter), It.Is<string>(log => log.StartsWith("Using cached Xass token. Token expiry in"))),
                    Times.Once);
        }

        [TestMethod]
        public async Task XassTokenShouldRefreshTokenCacheBeforeExpiry()
        {
            // Arrange - seed the cache.
            var xassToken = new XassToken
            {
                Token = "xassTokenOK",
                NotAfter = currentClockTime.DateTime.AddDays(1),
                IssueInstant = currentClockTime.DateTime,
            };
            this.mockHttpClient
                .Setup(
                    client => client.SendAsync(
                        It.IsAny<HttpRequestMessage>(),
                        It.IsAny<HttpCompletionOption>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ObjectContent<XassToken>(xassToken, new JsonMediaTypeFormatter()) })
                .Verifiable();

            XassToken xassTokenResponse = await this.adapter.GetXassTokenAsync().ConfigureAwait(false);
            Assert.IsNotNull(xassTokenResponse);
            this.mockHttpClient.Verify(client => client.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<HttpCompletionOption>()), Times.Once);
            this.mockHttpClient.Verify();
            this.mockLogger.Verify(c => c.Information(nameof(XboxAccountsAdapter), "Fetching new Xass token for the first time."), Times.Once);
            this.mockLogger.Invocations.Clear();

            // Make the time near expiry based on previous token response.
            this.mockHttpClient
                .Setup(
                    client => client.SendAsync(
                        It.IsAny<HttpRequestMessage>(),
                        It.IsAny<HttpCompletionOption>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ObjectContent<XassToken>(xassToken, new JsonMediaTypeFormatter()) })
                .Verifiable();
            this.mockClock.Setup(c => c.UtcNow)
                .Returns(new DateTimeOffset(xassTokenResponse.NotAfter.AddMinutes(-1 * this.mockConfiguration.Object.RefreshXassTokenBeforeExpiryMinutes)));
            xassTokenResponse = await this.adapter.GetXassTokenAsync().ConfigureAwait(false);
            Assert.IsNotNull(xassTokenResponse);
            this.mockHttpClient.Verify(client => client.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<HttpCompletionOption>()), Times.Exactly(2));
            this.mockHttpClient.Verify();
            this.mockLogger
                .Verify(
                    c => c.Information(nameof(XboxAccountsAdapter), It.Is<string>(log => log.StartsWith("Refreshing cached Xass token. Token expiry in"))),
                    Times.Once);
        }

        [TestMethod]
        public async Task XassTokenShouldRefreshAfterCacheAgeExceeded()
        {
            // Arrange - seed the cache.
            var xassToken = new XassToken
            {
                Token = "xassTokenOK",
                NotAfter = currentClockTime.DateTime.AddDays(1),
                IssueInstant = currentClockTime.DateTime,
            };
            this.mockHttpClient
                .Setup(
                    client => client.SendAsync(
                        It.IsAny<HttpRequestMessage>(),
                        It.IsAny<HttpCompletionOption>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ObjectContent<XassToken>(xassToken, new JsonMediaTypeFormatter()) })
                .Verifiable();

            XassToken xassTokenResponse = await this.adapter.GetXassTokenAsync().ConfigureAwait(false);
            Assert.IsNotNull(xassTokenResponse);
            this.mockHttpClient.Verify(client => client.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<HttpCompletionOption>()), Times.Once);
            this.mockHttpClient.Verify();
            this.mockLogger.Verify(c => c.Information(nameof(XboxAccountsAdapter), "Fetching new Xass token for the first time."), Times.Once);
            this.mockLogger.Invocations.Clear();

            // Make the time near expiry based on previous token issue time.
            this.mockHttpClient
                .Setup(
                    client => client.SendAsync(
                        It.IsAny<HttpRequestMessage>(),
                        It.IsAny<HttpCompletionOption>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ObjectContent<XassToken>(xassToken, new JsonMediaTypeFormatter()) })
                .Verifiable();
            this.mockClock.Setup(c => c.UtcNow)
                .Returns(new DateTimeOffset(xassTokenResponse.IssueInstant.AddMinutes(this.mockConfiguration.Object.MaxXassTokenCacheAgeMinutes)));

            // Act
            xassTokenResponse = await this.adapter.GetXassTokenAsync().ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(xassTokenResponse);
            this.mockHttpClient.Verify(client => client.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<HttpCompletionOption>()), Times.Exactly(2));
            this.mockHttpClient.Verify();
            this.mockLogger
                .Verify(
                    c => c.Information(nameof(XboxAccountsAdapter), It.Is<string>(log => log.StartsWith("Refreshing cached Xass token due to max cache age reached: "))),
                    Times.Once);
        }

        [TestMethod]
        public async Task GetUsersInfoByPuidShouldSucceed()
        {
            // Arrange
            var expectedResult = new XboxLiveUsersLookupInfo
            {
                Users = new List<XboxLiveUserLookupInfo>
                {
                    new XboxLiveUserLookupInfo
                    {
                        GamerTag = "blackperl",
                        Email = "m@ms.com",
                        Xuid = "999",
                        Puid = 123
                    },
                    new XboxLiveUserLookupInfo
                    {
                        GamerTag = "blackperl2",
                        Email = "m2@ms.com",
                        Xuid = "9999",
                        Puid = 1234
                    }
                }
            };

            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ObjectContent<XboxLiveUsersLookupInfo>(expectedResult, new JsonMediaTypeFormatter())
            };

            var xassToken = new XassToken();
            xassToken.Token = "xassTokenOK";
            xassToken.NotAfter = DateTime.UtcNow.AddDays(1);
            xassToken.IssueInstant = DateTime.UtcNow;

            var xstsToken = new XstsToken();
            xstsToken.Token = "xstsTokenOK";
            xstsToken.NotAfter = DateTime.UtcNow.AddDays(1);
            xstsToken.IssueInstant = DateTime.UtcNow.AddMilliseconds(-1);

            this.SetupMockHttpClient(responseMessage, xassToken, xstsToken);

            // Act
            AdapterResponse<Dictionary<long, string>> adapterResponse = await this.adapter.GetXuidsAsync(new List<long> { 123, 1234 }).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(adapterResponse);
            Assert.IsNull(adapterResponse.Error);
            Assert.AreEqual(2, adapterResponse.Result.Count);
            Assert.AreEqual("999", adapterResponse.Result[123]);
            Assert.AreEqual("9999", adapterResponse.Result[1234]);
        }

        [TestMethod]
        public async Task GetUsersInfoByPuidShouldBatchSucceed()
        {
            // Arrange
            var expectedResult1 = new XboxLiveUsersLookupInfo
            {
                Users = new List<XboxLiveUserLookupInfo>
                {
                    new XboxLiveUserLookupInfo
                    {
                        GamerTag = "blackperl",
                        Email = "m@ms.com",
                        Xuid = "999",
                        Puid = 123
                    },
                    new XboxLiveUserLookupInfo
                    {
                        GamerTag = "blackgems",
                        Email = "m@ms.com",
                        Xuid = "888",
                        Puid = 12
                    }
                }
            };
            var responseMessage1 = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ObjectContent<XboxLiveUsersLookupInfo>(expectedResult1, new JsonMediaTypeFormatter())
            };

            var expectedResult2 = new XboxLiveUsersLookupInfo
            {
                Users = new List<XboxLiveUserLookupInfo>
                {
                    new XboxLiveUserLookupInfo
                    {
                        GamerTag = "blackperl2",
                        Email = "m2@ms.com",
                        Xuid = "9999",
                        Puid = 1234
                    },
                    new XboxLiveUserLookupInfo
                    {
                        GamerTag = "blackperl23",
                        Email = "m2@ms.com",
                        Xuid = "8888",
                        Puid = 12345
                    }
                }
            };
            var responseMessage2 = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ObjectContent<XboxLiveUsersLookupInfo>(expectedResult2, new JsonMediaTypeFormatter())
            };
            
            var xassToken = new XassToken();
            xassToken.Token = "xassTokenOK";
            xassToken.NotAfter = DateTime.UtcNow.AddDays(1);
            xassToken.IssueInstant = DateTime.UtcNow;

            var xstsToken = new XstsToken();
            xstsToken.Token = "xstsTokenOK";
            xstsToken.NotAfter = DateTime.UtcNow.AddDays(1);
            xstsToken.IssueInstant = DateTime.UtcNow.AddMilliseconds(-1);

            var xassResponse = new HttpResponseMessage(HttpStatusCode.OK);
            xassResponse.Content = new ObjectContent<XassToken>(xassToken, new JsonMediaTypeFormatter());

            var xstsResponse = new HttpResponseMessage(HttpStatusCode.OK);
            xstsResponse.Content = new ObjectContent<XstsToken>(xstsToken, new JsonMediaTypeFormatter());

            this.mockHttpClient
                .SetupSequence(
                    client => client.SendAsync(
                        It.IsAny<HttpRequestMessage>(),
                        It.IsAny<HttpCompletionOption>()))
                .Returns(Task.FromResult(xassResponse))
                .Returns(Task.FromResult(xstsResponse))
                .Returns(Task.FromResult(responseMessage1))
                .Returns(Task.FromResult(responseMessage2));

            var puids = new List<long>();
            for (int i = 0; i < 230; i++)
            {
                puids.Add(i);
            }

            // Act
            AdapterResponse<Dictionary<long, string>> adapterResponse = await this.adapter.GetXuidsAsync(puids).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(adapterResponse);
            Assert.IsNull(adapterResponse.Error);
            Assert.AreEqual(4, adapterResponse.Result.Count);
            Assert.AreEqual("999", adapterResponse.Result[123]);
            Assert.AreEqual("888", adapterResponse.Result[12]);
            Assert.AreEqual("9999", adapterResponse.Result[1234]);
            Assert.AreEqual("8888", adapterResponse.Result[12345]);
        }

        [TestInitialize]
        public void TestInitialize()
        {
            this.mockClock = new Mock<IClock>(MockBehavior.Strict);
            this.mockClock.Setup(c => c.UtcNow).Returns(currentClockTime);

            this.mockHttpClient = new Mock<IHttpClient>(MockBehavior.Strict);
            this.mockConfiguration = new Mock<IXboxAccountsAdapterConfiguration>();

            this.mockConfiguration.SetupGet(m => m.XassServiceEndpoint).Returns("https://doesnotmatter.com");
            this.mockConfiguration.SetupGet(m => m.XstsServiceEndpoint).Returns("https://doesnotmatter.com");
            this.mockConfiguration.SetupGet(m => m.XasuServiceEndpoint).Returns("https://doesnotmatter.com");
            this.mockConfiguration.SetupGet(m => m.BaseUrl).Returns("https://doesnotmatter.com");
            this.mockConfiguration.SetupGet(m => m.XtokenMsaS2STargetSite).Returns("https://doesnotmatter.com");
            this.mockConfiguration.SetupGet(m => m.EnableAdapter).Returns(true);
            this.mockConfiguration.SetupGet(m => m.RefreshXassTokenBeforeExpiryMinutes).Returns(10);
            this.mockConfiguration.SetupGet(m => m.MaxXassTokenCacheAgeMinutes).Returns(480);

            var mockCertificate = new Mock<X509Certificate2>();

            var mockS2SAuthClient = new Mock<IS2SAuthClient>();
            mockS2SAuthClient.Setup(m => m.ClientCertificate).Returns(mockCertificate.Object);
            mockS2SAuthClient.Setup(m => m.GetAccessTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), null))
                .Returns(Task.FromResult("s2stoken"));

            this.adapter = new XboxAccountsAdapter(
                this.mockHttpClient.Object,
                this.mockConfiguration.Object,
                mockS2SAuthClient.Object,
                this.mockLogger.Object,
                this.mockClock.Object);
        }

        #region Helpers

        private void SetupMockHttpClient(HttpResponseMessage responseMessage, XassToken xassToken, XstsToken xstsToken)
        {
            var xassResponse = new HttpResponseMessage(HttpStatusCode.OK);
            xassResponse.Content = new ObjectContent<XassToken>(xassToken, new JsonMediaTypeFormatter());

            var xstsResponse = new HttpResponseMessage(HttpStatusCode.OK);
            xstsResponse.Content = new ObjectContent<XstsToken>(xstsToken, new JsonMediaTypeFormatter());

            this.mockHttpClient
                .SetupSequence(
                    client => client.SendAsync(
                        It.IsAny<HttpRequestMessage>(),
                        It.IsAny<HttpCompletionOption>()))
                .Returns(Task.FromResult(xassResponse))
                .Returns(Task.FromResult(xstsResponse))
                .Returns(Task.FromResult(responseMessage));
        }

        #endregion
    }
}

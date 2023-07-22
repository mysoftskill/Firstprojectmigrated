// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.UnitTests.AadRequestVerificationServiceAdapter
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.OSGS.HttpClientCommon;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Newtonsoft.Json;

    using HttpClient = Microsoft.OSGS.HttpClientCommon.HttpClient;

    [TestClass]
    public class AadRequestVerificationServiceAdapterTests
    {
        const string AuthHeaderValueApp = "Bearer ActorTokenValueGoesHere";

        const string AuthHeaderValuePft = "MSAuth1.0 actortoken=\"Bearer ActorTokenValueGoesHere\",accesstoken=\"Bearer AccessTokeNValueGoesHere\",type=\"PFAT\"";

        private IAadRequestVerificationServiceAdapter adapter;

        private Mock<IAadAuthManager> mockAadAuthManager;

        private Mock<IAadRequestVerificationServiceAdapterConfiguration> mockConfiguration;

        private Mock<ICounterFactory> mockCounterFactory;

        private Mock<IHttpClient> mockHttpClient;

        private IRequestContext mockRequestContext;

        public static IEnumerable<object[]> GenerateAadRequestVerificationConstructorData()
        {
            var httpClient = new Mock<IHttpClient>();
            var config = new Mock<IAadRequestVerificationServiceAdapterConfiguration>();
            var aadAuthManager = new Mock<IAadAuthManager>();
            var counterFactor = new Mock<ICounterFactory>();

            var data = new List<object[]>
            {
                new object[]
                {
                    null,
                    config.Object,
                    aadAuthManager.Object,
                    counterFactor.Object,
                    "Value cannot be null.\r\nParameter name: httpClient"
                },
                new object[]
                {
                    httpClient.Object,
                    null,
                    aadAuthManager.Object,
                    counterFactor.Object,
                    "Value cannot be null.\r\nParameter name: configuration"
                },
                new object[]
                {
                    httpClient.Object,
                    config.Object,
                    null,
                    counterFactor.Object,
                    "Value cannot be null.\r\nParameter name: aadAuthManager"
                }
            };

            return data;
        }

        public static IEnumerable<object[]> GeneratePuidData()
        {
            // new object[]{"[tokenString]",[expectedPuid],[expectedResult]}
            // The below tokens are dummy. I used Jwt debugger to generate them.
            //see here https://jwt.io/#debugger

            var data = new List<object[]>
            {
                new object[] // Invalid puid
                {
                    "e30.eyJ1bmlxdWVfbmFtZSI6IlRlc3QiLCJwdWlkIjoiaW52YWxpZF9wdWlkXzEyMzQ1IiwibmJmIjoxNTM2MzMwNDM3LCJleHAiOjE1MzYzMzA3MzcsImlhdCI6MTUzNjMzMDQzNywiaXNzIjoic2VsZiJ9.",
                    "invalid_puid_12345",
                    false
                },
                new object[] // Puid is 123647458884598
                {
                    "eyJhbGciOiJIUzI1NiJ9.eyJ1bmlxdWVfbmFtZSI6IlRlc3QiLCJwdWlkIjoiMTIzNjQ3NDU4ODg0NTk4IiwibmJmIjoxNTM2MzMwNDM3LCJleHAiOjE1MzYzMzA3MzcsImlhdCI6MTUzNjMzMDQzNywiaXNzIjoic2VsZiJ9.VNsWVB_x81xWq5wy-6yLGyi7hkgZZRfK5Qf8DaOldBM",
                    "123647458884598",
                    true
                },
                new object[] // Puid is empty
                {
                    "eyJhbGciOiJIUzI1NiJ9.eyJ1bmlxdWVfbmFtZSI6IlRlc3QiLCJwdWlkIjoiIiwibmJmIjoxNTM2MzMwNDM3LCJleHAiOjE1MzYzMzA3MzcsImlhdCI6MTUzNjMzMDQzNywiaXNzIjoic2VsZiJ9.2YaTUX-tHKgk2K-mBIqm6Vsy6yYBh_fwuoXzP6ct_o0",
                    string.Empty,
                    false
                },
                new object[] // Puid is null (no puid/home_puid claim)
                {
                    "eyJhbGciOiJIUzI1NiJ9.eyJ1bmlxdWVfbmFtZSI6IlRlc3QiLCJuYmYiOjE1MzYzMzA0MzcsImV4cCI6MTUzNjMzMDczNywiaWF0IjoxNTM2MzMwNDM3LCJpc3MiOiJzZWxmIn0.M85BrBAuVukmZgWPNjWJvSd_zelAgcH688VrlTVHNcU",
                    "null",
                    false
                },
                new object[] // No token
                {
                    "false_token",
                    "123123",
                    false
                },
                new object[] // Puid is in home_puid
                {
                    "eyJhbGciOiJIUzI1NiJ9.eyJ1bmlxdWVfbmFtZSI6IlRlc3QiLCJob21lX3B1aWQiOiIzMTQxNTkyNjUzNSIsIm5iZiI6MTUzNjMzMDQzNywiZXhwIjoxNTM2MzMwNzM3LCJpYXQiOjE1MzYzMzA0MzcsImlzcyI6InNlbGYifQ.1PXKEod-0LWPdlLlSJjNd5yyQ6VDo5Qpne4FcQ5M2IQ",
                    "31415926535",
                    true
                },
                new object[] // empty
                {
                    string.Empty,
                    string.Empty,
                    false
                }
            };
            return data;
        }

        public static IEnumerable<object[]> GenerateVerifierData()
        {
            // string verfierV2, string[] verifierV3, TenantIdType tenantIdType, bool isExport, bool shouldSucceeded
            var data = new List<object[]>
            {
                new object[]
                {
                    null,
                    new string[] { "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJwdWlkIjoiMTIzNDU2Nzg5MCJ9.BihCoR0E50Zup8umg0xEOjJZbN_nl8jou_1nc40215g" },
                    TenantIdType.Home,
                    false // missing verifier v2
                },
                new object[]
                {
                    "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJwdWlkIjoiMTIzNDU2Nzg5MCJ9.BihCoR0E50Zup8umg0xEOjJZbN_nl8jou_1nc40215g",
                    null,
                    TenantIdType.Home,
                    false // missing verifier v3
                },
                new object[]
                {
                    "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJwdWlkIjoiMTIzNDU2Nzg5MCJ9.BihCoR0E50Zup8umg0xEOjJZbN_nl8jou_1nc40215g",
                    new string[] { "v1", "v2" },
                    TenantIdType.Home,
                    false // more than 1 verifier v3
                },
                new object[]
                {
                    "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJwdWlkIjoiMTIzNDU2Nzg5MCJ9.BihCoR0E50Zup8umg0xEOjJZbN_nl8jou_1nc40215g",
                    new string[] { "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJwdWlkIjoiMTIzNDU2Nzg5MCJ9.BihCoR0E50Zup8umg0xEOjJZbN_nl8jou_1nc40215g" },
                    TenantIdType.Home,
                    true
                },
                new object[]
                {
                    null,
                    new string[] { "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJwdWlkIjoiMTIzNDU2Nzg5MCIsImhvbWVfdGlkIjoiN2JkYjI1NDUtNjcwMi00OTBkLThkMDctNWNjMGE1Mzc2ZGQ5In0.EeEU_TEx9CJt2swU2Y49G8hPl1gwJGhf0aB4XiHYh_k" },
                    TenantIdType.Resource,
                    true
                },
                new object[]
                {
                    "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJwdWlkIjoiMTIzNDU2Nzg5MCJ9.BihCoR0E50Zup8umg0xEOjJZbN_nl8jou_1nc40215g",
                    new string[] { "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJwdWlkIjoiMTIzNDU2Nzg5MCIsImhvbWVfdGlkIjoiN2JkYjI1NDUtNjcwMi00OTBkLThkMDctNWNjMGE1Mzc2ZGQ5In0.EeEU_TEx9CJt2swU2Y49G8hPl1gwJGhf0aB4XiHYh_k" },
                    TenantIdType.Resource,
                    false // Resource tenant request shouldn't have verifier v2
                },
            };

            return data;
        }

        [TestMethod]
        [DynamicData(nameof(GenerateAadRequestVerificationConstructorData), DynamicDataSourceType.Method)]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AadRequestVerificationServiceAdapterExHandling(
            IHttpClient httpClient,
            IAadRequestVerificationServiceAdapterConfiguration config,
            IAadAuthManager aadAuthManager,
            ICounterFactory counterFactory,
            string expectedMessage)
        {
            try
            {
                //Act
                var result = new AadRequestVerificationServiceAdapter(
                    httpClient,
                    config,
                    aadAuthManager,
                    counterFactory);
                Assert.Fail("Should have failed.");
            }
            catch (ArgumentNullException e)
            {
                //Assert
                Assert.AreEqual(expectedMessage, e.Message);
                throw;
            }
        }

        [TestMethod]
        public async Task ActorListAuthorizationFail()
        {
            var responseMessage = new HttpResponseMessage();
            responseMessage.StatusCode = HttpStatusCode.InternalServerError;
            responseMessage.Content = new StringContent("errormessage");
            this.mockHttpClient.Setup(c => c.SendAsync(It.Is<HttpRequestMessage>(m => m.Headers.Contains(HeaderNames.ClientRequestId)))).ReturnsAsync(responseMessage);

            AadRvsActorRequest aadRvsActorRequest = new AadRvsActorRequest
            {
                CorrelationId = "testCv",
                TargetObjectId = Guid.NewGuid().ToString(),
                TargetTenantId = Guid.NewGuid().ToString(),
                CommandIds = Guid.NewGuid().ToString()
            };

            AdapterResponse<AadRvsScopeResponse> result =
                await this.adapter.ActorListAuthorizationAsync(aadRvsActorRequest, this.mockRequestContext).ConfigureAwait(false);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(result.Error.Message, "errormessage");
        }

        [TestMethod]
        public async Task ActorListAuthorizationSuccess()
        {
            var responseMessage = new HttpResponseMessage();
            responseMessage.StatusCode = HttpStatusCode.OK;
            AadRvsScopeResponse aadRvsScopeResponse = new AadRvsScopeResponse
            {
                Message = null,
                Outcome = "OperationSuccess",
                Scopes = "User.Controller.Export User.Controller.Delete"
            };
            responseMessage.Content = new StringContent(JsonConvert.SerializeObject(aadRvsScopeResponse));
            this.mockHttpClient.Setup(c => c.SendAsync(It.Is<HttpRequestMessage>(m => m.Headers.Contains(HeaderNames.ClientRequestId)))).ReturnsAsync(responseMessage);

            AadRvsActorRequest aadRvsActorRequest = new AadRvsActorRequest
            {
                CorrelationId = "testCv",
                TargetObjectId = Guid.NewGuid().ToString(),
                TargetTenantId = Guid.NewGuid().ToString(),
                CommandIds = Guid.NewGuid().ToString()
            };

            AdapterResponse<AadRvsScopeResponse> result =
                await this.adapter.ActorListAuthorizationAsync(aadRvsActorRequest, this.mockRequestContext).ConfigureAwait(false);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(result.Result.Outcome, aadRvsScopeResponse.Outcome);
            Assert.AreEqual(result.Result.Scopes, aadRvsScopeResponse.Scopes);
            Assert.AreEqual(result.Result.Message, aadRvsScopeResponse.Message);
        }

        [TestMethod]
        [DataRow(HttpStatusCode.InternalServerError, AdapterErrorCode.Unknown)]
        [DataRow(HttpStatusCode.BadRequest, AdapterErrorCode.InvalidInput)]
        [DataRow(HttpStatusCode.Forbidden, AdapterErrorCode.Forbidden)]
        [DataRow(HttpStatusCode.Conflict, AdapterErrorCode.ConcurrencyConflict)]
        [DataRow((HttpStatusCode)429, AdapterErrorCode.TooManyRequests)]
        public async Task ConstructAccountCloseErrorHandlingSuccess(HttpStatusCode statusCode, AdapterErrorCode expectedErrorCode)
        {
            //Arrange
            var responseMessage = new HttpResponseMessage();
            responseMessage.StatusCode = statusCode;
            responseMessage.Content = new StringContent("doesnotmatter");

            string expectedVerifier = "expectedVerifier";
            responseMessage.Headers.Add("Verifier", new List<string> { expectedVerifier });
            this.mockHttpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>())).ReturnsAsync(responseMessage);

            //Act
            var aadRvsRequest = new AadRvsRequest() { PreVerifier = "preverifier" };
            AdapterResponse<AadRvsVerifiers> response = await this.adapter.ConstructAccountCloseAsync(aadRvsRequest).ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(response);
            Assert.IsFalse(response.IsSuccess);
            Assert.IsNull(response.Result);
            Assert.AreEqual(expectedErrorCode.ToString(), response.Error.Code.ToString());
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("123123")]
        public async Task ConstructAccountCloseSuccessV2(string orgIdPuid)
        {
            var responseMessage = new HttpResponseMessage();
            string expectedVerifier = "expectedVerifier";

            // V2 response has no V3 verifier
            var aadRvsResponse = new AadRvsResponse
            {
                Outcome = "outcome",
                Message = "message"
            };

            responseMessage.Headers.Add("Verifier", new List<string> { expectedVerifier });
            responseMessage.Content = new StringContent(JsonConvert.SerializeObject(aadRvsResponse));

            this.mockHttpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>())).ReturnsAsync(responseMessage);

            var aadRvsRequest = new AadRvsRequest();
            aadRvsRequest.OrgIdPuid = orgIdPuid;
            aadRvsRequest.PreVerifier = "preverifier";
            AdapterResponse<AadRvsVerifiers> result = await this.adapter.ConstructAccountCloseAsync(aadRvsRequest).ConfigureAwait(false);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(expectedVerifier, result.Result.V2);
            Assert.IsNull(result.Result.V3);
            this.ValidateHttpRequestMessageContainsAuthorizationHeaders();
        }

        [TestMethod]
        public async Task ConstructAccountCloseSuccessV3()
        {
            var responseMessage = new HttpResponseMessage();
            string expectedVerifierV2 = "expectedVerifierV2";
            string expectedVerifierV3_1 = "expectedVerifierV31";
            string expectedVerifierV3_2 = "expectedVerifierV32";

            // V3 response has V3 verifiers
            var aadRvsResponse = new AadRvsResponseV3
            {
                Outcome = "outcome",
                Message = "message",
                Verifiers = new string[] { expectedVerifierV3_1, expectedVerifierV3_2 }
            };

            responseMessage.Headers.Add("Verifier", new List<string> { expectedVerifierV2 });
            responseMessage.Content = new StringContent(JsonConvert.SerializeObject(aadRvsResponse));
            
            this.mockHttpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>())).ReturnsAsync(responseMessage);

            var aadRvsRequest = new AadRvsRequest();
            aadRvsRequest.OrgIdPuid = "123";
            aadRvsRequest.PreVerifier = "preverifier";
            AdapterResponse<AadRvsVerifiers> result = await this.adapter.ConstructAccountCloseAsync(aadRvsRequest).ConfigureAwait(false);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(expectedVerifierV2, result.Result.V2);
            Assert.IsNotNull(result.Result.V3);
            Assert.AreEqual(2, result.Result.V3.Length);
            Assert.AreEqual(expectedVerifierV3_1, result.Result.V3[0]);
            Assert.AreEqual(expectedVerifierV3_2, result.Result.V3[1]);
            this.ValidateHttpRequestMessageContainsAuthorizationHeaders();
        }

        [TestMethod]
        public async Task ConstructAccountCleanupWithoutRequestContextSuccess()
        {
            var responseMessage = new HttpResponseMessage();
            string expectedVerifierV3 = "expectedVerifierV3";

            // V3 response has V3 verifiers
            var aadRvsResponse = new AadRvsResponseV3
            {
                Outcome = "outcome",
                Message = "message",
                Verifiers = new string[] { expectedVerifierV3 }
            };

            responseMessage.Content = new StringContent(JsonConvert.SerializeObject(aadRvsResponse));

            this.mockHttpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>())).ReturnsAsync(responseMessage);

            var aadRvsRequest = new AadRvsRequest();
            aadRvsRequest.OrgIdPuid = "123";
            aadRvsRequest.PreVerifier = "preverifier";
            AdapterResponse<AadRvsVerifiers> result = await this.adapter.ConstructAccountCleanupAsync(aadRvsRequest, null).ConfigureAwait(false);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsNull(result.Result.V2);
            Assert.IsNotNull(result.Result.V3);
            Assert.AreEqual(1, result.Result.V3.Length);
            Assert.AreEqual(expectedVerifierV3, result.Result.V3[0]);
            this.ValidateHttpRequestMessageContainsAuthorizationHeaders();
            this.ValidateHttpRequestMessageAuthorizationScheme("Bearer");
        }

        [TestMethod]
        public async Task ConstructAccountCleanupWithRequestContextSuccess()
        {
            var responseMessage = new HttpResponseMessage();
            string expectedVerifierV3 = "expectedVerifierV3";

            // V3 response has V3 verifiers
            var aadRvsResponse = new AadRvsResponseV3
            {
                Outcome = "outcome",
                Message = "message",
                Verifiers = new string[] { expectedVerifierV3 }
            };

            responseMessage.Content = new StringContent(JsonConvert.SerializeObject(aadRvsResponse));

            this.mockHttpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>())).ReturnsAsync(responseMessage);

            var aadRvsRequest = new AadRvsRequest();
            aadRvsRequest.OrgIdPuid = "123";
            AdapterResponse<AadRvsVerifiers> result = await this.adapter.ConstructAccountCleanupAsync(aadRvsRequest, this.mockRequestContext).ConfigureAwait(false);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsNull(result.Result.V2);
            Assert.IsNotNull(result.Result.V3);
            Assert.AreEqual(1, result.Result.V3.Length);
            Assert.AreEqual(expectedVerifierV3, result.Result.V3[0]);
            this.ValidateHttpRequestMessageContainsAuthorizationHeaders();
            this.ValidateHttpRequestMessageAuthorizationScheme("MSAuth1.0");
        }

        [TestMethod]
        public async Task ConstructAccountCloseFailIfContinuationPresents()
        {
            var responseMessage = new HttpResponseMessage();
            string expectedVerifierV2 = "expectedVerifierV2";
            string expectedVerifierV3_1 = "expectedVerifierV31";
            string expectedVerifierV3_2 = "expectedVerifierV32";

            // V3 response has V3 verifiers
            var aadRvsResponse = new AadRvsResponseV3
            {
                Outcome = "outcome",
                Message = "message",
                Verifiers = new string[] { expectedVerifierV3_1, expectedVerifierV3_2 },
                Continuation = "aa"
            };

            responseMessage.Headers.Add("Verifier", new List<string> { expectedVerifierV2 });
            responseMessage.Content = new StringContent(JsonConvert.SerializeObject(aadRvsResponse));

            this.mockHttpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>())).ReturnsAsync(responseMessage);

            var aadRvsRequest = new AadRvsRequest();
            aadRvsRequest.OrgIdPuid = "123";
            aadRvsRequest.PreVerifier = "preverifier";
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await this.adapter.ConstructAccountCloseAsync(aadRvsRequest));
        }

        [TestMethod]
        public async Task ConstructDeleteSuccess()
        {
            var responseMessage = new HttpResponseMessage();
            string expectedVerifier = "expectedVerifier";
            responseMessage.Headers.Add("Verifier", new List<string> { expectedVerifier });
            this.mockHttpClient.Setup(c => c.SendAsync(It.Is<HttpRequestMessage>(m => m.Headers.Contains(HeaderNames.ClientRequestId)))).ReturnsAsync(responseMessage);

            AdapterResponse<string> result = await this.adapter.ConstructDeleteAsync(new AadRvsRequest(), this.mockRequestContext).ConfigureAwait(false);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(expectedVerifier, result.Result);
            this.ValidateHttpRequestMessageContainsAuthorizationHeaders();
        }

        [TestMethod]
        public async Task ConstructDeleteFail()
        {
            var responseMessage = new HttpResponseMessage();
            responseMessage.StatusCode = HttpStatusCode.InternalServerError;
            responseMessage.Content = new StringContent("errormessage");
            string expectedVerifier = "expectedVerifier";
            responseMessage.Headers.Add("Verifier", new List<string> { expectedVerifier });
            this.mockHttpClient.Setup(c => c.SendAsync(It.Is<HttpRequestMessage>(m => m.Headers.Contains(HeaderNames.ClientRequestId)))).ReturnsAsync(responseMessage);

            AdapterResponse<string> result = await this.adapter.ConstructDeleteAsync(new AadRvsRequest(), this.mockRequestContext).ConfigureAwait(false);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(result.Error.Message, "errormessage");
            this.ValidateHttpRequestMessageContainsAuthorizationHeaders();
        }

        [TestMethod]
        public async Task ShouldNotConstructDeleteWithMsaIdentity()
        {
            //Arrange
            var responseMessage = new HttpResponseMessage();
            string expectedVerifier = "expectedVerifier";
            responseMessage.Headers.Add("Verifier", new List<string> { expectedVerifier });
            this.mockRequestContext = new RequestContext(new MsaSiteIdentity(It.IsAny<string>(), It.IsAny<long>()), new Uri("https://unittest"), new Dictionary<string, string[]>());
            this.mockHttpClient.Setup(c => c.SendAsync(It.Is<HttpRequestMessage>(m => m.Headers.Contains(HeaderNames.ClientRequestId)))).ReturnsAsync(responseMessage);
            try
            {
                //Act
                AdapterResponse<string> result = await this.adapter.ConstructDeleteAsync(new AadRvsRequest(), this.mockRequestContext).ConfigureAwait(false);
                Assert.Fail("Shouldn't get here");
            }
            catch (ArgumentOutOfRangeException ex)
            {
                //Assert
                Assert.AreEqual("Specified argument was out of the range of valid values.\r\nParameter name: Identity", ex.Message);
            }
        }

        [TestMethod]
        public async Task ConstructExportSuccessV2()
        {
            var responseMessage = new HttpResponseMessage();
            string expectedVerifier = "expectedVerifier";

            var aadRvsResponse = new AadRvsResponse
            {
                Outcome = "outcome",
                Message = "message"
            };

            responseMessage.Headers.Add("Verifier", new List<string> { expectedVerifier });
            responseMessage.Content = new StringContent(JsonConvert.SerializeObject(aadRvsResponse));

            this.mockHttpClient.Setup(c => c.SendAsync(It.Is<HttpRequestMessage>(m => m.Headers.Contains(HeaderNames.ClientRequestId)))).ReturnsAsync(responseMessage);

            AdapterResponse<AadRvsVerifiers> result = await this.adapter.ConstructExportAsync(new AadRvsRequest(), this.mockRequestContext).ConfigureAwait(false);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(expectedVerifier, result.Result.V2);
            Assert.IsNull(result.Result.V3);
            this.ValidateHttpRequestMessageContainsAuthorizationHeaders();
        }

        [TestMethod]
        public async Task ConstructExportSuccessV3()
        {
            var responseMessage = new HttpResponseMessage();
            string expectedVerifierV2 = "expectedVerifierV2";
            string expectedVerifierV3_1 = "expectedVerifierV31";
            string expectedVerifierV3_2 = "expectedVerifierV32";

            // V3 response has V3 verifiers
            var aadRvsResponse = new AadRvsResponseV3
            {
                Outcome = "outcome",
                Message = "message",
                Verifiers = new string[] { expectedVerifierV3_1, expectedVerifierV3_2 }
            };

            responseMessage.Headers.Add("Verifier", new List<string> { expectedVerifierV2 });
            responseMessage.Content = new StringContent(JsonConvert.SerializeObject(aadRvsResponse));

            this.mockHttpClient.Setup(c => c.SendAsync(It.Is<HttpRequestMessage>(m => m.Headers.Contains(HeaderNames.ClientRequestId)))).ReturnsAsync(responseMessage);

            AdapterResponse<AadRvsVerifiers> result = await this.adapter.ConstructExportAsync(new AadRvsRequest(), this.mockRequestContext).ConfigureAwait(false);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(expectedVerifierV2, result.Result.V2);
            Assert.IsNotNull(result.Result.V3);
            Assert.AreEqual(2, result.Result.V3.Length);
            Assert.AreEqual(expectedVerifierV3_1, result.Result.V3[0]);
            Assert.AreEqual(expectedVerifierV3_2, result.Result.V3[1]);
            this.ValidateHttpRequestMessageContainsAuthorizationHeaders();
        }

        [TestMethod]
        public async Task ConstructExportFail()
        {
            var responseMessage = new HttpResponseMessage();
            responseMessage.StatusCode = HttpStatusCode.InternalServerError;
            responseMessage.Content = new StringContent("errormessage");
            string expectedVerifier = "expectedVerifier";
            responseMessage.Headers.Add("Verifier", new List<string> { expectedVerifier });
            this.mockHttpClient.Setup(c => c.SendAsync(It.Is<HttpRequestMessage>(m => m.Headers.Contains(HeaderNames.ClientRequestId)))).ReturnsAsync(responseMessage);

            AdapterResponse<AadRvsVerifiers> result = await this.adapter.ConstructExportAsync(new AadRvsRequest(), this.mockRequestContext).ConfigureAwait(false);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(result.Error.Message, "errormessage");
        }

        [TestMethod]
        public async Task ShouldNotConstructExportWithMsaSelfIdentitySuccess()
        {
            //Arrange
            var responseMessage = new HttpResponseMessage();
            string expectedVerifier = "expectedVerifier";
            responseMessage.Headers.Add("Verifier", new List<string> { expectedVerifier });
            this.mockRequestContext = new RequestContext(new MsaSiteIdentity(It.IsAny<string>(), It.IsAny<long>()), new Uri("https://unittest"), new Dictionary<string, string[]>());
            this.mockHttpClient.Setup(c => c.SendAsync(It.Is<HttpRequestMessage>(m => m.Headers.Contains(HeaderNames.ClientRequestId)))).ReturnsAsync(responseMessage);
            try
            {
                //Act
                AdapterResponse<AadRvsVerifiers> result = await this.adapter.ConstructExportAsync(new AadRvsRequest(), this.mockRequestContext).ConfigureAwait(false);
                Assert.Fail("Shouldn't get here");
            }
            catch (ArgumentOutOfRangeException ex)
            {
                //Assert
                Assert.AreEqual("Specified argument was out of the range of valid values.\r\nParameter name: Identity", ex.Message);
            }
        }

        [TestInitialize]
        public void TestInitialize()
        {
            this.mockHttpClient = new Mock<IHttpClient>();
            this.mockHttpClient.Setup(c => c.DefaultRequestHeaders).Returns(new HttpClient().DefaultRequestHeaders);
            this.mockConfiguration = new Mock<IAadRequestVerificationServiceAdapterConfiguration>();

            this.mockAadAuthManager = new Mock<IAadAuthManager>();
            this.mockCounterFactory = new Mock<ICounterFactory>(MockBehavior.Loose);
            var mockCounter = new Mock<ICounter>(MockBehavior.Loose);
            this.mockCounterFactory
                .Setup(c => c.GetCounter(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CounterType>()))
                .Returns(mockCounter.Object);

            this.mockConfiguration.SetupGet(c => c.BaseUrl).Returns("https://doesnotmatter.com");
            this.mockConfiguration.SetupGet(c => c.PartnerId).Returns("PartnerId");
            this.mockConfiguration.SetupGet(c => c.AadAppId).Returns("AadAppId");

            var identity = new AadIdentity(Guid.NewGuid().ToString(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "accessToken", "appDisplayName");
            this.mockRequestContext = new RequestContext(identity, new Uri("https://unittest"), new Dictionary<string, string[]>());

            HttpRequestHeaders requestHeaders = new HttpClient().DefaultRequestHeaders;
            requestHeaders.Add(HttpRequestHeader.Authorization.ToString(), AuthHeaderValueApp);

            this.mockAadAuthManager
                .Setup(m => m.SetAuthorizationHeaderProtectedForwardedTokenAsync(It.IsAny<HttpRequestHeaders>(), It.IsAny<string>(), It.IsAny<OutboundPolicyName>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask)
                .Callback((HttpRequestHeaders h, string at, OutboundPolicyName n, bool f) => h.Add(HttpRequestHeader.Authorization.ToString(), AuthHeaderValuePft));

            this.mockAadAuthManager
                .Setup(m => m.SetAuthorizationHeaderProtectedForwardedTokenAsync(It.IsAny<HttpRequestHeaders>(), It.IsAny<string>(), It.IsAny<IJwtOutboundPolicy>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask)
                .Callback((HttpRequestHeaders h, string at, IJwtOutboundPolicy p, bool f) => h.Add(HttpRequestHeader.Authorization.ToString(), AuthHeaderValuePft));

            this.mockAadAuthManager
                .Setup(m => m.SetAuthorizationHeaderAppTokenAsync(It.IsAny<HttpRequestHeaders>(), It.IsAny<IJwtOutboundPolicy>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask)
                .Callback((HttpRequestHeaders h, IJwtOutboundPolicy p, bool b, bool f) => h.Add(HttpRequestHeader.Authorization.ToString(), AuthHeaderValueApp));

            this.mockAadAuthManager
                .Setup(m => m.SetAuthorizationHeaderAppTokenAsync(It.IsAny<HttpRequestHeaders>(), It.IsAny<OutboundPolicyName>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask)
                .Callback((HttpRequestHeaders h, OutboundPolicyName p, bool b, bool f) => h.Add(HttpRequestHeader.Authorization.ToString(), AuthHeaderValueApp));

            this.adapter = new AadRequestVerificationServiceAdapter(
                this.mockHttpClient.Object,
                this.mockConfiguration.Object,
                this.mockAadAuthManager.Object,
                this.mockCounterFactory.Object);
        }

        [TestMethod]
        [DynamicData(nameof(GeneratePuidData), DynamicDataSourceType.Method)]
        public void TryGetOrgIdPuidWhenTokenWithDifferentInputs(string tokenString, string expectedPuid, bool expectedIsSuccess)
        {
            long.TryParse(expectedPuid, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out long parsedExpctedPuid);

            //Act
            var actualResult = this.adapter.TryGetOrgIdPuid(tokenString, out long orgIdPuidOutput);

            //Assert
            if (actualResult)
            {
                Assert.AreEqual(parsedExpctedPuid, orgIdPuidOutput);
            }

            Assert.AreEqual(expectedIsSuccess, actualResult);
        }

        [TestMethod]
        public async Task ConstructExportWithEmptyResponse()
        {
            //Arrange
            var responseMessage = new HttpResponseMessage();
            responseMessage.Headers.Add("Verifier", new List<string> { "dontcare" });
            responseMessage.Content = new StringContent(string.Empty);
            this.mockHttpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>())).ReturnsAsync(responseMessage);

            //Act
            AdapterResponse<AadRvsVerifiers> response = await this.adapter.ConstructExportAsync(new AadRvsRequest(), this.mockRequestContext).ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(response);
            Assert.IsTrue(response.IsSuccess);
        }

        [TestMethod]
        public async Task ConstructAccountCloseWithEmptyResponse()
        {
            //Arrange
            var responseMessage = new HttpResponseMessage();
            responseMessage.Headers.Add("Verifier", new List<string> { "dontcare" });
            responseMessage.Content = new StringContent(string.Empty);
            this.mockHttpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>())).ReturnsAsync(responseMessage);

            //Act
            var aadRvsRequest = new AadRvsRequest() { PreVerifier = "preverifier" };
            AdapterResponse<AadRvsVerifiers> response = await this.adapter.ConstructAccountCloseAsync(aadRvsRequest).ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(response);
            Assert.IsTrue(response.IsSuccess);
        }

        [TestMethod]
        [DynamicData(nameof(GenerateVerifierData), DynamicDataSourceType.Method)]
        public void UpdatePrivacyRequestWithVerifiersTest(string verfierV2, string[] verifierV3, TenantIdType tenantIdType, bool shouldSucceeded)
        {
            var request = new PrivacyRequest { Subject = new AadSubject { ObjectId = Guid.NewGuid(), TenantId = Guid.NewGuid() } };
            var verifier = new AadRvsVerifiers { V2 = verfierV2, V3 = verifierV3 };

            AdapterError error = this.adapter.UpdatePrivacyRequestWithVerifiers(request, verifier);

            if (shouldSucceeded)
            {
                Assert.IsNull(error);

                if (tenantIdType == TenantIdType.Home)
                {
                    Assert.IsInstanceOfType(request.Subject, typeof(AadSubject));
                    Assert.IsNotInstanceOfType(request.Subject, typeof(AadSubject2));

                    var aadSubject = request.Subject as AadSubject;
                    Assert.IsTrue(aadSubject.OrgIdPUID > 0);
                    Assert.IsTrue(string.IsNullOrEmpty(request.VerificationTokenV3));
                    Assert.AreEqual(verfierV2, request.VerificationToken);
                }
                else
                {
                    Assert.IsInstanceOfType(request.Subject, typeof(AadSubject2));

                    var aadSubject2 = request.Subject as AadSubject2;
                    Assert.IsTrue(aadSubject2.OrgIdPUID > 0);
                    Assert.AreEqual(tenantIdType, aadSubject2.TenantIdType);
                    Assert.AreEqual(verifierV3[0], request.VerificationTokenV3);
                    Assert.IsTrue(string.IsNullOrEmpty(request.VerificationToken));
                    Assert.AreEqual(Guid.Parse("7bdb2545-6702-490d-8d07-5cc0a5376dd9"), aadSubject2.HomeTenantId);
                }
            }
            else
            {
                Assert.IsNotNull(error);
            }
        }

        private void ValidateHttpRequestMessageContainsAuthorizationHeaders()
        {
            this.mockHttpClient
                .Verify(
                    c => c.SendAsync(It.Is<HttpRequestMessage>(message => message.Headers.Contains(HttpRequestHeader.Authorization.ToString()))),
                    Times.Once);
        }

        private void ValidateHttpRequestMessageAuthorizationScheme(string scheme)
        {
            this.mockHttpClient
                .Verify(
                    c => c.SendAsync(It.Is<HttpRequestMessage>(message => message.Headers.Authorization.Scheme.Contains(scheme))),
                    Times.Once);
        }
    }
}

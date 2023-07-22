// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.UnitTests.CustomerMasterAdapter
{
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.CustomerMasterAdapter;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.OSGS.HttpClientCommon;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Newtonsoft.Json;

    [TestClass]
    public class CMAdapterGetProfileTest : CMAdapterTestBase
    {
        [DataTestMethod]
        [DataRow(null, "/JarvisCM/me/profiles")]
        [DataRow("", "/JarvisCM/me/profiles")]
        [DataRow("family-jwt", "/JarvisCM/my-family/profiles")]
        public async Task GetPrivacyProfileAsync_Success(string familyJwt, string expectedPath)
        {
            var profileResponse = this.CreateGetPrivacyProfilesResponse();

            HttpRequestMessage httpRequestMessage = null;
            Mock<IHttpClient> httpClientMock;
            Mock<IPxfRequestContext> requestContextMock;
            ICustomerMasterAdapter adapter = this.ConfigureMockAdapterToSend(
                (u, t) => httpRequestMessage = u,
                () => profileResponse,
                out httpClientMock,
                out requestContextMock);

            requestContextMock.SetupGet(ctx => ctx.FamilyJsonWebToken).Returns(familyJwt);

            AdapterResponse<PrivacyProfile> response = await adapter.GetPrivacyProfileAsync(requestContextMock.Object).ConfigureAwait(false);

            Assert.IsNotNull(response);
            Assert.IsTrue(response.IsSuccess);
            Assert.IsNotNull(response.Result);

            var expectedProfileResponse = JsonConvert.DeserializeObject<PrivacyProfile>(profileResponse.Items.First().ToString());
            Assert.AreEqual(expectedProfileResponse.Advertising, response.Result.Advertising);
            Assert.AreEqual(expectedProfileResponse.TailoredExperiencesOffers, response.Result.TailoredExperiencesOffers);
            Assert.AreEqual(expectedProfileResponse.Type, response.Result.Type);
            Assert.AreEqual(expectedProfileResponse.ETag, response.Result.ETag);
            Assert.AreEqual(expectedProfileResponse.SharingState, response.Result.SharingState);

            Assert.AreEqual(expectedPath, httpRequestMessage.RequestUri.AbsolutePath);
        }

        /// <summary>
        /// Test null response -> AdapterErrorCode.EmptyResponse
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task GetPrivacyProfileAsync_NullResponseError()
        {
            HttpRequestMessage httpRequestMessage = null;
            Mock<IHttpClient> httpClientMock;
            Mock<IPxfRequestContext> requestContextMock;
            var adapter = this.ConfigureMockAdapterToSend(
                (u, t) => httpRequestMessage = u,
                new HttpResponseMessage(HttpStatusCode.OK), 
                out httpClientMock,
                out requestContextMock);

            var response = await adapter.GetPrivacyProfileAsync(requestContextMock.Object).ConfigureAwait(false);

            Assert.IsNotNull(response);
            Assert.IsFalse(response.IsSuccess);
            Assert.IsNotNull(response.Error);
            Assert.AreEqual(AdapterErrorCode.EmptyResponse, response.Error.Code);
        }

        /// <summary>
        /// Test json deserialiation failure -> AdapterErrorCode.JsonDeserializationFailure
        /// </summary>
        [TestMethod]
        public async Task GetPrivacyProfileAsync_InvalidJsonError()
        {
            HttpRequestMessage httpRequestMessage = null;
            Mock<IHttpClient> httpClientMock;
            Mock<IPxfRequestContext> requestContextMock;
            var adapter = this.ConfigureMockAdapterToSend(
                (u, t) => httpRequestMessage = u,
                new HttpResponseMessage(HttpStatusCode.OK) { Content = this.CreateHttpContent("this is not json") }, 
                out httpClientMock,
                out requestContextMock);

            var response = await adapter.GetPrivacyProfileAsync(requestContextMock.Object).ConfigureAwait(false);

            Assert.IsNotNull(response);
            Assert.IsFalse(response.IsSuccess);
            Assert.IsNotNull(response.Error);
            Assert.AreEqual(AdapterErrorCode.JsonDeserializationFailure, response.Error.Code);
        }

        /// <summary>
        /// Test unauthorized failure -> AdapterErrorCode.Unauthorized
        /// </summary>
        [TestMethod]
        public async Task GetPrivacyProfileAsync_PartnerAuthError()
        {
            HttpRequestMessage httpRequestMessage = null;
            Mock<IHttpClient> httpClientMock;
            Mock<IPxfRequestContext> requestContextMock;
            var adapter = this.ConfigureMockAdapterToSend(
                (u, t) => httpRequestMessage = u,
                new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = this.CreateHttpContent("this is a partner custom error message, not json") }, 
                out httpClientMock,
                out requestContextMock);

            var response = await adapter.GetPrivacyProfileAsync(requestContextMock.Object).ConfigureAwait(false);

            Assert.IsNotNull(response);
            Assert.IsFalse(response.IsSuccess);
            Assert.IsNotNull(response.Error);
            Assert.AreEqual(AdapterErrorCode.Unauthorized, response.Error.Code);
        }

        /// <summary>
        /// Test unknown failure -> AdapterErrorCode.Unknown
        /// </summary>
        [TestMethod]
        public async Task GetPrivacyProfileAsync_UnknownPartnerError()
        {
            HttpRequestMessage httpRequestMessage = null;
            Mock<IHttpClient> httpClientMock;
            Mock<IPxfRequestContext> requestContextMock;
            var adapter = this.ConfigureMockAdapterToSend(
                (u, t) => httpRequestMessage = u,
                new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = this.CreateHttpContent("this is a partner custom error message, not json") }, 
                out httpClientMock,
                out requestContextMock);

            var response = await adapter.GetPrivacyProfileAsync(requestContextMock.Object).ConfigureAwait(false);

            Assert.IsNotNull(response);
            Assert.IsFalse(response.IsSuccess);
            Assert.IsNotNull(response.Error);
            Assert.AreEqual(AdapterErrorCode.Unknown, response.Error.Code);
        }
    }
}
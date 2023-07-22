// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.UnitTests.CustomerMasterAdapter
{
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.OSGS.HttpClientCommon;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Newtonsoft.Json.Linq;

    [TestClass]
    public class CMAdapterGetProfileJObjectTest : CMAdapterTestBase
    {
        [TestMethod]
        public async Task GetPrivacyProfileJObjectAsync_Success()
        {
            var profileResponse = this.CreateGetPrivacyProfilesResponse();

            HttpRequestMessage httpRequestMessage = null;
            Mock<IHttpClient> httpClientMock;
            Mock<IPxfRequestContext> requestContextMock;
            var adapter = this.ConfigureMockAdapterToSend(
                (u, t) => httpRequestMessage = u,
                () => profileResponse,
                out httpClientMock,
                out requestContextMock);

            var response = await adapter.GetPrivacyProfileJObjectAsync(requestContextMock.Object);

            Assert.IsNotNull(response);
            Assert.IsTrue(response.IsSuccess);
            Assert.IsNotNull(response.Result);

            var expectedProfileResponse = profileResponse.Items.First();

            Assert.AreEqual(expectedProfileResponse.ToString(), response.Result.ToString());
        }

        [TestMethod]
        public async Task GetPrivacyProfileJObjectAsync_ContainsNoPrivacyProfile()
        {
            var profileResponse = this.CreateGetPrivacyProfilesResponse(numberItems: 0);

            HttpRequestMessage httpRequestMessage = null;
            Mock<IHttpClient> httpClientMock;
            Mock<IPxfRequestContext> requestContextMock;
            var adapter = this.ConfigureMockAdapterToSend(
                (u, t) => httpRequestMessage = u,
                () => profileResponse, 
                out httpClientMock,
                out requestContextMock);

            var response = await adapter.GetPrivacyProfileJObjectAsync(requestContextMock.Object);

            Assert.IsNotNull(response);
            Assert.IsTrue(response.IsSuccess);
            Assert.IsNull(response.Error);
            Assert.IsNotNull(response.Result);

            Assert.AreEqual(new JObject().ToString(), response.Result.ToString());
        }
    }
}
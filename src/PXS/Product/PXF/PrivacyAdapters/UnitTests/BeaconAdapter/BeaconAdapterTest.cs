// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.UnitTests.BeaconAdapter
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.DataContracts.V2;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.BeaconAdapter;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.OSGS.HttpClientCommon;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;
    using Newtonsoft.Json;

    [TestClass]
    public class BeaconAdapterTest
    {
        private Mock<IAadTokenProvider> aadTokenProvider;
        private Mock<ILogger> logger;
        private Mock<IPxfPartnerConfiguration> partnerConfiguration;
        private PxfRequestContext requestContext;


        private const string RawHttpClientResponse = "{\"items\":[{\"dateTime\":\"2020-05-15T16:36:01.5924154+00:00\",\"location\":{\"latitude\":1.0,\"longitude\":2.0,\"altitude\":0.0},\"accuracyRadius\":0.0},{\"dateTime\":\"2020-05-15T16:36:01.5924154+00:00\",\"location\":{\"latitude\":1.1,\"longitude\":2.1,\"altitude\":0.0},\"accuracyRadius\":0.0}],\"@nextLink\":\"https://dev.cortana.ai/userlocation/v1/my/LocationHistory?$maxpagesize=100&endTime=2020-01-01T00:00:00.000Z\"}";
        private const string EmptyRawHttpClientResponse = "{\"items\":[]}";
        private const string BeaconPartnerId = "BeaconAPI_UserLocationTransit";
        private const string BeaconBaseUrl = "https://dev.cortana.ai/userlocation/";
        private const string ExpectedUrl = "https://dev.cortana.ai/userlocation/v1/my/locationhistory";
        private const string AadResource = "https://cortana.ai/api";
        private const string AadScope = "Bing.Cortana-Internal.ReadWrite";
        private const string UserProxyTicket = "userProxyTicket";
        private const long Puid = 12345;

        private readonly Regex popTokenPattern = new Regex("(popToken=\")(.*)(\", type=\"AT_POP\")", RegexOptions.Compiled);

        [TestInitialize]
        public void Initialize()
        {
            this.partnerConfiguration = new Mock<IPxfPartnerConfiguration>();
            this.partnerConfiguration.SetupGet(c => c.AuthenticationType).Returns(AuthenticationType.AadPopToken);
            this.partnerConfiguration.SetupGet(c => c.BaseUrl).Returns(BeaconBaseUrl);
            this.partnerConfiguration.SetupGet(c => c.PartnerId).Returns(BeaconPartnerId);
            this.partnerConfiguration.SetupGet(c => c.PxfAdapterVersion).Returns(AdapterVersion.BeaconV1);
            this.partnerConfiguration.SetupGet(c => c.AadTokenResourceId).Returns(AadResource);
            this.partnerConfiguration.SetupGet(c => c.AadTokenScope).Returns(AadScope);

            this.logger = new Mock<ILogger>();

            // Create custom pop token
            this.aadTokenProvider = new Mock<IAadTokenProvider>();
            this.aadTokenProvider
                .Setup(o => o.GetPopTokenAsync(It.IsAny<AadPopTokenRequest>(), It.IsAny<CancellationToken>()))
                .Returns((AadPopTokenRequest req, CancellationToken cancel) =>
                    Task.FromResult(JsonConvert.SerializeObject(req)));

            this.partnerConfiguration
                .SetupGet(x => x.PartnerId)
                .Returns(BeaconPartnerId);

            this.requestContext = new PxfRequestContext(UserProxyTicket, null, Puid, Puid, 123, "country/region", false, new string[0]);
        }

        [TestMethod]
        public async Task BeaconAdapterGetLocationHistorySuccessful()
        {
            // Arrange
            HttpRequestMessage resultHttpRequest = null;
            var httpClient = new Mock<IHttpClient>();
            httpClient
                .Setup(m => m.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<HttpCompletionOption>()))
                .Callback((HttpRequestMessage req, HttpCompletionOption op) => resultHttpRequest = req)
                .Returns(Task.FromResult(
                    new HttpResponseMessage()
                    {
                        Content = new StringContent(RawHttpClientResponse)
                    }));

            var beaconAdapter = new BeaconAdapter(httpClient.Object, this.partnerConfiguration.Object, this.aadTokenProvider.Object, this.logger.Object);
            var expected = JsonConvert.DeserializeObject<PagedResponseV2<LocationResourceV2>>(RawHttpClientResponse);

            // Act
            PagedResponse<LocationResource> result = await beaconAdapter.GetLocationHistoryAsync(this.requestContext, OrderByType.DateTime);

            // Assert
            // Assert payload
            Assert.AreEqual(BeaconPartnerId, result.PartnerId);
            Assert.AreEqual(expected.Items.Count(), result.Items.Count);
            Assert.AreEqual(expected.NextLink.AbsoluteUri, result.NextLink.AbsoluteUri);

            // Assert HttpRequest
            Assert.IsNotNull(resultHttpRequest);
            Assert.AreEqual("application/json", resultHttpRequest.Headers.Accept.FirstOrDefault().MediaType);
            Assert.AreEqual("MSAuth1.0", resultHttpRequest.Headers.Authorization.Scheme);

            string resultPop = resultHttpRequest.Headers.Authorization.Parameter;
            Assert.IsTrue(popTokenPattern.IsMatch(resultPop));
            string tokenStr = this.popTokenPattern.Match(resultPop).Groups[2].Value;
            AadPopTokenRequest token = JsonConvert.DeserializeObject<AadPopTokenRequest>(tokenStr);

            Assert.AreEqual(UserProxyTicket, token.Claims["msa_pt"]);
            Assert.AreEqual(ExpectedUrl, token.RequestUri.OriginalString);
            Assert.AreEqual("GET", token.HttpMethod.Method);
            Assert.AreEqual(AadResource, token.Resource);
            Assert.AreEqual(AadScope, token.Scope);
        }

        [TestMethod]
        public async Task BeaconAdapterGetEmptyLocationHistorySuccessful()
        {
            // Arrange
            HttpRequestMessage resultHttpRequest = null;
            var httpClient = new Mock<IHttpClient>();
            httpClient
                .Setup(m => m.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<HttpCompletionOption>()))
                .Callback((HttpRequestMessage req, HttpCompletionOption op) => resultHttpRequest = req)
                .Returns(Task.FromResult(
                    new HttpResponseMessage()
                    {
                        Content = new StringContent(EmptyRawHttpClientResponse)
                    }));

            var beaconAdapter = new BeaconAdapter(httpClient.Object, this.partnerConfiguration.Object, this.aadTokenProvider.Object, this.logger.Object);

            // Act
            PagedResponse<LocationResource> result = await beaconAdapter.GetLocationHistoryAsync(this.requestContext, OrderByType.DateTime);

            // Assert
            // Assert payload
            Assert.AreEqual(BeaconPartnerId, result.PartnerId);
            Assert.AreEqual(0, result.Items.Count);
            Assert.AreEqual(null, result.NextLink);

            // Assert HttpRequest
            Assert.IsNotNull(resultHttpRequest);
            Assert.AreEqual("application/json", resultHttpRequest.Headers.Accept.FirstOrDefault().MediaType);
            Assert.AreEqual("MSAuth1.0", resultHttpRequest.Headers.Authorization.Scheme);

            string resultPop = resultHttpRequest.Headers.Authorization.Parameter;
            Assert.IsTrue(popTokenPattern.IsMatch(resultPop));
            string tokenStr = this.popTokenPattern.Match(resultPop).Groups[2].Value;
            AadPopTokenRequest token = JsonConvert.DeserializeObject<AadPopTokenRequest>(tokenStr);

            Assert.AreEqual(UserProxyTicket, token.Claims["msa_pt"]);
            Assert.AreEqual(ExpectedUrl, token.RequestUri.OriginalString);
            Assert.AreEqual("GET", token.HttpMethod.Method);
            Assert.AreEqual(AadResource, token.Resource);
            Assert.AreEqual(AadScope, token.Scope);
        }

        [TestMethod]
        public async Task BeaconAdapterGetNextLocationPageAsyncSuccessful()
        {
            // Arrange
            HttpRequestMessage resultHttpRequest = null;
            var httpClient = new Mock<IHttpClient>();
            httpClient
                .Setup(m => m.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<HttpCompletionOption>()))
                .Callback((HttpRequestMessage req, HttpCompletionOption op) => resultHttpRequest = req)
                .Returns(Task.FromResult(
                    new HttpResponseMessage()
                    {
                        Content = new StringContent(RawHttpClientResponse)
                    }));

            var beaconAdapter = new BeaconAdapter(httpClient.Object, this.partnerConfiguration.Object, this.aadTokenProvider.Object, this.logger.Object);
            var expected = JsonConvert.DeserializeObject<PagedResponseV2<LocationResourceV2>>(RawHttpClientResponse);

            // Act
            PagedResponse<LocationResource> result = await beaconAdapter.GetNextLocationPageAsync(this.requestContext, new Uri(BeaconBaseUrl));

            // Assert
            // Assert payload
            Assert.AreEqual(BeaconPartnerId, result.PartnerId);
            Assert.AreEqual(expected.Items.Count(), result.Items.Count);
            Assert.AreEqual(expected.NextLink.AbsoluteUri, result.NextLink.AbsoluteUri);

            // Assert HttpRequest
            Assert.IsNotNull(resultHttpRequest);
            Assert.AreEqual("application/json", resultHttpRequest.Headers.Accept.FirstOrDefault().MediaType);
            Assert.AreEqual("MSAuth1.0", resultHttpRequest.Headers.Authorization.Scheme);

            string resultPop = resultHttpRequest.Headers.Authorization.Parameter;
            Assert.IsTrue(popTokenPattern.IsMatch(resultPop));
            string tokenStr = this.popTokenPattern.Match(resultPop).Groups[2].Value;
            AadPopTokenRequest token = JsonConvert.DeserializeObject<AadPopTokenRequest>(tokenStr);

            Assert.AreEqual(UserProxyTicket, token.Claims["msa_pt"]);
            Assert.AreEqual(BeaconBaseUrl, token.RequestUri.OriginalString);
            Assert.AreEqual("GET", token.HttpMethod.Method);
            Assert.AreEqual(AadResource, token.Resource);
            Assert.AreEqual(AadScope, token.Scope);
        }
    }
}

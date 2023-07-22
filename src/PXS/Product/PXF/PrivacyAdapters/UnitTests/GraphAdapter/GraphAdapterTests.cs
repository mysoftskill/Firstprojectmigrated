// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.UnitTests.GraphAdapter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Factory;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Graph;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.OSGS.HttpClientCommon;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Newtonsoft.Json;

    [TestClass]
    public class GraphAdapterTests
    {
        private Mock<IAadAuthManager> authManager;

        private Mock<IGraphAdapterConfiguration> configuration;

        private Mock<ICounterFactory> counterFactory;

        private IGraphAdapter graphAdapter;

        private Mock<IHttpClient> httpClient;

        [TestMethod]
        public async Task GetDirectoryRoleMembersSuccess()
        {
            var objectId1 = Guid.NewGuid();
            var objectId2 = Guid.NewGuid();

            HttpResponseMessage expectedResponseMessage = new HttpResponseMessage();
            GetDirectoryRoleMemberResponse getDirectoryRoleMemberResponse = new GetDirectoryRoleMemberResponse
            {
                Value = new List<DirectoryRoleMember>
                {
                    new DirectoryRoleMember
                    {
                        Url = $"https://graph.windows.net/myorganization/directoryObjects/{objectId1}/Microsoft.DirectoryServices.User"
                    },
                    new DirectoryRoleMember
                    {
                        Url = $"https://graph.windows.net/myorganization/directoryObjects/{objectId2}/Microsoft.DirectoryServices.User"
                    }
                }
            };

            expectedResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(getDirectoryRoleMemberResponse));
            this.httpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>())).Returns(Task.FromResult(expectedResponseMessage));
            var actualResult = await this.graphAdapter.GetDirectoryRoleMembersAsync(Guid.NewGuid(), Guid.NewGuid()).ConfigureAwait(false);

            Assert.IsTrue(actualResult.IsSuccess);
            Assert.IsNotNull(actualResult.Result);
            Assert.AreEqual(2, actualResult.Result.Value.Count);
            Assert.IsTrue(actualResult.Result.Value.Any(member => member.Url.Contains(objectId1.ToString())));
            Assert.IsTrue(actualResult.Result.Value.Any(member => member.Url.Contains(objectId2.ToString())));
        }

        [TestMethod]
        public async Task GetDirectoryRolesSuccess()
        {
            var roleTemplateId = Guid.NewGuid();
            var roleObjectId = Guid.NewGuid();

            HttpResponseMessage expectedResponseMessage = new HttpResponseMessage();
            GetDirectoryRolesResponse getDirectoryRolesResponse = new GetDirectoryRolesResponse
            {
                Value = new List<DirectoryRole>
                {
                    new DirectoryRole
                    {
                        ObjectId = roleObjectId.ToString(),
                        DisplayName = "Company Administrator",
                        RoleTemplateId = roleTemplateId.ToString()
                    }
                }
            };

            expectedResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(getDirectoryRolesResponse));
            this.httpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>())).Returns(Task.FromResult(expectedResponseMessage));
            var actualResult = await this.graphAdapter.GetDirectoryRolesAsync(Guid.NewGuid(), Guid.NewGuid()).ConfigureAwait(false);

            Assert.IsTrue(actualResult.IsSuccess);
            Assert.IsNotNull(actualResult.Result);
            Assert.AreEqual(1, actualResult.Result.Value.Count);
            Assert.AreEqual(roleObjectId.ToString(), actualResult.Result.Value[0].ObjectId);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        [DynamicData(nameof(GraphAdapterConstructorTestData), DynamicDataSourceType.Method)]
        public void GraphAdapterNullHandlingSuccess(
            IPrivacyConfigurationManager configurationManager,
            IHttpClientFactory httpClientFactory,
            ICounterFactory counterFactory,
            IAadAuthManager authManager)

        {
            //Act
            new GraphAdapter(configurationManager, httpClientFactory, counterFactory, authManager);
        }

        [TestInitialize]
        public void Initialize()
        {
            this.configuration = CreateMockGraphAdapterConfiguration();

            Mock<IPrivacyConfigurationManager> configManager = CreateMockConfigurationManager(this.configuration);

            this.httpClient = new Mock<IHttpClient>();
            Mock<IHttpClientFactory> clientFactory = new Mock<IHttpClientFactory>();
            clientFactory.Setup(f => f.CreateHttpClient(It.IsAny<IPrivacyPartnerAdapterConfiguration>(), It.IsAny<ICounterFactory>()))
                .Returns(this.httpClient.Object);

            this.authManager = CreateMockAuthManager();

            this.counterFactory = CreateMockCounterFactory();

            this.graphAdapter = new GraphAdapter(configManager.Object, clientFactory.Object, this.counterFactory.Object, this.authManager.Object);
        }

        [TestMethod]
        public async Task IsMemberOfFail()
        {
            HttpResponseMessage expectedResponseMessage = new HttpResponseMessage();
            expectedResponseMessage.StatusCode = HttpStatusCode.InternalServerError;
            expectedResponseMessage.Content = new StringContent("errormessage");
            this.httpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>())).Returns(Task.FromResult(expectedResponseMessage));
            var actualResult = await this.graphAdapter.IsMemberOfAsync(Guid.NewGuid(), Guid.NewGuid()).ConfigureAwait(false);

            Assert.IsFalse(actualResult.IsSuccess);
            Assert.IsNotNull(actualResult.Error);
            Assert.AreEqual(AdapterErrorCode.Unknown, actualResult.Error.Code);
            Assert.AreEqual(actualResult.Error.Message, "errormessage");
        }

        [TestMethod]
        public async Task IsMemberOfSuccess()
        {
            HttpResponseMessage expectedResponseMessage = new HttpResponseMessage();
            IsMemberOfResponse expectedReponse = new IsMemberOfResponse
            {
                Value = true
            };
            expectedResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(expectedReponse));
            this.httpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>())).Returns(Task.FromResult(expectedResponseMessage));
            var actualResult = await this.graphAdapter.IsMemberOfAsync(Guid.NewGuid(), Guid.NewGuid()).ConfigureAwait(false);

            Assert.IsTrue(actualResult.IsSuccess);
            Assert.IsNotNull(actualResult.Result);
            Assert.IsTrue(actualResult.Result.Value);
        }

        #region Test Data

        private static Mock<IGraphAdapterConfiguration> CreateMockGraphAdapterConfiguration()
        {
            var mockGraphAdapter = new Mock<IGraphAdapterConfiguration>();
            mockGraphAdapter.SetupGet(c => c.BaseUrl).Returns("https://doesnotmatter.com");
            mockGraphAdapter.SetupGet(c => c.PartnerId).Returns("doesnotmatter");
            mockGraphAdapter.SetupGet(c => c.AadGraphResource).Returns("I'maresouce");
            return mockGraphAdapter;
        }

        public static IEnumerable<object[]> GraphAdapterConstructorTestData()
        {
            var mockAdapterConfig = CreateMockGraphAdapterConfiguration();
            var mockConfigurationManager = CreateMockConfigurationManager(mockAdapterConfig);

            var mockConfigurationManagerNull = new Mock<IPrivacyConfigurationManager>();
            mockConfigurationManagerNull
                .SetupGet(m => m.AdaptersConfiguration)
                .Returns(new Mock<IAdaptersConfiguration>().Object);

            var mockHttpClientFactory = CreateMockHttpClientFactory();
            var mockCounterFactory = CreateMockCounterFactory();
            var mockAuthManager = CreateMockAuthManager();

            var data = new List<object[]>
            {
                new object[]
                {
                    mockConfigurationManager.Object,
                    null,
                    mockCounterFactory.Object,
                    mockAuthManager.Object
                },
                new object[]
                {
                    mockConfigurationManager.Object,
                    mockHttpClientFactory.Object,
                    null,
                    mockAuthManager.Object
                },
                new object[]
                {
                    mockConfigurationManagerNull.Object,
                    mockHttpClientFactory.Object,
                    mockCounterFactory.Object,
                    mockAuthManager.Object
                },
                new object[]
                {
                    mockConfigurationManager.Object,
                    mockHttpClientFactory.Object,
                    mockCounterFactory.Object,
                    null
                }
            };
            return data;
        }

        private static Mock<IPrivacyConfigurationManager> CreateMockConfigurationManager(Mock<IGraphAdapterConfiguration> configuration)
        {
            Mock<IAdaptersConfiguration> adapterConfig = new Mock<IAdaptersConfiguration>();
            adapterConfig.SetupGet(c => c.GraphAdapterConfiguration).Returns(configuration.Object);

            var configManager = new Mock<IPrivacyConfigurationManager>();
            configManager.SetupGet(m => m.AdaptersConfiguration).Returns(adapterConfig.Object);

            return configManager;
        }

        private static Mock<IHttpClientFactory> CreateMockHttpClientFactory()
        {
            return new Mock<IHttpClientFactory>();
        }

        private static Mock<ICounterFactory> CreateMockCounterFactory()
        {
            return new Mock<ICounterFactory>();
        }

        private static Mock<IAadAuthManager> CreateMockAuthManager()
        {
            var mockAuthManager = new Mock<IAadAuthManager>();
            mockAuthManager.Setup(m => m.GetAccessTokenAsync(It.IsAny<string>())).Returns(Task.FromResult("I'manaccesstoken"));
            return mockAuthManager;
        }

        #endregion
    }
}

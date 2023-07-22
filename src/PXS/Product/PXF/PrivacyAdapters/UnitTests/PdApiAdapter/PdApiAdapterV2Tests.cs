// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.UnitTests.PdApiAdapterV2
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Spatial;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.DataContracts.V2;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Converters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.V2;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common;
    using Microsoft.OSGS.HttpClientCommon;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Windows.Services.AuthN.Client.S2S;

    using Moq;

    using Newtonsoft.Json;

    using Microsoft.PrivacyServices.Common.Azure;

    [TestClass]
    public class PdApiAdapterV2Tests
    {
        private readonly string innerTokenResult = "IBETOKENIZED!";

        private readonly Policy policy = Policies.Current;

        private Mock<IAadTokenProvider> aadTokenProvider;

        private Mock<ICertificateProvider> certProvider;

        private Mock<IHttpClient> httpClient;

        private Mock<ILogger> logger;

        private Mock<IPxfPartnerConfiguration> partnerConfig;

        private PdApiAdapterV2 pdApiAdapterV2;

        private Mock<PxfRequestContext> requestContext;

        private HttpResponseMessage response;

        private Mock<IS2SAuthClient> s2SAuthClient;

        [TestInitialize]
        public void Initialize()
        {
            this.response = new HttpResponseMessage(HttpStatusCode.OK);
            this.s2SAuthClient = new Mock<IS2SAuthClient>();
            this.partnerConfig = new Mock<IPxfPartnerConfiguration>();
            this.partnerConfig.SetupGet(c => c.AuthenticationType).Returns(AuthenticationType.AadPopToken);
            this.partnerConfig.SetupGet(c => c.BaseUrl).Returns("https://doesnotmatter.com");
            this.partnerConfig.SetupGet(c => c.PartnerId).Returns("doesnotmatter");
            this.partnerConfig.SetupGet(c => c.CustomHeaders).Returns(
                new Dictionary<string, string>
                {
                    { "key1", "value1" },
                    { "customheader2", "customvalue2" }
                });

            this.certProvider = new Mock<ICertificateProvider>();
            this.logger = new Mock<ILogger>();

            this.aadTokenProvider = new Mock<IAadTokenProvider>();
            this.aadTokenProvider
                .Setup(o => o.GetPopTokenAsync(It.IsAny<AadPopTokenRequest>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(this.innerTokenResult));

            this.httpClient = new Mock<IHttpClient>();
            this.httpClient.Setup(m => m.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<HttpCompletionOption>())).Returns(Task.FromResult(this.response));

            this.requestContext = new Mock<PxfRequestContext>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<long>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                true,
                It.IsAny<string[]>());

            this.pdApiAdapterV2 = new PdApiAdapterV2(
                this.httpClient.Object,
                this.s2SAuthClient.Object,
                this.partnerConfig.Object,
                this.certProvider.Object,
                this.aadTokenProvider.Object,
                this.logger.Object);
        }

        [TestMethod]
        public async Task WarmupAsyncSuccess()
        {
            var appUsageResource = this.GeneratePagedResponseAppUsageResource();
            this.response.Content = new StringContent(JsonConvert.SerializeObject(appUsageResource));
            await this.pdApiAdapterV2.WarmupAsync(this.requestContext.Object, ResourceType.AppUsage);

            var browseResources = this.GeneratePagedResponseBrowseResource();
            this.response.Content = new StringContent(JsonConvert.SerializeObject(browseResources));
            await this.pdApiAdapterV2.WarmupAsync(this.requestContext.Object, ResourceType.Browse);

            var contentConsumption = this.GeneratePagedResponseContentConsumptionResourceV2();
            this.response.Content = new StringContent(JsonConvert.SerializeObject(contentConsumption));
            await this.pdApiAdapterV2.WarmupAsync(this.requestContext.Object, ResourceType.ContentConsumption);

            var locationResource = this.GeneratePagedResponseLocationResourceV2();
            this.response.Content = new StringContent(JsonConvert.SerializeObject(locationResource));
            await this.pdApiAdapterV2.WarmupAsync(this.requestContext.Object, ResourceType.Location);
            await this.pdApiAdapterV2.WarmupAsync(this.requestContext.Object, ResourceType.MicrosoftHealthLocation);

            var searchResource = this.GeneratePagedResponseSearchResourceV2();
            this.response.Content = new StringContent(JsonConvert.SerializeObject(searchResource));
            await this.pdApiAdapterV2.WarmupAsync(this.requestContext.Object, ResourceType.Search);

            var voiceResource = this.GeneratePagedResponseVoiceResource();
            this.response.Content = new StringContent(JsonConvert.SerializeObject(voiceResource));
            await this.pdApiAdapterV2.WarmupAsync(this.requestContext.Object, ResourceType.Voice);
        }

        #region Browse

        [TestMethod]
        public async Task GetBrowseHistoryAsyncSuccess()
        {
            var pagedResponse = this.GeneratePagedResponseBrowseResource();

            this.response.Content = new StringContent(JsonConvert.SerializeObject(pagedResponse));

            //Act
            var actualResult = await this.pdApiAdapterV2.GetBrowseHistoryAsync(
                this.requestContext.Object,
                OrderByType.DateTime,
                DateOption.Between,
                DateTime.Now,
                DateTime.Now.AddDays(10),
                "bing").ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(actualResult);
            Assert.AreEqual(this.partnerConfig.Object.PartnerId, actualResult.PartnerId);
            EqualityHelper.AreEqual(pagedResponse, actualResult);
        }

        [TestMethod]
        public async Task GetBrowseHistoryAsyncTokenSuccess()
        {
            var pagedResponse = this.GeneratePagedResponseBrowseResource();

            //Act
            this.response.Content = new StringContent(JsonConvert.SerializeObject(pagedResponse));
            var actualResult = await this.pdApiAdapterV2.GetBrowseHistoryAsync(
                this.requestContext.Object,
                OrderByType.DateTime,
                DateOption.Between,
                DateTime.Now,
                DateTime.Now.AddDays(10),
                "bing").ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(actualResult);
            Assert.AreEqual(this.partnerConfig.Object.PartnerId, actualResult.PartnerId);
            EqualityHelper.AreEqual(pagedResponse, actualResult);
        }

        [TestMethod]
        public async Task GetBrowseHistoryAsyncFail()
        {
            var pagedResponse = this.GeneratePagedResponseBrowseResource();
            this.response.Content = new StringContent(JsonConvert.SerializeObject(pagedResponse));

            try
            {
                //Act
                await this.pdApiAdapterV2.GetBrowseHistoryAsync(
                    this.requestContext.Object,
                    OrderByType.SearchTerms,
                    DateOption.Between,
                    DateTime.Now,
                    DateTime.Now.AddDays(10),
                    "bing").ConfigureAwait(false);

                //Assert
                Assert.Fail("Should never get here because we should have thrown");
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            try
            {
                //Act
                await this.pdApiAdapterV2.GetBrowseHistoryAsync(
                    this.requestContext.Object,
                    OrderByType.DateTime,
                    DateOption.SingleDay,
                    DateTime.Now,
                    DateTime.Now.AddDays(10),
                    "bing").ConfigureAwait(false);

                //Assert
                Assert.Fail("Should never get here because we should have thrown");
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }

        [TestMethod]
        public async Task DeleteBrowseHistoryAsyncSuccess()
        {
            //Arrange
            var deleteResponse = new DeleteResourceResponse
            {
                Status = ResourceStatus.Deleted
            };
            var browseV2Delete = new List<BrowseV2Delete>
            {
                new BrowseV2Delete("https://test.com", DateTimeOffset.UtcNow),
                new BrowseV2Delete("https://test.com", DateTimeOffset.UtcNow.AddDays(1))
            };

            //Act
            this.response.Content = new StringContent(JsonConvert.SerializeObject(deleteResponse));
            var actualResult = await this.pdApiAdapterV2.DeleteBrowseHistoryAsync(this.requestContext.Object, browseV2Delete.ToArray()).ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(actualResult);
            Assert.IsNull(actualResult.ErrorMessage);
            Assert.AreEqual(deleteResponse.Status, actualResult.Status);

            //Act
            actualResult = await this.pdApiAdapterV2.DeleteBrowseHistoryAsync(this.requestContext.Object).ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(actualResult);
            Assert.IsNull(actualResult.ErrorMessage);
            Assert.AreEqual(deleteResponse.Status, actualResult.Status);
        }

        [TestMethod]
        public async Task DeleteBrowseHistoryAsyncFail()
        {
            //Arrange
            var deleteResponse = new DeleteResourceResponse
            {
                Status = ResourceStatus.Error
            };
            var browseV2Delete = new List<BrowseV2Delete>
            {
                new BrowseV2Delete("https://test.com", DateTimeOffset.UtcNow),
                new BrowseV2Delete("https://test.com", DateTimeOffset.UtcNow.AddDays(1))
            };

            //Act
            this.response.Content = new StringContent(JsonConvert.SerializeObject(deleteResponse));
            var actualResult = await this.pdApiAdapterV2.DeleteBrowseHistoryAsync(this.requestContext.Object, browseV2Delete.ToArray()).ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(actualResult);
            Assert.IsNotNull(actualResult.Status);
            Assert.AreNotEqual(deleteResponse.Status, actualResult.Status);
        }

        [TestMethod]
        public async Task GetNextBrowsePageAsyncSuccess()
        {
            //Arrange
            Uri nextUri = new Uri("https://www.nextUri.com");
            var pagedResponse = this.GeneratePagedResponseBrowseResource();
            this.response.Content = new StringContent(JsonConvert.SerializeObject(pagedResponse));

            //Act
            var actualResult = await this.pdApiAdapterV2.GetNextBrowsePageAsync(this.requestContext.Object, nextUri).ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(actualResult);
            Assert.AreEqual(1, pagedResponse.Items.Count());
            Assert.AreEqual(pagedResponse.Items.First().NavigatedToUrl, actualResult.NextLink);
            EqualityHelper.AreEqual(pagedResponse, actualResult);
        }

        [TestMethod]
        public async Task GetNextBrowsePageAsyncFail()
        {
            //Arrange
            Uri nextUri = new Uri("https://www.nextUri.com");
            this.response.StatusCode = HttpStatusCode.InternalServerError;
            this.response.Content = new StringContent("errormessage");
            this.httpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>())).Returns(Task.FromResult(this.response));

            try
            {
                //Act
                var actualResult = await this.pdApiAdapterV2.GetNextBrowsePageAsync(this.requestContext.Object, nextUri).ConfigureAwait(false);

                //Assert
                Assert.Fail("Should never get here because we should have thrown");
            }
            catch (PxfAdapterException)
            {
                //Expected
            }
        }

        [TestMethod]
        public async Task GetNextSearchPageAsyncSuccess()
        {
            //Arrange
            Uri nextUri = new Uri("https://www.nextUri.com");
            var pagedResponse = this.GeneratePagedResponseSearchResourceV2();
            var expected = pagedResponse.Items.Select(x => x.ToSeachResource(this.partnerConfig.Object.PartnerId)).ToList();
            this.response.Content = new StringContent(JsonConvert.SerializeObject(pagedResponse));

            //Act
            var actualResult = await this.pdApiAdapterV2.GetNextSearchPageAsync(this.requestContext.Object, nextUri).ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(actualResult);
            Assert.AreEqual(1, pagedResponse.Items.Count());
            EqualityHelper.AreEqual(expected, actualResult.Items);
        }

        [TestMethod]
        public async Task GetBrowseAggregateCountSuccess()
        {
            var response = new CountResourceResponse()
            {
                Count = 6
            };
            // Overwrites the response object
            this.response.Content = new StringContent(JsonConvert.SerializeObject(response));

            //Act
            var actualResult = await this.pdApiAdapterV2.GetBrowseHistoryAggregateCountAsync(this.requestContext.Object).ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(actualResult);
            Assert.AreEqual(6, actualResult.Count);

            //Verify url matches correct path and query
            this.httpClient.Verify(_ => _.SendAsync(It.Is<HttpRequestMessage>(
                mo => mo.RequestUri.PathAndQuery == "/v2/my/browsehistory/?$count"),
                It.IsAny<HttpCompletionOption>()),
                Times.Once);
        }

        #endregion Browse

        #region AppUsage

        [TestMethod]
        public async Task GetAppUsageAsyncSuccess()
        {
            var pagedResponse = this.GeneratePagedResponseAppUsageResource();
            this.response.Content = new StringContent(JsonConvert.SerializeObject(pagedResponse));

            //Act
            var actualResult = await this.pdApiAdapterV2.GetAppUsageAsync(this.requestContext.Object, DateTimeOffset.UtcNow, null, null).ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(actualResult);
            EqualityHelper.AreEqual(pagedResponse.Items, actualResult.Items);

            //Act
            actualResult = await this.pdApiAdapterV2.GetAppUsageAsync(this.requestContext.Object, OrderByType.DateTime, DateOption.Between, DateTime.Now, null, "bing")
                .ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(actualResult);
            EqualityHelper.AreEqual(pagedResponse.Items, actualResult.Items);
        }

        [TestMethod]
        public async Task GetAppUsageAsyncWatchdogSuccess()
        {
            var pagedResponse = this.GeneratePagedResponseAppUsageResource();
            this.response.Content = new StringContent(JsonConvert.SerializeObject(pagedResponse));

            //Act
            var actualResult = await this.pdApiAdapterV2.GetAppUsageAsync(this.requestContext.Object, DateTimeOffset.UtcNow, null, null).ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(actualResult);
            EqualityHelper.AreEqual(pagedResponse.Items, actualResult.Items);

            //Act
            actualResult = await this.pdApiAdapterV2.GetAppUsageAsync(
                this.requestContext.Object,
                OrderByType.DateTime,
                DateOption.Between,
                DateTime.Now,
                DateTime.Now.AddDays(10),
                "bing").ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(actualResult);
            EqualityHelper.AreEqual(pagedResponse.Items, actualResult.Items);
        }

        [TestMethod]
        public async Task GetAppUsageAsyncFail()
        {
            var pagedResponse = this.GeneratePagedResponseAppUsageResource();
            this.response.Content = new StringContent(JsonConvert.SerializeObject(pagedResponse));

            //Invalid argument for OrderByType
            try
            {
                //Act
                await this.pdApiAdapterV2.GetAppUsageAsync(
                    this.requestContext.Object,
                    OrderByType.SearchTerms,
                    DateOption.Between,
                    DateTime.Now,
                    DateTime.Now.AddDays(10)).ConfigureAwait(false);

                //Assert
                Assert.Fail("Should never get here because we should have thrown");
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            //Invalid argument for DateOption
            try
            {
                //Act
                await this.pdApiAdapterV2.GetAppUsageAsync(
                    this.requestContext.Object,
                    OrderByType.DateTime,
                    DateOption.SingleDay,
                    DateTime.Now,
                    DateTime.Now.AddDays(10));

                //Assert
                Assert.Fail("Should never get here because we should have thrown");
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }

        [TestMethod]
        public async Task GetNextAppUsagePageAsyncSuccess()
        {
            var pagedResponse = this.GeneratePagedResponseAppUsageResource();
            this.response.Content = new StringContent(JsonConvert.SerializeObject(pagedResponse));

            //Act
            var actualResult = await this.pdApiAdapterV2.GetNextAppUsagePageAsync(this.requestContext.Object, new Uri("https://www.nexturi.com"));

            //Assert
            Assert.IsNotNull(actualResult);
            EqualityHelper.AreEqual(pagedResponse.Items, actualResult.Items);
        }

        [TestMethod]
        public async Task GetNextAppUsagePageAsyncFail()
        {
            //Arrange
            this.response.StatusCode = HttpStatusCode.InternalServerError;
            this.response.Content = new StringContent("errormessage");

            try
            {
                //Act
                var actualResult = await this.pdApiAdapterV2.GetNextAppUsagePageAsync(this.requestContext.Object, new Uri("https://www.nexturi.com"));

                //Assert
                Assert.Fail("Should never get here because we should have thrown");
            }
            catch (PxfAdapterException)
            {
                // do nothing, this is what we wanted.
            }
        }

        [TestMethod]
        public async Task DeleteAppUsageAsyncFail()
        {
            //Arrange
            var deleteResponse = new DeleteResourceResponse
            {
                Status = ResourceStatus.Deleted
            };

            var appUsageV2Deletes = new List<AppUsageV2Delete>
            {
                new AppUsageV2Delete("id123", DateTimeOffset.UtcNow, "Monthly"),
                new AppUsageV2Delete("id098", DateTimeOffset.UtcNow.AddDays(1), "Daily")
            };

            //Act
            this.response.StatusCode = HttpStatusCode.InternalServerError;
            this.httpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>())).Returns(Task.FromResult(this.response));
            var actualResult = await this.pdApiAdapterV2.DeleteAppUsageAsync(this.requestContext.Object, appUsageV2Deletes.ToArray()).ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(actualResult.ErrorMessage);
            Assert.AreEqual(ResourceStatus.Error, actualResult.Status);
        }

        [TestMethod]
        public async Task DeleteAppUsageAsyncSuccess()
        {
            //Arrange
            var deleteResponse = new DeleteResourceResponse
            {
                Status = ResourceStatus.Deleted
            };
            var appUsageV2Deletes = new List<AppUsageV2Delete>
            {
                new AppUsageV2Delete("id123", DateTimeOffset.UtcNow, "Monthly"),
                new AppUsageV2Delete("id098", DateTimeOffset.UtcNow.AddDays(1), "Daily")
            };

            //Act
            this.response.Content = new StringContent(JsonConvert.SerializeObject(deleteResponse));
            var actualResult = await this.pdApiAdapterV2.DeleteAppUsageAsync(this.requestContext.Object, appUsageV2Deletes.ToArray()).ConfigureAwait(false);

            //Assert
            Assert.IsNull(actualResult.ErrorMessage);
            Assert.AreEqual(deleteResponse.Status, actualResult.Status);

            //Act
            actualResult = await this.pdApiAdapterV2.DeleteAppUsageAsync(this.requestContext.Object).ConfigureAwait(false);

            //Assert
            Assert.IsNull(actualResult.ErrorMessage);
            Assert.AreEqual(deleteResponse.Status, actualResult.Status);

            //Act
            actualResult = await this.pdApiAdapterV2.DeleteAppUsageAsync(this.requestContext.Object).ConfigureAwait(false);

            //Assert
            Assert.IsNull(actualResult.ErrorMessage);
            Assert.AreEqual(deleteResponse.Status, actualResult.Status);
        }

        [TestMethod]
        public async Task GetAppUsageAggregateCountAsyncSuccess()
        {
            var response = new CountResourceResponse()
            {
                Count = 5
            };
            // Overwrites the response object
            this.response.Content = new StringContent(JsonConvert.SerializeObject(response));

            //Act
            var actualResult = await this.pdApiAdapterV2.GetAppUsageAggregateCountAsync(this.requestContext.Object).ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(actualResult);
            Assert.AreEqual(5, actualResult.Count);

            //Verify url matches correct path and query
            this.httpClient.Verify(_ => _.SendAsync(It.Is<HttpRequestMessage>(
                mo => mo.RequestUri.PathAndQuery == "/v2/my/appusage/?$count"), 
                It.IsAny<HttpCompletionOption>()),
                Times.Once);
        }
        #endregion //AppUsage

        #region Voice

        [TestMethod]
        public async Task GetVoiceHistoryAsyncSuccess()
        {
            //Arrange
            var pagedResponse = this.GeneratePagedResponseVoiceResource();
            this.response.Content = new StringContent(JsonConvert.SerializeObject(pagedResponse));

            //Act
            var actualResult = await this.pdApiAdapterV2.GetVoiceHistoryAsync(
                this.requestContext.Object,
                DateTimeOffset.UtcNow,
                null,
                "bing");

            //Assert
            Assert.IsNotNull(actualResult);
            EqualityHelper.AreEqual(pagedResponse.Items, actualResult.Items);

            //Act
            actualResult = await this.pdApiAdapterV2.GetVoiceHistoryAsync(
                this.requestContext.Object,
                OrderByType.DateTime,
                DateOption.Between,
                DateTime.Now,
                DateTime.Now.AddDays(10),
                "bing");

            //Assert
            Assert.IsNotNull(actualResult);
            EqualityHelper.AreEqual(pagedResponse.Items, actualResult.Items);
        }

        [TestMethod]
        public async Task GetVoiceHistoryAsyncFail()
        {
            var pagedResponse = this.GeneratePagedResponseVoiceResource();
            this.response.Content = new StringContent(JsonConvert.SerializeObject(pagedResponse));

            //Invalid argument for OrderByType
            try
            {
                //Act
                await this.pdApiAdapterV2.GetVoiceHistoryAsync(
                    this.requestContext.Object,
                    OrderByType.SearchTerms,
                    DateOption.Between,
                    DateTime.Now,
                    DateTime.Now.AddDays(10),
                    "bing");

                //Assert
                Assert.Fail("Should never get here because we should have thrown");
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            //Invalid argument for DateOption
            try
            {
                //Act
                await this.pdApiAdapterV2.GetVoiceHistoryAsync(
                    this.requestContext.Object,
                    OrderByType.DateTime,
                    DateOption.SingleDay,
                    DateTime.Now,
                    DateTime.Now.AddDays(10),
                    "bing");

                //Assert
                Assert.Fail("Should never get here because we should have thrown");
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }

        [TestMethod]
        public async Task GetNextVoicePageAsyncSuccess()
        {
            var pagedResponse = this.GeneratePagedResponseVoiceResource();
            this.response.Content = new StringContent(JsonConvert.SerializeObject(pagedResponse));

            //Act
            var actualResult = await this.pdApiAdapterV2.GetNextVoicePageAsync(this.requestContext.Object, new Uri("https://www.nexturi.com"));

            //Assert
            Assert.IsNotNull(actualResult);
            EqualityHelper.AreEqual(pagedResponse.Items, actualResult.Items);
        }

        [TestMethod]
        public async Task GetVoiceHistoryAudioAsyncSuccess()
        {
            string id = "id124";
            var pagedResponse = this.GeneratePagedResponseV2AudioResourceV2();
            var expected = pagedResponse.Items.First();
            this.response.Content = new StringContent(JsonConvert.SerializeObject(pagedResponse));

            //Act
            var actualResult = await this.pdApiAdapterV2.GetVoiceHistoryAudioAsync(this.requestContext.Object, id).ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(actualResult);
            Assert.AreEqual(1, pagedResponse.Items.Count());
            Assert.AreEqual(expected.DeviceId, actualResult.DeviceId);
            Assert.AreEqual(expected.DateTime, actualResult.DateTime);
            Assert.AreEqual(expected.Id, actualResult.Id);

            //Act
            actualResult = await this.pdApiAdapterV2.GetVoiceHistoryAudioAsync(this.requestContext.Object, id, DateTimeOffset.UtcNow).ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(actualResult);
            Assert.AreEqual(1, pagedResponse.Items.Count());
            Assert.AreEqual(expected.DeviceId, actualResult.DeviceId);
            Assert.AreEqual(expected.DateTime, actualResult.DateTime);
            Assert.AreEqual(expected.Id, actualResult.Id);
        }

        [TestMethod]
        public async Task GetVoiceAggregateCountSuccess()
        {
            var response = new CountResourceResponse()
            {
                Count = 7
            };
            // Overwrites the response object
            this.response.Content = new StringContent(JsonConvert.SerializeObject(response));

            //Act
            var actualResult = await this.pdApiAdapterV2.GetVoiceHistoryAggregateCountAsync(this.requestContext.Object).ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(actualResult);
            Assert.AreEqual(7, actualResult.Count);

            //Verify url matches correct path and query
            this.httpClient.Verify(_ => _.SendAsync(It.Is<HttpRequestMessage>(
                mo => mo.RequestUri.PathAndQuery == "/v2/my/voicehistory/?$count"),
                It.IsAny<HttpCompletionOption>()),
                Times.Once);
        }
        #endregion //Voice

        #region Location

        [TestMethod]
        public async Task GetLocationHistoryAsyncSuccess()
        {
            var pagedResponse = this.GeneratePagedResponseLocationResourceV2();
            var expected = pagedResponse.Items.Select(x => x.ToLocationResource(this.partnerConfig.Object.PartnerId)).ToList();
            this.response.Content = new StringContent(JsonConvert.SerializeObject(pagedResponse));

            //Act
            var actualResult = await this.pdApiAdapterV2.GetLocationHistoryAsync(
                this.requestContext.Object,
                OrderByType.DateTime,
                DateOption.Between,
                DateTime.Now,
                DateTime.Now.AddDays(10)).ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(actualResult);
            EqualityHelper.AreEqual(expected, actualResult.Items);
        }

        [TestMethod]
        public async Task GetLocationHistoryAsyncFail()
        {
            var pagedResponse = this.GeneratePagedResponseLocationResourceV2();
            this.response.Content = new StringContent(JsonConvert.SerializeObject(pagedResponse));

            //Argument out of range for 'OrderByType'
            try
            {
                //Act
                await this.pdApiAdapterV2.GetLocationHistoryAsync(this.requestContext.Object, OrderByType.SearchTerms, DateOption.Between, DateTime.Now, DateTime.Now.AddDays(10))
                    .ConfigureAwait(false);

                //Assert
                Assert.Fail("Should never get here because we should have thrown");
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            //Argument out of range for 'DateOption'
            try
            {
                //Act
                await this.pdApiAdapterV2.GetLocationHistoryAsync(this.requestContext.Object, OrderByType.DateTime, DateOption.SingleDay, DateTime.Now, DateTime.Now.AddDays(10))
                    .ConfigureAwait(false);

                //Assert
                Assert.Fail("Should never get here because we should have thrown");
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }

        [TestMethod]
        public async Task GetNextLocationPageAsyncSuccess()
        {
            var pagedResponse = this.GeneratePagedResponseLocationResourceV2();
            var expected = pagedResponse.Items.Select(i => i.ToLocationResource(this.partnerConfig.Object.PartnerId)).ToList();
            this.response.Content = new StringContent(JsonConvert.SerializeObject(pagedResponse));

            //Act
            var actualResult = await this.pdApiAdapterV2.GetNextLocationPageAsync(this.requestContext.Object, new Uri("https://www.nexturi.com")).ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(actualResult);
            EqualityHelper.AreEqual(expected, actualResult.Items);
        }

        [TestMethod]
        public async Task DeleteLocationAsyncSuccess()
        {
            //Arrange
            var deleteResponse = new DeleteResourceResponse
            {
                Status = ResourceStatus.Deleted
            };

            var locationV2Deletes = new List<LocationV2Delete>
            {
                new LocationV2Delete(DateTimeOffset.UtcNow)
            };

            this.response.Content = new StringContent(JsonConvert.SerializeObject(deleteResponse));

            //Act
            var actualResult = await this.pdApiAdapterV2.DeleteLocationAsync(this.requestContext.Object, locationV2Deletes.ToArray()).ConfigureAwait(false);

            //Assert
            Assert.IsNull(actualResult.ErrorMessage);
            Assert.AreEqual(deleteResponse.Status, actualResult.Status);
        }

        [TestMethod]
        public async Task DeleteLocationHistoryAsyncSuccess()
        {
            //Act
            var actualResult = await this.pdApiAdapterV2.DeleteLocationHistoryAsync(this.requestContext.Object).ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(actualResult);
            Assert.IsNull(actualResult.ErrorMessage);
            Assert.AreEqual(ResourceStatus.Deleted, actualResult.Status);
        }

        [TestMethod]
        public async Task DeleteVoiceHistoryAsyncSuccess()
        {
            //Arrange
            var deleteResponse = new DeleteResourceResponse
            {
                Status = ResourceStatus.Deleted
            };
            var voiceV2Deletes = new List<VoiceV2Delete>
            {
                new VoiceV2Delete("id123", DateTimeOffset.UtcNow),
                new VoiceV2Delete("id098", DateTimeOffset.UtcNow.AddDays(1))
            };
            this.response.Content = new StringContent(JsonConvert.SerializeObject(deleteResponse));

            //Act
            var actualResult = await this.pdApiAdapterV2.DeleteVoiceHistoryAsync(this.requestContext.Object, voiceV2Deletes.ToArray()).ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(actualResult);
            Assert.IsNull(actualResult.ErrorMessage);
            Assert.AreEqual(deleteResponse.Status, actualResult.Status);

            //Act
            actualResult = await this.pdApiAdapterV2.DeleteVoiceHistoryAsync(this.requestContext.Object).ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(actualResult);
            Assert.IsNull(actualResult.ErrorMessage);
            Assert.AreEqual(deleteResponse.Status, actualResult.Status);
        }

        [TestMethod]
        public async Task GetLocationAsyncSuccess()
        {
            //Arrange
            var pagedResponse = this.GeneratePagedResponseLocationResourceV2();
            var expected = pagedResponse.Items.Select(i => i.ToLocationResource(this.partnerConfig.Object.PartnerId)).ToList();
            this.response.Content = new StringContent(JsonConvert.SerializeObject(pagedResponse));

            //Act
            var actualResult = await this.pdApiAdapterV2.GetLocationAsync(this.requestContext.Object, DateTimeOffset.UtcNow, null, null).ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(actualResult);
            EqualityHelper.AreEqual(expected, actualResult.Items);
        }

        [TestMethod]
        public async Task GetLocationAggregateCountSuccess()
        {
            var response = new CountResourceResponse()
            {
                Count = 8
            };
            // Overwrites the response object
            this.response.Content = new StringContent(JsonConvert.SerializeObject(response));

            //Act
            var actualResult = await this.pdApiAdapterV2.GetLocationAggregateCountAsync(this.requestContext.Object).ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(actualResult);
            Assert.AreEqual(8, actualResult.Count);

            //Verify url matches correct path and query
            this.httpClient.Verify(_ => _.SendAsync(It.Is<HttpRequestMessage>(
                mo => mo.RequestUri.PathAndQuery == "/v2/my/locationhistory/?$count"),
                It.IsAny<HttpCompletionOption>()),
                Times.Once);
        }

        #endregion //Location

        #region Search

        [TestMethod]
        public async Task DeleteSearchHistoryAsyncSuccess()
        {
            //Arrange
            var deleteResponse = new DeleteResourceResponse
            {
                Status = ResourceStatus.Deleted
            };
            var searchV2Deletes = new List<SearchV2Delete>
            {
                new SearchV2Delete("id456")
            };
            this.response.Content = new StringContent(JsonConvert.SerializeObject(deleteResponse));

            //Act
            var actualResult = await this.pdApiAdapterV2.DeleteSearchHistoryAsync(this.requestContext.Object, searchV2Deletes.ToArray()).ConfigureAwait(false);

            //Assert
            Assert.IsNull(actualResult.ErrorMessage);
            Assert.AreEqual(deleteResponse.Status, actualResult.Status);

            //Act
            actualResult = await this.pdApiAdapterV2.DeleteSearchHistoryAsync(this.requestContext.Object).ConfigureAwait(false);

            //Assert
            Assert.IsNull(actualResult.ErrorMessage);
            Assert.AreEqual(deleteResponse.Status, actualResult.Status);

            //Act
            actualResult = await this.pdApiAdapterV2.DeleteSearchHistoryAsync(this.requestContext.Object, true).ConfigureAwait(false);

            //Assert
            Assert.IsNull(actualResult.ErrorMessage);
            Assert.AreEqual(deleteResponse.Status, actualResult.Status);

            //Act
            actualResult = await this.pdApiAdapterV2.DeleteSearchHistoryAsync(this.requestContext.Object, false).ConfigureAwait(false);

            //Assert
            Assert.IsNull(actualResult.ErrorMessage);
            Assert.AreEqual(deleteResponse.Status, actualResult.Status);
        }

        [TestMethod]
        public async Task GetSearchHistoryAsyncSuccess()
        {
            //Arrange
            var pagedResponse = this.GeneratePagedResponseSearchResourceV2();
            this.response.Content = new StringContent(JsonConvert.SerializeObject(pagedResponse));

            //Act
            var actualResult = await this.pdApiAdapterV2.GetSearchHistoryAsync(this.requestContext.Object, DateTimeOffset.UtcNow, null, "bing").ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(actualResult);
            Assert.AreEqual(actualResult.Items.Count, pagedResponse.Items.Count);

            //Act
            actualResult = await this.pdApiAdapterV2.GetSearchHistoryAsync(
                this.requestContext.Object,
                OrderByType.DateTime,
                DateOption.Between,
                DateTime.Now,
                DateTime.Now.AddDays(10),
                "bing").ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(actualResult);
            Assert.AreEqual(actualResult.Items.Count, pagedResponse.Items.Count);
            EqualityHelper.AreEqual(pagedResponse.Items, pagedResponse.Items);
        }

        [TestMethod]
        public async Task GetSearchHistoryAsyncFail()
        {
            //Arrange
            var pagedResponse = this.GeneratePagedResponseSearchResourceV2();
            this.response.Content = new StringContent(JsonConvert.SerializeObject(pagedResponse));

            try
            {
                //Act
                await this.pdApiAdapterV2.GetSearchHistoryAsync(this.requestContext.Object, OrderByType.SearchTerms, DateOption.Between, DateTime.Now, DateTime.Now.AddDays(10))
                    .ConfigureAwait(false);

                //Assert
                Assert.Fail("Should never get here because we should have thrown");
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            try
            {
                //Act
                await this.pdApiAdapterV2.GetSearchHistoryAsync(this.requestContext.Object, OrderByType.DateTime, DateOption.SingleDay, DateTime.Now, DateTime.Now.AddDays(10))
                    .ConfigureAwait(false);

                //Assert
                Assert.Fail("Should never get here because we should have thrown");
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }

        [TestMethod]
        public async Task GetNextSearchHistoryPageAsyncSuccess()
        {
            //Arrange
            Uri nextUri = new Uri("https://www.nextUri.com");
            var pagedResponse = this.GeneratePagedResponseSearchResourceV2();
            var expected = pagedResponse.Items.Select(x => x.ToSeachResource(this.partnerConfig.Object.PartnerId)).ToList();
            this.response.Content = new StringContent(JsonConvert.SerializeObject(pagedResponse));

            //Act
            var actualResult = await this.pdApiAdapterV2.GetNextSearchHistoryPageAsync(this.requestContext.Object, nextUri).ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(actualResult);
            Assert.AreEqual(pagedResponse.Items.Count, actualResult.Items.Count);
            EqualityHelper.AreEqual(expected, actualResult.Items);
        }

        [TestMethod]
        public async Task GetSearchHistoryAggregateCountSuccess()
        {
            var response = new CountResourceResponse()
            {
                Count = 9
            };
            // Overwrites the response object
            this.response.Content = new StringContent(JsonConvert.SerializeObject(response));

            //Act
            var actualResult = await this.pdApiAdapterV2.GetSearchHistoryAggregateCountAsync(this.requestContext.Object).ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(actualResult);
            Assert.AreEqual(9, actualResult.Count);

            //Verify url matches correct path and query
            this.httpClient.Verify(_ => _.SendAsync(It.Is<HttpRequestMessage>(
                mo => mo.RequestUri.PathAndQuery == "/v2/my/searchhistory/?$count"),
                It.IsAny<HttpCompletionOption>()),
                Times.Once);
        }

        #endregion //Search

        #region Content consumption

        [TestMethod]
        public async Task GetNextContentConsumptionPageAsyncSuccess()
        {
            //Arrange
            Uri nextUri = new Uri("https://www.nextUri.com");
            var pagedResponse = this.GeneratePagedResponseContentConsumptionResourceV2();
            var expected = pagedResponse.Items.Select(x => x.ToContentConsumptionResource(this.partnerConfig.Object.PartnerId)).ToList();
            this.response.Content = new StringContent(JsonConvert.SerializeObject(pagedResponse));

            //Act
            var actualResult = await this.pdApiAdapterV2.GetNextContentConsumptionPageAsync(this.requestContext.Object, nextUri).ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(actualResult);
            Assert.AreEqual(pagedResponse.Items.Count(), actualResult.Items.Count);
            EqualityHelper.AreEqual(expected, actualResult.Items);
        }

        [TestMethod]
        public async Task GetContentConsumptionAsyncSuccess()
        {
            //Arrange
            var pagedResponse = this.GeneratePagedResponseContentConsumptionResourceV2();
            var expected = pagedResponse.Items.Select(x => x.ToContentConsumptionResource(this.partnerConfig.Object.PartnerId)).ToList();
            this.response.Content = new StringContent(JsonConvert.SerializeObject(pagedResponse));

            //Act
            var actualResult = await this.pdApiAdapterV2.GetContentConsumptionAsync(this.requestContext.Object, DateTimeOffset.UtcNow, null, null).ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(actualResult);
            EqualityHelper.AreEqual(expected, actualResult.Items);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public async Task GetContentConsumptionAsyncInvalidDataFail()
        {
            //Arrange
            var pagedResponse = this.GeneratePagedResponseContentConsumptionResourceV2();
            pagedResponse.Items.First().MediaType = "wrongtype";
            this.response.Content = new StringContent(JsonConvert.SerializeObject(pagedResponse));

            //Act
            var actualResult = await this.pdApiAdapterV2.GetContentConsumptionAsync(this.requestContext.Object, DateTimeOffset.UtcNow, null, null).ConfigureAwait(false);

            //Assert
            Assert.Fail("Should never get here because we should have thrown");
        }

        [TestMethod]
        [ExpectedException(typeof(PxfAdapterException))]
        public async Task GetContentConsumptionAsyncNullArgumentFail()
        {
            this.response.Content = new StringContent(JsonConvert.Null);

            //Act
            await this.pdApiAdapterV2.GetContentConsumptionAsync(this.requestContext.Object, DateTimeOffset.UtcNow, null, null).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeleteContentConsumptionAsyncSuccess()
        {
            //Arrange
            var deleteResponse = new DeleteResourceResponse
            {
                Status = ResourceStatus.Deleted
            };
            var contentConsumptionV2Delete = new List<ContentConsumptionV2Delete>
            {
                new ContentConsumptionV2Delete("id123", DateTimeOffset.UtcNow),
                new ContentConsumptionV2Delete("id098", DateTimeOffset.UtcNow.AddDays(1))
            };

            //Act
            this.response.Content = new StringContent(JsonConvert.SerializeObject(deleteResponse));
            var actualResult = await this.pdApiAdapterV2.DeleteContentConsumptionAsync(this.requestContext.Object, contentConsumptionV2Delete.ToArray()).ConfigureAwait(false);

            //Assert
            Assert.IsNull(actualResult.ErrorMessage);
            Assert.AreEqual(deleteResponse.Status, actualResult.Status);

            //Act
            actualResult = await this.pdApiAdapterV2.DeleteContentConsumptionAsync(this.requestContext.Object).ConfigureAwait(false);

            //Assert
            Assert.IsNull(actualResult.ErrorMessage);
            Assert.AreEqual(deleteResponse.Status, actualResult.Status);
        }

        [TestMethod]
        public async Task GetContentConsumptionAggregateCountSuccess()
        {
            var response = new CountResourceResponse()
            {
                Count = 10
            };
            // Overwrites the response object
            this.response.Content = new StringContent(JsonConvert.SerializeObject(response));

            //Act
            var actualResult = await this.pdApiAdapterV2.GetContentConsumptionAggregateCountAsync(this.requestContext.Object).ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(actualResult);
            Assert.AreEqual(10, actualResult.Count);

            //Verify url matches correct path and query
            this.httpClient.Verify(_ => _.SendAsync(It.Is<HttpRequestMessage>(
                mo => mo.RequestUri.PathAndQuery == "/v2/my/contentconsumption/?$count"),
                It.IsAny<HttpCompletionOption>()),
                Times.Once);
        }

        #endregion //Content consumption

        #region Data Test Methods

        private PagedResponse<BrowseResource> GeneratePagedResponseBrowseResource()
        {
            var pagedResponse = new PagedResponse<BrowseResource>
            {
                Items = new List<BrowseResource>
                {
                    new BrowseResource
                    {
                        DeviceId = Guid.NewGuid().ToString(),
                        Id = Guid.NewGuid().ToString(),
                        Status = ResourceStatus.Active,
                        DateTime = DateTime.Now,
                        PageTitle = "pagetitle",
                        PartnerId = Guid.NewGuid().ToString()
                    }
                }
            };
            return pagedResponse;
        }

        private PagedResponse<VoiceResource> GeneratePagedResponseVoiceResource()
        {
            var pagedResponse = new PagedResponse<VoiceResource>
            {
                Items = new List<VoiceResource>
                {
                    new VoiceResource
                    {
                        DeviceId = Guid.NewGuid().ToString(),
                        DeviceType = "Xbox",
                        DateTime = DateTime.Now,
                        DisplayText = "voice text",
                        Application = "application",
                        Id = Guid.NewGuid().ToString()
                    }
                }
            };
            return pagedResponse;
        }

        private PagedResponseV2<VoiceAudioResourceV2> GeneratePagedResponseV2AudioResourceV2()
        {
            var pagedResponse = new PagedResponseV2<VoiceAudioResourceV2>
            {
                Items = new List<VoiceAudioResourceV2>
                {
                    new VoiceAudioResourceV2
                    {
                        Id = Guid.NewGuid().ToString(),
                        DateTime = DateTime.Now,
                        DeviceId = Guid.NewGuid().ToString(),
                        DeviceType = "xbox",
                        DisplayText = "displaytext",
                        AverageByteRate = 12,
                        Application = "dsahboard"
                    }
                }
            };
            return pagedResponse;
        }

        private PagedResponse<AppUsageResource> GeneratePagedResponseAppUsageResource()
        {
            var pagedResponse = new PagedResponse<AppUsageResource>
            {
                Items = new List<AppUsageResource>
                {
                    new AppUsageResource
                    {
                        AppId = "appId1234",
                        DeviceId = Guid.NewGuid().ToString(),
                        Id = Guid.NewGuid().ToString(),
                        DateTime = DateTime.Now,
                        AppName = "pagetitle",
                        EndDateTime = DateTimeOffset.UtcNow.AddDays(10),
                        Aggregation = "Daily",
                        AppIconBackground = "#334324"
                    }
                }
            };
            return pagedResponse;
        }

        private PagedResponseV2<LocationResourceV2> GeneratePagedResponseLocationResourceV2()
        {
            var pagedResponse = new PagedResponseV2<LocationResourceV2>
            {
                Items = new List<LocationResourceV2>
                {
                    new LocationResourceV2
                    {
                        DeviceId = Guid.NewGuid().ToString(),
                        DateTime = DateTime.Now,
                        LocationType = LocationEnumsV2.LocationTypeV2.Device,
                        ActivityType = LocationEnumsV2.LocationActivityTypeV2.Bike,
                        DeviceType = LocationEnumsV2.LocationDeviceTypeV2.PC,
                        Location = GeographyPoint.Create(46.005813085496385, -121.15351310383608),
                        Name = "HOME",
                        AccuracyRadius = 100,
                        Distance = 60544,
                        EndDateTime = DateTimeOffset.UtcNow.AddDays(10),
                        Url = "https://health.microsoft.com/MyLongBikeRide/"
                    }
                }
            };

            return pagedResponse;
        }

        private PagedResponse<SearchResourceV2> GeneratePagedResponseSearchResourceV2()
        {
            var partnerId = Guid.NewGuid().ToString();
            var pagedResponse = new PagedResponse<SearchResourceV2>
            {
                Items = new List<SearchResourceV2>
                {
                    new SearchResourceV2
                    {
                        DeviceId = Guid.NewGuid().ToString(),
                        Id = Guid.NewGuid().ToString(),
                        DateTime = DateTime.Now,
                        SearchTerms = "news",

                        //Sources
                        Navigations = new List<NavigatedUrlV2>
                        {
                            new NavigatedUrlV2
                            {
                                Url = new Uri("https://www.NevigatedtoUri1.com"),
                                PageTitle = "NevigatedtoUri1",
                                DateTime = DateTime.Now.AddSeconds(10)
                            },
                            new NavigatedUrlV2
                            {
                                Url = new Uri("https://www.NevigatedtoUri2.com"),
                                PageTitle = "NevigatedtoUri2",
                                DateTime = DateTime.Now.AddSeconds(20)
                            }
                        }
                    }
                },

                NextLink = new Uri("https://www.nextUri.com"),
                PartnerId = partnerId
            };

            return pagedResponse;
        }

        private PagedResponseV2<ContentConsumptionResourceV2> GeneratePagedResponseContentConsumptionResourceV2()
        {
            var pagedResponse = new PagedResponseV2<ContentConsumptionResourceV2>
            {
                Items = new List<ContentConsumptionResourceV2>
                {
                    new ContentConsumptionResourceV2
                    {
                        DeviceId = Guid.NewGuid().ToString(),
                        Id = Guid.NewGuid().ToString(),
                        DateTime = DateTime.Now,
                        AppName = "music",
                        Artist = "aaa",
                        ContainerName = "AAA",
                        ConsumptionTimeSeconds = 120,
                        Title = "ABC",
                        MediaType = ContentConsumptionResource.ContentType.Song.ToString()
                    }
                }
            };
            return pagedResponse;
        }

        #endregion //Data Test Methods
    }
}

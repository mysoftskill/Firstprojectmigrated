namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.Timeline
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers;
    using Microsoft.Membership.MemberServices.Privacy.Core.PCF;
    using Microsoft.Membership.MemberServices.Privacy.Core.Timeline;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2;
    using Microsoft.Membership.MemberServices.PrivacyAdapters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.V2;
    using Microsoft.Membership.MemberServices.ScheduleDbClient;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class AggregateCountUnitTest
    {
        private TimelineService timelineService;

        private Mock<IPxfDispatcher> mockPxfDispatcher;

        private Mock<IPcfProxyService> mockPcfService;

        private Mock<IRequestClassifier> mockRequestClassifier;

        private Mock<ILogger> mockILogger;

        private Mock<IScheduleDbClient> mockScheduleDbClient;

        private Mock<IRequestContext> requestContext;

        private Dictionary<ResourceType, int> expectedResourceCounts;

        private Mock<IMsaIdentityServiceAdapter> msaIdentityServiceAdapter;

        [TestInitialize]
        public void TestInitializeAggregateCount()
        {
            this.requestContext = new Mock<IRequestContext>();

            this.expectedResourceCounts = new Dictionary<ResourceType, int>()
            {
                {ResourceType.AppUsage,5 },
                {ResourceType.Browse,6},
                {ResourceType.ContentConsumption,7},
                {ResourceType.Location,8},
                {ResourceType.Search,9},
                {ResourceType.Voice,10}
            };

            this.mockPxfDispatcher = new Mock<IPxfDispatcher>();

            // Adds all the expected ResourceCounts
            this.InitializeResourceResponses();

            // Only used for delete
            this.mockPcfService = new Mock<IPcfProxyService>();

            // Only used for delete
            this.mockRequestClassifier = new Mock<IRequestClassifier>();

            // Occasionally called to verify checks complete
            this.mockILogger = new Mock<ILogger>();

            this.mockScheduleDbClient = new Mock<IScheduleDbClient>();

            this.msaIdentityServiceAdapter = new Mock<IMsaIdentityServiceAdapter>();

            this.timelineService = new TimelineService(
                this.mockPxfDispatcher.Object,
                Policies.Current,
                this.mockPcfService.Object,
                this.mockRequestClassifier.Object,
                this.mockScheduleDbClient.Object,
                this.msaIdentityServiceAdapter.Object,
                this.mockILogger.Object
                );
        }

        [TestMethod]
        public async Task TimelineAggregateCountSingleSuccess()
        {
            var cardList = new List<string>();
            cardList.Add(TimelineCard.CardTypes.AppUsageCard);
            ServiceResponse<AggregateCountResponse> response = await timelineService.GetAggregateCountAsync(this.requestContext.Object, cardList).ConfigureAwait(false);
            
            Assert.IsTrue(response.Result.AggregateCounts.ContainsKey(TimelineCard.CardTypes.AppUsageCard),"Timeline card was not in the result");
            Assert.AreEqual(response.Result.AggregateCounts[TimelineCard.CardTypes.AppUsageCard], expectedResourceCounts[ResourceType.AppUsage]);
        }

        [TestMethod]
        public async Task TimelineAggregateCountMultipleSuccess()
        {
            var cardList = new List<string>();
            cardList.Add(TimelineCard.CardTypes.AppUsageCard);
            cardList.Add(TimelineCard.CardTypes.BrowseCard);
            cardList.Add(TimelineCard.CardTypes.ContentConsumptionCount);
            cardList.Add(TimelineCard.CardTypes.LocationCard);
            cardList.Add(TimelineCard.CardTypes.SearchCard);
            cardList.Add(TimelineCard.CardTypes.VoiceCard);
            ServiceResponse<AggregateCountResponse> response = await timelineService.GetAggregateCountAsync(this.requestContext.Object, cardList).ConfigureAwait(false);
            
            Assert.AreEqual(response.Result.AggregateCounts.Count, this.expectedResourceCounts.Count);
            foreach(var card in cardList)
            {
                int expectedCount = -1;
                switch (card)
                {
                    case TimelineCard.CardTypes.AppUsageCard:
                        expectedCount = this.expectedResourceCounts[ResourceType.AppUsage];
                        break;
                    case TimelineCard.CardTypes.BrowseCard:
                        expectedCount = this.expectedResourceCounts[ResourceType.Browse];
                        break;
                    case TimelineCard.CardTypes.ContentConsumptionCount:
                        expectedCount = this.expectedResourceCounts[ResourceType.ContentConsumption];
                        break;
                    case TimelineCard.CardTypes.LocationCard:
                        expectedCount = this.expectedResourceCounts[ResourceType.Location];
                        break;
                    case TimelineCard.CardTypes.SearchCard:
                        expectedCount = this.expectedResourceCounts[ResourceType.Search];
                        break;
                    case TimelineCard.CardTypes.VoiceCard:
                        expectedCount = this.expectedResourceCounts[ResourceType.Voice];
                        break;
                }
                Assert.AreEqual(response.Result.AggregateCounts[card], expectedCount);
            }
        }

        [TestMethod]
        public async Task TimelineAggregateCountZeroCountSuccess()
        {
            var cardList = new List<string>();
            cardList.Add(TimelineCard.CardTypes.BrowseCard);

            // Reset browse to return 0
            var mockBrowseAdapter = new Mock<IBrowseHistoryV2Adapter>();
            var responseCount = new CountResourceResponse() { Count = 0 };
            mockBrowseAdapter
                .Setup(_ => _.GetBrowseHistoryAggregateCountAsync(It.IsAny<IPxfRequestContext>()))
                .ReturnsAsync(responseCount);
            Mock<IPxfAdapter> mockBrowse = mockBrowseAdapter.As<IPxfAdapter>();

            this.InitializeAggregateCountType(ResourceType.Browse, mockBrowse);

            ServiceResponse<AggregateCountResponse> response = await timelineService.GetAggregateCountAsync(this.requestContext.Object, cardList).ConfigureAwait(false);

            Assert.IsTrue(response.Result.AggregateCounts.ContainsKey(TimelineCard.CardTypes.BrowseCard), "Timeline card was not in the result");
            Assert.AreEqual(response.Result.AggregateCounts[TimelineCard.CardTypes.BrowseCard], 0);

        }

        [TestMethod]
        public async Task TimelineAggregateCountContentConsumptionSuccess()
        {
            var cardList = new List<string>();
            cardList.Add(TimelineCard.CardTypes.ContentConsumptionCount);

            ServiceResponse<AggregateCountResponse> response = await timelineService.GetAggregateCountAsync(this.requestContext.Object, cardList).ConfigureAwait(false);

            Assert.IsTrue(response.Result.AggregateCounts.ContainsKey(TimelineCard.CardTypes.ContentConsumptionCount), "Timeline card was not in the result");
            Assert.AreEqual(response.Result.AggregateCounts[TimelineCard.CardTypes.ContentConsumptionCount], expectedResourceCounts[ResourceType.ContentConsumption]);
        }

        [TestMethod]
        public async Task TimelineAggregateCountContentConsumptionMismatch()
        {
            var cardList = new List<string>();
            cardList.Add(TimelineCard.CardTypes.BookConsumptionCard);
            cardList.Add(TimelineCard.CardTypes.EpisodeConsumptionCard);

            ServiceResponse<AggregateCountResponse> response = await timelineService.GetAggregateCountAsync(this.requestContext.Object, cardList).ConfigureAwait(false);
            Assert.IsFalse(response.IsSuccess, "This call should fail");
            Assert.AreEqual("An unsupported cardtype was called. Exception Message: CardType BookConsumptionCard is not a valid ResourceType to count", response.Error.Message);
           
        }

        [TestMethod]
        public async Task TimelineAggregateCountFailsIfAllCallsFail()
        {
            var cardList = new List<string>();
            cardList.Add(TimelineCard.CardTypes.BrowseCard);
            cardList.Add(TimelineCard.CardTypes.LocationCard);

            // Reset browse to throw exception
            var mockBrowseAdapter = new Mock<IBrowseHistoryV2Adapter>();
            mockBrowseAdapter
                .Setup(_ => _.GetBrowseHistoryAggregateCountAsync(It.IsAny<IPxfRequestContext>()))
                .Throws(new Exception());
            Mock<IPxfAdapter> mockBrowse = mockBrowseAdapter.As<IPxfAdapter>();
            this.InitializeAggregateCountType(ResourceType.Browse, mockBrowse);

            // Reset location to throw exception
            var mockLocationAdapter = new Mock<ILocationV2Adapter>();
            mockLocationAdapter
                .Setup(_ => _.GetLocationAggregateCountAsync(It.IsAny<IPxfRequestContext>()))
                .Throws(new Exception());
            Mock<IPxfAdapter> mockLocation = mockLocationAdapter.As<IPxfAdapter>();
            this.InitializeAggregateCountType(ResourceType.Location, mockLocation);

            ServiceResponse<AggregateCountResponse> response = await timelineService.GetAggregateCountAsync(this.requestContext.Object, cardList).ConfigureAwait(false);

            Assert.IsFalse(response.IsSuccess, "This call should fail when all dependant calls fail");

            // Returns last error
            Assert.IsTrue(response.Error.Message.Contains("All calls to obtain aggregate counts failed."));
            Assert.IsTrue(response.Error.Message.Contains("Exception of type 'System.Exception' was thrown."));
        }

        [TestMethod]
        public async Task TimelineAggregateCountMixedSuccess()
        {
            var cardList = new List<string>();
            cardList.Add(TimelineCard.CardTypes.BrowseCard);
            cardList.Add(TimelineCard.CardTypes.LocationCard);

            // Reset location to throw exception
            var mockLocationAdapter = new Mock<ILocationV2Adapter>();
            mockLocationAdapter
                .Setup(_ => _.GetLocationAggregateCountAsync(It.IsAny<IPxfRequestContext>()))
                .Throws(new Exception());
            Mock<IPxfAdapter> mockLocation = mockLocationAdapter.As<IPxfAdapter>();
            this.InitializeAggregateCountType(ResourceType.Location, mockLocation);

            ServiceResponse<AggregateCountResponse> response = await timelineService.GetAggregateCountAsync(this.requestContext.Object, cardList).ConfigureAwait(false);

            Assert.IsTrue(response.IsSuccess, "This call should succeed");
            Assert.AreEqual(expectedResourceCounts[ResourceType.Browse], response.Result.AggregateCounts[TimelineCard.CardTypes.BrowseCard]);
            Assert.AreEqual(int.MinValue, response.Result.AggregateCounts[TimelineCard.CardTypes.LocationCard]);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task TimelineAggregateCountMultiplesOfSameResource()
        {
            var cardList = new List<string>();
            cardList.Add(TimelineCard.CardTypes.BrowseCard);
            cardList.Add(TimelineCard.CardTypes.BrowseCard);

            ServiceResponse<AggregateCountResponse> response = await timelineService.GetAggregateCountAsync(this.requestContext.Object, cardList).ConfigureAwait(false);
        }

        private void InitializeResourceResponses()
        {
            // Setup App Usage
            var mockAppUsageAdapter = new Mock<IAppUsageV2Adapter>();
            var responseCount = new CountResourceResponse() { Count = expectedResourceCounts[ResourceType.AppUsage] };
            mockAppUsageAdapter
                .Setup(_ => _.GetAppUsageAggregateCountAsync(It.IsAny<IPxfRequestContext>()))
                .ReturnsAsync(responseCount);
            Mock<IPxfAdapter> mockAppUsage = mockAppUsageAdapter.As<IPxfAdapter>();

            this.InitializeAggregateCountType(ResourceType.AppUsage, mockAppUsage);

            // Setup Browse
            var mockBrowseAdapter = new Mock<IBrowseHistoryV2Adapter>();
            responseCount = new CountResourceResponse() { Count = expectedResourceCounts[ResourceType.Browse] };
            mockBrowseAdapter
                .Setup(_ => _.GetBrowseHistoryAggregateCountAsync(It.IsAny<IPxfRequestContext>()))
                .ReturnsAsync(responseCount);
            Mock<IPxfAdapter> mockBrowse = mockBrowseAdapter.As<IPxfAdapter>();

            this.InitializeAggregateCountType(ResourceType.Browse, mockBrowse);

            // Setup Content Consumption
            var mockContentAdapter = new Mock<IContentConsumptionV2Adapter>();
            responseCount = new CountResourceResponse() { Count = expectedResourceCounts[ResourceType.ContentConsumption] };
            mockContentAdapter
                .Setup(_ => _.GetContentConsumptionAggregateCountAsync(It.IsAny<IPxfRequestContext>()))
                .ReturnsAsync(responseCount);
            Mock<IPxfAdapter> mockContent = mockContentAdapter.As<IPxfAdapter>();

            this.InitializeAggregateCountType(ResourceType.ContentConsumption, mockContent);

            // Setup Location
            var mockLocationAdapter = new Mock<ILocationV2Adapter>();
            responseCount = new CountResourceResponse() { Count = expectedResourceCounts[ResourceType.Location] };
            mockLocationAdapter
                .Setup(_ => _.GetLocationAggregateCountAsync(It.IsAny<IPxfRequestContext>()))
                .ReturnsAsync(responseCount);
            Mock<IPxfAdapter> mockLocation = mockLocationAdapter.As<IPxfAdapter>();

            this.InitializeAggregateCountType(ResourceType.Location, mockLocation);

            // Setup Search
            var mockSearchAdapter = new Mock<ISearchHistoryV2Adapter>();
            responseCount = new CountResourceResponse() { Count = expectedResourceCounts[ResourceType.Search] };
            mockSearchAdapter
                .Setup(_ => _.GetSearchHistoryAggregateCountAsync(It.IsAny<IPxfRequestContext>()))
                .ReturnsAsync(responseCount);
            Mock<IPxfAdapter> mockSearch = mockSearchAdapter.As<IPxfAdapter>();

            this.InitializeAggregateCountType(ResourceType.Search, mockSearch);

            // Setup Voice
            var mockVoiceAdapter = new Mock<IVoiceHistoryV2Adapter>();
            responseCount = new CountResourceResponse() { Count = expectedResourceCounts[ResourceType.Voice] };
            mockVoiceAdapter
                .Setup(_ => _.GetVoiceHistoryAggregateCountAsync(It.IsAny<IPxfRequestContext>()))
                .ReturnsAsync(responseCount);
            Mock<IPxfAdapter> mockVoice = mockVoiceAdapter.As<IPxfAdapter>();

            this.InitializeAggregateCountType(ResourceType.Voice, mockVoice);
        }

        private void InitializeAggregateCountType(ResourceType resourceType, Mock<IPxfAdapter> pxfAdapter)
        {
            List<PartnerAdapter> adapters = new List<PartnerAdapter>();

            var partnerAdapater = new PartnerAdapter()
            {
                Adapter = pxfAdapter.Object
            };
            adapters.Add(partnerAdapater);

            this.mockPxfDispatcher.Setup(_ => _.GetAdaptersForResourceType(
                It.IsAny<IPxfRequestContext>(),
                resourceType,
                It.IsAny<PxfAdapterCapability>())).Returns(adapters);
        }
    }
}

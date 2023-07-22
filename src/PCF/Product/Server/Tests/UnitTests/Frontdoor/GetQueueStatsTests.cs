namespace PCF.UnitTests.Frontdoor
{
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor;
    using Microsoft.PrivacyServices.CommandFeed.Service.Telemetry.Repository;
    using Microsoft.PrivacyServices.Identity;
    using Microsoft.Windows.Services.AuthN.Server;
    using Moq;
    using Ploeh.AutoFixture;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    [Trait("Category", "UnitTest")]
    public class GetQueueStatsTests : INeedDataBuilders
    {
        [Fact]
        public async Task AgentNotAuthorizedReturnsUnauthorized()
        {
            var authorizer = this.AMockOf<IAuthorizer>();
            var dataAgentMap = this.AMockOf<IDataAgentMap>();
            var kustoRepository = this.AMockOf<ITelemetryRepository>();

            var agentId = this.AnAgentId();

            authorizer.Setup(m => m.CheckAuthorizedAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<AgentId>()))
                         .Throws(new AuthNException(AuthNErrorCode.AuthenticationFailed, "authentication failed"));

            GetQueueStatsActionResult queueStatsAction = new GetQueueStatsActionResult(
                agentId,
                new HttpRequestMessage(HttpMethod.Get, "/getqueuestats"),
                dataAgentMap.Object,
                authorizer.Object,
                kustoRepository.Object);

            var response = await queueStatsAction.ExecuteAsync(CancellationToken.None);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AgentWhitlistFlightNotEnabledGivesBadRequest()
        {
            var authorizer = this.AMockOf<IAuthorizer>();
            var dataAgentMap = this.AMockOf<IDataAgentMap>();
            var kustoRepository = this.AMockOf<ITelemetryRepository>();

            var agentId = this.AnAgentId();

            GetQueueStatsActionResult queueStatsAction = new GetQueueStatsActionResult(
                agentId,
                new HttpRequestMessage(HttpMethod.Get, "/getqueuestats"),
                dataAgentMap.Object,
                authorizer.Object,
                kustoRepository.Object);
            
            using (new FlightDisabled(FlightingNames.AllowedAgentIdForQueueStatsApi))
            {
                var response = await queueStatsAction.ExecuteAsync(CancellationToken.None);
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }
        }

        [Fact]
        public async Task MalformedAssetQualifierGivesBadRequest()
        {
            var agentId = this.AnAgentId();

            var authorizer = this.AMockOf<IAuthorizer>();
            var dataAgentMap = this.AMockOf<IDataAgentMap>();
            var kustoRepository = this.AMockOf<ITelemetryRepository>();
            var dataAgentInfo = this.AMockOf<IDataAgentInfo>();

            dataAgentMap.SetupGet(m => m[agentId]).Returns(dataAgentInfo.Object);

            GetQueueStatsActionResult queueStatsAction = new GetQueueStatsActionResult(
                agentId,
                CreateQueueStatsRequest("malformed", string.Empty),
                dataAgentMap.Object,
                authorizer.Object,
                kustoRepository.Object);
            
            using (new FlightEnabled(FlightingNames.AllowedAgentIdForQueueStatsApi))
            {
                var response = await queueStatsAction.ExecuteAsync(CancellationToken.None);
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }
        }

        [Fact]
        public async Task InvalidAssetQualifierGivesBadRequest()
        {
            var agentId = this.AnAgentId();
            var assetQualifier = AssetQualifier.CreateForKusto("myCluster");

            var authorizer = this.AMockOf<IAuthorizer>();
            var dataAgentMap = this.AMockOf<IDataAgentMap>();
            var kustoRepository = this.AMockOf<ITelemetryRepository>();
            var dataAgentInfo = this.AMockOf<IDataAgentInfo>();
            var mockAssetGroupInfo = this.AMockOf<IAssetGroupInfo>();
            mockAssetGroupInfo.SetupGet(m => m.AssetGroupQualifier).Returns(AssetQualifier.CreateForKusto("notMyCluster").Value);

            dataAgentMap.SetupGet(m => m[agentId]).Returns(dataAgentInfo.Object);
            dataAgentInfo.SetupGet(m => m.AssetGroupInfos).Returns(new[] { mockAssetGroupInfo.Object });

            GetQueueStatsActionResult queueStatsAction = new GetQueueStatsActionResult(
                agentId,
                CreateQueueStatsRequest(assetQualifier.Value, string.Empty),
                dataAgentMap.Object,
                authorizer.Object,
                kustoRepository.Object);
            
            using (new FlightEnabled(FlightingNames.AllowedAgentIdForQueueStatsApi))
            {
                var response = await queueStatsAction.ExecuteAsync(CancellationToken.None);
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }
        }

        [Fact]
        public async Task MalformedCommandTypeGivesBadRequest()
        {
            var agentId = this.AnAgentId();

            var authorizer = this.AMockOf<IAuthorizer>();
            var dataAgentMap = this.AMockOf<IDataAgentMap>();
            var kustoRepository = this.AMockOf<ITelemetryRepository>();
            var dataAgentInfo = this.AMockOf<IDataAgentInfo>();

            dataAgentMap.SetupGet(m => m[agentId]).Returns(dataAgentInfo.Object);

            GetQueueStatsActionResult queueStatsAction = new GetQueueStatsActionResult(
                agentId,
                CreateQueueStatsRequest(string.Empty, "notGood"),
                dataAgentMap.Object,
                authorizer.Object,
                kustoRepository.Object);
            
            using (new FlightEnabled(FlightingNames.AllowedAgentIdForQueueStatsApi))
            {
                var response = await queueStatsAction.ExecuteAsync(CancellationToken.None);
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }
        }

        [Fact]
        public async Task ExecuteAsyncReturnsQueueStats()
        {
            var agentId = this.AnAgentId();

            var authorizer = this.AMockOf<IAuthorizer>();
            var dataAgentMap = this.AMockOf<IDataAgentMap>();
            var kustoRepository = this.AMockOf<ITelemetryRepository>();
            var dataAgentInfo = this.AMockOf<IDataAgentInfo>();
            var queueStats = new List<QueueStats>
            {
                new QueueStats()
                {
                    AssetGroupQualifier = "ag", CommandType = "AccountClose", PendingCommandCount = 10, Timestamp = DateTimeOffset.UtcNow
                }
            };

            dataAgentMap.SetupGet(m => m[agentId]).Returns(dataAgentInfo.Object);

            kustoRepository
                .Setup(m => m.GetAgentStats(It.IsAny<IDataAgentMap>(), agentId, It.IsAny<AssetGroupId>(), It.IsAny<PrivacyCommandType>()))
                .Returns(Task.FromResult(queueStats));

            GetQueueStatsActionResult queueStatsAction = new GetQueueStatsActionResult(
                agentId,
                CreateQueueStatsRequest(string.Empty, string.Empty),
                dataAgentMap.Object,
                authorizer.Object,
                kustoRepository.Object);
            
            using (new FlightEnabled(FlightingNames.AllowedAgentIdForQueueStatsApi))
            {
                var response = await queueStatsAction.ExecuteAsync(CancellationToken.None);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Fact]
        public async Task ExecuteAsyncWithAssetGroupAndCommandTypeReturnsQueueStats()
        {
            var agentId = this.AnAgentId();
            var assetGroupId = this.AnAssetGroupId();

            var authorizer = this.AMockOf<IAuthorizer>();
            var dataAgentMap = this.AMockOf<IDataAgentMap>();
            var kustoRepository = this.AMockOf<ITelemetryRepository>();
            var dataAgentInfo = this.AMockOf<IDataAgentInfo>();
            var assetQualifier = AssetQualifier.CreateForKusto("myCluster");
            var mockAssetGroupInfo = this.AMockOf<IAssetGroupInfo>();

            var queueStats = new List<QueueStats>
            {
                new QueueStats()
                {
                    AssetGroupQualifier = assetQualifier.Value, CommandType = "AccountClose", PendingCommandCount = 10, Timestamp = DateTimeOffset.UtcNow
                }
            };

            dataAgentMap.SetupGet(m => m[agentId]).Returns(dataAgentInfo.Object);
            dataAgentInfo.SetupGet(m => m.AssetGroupInfos).Returns(new[] { mockAssetGroupInfo.Object });
            mockAssetGroupInfo.SetupGet(m => m.AssetGroupQualifier).Returns(assetQualifier.Value);
            mockAssetGroupInfo.SetupGet(m => m.AssetGroupId).Returns(assetGroupId);
            kustoRepository
                .Setup(m => m.GetAgentStats(It.IsAny<IDataAgentMap>(), agentId, assetGroupId, PrivacyCommandType.AccountClose))
                .Returns(Task.FromResult(queueStats));

            GetQueueStatsActionResult queueStatsAction = new GetQueueStatsActionResult(
                agentId,
                CreateQueueStatsRequest(assetQualifier.Value, PrivacyCommandType.AccountClose.ToString()),
                dataAgentMap.Object,
                authorizer.Object,
                kustoRepository.Object);
            
            using (new FlightEnabled(FlightingNames.AllowedAgentIdForQueueStatsApi))
            {
                var response = await queueStatsAction.ExecuteAsync(CancellationToken.None);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        private static HttpRequestMessage CreateQueueStatsRequest(
            string assetQualifier,
            string commandType)
        {
            var request = new Fixture().Create<QueueStatsRequest>();

            request.AssetGroupQualifier = assetQualifier;
            request.CommandType = commandType;

            return new HttpRequestMessage(HttpMethod.Post, "/getqueuestats")
            {
                Content = new JsonContent(request)
            };
        }
    }
}

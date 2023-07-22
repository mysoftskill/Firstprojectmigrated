namespace PCF.UnitTests.Frontdoor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor;
    using Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor.ApiTrafficThrottling;
    using Microsoft.Windows.Services.AuthN.Server;

    using Moq;

    using Newtonsoft.Json;

    using Ploeh.AutoFixture;

    using Xunit;

    [Trait("Category", "UnitTest")]
    public class BatchCompleteCheckpointTests : INeedDataBuilders
    {
        private const int NumberOfCheckpoints = 5;

        private Mock<IApiTrafficHandler> mockApiTrafficHandler;

        public BatchCompleteCheckpointTests()
        {
            this.mockApiTrafficHandler = new Mock<IApiTrafficHandler>();
            this.mockApiTrafficHandler.Setup(
                m => m.ShouldAllowTraffic("PCF.ApiTrafficPercantage", "PostBatchCompleteCheckpoint", It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            this.mockApiTrafficHandler.Setup(
                m => m.GetTooManyRequestsResponse()).Returns(
                new HttpResponseMessage((HttpStatusCode)429)
                {
                    Content = new StringContent("Too Many Requests. Retry later with suggested delay in retry header."),
                    Headers = { RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(5)) }
                });
        }

        [Fact]
        public async Task AgentNotAuthorized()
        {
            AgentId agentId = this.AnAgentId();

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var repository = new Mock<ICommandHistoryRepository>();

            var checkpointRequest = this.CreateBatchCheckpointCompleteRequest(agentId, BatchCheckpointCompleteActionResult.MaximumBatchSize);

            authorizer.Setup(m => m.CheckAuthorizedAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<AgentId>()))
                .Throws(new AuthNException(AuthNErrorCode.AuthenticationFailed, "Some authN problem"));

            BatchCheckpointCompleteActionResult checkpointAction = new BatchCheckpointCompleteActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                null,                   // null publisher ensures not used.
                null,                   // null batchComplete publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                repository.Object,
                this.mockApiTrafficHandler.Object);

            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task NullContentRequest()
        {
            AgentId agentId = this.AnAgentId();

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var repository = new Mock<ICommandHistoryRepository>();

            var emptyRequest = new HttpRequestMessage(HttpMethod.Post, "/checkpoint")
            {
                Content = new StringContent(string.Empty)
            };

            BatchCheckpointCompleteActionResult checkpointAction = new BatchCheckpointCompleteActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                null,                   // null publisher ensures not used.
                null,                   // null batchComplete publisher ensures not used.
                emptyRequest,
                Logger.Instance,
                repository.Object,
                this.mockApiTrafficHandler.Object);

            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task EmptyContentRequest()
        {
            AgentId agentId = this.AnAgentId();

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var repository = new Mock<ICommandHistoryRepository>();

            var emptyRequest = new HttpRequestMessage(HttpMethod.Post, "/checkpoint")
            {
                Content = new StringContent("[]")
            };

            BatchCheckpointCompleteActionResult checkpointAction = new BatchCheckpointCompleteActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                null,                   // null publisher ensures not used.
                null,                   // null batchComplete publisher ensures not used.
                emptyRequest,
                Logger.Instance,
                repository.Object,
                this.mockApiTrafficHandler.Object);

            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task NonParsableRequest()
        {
            AgentId agentId = this.AnAgentId();

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var repository = new Mock<ICommandHistoryRepository>();

            var emptyRequest = new HttpRequestMessage(HttpMethod.Post, "/checkpoint")
            {
                Content = new StringContent("bad")
            };

            BatchCheckpointCompleteActionResult checkpointAction = new BatchCheckpointCompleteActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                null,                   // null publisher ensures not used.
                null,                   // null batchComplete publisher ensures not used.
                emptyRequest,
                Logger.Instance,
                repository.Object,
                this.mockApiTrafficHandler.Object);

            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains(nameof(JsonReaderException), content);
        }

        [Fact]
        public async Task RequestEntityTooLarge()
        {
            AgentId agentId = this.AnAgentId();

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var repository = new Mock<ICommandHistoryRepository>();

            var checkpointRequest = this.CreateBatchCheckpointCompleteRequest(agentId, BatchCheckpointCompleteActionResult.MaximumBatchSize + 1);

            BatchCheckpointCompleteActionResult checkpointAction = new BatchCheckpointCompleteActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                null,                   // null publisher ensures not used.
                null,                   // null batchComplete publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                repository.Object,
                this.mockApiTrafficHandler.Object);

            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);
            Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        }

        [Fact]
        public async Task NonParsableCommandId()
        {
            AgentId agentId = this.AnAgentId();

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var repository = new Mock<ICommandHistoryRepository>();

            var checkpointRequest = this.CreateBatchCheckpointCompleteRequest(agentId, NumberOfCheckpoints, null, null, cp => cp.CommandId = "badId");

            BatchCheckpointCompleteActionResult checkpointAction = new BatchCheckpointCompleteActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                null,                   // null publisher ensures not used.
                null,                   // null batchComplete publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                repository.Object,
                this.mockApiTrafficHandler.Object);

            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            IEnumerable<CheckpointCompleteResponse> failedCheckpoints = null;
            string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.Content.Headers.ContentType.MediaType.Contains("application/json"))
            {
                failedCheckpoints = JsonConvert.DeserializeObject<IEnumerable<CheckpointCompleteResponse>>(responseBody);
            }

            string errorMessage = $"One or more checkpointIds were null, white space or not parsable. {BatchCheckpointCompleteActionResult.CheckpointCompleteErrorCode.MalformedLeaseReceipt.ToString()}";
            Assert.All(failedCheckpoints.Select(fc => fc.Error), error => Assert.True(error == errorMessage));
        }

        [Fact]
        public async Task NonParsableLeaseReceipt()
        {
            AgentId agentId = this.AnAgentId();

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var repository = new Mock<ICommandHistoryRepository>();

            var checkpointRequest = this.CreateBatchCheckpointCompleteRequest(agentId, NumberOfCheckpoints, null, null, cp => cp.LeaseReceipt = "badLease");

            BatchCheckpointCompleteActionResult checkpointAction = new BatchCheckpointCompleteActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                null,                   // null publisher ensures not used.
                null,                   // null batchComplete publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                repository.Object,
                this.mockApiTrafficHandler.Object);

            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            IEnumerable<CheckpointCompleteResponse> failedCheckpoints = null;
            string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.Content.Headers.ContentType.MediaType.Contains("application/json"))
            {
                failedCheckpoints = JsonConvert.DeserializeObject<IEnumerable<CheckpointCompleteResponse>>(responseBody);
            }

            Assert.All(failedCheckpoints.Select(fc => fc.Error), error => Assert.True(error == BatchCheckpointCompleteActionResult.CheckpointCompleteErrorCode.MalformedLeaseReceipt.ToString()));
        }

        [Fact]
        public async Task LeaseReceiptAgentIdMismatch_DifferentFromCheckpointAgentId()
        {
            AgentId agentId = this.AnAgentId();
            AgentId agentId2 = this.AnAgentId();

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var repository = new Mock<ICommandHistoryRepository>();

            var checkpointRequest = this.CreateBatchCheckpointCompleteRequest(agentId2, NumberOfCheckpoints);

            BatchCheckpointCompleteActionResult checkpointAction = new BatchCheckpointCompleteActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                null,                   // null publisher ensures not used.
                null,                   // null batchComplete publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                repository.Object,
                this.mockApiTrafficHandler.Object);

            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            IEnumerable<CheckpointCompleteResponse> failedCheckpoints = null;
            string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.Content.Headers.ContentType.MediaType.Contains("application/json"))
            {
                failedCheckpoints = JsonConvert.DeserializeObject<IEnumerable<CheckpointCompleteResponse>>(responseBody);
            }

            Assert.All(failedCheckpoints.Select(fc => fc.Error), error => Assert.True(error == BatchCheckpointCompleteActionResult.CheckpointCompleteErrorCode.LeaseReceiptAgentIdMismatch.ToString()));
        }

        [Fact]
        public async Task CommandIdMismatch()
        {
            AgentId agentId = this.AnAgentId();

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var repository = new Mock<ICommandHistoryRepository>();

            var checkpointRequest = this.CreateBatchCheckpointCompleteRequest(agentId, NumberOfCheckpoints, null, null, cp => cp.CommandId = new CommandId(Guid.NewGuid()).ToString());

            BatchCheckpointCompleteActionResult checkpointAction = new BatchCheckpointCompleteActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                null,                   // null publisher ensures not used.
                null,                   // null batchComplete publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                repository.Object,
                this.mockApiTrafficHandler.Object);

            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            IEnumerable<CheckpointCompleteResponse> failedCheckpoints = null;
            string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.Content.Headers.ContentType.MediaType.Contains("application/json"))
            {
                failedCheckpoints = JsonConvert.DeserializeObject<IEnumerable<CheckpointCompleteResponse>>(responseBody);
            }

            Assert.All(failedCheckpoints.Select(fc => fc.Error), error => Assert.True(error == BatchCheckpointCompleteActionResult.CheckpointCompleteErrorCode.MalformedLeaseReceipt.ToString()));
        }

        [Fact]
        public async Task LeaseReceiptCommandExpired()
        {
            AgentId agentId = this.AnAgentId();

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var repository = new Mock<ICommandHistoryRepository>();

            commandQueue.Setup(m => m.SupportsLeaseReceipt(It.IsAny<LeaseReceipt>())).Returns(true);
            var checkpointRequest = this.CreateBatchCheckpointCompleteRequest(agentId, NumberOfCheckpoints, null, lr => lr.CommandCreatedTime = DateTimeOffset.UtcNow.AddDays(-60), null);

            BatchCheckpointCompleteActionResult checkpointAction = new BatchCheckpointCompleteActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                null,                   // null publisher ensures not used.
                null,                   // null batchComplete publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                repository.Object,
                this.mockApiTrafficHandler.Object);

            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            IEnumerable<CheckpointCompleteResponse> failedCheckpoints = null;
            string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.Content.Headers.ContentType.MediaType.Contains("application/json"))
            {
                failedCheckpoints = JsonConvert.DeserializeObject<IEnumerable<CheckpointCompleteResponse>>(responseBody);
            }

            Assert.All(failedCheckpoints.Select(fc => fc.Error), error => Assert.True(error == BatchCheckpointCompleteActionResult.CheckpointCompleteErrorCode.CommandAlreadyExpired.ToString()));
        }


        [Fact]
        public async Task LeaseReceiptAgentIdMismatch_FromDataAgentMapNotHavingAgentId()
        {
            AgentId agentId = this.AnAgentId();

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>(); // Missing AgentMap configuration -> try get agentId returns false;
            var repository = new Mock<ICommandHistoryRepository>();

            commandQueue.Setup(m => m.SupportsLeaseReceipt(It.IsAny<LeaseReceipt>())).Returns(true);

            var checkpointRequest = this.CreateBatchCheckpointCompleteRequest(agentId, NumberOfCheckpoints);
            BatchCheckpointCompleteActionResult checkpointAction = new BatchCheckpointCompleteActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                null,                   // null publisher ensures not used.
                null,                   // null batchComplete publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                repository.Object,
                this.mockApiTrafficHandler.Object);

            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            IEnumerable<CheckpointCompleteResponse> failedCheckpoints = null;
            string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.Content.Headers.ContentType.MediaType.Contains("application/json"))
            {
                failedCheckpoints = JsonConvert.DeserializeObject<IEnumerable<CheckpointCompleteResponse>>(responseBody);
            }

            Assert.All(failedCheckpoints.Select(fc => fc.Error), error => Assert.True(error == BatchCheckpointCompleteActionResult.CheckpointCompleteErrorCode.LeaseReceiptAgentIdMismatch.ToString()));
        }

        [Fact]
        public async Task LeaseReceiptAssetGroupIdMismatch()
        {
            AgentId agentId = this.AnAgentId();
            AssetGroupId assetGroupId = this.AnAssetGroupId();
            AssetGroupId badAssetGroupId = this.AnAssetGroupId();

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var repository = new Mock<ICommandHistoryRepository>();

            commandQueue.Setup(m => m.SupportsLeaseReceipt(It.IsAny<LeaseReceipt>())).Returns(true);
            var checkpointRequest = this.CreateBatchCheckpointCompleteRequest(agentId, NumberOfCheckpoints, null, lr => lr.AssetGroupId = assetGroupId);
            ConfigureDataAgentMap(dataAgentMap, agentId, badAssetGroupId);

            BatchCheckpointCompleteActionResult checkpointAction = new BatchCheckpointCompleteActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                null,                   // null publisher ensures not used.
                null,                   // null batchComplete publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                repository.Object,
                this.mockApiTrafficHandler.Object);

            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            IEnumerable<CheckpointCompleteResponse> failedCheckpoints = null;
            string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.Content.Headers.ContentType.MediaType.Contains("application/json"))
            {
                failedCheckpoints = JsonConvert.DeserializeObject<IEnumerable<CheckpointCompleteResponse>>(responseBody);
            }

            Assert.All(failedCheckpoints.Select(fc => fc.Error), error => Assert.True(error == BatchCheckpointCompleteActionResult.CheckpointCompleteErrorCode.LeaseReceiptAssetGroupIdMismatch.ToString()));
        }

        [Fact]
        public async Task LeaseReceiptNotSupported()
        {
            AgentId agentId = this.AnAgentId();
            AssetGroupId assetGroupId = this.AnAssetGroupId();

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var repository = new Mock<ICommandHistoryRepository>();

            var checkpointRequest = this.CreateBatchCheckpointCompleteRequest(agentId, NumberOfCheckpoints, null, lr => lr.AssetGroupId = assetGroupId);
            ConfigureDataAgentMap(dataAgentMap, agentId, assetGroupId);

            commandQueue.Setup(m => m.SupportsLeaseReceipt(It.IsAny<LeaseReceipt>())).Returns(false);

            BatchCheckpointCompleteActionResult checkpointAction = new BatchCheckpointCompleteActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                null,                   // null publisher ensures not used.
                null,                   // null batchComplete publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                repository.Object,
                this.mockApiTrafficHandler.Object);

            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            IEnumerable<CheckpointCompleteResponse> failedCheckpoints = null;
            string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.Content.Headers.ContentType.MediaType.Contains("application/json"))
            {
                failedCheckpoints = JsonConvert.DeserializeObject<IEnumerable<CheckpointCompleteResponse>>(responseBody);
            }

            Assert.All(failedCheckpoints.Select(fc => fc.Error), error => Assert.True(error == BatchCheckpointCompleteActionResult.CheckpointCompleteErrorCode.LeaseReceiptNotSupported.ToString()));
        }

        [Fact]
        public async Task InvalidLeaseVersion_CommandNotFound()
        {
            AgentId agentId = this.AnAgentId();
            AssetGroupId assetGroupId = this.AnAssetGroupId();

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var repository = new Mock<ICommandHistoryRepository>();

            repository.Setup(m => m.QueryPrivacyCommandAsync(It.IsAny<LeaseReceipt>()))
                .Returns(Task.FromResult<PrivacyCommand>(null));
            commandQueue.Setup(m => m.SupportsLeaseReceipt(It.IsAny<LeaseReceipt>())).Returns(true);

            ConfigureDataAgentMap(dataAgentMap, agentId, assetGroupId);
            HttpRequestMessage checkpointRequest = this.CreateBatchCheckpointCompleteRequest(
                agentId,
                NumberOfCheckpoints,
                null,
                lr =>
                {
                    lr.AssetGroupId = assetGroupId;
                    lr.Version = -1;
                });

            BatchCheckpointCompleteActionResult checkpointAction = new BatchCheckpointCompleteActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                null,                   // null publisher ensures not used.
                null,                   // null batchComplete publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                repository.Object,
                this.mockApiTrafficHandler.Object);

            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            IEnumerable<CheckpointCompleteResponse> failedCheckpoints = null;
            string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.Content.Headers.ContentType.MediaType.Contains("application/json"))
            {
                failedCheckpoints = JsonConvert.DeserializeObject<IEnumerable<CheckpointCompleteResponse>>(responseBody);
            }

            Assert.All(failedCheckpoints.Select(fc => fc.Error), error => Assert.True(error == BatchCheckpointCompleteActionResult.CheckpointCompleteErrorCode.CommandNotFound.ToString()));
        }

        [Fact]
        public async Task CommandNotFound_FromVariantsCheck()
        {
            AgentId agentId = this.AnAgentId();
            AssetGroupId assetGroupId = this.AnAssetGroupId();

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var repository = new Mock<ICommandHistoryRepository>();

            var checkpointRequest = this.CreateBatchCheckpointCompleteRequest(
                agentId,
                NumberOfCheckpoints,
                null,
                lr => lr.AssetGroupId = assetGroupId,
                cp => cp.VariantIds = new[] { Guid.NewGuid().ToString() });
            ConfigureDataAgentMap(dataAgentMap, agentId, assetGroupId);

            repository.Setup(m => m.QueryPrivacyCommandAsync(It.IsAny<LeaseReceipt>()))
                .Returns(Task.FromResult<PrivacyCommand>(null));
            commandQueue.Setup(m => m.SupportsLeaseReceipt(It.IsAny<LeaseReceipt>())).Returns(true);

            BatchCheckpointCompleteActionResult checkpointAction = new BatchCheckpointCompleteActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                null,                   // null publisher ensures not used.
                null,                   // null batchComplete publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                repository.Object,
                this.mockApiTrafficHandler.Object);
            
            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            IEnumerable<CheckpointCompleteResponse> failedCheckpoints = null;
            string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.Content.Headers.ContentType.MediaType.Contains("application/json"))
            {
                failedCheckpoints = JsonConvert.DeserializeObject<IEnumerable<CheckpointCompleteResponse>>(responseBody);
            }

            Assert.All(failedCheckpoints.Select(fc => fc.Error), error => Assert.True(error == BatchCheckpointCompleteActionResult.CheckpointCompleteErrorCode.CommandNotFound.ToString()));
        }

        [Fact]
        public async Task InvalidVariantsSpecified()
        {
            AgentId agentId = this.AnAgentId();
            AssetGroupId assetGroupId = this.AnAssetGroupId();

            var authenticator = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var repository = new Mock<ICommandHistoryRepository>();

            DeleteCommand deleteCommand = this.ADeleteCommand(agentId, assetGroupId);

            var checkpointRequest = this.CreateBatchCheckpointCompleteRequest(
                agentId,
                NumberOfCheckpoints,
                null,
                lr => lr.AssetGroupId = assetGroupId,
                cp => cp.VariantIds = new[] { Guid.NewGuid().ToString() });

            var assetGroup = ConfigureDataAgentMap(dataAgentMap, agentId, assetGroupId);

            assetGroup.Setup(m => m.VariantInfosAppliedByAgents).Returns(new List<IAssetGroupVariantInfo>());
            assetGroup.Setup(m => m.VariantInfosAppliedByPcf).Returns(new List<IAssetGroupVariantInfo>());


            repository.Setup(m => m.QueryPrivacyCommandAsync(It.IsAny<LeaseReceipt>()))
                .ReturnsAsync(deleteCommand);
            commandQueue.Setup(m => m.SupportsLeaseReceipt(It.IsAny<LeaseReceipt>())).Returns(true);

            BatchCheckpointCompleteActionResult checkpointAction = new BatchCheckpointCompleteActionResult(
                agentId,
                authenticator.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                null,                   // null publisher ensures not used.
                null,                   // null batchComplete publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                repository.Object,
                this.mockApiTrafficHandler.Object);

            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            IEnumerable<CheckpointCompleteResponse> failedCheckpoints = null;
            string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.Content.Headers.ContentType.MediaType.Contains("application/json"))
            {
                failedCheckpoints = JsonConvert.DeserializeObject<IEnumerable<CheckpointCompleteResponse>>(responseBody);
            }

            Assert.All(failedCheckpoints.Select(fc => fc.Error), error => Assert.True(error == BatchCheckpointCompleteActionResult.CheckpointCompleteErrorCode.InvalidVariantsSpecified.ToString()));
        }

        [Theory]
        [InlineData(new int[] { 60, 200, 600 }, 15)]
        [InlineData(new int[] { 86400, 86400, 86400 }, 21600)]
        [InlineData(new int[] { 86404, 86404, 86404 }, 21600)] // cap to 21600
        [InlineData(new int[] { -100, 86400, 86400 }, 0)] // negative results in 0
        public void CalculateRandomVisibilityDelay(int[] minimumRemainingLeaseTimesSecs, double expectedMaxVisibilityDelaySecs)
        {
            var now = DateTimeOffset.UtcNow;
            var leaseReceipts = minimumRemainingLeaseTimesSecs.Select(timeSecs =>
            {
                var lease = this.ALeaseReceipt().Build();
                lease.ApproximateExpirationTime = now.AddSeconds(timeSecs);
                return lease;
            });

            TimeSpan randomDelay = BatchCheckpointCompleteActionResult.CalculateRandomVisibilityDelay(leaseReceipts, now, out double maxDelay);

            Assert.Equal(expectedMaxVisibilityDelaySecs, maxDelay);
            Assert.True(randomDelay.TotalSeconds <= maxDelay);
        }

        [Fact]
        public async Task BatchCheckpointGetsThrottled()
        {
            AgentId agentId = this.AnAgentId();

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var repository = new Mock<ICommandHistoryRepository>();

            commandQueue.Setup(m => m.SupportsLeaseReceipt(It.IsAny<LeaseReceipt>())).Returns(true);
            var checkpointRequest = this.CreateBatchCheckpointCompleteRequest(agentId, NumberOfCheckpoints);


            // throttle traffic for given agent
            this.mockApiTrafficHandler.Setup(
                m => m.ShouldAllowTraffic("PCF.ApiTrafficPercantage", "PostBatchCompleteCheckpoint", agentId.ToString(), It.IsAny<string>())).Returns(false);

            BatchCheckpointCompleteActionResult checkpointAction = new BatchCheckpointCompleteActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                null,                   // null publisher ensures not used.
                null,                   // null batchComplete publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                repository.Object,
                this.mockApiTrafficHandler.Object);

            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);

            // assert
            Assert.Equal((HttpStatusCode)429, response.StatusCode);

            string content = await response.Content.ReadAsStringAsync();
            Assert.Equal("Too Many Requests. Retry later with suggested delay in retry header.", content);

            Assert.Equal(TimeSpan.FromSeconds(5), response.Headers.RetryAfter.Delta.Value);
        }

        private static CheckpointCompleteRequest CreateCheckpointCompleteRequest(
            LeaseReceipt leaseReceipt,
            Action<CheckpointCompleteRequest> customization = null)
        {
            var checkpoint = new Fixture().Create<CheckpointCompleteRequest>();

            checkpoint.LeaseReceipt = leaseReceipt.Serialize();
            checkpoint.VariantIds = null;
            checkpoint.CommandId = leaseReceipt.CommandId.Value;
            customization?.Invoke(checkpoint);

            return checkpoint;
        }

        private HttpRequestMessage CreateBatchCheckpointCompleteRequest(
            AgentId agentId,
            int batchSize,
            IEnumerable<CheckpointCompleteRequest> customCheckpoint = null,
            Action<LeaseReceipt> leaseCustomization = null,
            Action<CheckpointCompleteRequest> requestCustomization = null)
        {
            List<CheckpointCompleteRequest> checkpoints = Enumerable.Range(1, batchSize)
                .Select(
                    i =>
                    {
                        LeaseReceipt lease = this.ALeaseReceipt(agentId);
                        leaseCustomization?.Invoke(lease);
                        return lease;
                    })
                .Select(lr => CreateCheckpointCompleteRequest(lr, requestCustomization)).ToList();

            if (customCheckpoint != null)
            {
                checkpoints.AddRange(customCheckpoint);
            }

            return new HttpRequestMessage(HttpMethod.Post, "/checkpoint")
            {
                Content = new JsonContent(checkpoints)
            };
        }

        private static Mock<IAssetGroupInfo> ConfigureDataAgentMap(Mock<IDataAgentMap> dataAgentMap, AgentId agentId, AssetGroupId assetGroupId)
        {
            Mock<IAssetGroupInfo> mockAssetGroupInfo = new Mock<IAssetGroupInfo>();
            mockAssetGroupInfo.SetupGet(m => m.AssetGroupId).Returns(assetGroupId);
            IAssetGroupInfo assetGroupInfo = mockAssetGroupInfo.Object;

            Mock<IDataAgentInfo> dataAgentInfo = new Mock<IDataAgentInfo>();
            dataAgentInfo.SetupGet(m => m.AssetGroupInfos).Returns(new[] { mockAssetGroupInfo.Object });
            dataAgentInfo.Setup(m => m.TryGetAssetGroupInfo(It.IsAny<AssetGroupId>(), out assetGroupInfo)).Returns(true);

            IDataAgentInfo info = dataAgentInfo.Object;
            dataAgentMap.Setup(m => m.TryGetAgent(It.IsAny<AgentId>(), out info)).Returns(true);
            dataAgentMap.SetupGet(m => m[agentId]).Returns(dataAgentInfo.Object);
            dataAgentMap.Setup(m => m.TryGetAgent(agentId, out info)).Returns(true);

            return mockAssetGroupInfo;
        }
    }
}

namespace PCF.UnitTests
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor;
    using Microsoft.Windows.Services.AuthN.Server;

    using Moq;

    using Newtonsoft.Json;

    using Ploeh.AutoFixture;

    using Xunit;

    [Trait("Category", "UnitTest")]
    public class ReplayCommandsTest : INeedDataBuilders
    {
        [Fact]
        public async Task AgentNotAuthorized()
        {
            var agentId = this.AnAgentId();

            var authorizer = this.AMockOf<IAuthorizer>();
            var map = this.AMockOf<IDataAgentMap>();

            var replayCommandsRequest = CreateReplayCommandsRequest(
                DateTimeOffset.UtcNow.AddDays(-5), DateTimeOffset.UtcNow);

            authorizer.Setup(m => m.CheckAuthorizedAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<AgentId>()))
                         .Throws(new AuthNException(AuthNErrorCode.AuthenticationFailed, "Some authN problem"));

            var replayCommandsActionResult = new ReplayCommandsActionResult(
                agentId,
                replayCommandsRequest,
                map.Object,
                authorizer.Object,
                AuthenticationScope.Agent,
                null,
                null,
                null,
                null);

            var response = await replayCommandsActionResult.ExecuteAsync(CancellationToken.None);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task MalformedAssetQualifier()
        {
            var agentId = this.AnAgentId();

            var authorizer = this.AMockOf<IAuthorizer>();
            var map = this.AMockOf<IDataAgentMap>();

            var fakeInvalidAssetQualifier = "InvalidAssetQualifier";
            var replayCommandsRequest = CreateReplayCommandsRequest(
                DateTimeOffset.UtcNow.AddDays(-5), DateTimeOffset.UtcNow, new string[] { fakeInvalidAssetQualifier });

            var replayCommandsActionResult = new ReplayCommandsActionResult(
                agentId,
                replayCommandsRequest,
                map.Object,
                authorizer.Object,
                AuthenticationScope.Agent,
                null,
                null,
                null,
                null);

            var response = await replayCommandsActionResult.ExecuteAsync(CancellationToken.None);

            await AssertReplayCommandsErrorAsync(response, HttpStatusCode.BadRequest, ReplayCommandsActionResult.ReplayCommandsErrorCode.MalformedAssetQualifier);
        }

        [Fact]
        public async Task InvalidReplayDates()
        {
            var agentId = this.AnAgentId();

            var authorizer = this.AMockOf<IAuthorizer>();
            var map = this.AMockOf<IDataAgentMap>();

            var agentInfo = this.AMockOf<IDataAgentInfo>();
            var assetGroupInfo = this.AMockOf<IAssetGroupInfo>();
            map.SetupGet(x => x[agentId]).Returns(agentInfo.Object);
            agentInfo.SetupGet(x => x.AssetGroupInfos).Returns(new[] { assetGroupInfo.Object });

            var replayCommandsRequest = CreateReplayCommandsRequest(
                DateTimeOffset.UtcNow.AddDays(-50), DateTimeOffset.UtcNow);

            var replayCommandsActionResult = new ReplayCommandsActionResult(
                agentId,
                replayCommandsRequest,
                map.Object,
                authorizer.Object,
                AuthenticationScope.Agent,
                null,
                null,
                null,
                null);

            var response = await replayCommandsActionResult.ExecuteAsync(CancellationToken.None);

            await AssertReplayCommandsErrorAsync(response, HttpStatusCode.BadRequest, ReplayCommandsActionResult.ReplayCommandsErrorCode.InvalidReplayDates);
        }

        [Fact]
        public async Task NullReplayDates()
        {
            var agentId = this.AnAgentId();

            var authorizer = this.AMockOf<IAuthorizer>();
            var map = this.AMockOf<IDataAgentMap>();

            var agentInfo = this.AMockOf<IDataAgentInfo>();
            var assetGroupInfo = this.AMockOf<IAssetGroupInfo>();
            map.SetupGet(x => x[agentId]).Returns(agentInfo.Object);
            agentInfo.SetupGet(x => x.AssetGroupInfos).Returns(new[] { assetGroupInfo.Object });

            var replayCommandsRequest = CreateReplayCommandsRequest(
                null, null);

            var replayCommandsActionResult = new ReplayCommandsActionResult(
                agentId,
                replayCommandsRequest,
                map.Object,
                authorizer.Object,
                AuthenticationScope.Agent,
                null,
                null,
                null,
                null);

            var response = await replayCommandsActionResult.ExecuteAsync(CancellationToken.None);

            await AssertReplayCommandsErrorAsync(response, HttpStatusCode.BadRequest, ReplayCommandsActionResult.ReplayCommandsErrorCode.InvalidReplayDates);
        }

        [Fact]
        public async Task ReplayForAllAssetGroupsByDates()
        {
            var agentId = this.AnAgentId();

            var authorizer = this.AMockOf<IAuthorizer>();
            var map = this.AMockOf<IDataAgentMap>();
            var requestPublisher = this.AMockOf<IAzureWorkItemQueuePublisher<ReplayRequestWorkItem>>();

            var agentInfo = this.AMockOf<IDataAgentInfo>();
            var assetGroupInfo = this.AMockOf<IAssetGroupInfo>();
            map.SetupGet(x => x[agentId]).Returns(agentInfo.Object);
            agentInfo.SetupGet(x => x.AssetGroupInfos).Returns(new[] { assetGroupInfo.Object });

            var replayCommandsRequest = CreateReplayCommandsRequest(
                DateTimeOffset.UtcNow.AddDays(-5), DateTimeOffset.UtcNow);

            var replayCommandsActionResult = new ReplayCommandsActionResult(
                agentId,
                replayCommandsRequest,
                map.Object,
                authorizer.Object,
                AuthenticationScope.Agent,
                requestPublisher.Object,
                null,
                null,
                null);

            var response = await replayCommandsActionResult.ExecuteAsync(CancellationToken.None);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ReplayCommandsTooLarge()
        {
            var agentId = this.AnAgentId();

            var authorizer = this.AMockOf<IAuthorizer>();
            var map = this.AMockOf<IDataAgentMap>();

            var agentInfo = this.AMockOf<IDataAgentInfo>();
            var assetGroupInfo = this.AMockOf<IAssetGroupInfo>();
            map.SetupGet(x => x[agentId]).Returns(agentInfo.Object);
            agentInfo.SetupGet(x => x.AssetGroupInfos).Returns(new[] { assetGroupInfo.Object });

            var count = ReplayCommandsActionResult.MaximumReplayCommands + 1;
            string[] commands = new string[count];

            var replayCommandsRequest = CreateReplayCommandsRequest(
                DateTimeOffset.UtcNow.AddDays(-50), DateTimeOffset.UtcNow, commandIds: commands);

            var replayCommandsActionResult = new ReplayCommandsActionResult(
                agentId,
                replayCommandsRequest,
                map.Object,
                authorizer.Object,
                AuthenticationScope.Agent,
                null,
                null,
                null,
                null);

            var response = await replayCommandsActionResult.ExecuteAsync(CancellationToken.None);

            await AssertReplayCommandsErrorAsync(response, HttpStatusCode.BadRequest, ReplayCommandsActionResult.ReplayCommandsErrorCode.CommandsExceedsMaxNumberAllowed);
        }

        [Fact]
        public async Task ReplayCommandIdInvalid()
        {
            var agentId = this.AnAgentId();

            var authorizer = this.AMockOf<IAuthorizer>();
            var map = this.AMockOf<IDataAgentMap>();
            var requestPublisher = this.AMockOf<IAzureWorkItemQueuePublisher<ReplayRequestWorkItem>>();
            var enqueuePublisher = this.AMockOf<IAzureWorkItemQueuePublisher<EnqueueBatchReplayCommandsWorkItem>>();
            var commandHistory = this.AMockOf<ICommandHistoryRepository>();
            var appConfig = this.AMockOf<IAppConfiguration>();

            var agentInfo = this.AMockOf<IDataAgentInfo>();
            var assetGroupInfo = this.AMockOf<IAssetGroupInfo>();
            map.SetupGet(x => x[agentId]).Returns(agentInfo.Object);
            agentInfo.SetupGet(x => x.AssetGroupInfos).Returns(new[] { assetGroupInfo.Object });

            string commandId = "abcde";
            string[] commands = new string[] { commandId, commandId };

            var replayCommandsRequest = CreateReplayCommandsRequest(
                DateTimeOffset.UtcNow.AddDays(-50), DateTimeOffset.UtcNow, commandIds: commands);

            var replayCommandsActionResult = new ReplayCommandsActionResult(
                agentId,
                replayCommandsRequest,
                map.Object,
                authorizer.Object,
                AuthenticationScope.Agent,
                requestPublisher.Object,
                enqueuePublisher.Object,
                commandHistory.Object,
                appConfig.Object);

            var response = await replayCommandsActionResult.ExecuteAsync(CancellationToken.None);

            await AssertReplayCommandsErrorAsync(response, HttpStatusCode.BadRequest, ReplayCommandsActionResult.ReplayCommandsErrorCode.InvalidCommandIds, $"[CommandId:{commandId},Error:InvalidFormat];");
        }

        [Fact]
        public async Task ReplayCommandIdNotFound()
        {
            var agentId = this.AnAgentId();

            var authorizer = this.AMockOf<IAuthorizer>();
            var map = this.AMockOf<IDataAgentMap>();
            var commandHistory = this.AMockOf<ICommandHistoryRepository>();
            var appConfig = this.AMockOf<IAppConfiguration>();

            var agentInfo = this.AMockOf<IDataAgentInfo>();
            var assetGroupInfo = this.AMockOf<IAssetGroupInfo>();
            map.SetupGet(x => x[agentId]).Returns(agentInfo.Object);
            agentInfo.SetupGet(x => x.AssetGroupInfos).Returns(new[] { assetGroupInfo.Object });

            commandHistory.Setup(x => x.QueryAsync(It.IsAny<CommandId>(), CommandHistoryFragmentTypes.Core)).Returns(Task.FromResult((CommandHistoryRecord)null));

            var commandId = this.ACommandId();
            string[] commands = new string[] { commandId.ToString() };

            var replayCommandsRequest = CreateReplayCommandsRequest(
                DateTimeOffset.UtcNow.AddDays(-50), DateTimeOffset.UtcNow, commandIds: commands);

            var replayCommandsActionResult = new ReplayCommandsActionResult(
                agentId,
                replayCommandsRequest,
                map.Object,
                authorizer.Object,
                AuthenticationScope.Agent,
                null,
                null,
                commandHistory.Object,
                appConfig.Object);

            var response = await replayCommandsActionResult.ExecuteAsync(CancellationToken.None);

            await AssertReplayCommandsErrorAsync(response, HttpStatusCode.BadRequest, ReplayCommandsActionResult.ReplayCommandsErrorCode.InvalidCommandIds, $"[CommandId:{commandId.ToString()},Error:NotFound];");
        }

        [Fact]
        public async Task ReplayForAllAssetGroupsByIds()
        {
            var agentId = this.AnAgentId();

            var authorizer = this.AMockOf<IAuthorizer>();
            var map = this.AMockOf<IDataAgentMap>();
            var enqueuePublisher = this.AMockOf<IAzureWorkItemQueuePublisher<EnqueueBatchReplayCommandsWorkItem>>();
            var commandHistory = this.AMockOf<ICommandHistoryRepository>();
            var appConfig = this.AMockOf<IAppConfiguration>();

            var agentInfo = this.AMockOf<IDataAgentInfo>();
            var assetGroupInfo = this.AMockOf<IAssetGroupInfo>();
            map.SetupGet(x => x[agentId]).Returns(agentInfo.Object);
            agentInfo.SetupGet(x => x.AssetGroupInfos).Returns(new[] { assetGroupInfo.Object });

            var commandId = this.ACommandId();
            string[] commands = new string[] { commandId.ToString() };
            CommandHistoryRecord record = this.AColdStorageCommandRecord(commandId).Build();
            commandHistory.Setup(x => x.QueryAsync(It.IsAny<CommandId>(), CommandHistoryFragmentTypes.Core)).Returns(Task.FromResult(record));
            var replayCommandsRequest = CreateReplayCommandsRequest(
                DateTimeOffset.UtcNow.AddDays(-5), DateTimeOffset.UtcNow, commandIds: commands);

            var replayCommandsActionResult = new ReplayCommandsActionResult(
                agentId,
                replayCommandsRequest,
                map.Object,
                authorizer.Object,
                AuthenticationScope.Agent,
                null,
                enqueuePublisher.Object,
                commandHistory.Object,
                appConfig.Object);

            var response = await replayCommandsActionResult.ExecuteAsync(CancellationToken.None);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ReplayExportCommands()
        {
            var agentId = this.AnAgentId();

            var authorizer = this.AMockOf<IAuthorizer>();
            var map = this.AMockOf<IDataAgentMap>();
            var enqueuePublisher = this.AMockOf<IAzureWorkItemQueuePublisher<EnqueueBatchReplayCommandsWorkItem>>();
            var commandHistory = this.AMockOf<ICommandHistoryRepository>();
            var appConfig = this.AMockOf<IAppConfiguration>();

            var agentInfo = this.AMockOf<IDataAgentInfo>();
            var assetGroupInfo = this.AMockOf<IAssetGroupInfo>();
            map.SetupGet(x => x[agentId]).Returns(agentInfo.Object);
            agentInfo.SetupGet(x => x.AssetGroupInfos).Returns(new[] { assetGroupInfo.Object });

            var commandId = this.ACommandId();
            string[] commands = new string[] { commandId.ToString() };
            CommandHistoryRecord record = this.AColdStorageCommandRecord(commandId).Build();
            commandHistory.Setup(x => x.QueryAsync(It.IsAny<CommandId>(), CommandHistoryFragmentTypes.Core)).Returns(Task.FromResult(record));
            var replayCommandsRequest = CreateReplayCommandsRequest(
                DateTimeOffset.UtcNow.AddDays(-5), DateTimeOffset.UtcNow, commandIds: commands, 
                includeExportCommands: true);
            
            appConfig.Setup(a => a.IsFeatureFlagEnabledAsync(FeatureNames.PCF.EnableExportCommandReplay, It.IsAny<bool>()))
                .ReturnsAsync(true);

            var replayCommandsActionResult = new ReplayCommandsActionResult(
                agentId,
                replayCommandsRequest,
                map.Object,
                authorizer.Object,
                AuthenticationScope.Agent,
                null,
                enqueuePublisher.Object,
                commandHistory.Object,
                appConfig.Object);

            var response = await replayCommandsActionResult.ExecuteAsync(CancellationToken.None);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        private static async Task AssertReplayCommandsErrorAsync(
            HttpResponseMessage response,
            HttpStatusCode httpStatus,
            ReplayCommandsActionResult.ReplayCommandsErrorCode expectedErrorCode,
            string expectedErrorMessage = null)
        {
            Assert.Equal(response.StatusCode, httpStatus);

            string content = await response.Content.ReadAsStringAsync();
            var error = JsonConvert.DeserializeObject<ReplayCommandsActionResult.ReplayCommandsError>(content);

            Assert.Equal(expectedErrorCode, error.ErrorCode);

            if (expectedErrorMessage != null)
            {
                Assert.Equal(expectedErrorMessage, error.Message);
            }
            else
            {
                Assert.Equal(expectedErrorCode.ToString(), error.Message);
            }
        }

        private static HttpRequestMessage CreateReplayCommandsRequest(
            DateTimeOffset? replayFromDate,
            DateTimeOffset? replayToDate,
            string[] assetGroupQualifiers = null,
            string[] commandIds = null,
            bool? includeExportCommands = null)
        {
            var request = new Fixture().Create<ReplayCommandsRequest>();

            request.ReplayFromDate = replayFromDate;
            request.ReplayToDate = replayToDate;
            request.AssetQualifiers = assetGroupQualifiers;
            request.CommandIds = commandIds;
            request.IncludeExportCommands = includeExportCommands;

            return new HttpRequestMessage(HttpMethod.Post, "/replaycommands")
            {
                Content = new JsonContent(request)
            };
        }
    }
}

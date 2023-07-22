namespace PCF.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor;
    using Microsoft.PrivacyServices.SignalApplicability;
    using Microsoft.Windows.Services.AuthN.Server;
    using Moq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;

    [Trait("Category", "UnitTest")]
    public class FrontdoorQueryCommandTests : INeedDataBuilders
    {
        [Fact]
        public async Task AgentNotAuthorized()
        {
            var agentId = this.AnAgentId();
            var leaseReceipt = this.ALeaseReceipt(agentId);

            var authorizer = this.AMockOf<IAuthorizer>();
            var commandQueue = this.AMockOf<ICommandQueue>();
            var publisher = this.AMockOf<ICommandLifecycleEventPublisher>();
            var map = this.AMockOf<IDataAgentMap>();
            var commandHistory = this.AMockOf<ICommandHistoryRepository>();

            var queryCommandRequest = CreateQueryCommandRequest(leaseReceipt);

            authorizer.Setup(m => m.CheckAuthorizedAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<AgentId>()))
                         .Throws(new AuthNException(AuthNErrorCode.AuthenticationFailed, "Some authN problem"));

            QueryCommandActionResult queryCommandAction = new QueryCommandActionResult(
                agentId,
                queryCommandRequest,
                commandQueue.Object,
                commandHistory.Object,
                map.Object,
                publisher.Object,
                authorizer.Object);

            var response = await queryCommandAction.ExecuteAsync(CancellationToken.None);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task LeaseReceiptAgentIdMismatch()
        {
            var agentId = this.AnAgentId();
            var leaseReceipt = this.ALeaseReceipt();

            var authorizer = this.AMockOf<IAuthorizer>();
            var commandQueue = this.AMockOf<ICommandQueue>();
            var publisher = this.AMockOf<ICommandLifecycleEventPublisher>();
            var map = this.AMockOf<IDataAgentMap>();
            var commandHistory = this.AMockOf<ICommandHistoryRepository>();

            var queryCommandRequest = CreateQueryCommandRequest(leaseReceipt);

            QueryCommandActionResult queryCommandAction = new QueryCommandActionResult(
                agentId,
                queryCommandRequest,
                commandQueue.Object,
                commandHistory.Object,
                map.Object,
                publisher.Object,
                authorizer.Object);

            var response = await queryCommandAction.ExecuteAsync(CancellationToken.None);
            await AssertCheckpointErrorAsync(response, HttpStatusCode.BadRequest, QueryCommandActionResult.QueryCommandErrorCode.LeaseReceiptAgentIdMismatch);
        }

        [Fact]
        public async Task QueryCommandNotQueryable()
        {
            var agentId = this.AnAgentId();
            var leaseReceipt = this.ALeaseReceipt(agentId);
            leaseReceipt.WithValue(x => x.QueueStorageType, QueueStorageType.AzureQueueStorage);
            leaseReceipt.WithValue(x => x.Version, 3);

            var authorizer = this.AMockOf<IAuthorizer>();
            var commandQueue = this.AMockOf<ICommandQueue>();
            var publisher = this.AMockOf<ICommandLifecycleEventPublisher>();
            var map = this.AMockOf<IDataAgentMap>();
            var commandHistory = this.AMockOf<ICommandHistoryRepository>();

            var queryCommandRequest = CreateQueryCommandRequest(leaseReceipt);

            QueryCommandActionResult queryCommandAction = new QueryCommandActionResult(
                agentId,
                queryCommandRequest,
                commandQueue.Object,
                commandHistory.Object,
                map.Object,
                publisher.Object,
                authorizer.Object);

            var response = await queryCommandAction.ExecuteAsync(CancellationToken.None);
            await AssertCheckpointErrorAsync(response, HttpStatusCode.MethodNotAllowed, QueryCommandActionResult.QueryCommandErrorCode.CommandNotQueryable);
        }

        [Fact]
        public async Task CommandDoesNotExist()
        {
            var agentId = this.AnAgentId();
            var leaseReceipt = this.ALeaseReceipt(agentId);

            var authorizer = this.AMockOf<IAuthorizer>();
            var commandQueue = this.AMockOf<ICommandQueue>();
            var publisher = this.AMockOf<ICommandLifecycleEventPublisher>();
            var map = this.AMockOf<IDataAgentMap>();
            var commandHistory = this.AMockOf<ICommandHistoryRepository>();

            var queryCommandRequest = CreateQueryCommandRequest(leaseReceipt);

            QueryCommandActionResult queryCommandAction = new QueryCommandActionResult(
                agentId,
                queryCommandRequest,
                commandQueue.Object,
                commandHistory.Object,
                map.Object,
                publisher.Object,
                authorizer.Object);

            var response = await queryCommandAction.ExecuteAsync(CancellationToken.None);
            await AssertCheckpointErrorAsync(response, HttpStatusCode.BadRequest, QueryCommandActionResult.QueryCommandErrorCode.CommandNotFound);
        }
                
        [Fact]
        public async Task CommandFailed()
        {
            var agentId = this.AnAgentId();
            var commandId = this.ACommandId();
            var deleteCommand = this.ADeleteCommand(agentId, null, commandId).Build();

            var leaseReceipt = this.ALeaseReceipt(deleteCommand.AgentId, deleteCommand.AssetGroupId, deleteCommand.CommandId, deleteCommand.Subject.GetSubjectType()).Build();

            var authorizer = this.AMockOf<IAuthorizer>();
            var commandQueue = this.AMockOf<ICommandQueue>();
            var publisher = this.AMockOf<ICommandLifecycleEventPublisher>();
            var dataAgentMap = this.AMockOf<IDataAgentMap>();
            var commandHistory = this.AMockOf<ICommandHistoryRepository>();

            commandQueue.Setup(m => m.QueryCommandAsync(It.IsAny<LeaseReceipt>()))
                        .ReturnsAsync(deleteCommand);

            commandQueue.Setup(m => m.ReplaceAsync(It.IsAny<LeaseReceipt>(), It.IsAny<PrivacyCommand>(), It.IsAny<CommandReplaceOperations>()))
                        .Callback<LeaseReceipt, PrivacyCommand, CommandReplaceOperations>((lr, pc, cro) =>
                        {
                            Assert.True(!string.IsNullOrEmpty(pc.AgentState));
                            Assert.True(pc.NextVisibleTime <= DateTimeOffset.UtcNow);
                        })
                        .Returns(Task.FromResult(leaseReceipt));

            Mock<IAssetGroupInfo> mockAssetGroupInfo = ConfigureDataAgentMap(dataAgentMap, leaseReceipt);
            mockAssetGroupInfo.SetupGet(m => m.DelinkApproved).Returns(true);

            // Applicablity mock
            ApplicabilityResult applicabilityResult = new ApplicabilityResult();
            mockAssetGroupInfo.Setup(m => m.IsCommandActionable(It.IsAny<PrivacyCommand>(), out applicabilityResult)).Returns(true);

            var queryCommandRequest = CreateQueryCommandRequest(leaseReceipt, r => r.Status = PrivacyCommandStatus.Failed.ToString());

            QueryCommandActionResult queryCommandAction = new QueryCommandActionResult(
                agentId,
                queryCommandRequest,
                commandQueue.Object,
                commandHistory.Object,
                dataAgentMap.Object,
                publisher.Object,
                authorizer.Object);

            var response = await queryCommandAction.ExecuteAsync(CancellationToken.None);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CommandPending()
        {
            var agentId = this.AnAgentId();
            var commandId = this.ACommandId();
            var deleteCommand = this.ADeleteCommand(agentId, null, commandId).Build();

            var leaseReceipt = this.ALeaseReceipt(deleteCommand.AgentId, deleteCommand.AssetGroupId, deleteCommand.CommandId, deleteCommand.Subject.GetSubjectType()).Build();

            var authorizer = this.AMockOf<IAuthorizer>();
            var commandQueue = this.AMockOf<ICommandQueue>();
            var publisher = this.AMockOf<ICommandLifecycleEventPublisher>();
            var map = this.AMockOf<IDataAgentMap>();
            var commandHistory = this.AMockOf<ICommandHistoryRepository>();

            commandQueue.Setup(m => m.QueryCommandAsync(It.IsAny<LeaseReceipt>()))
                        .ReturnsAsync(deleteCommand);

            Mock<IAssetGroupInfo> mockAssetGroupInfo = ConfigureDataAgentMap(map, leaseReceipt);
            mockAssetGroupInfo.SetupGet(m => m.DelinkApproved).Returns(true);

            // Applicablity mock
            ApplicabilityResult applicabilityResult = new ApplicabilityResult();
            mockAssetGroupInfo.Setup(m => m.IsCommandActionable(It.IsAny<PrivacyCommand>(), out applicabilityResult)).Returns(true);

            var queryCommandRequest = CreateQueryCommandRequest(leaseReceipt, r => r.Status = PrivacyCommandStatus.Failed.ToString());

            QueryCommandActionResult queryCommandAction = new QueryCommandActionResult(
                agentId,
                queryCommandRequest,
                commandQueue.Object,
                commandHistory.Object,
                map.Object,
                publisher.Object,
                authorizer.Object);

            var response = await queryCommandAction.ExecuteAsync(CancellationToken.None);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        private static HttpRequestMessage CreateQueryCommandRequest(
            LeaseReceipt leaseReceipt,
            Action<CheckpointRequest> customization = null)
        {
            var request = new Fixture().Create<CheckpointRequest>();

            request.LeaseReceipt = leaseReceipt.Serialize();
            request.Status = PrivacyCommandStatus.Pending.ToString();
            request.Variants = null;
            customization?.Invoke(request);

            return new HttpRequestMessage(HttpMethod.Post, "/command")
            {
                Content = new JsonContent(request)
            };
        }

        private static async Task AssertCheckpointErrorAsync(
            HttpResponseMessage response,
            HttpStatusCode httpStatus,
            QueryCommandActionResult.QueryCommandErrorCode expectedError)
        {
            Assert.Equal(response.StatusCode, httpStatus);

            string content = await response.Content.ReadAsStringAsync();
            var error = JsonConvert.DeserializeObject<QueryCommandActionResult.QueryCommandError>(content);

            Assert.Equal(expectedError, error.ErrorCode);
        }

        private static Mock<IAssetGroupInfo> ConfigureDataAgentMap(Mock<IDataAgentMap> dataAgentMap, LeaseReceipt leaseReceipt)
        {
            Mock<IAssetGroupInfo> assetGroupInfo = new Mock<IAssetGroupInfo>();
            assetGroupInfo.SetupGet(m => m.AssetGroupId).Returns(leaseReceipt.AssetGroupId);
            assetGroupInfo.Setup(m => m.VariantInfosAppliedByAgents).Returns(new List<IAssetGroupVariantInfo>());
            assetGroupInfo.Setup(m => m.VariantInfosAppliedByPcf).Returns(new List<IAssetGroupVariantInfo>());

            IAssetGroupInfo groupInfo = assetGroupInfo.Object;

            Mock<IDataAgentInfo> dataAgentInfo = new Mock<IDataAgentInfo>();
            dataAgentInfo.SetupGet(m => m.AssetGroupInfos).Returns(new[] { assetGroupInfo.Object });
            dataAgentInfo.Setup(m => m.TryGetAssetGroupInfo(leaseReceipt.AssetGroupId, out groupInfo)).Returns(true);

            IDataAgentInfo agentInfo = dataAgentInfo.Object;
            
            dataAgentMap.SetupGet(m => m[leaseReceipt.AgentId]).Returns(dataAgentInfo.Object);
            dataAgentMap.Setup(m => m.TryGetAgent(leaseReceipt.AgentId, out agentInfo)).Returns(true);

            return assetGroupInfo;
        }
    }
}

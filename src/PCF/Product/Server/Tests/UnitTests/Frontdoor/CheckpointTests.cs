namespace PCF.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor;
    using Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor.ApiTrafficThrottling;
    using Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache;
    using Microsoft.PrivacyServices.SignalApplicability;
    using Microsoft.Windows.Services.AuthN.Server;
    using Moq;
    using Newtonsoft.Json;
    using Ploeh.AutoFixture;
    using Xunit;
    using Common = Microsoft.PrivacyServices.CommandFeed.Service.Common;

    [Trait("Category", "UnitTest")]
    public class FrontdoorCheckpointTests : INeedDataBuilders
    {
        private Mock<ICommandHistoryRepository> mockCommandHistory = new Mock<ICommandHistoryRepository>(MockBehavior.Strict);

        private Mock<IApiTrafficHandler> mockApiTrafficHandler;

        public FrontdoorCheckpointTests()
        {
            this.mockApiTrafficHandler = new Mock<IApiTrafficHandler>();
            this.mockApiTrafficHandler.Setup(
                m => m.ShouldAllowTraffic("PCF.ApiTrafficPercantage", "PostCheckpoint", It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            this.mockApiTrafficHandler.Setup(
                m => m.GetTooManyRequestsResponse()).Returns(
                new HttpResponseMessage((HttpStatusCode)429)
                {
                    Content = new StringContent("Too Many Requests. Retry later with suggested delay in retry header."),
                    Headers = { RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(5)) }
                });
        }

        [Theory]
        [InlineData(null, 0, CommandReplaceOperations.LeaseExtension)]
        [InlineData("", 0, CommandReplaceOperations.LeaseExtension)]
        [InlineData("", -1, CommandReplaceOperations.LeaseExtension)]
        [InlineData("", 1, CommandReplaceOperations.LeaseExtension)]
        [InlineData("", int.MaxValue, CommandReplaceOperations.LeaseExtension)]
        [InlineData("agent state here", 1, CommandReplaceOperations.LeaseExtension | CommandReplaceOperations.CommandContent)]
        [InlineData("agent state here", 0, CommandReplaceOperations.CommandContent)]
        public void ConvertToCommandReplaceOperationsSuccess(string agentState, int leaseExtensionSeconds, CommandReplaceOperations expectedCommandReplaceOperations)
        {
            CheckpointRequest checkpointRequest = new CheckpointRequest { AgentState = agentState, LeaseExtensionSeconds = leaseExtensionSeconds };
            Assert.Equal(expectedCommandReplaceOperations, CheckpointActionResult.ConvertToCommandReplaceOperations(checkpointRequest));
        }

        [Fact]
        public async Task AgentNotAuthorized()
        {
            AgentId agentId = this.AnAgentId();
            LeaseReceipt leaseReceipt = this.ALeaseReceipt(agentId);

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();

            var checkpointRequest = CreateCheckpointRequest(leaseReceipt);

            authorizer.Setup(m => m.CheckAuthorizedAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<AgentId>()))
                         .Throws(new AuthNException(AuthNErrorCode.AuthenticationFailed, "Some authN problem"));

            CheckpointActionResult checkpointAction = new CheckpointActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                null,                   // null publisher ensures not used.
                null,                   // null deferral publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                this.mockCommandHistory.Object,
                this.mockApiTrafficHandler.Object);

            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task MalformedRequest()
        {
            AgentId agentId = this.AnAgentId();

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var publisher = new Mock<ICommandLifecycleEventPublisher>();

            var checkpointRequest = new HttpRequestMessage(HttpMethod.Post, "/checkpoint")
            {
                Content = new StringContent("bad request")
            };

            CheckpointActionResult checkpointAction = new CheckpointActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                publisher.Object,
                null, // null deferral publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                this.mockCommandHistory.Object,
                this.mockApiTrafficHandler.Object);

            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains(nameof(JsonReaderException), content);
        }

        [Fact]
        public async Task AgentStateTooLarge()
        {
            AgentId agentId = this.AnAgentId();
            LeaseReceipt leaseReceipt = this.ALeaseReceipt();

            var authenticator = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var publisher = new Mock<ICommandLifecycleEventPublisher>();

            var stateBuilder = new StringBuilder();
            for (int i = 0; i < 1025; i++)
            {
                stateBuilder.Append('a');
            }

            var checkpointRequest = CreateCheckpointRequest(leaseReceipt, r => r.AgentState = stateBuilder.ToString());

            CheckpointActionResult checkpointAction = new CheckpointActionResult(
                agentId,
                authenticator.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                publisher.Object,
                null, // null deferral publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                this.mockCommandHistory.Object,
                this.mockApiTrafficHandler.Object);


            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);
            await AssertCheckpointErrorAsync(response, HttpStatusCode.BadRequest, CheckpointActionResult.CheckpointErrorCode.AgentStateExceedsMaxSizeAllowed);
        }

        [Fact]
        public async Task LeaseReceiptAgentIdMismatch()
        {
            AgentId agentId = this.AnAgentId();
            LeaseReceipt leaseReceipt = this.ALeaseReceipt();

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var publisher = new Mock<ICommandLifecycleEventPublisher>();

            var checkpointRequest = CreateCheckpointRequest(leaseReceipt);

            CheckpointActionResult checkpointAction = new CheckpointActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                publisher.Object,
                null, // null deferral publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                this.mockCommandHistory.Object,
                this.mockApiTrafficHandler.Object);


            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);
            await AssertCheckpointErrorAsync(response, HttpStatusCode.BadRequest, CheckpointActionResult.CheckpointErrorCode.LeaseReceiptAgentIdMismatch);
        }

        [Fact]
        public async Task NegativeLeaseExtension()
        {
            AgentId agentId = this.AnAgentId();
            LeaseReceipt leaseReceipt = this.ALeaseReceipt(agentId);

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var publisher = new Mock<ICommandLifecycleEventPublisher>();

            var checkpointRequest = CreateCheckpointRequest(leaseReceipt, r => r.LeaseExtensionSeconds = -1);

            CheckpointActionResult checkpointAction = new CheckpointActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                publisher.Object,
                null,                   // null deferral publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                this.mockCommandHistory.Object,
                this.mockApiTrafficHandler.Object);


            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);
            await AssertCheckpointErrorAsync(response, HttpStatusCode.BadRequest, CheckpointActionResult.CheckpointErrorCode.InvalidLeaseExtension);
        }

        [Fact]
        public async Task LeaseExtensionForAgeoutOverLimit()
        {
            AgentId agentId = this.AnAgentId();
            LeaseReceipt leaseReceipt = this.ALeaseReceipt(agentId, commandType: PrivacyCommandType.AgeOut);

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var publisher = new Mock<ICommandLifecycleEventPublisher>();

            var checkpointRequest = CreateCheckpointRequest(leaseReceipt, r => r.LeaseExtensionSeconds = (int)(TimeSpan.FromDays(10).TotalSeconds));

            CheckpointActionResult checkpointAction = new CheckpointActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                publisher.Object,
                null,                   // null deferral publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                this.mockCommandHistory.Object,
                this.mockApiTrafficHandler.Object);


            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);
            await AssertCheckpointErrorAsync(response, HttpStatusCode.BadRequest, CheckpointActionResult.CheckpointErrorCode.InvalidLeaseExtension);
        }

        [Fact]
        public async Task LeaseReceiptCommandExpired()
        {
            AgentId agentId = this.AnAgentId();
            LeaseReceipt leaseReceipt = this.ALeaseReceipt(agentId);

            leaseReceipt.CommandCreatedTime = DateTimeOffset.UtcNow.AddDays(-60);

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var publisher = new Mock<ICommandLifecycleEventPublisher>();

            commandQueue.Setup(m => m.SupportsLeaseReceipt(It.IsAny<LeaseReceipt>())).Returns(true);

            var checkpointRequest = CreateCheckpointRequest(leaseReceipt);

            CheckpointActionResult checkpointAction = new CheckpointActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                publisher.Object,
                null,                   // null deferral publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                this.mockCommandHistory.Object,
                this.mockApiTrafficHandler.Object);


            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);
            await AssertCheckpointErrorAsync(response, HttpStatusCode.BadRequest, CheckpointActionResult.CheckpointErrorCode.CommandAlreadyExpired);
        }

        [Fact]
        public async Task ExportCommandIsComplete()
        {
            AgentId agentId = this.AnAgentId();
            AssetGroupId assetGroupId = this.AnAssetGroupId();
            CommandId commandId = this.ACommandId();
            this.mockCommandHistory.Setup(c => c.QueryIsCompleteByAgentAsync(It.IsAny<LeaseReceipt>())).ReturnsAsync(true);

            Common.ExportCommand exportCommand = this.AnExportCommand(agentId, assetGroupId, commandId);
            LeaseReceipt leaseReceipt = new LeaseReceipt(
                "moniker",
                commandId,
                "etag",
                assetGroupId,
                agentId,
                Common.SubjectType.Msa,
                DateTimeOffset.UtcNow.AddMinutes(1),
                "fakequalifier",
                PrivacyCommandType.Export,
                string.Empty,
                DateTimeOffset.UtcNow.AddMinutes(-1),
                QueueStorageType.AzureCosmosDb);

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var publisher = new Mock<ICommandLifecycleEventPublisher>();
            var logger = new Mock<ILogger>();

            commandQueue.Setup(m => m.DeleteAsync(It.IsAny<LeaseReceipt>()))
                .Throws(new CommandFeedException("command not found") { ErrorCode = CommandFeedInternalErrorCode.NotFound });

            commandQueue.Setup(m => m.SupportsLeaseReceipt(It.IsAny<LeaseReceipt>())).Returns(true);

            ConfigureDataAgentMap(dataAgentMap, leaseReceipt);
            var checkpointRequest = CreateCheckpointRequestWithExportFileSizeDetails(leaseReceipt, r => r.Status = PrivacyCommandStatus.Complete.ToString());

            CheckpointActionResult checkpointAction = new CheckpointActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                publisher.Object,
                null,                   // null deferral publisher ensures not used.
                checkpointRequest,
                logger.Object,
                this.mockCommandHistory.Object,
                this.mockApiTrafficHandler.Object);

            var response = await checkpointAction.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            await AssertCheckpointErrorAsync(response, HttpStatusCode.BadRequest, CheckpointActionResult.CheckpointErrorCode.CommandAlreadyCompleted);
        }

        [Fact]
        public async Task CommandDoesNotExist()
        {
            AgentId agentId = this.AnAgentId();
            AssetGroupId assetGroupId = this.AnAssetGroupId();
            CommandId commandId = this.ACommandId();
            Guid disallowedVariantId = Guid.NewGuid();

            LeaseReceipt leaseReceipt = this.ALeaseReceipt(agentId, assetGroupId, commandId);

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var publisher = new Mock<ICommandLifecycleEventPublisher>();

            this.mockCommandHistory.Setup(c => c.QueryPrivacyCommandAsync(It.IsAny<LeaseReceipt>())).Returns(Task.FromResult<Common.PrivacyCommand>(null));

            commandQueue.Setup(m => m.SupportsLeaseReceipt(It.IsAny<LeaseReceipt>())).Returns(true);
            Mock<IAssetGroupInfo> assetGroup = ConfigureDataAgentMap(dataAgentMap, leaseReceipt);

            // Claiming a variant forces us to read the whole command.
            var checkpointRequest = CreateCheckpointRequest(leaseReceipt, r => r.Variants = new[] { disallowedVariantId.ToString() });

            CheckpointActionResult checkpointAction = new CheckpointActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                publisher.Object,
                null,                   // null deferral publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                this.mockCommandHistory.Object,
                this.mockApiTrafficHandler.Object);


            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);
            await AssertCheckpointErrorAsync(response, HttpStatusCode.InternalServerError, CheckpointActionResult.CheckpointErrorCode.CommandNotFound);
        }

        [Fact]
        public async Task InvalidVariants()
        {
            AgentId agentId = this.AnAgentId();
            AssetGroupId assetGroupId = this.AnAssetGroupId();
            CommandId commandId = this.ACommandId();
            Guid allowedVariantId = Guid.NewGuid();
            Guid disallowedVariantId = Guid.NewGuid();

            LeaseReceipt leaseReceipt = this.ALeaseReceipt(agentId, assetGroupId, commandId);
            Common.DeleteCommand deleteCommand = this.ADeleteCommand(agentId, assetGroupId);

            var authenticator = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var publisher = new Mock<ICommandLifecycleEventPublisher>();

            leaseReceipt.CommandId = deleteCommand.CommandId;
            var allowedVariantInfo = this.AnAssetGroupVariantInfoDocument().With(x => x.VariantId, new VariantId(allowedVariantId)).Build();

            this.mockCommandHistory.Setup(c => c.QueryPrivacyCommandAsync(It.IsAny<LeaseReceipt>())).ReturnsAsync(deleteCommand);

            commandQueue.Setup(m => m.SupportsLeaseReceipt(It.IsAny<LeaseReceipt>())).Returns(true);

            Mock<IAssetGroupInfo> assetGroup = ConfigureDataAgentMap(dataAgentMap, leaseReceipt);
            assetGroup.Setup(m => m.VariantInfosAppliedByAgents).Returns(new List<IAssetGroupVariantInfo> { new AssetGroupVariantInfo(allowedVariantInfo, false) });
            assetGroup.Setup(m => m.VariantInfosAppliedByPcf).Returns(new List<IAssetGroupVariantInfo>());

            var checkpointRequest = CreateCheckpointRequest(leaseReceipt, r => r.Variants = new[] { disallowedVariantId.ToString() });

            CheckpointActionResult checkpointAction = new CheckpointActionResult(
                agentId,
                authenticator.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                publisher.Object,
                null,                   // null deferral publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                this.mockCommandHistory.Object,
                this.mockApiTrafficHandler.Object);

            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);
            await AssertCheckpointErrorAsync(response, HttpStatusCode.BadRequest, CheckpointActionResult.CheckpointErrorCode.InvalidVariantsSpecified);
        }

        [Fact]
        public async Task ValidVariantsReturnsOkStatus()
        {
            AgentId agentId = this.AnAgentId();
            AssetGroupId assetGroupId = this.AnAssetGroupId();
            CommandId commandId = this.ACommandId();
            Guid allowedVariantId = Guid.NewGuid();
            Guid disallowedVariantId = Guid.NewGuid();

            LeaseReceipt leaseReceipt = this.ALeaseReceipt(agentId, assetGroupId, commandId);
            Common.DeleteCommand deleteCommand = this.ADeleteCommand(agentId, assetGroupId);

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var publisher = new Mock<ICommandLifecycleEventPublisher>();

            bool callbackInvoked = false;
            bool publishCalled = false;

            leaseReceipt.CommandId = deleteCommand.CommandId;
            var allowedVariantInfo = this.AnAssetGroupVariantInfoDocument().With(x => x.VariantId, new VariantId(allowedVariantId)).With(x => x.DataTypes, new string[0]).Build();

            this.mockCommandHistory.Setup(c => c.QueryPrivacyCommandAsync(It.IsAny<LeaseReceipt>())).ReturnsAsync(deleteCommand);

            commandQueue.Setup(m => m.SupportsLeaseReceipt(It.IsAny<LeaseReceipt>())).Returns(true);

            commandQueue.Setup(m => m.DeleteAsync(It.IsAny<LeaseReceipt>()))
                .Callback<LeaseReceipt>(lr =>
                {
                    // Assert that publish was invoked before invoking replace in docdb.
                    // This is to avoid an error where the command is completed in docdb, but
                    // the publish step fails.
                    Assert.True(publishCalled);
                    callbackInvoked = true;
                })
                .Returns(Task.FromResult(leaseReceipt));

            Mock<IAssetGroupInfo> assetGroup = ConfigureDataAgentMap(dataAgentMap, leaseReceipt);
            assetGroup.Setup(m => m.VariantInfosAppliedByAgents).Returns(new List<IAssetGroupVariantInfo> { new AssetGroupVariantInfo(allowedVariantInfo, false) });
            assetGroup.Setup(m => m.VariantInfosAppliedByPcf).Returns(new List<IAssetGroupVariantInfo>());

            var checkpointRequest = CreateCheckpointRequest(leaseReceipt, r =>
            {
                r.Status = PrivacyCommandStatus.Complete.ToString();
                r.Variants = new[] { allowedVariantId.ToString() };
            });


            publisher.Setup(m => m.PublishCommandCompletedAsync(
                    It.IsAny<AgentId>(),
                    It.IsAny<AssetGroupId>(),
                    It.IsAny<string>(),
                    It.IsAny<CommandId>(),
                    It.IsAny<PrivacyCommandType>(),
                    It.IsAny<DateTimeOffset?>(),
                    It.IsAny<string[]>(),
                    It.IsAny<bool>(),
                    It.IsAny<int>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<ForceCompleteReasonCode?>()))
                .Callback<AgentId, AssetGroupId, string, CommandId, PrivacyCommandType, DateTimeOffset?, string[], bool, int, bool, string, bool, ForceCompleteReasonCode?>(
                    (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, completedByPcf, arg13) =>
                    {
                        Assert.False(completedByPcf);
                        publishCalled = true;
                    })
                .Returns(Task.FromResult(true));

            CheckpointActionResult checkpointAction = new CheckpointActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                publisher.Object,
                null,                   // null deferral publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                this.mockCommandHistory.Object,
                this.mockApiTrafficHandler.Object);

            HttpResponseMessage response = await checkpointAction.ExecuteAsync(CancellationToken.None);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(callbackInvoked);
        }

        [Fact]
        public async Task InvalidCommandStatus()
        {
            AgentId agentId = this.AnAgentId();
            AssetGroupId assetGroupId = this.AnAssetGroupId();
            CommandId commandId = this.ACommandId();

            LeaseReceipt leaseReceipt = this.ALeaseReceipt(agentId, assetGroupId, commandId);
            Common.DeleteCommand deleteCommand = this.ADeleteCommand(agentId, assetGroupId);

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var publisher = new Mock<ICommandLifecycleEventPublisher>();

            commandQueue.Setup(m => m.QueryCommandAsync(It.IsAny<LeaseReceipt>()))
                        .ReturnsAsync(deleteCommand);

            commandQueue.Setup(m => m.SupportsLeaseReceipt(It.IsAny<LeaseReceipt>())).Returns(true);

            ConfigureDataAgentMap(dataAgentMap, leaseReceipt);

            var checkpointRequest = CreateCheckpointRequest(leaseReceipt, r => r.Status = "asdfasdfasdf");

            CheckpointActionResult checkpointAction = new CheckpointActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                publisher.Object,
                null,                   // null deferral publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                this.mockCommandHistory.Object,
                this.mockApiTrafficHandler.Object);


            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);
            await AssertCheckpointErrorAsync(response, HttpStatusCode.BadRequest, CheckpointActionResult.CheckpointErrorCode.InvalidCommandStatus);
        }

        [Fact]
        public async Task CommandDeidentify()
        {
            AgentId agentId = this.AnAgentId();
            AssetGroupId assetGroupId = this.AnAssetGroupId();
            CommandId commandId = this.ACommandId();

            LeaseReceipt leaseReceipt = this.ALeaseReceipt(agentId, assetGroupId, commandId);
            Common.DeleteCommand deleteCommand = this.ADeleteCommand(agentId, assetGroupId);

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var publisher = new Mock<ICommandLifecycleEventPublisher>();

            bool callbackInvoked = false;
            bool publishCalled = false;

            leaseReceipt.CommandId = deleteCommand.CommandId;

            commandQueue.Setup(m => m.QueryCommandAsync(It.IsAny<LeaseReceipt>()))
                        .ReturnsAsync(deleteCommand);

            commandQueue.Setup(m => m.SupportsLeaseReceipt(It.IsAny<LeaseReceipt>())).Returns(true);

            commandQueue.Setup(m => m.DeleteAsync(It.IsAny<LeaseReceipt>()))
                        .Callback<LeaseReceipt>(lr =>
                        {
                            // Assert that publish was invoked before invoking replace in docdb.
                            // This is to avoid an error where the command is completed in docdb, but
                            // the publish step fails.
                            Assert.True(publishCalled);
                            callbackInvoked = true;
                        })
                        .Returns(Task.FromResult(leaseReceipt));

            publisher.Setup(m => m.PublishCommandCompletedAsync(
                    It.IsAny<AgentId>(),
                    It.IsAny<AssetGroupId>(),
                    It.IsAny<string>(),
                    It.IsAny<CommandId>(),
                    It.IsAny<PrivacyCommandType>(),
                    It.IsAny<DateTimeOffset?>(),
                    It.IsAny<string[]>(),
                    It.IsAny<bool>(),
                    It.IsAny<int>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<ForceCompleteReasonCode?>()))
                .Callback<AgentId, AssetGroupId, string, CommandId, PrivacyCommandType, DateTimeOffset?, string[], bool, int, bool, string, bool, ForceCompleteReasonCode?>(
                (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, completedByPcf, arg13) =>
                {
                    Assert.False(completedByPcf);
                    publishCalled = true;
                })
                    .Returns(Task.FromResult(true));

            Mock<IAssetGroupInfo> assetGroup = ConfigureDataAgentMap(dataAgentMap, leaseReceipt);

            var checkpointRequest = CreateCheckpointRequest(leaseReceipt, r => r.Status = PrivacyCommandStatus.Deidentify.ToString());

            CheckpointActionResult checkpointAction = new CheckpointActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                publisher.Object,
                null,                   // null deferral publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                this.mockCommandHistory.Object,
                this.mockApiTrafficHandler.Object);


            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(callbackInvoked);
        }

        [Fact]
        public async Task CommandCompletedWithShortLease()
        {
            AgentId agentId = this.AnAgentId();
            AssetGroupId assetGroupId = this.AnAssetGroupId();
            CommandId commandId = this.ACommandId();

            LeaseReceipt leaseReceipt = this.ALeaseReceipt(agentId, assetGroupId, commandId);
            Common.DeleteCommand deleteCommand = this.ADeleteCommand(agentId, assetGroupId);

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var publisher = new Mock<ICommandLifecycleEventPublisher>();

            bool callbackInvoked = false;
            bool publishCalled = false;

            leaseReceipt.CommandId = deleteCommand.CommandId;

            // We should not query for this execution path.
            commandQueue.Setup(m => m.QueryCommandAsync(It.IsAny<LeaseReceipt>())).Callback(() => Assert.True(false)).Returns(Task.FromResult<Common.PrivacyCommand>(null));

            commandQueue.Setup(m => m.SupportsLeaseReceipt(It.IsAny<LeaseReceipt>())).Returns(true);

            commandQueue.Setup(m => m.DeleteAsync(It.IsAny<LeaseReceipt>()))
                        .Callback<LeaseReceipt>(lr =>
                        {
                            // Assert that publish was invoked before invoking replace in docdb.
                            // This is to avoid an error where the command is completed in docdb, but
                            // the publish step fails.
                            Assert.True(publishCalled);
                            callbackInvoked = true;
                        })
                        .Returns(Task.FromResult(leaseReceipt));

            publisher.Setup(m => m.PublishCommandCompletedAsync(
                    It.IsAny<AgentId>(),
                    It.IsAny<AssetGroupId>(),
                    It.IsAny<string>(),
                    It.IsAny<CommandId>(),
                    It.IsAny<PrivacyCommandType>(),
                    It.IsAny<DateTimeOffset?>(),
                    It.IsAny<string[]>(),
                    It.IsAny<bool>(),
                    It.IsAny<int>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<ForceCompleteReasonCode?>()))
                .Callback<AgentId, AssetGroupId, string, CommandId, PrivacyCommandType, DateTimeOffset?, string[], bool, int, bool, string, bool, ForceCompleteReasonCode?>(
                    (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, completedByPcf, arg13) =>
                    {
                        Assert.False(completedByPcf);
                        publishCalled = true;
                    })
                .Returns(Task.FromResult(true));

            Mock<IAssetGroupInfo> assetGroup = ConfigureDataAgentMap(dataAgentMap, leaseReceipt);

            var checkpointRequest = CreateCheckpointRequest(leaseReceipt, r => r.Status = PrivacyCommandStatus.Complete.ToString());

            CheckpointActionResult checkpointAction = new CheckpointActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                publisher.Object,
                null,                   // null deferral publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                this.mockCommandHistory.Object,
                this.mockApiTrafficHandler.Object);


            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(callbackInvoked);
        }

        [Fact]
        public async Task ShouldLogFileSizesForExportCommand()
        {
            AgentId agentId = this.AnAgentId();
            AssetGroupId assetGroupId = this.AnAssetGroupId();
            CommandId commandId = this.ACommandId();
            this.mockCommandHistory.Setup(c => c.QueryIsCompleteByAgentAsync(It.IsAny<LeaseReceipt>())).ReturnsAsync(false);

            Common.ExportCommand exportCommand = this.AnExportCommand(agentId, assetGroupId, commandId);
            LeaseReceipt leaseReceipt = new LeaseReceipt(
                "moniker",
                commandId,
                "etag",
                assetGroupId,
                agentId,
                Common.SubjectType.Msa,
                DateTimeOffset.UtcNow.AddMinutes(1),
                "fakequalifier",
                PrivacyCommandType.Export,
                string.Empty,
                DateTimeOffset.UtcNow.AddMinutes(-1),
                QueueStorageType.AzureCosmosDb);

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var publisher = new Mock<ICommandLifecycleEventPublisher>();
            var logger = new Mock<ILogger>();

            commandQueue.Setup(m => m.QueryCommandAsync(It.IsAny<LeaseReceipt>()))
                .ReturnsAsync(exportCommand);

            commandQueue.Setup(m => m.SupportsLeaseReceipt(It.IsAny<LeaseReceipt>())).Returns(true);

            ConfigureDataAgentMap(dataAgentMap, leaseReceipt);
            var checkpointRequest = CreateCheckpointRequestWithExportFileSizeDetails(leaseReceipt, r => r.Status = PrivacyCommandStatus.Complete.ToString());

            CheckpointActionResult checkpointAction = new CheckpointActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                publisher.Object,
                null,                   // null deferral publisher ensures not used.
                checkpointRequest,
                logger.Object,
                this.mockCommandHistory.Object,
                this.mockApiTrafficHandler.Object);


            var response = await checkpointAction.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            // Give time for fire and forget logging tasks to complete
            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);

            logger.Verify(s => s.LogExportFileSizeEvent(agentId, assetGroupId, commandId, "fileName1", 2345, 1234, true, SubjectType.Msa, AgentType.NonCosmos, string.Empty), Times.Once);
            logger.Verify(s => s.LogExportFileSizeEvent(agentId, assetGroupId, commandId, "fileName2", 2345, 2345, false, SubjectType.Msa, AgentType.NonCosmos, string.Empty), Times.Once);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ShouldLogFileSizesForExportCommandAadSubject()
        {
            AgentId agentId = this.AnAgentId();
            AssetGroupId assetGroupId = this.AnAssetGroupId();
            CommandId commandId = this.ACommandId();
            this.mockCommandHistory.Setup(c => c.QueryIsCompleteByAgentAsync(It.IsAny<LeaseReceipt>())).ReturnsAsync(false);

            Common.ExportCommand exportCommand = this.AnExportCommand(agentId, assetGroupId, commandId);
            LeaseReceipt leaseReceipt = new LeaseReceipt(
                "moniker",
                commandId,
                "etag",
                assetGroupId,
                agentId,
                Common.SubjectType.Aad,
                DateTimeOffset.UtcNow.AddMinutes(1),
                "fakequalifier",
                PrivacyCommandType.Export,
                "Mooncake",
                DateTimeOffset.UtcNow.AddMinutes(-1),
                QueueStorageType.AzureCosmosDb);

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var publisher = new Mock<ICommandLifecycleEventPublisher>();
            var logger = new Mock<ILogger>();

            commandQueue.Setup(m => m.QueryCommandAsync(It.IsAny<LeaseReceipt>()))
                .ReturnsAsync(exportCommand);

            commandQueue.Setup(m => m.SupportsLeaseReceipt(It.IsAny<LeaseReceipt>())).Returns(true);

            ConfigureDataAgentMap(dataAgentMap, leaseReceipt);
            var checkpointRequest = CreateCheckpointRequestWithExportFileSizeDetails(leaseReceipt, r => r.Status = PrivacyCommandStatus.Complete.ToString());

            CheckpointActionResult checkpointAction = new CheckpointActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                publisher.Object,
                null,                   // null deferral publisher ensures not used.
                checkpointRequest,
                logger.Object,
                this.mockCommandHistory.Object,
                this.mockApiTrafficHandler.Object);


            var response = await checkpointAction.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            // Give time for fire and forget logging tasks to complete
            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);

            logger.Verify(s => s.LogExportFileSizeEvent(agentId, assetGroupId, commandId, "fileName1", 2345, 1234, true, SubjectType.Aad, AgentType.NonCosmos, "Mooncake"), Times.Once);
            logger.Verify(s => s.LogExportFileSizeEvent(agentId, assetGroupId, commandId, "fileName2", 2345, 2345, false, SubjectType.Aad, AgentType.NonCosmos, "Mooncake"), Times.Once);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ShouldNotLogFileSizesForNonExportCommands()
        {
            AgentId agentId = this.AnAgentId();
            AssetGroupId assetGroupId = this.AnAssetGroupId();
            CommandId commandId = this.ACommandId();

            Common.DeleteCommand deleteCommand = this.ADeleteCommand(agentId, assetGroupId, commandId);
            LeaseReceipt leaseReceipt = new LeaseReceipt(
                "moniker",
                commandId,
                "etag",
                assetGroupId,
                agentId,
                Common.SubjectType.Msa,
                DateTimeOffset.UtcNow.AddMinutes(1),
                "fakequalifier",
                PrivacyCommandType.Delete,
                string.Empty,
                DateTimeOffset.UtcNow.AddMinutes(-1),
                QueueStorageType.AzureCosmosDb);

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var publisher = new Mock<ICommandLifecycleEventPublisher>();
            var logger = new Mock<ILogger>();

            commandQueue.Setup(m => m.QueryCommandAsync(It.IsAny<LeaseReceipt>()))
                .ReturnsAsync(deleteCommand);

            commandQueue.Setup(m => m.SupportsLeaseReceipt(It.IsAny<LeaseReceipt>())).Returns(true);

            ConfigureDataAgentMap(dataAgentMap, leaseReceipt);
            var checkpointRequest = CreateCheckpointRequestWithExportFileSizeDetails(leaseReceipt, r => r.Status = PrivacyCommandStatus.Complete.ToString());

            CheckpointActionResult checkpointAction = new CheckpointActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                publisher.Object,
                null,                   // null deferral publisher ensures not used.
                checkpointRequest,
                logger.Object,
                this.mockCommandHistory.Object,
                this.mockApiTrafficHandler.Object);


            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);

            logger.Verify(s => s.LogExportFileSizeEvent(agentId, assetGroupId, commandId, "fileName1", 2345, 1234, true, SubjectType.Msa, AgentType.NonCosmos, string.Empty), Times.Never);
            logger.Verify(s => s.LogExportFileSizeEvent(agentId, assetGroupId, commandId, "fileName2", 2345, 2345, false, SubjectType.Msa, AgentType.NonCosmos, string.Empty), Times.Never);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CommandCompletedWithLongLease()
        {
            AgentId agentId = this.AnAgentId();
            AssetGroupId assetGroupId = this.AnAssetGroupId();
            CommandId commandId = this.ACommandId();

            LeaseReceipt leaseReceipt = this.ALeaseReceipt(agentId, assetGroupId, commandId).WithValue(x => x.ApproximateExpirationTime, DateTimeOffset.UtcNow.AddYears(1));

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var publisher = new Mock<ICommandLifecycleEventPublisher>();
            var deferralPublisher = this.AMockOf<IAzureWorkItemQueuePublisher<DeleteFromQueueWorkItem>>();

            bool callbackInvoked = false;
            bool publishCalled = false;

            commandQueue.Setup(m => m.SupportsLeaseReceipt(It.IsAny<LeaseReceipt>())).Returns(true);

            // We should not query for this execution path.
            commandQueue.Setup(m => m.QueryCommandAsync(It.IsAny<LeaseReceipt>())).Callback(() => Assert.True(false)).Returns(Task.FromResult<Common.PrivacyCommand>(null));
            deferralPublisher.Setup(m => m.PublishAsync(It.IsAny<DeleteFromQueueWorkItem>(), It.IsAny<TimeSpan?>()))
                        .Callback<DeleteFromQueueWorkItem, TimeSpan?>((wi, ts) =>
                        {
                            // Assert that publish was invoked before invoking replace in docdb.
                            // This is to avoid an error where the command is completed in docdb, but
                            // the publish step fails.
                            Assert.True(publishCalled);

                            Assert.NotNull(ts);
                            Assert.True(ts > TimeSpan.Zero);
                            Assert.Equal(agentId, wi.AgentId);
                            Assert.Single(wi.LeaseReceipts);

                            callbackInvoked = true;
                        })
                        .Returns(Task.FromResult(leaseReceipt));

            publisher.Setup(m => m.PublishCommandCompletedAsync(
                    It.IsAny<AgentId>(),
                    It.IsAny<AssetGroupId>(),
                    It.IsAny<string>(),
                    It.IsAny<CommandId>(),
                    It.IsAny<PrivacyCommandType>(),
                    It.IsAny<DateTimeOffset?>(),
                    It.IsAny<string[]>(),
                    It.IsAny<bool>(),
                    It.IsAny<int>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<ForceCompleteReasonCode?>()))
                .Callback<AgentId, AssetGroupId, string, CommandId, PrivacyCommandType, DateTimeOffset?, string[], bool, int, bool, string, bool, ForceCompleteReasonCode?>(
                    (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, completedByPcf, arg13) =>
                    {
                        Assert.False(completedByPcf);
                        publishCalled = true;
                    })
                .Returns(Task.FromResult(true));

            Mock<IAssetGroupInfo> assetGroup = ConfigureDataAgentMap(dataAgentMap, leaseReceipt);

            var checkpointRequest = CreateCheckpointRequest(leaseReceipt, r => r.Status = PrivacyCommandStatus.Complete.ToString());

            CheckpointActionResult checkpointAction = new CheckpointActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                publisher.Object,
                deferralPublisher.Object,
                checkpointRequest,
                Logger.Instance,
                this.mockCommandHistory.Object,
                this.mockApiTrafficHandler.Object);


            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(callbackInvoked);
        }

        [Fact]
        public async Task CommandFailed()
        {
            AgentId agentId = this.AnAgentId();
            AssetGroupId assetGroupId = this.AnAssetGroupId();
            CommandId commandId = this.ACommandId();

            LeaseReceipt leaseReceipt = this.ALeaseReceipt(agentId, assetGroupId, commandId);
            Common.DeleteCommand deleteCommand = this.ADeleteCommand(agentId, assetGroupId);

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var publisher = new Mock<ICommandLifecycleEventPublisher>();

            bool callbackInvoked = false;

            leaseReceipt.CommandId = deleteCommand.CommandId;
            this.mockCommandHistory.Setup(c => c.QueryPrivacyCommandAsync(It.IsAny<LeaseReceipt>())).ReturnsAsync(deleteCommand);

            commandQueue.Setup(m => m.SupportsLeaseReceipt(It.IsAny<LeaseReceipt>())).Returns(true);

            commandQueue.Setup(m => m.ReplaceAsync(It.IsAny<LeaseReceipt>(), It.IsAny<Common.PrivacyCommand>(), It.IsAny<CommandReplaceOperations>()))
                        .Callback<LeaseReceipt, Common.PrivacyCommand, CommandReplaceOperations>((lr, pc, cro) =>
                        {
                            Assert.True(!string.IsNullOrEmpty(pc.AgentState));
                            Assert.True(pc.NextVisibleTime <= DateTimeOffset.UtcNow.AddMinutes(5));
                            callbackInvoked = true;
                        })
                        .Returns(Task.FromResult(leaseReceipt));

            Mock<IAssetGroupInfo> assetGroup = ConfigureDataAgentMap(dataAgentMap, leaseReceipt);

            var checkpointRequest = CreateCheckpointRequest(leaseReceipt, r => r.Status = PrivacyCommandStatus.Failed.ToString());

            CheckpointActionResult checkpointAction = new CheckpointActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                publisher.Object,
                null,                   // null deferral publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                this.mockCommandHistory.Object,
                this.mockApiTrafficHandler.Object);

            HttpResponseMessage response = await checkpointAction.ExecuteAsync(CancellationToken.None);

            publisher.Verify(
                eventPublisher => eventPublisher.PublishCommandFailedAsync(
                    It.IsAny<AgentId>(),
                    It.IsAny<AssetGroupId>(),
                    It.IsAny<string>(),
                    It.IsAny<CommandId>(),
                    It.IsAny<PrivacyCommandType>()),
                Times.Once);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(callbackInvoked);
        }

        [Fact]
        public async Task CommandSoftDelete()
        {
            AgentId agentId = this.AnAgentId();
            AssetGroupId assetGroupId = this.AnAssetGroupId();
            CommandId commandId = this.ACommandId();

            LeaseReceipt leaseReceipt = this.ALeaseReceipt(agentId, assetGroupId, commandId);
            Common.DeleteCommand deleteCommand = this.ADeleteCommand(agentId, assetGroupId);

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var publisher = new Mock<ICommandLifecycleEventPublisher>();

            bool callbackInvoked = false;
            bool publishCalled = false;

            deleteCommand.NextVisibleTime = DateTime.UtcNow;

            leaseReceipt.CommandId = deleteCommand.CommandId;

            this.mockCommandHistory.Setup(c => c.QueryPrivacyCommandAsync(It.IsAny<LeaseReceipt>())).ReturnsAsync(deleteCommand);

            commandQueue.Setup(m => m.SupportsLeaseReceipt(It.IsAny<LeaseReceipt>())).Returns(true);

            commandQueue.Setup(m => m.ReplaceAsync(It.IsAny<LeaseReceipt>(), It.IsAny<Common.PrivacyCommand>(), It.IsAny<CommandReplaceOperations>()))
                        .Callback<LeaseReceipt, Common.PrivacyCommand, CommandReplaceOperations>((lr, pc, cro) =>
                        {
                            // Enforce that publish is called prior to updating the command. This prevents cases
                            // where we update docdb first but the publish step later fails.
                            Assert.True(publishCalled);

                            Assert.True(!string.IsNullOrEmpty(pc.AgentState));
                            Assert.True(pc.NextVisibleTime >= DateTimeOffset.UtcNow.AddSeconds(100));

                            callbackInvoked = true;
                        })
                        .Returns(Task.FromResult(leaseReceipt));

            publisher.Setup(m => m.PublishCommandSoftDeletedAsync(
                It.IsAny<AgentId>(),
                It.IsAny<AssetGroupId>(),
                It.IsAny<string>(),
                It.IsAny<CommandId>(),
                It.IsAny<PrivacyCommandType>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<string>()))
                     .Callback(() => publishCalled = true)
                     .Returns(Task.FromResult(true));

            Mock<IAssetGroupInfo> assetGroup = ConfigureDataAgentMap(dataAgentMap, leaseReceipt);

            var checkpointRequest = CreateCheckpointRequest(leaseReceipt, r =>
            {
                r.Status = PrivacyCommandStatus.SoftDelete.ToString();
                r.LeaseExtensionSeconds = 86400;
            });

            CheckpointActionResult checkpointAction = new CheckpointActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                publisher.Object,
                null,                   // null deferral publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                this.mockCommandHistory.Object,
                this.mockApiTrafficHandler.Object);

            HttpResponseMessage response = await checkpointAction.ExecuteAsync(CancellationToken.None);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(callbackInvoked);
        }

        [Fact]
        public async Task CommandUnexpectedTypePending()
        {
            AgentId agentId = this.AnAgentId();
            AssetGroupId assetGroupId = this.AnAssetGroupId();
            CommandId commandId = this.ACommandId();

            LeaseReceipt leaseReceipt = this.ALeaseReceipt(agentId, assetGroupId, commandId);
            Common.DeleteCommand deleteCommand = this.ADeleteCommand(agentId, assetGroupId);

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var publisher = new Mock<ICommandLifecycleEventPublisher>();

            bool callbackInvoked = false;

            leaseReceipt.CommandId = deleteCommand.CommandId;
            this.mockCommandHistory.Setup(c => c.QueryPrivacyCommandAsync(It.IsAny<LeaseReceipt>())).ReturnsAsync(deleteCommand);

            commandQueue.Setup(m => m.SupportsLeaseReceipt(It.IsAny<LeaseReceipt>())).Returns(true);

            commandQueue.Setup(m => m.ReplaceAsync(It.IsAny<LeaseReceipt>(), It.IsAny<Common.PrivacyCommand>(), It.IsAny<CommandReplaceOperations>()))
                .Callback<LeaseReceipt, Common.PrivacyCommand, CommandReplaceOperations>((lr, pc, cro) =>
                {
                    Assert.True(string.IsNullOrEmpty(pc.AgentState));
                    Assert.True(pc.NextVisibleTime <= DateTimeOffset.UtcNow.AddHours(28));
                    Assert.True(pc.NextVisibleTime >= DateTimeOffset.UtcNow.AddHours(20));

                    callbackInvoked = true;
                })
                .Returns(Task.FromResult(leaseReceipt));

            Mock<IAssetGroupInfo> assetGroup = ConfigureDataAgentMap(dataAgentMap, leaseReceipt);

            // Command fits filtering
            ApplicabilityResult applicabilityResult = new ApplicabilityResult();
            assetGroup.Setup(m => m.IsCommandActionable(It.IsAny<Common.PrivacyCommand>(), out applicabilityResult)).Returns(true);

            var checkpointRequest = CreateCheckpointRequest(leaseReceipt, r => r.Status = PrivacyCommandStatus.UnexpectedCommand.ToString());

            CheckpointActionResult checkpointAction = new CheckpointActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                publisher.Object,
                null,                   // null deferral publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                this.mockCommandHistory.Object,
                this.mockApiTrafficHandler.Object);

            HttpResponseMessage response = await checkpointAction.ExecuteAsync(CancellationToken.None);

            publisher.Verify(
                eventPublisher => eventPublisher.PublishCommandUnexpectedAsync(
                    It.IsAny<AgentId>(),
                    It.IsAny<AssetGroupId>(),
                    It.IsAny<string>(),
                    It.IsAny<CommandId>(),
                    It.IsAny<PrivacyCommandType>()),
                Times.Once);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(callbackInvoked);
        }

        [Fact]
        public async Task UnexpectedCommandTestInProduction()
        {
            AgentId agentId = this.AnAgentId();
            AssetGroupId assetGroupId = this.AnAssetGroupId();
            CommandId commandId = this.ACommandId();

            LeaseReceipt leaseReceipt = this.ALeaseReceipt(agentId, assetGroupId, commandId);
            Common.DeleteCommand deleteCommand = this.ADeleteCommand(agentId, assetGroupId);

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var publisher = new Mock<ICommandLifecycleEventPublisher>();

            bool callbackInvoked = false;

            leaseReceipt.CommandId = deleteCommand.CommandId;

            this.mockCommandHistory.Setup(c => c.QueryPrivacyCommandAsync(It.IsAny<LeaseReceipt>())).ReturnsAsync(deleteCommand);

            commandQueue.Setup(m => m.SupportsLeaseReceipt(It.IsAny<LeaseReceipt>())).Returns(true);

            commandQueue.Setup(m => m.ReplaceAsync(It.IsAny<LeaseReceipt>(), It.IsAny<Common.PrivacyCommand>(), It.IsAny<CommandReplaceOperations>()))
                .Callback<LeaseReceipt, Common.PrivacyCommand, CommandReplaceOperations>((lr, pc, cro) =>
                {
                    Assert.True(string.IsNullOrEmpty(pc.AgentState));
                    Assert.True(pc.NextVisibleTime <= DateTimeOffset.UtcNow.AddDays(1));

                    callbackInvoked = true;
                })
                .Returns(Task.FromResult(leaseReceipt));

            Mock<IAssetGroupInfo> assetGroup = ConfigureDataAgentMap(dataAgentMap, leaseReceipt);

            // Command fits filtering
            ApplicabilityResult applicabilityResult = new ApplicabilityResult();
            assetGroup.Setup(m => m.IsCommandActionable(It.IsAny<Common.PrivacyCommand>(), out applicabilityResult)).Returns(true);
            assetGroup.SetupGet(info => info.AgentReadinessState).Returns(AgentReadinessState.TestInProd);

            var checkpointRequest = CreateCheckpointRequest(leaseReceipt, r => r.Status = PrivacyCommandStatus.UnexpectedCommand.ToString());

            CheckpointActionResult checkpointAction = new CheckpointActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                publisher.Object,
                null,                   // null deferral publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                this.mockCommandHistory.Object,
                this.mockApiTrafficHandler.Object);


            bool publishCalled = false;
            publisher.Setup(m => m.PublishCommandCompletedAsync(
                    It.IsAny<AgentId>(),
                    It.IsAny<AssetGroupId>(),
                    It.IsAny<string>(),
                    It.IsAny<CommandId>(),
                    It.IsAny<PrivacyCommandType>(),
                    It.IsAny<DateTimeOffset?>(),
                    It.IsAny<string[]>(),
                    It.IsAny<bool>(),
                    It.IsAny<int>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<ForceCompleteReasonCode?>()))
                .Callback<AgentId, AssetGroupId, string, CommandId, PrivacyCommandType, DateTimeOffset?, string[], bool, int, bool, string, bool, ForceCompleteReasonCode?>(
                    (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, completedByPcf, arg13) =>
                    {
                        Assert.True(completedByPcf);
                        Assert.False(publishCalled);
                        publishCalled = true;
                    })
                .Returns(Task.FromResult(true));

            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(callbackInvoked);
        }

        [Fact]
        public async Task VerificationFailCommandTestInProduction()
        {
            AgentId agentId = this.AnAgentId();
            AssetGroupId assetGroupId = this.AnAssetGroupId();
            CommandId commandId = this.ACommandId();

            LeaseReceipt leaseReceipt = this.ALeaseReceipt(agentId, assetGroupId, commandId);
            Common.DeleteCommand deleteCommand = this.ADeleteCommand(agentId, assetGroupId);

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var publisher = new Mock<ICommandLifecycleEventPublisher>();

            bool callbackInvoked = false;

            leaseReceipt.CommandId = deleteCommand.CommandId;

            commandQueue.Setup(m => m.QueryCommandAsync(It.IsAny<LeaseReceipt>()))
                .ReturnsAsync(deleteCommand);

            commandQueue.Setup(m => m.SupportsLeaseReceipt(It.IsAny<LeaseReceipt>())).Returns(true);

            commandQueue.Setup(m => m.ReplaceAsync(It.IsAny<LeaseReceipt>(), It.IsAny<Common.PrivacyCommand>(), It.IsAny<CommandReplaceOperations>()))
                .Callback<LeaseReceipt, Common.PrivacyCommand, CommandReplaceOperations>((lr, pc, cro) =>
                {
                    Assert.True(string.IsNullOrEmpty(pc.AgentState));
                    Assert.True(pc.NextVisibleTime <= DateTimeOffset.UtcNow.AddDays(1));

                    callbackInvoked = true;
                })
                .Returns(Task.FromResult(leaseReceipt));

            Mock<IAssetGroupInfo> assetGroup = ConfigureDataAgentMap(dataAgentMap, leaseReceipt);

            // Command fits filtering
            ApplicabilityResult applicabilityResult = new ApplicabilityResult();
            assetGroup.Setup(m => m.IsCommandActionable(It.IsAny<Common.PrivacyCommand>(), out applicabilityResult)).Returns(true);
            assetGroup.SetupGet(info => info.AgentReadinessState).Returns(AgentReadinessState.TestInProd);

            var checkpointRequest = CreateCheckpointRequest(leaseReceipt, r => r.Status = PrivacyCommandStatus.VerificationFailed.ToString());

            CheckpointActionResult checkpointAction = new CheckpointActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                publisher.Object,
                null,                   // null deferral publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                this.mockCommandHistory.Object,
                this.mockApiTrafficHandler.Object);


            bool publishCalled = false;
            publisher.Setup(m => m.PublishCommandCompletedAsync(
                    It.IsAny<AgentId>(),
                    It.IsAny<AssetGroupId>(),
                    It.IsAny<string>(),
                    It.IsAny<CommandId>(),
                    It.IsAny<PrivacyCommandType>(),
                    It.IsAny<DateTimeOffset?>(),
                    It.IsAny<string[]>(),
                    It.IsAny<bool>(),
                    It.IsAny<int>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<ForceCompleteReasonCode?>()))
                .Callback<AgentId, AssetGroupId, string, CommandId, PrivacyCommandType, DateTimeOffset?, string[], bool, int, bool, string, bool, ForceCompleteReasonCode?>(
                    (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, completedByPcf, arg13) =>
                    {
                        Assert.True(completedByPcf);
                        Assert.False(publishCalled);
                        publishCalled = true;
                    })
                .Returns(Task.FromResult(true));

            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(callbackInvoked);
        }

        [Fact]
        public async Task UnexpectedVerificationFailureTestInProduction()
        {
            AgentId agentId = this.AnAgentId();
            AssetGroupId assetGroupId = this.AnAssetGroupId();
            CommandId commandId = this.ACommandId();

            LeaseReceipt leaseReceipt = this.ALeaseReceipt(agentId, assetGroupId, commandId);
            Common.DeleteCommand deleteCommand = this.ADeleteCommand(agentId, assetGroupId);

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var publisher = new Mock<ICommandLifecycleEventPublisher>();

            bool callbackInvoked = false;

            leaseReceipt.CommandId = deleteCommand.CommandId;

            commandQueue.Setup(m => m.QueryCommandAsync(It.IsAny<LeaseReceipt>()))
                .ReturnsAsync(deleteCommand);

            commandQueue.Setup(m => m.SupportsLeaseReceipt(It.IsAny<LeaseReceipt>())).Returns(true);

            commandQueue.Setup(m => m.ReplaceAsync(It.IsAny<LeaseReceipt>(), It.IsAny<Common.PrivacyCommand>(), It.IsAny<CommandReplaceOperations>()))
                .Callback<LeaseReceipt, Common.PrivacyCommand, CommandReplaceOperations>((lr, pc, cro) =>
                {
                    Assert.True(string.IsNullOrEmpty(pc.AgentState));
                    Assert.True(pc.NextVisibleTime <= DateTimeOffset.UtcNow.AddDays(1));

                    callbackInvoked = true;
                })
                .Returns(Task.FromResult(leaseReceipt));

            Mock<IAssetGroupInfo> assetGroup = ConfigureDataAgentMap(dataAgentMap, leaseReceipt);

            // Command fits filtering
            ApplicabilityResult applicabilityResult = new ApplicabilityResult();
            assetGroup.Setup(m => m.IsCommandActionable(It.IsAny<Common.PrivacyCommand>(), out applicabilityResult)).Returns(true);
            assetGroup.SetupGet(info => info.AgentReadinessState).Returns(AgentReadinessState.TestInProd);

            var checkpointRequest = CreateCheckpointRequest(leaseReceipt, r => r.Status = PrivacyCommandStatus.UnexpectedVerificationFailure.ToString());

            CheckpointActionResult checkpointAction = new CheckpointActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                publisher.Object,
                null,                   // null deferral publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                this.mockCommandHistory.Object,
                this.mockApiTrafficHandler.Object);


            bool publishCalled = false;
            publisher.Setup(m => m.PublishCommandCompletedAsync(
                    It.IsAny<AgentId>(),
                    It.IsAny<AssetGroupId>(),
                    It.IsAny<string>(),
                    It.IsAny<CommandId>(),
                    It.IsAny<PrivacyCommandType>(),
                    It.IsAny<DateTimeOffset?>(),
                    It.IsAny<string[]>(),
                    It.IsAny<bool>(),
                    It.IsAny<int>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<ForceCompleteReasonCode?>()))
                .Callback<AgentId, AssetGroupId, string, CommandId, PrivacyCommandType, DateTimeOffset?, string[], bool, int, bool, string, bool, ForceCompleteReasonCode?>(
                    (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, completedByPcf, arg13) =>
                    {
                        Assert.True(completedByPcf);
                        Assert.False(publishCalled);
                        publishCalled = true;
                    })
                .Returns(Task.FromResult(true));

            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(callbackInvoked);
        }

        [Fact]
        public async Task FailedCommandTestInProduction()
        {
            AgentId agentId = this.AnAgentId();
            AssetGroupId assetGroupId = this.AnAssetGroupId();
            CommandId commandId = this.ACommandId();

            LeaseReceipt leaseReceipt = this.ALeaseReceipt(agentId, assetGroupId, commandId);
            Common.DeleteCommand deleteCommand = this.ADeleteCommand(agentId, assetGroupId);

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var publisher = new Mock<ICommandLifecycleEventPublisher>();

            bool callbackInvoked = false;

            leaseReceipt.CommandId = deleteCommand.CommandId;

            commandQueue.Setup(m => m.QueryCommandAsync(It.IsAny<LeaseReceipt>()))
                .ReturnsAsync(deleteCommand);

            commandQueue.Setup(m => m.SupportsLeaseReceipt(It.IsAny<LeaseReceipt>())).Returns(true);

            commandQueue.Setup(m => m.ReplaceAsync(It.IsAny<LeaseReceipt>(), It.IsAny<Common.PrivacyCommand>(), It.IsAny<CommandReplaceOperations>()))
                .Callback<LeaseReceipt, Common.PrivacyCommand, CommandReplaceOperations>((lr, pc, cro) =>
                {
                    Assert.True(string.IsNullOrEmpty(pc.AgentState));
                    Assert.True(pc.NextVisibleTime <= DateTimeOffset.UtcNow.AddDays(1));

                    callbackInvoked = true;
                })
                .Returns(Task.FromResult(leaseReceipt));

            Mock<IAssetGroupInfo> assetGroup = ConfigureDataAgentMap(dataAgentMap, leaseReceipt);

            // Command fits filtering
            ApplicabilityResult applicabilityResult = new ApplicabilityResult();
            assetGroup.Setup(m => m.IsCommandActionable(It.IsAny<Common.PrivacyCommand>(), out applicabilityResult)).Returns(true);
            assetGroup.SetupGet(info => info.AgentReadinessState).Returns(AgentReadinessState.TestInProd);

            var checkpointRequest = CreateCheckpointRequest(leaseReceipt, r => r.Status = PrivacyCommandStatus.Failed.ToString());

            CheckpointActionResult checkpointAction = new CheckpointActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                publisher.Object,
                null,                   // null deferral publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                this.mockCommandHistory.Object,
                this.mockApiTrafficHandler.Object);


            bool publishCalled = false;
            publisher.Setup(m => m.PublishCommandCompletedAsync(
                    It.IsAny<AgentId>(),
                    It.IsAny<AssetGroupId>(),
                    It.IsAny<string>(),
                    It.IsAny<CommandId>(),
                    It.IsAny<PrivacyCommandType>(),
                    It.IsAny<DateTimeOffset?>(),
                    It.IsAny<string[]>(),
                    It.IsAny<bool>(),
                    It.IsAny<int>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<ForceCompleteReasonCode?>()))
                .Callback<AgentId, AssetGroupId, string, CommandId, PrivacyCommandType, DateTimeOffset?, string[], bool, int, bool, string, bool, ForceCompleteReasonCode?>(
                    (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, completedByPcf, arg13) =>
                    {
                        Assert.True(completedByPcf);
                        Assert.False(publishCalled);
                        publishCalled = true;
                    })
                .Returns(Task.FromResult(true));

            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(callbackInvoked);
        }


        [Fact]
        public async Task CommandUnexpectedTypeComplete()
        {
            AgentId agentId = this.AnAgentId();
            AssetGroupId assetGroupId = this.AnAssetGroupId();
            CommandId commandId = this.ACommandId();

            LeaseReceipt leaseReceipt = this.ALeaseReceipt(agentId, assetGroupId, commandId);
            Common.DeleteCommand deleteCommand = this.ADeleteCommand(agentId, assetGroupId);

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var publisher = new Mock<ICommandLifecycleEventPublisher>();

            bool callbackInvoked = false;

            leaseReceipt.CommandId = deleteCommand.CommandId;
            this.mockCommandHistory.Setup(c => c.QueryPrivacyCommandAsync(It.IsAny<LeaseReceipt>())).ReturnsAsync(deleteCommand);

            commandQueue.Setup(m => m.SupportsLeaseReceipt(It.IsAny<LeaseReceipt>())).Returns(true);

            commandQueue.Setup(m => m.DeleteAsync(It.IsAny<LeaseReceipt>()))
                .Callback<LeaseReceipt>(lr =>
                {
                    callbackInvoked = true;
                })
                .Returns(Task.FromResult(leaseReceipt));

            Mock<IAssetGroupInfo> assetGroup = ConfigureDataAgentMap(dataAgentMap, leaseReceipt);

            // Command does not fit filtering
            ApplicabilityResult applicabilityResult = new ApplicabilityResult()
            {
                Status = ApplicabilityStatus.DoesNotApply,
                ReasonCode = ApplicabilityReasonCode.DoesNotMatchAssetGroupDataTypes
            };
            assetGroup.Setup(m => m.IsCommandActionable(It.IsAny<Common.PrivacyCommand>(), out applicabilityResult)).Returns(false);

            var checkpointRequest = CreateCheckpointRequest(leaseReceipt, r => r.Status = PrivacyCommandStatus.UnexpectedCommand.ToString());

            CheckpointActionResult checkpointAction = new CheckpointActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                publisher.Object,
                null,                   // null deferral publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                this.mockCommandHistory.Object,
                this.mockApiTrafficHandler.Object);

            HttpResponseMessage response = await checkpointAction.ExecuteAsync(CancellationToken.None);

            publisher.Verify(
                eventPublisher => eventPublisher.PublishCommandUnexpectedAsync(
                    It.IsAny<AgentId>(),
                    It.IsAny<AssetGroupId>(),
                    It.IsAny<string>(),
                    It.IsAny<CommandId>(),
                    It.IsAny<PrivacyCommandType>()),
                Times.Once);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(callbackInvoked);
        }

        [Fact]
        public async Task CommandUnexpectedTypeCompleteFakePPEAssetGroup()
        {
            AgentId agentId = this.AnAgentId();
            AssetGroupId assetGroupId = this.AnAssetGroupId();
            CommandId commandId = this.ACommandId();

            LeaseReceipt leaseReceipt = this.ALeaseReceipt(agentId, assetGroupId, commandId);
            Common.DeleteCommand deleteCommand = this.ADeleteCommand(agentId, assetGroupId);

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var publisher = new Mock<ICommandLifecycleEventPublisher>();

            bool callbackInvoked = false;

            leaseReceipt.CommandId = deleteCommand.CommandId;
            this.mockCommandHistory.Setup(c => c.QueryPrivacyCommandAsync(It.IsAny<LeaseReceipt>())).ReturnsAsync(deleteCommand);

            commandQueue.Setup(m => m.SupportsLeaseReceipt(It.IsAny<LeaseReceipt>())).Returns(true);

            commandQueue.Setup(m => m.DeleteAsync(It.IsAny<LeaseReceipt>()))
                .Callback<LeaseReceipt>(lr =>
                {
                    callbackInvoked = true;
                })
                .Returns(Task.FromResult(true));

            Mock<IAssetGroupInfo> assetGroup = ConfigureDataAgentMap(dataAgentMap, leaseReceipt);
            assetGroup.SetupGet(m => m.IsFakePreProdAssetGroup).Returns(true);

            // Command does not fit filtering
            ApplicabilityResult applicabilityResult = new ApplicabilityResult()
            {
                Status = ApplicabilityStatus.DoesNotApply,
                ReasonCode = ApplicabilityReasonCode.DoesNotMatchAssetGroupDataTypes
            };
            assetGroup.Setup(m => m.IsCommandActionable(It.IsAny<Common.PrivacyCommand>(), out applicabilityResult)).Returns(false);

            var checkpointRequest = CreateCheckpointRequest(leaseReceipt, r => r.Status = PrivacyCommandStatus.UnexpectedCommand.ToString());

            CheckpointActionResult checkpointAction = new CheckpointActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                publisher.Object,
                null,                   // null deferral publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                this.mockCommandHistory.Object,
                this.mockApiTrafficHandler.Object);

            HttpResponseMessage response = await checkpointAction.ExecuteAsync(CancellationToken.None);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(callbackInvoked);
        }

        [Fact]
        public async Task CommandUnexpectedVerificationFailure()
        {
            AgentId agentId = this.AnAgentId();
            AssetGroupId assetGroupId = this.AnAssetGroupId();
            CommandId commandId = this.ACommandId();

            LeaseReceipt leaseReceipt = this.ALeaseReceipt(agentId, assetGroupId, commandId);
            Common.DeleteCommand deleteCommand = this.ADeleteCommand(agentId, assetGroupId);

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var publisher = new Mock<ICommandLifecycleEventPublisher>();

            bool callbackInvoked = false;
            string agentState = "new agent state";

            deleteCommand.NextVisibleTime = DateTime.UtcNow;

            leaseReceipt.CommandId = deleteCommand.CommandId;

            this.mockCommandHistory.Setup(c => c.QueryPrivacyCommandAsync(It.IsAny<LeaseReceipt>())).ReturnsAsync(deleteCommand);

            commandQueue.Setup(m => m.SupportsLeaseReceipt(It.IsAny<LeaseReceipt>())).Returns(true);

            commandQueue.Setup(m => m.ReplaceAsync(It.IsAny<LeaseReceipt>(), It.IsAny<Common.PrivacyCommand>(), It.IsAny<CommandReplaceOperations>()))
                        .Callback<LeaseReceipt, Common.PrivacyCommand, CommandReplaceOperations>((lr, pc, cro) =>
                        {
                            Assert.Equal(agentState, pc.AgentState);
                            Assert.True(
                                pc.NextVisibleTime.AddSeconds(1) > DateTimeOffset.UtcNow.AddHours(2) &&
                                pc.NextVisibleTime < DateTimeOffset.UtcNow.AddHours(4).AddSeconds(1));

                            callbackInvoked = true;
                        })
                        .Returns(Task.FromResult(leaseReceipt));

            Mock<IAssetGroupInfo> assetGroup = ConfigureDataAgentMap(dataAgentMap, leaseReceipt);

            var checkpointRequest = CreateCheckpointRequest(leaseReceipt, r =>
            {
                r.Status = PrivacyCommandStatus.UnexpectedVerificationFailure.ToString();
                r.LeaseExtensionSeconds = 86400;
                r.AgentState = agentState;
            });

            CheckpointActionResult checkpointAction = new CheckpointActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                publisher.Object,
                null,                   // null deferral publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                this.mockCommandHistory.Object, 
                this.mockApiTrafficHandler.Object);

            HttpResponseMessage response = await checkpointAction.ExecuteAsync(CancellationToken.None);

            publisher.Verify(
                eventPublisher => eventPublisher.PublishCommandUnexpectedVerificationFailureAsync(
                    It.IsAny<AgentId>(),
                    It.IsAny<AssetGroupId>(),
                    It.IsAny<string>(),
                    It.IsAny<CommandId>(),
                    It.IsAny<PrivacyCommandType>()),
                Times.Once);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(callbackInvoked);
        }

        [Fact]
        public async Task CommandVerificationFailed()
        {
            AgentId agentId = this.AnAgentId();
            AssetGroupId assetGroupId = this.AnAssetGroupId();
            CommandId commandId = this.ACommandId();
            string agentState = "asldfkjalskdjf";

            LeaseReceipt leaseReceipt = this.ALeaseReceipt(agentId, assetGroupId, commandId);
            Common.DeleteCommand deleteCommand = this.ADeleteCommand(agentId, assetGroupId);

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var publisher = new Mock<ICommandLifecycleEventPublisher>();

            bool callbackInvoked = false;

            deleteCommand.NextVisibleTime = DateTime.UtcNow;

            leaseReceipt.CommandId = deleteCommand.CommandId;

            this.mockCommandHistory.Setup(c => c.QueryPrivacyCommandAsync(It.IsAny<LeaseReceipt>())).ReturnsAsync(deleteCommand);

            commandQueue.Setup(m => m.SupportsLeaseReceipt(It.IsAny<LeaseReceipt>())).Returns(true);
            commandQueue.Setup(m => m.ReplaceAsync(It.IsAny<LeaseReceipt>(), It.IsAny<Common.PrivacyCommand>(), It.IsAny<CommandReplaceOperations>()))
                        .Callback<LeaseReceipt, Common.PrivacyCommand, CommandReplaceOperations>((lr, pc, cro) =>
                        {
                            Assert.Equal(agentState, pc.AgentState);
                            Assert.True(
                                pc.NextVisibleTime.AddSeconds(1) > DateTimeOffset.UtcNow.AddHours(20) &&
                                pc.NextVisibleTime < DateTimeOffset.UtcNow.AddHours(28).AddSeconds(1));

                            callbackInvoked = true;
                        })
                        .Returns(Task.FromResult(leaseReceipt));

            Mock<IAssetGroupInfo> assetGroup = ConfigureDataAgentMap(dataAgentMap, leaseReceipt);

            var checkpointRequest = CreateCheckpointRequest(leaseReceipt, r =>
            {
                r.Status = PrivacyCommandStatus.VerificationFailed.ToString();
                r.LeaseExtensionSeconds = 86400;
                r.AgentState = agentState;
            });

            CheckpointActionResult checkpointAction = new CheckpointActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                publisher.Object,
                null,                   // null deferral publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                this.mockCommandHistory.Object,
                this.mockApiTrafficHandler.Object);

            HttpResponseMessage response = await checkpointAction.ExecuteAsync(CancellationToken.None);

            publisher.Verify(
                eventPublisher => eventPublisher.PublishCommandVerificationFailedAsync(
                    It.IsAny<AgentId>(),
                    It.IsAny<AssetGroupId>(),
                    It.IsAny<string>(),
                    It.IsAny<CommandId>(),
                    It.IsAny<PrivacyCommandType>()),
                Times.Once);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(callbackInvoked);
        }

        [Fact]
        public async Task Checkpoint_MalformedLeaseReceipt()
        {
            AgentId agentId = this.AnAgentId();
            AssetGroupId assetGroupId = this.AnAssetGroupId();
            CommandId commandId = this.ACommandId();

            LeaseReceipt leaseReceipt = this.ALeaseReceipt(agentId, assetGroupId, commandId);
            Common.DeleteCommand deleteCommand = this.ADeleteCommand(agentId, assetGroupId);

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var publisher = new Mock<ICommandLifecycleEventPublisher>();

            var request = new Fixture().Create<CheckpointRequest>();

            request.LeaseReceipt = "Bla-bla-bla is unexpected lease.";
            request.Status = PrivacyCommandStatus.Pending.ToString();
            request.Variants = null;

            var checkpointRequest = new HttpRequestMessage(HttpMethod.Post, "/checkpoint")
            {
                Content = new JsonContent(request)
            };

            CheckpointActionResult checkpointAction = new CheckpointActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                publisher.Object,
                null,                   // null deferral publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                this.mockCommandHistory.Object,
                this.mockApiTrafficHandler.Object);


            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);

            await AssertCheckpointErrorAsync(response, HttpStatusCode.BadRequest, CheckpointActionResult.CheckpointErrorCode.MalformedLeaseReceipt);
        }

        [Fact]
        public async Task Checkpoint_EmptyLease_MalformedLeaseReceipt()
        {
            AgentId agentId = this.AnAgentId();
            AssetGroupId assetGroupId = this.AnAssetGroupId();
            CommandId commandId = this.ACommandId();

            LeaseReceipt leaseReceipt = this.ALeaseReceipt(agentId, assetGroupId, commandId);
            Common.DeleteCommand deleteCommand = this.ADeleteCommand(agentId, assetGroupId);

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var publisher = new Mock<ICommandLifecycleEventPublisher>();

            var request = new Fixture().Create<CheckpointRequest>();

            request.LeaseReceipt = string.Empty;
            request.Status = PrivacyCommandStatus.Pending.ToString();
            request.Variants = null;

            var checkpointRequest = new HttpRequestMessage(HttpMethod.Post, "/checkpoint")
            {
                Content = new JsonContent(request)
            };

            CheckpointActionResult checkpointAction = new CheckpointActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                publisher.Object,
                null,                   // null deferral publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                this.mockCommandHistory.Object,
                this.mockApiTrafficHandler.Object);


            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);

            await AssertCheckpointErrorAsync(response, HttpStatusCode.BadRequest, CheckpointActionResult.CheckpointErrorCode.MalformedLeaseReceipt);
        }

        [Fact]
        public async Task Checkpoint_NullLease_MalformedLeaseReceipt()
        {
            AgentId agentId = this.AnAgentId();
            AssetGroupId assetGroupId = this.AnAssetGroupId();
            CommandId commandId = this.ACommandId();

            LeaseReceipt leaseReceipt = this.ALeaseReceipt(agentId, assetGroupId, commandId);
            Common.DeleteCommand deleteCommand = this.ADeleteCommand(agentId, assetGroupId);

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var publisher = new Mock<ICommandLifecycleEventPublisher>();

            var request = new Fixture().Create<CheckpointRequest>();

            request.LeaseReceipt = null;
            request.Status = PrivacyCommandStatus.Pending.ToString();
            request.Variants = null;

            var checkpointRequest = new HttpRequestMessage(HttpMethod.Post, "/checkpoint")
            {
                Content = new JsonContent(request)
            };

            CheckpointActionResult checkpointAction = new CheckpointActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                publisher.Object,
                null,                   // null deferral publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                this.mockCommandHistory.Object,
                this.mockApiTrafficHandler.Object);


            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);

            await AssertCheckpointErrorAsync(response, HttpStatusCode.BadRequest, CheckpointActionResult.CheckpointErrorCode.MalformedLeaseReceipt);
        }

        [Fact]
        public async Task Checkpoint_Pending()
        {
            AgentId agentId = this.AnAgentId();
            AssetGroupId assetGroupId = this.AnAssetGroupId();
            CommandId commandId = this.ACommandId();

            int leaseExtensionSeconds = 600;
            string agentState = "asldfjaslkjdf";

            LeaseReceipt leaseReceipt = this.ALeaseReceipt(agentId, assetGroupId, commandId);
            Common.DeleteCommand deleteCommand = this.ADeleteCommand(agentId, assetGroupId);

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var publisher = new Mock<ICommandLifecycleEventPublisher>();

            this.mockCommandHistory.Setup(c => c.QueryPrivacyCommandAsync(It.IsAny<LeaseReceipt>())).ReturnsAsync(deleteCommand);

            bool callbackInvoked = false;

            leaseReceipt.CommandId = deleteCommand.CommandId;

            commandQueue.Setup(m => m.SupportsLeaseReceipt(It.IsAny<LeaseReceipt>())).Returns(true);

            var expectedNextVisibleTime =
                deleteCommand.NextVisibleTime.AddSeconds(leaseExtensionSeconds);
            commandQueue.Setup(m => m.ReplaceAsync(It.IsAny<LeaseReceipt>(), It.IsAny<Common.PrivacyCommand>(), It.IsAny<CommandReplaceOperations>()))
                        .Callback<LeaseReceipt, Common.PrivacyCommand, CommandReplaceOperations>((lr, pc, cro) =>
                        {
                            Assert.Equal(agentState, pc.AgentState);
                            Assert.True(
                                pc.NextVisibleTime < expectedNextVisibleTime.AddSeconds(1) &&
                                pc.NextVisibleTime > expectedNextVisibleTime.AddSeconds(-1));
                            callbackInvoked = true;
                        })
                        .Returns(Task.FromResult(leaseReceipt));

            Mock<IAssetGroupInfo> assetGroup = ConfigureDataAgentMap(dataAgentMap, leaseReceipt);

            var checkpointRequest = CreateCheckpointRequest(
                leaseReceipt,
                r =>
                {
                    r.LeaseExtensionSeconds = leaseExtensionSeconds;
                    r.Status = PrivacyCommandStatus.Pending.ToString();
                    r.AgentState = agentState;
                });

            CheckpointActionResult checkpointAction = new CheckpointActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                publisher.Object,
                null,                   // null deferral publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                this.mockCommandHistory.Object,
                this.mockApiTrafficHandler.Object);

            HttpResponseMessage response = await checkpointAction.ExecuteAsync(CancellationToken.None);

            publisher.Verify(
                eventPublisher => eventPublisher.PublishCommandPendingAsync(
                    It.IsAny<AgentId>(),
                    It.IsAny<AssetGroupId>(),
                    It.IsAny<string>(),
                    It.IsAny<CommandId>(),
                    It.IsAny<PrivacyCommandType>()),
                Times.Once);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(callbackInvoked);
        }

        [Fact]
        public async Task Checkpoint_UnknownPrivacyCommandStatus()
        {
            AgentId agentId = this.AnAgentId();
            AssetGroupId assetGroupId = this.AnAssetGroupId();
            CommandId commandId = this.ACommandId();

            LeaseReceipt leaseReceipt = this.ALeaseReceipt(agentId, assetGroupId, commandId);
            Common.DeleteCommand deleteCommand = this.ADeleteCommand(agentId, assetGroupId);

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var publisher = new Mock<ICommandLifecycleEventPublisher>();

            var checkpointRequest = CreateCheckpointRequest(leaseReceipt);

            commandQueue.Setup(m => m.QueryCommandAsync(It.IsAny<LeaseReceipt>()))
                        .ReturnsAsync(deleteCommand);

            commandQueue.Setup(m => m.SupportsLeaseReceipt(It.IsAny<LeaseReceipt>())).Returns(true);

            Mock<IAssetGroupInfo> assetGroup = ConfigureDataAgentMap(dataAgentMap, leaseReceipt);

            var checkpointAction = new MockCheckpointActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                publisher.Object,
                null,                   // null deferral publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                this.mockCommandHistory.Object,
                this.mockApiTrafficHandler.Object);


            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);
            await AssertCheckpointErrorAsync(
                response,
                HttpStatusCode.BadRequest,
                CheckpointActionResult.CheckpointErrorCode.UnknownPrivacyCommandStatus);
        }


        [Fact]
        public async Task Checkpoint_GetsThrottled()
        {
            AgentId agentId = this.AnAgentId();
            AssetGroupId assetGroupId = this.AnAssetGroupId();
            CommandId commandId = this.ACommandId();

            LeaseReceipt leaseReceipt = this.ALeaseReceipt(agentId, assetGroupId, commandId);
            Common.DeleteCommand deleteCommand = this.ADeleteCommand(agentId, assetGroupId);

            var authorizer = new Mock<IAuthorizer>();
            var commandQueue = new Mock<ICommandQueue>();
            var dataAgentMap = new Mock<IDataAgentMap>();
            var publisher = new Mock<ICommandLifecycleEventPublisher>();

            leaseReceipt.CommandId = deleteCommand.CommandId;

            this.mockApiTrafficHandler.Setup(m => m.ShouldAllowTraffic("PCF.ApiTrafficPercantage", "PostCheckpoint", agentId.ToString(), It.IsAny<string>())).Returns(false);

            var checkpointRequest = CreateCheckpointRequest(leaseReceipt, r => r.Status = PrivacyCommandStatus.Complete.ToString());

            CheckpointActionResult checkpointAction = new CheckpointActionResult(
                agentId,
                authorizer.Object,
                commandQueue.Object,
                dataAgentMap.Object,
                publisher.Object,
                null,                   // null deferral publisher ensures not used.
                checkpointRequest,
                Logger.Instance,
                this.mockCommandHistory.Object,
                this.mockApiTrafficHandler.Object);

            var response = await checkpointAction.ExecuteAsync(CancellationToken.None);

            // assert
            Assert.Equal((HttpStatusCode)429, response.StatusCode);

            string content = await response.Content.ReadAsStringAsync();
            Assert.Equal("Too Many Requests. Retry later with suggested delay in retry header.", content);

            Assert.Equal(TimeSpan.FromSeconds(5), response.Headers.RetryAfter.Delta.Value);
        }


        [Theory]
        // No violation, recent command, short extension, uses requested extension
        [InlineData(false, false, 1, 1, 1.25)]
        [InlineData(false, true, 1, 1, 1.25)]
        [InlineData(true, false, 1, 1, 1.25)]
        [InlineData(true, true, 1, 1, 1.25)]
        // No violation, recent command, long extension, uses requested extension
        [InlineData(false, false, 25, 1, 25.25)]
        [InlineData(false, true, 25, 1, 25.25)]
        [InlineData(true, false, 25, 1, 25.25)]
        [InlineData(true, true, 25, 1, 25.25)]
        // Violates SLA, small extension, uses requested extension
        [InlineData(false, false, 1, 31, 1.25)]
        [InlineData(false, true, 1, 31, 1.25)]
        [InlineData(true, false, 1, 31, 1.25)]
        [InlineData(true, true, 1, 31, 1.25)]
        // Violates SLA, long extension, 1hr after safe extension threshold, limits to 24hr extension.
        [InlineData(false, false, 241, 11, 24.25)] // 11d + 15min + (10d + 1hr) = 21d 1hr 15min(0.25 hours)
        [InlineData(false, true, 241, 11, 24.25)] // 11d + 15min + (10d + 1hr) = 21d 1hr 15min(0.25 hours)
        [InlineData(true, false, 241, 4, 24.25)] // 4d + 15min + (10d + 1hr) = 14d 1hr 15min(0.25 hours)
        [InlineData(true, true, 241, 11, 24.25)] // 11d + 15min + (10d + 1hr) = 21d 1hr 15min(0.25 hours)
        // Violates SLA, long extension, 15min after safe extension threshold due to the initial next visible time, limits to 24hr extension.
        [InlineData(false, false, 240, 11, 24.25)] // 11d + 15min + 10d = 21d 15min(0.25 hours)
        [InlineData(false, true, 240, 11, 24.25)] // 11d + 15min + 10d = 21d 15min(0.25 hours)
        [InlineData(true, false, 240, 4, 24.25)] // 4d + 15min + 10d = 14d 15min(0.25 hours)
        [InlineData(true, true, 240, 11, 24.25)] // 11d + 15min + 10d = 21d 15min(0.25 hours)
        public void Checkpoint_ValidNextVisibleTime(bool isExport, bool isAadSubject, int requestedExtensionHours, int commandAgeDays, double expectedCalculatedExtension)
        {
            var now = DateTimeOffset.UtcNow;
            Common.PrivacyCommand command;

            if (isExport)
            {
                command = this.AnExportCommand().Build();

                if (isAadSubject)
                {
                    command.Subject = this.AnAadSubject().Build();
                }
                else
                {
                    command.Subject = this.AnMsaSubject().Build();
                }
            }
            else
            {
                command = this.ADeleteCommand().Build();
            }

            command.Timestamp = now.AddDays(-commandAgeDays);

            // Set the initial lease expiration time.
            command.NextVisibleTime = now.AddMinutes(15);

            DateTimeOffset nextVisibleTime = CheckpointActionResult.CalculateNextVisibleTime(command, requestedExtensionHours * 3600);
            double calculatedExtension = nextVisibleTime.Subtract(now).TotalHours;

            Assert.Equal(expectedCalculatedExtension, calculatedExtension);
        }

        private static HttpRequestMessage CreateCheckpointRequest(
            LeaseReceipt leaseReceipt,
            Action<CheckpointRequest> customization = null)
        {
            var request = new Fixture().Create<CheckpointRequest>();

            request.LeaseReceipt = leaseReceipt.Serialize();
            request.Status = PrivacyCommandStatus.Pending.ToString();
            request.Variants = null;
            customization?.Invoke(request);

            return new HttpRequestMessage(HttpMethod.Post, "/checkpoint")
            {
                Content = new JsonContent(request)
            };
        }

        private static HttpRequestMessage CreateCheckpointRequestWithExportFileSizeDetails(
            LeaseReceipt leaseReceipt,
            Action<CheckpointRequest> customization = null)
        {
            var request = new Fixture().Create<CheckpointRequest>();

            request.LeaseReceipt = leaseReceipt.Serialize();
            request.Status = PrivacyCommandStatus.Pending.ToString();
            request.Variants = null;
            customization?.Invoke(request);
            request.ExportedFileSizeDetails = new List<ExportedFileSizeDetails>()
            {
                new ExportedFileSizeDetails("fileName1", 1234, true, 2345),
                new ExportedFileSizeDetails("fileName2", 2345, false, 2345)
            };

            return new HttpRequestMessage(HttpMethod.Post, "/checkpoint")
            {
                Content = new JsonContent(request)
            };
        }

        private static async Task AssertCheckpointErrorAsync(
            HttpResponseMessage response,
            HttpStatusCode httpStatus,
            CheckpointActionResult.CheckpointErrorCode expectedError)
        {
            Assert.Equal(response.StatusCode, httpStatus);

            string content = await response.Content.ReadAsStringAsync();
            var error = JsonConvert.DeserializeObject<CheckpointActionResult.CheckpointError>(content);

            Assert.Equal(expectedError, error.ErrorCode);
        }

        private static Mock<IAssetGroupInfo> ConfigureDataAgentMap(Mock<IDataAgentMap> dataAgentMap, LeaseReceipt leaseReceipt)
        {
            Mock<IAssetGroupInfo> mockAssetGroupInfo = new Mock<IAssetGroupInfo>();
            mockAssetGroupInfo.SetupGet(m => m.AssetGroupId).Returns(leaseReceipt.AssetGroupId);
            IAssetGroupInfo assetGroupInfo = mockAssetGroupInfo.Object;

            Mock<IDataAgentInfo> dataAgentInfo = new Mock<IDataAgentInfo>();
            dataAgentInfo.SetupGet(m => m.AssetGroupInfos).Returns(new[] { mockAssetGroupInfo.Object });
            dataAgentInfo.Setup(m => m.TryGetAssetGroupInfo(It.IsAny<AssetGroupId>(), out assetGroupInfo)).Returns(true);

            IDataAgentInfo info = dataAgentInfo.Object;
            dataAgentMap.Setup(m => m.TryGetAgent(It.IsAny<AgentId>(), out info)).Returns(true);
            dataAgentMap.SetupGet(m => m[leaseReceipt.AgentId]).Returns(dataAgentInfo.Object);
            dataAgentMap.Setup(m => m.TryGetAgent(leaseReceipt.AgentId, out info)).Returns(true);

            return mockAssetGroupInfo;
        }

        private class MockCheckpointActionResult : CheckpointActionResult
        {
            public MockCheckpointActionResult(
                AgentId agentId,
                IAuthorizer authorizer,
                ICommandQueue queue,
                IDataAgentMap dataAgentMap,
                ICommandLifecycleEventPublisher publisher,
                IAzureWorkItemQueuePublisher<DeleteFromQueueWorkItem> deferralPublisher,
                HttpRequestMessage checkpointRequest,
                ILogger logger,
                ICommandHistoryRepository commandHistoryRepository,
                IApiTrafficHandler apiTrafficHandler)
                : base(
                      agentId,
                      authorizer,
                      queue,
                      dataAgentMap,
                      publisher,
                      deferralPublisher,
                      checkpointRequest,
                      logger,
                      commandHistoryRepository,
                      apiTrafficHandler)
            {
                this.RequestStatusFunctionMap = new Dictionary<PrivacyCommandStatus, Func<Task<CheckpointFinishAction>>>();
            }
        }
    }
}

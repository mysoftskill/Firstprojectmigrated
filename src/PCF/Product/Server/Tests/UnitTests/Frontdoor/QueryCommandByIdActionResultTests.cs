namespace PCF.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Client;
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

    using PrivacyCommand = Microsoft.PrivacyServices.CommandFeed.Service.Common.PrivacyCommand;

    [Trait("Category", "UnitTest")]
    public class FrontdoorQueryCommandByIdActionResultTests : INeedDataBuilders
    {
        [Fact]
        public async Task AgentNotAuthorized()
        {
            var map = this.AMockOf<IDataAgentMap>();

            var authorizer = this.AMockOf<IAuthorizer>();
            authorizer.Setup(m => m.CheckAuthorizedAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<AuthenticationScope>()))
                         .Throws(new AuthNException(AuthNErrorCode.AuthenticationFailed, "Some authN problem"));

            QueryCommandByIdActionResult queryCommandAction = new QueryCommandByIdActionResult(
                new HttpRequestMessage(),
                this.ACommandId(),
                this.AnAgentId(),
                this.AnAssetGroupId(),
                authorizer.Object,
                map.Object,
                AuthenticationScope.TestHooks,
                null,
                null);

            var response = await queryCommandAction.ExecuteAsync(CancellationToken.None);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CommandIdNotFoundInCommandHistory()
        {
            var commandHistoryMock = this.AMockOf<ICommandHistoryRepository>();
            commandHistoryMock.Setup(m => m.QueryAsync(It.IsAny<CommandId>(), CommandHistoryFragmentTypes.Core | CommandHistoryFragmentTypes.Status))
                              .ReturnsAsync((CommandHistoryRecord)null);

            await this.RunTest(commandHistoryMock.Object, null, this.AnAgentId(), this.AnAssetGroupId(), QueryCommandByIdActionResult.ResponseCode.CommandNotFound, HttpStatusCode.OK);
        }

        [Fact]
        public async Task CommandIdDoesNotHaveRoutingInformationStored()
        {
            var agentId = this.AnAgentId();
            var assetGroupId = this.AnAssetGroupId();

            CommandHistoryRecord record = this.AColdStorageCommandRecord().Build();
            var status = new CommandHistoryAssetGroupStatusRecord(agentId, assetGroupId)
            {
                StorageAccountMoniker = null,
                IngestionTime = null,
            };

            record.StatusMap[(agentId, assetGroupId)] = status;

            var commandHistoryMock = this.AMockOf<ICommandHistoryRepository>();
            commandHistoryMock.Setup(m => m.QueryAsync(It.IsAny<CommandId>(), CommandHistoryFragmentTypes.Core | CommandHistoryFragmentTypes.Status))
                              .ReturnsAsync(record);

            await this.RunTest(commandHistoryMock.Object, null, agentId, assetGroupId, QueryCommandByIdActionResult.ResponseCode.UnableToResolveLocation, HttpStatusCode.OK);
        }

        [Fact]
        public async Task CommandIdDoesNotYetDelivered()
        {
            var agentId = this.AnAgentId();
            var assetGroupId = this.AnAssetGroupId();

            CommandHistoryRecord record = this.AColdStorageCommandRecord().Build();
            var status = new CommandHistoryAssetGroupStatusRecord(agentId, assetGroupId)
            {
                StorageAccountMoniker = "foobar",
                IngestionTime = null
            };

            record.StatusMap[(agentId, assetGroupId)] = status;

            var commandHistoryMock = this.AMockOf<ICommandHistoryRepository>();
            commandHistoryMock.Setup(m => m.QueryAsync(It.IsAny<CommandId>(), CommandHistoryFragmentTypes.Core | CommandHistoryFragmentTypes.Status))
                              .ReturnsAsync(record);

            await this.RunTest(commandHistoryMock.Object, null, agentId, assetGroupId, QueryCommandByIdActionResult.ResponseCode.CommandNotYetDelivered, HttpStatusCode.OK);
        }

        [Fact]
        public async Task CommandIdWasNotApplicable()
        {
            var agentId = this.AnAgentId();
            var assetGroupId = this.AnAssetGroupId();

            CommandHistoryRecord record = this.AColdStorageCommandRecord().Build();

            var commandHistoryMock = this.AMockOf<ICommandHistoryRepository>();
            commandHistoryMock.Setup(m => m.QueryAsync(It.IsAny<CommandId>(), CommandHistoryFragmentTypes.Core | CommandHistoryFragmentTypes.Status))
                              .ReturnsAsync(record);

            await this.RunTest(commandHistoryMock.Object, null, agentId, assetGroupId, QueryCommandByIdActionResult.ResponseCode.CommandNotApplicable, HttpStatusCode.OK);
        }

        [Theory]
        [InlineData(QueueStorageType.AzureCosmosDb, true)]
        [InlineData(QueueStorageType.AzureQueueStorage, false)]
        [InlineData(QueueStorageType.Undefined, true)]
        public async Task CommandNotQueryable(QueueStorageType queueStorageType, bool shouldBeQueryable)
        {
            var agentId = this.AnAgentId();
            var assetGroupId = this.AnAssetGroupId();

            CommandHistoryRecord record = this.AColdStorageCommandRecord().Build();
            var status = new CommandHistoryAssetGroupStatusRecord(agentId, assetGroupId)
            {
                StorageAccountMoniker = "foobar",
                IngestionTime = DateTimeOffset.UtcNow,
            };
            record.StatusMap[(agentId, assetGroupId)] = status;
            record.Core.QueueStorageType = queueStorageType;

            var queueFactoryMock = this.AMockOf<ICommandQueueFactory>();
            var queueMock = this.AMockOf<ICommandQueue>();
            queueMock.Setup(m => m.QueryCommandAsync(It.IsAny<LeaseReceipt>())).ReturnsAsync(this.ADeleteCommand());
            queueFactoryMock.Setup(m => m.CreateQueue(It.IsAny<AgentId>(), It.IsAny<AssetGroupId>(), It.IsAny<SubjectType>(), queueStorageType)).Returns(queueMock.Object);

            var commandHistoryMock = this.AMockOf<ICommandHistoryRepository>();
            commandHistoryMock.Setup(m => m.QueryAsync(It.IsAny<CommandId>(), CommandHistoryFragmentTypes.Core | CommandHistoryFragmentTypes.Status))
                .ReturnsAsync(record);

            if (!shouldBeQueryable)
            {
                await this.RunTest(commandHistoryMock.Object, queueFactoryMock.Object, agentId, assetGroupId, QueryCommandByIdActionResult.ResponseCode.CommandNotQueryable, HttpStatusCode.MethodNotAllowed);
            }
            else
            {
                await this.RunTest(commandHistoryMock.Object, queueFactoryMock.Object, agentId, assetGroupId, QueryCommandByIdActionResult.ResponseCode.OK, HttpStatusCode.OK);
            }
        }

        [Fact]
        public async Task CommandIdAlreadyCompleted()
        {
            var agentId = this.AnAgentId();
            var assetGroupId = this.AnAssetGroupId();

            CommandHistoryRecord record = this.AColdStorageCommandRecord().Build();
            var status = new CommandHistoryAssetGroupStatusRecord(agentId, assetGroupId)
            {
                StorageAccountMoniker = "foobar",
                IngestionTime = DateTimeOffset.UtcNow,
                CompletedTime = DateTimeOffset.UtcNow,
            };
            record.StatusMap[(agentId, assetGroupId)] = status;

            var commandHistoryMock = this.AMockOf<ICommandHistoryRepository>();
            commandHistoryMock.Setup(m => m.QueryAsync(It.IsAny<CommandId>(), CommandHistoryFragmentTypes.Core | CommandHistoryFragmentTypes.Status))
                              .ReturnsAsync(record);

            await this.RunTest(commandHistoryMock.Object, null, agentId, assetGroupId, QueryCommandByIdActionResult.ResponseCode.CommandAlreadyCompleted, HttpStatusCode.OK);
        }

        [Fact]
        public async Task CommandIdNotFoundInQueue()
        {
            var agentId = this.AnAgentId();
            var assetGroupId = this.AnAssetGroupId();

            CommandHistoryRecord record = this.AColdStorageCommandRecord().Build();
            var status = new CommandHistoryAssetGroupStatusRecord(agentId, assetGroupId)
            {
                StorageAccountMoniker = "foobar",
                IngestionTime = DateTimeOffset.UtcNow,
            };
            record.StatusMap[(agentId, assetGroupId)] = status;

            var commandHistoryMock = this.AMockOf<ICommandHistoryRepository>();
            commandHistoryMock.Setup(m => m.QueryAsync(It.IsAny<CommandId>(), CommandHistoryFragmentTypes.Core | CommandHistoryFragmentTypes.Status))
                              .ReturnsAsync(record);

            var queueFactoryMock = this.AMockOf<ICommandQueueFactory>();
            var queueMock = this.AMockOf<ICommandQueue>();
            queueMock.Setup(m => m.QueryCommandAsync(It.IsAny<LeaseReceipt>())).ReturnsAsync((PrivacyCommand)null);

            queueFactoryMock.Setup(m => m.CreateQueue(It.IsAny<AgentId>(), It.IsAny<AssetGroupId>(), It.IsAny<SubjectType>(), It.IsAny<QueueStorageType>())).Returns(queueMock.Object);

            await this.RunTest(commandHistoryMock.Object, queueFactoryMock.Object, agentId, assetGroupId, QueryCommandByIdActionResult.ResponseCode.CommandNotFoundInQueue, HttpStatusCode.OK);
        }

        [Fact]
        public async Task AllOK()
        {
            var agentId = this.AnAgentId();
            var assetGroupId = this.AnAssetGroupId();

            CommandHistoryRecord record = this.AColdStorageCommandRecord().Build();
            var status = new CommandHistoryAssetGroupStatusRecord(agentId, assetGroupId)
            {
                StorageAccountMoniker = "foobar",
                IngestionTime = DateTimeOffset.UtcNow,
            };
            record.StatusMap[(agentId, assetGroupId)] = status;

            var commandHistoryMock = this.AMockOf<ICommandHistoryRepository>();
            commandHistoryMock.Setup(m => m.QueryAsync(It.IsAny<CommandId>(), CommandHistoryFragmentTypes.Core | CommandHistoryFragmentTypes.Status))
                              .ReturnsAsync(record);

            var queueFactoryMock = this.AMockOf<ICommandQueueFactory>();
            var queueMock = this.AMockOf<ICommandQueue>();
            queueMock.Setup(m => m.QueryCommandAsync(It.IsAny<LeaseReceipt>())).ReturnsAsync(this.ADeleteCommand());

            queueFactoryMock.Setup(m => m.CreateQueue(It.IsAny<AgentId>(), It.IsAny<AssetGroupId>(), It.IsAny<SubjectType>(), It.IsAny<QueueStorageType>())).Returns(queueMock.Object);

            await this.RunTest(commandHistoryMock.Object, queueFactoryMock.Object, agentId, assetGroupId, QueryCommandByIdActionResult.ResponseCode.OK, HttpStatusCode.OK);
        }

        private async Task RunTest(
            ICommandHistoryRepository commandHistory,
            ICommandQueueFactory queueFactory,
            AgentId agentId,
            AssetGroupId assetGroupId,
            QueryCommandByIdActionResult.ResponseCode responseCode,
            HttpStatusCode expectedStatusCode)
        {
            var agentInfo = this.AMockOf<IDataAgentInfo>();
            agentInfo.Setup(x => x.IsOptedIntoAadSubject2()).Returns(false);

            var map = this.AMockOf<IDataAgentMap>();
            map.SetupGet(x => x[agentId]).Returns(agentInfo.Object);

            QueryCommandByIdActionResult queryCommandAction = new QueryCommandByIdActionResult(
                new HttpRequestMessage(),
                this.ACommandId(),
                agentId,
                assetGroupId,
                this.AMockOf<IAuthorizer>().Object,
                map.Object,
                AuthenticationScope.TestHooks,
                commandHistory,
                queueFactory);

            var response = await queryCommandAction.ExecuteAsync(CancellationToken.None);
            Assert.Equal(expectedStatusCode, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var parsedResponse = JsonConvert.DeserializeObject<QueryCommandByIdActionResult.QueryResponse>(body);

            Assert.Equal(responseCode, parsedResponse.ResponseCode);
            if (parsedResponse.ResponseCode == QueryCommandByIdActionResult.ResponseCode.OK)
            {
                Assert.NotNull(parsedResponse.Command);
            }
            else
            {
                Assert.Null(parsedResponse.Command);
            }
        }
    }
}

namespace PCF.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor;
    using Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache;
    using Microsoft.PrivacyServices.PXS.Command.CommandStatus;
    using Microsoft.PrivacyServices.SignalApplicability;
    using Microsoft.Windows.Services.AuthN.Server;
    using Moq;
    using Newtonsoft.Json;
    using Xunit;

    [Trait("Category", "UnitTest")]
    public class GetCommandStatusTests : INeedDataBuilders
    {
        [Fact]
        public async Task GetCommandStatusByIdNotAuthorized()
        {
            var authorizer = new Mock<IAuthorizer>();
            var coldStorage = new Mock<ICommandHistoryRepository>();
            var exportStorageManager = new Mock<IExportStorageManager>();
            var commandId = this.ACommandId();

            authorizer.Setup(m => m.CheckAuthorizedAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<AuthenticationScope>()))
                         .Throws(new AuthNException(AuthNErrorCode.AuthenticationFailed));

            GetCommandStatusByCommandIdActionResult actionResult = new GetCommandStatusByCommandIdActionResult(
                new HttpRequestMessage(),
                commandId,
                coldStorage.Object,
                exportStorageManager.Object,
                new TestDataAgentMap(),
                authorizer.Object,
                AuthenticationScope.TestHooks);

            var response = await actionResult.ExecuteAsync(CancellationToken.None);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetCommandStatusByIdNotFound()
        {
            var authorizer = new Mock<IAuthorizer>();
            var coldStorage = new Mock<ICommandHistoryRepository>();
            var exportStorageManager = new Mock<IExportStorageManager>();
            var commandId = this.ACommandId();

            authorizer.Setup(m => m.CheckAuthorizedAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<AuthenticationScope>()))
                      .Returns(Task.FromResult(new PcfAuthenticationContext()));

            coldStorage.Setup(m => m.QueryAsync(It.IsAny<CommandId>(), It.IsAny<CommandHistoryFragmentTypes>()))
                       .Returns(Task.FromResult((CommandHistoryRecord)null));

            GetCommandStatusByCommandIdActionResult actionResult = new GetCommandStatusByCommandIdActionResult(
                new HttpRequestMessage(),
                commandId,
                coldStorage.Object,
                exportStorageManager.Object,
                new TestDataAgentMap(),
                authorizer.Object,
                AuthenticationScope.TestHooks);

            var response = await actionResult.ExecuteAsync(CancellationToken.None);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task QueryCommandStatus()
        {
            var authorizer = new Mock<IAuthorizer>();
            var coldStorage = new Mock<ICommandHistoryRepository>();
            var exportStorageManager = new Mock<IExportStorageManager>();
            var commandId1 = this.ACommandId();
            var commandId2 = this.ACommandId();

            authorizer.Setup(m => m.CheckAuthorizedAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<AuthenticationScope>()))
                      .Returns(Task.FromResult(new PcfAuthenticationContext()));

            var testMap = new TestDataAgentMap();
            var record1 = CreateTestRecord(commandId1, testMap, new MsaSubject() { Anid = "anid", Cid = 42, Opid = "opid", Puid = 42 });
            var record2 = CreateTestRecord(commandId2, testMap, new AadSubject() { OrgIdPUID = 42 });

            coldStorage.Setup(m => m.QueryAsync(It.IsAny<IPrivacySubject>(), It.IsAny<string>(), It.IsAny<IList<PrivacyCommandType>>(), It.IsAny<DateTimeOffset>(), It.IsAny<CommandHistoryFragmentTypes>()))
                       .ReturnsAsync(new[] { record1, record2, });

            exportStorageManager.Setup(m => m.GetReadOnlyContainerUri(It.IsAny<Uri>())).Returns(new Uri("https://www.bing.com"));

            QueryCommandStatusActionResult actionResult = new QueryCommandStatusActionResult(
                new MsaSubject { Puid = 42 },
                "requester",
                new[] { PrivacyCommandType.Export, PrivacyCommandType.Delete },
                DateTimeOffset.MinValue,
                new HttpRequestMessage(),
                coldStorage.Object,
                exportStorageManager.Object,
                testMap,
                authorizer.Object,
                AuthenticationScope.TestHooks);

            var response = await actionResult.ExecuteAsync(CancellationToken.None);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var parsedResponse = JsonConvert.DeserializeObject<CommandStatusResponse[]>(await response.Content.ReadAsStringAsync());

            Assert.Equal(2, parsedResponse.Length);
            Assert.Contains(parsedResponse, x => x.CommandId.ToString("n") == commandId1.Value);
            Assert.Contains(parsedResponse, x => x.CommandId.ToString("n") == commandId2.Value);

            // Make sure that redacted is the default behavior.
            foreach (var responseItem in parsedResponse)
            {
                Assert.Equal(GetCommandStatusActionResult.RedactedReplacementString, responseItem.Requester);
                Assert.Equal(GetCommandStatusActionResult.RedactedReplacementString, responseItem.Context);
                Assert.Equal(new Uri("https://" + GetCommandStatusActionResult.RedactedReplacementString), responseItem.FinalExportDestinationUri);
                Assert.Null(responseItem.Subject);
            }
        }

        [Fact]
        public Task GetCommandStatusByIdWithRedaction()
        {
            var authorizer = new Mock<IAuthorizer>();
            var coldStorage = new Mock<ICommandHistoryRepository>();
            var exportStorageManager = new Mock<IExportStorageManager>();
            var commandId = this.ACommandId();

            return this.GetCommandStatusById(commandId, true, authorizer, coldStorage, exportStorageManager);
        }
        
        [Fact]
        public Task GetCommandStatusByIdWithoutRedaction()
        {
            var authorizer = new Mock<IAuthorizer>();
            var coldStorage = new Mock<ICommandHistoryRepository>();
            var exportStorageManager = new Mock<IExportStorageManager>();
            var commandId = this.ACommandId();

            return this.GetCommandStatusById(commandId, false, authorizer, coldStorage, exportStorageManager);
        }

        [Fact]
        public void GetCommandStatusSuccessRateNullCompletedCount()
        {
            long? completedCommands = null;
            long forceCompletedCommands = 0;
            long? totalCommands = 1;

            var successRate = GetCommandStatusActionResult.GetCompletionSuccessRate(completedCommands, forceCompletedCommands, totalCommands);

            Assert.True(successRate < 0);
        }

        [Fact]
        public void GetCommandStatusSuccessRateNullTotalCount()
        {
            long? completedCommands = 5;
            long forceCompletedCommands = 0;
            long? totalCommands = null;

            var successRate = GetCommandStatusActionResult.GetCompletionSuccessRate(completedCommands, forceCompletedCommands, totalCommands);

            Assert.True(successRate < 0);
        }

        [Fact]
        public void GetCommandStatusSuccessRateZeroTotalCount()
        {
            long? completedCommands = 5;
            long forceCompletedCommands = 0;
            long? totalCommands = 0;

            var successRate = GetCommandStatusActionResult.GetCompletionSuccessRate(completedCommands, forceCompletedCommands, totalCommands);

            Assert.True(successRate < 0);
        }

        [Fact]
        public void GetCommandStatusSuccessRateValidNumbers()
        {
            long? completedCommands = 6;
            long forceCompletedCommands = 1;
            long? totalCommands = 10;

            var successRate = GetCommandStatusActionResult.GetCompletionSuccessRate(completedCommands, forceCompletedCommands, totalCommands);

            Assert.Equal(0.5, successRate);
        }

        private async Task GetCommandStatusById(
            CommandId commandId,
            bool redactionOn,
            Mock<IAuthorizer> authorizer,
            Mock<ICommandHistoryRepository> coldStorage,
            Mock<IExportStorageManager> exportStorageManager)
        {
            var agentMap = new TestDataAgentMap();

            authorizer.Setup(m => m.CheckAuthorizedAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<AuthenticationScope>()))
                      .Returns(Task.FromResult(new PcfAuthenticationContext()));

            var record = CreateTestRecord(commandId, new TestDataAgentMap(), new AadSubject() { OrgIdPUID = 42 });
            coldStorage.Setup(m => m.QueryAsync(commandId, It.IsAny<CommandHistoryFragmentTypes>())).ReturnsAsync(record);

            exportStorageManager.Setup(m => m.GetReadOnlyContainerUri(It.IsAny<Uri>())).Returns(record.Core.FinalExportDestinationUri);

            GetCommandStatusByCommandIdActionResult actionResult = new GetCommandStatusByCommandIdActionResult(
                new HttpRequestMessage(),
                commandId,
                coldStorage.Object,
                exportStorageManager.Object,
                agentMap,
                authorizer.Object,
                AuthenticationScope.TestHooks);
            
            actionResult.RedactConfidentialFields = redactionOn;

            var response = await actionResult.ExecuteAsync(CancellationToken.None);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var parsedResponse = JsonConvert.DeserializeObject<CommandStatusResponse>(await response.Content.ReadAsStringAsync());

            Assert.Equal(parsedResponse.CommandId.ToString("n"), commandId.Value);
            Assert.Equal(record.Core.IsGloballyComplete, parsedResponse.IsGloballyComplete);
            Assert.Equal(record.Core.CompletedTime ?? DateTimeOffset.MinValue, parsedResponse.CompletedTime);

            if (redactionOn)
            {
                Assert.Equal(GetCommandStatusActionResult.RedactedReplacementString, parsedResponse.Requester);
                Assert.Equal(GetCommandStatusActionResult.RedactedReplacementString, parsedResponse.Context);
                Assert.Equal(new Uri("https://" + GetCommandStatusActionResult.RedactedReplacementString), parsedResponse.FinalExportDestinationUri);
                Assert.Null(parsedResponse.Subject);
            }
            else
            {
                Assert.Equal(record.Core.Requester, parsedResponse.Requester);
                Assert.Equal(record.Core.Context, parsedResponse.Context);
                Assert.Equal(record.Core.FinalExportDestinationUri, parsedResponse.FinalExportDestinationUri);

                Assert.IsType<AadSubject>(parsedResponse.Subject);
                Assert.Equal(42, ((AadSubject)parsedResponse.Subject).OrgIdPUID);
            }

            foreach (var assetGroupStatus in parsedResponse.AssetGroupStatuses)
            {
                Assert.True(agentMap.TryGetAgent(new AgentId(assetGroupStatus.AgentId), out var agentInfo));
                Assert.True(agentInfo.TryGetAssetGroupInfo(new AssetGroupId(assetGroupStatus.AssetGroupId), out var assetGroupInfo));

                (AgentId, AssetGroupId) key = (assetGroupInfo.AgentId, assetGroupInfo.AssetGroupId);

                CommandIngestionAuditRecord auditRecord = record.AuditMap[key];
                CommandHistoryAssetGroupStatusRecord statusRecord = record.StatusMap[key];

                Assert.Equal(auditRecord.DebugText, assetGroupStatus.IngestionDebugText);
                Assert.Equal(auditRecord.IngestionStatus.ToString(), assetGroupStatus.IngestionActionTaken);

                Assert.Equal(statusRecord.CompletedTime, assetGroupStatus.CompletedTime);
                Assert.Equal(statusRecord.SoftDeleteTime, assetGroupStatus.SoftDeleteTime);
                Assert.Equal(statusRecord.IngestionTime, assetGroupStatus.IngestionTime);
            }
        }

        private static CommandHistoryRecord CreateTestRecord(CommandId commandId, IDataAgentMap dataAgentMap, IPrivacySubject subject)
        {
            var completed = DateTimeOffset.UtcNow;
            var softDeleted = DateTimeOffset.UtcNow.AddMinutes(-5);
            var started = DateTimeOffset.UtcNow.AddMinutes(-60);

            CommandHistoryRecord record = new CommandHistoryRecord(commandId);

            record.Core.CommandType = PrivacyCommandType.Delete;
            record.Core.Context = "super secret context";
            record.Core.Requester = "highly classified";
            record.Core.FinalExportDestinationUri = new Uri("https://also-very-secret.com");
            record.Core.IsGloballyComplete = true;
            record.Core.Subject = subject;
            record.Core.QueueStorageType = QueueStorageType.AzureCosmosDb;

            foreach (var agentId in dataAgentMap.GetAgentIds())
            {
                var agentInfo = dataAgentMap[agentId];
                foreach (var assetGroup in agentInfo.AssetGroupInfos)
                {
                    record.AuditMap[(agentId, assetGroup.AssetGroupId)] = new CommandIngestionAuditRecord
                    {
                        ApplicabilityReasonCode = ApplicabilityReasonCode.None,
                        DebugText = "debug text",
                        IngestionStatus = CommandIngestionStatus.SentToAgent,
                    };

                    record.StatusMap[(agentId, assetGroup.AssetGroupId)] = new CommandHistoryAssetGroupStatusRecord(assetGroup.AgentId, assetGroup.AssetGroupId)
                    {
                        ClaimedVariants = new[] { "v1", },
                        CompletedTime = completed,
                        SoftDeleteTime = softDeleted,
                        IngestionTime = started,
                        Delinked = false,
                    };
                }
            }

            return record;
        }
    }
}

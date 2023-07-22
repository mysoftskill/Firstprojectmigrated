namespace PCF.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.Documents;
    using Microsoft.Identity.Client;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandHistory;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.SignalApplicability;
    using Moq;
    using PCF.UnitTests.CommandLifecycle;
    using Xunit;

    using PrivacyCommand = Microsoft.PrivacyServices.CommandFeed.Service.Common.PrivacyCommand;

    [Trait("Category", "UnitTest")]
    public class CommandHistoryRepositoryTests : BaseCommandLifeCycleTests, INeedDataBuilders
    {
        #region Where Clause Building Tests

        [Fact]
        public async Task QueryMsaSubjectTest()
        {
            var oldestRecord = DateTimeOffset.UtcNow.AddDays(-1);

            await QueryTest(
                new MsaSubject { Puid = 42 },
                null,
                null,
                oldestRecord,
                "SELECT * FROM c WHERE c.s.puid = @puid AND c.crt >= @createdTime",
                new Dictionary<string, object>
                {
                    ["@puid"] = "42",
                    ["@createdTime"] = oldestRecord.ToUnixTimeSeconds(),
                });
        }

        [Fact]
        public async Task QueryAadSubjectTest()
        {
            Guid aadId = Guid.NewGuid();
            var oldestRecord = DateTimeOffset.UtcNow.AddDays(-1);

            await QueryTest(
                new AadSubject { ObjectId = aadId },
                null,
                null,
                oldestRecord,
                "SELECT * FROM c WHERE c.s.objectId = @objectId AND c.crt >= @createdTime",
                new Dictionary<string, object>
                {
                    ["@objectId"] = aadId,
                    ["@createdTime"] = oldestRecord.ToUnixTimeSeconds(),
                });
        }

        [Fact]
        public async Task QueryRequesterTest()
        {
            this.Initialize();
            var oldestRecord = DateTimeOffset.UtcNow.AddDays(-1);

            await QueryTest(
                null,
                "foo",
                null,
                oldestRecord,
                "SELECT * FROM c WHERE  c.r IN (@requester_0,@requester_1) AND c.crt >= @createdTime",
                new Dictionary<string, object>
                {
                    ["@requester_0"] = "foo",
                    ["@requester_1"] = "bar",
                    ["@createdTime"] = oldestRecord.ToUnixTimeSeconds(),
                });
        }

        [Fact]
        public async Task QueryRequesterTestWhereRequestorIsNotPresentInConfig() // When the requester is not PCD
        {
            this.Initialize();
            var oldestRecord = DateTimeOffset.UtcNow.AddDays(-1);

            await QueryTest(
                null,
                "baz",
                null,
                oldestRecord,
                "SELECT * FROM c WHERE c.r = @requester AND c.crt >= @createdTime",
                new Dictionary<string, object>
                {
                    ["@requester"] = "baz",
                    ["@createdTime"] = oldestRecord.ToUnixTimeSeconds(),
                });
        }

        [Fact]
        public async Task QuerySingleCommandTypeTest()
        {
            var oldestRecord = DateTimeOffset.UtcNow.AddDays(-1);

            await QueryTest(
                null,
                null,
                new[] { PrivacyCommandType.Export },
                oldestRecord,
                "SELECT * FROM c WHERE (c.ct = @commandType0) AND c.crt >= @createdTime",
                new Dictionary<string, object>
                {
                    ["@commandType0"] = (int)PrivacyCommandType.Export,
                    ["@createdTime"] = oldestRecord.ToUnixTimeSeconds(),
                });
        }

        [Fact]
        public async Task QueryMultipleCommandTypesTest()
        {
            var oldestRecord = DateTimeOffset.UtcNow.AddDays(-1);

            await QueryTest(
                null,
                null,
                new[] { PrivacyCommandType.Export, PrivacyCommandType.Delete },
                oldestRecord,
                "SELECT * FROM c WHERE (c.ct = @commandType0 OR c.ct = @commandType1) AND c.crt >= @createdTime",
                new Dictionary<string, object>
                {
                    ["@commandType0"] = (int)PrivacyCommandType.Export,
                    ["@commandType1"] = (int)PrivacyCommandType.Delete,
                    ["@createdTime"] = oldestRecord.ToUnixTimeSeconds(),
                });
        }

        [Fact]
        public async Task QuerySinceHourAgoTest()
        {
            var oldestRecord = DateTimeOffset.UtcNow.AddDays(-1);

            await QueryTest(
                null,
                null,
                null,
                oldestRecord,
                "SELECT * FROM c WHERE c.crt >= @createdTime",
                new Dictionary<string, object>
                {
                    ["@createdTime"] = oldestRecord.ToUnixTimeSeconds(),
                });
        }

        [Fact]
        public async Task QueryAllTogetherNowTestWhenRequesterIsPCD()
        {
            this.Initialize();
            var oldestRecord = DateTimeOffset.UtcNow.AddDays(-1);
            Guid aadId = Guid.NewGuid();

            await QueryTest(
                new AadSubject { ObjectId = aadId },
                "foo",
                new[] { PrivacyCommandType.Export, PrivacyCommandType.Delete },
                oldestRecord,
                "SELECT * FROM c WHERE c.s.objectId = @objectId AND c.r IN (@requester_0,@requester_1) AND (c.ct = @commandType0 OR c.ct = @commandType1) AND c.crt >= @createdTime",
                new Dictionary<string, object>
                {
                    ["@objectId"] = aadId,
                    ["@requester_0"] = "foo",
                    ["@requester_1"] = "bar",
                    ["@commandType0"] = (int)PrivacyCommandType.Export,
                    ["@commandType1"] = (int)PrivacyCommandType.Delete,
                    ["@createdTime"] = oldestRecord.ToUnixTimeSeconds(),
                });
        }

        [Fact]
        public async Task QueryAllTogetherNowTest()
        {
            this.Initialize();
            var oldestRecord = DateTimeOffset.UtcNow.AddDays(-1);
            Guid aadId = Guid.NewGuid();

            await QueryTest(
                new AadSubject { ObjectId = aadId },
                "baz",
                new[] { PrivacyCommandType.Export, PrivacyCommandType.Delete },
                oldestRecord,
                "SELECT * FROM c WHERE c.s.objectId = @objectId AND c.r = @requester AND (c.ct = @commandType0 OR c.ct = @commandType1) AND c.crt >= @createdTime",
                new Dictionary<string, object>
                {
                    ["@objectId"] = aadId,
                    ["@requester"] = "baz",
                    ["@commandType0"] = (int)PrivacyCommandType.Export,
                    ["@commandType1"] = (int)PrivacyCommandType.Delete,
                    ["@createdTime"] = oldestRecord.ToUnixTimeSeconds(),
                });
        }

        #endregion

        #region Query Tests

        [Theory]
        [InlineData("2019-01-01", true)]
        [InlineData("1/1/0001 12:00:00 AM +00:00", false)] // this is default(DateTimeOffset), which is not a valid completed time
        [InlineData(null, false)]
        public async Task QueryIsCompleteByAgentTest(string completedTime, bool expectedQueryIsComplete)
        {
            var commandId = this.ACommandId();
            BlobPointer statusPointer = CreateBlobPointer();
            var agentId = this.AnAgentId();
            var assetGroupId = this.AnAssetGroupId();

            var statusDocs = new[]
            {
                new AssetGroupStatusDocument(new CommandHistoryAssetGroupStatusRecord(agentId, assetGroupId)
                {
                    ClaimedVariants = new string[0],
                    IngestionTime = DateTimeOffset.UtcNow,
                    CompletedTime = !string.IsNullOrWhiteSpace(completedTime) ? DateTimeOffset.Parse(completedTime) : (DateTimeOffset?)null
                })
            };

            var statusEtag = Guid.NewGuid().ToString();

            CoreCommandDocument coreCommandDocument = this.ACoreCommandDocument()
                .With(c => c.StatusBlobPointer, statusPointer)
                .With(c => c.Id, commandId.Value)
                .Build();
            Mock<ICommandHistoryBlobClient> mockBlob = new Mock<ICommandHistoryBlobClient>();
            mockBlob.Setup(m => m.ReadBlobAsync<AssetGroupStatusDocument[]>(statusPointer))
                .ReturnsAsync((statusDocs, statusEtag));

            Mock<ICommandHistoryDocDbClient> mockDocDb = new Mock<ICommandHistoryDocDbClient>();
            mockDocDb.Setup(c => c.PointQueryAsync(It.IsAny<CommandId>())).ReturnsAsync(coreCommandDocument);
            var repository = new CommandHistoryRepository(mockDocDb.Object, mockBlob.Object, null, null);
            var leaseReceipt = new LeaseReceipt("a", "token", this.AnAgeOutCommand().Build(), QueueStorageType.AzureCosmosDb)
            {
                AgentId = agentId,
                AssetGroupId = assetGroupId
            };

            bool result = await repository.QueryIsCompleteByAgentAsync(leaseReceipt);
            Assert.Equal(expectedQueryIsComplete, result);
            mockBlob.Verify(c => c.ReadBlobAsync<AssetGroupStatusDocument[]>(statusPointer), Times.Once);
        }

        [Theory]
        [InlineData(QueueStorageType.AzureCosmosDb)]
        [InlineData(QueueStorageType.AzureQueueStorage)]
        public async Task QueryPrivacyCommandByLeaseReceipt(QueueStorageType queueStorageType)
        {
            CoreCommandDocument coreCommandDocument = this.ACoreCommandDocument().Build();
            Mock<ICommandHistoryBlobClient> mockBlob = new Mock<ICommandHistoryBlobClient>();
            Mock<ICommandHistoryDocDbClient> mockDocDb = new Mock<ICommandHistoryDocDbClient>();
            mockDocDb.Setup(c => c.PointQueryAsync(It.IsAny<CommandId>())).ReturnsAsync(coreCommandDocument);
            var repository = new CommandHistoryRepository(mockDocDb.Object, mockBlob.Object, null, null);
            var leaseReceipt = new LeaseReceipt("a", "token", this.AnAgeOutCommand().Build(), queueStorageType);

            PrivacyCommand result = await repository.QueryPrivacyCommandAsync(leaseReceipt);

            Assert.NotNull(result);
            Assert.Equal(new CommandId(coreCommandDocument.Id), result.CommandId);

            Assert.Equal(leaseReceipt.AgentId, result.AgentId);
            Assert.Equal(leaseReceipt.AgentId, result.LeaseReceipt.AgentId);

            Assert.Equal(leaseReceipt.AssetGroupId, result.AssetGroupId);
            Assert.Equal(leaseReceipt.AssetGroupId, result.LeaseReceipt.AssetGroupId);

            Assert.Equal(leaseReceipt.AssetGroupQualifier, result.AssetGroupQualifier);
            Assert.Equal(leaseReceipt.AssetGroupQualifier, result.LeaseReceipt.AssetGroupQualifier);

            Assert.Equal(leaseReceipt.Token, result.LeaseReceipt.Token);
            Assert.Equal(leaseReceipt.ApproximateExpirationTime, result.NextVisibleTime);
            Assert.Equal(queueStorageType, leaseReceipt.QueueStorageType);
        }

        [Fact]
        public async Task ReadAndUpdateByCommandId()
        {
            var commandId = this.ACommandId();
            var mockDocDb = new Mock<ICommandHistoryDocDbClient>();
            var mockBlob = new Mock<ICommandHistoryBlobClient>();

            var mockDisposable = new Mock<IList<string>>().Object;

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            BlobPointer statusPointer = CreateBlobPointer();
            BlobPointer auditPointer = CreateBlobPointer();
            BlobPointer exportPointer = CreateBlobPointer();

#pragma warning disable CS0618 // Type or member is obsolete
            mockDocDb.Setup(m => m.PointQueryAsync(commandId))
                     .ReturnsAsync(new CoreCommandDocument()
                     {
                         AuditBlobPointer = auditPointer,
                         StatusBlobPointer = statusPointer,
                         ExportDestinationBlobPointer = exportPointer,
                         Id = commandId.Value,
                         Subject = this.AnMsaSubject().Build(),
                     });

            var agentId = this.AnAgentId();
            var assetGroupId = this.AnAssetGroupId();

            var auditDocs = new AssetGroupAuditDocument[]
            {
                new AssetGroupAuditDocument(
                    agentId, 
                    assetGroupId, 
                    new CommandIngestionAuditRecord
                    {
                        ApplicabilityReasonCode = ApplicabilityReasonCode.None,
                        DebugText = "OK",
                        IngestionStatus = CommandIngestionStatus.SentToAgent
                    })
            };

            var auditEtag = Guid.NewGuid().ToString();

            var statusDocs = new AssetGroupStatusDocument[]
            {
                new AssetGroupStatusDocument(new CommandHistoryAssetGroupStatusRecord(agentId, assetGroupId)
                {
                    ClaimedVariants = new string[0],
                    IngestionTime = DateTimeOffset.UtcNow,
                })
            };

            var statusEtag = Guid.NewGuid().ToString();

            var exportDocs = new ExportDestinationDocument[0];
            var exportEtag = Guid.NewGuid().ToString();

            mockBlob.Setup(m => m.ReadBlobAsync<AssetGroupAuditDocument[]>(auditPointer))
                    .ReturnsAsync((auditDocs, auditEtag));

            mockBlob.Setup(m => m.ReadBlobAsync<AssetGroupStatusDocument[]>(statusPointer))
                    .ReturnsAsync((statusDocs, statusEtag));

            mockBlob.Setup(m => m.ReadBlobAsync<ExportDestinationDocument[]>(exportPointer))
                    .ReturnsAsync((exportDocs, exportEtag));

            bool auditReplaced = false;
            mockBlob.Setup(m => m.ReplaceBlobAsync(auditPointer, It.IsAny<object>(), auditEtag))
                    .Callback(() => auditReplaced = true)
                    .Returns(Task.FromResult(true));

#pragma warning restore CS0618 // Type or member is obsolete

            var repository = new CommandHistoryRepository(mockDocDb.Object, mockBlob.Object, null, null);

            var record = await repository.QueryAsync(commandId, CommandHistoryFragmentTypes.All);

            Assert.NotNull(record);
            Assert.Equal(CommandHistoryFragmentTypes.None, record.GetChangedFragments());
            Assert.Equal(commandId, record.CommandId);

            Assert.NotNull(record.Core);
            Assert.Single(record.AuditMap);
            Assert.Single(record.StatusMap);
            Assert.Empty(record.ExportDestinations);

            var context = (CommandHistoryOperationContext)record.ReadContext;

            Assert.Equal(statusPointer, context.StatusBlobPointer);
            Assert.Equal(auditPointer, context.AuditBlobPointer);
            Assert.Equal(exportPointer, context.ExportDestinationBlobPointer);
            Assert.Equal(statusEtag, context.StatusBlobEtag);
            Assert.Equal(auditEtag, context.AuditBlobEtag);
            Assert.Equal(exportEtag, context.ExportDestinationBlobEtag);

            // Try an update and writeback.
            record.AuditMap.First().Value.DebugText = "blah";
            Assert.True(record.AuditMap.IsDirty);

            // Nothing
            await Assert.ThrowsAsync<InvalidOperationException>(() => repository.ReplaceAsync(record, CommandHistoryFragmentTypes.None));

            // Not changed.
            await Assert.ThrowsAsync<InvalidOperationException>(() => repository.ReplaceAsync(record, CommandHistoryFragmentTypes.Status));

            // Only partially changed
            await Assert.ThrowsAsync<InvalidOperationException>(() => repository.ReplaceAsync(record, CommandHistoryFragmentTypes.Status | CommandHistoryFragmentTypes.Audit));

            Assert.False(auditReplaced);

            // Works!
            await repository.ReplaceAsync(record, CommandHistoryFragmentTypes.Audit);

            Assert.True(auditReplaced);
        }

        [Fact]
        public async Task TestQueryCommandsForReplay()
        {
            Mock<ICommandHistoryBlobClient> mockBlob = new Mock<ICommandHistoryBlobClient>();
            Mock<ICommandHistoryDocDbClient> mockDocDb = new Mock<ICommandHistoryDocDbClient>();

            DateTimeOffset startTime = DateTimeOffset.UtcNow.AddDays(-1);
            DateTimeOffset endTime = DateTimeOffset.UtcNow;

            mockDocDb.Setup(m => m.CrossPartitionQueryAsync(It.IsAny<SqlQuerySpec>(), It.IsAny<string>(), It.IsAny<int>()))
                     .Callback<SqlQuerySpec, string, int>((s, ct, maxItemCount) =>
                     {
                         Assert.Equal("SELECT * FROM c WHERE c.ct != 2 AND c.crt >= @startTime AND c.crt < @endTime", s.QueryText);
                         Assert.Equal(2, s.Parameters.Count);
                         Assert.Equal(startTime.ToUnixTimeSeconds(), s.Parameters.Single(x => x.Name == "@startTime").Value);
                         Assert.Equal(endTime.ToUnixTimeSeconds(), s.Parameters.Single(x => x.Name == "@endTime").Value);
                     })
                     .ReturnsAsync((new CoreCommandDocument[0], null));

            var repository = new CommandHistoryRepository(mockDocDb.Object, mockBlob.Object, null, null);

            var result = await repository.GetCommandsForReplayAsync(
                startTime,
                endTime,
                null,
                false,
                null);

            Assert.Empty(result.pxsCommands);
            Assert.Null(result.continuationToken);
        }

        [Fact]
        public async Task TestQueryCommandsForReplayWithSubject()
        {
            Mock<ICommandHistoryBlobClient> mockBlob = new Mock<ICommandHistoryBlobClient>();
            Mock<ICommandHistoryDocDbClient> mockDocDb = new Mock<ICommandHistoryDocDbClient>();

            DateTimeOffset startTime = DateTimeOffset.UtcNow.AddDays(-1);
            DateTimeOffset endTime = DateTimeOffset.UtcNow;

            mockDocDb.Setup(m => m.CrossPartitionQueryAsync(It.IsAny<SqlQuerySpec>(), It.IsAny<string>(), It.IsAny<int>()))
                     .Callback<SqlQuerySpec, string, int>((s, ct, maxItemCount) =>
                     {
                         Assert.Equal("SELECT * FROM c WHERE c.ct != 2 AND c.crt >= @startTime AND c.crt < @endTime AND c.s.type = \"aad\"", s.QueryText);
                         Assert.Equal(2, s.Parameters.Count);
                         Assert.Equal(startTime.ToUnixTimeSeconds(), s.Parameters.Single(x => x.Name == "@startTime").Value);
                         Assert.Equal(endTime.ToUnixTimeSeconds(), s.Parameters.Single(x => x.Name == "@endTime").Value);
                     })
                     .ReturnsAsync((new CoreCommandDocument[0], null));

            var repository = new CommandHistoryRepository(mockDocDb.Object, mockBlob.Object, null, null);

            var result = await repository.GetCommandsForReplayAsync(
                startTime,
                endTime,
                "aad",
                false,
                null);

            Assert.Empty(result.pxsCommands);
            Assert.Null(result.continuationToken);
        }

        [Fact]
        public async Task TestQueryCommandsForReplayForExport()
        {
            Mock<ICommandHistoryBlobClient> mockBlob = new Mock<ICommandHistoryBlobClient>();
            Mock<ICommandHistoryDocDbClient> mockDocDb = new Mock<ICommandHistoryDocDbClient>();
            Mock<IAppConfiguration> mockAppConfig = new Mock<IAppConfiguration>();

            DateTimeOffset startTime = DateTimeOffset.UtcNow.AddDays(-1);
            DateTimeOffset endTime = DateTimeOffset.UtcNow;

            mockDocDb.Setup(m => m.CrossPartitionQueryAsync(It.IsAny<SqlQuerySpec>(), It.IsAny<string>(), It.IsAny<int>()))
                     .Callback<SqlQuerySpec, string, int>((s, ct, maxItemCount) =>
                     {
                         Assert.Equal("SELECT * FROM c WHERE c.crt >= @startTime AND c.crt < @endTime", s.QueryText);
                         Assert.Equal(2, s.Parameters.Count);
                         Assert.Equal(startTime.ToUnixTimeSeconds(), s.Parameters.Single(x => x.Name == "@startTime").Value);
                         Assert.Equal(endTime.ToUnixTimeSeconds(), s.Parameters.Single(x => x.Name == "@endTime").Value);
                     })
                     .ReturnsAsync((new CoreCommandDocument[0], null));
            mockAppConfig.Setup(a => a.IsFeatureFlagEnabledAsync(FeatureNames.PCF.EnableExportCommandReplay, It.IsAny<bool>()))
                .ReturnsAsync(true);

            var repository = new CommandHistoryRepository(mockDocDb.Object, mockBlob.Object, null, mockAppConfig.Object);

            var result = await repository.GetCommandsForReplayAsync(
                startTime,
                endTime,
                null,
                true,
                null);

            Assert.Empty(result.pxsCommands);
            Assert.Null(result.continuationToken);
        }

        [Fact]
        public async Task TestQueryCommandsForReplayWithExportFeatureFlagDisabled()
        {
            Mock<ICommandHistoryBlobClient> mockBlob = new Mock<ICommandHistoryBlobClient>();
            Mock<ICommandHistoryDocDbClient> mockDocDb = new Mock<ICommandHistoryDocDbClient>();
            Mock<IAppConfiguration> mockAppConfig = new Mock<IAppConfiguration>();

            DateTimeOffset startTime = DateTimeOffset.UtcNow.AddDays(-1);
            DateTimeOffset endTime = DateTimeOffset.UtcNow;

            mockDocDb.Setup(m => m.CrossPartitionQueryAsync(It.IsAny<SqlQuerySpec>(), It.IsAny<string>(), It.IsAny<int>()))
                     .Callback<SqlQuerySpec, string, int>((s, ct, maxItemCount) =>
                     {
                         Assert.Equal("SELECT * FROM c WHERE c.ct != 2 AND c.crt >= @startTime AND c.crt < @endTime", s.QueryText);
                         Assert.Equal(2, s.Parameters.Count);
                         Assert.Equal(startTime.ToUnixTimeSeconds(), s.Parameters.Single(x => x.Name == "@startTime").Value);
                         Assert.Equal(endTime.ToUnixTimeSeconds(), s.Parameters.Single(x => x.Name == "@endTime").Value);
                     })
                     .ReturnsAsync((new CoreCommandDocument[0], null));

            mockAppConfig.Setup(a => a.IsFeatureFlagEnabledAsync(FeatureNames.PCF.EnableExportCommandReplay, It.IsAny<bool>()))
                .ReturnsAsync(false);

            var repository = new CommandHistoryRepository(mockDocDb.Object, mockBlob.Object, null, mockAppConfig.Object);


            var result = await repository.GetCommandsForReplayAsync(
                startTime,
                endTime,
                null,
                true,
                null);

            Assert.Empty(result.pxsCommands);
            Assert.Null(result.continuationToken);
        }

        [Fact]
        public async Task QueryIncompleteExports_Aad()
        {
            Mock<ICommandHistoryBlobClient> mockBlob = new Mock<ICommandHistoryBlobClient>();
            Mock<ICommandHistoryDocDbClient> mockDocDb = new Mock<ICommandHistoryDocDbClient>();

            DateTimeOffset startTime = DateTimeOffset.UtcNow.AddDays(-1);
            DateTimeOffset endTime = DateTimeOffset.UtcNow;

            mockDocDb.Setup(m => m.CrossPartitionQueryAsync(It.IsAny<SqlQuerySpec>(), It.IsAny<string>(), It.IsAny<int>()))
                     .Callback<SqlQuerySpec, string, int>((s, ct, maxItemCount) =>
                     {
                         Assert.Equal("SELECT * FROM c WHERE c.ct = @commandType AND (c.crt BETWEEN @oldestRecord AND @newestRecord) AND c.c = false AND c.s.type = @subject", s.QueryText);
                         Assert.Equal(4, s.Parameters.Count);
                         Assert.Equal(startTime.ToUnixTimeSeconds(), s.Parameters.Single(x => x.Name == "@oldestRecord").Value);
                         Assert.Equal(endTime.ToUnixTimeSeconds(), s.Parameters.Single(x => x.Name == "@newestRecord").Value);
                         Assert.Equal(2, s.Parameters.Single(x => x.Name == "@commandType").Value);
                         Assert.Equal("aad", s.Parameters.Single(x => x.Name == "@subject").Value);
                     })
                     .ReturnsAsync((new CoreCommandDocument[0], null));

            var repository = new CommandHistoryRepository(mockDocDb.Object, mockBlob.Object, null, null);

            var result = await repository.QueryIncompleteExportsAsync(
                startTime,
                endTime,
                true,
                CommandHistoryFragmentTypes.Core);

            Assert.Empty(result);
        }

        [Fact]
        public async Task QueryIncompleteExports_NonAad()
        {
            Mock<ICommandHistoryBlobClient> mockBlob = new Mock<ICommandHistoryBlobClient>();
            Mock<ICommandHistoryDocDbClient> mockDocDb = new Mock<ICommandHistoryDocDbClient>();

            DateTimeOffset startTime = DateTimeOffset.UtcNow.AddDays(-1);
            DateTimeOffset endTime = DateTimeOffset.UtcNow;

            mockDocDb.Setup(m => m.CrossPartitionQueryAsync(It.IsAny<SqlQuerySpec>(), It.IsAny<string>(), It.IsAny<int>()))
                     .Callback<SqlQuerySpec, string, int>((s, ct, maxItemCount) =>
                     {
                         Assert.Equal("SELECT * FROM c WHERE c.ct = @commandType AND (c.crt BETWEEN @oldestRecord AND @newestRecord) AND c.c = false AND c.s.type != @subject", s.QueryText);
                         Assert.Equal(4, s.Parameters.Count);
                         Assert.Equal(startTime.ToUnixTimeSeconds(), s.Parameters.Single(x => x.Name == "@oldestRecord").Value);
                         Assert.Equal(endTime.ToUnixTimeSeconds(), s.Parameters.Single(x => x.Name == "@newestRecord").Value);
                         Assert.Equal(2, s.Parameters.Single(x => x.Name == "@commandType").Value);
                         Assert.Equal("aad", s.Parameters.Single(x => x.Name == "@subject").Value);
                     })
                     .ReturnsAsync((new CoreCommandDocument[0], null));

            var repository = new CommandHistoryRepository(mockDocDb.Object, mockBlob.Object, null, null);

            var result = await repository.QueryIncompleteExportsAsync(
                startTime,
                endTime,
                false,
                CommandHistoryFragmentTypes.Core);

            Assert.Empty(result);
        }

        #endregion

        #region Insert Tests

        [Fact]
        public async Task InsertNewCommandRecord_Success()
        {
            var commandId = this.ACommandId();
            var mockDocDb = new Mock<ICommandHistoryDocDbClient>();
            var mockBlob = new Mock<ICommandHistoryBlobClient>();

            bool coreInserted = false;
            bool auditInserted = false;
            bool statusInserted = false;
            bool exportInserted = false;

            mockBlob.Setup(m => m.CreateBlobAsync(It.IsAny<AssetGroupAuditDocument[]>()))
                    .Callback(() => auditInserted = true)
                    .ReturnsAsync(CreateBlobPointer());

            mockBlob.Setup(m => m.CreateBlobAsync(It.IsAny<AssetGroupStatusDocument[]>()))
                    .Callback(() => statusInserted = true)
                    .ReturnsAsync(CreateBlobPointer());

            mockBlob.Setup(m => m.CreateBlobAsync(It.IsAny<ExportDestinationDocument[]>()))
                    .Callback(() => exportInserted = true)
                    .ReturnsAsync(CreateBlobPointer());

            mockDocDb.Setup(m => m.InsertAsync(It.IsAny<CoreCommandDocument>()))
                     .Callback(() => coreInserted = true)
                     .Returns(Task.FromResult(true));

            CommandHistoryRecord record = new CommandHistoryRecord(this.ACommandId());

            record.Core.CommandType = PrivacyCommandType.Delete;
            record.Core.CreatedTime = DateTimeOffset.UtcNow;

            record.AuditMap[(this.AnAgentId(), this.AnAssetGroupId())] = new CommandIngestionAuditRecord
            {
                ApplicabilityReasonCode = ApplicabilityReasonCode.AssetGroupInfoIsDeprecated,
                DebugText = ":(",
                IngestionStatus = CommandIngestionStatus.DroppedDueToFiltering
            };

            var repository = new CommandHistoryRepository(mockDocDb.Object, mockBlob.Object, null, null);
            bool result = await repository.TryInsertAsync(record);

            Assert.True(auditInserted);
            Assert.True(exportInserted);
            Assert.True(statusInserted);
            Assert.True(coreInserted);

            Assert.True(result);
        }

        [Fact]
        public async Task InsertNewCommandRecord_BlobConflict()
        {
            var commandId = this.ACommandId();
            var mockDocDb = new Mock<ICommandHistoryDocDbClient>();
            var mockBlob = new Mock<ICommandHistoryBlobClient>();

            bool coreInserted = false;
            bool auditInserted = false;
            bool statusInserted = false;
            bool exportInserted = false;

            mockBlob.Setup(m => m.CreateBlobAsync(It.IsAny<AssetGroupAuditDocument[]>()))
                    .Callback(() => auditInserted = true)
                    .ReturnsAsync(CreateBlobPointer());

            mockBlob.Setup(m => m.CreateBlobAsync(It.IsAny<AssetGroupStatusDocument[]>()))
                    .ThrowsAsync(new CommandFeedException("conflict") { ErrorCode = CommandFeedInternalErrorCode.Conflict });

            mockBlob.Setup(m => m.CreateBlobAsync(It.IsAny<ExportDestinationDocument[]>()))
                    .Callback(() => exportInserted = true)
                    .ReturnsAsync(CreateBlobPointer());

            mockDocDb.Setup(m => m.InsertAsync(It.IsAny<CoreCommandDocument>()))
                     .Callback(() => coreInserted = true)
                     .Returns(Task.FromResult(true));

            CommandHistoryRecord record = new CommandHistoryRecord(this.ACommandId());

            record.Core.CommandType = PrivacyCommandType.Delete;
            record.Core.CreatedTime = DateTimeOffset.UtcNow;

            record.AuditMap[(this.AnAgentId(), this.AnAssetGroupId())] = new CommandIngestionAuditRecord
            {
                ApplicabilityReasonCode = ApplicabilityReasonCode.AssetGroupInfoIsDeprecated,
                DebugText = ":(",
                IngestionStatus = CommandIngestionStatus.DroppedDueToFiltering
            };

            var repository = new CommandHistoryRepository(mockDocDb.Object, mockBlob.Object, null, null);
            bool result = await repository.TryInsertAsync(record);

            Assert.True(auditInserted);
            Assert.True(exportInserted);
            Assert.False(statusInserted);
            Assert.False(coreInserted);

            Assert.False(result);
        }

        [Fact]
        public async Task InsertNewCommandRecord_CoreConflict()
        {
            var commandId = this.ACommandId();
            var mockDocDb = new Mock<ICommandHistoryDocDbClient>();
            var mockBlob = new Mock<ICommandHistoryBlobClient>();

            bool coreInserted = false;
            bool auditInserted = false;
            bool statusInserted = false;
            bool exportInserted = false;

            mockBlob.Setup(m => m.CreateBlobAsync(It.IsAny<AssetGroupAuditDocument[]>()))
                    .Callback(() => auditInserted = true)
                    .ReturnsAsync(CreateBlobPointer());

            mockBlob.Setup(m => m.CreateBlobAsync(It.IsAny<AssetGroupStatusDocument[]>()))
                    .Callback(() => statusInserted = true)
                    .ReturnsAsync(CreateBlobPointer());

            mockBlob.Setup(m => m.CreateBlobAsync(It.IsAny<ExportDestinationDocument[]>()))
                    .Callback(() => exportInserted = true)
                    .ReturnsAsync(CreateBlobPointer());

            mockDocDb.Setup(m => m.InsertAsync(It.IsAny<CoreCommandDocument>()))
                     .Throws(new CommandFeedException("conflict") { ErrorCode = CommandFeedInternalErrorCode.Conflict });

            CommandHistoryRecord record = new CommandHistoryRecord(this.ACommandId());

            record.Core.CommandType = PrivacyCommandType.Delete;
            record.Core.CreatedTime = DateTimeOffset.UtcNow;

            record.AuditMap[(this.AnAgentId(), this.AnAssetGroupId())] = new CommandIngestionAuditRecord
            {
                ApplicabilityReasonCode = ApplicabilityReasonCode.AssetGroupInfoIsDeprecated,
                DebugText = ":(",
                IngestionStatus = CommandIngestionStatus.DroppedDueToFiltering
            };

            var repository = new CommandHistoryRepository(mockDocDb.Object, mockBlob.Object, null, null);
            bool result = await repository.TryInsertAsync(record);

            Assert.True(auditInserted);
            Assert.True(exportInserted);
            Assert.True(statusInserted);
            Assert.False(coreInserted);

            Assert.False(result);
        }

        [Fact]
        public async Task InsertNewCommandRecord_BlobThrottle()
        {
            var commandId = this.ACommandId();
            var mockDocDb = new Mock<ICommandHistoryDocDbClient>();
            var mockBlob = new Mock<ICommandHistoryBlobClient>();

            bool coreInserted = false;
            bool auditInserted = false;
            bool statusInserted = false;
            bool exportInserted = false;

            mockBlob.Setup(m => m.CreateBlobAsync(It.IsAny<AssetGroupAuditDocument[]>()))
                    .Callback(() => auditInserted = true)
                    .ReturnsAsync(CreateBlobPointer());

            mockBlob.Setup(m => m.CreateBlobAsync(It.IsAny<AssetGroupStatusDocument[]>()))
                    .ThrowsAsync(new CommandFeedException("throttle") { ErrorCode = CommandFeedInternalErrorCode.Throttle });

            mockBlob.Setup(m => m.CreateBlobAsync(It.IsAny<ExportDestinationDocument[]>()))
                    .Callback(() => exportInserted = true)
                    .ReturnsAsync(CreateBlobPointer());

            mockDocDb.Setup(m => m.InsertAsync(It.IsAny<CoreCommandDocument>()))
                     .Callback(() => coreInserted = true)
                     .Returns(Task.FromResult(true));

            CommandHistoryRecord record = new CommandHistoryRecord(this.ACommandId());

            record.Core.CommandType = PrivacyCommandType.Delete;
            record.Core.CreatedTime = DateTimeOffset.UtcNow;

            record.AuditMap[(this.AnAgentId(), this.AnAssetGroupId())] = new CommandIngestionAuditRecord
            {
                ApplicabilityReasonCode = ApplicabilityReasonCode.AssetGroupInfoIsDeprecated,
                DebugText = ":(",
                IngestionStatus = CommandIngestionStatus.DroppedDueToFiltering
            };

            var repository = new CommandHistoryRepository(mockDocDb.Object, mockBlob.Object, null, null);
            await Assert.ThrowsAsync<CommandFeedException>(() => repository.TryInsertAsync(record));

            Assert.True(auditInserted);
            Assert.True(exportInserted);
            Assert.False(statusInserted);
            Assert.False(coreInserted);
        }

        [Fact]
        public async Task InsertNewCommandRecord_CoreThrottle()
        {
            var commandId = this.ACommandId();
            var mockDocDb = new Mock<ICommandHistoryDocDbClient>();
            var mockBlob = new Mock<ICommandHistoryBlobClient>();

            bool coreInserted = false;
            bool auditInserted = false;
            bool statusInserted = false;
            bool exportInserted = false;

            mockBlob.Setup(m => m.CreateBlobAsync(It.IsAny<AssetGroupAuditDocument[]>()))
                    .Callback(() => auditInserted = true)
                    .ReturnsAsync(CreateBlobPointer());

            mockBlob.Setup(m => m.CreateBlobAsync(It.IsAny<AssetGroupStatusDocument[]>()))
                    .Callback(() => statusInserted = true)
                    .ReturnsAsync(CreateBlobPointer());

            mockBlob.Setup(m => m.CreateBlobAsync(It.IsAny<ExportDestinationDocument[]>()))
                    .Callback(() => exportInserted = true)
                    .ReturnsAsync(CreateBlobPointer());

            mockDocDb.Setup(m => m.InsertAsync(It.IsAny<CoreCommandDocument>()))
                    .Throws(new CommandFeedException("throttle") { ErrorCode = CommandFeedInternalErrorCode.Throttle });

            CommandHistoryRecord record = new CommandHistoryRecord(this.ACommandId());

            record.Core.CommandType = PrivacyCommandType.Delete;
            record.Core.CreatedTime = DateTimeOffset.UtcNow;

            record.AuditMap[(this.AnAgentId(), this.AnAssetGroupId())] = new CommandIngestionAuditRecord
            {
                ApplicabilityReasonCode = ApplicabilityReasonCode.AssetGroupInfoIsDeprecated,
                DebugText = ":(",
                IngestionStatus = CommandIngestionStatus.DroppedDueToFiltering
            };

            var repository = new CommandHistoryRepository(mockDocDb.Object, mockBlob.Object, null, null);
            await Assert.ThrowsAsync<CommandFeedException>(() => repository.TryInsertAsync(record));

            Assert.True(auditInserted);
            Assert.True(exportInserted);
            Assert.True(statusInserted);
            Assert.False(coreInserted);
        }

        #endregion

        private static BlobPointer CreateBlobPointer()
        {
            return new BlobPointer
            {
                AccountName = Guid.NewGuid().ToString(),
                BlobName = Guid.NewGuid().ToString(),
                ContainerName = Guid.NewGuid().ToString(),
            };
        }

        private static async Task QueryTest(
            IPrivacySubject subject, 
            string requester, 
            IList<PrivacyCommandType> commandTypes, 
            DateTimeOffset oldestRecord,
            string expectedWhereStatement,
            Dictionary<string, object> expectedParameters)
        {
            Mock<ICommandHistoryBlobClient> mockBlob = new Mock<ICommandHistoryBlobClient>();
            Mock<ICommandHistoryDocDbClient> mockDocDb = new Mock<ICommandHistoryDocDbClient>();

            mockDocDb.Setup(m => m.MaxParallelismCrossPartitionQueryAsync(It.IsAny<SqlQuerySpec>(), It.IsAny<string>()))
                     .Callback<SqlQuerySpec, string>((s, ct) =>
                     {
                         Assert.Equal(expectedWhereStatement, s.QueryText);
                         Assert.Equal(expectedParameters.Count, s.Parameters.Count);

                         foreach (var kvp in expectedParameters)
                         {
                             Assert.Equal(kvp.Value, s.Parameters.Single(x => x.Name == kvp.Key).Value);
                         }
                     })
                     .ReturnsAsync((new CoreCommandDocument[0], null));

            var repository = new CommandHistoryRepository(mockDocDb.Object, mockBlob.Object, null, null);
            var result = await repository.QueryAsync(subject, requester, commandTypes, oldestRecord, CommandHistoryFragmentTypes.Core);
        }
    }
}

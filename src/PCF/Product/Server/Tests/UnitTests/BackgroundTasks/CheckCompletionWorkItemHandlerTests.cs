namespace PCF.UnitTests.BackgroundTasks
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks.CommandStatusAggregation;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeedv2.Common.Storage;
    using Moq;
    using Xunit;

    [Trait("Category", "UnitTest")]
    public class CheckCompletionWorkItemHandlerTests
    {
        /// <summary>
        /// Verify completion when there is no command history.
        /// </summary>
        [Fact]
        public async void CompleteWithNoCommandHistory()
        {
            var repository = new Mock<ICommandHistoryRepository>();
            repository.Setup(r => r.QueryAsync(It.IsAny<CommandId>(), It.IsAny<CommandHistoryFragmentTypes>()))
                .Returns(Task.FromResult<CommandHistoryRecord>(null));
            
            var appConfiguration = new Mock<IAppConfiguration>();
            appConfiguration.Setup(a => a.IsFeatureFlagEnabledAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(false);

            var exportInfoProvider = new Mock<IPCFv2ExportInfoProvider>();
            exportInfoProvider.Setup(r => r.GetExpectionWorkerLastestRunTimeAsync())
                .Returns(Task.FromResult(DateTime.Now));

            var cosmosDBClientFactory = new Mock<ICosmosDbClientFactory>();
            var handler = new CheckCompletionWorkItemQueueHandler(repository.Object, null, appConfiguration.Object, cosmosDBClientFactory.Object, exportInfoProvider.Object);
            var result = await handler.ProcessWorkItemAsync(new QueueWorkItemWrapper<CheckCompletionWorkItem>(
                new CheckCompletionWorkItem(),
                Mock.Of<IAzureCloudQueue>(),
                null,
                null));

            Assert.True(result.Complete);
        }

        /// <summary>
        /// Verify completion when command is already globally complete.
        /// </summary>
        [Fact]
        public async void CompleteWithGloballyComplete()
        {
            var commandId = new CommandId(Guid.NewGuid().ToString());
            var commandHistoryCore = new CommandHistoryCoreRecord(commandId);
            commandHistoryCore.IsGloballyComplete = true;
            var commandHistory = new CommandHistoryRecord(commandId, commandHistoryCore, null, null, null, null); ;

            var repository = new Mock<ICommandHistoryRepository>();
            repository.Setup(r => r.QueryAsync(It.IsAny<CommandId>(), It.IsAny<CommandHistoryFragmentTypes>()))
                .Returns(Task.FromResult(commandHistory));

            var appConfiguration = new Mock<IAppConfiguration>();
            appConfiguration.Setup(a => a.IsFeatureFlagEnabledAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(false);

            var exportInfoProvider = new Mock<IPCFv2ExportInfoProvider>();
            exportInfoProvider.Setup(r => r.GetExpectionWorkerLastestRunTimeAsync())
                .Returns(Task.FromResult(DateTime.Now));

            var cosmosDBClientFactory = new Mock<ICosmosDbClientFactory>();
            var handler = new CheckCompletionWorkItemQueueHandler(repository.Object, null, appConfiguration.Object, cosmosDBClientFactory.Object, exportInfoProvider.Object);
            var result = await handler.ProcessWorkItemAsync(new QueueWorkItemWrapper<CheckCompletionWorkItem>(
                new CheckCompletionWorkItem(),
                Mock.Of<IAzureCloudQueue>(),
                null,
                null));

            Assert.True(result.Complete);
        }

        /// <summary>
        /// Verify retry when completion time is not set for all history records.
        /// </summary>
        [Fact]
        public async void RetryIncompletedTime()
        {
            var agentId = new AgentId(Guid.NewGuid());
            var assetGroupId1 = new AssetGroupId(Guid.NewGuid());
            var assetGroupId2 = new AssetGroupId(Guid.NewGuid());

            var commandStatusRecord1 = new CommandHistoryAssetGroupStatusRecord(agentId, assetGroupId1);
            commandStatusRecord1.CompletedTime = null;
            var commandStatusRecord2 = new CommandHistoryAssetGroupStatusRecord(agentId, assetGroupId2);
            commandStatusRecord2.CompletedTime = DateTimeOffset.UtcNow;

            IDictionary<ValueTuple<AgentId, AssetGroupId>, CommandHistoryAssetGroupStatusRecord> statusMap =
                new Dictionary<ValueTuple<AgentId, AssetGroupId>, CommandHistoryAssetGroupStatusRecord>()
                {
                    { new ValueTuple<AgentId, AssetGroupId>(agentId, assetGroupId1), commandStatusRecord1 },
                    { new ValueTuple<AgentId, AssetGroupId>(agentId, assetGroupId2), commandStatusRecord2 }
                };

            var commandId = new CommandId(Guid.NewGuid().ToString());
            var commandHistoryCore = new CommandHistoryCoreRecord(commandId);
            var commandHistory = new CommandHistoryRecord(commandId, commandHistoryCore, null, statusMap, null, null); ;

            var repository = new Mock<ICommandHistoryRepository>();
            repository.Setup(r => r.QueryAsync(It.IsAny<CommandId>(), It.IsAny<CommandHistoryFragmentTypes>()))
                .Returns(Task.FromResult(commandHistory));

            var appConfiguration = new Mock<IAppConfiguration>();
            appConfiguration.Setup(a => a.IsFeatureFlagEnabledAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(false);

            var exportInfoProvider = new Mock<IPCFv2ExportInfoProvider>();
            exportInfoProvider.Setup(r => r.GetExpectionWorkerLastestRunTimeAsync())
                .Returns(Task.FromResult(DateTime.Now));

            var cosmosDBClientFactory = new Mock<ICosmosDbClientFactory>();

            var handler = new CheckCompletionWorkItemQueueHandler(repository.Object, null, appConfiguration.Object, cosmosDBClientFactory.Object, exportInfoProvider.Object);
            var result = await handler.ProcessWorkItemAsync(new QueueWorkItemWrapper<CheckCompletionWorkItem>(
                new CheckCompletionWorkItem(),
                Mock.Of<IAzureCloudQueue>(),
                null,
                null));

            Assert.False(result.Complete);
            Assert.Equal(TimeSpan.FromDays(1), result.Delay);
        }

        /// <summary>
        /// Verify retry of ingestion time is not set for any history record.
        /// </summary>
        [Fact]
        public async void RetryIngestionTime()
        {
            var agentId = new AgentId(Guid.NewGuid());
            var assetGroupId1 = new AssetGroupId(Guid.NewGuid());
            var assetGroupId2 = new AssetGroupId(Guid.NewGuid());

            var commandStatusRecord1 = new CommandHistoryAssetGroupStatusRecord(agentId, assetGroupId1);
            commandStatusRecord1.IngestionTime = null;
            var commandStatusRecord2 = new CommandHistoryAssetGroupStatusRecord(agentId, assetGroupId2);
            commandStatusRecord2.IngestionTime = null;

            IDictionary<ValueTuple<AgentId, AssetGroupId>, CommandHistoryAssetGroupStatusRecord> statusMap =
                new Dictionary<ValueTuple<AgentId, AssetGroupId>, CommandHistoryAssetGroupStatusRecord>()
                {
                    { new ValueTuple<AgentId, AssetGroupId>(agentId, assetGroupId1), commandStatusRecord1 },
                    { new ValueTuple<AgentId, AssetGroupId>(agentId, assetGroupId2), commandStatusRecord2 }
                };

            var commandId = new CommandId(Guid.NewGuid().ToString());
            var commandHistoryCore = new CommandHistoryCoreRecord(commandId);
            var commandHistory = new CommandHistoryRecord(commandId, commandHistoryCore, null, statusMap, null, null); ;

            var repository = new Mock<ICommandHistoryRepository>();
            repository.Setup(r => r.QueryAsync(It.IsAny<CommandId>(), It.IsAny<CommandHistoryFragmentTypes>()))
                .Returns(Task.FromResult(commandHistory));

            var appConfiguration = new Mock<IAppConfiguration>();
            appConfiguration.Setup(a => a.IsFeatureFlagEnabledAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(false);

            var exportInfoProvider = new Mock<IPCFv2ExportInfoProvider>();
            exportInfoProvider.Setup(r => r.GetExpectionWorkerLastestRunTimeAsync())
                .Returns(Task.FromResult(DateTime.Now));

            var cosmosDBClientFactory = new Mock<ICosmosDbClientFactory>();

            var handler = new CheckCompletionWorkItemQueueHandler(repository.Object, null, appConfiguration.Object, cosmosDBClientFactory.Object, exportInfoProvider.Object);
            var result = await handler.ProcessWorkItemAsync(new QueueWorkItemWrapper<CheckCompletionWorkItem>(
                new CheckCompletionWorkItem(),
                Mock.Of<IAzureCloudQueue>(),
                null,
                null));

            Assert.False(result.Complete);
            Assert.Equal(TimeSpan.FromDays(1), result.Delay);
        }

        /// <summary>
        /// Verify completion for delete command.
        /// </summary>
        [Fact]
        public async void CompleteDeleteCommand()
        {
            var commandId = new CommandId(Guid.NewGuid().ToString());
            var commandHistoryCore = new CommandHistoryCoreRecord(commandId);
            commandHistoryCore.CommandType = PrivacyCommandType.Delete;
            var commandHistory = new CommandHistoryRecord(commandId, commandHistoryCore, null, MakeDefaultStatusMap(), null, null); ;

            var repository = new Mock<ICommandHistoryRepository>();
            repository.Setup(r => r.QueryAsync(commandId, CommandHistoryFragmentTypes.Core | CommandHistoryFragmentTypes.Status | CommandHistoryFragmentTypes.ExportDestinations))
                .Returns(Task.FromResult(commandHistory))
                .Verifiable();

            repository.Setup(r => r.ReplaceAsync(commandHistory, CommandHistoryFragmentTypes.Core))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var appConfiguration = new Mock<IAppConfiguration>();
            appConfiguration.Setup(a => a.IsFeatureFlagEnabledAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(false);

            var exportInfoProvider = new Mock<IPCFv2ExportInfoProvider>();
            exportInfoProvider.Setup(r => r.GetExpectionWorkerLastestRunTimeAsync())
                .Returns(Task.FromResult(DateTime.Now));

            var cosmosDBClientFactory = new Mock<ICosmosDbClientFactory>();

            var handler = new CheckCompletionWorkItemQueueHandler(repository.Object, null, appConfiguration.Object, cosmosDBClientFactory.Object, exportInfoProvider.Object);
            var result = await handler.ProcessWorkItemAsync(new QueueWorkItemWrapper<CheckCompletionWorkItem>(
                new CheckCompletionWorkItem() { CommandId = commandId },
                Mock.Of<IAzureCloudQueue>(),
                null,
                null));

            Assert.True(result.Complete);
            Assert.NotNull(commandHistory.Core.CompletedTime);
            repository.VerifyAll();
        }

        /// <summary>
        /// Verify completion for export command.
        /// </summary>
        [Fact]
        public async void CompleteExportCommand()
        {
            var commandId = new CommandId(Guid.NewGuid().ToString());
            var commandHistoryCore = new CommandHistoryCoreRecord(commandId);
            commandHistoryCore.CommandType = PrivacyCommandType.Export;
            var commandHistory = new CommandHistoryRecord(commandId, commandHistoryCore, null, MakeDefaultStatusMap(), null, null); ;

            var repository = new Mock<ICommandHistoryRepository>();
            repository.Setup(r => r.QueryAsync(commandId, CommandHistoryFragmentTypes.Core | CommandHistoryFragmentTypes.Status | CommandHistoryFragmentTypes.ExportDestinations))
                .Returns(Task.FromResult(commandHistory))
                .Verifiable();

            repository.Setup(r => r.ReplaceAsync(commandHistory, CommandHistoryFragmentTypes.Core))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var appConfiguration = new Mock<IAppConfiguration>();
            appConfiguration.Setup(a => a.IsFeatureFlagEnabledAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(false);

            var exportInfoProvider = new Mock<IPCFv2ExportInfoProvider>();
            exportInfoProvider.Setup(r => r.GetExpectionWorkerLastestRunTimeAsync())
                .Returns(Task.FromResult(DateTime.Now));

            var cosmosDBClientFactory = new Mock<ICosmosDbClientFactory>();

            var handler = new CheckCompletionWorkItemQueueHandler(repository.Object, null, appConfiguration.Object, cosmosDBClientFactory.Object, exportInfoProvider.Object);
            var result = await handler.ProcessWorkItemAsync(new QueueWorkItemWrapper<CheckCompletionWorkItem>(
                new CheckCompletionWorkItem() { CommandId = commandId },
                Mock.Of<IAzureCloudQueue>(),
                null,
                null));

            Assert.True(result.Complete);
            Assert.NotNull(commandHistory.Core.CompletedTime);
            repository.VerifyAll();
        }

        [Fact]
        public async void VerifyExpectationWorkerRuntimeCheck()
        {
            var commandId = new CommandId(Guid.NewGuid().ToString());
            var commandHistoryCore = new CommandHistoryCoreRecord(commandId);
            commandHistoryCore.CommandType = PrivacyCommandType.Export;
            commandHistoryCore.CreatedTime = DateTime.Now.AddDays(2);
            var commandHistory = new CommandHistoryRecord(commandId, commandHistoryCore, null, MakeDefaultStatusMap(), null, null); ;

            var repository = new Mock<ICommandHistoryRepository>();
            repository.Setup(r => r.QueryAsync(commandId, CommandHistoryFragmentTypes.Core | CommandHistoryFragmentTypes.Status | CommandHistoryFragmentTypes.ExportDestinations))
                .Returns(Task.FromResult(commandHistory))
                .Verifiable();

            var appConfiguration = new Mock<IAppConfiguration>();
            appConfiguration.Setup(a => a.IsFeatureFlagEnabledAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(true);

            var cosmosDBClientFactory = new Mock<ICosmosDbClientFactory>();
            var exportExpecatationEntry = new Mock<ICosmosDbClient<ExportExpectationEventEntry>>();
            var exportCompletedEntry = new Mock<ICosmosDbClient<CompletedExportEventEntry>>();

            var exportInfoProvider = new Mock<IPCFv2ExportInfoProvider>();
            exportInfoProvider.Setup(r => r.GetExpectionWorkerLastestRunTimeAsync())
                .Returns(Task.FromResult(DateTime.Now))
                .Verifiable();

            cosmosDBClientFactory.Setup(r => r.GetCosmosDbClient<ExportExpectationEventEntry>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(exportExpecatationEntry.Object)
                .Verifiable();

            cosmosDBClientFactory.Setup(r => r.GetCosmosDbClient<CompletedExportEventEntry>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
               .Returns(exportCompletedEntry.Object)
               .Verifiable();

            IList<CompletedExportEventEntry> results = new List<CompletedExportEventEntry>();
            exportCompletedEntry.Setup(r => r.ReadEntriesAsync(It.IsAny<string>(), null, CancellationToken.None))
                .Returns(Task.FromResult(results))
                .Verifiable();

            var handler = new CheckCompletionWorkItemQueueHandler(repository.Object, null, appConfiguration.Object, cosmosDBClientFactory.Object, exportInfoProvider.Object);
            var result = await handler.ProcessWorkItemAsync(new QueueWorkItemWrapper<CheckCompletionWorkItem>(
                new CheckCompletionWorkItem() { CommandId = commandId },
                Mock.Of<IAzureCloudQueue>(),
                null,
                null));

            Assert.False(result.Complete);
            // this has to match the delay in checkcompletionworker
            Assert.Equal(TimeSpan.FromHours(2), result.Delay);

            repository.VerifyAll();
            exportInfoProvider.VerifyAll();
            cosmosDBClientFactory.VerifyAll();
            exportCompletedEntry.VerifyAll();
        }

        [Fact]
        public async void VerifyPCFV2ExportDestinationsAreAddedToCommandHistoryRecord()
        {
            var commandId = new CommandId(Guid.NewGuid().ToString());
            var commandHistoryCore = new CommandHistoryCoreRecord(commandId);
            commandHistoryCore.CommandType = PrivacyCommandType.Export;
            commandHistoryCore.CreatedTime = DateTime.Now.Subtract(TimeSpan.FromDays(1));
            commandHistoryCore.FinalExportDestinationUri = ExportStorageManager.Instance.GetManagedStorageUri();
            var commandHistory = new CommandHistoryRecord(commandId, commandHistoryCore, null, MakeDefaultStatusMap(), null, null); ;

            var repository = new Mock<ICommandHistoryRepository>();
            repository.Setup(r => r.QueryAsync(commandId, CommandHistoryFragmentTypes.Core | CommandHistoryFragmentTypes.Status | CommandHistoryFragmentTypes.ExportDestinations))
                .Returns(Task.FromResult(commandHistory))
                .Verifiable();

            var appConfiguration = new Mock<IAppConfiguration>();
            appConfiguration.Setup(a => a.IsFeatureFlagEnabledAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(true);

            var cosmosDBClientFactory = new Mock<ICosmosDbClientFactory>();
            var exportExpecatationEntry = new Mock<ICosmosDbClient<ExportExpectationEventEntry>>();
            var exportCompletedEntry = new Mock<ICosmosDbClient<CompletedExportEventEntry>>();

            var exportInfoProvider = new Mock<IPCFv2ExportInfoProvider>();
            exportInfoProvider.Setup(r => r.GetExpectionWorkerLastestRunTimeAsync())
                .Returns(Task.FromResult(DateTime.Now));

            cosmosDBClientFactory.Setup(r => r.GetCosmosDbClient<ExportExpectationEventEntry>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(exportExpecatationEntry.Object)
                .Verifiable();

            cosmosDBClientFactory.Setup(r => r.GetCosmosDbClient<CompletedExportEventEntry>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
               .Returns(exportCompletedEntry.Object)
               .Verifiable();

            IList<CompletedExportEventEntry> results = new List<CompletedExportEventEntry>();
            exportCompletedEntry.Setup(r => r.ReadEntriesAsync(It.IsAny<string>(), null, CancellationToken.None))
                .Returns(Task.FromResult(results))
                .Verifiable();

            exportInfoProvider.Setup(r => r.GetExpectionWorkerLastestRunTimeAsync())
                .Returns(Task.FromResult(DateTime.Now))
                .Verifiable();

            Guid agentId = Guid.NewGuid();
            Guid assetGroupId = Guid.NewGuid();
            string compoundKey = $"{agentId.ToString()}.{assetGroupId.ToString()}";

            IList<ExportExpectationEventEntry> expectations = new List<ExportExpectationEventEntry>()
            {
                new ExportExpectationEventEntry()
                {
                    FinalContainerUri = new Uri("https://someURI.com"),
                    FinalDestinationPath = "SomePath",
                    CommandId = commandId.GuidValue,
                    CompoundKey = compoundKey,
                    ExportStatus = 1
                }
            };

            exportExpecatationEntry.Setup(r => r.ReadEntriesAsync(It.IsAny<string>(), null, CancellationToken.None))
                .Returns(Task.FromResult(expectations))
                .Verifiable();

            var handler = new CheckCompletionWorkItemQueueHandler(repository.Object, null, appConfiguration.Object, cosmosDBClientFactory.Object, exportInfoProvider.Object);
            try
            {
                var result = await handler.ProcessWorkItemAsync(new QueueWorkItemWrapper<CheckCompletionWorkItem>(
                    new CheckCompletionWorkItem() { CommandId = commandId },
                    Mock.Of<IAzureCloudQueue>(),
                    null,
                    null));
            }
            catch
            {
                // do nothing its expected to throw.
            }
           
            Assert.True(commandHistory.ExportDestinations.ContainsKey((new AgentId(agentId), new AssetGroupId(assetGroupId))));
            repository.VerifyAll();

            exportInfoProvider.VerifyAll();
            cosmosDBClientFactory.VerifyAll();
            exportCompletedEntry.VerifyAll();
            exportExpecatationEntry.VerifyAll();
        }

        [Fact]
        public async void VerifyForceCompletedExportsAreNotBlockedByPCFV2Agents()
        {
            var commandId = new CommandId(Guid.NewGuid().ToString());
            var commandHistoryCore = new CommandHistoryCoreRecord(commandId);
            commandHistoryCore.CommandType = PrivacyCommandType.Export;
            commandHistoryCore.CreatedTime = DateTime.Now.Subtract(TimeSpan.FromDays(1));
            var commandHistory = new CommandHistoryRecord(commandId, commandHistoryCore, null, MakeDefaultStatusMap(), null, null); ;

            var repository = new Mock<ICommandHistoryRepository>();
            repository.Setup(r => r.QueryAsync(commandId, CommandHistoryFragmentTypes.Core | CommandHistoryFragmentTypes.Status | CommandHistoryFragmentTypes.ExportDestinations))
                .Returns(Task.FromResult(commandHistory))
                .Verifiable();

            repository.Setup(r => r.ReplaceAsync(commandHistory, CommandHistoryFragmentTypes.Core))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var appConfiguration = new Mock<IAppConfiguration>();
            appConfiguration.Setup(a => a.IsFeatureFlagEnabledAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(true);

            var cosmosDBClientFactory = new Mock<ICosmosDbClientFactory>();
            var exportExpecatationEntry = new Mock<ICosmosDbClient<ExportExpectationEventEntry>>();
            var exportCompletedEntry = new Mock<ICosmosDbClient<CompletedExportEventEntry>>();

            var exportInfoProvider = new Mock<IPCFv2ExportInfoProvider>();
            exportInfoProvider.Setup(r => r.GetExpectionWorkerLastestRunTimeAsync())
                .Returns(Task.FromResult(DateTime.Now));

            cosmosDBClientFactory.Setup(r => r.GetCosmosDbClient<ExportExpectationEventEntry>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(exportExpecatationEntry.Object)
                .Verifiable();

            cosmosDBClientFactory.Setup(r => r.GetCosmosDbClient<CompletedExportEventEntry>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
               .Returns(exportCompletedEntry.Object)
               .Verifiable();

            IList<CompletedExportEventEntry> results = new List<CompletedExportEventEntry>()
            {
                new CompletedExportEventEntry()
                {
                   Id = commandId.ToString()
                }
            };

            exportCompletedEntry.Setup(r => r.ReadEntriesAsync(It.IsAny<string>(), null, CancellationToken.None))
                .Returns(Task.FromResult(results))
                .Verifiable();

            exportInfoProvider.Setup(r => r.GetExpectionWorkerLastestRunTimeAsync())
                .Returns(Task.FromResult(DateTime.Now))
                .Verifiable();

            Guid agentId = Guid.NewGuid();
            Guid assetGroupId = Guid.NewGuid();
            string compoundKey = $"{agentId.ToString()}.{assetGroupId.ToString()}";

            IList<ExportExpectationEventEntry> expectations = new List<ExportExpectationEventEntry>()
            {
                new ExportExpectationEventEntry()
                {
                    FinalContainerUri = new Uri("https://someURI.com"),
                    FinalDestinationPath = "SomePath",
                    CommandId = commandId.GuidValue,
                    CompoundKey = compoundKey,
                    ExportStatus = 0
                }
            };

            exportExpecatationEntry.Setup(r => r.ReadEntriesAsync(It.IsAny<string>(), null, CancellationToken.None))
                .Returns(Task.FromResult(expectations))
                .Verifiable();

            var handler = new CheckCompletionWorkItemQueueHandler(repository.Object, null, appConfiguration.Object, cosmosDBClientFactory.Object, exportInfoProvider.Object);
            var result = await handler.ProcessWorkItemAsync(new QueueWorkItemWrapper<CheckCompletionWorkItem>(
                new CheckCompletionWorkItem() { CommandId = commandId },
                Mock.Of<IAzureCloudQueue>(),
                null,
                null));

            Assert.True(result.Complete);
            repository.VerifyAll();

            cosmosDBClientFactory.VerifyAll();
            exportCompletedEntry.VerifyAll();
        }

        private IDictionary<ValueTuple<AgentId, AssetGroupId>, CommandHistoryAssetGroupStatusRecord> MakeDefaultStatusMap()
        {
            var agentId = new AgentId(Guid.NewGuid());
            var assetGroupId = new AssetGroupId(Guid.NewGuid());

            var commandStatusRecord = new CommandHistoryAssetGroupStatusRecord(agentId, assetGroupId);
            commandStatusRecord.IngestionTime = DateTimeOffset.UtcNow;
            commandStatusRecord.CompletedTime = DateTimeOffset.UtcNow;

            IDictionary<ValueTuple<AgentId, AssetGroupId>, CommandHistoryAssetGroupStatusRecord> statusMap =
                new Dictionary<ValueTuple<AgentId, AssetGroupId>, CommandHistoryAssetGroupStatusRecord>()
                {
                    { new ValueTuple<AgentId, AssetGroupId>(agentId, assetGroupId), commandStatusRecord }
                };

            return statusMap;
        }
    }
}

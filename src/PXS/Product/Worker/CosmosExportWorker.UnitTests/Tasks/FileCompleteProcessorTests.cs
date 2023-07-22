// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExportWorker.UnitTests.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Data;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Tasks;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem.Cosmos;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExportWorker.UnitTests.TestUtility;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Azure.Storage.RetryPolicies;

    using Moq;

    [TestClass]
    public class FileCompleteProcessorTests
    {
        private const string DataManifestName = "DATAMANIFEST";
        private const string ReqManifestFile = "REQMANIFEST";
        private const string DataFileName = "DATAFILE";
        private const string CosmosTag = "COSMOSTAG";
        private const string AgentId = "AGENTID";
        private const string TaskId = "TASKID";
        
        private const string PathPrefix = "PATH";

        private const string DataManifestPath = 
            FileCompleteProcessorTests.PathPrefix + "/" + FileCompleteProcessorTests.DataManifestName;

        private const string DataManifestPathEscaped =
            FileCompleteProcessorTests.PathPrefix + "$2f" + FileCompleteProcessorTests.DataManifestName;


        private const string ReqManifestPath = 
            FileCompleteProcessorTests.PathPrefix + "/" + FileCompleteProcessorTests.ReqManifestFile;

        private const string DataFilePath = 
            FileCompleteProcessorTests.PathPrefix + "/" + FileCompleteProcessorTests.DataFileName;

        private class FileCompleteProcessorTestException : Exception
        {
        }

        public class TestPaths : ICosmosRelativePathsAndExpiryTimes
        {
            public string BasePath { get; } = "BASE/";
            public string AgentOutput { get; } = "OUTPUT/";
            public string PostProcessHolding { get; } = "HOLDING/";
            public string ActivityLog { get; } = "ACTIVITY/";
            public string DeadLetter { get; } = "DEADLETTER/";
            public int ActivityLogExpiryHours { get; } = 1;
            public int DeadLetterExpiryHours { get; } = 1;
            public int HoldingExpiryHours { get; } = 1;
            public string StatsLog { get; } = "STATS/";
            public int StatsLogExpiryHours { get; } = 1;
            public int ManifestHoldingExpiryHours { get; } = 1;
        }

        private class TestConfig : IFileCompleteProcessorConfig
        {
            public string Tag { get; } = FileCompleteProcessorTests.CosmosTag;
            public string TaskType { get; } = "Type";
            public int InstanceCount { get; } = 1;
            public int LeaseMinutes { get; } = 30;
            public int MinimumRenewMinutes { get; } = 5;
            public int MaxStateUpdateAttempts { get; set; } = 1;
            public int CommandReaderLeaseUpdateRowCount { get; } = 1000000;
            public int DelayOnExceptionMinutes { get; } = 0;
        }

        private readonly Mock<ITable<ManifestFileSetState>> mockManifestState = new Mock<ITable<ManifestFileSetState>>();
        private readonly Mock<IFileProgressTrackerFactory> mockTrackFactory = new Mock<IFileProgressTrackerFactory>();
        private readonly Mock<IQueueItem<CompleteDataFile>> mockQueueItem = new Mock<IQueueItem<CompleteDataFile>>();
        private readonly Mock<IQueue<CompleteDataFile>> mockQueue = new Mock<IQueue<CompleteDataFile>>();
        private readonly Mock<IFileProgressTracker> mockTrack = new Mock<IFileProgressTracker>();
        private readonly Mock<ITable<CommandState>> mockCommandState = new Mock<ITable<CommandState>>();
        private readonly Mock<IFileSystemManager> mockFileSystemMgr = new Mock<IFileSystemManager>();
        private readonly Mock<ICosmosFileSystem> mockFileSource = new Mock<ICosmosFileSystem>(MockBehavior.Strict);
        private readonly Mock<ICounterFactory> mockCounters = new Mock<ICounterFactory>();
        private readonly MockLogger mockLog = new MockLogger();
        private readonly Mock<IClock> mockClock = new Mock<IClock>();
        private readonly Mock<IFile> mockDataManifest = new Mock<IFile>();
        private readonly Mock<IFile> mockReqManifest = new Mock<IFile>();

        private readonly CompleteDataFile data = new CompleteDataFile
        {
            AgentId = FileCompleteProcessorTests.AgentId,
            DataFilePath = FileCompleteProcessorTests.DataFilePath,
            ManifestPath = FileCompleteProcessorTests.DataManifestPath,
            CosmosTag = FileCompleteProcessorTests.CosmosTag,
        };

        private readonly MemoryStream reqManifestContents = new MemoryStream();
        private readonly TestConfig config = new TestConfig();
        private FileCompleteProcessor testObj;

        [TestInitialize]
        public void Init()
        {
            this.mockQueue
                .Setup(o => o.DequeueAsync(
                    It.IsAny<TimeSpan>(), 
                    It.IsAny<TimeSpan>(), 
                    It.IsAny<IRetryPolicy>(), 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockQueueItem.Object);

            this.mockTrackFactory
                .Setup(o => o.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Utility.TraceLoggerAction>()))
                .Returns(this.mockTrack.Object);

            this.mockQueueItem.SetupGet(o => o.Data).Returns(() => this.data);

            this.mockFileSystemMgr.SetupGet(o => o.CosmosPathsAndExpiryTimes).Returns(new TestPaths());
            this.mockFileSystemMgr.Setup(o => o.GetFileSystem(It.IsAny<string>())).Returns(this.mockFileSource.Object);

            this.mockFileSource
                .Setup(o => o.OpenExistingFileAsync(FileCompleteProcessorTests.ReqManifestPath))
                .ReturnsAsync(this.mockReqManifest.Object);

            this.mockFileSource
                .Setup(o => o.OpenExistingFileAsync(FileCompleteProcessorTests.DataManifestPath))
                .ReturnsAsync(this.mockDataManifest.Object);

            this.mockReqManifest.Setup(o => o.GetDataReader()).Returns(() => this.reqManifestContents);

            this.mockManifestState
                .Setup(o => o.ReplaceAsync(It.IsAny<ManifestFileSetState>()))
                .ReturnsAsync(true);

            this.testObj = new FileCompleteProcessor(
                this.config,
                this.mockManifestState.Object,
                this.mockTrackFactory.Object,
                this.mockQueue.Object,
                this.mockCommandState.Object,
                this.mockFileSystemMgr.Object,
                this.mockCounters.Object,
                this.mockLog.Object,
                this.mockClock.Object);
        }

        [TestMethod]
        public async Task ProcessorReturnsImmediatelyIfDequeueReturnsNull()
        {
            this.mockQueue
                .Setup(
                    o => o.DequeueAsync(
                        It.IsAny<TimeSpan>(), 
                        It.IsAny<TimeSpan>(), 
                        It.IsAny<IRetryPolicy>(), 
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync((IQueueItem<CompleteDataFile>)null);

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(FileCompleteProcessorTests.TaskId, "context");

            // verify
            this.mockQueue.Verify(
                o => o.DequeueAsync(
                    TimeSpan.FromMinutes(this.config.LeaseMinutes), 
                    It.IsAny<TimeSpan>(), 
                    It.IsAny<IRetryPolicy>(), 
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.mockManifestState.Verify(o => o.GetItemAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task ProcessorReturnsImmediatelyIfStateObjectCannotBeFound()
        {
            this.mockManifestState
                .Setup(o => o.GetItemAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((ManifestFileSetState)null);

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(FileCompleteProcessorTests.TaskId, "context");

            // verify
            this.mockQueue.Verify(
                o => o.DequeueAsync(
                    TimeSpan.FromMinutes(this.config.LeaseMinutes),
                    It.IsAny<TimeSpan>(), 
                    It.IsAny<IRetryPolicy>(), 
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.mockManifestState.Verify(
                o => o.GetItemAsync(this.data.AgentId, FileCompleteProcessorTests.DataManifestPathEscaped), 
                Times.Once);

            this.mockFileSource.Verify(o => o.OpenExistingFileAsync(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task ProcessorReturnsImmediatelyIfStateObjectHasANonEmptyDataFilesList()
        {
            ManifestFileSetState source = new ManifestFileSetState
            {
                AgentId = this.data.AgentId,
                ManifestPath = this.data.ManifestPath,

                RequestManifestPath = FileCompleteProcessorTests.ReqManifestPath,
                DataFileTags = new[] { FileCompleteProcessorTests.DataFilePath + "2" }
            };

            this.mockManifestState
                .Setup(o => o.GetItemAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(source);

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(FileCompleteProcessorTests.TaskId, "context");

            // verify
            this.mockQueue.Verify(
                o => o.DequeueAsync(
                    TimeSpan.FromMinutes(this.config.LeaseMinutes), 
                    It.IsAny<TimeSpan>(), 
                    It.IsAny<IRetryPolicy>(), 
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.mockManifestState.Verify(
                o => o.GetItemAsync(this.data.AgentId, FileCompleteProcessorTests.DataManifestPathEscaped),
                Times.Once);

            this.mockFileSource.Verify(o => o.OpenExistingFileAsync(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task ProcessorRemovesDataFileFromManifestStateWhenItExistsAndUpdatesState()
        {
            const string DataTagPrefix =
                FileCompleteProcessorTests.CosmosTag + "." + FileCompleteProcessorTests.AgentId;

            const string DataTag1 = DataTagPrefix + "." + FileCompleteProcessorTests.DataFileName;
            const string DataTag2 = DataTag1 + "2";

            ManifestFileSetState source = new ManifestFileSetState
            {
                AgentId = this.data.AgentId,
                ManifestPath = this.data.ManifestPath,

                RequestManifestPath = FileCompleteProcessorTests.ReqManifestPath,
                DataFileTags = new[] { DataTag1, DataTag2 }
            };

            Func<ManifestFileSetState, bool> verifier =
                o =>
                {
                    Assert.AreEqual(source.AgentId, o.AgentId);
                    Assert.AreEqual(source.ManifestPath, o.ManifestPath);
                    Assert.AreEqual(source.RequestManifestPath, o.RequestManifestPath);
                    Assert.AreEqual(1, o.DataFileTags.Count);
                    Assert.AreEqual(1, o.DataFileTags.Count(f => DataTag2.Equals(f)));
                    return true;
                };

            this.mockFileSource.Reset();

            this.mockManifestState
                .Setup(o => o.GetItemAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(source);

            this.mockManifestState
                .Setup(o => o.ReplaceAsync(It.IsAny<ManifestFileSetState>()))
                .ReturnsAsync(true);
            
            // test
            await this.testObj.RunSingleInstanceOnePassAsync(FileCompleteProcessorTests.TaskId, "context");

            // verify
            this.mockManifestState.Verify(
                o => o.GetItemAsync(this.data.AgentId, FileCompleteProcessorTests.DataManifestPathEscaped), Times.Once);

            this.mockManifestState.Verify(o => o.ReplaceAsync(It.Is<ManifestFileSetState>(p => verifier(p))));
        }

        [TestMethod]
        public async Task ProcessorFetchesCommandIdsMarksThemAsCompleteAndMovesRequestManifestIfNoDataFilesRemain()
        {
            const string CommandId = "commandid";

            string expectedPath =
                this.mockFileSystemMgr.Object.CosmosPathsAndExpiryTimes.PostProcessHolding +
                Utility.EnsureTrailingSlash(this.data.AgentId);

            ManifestFileSetState source = new ManifestFileSetState
            {
                AgentId = this.data.AgentId,
                ManifestPath = this.data.ManifestPath,

                RequestManifestPath = FileCompleteProcessorTests.ReqManifestPath,
                DataFileTags = new string[0],
            };

            Func<CommandState, bool> verifier =
                o =>
                {
                    Assert.AreEqual(FileCompleteProcessorTests.AgentId, o.AgentId);
                    Assert.AreEqual(CommandId, o.RowKey);
                    Assert.IsTrue(o.IsComplete);
                    return true;
                };

            this.mockFileSource
                .Setup(o => o.OpenExistingFileAsync(FileCompleteProcessorTests.DataManifestPath))
                .ReturnsAsync((IFile)null);

            this.mockCommandState
                .Setup(o => o.QueryAsync(It.IsAny<string>()))
                .ReturnsAsync(
                    new[] { new CommandState { IsComplete = false, AgentId = this.data.AgentId, CommandId = CommandId } });

            this.mockCommandState
                .Setup(o => o.ReplaceAsync(It.IsAny<CommandState>()))
                .ReturnsAsync(true);

            this.mockManifestState
                .Setup(o => o.GetItemAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(source);

            TestUtilities.PopulateStreamWithString(CommandId + "\n", this.reqManifestContents);

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(FileCompleteProcessorTests.TaskId, "context");

            // verify
            this.mockCommandState.Verify(o => o.ReplaceAsync(It.Is<CommandState>(p => verifier(p))), Times.Once);
            this.mockFileSource.Verify(o => o.OpenExistingFileAsync(FileCompleteProcessorTests.ReqManifestPath), Times.Once);

            this.mockCommandState.Verify(
                o => o.QueryAsync($"PartitionKey eq '{FileCompleteProcessorTests.AgentId}' and (RowKey eq '{CommandId}')"),
                Times.Once);

            this.mockReqManifest.Verify(o => o.GetDataReader(), Times.Once);
            this.mockReqManifest.Verify(o => o.MoveRelativeAsync(expectedPath, true, true), Times.Once);
            this.mockReqManifest.Verify(
                o => o.SetLifetimeAsync(
                    TimeSpan.FromHours(this.mockFileSystemMgr.Object.CosmosPathsAndExpiryTimes.HoldingExpiryHours), true),
                Times.Once);
        }

        [TestMethod]
        public async Task ProcessorFetchesCommandIdsMarksThemAsCompleteAndRetriesIfFirstAttemptFails()
        {
            const string CommandId = "commandid";

            string expectedPath =
                this.mockFileSystemMgr.Object.CosmosPathsAndExpiryTimes.PostProcessHolding +
                Utility.EnsureTrailingSlash(this.data.AgentId);

            ManifestFileSetState source = new ManifestFileSetState
            {
                AgentId = this.data.AgentId,
                ManifestPath = this.data.ManifestPath,

                RequestManifestPath = FileCompleteProcessorTests.ReqManifestPath,
                DataFileTags = new string[0],
            };

            Func<CommandState, bool> verifier =
                o =>
                {
                    Assert.AreEqual(FileCompleteProcessorTests.AgentId, o.AgentId);
                    Assert.AreEqual(CommandId, o.RowKey);
                    Assert.IsTrue(o.IsComplete);
                    return true;
                };

            this.mockFileSource
                .Setup(o => o.OpenExistingFileAsync(FileCompleteProcessorTests.DataManifestPath))
                .ReturnsAsync((IFile)null);

            this.mockCommandState
                .Setup(o => o.QueryAsync(It.IsAny<string>()))
                .Returns(
                    (string o) =>
                        Task.FromResult(
                            new[]
                            {
                                new CommandState { IsComplete = false, AgentId = this.data.AgentId, CommandId = CommandId }
                            } as ICollection<CommandState>));

            this.mockCommandState
                .SetupSequence(o => o.ReplaceAsync(It.IsAny<CommandState>()))
                .ReturnsAsync(false)
                .ReturnsAsync(true);

            this.config.MaxStateUpdateAttempts = 2;

            this.mockManifestState
                .Setup(o => o.GetItemAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(source);

            TestUtilities.PopulateStreamWithString(CommandId + "\n", this.reqManifestContents);

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(FileCompleteProcessorTests.TaskId, "context");

            // verify
            this.mockCommandState.Verify(o => o.ReplaceAsync(It.Is<CommandState>(p => verifier(p))), Times.Exactly(2));
            this.mockFileSource.Verify(o => o.OpenExistingFileAsync(FileCompleteProcessorTests.ReqManifestPath), Times.Once);

            this.mockCommandState.Verify(
                o => o.QueryAsync($"PartitionKey eq '{FileCompleteProcessorTests.AgentId}' and (RowKey eq '{CommandId}')"),
                Times.Exactly(2));

            this.mockReqManifest.Verify(o => o.GetDataReader(), Times.Once);
            this.mockReqManifest.Verify(o => o.MoveRelativeAsync(expectedPath, true, true), Times.Once);
            this.mockReqManifest.Verify(
                o => o.SetLifetimeAsync(
                    TimeSpan.FromHours(this.mockFileSystemMgr.Object.CosmosPathsAndExpiryTimes.HoldingExpiryHours), true),
                Times.Once);
        }

        [TestMethod]
        public async Task ProcessorMovesDataManifestFileIfNoDataFilesRemain()
        {
            const string CommandId = "commandid";

            string expectedPath =
                this.mockFileSystemMgr.Object.CosmosPathsAndExpiryTimes.PostProcessHolding +
                Utility.EnsureTrailingSlash(this.data.AgentId);

            ManifestFileSetState source = new ManifestFileSetState
            {
                AgentId = this.data.AgentId,
                ManifestPath = this.data.ManifestPath,

                RequestManifestPath = FileCompleteProcessorTests.ReqManifestPath,
                DataFileTags = new string[0],
            };

            this.mockFileSource
                .Setup(o => o.OpenExistingFileAsync(FileCompleteProcessorTests.ReqManifestPath))
                .ReturnsAsync((IFile)null);

            this.mockCommandState
                .Setup(o => o.QueryAsync(It.IsAny<string>()))
                .ReturnsAsync(
                    new[] { new CommandState { IsComplete = false, AgentId = this.data.AgentId, CommandId = CommandId } });

            this.mockManifestState
                .Setup(o => o.GetItemAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(source);

            TestUtilities.PopulateStreamWithString(CommandId + "\n", this.reqManifestContents);

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(FileCompleteProcessorTests.TaskId, "context");

            // verify
            this.mockFileSource.Verify(o => o.OpenExistingFileAsync(FileCompleteProcessorTests.DataManifestPath), Times.Once);
            this.mockDataManifest.Verify(o => o.MoveRelativeAsync(expectedPath, true, true), Times.Once);
            this.mockDataManifest.Verify(
                o => o.SetLifetimeAsync(
                    TimeSpan.FromHours(this.mockFileSystemMgr.Object.CosmosPathsAndExpiryTimes.HoldingExpiryHours), true),
                Times.Once);
        }

        [TestMethod]
        public async Task ProcessorDeletesTheManifestStateIfNoItemsRemain()
        {
            const string CommandId = "COMMANDID";

            ManifestFileSetState source = new ManifestFileSetState
            {
                AgentId = this.data.AgentId,
                ManifestPath = this.data.ManifestPath,

                RequestManifestPath = FileCompleteProcessorTests.ReqManifestPath,
                DataFileTags = new string[0],
            };

            Func<ManifestFileSetState, bool> verifier =
                o =>
                {
                    Assert.AreEqual(source.AgentId, o.AgentId);
                    Assert.AreEqual(source.ManifestPath, o.ManifestPath);
                    return true;
                };

            this.mockFileSource
                .Setup(o => o.OpenExistingFileAsync(FileCompleteProcessorTests.ReqManifestPath))
                .ReturnsAsync((IFile)null);

            this.mockFileSource
                .Setup(o => o.OpenExistingFileAsync(FileCompleteProcessorTests.DataManifestPath))
                .ReturnsAsync((IFile)null);

            this.mockCommandState
                .Setup(o => o.QueryAsync(It.IsAny<string>()))
                .ReturnsAsync(
                    new[] { new CommandState { IsComplete = false, AgentId = this.data.AgentId, CommandId = CommandId } });

            this.mockManifestState
                .Setup(o => o.GetItemAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(source);

            TestUtilities.PopulateStreamWithString(CommandId + "\n", this.reqManifestContents);

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(FileCompleteProcessorTests.TaskId, "context");

            // verify
            this.mockManifestState.Verify(o => o.DeleteItemAsync(It.Is<ManifestFileSetState>(p => verifier(p))), Times.Once);
        }
    }
}
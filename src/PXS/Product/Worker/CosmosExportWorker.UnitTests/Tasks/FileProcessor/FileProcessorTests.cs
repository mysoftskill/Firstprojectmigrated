// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExportWorker.UnitTests.Tasks.FileProcessor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Data;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Tasks;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem.Cosmos;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Locks;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExportWorker.UnitTests.TestUtility;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Azure.Storage.RetryPolicies;

    using Moq;
    using Microsoft.Azure.ComplianceServices.Common;

    [TestClass]
    public class FileProcessorTests
    {
        private const string CosmosTag = "CTAG";
        private const string AgentId = "AGENTID";
        private const string TaskId = "TASKID";

        private class FileProcessorTestException : Exception {  }

        private class ThresholdConfig : ICosmosFileSizeThresholds
        {
            public long Oversized { get; set; } = 5000;
            public long Medium { get; set; } = 1000;
            public long Large { get; set; } = 2000;
        }

        public class TestPaths : ICosmosRelativePathsAndExpiryTimes
        {
            public string BasePath { get; } = "BASE/";
            public string AgentOutput { get; } = "OUTPUT/";
            public string PostProcessHolding { get; } = "HOLDING/";
            public string ActivityLog { get; } = "ACTIVITY/";
            public string DeadLetter { get; } = "DEADLETTER/";
            public int ActivityLogExpiryHours { get; } = 2;
            public int DeadLetterExpiryHours { get; } = 1;
            public int HoldingExpiryHours { get; } = 1;
            public string StatsLog { get; } = "STATS/";
            public int StatsLogExpiryHours { get; } = 1;
            public int ManifestHoldingExpiryHours { get; } = 1;
        }

        private class TestConfig : IFileProcessorConfig
        {
            public string Tag { get; } = "Tag";
            public string TaskType { get; } = "Type";
            public int InstanceCount { get; } = 3;
            public int LeaseMinutes { get; } = 30;
            public int MinimumRenewMinutes { get; } = 5;
            public int CommandPendingByteThreshold { get; } = 1024;
            public int OverallPendingByteThreshold { get; set; } = 2048;
            public int DelayIfCouldNotCompleteMinutes { get; } = 10;
            public int DelayOnExceptionMinutes { get; } = 0;
            public int MaxDequeueCount { get; } = 5;
            public int ProgressUpdateSeconds { get; } = 1;
            public int OversizedFileInstances { get; } = 1;
            public string AssumeNonTransientThreshold { get; } = "3.00:00:00";
            public int LargeFileInstances { get; } = 1;
            public int MediumFileInstances { get; } = 1;
            public int EmptyFileInstances { get; set; } = 0;
        }

        private readonly Mock<IPartitionedQueue<PendingDataFile, FileSizePartition>> mockPendingQueue =
            new Mock<IPartitionedQueue<PendingDataFile, FileSizePartition>>();

        private readonly Mock<IFileProgressTrackerFactory> mockTrackFactory = new Mock<IFileProgressTrackerFactory>();
        private readonly Mock<IQueueItem<PendingDataFile>> mockQueueItem = new Mock<IQueueItem<PendingDataFile>>();
        private readonly Mock<IPeriodicFileWriterFactory> mockPeriodicFactory = new Mock<IPeriodicFileWriterFactory>();
        private readonly Mock<ICommandDataWriterFactory> mockWriterFactory = new Mock<ICommandDataWriterFactory>();
        private readonly Mock<IQueue<CompleteDataFile>> mockDoneQueue = new Mock<IQueue<CompleteDataFile>>();
        private readonly Mock<ITable<CommandFileState>> mockCommandFileState = new Mock<ITable<CommandFileState>>();
        private readonly Mock<IFileProgressTracker> mockTrack = new Mock<IFileProgressTracker>();
        private readonly Mock<IPeriodicFileWriter> mockPeriodic = new Mock<IPeriodicFileWriter>();
        private readonly Mock<IFileSystemManager> mockFileSystemMgr = new Mock<IFileSystemManager>();
        private readonly Mock<ICommandDataWriter> mockWriter = new Mock<ICommandDataWriter>();
        private readonly Mock<ICosmosFileSystem> mockFileSource = new Mock<ICosmosFileSystem>();
        private readonly Mock<ITelemetryLogger> mockTelemetry = new Mock<ITelemetryLogger>();
        private readonly Mock<ICounterFactory> mockCounterFactory = new Mock<ICounterFactory>();
        private readonly Mock<ILockManager> mockLockManager = new Mock<ILockManager>();
        private readonly Mock<ILockLease> mockLease = new Mock<ILockLease>();
        private readonly Mock<ICounter> mockCounter = new Mock<ICounter>();
        private readonly Mock<IClock> mockClock = new Mock<IClock>();
        private readonly Mock<IFile> mockFile = new Mock<IFile>();
        private readonly MockLogger mockLog = new MockLogger();

        private readonly PendingDataFile data = new PendingDataFile { AgentId = FileProcessorTests.AgentId };
        private readonly MemoryStream fileContents = new MemoryStream();
        private readonly TestConfig config = new TestConfig();
        private readonly IAppConfiguration appConfig = new AppConfiguration("local.settings.json");

        private FileProcessor testObj;

        [TestInitialize]
        public void Init()
        {
            this.mockClock.SetupGet(o => o.UtcNow).Returns(new DateTimeOffset(2006, 04, 15, 15, 00, 00, TimeSpan.Zero));

            this.mockWriterFactory.Setup(
                o => o.CreateAsync(
                    It.IsAny<CancellationToken>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>())).ReturnsAsync(this.mockWriter.Object);

            this.mockTrackFactory
                .Setup(o => o.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Utility.TraceLoggerAction>()))
                .Returns(this.mockTrack.Object);

            this.mockCounterFactory
                .Setup(o => o.GetCounter(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CounterType>()))
                .Returns(this.mockCounter.Object);

            this.mockPendingQueue
                .Setup(
                    o => o.DequeueAsync(
                        It.IsAny<IReadOnlyList<FileSizePartition>>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<IRetryPolicy>(),
                        It.IsAny<CancellationToken>()))
                .Returns(
                    (IReadOnlyList<FileSizePartition> filter, TimeSpan x1, TimeSpan x2, IRetryPolicy x3, CancellationToken x4) =>
                    {
                        FileSizePartition value = filter?.FirstOrDefault() ?? FileSizePartition.Small;
                        if (value == FileSizePartition.Invalid)
                        {
                            value = FileSizePartition.Small;
                        }

                        return Task.FromResult(
                            new PartitionedQueueItem<PendingDataFile, FileSizePartition>(this.mockQueueItem.Object, value));
                    });

            this.mockPeriodicFactory
                .Setup(
                    o => o.Create(
                        It.IsAny<ICosmosFileSystem>(),
                        It.IsAny<string>(),
                        It.IsAny<Func<DateTimeOffset, string>>(),
                        It.IsAny<TimeSpan>()))
                .Returns(this.mockPeriodic.Object);

            this.mockLockManager
                .Setup(
                    o => o.AttemptAcquireAsync(
                        It.IsAny<string>(), 
                        It.IsAny<string>(), 
                        It.IsAny<string>(), 
                        It.IsAny<TimeSpan>(), 
                        It.IsAny<bool>()))
                .ReturnsAsync(this.mockLease.Object);

            this.mockFileSystemMgr.SetupGet(o => o.CosmosPathsAndExpiryTimes).Returns(new TestPaths());
            this.mockFileSystemMgr.SetupGet(o => o.FileSizeThresholds).Returns(new ThresholdConfig());
            this.mockFileSystemMgr.Setup(o => o.GetFileSystem(It.IsAny<string>())).Returns(this.mockFileSource.Object);

            this.mockFileSource.Setup(o => o.OpenExistingFileAsync(It.IsAny<string>())).ReturnsAsync(this.mockFile.Object);

            this.mockQueueItem.SetupGet(o => o.Data).Returns(() => this.data);
            this.mockQueueItem.SetupGet(o => o.DequeueCount).Returns(() => 0);

            this.mockWriter.Setup(o => o.CloseAsync()).Returns(Task.CompletedTask);
            this.mockWriter
                .Setup(o => o.WriteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(0);

            this.mockFile.Setup(o => o.GetDataReader()).Returns(() => this.fileContents);

            this.testObj = new FileProcessor(
                this.mockTrackFactory.Object,
                this.mockPeriodicFactory.Object,
                this.mockWriterFactory.Object,
                this.mockDoneQueue.Object,
                this.mockCommandFileState.Object,
                this.mockPendingQueue.Object,
                this.config,
                this.mockFileSystemMgr.Object,
                this.mockTelemetry.Object,
                this.mockCounterFactory.Object,
                this.mockLockManager.Object,
                this.mockLog.Object,
                this.mockClock.Object,
                this.appConfig);
        }

        [TestMethod]
        public async Task ProcessorReturnsImmediatelyWithoutAcquiringALockIfDequeueReturnsNull()
        {
            this.mockPendingQueue
                .Setup(
                    o => o.DequeueAsync(
                        It.IsAny<IReadOnlyList<FileSizePartition>>(),
                        It.IsAny<TimeSpan>(), 
                        It.IsAny<TimeSpan>(), 
                        It.IsAny<IRetryPolicy>(), 
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync((PartitionedQueueItem<PendingDataFile, FileSizePartition>)null);

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(FileProcessorTests.TaskId, "context");

            // verify
            this.mockPendingQueue.Verify(
                o => o.DequeueAsync(
                    It.IsAny<IReadOnlyList<FileSizePartition>>(),
                    TimeSpan.FromMinutes(this.config.LeaseMinutes), 
                    It.IsAny<TimeSpan>(), 
                    It.IsAny<IRetryPolicy>(), 
                    It.IsAny<CancellationToken>()), 
                Times.Once);

            this.mockLockManager.Verify(
                o => o.AttemptAcquireAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()),
                Times.Never);
        }

        [TestMethod]
        public async Task ProcessorReturnsImmediatelyAcquiringLockIfDequeueCountTooHigh()
        {
            this.mockQueueItem.SetupGet(o => o.DequeueCount).Returns(() => this.config.MaxDequeueCount + 1);

            this.data.CosmosTag = FileProcessorTests.CosmosTag;
            this.data.DataFilePath = "PATH";

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(FileProcessorTests.TaskId, "context");

            // verify
            this.mockPendingQueue.Verify(
                o => o.DequeueAsync(
                    It.IsAny<IReadOnlyList<FileSizePartition>>(),
                    TimeSpan.FromMinutes(this.config.LeaseMinutes), 
                    It.IsAny<TimeSpan>(), 
                    It.IsAny<IRetryPolicy>(), 
                    It.IsAny<CancellationToken>()), 
                Times.Once);

            this.mockLockManager.Verify(
                o => o.AttemptAcquireAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<bool>()),
                Times.Never);
        }

        [TestMethod]
        public async Task ProcessorReturnsImmediatelyWithoutOpeningFileIfLockCantBeAcquired()
        {
            this.mockLockManager
                .Setup(
                    o => o.AttemptAcquireAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<bool>()))
                .ReturnsAsync((ILockLease)null);

            this.data.CosmosTag = FileProcessorTests.CosmosTag;
            this.data.DataFilePath = "PATH";

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(FileProcessorTests.TaskId, "context");

            // verify
            this.mockPendingQueue.Verify(
                o => o.DequeueAsync(
                    It.IsAny<IReadOnlyList<FileSizePartition>>(),
                    TimeSpan.FromMinutes(this.config.LeaseMinutes), 
                    It.IsAny<TimeSpan>(), 
                    It.IsAny<IRetryPolicy>(), 
                    It.IsAny<CancellationToken>()), Times.Once);

            this.mockLockManager.Verify(
                o => o.AttemptAcquireAsync(
                    FileProcessorTests.AgentId, 
                    this.data.DataFilePath, 
                    FileProcessorTests.TaskId, 
                    TimeSpan.FromMinutes(this.config.LeaseMinutes),
                    false),
                Times.Once);

            this.mockFileSource.Verify(o => o.OpenExistingFileAsync(It.IsAny<string>()), Times.Never);
            this.mockQueueItem.Verify(
                o => o.RenewLeaseAsync(TimeSpan.FromMinutes(this.config.DelayIfCouldNotCompleteMinutes)),
                Times.Once);
        }

        [TestMethod]
        public async Task ProcessorAttemptsToOpensFileWithFixedUpNameIfFilePathContainsLowercaseReplacementVariables()
        {
            const string ExpectedOpenPath = "DPATH_2018_01";

            this.data.CosmosTag = FileProcessorTests.CosmosTag;
            this.data.ManifestPath = "MPATH_2018_01_01.txt";
            this.data.DataFilePath = "DPATH_%y_01";

            this.mockFileSource.Setup(o => o.OpenExistingFileAsync(It.IsAny<string>())).ReturnsAsync((IFile)null);

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(FileProcessorTests.TaskId, "context");

            // verify
            this.mockPendingQueue.Verify(
                o => o.DequeueAsync(
                    It.IsAny<IReadOnlyList<FileSizePartition>>(),
                    TimeSpan.FromMinutes(this.config.LeaseMinutes),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<IRetryPolicy>(),
                    It.IsAny<CancellationToken>()), Times.Once);

            this.mockLockManager.Verify(
                o => o.AttemptAcquireAsync(
                    FileProcessorTests.AgentId,
                    this.data.DataFilePath,
                    FileProcessorTests.TaskId,
                    TimeSpan.FromMinutes(this.config.LeaseMinutes),
                    false),
                Times.Once);

            this.mockFileSource.Verify(o => o.OpenExistingFileAsync(ExpectedOpenPath), Times.Once);
            this.mockQueueItem.Verify(o => o.CompleteAsync(), Times.Once);
            this.mockLease.Verify(o => o.ReleaseAsync(true));
        }

        [TestMethod]
        public async Task ProcessorAttemptsToOpensFileIfLockObtained()
        {
            this.data.CosmosTag = FileProcessorTests.CosmosTag;
            this.data.DataFilePath = "PATH";

            this.mockFileSource.Setup(o => o.OpenExistingFileAsync(It.IsAny<string>())).ReturnsAsync((IFile)null);

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(FileProcessorTests.TaskId, "context");

            // verify
            this.mockPendingQueue.Verify(
                o => o.DequeueAsync(
                    It.IsAny<IReadOnlyList<FileSizePartition>>(),
                    TimeSpan.FromMinutes(this.config.LeaseMinutes),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<IRetryPolicy>(),
                    It.IsAny<CancellationToken>()), Times.Once);

            this.mockLockManager.Verify(
                o => o.AttemptAcquireAsync(
                    FileProcessorTests.AgentId,
                    this.data.DataFilePath,
                    FileProcessorTests.TaskId,
                    TimeSpan.FromMinutes(this.config.LeaseMinutes),
                    false),
                Times.Once);

            this.mockFileSource.Verify(o => o.OpenExistingFileAsync(this.data.DataFilePath), Times.Once);
            this.mockQueueItem.Verify(o => o.CompleteAsync(), Times.Once);
            this.mockLease.Verify(o => o.ReleaseAsync(true));
        }

        [TestMethod]
        public async Task ProcessorReleasesLeaseInNonPurgeModeIfFileOpThrows()
        {
            this.data.CosmosTag = FileProcessorTests.CosmosTag;
            this.data.DataFilePath = "PATH";

            this.mockFileSource
                .Setup(o => o.OpenExistingFileAsync(It.IsAny<string>()))
                .Returns(Task.FromException<IFile>(new FileProcessorTestException()));

            // test
            try
            {
                await this.testObj.RunSingleInstanceOnePassAsync(FileProcessorTests.TaskId, "context");
            }
            catch (FileProcessorTestException)
            {
            }

            // verify
            this.mockQueueItem.Verify(
                o => o.RenewLeaseAsync(TimeSpan.FromMinutes(this.config.DelayIfCouldNotCompleteMinutes)), 
                Times.Once);
            this.mockLease.Verify(o => o.ReleaseAsync(false));
        }

        [TestMethod]
        public async Task ProcessorProcessesFileBySendingToWriterAndClosingWriterWhenComplete()
        {
            const string CommandId = "cid";
            const string ProductId = "PID";
            const string RowData = "DATA";
            const int RowCount = 502;
            const int Size = 20060415;

            string rowData = $"{CommandId}\t{ProductId}\t{RowData}\n";

            Func<ICollection<CommandFileState>, bool> verifier = 
                c =>
                {
                    CommandFileState item;

                    Assert.AreEqual(1, c.Count);
                    
                    item = c.First();
                    Assert.AreEqual((CommandId + "&" + this.data.DataFilePath), item.DataFilePathAndCommand);
                    Assert.AreEqual(this.data.AgentId, item.AgentId);
                    Assert.AreEqual(CommandId, item.CommandId);
                    Assert.AreEqual(RowCount, item.RowCount);
                    Assert.AreEqual(Size, item.ByteCount);
                    return true;
                };

            TestUtilities.PopulateStreamWithString(rowData, this.fileContents);

            this.data.ExportFileName = "FILE";
            this.data.ManifestPath = "PATH/MANIFEST.txt";
            this.data.DataFilePath = "PATH";
            this.data.CosmosTag = FileProcessorTests.CosmosTag;

            this.mockWriter.SetupGet(o => o.LogForCommandFeed).Returns(true);
            this.mockWriter.SetupGet(o => o.CommandId).Returns(CommandId);
            this.mockWriter.SetupGet(o => o.RowCount).Returns(RowCount);
            this.mockWriter.SetupGet(o => o.Size).Returns(Size);

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(FileProcessorTests.TaskId, "context");

            // verify
            this.mockWriterFactory.Verify(
                o => o.CreateAsync(
                    It.IsAny<CancellationToken>(), FileProcessorTests.AgentId, CommandId, this.data.ExportFileName),
                Times.Once);

            this.mockWriter
                .Verify(
                    o => o.WriteAsync(ProductId, RowData, this.config.CommandPendingByteThreshold), 
                    Times.Once);

            this.mockWriter.Verify(o => o.CloseAsync(), Times.Once);

            this.mockCommandFileState.Verify(
                o => o.InsertBatchAsync(It.Is<ICollection<CommandFileState>>(p => verifier(p))), Times.Once);
        }

        [TestMethod]
        [DataRow(TransientFailureMode.AssumeNonTransient)]
        [DataRow(TransientFailureMode.AssumeTransient)]
        public async Task ProcessorSetsWriterModeAppropriately(TransientFailureMode mode)
        {
            const string CommandId = "cid";
            const string ProductId = "PID";
            const string RowData = "DATA";
            const int RowCount = 502;
            const int Size = 20060415;

            string rowData = $"{CommandId}\t{ProductId}\t{RowData}\n";

            TestUtilities.PopulateStreamWithString(rowData, this.fileContents);

            this.data.ExportFileName = "FILE";
            this.data.ManifestPath = "PATH/MANIFEST.txt";
            this.data.DataFilePath = "PATH";
            this.data.CosmosTag = FileProcessorTests.CosmosTag;

            this.mockWriter.SetupGet(o => o.LogForCommandFeed).Returns(true);
            this.mockWriter.SetupGet(o => o.CommandId).Returns(CommandId);
            this.mockWriter.SetupGet(o => o.RowCount).Returns(RowCount);
            this.mockWriter.SetupGet(o => o.Size).Returns(Size);
            
            this.mockFile
                .SetupGet(o => o.Created)
                .Returns(
                    mode == TransientFailureMode.AssumeTransient ?
                        this.mockClock.Object.UtcNow :
                        this.mockClock.Object.UtcNow.AddTicks(
                            -1 * TimeSpan.Parse(this.config.AssumeNonTransientThreshold).Ticks));
            // test
            await this.testObj.RunSingleInstanceOnePassAsync(FileProcessorTests.TaskId, "context");

            // verify
            this.mockWriter.VerifySet(o => o.TransientFailureMode = mode, Times.Once);
        }

        [TestMethod]
        public async Task ProcessorFlushesFileIfAccumulatedPendingDataIsGreaterThanThreshold()
        {
            const string CommandId = "cid";
            const string ProductId = "PID";
            const string RowData = "DATA";
            const int RowCount = 502;
            const int Size = 20060415;

            int pending = this.config.OverallPendingByteThreshold + 1;

            string rowData = $"{CommandId}\t{ProductId}\t{RowData}\n";

            TestUtilities.PopulateStreamWithString(rowData, this.fileContents);

            this.data.ExportFileName = "FILE";
            this.data.ManifestPath = "PATH/MANIFEST.txt";
            this.data.DataFilePath = "PATH";
            this.data.CosmosTag = FileProcessorTests.CosmosTag;

            this.mockWriter.SetupGet(o => o.PendingSize).Returns(pending);
            this.mockWriter.SetupGet(o => o.CommandId).Returns(CommandId);
            this.mockWriter.SetupGet(o => o.RowCount).Returns(RowCount);
            this.mockWriter.SetupGet(o => o.Size).Returns(Size);

            this.mockWriter.Setup(o => o.FlushAsync()).ReturnsAsync(-1 * pending);
            this.mockWriter
                .Setup(o => o.WriteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(pending);

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(FileProcessorTests.TaskId, "context");

            // verify
            this.mockWriter
                .Verify(
                    o => o.WriteAsync(ProductId, RowData, this.config.CommandPendingByteThreshold),
                    Times.Once);

            this.mockWriter.Verify(o => o.FlushAsync(), Times.Once);
            this.mockWriter.Verify(o => o.CloseAsync(), Times.Once);
        }

        [TestMethod]
        public async Task ProcessorEnqueuesFileCompletionItemAfterProcessing()
        {
            Func<CompleteDataFile, bool> verifier =
                o =>
                {
                    Assert.AreEqual(this.data.DataFilePath, o.DataFilePath);
                    Assert.AreEqual(this.data.ManifestPath, o.ManifestPath);
                    Assert.AreEqual(this.data.AgentId, o.AgentId);
                    Assert.AreEqual(this.data.CosmosTag, o.CosmosTag);
                    return true;
                };

            this.data.ExportFileName = "FILE";
            this.data.DataFilePath = "PATH";
            this.data.ManifestPath = "MANIFEST";
            this.data.CosmosTag = FileProcessorTests.CosmosTag;
            this.data.AgentId = FileProcessorTests.AgentId;

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(FileProcessorTests.TaskId, "context");

            // verify
            this.mockDoneQueue.Verify(
                o => o.EnqueueAsync(It.Is<CompleteDataFile> (p => verifier(p)), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task ProcessorMovesFileToHoldingAndSetsExpiryAfterProcessing()
        {
            string expectedPath =
                this.mockFileSystemMgr.Object.CosmosPathsAndExpiryTimes.PostProcessHolding +
                Utility.EnsureTrailingSlash(FileProcessorTests.AgentId);

            this.data.ExportFileName = "FILE";
            this.data.DataFilePath = "PATH";
            this.data.ManifestPath = "MANIFEST";
            this.data.CosmosTag = FileProcessorTests.CosmosTag;
            this.data.AgentId = FileProcessorTests.AgentId;

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(FileProcessorTests.TaskId, "context");

            // verify
            this.mockFile.Verify(o => o.MoveRelativeAsync(expectedPath, true, true), Times.Once);
            this.mockFile.Verify(
                o => o.SetLifetimeAsync(
                    TimeSpan.FromHours(this.mockFileSystemMgr.Object.CosmosPathsAndExpiryTimes.HoldingExpiryHours), 
                    true), 
                Times.Once);
        }

        [TestMethod]
        public async Task ProcessorAbandonsDataWhenWriterWriteThrowsNonTransientException()
        {
            const string ErrorInfo = "ERRORINFO";
            const string CommandId = "cid";
            const string ProductId = "PID";
            const string RowData0 = "DATA0";
            const string RowData1 = "DATA1";

            string rowData = 
                $"{CommandId}\t{ProductId}\t{RowData0}\n{CommandId}\t{ProductId}\t{RowData1}\n";

            Func<ICollection<CommandFileState>, bool> verifier =
                c =>
                {
                    CommandFileState item;

                    Assert.AreEqual(1, c.Count);

                    item = c.First();
                    Assert.AreEqual(ErrorInfo, item.NonTransientErrorInfo);
                    Assert.AreEqual((CommandId + "&" + this.data.DataFilePath), item.DataFilePathAndCommand);
                    Assert.AreEqual(this.data.AgentId, item.AgentId);
                    Assert.AreEqual(CommandId, item.CommandId);
                    Assert.AreEqual(0, item.RowCount);
                    Assert.AreEqual(0, item.ByteCount);
                    return true;
                };

            TestUtilities.PopulateStreamWithString(rowData, this.fileContents);

            this.data.ExportFileName = "FILE";
            this.data.ManifestPath = "PATH/MANIFEST.txt";
            this.data.DataFilePath = "PATH";
            this.data.CosmosTag = FileProcessorTests.CosmosTag;

            this.mockWriter.SetupGet(o => o.LastErrorDetails).Returns(ErrorInfo);
            this.mockWriter.SetupGet(o => o.LogForCommandFeed).Returns(true);
            this.mockWriter.SetupGet(o => o.CommandId).Returns(CommandId);
            this.mockWriter.SetupGet(o => o.RowCount).Returns(0);
            this.mockWriter.SetupGet(o => o.Size).Returns(0);

            this.mockWriter
                .Setup(o => o.WriteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Task.FromException<long>(new NonTransientStorageException("Hi!")));

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(FileProcessorTests.TaskId, "context");

            // verify
            this.mockWriterFactory.Verify(
                o => o.CreateAsync(
                    It.IsAny<CancellationToken>(), FileProcessorTests.AgentId, CommandId, this.data.ExportFileName),
                Times.Once);

            this.mockWriter
                .Verify(
                    o => o.WriteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()),
                    Times.Exactly(2));

            this.mockWriter.Verify(o => o.CloseAsync(), Times.Once);

            this.mockCommandFileState.Verify(
                o => o.InsertBatchAsync(It.Is<ICollection<CommandFileState>>(p => verifier(p))), Times.Once);
        }

        [TestMethod]
        public async Task ProcessorAbandonsDataWhenWriterFlushThrowsNonTransientException()
        {
            const string ErrorInfo = "ERRORINFO";
            const string CommandId = "cid";
            const string ProductId = "PID";
            const string RowData0 = "DATA0";
            const string RowData1 = "DATA1";

            string rowData =
                $"{CommandId}\t{ProductId}\t{RowData0}\n{CommandId}\t{ProductId}\t{RowData1}\n";

            Func<ICollection<CommandFileState>, bool> verifier =
                c =>
                {
                    CommandFileState item;

                    Assert.AreEqual(1, c.Count);

                    item = c.First();
                    Assert.AreEqual(ErrorInfo, item.NonTransientErrorInfo);
                    Assert.AreEqual((CommandId + "&" + this.data.DataFilePath), item.DataFilePathAndCommand);
                    Assert.AreEqual(this.data.AgentId, item.AgentId);
                    Assert.AreEqual(CommandId, item.CommandId);
                    Assert.AreEqual(0, item.RowCount);
                    Assert.AreEqual(0, item.ByteCount);
                    return true;
                };

            TestUtilities.PopulateStreamWithString(rowData, this.fileContents);

            // force the task to issue a flush
            this.config.OverallPendingByteThreshold = 32;
            this.mockWriter
                .Setup(o => o.WriteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Task.FromResult((long)this.config.OverallPendingByteThreshold + 1));

            this.data.ExportFileName = "FILE";
            this.data.ManifestPath = "PATH/MANIFEST.txt";
            this.data.DataFilePath = "PATH";
            this.data.CosmosTag = FileProcessorTests.CosmosTag;

            this.mockWriter.SetupGet(o => o.LastErrorDetails).Returns(ErrorInfo);
            this.mockWriter.SetupGet(o => o.LogForCommandFeed).Returns(true);
            this.mockWriter.SetupGet(o => o.PendingSize).Returns((long)this.config.OverallPendingByteThreshold + 1);
            this.mockWriter.SetupGet(o => o.CommandId).Returns(CommandId);
            this.mockWriter.SetupGet(o => o.RowCount).Returns(0);
            this.mockWriter.SetupGet(o => o.Size).Returns(0);

            this.mockWriter
                .Setup(o => o.FlushAsync())
                .Returns(Task.FromException<long>(new NonTransientStorageException("Hi!")));

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(FileProcessorTests.TaskId, "context");

            // verify
            this.mockWriterFactory.Verify(
                o => o.CreateAsync(
                    It.IsAny<CancellationToken>(), FileProcessorTests.AgentId, CommandId, this.data.ExportFileName),
                Times.Once);

            this.mockWriter
                .Verify(
                    o => o.WriteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()),
                    Times.Exactly(2));

            this.mockWriter.Verify(o => o.FlushAsync(), Times.AtLeast(1));

            this.mockWriter.Verify(o => o.CloseAsync(), Times.Once);

            this.mockCommandFileState.Verify(
                o => o.InsertBatchAsync(It.Is<ICollection<CommandFileState>>(p => verifier(p))), Times.Once);
        }

        [TestMethod]
        public async Task ProcessorAbandonsDataWhenWriterCloseThrowsNonTransientExceptionOnWrite()
        {
            const string ErrorInfo = "ERRORINFO";
            const string CommandId = "cid";
            const string ProductId = "PID";
            const string RowData0 = "DATA0";
            const string RowData1 = "DATA1";

            string rowData =
                $"{CommandId}\t{ProductId}\t{RowData0}\n{CommandId}\t{ProductId}\t{RowData1}\n";

            Func<ICollection<CommandFileState>, bool> verifier =
                c =>
                {
                    CommandFileState item;

                    Assert.AreEqual(1, c.Count);

                    item = c.First();
                    Assert.AreEqual(ErrorInfo, item.NonTransientErrorInfo);
                    Assert.AreEqual((CommandId + "&" + this.data.DataFilePath), item.DataFilePathAndCommand);
                    Assert.AreEqual(this.data.AgentId, item.AgentId);
                    Assert.AreEqual(CommandId, item.CommandId);
                    Assert.AreEqual(0, item.RowCount);
                    Assert.AreEqual(0, item.ByteCount);
                    return true;
                };

            TestUtilities.PopulateStreamWithString(rowData, this.fileContents);

            this.data.ExportFileName = "FILE";
            this.data.ManifestPath = "PATH/MANIFEST.txt";
            this.data.DataFilePath = "PATH";
            this.data.CosmosTag = FileProcessorTests.CosmosTag;

            this.mockWriter.SetupGet(o => o.LastErrorDetails).Returns(ErrorInfo);
            this.mockWriter.SetupGet(o => o.LogForCommandFeed).Returns(true);
            this.mockWriter.SetupGet(o => o.CommandId).Returns(CommandId);
            this.mockWriter.SetupGet(o => o.RowCount).Returns(0);
            this.mockWriter.SetupGet(o => o.Size).Returns(0);

            this.mockWriter
                .Setup(o => o.CloseAsync())
                .Returns(Task.FromException<int>(new NonTransientStorageException("Hi!")));

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(FileProcessorTests.TaskId, "context");

            // verify
            this.mockWriter.Verify(o => o.CloseAsync(), Times.Once);

            this.mockCommandFileState.Verify(
                o => o.InsertBatchAsync(It.Is<ICollection<CommandFileState>>(p => verifier(p))), Times.Once);
        }

        [TestMethod]
        [DataRow(999, FileSizePartition.Small)]
        [DataRow(1999,  FileSizePartition.Medium)]
        [DataRow(2999,  FileSizePartition.Large)]
        public async Task ProcessorRoutesEmptyQueueItemWithNonZeroSizedFileToCorrectNonEmptyQueue(
            int fileSize,
            FileSizePartition partitionId)
        {
            this.data.ExportFileName = "FILE";
            this.data.DataFilePath = "PATH";
            this.data.CosmosTag = FileProcessorTests.CosmosTag;

            this.mockPendingQueue
                .Setup(
                    o => o.DequeueAsync(
                        It.IsAny<IReadOnlyList<FileSizePartition>>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<IRetryPolicy>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    new PartitionedQueueItem<PendingDataFile, FileSizePartition>(
                        this.mockQueueItem.Object, FileSizePartition.Empty));

            this.mockFile.SetupGet(o => o.Size).Returns(fileSize);

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(FileProcessorTests.TaskId, "context");

            // verify
            this.mockFile.Verify(o => o.GetDataReader(), Times.Never);

            this.mockDoneQueue.Verify(
                o => o.EnqueueAsync(It.IsAny<CompleteDataFile>(), It.IsAny<CancellationToken>()), Times.Never);

            this.mockPendingQueue.Verify(
                o => o.EnqueueAsync(partitionId, It.IsAny<PendingDataFile>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task ProcessorProcessesEmptyQueueItemWithZeroSizedFileAndZeroDataAsNormalFile()
        {
            this.data.ExportFileName = "FILE";
            this.data.ManifestPath = "PATH/MANIFEST.txt";
            this.data.DataFilePath = "PATH";
            this.data.CosmosTag = FileProcessorTests.CosmosTag;

            this.mockPendingQueue
                .Setup(
                    o => o.DequeueAsync(
                        It.IsAny<IReadOnlyList<FileSizePartition>>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<IRetryPolicy>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    new PartitionedQueueItem<PendingDataFile, FileSizePartition>(
                        this.mockQueueItem.Object, FileSizePartition.Empty));

            this.mockFile.Setup(o => o.ReadFileChunkAsync(It.IsAny<long>(), It.IsAny<int>())).ReturnsAsync(new MemoryStream());
            this.mockFile.SetupGet(o => o.Size).Returns(0);

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(FileProcessorTests.TaskId, "context");

            // verify
            this.mockFile.Verify(o => o.GetDataReader(), Times.Once);

            this.mockDoneQueue.Verify(
                o => o.EnqueueAsync(
                    It.Is<CompleteDataFile>(p => this.data.DataFilePath.EqualsIgnoreCase(p.DataFilePath)), 
                    It.IsAny<CancellationToken>()), 
                Times.Once);

            this.mockPendingQueue.Verify(
                o => o.EnqueueAsync(It.IsAny<FileSizePartition>(), It.IsAny<PendingDataFile>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [TestMethod]
        public async Task ProcessorProcessesEmptyQueueItemWithMissingFileAsNormalFile()
        {
            this.data.ExportFileName = "FILE";
            this.data.DataFilePath = "PATH";
            this.data.CosmosTag = FileProcessorTests.CosmosTag;

            this.mockPendingQueue
                .Setup(
                    o => o.DequeueAsync(
                        It.IsAny<IReadOnlyList<FileSizePartition>>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<IRetryPolicy>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    new PartitionedQueueItem<PendingDataFile, FileSizePartition>(
                        this.mockQueueItem.Object, FileSizePartition.Empty));

            this.mockFileSource.Setup(o => o.OpenExistingFileAsync(It.IsAny<string>())).ReturnsAsync((IFile)null);

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(FileProcessorTests.TaskId, "context");

            // verify
            this.mockDoneQueue.Verify(
                o => o.EnqueueAsync(
                    It.Is<CompleteDataFile>(p => this.data.DataFilePath.EqualsIgnoreCase(p.DataFilePath)),
                    It.IsAny<CancellationToken>()), 
                Times.Once);

            this.mockPendingQueue.Verify(
                o => o.EnqueueAsync(It.IsAny<FileSizePartition>(), It.IsAny<PendingDataFile>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [TestMethod]
        public async Task ProcessorReleasesEmptyQueueItemWithZeroSizedFileAndNonZeroDataForLaterProcessing()
        {
            this.data.ExportFileName = "FILE";
            this.data.DataFilePath = "PATH";
            this.data.CosmosTag = FileProcessorTests.CosmosTag;

            this.mockPendingQueue
                .Setup(
                    o => o.DequeueAsync(
                        It.IsAny<IReadOnlyList<FileSizePartition>>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<IRetryPolicy>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    new PartitionedQueueItem<PendingDataFile, FileSizePartition>(
                        this.mockQueueItem.Object, FileSizePartition.Empty));

            this.mockFile.SetupGet(o => o.Size).Returns(0);

            this.mockFile
                .Setup(o => o.ReadFileChunkAsync(It.IsAny<long>(), It.IsAny<int>()))
                .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("123\t123\t{}\n")));

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(FileProcessorTests.TaskId, "context");

            // verify
            this.mockDoneQueue.Verify(
                o => o.EnqueueAsync(It.IsAny<CompleteDataFile>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.mockPendingQueue.Verify(
                o => o.EnqueueAsync(It.IsAny<FileSizePartition>(), It.IsAny<PendingDataFile>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.mockLease.Verify(o => o.ReleaseAsync(false), Times.Once);
            this.mockQueueItem.Verify(
                o => o.RenewLeaseAsync(TimeSpan.FromMinutes(this.config.DelayIfCouldNotCompleteMinutes)), 
                Times.Once);
        }
    }
}

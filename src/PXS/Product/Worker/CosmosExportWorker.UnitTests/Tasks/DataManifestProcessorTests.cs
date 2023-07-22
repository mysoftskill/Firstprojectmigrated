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
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility.ManifestParsers;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExportWorker.UnitTests.TestUtility;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Azure.Storage.RetryPolicies;

    using Moq;

    [TestClass]
    public class DataManifestProcessorTests
    {
        private const string PathPrefix = "/PATH";
        private const string DataManifestName = "DATAMANIFEST";
        private const string DataManifestPath = DataManifestProcessorTests.PathPrefix + "/" + DataManifestProcessorTests.DataManifestName;
        private const string ReqManifestName = "REQMANIFEST";
        private const string ReqManifestPath = DataManifestProcessorTests.PathPrefix + "/" + DataManifestProcessorTests.ReqManifestName;
        private const string CosmosTag = "COSMOSTAG";
        private const string CommandId = "COMMANDID";
        private const string AgentId = "AGENTID";
        private const string TaskId = "TASKID";

        private class DataManifestProcessorTestException : Exception { }

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
            public int ActivityLogExpiryHours { get; } = 1;
            public int DeadLetterExpiryHours { get; } = 1;
            public int HoldingExpiryHours { get; } = 1;
            public string StatsLog { get; } = "STATS/";
            public int StatsLogExpiryHours { get; } = 1;
            public int ManifestHoldingExpiryHours { get; } = 1;
        }

        private class TestConfig : IDataManifestProcessorConfig
        {
            public string Tag { get; } = "Tag";
            public string TaskType { get; } = "Type";
            public int InstanceCount { get; } = 1;
            public int LeaseMinutes { get; } = 30;
            public int MinimumRenewMinutes { get; } = 5;
            public int MaxStateUpdateAttempts { get; } = 1;
            public int DelayIfCouldNotCompleteMinutes { get; } = 5;
            public int MaxCommandWaitTimeMinutes { get; } = 120;
            public int DelayOnExceptionMinutes { get; } = 0;
            public int MaxDequeueCount { get; } = 20;
            public int CommandReaderLeaseUpdateRowCount { get; } = 1000000;
        }

        private readonly Mock<IPartitionedQueue<PendingDataFile, FileSizePartition>> mockPendingQueue = 
            new Mock<IPartitionedQueue<PendingDataFile, FileSizePartition>>();

        private readonly Mock<ITable<ManifestFileSetState>> mockManifestState = new Mock<ITable<ManifestFileSetState>>();
        private readonly Mock<IFileProgressTrackerFactory> mockTrackFactory = new Mock<IFileProgressTrackerFactory>();
        private readonly Mock<IQueueItem<ManifestFileSet>> mockQueueItem = new Mock<IQueueItem<ManifestFileSet>>();
        private readonly Mock<IRequestCommandUtilities> mockReqUtils = new Mock<IRequestCommandUtilities>();
        private readonly Mock<IQueue<CompleteDataFile>> mockCompleteQueue = new Mock<IQueue<CompleteDataFile>>();
        private readonly Mock<IQueue<ManifestFileSet>> mockFileSetQueue = new Mock<IQueue<ManifestFileSet>>();
        private readonly Mock<IFileProgressTracker> mockTrack = new Mock<IFileProgressTracker>();
        private readonly Mock<IFileSystemManager> mockFileSystemMgr = new Mock<IFileSystemManager>();
        private readonly Mock<ICosmosFileSystem> mockActivityLog = new Mock<ICosmosFileSystem>();
        private readonly Mock<ICosmosFileSystem> mockFileSource = new Mock<ICosmosFileSystem>(MockBehavior.Strict);
        private readonly Mock<ITelemetryLogger> mockTelemetry = new Mock<ITelemetryLogger>();
        private readonly Mock<ICounterFactory> mockCounters = new Mock<ICounterFactory>();
        private readonly Mock<ILockManager> mockLockManager = new Mock<ILockManager>();
        private readonly Mock<ILockLease> mockLease = new Mock<ILockLease>();
        private readonly Mock<ICounter> mockCounter = new Mock<ICounter>();
        private readonly Mock<IClock> mockClock = new Mock<IClock>();
        private readonly Mock<IFile> mockActivityFile = new Mock<IFile>();
        private readonly Mock<IFile> mockDataManifest = new Mock<IFile>();
        private readonly Mock<IFile> mockReqManifest = new Mock<IFile>();
        private readonly Mock<IFile> mockDataFile = new Mock<IFile>();
        private readonly MockLogger mockLog = new MockLogger();

        private readonly ManifestFileSet data = new ManifestFileSet
        {
            RequestManifestPath = DataManifestProcessorTests.ReqManifestPath,
            DataManifestPath = DataManifestProcessorTests.DataManifestPath,
            CosmosTag = DataManifestProcessorTests.CosmosTag,
            AgentId = DataManifestProcessorTests.AgentId,
        };

        private readonly RequestCommandsInfo cmdStatusResult = new RequestCommandsInfo
        {
            Commands = new Dictionary<CommandStatusCode, ICollection<string>>
            {
                { CommandStatusCode.Actionable, new[] { DataManifestProcessorTests.CommandId } }
            },
            HasNotAvailable = false,
            HasMissing = false,
        };

        private readonly ThresholdConfig threadholdConfig = new ThresholdConfig();
        private readonly MemoryStream dataManifestContents = new MemoryStream();
        private readonly MemoryStream reqManifestContents = new MemoryStream();
        private readonly TestConfig config = new TestConfig();

        private DataManifestProcessor testObj;

        [TestInitialize]
        public void Init()
        {
            this.mockFileSetQueue
                .Setup(
                    o => o.DequeueAsync(
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
            this.mockFileSystemMgr.SetupGet(o => o.FileSizeThresholds).Returns(() => this.threadholdConfig);
            this.mockFileSystemMgr.SetupGet(o => o.ActivityLog).Returns(this.mockActivityLog.Object);
            this.mockFileSystemMgr.Setup(o => o.GetFileSystem(It.IsAny<string>())).Returns(this.mockFileSource.Object);

            this.mockActivityLog.SetupGet(o => o.RootDirectory).Returns("ACTIVITYBASE/");
            this.mockActivityLog
                .Setup(o => o.CreateFileAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<FileCreateMode>()))
                .ReturnsAsync(this.mockActivityFile.Object);

            this.mockFileSource
                .Setup(o => o.OpenExistingFileAsync(DataManifestProcessorTests.ReqManifestPath))
                .ReturnsAsync(this.mockReqManifest.Object);

            this.mockFileSource
                .Setup(o => o.OpenExistingFileAsync(DataManifestProcessorTests.DataManifestPath))
                .ReturnsAsync(this.mockDataManifest.Object);

            this.mockDataManifest.SetupGet(o => o.ParentDirectory).Returns(DataManifestProcessorTests.PathPrefix);
            this.mockDataManifest.SetupGet(o => o.Path).Returns(DataManifestProcessorTests.DataManifestPath);
            this.mockDataManifest.SetupGet(o => o.Name).Returns(DataManifestProcessorTests.DataManifestName);
            this.mockDataManifest.SetupGet(o => o.Created).Returns(() => this.mockClock.Object.UtcNow.AddMilliseconds(1));
            this.mockDataManifest.Setup(o => o.GetDataReader()).Returns(() => this.dataManifestContents);

            this.mockReqManifest.SetupGet(o => o.ParentDirectory).Returns(DataManifestProcessorTests.PathPrefix);
            this.mockReqManifest.SetupGet(o => o.Path).Returns(DataManifestProcessorTests.ReqManifestName);
            this.mockReqManifest.SetupGet(o => o.Name).Returns(DataManifestProcessorTests.ReqManifestPath);
            this.mockReqManifest.SetupGet(o => o.Created).Returns(() => this.mockClock.Object.UtcNow);
            this.mockReqManifest.Setup(o => o.GetDataReader()).Returns(() => this.reqManifestContents);

            this.mockReqUtils
                .Setup(
                    o => o.DetermineCommandStatusAsync(
                        It.IsAny<OperationContext>(),
                        It.IsAny<string>(),
                        It.IsAny<ICollection<string>>(),
                        It.IsAny<ILeaseRenewer>(),
                        It.IsAny<CancellationToken>(),
                        It.IsAny<bool>()))
                .ReturnsAsync(this.cmdStatusResult);

            this.mockManifestState
                .Setup(o => o.InsertAsync(It.IsAny<ManifestFileSetState>()))
                .ReturnsAsync(true);

            this.mockLockManager
                .Setup(
                    o => o.AttemptAcquireAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<bool>()))
                .ReturnsAsync(this.mockLease.Object);

            this.mockCounters
                .Setup(o => o.GetCounter(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CounterType>()))
                .Returns(this.mockCounter.Object);

            this.mockClock.SetupGet(o => o.UtcNow).Returns(new DateTimeOffset(2006, 4, 15, 15, 0, 0, TimeSpan.Zero));

            this.testObj = new DataManifestProcessor(
                this.config,
                this.mockTrackFactory.Object,
                this.mockManifestState.Object,
                this.mockCompleteQueue.Object,
                this.mockFileSetQueue.Object,
                this.mockPendingQueue.Object,
                this.mockReqUtils.Object,
                this.mockFileSystemMgr.Object,
                this.mockTelemetry.Object,
                this.mockCounters.Object,
                this.mockLockManager.Object,
                this.mockLog.Object,
                this.mockClock.Object);
        }

        [TestMethod]
        public async Task ProcessorReturnsImmediatelyWithoutAcquiringALockIfDequeueReturnsNull()
        {
            this.mockFileSetQueue
                .Setup(
                    o => o.DequeueAsync(
                        It.IsAny<TimeSpan>(), 
                        It.IsAny<TimeSpan>(), 
                        It.IsAny<IRetryPolicy>(), 
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync((IQueueItem<ManifestFileSet>)null);

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(DataManifestProcessorTests.TaskId, "context");

            // verify
            this.mockFileSetQueue.Verify(
                o => o.DequeueAsync(
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

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(DataManifestProcessorTests.TaskId, "context");

            // verify
            this.mockFileSetQueue.Verify(
                o => o.DequeueAsync(
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

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(DataManifestProcessorTests.TaskId, "context");

            // verify
            this.mockFileSetQueue.Verify(
                o => o.DequeueAsync(
                    TimeSpan.FromMinutes(this.config.LeaseMinutes), 
                    It.IsAny<TimeSpan>(), 
                    It.IsAny<IRetryPolicy>(), 
                    It.IsAny<CancellationToken>()), 
                Times.Once);

            this.mockLockManager.Verify(
                o => o.AttemptAcquireAsync(
                    DataManifestProcessorTests.AgentId,
                    this.data.DataManifestPath,
                    DataManifestProcessorTests.TaskId,
                    TimeSpan.FromMinutes(this.config.LeaseMinutes),
                    false),
                Times.Once);

            this.mockFileSource.Verify(o => o.OpenExistingFileAsync(It.IsAny<string>()), Times.Never);

            this.mockQueueItem.Verify(o => o.CompleteAsync(), Times.Once);
        }

        [TestMethod]
        public async Task ProcessorAttemptsToOpensFileIfLockObtained()
        {
            this.mockFileSource.Setup(o => o.OpenExistingFileAsync(It.IsAny<string>())).ReturnsAsync((IFile)null);

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(DataManifestProcessorTests.TaskId, "context");

            // verify
            this.mockFileSetQueue.Verify(
                o => o.DequeueAsync(
                    TimeSpan.FromMinutes(this.config.LeaseMinutes), 
                    It.IsAny<TimeSpan>(), 
                    It.IsAny<IRetryPolicy>(), 
                    It.IsAny<CancellationToken>()), 
                Times.Once);

            this.mockLockManager.Verify(
                o => o.AttemptAcquireAsync(
                    DataManifestProcessorTests.AgentId,
                    this.data.DataManifestPath,
                    DataManifestProcessorTests.TaskId,
                    TimeSpan.FromMinutes(this.config.LeaseMinutes),
                    false),
                Times.Once);

            this.mockFileSource.Verify(o => o.OpenExistingFileAsync(this.data.RequestManifestPath), Times.Once);
            this.mockFileSource.Verify(o => o.OpenExistingFileAsync(this.data.DataManifestPath), Times.Once);
            this.mockQueueItem.Verify(o => o.CompleteAsync(), Times.Once);
            this.mockLease.Verify(o => o.ReleaseAsync(true));
        }
        

        [TestMethod]
        public async Task ProcessorReleasesLeaseInNonPurgeModeIfFileOpThrows()
        {
            this.mockFileSource
                .Setup(o => o.OpenExistingFileAsync(It.IsAny<string>()))
                .Returns(Task.FromException<IFile>(new DataManifestProcessorTestException()));

            // test
            try
            {
                await this.testObj.RunSingleInstanceOnePassAsync(DataManifestProcessorTests.TaskId, "context");
            }
            catch (DataManifestProcessorTestException)
            {
            }

            // verify
            this.mockQueueItem.Verify(
                o => o.RenewLeaseAsync(TimeSpan.FromMinutes(this.config.DelayIfCouldNotCompleteMinutes)), 
                Times.Once);

            this.mockLease.Verify(o => o.ReleaseAsync(false));
        }
        
        [TestMethod]
        public async Task ProcessorReadsRequestManifestQueriesStateStoreForTheCommandsListedInsideAndInsertsToManifestState()
        {
            int expectedHash = Utility.GetHashCodeForUnorderedCollection(
                new[] { DataManifestProcessorTests.CommandId.ToLowerInvariant() });

            TestUtilities.PopulateStreamWithString(DataManifestProcessorTests.CommandId + "\n", this.reqManifestContents);

            Func<ManifestFileSetState, bool> verifier =
                o =>
                {
                    Assert.AreEqual(DataManifestProcessorTests.AgentId, o.AgentId);
                    Assert.AreEqual(DataManifestProcessorTests.ReqManifestPath, o.RequestManifestPath);
                    Assert.AreEqual(this.mockReqManifest.Object.Created, o.RequestManifestCreateTime);
                    Assert.AreEqual(expectedHash, o.RequestManifestHash);
                    return true;
                };

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(DataManifestProcessorTests.TaskId, "context");

            // verify
            this.mockFileSource.Verify(o => o.OpenExistingFileAsync(DataManifestProcessorTests.ReqManifestPath), Times.Once);

            this.mockReqManifest.Verify(o => o.GetDataReader(), Times.Once);

            this.mockReqUtils.Verify(
                o => o.DetermineCommandStatusAsync(
                    It.IsAny<OperationContext>(),
                    DataManifestProcessorTests.AgentId,
                    It.Is<ICollection<string>>(p => p.Count == 1 && p.Contains(DataManifestProcessorTests.CommandId)),
                    It.IsAny<ILeaseRenewer>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<bool>()),
                Times.Once);

            this.mockManifestState.Verify(o => o.InsertAsync(It.Is<ManifestFileSetState>(p => verifier(p))), Times.Once);
            this.mockManifestState.Verify(o => o.InsertAsync(It.IsAny<ManifestFileSetState>()), Times.Once);
        }

        [TestMethod]
        [DataRow(false, CommandStatusCode.Undetermined, false, false)]
        [DataRow(false, CommandStatusCode.NotAvailable, false, false)]
        [DataRow(false, CommandStatusCode.Missing, false, true)]
        [DataRow(false, CommandStatusCode.Actionable, true, false)]
        [DataRow(false, CommandStatusCode.Completed, true, false)]
        [DataRow(false, CommandStatusCode.Ignored, true, false)]
        [DataRow(false, CommandStatusCode.NotApplicable, true, false)]
        [DataRow(true, CommandStatusCode.Undetermined, false, false)]
        [DataRow(true, CommandStatusCode.NotAvailable, false, false)]
        [DataRow(true, CommandStatusCode.Missing, true, true)]
        [DataRow(true, CommandStatusCode.Actionable, true, false)]
        [DataRow(true, CommandStatusCode.Completed, true, false)]
        [DataRow(true, CommandStatusCode.Ignored, true, false)]
        [DataRow(true, CommandStatusCode.NotApplicable, true, false)]
        public async Task ProcessorEnqueuesAppropriatelyBasedOnCommandState(
            bool isManifestOld,
            CommandStatusCode code,
            bool enqueueExpected,
            bool reportsMissingFiles)
        {
            Func<CompleteDataFile, bool> verifier =
                o =>
                {
                    Assert.AreEqual(DataManifestProcessorTests.DataManifestPath, o.ManifestPath);
                    Assert.AreEqual(DataManifestProcessorTests.CosmosTag, o.CosmosTag);
                    Assert.AreEqual(DataManifestProcessorTests.AgentId, o.AgentId);
                    return true;
                };

            if (isManifestOld)
            {
                int ageMins = this.config.MaxCommandWaitTimeMinutes + 1;

                // 'too old' config value is set to 2 hours, so move this back in time 3 hours
                this.mockDataManifest.SetupGet(o => o.Created).Returns(this.mockClock.Object.UtcNow.AddMinutes(-1 * ageMins));
                this.mockReqManifest.SetupGet(o => o.Created).Returns(this.mockClock.Object.UtcNow.AddMinutes(-1 * ageMins));
            }

            TestUtilities.PopulateStreamWithString(DataManifestProcessorTests.CommandId + "\n", this.reqManifestContents);

            // this intentionally does not setup a data file which causes the enqueue to happen to the completed queue instead 
            //  of the file queue, but from the point of view of the command code, there is no difference so this is valid
            //  for this test
            TestUtilities.PopulateStreamWithString(string.Empty, this.dataManifestContents);

            this.cmdStatusResult.Commands.Clear();
            this.cmdStatusResult.Commands[code] = new[] { DataManifestProcessorTests.CommandId };

            this.cmdStatusResult.HasNotAvailable = (code == CommandStatusCode.NotAvailable);
            this.cmdStatusResult.HasUndetermined = (code == CommandStatusCode.Undetermined);
            this.cmdStatusResult.HasMissing = (code == CommandStatusCode.Missing);

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(DataManifestProcessorTests.TaskId, "context");

            // verify
            this.mockCompleteQueue.Verify(
                o => o.EnqueueAsync(It.Is<CompleteDataFile>(p => verifier(p)), It.IsAny<CancellationToken>()), 
                enqueueExpected ? (Func<Times>)Times.Once : Times.Never);
        }

        [TestMethod]
        [DataRow(false, CommandStatusCode.Undetermined, false)]
        [DataRow(false, CommandStatusCode.NotAvailable, false)]
        [DataRow(false, CommandStatusCode.Missing, true)]
        [DataRow(false, CommandStatusCode.Actionable, false)]
        [DataRow(false, CommandStatusCode.Completed, false)]
        [DataRow(false, CommandStatusCode.Ignored, false)]
        [DataRow(false, CommandStatusCode.NotApplicable, false)]
        [DataRow(false, CommandStatusCode.Undetermined, false)]
        [DataRow(true, CommandStatusCode.NotAvailable, false)]
        [DataRow(true, CommandStatusCode.Missing, true)]
        [DataRow(true, CommandStatusCode.Actionable, false)]
        [DataRow(true, CommandStatusCode.Completed, false)]
        [DataRow(true, CommandStatusCode.Ignored, false)]
        [DataRow(true, CommandStatusCode.NotApplicable, false)]
        public async Task ProcessorLogsCommandResultsAppropriatelyBasedOnCommandState(
            bool isManifestOld,
            CommandStatusCode code,
            bool reportsMissingFiles)
        {
            TimeSpan ageBatch = TimeSpan.Zero;

            if (isManifestOld)
            {
                int ageMins = this.config.MaxCommandWaitTimeMinutes + 1;

                ageBatch = TimeSpan.FromMinutes(ageMins);

                // 'too old' config value is set to 2 hours, so move this back in time 3 hours
                this.mockDataManifest.SetupGet(o => o.Created).Returns(this.mockClock.Object.UtcNow.AddMinutes(-1 * ageMins));
                this.mockReqManifest.SetupGet(o => o.Created).Returns(this.mockClock.Object.UtcNow.AddMinutes(-1 * ageMins));
            }

            TestUtilities.PopulateStreamWithString(DataManifestProcessorTests.CommandId + "\n", this.reqManifestContents);

            // this intentionally does not setup a data file which causes the enqueue to happen to the completed queue instead 
            //  of the file queue, but from the point of view of the command code, there is no difference so this is valid
            //  for this test
            TestUtilities.PopulateStreamWithString(string.Empty, this.dataManifestContents);

            this.cmdStatusResult.Commands.Clear();
            this.cmdStatusResult.Commands[code] = new[] { DataManifestProcessorTests.CommandId };

            this.cmdStatusResult.HasNotAvailable = (code == CommandStatusCode.NotAvailable);
            this.cmdStatusResult.HasUndetermined = (code == CommandStatusCode.Undetermined);
            this.cmdStatusResult.HasMissing = (code == CommandStatusCode.Missing);

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(DataManifestProcessorTests.TaskId, "context");

            // verify
            this.mockReqUtils.Verify(
                o => o.ReportCommandSummaryAsync(
                    It.IsAny<OperationContext>(),
                    this.mockTrack.Object,
                    this.mockActivityLog.Object,
                    It.IsAny<ITaskTracer>(),
                    DataManifestProcessorTests.AgentId,
                    It.Is<RequestCommandsInfo>(
                        p => p.Commands.Count == 1 &&
                             p.Commands.ContainsKey(code) &&
                             p.Commands[code].Count == 1 &&
                             p.Commands[code].First().EqualsIgnoreCase(DataManifestProcessorTests.CommandId)),
                    this.mockReqManifest.Object.Name,
                    ageBatch,
                    TimeSpan.FromMinutes(this.config.MaxCommandWaitTimeMinutes),
                    isManifestOld),
                Times.Once);
        }

        [TestMethod]
        public async Task ProcessorReadsDataManifestAndInsertsIntoManifestState()
        {
            const string DataFileNameWithReplacement = "DATAFILE_2018_01_02.txt";
            const string ManifestFileName = "MFILE_2018_01_02.txt";
            const string DataFileNameRaw = "DATAFILE_%Y_01_%d.txt";
            const string ManifestPath = "PATH/" + ManifestFileName;
            const string DataFilePath = "/PATH/" + DataFileNameWithReplacement;

            const string ExpectedFileName =
                DataManifestProcessorTests.CosmosTag + "." +
                DataManifestProcessorTests.AgentId + "." +
                DataFileNameWithReplacement;

            int expectedHash = Utility.GetHashCodeForUnorderedCollection(new[] { DataFileNameRaw });

            Func<ManifestFileSetState, bool> verifier =
                o =>
                {
                    Assert.AreEqual(DataManifestProcessorTests.AgentId, o.AgentId);
                    Assert.AreEqual(ManifestPath, o.ManifestPath);
                    Assert.AreEqual(1, o.DataFileTags.Count);
                    Assert.AreEqual(ExpectedFileName, o.DataFileTags.First());
                    Assert.AreEqual(this.mockDataManifest.Object.Created, o.DataFileManifestCreateTime);
                    Assert.AreEqual(expectedHash, o.DataFileManifestHash);
                    return true;
                };

            this.data.DataManifestPath = ManifestPath;

            this.mockDataManifest.SetupGet(o => o.Path).Returns(ManifestPath);
            this.mockDataManifest.SetupGet(o => o.Name).Returns(ManifestFileName);

            this.mockFileSource
                .Setup(o => o.OpenExistingFileAsync(ManifestPath))
                .ReturnsAsync(this.mockDataManifest.Object);

            this.mockFileSource
                .Setup(o => o.OpenExistingFileAsync(DataManifestProcessorTests.DataManifestPath))
                .Returns(Task.FromException<IFile>(new DataManifestProcessorTestException()));

            this.mockFileSource
                .Setup(o => o.OpenExistingFileAsync(DataFilePath))
                .ReturnsAsync(this.mockDataFile.Object);

            TestUtilities.PopulateStreamWithString(DataManifestProcessorTests.CommandId + "\n", this.reqManifestContents);
            TestUtilities.PopulateStreamWithString(DataFileNameRaw + "\n", this.dataManifestContents);

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(DataManifestProcessorTests.TaskId, "context");

            // verify
            this.mockDataManifest.Verify(o => o.GetDataReader(), Times.Once);
            this.mockManifestState.Verify(o => o.InsertAsync(It.Is<ManifestFileSetState>(p => verifier(p))), Times.Once);
            this.mockManifestState.Verify(o => o.InsertAsync(It.IsAny<ManifestFileSetState>()), Times.Once);
        }

        [TestMethod]
        public async Task ProcessorReadsDataManifestAndEnqueuesDataFilesToManifestFileQueue()
        {
            const string DataFileNameReplacement = "DATAFILE_2018_01_02.txt";
            const string ManifestFileName = "MFILE_2018_01_02.txt";
            const string ExportFileName = "DATAFILE";
            const string ManifestPath = "PATH/" + ManifestFileName;
            const string DataFilePath = "/PATH/" + DataFileNameReplacement;
            const string DataFileName = "DATAFILE_%Y_01_%d.txt";

            Func<PendingDataFile, bool> queueVerifier =
                o =>
                {
                    Assert.AreEqual(DataManifestProcessorTests.AgentId, o.AgentId);
                    Assert.AreEqual(this.data.CosmosTag, o.CosmosTag);
                    Assert.AreEqual(ManifestPath, o.ManifestPath);
                    Assert.AreEqual(DataManifestProcessorTests.PathPrefix + "/" + DataFileNameReplacement, o.DataFilePath);
                    Assert.AreEqual(ExportFileName + ".json", o.ExportFileName);
                    return true;
                };

            this.data.DataManifestPath = ManifestPath;

            this.mockDataManifest.SetupGet(o => o.Path).Returns(ManifestPath);
            this.mockDataManifest.SetupGet(o => o.Name).Returns(ManifestFileName);

            this.mockFileSource
                .Setup(o => o.OpenExistingFileAsync(ManifestPath))
                .ReturnsAsync(this.mockDataManifest.Object);

            this.mockFileSource
                .Setup(o => o.OpenExistingFileAsync(DataManifestProcessorTests.DataManifestPath))
                .Returns(Task.FromException<IFile>(new DataManifestProcessorTestException()));

            this.mockFileSource
                .Setup(o => o.OpenExistingFileAsync(DataFilePath))
                .ReturnsAsync(this.mockDataFile.Object);

            TestUtilities.PopulateStreamWithString(DataManifestProcessorTests.CommandId + "\n", this.reqManifestContents);
            TestUtilities.PopulateStreamWithString(DataFileName + "\n", this.dataManifestContents);

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(DataManifestProcessorTests.TaskId, "context");

            // verify
            this.mockPendingQueue.Verify(
                o => o.EnqueueAsync(It.IsAny<FileSizePartition>(), It.IsAny<PendingDataFile>(), It.IsAny<CancellationToken>()),
                Times.Once);

            this.mockPendingQueue.Verify(
                o => o.EnqueueAsync(
                    It.IsAny<FileSizePartition>(), It.Is<PendingDataFile>(p => queueVerifier(p)), It.IsAny<CancellationToken>()),
                Times.Once);

            this.mockCompleteQueue.Verify(
                o => o.EnqueueAsync(It.IsAny<CompleteDataFile>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task ProcessorReadsDataManifestAndEnqueuesToFileCompleteQueueIfNoDataFiles()
        {
            Func<ManifestFileSetState, bool> tableVerifier =
                o =>
                {
                    Assert.AreEqual(DataManifestProcessorTests.AgentId, o.AgentId);
                    Assert.AreEqual(DataManifestProcessorTests.DataManifestPath, o.ManifestPath);
                    Assert.AreEqual(DataManifestProcessorTests.ReqManifestPath, o.RequestManifestPath);
                    Assert.AreEqual(0, o.DataFileTags.Count);
                    return true;
                };

            Func<CompleteDataFile, bool> queueVerifier =
                o =>
                {
                    Assert.AreEqual(DataManifestProcessorTests.AgentId, o.AgentId);
                    Assert.AreEqual(this.data.CosmosTag, o.CosmosTag);
                    Assert.AreEqual(DataManifestProcessorTests.DataManifestPath, o.ManifestPath);
                    Assert.AreEqual(0, o.DataFilePath.Length);
                    return true;
                };

            TestUtilities.PopulateStreamWithString(DataManifestProcessorTests.CommandId + "\n", this.reqManifestContents);
            this.dataManifestContents.SetLength(0);
            this.dataManifestContents.Flush();

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(DataManifestProcessorTests.TaskId, "context");

            // verify
            this.mockDataManifest.Verify(o => o.GetDataReader(), Times.Once);
            this.mockManifestState.Verify(o => o.InsertAsync(It.Is<ManifestFileSetState>(p => tableVerifier(p))), Times.Once);

            this.mockCompleteQueue.Verify(
                o => o.EnqueueAsync(It.IsAny<CompleteDataFile>(), It.IsAny<CancellationToken>()), Times.Once);
            this.mockCompleteQueue.Verify(
                o => o.EnqueueAsync(It.Is<CompleteDataFile>(p => queueVerifier(p)), It.IsAny<CancellationToken>()), Times.Once);

            this.mockPendingQueue.Verify(
                o => o.EnqueueAsync(It.IsAny<FileSizePartition>(), It.IsAny<PendingDataFile>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
        
        [TestMethod]
        [DataRow(0, 1000, 2000, FileSizePartition.Empty)]
        [DataRow(999, 1000, 2000, FileSizePartition.Small)]
        [DataRow(1999, 1000, 2000, FileSizePartition.Medium)]
        [DataRow(2999, 1000, 2000, FileSizePartition.Large)]
        public async Task ProcessorEnqueuesToCorrectQueueBasedOnFileSizeAndThresholds(
            long size,
            long mediumThreshold,
            long largeThreshold,
            FileSizePartition partitionId)
        {
            const string DataFileNameReplacement = "DATAFILE_2018_01_02.txt";
            const string ManifestFileName = "MFILE_2018_01_02.txt";
            const string ManifestPath = "PATH/" + ManifestFileName;
            const string DataFilePath = "/PATH/" + DataFileNameReplacement;
            const string DataFileName = "DATAFILE_%Y_01_%d.txt";

            this.data.DataManifestPath = ManifestPath;

            this.mockDataManifest.SetupGet(o => o.Path).Returns(ManifestPath);
            this.mockDataManifest.SetupGet(o => o.Name).Returns(ManifestFileName);

            this.mockFileSource
                .Setup(o => o.OpenExistingFileAsync(ManifestPath))
                .ReturnsAsync(this.mockDataManifest.Object);

            this.mockFileSource
                .Setup(o => o.OpenExistingFileAsync(DataManifestProcessorTests.DataManifestPath))
                .Returns(Task.FromException<IFile>(new DataManifestProcessorTestException()));

            this.mockFileSource
                .Setup(o => o.OpenExistingFileAsync(DataFilePath))
                .ReturnsAsync(this.mockDataFile.Object);

            TestUtilities.PopulateStreamWithString(DataManifestProcessorTests.CommandId + "\n", this.reqManifestContents);
            TestUtilities.PopulateStreamWithString(DataFileName + "\n", this.dataManifestContents);

            this.threadholdConfig.Medium = mediumThreshold;
            this.threadholdConfig.Large = largeThreshold;

            this.mockDataFile.SetupGet(o => o.Size).Returns(size);

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(DataManifestProcessorTests.TaskId, "context");

            // verify
            this.mockPendingQueue.Verify(
                o => o.EnqueueAsync(partitionId, It.IsAny<PendingDataFile>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
        
        [TestMethod]
        public async Task ProcessorUpdatesStateCounterIfItemAlreadyInserted()
        {
            const string DataFileName = "DATAFILE";
            const string DataFilePath = "/PATH/" + DataFileName;
            const int InitialCounter = 1;

            ManifestFileSetState getResult = new ManifestFileSetState
            {
                ManifestPath = DataManifestProcessorTests.DataManifestPath,
                AgentId = DataManifestProcessorTests.AgentId,
                Counter = InitialCounter,
                DataFileTags = new List<string>()
            };

            Func<ManifestFileSetState, bool> tableVerifier =
                o =>
                {
                    Assert.AreSame(getResult, o);
                    Assert.AreEqual(InitialCounter + 1, o.Counter);
                    return true;
                };

            this.mockFileSource
                .Setup(o => o.OpenExistingFileAsync(DataFilePath))
                .ReturnsAsync(this.mockDataFile.Object);

            TestUtilities.PopulateStreamWithString(DataManifestProcessorTests.CommandId + "\n", this.reqManifestContents);
            TestUtilities.PopulateStreamWithString(DataFileName + "\n", this.dataManifestContents);

            this.mockManifestState.Setup(o => o.InsertAsync(It.IsAny<ManifestFileSetState>())).ReturnsAsync(false);
            this.mockManifestState.Setup(o => o.GetItemAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(getResult);

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(DataManifestProcessorTests.TaskId, "context");

            // verify
            this.mockManifestState.Verify(o => o.ReplaceAsync(It.Is<ManifestFileSetState>(p => tableVerifier(p))), Times.Once);
            this.mockManifestState.Verify(o => o.GetItemAsync(getResult.PartitionKey, getResult.RowKey), Times.Once);
        }

        [TestMethod]
        public async Task ProcessorOnlySendsDataFilesToQueueIfStateAlreadyInsertedAndFileStillInDataFileList()
        {
            const string DataFileName1 = "DATAFILE1";
            const string DataFileName2 = "DATAFILE2";
            const string DataFilePath1 = "/PATH/" + DataFileName1;
            const string DataFilePath2 = "/PATH/" + DataFileName2;
            const string CosmosTagLocal = DataManifestProcessorTests.CosmosTag;
            const string AgentIdLocal = DataManifestProcessorTests.AgentId;
            const int InitialCounter = 1;

            ManifestFileSetState getResult = new ManifestFileSetState
            {
                ManifestPath = DataManifestProcessorTests.DataManifestPath,
                AgentId = DataManifestProcessorTests.AgentId,
                Counter = InitialCounter,
                DataFileTags = new List<string>
                {
                    CosmosTagLocal + "." + AgentIdLocal + "." + DataFileName1,
                }
            };

            Func<PendingDataFile, bool> queueVerifier =
                o =>
                {
                    Assert.AreEqual(DataFilePath1, o.DataFilePath);
                    return true;
                };

            this.mockFileSource
                .Setup(o => o.OpenExistingFileAsync(DataFilePath1))
                .ReturnsAsync(this.mockDataFile.Object);

            this.mockFileSource
                .Setup(o => o.OpenExistingFileAsync(DataFilePath2))
                .ReturnsAsync(this.mockDataFile.Object);

            TestUtilities.PopulateStreamWithString(DataManifestProcessorTests.CommandId + "\n", this.reqManifestContents);
            TestUtilities.PopulateStreamWithString(DataFileName1 + "\n" + DataFileName2 + "\n", this.dataManifestContents);

            this.mockManifestState.Setup(o => o.InsertAsync(It.IsAny<ManifestFileSetState>())).ReturnsAsync(false);
            this.mockManifestState.Setup(o => o.GetItemAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(getResult);

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(DataManifestProcessorTests.TaskId, "context");

            // verify
            this.mockPendingQueue.Verify(
                o => o.EnqueueAsync(It.IsAny<FileSizePartition>(), It.IsAny<PendingDataFile>(), It.IsAny<CancellationToken>()),
                Times.Once);
            this.mockPendingQueue.Verify(
                o => o.EnqueueAsync(
                    It.IsAny<FileSizePartition>(), 
                    It.Is<PendingDataFile>(p => queueVerifier(p)), 
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task ProcessorSendsAllItemsToQueueIfStateNotAlreadyInserted()
        {
            const string DataFileName1 = "DATAFILE1";
            const string DataFileName2 = "DATAFILE2";
            const string DataFilePath1 = "/PATH/" + DataFileName1;
            const string DataFilePath2 = "/PATH/" + DataFileName2;

            this.mockFileSource
                .Setup(o => o.OpenExistingFileAsync(DataFilePath1))
                .ReturnsAsync(this.mockDataFile.Object);

            this.mockFileSource
                .Setup(o => o.OpenExistingFileAsync(DataFilePath2))
                .ReturnsAsync(this.mockDataFile.Object);

            TestUtilities.PopulateStreamWithString(DataManifestProcessorTests.CommandId + "\n", this.reqManifestContents);
            TestUtilities.PopulateStreamWithString(DataFileName1 + "\n" + DataFileName2 + "\n", this.dataManifestContents);

            this.mockManifestState.Setup(o => o.InsertAsync(It.IsAny<ManifestFileSetState>())).ReturnsAsync(true);

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(DataManifestProcessorTests.TaskId, "context");

            // verify
            this.mockPendingQueue.Verify(
                o => o.EnqueueAsync(It.IsAny<FileSizePartition>(), It.IsAny<PendingDataFile>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            this.mockPendingQueue.Verify(
                o => o.EnqueueAsync(
                    It.IsAny<FileSizePartition>(),
                    It.Is<PendingDataFile>(p => DataFilePath2.EqualsIgnoreCase(p.DataFilePath)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            this.mockPendingQueue.Verify(
                o => o.EnqueueAsync(
                    It.IsAny<FileSizePartition>(),
                    It.Is<PendingDataFile>(p => DataFilePath1.EqualsIgnoreCase(p.DataFilePath)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        [DataRow(0, false, false, true)] // good date, good hash
        [DataRow(0, false, true, false)] // good date, bad hash
        [DataRow(0, true, false, true)]  // good date, missing hash
        [DataRow(1, false, false, true)] // bad date, good hash (good hash overrides bad date)
        [DataRow(1, false, true, false)] // bad date, bad hash
        [DataRow(1, true, false, false)] // bad date, missing hash
        public async Task ProcessorEnqueuesItemsAsAppropriateBasedOnDataManifestAgeAndHashAndAnyPreviousState(
            int ageDifference,
            bool useNullHash,
            bool useBadHash,
            bool enqueued)
        {
            const string Filename = "FILENAME";
            const string DataFilePath1 = "/PATH/" + Filename;

            int expectedHash = Utility.GetHashCodeForUnorderedCollection(new[] { Filename });

            ManifestFileSetState getResult = new ManifestFileSetState
            {
                ManifestPath = DataManifestProcessorTests.DataManifestPath,
                AgentId = DataManifestProcessorTests.AgentId,
                Counter = 0,
                DataFileTags = new List<string> { $"{this.data.CosmosTag}.{this.data.AgentId}.{Filename}" },

                DataFileManifestCreateTime = this.mockDataManifest.Object.Created.AddMinutes(ageDifference),
                DataFileManifestHash = useNullHash ? (int?)null : (useBadHash ? expectedHash + 1: expectedHash),

                RequestManifestCreateTime = this.mockReqManifest.Object.Created,
                RequestManifestHash = null,
            };

            TestUtilities.PopulateStreamWithString(Filename + "\n", this.dataManifestContents);
            TestUtilities.PopulateStreamWithString(DataManifestProcessorTests.CommandId + "\n", this.reqManifestContents);

            this.mockManifestState.Setup(o => o.InsertAsync(It.IsAny<ManifestFileSetState>())).ReturnsAsync(false);
            this.mockManifestState.Setup(o => o.GetItemAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(getResult);

            this.mockFileSource
                .Setup(o => o.OpenExistingFileAsync(DataFilePath1))
                .ReturnsAsync(this.mockDataFile.Object);

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(DataManifestProcessorTests.TaskId, "context");

            // verify
            this.mockPendingQueue.Verify(
                o => o.EnqueueAsync(It.IsAny<FileSizePartition>(), It.IsAny<PendingDataFile>(), It.IsAny<CancellationToken>()),
                enqueued ? (Func<Times>)Times.Once : Times.Never);
        }

        [TestMethod]
        [DataRow(0, false, false, true)] // good date, good hash
        [DataRow(0, false, true, false)] // good date, bad hash
        [DataRow(0, true, false, true)]  // good date, missing hash
        [DataRow(1, false, false, true)] // bad date, good hash (good hash overrides bad date)
        [DataRow(1, false, true, false)] // bad date, bad hash
        [DataRow(1, true, false, false)] // bad date, missing hash
        public async Task ProcessorEnqueuesItemsAsAppropriateBasedOnRequestManifestAgeAndHashAndAnyPreviousState(
            int ageDifference,
            bool useNullHash,
            bool useBadHash,
            bool enqueued)
        {
            const string Filename = "FILENAME";
            const string DataFilePath1 = "/PATH/" + Filename;

            int expectedHash = Utility.GetHashCodeForUnorderedCollection(
                new[] { DataManifestProcessorTests.CommandId.ToLowerInvariant() });

            ManifestFileSetState getResult = new ManifestFileSetState
            {
                ManifestPath = DataManifestProcessorTests.DataManifestPath,
                AgentId = DataManifestProcessorTests.AgentId,
                Counter = 0,
                DataFileTags = new List<string> { $"{this.data.CosmosTag}.{this.data.AgentId}.{Filename}" },

                DataFileManifestCreateTime = this.mockDataManifest.Object.Created,
                DataFileManifestHash = null,

                RequestManifestCreateTime = this.mockReqManifest.Object.Created.AddMinutes(ageDifference),
                RequestManifestHash = useNullHash ? (int?)null : (useBadHash ? expectedHash + 1 : expectedHash),
            };

            TestUtilities.PopulateStreamWithString(Filename + "\n", this.dataManifestContents);
            TestUtilities.PopulateStreamWithString(DataManifestProcessorTests.CommandId + "\n", this.reqManifestContents);

            this.mockManifestState.Setup(o => o.InsertAsync(It.IsAny<ManifestFileSetState>())).ReturnsAsync(false);
            this.mockManifestState.Setup(o => o.GetItemAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(getResult);

            this.mockFileSource
                .Setup(o => o.OpenExistingFileAsync(DataFilePath1))
                .ReturnsAsync(this.mockDataFile.Object);

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(DataManifestProcessorTests.TaskId, "context");

            // verify
            this.mockPendingQueue.Verify(
                o => o.EnqueueAsync(It.IsAny<FileSizePartition>(), It.IsAny<PendingDataFile>(), It.IsAny<CancellationToken>()),
                enqueued ? (Func<Times>)Times.Once : Times.Never);
        }

        [TestMethod]
        public async Task ProcessorDoesNotEnqueueItemsIfDataManifestFileListIsNotSupersetOfStateFileSet()
        {
            const string FilenmameManifest = "FILENAME2";
            const string FilenmameState = "FILENAME1";

            ManifestFileSetState getResult = new ManifestFileSetState
            {
                ManifestPath = DataManifestProcessorTests.DataManifestPath,
                AgentId = DataManifestProcessorTests.AgentId,
                Counter = 0,
                DataFileTags = new List<string> { $"{this.data.CosmosTag}.{this.data.AgentId}.{FilenmameState}" },

                DataFileManifestCreateTime = this.mockDataManifest.Object.Created,
                RequestManifestCreateTime = this.mockReqManifest.Object.Created,
            };

            TestUtilities.PopulateStreamWithString(FilenmameManifest + "\n", this.dataManifestContents);
            TestUtilities.PopulateStreamWithString(DataManifestProcessorTests.CommandId + "\n", this.reqManifestContents);

            this.mockManifestState.Setup(o => o.InsertAsync(It.IsAny<ManifestFileSetState>())).ReturnsAsync(false);
            this.mockManifestState.Setup(o => o.GetItemAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(getResult);

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(DataManifestProcessorTests.TaskId, "context");

            // verify
            this.mockPendingQueue.Verify(
                o => o.EnqueueAsync(It.IsAny<FileSizePartition>(), It.IsAny<PendingDataFile>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}

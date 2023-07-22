// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExportWorker.UnitTests.Tasks
{
    using System;
    using System.Collections.Generic;
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

    using Moq;
    using Microsoft.Azure.ComplianceServices.Common;

    [TestClass]
    public class CosmosMonitorTests
    {
        private const string DataManifestName = "DataFileManifest_2018_01_01_00";
        private const string ReqManifestName = "RequestManifest_2018_01_01_00";
        private const string AgentId = "AGENTID";
        private const string TaskId = "TASKID";

        private class TestCosmosInfo : ITaggedCosmosVcConfig
        {
            public IServicePointConfiguration ServicePointConfiguration => null;
            public bool ApplyRelativeBasePath { get; } = true;
            public bool UseDefaultCredentials { get; } = true;
            public string CosmosVcPath { get; } = "ROOT/";
            public string CosmosCertificateSubject { get; } = "subject";
            public string CosmosTag { get; } = "COSMOSTAG";

            public string CosmosAdlsAccountName => throw new NotImplementedException();

            public string RootDir { get; } = "ROOT/";
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

        private class TestConfig : ICosmosMonitorConfig
        {
            public string Tag { get; } = "Tag";
            public string TaskType { get; } = "Type";
            public int InstanceCount { get; } = 1;
            public IList<ITaggedCosmosVcConfig> CosmosVcs { get; } = new List<ITaggedCosmosVcConfig> { new TestCosmosInfo() };
            public int RepeatDelayMinutes { get; } = 1;
            public int MinimumManifestEnqueueIntervalMinutes { get; set; } = 1;
            public int DelayOnExceptionMinutes { get; } = 0;
            public int MaxEnqueueAgeHours { get; } = 720;
            public int DeleteAgeHours { get; } = 1440;
            public int MinBatchAgeMinutes { get; } = 15;
        }

        private readonly Mock<ITable<ManifestFileSetState>> mockFileSetState = new Mock<ITable<ManifestFileSetState>>();
        private readonly Mock<IQueue<ManifestFileSet>> mockQueue = new Mock<IQueue<ManifestFileSet>>();
        private readonly Mock<IFileSystemManager> mockFileSystemMgr = new Mock<IFileSystemManager>();
        private readonly Mock<ICommandDataWriter> mockWriter = new Mock<ICommandDataWriter>();
        private readonly Mock<ICosmosFileSystem> mockFileSource = new Mock<ICosmosFileSystem>();
        private readonly Mock<ICounterFactory> mockCounterFactory = new Mock<ICounterFactory>();
        private readonly Mock<IDirectory> mockDirAgent = new Mock<IDirectory>();
        private readonly Mock<IDirectory> mockDirRoot = new Mock<IDirectory>();
        private readonly Mock<ICounter> mockCounter = new Mock<ICounter>();
        private readonly MockLogger mockLog = new MockLogger();
        private readonly Mock<IClock> mockClock = new Mock<IClock>();
        private readonly Mock<IFile> mockDataManifest = new Mock<IFile>();
        private readonly Mock<IFile> mockReqManifest = new Mock<IFile>();
        private readonly Mock<IFile> mockDataFile = new Mock<IFile>();

        private readonly TestConfig config = new TestConfig();

        private string dataManifestPath;
        private string reqManifestPath;
        private CosmosMonitor testObj;

        [TestInitialize]
        public void Init()
        {
            string basePath;
            string agentId;
            string agentDir;

            this.mockFileSystemMgr.SetupGet(o => o.CosmosPathsAndExpiryTimes).Returns(new TestPaths());

            basePath =
                this.config.CosmosVcs.First().CosmosVcPath +
                this.mockFileSystemMgr.Object.CosmosPathsAndExpiryTimes.BasePath +
                this.mockFileSystemMgr.Object.CosmosPathsAndExpiryTimes.AgentOutput;

            agentId = CosmosMonitorTests.AgentId;
            agentDir = basePath + agentId + "/";

            this.dataManifestPath = agentDir + CosmosMonitorTests.DataManifestName;
            this.reqManifestPath = agentDir + CosmosMonitorTests.ReqManifestName;

            this.mockFileSystemMgr.Setup(o => o.GetFileSystem(It.IsAny<string>())).Returns(this.mockFileSource.Object);
            this.mockFileSource.Setup(o => o.OpenExistingDirectoryAsync(It.IsAny<string>())).ReturnsAsync(this.mockDirRoot.Object);

            this.mockDirRoot
                .Setup(o => o.EnumerateAsync())
                .ReturnsAsync(new[] { this.mockDirAgent.Object });

            this.mockDirAgent
                .Setup(o => o.EnumerateAsync())
                .ReturnsAsync(new[] { this.mockReqManifest.Object, this.mockDataFile.Object, this.mockDataManifest.Object });

            this.mockCounterFactory
                .Setup(o => o.GetCounter(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CounterType>()))
                .Returns(this.mockCounter.Object);

            this.mockWriter.Setup(o => o.CloseAsync()).Returns(Task.CompletedTask);
            this.mockWriter
                .Setup(o => o.WriteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(0);

            this.mockClock.SetupGet(o => o.UtcNow).Returns(new DateTimeOffset(2006, 4, 15, 15, 0, 0, TimeSpan.Zero));

            this.mockDirRoot.SetupGet(o => o.Type).Returns(FileSystemObjectType.Directory);
            this.mockDirRoot.SetupGet(o => o.Path).Returns(basePath);
            this.mockDirRoot.SetupGet(o => o.Name).Returns("OUTPUT");

            this.mockDirAgent.SetupGet(o => o.Type).Returns(FileSystemObjectType.Directory);
            this.mockDirAgent.SetupGet(o => o.Path).Returns(agentDir);
            this.mockDirAgent.SetupGet(o => o.Name).Returns(agentId);

            this.mockDataManifest.SetupGet(o => o.Type).Returns(FileSystemObjectType.File);
            this.mockDataManifest.SetupGet(o => o.Path).Returns(this.dataManifestPath);
            this.mockDataManifest.SetupGet(o => o.Name).Returns(CosmosMonitorTests.DataManifestName);
            this.mockDataManifest
                .SetupGet(o => o.Created)
                .Returns(this.mockClock.Object.UtcNow.AddMinutes(-1 * this.config.MinBatchAgeMinutes));

            this.mockReqManifest.SetupGet(o => o.Type).Returns(FileSystemObjectType.File);
            this.mockReqManifest.SetupGet(o => o.Path).Returns(this.reqManifestPath);
            this.mockReqManifest.SetupGet(o => o.Name).Returns(CosmosMonitorTests.ReqManifestName);
            this.mockReqManifest
                .SetupGet(o => o.Created)
                .Returns(this.mockClock.Object.UtcNow.AddMinutes(-1 * this.config.MinBatchAgeMinutes));

            this.mockDataFile.SetupGet(o => o.Type).Returns(FileSystemObjectType.File);
            this.mockDataFile.SetupGet(o => o.Path).Returns(agentDir + "/DataFile_2018_01_01_00");
            this.mockDataFile.SetupGet(o => o.Name).Returns("DataFile_2018_01_01_00");
            this.mockDataFile.SetupGet(o => o.Created).Returns(this.mockClock.Object.UtcNow);
        }

        private void CreateTestObj()
        {
            this.testObj = new CosmosMonitor(
                this.mockFileSetState.Object,
                this.mockQueue.Object,
                this.config,
                this.mockFileSystemMgr.Object,
                this.mockCounterFactory.Object,
                this.mockLog.Object,
                this.mockClock.Object,
                new AppConfiguration(@"local.settings.json"));
        }

        [TestMethod]
        public async Task MonitorOpensRootAndEnumeratesAgentSubdirectories()
        {
            this.CreateTestObj();

            // test 
            await this.testObj.RunSingleInstanceOnePassAsync(CosmosMonitorTests.TaskId, "context");

            // verify
            this.mockFileSystemMgr.Verify(o => o.GetFileSystem(this.config.CosmosVcs.First().CosmosTag), Times.Once);
            this.mockDirRoot.Verify(o => o.EnumerateAsync(), Times.Once);
        }

        [TestMethod]
        public async Task MonitorEnumeratesFilesInAgentDirsAndEnqueuesThem()
        {
            ManifestFileSetState query =
                new ManifestFileSetState
                {
                    ManifestPath = this.dataManifestPath,
                    AgentId = CosmosMonitorTests.AgentId
                };

            Func<ManifestFileSet, bool> verifier = 
                o =>
                {
                    Assert.AreEqual(CosmosMonitorTests.AgentId, o.AgentId);
                    Assert.AreEqual(this.dataManifestPath, o.DataManifestPath);
                    Assert.AreEqual(this.reqManifestPath, o.RequestManifestPath);
                    Assert.AreEqual(this.config.CosmosVcs.First().CosmosTag, o.CosmosTag);
                    return true;
                };

            this.CreateTestObj();

            // test 
            await this.testObj.RunSingleInstanceOnePassAsync(CosmosMonitorTests.TaskId, "context");

            // verify
            this.mockFileSystemMgr.Verify(o => o.GetFileSystem(this.config.CosmosVcs.First().CosmosTag), Times.Once);
            this.mockFileSetState.Verify(o => o.GetItemAsync(query.PartitionKey, query.RowKey), Times.Once);
            this.mockDirAgent.Verify(o => o.EnumerateAsync(), Times.Once);

            this.mockQueue.Verify(o => o.EnqueueAsync(It.IsAny<ManifestFileSet>(), It.IsAny<CancellationToken>()), Times.Once);

            this.mockQueue.Verify(
                o => o.EnqueueAsync(It.Is<ManifestFileSet>(p => verifier(p)), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task MonitorSkipsManifestsIfManifestStateTableUpdatedRecently()
        {
            ManifestFileSetState query =
                new ManifestFileSetState
                {
                    ManifestPath = this.dataManifestPath,
                    AgentId = CosmosMonitorTests.AgentId,
                    Timestamp = this.mockClock.Object.UtcNow.AddHours(-1)
                };

            this.mockFileSetState.Setup(o => o.GetItemAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(query);

            this.config.MinimumManifestEnqueueIntervalMinutes = 120;

            this.CreateTestObj();

            // test 
            await this.testObj.RunSingleInstanceOnePassAsync(CosmosMonitorTests.TaskId, "context");

            // verify
            this.mockQueue.Verify(o => o.EnqueueAsync(It.IsAny<ManifestFileSet>(), It.IsAny<CancellationToken>()), Times.Never);
        }
        
        [TestMethod]
        public async Task MonitorSkipsManifestsIfManifestFileTooOld()
        {
            this.mockDataManifest
                .SetupGet(o => o.Created)
                .Returns(this.mockClock.Object.UtcNow.AddHours(-1 * (this.config.MaxEnqueueAgeHours + 1)));

            this.CreateTestObj();

            // test 
            await this.testObj.RunSingleInstanceOnePassAsync(CosmosMonitorTests.TaskId, "context");

            // verify
            this.mockQueue.Verify(o => o.EnqueueAsync(It.IsAny<ManifestFileSet>(), It.IsAny<CancellationToken>()), Times.Never);
            this.mockDataManifest.Verify(o => o.DeleteAsync(), Times.Never);
        }

        [TestMethod]
        public async Task MonitorDeletesFilesIfFileTooOld()
        {
            this.mockDataManifest
                .SetupGet(o => o.Created)
                .Returns(this.mockClock.Object.UtcNow.AddHours(-1 * (this.config.DeleteAgeHours + 1)));

            this.CreateTestObj();

            // test 
            await this.testObj.RunSingleInstanceOnePassAsync(CosmosMonitorTests.TaskId, "context");

            // verify
            this.mockQueue.Verify(o => o.EnqueueAsync(It.IsAny<ManifestFileSet>(), It.IsAny<CancellationToken>()), Times.Never);
            this.mockDataManifest.Verify(o => o.DeleteAsync(), Times.Once);
        }

        [TestMethod]
        public async Task MonitorSkipsBatchesWhoseDataFileManifestsAreTooYoung()
        {
            this.mockDataManifest
                .SetupGet(o => o.Created)
                .Returns(this.mockClock.Object.UtcNow.AddMinutes(-1 * (this.config.MinBatchAgeMinutes - 1)));

            this.CreateTestObj();

            // test 
            await this.testObj.RunSingleInstanceOnePassAsync(CosmosMonitorTests.TaskId, "context");

            // verify
            this.mockQueue.Verify(o => o.EnqueueAsync(It.IsAny<ManifestFileSet>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task MonitorSkipsBatchesWhoseRequestManifestsAreTooYoung()
        {
            this.mockReqManifest
                .SetupGet(o => o.Created)
                .Returns(this.mockClock.Object.UtcNow.AddMinutes(-1 * (this.config.MinBatchAgeMinutes - 1)));

            this.CreateTestObj();

            // test 
            await this.testObj.RunSingleInstanceOnePassAsync(CosmosMonitorTests.TaskId, "context");

            // verify
            this.mockQueue.Verify(o => o.EnqueueAsync(It.IsAny<ManifestFileSet>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}

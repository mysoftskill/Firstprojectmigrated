// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExportWorker.UnitTests.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Locks;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Data;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Tasks;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility.Storage;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExportWorker.UnitTests.TestUtility;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class TableRowCleanerTests
    {
        private const string TaskId = "TASKID";

        private class TestConfig : ICleanerConfig
        {
            public StateTable Table { get; set; } = StateTable.ManifestFile;

            public string Tag { get; set; } = "TAG";
            public string TaskType { get; set;} = "TYPE";
            public int InstanceCount { get; set;} = 1;
            public int DelayOnExceptionMinutes { get; set;} = 1;
            
            public int LastModifiedThresholdDays { get; set;} = 60;
            public int NonEmptyBatchDelaySeconds { get; set;} = 5;
            public int EmptyBatchDelaySeconds { get; set;} = 10; 
            public int DelayRandomLimit { get; set;} = 2;
            public int MaxBatchSize { get; set;} = 10;
        }

        private readonly Mock<ITable<BasicTableState>> mockTable = new Mock<ITable<BasicTableState>>();
        private readonly Mock<ICounterFactory> mockCounters = new Mock<ICounterFactory>();
        private readonly Mock<ITableManager> mockTableMgr = new Mock<ITableManager>();
        private readonly Mock<ILockManager> mockLockMgr = new Mock<ILockManager>();
        private readonly Mock<ILockLease> mockLease = new Mock<ILockLease>();
        private readonly Mock<ICounter> mockCounter = new Mock<ICounter>();
        private readonly Mock<IRandom> mockRng = new Mock<IRandom>();
        private readonly Mock<IClock> mockClock = new Mock<IClock>();
        private readonly MockLogger mockLog = new MockLogger();

        private readonly List<BasicTableState> queryResult = new List<BasicTableState>();

        private TableRowCleaner testObj;
        
        [TestInitialize]
        public void Init()
        {
            this.mockLockMgr
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

            this.mockRng.Setup(o => o.Next(It.IsAny<int>(), It.IsAny<int>())).Returns(0);

            this.mockTableMgr.Setup(o => o.GetTable<BasicTableState>(It.IsAny<string>())).Returns(this.mockTable.Object);

            this.mockTable
                .Setup(o => o.QueryAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(this.queryResult);

            this.mockTable.SetupGet(o => o.BatchOperationMaxItemCount).Returns(2);
        }

        private void SetupTestObj(TestConfig config = null)
        {
            this.testObj = new TableRowCleaner(
                config ?? new TestConfig(),
                this.mockTableMgr.Object,
                this.mockLockMgr.Object,
                this.mockCounters.Object,
                this.mockClock.Object,
                this.mockRng.Object,
                this.mockLog);
        }

        [TestMethod]
        public async Task RunOnceReturnsImmediatelyIfTaskLeaseNotAcquired()
        {
            TestConfig cfg = new TestConfig();

            this.mockLockMgr
                .Setup(
                    o => o.AttemptAcquireAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<bool>()))
                .ReturnsAsync((ILockLease)null);

            this.SetupTestObj(cfg);

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(TableRowCleanerTests.TaskId, "context");

            // validate
            this.mockLockMgr.Verify(
                o => o.AttemptAcquireAsync(
                    TableRowCleaner.CleanerLockGroup,
                    cfg.Table.ToString(),
                    TableRowCleanerTests.TaskId,
                    TableRowCleaner.LockDuration,
                    true),
                Times.Once);

            this.mockTableMgr.Verify(o => o.GetTable<BasicTableState>(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task RunOnceAcquiresAndQueriesTableIfLockObtained()
        {
            string expectedQuery;

            TestConfig cfg = new TestConfig();

            expectedQuery =
                "Timestamp lt datetime'" +
                this.mockClock.Object.UtcNow.AddDays(-1 * cfg.LastModifiedThresholdDays).ToString("yyyy-MM-ddTHH:mm:ssZ") +
                "'";

            this.SetupTestObj(cfg);

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(TableRowCleanerTests.TaskId, "context");

            // validate
            this.mockTableMgr.Verify(o => o.GetTable<BasicTableState>(cfg.Table.ToString()), Times.Once);
            this.mockTable.Verify(o => o.QueryAsync(expectedQuery, cfg.MaxBatchSize, TableRowCleaner.ColumnList), Times.Once);
        }

        [TestMethod]
        public async Task RunOnceDeletesABatchForEachPartitionKeyInResult()
        {
            TestConfig cfg = new TestConfig();

            this.queryResult.Add(new BasicTableState { PartitionKey = "P1", RowKey = "R1" });
            this.queryResult.Add(new BasicTableState { PartitionKey = "P2", RowKey = "R2" });

            this.SetupTestObj(cfg);

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(TableRowCleanerTests.TaskId, "context");

            // validate
            this.mockTable.Verify(
                o => o.DeleteBatchAsync(
                    It.Is<ICollection<BasicTableState>>(
                        p => p.Count == 1 && "P1".Equals(p.First().PartitionKey) && "R1".Equals(p.First().RowKey))),
                Times.Once);

            this.mockTable.Verify(
                o => o.DeleteBatchAsync(
                    It.Is<ICollection<BasicTableState>>(
                        p => p.Count == 1 && "P2".Equals(p.First().PartitionKey) && "R2".Equals(p.First().RowKey))),
                Times.Once);

            this.mockTable.Verify(o => o.DeleteBatchAsync(It.IsAny<ICollection<BasicTableState>>()), Times.Exactly(2));
        }

        [TestMethod]
        public async Task RunOnceDeletesItemsForSamePartitionInABatch()
        {
            TestConfig cfg = new TestConfig();

            this.queryResult.Add(new BasicTableState { PartitionKey = "P1", RowKey = "R1" });
            this.queryResult.Add(new BasicTableState { PartitionKey = "P1", RowKey = "R2" });

            this.SetupTestObj(cfg);

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(TableRowCleanerTests.TaskId, "context");

            // validate
            this.mockTable.Verify(
                o => o.DeleteBatchAsync(
                    It.Is<ICollection<BasicTableState>>(
                        p => p.Count == 2 && 
                             p.Any(item => "P1".Equals(item.PartitionKey) && "R1".Equals(item.RowKey)) &&
                             p.Any(item => "P1".Equals(item.PartitionKey) && "R2".Equals(item.RowKey)))),
                Times.Once);

            this.mockTable.Verify(o => o.DeleteBatchAsync(It.IsAny<ICollection<BasicTableState>>()), Times.Exactly(1));
        }

        [TestMethod]
        public async Task RunOnceBreaksBatchesIntoMultipleCallsIfTheNumberExceedsTheTablesMaxBatchSize()
        {
            TestConfig cfg = new TestConfig();

            this.queryResult.Add(new BasicTableState { PartitionKey = "P1", RowKey = "R1" });
            this.queryResult.Add(new BasicTableState { PartitionKey = "P1", RowKey = "R2" });
            this.queryResult.Add(new BasicTableState { PartitionKey = "P1", RowKey = "R3" });

            this.SetupTestObj(cfg);

            this.mockTable.SetupGet(o => o.BatchOperationMaxItemCount).Returns(2);

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(TableRowCleanerTests.TaskId, "context");

            // validate
            this.mockTable.Verify(
                o => o.DeleteBatchAsync(
                    It.Is<ICollection<BasicTableState>>(
                        p => p.Count == 2 &&
                             p.Any(item => "P1".Equals(item.PartitionKey) && "R1".Equals(item.RowKey)) &&
                             p.Any(item => "P1".Equals(item.PartitionKey) && "R2".Equals(item.RowKey)))),
                Times.Once);

            this.mockTable.Verify(
                o => o.DeleteBatchAsync(
                    It.Is<ICollection<BasicTableState>>(
                        p => p.Count == 1 && "P1".Equals(p.First().PartitionKey) && "R3".Equals(p.First().RowKey))),
                Times.Once);

            this.mockTable.Verify(o => o.DeleteBatchAsync(It.IsAny<ICollection<BasicTableState>>()), Times.Exactly(2));
        }
    }
}

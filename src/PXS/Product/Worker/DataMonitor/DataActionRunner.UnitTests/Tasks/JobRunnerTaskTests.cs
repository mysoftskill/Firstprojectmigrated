// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.Runner.UnitTests.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Locks;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.Exceptions;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Data;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Storage;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Tasks;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Utility;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Azure.Storage.RetryPolicies;
    using Moq;

    [TestClass]
    public class JobRunnerTaskTests
    {
        private class JobRunnerTaskTestsException : Exception {  }

        private class Config : IDataActionJobRunnerConfig
        {
            public string Tag { get; set; }
            public string TaskType { get; set; }
            public int InstanceCount { get; set; }
            public int DelayOnExceptionMinutes { get; set; }
            public string LockTableName { get; set; }
            public int MaxDequeueCount { get; set; }
            public int DelayIfCouldNotCompleteMinutes { get; set; }
            public int LeaseMinutes { get; set; }
            public int DelayOnEmptyQueueSeconds { get; set; }
            public bool ForceVerboseLogOnSuccess { get; set; }
            public bool ForceSimulationMode { get; set; }
        }

        private const string TaskId = "TASKID";

        private readonly Mock<IQueueItem<JobWorkItem>> mockQItem = new Mock<IQueueItem<JobWorkItem>>();
        private readonly Mock<IQueue<JobWorkItem>> mockQ = new Mock<IQueue<JobWorkItem>>();
        private readonly Mock<ICounterFactory> mockCounterFact = new Mock<ICounterFactory>();
        private readonly Mock<IActionExecutor> mockExecutor = new Mock<IActionExecutor>();
        private readonly Mock<IContextFactory> mockCtxFact = new Mock<IContextFactory>();
        private readonly Mock<IExecuteContext> mockCtx = new Mock<IExecuteContext>();
        private readonly Mock<ILockManager> mockLogMgr = new Mock<ILockManager>();
        private readonly Mock<ILockLease> mockLease = new Mock<ILockLease>();
        private readonly Mock<ICounter> mockCtr = new Mock<ICounter>();
        private readonly ILogger mockLog = new MockLogger();

        private readonly Config config = new Config
        {
            Tag = "Tag",
            TaskType = "TaskType",
            InstanceCount = 1,
            DelayOnExceptionMinutes = 1,
            LockTableName = "LockTable",
            MaxDequeueCount = 100,
            DelayIfCouldNotCompleteMinutes = 1,
            ForceVerboseLogOnSuccess = false,
            LeaseMinutes = 1,
        };

        private readonly ActionRefRunnable aref = new ActionRefRunnable
        {
            Tag = "tag1",
            MaxRuntime = TimeSpan.FromDays(1),
            Id = "lock1"
        };

        private readonly IDictionary<string, IDictionary<string, string>> extProps = 
            new Dictionary<string, IDictionary<string, string>>();

        private JobWorkItem data;

        private JobRunnerTask testObj;

        [TestInitialize]
        public void TestInit()
        {
            this.data = new JobWorkItem(this.mockExecutor.Object, this.aref, this.extProps);

            this.mockCtxFact
                .Setup(
                    o => o.Create<IExecuteContext>(
                        It.IsAny<CancellationToken>(), 
                        It.IsAny<bool>(),
                        It.IsAny<IDictionary<string, IDictionary<string, string>>>(),
                        It.IsAny<string>()))
                .Returns(this.mockCtx.Object);

            this.mockCtxFact.Setup(o => o.Create<IExecuteContext>(It.IsAny<string>())).Returns(this.mockCtx.Object);

            this.mockLogMgr
                .Setup(
                    o => o.AttemptAcquireAsync(
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(this.mockLease.Object);

            this.mockLogMgr
                .Setup(
                    o => o.AttemptAcquireAsync(
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<bool>()))
                .ReturnsAsync(this.mockLease.Object);

            this.mockCtx.Setup(o => o.GetLogs(It.IsAny<EntryTypes>())).Returns(string.Empty);

            this.mockQ
                .Setup(
                    o => o.DequeueAsync(
                        It.IsAny<TimeSpan>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<IRetryPolicy>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockQItem.Object);

            this.mockQItem.SetupGet(o => o.Data).Returns(this.data);

            this.mockCounterFact
                .Setup(o => o.GetCounter(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CounterType>()))
                .Returns(this.mockCtr.Object);
        }

        private void CreateTestObj()
        {
            this.testObj = new JobRunnerTask(
                this.config,
                this.mockQ.Object,
                this.mockCounterFact.Object,
                this.mockCtxFact.Object,
                this.mockLogMgr.Object,
                this.mockLog);
        }

        [TestMethod]
        public async Task ProcessorReturnsImmediatelyWithoutAcquiringALockIfDequeueReturnsNull()
        {
            this.mockQ
                .Setup(
                    o => o.DequeueAsync(
                        It.IsAny<TimeSpan>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<IRetryPolicy>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync((IQueueItem<JobWorkItem>)null);

            this.CreateTestObj();

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(JobRunnerTaskTests.TaskId, "context");

            // verify
            this.mockQ.Verify(
                o => o.DequeueAsync(
                    TimeSpan.FromMinutes(this.config.LeaseMinutes),
                    TimeSpan.MaxValue,
                    It.IsAny<IRetryPolicy>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.mockLogMgr.Verify(
                o => o.AttemptAcquireAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()),
                Times.Never);
        }

        [TestMethod]
        public async Task ProcessorReturnsImmediatelyAcquiringLockIfDequeueCountTooHigh()
        {
            this.mockQItem.SetupGet(o => o.DequeueCount).Returns(() => this.config.MaxDequeueCount + 1);

            this.CreateTestObj();

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(JobRunnerTaskTests.TaskId, "context");

            // verify
            this.mockQ.Verify(
                o => o.DequeueAsync(
                    TimeSpan.FromMinutes(this.config.LeaseMinutes),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<IRetryPolicy>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.mockLogMgr.Verify(
                o => o.AttemptAcquireAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<bool>()),
                Times.Never);
        }

        [TestMethod]
        public async Task ProcessorReturnsImmediatelyWithoutExecutingActionIfLockCantBeAcquired()
        {
            this.mockLogMgr
                .Setup(
                    o => o.AttemptAcquireAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<bool>()))
                .ReturnsAsync((ILockLease)null);

            this.CreateTestObj();

            // test
            await this.testObj.RunSingleInstanceOnePassAsync(JobRunnerTaskTests.TaskId, "context");

            // verify
            this.mockQ.Verify(
                o => o.DequeueAsync(
                    TimeSpan.FromMinutes(this.config.LeaseMinutes),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<IRetryPolicy>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.mockLogMgr.Verify(
                o => o.AttemptAcquireAsync(
                    Constants.JobRunnerLockGroup,
                    this.data.RefId,
                    JobRunnerTaskTests.TaskId,
                    this.data.TaskLeaseTime,
                    false),
                Times.Once);

            this.mockExecutor.Verify(
                o => o.ExecuteActionAsync(It.IsAny<IExecuteContext>(), It.IsAny<ActionRef>()), 
                Times.Never);

            this.mockQItem.Verify(o => o.CompleteAsync(), Times.Once);
        }

        [TestMethod]
        [DataRow(false, false, false, EntryTypes.Title)]
        [DataRow(false, false, true, EntryTypes.All)]
        [DataRow(true, false, false, EntryTypes.All)]
        [DataRow(true, true, false, EntryTypes.All)]
        public async Task ProcessorAttemptsToExecuteActionIfLockObtained(
            bool hasErrors,
            bool throws,
            bool logVerbose,
            EntryTypes expectedLogsType)
        {
            object expected = new object();

            if (throws)
            {
                hasErrors = true;

                this.mockExecutor
                    .Setup(o => o.ExecuteActionAsync(It.IsAny<IExecuteContext>(), It.IsAny<ActionRef>()))
                    .Returns(Task.FromException<object>(new JobRunnerTaskTestsException()));
            }
            else
            {
                this.mockCtx.SetupGet(o => o.HasErrors).Returns(hasErrors);
                this.mockExecutor
                    .Setup(o => o.ExecuteActionAsync(It.IsAny<IExecuteContext>(), It.IsAny<ActionRef>()))
                    .ReturnsAsync(expected);
            }

            this.config.ForceVerboseLogOnSuccess = logVerbose;

            this.CreateTestObj();

            // test
            try
            {
                await this.testObj.RunSingleInstanceOnePassAsync(JobRunnerTaskTests.TaskId, "context");
                Assert.IsFalse(hasErrors, "Should have thrown exception when context reports errors present");
            }
            catch (ActionExecuteException)
            {
                Assert.IsTrue(hasErrors, "Should not have thrown exception when context reports no errors");
            }
            catch(JobRunnerTaskTestsException)
            {

            }

            // verify
            this.mockQ.Verify(
                o => o.DequeueAsync(
                    TimeSpan.FromMinutes(this.config.LeaseMinutes),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<IRetryPolicy>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.mockLogMgr.Verify(
                o => o.AttemptAcquireAsync(
                    Constants.JobRunnerLockGroup,
                    this.data.RefId,
                    JobRunnerTaskTests.TaskId,
                    this.data.TaskLeaseTime,
                    false),
                Times.Once);

            this.mockCtxFact.Verify(
                o => o.Create<IExecuteContext>(
                    It.IsAny<CancellationToken>(),
                    this.aref.IsSimulation,
                    this.extProps,
                    It.IsAny<string>()),
                Times.Once);

            this.mockLease.Verify(o => o.ReleaseAsync(false), Times.Once);

            this.mockExecutor.Verify(
                o => o.ExecuteActionAsync(this.mockCtx.Object, this.data.ActionRef), 
                Times.Once);

            if (hasErrors)
            {
                this.mockQItem.Verify(
                    o => o.RenewLeaseAsync(TimeSpan.FromMinutes(this.config.DelayIfCouldNotCompleteMinutes)), 
                    Times.Once);
            }
            else
            {
                this.mockQItem.Verify(o => o.CompleteAsync(), Times.Once);
            }

            this.mockCtx.Verify(o => o.GetLogs(expectedLogsType), Times.Once);
        }
    }
}

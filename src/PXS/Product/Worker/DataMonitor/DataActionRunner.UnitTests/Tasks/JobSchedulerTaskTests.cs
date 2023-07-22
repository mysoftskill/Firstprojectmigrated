// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.Runner.UnitTests.Tasks
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.Exceptions;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Data;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Storage;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class JobSchedulerTaskTests
    {
        private const string TaskId = "TASKID";

        private readonly Mock<IDataActionJobSchedulerConfig> mockCfg = new Mock<IDataActionJobSchedulerConfig>();
        private readonly Mock<IActionManagerFactory> mockMgrFact = new Mock<IActionManagerFactory>();
        private readonly Mock<IQueue<JobWorkItem>> mockQ = new Mock<IQueue<JobWorkItem>>();
        private readonly Mock<ICounterFactory> mockCounterFact = new Mock<ICounterFactory>();
        private readonly Mock<IActionAccessor> mockAccessor = new Mock<IActionAccessor>();
        private readonly Mock<IContextFactory> mockCtxFact = new Mock<IContextFactory>();
        private readonly Mock<IParseContext> mockCtx = new Mock<IParseContext>();
        private readonly Mock<ICounter> mockCtr = new Mock<ICounter>();
        private readonly ILogger mockLog = new MockLogger();
        private readonly Mock<IAppConfiguration> mockAppConfig = new Mock<IAppConfiguration>();

        private JobSchedulerTask testObj;

        [TestInitialize]
        public void TestInit()
        {
            Sll.ResetContext();
            this.mockMgrFact.Setup(o => o.CreateStoreManager()).Returns(this.mockAccessor.Object);
            this.mockCtxFact.Setup(o => o.Create<IParseContext>(It.IsAny<string>())).Returns(this.mockCtx.Object);

            this.mockCtx.Setup(o => o.GetLogs(It.IsAny<EntryTypes>())).Returns(string.Empty);

            this.mockCfg.SetupGet(o => o.RunFrequencySeconds).Returns(10);

            this.mockCounterFact
                .Setup(o => o.GetCounter(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CounterType>()))
                .Returns(this.mockCtr.Object);

            this.mockAppConfig
                .Setup(o => o.GetConfigValue<bool>(ConfigNames.PXS.DataActionRunner_EnableJobScheduler, It.IsAny<bool>()))
                .Returns(true);
        }
        
        [TestCleanup]
        public void TestCleanup()
        {
            Sll.ResetContext();
        }

        private void CreateTestObj()
        {
            this.testObj = new JobSchedulerTask(
                this.mockCfg.Object,
                this.mockMgrFact.Object,
                this.mockQ.Object,
                this.mockCtxFact.Object,
                this.mockCounterFact.Object,
                this.mockLog,
                this.mockAppConfig.Object);
        }

        [TestMethod]
        [DataRow(false, false, EntryTypes.Title)]
        [DataRow(false, true, EntryTypes.All)]
        [DataRow(true, false, EntryTypes.All)]
        public async Task RunOnceReadsStoreAndEnqueuesActionsAndFetchesAppropriateLogs(
            bool hasErrors,
            bool logVerbose,
            EntryTypes expectedLogsType)
        {
            this.CreateTestObj();

            this.mockCtx.SetupGet(o => o.HasErrors).Returns(hasErrors);
            this.mockCfg.SetupGet(o => o.ForceVerboseLogOnSuccess).Returns(logVerbose);

            // test 
            try
            {
                await this.testObj.RunSingleInstanceOnePassAsync(JobSchedulerTaskTests.TaskId, "context");
                Assert.IsFalse(hasErrors, "Should have thrown exception when context reports errors present");
            }
            catch (ActionParseException)
            {
                Assert.IsTrue(hasErrors, "Should not have thrown exception when context reports no errors");
            }

            // verify
            this.mockCtx.Verify(o => o.GetLogs(expectedLogsType), Times.Once);

            this.mockAccessor.Verify(o => o.InitializeAndRetrieveActionsAsync(this.mockCtx.Object, false), Times.Once);

            if (hasErrors == false)
            {
                this.mockAccessor.Verify(
                    o => o.EnqueueActionsToExecuteAsync(this.mockQ.Object, It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }
    }
}

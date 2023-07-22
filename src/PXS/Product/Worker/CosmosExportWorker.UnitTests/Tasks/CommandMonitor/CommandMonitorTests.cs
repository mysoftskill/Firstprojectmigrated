// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExportWorker.UnitTests.Tasks.CommandMonitor
{
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Tasks;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExportWorker.UnitTests.TestUtility;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class CommandMonitorTests
    {
        private readonly Mock<ICommandObjectFactory> mockFactory = new Mock<ICommandObjectFactory>();
        private readonly Mock<ICommandMonitorConfig> mockCfg = new Mock<ICommandMonitorConfig>();
        private readonly Mock<ICounterFactory> mockCounters = new Mock<ICounterFactory>();
        private readonly Mock<ICommandReceiver> mockReveiver = new Mock<ICommandReceiver>();
        private readonly MockLogger mockLog = new MockLogger();

        private CommandMonitor testObj;

        [TestInitialize]
        public void Init()
        {
            this.testObj = new CommandMonitor(
                this.mockFactory.Object, 
                this.mockCfg.Object, 
                this.mockCounters.Object, 
                this.mockLog.Object);

            this.mockFactory.Setup(o => o.CreateCommandReceiver(It.IsAny<string>())).Returns(this.mockReveiver.Object);
            this.mockReveiver.Setup(o => o.BeginReceivingAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        }

        [TestMethod]
        public async Task RunOnceCreatesReceiverAndInvokesIt()
        {
            const string TaskId = "tid";
            
            // test
            await this.testObj.RunSingleInstanceOnePassAsync(TaskId, "tid2");

            // verify
            this.mockFactory.Verify(o => o.CreateCommandReceiver(TaskId), Times.Once);
            this.mockReveiver.Verify(o => o.BeginReceivingAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

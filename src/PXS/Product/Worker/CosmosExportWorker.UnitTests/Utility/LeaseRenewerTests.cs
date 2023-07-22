// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExportWorker.UnitTests.Utility
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    public interface IExecLocal
    {
        Task<bool> ExecAsync();
        void Log(string s);
    }

    [TestClass]
    public class LeaseRenewerTests
    {
        private readonly Mock<IExecLocal> mockExec = new Mock<IExecLocal>();
        private readonly Mock<IClock> mockClock = new Mock<IClock>();

        private readonly TimeSpan freq = TimeSpan.FromSeconds(1);

        private LeaseRenewer testObj;

        [TestInitialize]
        public void Init()
        {
            this.mockClock.Setup(o => o.UtcNow).Returns(new DateTimeOffset(2006, 4, 15, 15, 0, 0, TimeSpan.Zero));

            this.mockExec.Setup(o => o.ExecAsync()).ReturnsAsync(true);
            
            this.testObj = new LeaseRenewer(
                new Func<Task<bool>>[] { () => this.mockExec.Object.ExecAsync() },
                this.freq,
                this.mockClock.Object,
                this.mockExec.Object.Log,
                "tag");
        }

        [TestMethod]
        public async Task RenewDoesNothingIfInsufficientTimeSinceLastRenew()
        {
            // test
            await this.testObj.RenewAsync();

            // verify
            this.mockExec.Verify(o => o.ExecAsync(), Times.Never);
        }

        [TestMethod]
        public async Task RenewAttemptsRenewIfSufficientTimePassedSinceLastRenew()
        {
            DateTimeOffset now = this.mockClock.Object.UtcNow.Add(this.freq.Add(TimeSpan.FromMinutes(1)));
            this.mockClock.Setup(o => o.UtcNow).Returns(now);

            // test
            await this.testObj.RenewAsync();

            // verify
            this.mockExec.Verify(o => o.ExecAsync(), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(LeaseLostException))]
        public async Task RenewThrowsIfRenewAttemptedButReturnsFalse()
        {
            DateTimeOffset now = this.mockClock.Object.UtcNow.Add(this.freq.Add(TimeSpan.FromMinutes(1)));
            this.mockClock.Setup(o => o.UtcNow).Returns(now);

            this.mockExec.Setup(o => o.ExecAsync()).ReturnsAsync(false);

            // test
            await this.testObj.RenewAsync();
        }
    }
}

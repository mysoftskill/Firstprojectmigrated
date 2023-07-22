// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.CosmosHelpers
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.Common.Cosmos;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using ILogger = PrivacyServices.Common.Azure.ILogger;

    /// <summary>
    ///     Client to interact with Cosmos using their Scope API and retries on failure
    /// </summary>
    [TestClass]
    public class CosmosClientRetryTests
    {
        private readonly Mock<IRetryStrategyConfiguration> mockCfg = new Mock<IRetryStrategyConfiguration>();
        private readonly Mock<ICosmosClient> mockInner = new Mock<ICosmosClient>();
        private readonly Mock<ILogger> mockLogger = new Mock<ILogger>();

        private class CosmosClientRetryTestException : Exception { };

        private class ConfigInterval : IIncrementIntervalRetryConfiguration
        {
            public uint RetryCount { get; set; } = 1;
            public ulong InitialIntervalInMilliseconds { get; set; } = 1;
            public ulong IntervalIncrementInMilliseconds { get; set; } = 1;
        }

        private readonly ConfigInterval cfg = new ConfigInterval();

        private CosmosClientRetry testObj;

        [TestInitialize]
        public void Init()
        {
            this.mockCfg.SetupGet(o => o.RetryMode).Returns(RetryMode.IncrementInterval);
            this.mockCfg.SetupGet(o => o.IncrementIntervalRetryConfiguration).Returns(this.cfg);

            this.mockInner.Setup(o => o.CreateAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

            this.testObj = new CosmosClientRetry(this.mockCfg.Object, this.mockLogger.Object, this.mockInner.Object);
        }

        [TestMethod]
        public async Task CreateCallsInnerMethodOnceIfNoFailure()
        {
            const string Stream = "STR";

            // test 
            await this.testObj.CreateAsync(Stream);

            // verify
            this.mockInner.Verify(o => o.CreateAsync(Stream), Times.Once);
        }

        [TestMethod]
        public async Task CreateCallsInnerMethodTwiceButDoesNotFailIfSecondAttemptSucceeds()
        {
            const string Stream = "STR";

            this.mockInner
                .SetupSequence(o => o.CreateAsync(It.IsAny<string>()))
                .Returns(Task.FromException(new CosmosClientRetryTestException()))
                .Returns(Task.CompletedTask);

            // test 
            await this.testObj.CreateAsync(Stream);

            // verify
            this.mockInner.Verify(o => o.CreateAsync(Stream), Times.Exactly(2));
        }

        [TestMethod]
        [ExpectedException(typeof(CosmosClientRetryTestException))]
        public async Task CreateCallsInnerMethodTwiceButDoesFailsIfSecondAttemptFails()
        {
            const string Stream = "STR";

            this.mockInner
                .SetupSequence(o => o.CreateAsync(It.IsAny<string>()))
                .Returns(Task.FromException(new CosmosClientRetryTestException()))
                .Returns(Task.FromException(new CosmosClientRetryTestException()))
                .Returns(Task.CompletedTask);

            // test 
            await this.testObj.CreateAsync(Stream);

            // verify
            this.mockInner.Verify(o => o.CreateAsync(Stream), Times.Exactly(2));
        }

        [TestMethod]
        public async Task CreateCallsInnerMethodOnceIfNoFailureAndNoRetryStrategy()
        {
            const string Stream = "STR";

            this.testObj = new CosmosClientRetry(null, this.mockLogger.Object, this.mockInner.Object);

            // test 
            await this.testObj.CreateAsync(Stream);

            // verify
            this.mockInner.Verify(o => o.CreateAsync(Stream), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(CosmosClientRetryTestException))]
        public async Task CreateCallsInnerMethodOnceAndThrowsIfFailureAndNoRetryStrategy()
        {
            const string Stream = "STR";

            this.testObj = new CosmosClientRetry(null, this.mockLogger.Object, this.mockInner.Object);

            this.mockInner
                .SetupSequence(o => o.CreateAsync(It.IsAny<string>()))
                .Returns(Task.FromException(new CosmosClientRetryTestException()))
                .Returns(Task.CompletedTask);

            // test 
            await this.testObj.CreateAsync(Stream);

            // verify
            this.mockInner.Verify(o => o.CreateAsync(Stream), Times.Once);
        }
    }
}

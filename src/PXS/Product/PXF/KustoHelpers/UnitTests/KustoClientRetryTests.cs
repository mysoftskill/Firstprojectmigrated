// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.PrivacyServices.KustoHelpers.UnitTests
{
    using System;
    using System.Data;
    using System.Threading.Tasks;

    using Kusto.Data.Common;

    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.PrivacyServices.KustoHelpers;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    /// <summary>
    ///     Client to interact with Cosmos using their Scope API and retries on failure
    /// </summary>
    [TestClass]
    public class KustoClientRetryTests
    {
        private readonly Mock<IRetryStrategyConfiguration> mockCfg = new Mock<IRetryStrategyConfiguration>();
        private readonly Mock<ICslQueryProvider> mockInner = new Mock<ICslQueryProvider>();
        private readonly Mock<IDataReader> mockReader = new Mock<IDataReader>();
        private readonly Mock<ILogger> mockLogger = new Mock<ILogger>();

        private class KustoClientRetryTestException : Exception { };

        private class ConfigInterval : IIncrementIntervalRetryConfiguration
        {
            public uint RetryCount { get; set; } = 1;
            public ulong InitialIntervalInMilliseconds { get; set; } = 1;
            public ulong IntervalIncrementInMilliseconds { get; set; } = 1;
        }

        private readonly ConfigInterval cfg = new ConfigInterval();

        private KustoClientRetry testObj;

        [TestInitialize]
        public void Init()
        {
            this.mockCfg.SetupGet(o => o.RetryMode).Returns(RetryMode.IncrementInterval);
            this.mockCfg.SetupGet(o => o.IncrementIntervalRetryConfiguration).Returns(this.cfg);

            this.mockInner
                .Setup(o => o.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ClientRequestProperties>()))
                .ReturnsAsync(this.mockReader.Object);

            this.testObj = new KustoClientRetry(this.mockCfg.Object, this.mockInner.Object, this.mockLogger.Object);
        }

        [TestMethod]
        public async Task CreateCallsInnerMethodOnceIfNoFailure()
        {
            const string Query = "QUERY";
            const string Name = "NAME";

            ClientRequestProperties props = new ClientRequestProperties();

            // test 
            await this.testObj.ExecuteQueryAsync(Name, Query, props);

            // verify
            this.mockInner.Verify(o => o.ExecuteQueryAsync(Name, Query, props), Times.Once);
        }

        [TestMethod]
        public async Task CreateCallsInnerMethodTwiceButDoesNotFailIfSecondAttemptSucceeds()
        {
            const string Query = "QUERY";
            const string Name = "NAME";

            ClientRequestProperties props = new ClientRequestProperties();

            this.mockInner
                .SetupSequence(
                    o => o.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ClientRequestProperties>()))
                .Returns(Task.FromException<IDataReader>(new KustoClientRetryTestException()))
                .Returns(Task.FromResult<IDataReader>(null));

            // test 
            await this.testObj.ExecuteQueryAsync(Name, Query, props);

            // verify
            this.mockInner.Verify(o => o.ExecuteQueryAsync(Name, Query, props), Times.Exactly(2));
        }

        [TestMethod]
        [ExpectedException(typeof(KustoClientRetryTestException))]
        public async Task CreateCallsInnerMethodTwiceButDoesFailsIfSecondAttemptFails()
        {
            const string Query = "QUERY";
            const string Name = "NAME";

            ClientRequestProperties props = new ClientRequestProperties();

            this.mockInner
                .SetupSequence(
                    o => o.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ClientRequestProperties>()))
                .Returns(Task.FromException<IDataReader>(new KustoClientRetryTestException()))
                .Returns(Task.FromException<IDataReader>(new KustoClientRetryTestException()))
                .Returns(Task.FromResult<IDataReader>(null));

            // test 
            await this.testObj.ExecuteQueryAsync(Name, Query, props);

            // verify
            this.mockInner.Verify(o => o.ExecuteQueryAsync(Name, Query, props), Times.Exactly(2));
        }

        [TestMethod]
        public async Task CreateCallsInnerMethodOnceIfNoFailureAndNoRetryStrategy()
        {
            const string Query = "QUERY";
            const string Name = "NAME";

            ClientRequestProperties props = new ClientRequestProperties();

            this.testObj = new KustoClientRetry(null, this.mockInner.Object, this.mockLogger.Object);

            // test 
            await this.testObj.ExecuteQueryAsync(Name, Query, props);

            // verify
            this.mockInner.Verify(o => o.ExecuteQueryAsync(Name, Query, props), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(KustoClientRetryTestException))]
        public async Task CreateCallsInnerMethodOnceAndThrowsIfFailureAndNoRetryStrategy()
        {
            const string Query = "QUERY";
            const string Name = "NAME";

            ClientRequestProperties props = new ClientRequestProperties();

            this.testObj = new KustoClientRetry(null, this.mockInner.Object, this.mockLogger.Object);

            this.mockInner
                .SetupSequence(
                    o => o.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ClientRequestProperties>()))
                .Returns(Task.FromException<IDataReader>(new KustoClientRetryTestException()))
                .Returns(Task.FromResult<IDataReader>(null));

            // test 
            await this.testObj.ExecuteQueryAsync(Name, Query, props);

            // verify
            this.mockInner.Verify(o => o.ExecuteQueryAsync(Name, Query, props), Times.Once);
        }
    }
}

// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.UnitTests.AzureQueueProcessor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.AzureQueueProcessor;
    using Microsoft.Membership.MemberServices.Privacy.Core.AQS;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class AccountDeleteQueueWriterTests
    {
        private Mock<IMsaAccountDeleteQueue> queue;

        private Mock<ICounterFactory> counterFactory;

        private ILogger logger;

        [TestInitialize]
        public void Init()
        {
            this.queue = new Mock<IMsaAccountDeleteQueue>(MockBehavior.Strict);
            this.queue
                .Setup(c => c.EnqueueAsync(It.IsAny<IEnumerable<AccountDeleteInformation>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            this.counterFactory = new Mock<ICounterFactory>(MockBehavior.Loose);
            this.counterFactory
                .Setup(c => c.GetCounter(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CounterType>()))
                .Returns(new Mock<ICounter>(MockBehavior.Loose).Object);
            this.logger = new ConsoleLogger();
        }

        [TestMethod]
        public async Task WriteDeleteAsyncSuccess()
        {
            // Arrange
            var writer = new AccountDeleteQueueWriter(this.queue.Object, this.counterFactory.Object, this.logger);

            // Act
            AdapterResponse<AccountDeleteInformation> result = await writer.WriteDeleteAsync(new AccountDeleteInformation() { Puid = 42 }, null).ConfigureAwait(false);

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(42, result.Result.Puid);
        }

        [TestMethod]
        public async Task WriteDeleteAsyncFailureToWrite()
        {
            // Arrange
            this.queue
                .Setup(c => c.EnqueueAsync(It.IsAny<IEnumerable<AccountDeleteInformation>>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception());
            var writer = new AccountDeleteQueueWriter(this.queue.Object, this.counterFactory.Object, this.logger);

            // Act
            AdapterResponse<AccountDeleteInformation> result = await writer.WriteDeleteAsync(new AccountDeleteInformation() { Puid = 42 }, null).ConfigureAwait(false);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            StringAssert.StartsWith(result.Error.Message, "Write to azure queue failed for");
        }

        [TestMethod]
        public async Task WriteDeletesAsyncSuccess()
        {
            // Arrange
            var writer = new AccountDeleteQueueWriter(this.queue.Object, this.counterFactory.Object, this.logger);

            // Act
            var results = await writer
                .WriteDeletesAsync(
                    new[]
                    {
                        new AccountDeleteInformation() { Puid = 42, Reason = AccountCloseReason.UserAccountAgedOut },
                        new AccountDeleteInformation() { Puid = 55, Reason = AccountCloseReason.UserAccountClosed }
                    },
                    null).ConfigureAwait(false);

            // Assert
            Assert.IsTrue(results.IsSuccess);
            Assert.AreEqual(2, results.Result.ToList().Count);
        }

        [TestMethod]
        public async Task WriteDeletesAsyncFailureToWrite()
        {
            // Arrange
            this.queue
                .Setup(c => c.EnqueueAsync(It.IsAny<IEnumerable<AccountDeleteInformation>>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception());
            var writer = new AccountDeleteQueueWriter(this.queue.Object, this.counterFactory.Object, this.logger);

            // Act
            var results = await writer
                .WriteDeletesAsync(
                    new[]
                    {
                        new AccountDeleteInformation() { Puid = 42, Reason = AccountCloseReason.UserAccountAgedOut },
                        new AccountDeleteInformation() { Puid = 55, Reason = AccountCloseReason.UserAccountClosed }
                    },
                    null).ConfigureAwait(false);

            // Assert
            Assert.IsFalse(results.IsSuccess);
            StringAssert.StartsWith(results.Error.Message, "Write to azure queue failed for");
        }
    }
}

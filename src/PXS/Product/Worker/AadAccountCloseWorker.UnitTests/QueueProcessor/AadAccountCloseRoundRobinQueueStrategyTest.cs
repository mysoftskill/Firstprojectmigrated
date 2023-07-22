// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.UnitTests.QueueProcessor
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.QueueProcessor;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    /// <summary>
    ///     AadAccountCloseRoundRobinQueueStrategyTest
    /// </summary>
    [TestClass]
    public class AadAccountCloseRoundRobinQueueStrategyTest
    {
        private readonly IList<IQueue<AccountCloseRequest>> queues = new List<IQueue<AccountCloseRequest>>();

        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowArgumentNullExceptionOnNull()
        {
            var strategy = new AadAccountCloseRoundRobinQueueStrategy(null);
        }

        [TestMethod]
        public void ShouldAllowEmptyList()
        {
            var strategy = new AadAccountCloseRoundRobinQueueStrategy(new List<IQueue<AccountCloseRequest>>());
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(3)]
        [DataRow(50)]
        public void ShouldGetAllQueues(int queueCount)
        {
            var mockQueue = new Mock<IQueue<AccountCloseRequest>>(MockBehavior.Strict);
            for (int i = 0; i < queueCount; i++)
            {
                this.queues.Add(mockQueue.Object);
            }

            var strategy = new AadAccountCloseRoundRobinQueueStrategy(this.queues);

            IList<IQueue<AccountCloseRequest>> allQueues = strategy.GetAllQueues();

            Assert.AreEqual(this.queues.Count, allQueues.Count);
        }

        [TestMethod]
        [DataRow(5, true)]
        [DataRow(1, true)]
        [DataRow(0, false)]
        public void ShouldGetNextQueue(int queueCount, bool expectedResult)
        {
            for (int i = 0; i < queueCount; i++)
            {
                var mockQueue = new Mock<IQueue<AccountCloseRequest>>(MockBehavior.Strict);
                this.queues.Add(mockQueue.Object);
            }

            var strategy = new AadAccountCloseRoundRobinQueueStrategy(this.queues);

            bool result = strategy.TryGetNextQueueAndRemove(out IQueue<AccountCloseRequest> queue);

            Assert.AreEqual(expectedResult, result);

            if (expectedResult)
            {
                Assert.IsNotNull(queue);
            }
            else
            {
                Assert.IsNull(queue);
            }
        }

        [TestMethod]
        [DataRow(5, true, 4)]
        [DataRow(1, true, 0)]
        [DataRow(0, false, 0)]
        public void ShouldGetNextQueueAndRemove(int queueCount, bool queueFound, int remainingQueueCount)
        {
            for (int i = 0; i < queueCount; i++)
            {
                var mockQueue = new Mock<IQueue<AccountCloseRequest>>(MockBehavior.Strict);
                this.queues.Add(mockQueue.Object);
            }

            var strategy = new AadAccountCloseRoundRobinQueueStrategy(this.queues);

            bool result = strategy.TryGetNextQueueAndRemove(out IQueue<AccountCloseRequest> queue);
            IList<IQueue<AccountCloseRequest>> remainingQueues = strategy.GetAllQueues();

            Assert.AreEqual(queueFound, result);
            Assert.AreEqual(remainingQueueCount, remainingQueues.Count);

            if (queueFound)
            {
                Assert.IsNotNull(queue);
            }
            else
            {
                Assert.IsNull(queue);
            }
        }

        [TestMethod]
        [DataRow(0, false)]
        [DataRow(1, true)]
        [DataRow(3, true)]
        [DataRow(50, true)]
        public void ShouldGetRandomQueue(int queueCount, bool shouldFindQueue)
        {
            var mockQueue = new Mock<IQueue<AccountCloseRequest>>(MockBehavior.Strict);
            for (int i = 0; i < queueCount; i++)
            {
                this.queues.Add(mockQueue.Object);
            }

            var strategy = new AadAccountCloseRoundRobinQueueStrategy(this.queues);

            IQueue<AccountCloseRequest> randomQueue = strategy.GetRandomQueue();

            if (shouldFindQueue)
            {
                Assert.IsNotNull(randomQueue);
            }
            else
            {
                Assert.IsNull(randomQueue);
            }
        }
    }
}

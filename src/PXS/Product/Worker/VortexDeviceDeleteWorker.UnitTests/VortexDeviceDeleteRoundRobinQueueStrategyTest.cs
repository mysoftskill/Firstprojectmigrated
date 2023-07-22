// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.VortexDeviceDeleteWorker.UnitTests
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.Membership.MemberServices.Privacy.Core.Vortex;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    /// <summary>
    ///     VortexDeviceDeleteRoundRobinQueueStrategyTest
    /// </summary>
    [TestClass]
    public class VortexDeviceDeleteRoundRobinQueueStrategyTest
    {
        private readonly IList<IQueue<DeviceDeleteRequest>> queues = new List<IQueue<DeviceDeleteRequest>>();

        [TestMethod]
        public void ShouldAllowEmptyList()
        {
            var strategy = new RoundRobinQueueSelectionStrategy<DeviceDeleteRequest>(new List<IQueue<DeviceDeleteRequest>>());
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(3)]
        [DataRow(50)]
        public void ShouldGetAllQueues(int queueCount)
        {
            var mockQueue = new Mock<IQueue<DeviceDeleteRequest>>(MockBehavior.Strict);
            for (int i = 0; i < queueCount; i++)
            {
                this.queues.Add(mockQueue.Object);
            }

            var strategy = new RoundRobinQueueSelectionStrategy<DeviceDeleteRequest>(this.queues);

            IList<IQueue<DeviceDeleteRequest>> allQueues = strategy.GetAllQueues();

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
                var mockQueue = new Mock<IQueue<DeviceDeleteRequest>>(MockBehavior.Strict);
                this.queues.Add(mockQueue.Object);
            }

            var strategy = new RoundRobinQueueSelectionStrategy<DeviceDeleteRequest>(this.queues);

            bool result = strategy.TryGetNextQueueAndRemove(out IQueue<DeviceDeleteRequest> queue);

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
                var mockQueue = new Mock<IQueue<DeviceDeleteRequest>>(MockBehavior.Strict);
                this.queues.Add(mockQueue.Object);
            }

            var strategy = new RoundRobinQueueSelectionStrategy<DeviceDeleteRequest>(this.queues);

            bool result = strategy.TryGetNextQueueAndRemove(out IQueue<DeviceDeleteRequest> queue);
            IList<IQueue<DeviceDeleteRequest>> remainingQueues = strategy.GetAllQueues();

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
            var mockQueue = new Mock<IQueue<DeviceDeleteRequest>>(MockBehavior.Strict);
            for (int i = 0; i < queueCount; i++)
            {
                this.queues.Add(mockQueue.Object);
            }

            var strategy = new RoundRobinQueueSelectionStrategy<DeviceDeleteRequest>(this.queues);

            IQueue<DeviceDeleteRequest> randomQueue = strategy.GetRandomQueue();

            if (shouldFindQueue)
            {
                Assert.IsNotNull(randomQueue);
            }
            else
            {
                Assert.IsNull(randomQueue);
            }
        }

        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowArgumentNullExceptionOnNull()
        {
            var strategy = new RoundRobinQueueSelectionStrategy<DeviceDeleteRequest>(null);
        }
    }
}

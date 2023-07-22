// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.Helpers.Queues
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Azure.Storage.RetryPolicies;

    using Moq;

    [TestClass]
    public class PartitionedQueueTests
    {
        public class TestData
        {
            public int Id { get; set; }
        }

        public Mock<IQueueItem<TestData>> dequeueItem = new Mock<IQueueItem<TestData>>();
        public Mock<IQueue<TestData>> mockQ0 = new Mock<IQueue<TestData>>();
        public Mock<IQueue<TestData>> mockQ1 = new Mock<IQueue<TestData>>();
        public Mock<IQueue<TestData>> mockQ2 = new Mock<IQueue<TestData>>();

        public PartitionedQueue<TestData, int> testObj;

        [TestInitialize]
        public void Init()
        {
            this.mockQ0.SetupGet(o => o.Name).Returns("Q0");
            this.mockQ1.SetupGet(o => o.Name).Returns("Q1");
            this.mockQ2.SetupGet(o => o.Name).Returns("Q2");

            this.testObj = new PartitionedQueue<TestData, int>();
        }

        [TestMethod]
        public void NameHasCorrectValueAfterConstruction()
        {
            Assert.AreEqual("testdata-partitioned", this.testObj.Name);
        }

        [TestMethod]
        public void CanSuccessfullyAddQueuesWhenInInitMode()
        {
            IReadOnlyList<QueuePartition<int>> result;

            // test
            this.testObj.AddPartition(0, this.mockQ0.Object);
            this.testObj.AddPartition(1, this.mockQ1.Object);

            // verify
            result = this.testObj.Partitions;

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(1, result.Count(o => o.Id == 0));
            Assert.AreEqual(1, result.Count(o => o.Id == 1));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CannotAddQueuesWhenInQueueMode()
        {
            this.testObj.SetQueueMode();

            // test
            this.testObj.AddPartition(0, this.mockQ0.Object);
        }

        [TestMethod]
        public void PartitionsCorrectlyReportsAddedQueues()
        {
            IReadOnlyList<QueuePartition<int>> result;

            // test
            this.testObj.AddPartition(0, this.mockQ0.Object);
            this.testObj.AddPartition(1, this.mockQ1.Object);

            // verify
            result = this.testObj.Partitions;

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(1, result.Count(o => o.Id == 0 && this.mockQ0.Object.Name.EqualsIgnoreCase(o.Name)));
            Assert.AreEqual(1, result.Count(o => o.Id == 1 && this.mockQ1.Object.Name.EqualsIgnoreCase(o.Name)));
        }
        
        [TestMethod]
        public async Task GetSizeAsyncReportsSumOfSizesOfInidividualQueues()
        {
            ulong result;

            this.mockQ0.Setup(o => o.GetQueueSizeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1ul);
            this.mockQ1.Setup(o => o.GetQueueSizeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2ul);
            this.mockQ2.Setup(o => o.GetQueueSizeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(4ul);

            this.testObj.AddPartition(0, this.mockQ0.Object);
            this.testObj.AddPartition(1, this.mockQ1.Object);
            this.testObj.AddPartition(2, this.mockQ2.Object);
            this.testObj.SetQueueMode();

            // test
            result = await this.testObj.GetSizeAsync(CancellationToken.None);

            Assert.AreEqual(7ul, result);
        }

        [TestMethod]
        public async Task GetPartitionSizesAsyncReportsSizesOfInidividualQueues()
        {
            IReadOnlyList<QueuePartitionSize<int>> result;

            this.mockQ0.Setup(o => o.GetQueueSizeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1ul);
            this.mockQ1.Setup(o => o.GetQueueSizeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2ul);
            this.mockQ2.Setup(o => o.GetQueueSizeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(4ul);

            this.testObj.AddPartition(0, this.mockQ0.Object);
            this.testObj.AddPartition(1, this.mockQ1.Object);
            this.testObj.AddPartition(2, this.mockQ2.Object);
            this.testObj.SetQueueMode();

            // test
            result = await this.testObj.GetPartitionSizesAsync(CancellationToken.None);

            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(1ul, result.First(o => o.Id == 0).Count);
            Assert.AreEqual(2ul, result.First(o => o.Id == 1).Count);
            Assert.AreEqual(4ul, result.First(o => o.Id == 2).Count);
        }

        [TestMethod]
        public async Task EnqueueSendsItemToSpecifiedQueue()
        {
            TestData item = new TestData();

            this.testObj.AddPartition(0, this.mockQ0.Object);
            this.testObj.AddPartition(1, this.mockQ1.Object);
            this.testObj.AddPartition(2, this.mockQ2.Object);
            this.testObj.SetQueueMode();

            // test
            await this.testObj.EnqueueAsync(1, item, CancellationToken.None);

            // verify
            this.mockQ0.Verify(o => o.EnqueueAsync(It.IsAny<TestData>(), It.IsAny<CancellationToken>()), Times.Never);
            this.mockQ1.Verify(o => o.EnqueueAsync(item, CancellationToken.None), Times.Once);
            this.mockQ2.Verify(o => o.EnqueueAsync(It.IsAny<TestData>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task DequeueCallsPartitionsInOrderUntilItGetsANonNullValue()
        {
            PartitionedQueueItem<TestData, int> result;
            TestData item = new TestData();

            this.testObj.AddPartition(0, this.mockQ0.Object);
            this.testObj.AddPartition(1, this.mockQ1.Object);
            this.testObj.AddPartition(2, this.mockQ2.Object);
            this.testObj.SetQueueMode();

            this.dequeueItem.SetupGet(o => o.Data).Returns(item);
            this.mockQ1
                .Setup(
                    o => o.DequeueAsync(
                        It.IsAny<TimeSpan>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<IRetryPolicy>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.dequeueItem.Object);

            // test
            result = await this.testObj.DequeueAsync(
                new[] { 0, 1, 2 }, TimeSpan.MaxValue, TimeSpan.MaxValue, null, CancellationToken.None);

            // verify
            Assert.AreSame(this.dequeueItem.Object, result.Item);
            Assert.AreEqual(1, result.PartitionId);

            this.mockQ0.Verify(o => o.DequeueAsync(TimeSpan.MaxValue, TimeSpan.MaxValue, null, CancellationToken.None), Times.Once);
            this.mockQ1.Verify(o => o.DequeueAsync(TimeSpan.MaxValue, TimeSpan.MaxValue, null, CancellationToken.None), Times.Once);

            this.mockQ2.Verify(
                o => o.DequeueAsync(It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<IRetryPolicy>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [TestMethod]
        public async Task DequeueCallsPartitionsInOrderAndReturnsNullIfAllReturnNull()
        {
            PartitionedQueueItem<TestData, int> result;

            this.testObj.AddPartition(0, this.mockQ0.Object);
            this.testObj.AddPartition(1, this.mockQ1.Object);
            this.testObj.AddPartition(2, this.mockQ2.Object);
            this.testObj.SetQueueMode();

            // test
            result = await this.testObj.DequeueAsync(
                new[] { 0, 1, 2 }, TimeSpan.MaxValue, TimeSpan.MaxValue, null, CancellationToken.None);

            // verify
            Assert.IsNull(result);

            this.mockQ0.Verify(o => o.DequeueAsync(TimeSpan.MaxValue, TimeSpan.MaxValue, null, CancellationToken.None), Times.Once);
            this.mockQ1.Verify(o => o.DequeueAsync(TimeSpan.MaxValue, TimeSpan.MaxValue, null, CancellationToken.None), Times.Once);
            this.mockQ2.Verify(o => o.DequeueAsync(TimeSpan.MaxValue, TimeSpan.MaxValue, null, CancellationToken.None), Times.Once);
        }

        [TestMethod]
        public async Task DequeueSkipsPartitionsThatAreNotAllowed()
        {
            PartitionedQueueItem<TestData, int> result;

            this.testObj.AddPartition(0, this.mockQ0.Object);
            this.testObj.AddPartition(1, this.mockQ1.Object);
            this.testObj.AddPartition(2, this.mockQ2.Object);
            this.testObj.SetQueueMode();

            // test
            result = await this.testObj.DequeueAsync(
                new[] { 2 }, TimeSpan.MaxValue, TimeSpan.MaxValue, null, CancellationToken.None);

            // verify
            Assert.IsNull(result);

            this.mockQ0.Verify(
                o => o.DequeueAsync(It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<IRetryPolicy>(), It.IsAny<CancellationToken>()), 
                Times.Never);

            this.mockQ1.Verify(
                o => o.DequeueAsync(It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<IRetryPolicy>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.mockQ2.Verify(o => o.DequeueAsync(TimeSpan.MaxValue, TimeSpan.MaxValue, null, CancellationToken.None), Times.Once);
        }

        [TestMethod]
        public async Task DequeueBatchCallsPartitionsInOrderUntilItGetsANonNullValue()
        {
            IList<PartitionedQueueItem<TestData, int>> result;
            TestData item = new TestData();

            this.testObj.AddPartition(0, this.mockQ0.Object);
            this.testObj.AddPartition(1, this.mockQ1.Object);
            this.testObj.AddPartition(2, this.mockQ2.Object);
            this.testObj.SetQueueMode();

            this.dequeueItem.SetupGet(o => o.Data).Returns(item);
            this.mockQ1
                .Setup(
                    o => o.DequeueBatchAsync(
                        It.IsAny<TimeSpan>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<int>(),
                        It.IsAny<IRetryPolicy>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { this.dequeueItem.Object });

            // test
            result = await this.testObj.DequeueBatchAsync(
                new[] { 0, 1, 2 }, TimeSpan.MaxValue, TimeSpan.MaxValue, 1, null, CancellationToken.None);

            // verify
            Assert.AreEqual(1, result.Count);
            Assert.AreSame(this.dequeueItem.Object, result.First().Item);
            Assert.AreEqual(1, result.First().PartitionId);

            this.mockQ0.Verify(
                o => o.DequeueBatchAsync(TimeSpan.MaxValue, TimeSpan.MaxValue, 1, null, CancellationToken.None), Times.Once);

            this.mockQ1.Verify(
                o => o.DequeueBatchAsync(TimeSpan.MaxValue, TimeSpan.MaxValue, 1, null, CancellationToken.None), Times.Once);

            this.mockQ2.Verify(
                o => o.DequeueBatchAsync(
                     It.IsAny<TimeSpan>(), 
                     It.IsAny<TimeSpan>(), 
                     It.IsAny<int>(),
                     It.IsAny<IRetryPolicy>(), 
                    It.IsAny<CancellationToken>()), 
                Times.Never);
        }

        [TestMethod]
        public async Task DequeueBatchCallsPartitionsInOrderAndReturnsNullIfAllReturnNull()
        {
            IList<PartitionedQueueItem<TestData, int>> result;

            this.testObj.AddPartition(0, this.mockQ0.Object);
            this.testObj.AddPartition(1, this.mockQ1.Object);
            this.testObj.AddPartition(2, this.mockQ2.Object);
            this.testObj.SetQueueMode();

            // test
            result = await this.testObj.DequeueBatchAsync(
                new[] { 0, 1, 2 }, TimeSpan.MaxValue, TimeSpan.MaxValue, 1, null, CancellationToken.None);

            // verify
            Assert.IsNull(result);

            this.mockQ0.Verify(
                o => o.DequeueBatchAsync(TimeSpan.MaxValue, TimeSpan.MaxValue, 1, null, CancellationToken.None), Times.Once);
            this.mockQ1.Verify(
                o => o.DequeueBatchAsync(TimeSpan.MaxValue, TimeSpan.MaxValue, 1, null, CancellationToken.None), Times.Once);
            this.mockQ2.Verify(
                o => o.DequeueBatchAsync(TimeSpan.MaxValue, TimeSpan.MaxValue, 1, null, CancellationToken.None), Times.Once);
        }

        [TestMethod]
        public async Task DequeueBatchSkipsPartitionsThatAreNotAllowed()
        {
            IList<PartitionedQueueItem<TestData, int>> result;

            this.testObj.AddPartition(0, this.mockQ0.Object);
            this.testObj.AddPartition(1, this.mockQ1.Object);
            this.testObj.AddPartition(2, this.mockQ2.Object);
            this.testObj.SetQueueMode();

            // test
            result = await this.testObj.DequeueBatchAsync(
                new[] { 2 }, TimeSpan.MaxValue, TimeSpan.MaxValue, 1, null, CancellationToken.None);

            // verify
            Assert.IsNull(result);

            this.mockQ0.Verify(
                o => o.DequeueBatchAsync(
                    It.IsAny<TimeSpan>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<int>(),
                    It.IsAny<IRetryPolicy>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.mockQ1.Verify(
                o => o.DequeueBatchAsync(
                    It.IsAny<TimeSpan>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<int>(),
                    It.IsAny<IRetryPolicy>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.mockQ2.Verify(
                o => o.DequeueBatchAsync(TimeSpan.MaxValue, TimeSpan.MaxValue, 1, null, CancellationToken.None), Times.Once);
        }
    }
}

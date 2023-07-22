namespace Microsoft.Azure.ComplianceServices.Common.UnitTests.CloudQueue
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ComplianceServices.Common.Queues;
    using Moq;
    using Xunit;

    public class CloudQueueTests
    {
        [Fact]
        public async Task CloudQueuePoolCreateIfNotExistsAsyncTest()
        {
            Mock<ICloudQueue<int>> mockCloudQueue = new Mock<ICloudQueue<int>>();
            ICloudQueue<int>[] cloudQueues = new ICloudQueue<int>[] { mockCloudQueue.Object };
            CloudQueuePool<int> pool = new CloudQueuePool<int>(cloudQueues);

            await pool.CreateIfNotExistsAsync().ConfigureAwait(false);

            mockCloudQueue.Verify(m => m.CreateIfNotExistsAsync(), Times.Once);
        }

        /// <summary>
        /// Enqueue cloud queue if one of the queue failed.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception">Exception.</exception>
        [Fact]
        public async Task CloudQueuePoolEnqueuAsyncTest()
        {
            bool badQueueCalled = false;
            bool cloudQueueCalledAfterBadQueue = false;

            Mock<ICloudQueue<int>> mockBadCloudQueue = new Mock<ICloudQueue<int>>();
            mockBadCloudQueue.Setup(m => m.EnqueueAsync(
                It.IsAny<int>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns((int data, TimeSpan? timeToLive, TimeSpan? invisibilityDelay, CancellationToken cancellationToken) =>
            {
                badQueueCalled = true;
                throw new Exception("Exception");
            }
            );

            Mock<ICloudQueue<int>> mockCloudQueue = new Mock<ICloudQueue<int>>();
            mockCloudQueue.Setup(m => m.EnqueueAsync(
                It.IsAny<int>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns((int data, TimeSpan? timeToLive, TimeSpan? invisibilityDelay, CancellationToken cancellationToken) =>
            {
                if (badQueueCalled)
                {
                    cloudQueueCalledAfterBadQueue = true;
                }
                return Task.FromResult(true);
            });

            ICloudQueue<int>[] cloudQueues = new ICloudQueue<int>[] { mockCloudQueue.Object, mockBadCloudQueue.Object };
            CloudQueuePool<int> pool = new CloudQueuePool<int>(cloudQueues);


            while (!badQueueCalled)
            {
                await pool.EnqueueAsync(10).ConfigureAwait(false);
            }

            Assert.True(badQueueCalled);
            Assert.True(cloudQueueCalledAfterBadQueue);
        }

        /// <summary>
        /// Verify if secondary queue was used if the first one fail.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception">Exception.</exception>
        [Fact]
        public async Task CloudQueuePoolDequeuAsyncTest01()
        {
            bool badQueueCalled = false;
            bool cloudQueueCalledAfterBadQueue = false;

            Mock<ICloudQueueItem<int>> mockCloudQueueItem = new Mock<ICloudQueueItem<int>>();

            Mock<ICloudQueue<int>> mockBadCloudQueue = new Mock<ICloudQueue<int>>();
            mockBadCloudQueue.Setup(m => m.DequeueAsync(
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns((TimeSpan? visibilityTimeout, CancellationToken cancellationToken) =>
            {
                badQueueCalled = true;
                throw new Exception("Exception");
            });

            Mock<ICloudQueue<int>> mockCloudQueue = new Mock<ICloudQueue<int>>();
            mockCloudQueue.Setup(m => m.DequeueAsync(
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns((TimeSpan? visibilityTimeout, CancellationToken cancellationToken) =>
            {
                if (badQueueCalled)
                {
                    cloudQueueCalledAfterBadQueue = true;
                }
                return Task.FromResult<ICloudQueueItem<int>>(mockCloudQueueItem.Object);
            });

            ICloudQueue<int>[] cloudQueues = new ICloudQueue<int>[] { mockCloudQueue.Object, mockBadCloudQueue.Object };
            CloudQueuePool<int> pool = new CloudQueuePool<int>(cloudQueues);

            while (!badQueueCalled)
            {
                await pool.DequeueAsync().ConfigureAwait(false);
            }

            Assert.True(badQueueCalled);
            Assert.True(cloudQueueCalledAfterBadQueue);
        }

        /// <summary>
        /// Verify if secondary queue was used if the first one return null (no messages).
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception">Exception.</exception>
        [Fact]
        public async Task CloudQueuePoolDequeuAsyncTest02()
        {
            bool badQueueCalled = false;
            bool cloudQueueCalledAfterBadQueue = false;

            Mock<ICloudQueueItem<int>> mockCloudQueueItem = new Mock<ICloudQueueItem<int>>();

            Mock<ICloudQueue<int>> mockBadCloudQueue = new Mock<ICloudQueue<int>>();
            mockBadCloudQueue.Setup(m => m.DequeueAsync(
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns((TimeSpan? visibilityTimeout, CancellationToken cancellationToken) =>
            {
                badQueueCalled = true;
                return null;
            });

            Mock<ICloudQueue<int>> mockCloudQueue = new Mock<ICloudQueue<int>>();
            mockCloudQueue.Setup(m => m.DequeueAsync(
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns((TimeSpan? visibilityTimeout, CancellationToken cancellationToken) =>
            {
                if (badQueueCalled)
                {
                    cloudQueueCalledAfterBadQueue = true;
                }
                return Task.FromResult<ICloudQueueItem<int>>(mockCloudQueueItem.Object);
            });

            ICloudQueue<int>[] cloudQueues = new ICloudQueue<int>[] { mockCloudQueue.Object, mockBadCloudQueue.Object };
            CloudQueuePool<int> pool = new CloudQueuePool<int>(cloudQueues);

            while (!badQueueCalled)
            {
                await pool.DequeueAsync().ConfigureAwait(false);
            }

            Assert.True(badQueueCalled);
            Assert.True(cloudQueueCalledAfterBadQueue);
        }

        /// <summary>
        /// Throw exception if one of the queue fail and another return null.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception">Exception.</exception>
        [Fact]
        public Task CloudQueuePoolDequeuAsyncTest03()
        {
            Mock<ICloudQueue<int>> mockBadCloudQueue = new Mock<ICloudQueue<int>>();
            mockBadCloudQueue.Setup(m => m.DequeueAsync(
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns((TimeSpan? visibilityTimeout, CancellationToken cancellationToken) =>
            {
                throw new Exception("Exception");
            });

            Mock<ICloudQueue<int>> mockCloudQueue = new Mock<ICloudQueue<int>>();

            ICloudQueue<int>[] cloudQueues = new ICloudQueue<int>[] { mockCloudQueue.Object, mockBadCloudQueue.Object };
            CloudQueuePool<int> pool = new CloudQueuePool<int>(cloudQueues);

            return Assert.ThrowsAsync<AggregateException>(() => pool.DequeueAsync());
        }

        /// <summary>
        /// Verify if secondary queue was used if the first one fail.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception">Exception.</exception>
        [Fact]
        public async Task CloudQueuePoolDequeueBatchAsyncTest01()
        {
            bool badQueueCalled = false;
            bool cloudQueueCalledAfterBadQueue = false;

            IList<ICloudQueueItem<int>> cloudQueueItems = new List<ICloudQueueItem<int>>();

            Mock<ICloudQueue<int>> mockBadCloudQueue = new Mock<ICloudQueue<int>>();
            mockBadCloudQueue.Setup(m => m.DequeueBatchAsync(
                It.IsAny<TimeSpan?>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Returns((TimeSpan? visibilityTimeout, int maxCount, CancellationToken cancellationToken) =>
            {
                badQueueCalled = true;
                throw new Exception("Exception");
            });

            Mock<ICloudQueue<int>> mockCloudQueue = new Mock<ICloudQueue<int>>();
            mockCloudQueue.Setup(m => m.DequeueBatchAsync(
                It.IsAny<TimeSpan?>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Returns((TimeSpan? visibilityTimeout, int maxCount, CancellationToken cancellationToken) =>
            {
                if (badQueueCalled)
                {
                    cloudQueueCalledAfterBadQueue = true;
                }
                return Task.FromResult<IList<ICloudQueueItem<int>>>(cloudQueueItems);
            });

            ICloudQueue<int>[] cloudQueues = new ICloudQueue<int>[] { mockCloudQueue.Object, mockBadCloudQueue.Object };
            CloudQueuePool<int> pool = new CloudQueuePool<int>(cloudQueues);

            while (!badQueueCalled)
            {
                await pool.DequeueBatchAsync().ConfigureAwait(false);
            }

            Assert.True(badQueueCalled);
            Assert.True(cloudQueueCalledAfterBadQueue);
        }
    }
}
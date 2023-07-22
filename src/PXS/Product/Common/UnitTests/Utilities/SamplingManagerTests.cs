// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.UnitTests.Utilities
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SamplingManagerTests
    {
        private const string Id = nameof(SamplingManagerTests);

        /// <summary>
        ///     Tests that if we have a smaller list than RPS we end up getting expected amounts through
        /// </summary>
        /// <param name="maxItemsPerSecond">The max allowed items per second</param>
        /// <param name="expectedPerIteration">The number of items expected to pass through each iteration</param>
        /// <param name="iterationSampleSize">The number of "items" per iteration</param>
        /// <param name="iterations">The number of iterations</param>
        /// <param name="finalIterationCount">After all the iterations, the number we expect to get from trying one last time</param>
        [DataTestMethod]
        [DataRow(2, 1, 1, 2, 0)]
        public void ShouldAllowWholeListIfExcess(double maxItemsPerSecond, int expectedPerIteration, int iterationSampleSize, int iterations, int finalIterationCount)
        {
            var arr = new byte[iterationSampleSize];
            var manager = new SamplingManager(new Dictionary<string, double> { { Id, maxItemsPerSecond } });
            IEnumerable<byte> results;
            for (int x = 0; x < iterations; ++x)
            {
                results = manager.ApplySamplingToCollection(Id, arr);
                Assert.AreEqual(expectedPerIteration, results.Count());
            }

            results = manager.ApplySamplingToCollection(Id, arr);
            Assert.AreEqual(finalIterationCount, results.Count());
        }

        /// <summary>
        ///     Checks that only the allowed number of items per second get through if we have a huge list
        /// </summary>
        /// <param name="maxItemsPerSecond">The max items we allow through per second</param>
        /// <param name="expectedItemsAllowed">The amount we expect to get through</param>
        /// <param name="itemCount">The number of items originally</param>
        [DataTestMethod]
        [DataRow(0, 0, 100)]
        [DataRow(.1, 0, 100)]
        [DataRow(1, 1, 100)]
        [DataRow(1.5, 1, 5)]
        [DataRow(2, 2, 5)]
        [DataRow(.3, 0, 2)]
        [DataRow(10.3, 10, 20)]
        [DataRow(103.5, 103, 1000)]
        [DataRow(10, 3, 3)]
        public void ShouldDownsizeList(double maxItemsPerSecond, int expectedItemsAllowed, int itemCount)
        {
            var arr = new byte[itemCount];
            var manager = new SamplingManager(new Dictionary<string, double> { { Id, maxItemsPerSecond } });
            IEnumerable<byte> results = manager.ApplySamplingToCollection(Id, arr);
            Assert.AreEqual(expectedItemsAllowed, results.Count());
        }

        /// <summary>
        ///     Checks that an action is only performed the max number of times per second
        /// </summary>
        /// <param name="maxRps">The max requests per second that can happen</param>
        /// <param name="expectedValue">The expected value after attempting an action a number of times</param>
        /// <param name="numberOfTimes">The number of times to attempt an action</param>
        [DataTestMethod]
        [DataRow(0, 0, 100)]
        [DataRow(.5, 0, 100)]
        [DataRow(2, 2, 100)]
        [DataRow(15, 10, 10)]
        public void ShouldSampleActionAmount(double maxRps, int expectedValue, int numberOfTimes)
        {
            var manager = new SamplingManager(new Dictionary<string, double> { { Id, maxRps } });
            int count = 0;
            Parallel.For(
                0,
                numberOfTimes,
                x =>
                    manager.ApplySamplingAsync(Id, () => Task.Run(() => Interlocked.Increment(ref count))).Wait());

            Assert.AreEqual(expectedValue, count);
        }

        [DataTestMethod]
        [DataRow(0, 0, 9999999)]
        public void ShouldSampleToZero(double maxRps, int expectedValue, int numberOfTimes)
        {
            var manager = new SamplingManager(new Dictionary<string, double> { { Id, maxRps } });
            int count = 0;
            Parallel.For(
                0,
                numberOfTimes,
                x =>
                    manager.ApplySamplingAsync(Id, () => Task.Run(() => Interlocked.Increment(ref count))).Wait());

            Assert.AreEqual(expectedValue, count);
        }
    }
}

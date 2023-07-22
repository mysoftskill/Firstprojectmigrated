// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests
{
    using System;
    using System.Threading;

    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Privacy.Core.Throttling;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    /// <summary>
    ///     InMemoryRequestThrottler Tests
    /// </summary>
    [TestClass]
    public class InMemoryRequestThrottlerTests
    {
        [TestMethod]
        public void PressureTest()
        {
            (ICounterFactory counterFactory, Action verifyAction) counterData = this.SetupCounters(0, 0);
            var throttler = new InMemoryRequestThrottler(Guid.NewGuid().ToString(), 10, TimeSpan.FromHours(1), counterData.counterFactory);

            for (int i = 0; i < InMemoryRequestThrottler.MaxRecordCount + 100; i++)
            {
                throttler.ShouldThrottle(Guid.NewGuid().ToString());
                Assert.IsTrue(throttler.CurrentRecordCount <= InMemoryRequestThrottler.MaxRecordCount);
            }
        }

        [TestMethod]
        public void TimePassed()
        {
            (ICounterFactory counterFactory, Action verifyAction) counterData = this.SetupCounters(180, 20);
            var throttler = new InMemoryRequestThrottler(Guid.NewGuid().ToString(), 10, TimeSpan.FromMilliseconds(500), counterData.counterFactory);

            for (int i = 0; i < 100; i++)
                Assert.AreEqual(i >= 10, throttler.ShouldThrottle("foo"), $"{i} >= 10");
            Thread.Sleep(1000);
            for (int i = 0; i < 100; i++)
                Assert.AreEqual(i >= 10, throttler.ShouldThrottle("foo"), $"{i} >= 10");

            counterData.verifyAction();
        }

        [TestMethod]
        public void TwoKeys()
        {
            (ICounterFactory counterFactory, Action verifyAction) counterData = this.SetupCounters(180, 20);
            var throttler = new InMemoryRequestThrottler(Guid.NewGuid().ToString(), 10, TimeSpan.FromHours(1), counterData.counterFactory);

            for (int i = 0; i < 100; i++)
                Assert.AreEqual(i >= 10, throttler.ShouldThrottle("foo"), $"{i} >= 10");
            for (int i = 0; i < 100; i++)
                Assert.AreEqual(i >= 10, throttler.ShouldThrottle("bar"), $"{i} >= 10");

            counterData.verifyAction();
        }

        private (ICounterFactory counterFactory, Action verifyAction) SetupCounters(int throttledCount, int notThrottledCount)
        {
            var counter = new Mock<ICounter>();
            counter.Setup(c => c.Increment(It.IsAny<string>()));

            var counterFactory = new Mock<ICounterFactory>();
            counterFactory.Setup(f => f.GetCounter(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CounterType>()))
                .Returns(() => counter.Object);

            return (counterFactory.Object, () =>
            {
                counter.Verify(c => c.Increment("Throttled"), Times.Exactly(throttledCount));
                counter.Verify(c => c.Increment("NotThrottled"), Times.Exactly(notThrottledCount));
            });
        }
    }
}

// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;
    using Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.Factory;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// ResourceAggregator-Browse Tests
    /// </summary>
    [TestClass]
    public class ResourceAggregatorBrowseTests
    {
        [TestMethod]
        public void AggregateBrowseTwoItemsSameDay()
        {
            List<BrowseHistoryV1> items = new List<BrowseHistoryV1>
            {
                BrowseHistoryV1Factory.Create(new DateTimeOffset(2016, 1, 1, 23, 30, 0, TimeSpan.Zero), "https://www.microsoft.com"),
                BrowseHistoryV1Factory.Create(new DateTimeOffset(2016, 1, 1, 23, 45, 0, TimeSpan.Zero), "https://www.microsoft.com")
            };

            Assert.IsTrue(ResourceAggregator.Aggregate(items[0], items[1], TimeSpan.Zero));

            Assert.AreEqual(2, items[0].Ids.Count);
            Assert.AreEqual(2, items[0].AggregateCount);
            Assert.IsTrue(items[0].IsAggregate);
        }

        [TestMethod]
        public void AggregateBrowseTwoItemsDifferentDayWithTimeZoneOffset()
        {
            List<BrowseHistoryV1> items = new List<BrowseHistoryV1>
            {
                BrowseHistoryV1Factory.Create(new DateTimeOffset(2016, 1, 1, 23, 59, 59, new TimeSpan(-1, 0, 0)), "https://www.microsoft.com"),
                BrowseHistoryV1Factory.Create(new DateTimeOffset(2016, 1, 1, 23, 59, 59, new TimeSpan(1, 0, 0)), "https://www.microsoft.com")
            };

            Assert.IsFalse(ResourceAggregator.Aggregate(items[0], items[1], TimeSpan.Zero));

            Assert.AreEqual(1, items[0].Ids.Count);
            Assert.IsFalse(items[0].IsAggregate);

            Assert.AreEqual(1, items[1].Ids.Count);
            Assert.IsFalse(items[1].IsAggregate);
        }

        [TestMethod]
        public void AggregateBrowseTwoItemsSameDayDifferentPartners()
        {
            List<BrowseHistoryV1> items = new List<BrowseHistoryV1>
            {
                BrowseHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), "https://www.microsoft.com", partnerId: "P1"),
                BrowseHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), "https://www.microsoft.com", partnerId: "P2")
            };

            Assert.IsTrue(ResourceAggregator.Aggregate(items[0], items[1], TimeSpan.Zero));

            Assert.AreEqual(2, items[0].Ids.Count);

            // different partners mean the aggregate count is de-duped
            Assert.AreEqual(1, items[0].AggregateCount);
            Assert.IsTrue(items[0].IsAggregate);
        }

        [TestMethod]
        public void AggregateBrowseMultipleItemsSameDay()
        {
            List<BrowseHistoryV1> items = new List<BrowseHistoryV1>
            {
                BrowseHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), "https://www.microsoft.com"),
                BrowseHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), "https://www.microsoft.com"),
                BrowseHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), "https://www.microsoft.com"),
                BrowseHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), "https://www.microsoft.com"),
                BrowseHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), "https://www.microsoft.com")
            };

            for (int i = 1; i < items.Count; i++)
                Assert.IsTrue(ResourceAggregator.Aggregate(items[0], items[i], TimeSpan.Zero));

            Assert.AreEqual(5, items[0].Ids.Count);
            Assert.AreEqual(5, items[0].AggregateCount);
            Assert.IsTrue(items[0].IsAggregate);
        }

        [TestMethod]
        public void AggregateBrowseTwoItemsDifferentDayNotGrouped()
        {
            List<BrowseHistoryV1> items = new List<BrowseHistoryV1>
            {
                BrowseHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), "https://www.microsoft.com"),
                BrowseHistoryV1Factory.Create(DateTime.Parse("2000-01-01"), "https://www.microsoft.com")
            };

            Assert.IsFalse(ResourceAggregator.Aggregate(items[0], items[1], TimeSpan.Zero));

            Assert.AreEqual(1, items[0].Ids.Count);
            Assert.AreEqual(1, items[0].AggregateCount);
            Assert.IsFalse(items[0].IsAggregate);

            Assert.AreEqual(1, items[1].Ids.Count);
            Assert.AreEqual(1, items[1].AggregateCount);
            Assert.IsFalse(items[1].IsAggregate);
        }

        [TestMethod]
        public void AggregateBrowseTwoDifferentItemsSameDayNotGrouped()
        {
            List<BrowseHistoryV1> items = new List<BrowseHistoryV1>
            {
                BrowseHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), "https://www.microsoft.com"),
                BrowseHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), "https://www.microsoft.com/en-us/")
            };

            Assert.IsFalse(ResourceAggregator.Aggregate(items[0], items[1], TimeSpan.Zero));

            Assert.AreEqual(1, items[0].Ids.Count);
            Assert.AreEqual(1, items[0].AggregateCount);
            Assert.IsFalse(items[0].IsAggregate);

            Assert.AreEqual(1, items[1].Ids.Count);
            Assert.AreEqual(1, items[1].AggregateCount);
            Assert.IsFalse(items[1].IsAggregate);
        }

        [TestMethod]
        public void AggregateBrowseMultipleItemsAlreadyAggregatedGetGrouped()
        {
            // All of these resources have the same date and matching navigated-to-url, but they are also already aggregate resources (contain multiple ids)
            List<BrowseHistoryV1> items = new List<BrowseHistoryV1>
            {
                BrowseHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), "https://www.microsoft.com", new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }),
                BrowseHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), "https://www.microsoft.com", new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }),
                BrowseHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), "https://www.microsoft.com", new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() })
            };

            for (int i = 1; i < items.Count; i++)
                Assert.IsTrue(ResourceAggregator.Aggregate(items[0], items[i], TimeSpan.Zero));

            Assert.AreEqual(6, items[0].Ids.Count);
            Assert.AreEqual(6, items[0].AggregateCount);
            Assert.IsTrue(items[0].IsAggregate);
        }

        [TestMethod]
        public void AggregateBrowseMultipleItemsMixedSomeGroupedSomeNot()
        {
            List<BrowseHistoryV1> items = new List<BrowseHistoryV1>
            {
                BrowseHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), "https://www.microsoft.com"),
                BrowseHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), "https://www.microsoft.com"),
                BrowseHistoryV1Factory.Create(DateTime.Parse("2000-01-01"), "https://www.microsoft.com"),
                BrowseHistoryV1Factory.Create(DateTime.Parse("2000-05-05"), "https://www.microsoft.com")
            };

            Assert.IsTrue(ResourceAggregator.Aggregate(items[0], items[1], TimeSpan.Zero));
            Assert.IsFalse(ResourceAggregator.Aggregate(items[0], items[2], TimeSpan.Zero));
            Assert.IsFalse(ResourceAggregator.Aggregate(items[0], items[3], TimeSpan.Zero));

            Assert.AreEqual(2, items[0].Ids.Count);
            Assert.AreEqual(2, items[0].AggregateCount);
            Assert.IsTrue(items[0].IsAggregate);
            
            Assert.AreEqual(1, items[1].Ids.Count);
            Assert.AreEqual(1, items[1].AggregateCount);
            Assert.IsFalse(items[1].IsAggregate);
            
            Assert.AreEqual(1, items[2].Ids.Count);
            Assert.AreEqual(1, items[2].AggregateCount);
            Assert.IsFalse(items[2].IsAggregate);
        }
    }
}
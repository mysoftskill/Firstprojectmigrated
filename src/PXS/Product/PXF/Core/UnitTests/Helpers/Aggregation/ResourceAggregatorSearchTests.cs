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
    /// ResourceAggregator-Search Tests
    /// </summary>
    [TestClass]
    public class ResourceAggregatorSearchTests
    {
        [TestMethod]
        public void AggregateSearchTwoItemsSameDay()
        {
            var item1 = SearchHistoryV1Factory.Create(new DateTimeOffset(2016, 1, 1, 23, 30, 0, TimeSpan.Zero), "https://www.microsoft.com", searchTerms: "foo");
            var item2 = SearchHistoryV1Factory.Create(new DateTimeOffset(2016, 1, 1, 23, 45, 0, TimeSpan.Zero), "https://www.microsoft.com", searchTerms: "foo");

            Assert.IsTrue(ResourceAggregator.Aggregate(item1, item2, TimeSpan.Zero));
            Assert.AreEqual(2, item1.Ids.Count);
            Assert.IsTrue(item1.IsAggregate);
        }

        [TestMethod]
        public void AggregateSearchTwoItemsDifferentDayWithTimeZoneOffset()
        {
            var item1 = SearchHistoryV1Factory.Create(new DateTimeOffset(2016, 1, 1, 23, 59, 59, new TimeSpan(-1, 0, 0)), "https://www.microsoft.com", searchTerms: "foo");
            var item2 = SearchHistoryV1Factory.Create(new DateTimeOffset(2016, 1, 1, 23, 59, 59, new TimeSpan(1, 0, 0)), "https://www.microsoft.com", searchTerms: "foo");

            Assert.IsFalse(ResourceAggregator.Aggregate(item1, item2, TimeSpan.Zero));

            Assert.AreEqual(1, item1.Ids.Count);
            Assert.IsFalse(item1.IsAggregate);

            Assert.AreEqual(1, item2.Ids.Count);
            Assert.IsFalse(item2.IsAggregate);
        }

        [TestMethod]
        public void AggregateSearchTwoItemsNoNavigatedToUrlSameDay()
        {
            var item1 = SearchHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), "https://www.microsoft.com", searchTerms: null);
            var item2 = SearchHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), "https://www.microsoft.com", searchTerms: null);

            Assert.IsTrue(ResourceAggregator.Aggregate(item1, item2, TimeSpan.Zero));

            Assert.AreEqual(2, item1.Ids.Count);
            Assert.IsTrue(item1.IsAggregate);
        }

        [TestMethod]
        public void AggregateSearchMultipleItemsSameDay()
        {
            List<SearchHistoryV1> items = new List<SearchHistoryV1>
            {
                SearchHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), "https://www.microsoft.com", searchTerms: "foo"),
                SearchHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), "https://www.microsoft.com", searchTerms: "foo"),
                SearchHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), "https://www.microsoft.com", searchTerms: "foo"),
                SearchHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), "https://www.microsoft.com", searchTerms: "foo"),
                SearchHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), "https://www.microsoft.com", searchTerms: "foo")
            };

            for (int i = 1; i < items.Count; i++)
                Assert.IsTrue(ResourceAggregator.Aggregate(items[0], items[i], TimeSpan.Zero));

            Assert.AreEqual(5, items[0].Ids.Count);
            Assert.IsTrue(items[0].IsAggregate);
        }

        [TestMethod]
        public void AggregateSearchTwoItemsDifferentDayNotGrouped()
        {
            List<SearchHistoryV1> items = new List<SearchHistoryV1>
            {
                SearchHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), "https://www.microsoft.com", searchTerms: "foo"),
                SearchHistoryV1Factory.Create(DateTime.Parse("2000-01-01"), "https://www.microsoft.com", searchTerms: "foo")
            };

            Assert.IsFalse(ResourceAggregator.Aggregate(items[0], items[1], TimeSpan.Zero));

            Assert.AreEqual(1, items[0].Ids.Count);
            Assert.IsFalse(items[0].IsAggregate);

            Assert.AreEqual(1, items[1].Ids.Count);
            Assert.IsFalse(items[1].IsAggregate);
        }

        [TestMethod]
        public void AggregateSearchTwoDifferentNavigatedUrlSameDayAreGrouped()
        {
            List<SearchHistoryV1> items = new List<SearchHistoryV1>
            {
                // set the ordering of links so they can be verified they get re-ordered too
                SearchHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), "https://www.microsoft.com", searchTerms: "foo"),
                SearchHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), "https://www.microsoft.com/en-us/", searchTerms: "foo"),
            };

            Assert.IsTrue(ResourceAggregator.Aggregate(items[0], items[1], TimeSpan.Zero));

            Assert.AreEqual(2, items[0].Ids.Count);
            Assert.IsTrue(items[0].IsAggregate);

            CollectionAssert.AreEqual(
                new List<string>
                {
                    "https://www.microsoft.com",
                    "https://www.microsoft.com/en-us/"
                },
                items[0].NavigatedToUrls.Select(i => i.Url).ToList());
        }

        [TestMethod]
        public void AggregateSearchTwoDifferentNavigatedUrlSameDayAreGroupedAndSortedByMostRecentNavigationTime()
        {
            List<SearchHistoryV1> items = new List<SearchHistoryV1>
            {
                // set the ordering of links so the navigation time should be re-ordered
                SearchHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), "https://www.microsoft.com", searchTerms: "foo", navigationTime: DateTimeOffset.Parse("2016-01-01 1:00 PM")),
                SearchHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), "https://www.microsoft.com/en-us/", searchTerms: "foo", navigationTime: DateTimeOffset.Parse("2016-01-01 9:59 PM"))
            };

            Assert.IsTrue(ResourceAggregator.Aggregate(items[0], items[1], TimeSpan.Zero));

            Assert.AreEqual(2, items[0].Ids.Count);
            Assert.IsTrue(items[0].IsAggregate);

            // the url should be in sorted according to the most recent timestamp of the navigation event associated with the search
            CollectionAssert.AreEqual(
                new List<string>
                {
                    "https://www.microsoft.com/en-us/",
                    "https://www.microsoft.com"
                },
                items[0].NavigatedToUrls.Select(i => i.Url).ToList());
        }

        [TestMethod]
        public void AggregateSearchTwoDifferentSearchTermSameUrlSameDayNotGrouped()
        {
            List<SearchHistoryV1> items = new List<SearchHistoryV1>
            {
                SearchHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), "https://www.microsoft.com", searchTerms: "foo"),
                SearchHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), "https://www.microsoft.com", searchTerms: "bar")
            };

            Assert.IsFalse(ResourceAggregator.Aggregate(items[0], items[1], TimeSpan.Zero));

            Assert.AreEqual(1, items[0].Ids.Count);
            Assert.IsFalse(items[0].IsAggregate);

            Assert.AreEqual(1, items[1].Ids.Count);
            Assert.IsFalse(items[1].IsAggregate);
        }

        [TestMethod]
        public void AggregateSearchMultipleItemsAlreadyAggregatedGetGrouped()
        {
            // All of these resources have the same date and matching navigated-to-url, but they are also already aggregate resources (contain multiple ids)
            List<SearchHistoryV1> items = new List<SearchHistoryV1>
            {
                SearchHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), "https://www.microsoft.com", new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }, searchTerms: "foo"),
                SearchHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), "https://www.microsoft.com", new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }, searchTerms: "foo"),
                SearchHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), "https://www.microsoft.com", new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }, searchTerms: "foo")
            };

            for (int i = 1; i < items.Count; i++)
                Assert.IsTrue(ResourceAggregator.Aggregate(items[0], items[1], TimeSpan.Zero));

            Assert.AreEqual(6, items[0].Ids.Count);
            Assert.IsTrue(items[0].IsAggregate);
        }

        [TestMethod]
        public void AggregateSearchMultipleItemsMixedSomeGroupedSomeNot()
        {
            List<SearchHistoryV1> items = new List<SearchHistoryV1>
            {
                SearchHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), "https://www.microsoft.com", searchTerms: "foo"),
                SearchHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), "https://www.microsoft.com", searchTerms: "foo"),
                SearchHistoryV1Factory.Create(DateTime.Parse("2000-01-01"), "https://www.microsoft.com", searchTerms: "foo"),
                SearchHistoryV1Factory.Create(DateTime.Parse("2000-05-05"), "https://www.microsoft.com", searchTerms: "foo")
            };

            Assert.IsTrue(ResourceAggregator.Aggregate(items[0], items[1], TimeSpan.Zero));
            Assert.IsFalse(ResourceAggregator.Aggregate(items[0], items[2], TimeSpan.Zero));
            Assert.IsFalse(ResourceAggregator.Aggregate(items[2], items[3], TimeSpan.Zero));

            Assert.AreEqual(2, items[0].Ids.Count);
            Assert.IsTrue(items[0].IsAggregate);

            Assert.AreEqual(1, items[2].Ids.Count);
            Assert.IsFalse(items[2].IsAggregate);

            Assert.AreEqual(1, items[3].Ids.Count);
            Assert.IsFalse(items[3].IsAggregate);
        }
    }
}
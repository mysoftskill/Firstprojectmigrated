// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests
{
    using System;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DateFilterResultTests
    {
        [TestMethod]
        public void EqualityDateTime()
        {
            var filterResult = DateFilterResult.ParseFromFilter("date eq datetime'2016-04-01'");

            Assert.AreEqual(Comparison.Equal, filterResult.Comparison);
            Assert.AreEqual(new DateTime(2016, 4, 1, 0, 0, 0, DateTimeKind.Utc), filterResult.StartDate);
            Assert.AreEqual(DateTimeKind.Utc, filterResult.StartDate.Value.Kind);
            Assert.IsNull(filterResult.EndDate);
        }

        [TestMethod]
        public void GreaterThenEqualDateTime()
        {
            var filterResult = DateFilterResult.ParseFromFilter("datetime ge datetime'2016-04-01T13:32:56.007'");

            Assert.AreEqual(Comparison.GreaterThanEqual, filterResult.Comparison);
            Assert.AreEqual(new DateTime(2016, 4, 1, 13, 32, 56, DateTimeKind.Utc) + TimeSpan.FromMilliseconds(7), filterResult.StartDate);
            Assert.AreEqual(DateTimeKind.Utc, filterResult.StartDate.Value.Kind);
            Assert.IsNull(filterResult.EndDate);
        }

        [TestMethod]
        public void LessThanEqualDateTime()
        {
            var filterResult = DateFilterResult.ParseFromFilter("datetime le datetime'2016-04-01T13:32:56.777'");

            Assert.AreEqual(Comparison.LessThanEqual, filterResult.Comparison);
            Assert.AreEqual(new DateTime(2016, 4, 1, 13, 32, 56, DateTimeKind.Utc) + +TimeSpan.FromMilliseconds(777), filterResult.StartDate);
            Assert.AreEqual(DateTimeKind.Utc, filterResult.StartDate.Value.Kind);
            Assert.IsNull(filterResult.EndDate);
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void GreaterThanDateTime()
        {
            var filterResult = DateFilterResult.ParseFromFilter("datetime gt datetime'2016-04-01T13:32:56.123'");
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void UnknownComparator()
        {
            var filterResult = DateFilterResult.ParseFromFilter("datetime foo datetime'2016-04-01T13:32:56.22'");
        }

        [TestMethod]
        public void BetweenDateTime()
        {
            var filterResult = DateFilterResult.ParseFromFilter("date ge datetime'2016-03-31T01:02:03.4444444' and date le datetime'2016-04-01T23:22:21.555'");

            Assert.AreEqual(Comparison.Between, filterResult.Comparison);
            Assert.AreEqual(new DateTime(2016, 3, 31, 1, 2, 3, DateTimeKind.Utc) + TimeSpan.FromTicks(4444444L), filterResult.StartDate);
            Assert.AreEqual(DateTimeKind.Utc, filterResult.StartDate.Value.Kind);
            Assert.AreEqual(new DateTime(2016, 4, 1, 23, 22, 21, DateTimeKind.Utc) + TimeSpan.FromMilliseconds(555), filterResult.EndDate);
            Assert.AreEqual(DateTimeKind.Utc, filterResult.EndDate.Value.Kind);
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void FilterOfCompleteGarbage()
        {
            var filterResult = DateFilterResult.ParseFromFilter("just some random text here");
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void BackwardsDateRanges()
        {
            var filterResult = DateFilterResult.ParseFromFilter("date le datetime'2016-03-31T01:02:03' and date ge datetime'2016-04-01T23:22:21.666'");

            Assert.AreEqual(Comparison.Between, filterResult.Comparison);
            Assert.AreEqual(new DateTime(2016, 3, 31, 1, 2, 3, DateTimeKind.Utc), filterResult.StartDate);
            Assert.AreEqual(DateTimeKind.Utc, filterResult.StartDate.Value.Kind);
            Assert.AreEqual(new DateTime(2016, 4, 1, 23, 22, 21, DateTimeKind.Utc) + TimeSpan.FromMilliseconds(666), filterResult.EndDate);
            Assert.AreEqual(DateTimeKind.Utc, filterResult.EndDate.Value.Kind);
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void NonOverlappingDates()
        {
            var filterResult = DateFilterResult.ParseFromFilter("date ge datetime'2016-04-01' and date le datetime'2016-03-31'");
        }
    }
}

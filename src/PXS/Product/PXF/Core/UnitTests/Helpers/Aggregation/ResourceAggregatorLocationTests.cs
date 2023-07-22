// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;
    using Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.Factory;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// ResourceAggregator-Location Tests
    /// </summary>
    [TestClass]
    public class ResourceAggregatorLocationTests : CoreServiceTestBase
    {
        [TestMethod]
        public void AggregateLocationNoItemsReturnsNull()
        {
            IList<LocationHistoryV1> items = null;
            ResourceAggregator.Aggregate(ref items, 10);
            Assert.IsNull(items);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void AggregateLocationInvalidMatchDistanceThrowsArgumentOutOfRangeException()
        {
            try
            {
                IList<LocationHistoryV1> items = new List<LocationHistoryV1>();
                ResourceAggregator.Aggregate(ref items, -1);
                Assert.Fail("Exception should have thrown.");
            }
            catch (ArgumentOutOfRangeException e)
            {
                Assert.AreEqual("Match distance must be positive.\r\nParameter name: matchDistance", e.Message);
                throw;
            }
        }

        [TestMethod]
        public void AggregateLocationMultiplePointsMergesToOne()
        {
            IList<LocationHistoryV1> items = new List<LocationHistoryV1>
            {
                LocationHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), latitude: 23.4567, longitude: 45.6789),
                LocationHistoryV1Factory.Create(DateTime.Parse("2016-03-03"), latitude: 23.4567, longitude: 45.6789),
                LocationHistoryV1Factory.Create(DateTime.Parse("2016-05-05"), latitude: 23.4567, longitude: 45.6789),
                LocationHistoryV1Factory.Create(DateTime.Parse("2016-04-04"), latitude: 23.4567, longitude: 45.6789),
                LocationHistoryV1Factory.Create(DateTime.Parse("2016-02-02"), latitude: 23.4567, longitude: 45.6789)
            };
            ResourceAggregator.Aggregate(ref items, 0);
            
            // Aggregates to 1 point and the most-recent dateTime is kept.
            Assert.IsNotNull(items);
            Assert.AreEqual(1, items.Count);
            Assert.AreEqual(DateTime.Parse("2016-05-05"), items[0].DateTime.DateTime);
        }

        [TestMethod]
        public void AggregateLocationMultiplePointsMergesCorrectly()
        {
            IList<LocationHistoryV1> items = new List<LocationHistoryV1>
            {
                LocationHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), latitude: 23.4567, longitude: 45.6789),
                LocationHistoryV1Factory.Create(DateTime.Parse("2016-03-03"), latitude: 0.4567, longitude: 0.6789),
                LocationHistoryV1Factory.Create(DateTime.Parse("2016-04-04"), latitude: 23.4567, longitude: 45.6789),
                LocationHistoryV1Factory.Create(DateTime.Parse("2016-02-02"), latitude: 23.4567, longitude: 45.6789),
                LocationHistoryV1Factory.Create(DateTime.Parse("2000-01-01"), latitude: 0.4567, longitude: 0.6789)
            };
            ResourceAggregator.Aggregate(ref items, 0);

            // Aggregates to 2 point and the most-recent dateTime is kept.
            Assert.IsNotNull(items);
            Assert.AreEqual(2, items.Count);
            Assert.AreEqual(DateTime.Parse("2016-03-03"), items[0].DateTime.DateTime);
            Assert.AreEqual(DateTime.Parse("2016-04-04"), items[1].DateTime.DateTime);
        }

        [TestMethod]
        public void AggregateLocationMultiplePointsMergesToOneAndAggregatesChildNodes()
        {
            IList<LocationHistoryV1> items = new List<LocationHistoryV1>
            {
                LocationHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), latitude: 23.4567, longitude: 45.6789),
                LocationHistoryV1Factory.Create(DateTime.Parse("2016-03-03"), latitude: 23.4567, longitude: 45.6789),
                LocationHistoryV1Factory.Create(DateTime.Parse("2016-05-05"), latitude: 23.4567, longitude: 45.6789),
            };
            ResourceAggregator.Aggregate(ref items, 0);
            
            // Aggregates to 1 point and the most-recent dateTime is kept.
            Assert.IsNotNull(items);
            Assert.AreEqual(1, items.Count);
            Assert.AreEqual(DateTime.Parse("2016-05-05"), items[0].DateTime.DateTime);

            Assert.IsNotNull(items[0].AggregateHistory);
            var aggregateHistory = items[0].AggregateHistory.ToList();

            Assert.AreEqual(3, aggregateHistory.Count);
            Assert.AreEqual(DateTime.Parse("2016-05-05"), aggregateHistory[0].DateTime.DateTime);
            Assert.AreEqual(DateTime.Parse("2016-03-03"), aggregateHistory[1].DateTime.DateTime);
            Assert.AreEqual(DateTime.Parse("2016-01-01"), aggregateHistory[2].DateTime.DateTime);
        }

        [TestMethod]
        public void AggregateLocationMultiplePointsCalculateCategorySameType()
        {
            IList<LocationHistoryV1> items = new List<LocationHistoryV1>
            {
                LocationHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), latitude: 23.4567, longitude: 45.6789, category: LocationCategory.Device),
                LocationHistoryV1Factory.Create(DateTime.Parse("2016-03-03"), latitude: 23.4567, longitude: 45.6789, category: LocationCategory.Device),
            };
            ResourceAggregator.Aggregate(ref items, 0);
            
            // Aggregates to 1 point and the most-recent dateTime is kept.
            Assert.IsNotNull(items);
            Assert.AreEqual(1, items.Count);
            Assert.AreEqual(DateTime.Parse("2016-03-03"), items[0].DateTime.DateTime);

            Assert.IsNotNull(items[0].AggregateHistory);
            var aggregateHistory = items[0].AggregateHistory.ToList();

            Assert.AreEqual(2, aggregateHistory.Count);
            Assert.AreEqual(DateTime.Parse("2016-03-03"), aggregateHistory[0].DateTime.DateTime);
            Assert.AreEqual(DateTime.Parse("2016-01-01"), aggregateHistory[1].DateTime.DateTime);

            // Verify category is the same
            Assert.AreEqual(LocationCategory.Device, items[0].Category);
        }

        [TestMethod]
        public void AggregateLocationMultiplePointsCalculateCategoryMixed()
        {
            IList<LocationHistoryV1> items = new List<LocationHistoryV1>
            {
                LocationHistoryV1Factory.Create(DateTime.Parse("2016-01-01"), latitude: 23.4567, longitude: 45.6789, category: LocationCategory.Device),
                LocationHistoryV1Factory.Create(DateTime.Parse("2016-03-03"), latitude: 23.4567, longitude: 45.6789, category: LocationCategory.ProcessedLog),
            };
            ResourceAggregator.Aggregate(ref items, 0);
            
            // Aggregates to 1 point and the most-recent dateTime is kept.
            Assert.IsNotNull(items);
            Assert.AreEqual(1, items.Count);
            Assert.AreEqual(DateTime.Parse("2016-03-03"), items[0].DateTime.DateTime);

            Assert.IsNotNull(items[0].AggregateHistory);
            var aggregateHistory = items[0].AggregateHistory.ToList();

            Assert.AreEqual(2, aggregateHistory.Count);
            Assert.AreEqual(DateTime.Parse("2016-03-03"), aggregateHistory[0].DateTime.DateTime);
            Assert.AreEqual(DateTime.Parse("2016-01-01"), aggregateHistory[1].DateTime.DateTime);

            // Verify category is Mixed
            Assert.AreEqual(LocationCategory.Mixed, items[0].Category);
        }

        [TestMethod]
        public void TestDistanceCalculation()
        {
            IList<LocationHistoryV1> items = new List<LocationHistoryV1>
            {
                LocationHistoryV1Factory.Create(DateTime.Parse("2016-09-07T21:11:09+00:00"), latitude: 47.652004241943359, longitude: -122.13392639160156, category: LocationCategory.Device),
                LocationHistoryV1Factory.Create(DateTime.Parse("2016-09-08T13:02:17.3701053+00:00"), latitude: 47.6520057, longitude: -122.1339243, category: LocationCategory.ProcessedLog),
            };
            ResourceAggregator.Aggregate(ref items, 11);
            Assert.IsNotNull(items);
            Assert.AreEqual(1, items.Count);
        }
    }
}
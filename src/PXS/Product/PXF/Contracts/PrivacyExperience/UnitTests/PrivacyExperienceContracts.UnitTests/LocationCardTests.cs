// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperienceContracts.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// </summary>
    [TestClass]
    public class LocationCardTests
    {
        [TestMethod]
        public void LocationAggregationTest()
        {
            var card1 = new LocationCard(
                "name",
                new LocationCard.GeographyPoint(122.5, -42.5, 0.0),
                null,
                "activityType",
                null,
                new Uri("https://microsoft.com"),
                null,
                "deviceType",
                new List<LocationCard.LocationImpression>(),
                DateTimeOffset.UtcNow,
                null,
                null);

            var card2 = new LocationCard(
                "name",

                // this point is arbitrarily close to the first point, but just INSIDE the range
                new LocationCard.GeographyPoint(122.504496608, -42.5, 0.0),
                null,
                "activityType",
                null,
                new Uri("https://microsoft.com"),
                null,
                "deviceType",
                new List<LocationCard.LocationImpression>(),
                DateTimeOffset.UtcNow,
                null,
                null);

            // The first two cards should aggregate
            Assert.IsTrue(LocationCard.Aggregate(TimeSpan.Zero, card1, card2));
            Assert.AreEqual(1, card1.AdditionalLocations.Count);
            Assert.AreEqual(card2.Timestamp, card1.AdditionalLocations.First().Timestamp);

            var card3 = new LocationCard(
                "name",

                // this point is arbitrarily close to the first point, but just OUTSIDE the range
                new LocationCard.GeographyPoint(122.504496609, -42.5, 0.0),
                null,
                "activityType",
                null,
                new Uri("https://microsoft.com"),
                null,
                "deviceType",
                new List<LocationCard.LocationImpression>(),
                DateTimeOffset.UtcNow,
                null,
                null);

            // This card will not aggregate, it's just out of range.
            Assert.IsFalse(LocationCard.Aggregate(TimeSpan.Zero, card1, card3));

            var card4 = new LocationCard(
                "name",

                // this point is arbitrarily close to the first point, but just INSIDE the range
                new LocationCard.GeographyPoint(122.5, -42.508368902, 0.0),
                null,
                "activityType",
                null,
                new Uri("https://microsoft.com"),
                null,
                "deviceType",
                new List<LocationCard.LocationImpression>(),
                DateTimeOffset.UtcNow,
                null,
                null);

            // This card will aggregate, it's just inside the range on the latitude dimension
            Assert.IsTrue(LocationCard.Aggregate(TimeSpan.Zero, card1, card4));
            Assert.AreEqual(2, card1.AdditionalLocations.Count);
            Assert.AreEqual(card4.Timestamp, card1.AdditionalLocations.ToList()[1].Timestamp);

            var card5 = new LocationCard(
                "name",

                // this point is arbitrarily close to the first point, but just OUTSIDE the range
                new LocationCard.GeographyPoint(122.5, -42.508368903, 0.0),
                null,
                "activityType",
                null,
                new Uri("https://microsoft.com"),
                null,
                "deviceType",
                new List<LocationCard.LocationImpression>(),
                DateTimeOffset.UtcNow,
                null,
                null);

            // This card will not aggregate, it's just out of range.
            Assert.IsFalse(LocationCard.Aggregate(TimeSpan.Zero, card1, card5));

            Assert.AreEqual(2, card1.AdditionalLocations.Count);
        }

        [TestMethod]
        public void LocationCardIdSerializationTest()
        {
            var locationImpressions = new List<LocationCard.LocationImpression>
            {
                new LocationCard.LocationImpression(new LocationCard.GeographyPoint(1, 2, 3), DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(42)),
                new LocationCard.LocationImpression(new LocationCard.GeographyPoint(4, 5, 6), DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(42)),
                new LocationCard.LocationImpression(new LocationCard.GeographyPoint(7, 8, 9), DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(42))
            };

            var card = new LocationCard(
                "name",
                new LocationCard.GeographyPoint(42, 43, 44),
                45,
                "activityType",
                DateTimeOffset.UtcNow,
                new Uri("https://microsoft.com"),
                46,
                "deviceType",
                locationImpressions,
                DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(42),
                new List<string> { "deviceId1", "deviceId2" },
                new List<string> { "source1", "source2" });

            var roundTrip = (LocationCard)TimelineCard.DeserializeId(card.Id);

            Assert.AreEqual(card.Timestamp, roundTrip.Timestamp);
            Assert.AreEqual(card.Location.Latitude, roundTrip.Location.Latitude);
            Assert.AreEqual(card.Location.Longitude, roundTrip.Location.Longitude);
            Assert.IsNotNull(card.AdditionalLocations);
            Assert.AreEqual(card.AdditionalLocations?.Count, roundTrip.AdditionalLocations?.Count);
            List<LocationCard.LocationImpression> cardAdditionalLocations = card.AdditionalLocations.ToList();
            Assert.IsNotNull(roundTrip.AdditionalLocations);
            List<LocationCard.LocationImpression> roundTripAdditionalLocations = roundTrip.AdditionalLocations.ToList();
            for (int i = 0; i < card.AdditionalLocations?.Count; i++)
            {
                Assert.AreEqual(cardAdditionalLocations[i].Latitude, roundTripAdditionalLocations[i].Latitude);
                Assert.AreEqual(cardAdditionalLocations[i].Longitude, roundTripAdditionalLocations[i].Longitude);
                Assert.AreEqual(cardAdditionalLocations[i].Timestamp, roundTripAdditionalLocations[i].Timestamp);
            }

            Assert.AreEqual(null, roundTrip.DeviceType); // extra fields shouldn't be part of the id
        }

        [ExpectedException(typeof(ArgumentNullException))]
        public void LocationCardNullLocationImpressionsTest()
        {
            // Tests null additional locations throws when doing aggregate.
            var card1 = new LocationCard(
                "name",
                new LocationCard.GeographyPoint(122.5, -42.5, 0.0),
                null,
                "activityType",
                null,
                new Uri("https://microsoft.com"),
                null,
                "deviceType",
                null,
                DateTimeOffset.UtcNow,
                null,
                null);

            var card2 = new LocationCard(
                "name",

                // this point is arbitrarily close to the first point, but just INSIDE the range
                new LocationCard.GeographyPoint(122.504496608, -42.5, 0.0),
                null,
                "activityType",
                null,
                new Uri("https://microsoft.com"),
                null,
                "deviceType",
                null,
                DateTimeOffset.UtcNow,
                null,
                null);

            // Prevent null location impressions
            try
            {
                Assert.IsTrue(LocationCard.Aggregate(TimeSpan.Zero, card1, card2));
                Assert.Fail("Should have thrown");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentNullException);
                var argumentNullException = e as ArgumentNullException;
                Assert.AreEqual(
                    "The card location impressions is null." + Environment.NewLine +
                    "Parameter name: AdditionalLocations",
                    argumentNullException.Message);
                throw;
            }
        }

        [TestMethod]
        public void LocationCardSerializationTest()
        {
            var locationImpressions = new List<LocationCard.LocationImpression>
            {
                new LocationCard.LocationImpression(new LocationCard.GeographyPoint(1, 2, 3), DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(42)),
                new LocationCard.LocationImpression(new LocationCard.GeographyPoint(4, 5, 6), DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(42)),
                new LocationCard.LocationImpression(new LocationCard.GeographyPoint(7, 8, 9), DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(42))
            };
            var card = new LocationCard(
                "name",
                new LocationCard.GeographyPoint(42, 43, 44),
                45,
                "activityType",
                DateTimeOffset.UtcNow,
                new Uri("https://microsoft.com"),
                46,
                "deviceType",
                locationImpressions,
                DateTimeOffset.UtcNow + TimeSpan.FromMinutes(42),
                new List<string> { "deviceId1", "deviceId2" },
                new List<string> { "source1", "source2" });

            string cardJson = JsonConvert.SerializeObject(card);

            // Double check we are sending the id
            JToken token = JToken.Parse(cardJson);
            Assert.IsNotNull(token["Id"]);

            var roundTrip = JsonConvert.DeserializeObject<LocationCard>(cardJson);

            // Double check we are receiving the id and not re-creating it when calling the public property getter
            FieldInfo fieldInfo = typeof(TimelineCard).GetField("id", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fieldInfo);
            Assert.IsNotNull(fieldInfo.GetValue(roundTrip));

            Assert.AreEqual(card.Id, roundTrip.Id);
            Assert.AreEqual(card.Timestamp, roundTrip.Timestamp);
            Assert.AreEqual(card.DeviceType, roundTrip.DeviceType);
            Assert.AreEqual(card.Location.Altitude, roundTrip.Location.Altitude);
            Assert.AreEqual(card.Location.Latitude, roundTrip.Location.Latitude);
            Assert.AreEqual(card.Location.Longitude, roundTrip.Location.Longitude);
            Assert.AreEqual(card.DeviceIds.Count, roundTrip.DeviceIds.Count);
            Assert.AreEqual(card.Sources.Count, roundTrip.Sources.Count);
            Assert.AreEqual(card.AccuracyRadius, roundTrip.AccuracyRadius);
            Assert.AreEqual(card.ActivityType, roundTrip.ActivityType);
            Assert.AreEqual(card.Distance, roundTrip.Distance);
            Assert.AreEqual(card.EndDateTime, roundTrip.EndDateTime);
            Assert.AreEqual(card.Name, roundTrip.Name);
            Assert.AreEqual(card.Url, roundTrip.Url);
            Assert.AreEqual(card.AdditionalLocations.Count, roundTrip.AdditionalLocations.Count);
        }

        [TestMethod]
        public void MaxAggregation()
        {
            LocationCard card = this.CreateTestCard();
            for (int i = 0; i < 59; i++)
                Assert.IsTrue(TimelineCard.Aggregate(TimeSpan.Zero, card, this.CreateTestCard(i + 1)));
            Assert.IsFalse(TimelineCard.Aggregate(TimeSpan.Zero, card, this.CreateTestCard(100)));
        }

        private LocationCard CreateTestCard(int offset = 0)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow.Date.AddTicks(offset);
            return new LocationCard(
                "name",
                new LocationCard.GeographyPoint(42, 43, 44),
                45,
                "activityType",
                now,
                new Uri("https://microsoft.com"),
                46,
                "deviceType",
                new List<LocationCard.LocationImpression>(),
                now,
                new List<string> { "deviceId1", "deviceId2" },
                new List<string> { "source1", "source2" });
        }
    }
}

// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperienceContracts.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    [TestClass]
    public class AppUsageCardTests
    {
        private AppUsageCard CreateSerializationTestCard(IDictionary<string, IList<string>> propertyBag)
        {
            return new AppUsageCard(
                "appId",
                "aggregation",
                "appIconBackground",
                new Uri("https://appIconUri"),
                "appName",
                "appPublisher",
                DateTimeOffset.UtcNow.Date,
                DateTimeOffset.UtcNow.Date.AddDays(1),
                new List<string> { "deviceId" },
                new List<string> { "source1", "source2" },
                propertyBag);
        }

        [TestMethod]
        public void AppUsageCardIdSerializationTest()
        {
            AppUsageCard card = this.CreateSerializationTestCard(null);

            TimelineCard cardFromId = TimelineCard.DeserializeId(card.Id);

            Assert.IsInstanceOfType(cardFromId, typeof(AppUsageCard));

            var roundTrip = cardFromId as AppUsageCard;

            Assert.AreEqual(card.AppId, roundTrip.AppId);
            Assert.AreEqual(card.Timestamp, roundTrip.Timestamp);
            Assert.AreEqual(card.EndTimestamp, roundTrip.EndTimestamp);
            Assert.AreEqual(card.Aggregation, roundTrip.Aggregation);
            Assert.AreEqual(null, roundTrip.AppPublisher); // extra fields shouldn't be part of the id
        }

        [TestMethod]
        public void AppUsageCardRoundtripsNullPropertyBagViaId()
        {
            AppUsageCard card = this.CreateSerializationTestCard(null);

            var roundTrip = TimelineCard.DeserializeId(card.Id) as AppUsageCard;

            Assert.IsNull(roundTrip.PropertyBag);
        }

        [TestMethod]
        public void AppUsageCardRoundtripsEmptyPropertyBagViaId()
        {
            AppUsageCard card = this.CreateSerializationTestCard(new Dictionary<string, IList<string>>());

            var roundTrip = TimelineCard.DeserializeId(card.Id) as AppUsageCard;

            Assert.IsNotNull(roundTrip.PropertyBag);
            Assert.AreEqual(0, roundTrip.PropertyBag.Count);
        }

        [TestMethod]
        public void AppUsageCardRoundtripsNonEmptyPropertyBagViaId()
        {
            const string Key = "key";
            const string Val1 = "val1";
            const string Val2 = "val2";

            AppUsageCard card = this.CreateSerializationTestCard(
                new Dictionary<string, IList<string>> { { Key, new[] { Val1, Val2 } } });

            var roundTrip = TimelineCard.DeserializeId(card.Id) as AppUsageCard;
            
            Assert.AreEqual(1, roundTrip.PropertyBag.Count);
            Assert.IsTrue(roundTrip.PropertyBag.ContainsKey(Key));

            ICollection<string> values = roundTrip.PropertyBag[Key];

            Assert.AreEqual(2, values.Count);
            Assert.IsTrue(values.Contains(Val1));
            Assert.IsTrue(values.Contains(Val2));
        }

        [TestMethod]
        public void AppUsageCardSerializationTest()
        {
            AppUsageCard card = this.CreateSerializationTestCard(
                new Dictionary<string, IList<string>> { { "key", new[] { "V1" } } });

            string cardJson = JsonConvert.SerializeObject(card);

            // Double check we are sending the id
            JToken token = JToken.Parse(cardJson);
            Assert.IsNotNull(token["Id"]);

            var roundTrip = JsonConvert.DeserializeObject<AppUsageCard>(cardJson);

            // Double check we are receiving the id and not re-creating it when calling the public property getter
            FieldInfo fieldInfo = typeof(TimelineCard).GetField("id", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fieldInfo);
            Assert.IsNotNull(fieldInfo.GetValue(roundTrip));

            // we never attempt to extract the property bag into an id when deserializing a serialized timecard object as it is
            //  expected that this kind of deserialization only occurs on the client and the client never needs the contents of
            //  the property bag.
            Assert.IsNull(roundTrip.PropertyBag);

            Assert.AreEqual(card.Id, roundTrip.Id);
            Assert.AreEqual(card.AppId, roundTrip.AppId);
            Assert.AreEqual(card.Timestamp, roundTrip.Timestamp);
            Assert.AreEqual(card.EndTimestamp, roundTrip.EndTimestamp);
            Assert.AreEqual(card.DeviceIds.Count, roundTrip.DeviceIds.Count);
            Assert.AreEqual(card.Sources.Count, roundTrip.Sources.Count);
            Assert.AreEqual(card.AppIconBackground, roundTrip.AppIconBackground);
            Assert.AreEqual(card.AppIconUri, roundTrip.AppIconUri);
            Assert.AreEqual(card.AppName, roundTrip.AppName);
            Assert.AreEqual(card.AppPublisher, roundTrip.AppPublisher);
            Assert.AreEqual(card.Aggregation, roundTrip.Aggregation);
        }
    }
}

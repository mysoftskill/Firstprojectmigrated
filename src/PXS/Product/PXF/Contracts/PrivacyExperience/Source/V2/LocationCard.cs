// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    /// <summary>
    ///     The location card.
    /// </summary>
    public class LocationCard : TimelineCard
    {
        /// <summary>
        ///     A geographical point in a location card.
        /// </summary>
        public class GeographyPoint
        {
            public double Altitude { get; }

            public double Latitude { get; }

            public double Longitude { get; }

            public GeographyPoint(double latitude, double longitude, double altitude)
            {
                this.Latitude = latitude;
                this.Longitude = longitude;
                this.Altitude = altitude;
            }
        }

        /// <summary>
        ///     Represents a location impression. This contains only basic data such as: lat, long, and timestamp.
        /// </summary>
        public class LocationImpression
        {
            // Keep the JsonProperty names short since they are part of the id.

            [JsonProperty("la")]
            public double Latitude { get; }

            [JsonProperty("lo")]
            public double Longitude { get; }

            [JsonProperty("t")]
            public DateTimeOffset Timestamp { get; }

            [JsonConstructor]
            public LocationImpression(double latitude, double longitude, DateTimeOffset timestamp)
            {
                this.Latitude = latitude;
                this.Longitude = longitude;
                this.Timestamp = timestamp;
            }

            public LocationImpression(GeographyPoint point, DateTimeOffset timestamp)
                : this(point.Latitude, point.Longitude, timestamp)
            {
            }
        }

        /// <summary>
        ///     This is arbitrarily chosen. We can fiddle with this to get different aggregation results in the UX.
        /// </summary>
        private const int AggregateWithinMeters = 500;

        /// <summary>
        ///     Max aggregation within a single card.
        /// </summary>
        private const int MaxAggregationCount = 60;

        public static bool Aggregate(TimeSpan timeZoneOffset, LocationCard a, LocationCard b)
        {
            // Plus one below for the primary (non-additional) entry itself.
            if (((a.AdditionalLocations?.Count ?? 0) + 1) >= MaxAggregationCount)
                return false;

            if (a.Timestamp.ToOffset(timeZoneOffset).Date != b.Timestamp.ToOffset(timeZoneOffset).Date)
                return false;

            if (!ShouldAggregateNearPoints(a.Location, b.Location))
                return false;

            if (a.AdditionalLocations == null)
            {
                throw new ArgumentNullException(nameof(a.AdditionalLocations), "The card location impressions is null.");
            }

            a.AdditionalLocations.Add(new LocationImpression(b.Location, b.Timestamp));

            return true;
        }

        public static LocationCard FromKeyComponents(IDictionary<string, string> components)
        {
            if (components == null)
                throw new ArgumentNullException(nameof(components));

            if (!components.TryGetValue(KeyConstants.AdditionalLocations, out string locationImpressionsStr))
                throw new ArgumentOutOfRangeException(nameof(components), $"Missing key component: {KeyConstants.AdditionalLocations}");

            var additionalLocations = JsonConvert.DeserializeObject<IList<LocationImpression>>(locationImpressionsStr);

            if (!components.TryGetValue(KeyConstants.Id, out string idStr))
                throw new ArgumentOutOfRangeException(nameof(components), $"Missing key component: {KeyConstants.Id}");
            var primaryLocationImpression = JsonConvert.DeserializeObject<LocationImpression>(idStr);

            // NOTE: This does not read altitude from the id after deserialization. So a value of 0 is used for altititude. If altitude is ever used, it needs to be serialized as well.
            return new LocationCard(
                null,
                new GeographyPoint(primaryLocationImpression.Latitude, primaryLocationImpression.Longitude, 0),
                null,
                null,
                null,
                null,
                null,
                null,
                additionalLocations,
                primaryLocationImpression.Timestamp,
                null,
                null);
        }

        /// <summary>
        ///     The accuracy radius, if known.
        /// </summary>
        public double? AccuracyRadius { get; }

        /// <summary>
        ///     The activity type.
        /// </summary>
        public string ActivityType { get; }

        /// <summary>
        ///     Additional individual locations seen within this card. Based on aggregation logic, the card may or may not contain additional locations.
        /// </summary>
        public IList<LocationImpression> AdditionalLocations { get; }

        /// <summary>
        ///     The device type.
        /// </summary>
        public string DeviceType { get; }

        /// <summary>
        ///     The location distance, if known.
        /// </summary>
        public double? Distance { get; }

        /// <summary>
        ///     The activity end time, if known.
        /// </summary>
        public DateTimeOffset? EndDateTime { get; }

        /// <summary>
        ///     The location of the event.
        /// </summary>
        public GeographyPoint Location { get; }

        /// <summary>
        ///     The name of the location entry.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     The Url for the location entry.
        /// </summary>
        public Uri Url { get; }

        /// <summary>
        ///     Construct a LocationCard
        /// </summary>
        public LocationCard(
            string name,
            GeographyPoint location,
            double? accuracyRadius,
            string activityType,
            DateTimeOffset? endDateTime,
            Uri url,
            double? distance,
            string deviceType,
            IList<LocationImpression> additionalLocations,
            DateTimeOffset timestamp,
            IList<string> deviceIds,
            IList<string> sources)
            : this(
                name,
                location,
                accuracyRadius,
                activityType,
                endDateTime,
                url,
                distance,
                deviceType,
                additionalLocations,
                null,
                timestamp,
                deviceIds,
                sources)
        {
        }

        [JsonConstructor]
        private LocationCard(
            string name,
            GeographyPoint location,
            double? accuracyRadius,
            string activityType,
            DateTimeOffset? endDateTime,
            Uri url,
            double? distance,
            string deviceType,
            IList<LocationImpression> additionalLocations,
            string id,
            DateTimeOffset timestamp,
            IList<string> deviceIds,
            IList<string> sources)
            : base(id, timestamp, deviceIds, sources)
        {
            this.DeviceType = deviceType;
            this.Name = name;
            this.Location = location;
            this.AccuracyRadius = accuracyRadius;
            this.ActivityType = activityType;
            this.EndDateTime = endDateTime;
            this.Url = url;
            this.Distance = distance;
            this.AdditionalLocations = additionalLocations;
        }

        protected override IDictionary<string, string> GetKeyComponents()
        {
            return new Dictionary<string, string>
            {
                {
                    KeyConstants.Id,
                    JsonConvert.SerializeObject(new LocationImpression(this.Location, this.Timestamp))
                },
                {
                    KeyConstants.AdditionalLocations,
                    JsonConvert.SerializeObject(this.AdditionalLocations)
                }
            };
        }

        /// <summary>
        ///     By https://en.wikipedia.org/wiki/Haversine_formula, are two points within <see cref="AggregateWithinMeters" /> meters of each other?
        /// </summary>
        /// <param name="a">First point</param>
        /// <param name="b">Second point</param>
        /// <returns>True if the points are within <see cref="AggregateWithinMeters" /> meters within each other.</returns>
        private static bool ShouldAggregateNearPoints(GeographyPoint a, GeographyPoint b)
        {
            // This is the essence of aggregation on timeline, which is different than the 'Sliding Circle' implementation used in v1.
            // This is simpler, and every other timeline aggregation is already n-squared through the results. When pages are only 100 items
            // this isn't a big deal, and I'd just rather aggregation was simpler. This algorithm also doesn't have any kind of advanced clustering,
            // it's just a simple within X meters algorithm. The primary goal here is de-duping exact lat long matches, or maybe a few meters around
            // at your home.

            double meanEarthRadius = 6371e3;
            double d2r = (Math.PI / 180.0);

            double aLat = a.Latitude * d2r;
            double bLat = b.Latitude * d2r;
            double latDelta = (b.Latitude - a.Latitude) * d2r;
            double longDelta = (b.Longitude - a.Longitude) * d2r;

            double x = Math.Sin(latDelta / 2) * Math.Sin(latDelta / 2) +
                       Math.Cos(aLat) * Math.Cos(bLat) *
                       Math.Sin(longDelta / 2) * Math.Sin(longDelta / 2);
            double y = 2 * Math.Atan2(Math.Sqrt(x), Math.Sqrt(1 - x));
            double z = meanEarthRadius * y;

            return z < AggregateWithinMeters;
        }
    }
}

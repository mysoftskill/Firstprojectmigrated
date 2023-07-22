// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Location V1
    /// </summary>
    public class LocationV1 : ResourceV1
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LocationV1"/> class.
        /// </summary>
        public LocationV1()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocationV1"/> class.
        /// </summary>
        /// <param name="value">The <see cref="LocationV1"/> value.</param>
        public LocationV1(LocationV1 value)
        {
            this.Name = value.Name;
            this.Address = value.Address;
            this.Latitude = value.Latitude;
            this.Longitude = value.Longitude;
            this.AccuracyRadius = value.AccuracyRadius;
            this.Category = value.Category;
            this.LocationType = value.LocationType;
            this.DeviceType = value.DeviceType;
            this.ActivityType = value.ActivityType;
            this.EndDateTime = value.EndDateTime;
            this.Url = value.Url;
            this.Distance = value.Distance;
            this.Ids = value.Ids;
            this.DateTime = value.DateTime;
            this.DeviceId = value.DeviceId;
            this.Source = value.Source;
            this.PartnerId = value.PartnerId;
            this.IsAggregate = value.IsAggregate;
        }

        /// <summary>
        /// Gets or sets the name of the location, if known.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the address, if known.
        /// </summary>
        [JsonProperty("address")]
        public AddressV1 Address { get; set; }

        /// <summary>
        /// Gets or sets the latitude.
        /// </summary>
        [JsonProperty("latitude")]
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude.
        /// </summary>
        [JsonProperty("longitude")]
        public double Longitude { get; set; }

        /// <summary>
        /// Gets or sets the accuracy radius in meters.
        /// </summary>
        [JsonProperty("accuracyRadius")]
        public long? AccuracyRadius { get; set; }

        /// <summary>
        /// Gets or sets the category.
        /// </summary>
        [JsonProperty("category")]
        [JsonConverter(typeof(StringEnumConverter))]
        public LocationCategory Category { get; set; }

        /// <summary>
        /// Type of location reading
        /// </summary>
        [JsonProperty("locationType", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        public LocationEnumsV1.LocationTypeV1? LocationType { get; set; }

        /// <summary>
        /// Type of device the reading is from
        /// </summary>
        [JsonProperty("deviceType", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        public LocationEnumsV1.LocationDeviceTypeV1? DeviceType { get; set; }

        /// <summary>
        /// Type of health activity performed
        /// </summary>
        [JsonProperty("activityType", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        public LocationEnumsV1.LocationActivityTypeV1? ActivityType { get; set; }

        /// <summary>
        /// Duration at location, or duration of activity or trip
        /// </summary>
        [JsonProperty("duration", NullValueHandling = NullValueHandling.Ignore)]
        [Obsolete("No longer supported.")]
        public TimeSpan? Duration { get; set; }

        /// <summary>
        /// End time. Used only in cases where there is a specified start and end time at or near this location.
        /// </summary>
        [JsonProperty("endDateTime", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? EndDateTime { get; set; }

        /// <summary>
        /// Deep link URL
        /// </summary>
        [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
        public string Url { get; set; }

        /// <summary>
        /// Distance of activity or trip
        /// </summary>
        [JsonProperty("distance", NullValueHandling = NullValueHandling.Ignore)]
        public int? Distance { get; set; }
    }
}
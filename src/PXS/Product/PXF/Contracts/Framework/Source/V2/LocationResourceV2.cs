// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.DataContracts.V2
{
    using System;
    using System.Spatial;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    ///     Location resource record V2
    /// </summary>
    public class LocationResourceV2 : PrivacyResourceV2
    {
        /// <summary>
        ///     Gets or sets the accuracy radius in meters.
        /// </summary>
        [JsonProperty("accuracyRadius")]
        public double? AccuracyRadius { get; set; }

        /// <summary>
        ///     Type of health activity performed
        /// </summary>
        [JsonProperty("activityType", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        public LocationEnumsV2.LocationActivityTypeV2? ActivityType { get; set; }

        /// <summary>
        ///     Type of device the reading is from
        /// </summary>
        [JsonProperty("deviceType", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        public LocationEnumsV2.LocationDeviceTypeV2? DeviceType { get; set; }

        /// <summary>
        ///     Distance of activity or trip
        /// </summary>
        [JsonProperty("distance", NullValueHandling = NullValueHandling.Ignore)]
        public double? Distance { get; set; }

        /// <summary>
        ///     End time. Used only in cases where there is a specified start and end time at or near this location.
        /// </summary>
        [JsonProperty("endDateTime", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? EndDateTime { get; set; }

        /// <summary>
        ///     Gets or sets the latlon
        /// </summary>
        [JsonProperty("location")]
        [JsonConverter(typeof(GeographyPointConverter))]
        public GeographyPoint Location { get; set; }

        /// <summary>
        ///     Type of location reading
        /// </summary>
        [JsonProperty("locationType", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        public LocationEnumsV2.LocationTypeV2? LocationType { get; set; }

        /// <summary>
        ///     Gets or sets the name of the location, if known.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        ///     Deep link URL
        /// </summary>
        [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
        public string Url { get; set; }
    }
}

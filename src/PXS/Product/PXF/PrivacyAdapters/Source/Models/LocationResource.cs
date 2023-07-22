// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Models
{
    using System;

    public enum LocationActivityType
    {
        Unspecified = 0,
        Hike = 1,
        Run = 2,
        Bike = 3,
    }

    public enum LocationType
    {
        Unknown = 0,
        Device = 1,
        Implicit = 2,
        Fitness = 3,
        Favorite = 4,
    }

    public enum LocationDeviceType
    {
        Unknown = 0,
        Phone = 1,
        Tablet = 2,
        PC = 3,
        Console = 4,
        Laptop = 5,
        Accessory = 6,
        Wearable = 7,
        SurfaceHub = 8,
        HeadMountedDisplay = 9,
    }

    /// <summary>
    /// Location resource record V1
    /// </summary>
    public sealed class LocationResource : Resource
    {
        /// <summary>
        /// Gets or sets the name of the location, if known.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the address, if known.
        /// </summary>
        public Address Address { get; set; }

        /// <summary>
        /// Gets or sets the latitude.
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude.
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Gets or sets the accuracy radius in meters.
        /// </summary>
        public double? AccuracyRadius { get; set; }

        /// <summary>
        /// Type of location reading
        /// </summary>
        public LocationType? LocationType { get; set; }

        /// <summary>
        /// Type of device the reading is from
        /// </summary>
        public LocationDeviceType? DeviceType { get; set; }

        /// <summary>
        /// Type of health activity performed
        /// </summary>
        public LocationActivityType? ActivityType { get; set; }

        /// <summary>
        /// Duration at location, or duration of activity or trip
        /// </summary>
        [Obsolete("This value is no longer supported.")]
        public TimeSpan? Duration { get; set; }

        /// <summary>
        /// End time. Used only in cases where there is a specified start and end time at or near this location.
        /// </summary>
        public DateTimeOffset? EndDateTime { get; set; }

        /// <summary>
        /// Deep link URL
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Distance of activity or trip
        /// </summary>
        public int? Distance { get; set; }
    }
}

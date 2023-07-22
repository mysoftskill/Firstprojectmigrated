// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.Factory
{
    using System;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;

    /// <summary>
    /// LocationHistoryV1Factory
    /// </summary>
    public static class LocationHistoryV1Factory
    {
        public static LocationHistoryV1 Create(
            DateTime dateTime, 
            string partnerId = "P1",
            double latitude = 0d,
            double longitude = 0d,
            LocationEnumsV1.LocationTypeV1 locationType = LocationEnumsV1.LocationTypeV1.Unknown,
            LocationCategory category = LocationCategory.Unknown)
        {
            return Create(dateTime, new[] { Guid.NewGuid().ToString() }, partnerId, latitude, longitude, locationType, category);
        }

        public static LocationHistoryV1 Create(
            DateTime dateTime, 
            string[] ids, 
            string partnerId = "P1", 
            double latitude = 0d, 
            double longitude = 0d,
            LocationEnumsV1.LocationTypeV1 locationType = LocationEnumsV1.LocationTypeV1.Unknown,
            LocationCategory category = LocationCategory.Unknown,
            string deviceId = "")
        {
            return new LocationHistoryV1
            {
                PartnerId = partnerId,
                DateTime = dateTime,
                Ids = ids,
                IsAggregate = false,
                DeviceId = deviceId,
                Latitude = latitude,
                Longitude = longitude,
                LocationType = locationType,
                AccuracyRadius = 0,
                Category = category
            };
        }
    }
}
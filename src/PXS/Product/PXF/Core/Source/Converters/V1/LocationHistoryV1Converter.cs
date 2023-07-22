// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Converters
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     LocationHistoryV1 Converter
    /// </summary>
    public static class LocationHistoryV1Converter
    {
        private const string ComponentName = nameof(LocationHistoryV1Converter);

        /// <summary>
        ///     Converts <see cref="LocationResource" /> to <see cref="LocationHistoryV1" />.
        /// </summary>
        /// <param name="locationResource">The location resource.</param>
        /// <param name="categoryMapping">The category mapping.</param>
        /// <returns>LocationHistoryV1</returns>
        public static LocationHistoryV1 ToLocationHistoryV1(this LocationResource locationResource, Dictionary<string, PxfLocationCategory> categoryMapping)
        {
            LocationHistoryV1 locationHistoryV1 = new LocationHistoryV1();
            locationHistoryV1.AccuracyRadius = locationResource.AccuracyRadius.HasValue ? (long)locationResource.AccuracyRadius.Value : (long?)null;
            locationHistoryV1.Address = locationResource.Address.ToAddressV1();
            locationHistoryV1.Latitude = locationResource.Latitude;
            locationHistoryV1.Longitude = locationResource.Longitude;
            locationHistoryV1.Name = locationResource.Name;
            locationHistoryV1.DateTime = locationResource.DateTime;
            locationHistoryV1.DeviceId = locationResource.DeviceId;
            locationHistoryV1.Source = locationResource.Sources?.FirstOrDefault();
            locationHistoryV1.PartnerId = locationResource.PartnerId;
            locationHistoryV1.LocationType = ConvertLocationType(locationResource.LocationType);
            locationHistoryV1.DeviceType = ConvertLocationDeviceType(locationResource.DeviceType);
            locationHistoryV1.ActivityType = ConvertLocationActivityType(locationResource.ActivityType);
            locationHistoryV1.EndDateTime = locationResource.EndDateTime;
            locationHistoryV1.Url = locationResource.Url;
            locationHistoryV1.Distance = locationResource.Distance;
            locationHistoryV1.Id = locationHistoryV1.Id;
            var ids = new List<string> { locationResource.Id };
            locationHistoryV1.Ids = ids;
            locationHistoryV1.IsAggregate = ids.Count > 1;

            locationHistoryV1.Category = MapPartnerIdToCategory(locationResource.PartnerId, categoryMapping);

            return locationHistoryV1;
        }

        private static LocationEnumsV1.LocationActivityTypeV1? ConvertLocationActivityType(LocationActivityType? from)
        {
            if (!from.HasValue)
            {
                return null;
            }

            switch (from)
            {
                case LocationActivityType.Hike: return LocationEnumsV1.LocationActivityTypeV1.Hike;
                case LocationActivityType.Run: return LocationEnumsV1.LocationActivityTypeV1.Run;
                case LocationActivityType.Bike: return LocationEnumsV1.LocationActivityTypeV1.Bike;
            }

            return LocationEnumsV1.LocationActivityTypeV1.Unspecified;
        }

        private static LocationEnumsV1.LocationDeviceTypeV1? ConvertLocationDeviceType(LocationDeviceType? from)
        {
            if (!from.HasValue)
            {
                return null;
            }

            switch (from)
            {
                case LocationDeviceType.Phone: return LocationEnumsV1.LocationDeviceTypeV1.Phone;
                case LocationDeviceType.Tablet: return LocationEnumsV1.LocationDeviceTypeV1.Tablet;
                case LocationDeviceType.PC: return LocationEnumsV1.LocationDeviceTypeV1.PC;
                case LocationDeviceType.Console: return LocationEnumsV1.LocationDeviceTypeV1.Console;
                case LocationDeviceType.Laptop: return LocationEnumsV1.LocationDeviceTypeV1.Laptop;
                case LocationDeviceType.Accessory: return LocationEnumsV1.LocationDeviceTypeV1.Accessory;
                case LocationDeviceType.Wearable: return LocationEnumsV1.LocationDeviceTypeV1.Wearable;
                case LocationDeviceType.SurfaceHub: return LocationEnumsV1.LocationDeviceTypeV1.SurfaceHub;
                case LocationDeviceType.HeadMountedDisplay: return LocationEnumsV1.LocationDeviceTypeV1.HeadMountedDisplay;
            }

            return LocationEnumsV1.LocationDeviceTypeV1.Unknown;
        }

        private static LocationEnumsV1.LocationTypeV1? ConvertLocationType(LocationType? from)
        {
            if (!from.HasValue)
            {
                return null;
            }

            switch (from)
            {
                case LocationType.Device: return LocationEnumsV1.LocationTypeV1.Device;
                case LocationType.Implicit: return LocationEnumsV1.LocationTypeV1.Implicit;
                case LocationType.Fitness: return LocationEnumsV1.LocationTypeV1.Fitness;
                case LocationType.Favorite: return LocationEnumsV1.LocationTypeV1.Favorite;
            }

            return LocationEnumsV1.LocationTypeV1.Unknown;
        }

        internal static LocationCategory MapPartnerIdToCategory(string partnerId, Dictionary<string, PxfLocationCategory> categoryMapping)
        {
            PxfLocationCategory pxfLocationCategory;

            if (!string.IsNullOrWhiteSpace(partnerId)
                && categoryMapping.TryGetValue(partnerId, out pxfLocationCategory))
            {
                switch (pxfLocationCategory)
                {
                    case PxfLocationCategory.Device:
                        return LocationCategory.Device;

                    case PxfLocationCategory.Search:
                        return LocationCategory.Search;

                    case PxfLocationCategory.Favorite:
                        return LocationCategory.Favorite;

                    case PxfLocationCategory.Inferred:
                        return LocationCategory.Inferred;

                    case PxfLocationCategory.ProcessedLog:
                        return LocationCategory.ProcessedLog;

                    case PxfLocationCategory.Fitness:
                        return LocationCategory.Fitness;
                }
            }

            return LocationCategory.Unknown;
        }

        internal static AddressV1 ToAddressV1(this Address addressResource)
        {
            if (addressResource == null)
            {
                return null;
            }

            AddressV1 addressV1 = new AddressV1();
            addressV1.AddressLine1 = addressResource.AddressLine1;
            addressV1.AddressLine2 = addressResource.AddressLine2;
            addressV1.AddressLine3 = addressResource.AddressLine3;
            addressV1.CountryRegion = addressResource.CountryRegion;
            addressV1.CountryRegionIso2 = addressResource.CountryRegionIso2;
            addressV1.FormattedAddress = addressResource.FormattedAddress;
            addressV1.Locality = addressResource.Locality;
            addressV1.PostalCode = addressResource.PostalCode;

            return addressV1;
        }

        /// <summary>
        ///     Converts a collection of <see cref="LocationResource" /> to a collection of <see cref="LocationHistoryV1" />.
        /// </summary>
        /// <param name="resources">The location resources.</param>
        /// <param name="categoryMapping">The location category mapping.</param>
        /// <param name="maxAccuracyRadius">The maximum accuracy radius allowed.</param>
        /// <returns>
        ///     A collection of <see cref="LocationHistoryV1" />
        /// </returns>
        internal static List<LocationHistoryV1> ToLocationHistoryV1(
            this IEnumerable<LocationResource> resources,
            Dictionary<string, PxfLocationCategory> categoryMapping,
            long maxAccuracyRadius)
        {
            if (resources == null)
            {
                return null;
            }

            var resourceListV1 = new List<LocationHistoryV1>();

            foreach (var resource in resources)
            {
                if (resource.AccuracyRadius == null)
                {
                    IfxTraceLogger.Instance.Information(
                        ComponentName,
                        "Location accuracy radius is null. DeviceId: '{0}', RequestId: {1}",
                        resource.DeviceId,
                        LogicalWebOperationContext.ServerActivityId.ToString());

                    // null radius is okay to use.
                    resourceListV1.Add(resource.ToLocationHistoryV1(categoryMapping));
                }
                else if (resource.AccuracyRadius <= maxAccuracyRadius)
                {
                    // location is precise enough to use.
                    resourceListV1.Add(resource.ToLocationHistoryV1(categoryMapping));
                }
                else
                {
                    // don't convert any data that has an accuracy radius > than the defined max
                    IfxTraceLogger.Instance.Information(
                        ComponentName,
                        "Location discarded due to exceeding radius threshold. Radius: '{0}', DeviceId: '{1}', RequestId: {2}",
                        resource.AccuracyRadius,
                        resource.DeviceId,
                        LogicalWebOperationContext.ServerActivityId.ToString());
                }
            }

            return resourceListV1;
        }
    }
}

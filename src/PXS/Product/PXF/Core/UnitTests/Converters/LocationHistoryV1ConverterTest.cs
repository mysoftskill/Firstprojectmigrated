// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Converters;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// LocationHistoryV1Converter Test
    /// </summary>
    [TestClass]
    public class LocationHistoryV1ConverterTest
    {
        private Dictionary<string, PxfLocationCategory> categoryMapping = new Dictionary<string, PxfLocationCategory>();

        #region PrivacyAdapters.Models to ExperienceContracts.V1

        [TestMethod]
        public void ToLocationHistoryV1Test()
        {
            var expected = new LocationHistoryV1
            {
                DateTime = DateTimeOffset.Parse("3/1/2016 12:07:10 AM +00:00", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                IsAggregate = false,
                DeviceId = "global[343434]",
                Source = "Bing",
                Ids = new[] { "abc123" },
                AccuracyRadius = 300,
                Latitude = 12.34d,
                Longitude = 56.78d,
                Address = new AddressV1 { AddressLine1 = "a", AddressLine2 = "b", AddressLine3 = "c", PostalCode = "55555", Locality = "test_locality", CountryRegion = "test_country_region", CountryRegionIso2 = "test_country_region_iso2", FormattedAddress = "test_formatted_address" },
                Name = "test_location_name",
                PartnerId = "Mock Partner id",
                Category = LocationCategory.Unknown,
                LocationType = LocationEnumsV1.LocationTypeV1.Device,
                DeviceType = LocationEnumsV1.LocationDeviceTypeV1.Phone,
                ActivityType = LocationEnumsV1.LocationActivityTypeV1.Bike,
                EndDateTime = DateTimeOffset.Parse("3/1/2016 12:47:10 AM +00:00", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                Url = "https://myhealth/is/awesome",
                Distance = 34343,
            };

            LocationResource adapterLocationResource = new LocationResource
            {
                DateTime = DateTimeOffset.Parse("3/1/2016 12:07:10 AM +00:00", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                DeviceId = "global[343434]",
                Sources = new[] { "Bing" },
                Id = "abc123",
                Status = ResourceStatus.Active,
                Name = "test_location_name",
                AccuracyRadius = 300,
                Latitude = 12.34d,
                Longitude = 56.78d,
                Address = new Address { AddressLine1 = "a", AddressLine2 = "b", AddressLine3 = "c", PostalCode = "55555", Locality = "test_locality", CountryRegion = "test_country_region", CountryRegionIso2 = "test_country_region_iso2", FormattedAddress = "test_formatted_address" },
                PartnerId = "Mock Partner id",
                LocationType = LocationType.Device,
                DeviceType = LocationDeviceType.Phone,
                ActivityType = LocationActivityType.Bike,
                EndDateTime = DateTimeOffset.Parse("3/1/2016 12:47:10 AM +00:00", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                Url = "https://myhealth/is/awesome",
                Distance = 34343,
            };

            var actual = adapterLocationResource.ToLocationHistoryV1(this.categoryMapping);

            EqualityHelper.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ToLocationHistoryV1FilterOutLargeRadius()
        {
            List<LocationHistoryV1> expected = new List<LocationHistoryV1> { new LocationHistoryV1() };
            const int MaxRadius = 200;
            const int LargerThanMaxRadius = MaxRadius + 1;

            LocationResource adapterLocationResource = new LocationResource
            {
                DateTime = DateTimeOffset.Parse("3/1/2016 12:07:10 AM +00:00", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                DeviceId = "global[343434]",
                Sources = new[] { "Bing" },
                Id = "abc123",
                Status = ResourceStatus.Active,
                Name = "test_location_name",
                AccuracyRadius = LargerThanMaxRadius,
                Latitude = 12.34d,
                Longitude = 56.78d,
                Address = new Address { AddressLine1 = "a", AddressLine2 = "b", AddressLine3 = "c", PostalCode = "55555", Locality = "test_locality", CountryRegion = "test_country_region", CountryRegionIso2 = "test_country_region_iso2", FormattedAddress = "test_formatted_address" },
                PartnerId = "Mock Partner id",
                LocationType = LocationType.Device,
                DeviceType = LocationDeviceType.Phone,
                ActivityType = LocationActivityType.Bike,
                EndDateTime = DateTimeOffset.Parse("3/1/2016 12:47:10 AM +00:00", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                Url = "https://myhealth/is/awesome",
                Distance = 34343,
            };

            List<LocationHistoryV1> actual = (new List<LocationResource> { adapterLocationResource }).ToLocationHistoryV1(this.categoryMapping, MaxRadius);

            EqualityHelper.AreEqual(expected, actual);
        }

        [TestMethod]
        public void MapPartnerIdToCategoryUnknown()
        {
            var mapping = new Dictionary<string, PxfLocationCategory>
            {
                { "MEEDevices", PxfLocationCategory.Device }
            };

            LocationCategory expected = LocationCategory.Unknown;

            var actual = LocationHistoryV1Converter.MapPartnerIdToCategory(string.Empty, mapping);
            Assert.AreEqual(expected, actual);

            actual = LocationHistoryV1Converter.MapPartnerIdToCategory(null, mapping);
            Assert.AreEqual(expected, actual);

            actual = LocationHistoryV1Converter.MapPartnerIdToCategory("Test_Partner_Id", mapping);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void MapPartnerIdToCategoryValid()
        {
            var mapping = new Dictionary<string, PxfLocationCategory>
            {
                { "MEEDevices", PxfLocationCategory.Device }
            };

            LocationCategory expected = LocationCategory.Device;

            var actual = LocationHistoryV1Converter.MapPartnerIdToCategory("MEEDevices", mapping);
            Assert.AreEqual(expected, actual);
        }

        #endregion
    }
}
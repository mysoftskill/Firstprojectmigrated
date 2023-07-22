// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.SearchHistory
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Microsoft.Membership.MemberServices.Privacy.Core.Converters;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// SearchHistoryV1 Converter Test
    /// </summary>
    [TestClass]
    public class SearchHistoryV1ConverterTest
    {
        #region PrivacyAdapters.Models to ExperienceContracts.V1

        [TestMethod]
        public void ToSearchHistoryV1Test()
        {
            var expected = new SearchHistoryV1
            {
                DateTime = DateTimeOffset.Parse("3/1/2016 12:07:10 AM +00:00", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                IsAggregate = false,
                DeviceId = "global[343434]",
                Source = "Bing",
                Ids = new[] { "abc123" },
                Location = new WebLocationV1 { AccuracyRadius = 0, Latitude = 12.34d, Longitude = 23.45d },
                SearchTerms = "foo!!",
                NavigatedToUrls = new List<NavigatedToUrlV1> {  new NavigatedToUrlV1 { Url = "https://foo.com", PageTitle = "Test Page" } },
                PartnerId = "Mock Partner id"
            };

            SearchResource adapterSearchResource = new SearchResource
            {
                DateTime = DateTimeOffset.Parse("3/1/2016 12:07:10 AM +00:00", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                DeviceId = "global[343434]",
                Sources = new[] { "Bing" },
                Location = new SearchLocation { AccuracyRadius = 0, Latitude = 12.34d, Longitude = 23.45d },
                SearchTerms = "foo!!",
                NavigatedToUrls = new List<NavigatedToUrlResource> { new NavigatedToUrlResource { Url = "https://foo.com", Title = "Test Page" } },
                Id = "abc123",
                Status = ResourceStatus.Active,
                PartnerId = "Mock Partner id"
            };

            var actual = adapterSearchResource.ToSearchHistoryV1();

            EqualityHelper.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ToWebLocationV1Test()
        {
            var expected = new WebLocationV1
            {
                AccuracyRadius = 987654321,
                Latitude = 45.678d,
                Longitude = 34.45678d
            };

            SearchLocation adapterSearchLocation = new SearchLocation
            {
                AccuracyRadius = 987654321,
                Latitude = 45.678d,
                Longitude = 34.45678d
            };

            WebLocationV1 actual = adapterSearchLocation.ToWebLocationV1();

            EqualityHelper.AreEqual(expected, actual);
        }

        #endregion
    }
}
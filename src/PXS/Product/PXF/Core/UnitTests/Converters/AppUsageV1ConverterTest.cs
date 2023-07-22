// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.AppUsage
{
    using System;
    using System.Globalization;
    using Microsoft.Membership.MemberServices.Privacy.Core.Converters;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// AppUsageV1 Converter Test
    /// </summary>
    [TestClass]
    public class AppUsageV1ConverterTest
    {
        #region PrivacyAdapters.Models to ExperienceContracts.V1

        [TestMethod]
        public void ToAppUsageV1Test()
        {
            var expected = new AppUsageV1
            {
                DateTime = DateTimeOffset.Parse("2/29/2016 12:07:10 AM +00:00", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                IsAggregate = false,
                DeviceId = "global[343434]",
                Source = "Bing",
                Ids = new[] { "abc123" },
                AppIconBackground = "AppIconBackground",
                AppIconUrl = "AppIconUrl",
                AppId = "AppId",
                AppName = "AppName",
                AppPublisher = "AppPublisher",
                PartnerId = "Mock Partner id",
            };

            AppUsageResource appUsageResource = new AppUsageResource
            {
                DateTime = DateTimeOffset.Parse("2/29/2016 12:07:10 AM +00:00", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                EndDateTime = default(DateTimeOffset), // AppUsageV1 unused
                DeviceId = "global[343434]",
                Sources = new[] { "Bing" },
                Id = "abc123",
                AppIconBackground = "AppIconBackground",
                AppIconUrl = "AppIconUrl",
                AppId = "AppId",
                AppName = "AppName",
                AppPublisher = "AppPublisher",
                Status = ResourceStatus.Active,
                PartnerId = "Mock Partner id"
            };

            var actual = appUsageResource.ToAppUsageV1();

            EqualityHelper.AreEqual(expected, actual);
        }

        #endregion
    }
}
// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.BrowseHistory
{
    using System;
    using System.Globalization;
    using Microsoft.Membership.MemberServices.Privacy.Core.Converters;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// BrowseHistoryV1 Converter Test
    /// </summary>
    [TestClass]
    public class BrowseHistoryV1ConverterTest
    {
        #region PrivacyAdapters.Models to ExperienceContracts.V1

        [TestMethod]
        public void ToBrowseHistoryV1Test()
        {
            var expected = new BrowseHistoryV1
            {
                DateTime = DateTimeOffset.Parse("2/29/2016 12:07:10 AM +00:00", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                IsAggregate = false,
                DeviceId = "global[343434]",
                Source = "Bing",
                Ids = new[] { "abc123" },
                SearchTerms = "pizza",
                NavigatedToUrl = "https://www.pizza.com",
                PageTitle = "Everyone Loves Pizza",
                PartnerId = "Mock Partner id",
                Location = null
            };

            BrowseResource adapterBrowseResource = new BrowseResource
            {
                DateTime = DateTimeOffset.Parse("2/29/2016 12:07:10 AM +00:00", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                DeviceId = "global[343434]",
                Sources = new[] { "Bing" },
                PageTitle = "Everyone Loves Pizza",
                SearchTerms = "pizza",
                NavigatedToUrl = "https://www.pizza.com",
                Id = "abc123",
                Status = ResourceStatus.Active,
                PartnerId = "Mock Partner id"
            };

            var actual = adapterBrowseResource.ToBrowseHistoryV1();

            EqualityHelper.AreEqual(expected, actual);
        }

        #endregion
    }
}
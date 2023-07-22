// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.Factory
{
    using System;
    using System.Linq;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;

    /// <summary>
    /// BrowseHistoryV1Factory
    /// </summary>
    public static class BrowseHistoryV1Factory
    {
        public static BrowseHistoryV1 Create(DateTimeOffset dateTime, string navigatedToUrl, string partnerId = "P1", string pageTitle = "", string searchTerms = "")
        {
            return Create(dateTime, navigatedToUrl, new[] { Guid.NewGuid().ToString() }, partnerId, pageTitle, searchTerms);
        }

        public static BrowseHistoryV1 Create(
            DateTimeOffset dateTime, string navigatedToUrl, string[] ids, string partnerId = "P1", string pageTitle = "", string searchTerms = "")
        {
            return new BrowseHistoryV1
            {
                PartnerId = partnerId,
                DateTime = dateTime,
                Ids = ids.ToList(),
                IsAggregate = ids.Length > 1,
                DeviceId = Guid.NewGuid().ToString(),
                Location = null,
                PageTitle = pageTitle,
                NavigatedToUrl = navigatedToUrl,
                SearchTerms = searchTerms,
                AggregateCount = ids.Length,
                AggregateCountByPartner = { { partnerId, ids.Length } }
            };
        }
    }
}
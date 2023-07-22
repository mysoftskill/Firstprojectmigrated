// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.Factory
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;

    /// <summary>
    /// SearchHistoryV1Factory
    /// </summary>
    public static class SearchHistoryV1Factory
    {
        public static SearchHistoryV1 Create(DateTimeOffset dateTime, string navigatedToUrl, string partnerId = "P1", string pageTitle = "", string searchTerms = "", DateTimeOffset? navigationTime = null)
        {
            return Create(dateTime, navigatedToUrl, new[] { Guid.NewGuid().ToString() }, partnerId, pageTitle, searchTerms, navigationTime);
        }

        public static SearchHistoryV1 Create(
            DateTimeOffset dateTime, string navigatedToUrl, string[] ids, string partnerId = "P1", string pageTitle = "", string searchTerms = "", DateTimeOffset? navigationTime = null)
        {
            return new SearchHistoryV1
            {
                PartnerId = partnerId,
                DateTime = dateTime,
                Ids = ids.ToList(),
                IsAggregate = false,
                DeviceId = Guid.NewGuid().ToString(),
                Location = null,
                NavigatedToUrls = new List<NavigatedToUrlV1> { new NavigatedToUrlV1 { PageTitle = pageTitle, Url = navigatedToUrl, DateTime = navigationTime } },
                SearchTerms = searchTerms
            };
        }
    }
}
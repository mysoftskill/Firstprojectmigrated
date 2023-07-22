// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Converters
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;

    /// <summary>
    /// BrowseHistoryV1 Converter
    /// </summary>
    public static class BrowseHistoryV1Converter
    {
        /// <summary>
        /// Converts a collection of <see cref="BrowseResource"/> to a collection of <see cref="BrowseHistoryV1"/>.
        /// </summary>
        /// <param name="browseResources">The browse resources.</param>
        /// <returns>A collection of <see cref="BrowseHistoryV1"/></returns>
        internal static List<BrowseHistoryV1> ToBrowseHistoryV1(this IEnumerable<BrowseResource> browseResources)
        {
            if (browseResources == null)
            {
                return null;
            }

            var browseHistoryV1 = new List<BrowseHistoryV1>();

            foreach (var browseResource in browseResources)
            {
                browseHistoryV1.Add(browseResource.ToBrowseHistoryV1());
            }

            return browseHistoryV1;
        }

        /// <summary>
        /// Converts <see cref="BrowseResource"/> to <see cref="BrowseHistoryV1"/>.
        /// </summary>
        /// <param name="browseResource">The browse resource.</param>
        /// <returns>BrowseHistoryV1</returns>
        public static BrowseHistoryV1 ToBrowseHistoryV1(this BrowseResource browseResource)
        {
            BrowseHistoryV1 browseHistoryV1 = new BrowseHistoryV1();
            browseHistoryV1.DateTime = browseResource.DateTime;
            browseHistoryV1.DeviceId = browseResource.DeviceId;
            browseHistoryV1.Id = browseResource.Id;
            var ids = new List<string> { browseResource.Id };
            browseHistoryV1.Ids = ids;
            browseHistoryV1.IsAggregate = ids.Count > 1;
            browseHistoryV1.NavigatedToUrl = browseResource.NavigatedToUrl;
            browseHistoryV1.PageTitle = browseResource.PageTitle;
            browseHistoryV1.SearchTerms = browseResource.SearchTerms;
            browseHistoryV1.Source = browseResource.Sources?.FirstOrDefault();
            browseHistoryV1.PartnerId = browseResource.PartnerId;
            
            return browseHistoryV1;
        }
    }
}
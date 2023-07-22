// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Converters
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Web;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;

    /// <summary>
    /// NextLinkConverter V1
    /// </summary>
    public static class NextLinkConverterV1
    {
        /// <summary>
        /// Creates the search-history next link.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="routePrefix">The route prefix.</param>
        /// <param name="count">The count value.</param>
        /// <param name="orderBy">The order by.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>The 'next link' to retrieve results from.</returns>
        public static string CreateSearchHistoryNextLink(Uri endpoint, string routePrefix, int count, string orderBy, string filter)
        {
            UriBuilder uriBuilder = new UriBuilder(endpoint);
            uriBuilder.Path = routePrefix;

            StringBuilder queryParams = new StringBuilder();
            queryParams.Append("count=" + count);

            if (!string.IsNullOrWhiteSpace(orderBy))
            {
                queryParams.Append("&orderBy=" + HttpUtility.UrlEncode(orderBy));
            }

            if (!string.IsNullOrWhiteSpace(filter))
            {
                queryParams.Append("&filter=" + HttpUtility.UrlEncode(filter));
            }

            uriBuilder.Query = queryParams.ToString();
            return uriBuilder.Uri.OriginalString;
        }

        /// <summary>
        /// Creates the next link.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="routePrefix">The route prefix.</param>
        /// <param name="count">The count value.</param>
        /// <param name="skip">The skip value.</param>
        /// <param name="orderBy">The order by.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>The 'next link' to retrieve results from.</returns>
        public static string CreateNextLink(Uri endpoint, string routePrefix, int count, int skip, string orderBy, string filter)
        {
            UriBuilder uriBuilder = new UriBuilder(endpoint);
            uriBuilder.Path = routePrefix;

            StringBuilder queryParams = new StringBuilder();
            queryParams.Append("count=" + count);
            var newSkip = skip + count;
            queryParams.Append("&skip=" + newSkip);

            if (!string.IsNullOrWhiteSpace(orderBy))
            {
                queryParams.Append("&orderBy=" + HttpUtility.UrlEncode(orderBy));
            }

            if (!string.IsNullOrWhiteSpace(filter))
            {
                queryParams.Append("&filter=" + HttpUtility.UrlEncode(filter));
            }

            uriBuilder.Query = queryParams.ToString();
            return uriBuilder.Uri.OriginalString;
        }

        /// <summary>
        /// Determines whether the next page exists.
        /// </summary>
        /// <param name="preSelectCount">The pre select count.</param>
        /// <param name="aggregateItems">The aggregate items.</param>
        public static bool DoesNextPageExist(int preSelectCount, IList<BrowseHistoryV1> aggregateItems)
        {
            return preSelectCount > aggregateItems.Count;
        }
    }
}
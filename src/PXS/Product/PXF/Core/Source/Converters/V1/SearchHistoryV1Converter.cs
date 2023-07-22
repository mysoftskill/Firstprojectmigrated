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
    /// SearchHistoryV1 Converter
    /// </summary>
    public static class SearchHistoryV1Converter
    {
        /// <summary>
        /// Converts a collection of <see cref="SearchResource"/> to a collection of <see cref="SearchHistoryV1"/>.
        /// </summary>
        /// <param name="searchResources">The search resources.</param>
        /// <returns>A collection of <see cref="SearchHistoryV1"/></returns>
        internal static List<SearchHistoryV1> ToSearchHistoryV1(this IEnumerable<SearchResource> searchResources)
        {
            if (searchResources == null)
            {
                return null;
            }

            var searchHistoryV1 = new List<SearchHistoryV1>();

            foreach (var searchResource in searchResources)
            {
                searchHistoryV1.Add(searchResource.ToSearchHistoryV1());
            }

            return searchHistoryV1;
        }

        /// <summary>
        /// Converts <see cref="SearchResource"/> to <see cref="SearchHistoryV1"/>.
        /// </summary>
        /// <param name="searchResource">The search resource.</param>
        /// <returns>SearchHistoryV1</returns>
        public static SearchHistoryV1 ToSearchHistoryV1(this SearchResource searchResource)
        {
            SearchHistoryV1 searchHistoryV1 = new SearchHistoryV1();
            searchHistoryV1.DateTime = searchResource.DateTime;
            searchHistoryV1.DeviceId = searchResource.DeviceId;
            var ids = new List<string> { searchResource.Id };
            searchHistoryV1.Id = searchResource.Id;
            searchHistoryV1.Ids = ids;
            searchHistoryV1.IsAggregate = ids.Count > 1;
            searchHistoryV1.Location = searchResource.Location.ToWebLocationV1();
            searchHistoryV1.NavigatedToUrls = searchResource.NavigatedToUrls.ToNavigatedToUrlsV1();
            searchHistoryV1.SearchTerms = searchResource.SearchTerms;
            searchHistoryV1.Source = searchResource.Sources?.FirstOrDefault();
            searchHistoryV1.PartnerId = searchResource.PartnerId;
            
            return searchHistoryV1;
        }

        private static string ConvertToNavigateToUrlV1(IList<string> navigatedToUrls)
        {
            if (navigatedToUrls == null)
            {
                return null;
            }

            if (navigatedToUrls.Count == 0)
            {
                return string.Empty;
            }

            return navigatedToUrls.First();
        }

        /// <summary>
        /// Converts <see cref="SearchLocation"/> to <see cref="WebLocationV1"/>.
        /// </summary>
        /// <param name="searchLocation">The search location.</param>
        /// <returns>WebLocationV1</returns>
        internal static WebLocationV1 ToWebLocationV1(this SearchLocation searchLocation)
        {
            if (searchLocation == null)
            {
                return null;
            }

            return new WebLocationV1
            {
                Latitude = searchLocation.Latitude,
                Longitude = searchLocation.Longitude,
                AccuracyRadius = searchLocation.AccuracyRadius
            };
        }

        /// <summary>
        /// Converts <see cref="IEnumerable{NavigatedToUrlResource}"/> to <see cref="IEnumerable{NavigatedToUrlV1}"/>
        /// </summary>
        /// <param name="resources">The resources.</param>
        internal static IList<NavigatedToUrlV1> ToNavigatedToUrlsV1(this IEnumerable<NavigatedToUrlResource> resources)
        {
            if (resources == null)
            {
                return null;
            }

            return resources
                .Where(n => n != null)
                .Select(n => n.ToNavigatedToUrlV1())
                .ToList();
        }

        /// <summary>
        /// Converts <see cref="NavigatedToUrlResource"/> to <see cref="NavigatedToUrlV1"/>
        /// </summary>
        /// <param name="resource">The resource.</param>
        internal static NavigatedToUrlV1 ToNavigatedToUrlV1(this NavigatedToUrlResource resource)
        {
            return new NavigatedToUrlV1
            {
                Url = resource.Url,
                PageTitle = resource.Title,
                DateTime = resource.Time
            };
        }
    }
}
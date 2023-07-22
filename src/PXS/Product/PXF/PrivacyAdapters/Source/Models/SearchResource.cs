// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     Search Location V1
    /// </summary>
    public class SearchLocation
    {
        /// <summary>
        ///     Gets or sets the accuracy radius in meters.
        /// </summary>
        public int? AccuracyRadius { get; set; }

        /// <summary>
        ///     Gets or sets the latitude.
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        ///     Gets or sets the longitude.
        /// </summary>
        public double Longitude { get; set; }
    }

    /// <summary>
    ///     Search resource record
    /// </summary>
    public sealed class SearchResource : Resource
    {
        /// <summary>
        ///     Click link titles from Bing Search history have some words surrounded by special characters
        ///     that represent bold tags. This method removes those characters.
        /// </summary>
        /// <param name="title">Click link title.</param>
        /// <returns>Click link title without bold tags.</returns>
        public static string RemoveBoldTags(string title)
        {
            if (title == null)
                return null;

            const char startTag = '\uE000';
            const char endTag = '\uE001';

            string cleansed = new string(title.Where(c => c != startTag && c != endTag).ToArray());
            return cleansed;
        }

        /// <summary>
        ///     Gets or sets the location of the user/device when search occurred, if known.
        /// </summary>
        public SearchLocation Location { get; set; }

        /// <summary>
        ///     Gets or sets the collection of NavigatedToUrl of the searched resource.
        /// </summary>
        public IList<NavigatedToUrlResource> NavigatedToUrls { get; set; }

        /// <summary>
        ///     Gets or sets the search history terms (space delimited between words), which are used if the user clicked on search result to browse to this page.
        /// </summary>
        public string SearchTerms { get; set; }
    }

    /// <summary>
    ///     Navigated-To-Url resource
    /// </summary>
    public sealed class NavigatedToUrlResource
    {
        /// <summary>
        ///     Gets or sets the time of the navigation event.
        /// </summary>
        public DateTimeOffset Time { get; set; }

        /// <summary>
        ///     Gets or sets the title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        ///     Gets or sets the URL.
        /// </summary>
        public string Url { get; set; }
    }
}

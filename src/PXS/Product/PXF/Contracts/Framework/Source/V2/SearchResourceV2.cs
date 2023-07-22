// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.DataContracts.V2
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    /// <summary>
    ///     Search resource record
    /// </summary>
    public class SearchResourceV2 : PrivacyResourceV2
    {
        /// <summary>
        ///     The impression guid id
        /// </summary>
        [JsonProperty("id")]
        [JsonRequired]
        public string Id { get; set; }

        /// <summary>
        ///     The navigated urls for the search.
        /// </summary>
        [JsonProperty("navigations")]
        public List<NavigatedUrlV2> Navigations { get; set; }

        /// <summary>
        ///     Gets or sets the search history terms (space delimited between words), which are used if the user clicked on search result to browse to this page.
        /// </summary>
        [JsonProperty("searchTerms")]
        public string SearchTerms { get; set; }
    }

    /// <summary>
    ///     A search request navigated url
    /// </summary>
    public class NavigatedUrlV2
    {
        /// <summary>
        ///     The timestamp of the navigation.
        /// </summary>
        [JsonProperty("dateTime")]
        public DateTimeOffset DateTime { get; set; }

        /// <summary>
        ///     The title of the page
        /// </summary>
        [JsonProperty("pageTitle")]
        public string PageTitle { get; set; }

        /// <summary>
        ///     The url visited.
        /// </summary>
        [JsonProperty("url")]
        public Uri Url { get; set; }
    }
}

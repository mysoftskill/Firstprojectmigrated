// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;

    /// <summary>
    /// Search History V1
    /// </summary>
    public class SearchHistoryV1 : WebActivityV1
    {
        /// <summary>
        /// Gets or sets the collection of navigated-to-urls.
        /// </summary>
        [JsonProperty("navigatedToUrls")]
        public IList<NavigatedToUrlV1> NavigatedToUrls { get; set; }
    }

    /// <summary>
    /// Navigated-To-Url V1
    /// </summary>
    public class NavigatedToUrlV1
    {
        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the page title.
        /// </summary>
        [JsonProperty("pageTitle")]
        public string PageTitle { get; set; }

        /// <summary>
        /// Gets or sets the date-time of the navigation event.
        /// </summary>
        /// <remarks>This is not exposed to the client.</remarks>
        [JsonIgnore]
        public DateTimeOffset? DateTime { get; set; }
    }
}
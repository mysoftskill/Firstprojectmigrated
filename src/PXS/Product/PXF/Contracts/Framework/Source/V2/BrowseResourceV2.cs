// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.DataContracts.V2
{
    using Newtonsoft.Json;

    /// <summary>
    ///     Browse resource record V2
    /// </summary>
    public class BrowseResourceV2 : PrivacyResourceV2
    {
        /// <summary>
        ///     Gets or sets the URL of the browsed site.
        /// </summary>
        [JsonProperty("navigatedToUrl")]
        public string NavigatedToUrl { get; set; }

        /// <summary>
        ///     Gets or sets the page title of the visited site.
        /// </summary>
        [JsonProperty("pageTitle")]
        public string PageTitle { get; set; }

        /// <summary>
        ///     Gets or sets the hash of the url
        /// </summary>
        [JsonProperty("urlHash")]
        public string UrlHash { get; set; }
    }
}

// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Models
{
    /// <summary>
    ///     Browse resource
    /// </summary>
    public sealed class BrowseResource : Resource
    {
        /// <summary>
        ///     Gets or sets the URL of the browsed site.
        /// </summary>
        public string NavigatedToUrl { get; set; }

        /// <summary>
        ///     Gets or sets the page title of the visited site.
        /// </summary>
        public string PageTitle { get; set; }

        /// <summary>
        ///     Gets or sets the text the user typed into the edge 'search or enter web address' field.
        /// </summary>
        public string SearchTerms { get; set; }

        /// <summary>
        ///     The hash of the url for uniqueness
        /// </summary>
        public string UrlHash { get; set; }
    }
}

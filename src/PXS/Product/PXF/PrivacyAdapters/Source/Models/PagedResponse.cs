// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A page of results
    /// </summary>
    /// <typeparam name="T">Type of the result set</typeparam>
    public sealed class PagedResponse<T>
    {
        /// <summary>
        /// The partner the results are from
        /// </summary>
        public string PartnerId { get; set; }

        /// <summary>
        /// Items in the page
        /// </summary>
        public IList<T> Items { get; set; }

        /// <summary>
        /// The next link, or null if there are no more results.
        /// </summary>
        public Uri NextLink { get; set; }
    }
}

// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Paged Response of type T
    /// </summary>
    public class PagedResponse<T>
    {
        /// <summary>
        /// Gets or sets the list of items in the current response page
        /// </summary>
        [JsonProperty("items")]
        public IList<T> Items { get; set; }

        /// <summary>
        /// Gets or sets the next link.
        /// </summary>
        [JsonProperty("nextLink")]
        public string NextLink { get; set; }
    }
}
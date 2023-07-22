// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.CustomerMasterAdapter.Models
{
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Represents a the profiles collection object
    /// </summary>
    public class Profiles
    {
        /// <summary>
        /// Items in the profile
        /// </summary>
        /// <remarks>The <see cref="JObject"/> is used here because each Item in the collection may be a different Profile schema.</remarks>
        [JsonProperty("items")]
        public IList<JObject> Items { get; set; }

        /// <summary>
        /// Relative paths to resources accociated with this account (e.g. profile, addresses etc.)
        /// </summary>
        [JsonProperty("links")]
        public IDictionary<string, Link> Links { get; set; }
    }
}

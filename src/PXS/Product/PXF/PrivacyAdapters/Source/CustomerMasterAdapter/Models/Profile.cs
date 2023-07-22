// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.CustomerMasterAdapter
{
    using Newtonsoft.Json;

    /// <summary>
    /// Represents a base profile in Jarvis Customer Master
    /// </summary>
    public class Profile
    {
        /// <summary>
        /// The type of Profile
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods"), JsonProperty("type")]
        public string Type { get; set; }
        
        /// <summary>
        /// Gets or sets the etag.
        /// </summary>
        [JsonProperty("etag", NullValueHandling = NullValueHandling.Ignore)]
        public string ETag { get; set; }

        /// <summary>
        /// Gets or sets the profile identifier.
        /// </summary>
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        // NOTE: Profile contains 'Links', but we don't use them so choosing not to deserialize from JSON
    }
}
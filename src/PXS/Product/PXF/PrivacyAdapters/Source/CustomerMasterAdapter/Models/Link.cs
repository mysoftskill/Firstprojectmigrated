// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.CustomerMasterAdapter
{
    using Newtonsoft.Json;

    /// <summary>
    /// Link
    /// </summary>
    public class Link
    {
        /// <summary>
        /// Relative link address
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }

        /// <summary>
        /// Supported HTTP method for this resource
        /// </summary>
        [JsonProperty("method")]
        public string Method { get; set; }
    }
}
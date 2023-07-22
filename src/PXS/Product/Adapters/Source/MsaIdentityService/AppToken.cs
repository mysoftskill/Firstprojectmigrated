// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Adapters.MsaIdentityService
{
    using Newtonsoft.Json;

    /// <summary>
    /// Response containing app token from MSA.
    /// </summary>
    public class AppToken
    {
        /// <summary>
        /// S2S access token
        /// </summary>
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        /// <summary>
        /// Type of access token
        /// </summary>
        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        /// <summary>
        /// Access token experation
        /// </summary>
        [JsonProperty("expires_in")]
        public double ExpirationInSeconds { get; set; }
    }
}

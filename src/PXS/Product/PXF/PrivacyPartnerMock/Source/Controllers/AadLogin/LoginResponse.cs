// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyMockService.Controllers.AadLogin
{
    using Newtonsoft.Json;

    /// <summary>response to a login invocation</summary>
    public class LoginResponse
    {
        /// <summary>Gets or sets the resulting access token</summary>
        [JsonProperty(PropertyName = "access_token")]
        public string AccessToken { get; set; }
    }
}

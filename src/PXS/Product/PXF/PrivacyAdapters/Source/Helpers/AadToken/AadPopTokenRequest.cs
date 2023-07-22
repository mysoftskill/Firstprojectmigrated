// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;

    /// <summary>
    ///     Data to be used to make an PoP Authenticator AAD token request class
    /// </summary>
    public class AadPopTokenRequest
    {
        /// <summary>
        ///     Gets or sets the claims to include in the PoP token
        /// </summary>
        public IDictionary<string, string> Claims { get; set; }

        /// <summary>
        ///     Gets or sets the resource to be accessed
        /// </summary>
        public string Resource { get; set; }

        /// <summary>
        ///     Gets or sets the security scope the token grants
        /// </summary>
        public string Scope { get; set; }

        /// <summary>
        ///     Gets or sets the URL to which the request is being sent
        /// </summary>
        public Uri RequestUri { get; set; }

        /// <summary>
        ///     Gets or sets the HTTP Verb used for the request
        /// </summary>
        public HttpMethod HttpMethod { get; set; }

        /// <summary>
        ///     Gets or sets the pop token request type
        /// </summary>
        public AadPopTokenRequestType Type { get; set; }
    }
}

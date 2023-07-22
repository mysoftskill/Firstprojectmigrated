// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers
{
    /// <summary>
    ///     data to be used to make an AAD token request
    /// </summary>
    public class AadTokenPartnerConfig
    {
        /// <summary>
        ///     Gets or sets the resource to be accessed
        /// </summary>
        public string Resource { get; set; }

        /// <summary>
        ///     Gets or sets the security scope the token grants
        /// </summary>
        public string Scope { get; set; }
    }
}

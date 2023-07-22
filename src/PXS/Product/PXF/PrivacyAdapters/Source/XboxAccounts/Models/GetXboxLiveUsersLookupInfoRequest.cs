// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts.Models
{
    using System.Collections.Generic;

    /// <summary>
    ///     GetXboxLiveUsersLookupInfoRequest.
    /// </summary>
    public class GetXboxLiveUsersLookupInfoRequest
    {
        /// <summary>
        ///     Gets or sets Puids.
        /// </summary>
        public IEnumerable<long> Puids { get; set; }
    }
}

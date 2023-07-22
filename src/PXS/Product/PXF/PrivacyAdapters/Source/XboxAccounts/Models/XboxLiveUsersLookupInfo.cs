// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts.Models
{
    using System.Collections.Generic;

    /// <summary>
    ///     XboxLiveUsersLookupInfo.
    /// </summary>
    public class XboxLiveUsersLookupInfo
    {
        /// <summary>
        ///     Gets or sets users.
        /// </summary>
        public IEnumerable<XboxLiveUserLookupInfo> Users { get; set; }
    }
}

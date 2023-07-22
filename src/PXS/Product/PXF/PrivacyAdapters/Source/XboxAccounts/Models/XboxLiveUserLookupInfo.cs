// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts.Models
{
    /// <summary>
    ///     Public class for XboxLiveUserLookupInfo
    /// </summary>
    public class XboxLiveUserLookupInfo
    {
        public string Email { get; set; }

        public string GamerTag { get; set; }

        public long Puid { get; set; }

        public string Xuid { get; set; }
    }
}

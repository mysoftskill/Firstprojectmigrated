// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models
{

    /// <summary>
    /// Get-Settings Args
    /// </summary>
    public class GetUserSettingsArgs : PrivacyExperienceClientBaseArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetUserSettingsArgs"/> class.
        /// </summary>
        /// <param name="userProxyTicket">The user proxy ticket.</param>
        public GetUserSettingsArgs(string userProxyTicket) : base(userProxyTicket)
        {
        }
    }
}
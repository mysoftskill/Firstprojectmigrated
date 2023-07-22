// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models
{
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;

    /// <summary>
    ///     Update-Settings Args
    /// </summary>
    public class UpdateUserSettingsArgs : PrivacyExperienceClientBaseArgs
    {
        /// <summary>
        ///     Gets or sets the resource settings.
        /// </summary>
        public ResourceSettingV1 ResourceSettings { get; set; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="UpdateUserSettingsArgs" /> class.
        /// </summary>
        /// <param name="userProxyTicket">The user proxy ticket.</param>
        public UpdateUserSettingsArgs(string userProxyTicket)
            : base(userProxyTicket)
        {
        }
    }
}

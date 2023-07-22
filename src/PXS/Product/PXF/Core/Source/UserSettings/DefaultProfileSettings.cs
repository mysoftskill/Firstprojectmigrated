// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.UserSettings
{
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.CustomerMasterAdapter;

    /// <summary>
    ///     Creates default privacy profile settings
    /// </summary>
    public static class DefaultProfileSettingsFactory
    {
        internal static string DefaultCreatePrivacyProfileType = "MsaPrivacy";

        /// <summary>
        ///     Creates the default privacy profile.
        /// </summary>
        public static PrivacyProfile CreateDefaultPrivacyProfile()
        {
            return new PrivacyProfile
            {
                Type = DefaultCreatePrivacyProfileType,
                Advertising = true,
                SharingState = null,
                TailoredExperiencesOffers = null,
                OnBehalfOfPrivacy = null,
                LocationPrivacy = null
            };
        }
    }
}

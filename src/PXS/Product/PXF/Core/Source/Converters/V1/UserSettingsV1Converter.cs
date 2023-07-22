// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Converters
{
    using System.Collections.Generic;

    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.CustomerMasterAdapter;

    /// <summary>
    ///     UserSettingsV1-Converter
    /// </summary>
    public static class UserSettingsV1Converter
    {
        /// <summary>
        ///     Converts to <see cref="ResourceSettingV1" />.
        /// </summary>
        /// <param name="from">The <see cref="PrivacyProfile" /> to convert from.</param>
        public static ResourceSettingV1 ToPrivacyUserSettings(this PrivacyProfile from)
        {
            if (from == null)
            {
                return null;
            }

            return new ResourceSettingV1
            {
                ETag = from.ETag,
                Advertising = from.Advertising,
                TailoredExperiencesOffers = from.TailoredExperiencesOffers,
                OnBehalfOfPrivacy = from.OnBehalfOfPrivacy,
                LocationPrivacy = from.LocationPrivacy,
                SharingState = from.SharingState
            };
        }

        /// <summary>
        ///     Converts to <see cref="IList{ResourceSettingV1}" />
        /// </summary>
        /// <param name="from">The <see cref="PrivacyProfile" /> to convert from.</param>
        public static IList<ResourceSettingV1> ToResourceSettingsV1(this PrivacyProfile from)
        {
            if (from == null)
            {
                return null;
            }

            IList<ResourceSettingV1> to = new List<ResourceSettingV1>();

            if (from.Advertising.HasValue)
            {
                to.Add(new ResourceSettingV1 { Advertising = from.Advertising });
            }

            if (from.TailoredExperiencesOffers.HasValue)
            {
                to.Add(new ResourceSettingV1 { TailoredExperiencesOffers = from.TailoredExperiencesOffers });
            }

            if (from.OnBehalfOfPrivacy.HasValue)
            {
                to.Add(new ResourceSettingV1 { OnBehalfOfPrivacy = from.OnBehalfOfPrivacy });
            }

            if (from.LocationPrivacy.HasValue)
            {
                to.Add(new ResourceSettingV1 { LocationPrivacy = from.LocationPrivacy });
            }

            if (from.SharingState.HasValue)
            {
                to.Add(new ResourceSettingV1 { SharingState = from.SharingState });
            }

            return to;
        }
    }
}

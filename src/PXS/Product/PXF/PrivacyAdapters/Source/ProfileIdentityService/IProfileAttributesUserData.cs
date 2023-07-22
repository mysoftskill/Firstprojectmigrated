// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.ProfileIdentityService
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Membership.MemberServices.Common;

    /// <summary>
    ///     MSA Profile User Data tied to <see cref="ProfileAttribute" />s
    /// </summary>
    public interface IProfileAttributesUserData
    {
        /// <summary>
        ///     Gets the user's age group if retrieved
        /// </summary>
        LegalAgeGroup? AgeGroup { get; }

        /// <summary>
        ///     Gets the user's birthdate
        /// </summary>
        DateTime? Birthdate { get; }

        /// <summary>
        ///     Gets the user's city if retrieved
        /// </summary>
        string City { get; }

        /// <summary>
        ///     Gets the user's country code if retrieved
        /// </summary>
        string CountryCode { get; }

        /// <summary>
        ///     A collection of the underlying profile attribute data
        /// </summary>
        IReadOnlyDictionary<ProfileAttribute, string> Data { get; }

        /// <summary>
        ///     Gets the user's first name if retrieved
        /// </summary>
        string FirstName { get; }

        /// <summary>
        ///     Gets the user's friendly name if retrieved
        /// </summary>
        string FriendlyName { get; }

        /// <summary>
        ///     Gets the user's last name if retrieved
        /// </summary>
        string LastName { get; }
    }
}

// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.ProfileIdentityService
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    using Microsoft.Membership.MemberServices.Common;

    public class ProfileAttributesUserData : IProfileAttributesUserData
    {
        private readonly IDictionary<ProfileAttribute, string> data;

        /// <inheritdoc />
        public LegalAgeGroup? AgeGroup => this.data.TryGetValue(ProfileAttribute.AgeGroup, out string group)
            ? (int.TryParse(group, out int ageGroup) ? (LegalAgeGroup)ageGroup : (LegalAgeGroup?)null)
            : null;

        /// <inheritdoc />
        public DateTime? Birthdate =>
            this.data.TryGetValue(ProfileAttribute.Birthdate, out string birth)
                ? DateTime.TryParseExact(birth, "d:M:yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result) ? result : (DateTime?)null
                : null;

        /// <inheritdoc />
        public string City => this.data.TryGetValue(ProfileAttribute.City, out string city) ? city : null;

        /// <inheritdoc />
        public string CountryCode => this.data.TryGetValue(ProfileAttribute.Country, out string countryCode) ? countryCode : null;

        /// <inheritdoc />
        public IReadOnlyDictionary<ProfileAttribute, string> Data => this.data as IReadOnlyDictionary<ProfileAttribute, string>;

        /// <inheritdoc />
        public string FirstName => this.data.TryGetValue(ProfileAttribute.FirstName, out string name) ? name : null;

        /// <inheritdoc />
        public string FriendlyName => this.data.TryGetValue(ProfileAttribute.FriendlyName, out string name) ? name : null;

        /// <inheritdoc />
        public string LastName => this.data.TryGetValue(ProfileAttribute.LastName, out string name) ? name : null;

        /// <summary>
        ///     Creates a new instance of <see cref="ProfileAttributesUserData" />
        /// </summary>
        /// <param name="data">The underlying data for populating the instance of <see cref="ProfileAttributesUserData" /></param>
        public ProfileAttributesUserData(IDictionary<ProfileAttribute, string> data)
        {
            this.data = data;
        }
    }
}

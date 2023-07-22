// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Common
{
    public enum LegalAgeGroup
    {
        Undefined = 0,

        /// <summary>
        ///     User is in a country/region that requires Parental Consent for their age, Parental Consent is not provided. User is under the age of statutory
        /// </summary>
        MinorWithoutParentalConsent = 1,

        /// <summary>
        ///     User is in a country/region that requires Parental Consent for their age, Parental Consent is granted. User is under the age of statutory
        /// </summary>
        MinorWithParentalConsent = 2,

        /// <summary>
        ///     Not a minor. User is above the age of majority
        /// </summary>
        Adult = 3,

        /// <summary>
        ///     User is not considered a minor or an adult in a country/region that does have Parental Consent regulations. User is above the statutory age, under age of majority
        /// </summary>
        NotAdult = 4,

        /// <summary>
        ///     User is considered a minor in their country/region, but no Parental Consent is required for minors in this country/region. User is under statutory age
        /// </summary>
        MinorNoParentalConsentRequired = 5
    }
}

// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Common
{
    using System;
    using System.Linq;

    /// <summary>
    ///     Helpers to determine authorization based on majority age
    /// </summary>
    public class AgeAuthZLegalAgeGroup : IAgeAuthZRules
    {
        private static readonly LegalAgeGroup[] adultAgeGroups =
        {
            LegalAgeGroup.Adult,
            LegalAgeGroup.Undefined
        };

        private static readonly LegalAgeGroup[] minorAgeGroups =
        {
            LegalAgeGroup.MinorNoParentalConsentRequired,
            LegalAgeGroup.MinorWithParentalConsent,
            LegalAgeGroup.MinorWithoutParentalConsent,
            LegalAgeGroup.NotAdult
        };

        /// <summary>
        ///     Determines whether the target user can DELETE.
        /// </summary>
        /// <param name="identity">The request's user identity</param>
        /// <returns>
        ///     <c>true</c> if the user is authorized to delete, else <c>false</c>
        /// </returns>
        public bool CanDelete(MsaSelfIdentity identity)
        {
            if (!identity.IsChildInFamily)
            {
                return true;
            }

            LegalAgeGroup? targetLegalAgeGroup = identity.TargetLegalAgeGroup ?? LegalAgeGroup.Undefined;

            // If a parent is trying to perform a delete action on behalf of the child
            // The child has to be a minor, or consenting to their parents performing OBO actions for them
            if (IsOnBehalfOf(identity))
            {
                return minorAgeGroups.Any(ageGroup => ageGroup == targetLegalAgeGroup) ||
                       (identity.IsFamilyConsentSet ?? false) && adultAgeGroups.Any(ageGroup => ageGroup == targetLegalAgeGroup);
            }

            // This is a self action and the child is part of a family requiring that they be an "adult"
            return adultAgeGroups.Any(a => a == targetLegalAgeGroup);
        }

        /// <summary>
        ///     Determines whether the target user can VIEW.
        /// </summary>
        /// <param name="identity">The request's user identity</param>
        /// <returns>
        ///     <c>true</c> if the user is authorized to view, else <c>false</c>
        /// </returns>
        public bool CanView(MsaSelfIdentity identity)
        {
            // If you're not in a family, or viewing for self it should always be allowed
            if (!identity.IsChildInFamily || !IsOnBehalfOf(identity))
            {
                return true;
            }

            LegalAgeGroup? targetLegalAgeGroup = identity.TargetLegalAgeGroup ?? LegalAgeGroup.Undefined;

            // For on behalf of, parents can always view the data of a minor, but must have consent from
            // a child that is considered an "adult"
            return minorAgeGroups.Any(ageGroup => ageGroup == targetLegalAgeGroup) ||
                   (identity.IsFamilyConsentSet ?? false) && adultAgeGroups.Any(ageGroup => ageGroup == targetLegalAgeGroup);
        }

        /// <summary>
        ///     Gets a value indicating the request is on behalf of the target user
        /// </summary>
        /// <param name="identity">The user identity</param>
        /// <returns><c>true</c> if on behalf of, otherwise <c>false</c></returns>
        private static bool IsOnBehalfOf(MsaSelfIdentity identity)
        {
            return !string.IsNullOrWhiteSpace(identity.FamilyJsonWebToken);
        }
    }
}

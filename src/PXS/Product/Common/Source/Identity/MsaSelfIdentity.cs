// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Common
{
    using System;
    using System.Globalization;

    /// <summary>
    ///     Represents a user authenticated via MSA S2s Self Auth
    /// </summary>
    public sealed class MsaSelfIdentity : MsaSiteIdentity
    {
        /// <summary>
        ///     Gets the cid (of the authorizing user)
        /// </summary>
        public long? AuthorizingCid { get; }

        /// <summary>
        ///     Gets the puid that is authorizing the request
        /// </summary>
        public long AuthorizingPuid { get; }

        /// <summary>
        ///     Family JWT
        /// </summary>
        public string FamilyJsonWebToken { get; }

        /// <summary>
        ///     Gets a value indicating if the user is a child.
        /// </summary>
        public bool IsChildInFamily { get; }

        /// <summary>
        ///     Gets a value indicating the child (who is not a minor) consents to OBO
        /// </summary>
        public bool? IsFamilyConsentSet { get; }

        /// <summary>
        ///     Gets the Legal Age Group value of the user
        /// </summary>
        public LegalAgeGroup? TargetLegalAgeGroup { get; }

        /// <summary>
        ///     Gets or sets the birth date.
        /// </summary>
        public DateTimeOffset? TargetBirthDate { get; set; }

        /// <summary>
        ///     Gets the cid of the target of the request
        /// </summary>
        public long? TargetCid { get; }

        /// <summary>
        ///     Gets or sets the target user's country/region.
        /// </summary>
        public string TargetCountry { get; }

        /// <summary>
        ///     Get the puid of the target of the request
        /// </summary>
        /// <remarks>May be different than AuthorizingPuid for on-behalf-of requests</remarks>
        public long TargetPuid { get; }

        /// <summary>
        ///     Gets the user proxy ticket.
        /// </summary>
        public string UserProxyTicket { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MsaSelfIdentity" /> class.
        /// </summary>
        /// <param name="userProxyTicket">The user proxy ticket.</param>
        /// <param name="familyJsonWebToken">The family JWT (can be null)</param>
        /// <param name="authorizingPuid">The user puid.</param>
        /// <param name="targetPuid">The target puid.</param>
        /// <param name="userAuthorizingCid">The user cid.</param>
        /// <param name="callerName">Name of the caller.</param>
        /// <param name="siteId">The site identifier.</param>
        /// <param name="targetCid">The target cid.</param>
        /// <param name="targetCountryRegion">The authenticated user's country/region.</param>
        /// <param name="targetBirthDate">The target birth date.</param>
        /// <param name="isChildInFamilyInFamily">if set to <c>true</c>, user is a child in a family.</param>
        /// <param name="authType">Type of the authentication.</param>
        /// <param name="legalAgeGroup">The legal age group value.</param>
        /// <param name="isFamilyConsentSet">Flag that indicates the not-minor child consents to OBO</param>
        public MsaSelfIdentity(
            string userProxyTicket,
            string familyJsonWebToken,
            long authorizingPuid,
            long targetPuid,
            long? userAuthorizingCid,
            string callerName,
            long siteId,
            long? targetCid,
            string targetCountryRegion,
            DateTimeOffset? targetBirthDate,
            bool isChildInFamilyInFamily,
            AuthType authType = AuthType.MsaSelf,
            LegalAgeGroup? legalAgeGroup = 0,
            bool? isFamilyConsentSet = null)
            : base(callerName, siteId)
        {
            this.UserProxyTicket = userProxyTicket;
            this.FamilyJsonWebToken = familyJsonWebToken;

            this.Name = authorizingPuid == default(long)
                ? (userAuthorizingCid ?? 0).ToString(CultureInfo.InvariantCulture)
                : authorizingPuid.ToString(CultureInfo.InvariantCulture);

            this.AuthorizingPuid = authorizingPuid;
            this.TargetPuid = targetPuid;
            this.AuthorizingCid = userAuthorizingCid;
            this.IsAuthenticated = true;
            this.TargetCid = targetCid;
            this.TargetCountry = targetCountryRegion;
            this.TargetBirthDate = targetBirthDate;
            this.AuthType = authType;
            this.IsChildInFamily = isChildInFamilyInFamily;
            this.TargetLegalAgeGroup = legalAgeGroup;
            this.IsFamilyConsentSet = isFamilyConsentSet;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{{MsaSelfIdentity: {this.CallerMsaSiteId}}}";
        }
    }
}

// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Family.Client.Claims;
    using Microsoft.Family.Client.Claims.Family;
    using Microsoft.IdentityModel.Tokens;

    /// <summary>
    ///     Interface to a wrapper object to a set of claims from a Family Json Web Token
    /// </summary>
    public interface IFamilyClaims
    {
        /// <summary>
        ///     The target PUID of this Family JWT
        /// </summary>
        long? TargetPuid { get; set; }

        /// <summary>
        ///     The PUID for whom the Family JWT was issued
        /// </summary>
        long? UserPuid { get; set; }

        /// <summary>
        ///     Check if this claims object is correctly signed and not expired
        /// </summary>
        bool CheckIsValid();

        /// <summary>
        ///     Check whether this set of claims states that one PUID is a parent of another
        /// </summary>
        /// <param name="parentPuid">Parent's puid</param>
        /// <param name="childPuid">Child's puid</param>
        bool ParentChildRelationshipIsClaimed(long parentPuid, long childPuid);
    }

    public interface IFamilyClaimsParser
    {
        /// <summary>
        /// Attempt to construct a FamilyClaims object from a raw string, and return success of failure.
        /// </summary>
        /// <param name="familyWebToken">Json Web Token</param>
        /// <param name="claims">out: family claims</param>
        bool TryParse(string familyWebToken, out IFamilyClaims claims);
    }

    public class FamilyClaimsParser : IFamilyClaimsParser
    {
        /// <summary>
        /// Attempt to construct a FamilyClaims object from a raw string, and return success of failure.
        /// </summary>
        /// <param name="familyWebToken">Json Web Token</param>
        /// <param name="claims">out: family claims</param>
        public bool TryParse(string familyWebToken, out IFamilyClaims claims)
        {
            bool value = FamilyClaims.TryParse(familyWebToken, out FamilyClaims temp);
            claims = temp;

            return value;
        }
    }

    /// <summary>
    ///     Wrapper around a set of claims from a Family JWT Token
    /// </summary>
    public class FamilyClaims : IFamilyClaims
    {
        // Constants used to find claims in a Family JWT Token
        public const string FamilyIssuer = "urn:microsoft:family";

        private static IClaimAuthenticator claimAuthenticator;

        private readonly FamilyRole targetRole;

        private readonly FamilyRole userRole;

        /// <summary>
        ///     Initializes the static properties. Required before TryParse() can be called.
        /// </summary>
        public static void Initialize(IClaimAuthenticator authenticator)
        {
            claimAuthenticator = authenticator;
        }

        /// <summary>
        ///     Attempt to construct a FamilyClaims object from a raw string, and return success of failure.
        /// </summary>
        /// <param name="familyWebToken">Json Web Token</param>
        /// <param name="claims">out: family claims</param>
        public static bool TryParse(string familyWebToken, out FamilyClaims claims)
        {
            if (claimAuthenticator == null)
            {
                throw new NotSupportedException("FamilyClaims has not been initialized.");
            }

            claims = GetFamilyClaimsOrNull(familyWebToken);
            return claims != null;
        }

        /// <summary>
        ///     The target PUID of this Family JWT
        /// </summary>
        public long? TargetPuid { get; set; }

        /// <summary>
        ///     The PUID for whom the Family JWT was issued
        /// </summary>
        public long? UserPuid { get; set; }

        /// <summary>
        ///     Check if this claims object is correctly signed and not expired
        /// </summary>
        public bool CheckIsValid()
        {
            return this.UserPuid != null &&
                   this.TargetPuid != null;
        }

        /// <summary>
        ///     Check whether this set of claims states that one PUID is a parent of another
        /// </summary>
        /// <param name="parentPuid">Parent's puid</param>
        /// <param name="childPuid">Child's puid</param>
        public bool ParentChildRelationshipIsClaimed(long parentPuid, long childPuid)
        {
            return this.UserPuid != null &&
                   this.TargetPuid != null &&
                   parentPuid == this.UserPuid.Value &&
                   childPuid == this.TargetPuid.Value &&
                   FamilyRole.Admin == this.userRole &&
                   FamilyRole.User == this.targetRole;
        }

        private FamilyClaims(string familyWebToken, IClaimAuthenticator authenticator)
        {
            IDictionary<string, string> claims = authenticator.GetClaims(familyWebToken);
            FamilyMemberClaim familyMemberClaim = FamilyMemberClaim.Create(claims);

            // Parent Values
            this.UserPuid = familyMemberClaim.Source.Puid;
            this.userRole = familyMemberClaim.Source.Role;

            // Child Values
            this.TargetPuid = familyMemberClaim.Target.Puid;
            this.targetRole = familyMemberClaim.Target.Role;
        }

        /// <summary>
        ///     Attempt to construct a FamilyClaims object from a raw string.  Return null on failure.
        /// </summary>
        /// <param name="familyWebToken">Json Web Token</param>
        private static FamilyClaims GetFamilyClaimsOrNull(string familyWebToken)
        {
            if (string.IsNullOrWhiteSpace(familyWebToken))
            {
                return null;
            }

            try
            {
                return new FamilyClaims(familyWebToken, claimAuthenticator);
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (SecurityTokenException)
            {
                return null;
            }
        }
    }
}

// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Security.Cryptography;
    using System.Text;

    internal static class AuthUtilities
    {
        /// <summary>
        ///     Get SHA256 hash of string.
        /// </summary>
        /// <param name="value">value to hash</param>
        /// <returns>hashed bytes</returns>
        public static byte[] GetSha256Hash(string value)
        {
            using (SHA256Cng sha256 = new SHA256Cng())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(value));
            }
        }

        /// <summary>
        ///     Convert hashed bytes to string
        /// </summary>
        /// <param name="hash">SHA256 hash</param>
        /// <returns>resulting string</returns>
        public static string GetStringFromHash(byte[] hash)
        {
            string hex = BitConverter.ToString(hash);
            return hex.Replace("-", string.Empty);
        }

        /// <summary>
        ///     Get the timestamp claim if available
        /// </summary>
        /// <param name="claimName">The name of the claim to fetch from the actor</param>
        /// <param name="claims">The actor claims to scan</param>
        /// <param name="offsetHours">Potential offset in hours with fallback, for expiration</param>
        /// <returns>epoch time safely adjusted with threshold for expiry timestamp</returns>
        public static string GetTimeStampClaim(
            string claimName,
            IEnumerable<Claim> claims,
            int offsetHours)
        {
            Claim claim = claims.SingleOrDefault(a => claimName.Equals(a.Type, StringComparison.OrdinalIgnoreCase));
            return claim != null ? claim.Value : DateTimeOffset.UtcNow.AddHours(offsetHours).ToUnixTimeSeconds().ToString();
        }
    }
}

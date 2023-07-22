namespace Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation
{
    using System;
    using System.Collections.Generic;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Security.Claims;
    using System.Text.RegularExpressions;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;

    /// <summary>
    /// Extension methods for Token and Claims from Token
    /// </summary>
    public static class TokenExtension
    {
        private static readonly Regex TenantPattern = new Regex("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", RegexOptions.Compiled);

        /// <summary>
        /// Gets the TenantId from the jwtSecurityToken, which the Guid at the issuer
        /// </summary>
        /// <param name="token">JwtSecurityToken from the JWT</param>
        /// <returns>TenantId as a Guid. Empty guid if not found.</returns>
        public static Guid GetTenantIdFromIssuerUrl(this JwtSecurityToken token)
        {
            var issuer = token.Issuer;

            if (!string.IsNullOrWhiteSpace(issuer))
            {
                var match = TenantPattern.Match(issuer);
                if (match.Success)
                {
                    return Guid.Parse(match.Value);
                }
            }

            return Guid.Empty;
        }

        /// <summary>
        /// Gets the KeyId from the JWTSecurityToken
        /// </summary>
        /// <param name="jwtSecurityToken">The securityToken from the JWT</param>
        /// <returns>The key id string if found. Null otherwise.</returns>
        public static string GetKeyId(this JwtSecurityToken jwtSecurityToken)
        {
           return jwtSecurityToken.Header.Kid;
        }

        /// <summary>
        /// Gets the Puid from the claims
        /// </summary>
        /// <param name="claims">Enumeration of Claims from the JWT</param>
        /// <param name="tenantIdType">Enumeration of Privacy Contract Subjects</param>
        /// <returns>Puid as long</returns>
        public static long GetPuid(this IEnumerable<Claim> claims, TenantIdType tenantIdType = TenantIdType.Home)
        {
            string puidValue;
            if (tenantIdType == TenantIdType.Resource)
            {
                puidValue = GetClaimValue(claims, "home_puid");
            }
            else
            {
                puidValue = GetClaimValue(claims, "puid");
            }
            if (!string.IsNullOrWhiteSpace(puidValue))
            {
                try
                {
                    return Convert.ToInt64(puidValue, 16);
                }
                catch (FormatException)
                {
                }
            }

            return 0;
        }

        /// <summary>
        /// Gets the Cid from the claims
        /// </summary>
        /// <param name="claims">Enumeration of Claims from the JWT</param>
        /// <returns>Cid as long</returns>
        public static long GetCid(this IEnumerable<Claim> claims)
        {
            var cidValue = GetClaimValue(claims, "cid");

            if (!string.IsNullOrWhiteSpace(cidValue))
            {
                try
                {
                    return Convert.ToInt64(cidValue, 16);
                }
                catch (FormatException)
                {
                }
            }

            return 0;
        }

        /// <summary>
        /// Gets the ObjectId from the claims
        /// </summary>
        /// <param name="claims">Enumeration of Claims from the JWT</param>
        /// <param name="tenantType">Determines if tenantType is Home or Resource</param>
        /// <returns>ObjectId as Guid</returns>
        public static Guid GetObjectId(this IEnumerable<Claim> claims, TenantIdType tenantType = TenantIdType.Home)
        {
            string objectId;
            if (tenantType == TenantIdType.Resource)
            {
                objectId = GetClaimValue(claims, "home_oid");  
            }
            else 
            {
                objectId = GetClaimValue(claims, "oid");
            }

            if (!string.IsNullOrWhiteSpace(objectId) && Guid.TryParse(objectId, out Guid objectIdGuid))
            {
                return objectIdGuid;
            }

            return Guid.Empty;
        }

        /// <summary>
        /// Gets the Azure storage uri from the claims
        /// </summary>
        /// <param name="claims">Enumeration of Claims from the JWT</param>
        /// <returns>Absolute Azure storage uri.</returns>
        public static Uri GetAzureStorageUri(this IEnumerable<Claim> claims)
        {
            var storagePath = GetClaimValue(claims, "azsp");

            if (string.IsNullOrWhiteSpace(storagePath))
            {
                // not found/empty
                return null;
            }

            if (!Uri.TryCreate(storagePath, UriKind.Absolute, out Uri storageUri))
            {
                throw new InvalidAzureStorageUriException($"Invalid Azure storage uri: {storagePath}");
            }

            return storageUri;
        }

        /// <summary>
        /// Gets the tenantId from the claims
        /// </summary>
        /// <param name="claims">Enumeration of Claims from the JWT</param>
        /// <returns>tenantId as Guid. Empty guid if not found.</returns>
        public static Guid GetTenantIdFromClaims(this IEnumerable<Claim> claims)
        {
            var tenantId = GetClaimValue(claims, "tid");

            if (!string.IsNullOrWhiteSpace(tenantId) && Guid.TryParse(tenantId, out Guid tenantIdGuid))
            {
                return tenantIdGuid;
            }

            return Guid.Empty;
        }

        /// <summary>
        /// Gets the homeTenantId from the claims
        /// </summary>
        /// <param name="claims">Enumeration of Claims from the JWT</param>
        /// <returns>hometenantId as Guid. Empty guid if not found.</returns>
        public static Guid GetHomeTenantIdFromClaims(this IEnumerable<Claim> claims)
        {
            var hometenantId = GetClaimValue(claims, "home_tid");

            if (!string.IsNullOrWhiteSpace(hometenantId) && Guid.TryParse(hometenantId, out Guid hometenantIdGuid))
            {
                return hometenantIdGuid;
            }

            return Guid.Empty;
        }

        /// <summary>
        /// Gets the tenantIdType from the claims
        /// </summary>
        /// <param name="claims">Enumeration of Claims from the JWT</param>
        /// <param name="loggableInformation">Unique value used to identify a validation request</param>
        /// <returns>TenantIdType as Enum, and throws error if it can't be parsed</returns>
        public static TenantIdType GetTenantIdTypeFromClaims(this IEnumerable<Claim> claims, LoggableInformation loggableInformation)
        {
            var tenantIdType = GetClaimValue(claims, "dsr_tid_type");

            if (!string.IsNullOrWhiteSpace(tenantIdType) && Enum.TryParse<TenantIdType>(tenantIdType, ignoreCase: true, out TenantIdType tenantIdTypeEnum))
            {
                return tenantIdTypeEnum;
            }
            else 
            {
                throw new InvalidPrivacyCommandException($"TenantIdType could not be parsed", loggableInformation);
            }
        }
        /// <summary>
        /// Gets the Anid from the claims
        /// </summary>
        /// <param name="claims">Enumeration of Claims from the JWT</param>
        /// <returns>Anid as string</returns>
        public static string GetAnid(this IEnumerable<Claim> claims)
        {
            return GetClaimValue(claims, "anid");
        }

        /// <summary>
        /// Gets the Xuid from the claims
        /// </summary>
        /// <param name="claims">Enumeration of Claims from the JWT</param>
        /// <returns>Xuid as string</returns>
        public static string GetXuid(this IEnumerable<Claim> claims)
        {
            return GetClaimValue(claims, "xuid");
        }

        /// <summary>
        /// Gets the operation from the claims
        /// </summary>
        /// <param name="claims">Enumeration of Claims from the JWT</param>
        /// <returns>operation as string</returns>
        public static string GetOperation(this IEnumerable<Claim> claims)
        {
            return GetClaimValue(claims, "op");
        }

        /// <summary>
        /// Gets the requestIds from the claims
        /// </summary>
        /// <param name="claims">Enumeration of Claims from the JWT</param>
        /// <returns>RequestId as string</returns>
        public static string[] GetRequestIds(this IEnumerable<Claim> claims)
        {
            string multipleRequestIds = GetClaimValue(claims, "rids");
            if (!string.IsNullOrEmpty(multipleRequestIds))
            {
                return multipleRequestIds.Split(',');
            }

            return new[] { GetClaimValue(claims, "rid") };
        }

        /// <summary>
        /// Gets the Processor Applicable flag from the claims.
        /// </summary>
        /// <param name="claims">The claims.</param>
        /// <returns>Processor applicability flag as a string.</returns>
        public static string GetProcessorApplicable(this IEnumerable<Claim> claims)
        {
            return GetClaimValue(claims, "pa");
        }
        
        /// <summary>
        /// Gets the Controller Applicable flag from the claims.
        /// </summary>
        /// <param name="claims">The claims.</param>
        /// <returns>Controller applicability flag as a string.</returns>
        public static string GetControllerApplicable(this IEnumerable<Claim> claims)
        {
            return GetClaimValue(claims, "ca");
        }

        /// <summary>
        /// Gets the data type from the claims.
        /// </summary>
        /// <param name="claims">The claims.</param>
        /// <returns>The data type as a string  or empty if it doesn't exist.</returns>
        public static string GetDataType(this IEnumerable<Claim> claims)
        {
            return GetClaimValue(claims, "dts");
        }

        /// <summary>
        /// Gets the version string from the claims.
        /// </summary>
        /// <param name="claims">The claims.</param>
        /// <returns>The version string.</returns>
        public static string GetVersion(this IEnumerable<Claim> claims)
        {
            return GetClaimValue(claims, "ver");
        }

        private static string GetClaimValue(IEnumerable<Claim> claims, string claimType)
        {
            if (claims.Any(claim => claim.Type == claimType))
            {
                return claims.First(claim => claim.Type == claimType).Value;
            }

            return string.Empty;
        }
    }
}

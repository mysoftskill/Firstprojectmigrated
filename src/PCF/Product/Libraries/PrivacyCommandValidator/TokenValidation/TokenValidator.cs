namespace Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation
{
    using System;
    using System.Collections.Generic;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Security.Claims;
    using System.Text.RegularExpressions;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Validator;
    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;
    using Microsoft.PrivacyServices.Policy;

    /// <summary>
    /// Validates the JwtToken and IValidator
    /// </summary>
    public class TokenValidator : ITokenValidator
    {
        private const string VerifierV2 = "2.0";

        private const string VerifierV3 = "3.0";

        /// <inheritdoc />
        public void RunPrechecksOnToken(JwtSecurityToken jwtSecurityToken, IPrivacySubject subject, LoggableInformation loggableInformation, EnvironmentConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(jwtSecurityToken.GetKeyId()))
            {
                throw new InvalidPrivacyCommandException("Verifier is missing key id.", loggableInformation);
            }

            string algorithm = jwtSecurityToken.Header.Alg;
            if (string.IsNullOrWhiteSpace(algorithm) || Enum.TryParse(algorithm, false, out SigningAlgorithm _))
            {
                throw new InvalidPrivacyCommandException($"Verifier has a missing or invalid algorithm. Algorithm: '{algorithm ?? "null"}'", loggableInformation);
            }

            var regex = new Regex(configuration.IssuerRegexPattern);
            if (string.IsNullOrWhiteSpace(jwtSecurityToken.Issuer) || !regex.IsMatch(jwtSecurityToken.Issuer))
            {
                throw new InvalidPrivacyCommandException($"Verifier doesn't contain an valid issuer. Issuer : '{jwtSecurityToken.Issuer ?? "null"}' Expected : '{configuration.IssuerRegexPattern}'", loggableInformation);
            }

            ValidateTenantId(jwtSecurityToken.GetTenantIdFromIssuerUrl(), subject, configuration.TenantId, loggableInformation);

            if (jwtSecurityToken.ValidTo == null || jwtSecurityToken.ValidTo.CompareTo(DateTime.Now) <= 0)
            {
                throw new InvalidPrivacyCommandException($"Verifier has expired. ValidTo date: '{(jwtSecurityToken.ValidTo == null ? "null" : jwtSecurityToken.ValidTo.ToString("MM-dd-yyyy"))}'", loggableInformation);
            }

            if (jwtSecurityToken.ValidFrom == null || jwtSecurityToken.ValidFrom.CompareTo(DateTime.UtcNow) > 0)
            {
                throw new InvalidPrivacyCommandException($"Verifier is not yet active. ValidFrom date: '{(jwtSecurityToken.ValidFrom == null ? "null" : jwtSecurityToken.ValidFrom.ToString("MM-dd-yyyy"))}'", loggableInformation);
            }
        }

        /// <inheritdoc />
        public void ValidateCommand(CommandClaims commandClaims, LoggableInformation loggableInformation, IEnumerable<Claim> claimsInToken)
        {
            if (commandClaims == null)
            {
                throw new ArgumentException("command cannot be null");
            }

            if (claimsInToken == null)
            {
                throw new InvalidPrivacyCommandException("No claims found in the verifier", loggableInformation);
            }

            ValidateOperation(commandClaims, claimsInToken, loggableInformation);
            ValidateRequestId(commandClaims, claimsInToken, loggableInformation);
            ValidateBooleanFlag("PA", commandClaims.ProcessorApplicable, claimsInToken.GetProcessorApplicable(), loggableInformation);
            ValidateBooleanFlag("CA", commandClaims.ControllerApplicable, claimsInToken.GetControllerApplicable(), loggableInformation);

            if (commandClaims.Subject.GetType() == typeof(AadSubject2))
            {
                ValidateAadSubject2(commandClaims.Subject as AadSubject2, loggableInformation, claimsInToken);
            }
            else if(commandClaims.Subject.GetType() == typeof(AadSubject))
            {
                ValidateAadSubject(commandClaims.Subject as AadSubject, loggableInformation, claimsInToken);
            }
            else if (commandClaims.Subject.GetType() == typeof(MsaSubject))
            {
                ValidateMsaSubject(commandClaims.Subject as MsaSubject, loggableInformation, claimsInToken);
            }
            else if (commandClaims.Subject.GetType() == typeof(DeviceSubject))
            {
                ValidateDeviceSubject(commandClaims.Subject as DeviceSubject, loggableInformation, claimsInToken);
            }
            else
            {
                throw new InvalidPrivacyCommandException($"Command Subject '{commandClaims.Subject.GetType()}'is not valid", loggableInformation);
            }

            if (commandClaims.AzureBlobContainerTargetUri != null)
            {
                Uri commandExportUri = commandClaims.AzureBlobContainerTargetUri;
                Uri tokenExportUri = claimsInToken.GetAzureStorageUri();

                // token claim not found
                if (tokenExportUri == null)
                {
                    throw new InvalidAzureStorageUriException("Token storage uri claim not found.");
                }

                // Does not match scheme
                if (commandExportUri.Scheme != tokenExportUri.Scheme)
                {
                    throw new InvalidAzureStorageUriException($"The command storage uri schema '{commandExportUri.Scheme}' does not match token one '{tokenExportUri.Scheme}'.");
                }

                // Does not match port
                if (commandExportUri.Port != tokenExportUri.Port)
                {
                    throw new InvalidAzureStorageUriException($"The command storage uri port '{commandExportUri.Port}' does not match token one '{tokenExportUri.Port}'.");
                }

                // Token is not base uri
                if (!tokenExportUri.IsBaseOf(commandExportUri))
                {
                    throw new InvalidAzureStorageUriException($"The token storage uri '{tokenExportUri.OriginalString}' is not the base uri for the command uri '{commandExportUri.OriginalString}'.");
                }
            }
        }

        private static void ValidateOperation(CommandClaims commandClaims, IEnumerable<Claim> claimsInToken, LoggableInformation loggableInformation)
        {
            string tokenOperationStr = claimsInToken.GetOperation();
            if (!Enum.TryParse(tokenOperationStr, true, out ValidOperation tokenOperation))
            {
                throw new InvalidPrivacyCommandException($"Failed to parse the token operation '{tokenOperationStr}'", loggableInformation);
            }

            if (tokenOperation == ValidOperation.ScopedDelete && commandClaims.Operation == ValidOperation.Delete)
            {
                ValidateDataType(commandClaims, claimsInToken, loggableInformation);
            }
            else if (tokenOperation != commandClaims.Operation)
            {
                throw new InvalidPrivacyCommandException($"Operation in command '{commandClaims.Operation}' does not match the Operation in verifier claims '{tokenOperation}'", loggableInformation);
            }
            else if (commandClaims.Operation == ValidOperation.ScopedDelete)
            {
                throw new InvalidPrivacyCommandException($"Command operation cannot be ScopedDelete", loggableInformation);
            }
        }

        private static void ValidateDataType(CommandClaims commandClaims, IEnumerable<Claim> claimsInToken, LoggableInformation loggableInformation)
        {
            string tokenRawDataType = claimsInToken.GetDataType();
            DataTypeId commandDataType = commandClaims.DataType;

            // Check for null or empty
            if (commandDataType == null || string.IsNullOrWhiteSpace(tokenRawDataType))
            {
                throw new InvalidPrivacyCommandException($"ScopedDelete operations must have a data type.");
            }

            // Parse claim
            DataTypeId tokenDataType;
            if (!Policies.Current.DataTypes.TryCreateId(tokenRawDataType, out tokenDataType))
            {
                throw new InvalidPrivacyCommandException($"Could not parse token data type '{tokenRawDataType}'.");
            }

            // DataType.Any is invalid
            if (commandDataType == Policies.Current.DataTypes.Ids.Any || tokenDataType == Policies.Current.DataTypes.Ids.Any)
            {
                throw new InvalidPrivacyCommandException($"ScopedDelete operations cannot have data type 'Any'.");
            }

            // Ensure token contains the command data type
            if (commandDataType != tokenDataType)
            {
                throw new InvalidPrivacyCommandException($"Data type mismatch. Command data type: {commandDataType.Value}. Token data type: {tokenDataType.Value}.", loggableInformation);
            }
        }

        private static void ValidateRequestId(CommandClaims commandClaims, IEnumerable<Claim> claimsInToken, LoggableInformation loggableInformation)
        {
            string[] requestIdsInToken = claimsInToken.GetRequestIds();
            if (!string.IsNullOrWhiteSpace(commandClaims.CommandId))
            {
                if (!Guid.TryParse(commandClaims.CommandId, out Guid claimCommandId))
                {
                    throw new InvalidPrivacyCommandException($"CommandId in command '{commandClaims.CommandId}' was not a GUID.", loggableInformation);
                }

                bool match = false;
                foreach (string requestIdInToken in requestIdsInToken)
                {
                    if (!Guid.TryParse(requestIdInToken, out Guid tokenCommandId))
                    {
                        throw new InvalidPrivacyCommandException($"CommandId in token '{requestIdInToken}' was not a GUID.", loggableInformation);
                    }

                    if (claimCommandId == tokenCommandId)
                    {
                        match = true;
                        break;
                    }
                }

                if (!match)
                {
                    string possibleRequestIds = string.Join(",", requestIdsInToken);
                    throw new InvalidPrivacyCommandException($"CommandId in command '{claimCommandId}' does not match the RequestIds in verifier claims '{possibleRequestIds}'", loggableInformation);
                }
            }
        }

        private static void ValidateBooleanFlag(string flagName, bool flagValue, string rawClaimValue, LoggableInformation loggableInformation)
        {
            // Only validate if the claims from the token has a value for this claim. If the command contains stuff not in the token, that's OK.
            if (!string.IsNullOrEmpty(rawClaimValue))
            {
                if (!bool.TryParse(rawClaimValue, out bool claimValue))
                {
                    throw new InvalidPrivacyCommandException($"{flagName} flag '{rawClaimValue}' was not parsable as a bool.", loggableInformation);
                }

                if (flagValue != claimValue)
                {
                    throw new InvalidPrivacyCommandException($"{flagName} flag in command '{flagValue}' did not match token claim of '{rawClaimValue}'.", loggableInformation);
                }
            }
        }

        private static void ValidateAadSubject(AadSubject subject, LoggableInformation loggableInformation, IEnumerable<Claim> claims)
        {
            if (subject.ObjectId != Guid.Empty && subject.ObjectId != claims.GetObjectId())
            {
                throw new InvalidPrivacyCommandException($"ObjectId in command does not match the ObjectId in verifier claims", loggableInformation);
            }

            if (subject.OrgIdPUID != 0 && subject.OrgIdPUID != claims.GetPuid())
            {
                throw new InvalidPrivacyCommandException($"OrgIdPUID in command does not match the PUID in verifier claims", loggableInformation);
            }

            if (subject.TenantId != Guid.Empty && subject.TenantId != claims.GetTenantIdFromClaims())
            {
                throw new InvalidPrivacyCommandException($"TenantId in command does not match the TenantId in verifier claims", loggableInformation);
            }
        }

        private static void ValidateAadSubject2(AadSubject2 subject, LoggableInformation loggableInformation, IEnumerable<Claim> claims)
        {
            string verifierVersion = claims.GetVersion();

            if (verifierVersion == VerifierV2 && subject.TenantIdType == TenantIdType.Resource)
            {
                throw new InvalidPrivacyCommandException($"V3 verifier is required for Resource Tenant", loggableInformation);
            }

            if (verifierVersion == VerifierV3 && subject.TenantIdType != claims.GetTenantIdTypeFromClaims(loggableInformation))
            {
                throw new InvalidPrivacyCommandException($"TenantIdType in command does not match the TenantIdType in verifier claims", loggableInformation);
            }

            if (subject.ObjectId != Guid.Empty && subject.ObjectId != claims.GetObjectId(subject.TenantIdType))
            {
                throw new InvalidPrivacyCommandException($"ObjectId in command does not match the ObjectId in verifier claims", loggableInformation);
            }

            if (subject.OrgIdPUID != 0 && subject.OrgIdPUID != claims.GetPuid(subject.TenantIdType))
            {
                throw new InvalidPrivacyCommandException($"OrgIdPUID in command does not match the PUID in verifier claims", loggableInformation);
            }

            if (subject.TenantId != Guid.Empty && subject.TenantId != claims.GetTenantIdFromClaims())
            {
                throw new InvalidPrivacyCommandException($"TenantId in command does not match the TenantId in verifier claims", loggableInformation);
            }

            if (subject.TenantIdType == TenantIdType.Resource)
            {
                // HomeTenantId cannot be empty if there is a type resource
                if (subject.HomeTenantId == Guid.Empty || subject.HomeTenantId != claims.GetHomeTenantIdFromClaims())
                {
                    throw new InvalidPrivacyCommandException($"TenantIdType in command does not match the TenantIdType in verifier claims", loggableInformation);
                }
            }
            else if (subject.TenantIdType == TenantIdType.Home)
            {
                // HomeTenantId can only be empty if the tenantIdType is home, and must be equal to the tenant Id
                if (subject.HomeTenantId != Guid.Empty && subject.HomeTenantId != claims.GetTenantIdFromClaims())
                {
                    throw new InvalidPrivacyCommandException($"HomeTenantId in command does not match the TenantId in verifier claims", loggableInformation);
                }
            }
            else
            {
                throw new InvalidPrivacyCommandException($"TenantIdType is not a valid value", loggableInformation);
            } 
        }

        private static void ValidateMsaSubject(MsaSubject subject, LoggableInformation loggableInformation, IEnumerable<Claim> claims)
        {
            if (subject.Puid != 0 && subject.Puid != claims.GetPuid())
            {
                throw new InvalidPrivacyCommandException("Puid in command does not match the PUID in verifier claims", loggableInformation);
            }

            if (subject.Cid != 0 && subject.Cid != claims.GetCid())
            {
                throw new InvalidPrivacyCommandException("Cid in command does not match the Cid in verifier claims", loggableInformation);
            }

            if (!string.IsNullOrWhiteSpace(subject.Anid) && string.Compare(subject.Anid, claims.GetAnid(), StringComparison.OrdinalIgnoreCase) != 0)
            {
                throw new InvalidPrivacyCommandException("Anid in command does not match the Anid in verifier claims", loggableInformation);
            }

            if (!string.IsNullOrWhiteSpace(subject.Xuid) && string.Compare(subject.Xuid, claims.GetXuid(), StringComparison.OrdinalIgnoreCase) != 0)
            {
                throw new InvalidPrivacyCommandException("Xuid in command does not match the Xuid in verifier claims", loggableInformation);
            }
        }

        private static void ValidateDeviceSubject(DeviceSubject subject, LoggableInformation loggableInformation, IEnumerable<Claim> claims)
        {
            // verifier contains deviceId as a puid
            if (subject.GlobalDeviceId != 0 && subject.GlobalDeviceId != claims.GetPuid())
            {
                throw new InvalidPrivacyCommandException("GlobalDeviceId in command does not match the PUID in verifier claims", loggableInformation);
            }
        }

        private static void ValidateTenantId(Guid tenantIdFromIssuer, IPrivacySubject subject, Guid configurationTenantId, LoggableInformation loggableInformation)
        {
            Guid expectedTenantId = configurationTenantId;
            if (subject.GetType() == typeof(MsaSubject) || subject.GetType() == typeof(DeviceSubject))
            {
                if (tenantIdFromIssuer == configurationTenantId)
                {
                    return;
                }
            }
            else if (subject is AadSubject)
            {
                // TODO: refactor this code. This logic is not applicable for AadSubjects. TenantId check is performed at the ValidateAadSubject method.
                return;
            }
            else
            {
                throw new InvalidPrivacyCommandException($"Command Subject '{subject.GetType()}'is not valid", loggableInformation);
            }

            throw new InvalidPrivacyCommandException($"Verifier doesn't contain an valid tenantId in issuer. TenantId in Issuer claim path: '{tenantIdFromIssuer}', expected tenantId : '{expectedTenantId}'", loggableInformation);
        }
    }
}

namespace Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation
{
    using System.Collections.Generic;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;

    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;

    /// <summary>
    /// Validates the JwtToken and IValidator
    /// </summary>
    public interface ITokenValidator
    {
        /// <summary>
        /// Does the prechecks on the JwtToken before verifying the signature
        /// </summary>
        /// <param name="jwtSecurityToken">JwtToken from the verifier</param>
        /// <param name="subject">PrivacySubject to provide the tenantId</param>
        /// <param name="loggableInformation">Loggable to provide information in exceptions</param>
        /// <param name="configuration">Environment configuration</param>
        void RunPrechecksOnToken(JwtSecurityToken jwtSecurityToken, IPrivacySubject subject, LoggableInformation loggableInformation, EnvironmentConfiguration configuration);

        /// <summary>
        /// Compare the JwtToken and the command and validate the command
        /// </summary>
        /// <param name="commandClaims">Privacy command to be validated</param>
        /// <param name="loggableInformation">Loggable to provide information in exceptions</param>
        /// <param name="claimsInToken">claims in the jwtToken</param>
        void ValidateCommand(CommandClaims commandClaims, LoggableInformation loggableInformation, IEnumerable<Claim> claimsInToken);
    }
}

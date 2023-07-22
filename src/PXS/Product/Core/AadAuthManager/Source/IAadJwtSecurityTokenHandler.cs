// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication
{
    using Microsoft.IdentityModel.Protocols;
    using Microsoft.IdentityModel.Protocols.OpenIdConnect;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Threading.Tasks;

    /// <summary>
    ///     IAadJwtSecurityTokenHandler.
    /// </summary>
    public interface IAadJwtSecurityTokenHandler
    {
        /// <summary>
        ///     Get the connection configuration for the JwtSecurityToken.
        /// </summary>
        /// <param name="token">The JwtSecurityToken</param>
        /// <param name="aadConfigRetriever">Configuration retriever</param>
        /// <returns>IConnectConfigurationWrapper object.</returns>
        Task<IConnectConfigurationWrapper> GetConnectConfigurationAsync(JwtSecurityToken token, IConfigurationRetriever<OpenIdConnectConfiguration> aadConfigRetriever);

        /// <summary>
        ///     Maps the token to App Id per issuing authority / sovereign cloud.
        /// </summary>
        /// <returns></returns>
        string MapTokenToAppId(JwtSecurityToken token);

        /// <summary>
        ///     Validate an incoming AAD JWT.
        /// </summary>
        /// <param name="token">The incoming AAD JWT.</param>
        /// <param name="appId">The app Id of the receiving service.</param>
        /// <param name="connectConfiguration">The connect configuration.</param>
        /// <returns>A ClaimsPrincipal object containing JWT claims.</returns>
        ClaimsPrincipal ValidateToken(string token, string appId, IConnectConfigurationWrapper connectConfiguration);

        /// <summary>
        /// Checks to see if the provided tenant ID is valid.
        /// </summary>
        /// <param name="tenantId">The tenantId</param>
        /// <param name="aadConfigRetriever">Configuration retriever</param>
        Task<bool> IsTenantIdValidAsync(string tenantId, IConfigurationRetriever<OpenIdConnectConfiguration> aadConfigRetriever);
    }
}

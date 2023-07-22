// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication
{
    /// <summary>
    ///     JwtOutboundPolicyConfig.
    /// </summary>
    public class JwtOutboundPolicy : IJwtOutboundPolicy
    {
        /// <summary>
        ///     Gets or sets the AppId.
        /// </summary>
        public string AppId { get; }

        /// <summary>
        ///     Gets or sets the Authority.
        /// </summary>
        public string Authority { get; }

        /// <summary>
        ///     Gets or sets the Resource.
        /// </summary>
        public string Resource { get; }

        /// <summary>
        ///     Gets or sets the TokenEndpoint.
        /// </summary>
        public string TokenEndpoint { get; }

        /// <summary>
        ///     Gets or sets the TargetTenantId.
        /// </summary>
        public string TargetTenantId { get; }

        /// <summary>
        ///     Create an instance of JwtOutboundPolicyConfig.
        /// </summary>
        /// <param name="targetAppId">The target App Id.</param>
        /// <param name="targetTenantId">The target Tenant Id.</param>
        /// <param name="aadLoginEndpoint">The aad login endpoint. ie. login.microsoftonline.com</param>
        /// <param name="stsAuthority">The sts authority. ie. sts.windows.net</param>
        public JwtOutboundPolicy(string targetAppId, string targetTenantId, string aadLoginEndpoint, string stsAuthority)
        {
            this.AppId = targetAppId;
            this.Resource = targetAppId;
            this.TargetTenantId = targetTenantId;
            this.Authority = $"https://{stsAuthority?.TrimEnd('/')}/{targetTenantId}/";
            this.TokenEndpoint = $"https://{aadLoginEndpoint?.TrimEnd('/')}/{targetTenantId}/oauth2/token";
        }
    }
}

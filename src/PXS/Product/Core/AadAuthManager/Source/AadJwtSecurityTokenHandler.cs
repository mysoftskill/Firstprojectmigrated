// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication
{
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.IdentityModel.Protocols;
    using Microsoft.IdentityModel.Protocols.OpenIdConnect;
    using Microsoft.IdentityModel.S2S.Configuration;
    using Microsoft.IdentityModel.S2S.Tokens;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.PrivacyServices.Common.Azure;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Net.Http;
    using System.Security.Claims;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    ///     AadJwtSecurityTokenHandler
    /// </summary>
    public class AadJwtSecurityTokenHandler : IAadJwtSecurityTokenHandler
    {
        private readonly string aadLoginEndpointForOpenId;

        private readonly string defaultAppId;

        private readonly Uri defaultOpenidConfigurationEndpoint;

        private readonly IIssuerAppIdConfig fairfaxConfiguration;

        private readonly IDictionary<string, string> issuerAppIdConfigurations;

        private readonly IDictionary<string, Uri> issuerOpenIdConfigurations;

        private readonly ILogger logger;

        private readonly static HttpClient httpClient = new HttpClient(new WebRequestHandler() { AllowPipelining = false });

        /// <summary>
        ///     Creates a new instance of AadJwtSecurityTokenHandler
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="privacyConfigurationManager">Configuration manager</param>
        public AadJwtSecurityTokenHandler(ILogger logger, IPrivacyConfigurationManager privacyConfigurationManager)
        {
            if (privacyConfigurationManager == null)
                throw new ArgumentNullException(nameof(privacyConfigurationManager));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.issuerOpenIdConfigurations = privacyConfigurationManager.AadTokenAuthGeneratorConfiguration.IssuerAppIdConfigs?.Values.ToDictionary(
                c => new Uri(c.StsAuthorityEndpoint).Host,
                c => new Uri(c.OpenIdConfigurationEndpoint),
                StringComparer.OrdinalIgnoreCase);
            this.issuerAppIdConfigurations = privacyConfigurationManager.AadTokenAuthGeneratorConfiguration.IssuerAppIdConfigs?.Values.ToDictionary(
                c => new Uri(c.StsAuthorityEndpoint).Host,
                c => c.AppId,
                StringComparer.OrdinalIgnoreCase);
            this.defaultAppId = privacyConfigurationManager.AadTokenAuthGeneratorConfiguration.AadAppId;
            this.defaultOpenidConfigurationEndpoint = new Uri(
                "https://" +
                $"{privacyConfigurationManager.AadTokenAuthGeneratorConfiguration.AadLoginEndpointForOpenId?.TrimEnd('/')}" +
                "/" +
                $"{privacyConfigurationManager.AadTokenAuthGeneratorConfiguration.AuthorityTenantIdForOpenId}" +
                "/.well-known/openid-configuration");
            this.aadLoginEndpointForOpenId = privacyConfigurationManager.AadTokenAuthGeneratorConfiguration.AadLoginEndpointForOpenId;

            // Workaround for fairfax because the issuing authority doesn't match the login authority endpoint, so it's not easy to know if the token is from fairfax.
            privacyConfigurationManager.AadTokenAuthGeneratorConfiguration?.IssuerAppIdConfigs?.TryGetValue("AadFairfax", out this.fairfaxConfiguration);
        }

        /// <inheritdoc />
        public async Task<IConnectConfigurationWrapper> GetConnectConfigurationAsync(JwtSecurityToken token, IConfigurationRetriever<OpenIdConnectConfiguration> aadConfigRetriever)
        {
            var metadataAddress = this.MapIssuerToOpenIdConfigurationEndpoint(token).ToString();
            IConfigurationManager<OpenIdConnectConfiguration> configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                metadataAddress,
                aadConfigRetriever);

            OpenIdConnectConfiguration connectConfig = null;
            try
            {
                connectConfig = await configurationManager.GetConfigurationAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (InvalidOperationException ex)
            {
                this.logger.Error(nameof(AadJwtSecurityTokenHandler), ex, $"Unable to get OpenIdConnectConfiguration for {metadataAddress}");
                throw new SecurityTokenException("Unable to get OpenIdConnectConfiguration");
            }

            return new ConnectConfigurationWrapper(connectConfig);
        }

        /// <inheritdoc />
        public async Task<bool> IsTenantIdValidAsync(string tenantId, IConfigurationRetriever<OpenIdConnectConfiguration> aadConfigRetriever)
        {
            bool isTenantIdValid = false;
            var metadataAddress = this.CreateOpenIdConfigurationEndpoint(tenantId).ToString();
            int retryCount = 0;
            //OpenIdConnectConfiguration openidConfiguration = null;
            logger.Information(nameof(AadJwtSecurityTokenHandler), $"Validation started for tenant: {tenantId}");
            while (retryCount < 3)
            {
                try
                {
                    var openidConfiguration = await OpenIdConnectConfigurationRetriever.GetAsync(
                    metadataAddress, CancellationToken.None);
                    isTenantIdValid = true;
                    break;
                }
                catch (Exception ex)
                {
                    using (HttpRequestMessage discoveryRequest = new HttpRequestMessage(HttpMethod.Get, metadataAddress))
                    {
                        try
                        {
                            using (HttpResponseMessage discoveryResponse = await httpClient.SendAsync(discoveryRequest))
                            {
                                HttpContent discoveryContentQuery = discoveryResponse.Content;
                                string contentString = await discoveryContentQuery.ReadAsStringAsync();
                                if (string.IsNullOrWhiteSpace(contentString) == false)
                                {
                                    dynamic discoveryContent = JsonConvert.DeserializeObject(contentString);
                                    string errorMessage = discoveryContent.error;
                                    if (errorMessage != null)
                                    {
                                        if (errorMessage == "invalid_tenant")
                                        {
                                            this.logger.Error(nameof(AadJwtSecurityTokenHandler), ex, $"Tenant {tenantId} not found.");
                                        }
                                        this.logger.Error(nameof(AadJwtSecurityTokenHandler), ex, $"Tenant validation failed for {tenantId} failed with error: {errorMessage}.");
                                    }
                                    else
                                    {
                                        isTenantIdValid = true;
                                    }
                                    break;
                                }
                            }
                        }
                        catch (Exception)
                        {
                            retryCount++;
                        }
                    }
                 
                }
            }

            return isTenantIdValid;
        }

        /// <inheritdoc />
        public string MapTokenToAppId(JwtSecurityToken token)
        {
            if (token == null)
            {
                this.logger.Error(nameof(AadJwtSecurityTokenHandler), $"Token is null - using default: {this.defaultAppId}");
                return this.defaultAppId;
            }
            
            string issuer = token.Issuer;
            Uri issuerUri;
            try
            {
                issuerUri = new Uri(issuer);
            }
            catch (Exception)
            {
                this.logger.Error(nameof(AadJwtSecurityTokenHandler), $"Issuer [{issuer}] wasn't a valid Uri. Using default: {this.defaultAppId}.]");
                return this.defaultAppId;
            }

            if (this.ShouldUseFairfaxConfiguration(issuer))
            {
                return this.fairfaxConfiguration.AppId;
            }

            if (this.issuerAppIdConfigurations.TryGetValue(issuerUri.Host, out string appIdValue))
            {
                return appIdValue;
            }

            this.logger.Warning(
                nameof(AadJwtSecurityTokenHandler),
                $"Mapping to default app id: {this.defaultAppId}. This should only happen if the issuer could not map to an app id.");
            return this.defaultAppId;
        }

        /// <inheritdoc />
        public ClaimsPrincipal ValidateToken(string token, string appId, IConnectConfigurationWrapper connectConfiguration)
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidAudience = appId,
                ValidateAudience = true,
                ValidIssuer = connectConfiguration.Issuer,
                ValidateIssuer = true,
                IssuerSigningKeys = connectConfiguration.SigningKeys
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var claimsPrincipal = tokenHandler.ValidateToken(token, validationParameters, out _);
                  
            return claimsPrincipal;
        }

        internal Uri MapIssuerToOpenIdConfigurationEndpoint(JwtSecurityToken token)
        {
            if (token == null)
            {
                this.logger.Error(nameof(AadJwtSecurityTokenHandler), "Token is null, cannot infer an open id configuration.");
                return this.defaultOpenidConfigurationEndpoint;
            }

            string issuer = token.Issuer;
            Uri issuerUri;
            try
            {
                issuerUri = new Uri(issuer);
            }
            catch (Exception)
            {
                this.logger.Error(nameof(AadJwtSecurityTokenHandler), $"Issuer: [{issuer}] wasn't a valid Uri. Using default: {this.defaultOpenidConfigurationEndpoint}.");
                return this.defaultOpenidConfigurationEndpoint;
            }

            if (this.ShouldUseFairfaxConfiguration(issuer))
            {
                return new Uri(this.fairfaxConfiguration.OpenIdConfigurationEndpoint);
            }

            if (this.issuerOpenIdConfigurations.TryGetValue(issuerUri.Host, out Uri openIdConfigurationEndpoint))
            {
                return openIdConfigurationEndpoint;
            }

            this.logger.Warning(
                nameof(AadJwtSecurityTokenHandler),
                $"Mapping to default OpenIdConfiguration: {this.defaultOpenidConfigurationEndpoint}. This should only happen if the issuer could not map to an OpenIdConfiguration.");
            return this.defaultOpenidConfigurationEndpoint;
        }

        internal Uri CreateOpenIdConfigurationEndpoint(string tenantId)
        {
            if (tenantId == null)
            {
                this.logger.Error(nameof(AadJwtSecurityTokenHandler), $"TenantID is null - using default endpoint {this.defaultOpenidConfigurationEndpoint}");
                return this.defaultOpenidConfigurationEndpoint;
            }

            return new Uri(
                "https://" +
                $"{this.aadLoginEndpointForOpenId?.TrimEnd('/')}" +
                "/" +
                $"{tenantId}" +
                "/.well-known/openid-configuration");

        }

        private bool ShouldUseFairfaxConfiguration(string issuer)
        {
            // Fairfax breaks the rules of other clouds because is uses the same issuing authority as PROD, so special case the tenant id belonging to that cloud.
            // To confirm, check values @ https://login.microsoftonline.us/f8cdef31-a31e-4b4a-93e4-5f571e91255a/.well-known/openid-configuration

            return issuer.Equals("https://sts.windows.net/f8cdef31-a31e-4b4a-93e4-5f571e91255a/", StringComparison.OrdinalIgnoreCase) && this.fairfaxConfiguration != null;
        }
    }
}

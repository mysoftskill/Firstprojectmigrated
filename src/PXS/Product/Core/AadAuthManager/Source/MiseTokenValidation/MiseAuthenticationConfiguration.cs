// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication
{
    using Microsoft.IdentityModel.S2S.Configuration;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using System.Collections.Generic;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.Cloud.InstrumentationFramework;

    public class MiseAuthenticationConfiguration
    {
        private MiseAuthenticationConfiguration(ILogger privacyServiceLogger, IPrivacyConfigurationManager privacyConfigurationManager)
        {
            var aadAuthenticationOptions = new AadAuthenticationOptions();
            var validIncomingAppIds = privacyConfigurationManager.AadTokenAuthGeneratorConfiguration.JwtInboundPolicyConfig.ValidIncomingAppIds;
            var validAudiences = privacyConfigurationManager.AadTokenAuthGeneratorConfiguration.JwtInboundPolicyConfig.Audiences;
            var issuerPrefixes = privacyConfigurationManager.AadTokenAuthGeneratorConfiguration.JwtInboundPolicyConfig.IssuerPrefixes;

            aadAuthenticationOptions.InboundPolicies = new List<AadInboundPolicyOptions>();
            aadAuthenticationOptions.ClientId = privacyConfigurationManager.AadTokenAuthGeneratorConfiguration.AadAppId;

            foreach (var issuer in issuerPrefixes)
            {
                var policy = new AadInboundPolicyOptions()
                {
                    Label = $"InboundPolicy_{issuer}",
                    AllowMultiTenant = true,
                    TenantId = "common",
                    ValidAudiences = validAudiences,
                    ValidApplicationIds = validIncomingAppIds,
                    Instance = issuer,
                    AuthenticationSchemes = AuthenticationSchemes,               
                    TokenValidationPolicy = new TokenValidationParametersOptions
                    {
                        ValidateAudience = true,
                        ValidateIssuer = true,
                        ValidateLifetime = true,
                    }
                };
                aadAuthenticationOptions.InboundPolicies.Add( policy );
            }
            foreach (var policy in aadAuthenticationOptions.InboundPolicies)
            {
                privacyServiceLogger.Log(IfxTracingLevel.Informational, "MiseTokenValidationUtility", $"Added Inbound Policy for issuer {policy.Instance} with ValidAudiences: {string.Join(" ", policy.ValidAudiences)} and ValidApplicationsIds: {string.Join(" ", policy.ValidApplicationIds)}");
            }
            AadAuthenticationOptions = aadAuthenticationOptions;
        }

        /// <summary>
        /// Set up AadAuthenticationOptions for use in MISE Token Validation
        /// </summary>
        /// <returns></returns>
        public static AadAuthenticationOptions GetAuthenticationOptionsInstance(ILogger privacyServiceLogger, IPrivacyConfigurationManager privacyConfigurationManager)
        {
            if (AadAuthenticationOptions == null)
            {
                _ = new MiseAuthenticationConfiguration(privacyServiceLogger, privacyConfigurationManager);
            }
            return AadAuthenticationOptions;
        }

        /// <summary>
        /// AadAuthenticationOptions used in MISE Token Validation
        /// </summary>
        private static AadAuthenticationOptions AadAuthenticationOptions { get; set; }

        /// <summary>
        /// AuthenticationScheme used in MISE Token Validation
        /// See https://identitydivision.visualstudio.com/DevEx/_git/MISE?path=%2Fsrc%2FCore%2FAuthenticationScheme.cs&_a=contents&version=GBmaster 
        /// and https://identitydivision.visualstudio.com/DevEx/_wiki/wikis/DevEx.wiki/13164/Validate-PFT-tokens For more information
        /// </summary>
        private static readonly List<string> AuthenticationSchemes = new List<string>() { "Bearer",  "MSAuth_1_0_PFAT"};
    }
}

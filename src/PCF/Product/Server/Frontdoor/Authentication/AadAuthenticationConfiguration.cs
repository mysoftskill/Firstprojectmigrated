namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.IdentityModel.Protocols;
    using Microsoft.IdentityModel.Protocols.OpenIdConnect;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.Windows.Services.AuthN.Server;

    /// <summary>
    /// Contains the configuration to validate bearer token for public and sovereign cloud instances
    /// </summary>
    public class AadAuthenticationConfiguration
    {
        /// <summary>
        /// Production configuration.
        /// </summary>
        public static readonly AadAuthenticationConfiguration Public = new AadAuthenticationConfiguration(
            Policies.Current.CloudInstances.Ids.Public.Value,
            "https://sts.windows.net/",
            Config.Instance.Common.AzureActiveDirectoryValidAudience,
            Config.Instance.Common.AzureActiveDirectoryValidAudienceAME);

        /// <summary>
        /// Mooncake configuration.
        /// </summary>
        public static readonly AadAuthenticationConfiguration Mooncake = new AadAuthenticationConfiguration(
            Policies.Current.CloudInstances.Ids.CN_Azure_Mooncake.Value,
            "https://sts.chinacloudapi.cn/",
            "https://ChinaGovCloud.partner.onmschina.cn/a1072bf2-9665-4644-86fe-094e0c48ead8");

        /// <summary>
        /// Fairfax configuration.
        /// </summary>
        public static readonly AadAuthenticationConfiguration Fairfax = new AadAuthenticationConfiguration(
            Policies.Current.CloudInstances.Ids.US_Azure_Fairfax.Value,
            "https://sts.microsoftonline.us/",
            "https://USGovCloud.onmicrosoft.com/30e7cf4b-a849-4a5a-9265-c0748a538c49");
        
        private readonly HashSet<string> audiences;

        /// <summary>
        /// The cloud instance deployment location value from Microsoft.PrivacyServices.Policy.Current.CloudInstances.Ids
        /// </summary>
        public string CloudInstance { get; }

        /// <summary>
        /// Secure Token Service endpoint (Issuer)
        /// </summary>
        public string Issuer { get; }

        /// <summary>
        /// OpenId configuration endpoint (metadataAddress) which is used by 
        /// Server2Server Authentication Library (SAL) to authenticate 
        /// </summary>
        public string OpenIdConfigurationEndpoint { get; private set; }

        /// <summary>
        /// The configuration manager for OpenIdConnectConfiguration
        /// </summary>
        public IConfigurationManager<OpenIdConnectConfiguration> ConfigurationManager { get; }

        /// <summary>
        /// ResourceId (pcf endpoint in the third party app)
        /// </summary>
        public IEnumerable<string> Audiences => this.audiences;

        /// <summary>
        /// Dictionary of AuthenticationConfigurations based on the issuer string ahd cloudInstance
        /// Note: generally issuer is unique per cloudInstance except in the special case of FirstParty apps
        /// where the issuer can be common across different cloud instances
        /// </summary>
        private static readonly ConcurrentDictionary<(string cloudInstance, string issuer), AadAuthenticationConfiguration> AuthenticationConfigurations
            = new ConcurrentDictionary<(string cloudInstance, string issuer), AadAuthenticationConfiguration>();

        private static readonly List<string> KnownAadTokenIssuers = new List<string>()
        {
            "https://sts.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47/",
            "https://sts.windows.net/33e01921-4d64-4f8c-a055-5bdaffd5e33d/",
            "https://sts.microsoftonline.de/82e38fd9-ed4f-4108-920b-ad208fe3879c/",
            "https://sts.microsoftonline.de/47700c02-dab5-4be3-958d-f90aff42622e/",
            "https://sts.microsoftonline.de/f577cd82-810c-43f9-a1f6-0cc532871050/",
            "https://sts.chinacloudapi.cn/a55a4d5b-9241-49b1-b4ff-befa8db00269/",
            "https://sts.chinacloudapi.cn/f9b28ad5-399e-4e2c-ba0a-e3097d7bf521/",
            "https://sts.chinacloudapi.cn/0b4a31a2-c1a0-475d-b363-5f26668660a3/",
            "https://sts.chinacloudapi.cn/3d0a72e2-8b06-4528-98df-1391c6f12c11/", // mcdevops.partner.onmschina.cn
            "https://sts.windows.net/cab8a31a-1906-4287-a0d8-4eef66b95f6e/",
            "https://sts.windows.net/f8cdef31-a31e-4b4a-93e4-5f571e91255a/",
            "https://sts.windows.net/975f013f-7f24-47e8-a7d3-abc4752bf346/",  // PME tenant Id. (AgentId: 8660a1a2-cb9c-4289-8e9d-63466f62d49c)
            "https://sts.windows.net/cdc5aeea-15c5-4db6-b079-fcadd2505dc2/", // Torus Tenant (Agent:d61df289-f421-4898-bfb9-286f3ad4bfc5)
            "https://sts.windows.net/cb2226c0-11aa-4521-b780-08bac67611f7/", // Fairfax Test tenant - Test_Test_Test_Microsoft EvoSTS AAD DoDCON
            "https://sts.windows.net/9b7f6997-548e-494d-b456-c6d17570d639/" // GraphAppManagement Tenant (Agent:0026f6f1-9444-4c2f-8183-587d0f26f5bd)
            #if INCLUDE_TEST_HOOKS
            , "https://sts.windows-ppe.net/ea8a4392-515e-481f-879e-6571ff2a8a36/" // PXS INT/CI pipelines APP ID: 705363a0-5817-47fb-ba32-59f47ce80bb7
            #endif
            
        };

        /// <summary>
        /// Constructor only used by the static instances, 
        /// since at this point we don't know the issuer and hence can't
        /// build the OpenIdConfig endpoint
        /// </summary>
        private AadAuthenticationConfiguration(
            string cloudInstance,
            string stsEndpoint,
            params string[] audiences)
        {
            this.CloudInstance = cloudInstance;
            this.Issuer = stsEndpoint;
            this.audiences = new HashSet<string>(audiences, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Constructs from a static instance and the App token (Jwt token)
        /// </summary>
        /// <param name="instanceToCreateFrom">Static instance from which to create</param>
        /// <param name="issuer">Issuer from the Jwt token</param>
        private AadAuthenticationConfiguration(AadAuthenticationConfiguration instanceToCreateFrom, string issuer)
        {
            this.CloudInstance = instanceToCreateFrom.CloudInstance;
            this.Issuer = instanceToCreateFrom.Issuer;
            this.audiences = new HashSet<string>(instanceToCreateFrom.audiences, instanceToCreateFrom.audiences.Comparer);
            
            ValidateIssuer(issuer);

            this.OpenIdConfigurationEndpoint = $"{issuer.TrimEnd('/')}/.well-known/openid-configuration";
            this.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                this.OpenIdConfigurationEndpoint,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever { RequireHttps = true });
        }

        /// <summary>
        /// Returns true if the given authentication configuration contains any of the given audiences.
        /// </summary>
        public bool ContainsAnyAudience(IEnumerable<string> testAudiences)
        {
            foreach (var audience in testAudiences)
            {
                if (this.audiences.Contains(audience))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Cache the AadAuthenticationConfiguration for each issuer and cloudInstance
        /// so we can have the ConfigurationManager manage its caching
        /// and not call the Config endpoint on every request
        /// </summary>
        private static AadAuthenticationConfiguration GetAuthenticationConfiguration(AadAuthenticationConfiguration staticInstance, string issuer)
        {
            if (!AuthenticationConfigurations.ContainsKey((staticInstance.CloudInstance, issuer)))
            {
                AuthenticationConfigurations[(staticInstance.CloudInstance, issuer)] = new AadAuthenticationConfiguration(staticInstance, issuer);
                return AuthenticationConfigurations[(staticInstance.CloudInstance, issuer)];
            }

            return AuthenticationConfigurations[(staticInstance.CloudInstance, issuer)];
        }

        /// <summary>
        /// Based on the given issuer string, get the appropriate configuration
        /// </summary>
        /// <param name="issuer">issuer in the JwtSecurityToken</param>
        /// <param name="audiences">The audiences in the JwtSecurityToken</param>
        /// <returns>The configuration which contains the keydiscoveryservice, ConfigurationManager and audience</returns>
        public static AadAuthenticationConfiguration GetAuthenticationConfiguration(string issuer, IEnumerable<string> audiences)
        {
            const string fairfaxAudience = "https://USGovCloud.onmicrosoft.com/30e7cf4b-a849-4a5a-9265-c0748a538c49";

            if (issuer.StartsWith(Mooncake.Issuer, StringComparison.OrdinalIgnoreCase))
            {
                return GetAuthenticationConfiguration(Mooncake, issuer);
            }

            if (issuer.StartsWith(Fairfax.Issuer, StringComparison.OrdinalIgnoreCase))
            {
                return GetAuthenticationConfiguration(Fairfax, issuer);
            }

            // Fairfax issuer is temporarily set to Public cloud sts endpoint instead of Fairfax sts endpoint while 
            // Fairfax AAD is being migrated from public cloud to Arlington so
            // try to identify fairfax using audiences.
            if (audiences.Contains(fairfaxAudience))
            {
                return GetAuthenticationConfiguration(Fairfax, issuer);
            }

            return GetAuthenticationConfiguration(Public, issuer);
        }

        /// <summary>
        /// Check if the issuer is one of the known list of issuers, if not log for now
        /// </summary>
        /// <param name="issuer">Issuer in the AAD Security Token</param>
        private static void ValidateIssuer(string issuer)
        {
            IncomingEvent.Current?.SetProperty("IsAadSecurityTokenIssuerValid", "true");
            if (!KnownAadTokenIssuers.Contains(issuer))
            {
                IncomingEvent.Current?.SetProperty("IsAadSecurityTokenIssuerValid", "false");
                throw new AuthNException(AuthNErrorCode.AuthenticationFailed, $"UnknownIssuer: {issuer}. Please contact ngpdataagent to trust this issuer.");
            }
        }
    }
}
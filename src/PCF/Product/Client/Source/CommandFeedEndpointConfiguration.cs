namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;

    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;
    using Microsoft.PrivacyServices.Policy;

    /// <summary>
    /// Declares all connection information used to communicate with the Privacy Command Feed.
    /// </summary>
    public class CommandFeedEndpointConfiguration
    {
        private const string PublicAadBaseUri = "https://login.microsoftonline.com/";
        private const string MooncakeAadBaseUri = "https://login.partner.microsoftonline.cn/";
        private const string FairfaxAadBaseUri = "https://login.microsoftonline.us/";

        private const string PcfV1HostNamePpe  = "pcf.privacy.microsoft-ppe.com";
        private const string PcfV1HostNameProd = "pcf.privacy.microsoft.com";
        
        private const string PcfV2HostNamePpe  = "pcfv2-ppe.privacy.microsoft-ppe.com";
        private const string PcfV2HostNameProd = "pcfv2.privacy.microsoft.com";

        private const string PcfResourceIdPpe = "https://MSAzureCloud.onmicrosoft.com/613e14a9-7c60-4f8b-863c-f719e68cd8db";
        private const string PcfResourceIdProd = "https://MSAzureCloud.onmicrosoft.com/469dcb1e-f765-4199-b091-1907c74d8a22";
        private const string PcfResourceIdProdMC = "https://ChinaGovCloud.partner.onmschina.cn/a1072bf2-9665-4644-86fe-094e0c48ead8";
        private const string PcfResourceIdProdFF = "https://USGovCloud.onmicrosoft.com/30e7cf4b-a849-4a5a-9265-c0748a538c49";

        private const string AadAuthorityMS = PublicAadBaseUri + "microsoft.onmicrosoft.com";
        private const string AadAuthorityAME = PublicAadBaseUri + "MSAzureCloud.onmicrosoft.com";

        /// <summary>
        /// Default settings for production for an app in the Microsoft tenant. Target resource id is the "NGP PCF Production" application in AME.
        /// </summary>
        public static readonly CommandFeedEndpointConfiguration Production = new CommandFeedEndpointConfiguration(
            msaTicketUri: new Uri("https://login.live.com/pksecure/oauth20_clientcredentials.srf"),
            commandFeedMsaSiteName: PcfV1HostNameProd,
            aadAuthority: AadAuthorityMS,
            commandFeedAadResourceId: PcfResourceIdProd,
            commandFeedHostName: PcfV1HostNameProd,
            environment: PcvEnvironment.Production,
            enforceValidation: true);

        /// <summary>
        /// Default settings for production for an app in the AME tenant. Target resource id is the "NGP PCF Production" application in AME.
        /// </summary>
        public static readonly CommandFeedEndpointConfiguration ProductionAME = new CommandFeedEndpointConfiguration(
            msaTicketUri: new Uri("https://login.live.com/pksecure/oauth20_clientcredentials.srf"),
            commandFeedMsaSiteName: PcfV1HostNameProd,
            aadAuthority: AadAuthorityAME,
            commandFeedAadResourceId: PcfResourceIdProd,
            commandFeedHostName: PcfV1HostNameProd,
            environment: PcvEnvironment.Production,
            enforceValidation: true);

        /// <summary>
        /// Production settings for Agents in Mooncake/Gallatin
        /// </summary>
        public static readonly CommandFeedEndpointConfiguration Mooncake = new CommandFeedEndpointConfiguration(
            msaTicketUri: new Uri("https://login.live.com/pksecure/oauth20_clientcredentials.srf"),
            commandFeedMsaSiteName: PcfV1HostNameProd,
            aadAuthority: MooncakeAadBaseUri + "ChinaGovCloud.partner.onmschina.cn",
            commandFeedAadResourceId: PcfResourceIdProdMC,
            commandFeedHostName: PcfV1HostNameProd,
            environment: PcvEnvironment.Production,
            enforceValidation: true);

        /// <summary>
        /// Production settings for Agents in Fairfax
        /// </summary>
        public static readonly CommandFeedEndpointConfiguration Fairfax = new CommandFeedEndpointConfiguration(
            msaTicketUri: new Uri("https://login.live.com/pksecure/oauth20_clientcredentials.srf"),
            commandFeedMsaSiteName: PcfV1HostNameProd,
            aadAuthority: FairfaxAadBaseUri + "USGovCloud.onmicrosoft.com",
            commandFeedAadResourceId: PcfResourceIdProdFF,
            commandFeedHostName: PcfV1HostNameProd,
            environment: PcvEnvironment.Production,
            enforceValidation: true);

        /// <summary>
        /// Default settings for PPE for an app in the Microsoft tenant. Target resource id is the "NGP PCF NonProd" application in AME.
        /// </summary>
        public static readonly CommandFeedEndpointConfiguration Preproduction = new CommandFeedEndpointConfiguration(
            msaTicketUri: new Uri("https://login.live.com/pksecure/oauth20_clientcredentials.srf"),
            commandFeedMsaSiteName: PcfV1HostNamePpe,
            aadAuthority: AadAuthorityMS,
            commandFeedAadResourceId: PcfResourceIdPpe,
            commandFeedHostName: PcfV1HostNamePpe,
            environment: PcvEnvironment.Preproduction,
            enforceValidation: false);

        /// <summary>
        /// Default settings for PPE for an app in the AME tenant. Target resource id is the "NGP PCF NonProd" application in AME.
        /// </summary>
        public static readonly CommandFeedEndpointConfiguration PreproductionAME = new CommandFeedEndpointConfiguration(
            msaTicketUri: new Uri("https://login.live.com/pksecure/oauth20_clientcredentials.srf"),
            commandFeedMsaSiteName: PcfV1HostNamePpe,
            aadAuthority: AadAuthorityAME,
            commandFeedAadResourceId: PcfResourceIdPpe,
            commandFeedHostName: PcfV1HostNamePpe,
            environment: PcvEnvironment.Preproduction,
            enforceValidation: false);

        /// <summary>
        /// Default settings for production v2 for an app in the Microsoft tenant. Target resource id is the "NGP PCF Production" application in AME.
        /// </summary>
        public static readonly CommandFeedEndpointConfiguration ProductionV2 = new CommandFeedEndpointConfiguration(
            msaTicketUri: null,
            commandFeedMsaSiteName: string.Empty,
            aadAuthority: AadAuthorityMS,
            commandFeedAadResourceId: PcfResourceIdProd,
            commandFeedHostName: PcfV2HostNameProd,
            environment: PcvEnvironment.Production,
            enforceValidation: true);

        /// <summary>
        /// Default settings for production v2 for an app in the AME tenant. Target resource id is the "NGP PCF Production" application in AME.
        /// </summary>
        public static readonly CommandFeedEndpointConfiguration ProductionAMEV2 = new CommandFeedEndpointConfiguration(
            msaTicketUri: null,
            commandFeedMsaSiteName: string.Empty,
            aadAuthority: AadAuthorityAME,
            commandFeedAadResourceId: PcfResourceIdProd,
            commandFeedHostName: PcfV2HostNameProd,
            environment: PcvEnvironment.Production,
            enforceValidation: true);

        /// <summary>
        /// Production settings for V2 Agents in Mooncake 
        /// </summary>
        public static readonly CommandFeedEndpointConfiguration MooncakeV2 = new CommandFeedEndpointConfiguration(
            msaTicketUri: null,
            commandFeedMsaSiteName: string.Empty,
            aadAuthority: MooncakeAadBaseUri + "ChinaGovCloud.partner.onmschina.cn",
            commandFeedAadResourceId: PcfResourceIdProdMC,
            commandFeedHostName: PcfV2HostNameProd,
            environment: PcvEnvironment.Production,
            enforceValidation: true);

        /// <summary>
        /// Production settings for V2 Agents in Fairfax
        /// </summary>
        public static readonly CommandFeedEndpointConfiguration FairfaxV2 = new CommandFeedEndpointConfiguration(
            msaTicketUri: null,
            commandFeedMsaSiteName: string.Empty,
            aadAuthority: FairfaxAadBaseUri + "USGovCloud.onmicrosoft.com",
            commandFeedAadResourceId: PcfResourceIdProdFF,
            commandFeedHostName: PcfV2HostNameProd,
            environment: PcvEnvironment.Production,
            enforceValidation: true);

        /// <summary>
        /// Default settings for PPE v2 for an app in the Microsoft tenant. Target resource id is the "NGP PCF NonProd" application in AME.
        /// </summary>
        public static readonly CommandFeedEndpointConfiguration PreproductionV2 = new CommandFeedEndpointConfiguration(
            msaTicketUri: null,
            commandFeedMsaSiteName: string.Empty,
            aadAuthority: AadAuthorityMS,
            commandFeedAadResourceId: PcfResourceIdPpe,
            commandFeedHostName: PcfV2HostNamePpe,
            environment: PcvEnvironment.Preproduction,
            enforceValidation: false);

        /// <summary>
        /// Default settings for PPE v2 for an app in the AME tenant. Target resource id is the "NGP PCF NonProd" application in AME.
        /// </summary>
        public static readonly CommandFeedEndpointConfiguration PreproductionAMEV2 = new CommandFeedEndpointConfiguration(
            msaTicketUri: null,
            commandFeedMsaSiteName: string.Empty,
            aadAuthority: AadAuthorityAME,
            commandFeedAadResourceId: PcfResourceIdPpe,
            commandFeedHostName: PcfV2HostNamePpe,
            environment: PcvEnvironment.Preproduction,
            enforceValidation: false);

        /// <summary>
        /// Initializes a new <see cref="CommandFeedEndpointConfiguration" />.
        /// </summary>
        /// <param name="msaTicketUri">The URI used to acquire MSA site based auth tickets.</param>
        /// <param name="commandFeedMsaSiteName">The MSA Site Name of the Privacy Command Feed.</param>
        /// <param name="aadAuthority">The AAD sign-in endpoint of the tenant</param>
        /// <param name="commandFeedAadResourceId">The Command Feed AAD App ID URI</param>
        /// <param name="commandFeedHostName">The host name of the Privacy Command Feed.</param>
        /// <param name="environment">Production or PreProduction</param>
        /// <param name="enforceValidation">If verifier validation needs to be enforced</param>
        public CommandFeedEndpointConfiguration(
            Uri msaTicketUri,
            string commandFeedMsaSiteName,
            string aadAuthority,
            string commandFeedAadResourceId,
            string commandFeedHostName,
            PcvEnvironment environment,
            bool enforceValidation)
        {
            this.MsaAuthEndpoint = msaTicketUri;
            this.CommandFeedHostName = commandFeedHostName;
            this.AadAuthority = aadAuthority;
            this.CommandFeedAadResourceId = commandFeedAadResourceId;
            this.CommandFeedMsaSiteName = commandFeedMsaSiteName;
            this.Environment = environment;
            this.EnforceValidation = enforceValidation;
        }

        /// <summary>
        /// Constructs an endpoint configuration from the deployment location and the AAD tenantHostName.
        /// Please review the <paramref name="deploymentLocation" /> parameter documentation.
        /// </summary>
        /// <param name="deploymentLocation">
        ///     <para>The sovereign or public cloud deploymentLocation.</para>
        ///     <para>
        ///     The current set of acceptable values for this parameter are:
        ///     "Public", "CN.Azure.Mooncake" and "US.Azure.Fairfax" or as dictated by the Ids in Microsoft.PrivacyServices.Policy
        ///     </para>
        ///     <para>These strings can be found as properties by using Microsoft.PrivacyServices.Policy.Policies.Current.CloudInstances.Ids.{your cloud location}.Value</para>
        /// </param>
        /// <param name="tenantHostName">Host of the AAD tenant in which the App is created. eg: MSGermany.onmicrosoft.de</param>
        /// <param name="environment">
        /// The Public cloud PcvEnvironment.Production or PcvEnvironment.Preproduction environment setting, defaulting to Production.
        /// Not applicable to agents located in Sovereign clouds since pre-production configurations aren't supported for this case.
        /// </param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public CommandFeedEndpointConfiguration(string deploymentLocation, string tenantHostName, PcvEnvironment environment = PcvEnvironment.Production)
        {
            CommandFeedEndpointConfiguration baseConfig;
            string aadBaseUri;

            if (deploymentLocation == Policies.Current.CloudInstances.Ids.Public.Value)
            {
                baseConfig = environment == PcvEnvironment.Production ? Production : Preproduction;
                aadBaseUri = PublicAadBaseUri;
            }
            else if (deploymentLocation == Policies.Current.CloudInstances.Ids.CN_Azure_Mooncake.Value)
            {
                baseConfig = Mooncake;
                aadBaseUri = MooncakeAadBaseUri;
            }
            else if (deploymentLocation == Policies.Current.CloudInstances.Ids.US_Azure_Fairfax.Value)
            {
                baseConfig = Fairfax;
                aadBaseUri = FairfaxAadBaseUri;
            }
            else
            {
                throw new ArgumentException($"Invalid deployment location cloud instance. {deploymentLocation}");
            }

            this.MsaAuthEndpoint = baseConfig.MsaAuthEndpoint;
            this.CommandFeedMsaSiteName = baseConfig.CommandFeedMsaSiteName;

            this.CommandFeedHostName = baseConfig.CommandFeedHostName;
            this.CommandFeedAadResourceId = baseConfig.CommandFeedAadResourceId;

            this.Environment = baseConfig.Environment;
            this.EnforceValidation = baseConfig.EnforceValidation;

            this.AadAuthority = new UriBuilder(new Uri(aadBaseUri))
            {
                Path = tenantHostName,
                Port = -1
            }.ToString();
        }

        /// <summary>
        /// The URI used to acquire MSA site based auth tickets.
        /// </summary>
        public Uri MsaAuthEndpoint { get; }

        /// <summary>
        /// The MSA site name for acquiring site based auth tickets.
        /// </summary>
        public string CommandFeedMsaSiteName { get; }

        /// <summary>
        /// The AAD sign-in endpoint of the tenant.
        /// </summary>
        public string AadAuthority { get; }

        /// <summary>
        /// The Command Feed AAD App ID URI.
        /// </summary>
        public string CommandFeedAadResourceId { get; }

        /// <summary>
        /// The DNS host name for connecting to the Privacy Command Feed.
        /// </summary>
        public string CommandFeedHostName { get; }

        /// <summary>
        /// Production or PreProduction.
        /// </summary>
        public PcvEnvironment Environment { get; }

        /// <summary>
        /// True in prod and false in all other environments.
        /// If true, the validation service validates the verifier string.
        /// </summary>
        public bool EnforceValidation { get; }
    }
}

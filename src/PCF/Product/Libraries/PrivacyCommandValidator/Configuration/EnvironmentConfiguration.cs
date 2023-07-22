namespace Microsoft.PrivacyServices.CommandFeed.Validator.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;

    /// <summary>
    /// Environment variables for Subject/Environment combination
    /// </summary>
    public class EnvironmentConfiguration
    {
        /// <summary>
        /// Environment configuration for production for MSA subject
        /// </summary>
        public static readonly EnvironmentConfiguration MsaProduction = new EnvironmentConfiguration(
            PcvEnvironment.Production,
            Issuer.Msa,
            new Uri("https://nexus.passport.com/public/partner/discovery/gdpr/key"),
            @"^https:\/\/aadrvs.msidentity.com\/[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$",
            false,
            "9188040d-6c67-4c5b-b112-36a304b66dad");

        /// <summary>
        /// Environment configuration for MSA - preproduction
        /// </summary>
        public static readonly EnvironmentConfiguration MsaPreproduction = new EnvironmentConfiguration(
            PcvEnvironment.Preproduction,
            Issuer.Msa,
            new Uri("https://nexus.passport-int.com/public/partner/discovery/gdpr/key"),
            @"^https:\/\/gdpr.login.live-int.com\/[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$",
            false,
            "4925308c-f164-4d2d-bc7e-0631132e9375");

        /// <summary>
        /// Initializes a new <see cref="EnvironmentConfiguration" />.
        /// </summary>
        /// <param name="environment">environment</param>
        /// <param name="issuer">Verifier Issuer such as AAD or MSA</param>
        /// <param name="tenantId">TenantId for validating MSA Issuer</param>
        /// <param name="keyDiscoveryEndPoint">KeyDiscoveryAPI endpoint</param>
        /// <param name="issuerRegexPattern">The regex pattern with the Uri followed by guid to verify issuer</param>
        /// <param name="isCertificateChainValidationEnabled">Enables public key certificate chain validation</param>
        public EnvironmentConfiguration(
            PcvEnvironment environment,
            Issuer issuer,
            Uri keyDiscoveryEndPoint,
            string issuerRegexPattern,
            bool isCertificateChainValidationEnabled,
            string tenantId = null)
        {
            this.Environment = environment;
            this.Issuer = issuer;
            this.TenantId = string.IsNullOrWhiteSpace(tenantId) ? Guid.Empty : Guid.Parse(tenantId);
            this.KeyDiscoveryEndPoint = keyDiscoveryEndPoint;
            this.IssuerRegexPattern = issuerRegexPattern;
            this.IsCertificateChainValidationEnabled = isCertificateChainValidationEnabled;
        }

        /// <summary>
        /// Environment
        /// </summary>
        public PcvEnvironment Environment { get; }

        /// <summary>
        /// AAD or MSA
        /// </summary>
        public Issuer Issuer { get; }

        /// <summary>
        /// TenantId to match the Issuer for MSA
        /// </summary>
        public Guid TenantId { get; }

        /// <summary>
        /// KeyDiscoveryAPI endpoint
        /// </summary>
        public Uri KeyDiscoveryEndPoint { get; }

        /// <summary>
        /// The regex pattern with the Uri followed by guid to verify issuer
        /// </summary>
        public string IssuerRegexPattern { get; }

        /// <summary>
        /// Enables public key certificate chain validation.
        /// </summary>
        public bool IsCertificateChainValidationEnabled { get; }

        /// <summary>
        /// Returns a validator environment configuration matching the commandCloudInstance.
        /// Throws if no matching environment is found.
        /// </summary>
        /// <param name="subject">PrivacySubject, Aad, Msa or Device</param>
        /// <param name="commandCloudInstance">The public or sovereign cloud instance target for this command</param>
        /// <param name="sovereignCloudConfigurations">KeyDiscoveryConfiguration list provided by the agent</param>
        /// <param name="pcvEnvironment">Production or PPE</param>
        /// <param name="loggableInformation">Information for logging exception</param>
        /// <returns>EnvironmentConfiguration based on the sovereign cloud</returns>
        /// <exception cref="InvalidPrivacyCommandException">If the CloudInstance in the command is invalid or not supported</exception>
        internal static EnvironmentConfiguration GetEnvironmentConfiguration(
            IPrivacySubject subject,
            string commandCloudInstance,
            List<KeyDiscoveryConfiguration> sovereignCloudConfigurations,
            PcvEnvironment pcvEnvironment,
            LoggableInformation loggableInformation)
        {
            if (subject.GetType() != typeof(AadSubject) && subject.GetType() != typeof(AadSubject2))
            {
                return pcvEnvironment == PcvEnvironment.Preproduction ? MsaPreproduction : MsaProduction;
            }

            if (string.IsNullOrWhiteSpace(commandCloudInstance))
            {
                commandCloudInstance = CloudInstance.Public;
            }

            // Find the standard matching configuration
            //    1. Use public PPE if pcvEnvironment is Preproduction since sovereign cloud agents don't have PPE
            //    2. Get the configuration from the KeyDiscoveryConfigurationCollection
            KeyDiscoveryConfiguration configuration;
            if (pcvEnvironment == PcvEnvironment.Preproduction)
            {
                configuration = KeyDiscoveryConfigurationCollection.PublicPpe;
            }
            else if (sovereignCloudConfigurations?.Any() == true)
            {
                // Get the configuration from the agent's custom sovereign cloud key discovery configurations
                configuration = sovereignCloudConfigurations.FirstOrDefault(c => c.CloudInstances.Contains(commandCloudInstance));
            }
            else
            {
                // KeyDiscoveryConfigurations is a Dictionary with an StringComparer.OrdinalIgnoreCase comparer
                KeyDiscoveryConfigurationCollection.KeyDiscoveryConfigurations.TryGetValue(commandCloudInstance, out configuration);
            }

            // Configuration not found, so assemble logging information and throw an exception.
            if (configuration == null)
            {
                var supportedCloudInstances = CloudInstance.All;
                if (sovereignCloudConfigurations != null)
                {
                    supportedCloudInstances = sovereignCloudConfigurations.SelectMany(kd => kd.CloudInstances).ToList();
                }

                throw new InvalidPrivacyCommandException(
                    $"CloudInstance '{commandCloudInstance}' is not in the supported list '{string.Join(",", supportedCloudInstances)}'",
                    loggableInformation);
            }

            return new EnvironmentConfiguration(
                pcvEnvironment,
                Issuer.Aad,
                configuration.KeyDiscoveryEndPoint,
                $"^{configuration.Issuer.AbsoluteUri.Replace("/", @"\/")}$",
                configuration.IsCertificateChainValidationEnabled);
        }
    }
}

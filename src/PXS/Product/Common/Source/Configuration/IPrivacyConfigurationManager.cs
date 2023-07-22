// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.Configuration
{
    using Microsoft.Membership.MemberServices.Configuration;

    /// <summary>
    ///     Interface for Privacy ConfigurationManager
    /// </summary>
    public interface IPrivacyConfigurationManager
    {
        /// <summary>
        ///     Gets the AAD Account close worker configuration
        /// </summary>
        IAadAccountCloseWorkerConfiguration AadAccountCloseWorkerConfiguration { get; }

        /// <summary>
        ///     Gets the AAD auth token generator configuration
        /// </summary>
        IAadTokenAuthConfiguration AadTokenAuthGeneratorConfiguration { get; }

        /// <summary>
        ///     Gets the PXF Adapters configuration.
        /// </summary>
        IAdaptersConfiguration AdaptersConfiguration { get; }

        /// <summary>
        ///     Gets the msa age out fake command worker configuration
        /// </summary>
        IMsaAgeOutFakeCommandWorkerConfiguration MsaAgeOutFakeCommandWorkerConfiguration { get; }

        /// <summary>
        ///     Gets the AQS Worker configuration
        /// </summary>
        IPrivacyAqsWorkerConfiguration AqsWorkerConfiguration { get; }

        /// <summary>
        ///     Gets the AzureKeyVaultConfiguration
        /// </summary>
        IAzureKeyVaultConfiguration AzureKeyVaultConfiguration { get; }

        /// <summary>
        ///     Gets the cosmos export agent configuration
        /// </summary>
        ICosmosExportAgentConfig CosmosExportAgentConfig { get; }

        /// <summary>
        ///     Gets data action runner configuration
        /// </summary>
        IDataActionRunnerConfig DataActionRunnerConfig { get; }

        /// <summary>
        ///     Gets the data management configuration.
        /// </summary>
        IDataManagementConfig DataManagementConfig { get; }

        /// <summary>
        ///     Gets the environment configuration
        /// </summary>
        IEnvironmentConfiguration EnvironmentConfiguration { get; }

        /// <summary>
        ///     Get the export configuration
        /// </summary>
        IExportConfiguration ExportConfiguration { get; }

        /// <summary>
        ///     Gets the mock data management configuration.
        /// </summary>
        IDataManagementConfig MockDataManagementConfig { get; }

        /// <summary>
        ///     Gets the Msa Identity-Service configuration.
        /// </summary>
        IMsaIdentityServiceConfiguration MsaIdentityServiceConfiguration { get; }

        /// <summary>
        ///     Gets the privacy-experience-service configuration.
        /// </summary>
        IPrivacyExperienceServiceConfiguration PrivacyExperienceServiceConfiguration { get; }

        /// <summary>
        ///     Gets the privacy-experience-service WD configuration.
        /// </summary>
        IPrivacyExperienceServiceWDConfiguration PrivacyExperienceServiceWDConfiguration { get; }

        /// <summary>
        ///     Gets the Privacy VSO Worker configuration
        /// </summary>
        IPrivacyVsoWorkerConfiguration PrivacyVsoWorkerConfiguration { get; }

        /// <summary>
        ///     Gets the RPS configuration.
        /// </summary>
        IRpsConfiguration RpsConfiguration { get; }

        /// <summary>
        ///     Gets the Sovereign Cloud PCF receiver configuration
        /// </summary>
        IPcfDataAgentConfiguration PcfDataAgentConfig { get; }

        /// <summary>
        ///     Gets the Sovereign Cloud PCF receiver configuration
        /// </summary>
        IPcfDataAgentConfiguration PcfDataAgentV2Config { get; }

        /// <summary>
        ///     Gets the Vortex Device Delete worker configuration
        /// </summary>
        IVortexDeviceDeleteWorkerConfiguration VortexDeviceDeleteWorkerConfiguration { get; }

        /// <summary>
        ///     Gets the worker watchdog configuration.
        /// </summary>
        IWorkerWatchdogConfiguration WorkerWatchdogConfiguration { get; }

        /// <summary>
        ///     Gets the partner mock configuration.
        /// </summary>
        IPrivacyPartnerMockConfiguration PartnerMockConfiguration { get; }

        /// <summary>
        ///     Gets the Azure App Configuration configuration.
        /// </summary>
        IAzureAppConfigurationSettings AzureAppConfigurationSettings { get; }

        /// <summary>
        ///     Gets the Azure Cache for Redis configuration.
        /// </summary>
        IAzureRedisCacheConfiguration AzureRedisCacheConfiguration { get; }

        /// <summary>
        ///     Gets the Recurrent Delete Worker Configuration
        /// </summary>
        IRecurringDeleteWorkerConfiguration RecurringDeleteWorkerConfiguration { get; }
    }
}

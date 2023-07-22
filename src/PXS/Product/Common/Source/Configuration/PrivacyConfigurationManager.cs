// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.Configuration.Privacy
{
    using System.Diagnostics;

    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.Search.Platform.Parallax;

    /// <summary>
    ///     Privacy ConfigurationManager
    /// </summary>
    public class PrivacyConfigurationManager : IPrivacyConfigurationManager
    {
        /// <summary>
        ///     Loads the current configuration.
        /// </summary>
        public static IPrivacyConfigurationManager LoadCurrentConfiguration(ILogger logger)
        {
            var loader = new PrivacyConfigurationLoader(logger);
            return loader.CurrentConfiguration;
        }

        /// <inheritdoc />
        public IAadAccountCloseWorkerConfiguration AadAccountCloseWorkerConfiguration { get; }

        /// <summary>
        ///     Gets the AAD auth token generator configuration
        /// </summary>
        public IAadTokenAuthConfiguration AadTokenAuthGeneratorConfiguration { get; }

        /// <summary>
        ///     Gets the PXF Adapters configuration.
        /// </summary>
        public IAdaptersConfiguration AdaptersConfiguration { get; }

        /// <summary>
        ///     Gets the AQS configuration.
        /// </summary>
        public IPrivacyAqsWorkerConfiguration AqsWorkerConfiguration { get; }

        /// <summary>
        ///     Gets the AzureKeyVaultConfiguration
        /// </summary>
        public IAzureKeyVaultConfiguration AzureKeyVaultConfiguration { get; }

        /// <summary>
        ///     Gets the cosmos export agent config
        /// </summary>
        public ICosmosExportAgentConfig CosmosExportAgentConfig { get; }

        /// <summary>
        ///     Gets data action runner configuration
        /// </summary>
        public IDataActionRunnerConfig DataActionRunnerConfig { get; }

        /// <summary>
        ///     Gets the data management configuration.
        /// </summary>
        public IDataManagementConfig DataManagementConfig { get; }

        /// <summary>
        ///     Gets the environment configuration.
        /// </summary>
        public IEnvironmentConfiguration EnvironmentConfiguration { get; }

        /// <summary>
        ///     Gets the DocumentDB configuration.
        /// </summary>

        public IExportConfiguration ExportConfiguration { get; }

        /// <summary>
        ///     Gets the mock data management configuration.
        /// </summary>
        public IDataManagementConfig MockDataManagementConfig { get; }

        /// <summary>
        ///     Gets the msa age out fake command worker configuration
        /// </summary>
        public IMsaAgeOutFakeCommandWorkerConfiguration MsaAgeOutFakeCommandWorkerConfiguration { get; }

        /// <summary>
        ///     Gets the Msa Identity-Service configuration.
        /// </summary>
        public IMsaIdentityServiceConfiguration MsaIdentityServiceConfiguration { get; }

        /// <summary>
        ///     Gets the privacy-experience-service configuration.
        /// </summary>
        public IPrivacyExperienceServiceConfiguration PrivacyExperienceServiceConfiguration { get; }

        /// <summary>
        ///     Gets the privacy-experience-service WD configuration.
        /// </summary>
        public IPrivacyExperienceServiceWDConfiguration PrivacyExperienceServiceWDConfiguration { get; }

        /// <summary>
        ///     Gets the Privacy VSO Worker configuration
        /// </summary>
        public IPrivacyVsoWorkerConfiguration PrivacyVsoWorkerConfiguration { get; }

        /// <summary>
        ///     Gets the RPS configuration.
        /// </summary>
        public IRpsConfiguration RpsConfiguration { get; }

        /// <summary>
        ///     Gets the Vortex Device Delete worker configuration
        /// </summary>
        public IVortexDeviceDeleteWorkerConfiguration VortexDeviceDeleteWorkerConfiguration { get; }

        /// <summary>
        ///     Gets the worker watchdog configuration.
        /// </summary>
        public IWorkerWatchdogConfiguration WorkerWatchdogConfiguration { get; }

        /// <summary>
        ///     Gets the PCF data agent configuration
        /// </summary>
        public IPcfDataAgentConfiguration PcfDataAgentConfig { get; }

        /// <summary>
        ///     Gets the PCF data agent configuration
        /// </summary>
        public IPcfDataAgentConfiguration PcfDataAgentV2Config { get; }

        /// <inheritdoc/>
        public IPrivacyPartnerMockConfiguration PartnerMockConfiguration { get; }

        /// <inheritdoc/>
        public IAzureAppConfigurationSettings AzureAppConfigurationSettings { get; }

        /// <inheritdoc/>
        public IAzureRedisCacheConfiguration AzureRedisCacheConfiguration { get; }

        /// <inheritdoc/>
        public IRecurringDeleteWorkerConfiguration RecurringDeleteWorkerConfiguration { get;  }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PrivacyConfigurationManager" /> class.
        /// </summary>
        /// <param name="configSnapshot">Snapshot of the configuration</param>
        public PrivacyConfigurationManager(VariantObjectStore configSnapshot)
        {
            this.AdaptersConfiguration = GetConfiguration<IAdaptersConfiguration>(configSnapshot, "Adapters.ini.flattened.ini");
            this.DataManagementConfig = GetConfiguration<IDataManagementConfig>(configSnapshot, "PxfAdapters.ini.flattened.ini");
            this.MockDataManagementConfig = GetConfiguration<IDataManagementConfig>(configSnapshot, "MockPxfAdapters.ini.flattened.ini");
            this.MsaIdentityServiceConfiguration = GetConfiguration<IMsaIdentityServiceConfiguration>(
                configSnapshot,
                "MsaIdentityService.ini.flattened.ini");

            this.PrivacyExperienceServiceConfiguration = GetConfiguration<IPrivacyExperienceServiceConfiguration>(
                configSnapshot,
                "PrivacyExperienceService.ini.flattened.ini");

            this.PrivacyExperienceServiceWDConfiguration = GetConfiguration<IPrivacyExperienceServiceWDConfiguration>(
                configSnapshot,
                "PrivacyExperienceServiceWD.ini.flattened.ini");

            this.RpsConfiguration = GetConfiguration<IRpsConfiguration>(configSnapshot, "Rps.ini.flattened.ini");
            this.ExportConfiguration = GetConfiguration<IExportConfiguration>(configSnapshot, "Export.ini.flattened.ini");
            this.AqsWorkerConfiguration = GetConfiguration<IPrivacyAqsWorkerConfiguration>(configSnapshot, "PrivacyAqsWorker.ini.flattened.ini");
            this.AadAccountCloseWorkerConfiguration = GetConfiguration<IAadAccountCloseWorkerConfiguration>(configSnapshot, "AadAccountCloseWorker.ini.flattened.ini");
            this.VortexDeviceDeleteWorkerConfiguration = GetConfiguration<IVortexDeviceDeleteWorkerConfiguration>(configSnapshot, "VortexDeviceDeleteWorker.ini.flattened.ini");
            this.PrivacyVsoWorkerConfiguration = GetConfiguration<IPrivacyVsoWorkerConfiguration>(configSnapshot, "PrivacyVsoWorker.ini.flattened.ini");
            this.WorkerWatchdogConfiguration = GetConfiguration<IWorkerWatchdogConfiguration>(configSnapshot, "WorkerWatchdogConfiguration.ini.flattened.ini");
            this.PcfDataAgentConfig = GetConfiguration<IPcfDataAgentConfiguration>(configSnapshot, "PcfDataAgent.ini.flattened.ini");
            this.PcfDataAgentV2Config = GetConfiguration<IPcfDataAgentConfiguration>(configSnapshot, "PcfDataAgentV2.ini.flattened.ini");

            this.AadTokenAuthGeneratorConfiguration = GetConfiguration<IAadTokenAuthConfiguration>(
                configSnapshot,
                "AadIdentityService.ini.flattened.ini");

            this.CosmosExportAgentConfig = GetConfiguration<ICosmosExportAgentConfig>(
                configSnapshot,
                "CosmosExportAgent.ini.flattened.ini");

            this.DataActionRunnerConfig = GetConfiguration<IDataActionRunnerConfig>(
                configSnapshot,
                "DataActionRunner.ini.flattened.ini");

            this.AzureKeyVaultConfiguration = GetConfiguration<IAzureKeyVaultConfiguration>(configSnapshot, "GlobalConfiguration.ini.flattened.ini");

            this.EnvironmentConfiguration = GetConfiguration<IEnvironmentConfiguration>(configSnapshot, "GlobalConfiguration.ini.flattened.ini");
            this.MsaAgeOutFakeCommandWorkerConfiguration = GetConfiguration<IMsaAgeOutFakeCommandWorkerConfiguration>(configSnapshot, "MsaAgeOutFakeCommandWorker.ini.flattened.ini");

            this.PartnerMockConfiguration = GetConfiguration<IPrivacyPartnerMockConfiguration>(configSnapshot, "PrivacyPartnerMockConfiguration.ini.flattened.ini");

            this.AzureAppConfigurationSettings = GetConfiguration<IAzureAppConfigurationSettings>(configSnapshot, "GlobalConfiguration.ini.flattened.ini");
            this.AzureRedisCacheConfiguration = GetConfiguration<IAzureRedisCacheConfiguration>(configSnapshot, "GlobalConfiguration.ini.flattened.ini");

            this.RecurringDeleteWorkerConfiguration = GetConfiguration<IRecurringDeleteWorkerConfiguration>(configSnapshot, "RecurrentDeleteWorker.ini.flattened.ini");
        }

        /// <summary>
        ///     Gets a configuration instance of the required type from the provided configuration snapshot.
        /// </summary>
        /// <typeparam name="T">The configuration type that needs to be retrieved.</typeparam>
        /// <param name="configSnapshot">The configuration snapshot.</param>
        /// <param name="configFile">The name of the configuration file this configuration resides in.</param>
        /// <returns>The required configuration instance.</returns>
        private static T GetConfiguration<T>(VariantObjectStore configSnapshot, string configFile)
            where T : class
        {
            if (configSnapshot.DataSourceNames.Contains(configFile))
            {
                VariantObjectProvider objectProvider = configSnapshot.GetResolvedObjectProvider(configFile);
                return objectProvider.GetSingletonObject<T>();
            }

            Trace.TraceWarning("Configuration snapshot does not contain '{0}'. Not loading configuration.", configFile);
            return null;
        }
    }
}

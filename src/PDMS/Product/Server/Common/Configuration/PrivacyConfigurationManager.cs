namespace Microsoft.PrivacyServices.DataManagement.Common.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.DataManagement.Client;
    using Microsoft.Search.Platform.Parallax;

    /// <summary>
    ///     Privacy ConfigurationManager
    /// </summary>
    public class PrivacyConfigurationManager : IPrivacyConfigurationManager
    {
        /// <summary>
        ///     Loads the current configuration.
        /// </summary>
        public static IPrivacyConfigurationManager LoadCurrentConfiguration()
        {
            var loader = new PrivacyConfigurationLoader();
            return loader.CurrentConfiguration;
        }

        /// <inheritdoc />
        public IAzureActiveDirectoryProviderConfig AzureActiveDirectoryProviderConfig { get; }

        /// <inheritdoc />
        public ITokenProviderConfig TokenProviderConfig { get; }

        /// <inheritdoc />
        public IServicePointManagerConfig ServicePointManagerConfig { get; }

        /// <inheritdoc />
        public IDocumentDatabaseConfig DocumentDatabaseConfig { get; private set; }

        /// <inheritdoc />
        public IAzureKeyVaultConfig AzureKeyVaultConfig { get; }

        /// <inheritdoc />
        public ISllConfig SllConfig { get; }

        /// <inheritdoc />
        public IKustoClientConfig KustoClientConfig { get; }

        /// <inheritdoc />
        public IClientConfiguration ServiceTreeClientConfig { get; }

        /// <inheritdoc />
        public IThrottlingConfiguration ThrottlingConfiguration { get; }

        /// <inheritdoc />
        public IOwinConfiguration OwinConfiguration { get; }

        /// <inheritdoc />
        public ICoreConfiguration CoreConfiguration { get; }

        /// <inheritdoc />
        public IAzureAppConfigurationSettings AppConfigSettings { get; }

        /// <inheritdoc />
        public IDataGridConfiguration DataGridConfiguration { get; }

        /// <inheritdoc />
        public IIcmConfiguration IcmConfiguration { get; }

        
        /// <inheritdoc />
        public IServiceTreeKustoConfiguration ServiceTreeKustoConfiguration { get; }

        /// <inheritdoc />
        public IDataAccessConfiguration DataAccessConfiguration { get; }

        /// <inheritdoc />
        public ILockConfig LockConfig { get; }

        /// <inheritdoc />
        public ICloudStorageConfig CloudStorageConfig { get; private set; }

        /// <inheritdoc />
        public ICloudQueueConfig CloudQueueConfig { get; private set; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PrivacyConfigurationManager" /> class.
        /// </summary>
        /// <param name="configSnapshot">Snapshot of the configuration</param>
        public PrivacyConfigurationManager(VariantObjectStore configSnapshot)
        {
            this.AzureActiveDirectoryProviderConfig = GetConfiguration<IAzureActiveDirectoryProviderConfig>(configSnapshot, "AadAuthentication.ini.flattened.ini");
            this.TokenProviderConfig = GetConfiguration<ITokenProviderConfig>(configSnapshot, "AadAuthentication.ini.flattened.ini");
            this.ServicePointManagerConfig = GetConfiguration<IServicePointManagerConfig>(configSnapshot, "ServicePointManager.ini.flattened.ini");
            this.DocumentDatabaseConfig = GetConfiguration<IDocumentDatabaseConfig>(configSnapshot, "DocumentDb.ini.flattened.ini");
            this.AzureKeyVaultConfig = GetConfiguration<IAzureKeyVaultConfig>(configSnapshot, "AzureKeyVaultConfig.ini.flattened.ini");
            this.SllConfig = GetConfiguration<ISllConfig>(configSnapshot, "Sll.ini.flattened.ini");
            this.KustoClientConfig = GetConfiguration<IKustoClientConfig>(configSnapshot, "KustoClient.ini.flattened.ini");
            this.ServiceTreeClientConfig = GetConfiguration<IClientConfiguration>(configSnapshot, "ServiceTree.ini.flattened.ini");
            this.ThrottlingConfiguration = GetConfiguration<IThrottlingConfiguration>(configSnapshot, "WebApi.ini.flattened.ini");
            this.OwinConfiguration = GetConfiguration<IOwinConfiguration>(configSnapshot, "Owin.ini.flattened.ini");
            this.CoreConfiguration = GetConfiguration<ICoreConfiguration>(configSnapshot, "CoreConfig.ini.flattened.ini");
            this.AppConfigSettings = GetConfiguration<IAzureAppConfigurationSettings>(configSnapshot, "CoreConfig.ini.flattened.ini");
            this.DataGridConfiguration = GetConfiguration<IDataGridConfiguration>(configSnapshot, "CoreConfig.ini.flattened.ini");
            this.IcmConfiguration = GetConfiguration<IIcmConfiguration>(configSnapshot, "CoreConfig.ini.flattened.ini");
            this.ServiceTreeKustoConfiguration = GetConfiguration<IServiceTreeKustoConfiguration>(configSnapshot, "CoreConfig.ini.flattened.ini");
            this.DataAccessConfiguration = GetConfiguration<IDataAccessConfiguration>(configSnapshot, "DataAccess.ini.flattened.ini");
            this.LockConfig = GetConfiguration<ILockConfig>(configSnapshot, "LockConfig.ini.flattened.ini");
            this.CloudStorageConfig = GetConfiguration<ICloudStorageConfig>(configSnapshot, "CloudStorageConfig.ini.flattened.ini");
            this.CloudQueueConfig = GetConfiguration<ICloudQueueConfig>(configSnapshot, "CloudStorageConfig.ini.flattened.ini");
        }

        /// <summary>
        /// Gets a configuration instance of the required type from the provided configuration snapshot.
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

            DualLogger.Instance.Warning(nameof(PrivacyConfigurationManager), $"Configuration snapshot does not contain '{configFile}'. Not loading configuration.");
            return null;
        }
    }
}

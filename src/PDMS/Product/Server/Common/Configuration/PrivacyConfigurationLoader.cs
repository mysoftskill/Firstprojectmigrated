namespace Microsoft.PrivacyServices.DataManagement.Common.Configuration
{
    using System;
    using System.IO;
    using System.Linq;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.Search.Platform.Parallax;
    using Microsoft.Search.Platform.Parallax.DataLoad;

    /// <summary>
    /// Privacy ConfigurationLoader
    /// </summary>
    public class PrivacyConfigurationLoader : IPrivacyConfigurationLoader
    {
        private const string ComponentName = nameof(PrivacyConfigurationLoader);

        /// <summary>
        /// The configuration store container
        /// </summary>
        private readonly VariantObjectStoreContainer configStoreContainer;

        /// <summary>
        /// The current configuration manager
        /// </summary>
        private PrivacyConfigurationManager currentConfigurationManager;

        /// <summary>
        /// This event is fired off when a new version of the Configuration is available.
        /// </summary>
        public event EventHandler<PrivacyConfigurationUpdateEventArgs> ConfigurationUpdate;

        /// <summary>
        /// Gets the latest version of the current configuration
        /// </summary>
        public IPrivacyConfigurationManager CurrentConfiguration => this.currentConfigurationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrivacyConfigurationLoader"/> class.
        /// </summary>
        public PrivacyConfigurationLoader()
        {
            this.configStoreContainer = VariantObjectStoreContainerFactory.Default.Create();

            // Load configs from disk. Avoid loading any Autopilot specific configs since these are used by other modules
            string[] omittedConfigFiles =
            {
                ".\\APLoggingConfig.ini.flattened.ini",
                ".\\autopilot.ini.flattened.ini",
                ".\\config.ini.flattened.ini",
                ".\\ServiceConfig.ini.flattened.ini"
            };

            foreach (string configFile in Directory.EnumerateFiles(".", "*.ini.flattened.ini"))
            {
                bool omitThisFile = omittedConfigFiles
                    .Any(
                        omittedConfigFile =>
                            string.Equals(omittedConfigFile, configFile, StringComparison.OrdinalIgnoreCase));

                if (omitThisFile)
                {
                    DualLogger.Instance.Information(ComponentName, "Not loading omitted config file: '{0}'", configFile);
                    continue;
                }

                DualLogger.Instance.Information(ComponentName, "Loading config to container from file: '{0}'", configFile);
                this.configStoreContainer.Load(configFile, SourceFileChangeBehavior.ReloadFileAfterChange);
            }

            this.currentConfigurationManager = new PrivacyConfigurationManager(this.configStoreContainer.GetCurrentSnapshot());
            this.configStoreContainer.DataLoadCommitted += this.ParallaxReloaded;
        }

        private void ParallaxReloaded(object sender, TransactionCommittedEventArgs e)
        {
            DualLogger.Instance.Information(nameof(PrivacyConfigurationLoader), $"ParallaxReloaded started.");

            this.currentConfigurationManager = new PrivacyConfigurationManager(this.configStoreContainer.GetCurrentSnapshot());
            this.ConfigurationUpdate?.Invoke(this, new PrivacyConfigurationUpdateEventArgs(this.currentConfigurationManager));

            DualLogger.Instance.Information(nameof(PrivacyConfigurationLoader), $"ParallaxReloaded finished.");
        }
    }
}
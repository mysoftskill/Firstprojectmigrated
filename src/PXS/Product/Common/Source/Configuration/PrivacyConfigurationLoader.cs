// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.Configuration.Privacy
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
        /// The logger
        /// </summary>
        private ILogger logger;

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
        public PrivacyConfigurationLoader(ILogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.configStoreContainer = VariantObjectStoreContainerFactory.Default.Create();

            foreach (string configFile in Directory.EnumerateFiles(".", "*.ini.flattened.ini"))
            {
                logger.Information(ComponentName, "Loading config to container from file: '{0}'", configFile);
                this.configStoreContainer.Load(configFile, SourceFileChangeBehavior.ReloadFileAfterChange);
            }

            this.currentConfigurationManager = new PrivacyConfigurationManager(this.configStoreContainer.GetCurrentSnapshot());
            this.configStoreContainer.DataLoadCommitted += this.ParallaxReloaded;
        }

        private void ParallaxReloaded(object sender, TransactionCommittedEventArgs e)
        {
            this.logger.Information(ComponentName, "ParallaxReloaded started.");

            this.currentConfigurationManager = new PrivacyConfigurationManager(this.configStoreContainer.GetCurrentSnapshot());
            this.ConfigurationUpdate?.Invoke(this, new PrivacyConfigurationUpdateEventArgs(this.currentConfigurationManager));

            this.logger.Information(ComponentName, "ParallaxReloaded finished.");
        }
    }
}
// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.Configuration.SovereignCloud
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    using Microsoft.Membership.MemberServices.Common.Configuration.Privacy;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.Search.Platform.Parallax;
    using Microsoft.Search.Platform.Parallax.DataLoad;

    /// <summary>
    ///     Loads conifiguration for the PXS Proxy
    /// </summary>
    public class PxsProxyConfigurationLoader : IPrivacyConfigurationLoader
    {
        private static readonly IList<string> preflattenedConfigurationFiles = new List<string>
        {
            "MsaIdentityService.ini.flattened.ini",
            "Adapters.ini.flattened.ini",
            "PrivacyExperienceService.ini.flattened.ini",
            "AadIdentityService.ini.flattened.ini",
            "AadAccountCloseWorker.ini.flattened.ini",
        };

        /// <summary>
        ///     The configuration store container
        /// </summary>
        private readonly VariantObjectStoreContainer configStoreContainer;

        private readonly ILogger logger;

        /// <summary>
        ///     The current configuration manager
        /// </summary>
        private PrivacyConfigurationManager currentConfigurationManager;

        /// <summary>
        ///     Gets the latest version of the current configuration
        /// </summary>
        public IPrivacyConfigurationManager CurrentConfiguration => this.currentConfigurationManager;

        /// <summary>
        ///     Creates a new instance
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="cloudInstance">The cloud instance name</param>
        public PxsProxyConfigurationLoader(ILogger logger, CloudInstanceType cloudInstance)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Assembly executingAssembly = Assembly.GetExecutingAssembly();

            string configurationFolderName = this.MapCloudInstanceToConfigurationFolder(cloudInstance);

            string configurationPrefix = $"{executingAssembly.GetName().Name}.Configuration.SovereignCloud.{configurationFolderName}";
            this.configStoreContainer = VariantObjectStoreContainerFactory.Default.Create();

            // Load config files by cloud instance name
            foreach (string configFile in preflattenedConfigurationFiles)
            {
                string fullConfigPath = $"{configurationPrefix}.{configFile}";
                logger.Information(nameof(PxsProxyConfigurationLoader), "Loading config to container from file: '{0}'", fullConfigPath);

                // Parallax loads from disk, so copy embedded resource to disk.
                using (Stream stream = executingAssembly.GetManifestResourceStream(fullConfigPath))
                using (FileStream fileStream = File.Create(configFile))
                {
                    if (stream == null)
                    {
                        throw new InvalidOperationException($"Configuration file does not exist as embedded resource: {fullConfigPath}");
                    }

                    stream.Seek(0, SeekOrigin.Begin);
                    stream.CopyTo(fileStream);
                }

                this.configStoreContainer.Load($".\\{configFile}", SourceFileChangeBehavior.IgnoreFileChange);
            }

            this.currentConfigurationManager = new PrivacyConfigurationManager(this.configStoreContainer.GetCurrentSnapshot());
            this.configStoreContainer.DataLoadCommitted += this.ParallaxReloaded;
        }

        public event EventHandler<PrivacyConfigurationUpdateEventArgs> ConfigurationUpdate;

        /// <summary>
        ///     Maps the cloud to the folder name the config is located within. This is just here to decouple exact name matches of enum to folder.
        /// </summary>
        /// <param name="cloudInstance">the cloud instance</param>
        /// <returns>The folder the config is located in.</returns>
        private string MapCloudInstanceToConfigurationFolder(CloudInstanceType cloudInstance)
        {
            switch (cloudInstance)
            {
                case CloudInstanceType.INT:
                    return "INT";
                case CloudInstanceType.AzurePPE:
                    return "PublicPPE";
                case CloudInstanceType.CNAzureMooncake:
                case CloudInstanceType.CNO365Gallatin:
                    return "Mooncake";
                case CloudInstanceType.USAzureFairfax:
                    return "Fairfax";
                default:
                    throw new ArgumentOutOfRangeException(nameof(cloudInstance), cloudInstance, $"Invalid cloud instance type: {cloudInstance}");
            }
        }

        private void ParallaxReloaded(object sender, TransactionCommittedEventArgs e)
        {
            this.logger.Information(nameof(PxsProxyConfigurationLoader), "ParallaxReloaded started.");

            this.currentConfigurationManager = new PrivacyConfigurationManager(this.configStoreContainer.GetCurrentSnapshot());
            this.ConfigurationUpdate?.Invoke(this, new PrivacyConfigurationUpdateEventArgs(this.currentConfigurationManager));

            this.logger.Information(nameof(PxsProxyConfigurationLoader), "ParallaxReloaded finished.");
        }
    }
}

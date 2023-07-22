// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.DataManagementConfig
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.DataManagementConfig;
    using Microsoft.PrivacyServices.Policy;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     PrivacyDataManagementConfigLoader
    /// </summary>
    public class DataManagementConfigLoader : IDataManagementConfigLoader
    {
        private static readonly DataManagementConfig currentDataManagementConfig = new DataManagementConfig();

        private static AdapterConfigurationSource adapterConfigurationSource;

        private readonly ILogger logger;

        private readonly IPrivacyConfigurationManager serviceConfiguration;

        /// <summary>
        ///     Gets the latest data-management configuration.
        /// </summary>
        public IDataManagementConfig CurrentDataManagementConfig => currentDataManagementConfig;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DataManagementConfigLoader" /> class.
        /// </summary>
        /// <param name="serviceConfiguration">The service configuration loader.</param>
        /// <param name="logger">The logger.</param>
        public DataManagementConfigLoader(
            IPrivacyConfigurationManager serviceConfiguration,
            ILogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.serviceConfiguration = serviceConfiguration ?? throw new ArgumentNullException(nameof(serviceConfiguration));

            if (serviceConfiguration.PrivacyExperienceServiceConfiguration == null)
                throw new ArgumentNullException(nameof(serviceConfiguration.PrivacyExperienceServiceConfiguration));

            // the source determines where the config is loaded from.
            adapterConfigurationSource = this.serviceConfiguration.PrivacyExperienceServiceConfiguration.AdapterConfigurationSource;
        }

        /// <summary>
        ///     Loads the configuration.
        /// </summary>
        public void Load()
        {
            switch (adapterConfigurationSource)
            {
                case AdapterConfigurationSource.MockConfiguration:
                    this.logger.Information(nameof(DataManagementConfigLoader), "Loading configuration file from ini mock configuration file.");
                    currentDataManagementConfig.RingPartnerConfigMapping = this.serviceConfiguration.MockDataManagementConfig?.RingPartnerConfigMapping;
                    this.DataManagementConfigurationUpdate?.Invoke(this, new DataManagementConfigUpdateEventArgs(currentDataManagementConfig));
                    break;

                case AdapterConfigurationSource.ConfigurationIniFile:
                    this.logger.Information(nameof(DataManagementConfigLoader), "Loading configuration file from ini configuration file.");
                    currentDataManagementConfig.RingPartnerConfigMapping = this.serviceConfiguration.DataManagementConfig?.RingPartnerConfigMapping;
                    this.DataManagementConfigurationUpdate?.Invoke(this, new DataManagementConfigUpdateEventArgs(currentDataManagementConfig));
                    break;

                default:
                    this.logger.Error(nameof(DataManagementConfigLoader), $"Invalid {nameof(AdapterConfigurationSource)} specified: {adapterConfigurationSource}");
                    throw new ArgumentOutOfRangeException(nameof(adapterConfigurationSource));
            }
        }

        /// <summary>
        ///     This event is fired off when a new version of the PDMS Configuration is available (the PDMS polling API found a new version).
        /// </summary>
        public event EventHandler<DataManagementConfigUpdateEventArgs> DataManagementConfigurationUpdate;
    }
}

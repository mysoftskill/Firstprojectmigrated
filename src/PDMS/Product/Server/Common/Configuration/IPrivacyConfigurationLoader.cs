namespace Microsoft.PrivacyServices.DataManagement.Common.Configuration
{
    using System;

    /// <summary>
    /// Interface for Configuration Loader.
    /// </summary>
    public interface IPrivacyConfigurationLoader
    {
        /// <summary>
        /// This event is fired off when a new version of the Configuration is available.
        /// </summary>
        event EventHandler<PrivacyConfigurationUpdateEventArgs> ConfigurationUpdate;

        /// <summary>
        /// Gets the latest version of the current configuration
        /// </summary>
        IPrivacyConfigurationManager CurrentConfiguration { get; }
    }
}

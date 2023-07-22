//--------------------------------------------------------------------------------
// <copyright file="IPrivacyConfigurationLoader.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Common.Configuration.Privacy
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

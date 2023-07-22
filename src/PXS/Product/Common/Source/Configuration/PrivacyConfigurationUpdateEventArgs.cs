//--------------------------------------------------------------------------------
// <copyright file="ConfigurationUpdateEventArgs.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Common.Configuration.Privacy
{
    using System;

    /// <summary>
    /// Configuration-Update event arguments.
    /// </summary>
    public class PrivacyConfigurationUpdateEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PrivacyConfigurationUpdateEventArgs"/> class.
        /// </summary>
        /// <param name="configurationManager">The configuration manager instance.</param>
        public PrivacyConfigurationUpdateEventArgs(IPrivacyConfigurationManager configurationManager)
        {
            this.ConfigurationManager = configurationManager;
        }

        /// <summary>
        /// Gets the configuration manager.
        /// </summary>
        public IPrivacyConfigurationManager ConfigurationManager { get; private set; }
    }
}

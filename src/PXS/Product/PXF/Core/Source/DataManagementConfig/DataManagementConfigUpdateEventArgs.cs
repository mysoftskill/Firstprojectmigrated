// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.DataManagementConfig
{
    using System;
    using Microsoft.Membership.MemberServices.Configuration;

    /// <summary>
    /// Data-Management Config-Update EventArgs
    /// </summary>
    public class DataManagementConfigUpdateEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataManagementConfigUpdateEventArgs"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public DataManagementConfigUpdateEventArgs(IDataManagementConfig configuration)
        {
            this.Config = configuration;
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        public IDataManagementConfig Config { get; }
    }
}
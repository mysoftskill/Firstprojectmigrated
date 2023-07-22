// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.DataManagementConfig
{
    using System;
    using Microsoft.Membership.MemberServices.Configuration;

    public interface IDataManagementConfigLoader
    {
        /// <summary>
        /// This event is fired off when a new version of the Configuration is available (the PDMS polling API found a new version).
        /// </summary>
        event EventHandler<DataManagementConfigUpdateEventArgs> DataManagementConfigurationUpdate;

        /// <summary>
        /// Loads the configuration.
        /// </summary>
        void Load();

        /// <summary>
        /// Gets the latest data-management configuration manager.
        /// </summary>
        IDataManagementConfig CurrentDataManagementConfig { get; }
    }
}
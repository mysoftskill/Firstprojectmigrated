// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.DataManagementAdapter
{
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication;
    using Microsoft.PrivacyServices.DataManagement.Client.V2;

    /// <summary>
    /// IDataManagementClientFactory
    /// </summary>
    public interface IDataManagementClientFactory
    {
        /// <summary>
        /// Creates the <see cref="DataManagementClient"/>
        /// </summary>
        /// <param name="certProvider">The cert provider.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="counterFactory">The counter factory.</param>
        /// <returns>DataManagementClient</returns>
        IDataManagementClient Create(
            ICertificateProvider certProvider,
            IPrivacyConfigurationManager configurationManager,
            ICounterFactory counterFactory);

        /// <summary>
        ///     Creates the <see cref="DataManagementClient" />.
        /// </summary>
        /// <param name="aadAuthManager">AAD auth manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="counterFactory">The counter factory.</param>
        /// <returns>DataManagementClient</returns>
        IDataManagementClient Create(
            IAadAuthManager aadAuthManager,
            IPrivacyConfigurationManager configurationManager,
            ICounterFactory counterFactory);
    }
}
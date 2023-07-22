// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.CustomerMasterAdapter
{
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Interface for CustomerMasterAdapterFactory
    /// </summary>
    public interface ICustomerMasterAdapterFactory
    {
        /// <summary>
        /// Creates a <see cref="CustomerMasterAdapter"/> based on configuration settings
        /// </summary>
        /// <param name="certProvider">Certificate Provider</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="counterFactory">The counter factory.</param>
        CustomerMasterAdapter Create(
            ICertificateProvider certProvider,
            IPrivacyConfigurationManager configurationManager,
            ILogger logger,
            ICounterFactory counterFactory);
    }
}
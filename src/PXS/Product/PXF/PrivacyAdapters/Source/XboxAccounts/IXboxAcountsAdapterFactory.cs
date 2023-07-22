// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts
{
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     Public interface for IXboxAcountsAdapterFactory
    /// </summary>
    public interface IXboxAcountsAdapterFactory
    {
        /// <summary>
        ///     Creates a <see cref="XboxAccountsAdapter" /> based on configuration settings
        /// </summary>
        /// <param name="certProvider">Certificate Provider</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="counterFactory">The counter factory.</param>
        /// <param name="clock">The clock.</param>
        XboxAccountsAdapter Create(
            ICertificateProvider certProvider,
            IPrivacyConfigurationManager configurationManager,
            ILogger logger,
            ICounterFactory counterFactory, 
            IClock clock);
    }
}

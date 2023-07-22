// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Clients.Implementations
{
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Clients.Interfaces;

    /// <summary>
    ///     Default Service Point Manager Config
    /// </summary>
    public class DefaultServicePointManagerConfig : IServicePointManagerConfig
    {
        /// <summary>
        ///     Gets the number of milliseconds after which an active connection to PXS is closed
        /// </summary>
        public int ConnectionLeaseTimeout => 25000;

        /// <summary>
        ///     Gets the value that determines if 100-Continue behavior is expected from PXS
        /// </summary>
        public bool Expect100Continue => false;

        /// <summary>
        ///     Maximum length of time that the ServicePoint instance is allowed to maintain an idle connection to an Internet
        ///     resource before it is recycled for use in another connection.
        /// </summary>
        public int MaxIdleTime => 60000;

        /// <summary>
        ///     Enable or disable nagle algorithm
        /// </summary>
        public bool UseNagleAlgorithm => false;
    }
}

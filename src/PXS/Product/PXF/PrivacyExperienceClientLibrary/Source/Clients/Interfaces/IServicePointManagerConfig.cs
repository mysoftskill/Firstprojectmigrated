// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Clients.Interfaces
{
    /// <summary>
    ///     Refer https://docs.microsoft.com/en-us/dotnet/api/system.net.servicepointmanager for property details
    /// </summary>
    public interface IServicePointManagerConfig
    {
        /// <summary>
        ///     Gets the number of milliseconds after which an active connection to PXS is closed
        /// </summary>
        int ConnectionLeaseTimeout { get; }

        /// <summary>
        ///     Gets the value that determines if 100-Continue behavior is expected from PXS
        /// </summary>
        bool Expect100Continue { get; }

        /// <summary>
        ///     Gets the amount of time a connection to PXS can remain idle before the connection is closed
        /// </summary>
        int MaxIdleTime { get; }

        /// <summary>
        ///     Enable or disable nagle algorithm
        /// </summary>
        bool UseNagleAlgorithm { get; }
    }
}

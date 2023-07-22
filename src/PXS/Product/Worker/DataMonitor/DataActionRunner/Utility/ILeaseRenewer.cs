// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.AgentWatcher.Utility
{
    using System.Threading.Tasks;

    /// <summary>
    ///     contract for classes that can renew leases
    /// </summary>
    public interface ILeaseRenewer
    {
        /// <summary>
        ///      Gets the count of renewals performed
        /// </summary>
        long Renewals { get; }

        /// <summary>
        ///      Renews leases that this class manages
        /// </summary>
        /// <returns>resulting value</returns>
        Task RenewAsync();
    }
}

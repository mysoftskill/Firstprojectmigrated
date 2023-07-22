// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Lease
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
        ///     Renews leases that this class manages
        /// </summary>
        /// <param name="force">
        ///     true to force the renewal even if the time since the last renewal hasn't yet expired
        ///     false to only renew the lease if the time since the last renewal hasn't expired
        /// </param>
        /// <returns>resulting value</returns>
        Task RenewAsync(bool force);

        /// <summary>
        ///      Renews leases that this class manages
        /// </summary>
        /// <returns>resulting value</returns>
        Task RenewAsync();
    }
}

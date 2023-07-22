// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Locks
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    ///     contract for objects providing an expirable lock manager
    /// </summary>
    public interface ILockLease
    {
        /// <summary>
        ///     Renews the lock lease
        /// </summary>
        /// <returns>duration to renew the lease for; if null, the original duration is used</returns>
        /// <returns>true if the lock could be renewed; false otherwise</returns>
        Task<bool> RenewAsync(TimeSpan? duration);

        /// <summary>
        ///     Releases the lock lease
        /// </summary>
        /// <param name="purgeLock">true to remove the lock structure entirely (if still owned); false to just release it</param>
        /// <returns>resulting value</returns>
        Task ReleaseAsync(bool purgeLock);
    }
}

// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Locks
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    ///     contract for objects providing expirable lock mechanisms
    /// </summary>
    public interface ILockManager
    {
        /// <summary>
        ///     attempts to acquire the specified lock for an owner
        /// </summary>
        /// <param name="lockGroup">lock partition</param>
        /// <param name="lockName">lock name</param>
        /// <param name="ownerTag">owner tag</param>
        /// <param name="duration">duration</param>
        /// <param name="assumeExists">true if the lock is expected to exist; false if not</param>
        /// <returns>a lease object that can be used to renew or release the lock</returns>
        /// <remarks>
        ///     assumeExists is used to optimize how the lock manager attempts to acquire the lock (e.g. if the lock is implemented
        ///      via a table, this controls whether the first action is to attempt to insert a new row or fetch an existing row)
        /// </remarks>
        Task<ILockLease> AttemptAcquireAsync(
            string lockGroup,
            string lockName,
            string ownerTag,
            TimeSpan duration,
            bool assumeExists);

        /// <summary>
        ///     attempts to acquire the specified lock for an owner
        /// </summary>
        /// <param name="lockGroup">lock partition</param>
        /// <param name="lockName">lock name</param>
        /// <param name="ownerTag">owner tag</param>
        /// <param name="duration">duration</param>
        /// <returns>a lease object that can be used to renew or release the lock</returns>
        Task<ILockLease> AttemptAcquireAsync(
            string lockGroup,
            string lockName,
            string ownerTag,
            TimeSpan duration);
    }

    /// <summary>
    ///     contract for objects providing expirable lock mechanisms
    /// </summary>
    internal interface ILockManagerInternal
    {
        /// <summary>
        ///     attempts to acquire the specified lock for an owner
        /// </summary>
        /// <param name="lockGroup">lock partition</param>
        /// <param name="lockName">lock name</param>
        /// <param name="ownerTag">owner tag</param>
        /// <param name="duration">duration</param>
        /// <param name="assumeExists">true if the lock is expected to exist; false if not</param>
        /// <returns>a lease object that can be used to renew or release the lock</returns>
        /// <remarks>
        ///     assumeExists is used to optimize how the lock manager attempts to acquire the lock (e.g. if the lock is implemented
        ///      via a table, this controls whether the first action is to attempt to insert a new row or fetch an existing row)
        /// </remarks>
        Task<ILockLease> AttemptAcquireAsync(
            string lockGroup,
            string lockName,
            string ownerTag,
            TimeSpan duration,
            bool assumeExists);

        /// <summary>
        ///     attempts to acquire the specified lock for an owner
        /// </summary>
        /// <param name="lockGroup">lock partition</param>
        /// <param name="lockName">lock name</param>
        /// <param name="ownerTag">owner tag</param>
        /// <param name="duration">duration</param>
        /// <returns>a lease object that can be used to renew or release the lock</returns>
        Task<ILockLease> AttemptAcquireAsync(
            string lockGroup,
            string lockName,
            string ownerTag,
            TimeSpan duration);
    }

}

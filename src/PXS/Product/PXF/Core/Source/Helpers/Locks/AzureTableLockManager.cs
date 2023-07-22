// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Locks
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Tables;
    using Microsoft.Azure.Cosmos.Table;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     implements a lock manager using Azure tables
    /// </summary>
    public sealed class AzureTableLockManager : ILockManager
    {
        public static readonly DateTime UnownedExpiryTime = new DateTime(2000, 01, 01, 00, 00, 00, DateTimeKind.Utc);

        private readonly IAzureStorageProvider storage;

        private readonly ILogger logger;

        private readonly IClock clock;

        private readonly string name;

        private ICloudTable table;

        /// <summary>
        ///     update mode when attempting to update the table
        /// </summary>
        private enum UpdateMode
        {
            NewAcquire,

            Renew,

            Release,

            ReleaseAndPurge
        }

        /// <summary>
        ///     Initializes a new instance of the AzureTableLockManager class
        /// </summary>
        /// <param name="storage">storage manager</param>
        /// <param name="logger">Geneva trace logger</param>
        /// <param name="clock">time provider</param>
        /// <param name="name">lock table name</param>
        public AzureTableLockManager(
            IAzureStorageProvider storage,
            ILogger logger,
            IClock clock,
            string name)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));

            this.name = string.IsNullOrWhiteSpace(name) ? typeof(AzureTableLockEntry).Name : name;
        }

        /// <summary>
        ///     attempts to acquire the specified lock for an owner
        /// </summary>
        /// <param name="lockGroup">lock partition</param>
        /// <param name="lockName">lock name</param>
        /// <param name="ownerTag">owner tag</param>
        /// <param name="duration">lock ownership duration</param>
        /// <param name="assumeExists">true if the lock is expected to exist; false if not</param>
        /// <returns>a lease object that can be used to renew or release the lock</returns>
        /// <remarks>
        ///     assumeExists is used to optimize how the lock manager attempts to acquire the lock (e.g. if the lock is implemented
        ///      via a table, this controls whether the first action is to attempt to insert a new row or fetch an existing row)
        /// </remarks>
        public async Task<ILockLease> AttemptAcquireAsync(
            string lockGroup,
            string lockName, 
            string ownerTag, 
            TimeSpan duration,
            bool assumeExists)
        {
            ArgumentCheck.ThrowIfLessThanOrEqualTo(duration, TimeSpan.Zero, nameof(duration));
            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(lockGroup, nameof(lockGroup));
            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(lockName, nameof(lockName));
            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(ownerTag, nameof(ownerTag));

            AzureTableLockEntry current = null;
            AzureTableLockEntry entry = null;
            bool attemptInsert = assumeExists == false;
            bool created = false;

            await this.EnsureInitialized().ConfigureAwait(false);

            // this is messy, but necessary.
            //  if the lock is expected not to exist, then we want to avoid the overhead of going to table store before attempting
            //   the insert, so we do the insert first in this case
            //  if the insert operation did not exist or was unsuccessful, attempt to fetch an existing lock
            //  if the fetch failed (because the insert was not tried first), then attempt the insert on the 2nd pass
            //  finally, if the last insert failed, attempt one final get (as someone else could have inserted between the initial 
            //   get and insert attempt)
            //  It is expected that if insert fails, the next get will succeed because the only non-fatal insert error is 'already
            //   exists, so the expected patterns of get & insert are:
            //   Insert -> Get
            //   Get -> Insert
            //   Get -> Insert -> Get

            for (int pass = 0; current == null && pass < 2; ++pass)
            {
                if (attemptInsert)
                {
                    current = await this.InsertLockEntityAsync(lockGroup, lockName, ownerTag, duration).ConfigureAwait(false);
                    created = current != null;
                }

                if (current == null)
                {
                    current = await this.FetchLockEntityAsync(lockGroup, lockName).ConfigureAwait(false);

                    // allow insert on the next pass if we need another pass don't already allow inserts
                    attemptInsert = true;
                }
            }

            // if we created the row, the ownership & duration info is already populated as the caller wants, so no need to issue an 
            //  update to set that info
            if (current != null)
            {
                entry = created ? 
                    current :
                    await this.AttemptLockEntryUpdateAsync(UpdateMode.NewAcquire, current, ownerTag, duration).ConfigureAwait(false);
            }

            return entry != null ? new LockLease(this, entry, duration) : null;
        }

        /// <summary>
        ///     attempts to acquire the specified lock for an owner
        /// </summary>
        /// <param name="lockGroup">lock partition</param>
        /// <param name="lockName">lock name</param>
        /// <param name="ownerTag">owner tag</param>
        /// <param name="duration">duration</param>
        /// <returns>a lease object that can be used to renew or release the lock</returns>
        public Task<ILockLease> AttemptAcquireAsync(
            string lockGroup,
            string lockName,
            string ownerTag,
            TimeSpan duration)
        {
            return this.AttemptAcquireAsync(lockGroup, lockName, ownerTag, duration, false);
        }

        /// <summary>
        ///     attempts to insert a new lock entry
        /// </summary>
        /// <param name="lockGroup">lock partition</param>
        /// <param name="lockName">lock name</param>
        /// <param name="ownerTag">owner tag</param>
        /// <param name="duration">duration</param>
        /// <returns>resulting value</returns>
        private async Task<AzureTableLockEntry> InsertLockEntityAsync(
            string lockGroup,
            string lockName,
            string ownerTag,
            TimeSpan duration)
        {
            AzureTableLockEntry current = new AzureTableLockEntry
            {
                LockGroup = lockGroup,
                LockName = lockName,
                LockExpires = this.clock.UtcNow.Add(duration).DateTime,
                OwnerTaskId = ownerTag,
            };

            TableResult result = await this.table.InsertAsync(current, true).ConfigureAwait(false);
            return result.HttpStatusCode == (int)HttpStatusCode.OK ? current : null;
        }

        /// <summary>
        ///     Populates the current lock entity
        /// </summary>
        /// <returns>resulting value</returns>
        private async Task<AzureTableLockEntry> FetchLockEntityAsync(
            string lockGroup,
            string lockName)
        {
            TableResult queryResult = 
                await this.table.QuerySingleRowAsync(
                    TableUtilities.EscapeKey(lockGroup),
                    TableUtilities.EscapeKey(lockName), 
                    true).ConfigureAwait(false);

            AzureTableLockEntry current = null;

            if (queryResult?.HttpStatusCode == (int)HttpStatusCode.OK)
            {
                current = queryResult.Result as AzureTableLockEntry;
                if (current == null)
                {
                    DynamicTableEntity temp = queryResult.Result as DynamicTableEntity;
                    if (temp != null)
                    {
                        current = new AzureTableLockEntry
                        {
                            PartitionKey = temp.PartitionKey,
                            Timestamp = temp.Timestamp,
                            RowKey = temp.RowKey,
                            ETag = temp.ETag,

                            LockExpires = temp.Properties["LockExpires"]?.DateTime ?? AzureTableLockManager.UnownedExpiryTime,
                            OwnerTaskId = temp.Properties["OwnerTaskId"]?.StringValue ?? string.Empty,
                        };
                    }
                }

                if (current == null)
                {
                    this.logger.Error(
                        nameof(AzureTableLockManager), $"Failed to load entry {lockName} from lock table {this.name}");
                }
            }
            
            return current;
        }

        /// <summary>
        ///     attempts to acquire the lock if it is expired or already owned by us
        /// </summary>
        /// <param name="mode">operation mode</param>
        /// <param name="current">most recently fetched lock entity</param>
        /// <param name="desiredOwnerId">desired owner identifier</param>
        /// <param name="duration">lock lease duration</param>
        /// <returns>non-null value if the lock was obtained, null if the lock was not obtained</returns>
        private async Task<AzureTableLockEntry> AttemptLockEntryUpdateAsync(
            UpdateMode mode,
            AzureTableLockEntry current,
            string desiredOwnerId,
            TimeSpan? duration)
        {
            if (this.table == null)
            {
                throw new InvalidOperationException(
                    "Attempting to update lock table without a valid accessor " + current.RowKey);
            }

            // if the lock isn't yet expired and we're not already the owner, then we should not attempt to write it
            if (mode == UpdateMode.NewAcquire && 
                current.LockExpires > this.clock.UtcNow.DateTime &&
                current.OwnerTaskId != null &&
                current.OwnerTaskId.EqualsIgnoreCase(desiredOwnerId) == false)
            {
                return null;
            }

            if (mode != UpdateMode.ReleaseAndPurge)
            {
                TableResult updateResult;

                if (mode != UpdateMode.Release)
                {
                    Debug.Assert(duration.HasValue);
                    current.LockExpires = this.clock.UtcNow.Add(duration.Value).UtcDateTime;
                    current.OwnerTaskId = desiredOwnerId;
                }
                else
                {
                    current.LockExpires = AzureTableLockManager.UnownedExpiryTime;
                    current.OwnerTaskId = string.Empty;
                }

                updateResult = await this.table.ReplaceAsync(current, true).ConfigureAwait(false);
                if (updateResult.HttpStatusCode == (int)HttpStatusCode.OK)
                {
                    AzureTableLockEntry result = updateResult.Result as AzureTableLockEntry;
                    if (result != null)
                    {
                        return result;
                    }

                    this.logger.Error(
                        nameof(AzureTableLockManager),
                        $"Invalid object entry {current.LockName} in lock table {this.name} after update");
                }
            }
            else
            {
                await this.table.DeleteAsync(current, true);
            }

            return null;
        }

        /// <summary>
        ///     attempts to acquire the lock if it is expired or already owned by us
        /// </summary>
        /// <param name="mode">operation mode</param>
        /// <param name="current">most recently fetched lock entity</param>
        /// <returns>non-null value if the lock was obtained, null if the lock was not obtained</returns>
        private Task<AzureTableLockEntry> AttemptLockEntryUpdateAsync(
            UpdateMode mode,
            AzureTableLockEntry current)
        {
            return this.AttemptLockEntryUpdateAsync(mode, current, null, null);
        }

        /// <summary>
        ///     Renews an existing lock lease
        /// </summary>
        /// <param name="entry">lock entry for existing lease</param>
        /// <param name="duration">duration to renew the lock for</param>
        /// <returns>a valid lock entry if the lease is renewed or null if the lease could not be renewed</returns>
        private Task<AzureTableLockEntry> RenewAsync(
            AzureTableLockEntry entry,
            TimeSpan duration)
        {
            // we're renewing with the same owner, so no need to do the owner check.  The update operation will ensure that no 
            //  new update occurred before updating
            return this.AttemptLockEntryUpdateAsync(UpdateMode.Renew, entry, entry.OwnerTaskId, duration);
        }

        /// <summary>
        ///     Releases an existing lock lease
        /// </summary>
        /// <param name="entry">lock entry for existing lease</param>
        /// <param name="purgeLock">true to remove the lock structure entirely (if still owned); false to just release it</param>
        /// <returns>resulting value</returns>
        private Task ReleaseAsync(
            AzureTableLockEntry entry,
            bool purgeLock)
        {
            // we're renewing with the same owner, so no need to do the owner check.  The update operation will ensure that no 
            //  new update occurred before updating
            return this.AttemptLockEntryUpdateAsync(purgeLock ? UpdateMode.ReleaseAndPurge : UpdateMode.Release, entry);
        }

        /// <summary>
        ///     Initializes the object
        /// </summary>
        /// <returns>resulting value</returns>
        private async Task EnsureInitialized()
        {
            if (this.table == null)
            {
                ICloudTable tableLocal;

                try
                {
                    tableLocal = await this.storage.GetCloudTableAsync(this.name).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    this.logger.Error(nameof(AzureTableLockManager), $"Failed to open lock table {this.name}: {e}");
                    throw;
                }

                Interlocked.CompareExchange(ref this.table, tableLocal, null);
            }
        }

        /// <summary>
        ///     object representing a lock lease
        /// </summary>
        private class LockLease : ILockLease
        {
            private readonly AzureTableLockManager @lock;
            private readonly TimeSpan duration;

            private AzureTableLockEntry entry;

            /// <summary>
            ///     Initializes a new instance of the LockLease class
            /// </summary>
            /// <param name="lock">lock manager</param>
            /// <param name="entry">entry</param>
            /// <param name="duration">duration</param>
            public LockLease(
                AzureTableLockManager @lock,
                AzureTableLockEntry entry,
                TimeSpan duration)
            {
                this.duration = duration;
                this.@lock = @lock;
                this.entry = entry;
            }

            /// <summary>
            ///     Renews the lock lease
            /// </summary>
            /// <returns>duration to renew the lease for; if null, the original duration is used</returns>
            /// <returns>true if the lock could be renewed; false otherwise</returns>
            public async Task<bool> RenewAsync(TimeSpan? duration)
            {
                if (this.entry != null)
                {
                    AzureTableLockEntry newEntry = 
                        await this.@lock.RenewAsync(this.entry, duration ?? this.duration).ConfigureAwait(false);

                    if (newEntry != null)
                    {
                        this.entry = newEntry;
                        return true;
                    }

                    return false;
                }

                throw new InvalidOperationException("Lock been released");
            }

            /// <summary>
            ///     Releases the lock lease
            /// </summary>
            /// <param name="purgeLock">true to remove the lock structure entirely (if still owned); false to just release it</param>
            /// <returns>resulting value</returns>
            public async Task ReleaseAsync(bool purgeLock)
            {
                if (this.entry == null)
                {
                    throw new InvalidOperationException("Lock been released");
                }

                await this.@lock.ReleaseAsync(this.entry, purgeLock).ConfigureAwait(false);
                this.entry = null;
            }
        }
    }
}

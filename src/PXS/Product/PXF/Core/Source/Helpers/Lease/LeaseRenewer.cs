// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Lease
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Utilities;

    /// <summary>
    ///     LeaseRenewer class
    /// </summary>
    public class LeaseRenewer : ILeaseRenewer
    {
        private readonly ICollection<Func<Task<bool>>> renwers;
        private readonly Action<string> errorLogger;
        private readonly TimeSpan frequency;
        private readonly IClock clock;
        private readonly string tag;

        private DateTimeOffset lastRenew;

        /// <summary>
        ///    Initializes a new instance of the LeaseRenewer class
        /// </summary>
        /// <param name="renwers">set of functions that will renew leases</param>
        /// <param name="frequency">frequency of lease renewal</param>
        /// <param name="clock">time clock</param>
        /// <param name="errorLogger">error logger</param>
        /// <param name="tag">tag representing the item for which the lease is held</param>
        public LeaseRenewer(
            ICollection<Func<Task<bool>>> renwers,
            TimeSpan frequency,
            IClock clock,
            Action<string> errorLogger,
            string tag)
        {
            this.errorLogger = errorLogger ?? throw new ArgumentNullException(nameof(errorLogger));
            this.renwers = renwers ?? throw new ArgumentNullException(nameof(renwers));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.tag = ArgumentCheck.ReturnIfNotNullEmptyOrWhiteSpace(tag, nameof(tag));

            this.frequency = frequency;

            this.lastRenew = clock.UtcNow;
        }

        /// <summary>
        ///      Gets the count of renewals performed
        /// </summary>
        public long Renewals { get; private set; } = 0;

        /// <summary>
        ///      Renews leases that this class manages
        /// </summary>
        /// <returns>resulting value</returns>
        public async Task RenewAsync(bool force)
        {
            if (this.lastRenew.Add(this.frequency) <= this.clock.UtcNow)
            {
                try
                {
                    ICollection<Task<bool>> waiters = this.renwers.Select(o => o()).ToList();

                    await Task.WhenAll(waiters).ConfigureAwait(false);

                    if (waiters.Any(o => o.Result == false))
                    {
                        throw new LeaseLostException("Failed to reacquire lease for item [" + this.tag + "]");
                    }
                }
                catch (Exception e)
                {
                    this.errorLogger($"Failed to reacquire lease for item [{this.tag}]: {e}");
                    throw;
                }

                this.lastRenew = this.clock.UtcNow;

                this.Renewals += 1;
            }
        }

        /// <summary>
        ///      Renews leases that this class manages
        /// </summary>
        /// <returns>resulting value</returns>
        public Task RenewAsync()
        {
            return this.RenewAsync(false);
        }
    }
}

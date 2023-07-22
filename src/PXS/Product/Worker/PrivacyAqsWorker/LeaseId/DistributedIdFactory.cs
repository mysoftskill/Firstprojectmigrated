// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.LeaseId
{
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Blob;

    public class DistributedIdFactory : IDistributedIdFactory
    {
        private readonly CloudBlobContainer blobContainer;

        private readonly int maxIds;

        private long hint;

        /// <summary>
        ///     Creates an instance of <see cref="DistributedIdFactory" />
        /// </summary>
        /// <param name="blobContainer">The blob container to create leases in</param>
        /// <param name="maxIds">The max number of ids to be able to lease out</param>
        public DistributedIdFactory(CloudBlobContainer blobContainer, int maxIds)
        {
            this.blobContainer = blobContainer ?? throw new ArgumentNullException(nameof(blobContainer));
            this.maxIds = maxIds;
            this.hint = 0;
        }

        /// <summary>
        ///     Gets an ID to use that has a specified lease time
        /// </summary>
        /// <param name="leaseTime">The lease time to have the Id before it expires (between 15-60 seconds)</param>
        /// <returns>An ID if one can be acquired, otherwise null if no id's are available</returns>
        public async Task<IDistributedId> AcquireIdAsync(TimeSpan leaseTime)
        {
            await this.blobContainer.CreateIfNotExistsAsync().ConfigureAwait(false);
            long tempHint = Interlocked.Read(ref this.hint);

            // hint to max
            for (long x = tempHint; x < this.maxIds; ++x)
            {
                IDistributedId id = await this.AcquireIdHelperAsync(leaseTime, x).ConfigureAwait(false);
                if (id != null)
                {
                    Interlocked.Exchange(ref this.hint, x + 1);
                    return id;
                }
            }

            // Cycle below hint
            for (long x = 0; x < this.maxIds && x < tempHint; ++x)
            {
                IDistributedId id = await this.AcquireIdHelperAsync(leaseTime, x).ConfigureAwait(false);
                if (id != null)
                {
                    Interlocked.Exchange(ref this.hint, x + 1);
                    return id;
                }
            }

            // Could not acquire an ID within the limits
            return null;
        }

        /// <summary>
        ///     Tries to acquire a specific id
        /// </summary>
        /// <param name="leaseTime">The length of time to acquire the id for</param>
        /// <param name="attemptId">The id that's being acquired</param>
        /// <returns>the object representing the id, or null if the id is already being used</returns>
        private async Task<IDistributedId> AcquireIdHelperAsync(TimeSpan leaseTime, long attemptId)
        {
            try
            {
                CloudBlockBlob blobRef = this.blobContainer.GetBlockBlobReference($"{attemptId}");
                if (!await blobRef.ExistsAsync().ConfigureAwait(false))
                {
                    await blobRef.UploadTextAsync(string.Empty).ConfigureAwait(false);
                }

                string leaseId = await blobRef.AcquireLeaseAsync(leaseTime).ConfigureAwait(false);
                this.hint = attemptId + 1;
                return new DistributedId(attemptId, leaseId, this.blobContainer);
            }
            catch (StorageException e)
            {
                // Conflict means someone already has a lease for this blob, all other errors are unexpected
                if (e.RequestInformation.HttpStatusCode != (int)HttpStatusCode.Conflict)
                {
                    throw;
                }
            }

            return null;
        }
    }
}

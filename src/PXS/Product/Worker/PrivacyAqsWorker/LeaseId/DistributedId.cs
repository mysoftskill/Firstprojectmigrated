// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.LeaseId
{
    using System.Threading.Tasks;

    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Blob;

    public class DistributedId : IDistributedId
    {
        private readonly CloudBlobContainer blobContainer;

        private readonly string leaseId;

        /// <inheritdoc />
        public long Id { get; }

        /// <inheritdoc />
        public async Task ReleaseAsync()
        {
            await this.blobContainer.CreateIfNotExistsAsync().ConfigureAwait(false);
            CloudBlockBlob blobRef = this.blobContainer.GetBlockBlobReference($"{this.Id}");
            await blobRef.ReleaseLeaseAsync(
                new AccessCondition
                {
                    LeaseId = this.leaseId
                }).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task RenewAsync()
        {
            await this.blobContainer.CreateIfNotExistsAsync().ConfigureAwait(false);
            CloudBlockBlob blobRef = this.blobContainer.GetBlockBlobReference($"{this.Id}");
            await blobRef.RenewLeaseAsync(
                new AccessCondition
                {
                    LeaseId = this.leaseId
                }).ConfigureAwait(false);
        }

        /// <summary>
        ///     Creates a new distributed id
        /// </summary>
        /// <param name="id">the id that is being assigned</param>
        /// <param name="leaseId">the blob lease id for internal implementation details</param>
        /// <param name="blobContainer">the cloud blob container to release/renew with</param>
        internal DistributedId(long id, string leaseId, CloudBlobContainer blobContainer)
        {
            this.Id = id;
            this.leaseId = leaseId;
            this.blobContainer = blobContainer;
        }
    }
}

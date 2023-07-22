// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Blob;

    /// <summary>
    ///     An export destination to an azure blob container.
    /// </summary>
    public class AzureContainerExportDestination : IExportDestination
    {
        private readonly CloudBlobDirectory directory;

        /// <summary>
        ///     Creates an export destination that is an azure blob container in which to write.
        /// </summary>
        /// <param name="container">The container to write to.</param>
        [Obsolete("Use constructor that takes directory instead of container")]
        public AzureContainerExportDestination(CloudBlobContainer container)
            : this(container.GetDirectoryReference(string.Empty))
        {
        }

        /// <summary>
        ///     Creates an export destination that is an azure blob container in which to write.
        /// </summary>
        /// <param name="directory">The container to write to.</param>
        public AzureContainerExportDestination(CloudBlobDirectory directory)
        {
            this.directory = directory;
        }

        /// <inheritdoc />
        public async Task<IExportFile> GetOrCreateFileAsync(string fileNameWithExtension)
        {
            CloudBlockBlob blobReference = this.directory.GetBlockBlobReference(fileNameWithExtension);

            // We probably want the blobs to have some error checking.
            // TODO: Retry policies, and do they even matter with async stream calls?
            // TODO: Timeout values, and do they even matter with async stream calls?
            var blobRequestOptions = new BlobRequestOptions { StoreBlobContentMD5 = true };

            // We don't want any access conditions. We should overwrite the blob if this is a retry.
            AccessCondition accessCondition = AccessCondition.GenerateEmptyCondition();

            // This will overwrite the blob if it exists, purposefully.
            CloudBlobStream stream = await blobReference.OpenWriteAsync(accessCondition, blobRequestOptions, null).ConfigureAwait(false);

            return new StreamExportFile(stream);
        }
    }
}

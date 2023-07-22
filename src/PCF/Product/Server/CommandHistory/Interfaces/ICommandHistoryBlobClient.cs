namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandHistory
{
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// Blob client for cold storage V2.
    /// </summary>
    internal interface ICommandHistoryBlobClient
    {
        /// <summary>
        /// Creates a blob in a random location.
        /// </summary>
        /// <param name="contents">The contents of the blob, which is serialized with JSON.NET.</param>
        /// <returns>The pointer to the blob.</returns>
        Task<BlobPointer> CreateBlobAsync(object contents);

        /// <summary>
        /// Replaces the blob referenced by the given pointer.
        /// </summary>
        /// <param name="blobPointer">The pointer to the blob.</param>
        /// <param name="contents">The contents of the blob, which is serialized with JSON.NET.</param>
        /// <param name="etag">The etag of the blob.</param>
        Task ReplaceBlobAsync(BlobPointer blobPointer, object contents, string etag);

        /// <summary>
        /// Reads the blob referenced by the given pointer.
        /// </summary>
        Task<(T value, string etag)> ReadBlobAsync<T>(BlobPointer blobPointer);
    }
}

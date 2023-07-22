namespace Microsoft.PrivacyServices.AnaheimId.Blob
{
    /// <summary>
    /// A interface for interacting with the blob client
    /// </summary>
    public interface ITestBlobClient
    {
        /// <summary>
        /// Creates a new blob with the provided name and message contents.
        /// </summary>
        /// <param name="blobName">The name of the blob to create.</param>
        /// <param name="message">The message to include in the blob content.</param>
        void CreateTextBlob(string blobName, string message);

        /// <summary>
        /// Check if a blob with a given name exists in the container
        /// </summary>
        /// <param name="blobName">The name of the blob to look for.</param>
        /// <returns>The existence of the blob in the container.</returns>
        bool CheckIfBlobExists(string blobName);

        /// <summary>
        /// Downloads a blob with the provided name and extracts the message contents.
        /// </summary>
        /// <param name="blobName">The name of the blob to read.</param>
        /// <returns>The message contents of the blob.</returns>
        string ReadTextBlob(string blobName);

        /// <summary>
        /// Deletes a blob with the provided name.
        /// </summary>
        /// <param name="blobName">The name of the blob to delete.</param>
        void DeleteBlob(string blobName);
    }
}
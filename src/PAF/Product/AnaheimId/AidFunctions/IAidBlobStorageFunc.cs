namespace Microsoft.PrivacyServices.AnaheimId.AidFunctions
{
    using System.IO;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Anaheim id Blob Storage Function.
    /// </summary>
    public interface IAidBlobStorageFunc
    {
        /// <summary>
        /// Runs the Anaheim ID Blob Storage Function.
        /// </summary>
        /// <param name="inBlob">The memory stream for the input blob.</param>
        /// <param name="name">The name of the blob.</param>
        /// <param name="logger">The logger instance.</param>
        void Run(Stream inBlob, string name, ILogger logger);
    }
}

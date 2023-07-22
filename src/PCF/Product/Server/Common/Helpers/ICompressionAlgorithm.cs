namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.IO;
    using System.IO.Compression;
    
    /// <summary>
    /// A compression algoritm.
    /// </summary>
    public interface ICompressionAlgorithm
    {
        /// <summary>
        /// The friendly name of this algorithm.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets a write-only stream that writes compressed data into <paramref name="destinationStream"/>.
        /// </summary>
        /// <param name="destinationStream">The stream to write the compressed data to.</param>
        /// <param name="compressionLevel">The level of compression requested.</param>
        Stream GetCompressionStream(Stream destinationStream, CompressionLevel compressionLevel);

        /// <summary>
        /// Gets a read-only stream that reads compressed data out of <paramref name="compressedDataStream"/>.
        /// </summary>
        /// <param name="compressedDataStream">The streawm containing the compressed data.</param>
        Stream GetDecompressionStream(Stream compressedDataStream);
    }
}

namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using Brotli;

    /// <summary>
    /// A Brotli implementation of ICompressionAlgorithm.
    /// </summary>
    public sealed class GzipCompressionAlgorithm : ICompressionAlgorithm
    {
        /// <inheritdoc/>
        public string Name => "gzip";

        /// <inheritdoc/>
        public Stream GetCompressionStream(Stream destinationStream, CompressionLevel compressionLevel)
        {
            return new GZipStream(destinationStream, compressionLevel, true);
        }

        /// <inheritdoc/>
        public Stream GetDecompressionStream(Stream compressedDataStream)
        {
            return new GZipStream(compressedDataStream, CompressionMode.Decompress, true);
        }
    }
}

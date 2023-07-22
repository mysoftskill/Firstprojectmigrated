namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using Brotli;

    /// <summary>
    /// A Brotli implementation of ICompressionAlgorithm.
    /// </summary>
    public sealed class BrotliCompressionAlgorithm : ICompressionAlgorithm
    {
        /// <inheritdoc/>
        public string Name => "brotli";

        /// <inheritdoc/>
        public Stream GetCompressionStream(Stream destinationStream, CompressionLevel compressionLevel)
        {
            if (compressionLevel == CompressionLevel.NoCompression)
            {
                throw new ArgumentOutOfRangeException(nameof(compressionLevel));
            }

            var stream = new BrotliStream(destinationStream, CompressionMode.Compress, true);

            if (compressionLevel == CompressionLevel.Fastest)
            {
                stream.SetQuality(2);
            }
            else
            {
                stream.SetQuality(5);
            }

            return stream;
        }

        /// <inheritdoc/>
        public Stream GetDecompressionStream(Stream compressedDataStream)
        {
            return new BrotliStream(compressedDataStream, CompressionMode.Decompress, true);
        }
    }
}

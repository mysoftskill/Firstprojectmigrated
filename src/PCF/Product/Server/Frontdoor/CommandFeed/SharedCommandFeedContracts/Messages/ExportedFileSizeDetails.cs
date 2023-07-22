namespace Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts
{
    /// <summary>
    ///     Exported File Size Details
    /// </summary>
    public class ExportedFileSizeDetails
    {
        /// <summary>
        ///     File name
        /// </summary>
        public string FileName { get; }

        /// <summary>
        ///     Is the file compressed
        /// </summary>
        public bool IsCompressed { get; }

        /// <summary>
        ///     Size of the Original File
        /// </summary>
        public long OriginalSize { get; }

        /// <summary>
        ///     Size of the file in compressed format
        /// </summary>
        public long CompressedSize { get; }

        /// <summary>
        ///     Exported File Size Object constructor
        /// </summary>
        /// <param name="fileName">File name exported</param>
        /// <param name="compressedSize">Compressed File size</param>
        /// <param name="isCompressed">Compressed status</param>
        /// <param name="originalSize">Original File size</param>
        public ExportedFileSizeDetails(string fileName, long compressedSize, bool isCompressed, long originalSize)
        {
            this.FileName = fileName;
            this.IsCompressed = isCompressed;

            // Compressed Size is set to streamLength irrespective of compression
            this.CompressedSize = compressedSize;

            this.OriginalSize = !isCompressed && originalSize == 0 ? compressedSize : originalSize;
        }
    }
}

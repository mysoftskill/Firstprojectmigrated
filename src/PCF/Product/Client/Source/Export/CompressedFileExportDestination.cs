namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Client.Helpers;

    /// <summary>
    ///     An export destination that uses Zip File compression.
    /// </summary>
    public class CompressedFileExportDestination : IExportDestination
    {
        private readonly IExportDestination innerDestination;

        /// <summary>
        /// Initializes a new instances of <see cref="CompressedFileExportDestination"/>.
        /// </summary>
        /// <param name="innerDestination">The inner destination.</param>
        public CompressedFileExportDestination(IExportDestination innerDestination)
        {
            this.innerDestination = innerDestination;
        }

        /// <inheritdoc />
        public async Task<IExportFile> GetOrCreateFileAsync(string fileNameWithExtension)
        {
            IExportFile innerFile = await this.innerDestination.GetOrCreateFileAsync(fileNameWithExtension + ".zip");

            StreamExportFile streamFile = innerFile as StreamExportFile;
            if (streamFile == null)
            {
                throw new InvalidOperationException("Compressed files must be used with the StreamExportFile class.");
            }

            return new CompressedExportFile(streamFile, fileNameWithExtension);
        }

        private sealed class CompressedExportFile : IExportFile
        {
            private readonly Stream inputBuffer;

            private readonly ZipArchive zipArchive;
            private readonly ZipArchiveEntry zipArchiveEntry;
            private readonly StreamExportFile innerFile;

            public CompressedExportFile(StreamExportFile innerFile, string fileNameAndPath)
            {
                this.innerFile = innerFile;
                
                this.zipArchive = new ZipArchive(innerFile.InnerStream, ZipArchiveMode.Create, true, Encoding.UTF8);

                string fileName = Path.GetFileName(fileNameAndPath);
                this.zipArchiveEntry = this.zipArchive.CreateEntry(fileName, CompressionLevel.Fastest);

                this.inputBuffer = new BufferedStream(this.zipArchiveEntry.Open(), 1024 * 1024);
            }

            public Task<long> AppendAsync(Stream data)
            {
                return data.CopyAsync(this.inputBuffer);
            }

            public void Dispose()
            {
                // Make sure all input data is flushed to the zip archive.
                this.inputBuffer.Dispose();
                
                // Close the zip archive.
                this.zipArchive.Dispose();

                // Dispose the inner file.
                this.innerFile.Dispose();
            }
        }
    }
}

// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ZipWriter
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Threading.Tasks;

    using Microsoft.Azure.Storage.Blob;

    /// <summary>
    ///     ZipWriter is a simple utility class that writes a zip file.
    /// </summary>
    public class ZipWriter : IDisposable
    {
        private readonly ZipArchive archive;

        public ZipWriter(Stream stream)
        {
            this.archive = new ZipArchive(stream, ZipArchiveMode.Create);
        }

        public void Dispose()
        {
            this.archive?.Dispose();
        }

        public void WriteEntryAsync(string relativePath, BinaryReader reader)
        {
            const int bufferSize = 64 * 1024;
            var buffer = new byte[bufferSize];
            ZipArchiveEntry entry = this.archive.CreateEntry(relativePath, CompressionLevel.Optimal);
            using (var writer = new BinaryWriter(entry.Open()))
            {
                int bytesRead = reader.Read(buffer, 0, buffer.Length);
                while (bytesRead > 0)
                {
                    writer.Write(buffer, 0, bytesRead);
                    bytesRead = reader.Read(buffer, 0, buffer.Length);
                }
            }
        }

        public async Task WriteEntryAsync(string relativePath, CloudBlockBlob blob)
        {
            const int bufferSize = 512 * 1024;
            var buffer = new byte[bufferSize];
            ZipArchiveEntry entry = this.archive.CreateEntry(relativePath, CompressionLevel.Optimal);
            using (var writer = new BinaryWriter(entry.Open()))
            {
                int bytesRead = await blob.DownloadRangeToByteArrayAsync(buffer, 0, 0, bufferSize).ConfigureAwait(false);
                long offset = bytesRead;
                while (bytesRead > 0)
                {
                    writer.Write(buffer, 0, bytesRead);
                    if (offset >= blob.Properties.Length)
                    {
                        break;
                    }
                    bytesRead = await blob.DownloadRangeToByteArrayAsync(buffer, 0, offset, bufferSize).ConfigureAwait(false);
                    offset += bytesRead;
                }
            }
        }
    }
}

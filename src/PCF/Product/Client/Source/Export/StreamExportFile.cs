// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System.IO;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Client.Helpers;

    /// <summary>
    ///     A file that represents simply a writeable stream of some kind.
    /// </summary>
    public sealed class StreamExportFile : IExportFile
    {
        private readonly bool leaveOpen;

        private readonly Stream stream;

        /// <summary>
        ///     An export file that is backed by simply a stream.
        /// </summary>
        /// <param name="stream">The stream this file represents.</param>
        /// <param name="leaveOpen">A flag indicating if the stream should be left open when disposing the file.</param>
        public StreamExportFile(Stream stream, bool leaveOpen = false)
        {
            this.stream = stream;
            this.leaveOpen = leaveOpen;
        }

        /// <summary>
        /// Gets the stream that this file uses.
        /// </summary>
        public Stream InnerStream => this.stream;

        /// <inheritdoc />
        public Task<long> AppendAsync(Stream data)
        {
            return data.CopyAsync(this.stream);
        }

        /// <summary>
        ///     Disposes the underlying stream.
        /// </summary>
        public void Dispose()
        {
            if (!this.leaveOpen)
            {
                this.stream?.Dispose();
            }
        }
    }
}

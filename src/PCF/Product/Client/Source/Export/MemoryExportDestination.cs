// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    ///     An export destination that is purely in memory, a list of <see cref="MemoryStream" />s.
    /// </summary>
    public class MemoryExportDestination : IExportDestination
    {
        /// <summary>
        ///     The list of files and their memory streams. If you modify or seek the streams
        ///     during export you will corrupt the data.
        /// </summary>
        public Dictionary<string, MemoryStream> Files { get; } = new Dictionary<string, MemoryStream>();

        /// <inheritdoc />
        public Task<IExportFile> GetOrCreateFileAsync(string fileNameWithExtension)
        {
            MemoryStream stream;
            lock (this.Files)
            {
                if (!this.Files.TryGetValue(fileNameWithExtension, out stream))
                {
                    stream = this.Files[fileNameWithExtension] = new MemoryStream();
                }
                else
                {
                    stream.SetLength(0);
                }
            }

            return Task.FromResult<IExportFile>(new StreamExportFile(stream, true));
        }
    }
}

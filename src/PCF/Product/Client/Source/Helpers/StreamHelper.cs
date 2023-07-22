// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Client.Helpers
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    ///     Contains helper methods for Stream Operations
    /// </summary>
    internal static class StreamHelper
    {
        /// <summary>
        ///     Copies source stream to destination stream
        /// </summary>
        /// <param name="source">source stream</param>
        /// <param name="destination">destination stream</param>
        /// <returns>Bytes copied to destination</returns>
        public static async Task<long> CopyAsync(this Stream source, Stream destination)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (!source.CanRead && !source.CanWrite)
            {
                throw new ObjectDisposedException(nameof(source), "Object Disposed");
            }

            if (!destination.CanRead && !destination.CanWrite)
            {
                throw new ObjectDisposedException(nameof(destination), "Object Disposed");
            }

            if (!source.CanRead)
            {
                throw new NotSupportedException("Not Supported. Unreadable Source Stream");
            }

            if (!destination.CanWrite)
            {
                throw new NotSupportedException("Not Supported Unwritable Destination Stream");
            }

            byte[] buffer = new byte[81920];
            long totalBytes = 0;
            while (true)
            {
                int bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None).ConfigureAwait(false);
                if (bytesRead != 0)
                {
                    await destination.WriteAsync(buffer, 0, bytesRead, CancellationToken.None).ConfigureAwait(false);
                }
                else
                {
                    break;
                }

                totalBytes += bytesRead;
            }

            return totalBytes;
        }
    }
}

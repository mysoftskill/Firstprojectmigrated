// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.Utilities
{
    using System.IO;
    using System.IO.Compression;
    using System.Threading.Tasks;

    /// <summary>
    /// A utility class to provide GZip compression and decompression methods.
    /// </summary>
    public static class GZipUtilities
    {
        /// <summary>
        /// Unzip/decompress GZip compressed content.
        /// </summary>
        /// <param name="content">The compressed content to decompress.</param>
        /// <returns>A string representation of the decompressed content.</returns>
        public static async Task<byte[]> Decompress(byte[] content)
        {
            using (var input = new MemoryStream(content))
            using (var zipper = new GZipStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                await zipper.CopyToAsync(output);
                return output.ToArray();
            }
        }

        /// <summary>
        /// Compress content using GZip.
        /// </summary>
        /// <param name="content">The content to compress.</param>
        /// <returns>A string representation of the compressed content.</returns>
        public static async Task<byte[]> Compress(byte[] content)
        {
            byte[] result;

            using (MemoryStream output = new MemoryStream())
            {
                using (GZipStream zipper = new GZipStream(output, CompressionMode.Compress, leaveOpen: false))
                {
                    await zipper.WriteAsync(content, 0, content.Length);
                }
                
                // Must let zipper finish before reading from output
                result = output.ToArray();
            }

            return result;
        }
    }
}

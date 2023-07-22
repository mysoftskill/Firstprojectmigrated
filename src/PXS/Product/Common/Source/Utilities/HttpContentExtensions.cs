// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.Utilities
{
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;

    /// <summary>
    /// An extension class to extend the capabilities of HTTP content.
    /// </summary>
    public static class HttpContentExtensions
    {
        /// <summary>
        /// Compresses the provided HTTP content using GZip encoding.
        /// </summary>
        /// <param name="content">The content to compress.</param>
        /// <returns>A new instance of HTTP content that is compressed. Retains original headers, where appropriate.</returns>
        public static async Task<HttpContent> CompressGZip(this HttpContent content)
        {
            byte[] originalData = await content.ReadAsByteArrayAsync();
            byte[] compressedData = await GZipUtilities.Compress(originalData);

            HttpContent compressedContent = new ByteArrayContent(compressedData);
            var compressedLength = compressedContent.Headers.ContentLength;

            compressedContent.Headers.Clear();
            compressedContent.Headers.AddOrReplace(content.Headers);

            // replace content-length since size has changed from original request
            compressedContent.Headers.ContentLength = compressedLength;

            // Add gzip to content-encoding since content is compressed
            compressedContent.Headers.ContentEncoding.Add(ContentCodings.GZip);
            return compressedContent;
        }

        /// <summary>
        /// Decompresses HTTP content that is compressed using GZip encoding.
        /// </summary>
        /// <param name="content">The content to decompress.</param>
        /// <returns>A new instance of HTTP content that is decompressed. Retains original headers, where appropriate.</returns>
        public static async Task<HttpContent> DecompressGZip(this HttpContent content)
        {
            byte[] compressedBody = await content.ReadAsByteArrayAsync();
            byte[] decompressedBody = await GZipUtilities.Decompress(compressedBody);

            HttpContent decompressedContent = new ByteArrayContent(decompressedBody);
            var decompressedLength = decompressedContent.Headers.ContentLength;

            decompressedContent.Headers.Clear();
            decompressedContent.Headers.AddOrReplace(content.Headers);

            // replace content-length since size has changed from original request
            decompressedContent.Headers.ContentLength = decompressedLength;

            // Remove gzip from content-encoding since content is no longer compressed
            // TODO: do case insensitive remove
            decompressedContent.Headers.ContentEncoding.Remove(ContentCodings.GZip);
            return decompressedContent;
        }

        public static void AddOrReplace(this HttpContentHeaders target, HttpContentHeaders others)
        {
            foreach (var header in others)
            {
                // Throws System.FormatException if target already contains header
                // "Cannot add value because header 'Content-Length' does not support multiple values."
                if (target.Contains(header.Key))
                {
                    target.Remove(header.Key);
                }

                target.Add(header.Key, header.Value);
            }
        }
    }
}

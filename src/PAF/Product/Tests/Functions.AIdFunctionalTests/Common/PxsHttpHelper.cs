namespace Microsoft.PrivacyServices.AzureFunctions.AIdFunctionalTests.AnaheimId
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using OSGSHttpClient = Microsoft.OSGS.HttpClientCommon;

    /// <summary>
    /// Helper class used for create PXS httpclient
    /// </summary>
    public static class PxsHttpHelper
    {
        /// <summary>
        /// Create Pxs Http Client.
        /// </summary>
        /// <param name="aidFctConfig">Aid Fct Config.</param>
        /// <returns>IHttpClient</returns>
        public static OSGSHttpClient.IHttpClient CreatePxsHttpClient(AidFctConfig aidFctConfig)
        {
            var certHandler = new WebRequestHandler
            {
                CheckCertificateRevocationList = true
            };
            var client = new OSGSHttpClient.HttpClient(certHandler) { BaseAddress = new Uri($"https://{aidFctConfig.PxsApiHost}") };

            var s2sCert = CertificateFinder.FindCertificateByName(aidFctConfig.CloudTestCertSubjectName, false);

            client.MessageHandler.AttachClientCertificate(s2sCert);

            ServicePointManager.ServerCertificateValidationCallback += (o, c, ch, er) => true;

            return client;
        }

        /// <summary>
        /// Compresses the provided HTTP content using GZip encoding.
        /// </summary>
        /// <param name="content">The content to compress.</param>
        /// <returns>A new instance of HTTP content that is compressed. Retains original headers, where appropriate.</returns>
        public static async Task<HttpContent> CompressGZip(this HttpContent content)
        {
            byte[] originalData = await content.ReadAsByteArrayAsync();

            byte[] compressedData;

            using (MemoryStream output = new MemoryStream())
            {
                using (GZipStream zipper = new GZipStream(output, CompressionMode.Compress, leaveOpen: false))
                {
                    await zipper.WriteAsync(originalData, 0, originalData.Length);
                }

                // Must let zipper finish before reading from output
                compressedData = output.ToArray();
            }

            HttpContent compressedContent = new ByteArrayContent(compressedData);
            var compressedLength = compressedContent.Headers.ContentLength;

            compressedContent.Headers.Clear();
            compressedContent.Headers.AddOrReplace(content.Headers);

            // replace content-length since size has changed from original request
            compressedContent.Headers.ContentLength = compressedLength;

            // Add gzip to content-encoding since content is compressed
            compressedContent.Headers.ContentEncoding.Add("gzip");
            return compressedContent;
        }

        /// <summary>
        /// Add or relace request headers
        /// </summary>
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

        /// <summary>
        /// Attaches the client certificate.
        /// </summary>
        /// <param name="messageHandler">The message handler.</param>
        /// <param name="clientCertificate">The client certificate.</param>
        public static void AttachClientCertificate(this HttpMessageHandler messageHandler, X509Certificate2 clientCertificate)
        {
            if (messageHandler == null)
                throw new ArgumentNullException(nameof(messageHandler));
            if (clientCertificate == null)
                throw new ArgumentNullException(nameof(clientCertificate));

            // Find the WebRequestHandler on which to attach the certificate
            WebRequestHandler webRequestHandler;
            while (true)
            {
                // Stop if we have found the WebRequestHandler
                webRequestHandler = messageHandler as WebRequestHandler;
                if (webRequestHandler != null)
                {
                    break;
                }

                // Otherwise, attempt to move to the next handler in the pipeline
                if (messageHandler is DelegatingHandler delegatingHandler)
                {
                    messageHandler = delegatingHandler.InnerHandler;
                }
                else
                {
                    // If the next handler is not a delegating handler, then we cannot proceed any further
                    break;
                }
            }

            if (webRequestHandler == null)
            {
                throw new ArgumentException("Must specify a WebRequestHandler to attach certificates in HTTP client.");
            }

            if (!webRequestHandler.ClientCertificates.Contains(clientCertificate))
            {
                webRequestHandler.ClientCertificates.Add(clientCertificate);
            }
        }
    }
}

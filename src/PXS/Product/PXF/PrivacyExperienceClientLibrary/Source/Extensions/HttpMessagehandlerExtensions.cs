// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Extensions
{
    using System;
    using System.Net.Http;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// HttpMessagehandlerExtensions
    /// </summary>
    public static class HttpMessageHandlerExtensions
    {
        /// <summary>
        /// Attaches the client certificate.
        /// </summary>
        /// <param name="messageHandler">The message handler.</param>
        /// <param name="clientCertificate">The client certificate.</param>
        public static void AttachClientCertificate(this HttpMessageHandler messageHandler, X509Certificate2 clientCertificate)
        {
            messageHandler.ThrowOnNull("messageHandler");
            clientCertificate.ThrowOnNull("clientCertificate");

            // Find the WebRequestHandler on which to attach the certificate
            WebRequestHandler webRequestHandler = null;
            while (true)
            {
                // Stop if we have found the WebRequestHandler
                webRequestHandler = messageHandler as WebRequestHandler;
                if (webRequestHandler != null)
                {
                    break;
                }

                // Otherwise, attempt to move to the next handler in the pipeline
                DelegatingHandler delegatingHandler = messageHandler as DelegatingHandler;
                if (delegatingHandler != null)
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
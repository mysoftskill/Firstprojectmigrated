// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Adapters.Common.Handlers
{
    using System.Net.Http;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// An HTTP client handler to attach a client certificate to the request before it is executed.
    /// </summary>
    public class CertificateHandler : WebRequestHandler
    {
        /// <summary>
        /// An HTTP client handler to attach a client certificate to the request before it is executed. Will not add the certificate
        /// if the certificate passed is null.
        /// </summary>
        /// <param name="certificate">The certificate to add to the request.</param>
        /// <param name="skipServerCertValidation">Whether to skip server SSL certificate validation.</param>
        public CertificateHandler(X509Certificate2 certificate, bool skipServerCertValidation)
        {
            if (certificate != null)
            {
                this.ClientCertificates.Add(certificate);
            }

            // Optionally skip server certification validation if configured (e.g. if we are calling a test/onebox endpoint 
            // that doesn't match the cert subject).
            // This should NOT be true for production environments
            if (skipServerCertValidation)
            {
                this.ServerCertificateValidationCallback = delegate { return true; };
            }
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return await base.SendAsync(request, cancellationToken);
        }
    }
}

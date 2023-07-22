namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.Owin;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    /// <summary>
    /// Client certificate loader.
    /// </summary>
    public static class ClientCertificateLoader
    {
        private const string ClientCertPropertyName = "PCF_ClientCertificate";
        
        // From System.Net.UnsafeNclNativeMethods.ErrorCodes.ERROR_NOT_FOUND.
        // It's returned from Win32 APIs when no certificate was presented by the client. 
        private const int CertificateNotFoundErrorCode = 1168;

        /// <summary>
        /// Loads the client certificate smartly. Assumes we are running in OWIN.
        /// </summary>
        public static async Task<X509Certificate2> LoadClientCertificateAsync(this HttpRequestMessage requestMessage)
        {
            if (requestMessage.Properties.ContainsKey(ClientCertPropertyName))
            {
                return (X509Certificate2)requestMessage.Properties[ClientCertPropertyName];
            }

            IOwinContext context = requestMessage.GetOwinContext();

            if (context == null)
            {
                return null;
            }

            HttpListenerContext listenerContext = (HttpListenerContext)context.Environment[typeof(HttpListenerContext).FullName];

            // Load client certificates only if this is MSA auth
            if (!ServiceAuthorizer.TryGetMsaTicket(requestMessage.Headers, out _))
            {
                return null;
            }

            X509Certificate2 cert;
            try
            {
                cert = await listenerContext.Request.GetClientCertificateAsync();
            }
            catch (Exception ex)
            {
                if (ex is HttpListenerException hle && hle.ErrorCode == CertificateNotFoundErrorCode)
                {
                    // No cert available from the client isn't unexpected, just catch and set to null.
                    PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "ClientCertificateLoader:CertNotFoundErrors").Increment();
                    cert = null;
                }
                else if (ex is InvalidOperationException)
                {
                    // Can happen if we do two concurrent requests to GetClientCertificateAsync.
                    // We want to throw in this case because it's our fault.
                    Logger.Instance?.UnexpectedException(ex);
                    throw;
                }
                else
                {
                    // Otherwise, just catch and set to null.
                    Logger.Instance?.UnexpectedException(ex);
                    cert = null;
                }
            }

            requestMessage.Properties[ClientCertPropertyName] = cert;
            return cert;
        }
    }
}

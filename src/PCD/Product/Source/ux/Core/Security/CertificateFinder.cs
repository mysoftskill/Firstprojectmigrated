namespace Microsoft.PrivacyServices.UX.Core.Security
{
    using System;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Cloud.InstrumentationFramework;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Provides a shortcut for retrieving certificates.
    /// </summary>
    public class CertificateFinder : ICertificateFinder
    {
        /// <summary>
        ///     Component name; for logging purposes.
        /// </summary>
        private const string ComponentName = nameof(CertificateFinder);

        /// <summary>
        ///     Instance of The logger.
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CertificateFinder" /> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public CertificateFinder(ILogger logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Finds certificate by subject name.
        /// </summary>
        /// <param name="subjectName">The subject name of the certificate.</param>
        /// <returns>The certificate.</returns>
        public X509Certificate2 FindBySubjectName(string subjectName)
        {
            const string MethodName = nameof(this.FindBySubjectName);
            logger.MethodEnter(ComponentName, MethodName);
            X509Certificate2 mostRecentCert;
            if (!string.IsNullOrWhiteSpace(subjectName))
            {
                using (var certStore = new X509Store(StoreLocation.LocalMachine))
                {
                    certStore.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                    var logMessage = new StringBuilder();
                    foreach (X509Certificate2 certStoreCertificate in certStore.Certificates)
                    {
                        if (certStoreCertificate != null)
                        {
                            logMessage.AppendLine(certStoreCertificate.ToString());
                            logMessage.AppendLine($"{nameof(certStoreCertificate.HasPrivateKey)}: {certStoreCertificate.HasPrivateKey}");
                        }
                    }
                    logger.Information(nameof(CertificateFinder), logMessage.ToString());
                    X509Certificate2Collection matchingCertificates = certStore.Certificates.Find(X509FindType.FindBySubjectName, subjectName, false);
                    logMessage.AppendLine($"Certs found in cert store with subject: '{subjectName}': {matchingCertificates.Count}");
                    mostRecentCert = CertHelper.GetCertWithMostRecentIssueDate(CertHelper.GetCertsWithExactSubjectName(matchingCertificates.Cast<X509Certificate2>(), subjectName), requirePrivateKey: true);

                    if(mostRecentCert == null)
                    {
                        string message = $"Certificate Subject: {subjectName} was not found in {StoreLocation.LocalMachine} store, or was invalid";
                        logger.Error(ComponentName, message);
                        throw new ArgumentException(message, nameof(subjectName));
                    }
                }
            }else
            {
                throw new ArgumentNullException(nameof(subjectName), $"Cannot find cert because value of {nameof(subjectName)} is null or whitespace.");
            }
            IfxTracingLevel traceLevel = IfxTracingLevel.Informational;
            if (!mostRecentCert.HasPrivateKey)
            {
                traceLevel = IfxTracingLevel.Error;
            }

            logger.Log(traceLevel, ComponentName, $"Result: {mostRecentCert}, {nameof(mostRecentCert.HasPrivateKey)}: {mostRecentCert?.HasPrivateKey}");

            logger.MethodExit(ComponentName, MethodName);

            return mostRecentCert;
        }
    }
}

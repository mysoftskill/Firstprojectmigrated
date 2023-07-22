// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Common
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Cloud.InstrumentationFramework;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     Certificate provider implementation.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class CertificateProvider : ICertificateProvider
    {
        /// <summary>
        ///     Component name; for logging purposes.
        /// </summary>
        private const string ComponentName = nameof(CertificateProvider);

        /// <summary>
        ///     Instance of The logger.
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CertificateProvider" /> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public CertificateProvider(ILogger logger)
        {
            this.logger = logger;
        }

        /// <summary>
        ///     Retrieve a client certificate.
        /// </summary>
        /// <param name="certConfig">The certificate configuration.</param>
        /// <returns>The retrieved certificate.</returns>
        public X509Certificate2 GetClientCertificate(ICertificateConfiguration certConfig)
        {
            const string MethodName = "GetClientCertificate";

            string certConfigString =
                certConfig == null
                    ? "null"
                    : string.Format(
                        CultureInfo.InvariantCulture,
                        "(Subject = {0}, Issuer = {1}, Thumbprint = {2})",
                        certConfig.Subject,
                        certConfig.Issuer,
                        certConfig.Thumbprint);

            this.logger.MethodEnter(ComponentName, MethodName);
            this.logger.Information(ComponentName, "Input: (CertConfig: {0})", certConfigString);

            X509Certificate2 result = null;
            if (certConfig != null)
            {
                X509Store certStore = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                try
                {
                    certStore.Open(OpenFlags.ReadOnly);

                    if (!string.IsNullOrWhiteSpace(certConfig.Thumbprint))
                    {
                        // Allow lookup just by thumbprint, if issuer isn't specified.
                        if (string.IsNullOrWhiteSpace(certConfig.Issuer))
                        {
                            result = certStore.Certificates.First(certConfig.Thumbprint, certConfig.CheckValidity);
                        }
                        else
                        {
                            X509Certificate2Collection certs = certStore.Certificates.Find(
                                X509FindType.FindByThumbprint,
                                certConfig.Thumbprint,
                                validOnly: certConfig.CheckValidity);

                            foreach (X509Certificate2 cert in certs)
                            {
                                if (string.Equals(cert.IssuerName.Name, certConfig.Issuer))
                                {
                                    this.logger.Information(nameof(CertificateProvider), cert.ToLogMessage("PXS"));
                                    result = cert;
                                    break;
                                }
                            }
                        }
                    }

                    // If not found by thumbprint lookup using subject name
                    if (result == null)
                    {
                        // Note we do not check that the Issuer matches the cert
                        result = GetClientCertificate(certConfig.Subject, StoreLocation.LocalMachine);
                    }

                    if (result == null)
                    {
                        string message = string.Format(
                            CultureInfo.InvariantCulture,
                            "CertificateConfiguration {0} was not found in LocalMachine store, or was invalid",
                            certConfigString);
                        this.logger.Error(ComponentName, message);
                        throw new ArgumentException(message, "certConfig");
                    }
                    else 
                    {
                        this.logger.Information(nameof(CertificateProvider), result.ToLogMessage("PXS"));

                    }
                }
                finally
                {
                    certStore.Close();
                }
            }

            IfxTracingLevel traceLevel = IfxTracingLevel.Informational;
            if (result == null || !result.HasPrivateKey)
            {
                traceLevel = IfxTracingLevel.Error;
            }

            this.logger.Log(traceLevel, ComponentName, $"Result: {result}, {nameof(result.HasPrivateKey)}: {result?.HasPrivateKey}");

            this.logger.MethodExit(ComponentName, MethodName);
            return result;
        }

        /// <summary>
        ///     Finds and returns the most recently issued certificate matching the subject.
        /// </summary>
        /// <param name="subject">The subject of the certificate.</param>
        /// <param name="storeLocation">The store location</param>
        /// <returns>The client certificate</returns>
        public X509Certificate2 GetClientCertificate(string subject, StoreLocation storeLocation)
        {
            const string MethodName = nameof(this.GetClientCertificate);
            this.logger.MethodEnter(ComponentName, MethodName);

            X509Certificate2 mostRecentCert;
            if (!string.IsNullOrWhiteSpace(subject))
            {
                // If the subject is prefixed with CN=, it's removed, because this method doesn't work with it.
                if (subject.StartsWith("CN="))
                {
                    subject = subject.Remove(0, 3);
                }

                X509Store store = new X509Store(StoreName.My, storeLocation);
                try
                {
                    store.Open(OpenFlags.ReadOnly);
                    var logMessage = new StringBuilder();
                    logMessage.AppendLine($"Certs found in cert store with subject: '{subject}': {store.Certificates.Count}");

                    foreach (X509Certificate2 certStoreCertificate in store.Certificates)
                    {
                        if (certStoreCertificate != null)
                        {
                            logMessage.AppendLine(certStoreCertificate.ToString());
                            logMessage.AppendLine($"{nameof(certStoreCertificate.HasPrivateKey)}: {certStoreCertificate.HasPrivateKey.ToString()}");
                        }
                    }

                    this.logger.Information(nameof(CertificateProvider), logMessage.ToString());

                    X509Certificate2Collection matchingCertificates = store.Certificates.Find(X509FindType.FindBySubjectName, subject, false);
                    mostRecentCert = CertHelper.GetCertWithMostRecentIssueDate(
                        CertHelper.GetCertsWithExactSubjectName(matchingCertificates.Cast<X509Certificate2>(), subject), 
                        requirePrivateKey: true);

                    if (mostRecentCert == null)
                    {
                        string message = $"Certificate Subject: {subject} was not found in {storeLocation.ToString()} store, or was invalid";
                        this.logger.Error(ComponentName, message);
                        throw new ArgumentException(message, nameof(subject));
                    }
                }
                finally
                {
                    store.Close();
                }
            }
            else
            {
                throw new ArgumentNullException(nameof(subject), $"Cannot find cert because value of {nameof(subject)} is null or whitespace.");
            }

            IfxTracingLevel traceLevel = IfxTracingLevel.Informational;
            if (!mostRecentCert.HasPrivateKey)
            {
                traceLevel = IfxTracingLevel.Error;
            }

            this.logger.Log(traceLevel, ComponentName, $"Result: {mostRecentCert}, {nameof(mostRecentCert.HasPrivateKey)}: {mostRecentCert?.HasPrivateKey}");

            this.logger.MethodExit(ComponentName, MethodName);
            return mostRecentCert;
        }
    }
}

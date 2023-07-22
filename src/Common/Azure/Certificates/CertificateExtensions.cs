//--------------------------------------------------------------------------------
// <copyright file="CertificateExtensions.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.PrivacyServices.Common.Azure
{
    using System;
    using System.Text;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;

    public static class CertificateExtensions
    {
        /// <summary>
        ///     Gets the hash value for the X.509 certificate as an array of bytes
        /// </summary>
        /// <param name="certificate">The X.509 certificate to get the hash of</param>
        /// <returns>the has value for the X.509 certificate as an array of bytes</returns>
        public static byte[] GetCertHash256(this X509Certificate2 certificate)
        {
            byte[] certHash;
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] certBytes = certificate.GetRawCertData();
                certHash = sha256Hash.ComputeHash(certBytes);
            }
            return certHash;
        }

        /// <summary>
        ///     Gets the SHA256 hash value for the X.509 certificate as a hexadecimal string
        /// </summary>
        /// <param name="certificate">X.509 certificate to get the hash of</param>
        /// <returns>the SHA256 hash value for the X.509 certificate as a hexadecimal string</returns>
        public static string GetCertHash256String(this X509Certificate2 certificate)
        {
            return BitConverter.ToString(certificate.GetCertHash256()).Replace("-", string.Empty);
        }

        /// <summary>
        ///     Gets the SHA256 hash value for the X.509 certificate as a base64 encoded string
        /// </summary>
        /// <param name="certificate">The X.509 certificate to get the hash of</param>
        /// <returns>the SHA256 hash value for the X.509 certificate as a base64 encoded string</returns>
        public static string GetCertHash256Base64String(this X509Certificate2 certificate)
        {
            return Convert.ToBase64String(certificate.GetCertHash256());
        }
        
        /// <summary>
        ///     Creates a formatted log message with certificate information.
        /// </summary>
        /// <param name="certificate">The X509Certificate2 certificate.</param>
        /// <param name="serviceName">The service using the certificate.</param>
        /// <param name="information">Additional information to include in the log message.</param>
        /// <returns>A fully formatted log message with certificate information.</returns>
        public static string ToLogMessage(this X509Certificate2 certificate, string serviceName, string information = null)
        {
            // Collect information to add to log message
            string service = serviceName ?? "Unknown";
            string subject = certificate?.Subject ?? "Unknown";
            string issuer = certificate?.Issuer ?? "Unknown";
            string version = certificate?.Version.ToString() ?? "Unknown";
            string validDate = certificate?.NotBefore.ToString() ?? "Unknown";
            string expiryDate = certificate?.NotAfter.ToString() ?? "Unknown";
            string thumbprint = certificate?.Thumbprint ?? "Unknown";
            string serialNumber = certificate?.SerialNumber ?? "Unknown";
            string friendlyName = certificate?.PublicKey.Oid.FriendlyName ?? "Unknown";
            string hasPrivateKey = certificate?.HasPrivateKey.ToString() ?? "Unknown";
            string rawDataLength = certificate?.RawData.Length.ToString() ?? "Unknown";

            // Create log message with all certificate information
            StringBuilder logMessageBuilder = new StringBuilder();
            logMessageBuilder.AppendLine("Certificate Information");
            logMessageBuilder.AppendLine($"Service: {service}");
            logMessageBuilder.AppendLine($"Subject: {subject}");
            logMessageBuilder.AppendLine($"Issuer: {issuer}");
            logMessageBuilder.AppendLine($"Version: {version}");
            logMessageBuilder.AppendLine($"Valid Date: {validDate}");
            logMessageBuilder.AppendLine($"Expiry Date: {expiryDate}");
            logMessageBuilder.AppendLine($"Thumbprint: {thumbprint}");
            logMessageBuilder.AppendLine($"Serial Number: {serialNumber}");
            logMessageBuilder.AppendLine($"Friendly Name: {friendlyName}");
            logMessageBuilder.AppendLine($"Has Private Key: {hasPrivateKey}");
            logMessageBuilder.AppendLine($"Raw Data Length: {rawDataLength}");

            // Include additional information if provided in the call
            if (information != null) 
            {
                logMessageBuilder.AppendLine($"Additional Information: {information}");
            }

            return logMessageBuilder.ToString();
        }
    }
}
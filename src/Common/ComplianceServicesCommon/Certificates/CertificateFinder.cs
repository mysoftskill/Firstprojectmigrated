namespace Microsoft.Azure.ComplianceServices.Common
{
    using System;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// A set of utility methods for locating certificates.
    /// </summary>
    public static class CertificateFinder
    {
        /// <summary>
        /// Attempts to load a certificate from the given thumbprint.
        /// </summary>
        /// <param name="thumbprint">The thumbprint.</param>
        /// <param name="validOnly">True to return only valid certificates.</param>
        /// <returns>The certificate.</returns>
        public static X509Certificate2 FindCertificateByThumbprint(string thumbprint, bool validOnly = true)
        {
            if (string.IsNullOrEmpty(thumbprint))
            {
                throw new ArgumentException("Thumbprint cannot be null or empty");
            }

            return CertificateFinder.Find<string>(thumbprint, validOnly, new Func<StoreName, StoreLocation, bool, string, X509Certificate2>(CertificateFinder.FindCertificateByThumbprint));
        }

        /// <summary>
        /// Attempts to load a certificate from the given serial number.
        /// </summary>
        /// <param name="serialNumber">The thumbprint.</param>
        /// <param name="validOnly">True to return only valid certificates.</param>
        /// <returns>The certificate.</returns>
        public static X509Certificate2 FindCertificateBySerialNumber(string serialNumber, bool validOnly = true)
        {
            if (string.IsNullOrEmpty(serialNumber))
            {
                throw new ArgumentException("Serial number cannot be null or empty");
            }

            return CertificateFinder.Find<string>(serialNumber, validOnly, new Func<StoreName, StoreLocation, bool, string, X509Certificate2>(CertificateFinder.FindCertificateBySerialNumber));
        }

        /// <summary>
        /// Attempts to load a certificate from the given subject name.
        /// </summary>
        /// <param name="subjectName">The thumbprint.</param>
        /// <param name="validOnly">True to return only valid certificates.</param>
        /// <returns>The certificate.</returns>
        public static X509Certificate2 FindCertificateByName(string subjectName, bool validOnly = true)
        {
            if (string.IsNullOrEmpty(subjectName))
            {
                throw new ArgumentException("Subject name cannot be null or empty");
            }

            return CertificateFinder.Find<string>(subjectName, validOnly, new Func<StoreName, StoreLocation, bool, string, X509Certificate2>(CertificateFinder.FindCertificateByName));
        }

        /// <summary>
        /// Attempts to load a certificate from the given thumbprint.
        /// </summary>
        /// <param name="storeName">The store to inspect.</param>
        /// <param name="storeLocation">The location of the store.</param>
        /// <param name="validOnly">True to return only valid certificates.</param>
        /// <param name="thumbprint">The thumbprint of the certificate.</param>
        /// <returns>The certificate.</returns>
        public static X509Certificate2 FindCertificateByThumbprint(StoreName storeName, StoreLocation storeLocation, bool validOnly, string thumbprint)
        {
            if (string.IsNullOrEmpty(thumbprint))
            {
                throw new ArgumentException("Thumbprint cannot be null or empty");
            }

            return CertificateFinder.Find(storeName, storeLocation, X509FindType.FindByThumbprint, validOnly, thumbprint);
        }

        /// <summary>
        /// Attempts to load a certificate from the given serial number.
        /// </summary>
        /// <param name="storeName">The store to inspect.</param>
        /// <param name="storeLocation">The location of the store.</param>
        /// <param name="validOnly">True to return only valid certificates.</param>
        /// <param name="serialNumber">The serial number of the certificate.</param>
        /// <returns>The certificate.</returns>
        public static X509Certificate2 FindCertificateBySerialNumber(StoreName storeName, StoreLocation storeLocation, bool validOnly, string serialNumber)
        {
            if (string.IsNullOrEmpty(serialNumber))
            {
                throw new ArgumentException("Serial number cannot be null or empty");
            }

            return CertificateFinder.Find(storeName, storeLocation, X509FindType.FindBySerialNumber, validOnly, serialNumber);
        }

        /// <summary>
        /// Attempts to load a certificate from the given serial number.
        /// </summary>
        /// <param name="storeName">The store to inspect.</param>
        /// <param name="storeLocation">The location of the store.</param>
        /// <param name="validOnly">True to return only valid certificates.</param>
        /// <param name="subjectName">The serial number of the certificate.</param>
        /// <returns>The certificate.</returns>
        public static X509Certificate2 FindCertificateByName(StoreName storeName, StoreLocation storeLocation, bool validOnly, string subjectName)
        {
            if (string.IsNullOrEmpty(subjectName))
            {
                throw new ArgumentException("Name cannot be null or empty");
            }

            return CertificateFinder.Find(storeName, storeLocation, X509FindType.FindBySubjectName, validOnly, subjectName);
        }
        
        private static X509Certificate2 Find(StoreName storeName, StoreLocation storeLocation, X509FindType findType, bool validOnly, object findValue)
        {
            X509Certificate2 x509 = null;
            X509Store store = null;
            try
            {
                store = new X509Store(storeName, storeLocation);
                store.Open(OpenFlags.OpenExistingOnly);
                X509Certificate2Collection certificates = store.Certificates.Find(findType, findValue, validOnly);

                // Return most recently issued certificate
                x509 = certificates.Cast<X509Certificate2>().OrderByDescending(c => c.NotBefore).FirstOrDefault();

            }
            finally
            {
                if (store != null)
                {
                    store.Close();
                }
            }

            return x509;
        }

        private static X509Certificate2 Find<T>(T value, bool validOnly, Func<StoreName, StoreLocation, bool, T, X509Certificate2> func)
        {
            X509Certificate2 validCert = func(StoreName.My, StoreLocation.LocalMachine, true, value) ?? func(StoreName.My, StoreLocation.CurrentUser, true, value);
            if (validCert != null || validOnly)
            {
                return validCert;
            }

            return func(StoreName.My, StoreLocation.LocalMachine, false, value) ?? func(StoreName.My, StoreLocation.CurrentUser, false, value);
        }
    }
}

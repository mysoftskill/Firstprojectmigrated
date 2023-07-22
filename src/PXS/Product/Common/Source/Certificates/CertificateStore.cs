// <copyright company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Oss.Membership.CommonCore.Extensions;

namespace Microsoft.Membership.MemberServices.Common
{
    public static class CertificateStore
    {
        public static CertificateStoreLocation LocalMachine
        {
            get
            {
                return new CertificateStoreLocation(StoreLocation.LocalMachine);
            }
        }

        public static CertificateStoreLocation CurrentUser
        {
            get
            {
                return new CertificateStoreLocation(StoreLocation.CurrentUser);
            }
        }
    }

    public class CertificateStoreLocation
    {
        private StoreLocation Location;

        public CertificateStoreLocation(StoreLocation location)
        {
            this.Location = location;
        }

        public CertificateStoreName My
        {
            get
            {
                return new CertificateStoreName(Location, StoreName.My);
            }
        }
    }

    public class CertificateStoreName
    {
        private StoreLocation Location;
        private StoreName Name;

        public CertificateStoreName(StoreLocation location, StoreName name)
        {
            this.Name = name;
            this.Location = location;
        }

        // Adding certificates to this collection will not install them. Install certs through CertificateStoreName.
        public X509Certificate2Collection Certificates
        {
            get
            {
                X509Store store = new X509Store(Name, Location);

                try
                {
                    // Cannot install certs through X509Certificate2Collection so only open with read access
                    store.Open(OpenFlags.ReadOnly);
                    return store.Certificates;
                }
                finally
                {
                    store.Close();
                }
            }
        }

        public void Install(X509Certificate2 certificate)
        {
            X509Store store = new X509Store(Name, Location);
            
            try
            {
                store.Open(OpenFlags.ReadWrite);
                store.Add(certificate);
            }
            finally
            {
                store.Close();
            }
        }
    }

    public static class X509Certificate2CollectionExtensions
    {
        /// <summary>
        /// Returns the first certificate in the collection that matches the specified thumbprint.
        /// </summary>
        /// <param name="certificates">The collection of certificates to search through.</param>
        /// <param name="thumbprint">The thumbprint to match against. No whitespaces.</param>
        /// <param name="validOnly"><see langword="true"/> to allow only valid certificates to be returned from the search; otherwise, <see langword="false"/>.</param>
        /// <returns>Returns the found certificate; otherwise, throws an exception.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when no matching certificate is found in the collection.</exception>
        public static X509Certificate2 First(this X509Certificate2Collection certificates, string thumbprint, bool validOnly = true)
        {
            var matchingCertificates = certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly);

            if (matchingCertificates.Count <= 0)
            {
                string message = "Collection contains no matching certificate. Thumbprint: {0}".FormatInvariant(thumbprint);
                throw new InvalidOperationException(message);
            }

            return matchingCertificates[0];
        }

        /// <summary>
        /// Determines whether a certificate with the specified thumbprint is in the collection.
        /// </summary>
        /// <param name="certificates">The collection of certificates to search through.</param>
        /// <param name="thumbprint">The thumbprint to match against. No whitespaces.</param>
        /// <param name="validOnly"><see langword="true"/> to search through only valid certificates; otherwise, <see langword="false"/>.</param>
        /// <returns><see langword="true"/> if any matching certificates are found; otherwise, <see langword="false"/>.</returns>
        public static bool Contains(this X509Certificate2Collection certificates, string thumbprint, bool validOnly = true)
        {
            var matchingCertificates = certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly);
            return matchingCertificates.Count > 0;
        }
    }
}

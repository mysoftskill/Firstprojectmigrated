namespace Microsoft.PrivacyServices.DataManagement.AzureKeyVaultCertificateInstaller
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using System.IO;
    using System.Security.AccessControl;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.FileSystem.KeyVault;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;

    public class CertificateInstaller
    {
        private readonly IEventWriterFactory eventWriterFactory;
        private readonly IPrivacyConfigurationManager privacyConfigurationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateInstaller"/> class.
        /// </summary>
        /// <param name="eventWriterFactory">EventWriter</param>
        /// <param name="privacyConfigurationManager">IPrivacyConfigurationManager</param>
        public CertificateInstaller(IEventWriterFactory eventWriterFactory, IPrivacyConfigurationManager privacyConfigurationManager)
        {
            this.eventWriterFactory = eventWriterFactory ?? throw new ArgumentNullException(nameof(eventWriterFactory));
            this.privacyConfigurationManager = privacyConfigurationManager;
        }

        /// <summary>
        ///     Runs the installer async.
        /// </summary>
        /// <returns>Task that returns true if the method execution was successful.</returns>
        public async Task<bool> RunAsync()
        {
            bool failure = false;
            try
            {
                var keyVaultReader = new AzureKeyVaultReader(this.privacyConfigurationManager, this.eventWriterFactory);

                // Get all the certs that exist in the Key Vault.
                IList<X509Certificate2> certs = await keyVaultReader.GetCertificatesAsync(CancellationToken.None).ConfigureAwait(false);

                // Install them all, and grant permissions to Network Service.
                foreach (X509Certificate2 cert in certs)
                {
                    this.eventWriterFactory.Trace(nameof(CertificateInstaller), cert.ToLogMessage("PDMS"));
                    failure |= !this.TryInstallCertificate(cert);
                    failure |= !this.TrySetCertificatePermissionsForNetworkService(cert);
                }

                // Configure the SSL certificate binding. It's stored in Key Vault under a special name,
                // so no thumbprint values need to be saved in configuration.
                X509Certificate2 sslCert = await keyVaultReader.GetCertificateByNameAsync("ssl").ConfigureAwait(false);
                failure |= !TryConfigureSslCertificateBinding(this.eventWriterFactory, sslCert.Thumbprint);
                if (failure)
                {
                    this.eventWriterFactory.WriteEvent(nameof(CertificateInstaller), $"Certificate installation fail.");
                }
                else
                {
                    this.eventWriterFactory.WriteEvent(nameof(CertificateInstaller), $"Certificate installation completed successfully!");
                }
            }
            catch (Exception e)
            {
                this.eventWriterFactory.WriteEvent(nameof(CertificateInstaller), $"An unknown error occured installing Certificate. Error: {e}", EventLevel.Error);
                failure = true;
            }

            return !failure;
        }

        private static bool TryConfigureSslCertificateBinding(IEventWriterFactory eventWriterFactory, string thumbprint, string port = "443")
        {
            eventWriterFactory.WriteEvent(nameof(CertificateInstaller), $"Begin binding ssl cert {thumbprint} to port: {port}");
            try
            {
                NetworkShellHttpContext.DeleteSslCert(NetworkShellHttpContext.IPv4Wildcard, port);
                const string HttpListenerAppId = "{12345678-db90-4b66-8b01-88f7af2e36bf}";
                NetworkShellHttpContext.AddSslCert(NetworkShellHttpContext.IPv4Wildcard, port, HttpListenerAppId, thumbprint);

                eventWriterFactory.WriteEvent(nameof(CertificateInstaller), $"Done binding ssl cert {thumbprint} to port: {port}");
            }
            catch (Exception e)
            {
                eventWriterFactory
                    .WriteEvent(
                    nameof(CertificateInstaller),
                    $"Error binding ssl cert {thumbprint} in method: {nameof(TryConfigureSslCertificateBinding)}. Error: {e.Message}",
                    EventLevel.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Install cert
        /// </summary>
        /// <param name="cert">The cert</param>
        private bool TryInstallCertificate(X509Certificate2 cert)
        {
            if (cert == null)
            {
                return false;
            }

            using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
            {
                try
                {
                    // Open the Certificate Store.
                    store.Open(OpenFlags.ReadWrite);

                    this.eventWriterFactory.WriteEvent(
                        nameof(CertificateInstaller),
                        $"Begin installing certificate: " +
                        $"Thumbprint: {cert.Thumbprint}, Subject: {cert.Subject}, Issuer: {cert.Issuer}, Valid between {cert.NotBefore} to {cert.NotAfter} " +
                        $"to {nameof(X509Store)}: {store.Name}, {store.Location}");

                    int certIndex = store.Certificates.IndexOf(cert);
                    if (certIndex >= 0 && store.Certificates[certIndex].PrivateKey != null)
                    {
                        this.eventWriterFactory.WriteEvent(nameof(CertificateInstaller), $"Certificate already exists in cert store and has private key: {cert.Thumbprint}.");
                    }
                    else
                    {
                        this.eventWriterFactory.WriteEvent(
                            nameof(CertificateInstaller),
                            $"Certificate doesn't exist in cert store: {store.Name}, {store.Location}. Adding the cert to the store.");

                        // If new cert (brand new or renewed) found, install it
                        // store.Add uses Upsert semantics.
                        store.Add(cert);
                        this.eventWriterFactory.WriteEvent(
                            nameof(CertificateInstaller),
                            $"Certificate installed successfully. cert: {cert.Thumbprint}");
                    }
                }
                catch (Exception exception)
                {
                    this.eventWriterFactory.WriteEvent(
                            $"{nameof(CertificateInstaller)}: Failed to install the certificate in Store. cert: {cert.Thumbprint}. Error: {exception.Message}", EventLevel.Error);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Services that run as NETWORK SERVICE need read access to the certificates key in order to use the certificate as a client certificate
        /// </summary>
        /// <param name="certificate"></param>
        private bool TrySetCertificatePermissionsForNetworkService(X509Certificate2 certificate)
        {
            string allUsersProfile = Environment.GetEnvironmentVariable("ALLUSERSPROFILE");
            const string KeyFileNameFormat = "{0}\\Microsoft\\Crypto\\RSA\\MachineKeys\\{1}";
            try
            {
                string uniqueName = ((RSACryptoServiceProvider)certificate.PrivateKey).CspKeyContainerInfo.UniqueKeyContainerName;
                string keyFileName = string.Format(KeyFileNameFormat, allUsersProfile, uniqueName);
                var keyFile = new FileInfo(keyFileName);
                FileSecurity acl = keyFile.GetAccessControl();
                acl.AddAccessRule(new FileSystemAccessRule("NETWORK SERVICE", FileSystemRights.Read, AccessControlType.Allow));
                keyFile.SetAccessControl(acl);
                this.eventWriterFactory.WriteEvent(
                            nameof(CertificateInstaller),
                            $"Success! grant permission for network service to cert: {certificate.Thumbprint}");
            }
            catch (Exception e)
            {
                this.eventWriterFactory.WriteEvent(
                            $"{nameof(CertificateInstaller)}: Failed to grant permission for network service to have access to cert: {certificate.Thumbprint}) Error: {e.Message}", EventLevel.Error);
                return false;
            }

            return true;
        }
    }
}

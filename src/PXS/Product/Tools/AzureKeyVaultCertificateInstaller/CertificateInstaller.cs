// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.PrivacyServices.AzureKeyVaultCertificateInstaller
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.AccessControl;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.SecretStore;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyHost;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     CertificateInstaller will install certificates on the machine
    /// </summary>
    public class CertificateInstaller
    {
        private readonly ILogger logger;

        private readonly IPrivacyConfigurationManager configuration;

        /// <summary>
        ///     The exclusion list of certs to avoid installing. This is useful if we want something else (ie KV VM extension) to manage installation of certs.
        /// </summary>
        private IList<string> exclusionList = new List<string> { "cosmos-vcclient" };

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateInstaller"/> class.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="configuration"></param>
        public CertificateInstaller(ILogger logger, IPrivacyConfigurationManager configuration)
        {
            ResetTraceDecorator.ResetTraceListeners();
            ResetTraceDecorator.AddConsoleTraceListener();
            DualLogger.AddTraceListener();
            this.logger = logger;
            this.configuration = configuration;
            GenevaHelper.Initialize(TraceTagPrefixes.ADGCS_PXS);
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
                var keyVaultReader = new AzureKeyVaultReader(
                    this.configuration,
                    new Clock(),
                    this.logger);

#if DEBUG
                if (this.configuration.EnvironmentConfiguration.EnvironmentType == EnvironmentType.OneBox)
                {
                    // for dev box we want to install them all (don't exclude anything)
                    this.exclusionList = null;
                }
#endif

                // Get all the certs that exist in the Key Vault.
                IList<X509Certificate2> certs = await keyVaultReader.GetCertificatesAsync(this.exclusionList, CancellationToken.None).ConfigureAwait(false);

                // Install them all, and grant permissions to Network Service.
                foreach (X509Certificate2 cert in certs)
                {
                    this.logger.Information(nameof(CertificateInstaller), cert.ToLogMessage("PXS"));
                    failure |= !TryInstallCertificate(cert, this.logger);
                    failure |= !this.TrySetCertificatePermissionsForNetworkService(cert);
                }

                // Configure the SSL certificate binding. It's stored in Key Vault under a special name,
                // so no thumbprint values need to be saved in configuration.
                X509Certificate2 sslCert = await keyVaultReader.GetCertificateCurrentVersionAsync("ssl").ConfigureAwait(false);
                failure |= !TryConfigureSslCertificateBinding(this.logger, sslCert.Thumbprint);

                if (failure)
                {
                    this.logger.Error(nameof(CertificateInstaller), "Certificate installation fail.");
                }
                else
                {
                    this.logger.Information(nameof(CertificateInstaller), "Certificate installation completed successfully!");
                }
            }
            catch (Exception e)
            {
                this.logger.Error(nameof(CertificateInstaller), e, "An unknown error occurred.");
                failure = true;
            }

            return !failure;
        }

        public static bool TryConfigureSslCertificateBinding(ILogger logger, string thumbprint, string port = "443")
        {
            logger.Information(nameof(CertificateInstaller), $"Begin binding ssl cert {thumbprint} to port: {port}");
            try
            {
                NetworkShellHttpContext.DeleteSslCert(NetworkShellHttpContext.IPv4Wildcard, port);
                const string HttpListenerAppId = "{12345678-db90-4b66-8b01-88f7af2e36bf}";
                NetworkShellHttpContext.AddSslCert(NetworkShellHttpContext.IPv4Wildcard, port, HttpListenerAppId, thumbprint);
                logger.Information(nameof(CertificateInstaller), $"Done binding ssl cert {thumbprint} to port: {port}");
            }
            catch (Exception e)
            {
                logger.Error(nameof(CertificateInstaller), e, $"Error binding ssl cert {thumbprint} in method: {nameof(TryConfigureSslCertificateBinding)}");
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Install cert
        /// </summary>
        /// <param name="cert">The cert</param>
        /// <param name="logger"></param>
        public static bool TryInstallCertificate(X509Certificate2 cert, ILogger logger)
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

                    logger.Information(
                        nameof(CertificateInstaller),
                        "Begin installing certificate: " +
                        $"Thumbprint: {cert.Thumbprint}, Subject: {cert.Subject}, Issuer: {cert.Issuer}, Valid between {cert.NotBefore} to {cert.NotAfter} " +
                        $"to {nameof(X509Store)}: {store.Name}, {store.Location}");

                    int certIndex = store.Certificates.IndexOf(cert);
                    if (certIndex >= 0 && store.Certificates[certIndex].PrivateKey != null)
                    {
                        logger.Information(nameof(CertificateInstaller), $"Certificate already exists in cert store and has private key: {cert.Thumbprint}.");
                    }
                    else
                    {
                        logger.Information(
                            nameof(CertificateInstaller),
                            $"Certificate doesn't exist in cert store: {store.Name}, {store.Location}. Adding the cert to the store.");

                        // If new cert (brand new or renewed) found, install it
                        // store.Add uses Upsert semantics.
                        store.Add(cert);
                        logger.Information(nameof(CertificateInstaller), $"Certificate installed successfully. cert: {cert.Thumbprint}");
                    }
                }
                catch (Exception exception)
                {
                    logger.Error(nameof(CertificateInstaller), exception, $"Failed to install the certificate in Store. cert: {cert.Thumbprint}");
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
                this.logger.Information(nameof(CertificateInstaller), $"Success! grant permission for network service to cert: {certificate.Thumbprint}");
            }
            catch (Exception e)
            {
                this.logger.Error(
                    nameof(CertificateInstaller),
                    e,
                    $"Failed to grant permission for network service to have access to cert: {certificate.Thumbprint})");
                return false;
            }

            return true;
        }
    }
}

namespace CertInstaller
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Security.AccessControl;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    using IniParser;
    using IniParser.Model;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// This program is responsible for loading certificates from Azure Key Vault, and installing them prior to our primary services
    /// executing. This program operates with some special logic in mind, to allow for it to work in situations where the primary
    /// services will not.
    /// </summary>
    /// <remarks>
    /// This used to be hosted in <c>powershell</c>. However, <c>powershell</c> does not play nicely with assembly binding redirects,
    /// which are necessary for even moderately complex libraries.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        private static ILogger traceLogger;
        private const string LoggingComponentName = "PCD_CertInstaller";

        /// <summary>
        /// Entry point.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        public static void Main(string[] args)
        {
            GenevaHelper.Initialize(TraceTagPrefixes.ADGCS_PCD);
            DualLogger.AddTraceListener();
            traceLogger = DualLogger.Instance;

            var iniParser = new FileIniDataParser();
                        
#if DEBUG
            if (Debugger.IsAttached)
            {
                ProcessStartInfo flattenerinfo = new ProcessStartInfo("..\\..\\certFlattener.cmd");
                Process flattener = Process.Start(flattenerinfo);
                flattener.WaitForExit();
            }
#endif

            IniData iniData = iniParser.ReadFile("config.ini.flattened.ini");
            var azureKeyVaultConfig = iniData["AzureKeyVaultConfig"];

            string baseUrl = azureKeyVaultConfig["BaseUrl"];

            InstallCertsAsync(baseUrl).Wait();
        }

        private static async Task InstallCertsAsync(string keyVaultBaseUrl)
        {
#if DEBUG
            var envname = Environment.GetEnvironmentVariable("PCD_EnvironmentName");
            if (envname == null || envname == "devbox")
            {
                Environment.SetEnvironmentVariable("AzureServicesAuthConnectionString",
                "RunAs=App;AppId=98431440-3ebb-4908-93d5-937cde6de308;TenantId=33e01921-4d64-4f8c-a055-5bdaffd5e33d;CertificateSubjectName=CN=s2s.manage.privacy.microsoft-int.com;CertificateStoreLocation=LocalMachine");
            }
#endif
           AzureKeyVaultClient azureKeyVaultClient;
           AzureManagedIdentityKeyVaultClientFactory factory = new AzureManagedIdentityKeyVaultClientFactory(keyVaultBaseUrl);
           azureKeyVaultClient = (AzureKeyVaultClient)factory.CreateDefaultKeyVaultClient();

            // First install the S2S cert. This cert is outbound, so we only really care about the latest version.
            await GetAndInstallCerts(azureKeyVaultClient, "s2s", latestOnly: true);

            // We need to install SSL certificate, and then bind it to the port.
            IList<X509Certificate2> sslCerts = await GetAndInstallCerts(azureKeyVaultClient, "ssl", latestOnly: true).ConfigureAwait(false);

            ConfigureSslCertificateBinding(sslCerts[0]);
        }

        private static async Task<IList<X509Certificate2>> GetAndInstallCerts(AzureKeyVaultClient azureKeyVaultClient, string name, bool latestOnly)
        {
            IList<X509Certificate2> certs = await azureKeyVaultClient.GetCertificateVersionsAsync(name).ConfigureAwait(false);
            if (latestOnly)
            {
                // Take the most recently issued cert.
                certs = new[] { CertHelper.GetCertWithMostRecentIssueDate(certs) };
            }

            foreach (var cert in certs)
            {
                // Checks if the cert exists before it installs it
                // This prevents the need for elevated permissions, since read only doesn't require admin
                bool certInstalled;
                traceLogger.Information(LoggingComponentName, cert.ToLogMessage("PCD"));
                using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
                {
                    store.Open(OpenFlags.ReadOnly);
                    
                    if (store.Certificates.Contains(cert))
                    {
                        certInstalled = true;
                    }
                    else
                    {
                        certInstalled = false;
                    }

                }
                if (!certInstalled)
                {
                    InstallCert(cert);
                }
                else
                {
                    traceLogger.Information(LoggingComponentName, ($"{cert.SubjectName.Name} with Thumbprint {cert.Thumbprint} is already installed"));
                }
                SetCertificatePermissionsForNetworkService(cert);
            }

            return certs;
        }

        private static void InstallCert(X509Certificate2 cert)
        {
            traceLogger.Information(LoggingComponentName, $"Begin installation of cert {cert.Thumbprint} into LOCALMACHINE\\MY");

            using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
            {
                // Open the Certificate Store.
                //
                store.Open(OpenFlags.ReadWrite);
                
                if (!store.Certificates.Contains(cert))
                {
                    // If new cert (brand new or renewed) found, install it
                    traceLogger.Information(LoggingComponentName, cert.ToLogMessage("PCD"));
                    store.Add(cert);
                }
            }
        }

        /// <summary>
        /// Services that run as NETWORK SERVICE need read access to the certificates key in order to use the certificate as a client certificate.
        /// </summary>
        /// <param name="certificate">The certificate to install.</param>
        private static void SetCertificatePermissionsForNetworkService(X509Certificate2 certificate)
        {
            traceLogger.Information(LoggingComponentName, $"Begin ACL of cert {certificate.Thumbprint} for NETWORK SERVICE");

            string allUsersProfile = Environment.GetEnvironmentVariable("ALLUSERSPROFILE");
            const string KeyFileNameFormat = "{0}\\Microsoft\\Crypto\\RSA\\MachineKeys\\{1}";

            string uniqueName = ((RSACryptoServiceProvider)certificate.PrivateKey).CspKeyContainerInfo.UniqueKeyContainerName;
            string keyFileName = string.Format(KeyFileNameFormat, allUsersProfile, uniqueName);
            var keyFile = new FileInfo(keyFileName);
            FileSecurity acl = keyFile.GetAccessControl();
            acl.AddAccessRule(new FileSystemAccessRule("NETWORK SERVICE", FileSystemRights.Read, AccessControlType.Allow));
            keyFile.SetAccessControl(acl);
        }

        private static void ConfigureSslCertificateBinding(X509Certificate2 cert)
        {
            string thumbprint = cert.Thumbprint;

            traceLogger.Information(LoggingComponentName, $"Begin binding ssl cert {thumbprint} to port 443");
            Process p = Process.Start("netsh", "http delete sslcert ipport=0.0.0.0:443");
            p.WaitForExit();
            traceLogger.Information(LoggingComponentName, $"Delete cert exited with code = {p.ExitCode}");
            if (p.ExitCode != 0 && p.ExitCode != 1)
            {
                // 0 is no error; 1 is cert not found
                throw new InvalidOperationException("Netsh delete http binding exited unexpectedly");
            }

            p = Process.Start("netsh", $"http add sslcert ipport=0.0.0.0:443 certhash={cert.Thumbprint} appid={{00112233-4455-6677-8899-AABBCCDDEEFF}} clientcertnegotiation=disable verifyclientcertrevocation=disable");
            p.WaitForExit();
            traceLogger.Information(LoggingComponentName, $"Add cert exited with code = {p.ExitCode}");
            if (p.ExitCode != 0)
            {
                // 0 is no error; 1 is cert not found
                throw new InvalidOperationException("Netsh add http binding exited unexpectedly");
            }
        }
    }
}

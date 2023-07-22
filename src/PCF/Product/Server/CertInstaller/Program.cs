namespace Microsoft.PrivacyServices.CommandFeed.Service.CertificateInstaller
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Security.AccessControl;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.Interfaces;
    using Microsoft.Azure.ComplianceServices.Common.SecretClient;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// This program is responsible for loading certificates from Azure Key Vault, and installing them prior to our primary services
    /// executing. This program operates with some special logic in mind, to allow for it to work in situations where the primary
    /// services will not.
    /// </summary>
    /// <remarks>
    /// This used to be hosted in powershell. However, powershell does not play nicely with assembly binding redirects,
    /// which are necessary for even moderately complex libraries.
    /// </remarks>
    public static class Program
    {
        /// <summary>
        /// Entry point.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "args")]
        public static void Main(string[] args)
        {
            GenevaHelper.Initialize(TraceTagPrefixes.ADGCS_PCF);
            Trace.Listeners.Add(IfxTraceLogger.Instance);

            string magicFileName = Path.Combine(Environment.GetEnvironmentVariable("SystemDrive"), "CommonSetupStamp.txt");

            // Put config into "simple" mode. This causes fancy values to not be read.
            EnvironmentInfo.Initialize("CertInstaller");

            try
            {
                // report an error to AP DM.
                EnvironmentInfo.HostingEnvironment.ReportServiceHealthStatusAsync(ServiceHealthStatus.Error, "Pcf.CommonSetup", "Common Setup is starting. This error clears upon successful completion").GetAwaiter().GetResult();

                InstallCertsAsync().GetAwaiter().GetResult();

                // Install RPS
                InstallRps();

                // Finally, write the file that says we've run to completion and clear the DM error.
                File.WriteAllText(magicFileName, EnvironmentInfo.AssemblyVersion);
                GrantNetworkServiceReadAccess(new FileInfo(magicFileName));
                EnvironmentInfo.HostingEnvironment.ReportServiceHealthStatusAsync(ServiceHealthStatus.OK, "Pcf.CommonSetup", "Woohoo").GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                IfxTraceLogger.Instance.Error(nameof(Process), ex.ToString());
                Console.WriteLine(ex.ToString());
                throw;
            }
        }

        private static async Task InstallCertsAsync()
        {
            IAzureKeyVaultClientFactory client = EnvironmentInfo.HostingEnvironment.CreateKeyVaultClientFactory(
                Config.Instance.AzureKeyVault.BaseUrl,
                Config.Instance.AzureManagement.ApplicationId);

            // Grant network service permission to listen on ports 80 and 443.
            AddNetworkServiceUrlAcl("https://+:443/");

            // SSL. We need to install it, and then bind it to the port.
            X509Certificate2 sslCert = await GetAndInstallCertAsync(client, "pcf-ssl");
            
            // ECR Drill Logging. PBI: #998245
            DualLogger.Instance.Information(nameof(InstallCertsAsync), sslCert.ToLogMessage("PCF", "The PCF SSL Certificate."));

            ConfigureSslCertificateBinding(sslCert);

#if INCLUDE_TEST_HOOKS

            /* sts cert is used for authentication of adls client accessing to adls account.
             * currently migrating to adls client is considered as a backup plan.  
             * refer to the following link for details of Adls client configuration:
             * https://microsoft.sharepoint-df.com/teams/NGPCommonInfra/_layouts/OneNote.aspx?id=%2Fteams%2FNGPCommonInfra%2FSiteAssets%2FNGP%20Common%20Infra%20Notebook&wd=target%28Knowledge%20Base.one%7C104FE2C5-2088-44FF-934D-06B844525BBA%2FADLS%7C5B998E03-D86E-4553-A1C8-0F9C9F3DF733%2F%29
             */

            X509Certificate2 stsCert = await GetAndInstallCertAsync(client, "pcf-sts-onecert");
            DualLogger.Instance.Information(nameof(InstallCertsAsync), stsCert.ToLogMessage("PCF", "The sts cert is used for authenticating the adls client."));

#endif
        }

        private static async Task<X509Certificate2> GetAndInstallCertAsync(IAzureKeyVaultClientFactory client, string name)
        {
            IList<X509Certificate2> certs = await client.CreateDefaultKeyVaultClient().GetCertificateVersionsAsync(name);
            X509Certificate2 mostRecentlyIssuedCert = CertHelper.GetCertWithMostRecentIssueDate(certs);

            try
            {
                InstallCert(mostRecentlyIssuedCert);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }

            return mostRecentlyIssuedCert;
        }

        private static void InstallCert(X509Certificate2 cert)
        {
            Console.WriteLine($"Begin installation of cert {cert.Thumbprint} into LOCALMACHINE\\MY");

            using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
            {
                // Open the Certificate Store.
                store.Open(OpenFlags.ReadWrite);

                if (!store.Certificates.Contains(cert))
                {
                    // If new cert (brand new or renewed) found, install it
                    store.Add(cert);
                }
            }

            SetCertificatePermissionsForNetworkService(cert);
        }

        /// <summary>
        ///     Services that run as NETWORK SERVICE need read access to the certificates key in order to use the certificate as a client certificate
        /// </summary>
        private static void SetCertificatePermissionsForNetworkService(X509Certificate2 certificate)
        {
            Console.WriteLine($"Begin ACL of cert {certificate.Thumbprint} for NETWORK SERVICE");

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
            Console.WriteLine($"Begin binding ssl cert {cert.Thumbprint} to port 443. Issued Date: {cert.NotBefore}. Expiration Date: {cert.NotAfter}");
            int exitCode = RunToExit("netsh", "http delete sslcert ipport=0.0.0.0:443");
            if (exitCode != 0 && exitCode != 1)
            {
                // 0 is no error; 1 is cert not found
                throw new InvalidOperationException("Netsh delete http binding exited unexpectedly");
            }

            exitCode = RunToExit("netsh", $"http add sslcert ipport=0.0.0.0:443 certhash={cert.Thumbprint} appid={{00112233-4455-6677-8899-AABBCCDDEEFF}} clientcertnegotiation=disable verifyclientcertrevocation=disable");
            if (exitCode != 0)
            {
                // 0 is no error; 1 is cert not found
                throw new InvalidOperationException("Netsh add http binding exited unexpectedly");
            }
        }

        private static void InstallRps()
        {
            string rpsConfigDirectory = Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles"), "Microsoft Passport RPS\\Config\\");
            Console.WriteLine("RPS Config path = " + rpsConfigDirectory);

            // uninstall rps
            Console.WriteLine("Uninstalling old RPS");
            var exitCode = RunToExit("msiexec", "/x rps64.msi /quiet /passive /norestart /lv+ rps64.msi.log");
            if (exitCode != 0 && exitCode != 1605) // 1605: we can't uninstall something not installed.
            {
                throw new InvalidOperationException("Failed to uninstall RPS");
            }

            // clean up RPS directory.
            Console.WriteLine("Purging old RPS config.");
            try
            {
                DirectoryInfo d = new DirectoryInfo(rpsConfigDirectory);
                DeleteAll(d.GetFiles("*.cer", SearchOption.AllDirectories));
                DeleteAll(d.GetFiles("*.xml", SearchOption.AllDirectories));
            }
            catch (DirectoryNotFoundException)
            {
                // nothing to do anyway
            }

            // Install RPS
            Console.WriteLine("Installing new RPS");
            Console.WriteLine($"ApplicationData: {System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData)}");
            if (RunToExit("msiexec", "/i rps64.msi /quiet /passive /norestart /lv+ rps64.msi.log ALLUSERS=1") != 0)
            {
                throw new InvalidOperationException("Failed to install RPS");
            }

            // create if not exists
            Directory.CreateDirectory(rpsConfigDirectory);

            Console.WriteLine("Copying new RPS config");
            File.Copy($"{Config.Instance.Common.DataFolder}\\RPS\\rpsserver.xml", Path.Combine(rpsConfigDirectory, "rpsserver.xml"), true);
            File.Copy("data\\rpscomponent.xml", Path.Combine(rpsConfigDirectory, "rpscomponent.xml"), true);

            DirectoryInfo configDirectoryInfo = new DirectoryInfo(rpsConfigDirectory);

            // grant network service access to config files.
            foreach (var file in configDirectoryInfo.GetFiles("*.xml", SearchOption.AllDirectories))
            {
                GrantNetworkServiceReadAccess(file);
            }
        }

        private static void AddNetworkServiceUrlAcl(string url)
        {
            int exitCode = RunToExit("netsh", $"http add urlacl url={url} user=NetworkService");
            if (exitCode != 0 && exitCode != 1)
            {
                throw new InvalidOperationException("netsh add url acl failed unexpectedly");
            }
        }

        private static int RunToExit(string processName, string arguments)
        {
            Process process = new Process();
            process.StartInfo.FileName = processName;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            Console.WriteLine($"Starting (user:{Environment.UserName}) \"{processName} {arguments}\"");

            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            Console.WriteLine(output);
            string err = process.StandardError.ReadToEnd();
            Console.WriteLine(err);
            process.WaitForExit();

            Console.WriteLine($"\"{processName} {arguments}\" Exited with code = {process.ExitCode}");

            return process.ExitCode;
        }

        private static void DeleteAll(IEnumerable<FileInfo> fileInfos)
        {
            foreach (var info in fileInfos)
            {
                info.Delete();
            }
        }

        private static void GrantNetworkServiceReadAccess(FileInfo fileInfo)
        {
            Console.WriteLine("Grant network service access to File = " + fileInfo.FullName);
            var accessRule = new FileSystemAccessRule("NETWORK SERVICE", FileSystemRights.Read, AccessControlType.Allow);

            var acl = fileInfo.GetAccessControl();
            acl.AddAccessRule(accessRule);
            fileInfo.SetAccessControl(acl);
        }
    }
}

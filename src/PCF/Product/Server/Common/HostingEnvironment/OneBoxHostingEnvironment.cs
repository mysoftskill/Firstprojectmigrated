namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using global::Azure.Identity;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.Azure.ComplianceServices.Common.Interfaces;
    using Microsoft.Azure.ComplianceServices.Common.SecretClient;

    /// <summary>
    /// Contains constants and methods related to running PCF in a onebox environment.
    /// </summary>
    public class OneBoxHostingEnvironment : IHostingEnvironment
    {
        public static bool CanInitialize => true;

        public bool IsDevMachine => true;

        public string EnvironmentName => "OneBox";

        public string NodeName => Environment.MachineName;

        private X509Certificate2 clientCert;

        public Task ReportServiceHealthStatusAsync(ServiceHealthStatus status, string name, string message)
        {
            return Task.CompletedTask;
        }

        public IPerformanceCounter GetOrCreatePerformanceCounter(PerformanceCounterType type, string name)
        {
            return new InMemoryPerformanceCounter("OneBox", name);
        }

        public virtual IAzureKeyVaultClientFactory CreateKeyVaultClientFactory(string keyVaultBaseUrl, string clientId)
        {
            return new AzureKeyVaultClientFactory(keyVaultBaseUrl, clientId, GetClientCertificate("cloudtest.privacy.microsoft-int.ms"));
        }

        public Task WaitForDependenciesInstalledAsync()
        {
            return Task.CompletedTask;
        }

        public IAppConfiguration CreateAppConfiguration(PrivacyServices.Common.Azure.ILogger logger)
        {
            return new AppConfiguration(@"local.settings.json", logger);
        }

        private X509Certificate2 GetClientCertificate(string subjectName)
        {
            if (clientCert == null)
            {
                using (X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
                {
                    store.Open(OpenFlags.ReadOnly);

                    X509Certificate2Collection certs = store.Certificates.Find(X509FindType.FindBySubjectName, subjectName, false);

                    if (certs.Count == 0)
                    {
                        throw new InvalidOperationException($"{subjectName} not installed on local machine. Have you run provisiondevmachine.ps1?");
                    }

                    clientCert = CertHelper.GetCertWithMostRecentIssueDate(CertHelper.GetCertsWithExactSubjectName(certs.Cast<X509Certificate2>(), subjectName));
                }

            }

            return clientCert;
        }
    }
}

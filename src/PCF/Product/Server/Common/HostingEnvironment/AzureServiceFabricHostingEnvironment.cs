namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.Azure.ComplianceServices.Common.Interfaces;
    using Microsoft.Azure.ComplianceServices.Common.SecretClient;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.SecretClient;

    /// <summary>
    /// Contains constants and methods related running PCF in Azure.
    /// </summary>
    public class AzureServiceFabricHostingEnvironment : IHostingEnvironment
    {
        private string serviceName;
        private static string environmentName;

        /// <summary>
        /// Initialize static properties
        /// </summary>
        static AzureServiceFabricHostingEnvironment()
        {
            environmentName = Environment.GetEnvironmentVariable("PCF_EnvironmentName");
        }

        public AzureServiceFabricHostingEnvironment(string serviceName)
        {
            this.serviceName = serviceName;
        }

        public static bool CanInitialize => 
            !string.IsNullOrEmpty(environmentName) 
            && !String.Equals(environmentName, "OneBox", StringComparison.OrdinalIgnoreCase);

        public bool IsDevMachine => false;

        public string EnvironmentName => environmentName;

        public string NodeName => Environment.MachineName;
               
        
        public IPerformanceCounter GetOrCreatePerformanceCounter(PerformanceCounterType type, string name)
        {
            return new InMemoryPerformanceCounter(this.EnvironmentName, name);
        }

        public Task ReportServiceHealthStatusAsync(ServiceHealthStatus status, string name, string message)
        {
            return Task.CompletedTask;
        }

        public Task WaitForDependenciesInstalledAsync() => Task.CompletedTask;

        public IAzureKeyVaultClientFactory CreateKeyVaultClientFactory(string keyVaultBaseUrl, string clientId)
        {
            return new AzureManagedIdentityKeyVaultClientFactory(keyVaultBaseUrl);
        }

        public IAppConfiguration CreateAppConfiguration(PrivacyServices.Common.Azure.ILogger logger)
        {
            string labelFilter = LabelNames.None;
            switch (environmentName.ToUpperInvariant())
            {
                case "PXSCI1-TEST-MW1P":
                case "PXSCI2-TEST-MW1P":
                    labelFilter = LabelNames.CI;
                    break;

                case "PXS-SANDBOX-SN2":
                    labelFilter = LabelNames.INT;
                    break;

                case "PXS-PPE-SN3P":
                case "PXS-PPE-MW1P": 
                    labelFilter = LabelNames.PPE;
                    break;
            }

            return new AppConfiguration(new Uri(Config.Instance.AzureAppConfigEndpoint), labelFilter, logger);
        }
    }
}

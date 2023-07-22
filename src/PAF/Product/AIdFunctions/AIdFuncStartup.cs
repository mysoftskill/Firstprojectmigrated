[assembly: Microsoft.Azure.Functions.Extensions.DependencyInjection.FunctionsStartup(typeof(Microsoft.PrivacyServices.AIdFunctions.AIdFuncStartup))]

namespace Microsoft.PrivacyServices.AIdFunctions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography.X509Certificates;
    using System.ServiceModel;
    using global::Azure.Identity;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.IfxMetric;
    using Microsoft.Azure.Functions.Extensions.DependencyInjection;
    using Microsoft.AzureAd.Icm.WebService.Client;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.PrivacyServices.AnaheimId;
    using Microsoft.PrivacyServices.AnaheimId.AidFunctions;
    using Microsoft.PrivacyServices.AnaheimId.Avro;
    using Microsoft.PrivacyServices.AnaheimId.Blob;
    using Microsoft.PrivacyServices.AnaheimId.Config;
    using Microsoft.PrivacyServices.AnaheimId.Icm;
    using Microsoft.PrivacyServices.AzureFunctions.Common;
    using Microsoft.PrivacyServices.AzureFunctions.Common.Helpers;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Common startup code for Anaheim functions.
    /// </summary>
    public class AIdFuncStartup : FunctionsStartup
    {
        private const string ComponentName = nameof(AIdFuncStartup);

        /// <summary>
        /// Configure functions.
        /// </summary>
        /// <param name="builder">Function host builder.</param>
        public override void Configure(IFunctionsHostBuilder builder)
        {
            IAIdFunctionConfiguration config;
            IAIdFunctionConfigurationBuilder configBuilder;
            IAppConfiguration appConfiguration;
            IMetricContainer metricContainer;
            IRedisClient redisClient;

            if (Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") == "Development")
            {
                configBuilder = new AIdLocalConfigurationBuilder(new List<string>() { "local.settings.json" });
                config = configBuilder.Build();

                appConfiguration = new AppConfiguration(localConfigFile: Path.Combine(Environment.CurrentDirectory, "local.settings.json"));
            }
            else
            {
                configBuilder = new AIdConfigurationBuilder();
                config = configBuilder.Build();

                // Initialize Geneva Ifx logging
                GenevaHelper.Initialize(TraceTagPrefixes.ADGCS_PAF, config.MonitoringTenant, config.MonitoringRole, config.AppName);
                appConfiguration = CreateAppConfiguration(config);
            }

            DualLogger.AddTraceListener();

            DualLogger.Instance.Information(ComponentName, "Creating start up....");

            builder.Services.AddSingleton<ILogger>((s) =>
            {
                return DualLogger.Instance;
            });

            // Create Metric container
            string roleInstance = Setup.GetRoleInstance();
            metricContainer = new MetricContainer(config.MonitoringTenant, config.MonitoringRole, roleInstance, config.MetricAccount, config.MetricPrefixName, DualLogger.Instance);

            // Create custom metric - queue depth metrics
            IMetric metric = new Metric2D(metricContainer.Factory, config.MetricAccount, "PAF.FunctionAnaheimQueueDepth", "StorageAccountName", "QueueName", DualLogger.Instance);
            metricContainer.AddMetric("PAF.FunctionAnaheimQueueDepth", metric);

            // Create redis client
            redisClient = RedisClientFactory.Create(config); 

            builder.Services.AddSingleton((s) =>
            {
                return metricContainer;
            });

            builder.Services.AddSingleton((s) =>
            {
                return config;
            });

            builder.Services.AddSingleton((s) =>
            {
                return appConfiguration;
            });

            builder.Services.AddSingleton((s) =>
            {
                return redisClient;
            });

            builder.Services.AddSingleton((s) =>
            {
                return SetupAidFunctionFactory(appConfiguration, metricContainer, redisClient);
            });
        }

        /// <summary>
        /// Setup the Anaheim ID Function Factory
        /// The Aid Function Factory will be configured differently based on the runtime environment.
        /// PAF_AID_ICMConnector_Enabled - Feature flag to enable or disable the actual icm connector.
        /// If actual icm connector is enabled the following feature values are required:
        ///     PAF_AID_ICMConnectorUrl - The url for the icm portal connection.
        ///     PAF_AID_ICMConnectorName - The name of the icm connector.
        ///     PAF_AID_ICMConnectorID - The id for the icm connector.
        ///     PAF_AID_ICMConnectorKeyVaultUrl = The keyvault to receive the icm client certificate authentication.
        ///     PAF_AID_ICMConnectorCertificateName - The name of the icm client certificate to retrieve from keyvault.
        /// If the icm connector is disabled the following feature value is required:    
        ///     PAF_AID_ICMMockTestFiles_Enabled - Flag to determine whether to keep the mock output file in blob storage.
        /// Note: The icm connector is always set to be disabled for local development and the blob storage account is not 
        /// used in the mock client when running locally. A mock test blob implementation will be used for storing the contents
        /// in an in memory dictionary that will simulate setting and receiving the content from the cloud.
        /// </summary>
        /// <returns>The Anaheim ID Function Factory specific to the environment.</returns>
        private static IAidFunctionsFactory SetupAidFunctionFactory(IAppConfiguration appConfiguration, IMetricContainer metricContainer, IRedisClient redisClient)
        {
            var deploymentEnv = AidHelpers.GetDeploymentEnvironment();
            IAidConfig aidconfig = AidConfig.Build(deploymentEnv);
            string environment = aidconfig.DeploymentEnvironment.ToString();
            int severity = environment == "PROD" ? 3 : 4;
            string containerEndpoint = string.Format(
                "https://{0}.blob.core.windows.net/{1}",
                aidconfig.StorageAccountName,
                "missingsignalscontainer");

            // If the ICMEnableClient flag in the Anaheim ID configuration is enabled use the real ICM connector.
            // If the flag is set to false use a mock version of the connector which outputs logs and test blobs for alerts.
            IMissingSignalIcmConnector missingSignalIcmConnector;
            bool icmConnectorEnabled = appConfiguration.IsFeatureFlagEnabledAsync(FeatureNames.PAF.PAF_AID_ICMConnector_Enabled).GetAwaiter().GetResult();
            DualLogger.Instance.Information(ComponentName, $"icmConnectorEnabled={icmConnectorEnabled}");
            if (icmConnectorEnabled)
            {
                // Get the icm connector routing information
                string icmConnectorURL = appConfiguration.GetConfigValue<string>(ConfigNames.PAF.PAF_AID_ICMConnectorUrl);
                DualLogger.Instance.Information(ComponentName, $"icmConnectorURL={icmConnectorURL}");
                string icmConnectorName = appConfiguration.GetConfigValue<string>(ConfigNames.PAF.PAF_AID_ICMConnectorName);
                DualLogger.Instance.Information(ComponentName, $"icmConnectorName={icmConnectorName}");
                string icmConnectorID = appConfiguration.GetConfigValue<string>(ConfigNames.PAF.PAF_AID_ICMConnectorID);
                DualLogger.Instance.Information(ComponentName, $"icmConnectorID={icmConnectorID}");

                // Extract the paramaters needed to retrieve the certificate from azure keyvault
                string icmConnectorKeyVaultUrl = appConfiguration.GetConfigValue<string>(ConfigNames.PAF.PAF_AID_ICMConnectorKeyVaultUrl);
                DualLogger.Instance.Information(ComponentName, $"icmConnectorKeyVaultUrl={icmConnectorKeyVaultUrl}");
                string icmConnectorCertificateName = appConfiguration.GetConfigValue<string>(ConfigNames.PAF.PAF_AID_ICMConnectorCertificateName);
                DualLogger.Instance.Information(ComponentName, $"icmConnectorCertificateName={icmConnectorCertificateName}");

                // Retrieve the client certificate from keyvault
                X509Certificate2 certificate = GetClientCertificate(aidconfig.AidUamiId, icmConnectorKeyVaultUrl, icmConnectorCertificateName);
                DualLogger.Instance.Information(ComponentName, $"Retreived certificate {icmConnectorCertificateName} with thumbprint {certificate.Thumbprint}.");

                // Create the Connector Incident Manager Client for the Missing Signal ICM Connector
                ConnectorIncidentManagerClient connectorIncidentManagerClient = CreateIncidentConnectorClient(certificate, icmConnectorURL);
                missingSignalIcmConnector = new MissingSignalIcmConnector(connectorIncidentManagerClient, new Guid(icmConnectorID), icmConnectorName, environment, severity, containerEndpoint);
                DualLogger.Instance.Information(ComponentName, "Created the Missing Signal Icm Connector");
            }
            else
            {
                // Create the mock icm client to write the output data to blob storage
                ITestBlobClient testBlockBlobClient;
                if (Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") == "Development")
                {
                    DualLogger.Instance.Information(ComponentName, $"Using the Mock Test Blob CLient");
                    testBlockBlobClient = new MockTestBlobClient(DualLogger.Instance);
                }
                else
                {
                    DualLogger.Instance.Information(ComponentName, $"Using the Test Blob CLient");
                    testBlockBlobClient = new TestBlobClient(DualLogger.Instance, 
                        containerEndpoint, 
                        new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = aidconfig.AidUamiId }));
                }
                bool keepTestFiles = appConfiguration.IsFeatureFlagEnabledAsync(FeatureNames.PAF.PAF_AID_ICMMockTestFiles_Enabled).GetAwaiter().GetResult();
                DualLogger.Instance.Information(ComponentName, $"keepTestFiles={keepTestFiles}");
                missingSignalIcmConnector = new MockMissingSignalIcmConnector(testBlockBlobClient, environment, severity, !keepTestFiles);
            }

            // Setup the Function Factory with AID Deployment Configuration
            IMissingRequestFileHelper fileHelper = new MissingRequestFileHelper();
            return new AidFunctionsFactory(aidconfig, metricContainer, fileHelper, missingSignalIcmConnector, appConfiguration, redisClient);
        }

        /// <summary>
        /// Retrieve the client certificate using the msi client id.
        /// </summary>
        /// <returns>An X509Certificate2 object.</returns>
        private static X509Certificate2 GetClientCertificate(string msiClientId, string keyVaultUrl, string certificateName)
        {
            // Create a keyvault client using msi
            var secretsReader = new SecretsReader(keyVaultUrl, new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = msiClientId }), "certificates/" + certificateName);

            // Try to download the full certificate including the private key from the keyault (Create the X509 object)
            try
            {
                string secret = secretsReader.GetSecretByNameAsync(certificateName).GetAwaiter().GetResult();
                return new X509Certificate2(Convert.FromBase64String(secret),
                    (string)null,
                    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
            }
            catch (Exception ex)
            {
                DualLogger.Instance.Error(ComponentName, ex.Message);
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Get the ICM client for a configured with a client auth certificate
        /// </summary>
        /// <returns>The connector incident client manager.</returns>
        private static ConnectorIncidentManagerClient CreateIncidentConnectorClient(X509Certificate2 certificate, string icmConnectorURL)
        {
            try
            {
                ConnectorIncidentManagerClient connectorIncidentManagerClient;
                if (certificate != null) 
                {
                    // Set Http Binding Options
                    var binding = new WS2007HttpBinding(SecurityMode.Transport)
                    {
                        Name = "IcmBindingConfigCert",
                        MaxBufferPoolSize = 4194304,
                        MaxReceivedMessageSize = 16777216
                    };

                    binding.Security.Transport.ProxyCredentialType = HttpProxyCredentialType.None;
                    binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Certificate;
                    binding.ReaderQuotas.MaxArrayLength = 16384;
                    binding.ReaderQuotas.MaxBytesPerRead = 1048576;
                    binding.ReaderQuotas.MaxStringContentLength = 1048576;
                    binding.Security.Message.EstablishSecurityContext = false;
                    binding.Security.Message.NegotiateServiceCredential = true;
                    binding.Security.Message.ClientCredentialType = MessageCredentialType.Certificate;

                    // Create and return incident manager client for the default endpoint address
                    connectorIncidentManagerClient = new ConnectorIncidentManagerClient(binding, new EndpointAddress(icmConnectorURL));
                    connectorIncidentManagerClient.ClientCredentials.ClientCertificate.Certificate = certificate;
                }
                else
                {
                    connectorIncidentManagerClient = new ConnectorIncidentManagerClient();
                    DualLogger.Instance.Error(ComponentName, "No certificate for the Incident Manager Client was provided.");
                }
                return connectorIncidentManagerClient;
            }
            catch (Exception ex)
            {
                DualLogger.Instance.Error(ComponentName, ex.Message);
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Create the app configuration for a given function configuration environment.
        /// </summary>
        /// <returns>The app configuration.</returns>
        private static IAppConfiguration CreateAppConfiguration(IAIdFunctionConfiguration config)
        {
            var monitoringTenant = config.MonitoringTenant;
            string labelFilter = LabelNames.None;
            string azureAppConfigEndpoint = config.AzureAppConfigEndpoint;

            if (monitoringTenant.ToUpperInvariant().Contains("CI1"))
            {
                labelFilter = "CI1";
            }
            else if (monitoringTenant.ToUpperInvariant().Contains("CI2"))
            {
                labelFilter = "CI2";
            }
            else if (monitoringTenant.ToUpperInvariant().Contains("PPE"))
            {
                labelFilter = "PPE";
            }

            return new AppConfiguration(
                azureAppConfigEndpoint: new Uri(azureAppConfigEndpoint),
                labelFilter: labelFilter,
                credential: new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = config.AIdUamiId }));
        }
    }
}

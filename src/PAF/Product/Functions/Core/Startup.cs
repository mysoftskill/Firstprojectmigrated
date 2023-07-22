[assembly: Microsoft.Azure.Functions.Extensions.DependencyInjection.FunctionsStartup(typeof(Microsoft.PrivacyServices.AzureFunctions.Core.Startup))]

namespace Microsoft.PrivacyServices.AzureFunctions.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using global::Azure.Identity;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.IfxMetric;
    using Microsoft.Azure.Functions.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.PrivacyServices.AzureFunctions.Common;
    using Microsoft.PrivacyServices.AzureFunctions.Common.Configuration;
    using Microsoft.PrivacyServices.AzureFunctions.Common.Helpers;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Common startup code for Privacy Azure functions.
    /// </summary>
    public class Startup : FunctionsStartup
    {
        private const string ComponentName = nameof(Startup);

        /// <summary>
        /// Configure functions.
        /// </summary>
        /// <param name="builder">Function host builder.</param>
        public override void Configure(IFunctionsHostBuilder builder)
        {
            IFunctionConfiguration config;
            IFunctionConfigurationBuilder configBuilder;
            IAuthenticationProvider authenticationProvider;
            IAppConfiguration appConfiguration;
            IMetricContainer metricContainer;

            if (Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") == "Development")
            {
                configBuilder = new PafLocalConfigurationBuilder(new List<string>() { "local.settings.json" });
                config = configBuilder.Build();

                var localConfig = config as PafLocalConfiguration;

                var reader = new SecretsReader(localConfig.PafClientId, localConfig.AMETenantId, localConfig.CertificateSubjectName, localConfig.PdmsKeyVaultUrl, "certificates/" + localConfig.PdmsCertName);
                var cert = reader.GetCertificateByNameAsync(localConfig.PdmsCertName).GetAwaiter().GetResult();

                authenticationProvider = new ClientSecretProvider(
                    localConfig.PdmsClientId,
                    localConfig.AMETenantId,
                    localConfig.PdmsResourceId,
                    cert);

                appConfiguration = new AppConfiguration(localConfigFile: Path.Combine(Environment.CurrentDirectory, "local.settings.json"));
            }
            else
            {
                configBuilder = new PafConfigurationBuilder();
                config = configBuilder.Build();
                authenticationProvider = new ManagedIdentityProvider(config.PafUamiId, config.PdmsResourceId);

                // Initialize Geneva Ifx logging
                GenevaHelper.Initialize(TraceTagPrefixes.ADGCS_PAF, config.MonitoringTenant, config.MonitoringRole, config.AppName);

                appConfiguration = CreateAppConfiguration(config);
            }

            DualLogger.AddTraceListener();

            // Create Metric container
            string roleInstance = Setup.GetRoleInstance();
            metricContainer = new MetricContainer(config.MonitoringTenant, config.MonitoringRole, roleInstance, config.MetricAccount, config.MetricPrefixName, DualLogger.Instance);

            // Create custom metric - poison queue metrics
            IMetric metric = new Metric2D(metricContainer.Factory, config.MetricAccount, "PAF.FunctionVariantRequestPoisonQueue", "PoisonQueue", "VariantRequestId", DualLogger.Instance);
            metricContainer.AddMetric("PAF.FunctionVariantRequestPoisonQueue", metric);

            builder.Services.AddSingleton<ILogger>((s) =>
            {
                return DualLogger.Instance;
            });

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
                return authenticationProvider;
            });

            builder.Services.AddSingleton<IVariantRequestProcessorFactory>((s) =>
            {
                return new VariantRequestProcessorFactory();
            });

            builder.Services.AddSingleton((s) =>
            {
                return appConfiguration;
            });
        }

        private static IAppConfiguration CreateAppConfiguration(IFunctionConfiguration config)
        {
            var monitoringTenant = config.MonitoringTenant;
            string labelFilter = LabelNames.None;
            string azureAppConfigEndpoint = config.AzureAppConfigEndpoint;

            if (monitoringTenant.ToUpperInvariant().Contains("PAF-CI1"))
            {
                labelFilter = "CI1";
            }
            else if (monitoringTenant.ToUpperInvariant().Contains("PAF-CI2"))
            {
                labelFilter = "CI2";
            }
            else if (monitoringTenant.ToUpperInvariant().Contains("PAF-PPE"))
            {
                labelFilter = "PPE";
            }

            return new AppConfiguration(
                azureAppConfigEndpoint: new Uri(azureAppConfigEndpoint),
                labelFilter: labelFilter,
                credential: new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = config.PafUamiId }));
        }
    }
}

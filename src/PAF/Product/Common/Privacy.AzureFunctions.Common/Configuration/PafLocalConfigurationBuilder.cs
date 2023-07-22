namespace Microsoft.PrivacyServices.AzureFunctions.Common.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Builder for PAF local configuration.
    /// </summary>
    public class PafLocalConfigurationBuilder : IFunctionConfigurationBuilder
    {
        private readonly List<string> configFiles;

        /// <summary>
        /// Initializes a new instance of the <see cref="PafLocalConfigurationBuilder"/> class.
        /// </summary>
        /// <param name="configFiles">Configuration files to load.</param>
        public PafLocalConfigurationBuilder(List<string> configFiles)
        {
            this.configFiles = configFiles;
        }

        /// <inheritdoc/>
        public IFunctionConfiguration Build()
        {
            var builder = new ConfigurationBuilder();

            foreach (var file in this.configFiles)
            {
                builder.AddJsonFile(Path.Combine(Environment.CurrentDirectory, file), optional: false, reloadOnChange: true);
            }

            var root = builder.Build();
            var config = root.GetSection("Values").Get<PafLocalConfiguration>();

            if (string.IsNullOrEmpty(config.AzureDevOpsAccessToken))
            {
                var reader = new SecretsReader(config.PafClientId, config.AMETenantId, config.CertificateSubjectName, config.PafKeyVaultUrl, "secrets/" + config.AdoSecretName);
                config.AzureDevOpsAccessToken = reader.GetSecretByNameAsync(config.AdoSecretName).GetAwaiter().GetResult();
            }

            if (string.IsNullOrEmpty(config.PafFunctionKey) && !string.IsNullOrEmpty(config.PafFunctionKeyName))
            {
                var reader = new SecretsReader(config.PafClientId, config.AMETenantId, config.CertificateSubjectName, config.PafKeyVaultUrl, "secrets/" + config.PafFunctionKeyName);
                config.PafFunctionKey = reader.GetSecretByNameAsync(config.PafFunctionKeyName).GetAwaiter().GetResult();
            }

            if (config.AadClientCert == null)
            {
                var reader = new SecretsReader(config.PafClientId, config.AMETenantId, config.CertificateSubjectName, config.PafKeyVaultUrl, "certificates/" + config.NGPVariantLinkingBotCertName);
                config.AadClientCert = reader.GetCertificateByNameAsync(config.NGPVariantLinkingBotCertName).GetAwaiter().GetResult();
            }

            return config;
        }
    }
}

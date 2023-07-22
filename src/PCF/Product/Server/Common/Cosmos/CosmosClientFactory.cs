namespace Microsoft.PrivacyServices.CommandFeed.Service.Common.Cosmos
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.SecretClient;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Cosmos.Structured;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Common.Cosmos;

    /// <summary>
    /// Defines a factory that creates and initializes a singleton Cosmos client
    /// </summary>
    public static class CosmosClientFactory
    {
        /// <summary>
        /// Initializes a new Cosmos client
        /// </summary>
        /// <returns>Returns a Cosmos client.</returns>
        public static async Task<ICosmosClient> InitializeCosmosClientAsync(ICosmosResourceFactory factory)
        {
            return await InitializeAdlsCosmosClient(factory).ConfigureAwait(false);

        }

        /// <summary>
        /// Create cosmos structured stream reader
        /// </summary>
        /// <param name="targetStreamFormat"></param>
        /// <param name="client"></param>
        /// <param name="maxAgeInDays"></param>
        /// <param name="factory"></param>
        /// <param name="isHourlyStream">true if it is an hourly stream.</param>
        /// <returns></returns>
        public static async Task<ICosmosStructuredStreamReader> CreateLatestCosmosStructuredStreamReaderAsync(string targetStreamFormat,
            ICosmosClient client,
            long maxAgeInDays,
            ICosmosResourceFactory factory,
            bool isHourlyStream = false)
        {
            (string path, DateTimeOffset modifiedTime)? stream;
            if (isHourlyStream)
            {
                stream = await GetLatestHourlyCosmosStream(targetStreamFormat, client, maxAgeInDays).ConfigureAwait(false);
            }
            else
            {
                stream = await GetLatestDailyCosmosStream(targetStreamFormat, client, maxAgeInDays).ConfigureAwait(false);
            }
            
            if (stream == null)
            {
                throw new InvalidOperationException($"Cannot find Cosmos stream for given format: '{targetStreamFormat}'");
            }

            if (client.ClientTechInUse() == ClientTech.Adls)
            {
                return await CreateAdlsStructuredStreamReader(stream.Value.path, stream.Value.modifiedTime, factory).ConfigureAwait(false);
            }
            else
            {
                throw new InvalidOperationException($"Unsupported client type: '{client.ClientTechInUse()}'");
            }
        }

        /// <summary>
        /// Read the latest cert from KV
        /// </summary>
        /// <param name="certName"></param>
        /// <returns></returns>
        public static async Task<X509Certificate2> GetMostRecentCertFromKeyVaultWithName(string certName)
        {
            IAzureKeyVaultClientFactory azureKeyVaultClientFactory = EnvironmentInfo.HostingEnvironment.CreateKeyVaultClientFactory(Config.Instance.AzureKeyVault.BaseUrl, Config.Instance.AzureManagement.ApplicationId);
            IAzureKeyVaultClient azureKeyVaultClient = azureKeyVaultClientFactory.CreateDefaultKeyVaultClient();

            IList<X509Certificate2> certs = await azureKeyVaultClient.GetCertificateVersionsAsync(certName).ConfigureAwait(false);

            if (certs?.Count == 0)
            {
                throw new InvalidOperationException($"No certs found in KV with secret name: {certName}. Cannot initialize CosmosClient.");
            }

            X509Certificate2 mostRecentlyIssuedCert = CertHelper.GetCertWithMostRecentIssueDate(certs);
            // ECR Drill Logging. PBI: #998245
            DualLogger.Instance.Information(nameof(CosmosClientFactory), mostRecentlyIssuedCert.ToLogMessage("PCF", "The cosmos client certificate."));

            return mostRecentlyIssuedCert;

        }

        /// <summary>
        /// Find latest cosmos stream, based on stream format
        /// </summary>
        /// <param name="streamFormat">Stream url. Use below templates to replace with corresponding values:
        /// {YYYY} - year
        /// {MM} - month
        /// {DD} - day.
        /// Example: /local/PDMSPrivate/PROD/PrivacyDeleteAuditor/PCFConfig_PPE/V2/{0:yyyy}/{0:MM}/PcfConfig_PPE_{0:yyyy}_{0:MM}_{0:dd}.ss
        /// </param>
        /// <param name="client"></param>
        /// <param name="maxAgeInDays"></param>
        /// <returns>Full path to the latest stream if exists, otherwise null.</returns>
        private static async Task<(string path, DateTimeOffset modifiedTime)?> GetLatestDailyCosmosStream(string streamFormat,
            ICosmosClient client, long maxAgeInDays)
        {
            int streamAgeDays = 0;
            DateTime currentUtcDateTime = DateTime.UtcNow;

            while (true)
            {
                if (streamAgeDays >= maxAgeInDays)
                {
                    return null;
                }

                var stream = string.Format(CultureInfo.InvariantCulture, streamFormat, currentUtcDateTime);

                if (await client.StreamExistsAsync(stream).ConfigureAwait(false))
                {
                    DateTimeOffset createdTime =
                        (await client.GetStreamInfoAsync(stream, true).ConfigureAwait(false)).CreateTime;

                    return (stream, createdTime);
                }

                currentUtcDateTime = currentUtcDateTime.AddDays(-1);
                streamAgeDays++;
            }
        }

        /// <summary>
        /// Find latest cosmos 6 hourly stream, based on stream format
        /// </summary>
        /// <param name="streamFormat">Stream url. Use below templates to replace with corresponding values:
        /// {YYYY} - year
        /// {MM} - month
        /// {DD} - day.
        /// {HH} - hour. (0, 6, 12, 18)
        /// Example: /local/PDMSPrivate/PROD/PrivacyDeleteAuditor/PCFConfig_PPE/V2/{0:yyyy}/{0:MM}/PcfConfig_PPE_{0:yyyy}_{0:MM}_{0:dd}T{0:HH}_00_00.ss
        /// </param>
        /// <param name="client"></param>
        /// <param name="maxAgeInDays"></param>
        /// <returns>Full path to the latest stream if exists, otherwise null.</returns>
        private static async Task<(string path, DateTimeOffset modifiedTime)?> GetLatestHourlyCosmosStream(string streamFormat,
            ICosmosClient client, long maxAgeInDays)
        {
            int streamAgeInHours = 0;
            DateTime currentUtcDateTime = DateTime.UtcNow;

            currentUtcDateTime = currentUtcDateTime.AddHours(-1 * currentUtcDateTime.Hour % 6);

            while (true)
            {
                if (streamAgeInHours >= maxAgeInDays * 24)
                {
                    return null;
                }

                var stream = string.Format(CultureInfo.InvariantCulture, streamFormat, currentUtcDateTime);

                if (await client.StreamExistsAsync(stream).ConfigureAwait(false))
                {
                    DateTimeOffset createdTime =
                        (await client.GetStreamInfoAsync(stream, true).ConfigureAwait(false)).CreateTime;

                    return (stream, createdTime);
                }

                currentUtcDateTime = currentUtcDateTime.AddHours(-6);
                streamAgeInHours += 6;
            }
        }

        private static async Task<ICosmosStructuredStreamReader> CreateAdlsStructuredStreamReader(string path, 
                        DateTimeOffset lastModifiedTime, ICosmosResourceFactory factory)
        {
            AdlsConfig config = await CreateAdlsConfig().ConfigureAwait(false);
            var reader = new AdlsCosmosStructuredStreamReader(path, lastModifiedTime, config,
                async () => await factory.GetAppToken(config).ConfigureAwait(false));
            await reader.InitializeAsync().ConfigureAwait(false);
            return reader;
        }

        private static async Task<AdlsConfig> CreateAdlsConfig()
        {
            X509Certificate2 mostRecentlyIssuedCert = await GetMostRecentCertFromKeyVaultWithName(Config.Instance.Adls.ClientAppCertificateName).ConfigureAwait(false);

            return new AdlsConfig(
                Config.Instance.Adls.AccountName,
                Config.Instance.Adls.ClientAppId,
                Config.Instance.Adls.AccountSuffix,
                Config.Instance.Adls.TenantId,
                mostRecentlyIssuedCert);
        }

        private static async Task<ICosmosClient> InitializeAdlsCosmosClient(ICosmosResourceFactory factory)
        {
            return factory.CreateCosmosAdlsClient(await CreateAdlsConfig().ConfigureAwait(false));
        }
    }
}
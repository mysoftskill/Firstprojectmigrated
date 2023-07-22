// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.EventHubProcessor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.KeyVault.Models;
    using Microsoft.Membership.MemberServices.Common.Azure;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.SecretStore;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Rest.Azure;

    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.Cloud.InstrumentationFramework;

    /// <summary>
    ///     A class for configuration extensions.
    /// </summary>
    public class EventHubHelpers : IEventHubHelpers
    {
        /// <summary>
        ///     The class name used for logging annotations.
        /// </summary>
        private const string ClassName = nameof(EventHubHelpers);

        private readonly IEventHubProcessorConfiguration eventHubProcessorConfig;

        private readonly Lazy<IKeyVaultClient> keyVault;

        private readonly ILogger logger;

        private readonly ISecretStoreReader secretStoreReader;

        private readonly AuditLogger auditLogger;

        private const string CallerName = "ADGCS PXS " + nameof(EventHubHelpers);

        private readonly string appId;

        public EventHubHelpers(IPrivacyConfigurationManager config, IAadAuthenticationHelper authHelper, ISecretStoreReader secretStoreReader, ILogger logger)
        {
            this.eventHubProcessorConfig = config.AadAccountCloseWorkerConfiguration.EventHubProcessorConfig ?? throw new ArgumentNullException(
                                               nameof(config.AadAccountCloseWorkerConfiguration.EventHubProcessorConfig));
            this.appId = config.AzureKeyVaultConfiguration?.AadAppId ?? CallerName;
            this.secretStoreReader = secretStoreReader ?? throw new ArgumentNullException(nameof(secretStoreReader));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.auditLogger = AuditLoggerFactory.CreateAuditLogger(this.logger);
            this.keyVault = new Lazy<IKeyVaultClient>(() => new KeyVaultClientInstrumented(new KeyVaultClient(authHelper.GetAccessTokenAsync)));
        }

        public async Task<string> GetAzureStorageConnectionStringAsync()
        {
            const string methodName = ClassName + "." + nameof(this.GetAzureStorageConnectionStringAsync) + "." + nameof(IAzureStorageConfiguration);
            const string ConnectionStringFormat = "DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1};EndpointSuffix={2}";

            const string ConnectionStringTraceNoAccountKeyFormat =
                "Storage connetion string: DefaultEndpointsProtocol=https;AccountName={0};AccountKey=<#REDACTED#>;EndpointSuffix={1}";

            ArgumentCheck.ThrowIfNull(this.logger, nameof(this.logger), null, methodName);
            ArgumentCheck.ThrowIfNull(this.secretStoreReader, nameof(this.secretStoreReader), null, methodName);

            if (!this.LeaseStorageConfig.UseEmulator)
            {
                this.logger.Information(methodName, "Using real Azure storage account.");
                try
                {
                    string accountKey = await this.secretStoreReader.ReadSecretByNameAsync(this.LeaseStorageConfig.AuthKeyEncryptedFilePath).ConfigureAwait(false);

                    this.logger.Information(
                        nameof(EventHubHelpers),
                        ConnectionStringTraceNoAccountKeyFormat,
                        this.LeaseStorageConfig.AccountName,
                        this.LeaseStorageConfig.StorageEndpointSuffix);

                    return ConnectionStringFormat.FormatInvariant(
                        this.LeaseStorageConfig.AccountName,
                        accountKey,
                        this.LeaseStorageConfig.StorageEndpointSuffix);
                }
                catch (Exception e)
                {
                    this.logger.Error(methodName, e, "Failed to read the account key secret.");
                    throw;
                }
            }

            // Local box only
            this.logger.Information(methodName, "Using development storage.");
            return "UseDevelopmentStorage=true;";
        }

        public async Task<IEnumerable<IConnectionInformation>> GetConnectionInformationsAsync()
        {
            IPage<SecretItem> items = await this.KeyVaultClient.GetSecretsAsync(this.eventHubProcessorConfig.EventHubConfig.VaultBaseUrl).ConfigureAwait(false);
            IEnumerable<ConnectionInformation> secrets = null;

            // By default, items will have up to 25 items per page, so we need to process all pages
            while (items.Any())
            {
                IEnumerable<ConnectionInformation> newSecrets = await Task.WhenAll(
                    items.Select(
                        async item =>
                        {
                            SecretBundle bundle = null;
                            var callerIdentities = new List<CallerIdentity> { new CallerIdentity(CallerIdentityType.ApplicationID, this.appId) };

                            try
                            {
                                bundle = await this.KeyVaultClient.GetSecretAsync(this.eventHubProcessorConfig.EventHubConfig.VaultBaseUrl, item.Identifier.Name)
                                    .ConfigureAwait(false);
                                var auditData = AuditData.BuildAccessToKeyVaultOperationAuditData(this.eventHubProcessorConfig.EventHubConfig.VaultBaseUrl, item.Identifier.Name, "Successfully retrieved secret from key vault.", CallerName, callerIdentities);
                                this.auditLogger.Log(auditData);
                            }
                            catch (Exception ex)
                            {
                                var auditData = AuditData.BuildAccessToKeyVaultOperationAuditData(this.eventHubProcessorConfig.EventHubConfig.VaultBaseUrl, item.Identifier.Name, $"Failed to retrieve secret from key vault. Exception is {ex.ToString()}", CallerName, callerIdentities, OperationResult.Failure);
                                this.auditLogger.Log(auditData);
                                throw;
                            }

                            this.logger.Information(nameof(EventHubHelpers), $"Connecting to event hub with identifier {item.Identifier.Name}");
                            return new ConnectionInformation(bundle.Value, item.Identifier.Name);
                        })).ConfigureAwait(false);

                // Just keep collecting all the secrets... yes...
                secrets = secrets?.Union(newSecrets) ?? newSecrets;

                if (!string.IsNullOrEmpty(items.NextPageLink))
                {
                    items = await this.KeyVaultClient.GetSecretsNextAsync(items.NextPageLink).ConfigureAwait(false);
                }
                else
                {
                    // We've reached the last page
                    break;
                }
            }

            // Secrets are in the form "Endpoint=ConnectionString;EntityPath=HubName;key=val"
            // values may also contain '='
            return secrets ?? throw new InvalidOperationException("Failed to get connection information from KeyVault");
        }

        private IKeyVaultClient KeyVaultClient => this.keyVault.Value;

        private IAzureStorageConfiguration LeaseStorageConfig => this.eventHubProcessorConfig.LeaseStorageConfig;
    }
}

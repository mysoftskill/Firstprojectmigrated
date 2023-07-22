namespace Microsoft.PrivacyServices.DataManagement.Common.Configuration
{
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.DataManagement.Client;

    /// <summary>
    ///     Interface for Privacy ConfigurationManager
    /// </summary>
    public interface IPrivacyConfigurationManager
    {
        /// <summary>
        ///     Gets the AAD Account close provider configuration
        /// </summary>
        IAzureActiveDirectoryProviderConfig AzureActiveDirectoryProviderConfig { get; }

        /// <summary>
        ///     Gets the TokenProvider configuration
        /// </summary>
        ITokenProviderConfig TokenProviderConfig { get; }

        /// <summary>
        ///     Gets the ServicePointManager configuration
        /// </summary>
        IServicePointManagerConfig ServicePointManagerConfig { get; }

        /// <summary>
        ///     Gets the DocumentDatabase configuration
        /// </summary>
        IDocumentDatabaseConfig DocumentDatabaseConfig { get; }

        /// <summary>
        /// Gets the AzureKeyVault Configuration
        /// </summary>
        IAzureKeyVaultConfig AzureKeyVaultConfig { get; }

        /// <summary>
        /// Gets the Sll Configuration
        /// </summary>
        ISllConfig SllConfig { get; }

        /// <summary>
        /// Gets the Kusto Client Configuration
        /// </summary>
        IKustoClientConfig KustoClientConfig { get; }

        /// <summary>
        /// Gets the ServiceTree Client Configuration
        /// </summary>
        IClientConfiguration ServiceTreeClientConfig { get; }

        /// <summary>
        /// Gets the Throttling Configuration
        /// </summary>
        IThrottlingConfiguration ThrottlingConfiguration { get; }

        /// <summary>
        /// Gets the Owin Configuration
        /// </summary>
        IOwinConfiguration OwinConfiguration { get; }

        /// <summary>
        /// Gets the Core Configuration
        /// </summary>
        ICoreConfiguration CoreConfiguration { get; }

        /// <summary>
        /// Gets the Azure App Configuration Settings
        /// </summary>
        IAzureAppConfigurationSettings AppConfigSettings { get; }

        /// <summary>
        /// Gets the DataGrid Configuration
        /// </summary>
        IDataGridConfiguration DataGridConfiguration { get; }

        /// <summary>
        /// Gets Icm Configuration
        /// </summary>
        IIcmConfiguration IcmConfiguration { get; }


        /// <summary>
        /// Gets ServiceTreeKusto Configuration
        /// </summary>
        IServiceTreeKustoConfiguration ServiceTreeKustoConfiguration { get; }

        /// <summary>
        /// Gets DataAccess Configuration
        /// </summary>
        IDataAccessConfiguration DataAccessConfiguration { get; }

        /// <summary>
        /// Gets ChangeFeedReader Lock Config
        /// </summary>
        ILockConfig LockConfig { get; }

        /// <summary>
        /// Gets ICloudStorageConfig
        /// </summary>
        ICloudStorageConfig CloudStorageConfig { get; }

        /// <summary>
        /// Gets ICloudQueueConfig
        /// </summary>
        ICloudQueueConfig CloudQueueConfig { get; }
    }
}

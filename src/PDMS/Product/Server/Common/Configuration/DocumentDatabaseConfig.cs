namespace Microsoft.PrivacyServices.DataManagement.Common.Configuration
{
    /// <summary>
    /// This is only used in DevBox env for overriding the collection Name
    /// </summary>
    public class DevBoxOnlyDocumentDatabaseConfig : IDocumentDatabaseConfig
    {
        public DevBoxOnlyDocumentDatabaseConfig(string databaseName, string entityCollectionName, string endpointUri, string keyVaultPrimaryKeyName)
        {
            DatabaseName = databaseName;
            EntityCollectionName = entityCollectionName;
            EndpointUri = endpointUri;
            KeyVaultPrimaryKeyName = keyVaultPrimaryKeyName;
        }

        public string DatabaseName { get; }

        public string EntityCollectionName { get; }

        public string EndpointUri { get; }

        public string KeyVaultPrimaryKeyName { get; }
    }
}

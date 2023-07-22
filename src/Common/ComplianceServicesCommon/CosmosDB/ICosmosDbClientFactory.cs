namespace Microsoft.PrivacyServices.CommandFeedv2.Common.Storage
{
    /// <summary>
    /// Factory for producing generic cosmos db clients.
    /// </summary>
    public interface ICosmosDbClientFactory
    {
        /// <summary>
        /// Get a Cosmos DB Client.
        /// </summary>
        /// <param name="containerName">the cosmos db container name.</param>
        /// <param name="databaseName">the cosmos db name.</param>
        /// <param name="dbEndpoint">the cosmos db endpoint.</param>
        ICosmosDbClient<T> GetCosmosDbClient<T>(string containerName, string databaseName, string dbEndpoint);
    }
}

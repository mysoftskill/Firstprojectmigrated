namespace Microsoft.PrivacyServices.CommandFeedv2.Common.Storage
{
    /// <summary>
    /// Factory for producing generic cosmos db clients.
    /// </summary>
    public class CosmosDbClientFactory : ICosmosDbClientFactory
    {
        /// <summary>
        /// Get a Cosmos DB Client.
        /// </summary>
        /// <param name="containerName">the cosmos db container name.</param>
        /// <param name="databaseName">the cosmos db name.</param>
        /// <param name="dbEndpoint">the cosmos db endpoint.</param>
        /// <param name="isOnebox">true if this is a onebox config.</param>
        public ICosmosDbClient<T> GetCosmosDbClient<T>(string containerName, string databaseName, string dbEndpoint)
        {
            return new CosmosDbClient<T>(containerName, databaseName, dbEndpoint);
        }
    }
}

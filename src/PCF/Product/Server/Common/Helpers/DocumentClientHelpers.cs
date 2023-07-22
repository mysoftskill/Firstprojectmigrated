
namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using System;

    /// <summary>
    /// Microsoft Azure Document Client Helpers
    /// </summary>
    public static class DocumentClientHelpers
    {
        /// <summary>
        /// Common helper method to create a ConnectionPolicy for DocumentClient
        /// </summary>
        /// <param name="maxRetryAttemptsOnThrottledRequests">The maximum number of retries in the case where the request fails because the Azure Cosmos DB service has applied rate limiting on the client.</param>
        /// <param name="maxRetryWaitTimeInSeconds">The maximum retry time in seconds for the Azure Cosmos DB service.</param>
        /// <param name="enableEndpointDiscovery">Flag to enable endpoint discovery for geo-replicated database accounts in the Azure Cosmos DB service.</param>
        /// <returns></returns>
        public static ConnectionPolicy CreateConnectionPolicy(
            int maxRetryAttemptsOnThrottledRequests = 9, 
            int maxRetryWaitTimeInSeconds = 30,
            bool enableEndpointDiscovery = true)
        {
            return new ConnectionPolicy
            {
                ConnectionMode = ConnectionMode.Direct,
                ConnectionProtocol = Protocol.Tcp,
                RetryOptions = new RetryOptions { 
                    MaxRetryAttemptsOnThrottledRequests = maxRetryAttemptsOnThrottledRequests, 
                    MaxRetryWaitTimeInSeconds = maxRetryWaitTimeInSeconds },
                EnableEndpointDiscovery = enableEndpointDiscovery,
            };
        }
    }
}

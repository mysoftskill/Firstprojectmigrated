namespace Microsoft.PrivacyServices.CommandFeed.Service.Autoscaler
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Common.Azure;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class CosmosDbClient : ISimpleCosmosDbCollectionClient
    {
        private readonly DocumentClient client;
        private readonly string databaseName;
        private readonly string collectionName;
        private readonly string accountName;
        private readonly IAadCredentialProvider credentialProvider;

        private int partitionCount;
        private string collectionSelfLink;

        private bool initialized;

        public CosmosDbClient(
            DocumentClient client, 
            IAadCredentialProvider credentialProvider,
            string accountName, 
            string databaseName, 
            string collectionName)
        {
            this.client = client;
            this.credentialProvider = credentialProvider;

            this.accountName = accountName;
            this.databaseName = databaseName;
            this.collectionName = collectionName;
        }

        public string FriendlyName => $"{this.accountName}.{this.databaseName}.{this.collectionName}";

        public async Task<int> GetCurrentThroughputAsync()
        {
            var offer = await this.GetCurrentOfferAsync();
            return offer.Content.OfferThroughput;
        }

        public async Task ReplaceThroughputAsync(int throughput)
        {
            var offer = await this.GetCurrentOfferAsync();
            var newOffer = new OfferV2(offer, throughput);
            await this.client.ReplaceOfferAsync(newOffer);
        }

        public async Task<int> GetPartitionCountAsync()
        {
            await this.InitializeAsync();
            return this.partitionCount;
        }

        private async Task<OfferV2> GetCurrentOfferAsync()
        {
            await this.InitializeAsync();

            using (var offerQuery = this.client.CreateOfferQuery().AsDocumentQuery())
            {
                while (offerQuery.HasMoreResults)
                {
                    var response = await offerQuery.ExecuteNextAsync<OfferV2>();
                    foreach (var item in response)
                    {
                        if (item.ResourceLink == this.collectionSelfLink)
                        {
                            return item;
                        }
                    }
                }
            }

            throw new InvalidOperationException("Unable to find offer!");
        }

        public async Task<ThrottleSummary> GetThrottleStatsAsync(DateTimeOffset endTime)
        {
            var recentStats = await this.GetThrottleStatsInTimeRangeAsync(endTime.AddMinutes(-2), endTime);
            var olderStats = await this.GetThrottleStatsInTimeRangeAsync(endTime.AddMinutes(-10), endTime);

            return new ThrottleSummary
            {
                TotalOlderRequests = olderStats.totalRequests,
                TotalOlderThrottledRequests = olderStats.throttledRequests,
                TotalRecentRequests = recentStats.totalRequests,
                TotalRecentThrottledRequests = recentStats.throttledRequests,
            };
        }

        private async Task<(long totalRequests, long throttledRequests)> GetThrottleStatsInTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime)
        {
            await this.InitializeAsync();

            string uri = null;
            uri = $"https://management.azure.com/subscriptions/{Config.Instance.AzureManagement.SubscriptionId}/resourceGroups/{Config.Instance.AzureManagement.ResourceGroupName}/providers/Microsoft.DocumentDb/";
            uri = $"{uri}databaseAccounts/{this.accountName}/providers/microsoft.insights/metrics";
            uri = $"{uri}?timespan={startTime:s}/{endTime:s}&interval=PT1M&metric=TotalRequests&aggregation=count&";
            uri = $"{uri}$filter=DatabaseName eq '{this.databaseName}' and CollectionName eq '{this.collectionName}' and StatusCode eq '*'&api-version=2017-05-01-preview";

            var creds = await this.credentialProvider.GetAzureManagementTokenCredentialsAsync();

            using (var httpRequest = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                await creds.ProcessHttpRequestAsync(httpRequest, default(CancellationToken));

                using (var client = new HttpClient())
                using (var response = await client.SendAsync(httpRequest))
                {
                    string body = await response.Content.ReadAsStringAsync();

                    DualLogger.Instance.Information(nameof(CosmosDbClient), $"Throttle stats response Code = {response.StatusCode}");
                    if (!response.IsSuccessStatusCode)
                    {
                        DualLogger.Instance.Error(nameof(CosmosDbClient), body);
                        throw new HttpRequestException($"Unexpected response code: {response.StatusCode}");
                    }

                    var result = JsonConvert.DeserializeObject<JObject>(body);

                    long totalRequests = 0;
                    long throttledRequests = 0;

                    // sum up all request counts across all status codes.
                    foreach (var token in result.SelectTokens("$..timeseries..data..count"))
                    {
                        totalRequests += token.Value<int>();
                    }

                    // sum up only throttled requests (statuscode == '429').
                    foreach (var token in result.SelectTokens("$..timeseries[?(@.metadatavalues[*].value == '429')]..data..count"))
                    {
                        throttledRequests += token.Value<int>();
                    }

                    return (totalRequests, throttledRequests);
                }
            }
        }

        private async Task InitializeAsync()
        {
            if (this.initialized)
            {
                return;
            }

            var collection = await this.client.ReadDocumentCollectionAsync(
                UriFactory.CreateDocumentCollectionUri(this.databaseName, this.collectionName), 
                new RequestOptions { PopulatePartitionKeyRangeStatistics = true });
            
            this.collectionSelfLink = collection.Resource.SelfLink;
            this.partitionCount = collection.Resource.PartitionKeyRangeStatistics.Count;

            this.initialized = true;
        }
    }
}

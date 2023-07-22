namespace Microsoft.PrivacyServices.CommandFeed.Service.Autoscaler
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// The CosmosDb autoscaler.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    public sealed class Program : PrivacyApplication
    {
        private Program() : base(CommandFeedService.Autoscaler)
        {
        }

        protected override void OnStart()
        {
            base.OnStart();

            DualLogger.Instance.Information(nameof(Program), "Starting CosmosDb Autoscaler");

            Dictionary<string, DocumentClient> cosmosDbClients = new Dictionary<string, DocumentClient>();
            foreach (var item in Config.Instance.CosmosDBQueues.Instances)
            {
                if (!cosmosDbClients.ContainsKey(item.Uri))
                {
                    cosmosDbClients[item.Uri] = new DocumentClient(
                        new Uri(item.Uri),
                        item.Key);
                }
            }

            var connectionPolicy = DocumentClientHelpers.CreateConnectionPolicy(maxRetryAttemptsOnThrottledRequests: 5);

            cosmosDbClients[Config.Instance.CommandHistory.Uri] = new DocumentClient(
                new Uri(Config.Instance.CommandHistory.Uri),
                Config.Instance.CommandHistory.Key,
                connectionPolicy);

            cosmosDbClients[Config.Instance.PdmsCache.DocumentDb.DatabaseUri.AbsoluteUri] = new DocumentClient(
                Config.Instance.PdmsCache.DocumentDb.DatabaseUri,
                Config.Instance.PdmsCache.DocumentDb.DatabaseKey,
                connectionPolicy);

            cosmosDbClients[Config.Instance.CommandReplay.Repository.Uri] = new DocumentClient(
                new Uri(Config.Instance.CommandReplay.Repository.Uri),
                Config.Instance.CommandReplay.Repository.Key,
                connectionPolicy);

            AadCredentialProvider credentialProvider = new AadCredentialProvider();

            var redisConnection = new RedisConnection(
                "CosmosDb Autoscaler",
                Config.Instance.AzureManagement.RedisCacheEndpoint,
                (int)Config.Instance.AzureManagement.RedisCachePort,
                Config.Instance.AzureManagement.RedisCachePassword,
                DualLogger.Instance);

            var redisClient = new RedisClient(redisConnection, DualLogger.Instance);

            List<ScaledCollection> scaledCollections = new List<ScaledCollection>();
            foreach (var pair in cosmosDbClients)
            {
                string accountName = new Uri(pair.Key).Host.Split('.')[0];

                // read all databases
                foreach (var database in pair.Value.CreateDatabaseQuery())
                {
                    // read all collections
                    foreach (var collection in pair.Value.CreateDocumentCollectionQuery(UriFactory.CreateDatabaseUri(database.Id)))
                    {
                        var cosmosDbClient = new CosmosDbClient(
                            pair.Value,
                            credentialProvider,
                            accountName,
                            database.Id,
                            collection.Id);

                        scaledCollections.Add(new ScaledCollection(cosmosDbClient, redisClient, Config.Instance.CosmosDbAutoscaler));
                    }
                }
            }

            foreach (var collection in scaledCollections)
            {
                this.AddTask(collection.ContinuallyScaleAsync(this.CancellationToken));
            }
        }

        public static void Main(string[] args)
        {
            Program program = new Program();
            program.Run(args);
        }
    }
}

namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandReplay
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    /// <summary>
    /// DocDB implementation of command replay job repository
    /// </summary>
    public class CommandReplayJobRepository : ICommandReplayJobRepository
    {
        private const string DatabaseName = "CommandReplayDb";
        private const string CollectionName = "ReplayJob";

        private readonly Uri databaseUri;
        private readonly Uri collectionUri;

        private readonly DocumentClient documentClient;

        /// <summary>
        /// Creates the repository with default settings.
        /// </summary>
        public CommandReplayJobRepository()
        {
            this.documentClient = new DocumentClient(
                new Uri(Config.Instance.CommandReplay.Repository.Uri),
                Config.Instance.CommandReplay.Repository.Key,
                DocumentClientHelpers.CreateConnectionPolicy(maxRetryAttemptsOnThrottledRequests: 5),
                ConsistencyLevel.Strong);

            this.databaseUri = UriFactory.CreateDatabaseUri(DatabaseName);
            this.collectionUri = UriFactory.CreateDocumentCollectionUri(DatabaseName, CollectionName);
        }

        /// <inheritdoc />
        public async Task InitializeAsync()
        {
            // create the database, if appropriate
            await this.documentClient.CreateDatabaseIfNotExistsAsync(new Database { Id = DatabaseName });

            // Create the collection, if appropriate.
            var collection = new DocumentCollection
            {
                Id = CollectionName,
                DefaultTimeToLive = (int)TimeSpan.FromDays(Config.Instance.CommandReplay.Repository.TimeToLiveDays).TotalSeconds
            };

            await this.documentClient.CreateDocumentCollectionIfNotExistsAsync(
                this.databaseUri,
                collection,
                new RequestOptions
                {
                    OfferEnableRUPerMinuteThroughput = true,
                    OfferThroughput = Config.Instance.CommandReplay.Repository.DefaultRUProvisioning
                });
        }

        /// <inheritdoc />
        public async Task InsertAsync(ReplayJobDocument replayJob)
        {
            replayJob.TimeToLive = DateTimeHelper.GetTimeToLiveSeconds(replayJob.CreatedTime.AddDays(Config.Instance.CommandReplay.Repository.TimeToLiveDays));

            await this.documentClient.InstrumentedCreateDocumentAsync(
                this.collectionUri,
                DatabaseName,
                CollectionName,
                replayJob,
                expectConflicts: true);
        }

        public async Task<ReplayJobDocument> PopNextItemAsync(TimeSpan leaseDuration)
        {
            var sqlQuery = new SqlQuerySpec(
                $"SELECT TOP 1 * FROM c WHERE c.nvt < @nvt",
                new SqlParameterCollection
                {
                    new SqlParameter { Name = "@nvt", Value = DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
                });

            var query = this.documentClient
                .CreateDocumentQuery<ReplayJobDocument>(this.collectionUri, sqlQuery)
                .AsDocumentQuery();

            var replayJob = await query.InstrumentedExecuteNextAsync<ReplayJobDocument>(DatabaseName, CollectionName);

            if (replayJob.items.Count == 0)
            {
                return null;
            }
            else
            {
                var jobRecord = replayJob.items.First();
                jobRecord.SetPropertyValue("nvt", DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (int)leaseDuration.TotalSeconds);

                try
                {
                    await this.ReplaceAsync(jobRecord, jobRecord.ETag);
                }
                catch (CommandFeedException ex)
                {
                    if (ex.ErrorCode == CommandFeedInternalErrorCode.Conflict)
                    {
                        return null;
                    }

                    throw;
                }

                var newJobRecord = await this.QueryAsync(jobRecord.Id);
                return newJobRecord;
            }
        }

        /// <inheritdoc />
        public async Task<ReplayJobDocument> QueryAsync(string jobId)
        {
            ReplayJobDocument replayJob = await this.documentClient.InstrumentedReadDocumentAsync<ReplayJobDocument>(
                UriFactory.CreateDocumentUri(DatabaseName, CollectionName, jobId),
                DatabaseName,
                CollectionName,
                expectNotFound: true);

            return replayJob;
        }

        /// <inheritdoc />
        public async Task<string> ReplaceAsync(ReplayJobDocument replayJob, string etag)
        {
            if (string.IsNullOrWhiteSpace(etag))
            {
                throw new ArgumentException("Etag must have a value!", nameof(etag));
            }

            var requestOptions = new RequestOptions
            {
                AccessCondition = new AccessCondition
                {
                    Type = AccessConditionType.IfMatch,
                    Condition = etag,
                }
            };

            replayJob.TimeToLive = DateTimeHelper.GetTimeToLiveSeconds(replayJob.CreatedTime.AddDays(Config.Instance.CommandReplay.Repository.TimeToLiveDays));

            var response = await this.documentClient.InstrumentedReplaceDocumentAsync(
                UriFactory.CreateDocumentUri(DatabaseName, CollectionName, replayJob.Id),
                DatabaseName,
                CollectionName,
                replayJob,
                expectConflicts: true,
                requestOptions: requestOptions);

            return response.ETag;
        }
    }
}

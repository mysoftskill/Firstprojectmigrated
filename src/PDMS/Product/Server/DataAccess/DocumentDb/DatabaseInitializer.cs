namespace Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;

    /// <summary>
    /// Implements initialization logic for Document DB.
    /// </summary>
    public class DatabaseInitializer
    {
        private static readonly Random RandomGenerator = new Random();
        private readonly string databaseName;
        private readonly string collectionName;
        private readonly int offerThroughput;
        private readonly IDocumentClient documentClient;
        private readonly IStoredProcedureProvider storedProcedureProvider;
        private readonly Trace trace;

        private readonly string componentName = nameof(DatabaseInitializer);

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseInitializer"/> class.
        /// Initializes the document database service in Azure storage.
        /// </summary>
        /// <param name="setupProperties">The setup properties.</param>
        /// <param name="documentClient">The client for talking to Document DB.</param>
        /// <param name="storedProcedureProvider">An optional provider for installing stored procedures.</param>
        /// <param name="trace">An optional delegate to trace debug messages.</param>
        public DatabaseInitializer(
            SetupProperties setupProperties, 
            IDocumentClient documentClient, 
            IStoredProcedureProvider storedProcedureProvider = null,
            Trace trace = null)
        {
            this.databaseName = setupProperties.DatabaseName;
            this.collectionName = setupProperties.CollectionName;
            this.offerThroughput = setupProperties.OfferThroughput;
            this.documentClient = documentClient;
            this.storedProcedureProvider = storedProcedureProvider;
            this.trace = trace;
        }

        /// <summary>
        /// A delegate to trace debug information.
        /// </summary>
        /// <param name="componentName">Component name</param>
        /// <param name="message">The information to trace.</param>
        public delegate void Trace(string componentName, string message);

        /// <summary>
        /// Initializes the database.
        /// </summary>
        /// <returns>A task that executes the initialization.</returns>
        public async Task InitializeAsync()
        {
            await this.CreateDatabaseAsync().ConfigureAwait(false);

            await this.CreateCollectionAsync().ConfigureAwait(false);

            if (this.storedProcedureProvider != null)
            {
                await Task.WhenAll(this.storedProcedureProvider.GetStoredProcedures().Select(s => this.CreateStoredProcedureAsync(s))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Creates a new stored procedure.
        /// </summary>
        /// <param name="storedProcedure">The stored procedure to create.</param>
        /// <returns>A task to complete the execution asynchronously.</returns>
        protected virtual Task CreateStoredProcedureAsync(StoredProcedure storedProcedure)
        {
            var storedProcedureUrl = UriFactory.CreateStoredProcedureUri(this.databaseName, this.collectionName, storedProcedure.Name);

            return this.ExecuteReliably(async () =>
            {
                try
                {
                    await this.documentClient.ReadStoredProcedureAsync(storedProcedureUrl).ConfigureAwait(false);
                    this.trace?.Invoke(
                        componentName,
                        $"Stored procedure already exists: {this.databaseName}.");

                    if (storedProcedure.Action == StoredProcedure.Actions.Remove)
                    {
                        await this.documentClient.DeleteStoredProcedureAsync(storedProcedureUrl).ConfigureAwait(false);
                        this.trace?.Invoke(
                            componentName,
                            $"Stored procedure deleted: {this.databaseName}.");
                    }
                }
                catch (DocumentClientException exn)
                {
                    // If the stored procedure does not exist, create a new stored procedure.
                    if (exn.StatusCode == HttpStatusCode.NotFound)
                    {
                        if (storedProcedure.Action == StoredProcedure.Actions.Install)
                        {
                            var storedProc = new Azure.Documents.StoredProcedure
                            {
                                Id = storedProcedure.Name,
                                Body = storedProcedure.Value
                            };

                            var collectionUrl = UriFactory.CreateDocumentCollectionUri(this.databaseName, this.collectionName);

                            try
                            {
                                await this.documentClient.CreateStoredProcedureAsync(collectionUrl, storedProc, null).ConfigureAwait(false);
                                this.trace?.Invoke(
                                    componentName,
                                    $"Stored procedure created: {storedProcedure.Name}.");
                            }
                            catch (DocumentClientException exn2)
                            {
                                // Due to a race condition in the test startup, multiple tests
                                // might try to created the stored procedure at the same time;
                                // if it's already created, then move on
                                if (exn2.StatusCode != HttpStatusCode.Conflict)
                                {
                                    throw;
                                }

                                this.trace?.Invoke(
                                    componentName,
                                    $"Stored Procedure already created: {storedProcedure.Name}.");
                            }
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
            });
        }

        /// <summary>
        /// Creates a new database.
        /// </summary>
        /// <returns>A task to complete the execution asynchronously.</returns>
        protected virtual Task CreateDatabaseAsync()
        {
            return this.ExecuteReliably(async () =>
            {
                try
                {
                    await this.documentClient.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(this.databaseName)).ConfigureAwait(false);
                    this.trace?.Invoke(
                        componentName,
                        $"Database already exists: {this.databaseName}.");
                }
                catch (DocumentClientException exn)
                {
                    // If the database does not exist, create a new database.
                    if (exn.StatusCode == HttpStatusCode.NotFound)
                    {
                        try
                        {
                            await this.documentClient.CreateDatabaseAsync(new Database { Id = this.databaseName }).ConfigureAwait(false);
                            this.trace?.Invoke(
                                componentName,
                                $"Database created: {this.databaseName}.");
                        }
                        catch (DocumentClientException exn2)
                        {
                            // Due to a race condition in the test startup, multiple tests
                            // might try to create the database at the same time;
                            // if it's already created, then move on
                            if (exn2.StatusCode != HttpStatusCode.Conflict)
                            {
                                throw;
                            }

                            this.trace?.Invoke(
                                componentName,
                                $"Database already created: {this.databaseName}.");
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
            });
        }

        /// <summary>
        /// Creates a collection in the database.
        /// </summary>
        /// <returns>A task to complete the execution asynchronously.</returns>
        protected virtual Task CreateCollectionAsync()
        {
            return this.ExecuteReliably(async () =>
            {
                try
                {
                    await this.documentClient.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(this.databaseName, this.collectionName)).ConfigureAwait(false);
                    this.trace?.Invoke(
                        componentName,
                        $"Collection already exists: {this.collectionName}.");
                }
                catch (DocumentClientException exn)
                {
                    if (exn.StatusCode == HttpStatusCode.NotFound)
                    {
                        DocumentCollection collectionInfo = new DocumentCollection
                        {
                            Id = this.collectionName,

                            // Configure collections for maximum query flexibility including string range queries.
                            IndexingPolicy = new IndexingPolicy(new RangeIndex(DataType.String) { Precision = -1 })
                        };

                        // Here we create a collection with specific RU/s.
                        try
                        {
                            await this.documentClient.CreateDocumentCollectionAsync(
                                UriFactory.CreateDatabaseUri(this.databaseName),
                                collectionInfo,
                                new RequestOptions { OfferThroughput = this.offerThroughput }).ConfigureAwait(false);

                            this.trace?.Invoke(
                                componentName,
                                $"Collection created: {this.collectionName}.");
                        }
                        catch (DocumentClientException exn2)
                        {
                            // Due to a race condition in the test startup, multiple tests
                            // might try to create the collection at the same time;
                            // if it's already created, then move on
                            if (exn2.StatusCode != HttpStatusCode.Conflict)
                            {
                                throw;
                            }

                            this.trace?.Invoke(
                                componentName,
                                $"Collection already created: {this.collectionName}.");
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
            });
        }

        /// <summary>
        /// Executes a function in a reliable manner. This is necessary to avoid issues with parallel execution.
        /// It is rare that we could hit this in production, but our test automation does hit this.
        /// </summary>
        /// <param name="function">A function to run reliably.</param>
        /// <returns>A task to execute the function.</returns>
        protected async Task ExecuteReliably(Func<Task> function)
        {
            bool success = false;
            int attempts = 10;

            do
            {
                try
                {
                    await function().ConfigureAwait(false);
                    success = true;
                }
                catch (DocumentClientException de)
                {
                    // if this is not RequestRateTooLargeException,
                    // or we have tried too many times, then throw
                    if (de.StatusCode == null || (int)de.StatusCode != 429 || --attempts <= 0)
                    {
                        throw;
                    }

                    // Retry after the x-ms-retry-after-ms value sent back from the server
                    System.Threading.Thread.Sleep(de.RetryAfter);
                }
                catch (AggregateException ae)
                {
                    // if this is not RequestRateTooLargeException,
                    // or we have tried too many times, then throw
                    if (!(ae.InnerException is DocumentClientException))
                    {
                        throw;
                    }

                    var de = ae.InnerException as DocumentClientException;
                    if (de?.StatusCode == null || (int)de.StatusCode != 429 || --attempts <= 0)
                    {
                        throw;
                    }

                    // Retry after the x-ms-retry-after-ms value sent back from the server
                    System.Threading.Thread.Sleep(de.RetryAfter);
                } 
                catch
                {
                    // Keep old behaviour for any other exceptions (needed for feature tests)
                    if (--attempts <= 0)
                    {
                        throw;
                    }

                    int delay = RandomGenerator.Next(2000);
                    System.Threading.Thread.Sleep(delay);
                }
            }
            while (!success);
        }
    }
}

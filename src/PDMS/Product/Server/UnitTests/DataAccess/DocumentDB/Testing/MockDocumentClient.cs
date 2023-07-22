namespace Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Security;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;

    using Moq;

    /// <summary>
    /// A mock version of the document client that reads/writes to in-memory data structures.
    /// This is used to test a minimal set of high priority scenarios as part of the unit test infrastructure.
    /// It is not designed to be a high fidelity emulator of the document DB client.
    /// </summary>
    public class MockDocumentClient : IDocumentClient
    {
        private IDictionary<string, IDictionary<string, Document>> storage;
        private DocumentCollection collection;
        private Database database;
        private readonly Mock<IDocumentClient> documentClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockDocumentClient" /> class.
        /// Registers the set of supported mock behaviors.
        /// </summary>
        /// <param name="originalClient">The original client.</param>
        public MockDocumentClient(IDocumentClient originalClient)
        {
            this.documentClient = new Mock<IDocumentClient>();
            this.MockClient = this.documentClient.Object;
            this.OriginalClient = originalClient;
        }

        #region Mock Document Client
        public Mock<IDocumentClient> DocumentClient
        {
            get
            {
                return this.documentClient;
            }
        }

        #endregion

        #region Default Properties
        /// <summary>
        /// Gets or sets the session.
        /// </summary>
        public object Session
        {
            get
            {
                return this.MockClient.Session;
            }

            set
            {
                this.MockClient.Session = value;
            }
        }

        /// <summary>
        /// Gets the service endpoint.
        /// </summary>
        public Uri ServiceEndpoint
        {
            get
            {
                return this.MockClient.ServiceEndpoint;
            }
        }

        /// <summary>
        /// Gets the write endpoint.
        /// </summary>
        public Uri WriteEndpoint
        {
            get
            {
                return this.MockClient.WriteEndpoint;
            }
        }

        /// <summary>
        /// Gets the read endpoint.
        /// </summary>
        public Uri ReadEndpoint
        {
            get
            {
                return this.MockClient.ReadEndpoint;
            }
        }

        /// <summary>
        /// Gets the connection policy.
        /// </summary>
        public ConnectionPolicy ConnectionPolicy
        {
            get
            {
                return this.MockClient.ConnectionPolicy;
            }
        }

        /// <summary>
        /// Gets the authentication key.
        /// </summary>
        public SecureString AuthKey
        {
            get
            {
                return this.MockClient.AuthKey;
            }
        }

        /// <summary>
        /// Gets the consistency level.
        /// </summary>
        public ConsistencyLevel ConsistencyLevel
        {
            get
            {
                return this.MockClient.ConsistencyLevel;
            }
        }
        #endregion

        private IDocumentClient MockClient { get; set; }

        private IDocumentClient OriginalClient { get; set; }

        #region Mock Implementations
        /// <summary>
        /// Create the database.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="options">The options.</param>
        /// <returns>The response.</returns>
        public Task<ResourceResponse<Database>> CreateDatabaseAsync(Database database, RequestOptions options = null)
        {
            this.storage = new Dictionary<string, IDictionary<string, Document>>();
            this.database = database;
            var response = new ResourceResponse<Database>(this.database);
            return Task.FromResult(response);
        }

        /// <summary>
        /// Read the database.
        /// </summary>
        /// <param name="databaseUri">The database uri.</param>
        /// <param name="options">The options.</param>
        /// <returns>The response.</returns>
        public Task<ResourceResponse<Database>> ReadDatabaseAsync(Uri databaseUri, RequestOptions options = null)
        {
            if (this.database == null)
            {
                throw DocumentClientExceptionModule.Create(HttpStatusCode.NotFound);
            }
            else
            {
                var response = new ResourceResponse<Database>(this.database);
                return Task.FromResult(response);
            }
        }

        /// <summary>
        /// Creates the document collection.
        /// </summary>
        /// <param name="databaseUri">The database uri.</param>
        /// <param name="documentCollection">The document collection.</param>
        /// <param name="options">The options.</param>
        /// <returns>The response.</returns>
        public Task<ResourceResponse<DocumentCollection>> CreateDocumentCollectionAsync(Uri databaseUri, DocumentCollection documentCollection, RequestOptions options = null)
        {
            this.storage.Add(documentCollection.Id, new Dictionary<string, Document>());
            this.collection = documentCollection;
            var response = new ResourceResponse<DocumentCollection>(this.collection);
            return Task.FromResult(response);
        }

        /// <summary>
        /// Reads the document collection.
        /// </summary>
        /// <param name="documentCollectionUri">The document collection uri.</param>
        /// <param name="options">The options.</param>
        /// <returns>The response.</returns>
        public Task<ResourceResponse<DocumentCollection>> ReadDocumentCollectionAsync(Uri documentCollectionUri, RequestOptions options = null)
        {
            if (this.collection == null)
            {
                throw DocumentClientExceptionModule.Create(HttpStatusCode.NotFound);
            }
            else
            {
                var response = new ResourceResponse<DocumentCollection>(this.collection);
                return Task.FromResult(response);
            }
        }

        /// <summary>
        /// Creates a document.
        /// </summary>
        /// <param name="documentCollectionUri">The document collection uri.</param>
        /// <param name="document">The document.</param>
        /// <param name="options">The options.</param>
        /// <param name="disableAutomaticIdGeneration">This value is not used.</param>
        /// <returns>The response.</returns>
        public Task<ResourceResponse<Document>> CreateDocumentAsync(Uri documentCollectionUri, object document, RequestOptions options = null, bool disableAutomaticIdGeneration = false)
        {
            var mockDocument = DocumentModule.Create(document);
            var id = mockDocument.GetPropertyValue<string>("id");
            var key = $"{documentCollectionUri.ToString()}/docs/{id}";
            this.storage[this.collection.Id].Add(key, mockDocument);
            return this.LoadResponseAsync(key);
        }

        /// <summary>
        /// Reads the document.
        /// </summary>
        /// <param name="documentUri">The document uri.</param>
        /// <param name="options">The options.</param>
        /// <returns>The response.</returns>
        public Task<ResourceResponse<Document>> ReadDocumentAsync(Uri documentUri, RequestOptions options = null)
        {
            return this.LoadResponseAsync(documentUri.ToString());
        }

        /// <summary>
        /// Deletes a document.
        /// </summary>
        /// <param name="documentUri">The document uri.</param>
        /// <param name="options">The options.</param>
        /// <returns>The response.</returns>
        public Task<ResourceResponse<Document>> DeleteDocumentAsync(Uri documentUri, RequestOptions options = null)
        {
            // TODO: I'm not sure if this is the actual behavior for document DB.
            var key = documentUri.ToString();
            var response = this.LoadResponseAsync(key);
            this.storage[this.collection.Id].Remove(key);
            return response;
        }

        /// <summary>
        /// Replaces a document.
        /// </summary>
        /// <param name="documentUri">The document uri.</param>
        /// <param name="document">The document.</param>
        /// <param name="options">The options.</param>
        /// <returns>The response.</returns>
        public Task<ResourceResponse<Document>> ReplaceDocumentAsync(Uri documentUri, object document, RequestOptions options = null)
        {
            var mockDocument = DocumentModule.Create(document);
            var id = mockDocument.GetPropertyValue<string>("id");
            var key = documentUri.ToString();

            if (options.AccessCondition.Type == AccessConditionType.IfMatch && !this.storage[this.collection.Id][key].ETag.Equals(options.AccessCondition.Condition))
            {
                throw DocumentClientExceptionModule.Create(HttpStatusCode.PreconditionFailed);
            }

            this.storage[this.collection.Id][key] = mockDocument;
            return this.LoadResponseAsync(key);
        }

        /// <summary>
        /// Executes a stored procedure. This only mimics the behavior of the <c>BulkUpsert</c> procedure. 
        /// For all others it does nothing.
        /// </summary>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <param name="storedProcedureUri">The procedure URI.</param>
        /// <param name="procedureParams">The parameters.</param>
        /// <returns>The result.</returns>
        public Task<StoredProcedureResponse<TValue>> ExecuteStoredProcedureAsync<TValue>(Uri storedProcedureUri, params dynamic[] procedureParams)
        {
            if (storedProcedureUri.ToString().Contains("V1.BulkUpsert"))
            {
                var upserts = (procedureParams[2] as IEnumerable<object>).Select(x => DocumentModule.Create(x)).ToArray();
                var deletes = (procedureParams[3] as IEnumerable<object>).Select(x => DocumentModule.Create(x)).ToArray();

                foreach (var upsert in upserts)
                {
                    var key = $"dbs/{procedureParams[0]}/colls/{procedureParams[1]}/docs/{this.GetId(upsert)}";
                    this.storage[this.collection.Id][key] = upsert;
                }

                foreach (var delete in deletes)
                {
                    var key = $"dbs/{procedureParams[0]}/colls/{procedureParams[1]}/docs/{this.GetId(delete)}";
                    this.storage[this.collection.Id].Remove(key);
                }

                return Task.FromResult(StoredProcedureResponseModule.Create<TValue>((TValue)(object)upserts, new System.Collections.Specialized.NameValueCollection()));
            }
            else
            {
                return this.MockClient.ExecuteStoredProcedureAsync<TValue>(storedProcedureUri, procedureParams);
            }
        }
        #endregion

        #region Official Implementations
        /// <summary>
        /// Creates a document query.
        /// </summary>
        /// <typeparam name="T">The response type.</typeparam>
        /// <param name="documentCollectionUri">The document collection uri.</param>
        /// <param name="feedOptions">The options.</param>
        /// <returns>The query.</returns>
        public IOrderedQueryable<T> CreateDocumentQuery<T>(Uri documentCollectionUri, FeedOptions feedOptions = null)
        {
            // This must use the original client. 
            // It is not possible to mock the query creation, 
            // but this won't actually call out to document db.
            return this.OriginalClient.CreateDocumentQuery<T>(documentCollectionUri, feedOptions);
        }
        #endregion

        #region Default Implementation
        #pragma warning disable 1591
        public Task<ResourceResponse<Attachment>> CreateAttachmentAsync(string documentLink, object attachment, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.MockClient.CreateAttachmentAsync(documentLink, attachment, options, cancellationToken);
        }

        public Task<ResourceResponse<Attachment>> CreateAttachmentAsync(string documentLink, Stream mediaStream, MediaOptions options = null, RequestOptions requestOptions = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.MockClient.CreateAttachmentAsync(documentLink, mediaStream, options, requestOptions, cancellationToken);
        }

        public Task<ResourceResponse<Attachment>> CreateAttachmentAsync(Uri documentUri, Stream mediaStream, MediaOptions options = null, RequestOptions requestOptions = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.MockClient.CreateAttachmentAsync(documentUri, mediaStream, options, requestOptions, cancellationToken);
        }

        public Task<ResourceResponse<Attachment>> CreateAttachmentAsync(Uri documentUri, object attachment, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.MockClient.CreateAttachmentAsync(documentUri, attachment, options, cancellationToken);
        }

        public Task<ResourceResponse<Database>> CreateDatabaseIfNotExistsAsync(Database database, RequestOptions options = null)
        {
            return this.MockClient.CreateDatabaseIfNotExistsAsync(database, options);
        }

        public IDocumentQuery<Document> CreateDocumentChangeFeedQuery(string collectionLink, ChangeFeedOptions feedOptions)
        {
            return this.MockClient.CreateDocumentChangeFeedQuery(collectionLink, feedOptions);
        }

        public IDocumentQuery<Document> CreateDocumentChangeFeedQuery(Uri collectionLink, ChangeFeedOptions feedOptions)
        {
            return this.MockClient.CreateDocumentChangeFeedQuery(collectionLink, feedOptions);
        }

        public Task<ResourceResponse<DocumentCollection>> CreateDocumentCollectionIfNotExistsAsync(Uri databaseUri, DocumentCollection documentCollection, RequestOptions options = null)
        {
            return this.MockClient.CreateDocumentCollectionIfNotExistsAsync(databaseUri, documentCollection, options);
        }

        public Task<ResourceResponse<DocumentCollection>> CreateDocumentCollectionIfNotExistsAsync(string databaseLink, DocumentCollection documentCollection, RequestOptions options = null)
        {
            return this.MockClient.CreateDocumentCollectionIfNotExistsAsync(databaseLink, documentCollection, options);
        }

        public Task<ResourceResponse<Document>> CreateDocumentAsync(string collectionLink, object document, RequestOptions options = null, bool disableAutomaticIdGeneration = false)
        {
            return this.MockClient.CreateDocumentAsync(collectionLink, document, options, disableAutomaticIdGeneration);
        }

        public Task<ResourceResponse<Document>> CreateDocumentAsync(string collectionLink, object document, RequestOptions options = null, bool disableAutomaticIdGeneration = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.MockClient.CreateDocumentAsync(collectionLink, document, options, disableAutomaticIdGeneration, cancellationToken);
        }

        public Task<ResourceResponse<Document>> CreateDocumentAsync(Uri documentCollectionUri, object document, RequestOptions options = null, bool disableAutomaticIdGeneration = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.MockClient.CreateDocumentAsync(documentCollectionUri, document, options, disableAutomaticIdGeneration, cancellationToken);
        }

        public Task<ResourceResponse<StoredProcedure>> CreateStoredProcedureAsync(string collectionLink, StoredProcedure storedProcedure, RequestOptions options = null)
        {
            return this.MockClient.CreateStoredProcedureAsync(collectionLink, storedProcedure, options);
        }

        public Task<ResourceResponse<Trigger>> CreateTriggerAsync(string collectionLink, Trigger trigger, RequestOptions options = null)
        {
            return this.MockClient.CreateTriggerAsync(collectionLink, trigger, options);
        }

        public Task<ResourceResponse<UserDefinedFunction>> CreateUserDefinedFunctionAsync(string collectionLink, UserDefinedFunction function, RequestOptions options = null)
        {
            return this.MockClient.CreateUserDefinedFunctionAsync(collectionLink, function, options);
        }

        public Task<ResourceResponse<User>> CreateUserAsync(string databaseLink, User user, RequestOptions options = null)
        {
            return this.MockClient.CreateUserAsync(databaseLink, user, options);
        }

        public Task<ResourceResponse<Permission>> CreatePermissionAsync(string userLink, Permission permission, RequestOptions options = null)
        {
            return this.MockClient.CreatePermissionAsync(userLink, permission, options);
        }

        public Task<ResourceResponse<Attachment>> DeleteAttachmentAsync(string attachmentLink, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.MockClient.DeleteAttachmentAsync(attachmentLink, options, cancellationToken);
        }

        public Task<ResourceResponse<Attachment>> DeleteAttachmentAsync(Uri attachmentUri, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.MockClient.DeleteAttachmentAsync(attachmentUri, options, cancellationToken);
        }

        public Task<ResourceResponse<Database>> DeleteDatabaseAsync(string databaseLink, RequestOptions options = null)
        {
            return this.MockClient.DeleteDatabaseAsync(databaseLink, options);
        }

        public Task<ResourceResponse<DocumentCollection>> DeleteDocumentCollectionAsync(string documentCollectionLink, RequestOptions options = null)
        {
            return this.MockClient.DeleteDocumentCollectionAsync(documentCollectionLink, options);
        }

        public Task<ResourceResponse<Document>> DeleteDocumentAsync(string documentLink, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.MockClient.DeleteDocumentAsync(documentLink, options, cancellationToken);
        }

        public Task<ResourceResponse<Document>> DeleteDocumentAsync(Uri documentUri, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.MockClient.DeleteDocumentAsync(documentUri, options, cancellationToken);
        }

        public Task<ResourceResponse<StoredProcedure>> DeleteStoredProcedureAsync(string storedProcedureLink, RequestOptions options = null)
        {
            return this.MockClient.DeleteStoredProcedureAsync(storedProcedureLink, options);
        }

        public Task<ResourceResponse<Trigger>> DeleteTriggerAsync(string triggerLink, RequestOptions options = null)
        {
            return this.MockClient.DeleteTriggerAsync(triggerLink, options);
        }

        public Task<ResourceResponse<UserDefinedFunction>> DeleteUserDefinedFunctionAsync(string functionLink, RequestOptions options = null)
        {
            return this.MockClient.DeleteUserDefinedFunctionAsync(functionLink, options);
        }

        public Task<ResourceResponse<User>> DeleteUserAsync(string userLink, RequestOptions options = null)
        {
            return this.MockClient.DeleteUserAsync(userLink, options);
        }

        public Task<ResourceResponse<Permission>> DeletePermissionAsync(string permissionLink, RequestOptions options = null)
        {
            return this.MockClient.DeletePermissionAsync(permissionLink, options);
        }

        public Task<ResourceResponse<Conflict>> DeleteConflictAsync(string conflictLink, RequestOptions options = null)
        {
            return this.MockClient.DeleteConflictAsync(conflictLink, options);
        }

        public Task<ResourceResponse<Attachment>> ReplaceAttachmentAsync(Attachment attachment, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.MockClient.ReplaceAttachmentAsync(attachment, options, cancellationToken);
        }

        public Task<ResourceResponse<Attachment>> ReplaceAttachmentAsync(Uri attachmentUri, Attachment attachment, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.MockClient.ReplaceAttachmentAsync(attachmentUri, attachment, options, cancellationToken);
        }

        public Task<ResourceResponse<DocumentCollection>> ReplaceDocumentCollectionAsync(DocumentCollection documentCollection, RequestOptions options = null)
        {
            return this.MockClient.ReplaceDocumentCollectionAsync(documentCollection, options);
        }

        public Task<ResourceResponse<Document>> ReplaceDocumentAsync(string documentLink, object document, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.MockClient.ReplaceDocumentAsync(documentLink, document, options, cancellationToken);
        }

        public Task<ResourceResponse<Document>> ReplaceDocumentAsync(Document document, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.MockClient.ReplaceDocumentAsync(document, options, cancellationToken);
        }

        public Task<ResourceResponse<Document>> ReplaceDocumentAsync(Uri attachmentUri, Attachment attachment, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.MockClient.ReplaceDocumentAsync(attachmentUri, attachment, options, cancellationToken);
        }
        public Task<ResourceResponse<Document>> ReplaceDocumentAsync(Uri documentUri, object document, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.MockClient.ReplaceDocumentAsync(documentUri, document, options, cancellationToken);
        }

        public Task<ResourceResponse<StoredProcedure>> ReplaceStoredProcedureAsync(StoredProcedure storedProcedure, RequestOptions options = null)
        {
            return this.MockClient.ReplaceStoredProcedureAsync(storedProcedure, options);
        }

        public Task<ResourceResponse<Trigger>> ReplaceTriggerAsync(Trigger trigger, RequestOptions options = null)
        {
            return this.MockClient.ReplaceTriggerAsync(trigger, options);
        }

        public Task<ResourceResponse<UserDefinedFunction>> ReplaceUserDefinedFunctionAsync(UserDefinedFunction function, RequestOptions options = null)
        {
            return this.MockClient.ReplaceUserDefinedFunctionAsync(function, options);
        }

        public Task<ResourceResponse<Permission>> ReplacePermissionAsync(Permission permission, RequestOptions options = null)
        {
            return this.MockClient.ReplacePermissionAsync(permission, options);
        }

        public Task<ResourceResponse<User>> ReplaceUserAsync(User user, RequestOptions options = null)
        {
            return this.MockClient.ReplaceUserAsync(user, options);
        }

        public Task<ResourceResponse<Offer>> ReplaceOfferAsync(Offer offer)
        {
            return this.MockClient.ReplaceOfferAsync(offer);
        }

        public Task<MediaResponse> UpdateMediaAsync(string mediaLink, Stream mediaStream, MediaOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.MockClient.UpdateMediaAsync(mediaLink, mediaStream, options, cancellationToken);
        }

        public Task<ResourceResponse<Attachment>> ReadAttachmentAsync(string attachmentLink, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.MockClient.ReadAttachmentAsync(attachmentLink, options, cancellationToken);
        }

        public Task<ResourceResponse<Attachment>> ReadAttachmentAsync(Uri attachmentUri, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.MockClient.ReadAttachmentAsync(attachmentUri, options, cancellationToken);
        }

        public Task<ResourceResponse<Database>> ReadDatabaseAsync(string databaseLink, RequestOptions options = null)
        {
            return this.MockClient.ReadDatabaseAsync(databaseLink, options);
        }

        public Task<ResourceResponse<DocumentCollection>> ReadDocumentCollectionAsync(string documentCollectionLink, RequestOptions options = null)
        {
            return this.MockClient.ReadDocumentCollectionAsync(documentCollectionLink, options);
        }

        public Task<ResourceResponse<Document>> ReadDocumentAsync(string documentLink, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.MockClient.ReadDocumentAsync(documentLink, options, cancellationToken);
        }

        public Task<ResourceResponse<Document>> ReadDocumentAsync(Uri documentUri, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.MockClient.ReadDocumentAsync(documentUri, options, cancellationToken);
        }

        public Task<DocumentResponse<T>> ReadDocumentAsync<T>(Uri documentUri, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.MockClient.ReadDocumentAsync<T>(documentUri, options, cancellationToken);
        }

        public Task<DocumentResponse<T>> ReadDocumentAsync<T>(string documentLink, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.MockClient.ReadDocumentAsync<T>(documentLink, options, cancellationToken);
        }

        public Task<ResourceResponse<StoredProcedure>> ReadStoredProcedureAsync(string storedProcedureLink, RequestOptions options = null)
        {
            return this.MockClient.ReadStoredProcedureAsync(storedProcedureLink, options);
        }

        public Task<ResourceResponse<Trigger>> ReadTriggerAsync(string triggerLink, RequestOptions options = null)
        {
            return this.MockClient.ReadTriggerAsync(triggerLink, options);
        }

        public Task<ResourceResponse<UserDefinedFunction>> ReadUserDefinedFunctionAsync(string functionLink, RequestOptions options = null)
        {
            return this.MockClient.ReadUserDefinedFunctionAsync(functionLink, options);
        }

        public Task<FeedResponse<PartitionKeyRange>> ReadPartitionKeyRangeFeedAsync(string partitionKeyRangesOrCollectionLink, FeedOptions options = null)
        {
            return this.MockClient.ReadPartitionKeyRangeFeedAsync(partitionKeyRangesOrCollectionLink, options);
        }

        public Task<FeedResponse<PartitionKeyRange>> ReadPartitionKeyRangeFeedAsync(Uri partitionKeyRangesOrCollectionUri, FeedOptions options = null)
        {
            return this.MockClient.ReadPartitionKeyRangeFeedAsync(partitionKeyRangesOrCollectionUri, options);
        }

        public Task<ResourceResponse<Permission>> ReadPermissionAsync(string permissionLink, RequestOptions options = null)
        {
            return this.MockClient.ReadPermissionAsync(permissionLink, options);
        }

        public Task<ResourceResponse<User>> ReadUserAsync(string userLink, RequestOptions options = null)
        {
            return this.MockClient.ReadUserAsync(userLink, options);
        }

        public Task<ResourceResponse<Conflict>> ReadConflictAsync(string conflictLink, RequestOptions options = null)
        {
            return this.MockClient.ReadConflictAsync(conflictLink, options);
        }

        public Task<ResourceResponse<Offer>> ReadOfferAsync(string offerLink)
        {
            return this.MockClient.ReadOfferAsync(offerLink);
        }

        public Task<MediaResponse> ReadMediaMetadataAsync(string mediaLink)
        {
            return this.MockClient.ReadMediaMetadataAsync(mediaLink);
        }

        public Task<MediaResponse> ReadMediaAsync(string mediaLink, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.MockClient.ReadMediaAsync(mediaLink, cancellationToken);
        }

        public Task<FeedResponse<Attachment>> ReadAttachmentFeedAsync(string documentLink, FeedOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.MockClient.ReadAttachmentFeedAsync(documentLink, options, cancellationToken);
        }

        public Task<FeedResponse<Attachment>> ReadAttachmentFeedAsync(Uri documentUri, FeedOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.MockClient.ReadAttachmentFeedAsync(documentUri, options, cancellationToken);
        }

        public Task<FeedResponse<Database>> ReadDatabaseFeedAsync(FeedOptions options = null)
        {
            return this.MockClient.ReadDatabaseFeedAsync(options);
        }

        public Task<FeedResponse<DocumentCollection>> ReadDocumentCollectionFeedAsync(string databaseLink, FeedOptions options = null)
        {
            return this.MockClient.ReadDocumentCollectionFeedAsync(databaseLink, options);
        }
        
        public Task<FeedResponse<dynamic>> ReadDocumentFeedAsync(string collectionLink, FeedOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.MockClient.ReadDocumentFeedAsync(collectionLink, options, cancellationToken);
        }

        public Task<FeedResponse<dynamic>> ReadDocumentFeedAsync(Uri documentCollectionUri, FeedOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.MockClient.ReadDocumentFeedAsync(documentCollectionUri, options, cancellationToken);
        }

        public Task<FeedResponse<StoredProcedure>> ReadStoredProcedureFeedAsync(string collectionLink, FeedOptions options = null)
        {
            return this.MockClient.ReadStoredProcedureFeedAsync(collectionLink, options);
        }

        public Task<FeedResponse<Trigger>> ReadTriggerFeedAsync(string collectionLink, FeedOptions options = null)
        {
            return this.MockClient.ReadTriggerFeedAsync(collectionLink, options);
        }

        public Task<FeedResponse<UserDefinedFunction>> ReadUserDefinedFunctionFeedAsync(string collectionLink, FeedOptions options = null)
        {
            return this.MockClient.ReadUserDefinedFunctionFeedAsync(collectionLink, options);
        }

        public Task<FeedResponse<Permission>> ReadPermissionFeedAsync(string userLink, FeedOptions options = null)
        {
            return this.MockClient.ReadPermissionFeedAsync(userLink, options);
        }

        public Task<FeedResponse<User>> ReadUserFeedAsync(string databaseLink, FeedOptions options = null)
        {
            return this.MockClient.ReadUserFeedAsync(databaseLink, options);
        }

        public Task<FeedResponse<Conflict>> ReadConflictFeedAsync(string collectionLink, FeedOptions options = null)
        {
            return this.MockClient.ReadConflictFeedAsync(collectionLink, options);
        }

        public Task<FeedResponse<Offer>> ReadOffersFeedAsync(FeedOptions options = null)
        {
            return this.MockClient.ReadOffersFeedAsync(options);
        }

        public Task<StoredProcedureResponse<TValue>> ExecuteStoredProcedureAsync<TValue>(string storedProcedureLink, params dynamic[] procedureParams)
        {
            return this.MockClient.ExecuteStoredProcedureAsync<TValue>(storedProcedureLink, procedureParams);
        }

        public Task<StoredProcedureResponse<TValue>> ExecuteStoredProcedureAsync<TValue>(string storedProcedureLink, RequestOptions options, params dynamic[] procedureParams)
        {
            return this.MockClient.ExecuteStoredProcedureAsync<TValue>(storedProcedureLink, options, procedureParams);
        }

        public Task<StoredProcedureResponse<TValue>> ExecuteStoredProcedureAsync<TValue>(string storedProcedureLink, RequestOptions options, CancellationToken cancellationToken, params dynamic[] procedureParams)
        {
            return this.MockClient.ExecuteStoredProcedureAsync<TValue>(storedProcedureLink, options, cancellationToken, procedureParams);
        }

        public Task<StoredProcedureResponse<TValue>> ExecuteStoredProcedureAsync<TValue>(Uri storedProcedureUri, RequestOptions options, CancellationToken cancellationToken, params dynamic[] procedureParams)
        {
            return this.MockClient.ExecuteStoredProcedureAsync<TValue>(storedProcedureUri, options, cancellationToken, procedureParams);
        }

        public Task<ResourceResponse<Attachment>> UpsertAttachmentAsync(string documentLink, object attachment, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.MockClient.UpsertAttachmentAsync(documentLink, attachment, options, cancellationToken);
        }

        public Task<ResourceResponse<Attachment>> UpsertAttachmentAsync(string documentLink, Stream mediaStream, MediaOptions options = null, RequestOptions requestOptions = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.MockClient.UpsertAttachmentAsync(documentLink, mediaStream, options, requestOptions, cancellationToken);
        }

        public Task<ResourceResponse<Attachment>> UpsertAttachmentAsync(Uri documentUri, object attachment, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.MockClient.UpsertAttachmentAsync(documentUri, attachment, options, cancellationToken);
        }

        public Task<ResourceResponse<Attachment>> UpsertAttachmentAsync(Uri documentUri, Stream mediaStream, MediaOptions options = null, RequestOptions requestOptions = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.MockClient.UpsertAttachmentAsync(documentUri, mediaStream, options, requestOptions, cancellationToken);
        }

        public Task<ResourceResponse<Document>> UpsertDocumentAsync(string collectionLink, object document, RequestOptions options = null, bool disableAutomaticIdGeneration = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.MockClient.UpsertDocumentAsync(collectionLink, document, options, disableAutomaticIdGeneration, cancellationToken);
        }

        public Task<ResourceResponse<Document>> UpsertDocumentAsync(Uri documentCollectionUri, object document, RequestOptions options = null, bool disableAutomaticIdGeneration = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.MockClient.UpsertDocumentAsync(documentCollectionUri, document, options, disableAutomaticIdGeneration, cancellationToken);
        }

        public Task<ResourceResponse<StoredProcedure>> UpsertStoredProcedureAsync(string collectionLink, StoredProcedure storedProcedure, RequestOptions options = null)
        {
            return this.MockClient.UpsertStoredProcedureAsync(collectionLink, storedProcedure, options);
        }

        public Task<ResourceResponse<Trigger>> UpsertTriggerAsync(string collectionLink, Trigger trigger, RequestOptions options = null)
        {
            return this.MockClient.UpsertTriggerAsync(collectionLink, trigger, options);
        }

        public Task<ResourceResponse<UserDefinedFunction>> UpsertUserDefinedFunctionAsync(string collectionLink, UserDefinedFunction function, RequestOptions options = null)
        {
            return this.MockClient.UpsertUserDefinedFunctionAsync(collectionLink, function, options);
        }

        public Task<ResourceResponse<Permission>> UpsertPermissionAsync(string userLink, Permission permission, RequestOptions options = null)
        {
            return this.MockClient.UpsertPermissionAsync(userLink, permission, options);
        }

        public Task<ResourceResponse<User>> UpsertUserAsync(string databaseLink, User user, RequestOptions options = null)
        {
            return this.MockClient.UpsertUserAsync(databaseLink, user, options);
        }

        public Task<DatabaseAccount> GetDatabaseAccountAsync()
        {
            return this.MockClient.GetDatabaseAccountAsync();
        }

        public Task<ResourceResponse<Attachment>> CreateAttachmentAsync(Uri documentUri, object attachment, RequestOptions options = null)
        {
            return this.MockClient.CreateAttachmentAsync(documentUri, attachment, options);
        }

        public Task<ResourceResponse<Attachment>> CreateAttachmentAsync(Uri documentUri, Stream mediaStream, MediaOptions options = null, RequestOptions requestOptions = null)
        {
            return this.MockClient.CreateAttachmentAsync(documentUri, mediaStream, options, requestOptions);
        }

        public Task<ResourceResponse<DocumentCollection>> CreateDocumentCollectionAsync(string databaseLink, DocumentCollection documentCollection, RequestOptions options = null)
        {
            return this.MockClient.CreateDocumentCollectionAsync(databaseLink, documentCollection, options);
        }

        public Task<ResourceResponse<StoredProcedure>> CreateStoredProcedureAsync(Uri documentCollectionUri, StoredProcedure storedProcedure, RequestOptions options = null)
        {
            return this.MockClient.CreateStoredProcedureAsync(documentCollectionUri, storedProcedure, options);
        }

        public Task<ResourceResponse<Trigger>> CreateTriggerAsync(Uri documentCollectionUri, Trigger trigger, RequestOptions options = null)
        {
            return this.MockClient.CreateTriggerAsync(documentCollectionUri, trigger, options);
        }

        public Task<ResourceResponse<UserDefinedFunction>> CreateUserDefinedFunctionAsync(Uri documentCollectionUri, UserDefinedFunction function, RequestOptions options = null)
        {
            return this.MockClient.CreateUserDefinedFunctionAsync(documentCollectionUri, function, options);
        }

        public Task<ResourceResponse<Permission>> CreatePermissionAsync(Uri userUri, Permission permission, RequestOptions options = null)
        {
            return this.MockClient.CreatePermissionAsync(userUri, permission, options);
        }

        public Task<ResourceResponse<User>> CreateUserAsync(Uri databaseUri, User user, RequestOptions options = null)
        {
            return this.MockClient.CreateUserAsync(databaseUri, user, options);
        }

        public Task<ResourceResponse<Attachment>> UpsertAttachmentAsync(Uri documentUri, object attachment, RequestOptions options = null)
        {
            return this.MockClient.UpsertAttachmentAsync(documentUri, attachment, options);
        }

        public Task<ResourceResponse<Attachment>> UpsertAttachmentAsync(Uri documentUri, Stream mediaStream, MediaOptions options = null, RequestOptions requestOptions = null)
        {
            return this.MockClient.UpsertAttachmentAsync(documentUri, mediaStream, options, requestOptions);
        }

        public Task<ResourceResponse<Document>> UpsertDocumentAsync(Uri documentCollectionUri, object document, RequestOptions options = null, bool disableAutomaticIdGeneration = false)
        {
            return this.MockClient.UpsertDocumentAsync(documentCollectionUri, document, options, disableAutomaticIdGeneration);
        }

        public Task<ResourceResponse<StoredProcedure>> UpsertStoredProcedureAsync(Uri documentCollectionUri, StoredProcedure storedProcedure, RequestOptions options = null)
        {
            return this.MockClient.UpsertStoredProcedureAsync(documentCollectionUri, storedProcedure, options);
        }

        public Task<ResourceResponse<Trigger>> UpsertTriggerAsync(Uri documentCollectionUri, Trigger trigger, RequestOptions options = null)
        {
            return this.MockClient.UpsertTriggerAsync(documentCollectionUri, trigger, options);
        }

        public Task<ResourceResponse<UserDefinedFunction>> UpsertUserDefinedFunctionAsync(Uri documentCollectionUri, UserDefinedFunction function, RequestOptions options = null)
        {
            return this.MockClient.UpsertUserDefinedFunctionAsync(documentCollectionUri, function, options);
        }

        public Task<ResourceResponse<Permission>> UpsertPermissionAsync(Uri userUri, Permission permission, RequestOptions options = null)
        {
            return this.MockClient.UpsertPermissionAsync(userUri, permission, options);
        }

        public Task<ResourceResponse<User>> UpsertUserAsync(Uri databaseUri, User user, RequestOptions options = null)
        {
            return this.MockClient.UpsertUserAsync(databaseUri, user, options);
        }

        public Task<ResourceResponse<Attachment>> DeleteAttachmentAsync(Uri attachmentUri, RequestOptions options = null)
        {
            return this.MockClient.DeleteAttachmentAsync(attachmentUri, options);
        }

        public Task<ResourceResponse<Database>> DeleteDatabaseAsync(Uri databaseUri, RequestOptions options = null)
        {
            return this.MockClient.DeleteDatabaseAsync(databaseUri, options);
        }

        public Task<ResourceResponse<DocumentCollection>> DeleteDocumentCollectionAsync(Uri documentCollectionUri, RequestOptions options = null)
        {
            return this.MockClient.DeleteDocumentCollectionAsync(documentCollectionUri, options);
        }

        public Task<ResourceResponse<StoredProcedure>> DeleteStoredProcedureAsync(Uri storedProcedureUri, RequestOptions options = null)
        {
            return this.MockClient.DeleteStoredProcedureAsync(storedProcedureUri, options);
        }

        public Task<ResourceResponse<Trigger>> DeleteTriggerAsync(Uri triggerUri, RequestOptions options = null)
        {
            return this.MockClient.DeleteTriggerAsync(triggerUri, options);
        }

        public Task<ResourceResponse<UserDefinedFunction>> DeleteUserDefinedFunctionAsync(Uri functionUri, RequestOptions options = null)
        {
            return this.MockClient.DeleteUserDefinedFunctionAsync(functionUri, options);
        }

        public Task<ResourceResponse<Permission>> DeletePermissionAsync(Uri permissionUri, RequestOptions options = null)
        {
            return this.MockClient.DeletePermissionAsync(permissionUri, options);
        }

        public Task<ResourceResponse<User>> DeleteUserAsync(Uri userUri, RequestOptions options = null)
        {
            return this.MockClient.DeleteUserAsync(userUri, options);
        }

        public Task<ResourceResponse<Conflict>> DeleteConflictAsync(Uri conflictUri, RequestOptions options = null)
        {
            return this.MockClient.DeleteConflictAsync(conflictUri, options);
        }

        public Task<ResourceResponse<Attachment>> ReplaceAttachmentAsync(Uri attachmentUri, Attachment attachment, RequestOptions options = null)
        {
            return this.MockClient.ReplaceAttachmentAsync(attachmentUri, attachment, options);
        }

        public Task<ResourceResponse<DocumentCollection>> ReplaceDocumentCollectionAsync(Uri documentCollectionUri, DocumentCollection documentCollection, RequestOptions options = null)
        {
            return this.MockClient.ReplaceDocumentCollectionAsync(documentCollectionUri, documentCollection, options);
        }

        public Task<ResourceResponse<StoredProcedure>> ReplaceStoredProcedureAsync(Uri storedProcedureUri, StoredProcedure storedProcedure, RequestOptions options = null)
        {
            return this.MockClient.ReplaceStoredProcedureAsync(storedProcedureUri, storedProcedure, options);
        }

        public Task<ResourceResponse<Trigger>> ReplaceTriggerAsync(Uri triggerUri, Trigger trigger, RequestOptions options = null)
        {
            return this.MockClient.ReplaceTriggerAsync(triggerUri, trigger, options);
        }

        public Task<ResourceResponse<UserDefinedFunction>> ReplaceUserDefinedFunctionAsync(Uri userDefinedFunctionUri, UserDefinedFunction function, RequestOptions options = null)
        {
            return this.MockClient.ReplaceUserDefinedFunctionAsync(userDefinedFunctionUri, function, options);
        }

        public Task<ResourceResponse<Permission>> ReplacePermissionAsync(Uri permissionUri, Permission permission, RequestOptions options = null)
        {
            return this.MockClient.ReplacePermissionAsync(permissionUri, permission, options);
        }

        public Task<ResourceResponse<User>> ReplaceUserAsync(Uri userUri, User user, RequestOptions options = null)
        {
            return this.MockClient.ReplaceUserAsync(userUri, user, options);
        }

        public Task<ResourceResponse<Attachment>> ReadAttachmentAsync(Uri attachmentUri, RequestOptions options = null)
        {
            return this.MockClient.ReadAttachmentAsync(attachmentUri, options);
        }
        
        public Task<ResourceResponse<StoredProcedure>> ReadStoredProcedureAsync(Uri storedProcedureUri, RequestOptions options = null)
        {
            return this.MockClient.ReadStoredProcedureAsync(storedProcedureUri, options);
        }

        public Task<ResourceResponse<Trigger>> ReadTriggerAsync(Uri triggerUri, RequestOptions options = null)
        {
            return this.MockClient.ReadTriggerAsync(triggerUri, options);
        }

        public Task<ResourceResponse<UserDefinedFunction>> ReadUserDefinedFunctionAsync(Uri functionUri, RequestOptions options = null)
        {
            return this.MockClient.ReadUserDefinedFunctionAsync(functionUri, options);
        }

        public Task<ResourceResponse<Permission>> ReadPermissionAsync(Uri permissionUri, RequestOptions options = null)
        {
            return this.MockClient.ReadPermissionAsync(permissionUri, options);
        }

        public Task<ResourceResponse<User>> ReadUserAsync(Uri userUri, RequestOptions options = null)
        {
            return this.MockClient.ReadUserAsync(userUri, options);
        }

        public Task<ResourceResponse<Conflict>> ReadConflictAsync(Uri conflictUri, RequestOptions options = null)
        {
            return this.MockClient.ReadConflictAsync(conflictUri, options);
        }

        public Task<FeedResponse<Attachment>> ReadAttachmentFeedAsync(Uri documentUri, FeedOptions options = null)
        {
            return this.MockClient.ReadAttachmentFeedAsync(documentUri, options);
        }

        public Task<FeedResponse<DocumentCollection>> ReadDocumentCollectionFeedAsync(Uri databaseUri, FeedOptions options = null)
        {
            return this.MockClient.ReadDocumentCollectionFeedAsync(databaseUri, options);
        }
        
        public Task<FeedResponse<dynamic>> ReadDocumentFeedAsync(Uri documentCollectionUri, FeedOptions options = null)
        {
            return this.MockClient.ReadDocumentFeedAsync(documentCollectionUri, options);
        }

        public Task<FeedResponse<StoredProcedure>> ReadStoredProcedureFeedAsync(Uri documentCollectionUri, FeedOptions options = null)
        {
            return this.MockClient.ReadStoredProcedureFeedAsync(documentCollectionUri, options);
        }

        public Task<FeedResponse<Trigger>> ReadTriggerFeedAsync(Uri documentCollectionUri, FeedOptions options = null)
        {
            return this.MockClient.ReadTriggerFeedAsync(documentCollectionUri, options);
        }

        public Task<FeedResponse<UserDefinedFunction>> ReadUserDefinedFunctionFeedAsync(Uri documentCollectionUri, FeedOptions options = null)
        {
            return this.MockClient.ReadUserDefinedFunctionFeedAsync(documentCollectionUri, options);
        }

        public Task<FeedResponse<Permission>> ReadPermissionFeedAsync(Uri userUri, FeedOptions options = null)
        {
            return this.MockClient.ReadPermissionFeedAsync(userUri, options);
        }

        public Task<FeedResponse<User>> ReadUserFeedAsync(Uri databaseUri, FeedOptions options = null)
        {
            return this.MockClient.ReadUserFeedAsync(databaseUri, options);
        }

        public Task<FeedResponse<Conflict>> ReadConflictFeedAsync(Uri documentCollectionUri, FeedOptions options = null)
        {
            return this.MockClient.ReadConflictFeedAsync(documentCollectionUri, options);
        }

        public Task<StoredProcedureResponse<TValue>> ExecuteStoredProcedureAsync<TValue>(Uri storedProcedureUri, RequestOptions options, params dynamic[] procedureParams)
        {
            return this.MockClient.ExecuteStoredProcedureAsync<TValue>(storedProcedureUri, options, procedureParams);
        }

        public IOrderedQueryable<T> CreateAttachmentQuery<T>(Uri documentUri, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateAttachmentQuery<T>(documentUri, feedOptions);
        }

        public IQueryable<T> CreateAttachmentQuery<T>(Uri documentUri, string sqlExpression, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateAttachmentQuery<T>(documentUri, sqlExpression, feedOptions);
        }

        public IQueryable<T> CreateAttachmentQuery<T>(Uri documentUri, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateAttachmentQuery<T>(documentUri, querySpec, feedOptions);
        }

        public IOrderedQueryable<Attachment> CreateAttachmentQuery(Uri documentUri, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateAttachmentQuery(documentUri, feedOptions);
        }
        
        public IQueryable<dynamic> CreateAttachmentQuery(Uri documentUri, string sqlExpression, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateAttachmentQuery(documentUri, sqlExpression, feedOptions);
        }
        
        public IQueryable<dynamic> CreateAttachmentQuery(Uri documentUri, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateAttachmentQuery(documentUri, querySpec, feedOptions);
        }

        public IOrderedQueryable<DocumentCollection> CreateDocumentCollectionQuery(Uri databaseUri, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateDocumentCollectionQuery(databaseUri, feedOptions);
        }
        
        public IQueryable<dynamic> CreateDocumentCollectionQuery(Uri databaseUri, string sqlExpression, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateDocumentCollectionQuery(databaseUri, sqlExpression, feedOptions);
        }
        
        public IQueryable<dynamic> CreateDocumentCollectionQuery(Uri databaseUri, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateDocumentCollectionQuery(databaseUri, querySpec, feedOptions);
        }

        public IOrderedQueryable<StoredProcedure> CreateStoredProcedureQuery(Uri documentCollectionUri, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateStoredProcedureQuery(documentCollectionUri, feedOptions);
        }
        
        public IQueryable<dynamic> CreateStoredProcedureQuery(Uri documentCollectionUri, string sqlExpression, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateStoredProcedureQuery(documentCollectionUri, sqlExpression, feedOptions);
        }
        
        public IQueryable<dynamic> CreateStoredProcedureQuery(Uri documentCollectionUri, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateStoredProcedureQuery(documentCollectionUri, querySpec, feedOptions);
        }

        public IOrderedQueryable<Trigger> CreateTriggerQuery(Uri documentCollectionUri, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateTriggerQuery(documentCollectionUri, feedOptions);
        }
        
        public IQueryable<dynamic> CreateTriggerQuery(Uri documentCollectionUri, string sqlExpression, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateTriggerQuery(documentCollectionUri, sqlExpression, feedOptions);
        }
        
        public IQueryable<dynamic> CreateTriggerQuery(Uri documentCollectionUri, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateTriggerQuery(documentCollectionUri, querySpec, feedOptions);
        }

        public IOrderedQueryable<UserDefinedFunction> CreateUserDefinedFunctionQuery(Uri documentCollectionUri, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateUserDefinedFunctionQuery(documentCollectionUri, feedOptions);
        }
        
        public IQueryable<dynamic> CreateUserDefinedFunctionQuery(Uri documentCollectionUri, string sqlExpression, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateUserDefinedFunctionQuery(documentCollectionUri, sqlExpression, feedOptions);
        }
        
        public IQueryable<dynamic> CreateUserDefinedFunctionQuery(Uri documentCollectionUri, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateUserDefinedFunctionQuery(documentCollectionUri, querySpec, feedOptions);
        }

        public IOrderedQueryable<Conflict> CreateConflictQuery(Uri documentCollectionUri, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateConflictQuery(documentCollectionUri, feedOptions);
        }
        
        public IQueryable<dynamic> CreateConflictQuery(Uri documentCollectionUri, string sqlExpression, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateConflictQuery(documentCollectionUri, sqlExpression, feedOptions);
        }
        
        public IQueryable<dynamic> CreateConflictQuery(Uri documentCollectionUri, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateConflictQuery(documentCollectionUri, querySpec, feedOptions);
        }

        public IQueryable<T> CreateDocumentQuery<T>(Uri documentCollectionUri, string sqlExpression, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateDocumentQuery<T>(documentCollectionUri, sqlExpression, feedOptions);
        }

        public IQueryable<T> CreateDocumentQuery<T>(Uri documentCollectionUri, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateDocumentQuery<T>(documentCollectionUri, querySpec, feedOptions);
        }
        
        public IQueryable<dynamic> CreateDocumentQuery(Uri documentCollectionUri, string sqlExpression, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateDocumentQuery(documentCollectionUri, sqlExpression, feedOptions);
        }
        
        public IQueryable<dynamic> CreateDocumentQuery(Uri documentCollectionUri, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateDocumentQuery(documentCollectionUri, querySpec, feedOptions);
        }

        public IOrderedQueryable<User> CreateUserQuery(Uri documentCollectionUri, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateUserQuery(documentCollectionUri, feedOptions);
        }
        
        public IQueryable<dynamic> CreateUserQuery(Uri documentCollectionUri, string sqlExpression, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateUserQuery(documentCollectionUri, sqlExpression, feedOptions);
        }
        
        public IQueryable<dynamic> CreateUserQuery(Uri documentCollectionUri, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateUserQuery(documentCollectionUri, querySpec, feedOptions);
        }

        public IOrderedQueryable<Permission> CreatePermissionQuery(Uri userUri, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreatePermissionQuery(userUri, feedOptions);
        }
        
        public IQueryable<dynamic> CreatePermissionQuery(Uri userUri, string sqlExpression, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreatePermissionQuery(userUri, sqlExpression, feedOptions);
        }
        
        public IQueryable<dynamic> CreatePermissionQuery(Uri userUri, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreatePermissionQuery(userUri, querySpec, feedOptions);
        }

        public IOrderedQueryable<T> CreateAttachmentQuery<T>(string documentLink, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateAttachmentQuery<T>(documentLink, feedOptions);
        }

        public IQueryable<T> CreateAttachmentQuery<T>(string documentLink, string sqlExpression, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateAttachmentQuery<T>(documentLink, sqlExpression, feedOptions);
        }

        public IQueryable<T> CreateAttachmentQuery<T>(string documentLink, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateAttachmentQuery<T>(documentLink, querySpec, feedOptions);
        }

        public IOrderedQueryable<Attachment> CreateAttachmentQuery(string documentLink, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateAttachmentQuery(documentLink, feedOptions);
        }
        
        public IQueryable<dynamic> CreateAttachmentQuery(string documentLink, string sqlExpression, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateAttachmentQuery(documentLink, sqlExpression, feedOptions);
        }
        
        public IQueryable<dynamic> CreateAttachmentQuery(string documentLink, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateAttachmentQuery(documentLink, querySpec, feedOptions);
        }

        public IOrderedQueryable<Database> CreateDatabaseQuery(FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateDatabaseQuery(feedOptions);
        }
        
        public IQueryable<dynamic> CreateDatabaseQuery(string sqlExpression, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateDatabaseQuery(sqlExpression, feedOptions);
        }
        
        public IQueryable<dynamic> CreateDatabaseQuery(SqlQuerySpec querySpec, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateDatabaseQuery(querySpec, feedOptions);
        }

        public IOrderedQueryable<DocumentCollection> CreateDocumentCollectionQuery(string databaseLink, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateDocumentCollectionQuery(databaseLink, feedOptions);
        }
        
        public IQueryable<dynamic> CreateDocumentCollectionQuery(string databaseLink, string sqlExpression, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateDocumentCollectionQuery(databaseLink, sqlExpression, feedOptions);
        }
        
        public IQueryable<dynamic> CreateDocumentCollectionQuery(string databaseLink, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateDocumentCollectionQuery(databaseLink, querySpec, feedOptions);
        }

        public IOrderedQueryable<StoredProcedure> CreateStoredProcedureQuery(string collectionLink, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateStoredProcedureQuery(collectionLink, feedOptions);
        }
        
        public IQueryable<dynamic> CreateStoredProcedureQuery(string collectionLink, string sqlExpression, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateStoredProcedureQuery(collectionLink, sqlExpression, feedOptions);
        }
        
        public IQueryable<dynamic> CreateStoredProcedureQuery(string collectionLink, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateStoredProcedureQuery(collectionLink, querySpec, feedOptions);
        }

        public IOrderedQueryable<Trigger> CreateTriggerQuery(string collectionLink, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateTriggerQuery(collectionLink, feedOptions);
        }
        
        public IQueryable<dynamic> CreateTriggerQuery(string collectionLink, string sqlExpression, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateTriggerQuery(collectionLink, sqlExpression, feedOptions);
        }
        
        public IQueryable<dynamic> CreateTriggerQuery(string collectionLink, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateTriggerQuery(collectionLink, querySpec, feedOptions);
        }

        public IOrderedQueryable<UserDefinedFunction> CreateUserDefinedFunctionQuery(string collectionLink, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateUserDefinedFunctionQuery(collectionLink, feedOptions);
        }
        
        public IQueryable<dynamic> CreateUserDefinedFunctionQuery(string collectionLink, string sqlExpression, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateUserDefinedFunctionQuery(collectionLink, sqlExpression, feedOptions);
        }
        
        public IQueryable<dynamic> CreateUserDefinedFunctionQuery(string collectionLink, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateUserDefinedFunctionQuery(collectionLink, querySpec, feedOptions);
        }

        public IOrderedQueryable<Conflict> CreateConflictQuery(string collectionLink, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateConflictQuery(collectionLink, feedOptions);
        }
        
        public IQueryable<dynamic> CreateConflictQuery(string collectionLink, string sqlExpression, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateConflictQuery(collectionLink, sqlExpression, feedOptions);
        }
        
        public IQueryable<dynamic> CreateConflictQuery(string collectionLink, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateConflictQuery(collectionLink, querySpec, feedOptions);
        }

        public IOrderedQueryable<Document> CreateDocumentQuery(Uri documentCollectionUri, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateDocumentQuery(documentCollectionUri, feedOptions);
        }

        public IOrderedQueryable<T> CreateDocumentQuery<T>(string collectionLink, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateDocumentQuery<T>(collectionLink, feedOptions);
        }

        public IQueryable<T> CreateDocumentQuery<T>(string collectionLink, string sqlExpression, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateDocumentQuery<T>(collectionLink, sqlExpression, feedOptions);
        }

        public IQueryable<T> CreateDocumentQuery<T>(string collectionLink, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateDocumentQuery<T>(collectionLink, querySpec, feedOptions);
        }

        public IOrderedQueryable<Document> CreateDocumentQuery(string collectionLink, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateDocumentQuery(collectionLink, feedOptions);
        }
        
        public IQueryable<dynamic> CreateDocumentQuery(string collectionLink, string sqlExpression, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateDocumentQuery(collectionLink, sqlExpression, feedOptions);
        }
        
        public IQueryable<dynamic> CreateDocumentQuery(string collectionLink, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateDocumentQuery(collectionLink, querySpec, feedOptions);
        }

        public IOrderedQueryable<User> CreateUserQuery(string usersLink, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateUserQuery(usersLink, feedOptions);
        }
        
        public IQueryable<dynamic> CreateUserQuery(string usersLink, string sqlExpression, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateUserQuery(usersLink, sqlExpression, feedOptions);
        }
        
        public IQueryable<dynamic> CreateUserQuery(string usersLink, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateUserQuery(usersLink, querySpec, feedOptions);
        }

        public IOrderedQueryable<Permission> CreatePermissionQuery(string permissionsLink, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreatePermissionQuery(permissionsLink, feedOptions);
        }
        
        public IQueryable<dynamic> CreatePermissionQuery(string permissionsLink, string sqlExpression, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreatePermissionQuery(permissionsLink, sqlExpression, feedOptions);
        }
        
        public IQueryable<dynamic> CreatePermissionQuery(string permissionsLink, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreatePermissionQuery(permissionsLink, querySpec, feedOptions);
        }

        public IOrderedQueryable<Offer> CreateOfferQuery(FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateOfferQuery(feedOptions);
        }
        
        public IQueryable<dynamic> CreateOfferQuery(string sqlExpression, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateOfferQuery(sqlExpression, feedOptions);
        }
        
        public IQueryable<dynamic> CreateOfferQuery(SqlQuerySpec querySpec, FeedOptions feedOptions = null)
        {
            return this.MockClient.CreateOfferQuery(querySpec, feedOptions);
        }
        #pragma warning restore 1591
        #endregion

        private async Task<ResourceResponse<Document>> LoadResponseAsync(string key)
        {
            if (this.storage[this.collection.Id].ContainsKey(key))
            {
                var document = this.storage[this.collection.Id][key];

                var response = new ResourceResponse<Document>(document);

                return await Task.FromResult(response).ConfigureAwait(false);
            }
            else
            {
                throw DocumentClientExceptionModule.Create(HttpStatusCode.NotFound);
            }
        }

        private string GetId(Document document)
        {
            return document.GetPropertyValue<string>("id");
        }
    }
}
namespace Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb
{
    using System;
    using System.Net;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.DataAccess.ActiveDirectory;

    using DocumentDB.Models;

    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;

    /// <summary>
    /// A module that provides basic CRUD operations for a document.
    /// </summary>
    public static class DocumentModule
    {
        /// <summary>
        /// Read a document. Returns null if it does not exist.
        /// </summary>
        /// <typeparam name="T">Document type.</typeparam>
        /// <param name="id">Document Id.</param>
        /// <param name="context">The context for this call.</param>
        /// <param name="documentHandler">An optional function to execute against the document before returning the value.</param>
        /// <returns>The document.</returns>
        public static async Task<T> Read<T>(object id, DocumentContext context, Func<T, T> documentHandler = null) where T : DocumentBase
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            Document document = null;
            T entity = null;

            try
            {
                ResourceResponse<Document> response = await context.DocumentClient.ReadDocumentAsync(
                    UriFactory.CreateDocumentUri(context.DatabaseName, context.CollectionName, id.ToString())).ConfigureAwait(false);

                document = response.Resource;

                entity = (T)(dynamic)document;

                if (documentHandler != null)
                {
                    return documentHandler.Invoke(entity);
                }
                else
                {
                    return entity;
                }
            }
            catch (DocumentClientException de)
            {
                if (de.StatusCode == HttpStatusCode.NotFound)
                {
                    // Document does not exist
                }
                else
                {
                    throw;
                }
            }

            return entity;
        }

        /// <summary>
        /// Create a document.
        /// </summary>
        /// <typeparam name="T">Document type.</typeparam>
        /// <param name="document">Document to be created.</param>
        /// <param name="context">The context for this call.</param>
        /// <returns>The created document.</returns>
        public static async Task<T> Create<T>(T document, DocumentContext context)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            ResourceResponse<Document> response = await context.DocumentClient.CreateDocumentAsync(
                UriFactory.CreateDocumentCollectionUri(context.DatabaseName, context.CollectionName), document).ConfigureAwait(false);

            return (T)(dynamic)response.Resource;
        }

        /// <summary>
        /// Update a document.
        /// </summary>
        /// <typeparam name="T">Document type.</typeparam>
        /// <param name="document">Document to be updated.</param>
        /// <param name="context">The context for this call.</param>
        /// <param name="notFoundHandler">An optional handler for issuing an exception when the document is not found.</param>
        /// <param name="etagMismatchHandler">An optional handler for issuing an exception when the document ETag does not match.</param>
        /// <returns>The replaced document.</returns>
        public static async Task<T> Update<T>(T document, DocumentContext context, Func<Exception, Exception> notFoundHandler = null, Func<Exception, Exception> etagMismatchHandler = null) where T : DocumentBase
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            RequestOptions requestOptions;
            if (document is CacheData)
            {
                // Remove the etag match condition for CacheData, since the condition is failing sometimes for some reason
                // and since the Id is user specific, the changes of conflict are low
                requestOptions = new RequestOptions();
            }
            else
            {
                requestOptions = new RequestOptions
                {
                    AccessCondition = new AccessCondition
                    {
                        Condition = document.GetETag(),
                        Type = AccessConditionType.IfMatch
                    }
                };
            }

            ResourceResponse<Document> response;

            try
            {
                response = await context.DocumentClient.ReplaceDocumentAsync(
                    UriFactory.CreateDocumentUri(
                        context.DatabaseName,
                        context.CollectionName,
                        document.GetId()),
                    document,
                    requestOptions).ConfigureAwait(false);
            }
            catch (DocumentClientException de)
            {
                Exception convertedException = null;

                if (de.StatusCode == HttpStatusCode.NotFound)
                {
                    convertedException = notFoundHandler?.Invoke(de);
                }
                else if (de.StatusCode == HttpStatusCode.PreconditionFailed)
                {
                    convertedException = etagMismatchHandler?.Invoke(de);
                }

                if (convertedException != null)
                {
                    throw convertedException;
                }

                throw;
            }

            return (T)(dynamic)response.Resource;
        }

        /// <summary>
        /// Contains contextual information necessary to query document DB.
        /// </summary>
        public class DocumentContext
        {
            /// <summary>
            /// Gets or sets the document client.
            /// </summary>
            public IDocumentClient DocumentClient { get; set; }

            /// <summary>
            /// Gets or sets the database name.
            /// </summary>
            public string DatabaseName { get; set; }

            /// <summary>
            /// Gets or sets the collection name.
            /// </summary>
            public string CollectionName { get; set; }
        }
    }
}
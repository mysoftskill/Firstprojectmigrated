namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.Azure.ComplianceServices.Common.Interfaces;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;

    /// <summary>
    /// A set of DocDB extension methods that apply instrumentation around the calls.
    /// </summary>
    public static class DocDbInstrumentationExtensions
    {
        private static readonly IPerformanceCounter RequestChargeCounter = PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "CosmosDbRequestCharge");

        /// <summary>
        /// Reads the given document, applying instrumentation around the operation.
        /// </summary>
        public static async Task<T> InstrumentedReadDocumentAsync<T>(
            this DocumentClient client, 
            Uri documentUri,
            string moniker,
            string collectionId,
            string partitionKey = null,
            bool expectThrottles = false,
            bool expectConflicts = false,
            bool expectNotFound = false,
            RequestOptions requestOptions = null,
            Action<CosmosDbOutgoingEvent, DocumentResponse<T>> extraLogging = null,
            [CallerFilePath] string callerFilePath = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = -1) where T : class, new()
        {
            SourceLocation callerLocation = new SourceLocation(callerMemberName, callerFilePath, callerLineNumber);

            try
            {
                return await InstrumentationTemplate<DocumentResponse<T>, T>(
                    new CosmosDbOutgoingEvent(callerLocation, moniker, collectionId, partitionKey),
                    expectThrottles,
                    expectConflicts,
                    expectNotFound,
                    extraLogging,
                    x => x.RequestCharge,
                    x => x.SessionToken,
                    x => x.Document,
                    _ => client.ReadDocumentAsync<T>(documentUri, requestOptions));
            }
            catch (CommandFeedException ex)
            {
                // Doc DB likes to throw a "NotFound" exception when it doesn't find something. This
                // catch block just checks for that and coalesces it into "null".
                if (ex.ErrorCode == CommandFeedInternalErrorCode.NotFound)
                {
                    return null;
                }

                throw;
            }
        }

        /// <summary>
        /// Executes the given stored procedure.
        /// </summary>
        public static Task<T> InstrumentedExecuteStoredProcedureAsync<T>(
            this DocumentClient client,
            Uri storedProcedureUri,
            string moniker,
            string collectionId,
            dynamic[] parameters,
            string partitionKey = null,
            bool expectThrottles = false,
            bool expectConflicts = false,
            bool expectNotFound = false,
            RequestOptions requestOptions = null,
            Action<CosmosDbOutgoingEvent, StoredProcedureResponse<T>> extraLogging = null,
            [CallerFilePath] string callerFilePath = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = -1) where T : new()
        {
            SourceLocation callerLocation = new SourceLocation(callerMemberName, callerFilePath, callerLineNumber);

            return InstrumentationTemplate<StoredProcedureResponse<T>, T>(
                new CosmosDbOutgoingEvent(callerLocation, moniker, collectionId, partitionKey),
                expectThrottles,
                expectConflicts,
                expectNotFound,
                extraLogging,
                x => x.RequestCharge,
                x => x.SessionToken,
                x => x.Response,
                _ => client.ExecuteStoredProcedureAsync<T>(storedProcedureUri, requestOptions, parameters));
        }

        /// <summary>
        /// Creates the given document.
        /// </summary>
        public static Task<Document> InstrumentedCreateDocumentAsync(
            this IDocumentClient client,
            Uri collectionUri,
            string moniker,
            string collectionId,
            object document,
            string partitionKey = null,
            bool expectThrottles = false,
            bool expectConflicts = false,
            bool expectNotFound = false,
            RequestOptions requestOptions = null,
            Action<CosmosDbOutgoingEvent, ResourceResponse<Document>> extraLogging = null,
            [CallerFilePath] string callerFilePath = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = -1)
        {
            SourceLocation callerLocation = new SourceLocation(callerMemberName, callerFilePath, callerLineNumber);

            return InstrumentationTemplate<ResourceResponse<Document>, Document>(
                new CosmosDbOutgoingEvent(callerLocation, moniker, collectionId, partitionKey),
                expectThrottles,
                expectConflicts,
                expectNotFound,
                extraLogging,
                x => x.RequestCharge,
                x => x.SessionToken,
                x => x.Resource,
                _ => client.CreateDocumentAsync(collectionUri, document, requestOptions));
        }

        /// <summary>
        /// Upserts the given document.
        /// </summary>
        public static Task<Document> InstrumentedUpsertDocumentAsync(
            this IDocumentClient client,
            Uri collectionUri,
            string moniker,
            string collectionId,
            object document,
            string partitionKey = null,
            bool expectThrottles = false,
            bool expectConflicts = false,
            bool expectNotFound = false,
            RequestOptions requestOptions = null,
            Action<CosmosDbOutgoingEvent, ResourceResponse<Document>> extraLogging = null,
            [CallerFilePath] string callerFilePath = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = -1)
        {
            SourceLocation callerLocation = new SourceLocation(callerMemberName, callerFilePath, callerLineNumber);

            return InstrumentationTemplate<ResourceResponse<Document>, Document>(
                new CosmosDbOutgoingEvent(callerLocation, moniker, collectionId, partitionKey),
                expectThrottles,
                expectConflicts,
                expectNotFound,
                extraLogging,
                x => x.RequestCharge,
                x => x.SessionToken,
                x => x.Resource,
                _ => client.UpsertDocumentAsync(collectionUri, document, requestOptions));
        }

        /// <summary>
        /// Deletes the document identified by the given URI.
        /// </summary>
        public static Task InstrumentedDeleteDocumentAsync(
            this DocumentClient client,
            Uri documentUri,
            string moniker,
            string collectionId,
            string partitionKey = null,
            bool expectThrottles = false,
            bool expectConflicts = false,
            bool expectNotFound = false,
            RequestOptions requestOptions = null,
            Action<CosmosDbOutgoingEvent, ResourceResponse<Document>> extraLogging = null,
            [CallerFilePath] string callerFilePath = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = -1)
        {
            SourceLocation callerLocation = new SourceLocation(callerMemberName, callerFilePath, callerLineNumber);

            return InstrumentationTemplate<ResourceResponse<Document>, Document>(
                new CosmosDbOutgoingEvent(callerLocation, moniker, collectionId, partitionKey),
                expectThrottles,
                expectConflicts,
                expectNotFound,
                extraLogging,
                x => x.RequestCharge,
                x => x.SessionToken,
                x => x.Resource,
                _ => client.DeleteDocumentAsync(documentUri, requestOptions));
        }

        /// <summary>
        /// Replaces the given document.
        /// </summary>
        public static Task<Document> InstrumentedReplaceDocumentAsync(
            this DocumentClient client,
            Uri documentUri,
            string moniker,
            string collectionId,
            object document,
            string partitionKey = null,
            bool expectThrottles = false,
            bool expectConflicts = false,
            bool expectNotFound = false,
            RequestOptions requestOptions = null,
            Action<CosmosDbOutgoingEvent, ResourceResponse<Document>> extraLogging = null,
            [CallerFilePath] string callerFilePath = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = -1)
        {
            SourceLocation callerLocation = new SourceLocation(callerMemberName, callerFilePath, callerLineNumber);

            return InstrumentationTemplate<ResourceResponse<Document>, Document>(
                new CosmosDbOutgoingEvent(callerLocation, moniker, collectionId, partitionKey),
                expectThrottles,
                expectConflicts,
                expectNotFound,
                extraLogging,
                x => x.RequestCharge,
                x => x.SessionToken,
                x => x.Resource,
                _ => client.ReplaceDocumentAsync(documentUri, document, requestOptions));
        }

        public static Task<FeedResponse<T>> InstrumentedFeedResponseExecuteNextAsync<T>(
            this IDocumentQuery<T> query,
            string moniker,
            string collectionId,
            string partitionKey = null,
            bool expectThrottles = false,
            bool expectConflicts = false,
            bool expectNotFound = false,
            Action<CosmosDbOutgoingEvent, FeedResponse<T>> extraLogging = null,
            [CallerFilePath] string callerFilePath = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = -1) where T : new()
        {
            SourceLocation callerLocation = new SourceLocation(callerMemberName, callerFilePath, callerLineNumber);

            return InstrumentationTemplate<FeedResponse<T>, FeedResponse<T>>(
                new CosmosDbOutgoingEvent(callerLocation, moniker, collectionId, partitionKey),
                expectThrottles,
                expectConflicts,
                expectNotFound,
                extraLogging,
                x => x.RequestCharge,
                x => x.SessionToken,
                x => x,
                _ => query.ExecuteNextAsync<T>());
        }

        public static Task<(List<T> items, string continuation)> InstrumentedExecuteNextAsync<T>(
            this IDocumentQuery<T> query,
            string moniker,
            string collectionId,
            string partitionKey = null,
            bool expectThrottles = false,
            bool expectConflicts = false,
            bool expectNotFound = false,
            Action<CosmosDbOutgoingEvent, FeedResponse<T>> extraLogging = null,
            [CallerFilePath] string callerFilePath = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = -1) where T : new()
        {
            SourceLocation callerLocation = new SourceLocation(callerMemberName, callerFilePath, callerLineNumber);

            return InstrumentationTemplate<FeedResponse<T>, (List<T> items, string continuation)>(
                new CosmosDbOutgoingEvent(callerLocation, moniker, collectionId, partitionKey),
                expectThrottles,
                expectConflicts,
                expectNotFound,
                extraLogging,
                x => x.RequestCharge,
                x => x.SessionToken,
                x => (x.ToList(), x.ResponseContinuation),
                _ => query.ExecuteNextAsync<T>());
        }

        private static Task<TReturn> InstrumentationTemplate<TResponse, TReturn>(
            CosmosDbOutgoingEvent ev,
            bool expectThrottles,
            bool expectConflicts,
            bool expectNotFound,
            Action<CosmosDbOutgoingEvent, TResponse> extraLogging,
            Func<TResponse, double> getRequestCharge,
            Func<TResponse, string> getSessionToken,
            Func<TResponse, TReturn> getReturnConverter,
            Func<CosmosDbOutgoingEvent, Task<TResponse>> callback)
        {
            return Logger.InstrumentAsync(
                ev,
                async _ =>
                {
                    try
                    {
                        TResponse item = await callback(ev);

                        extraLogging?.Invoke(ev, item);
                        ev.RequestCharge = getRequestCharge(item);
                        ev["SessionToken"] = getSessionToken(item);

                        RequestChargeCounter.Increment($"{ev.Moniker}.{ev.CollectionId}", (int)ev.RequestCharge);

                        return getReturnConverter(item);
                    }
                    catch (DocumentClientException ex)
                    {
                        var statusCode = ClassifyException(ev, ex);
                        RequestChargeCounter.Increment($"{ev.Moniker}.{ev.CollectionId}", (int)ex.RequestCharge);

                        if (statusCode == CommandFeedInternalErrorCode.Unknown)
                        {
                            // Unable to classify.
                            throw;
                        }

                        var commandFeedException = new CommandFeedException(ex)
                        {
                            ErrorCode = statusCode,
                        };

                        commandFeedException.IsExpected |= statusCode == CommandFeedInternalErrorCode.Throttle && expectThrottles;
                        commandFeedException.IsExpected |= statusCode == CommandFeedInternalErrorCode.Conflict && expectConflicts;
                        commandFeedException.IsExpected |= statusCode == CommandFeedInternalErrorCode.NotFound && expectNotFound;

                        throw commandFeedException;
                    }
                });
        }

        private static CommandFeedInternalErrorCode ClassifyException(CosmosDbOutgoingEvent ev, DocumentClientException ex)
        {
            const HttpStatusCode ThrottledStatusCode = (HttpStatusCode)429;

            ev.StatusCode = ex.StatusCode.ToString();
            ev.RequestCharge = ex.RequestCharge;

            if (ex.StatusCode == ThrottledStatusCode)
            {
                ev.IsThrottled = true;
                return CommandFeedInternalErrorCode.Throttle;
            }

            // Precondition failed is for "If-Match" style requests (think: etags). Conflict is for things like "insert".
            // However, they are semantically similar since they both mean "your request was rejected because the state on the server
            // didn't allow it", so we wrap these up into a "conflict" error code.
            if (ex.StatusCode == HttpStatusCode.PreconditionFailed || ex.StatusCode == HttpStatusCode.Conflict)
            {
                return CommandFeedInternalErrorCode.Conflict;
            }

            if (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return CommandFeedInternalErrorCode.NotFound;
            }

            return CommandFeedInternalErrorCode.Unknown;
        }
    }
}

// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations.BaseContexts;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.Telemetry;

    using Ms.Qos;

    using static System.FormattableString;

    /// <summary>
    ///     Wrapper for an outgoing API logical operation. Defines Part C fields.
    ///     <para>
    ///         This wrapper can be started/finished several times over for tracking retries. Retries are tracked with the
    ///         AttemptCount field, which is internally incremented whenever Start() is called.
    ///     </para>
    /// </summary>
    public class OutgoingApiEventWrapper : OutgoingLoggingContext
    {
        private const string RedactedResponse = "<Redacted>";

        /// <summary>
        ///     The logical operation.
        /// </summary>
        private OutgoingApiEvent operation;

        /// <summary>
        ///     Creates an OutgoingApiEvent request.
        /// </summary>
        /// <param name="partnerId">The partner ID</param>
        /// <param name="operationName">The operation name of the outgoing request (e.g. GetQuota)</param>
        /// <param name="operationVersion">The version of the outgoing operation.</param>
        /// <param name="targetUri">
        ///     The target URI. This is a string because it exists as a string in the Part B schema. Sometimes, the target uri
        ///     is not a known absolute Uri, so this is more flexible.
        /// </param>
        /// <param name="requestMethod">HTTP request method</param>
        /// <param name="dependencyType">Type of the dependency. Ex: AzureStore, WebService, SQL</param>
        /// <param name="protocol">The protocol. HTTP, HTTPS, FTP, TCP, UDP, SMTP, etc.</param>
        /// <returns>OutgoingApiEventWrapper</returns>
        /// <remarks>
        ///     See https://osgwiki.com/wiki/CommonSchema/PartB/OutgoingServiceRequest for schema info
        /// </remarks>
        public static OutgoingApiEventWrapper CreateBasicOutgoingEvent(
            string partnerId,
            string operationName,
            string operationVersion,
            string targetUri,
            HttpMethod requestMethod,
            string dependencyType,
            string protocol = "HTTPS")
        {
            return new OutgoingApiEventWrapper
            {
                TargetUri = targetUri,
                RequestMethod = requestMethod.ToString(),
                DependencyOperationName = operationName,
                DependencyOperationVersion = operationVersion,
                DependencyName = partnerId,
                DependencyType = dependencyType,
                PartnerId = partnerId,
                Protocol = protocol
            };
        }

        /// <summary>
        ///     Creates an OutgoingApiEvent for an HTTP request.
        /// </summary>
        /// <param name="partnerId">The partner ID</param>
        /// <param name="operationName">The operation name of the outgoing request (e.g. GetQuota)</param>
        /// <param name="operationVersion">The operation version.</param>
        /// <param name="requestUri">Request URI</param>
        /// <param name="requestMethod">HTTP request method</param>
        /// <param name="userPuid">User PUID.</param>
        public static OutgoingApiEventWrapper CreateHttpEventWithPuid(
            string partnerId,
            string operationName,
            string operationVersion,
            Uri requestUri,
            HttpMethod requestMethod,
            long? userPuid)
        {
            return CreateHttpEvent(
                partnerId,
                operationName,
                operationVersion,
                requestUri,
                requestMethod,
                new MsaId(userPuid));
        }

        /// <summary>
        ///     Creates an OutgoingApiEvent for a SOAP request.
        /// </summary>
        /// <param name="partnerId">The partner ID</param>
        /// <param name="operationName">The operation name of the outgoing request (e.g. GetQuota)</param>
        /// <param name="requestUri">Request URI</param>
        /// <param name="userPuid">User puid. May be null</param>
        public static OutgoingApiEventWrapper CreateSoapEvent(
            string partnerId,
            string operationName,
            Uri requestUri,
            long? userPuid)
        {
            return CreateHttpEvent(
                partnerId,
                operationName,
                requestUri,
                HttpMethod.Post,
                new MsaId(userPuid));
        }

        /// <summary>
        ///     Gets the correlation vector from the Sll Context, increments it and returns the incremented value.
        /// </summary>
        /// <returns>Correlation vector.</returns>
        public static string GetNextCorrelationVector()
        {
            return Sll.Context.Vector?.Increment();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="OutgoingApiEventWrapper" /> class.
        /// </summary>
        public OutgoingApiEventWrapper()
        {
            this.ServerActivityId = LogicalWebOperationContext.ServerActivityId.ToString();
            this.ClientActivityId = LogicalWebOperationContext.ClientActivityId.ToString();
            this.ExtraData = new Dictionary<string, string>();
        }

        /// <summary>
        ///     Finishes the event.
        /// </summary>
        /// <param name="elapsedMilliseconds">Elapsed milliseconds to use as latency for the operation.</param>
        public virtual void Finish(double? elapsedMilliseconds = null)
        {
            this.PopulateFinishEvent(this.operation, elapsedMilliseconds);

            TraceOperationHelper.PopupOperationResult(
                this.Success,
                this.operation.baseData.protocolStatusCode,
                this.ResultSignature,
                this.ResultDetails,
                this.traceOperation);

            base.Finish(this.operation);
            this.operation = null;
        }

        /// <summary>
        ///     Populates request data in the event
        /// </summary>
        /// <param name="request">http request object to populate data from</param>
        /// <param name="logDetails">true to log the response body and headers, false otherwise</param>
        /// <returns>resulting value</returns>
        public async Task PopulateFromRequestAsync(
            HttpRequestMessage request,
            bool logDetails)
        {
            if (request != null)
            {
                if (request.Method != null)
                    this.RequestMethod = request.Method.ToString();

                if (request.RequestUri != null)
                    this.TargetUri = request.RequestUri.ToString();

                if (logDetails)
                {
                    if (request.Headers != null)
                    {
                        this.RequestHeaders = request.Headers
                            .Where(h => !IsBlocklistedHeader(h.Key))
                            .ToDictionary(h => h.Key, h => string.Join(",", h.Value ?? Enumerable.Empty<string>()));
                    }

                    if (request.Content != null)
                    {
                        this.RequestContent = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>
        ///     Populates the request and response data in the event
        /// </summary>
        /// <param name="response">http response</param>
        /// <param name="logDetails">true to log the response body and headers, false otherwise</param>
        /// <returns>resulting value</returns>
        public async Task PopulateFromResponseAsync(
            HttpResponseMessage response,
            bool logDetails)
        {
            if (response == null)
            {
                return;
            }

            logDetails = logDetails || !response.IsSuccessStatusCode;

            await this.PopulateFromRequestAsync(
                response.RequestMessage,
                logDetails).ConfigureAwait(false);

            this.ProtocolStatusCode = ((int)response.StatusCode).ToString(CultureInfo.InvariantCulture);

            if (response.ReasonPhrase != null)
                this.ProtocolStatusCodePhrase = response.ReasonPhrase;

            try
            {
                if (response.Content != null)
                {
                    string bodyContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    this.ResponseSizeBytes = bodyContent?.Length ?? 0;
                    this.ResponseContent = logDetails ? bodyContent : RedactedResponse;
                }
            }
            catch (Exception ex)
            {
                this.ResponseContent = "Could not read response:" + Environment.NewLine + ex;
            }

            if (logDetails && response.Headers != null)
            {
                this.ResponseHeaders = response.Headers
                    .Where(h => !IsBlocklistedHeader(h.Key))
                    .ToDictionary(h => h.Key, h => string.Join(",", h.Value ?? Enumerable.Empty<string>()));
            }

            this.SetSuccess((int)response.StatusCode);
        }

        /// <summary>
        ///     Starts a new event.
        ///     Resets all error fields in this wrapper, and increments the AttemptCount and CorrelationVector fields, for retries.
        /// </summary>
        public virtual void Start()
        {
            this.Start(GetNextCorrelationVector());
        }

        /// <summary>
        ///     Starts a new event using the provided correlation vector.
        ///     Resets all error fields in this wrapper, and increments the AttemptCount, for retries.
        /// </summary>
        /// <param name="correlationVector">Starts a logical operation with this Correlation Vector.</param>
        public virtual void Start(string correlationVector)
        {
            if (this.operation != null)
            {
                throw new InvalidOperationException("Finish() must be called after Start().");
            }

            this.AttemptCount++;

            this.CorrelationVector = correlationVector;
            this.ServiceErrorCode = 0;
            this.ProtocolStatusCode = null;
            this.ErrorMessage = null;

            this.Stopwatch = Stopwatch.StartNew();
            string parentOperationName = GetParentOperationName();

            this.operation = new OutgoingApiEvent
            {
                baseData = new OutgoingServiceRequest { operationName = parentOperationName }
            };

            this.InitTraceOperation(parentOperationName);

            Sll.Context?.CorrelationContext?.FillCorrelationContextDictionary(this.operation.baseData.cC);
        }

        protected override string ResultDetails => this.ErrorMessage;

        protected override string ResultSignature => this.operation.baseData.protocolStatusCode;

        protected void InitTraceOperation(string parentOperationName)
        {
            this.traceOperation = new Tracer(Invariant($"{parentOperationName}:{this.DependencyOperationName}"));
        }

        protected void PopulateFinishEvent(OutgoingApiEvent operation, double? elapsedMilliseconds = null, ILogger logger = null)
        {
            if (operation == null)
            {
                throw new InvalidOperationException("Start() must be called before Finish().");
            }

            try
            {
                this.ElapsedMilliseconds = elapsedMilliseconds;
                this.RequestStatusDelegateMethod(false, this);
                this.PopulateOutgoingServiceRequest(operation.baseData);

                operation.ServerActivityId = this.ServerActivityId;
                operation.ClientActivityId = this.ClientActivityId;
                operation.PartnerId = this.PartnerId;
                operation.ErrorMessage = this.ErrorMessage;
                operation.AttemptCount = this.AttemptCount;
                operation.ProtocolStatusCodePhrase = this.ProtocolStatusCodePhrase;
                operation.ResponseContent = this.ResponseContent;
                operation.ExceptionTypeName = this.ExceptionTypeName;

                // If you have an event that is map<string, string> and optional, you would think that null would be ok
                // However, this causes a nullref somewhere in json serialization, and so we care to never set them to
                // null. By default the generated class sets them to an empty dictionary.

                if (this.RequestHeaders != null)
                {
                    operation.RequestHeaders = this.RequestHeaders;
                }

                if (this.ResponseHeaders != null)
                {
                    operation.ResponseHeaders = this.ResponseHeaders;
                }

                if (this.ExtraData != null)
                {
                    operation.ExtraData = this.ExtraData;
                }
            }
            catch (Exception e)
            {
                // Populating extra properties shouldn't prevent us from logging, so try this to be safe.
                string errorMessage = $"Exception occurred in populating additional SLL properties: {e}";

                if (logger == null)
                {
                    Trace.TraceError(errorMessage);
                }
                else
                {
                    logger.Error(nameof(OutgoingApiEventWrapper), errorMessage);
                }
            }
        }

        private void SetSuccess(int statusCode)
        {
            // Previously, we considered 'response.IsSuccessStatusCode' to indicate Success. 
            // However, some partners may send us 3xx codes, and those are not error codes.
            // Since http status code success is not the same as a success for SLL logs, we are treating anything less than 400 to be a success.
            this.Success = statusCode < 400;
        }

        /// <summary>
        ///     Creates an OutgoingApiEvent for an HTTP request.
        /// </summary>
        /// <param name="partnerId">The partner ID</param>
        /// <param name="operationName">The operation name of the outgoing request (e.g. GetQuota)</param>
        /// <param name="requestUri">Request URI</param>
        /// <param name="requestMethod">HTTP request method</param>
        /// <param name="userId">User ID. May be null (e.g. MSA access token retrieval)</param>
        private static OutgoingApiEventWrapper CreateHttpEvent(
            string partnerId,
            string operationName,
            Uri requestUri,
            HttpMethod requestMethod,
            MsaId userId)
        {
            return CreateHttpEvent(partnerId, operationName, null, requestUri, requestMethod, userId);
        }

        /// <summary>
        ///     Creates an OutgoingApiEvent for an HTTP request.
        /// </summary>
        /// <param name="partnerId">The partner ID</param>
        /// <param name="operationName">The operation name of the outgoing request (e.g. GetQuota)</param>
        /// <param name="operationVersion">The version of the outgoing operation.</param>
        /// <param name="requestUri">Request URI</param>
        /// <param name="requestMethod">HTTP request method</param>
        /// <param name="userId">User ID. May be null (e.g. MSA access token retrieval)</param>
        private static OutgoingApiEventWrapper CreateHttpEvent(
            string partnerId,
            string operationName,
            string operationVersion,
            Uri requestUri,
            HttpMethod requestMethod,
            MsaId userId)
        {
            if (!requestUri.Scheme.Equals("HTTP", StringComparison.OrdinalIgnoreCase) &&
                !requestUri.Scheme.Equals("HTTPS", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Uri scheme is not HTTP", nameof(requestUri));
            }

            var outgoingApiEvent = new OutgoingApiEventWrapper
            {
                TargetUri = requestUri.ToString(),
                RequestMethod = requestMethod.ToString(),
                Protocol = requestUri.Scheme,
                DependencyOperationName = operationName,
                DependencyOperationVersion = operationVersion,
                DependencyName = partnerId,
                DependencyType = "WebService",
                PartnerId = partnerId
            };

            outgoingApiEvent.SetUserId(userId);

            return outgoingApiEvent;
        }

        protected static string GetParentOperationName()
        {
            Data<IncomingServiceRequest> incomingOperation = Sll.Context.Incoming;
            string parentOperation = !string.IsNullOrWhiteSpace(incomingOperation?.baseData?.operationName)
                ? incomingOperation.baseData.operationName
                : "(UnknownOperation)";

            return parentOperation;
        }

        private static bool IsBlocklistedHeader(string header)
        {
            switch (header)
            {
                case "Authorization": // User authorization
                case "aeg-sas-key": // key for event grid
                case "X-PXS-RPSToken": // RPS token
                case "X-S2S-Access-Token": // S2S auth tokens
                case "X-User-Token": // For PDP user auth. Would reference the constant, but can't get there from here.
                case "X-REL-ID-TOKEN": // For PDP user auth. Would reference the constant, but can't get there from here.
                case "X-REL-PARTNER-SIG": // For PDP partner auth. Would reference the constant, but can't get there from here.
                case "X-S2S-Token": // DDS partner auth. Would reference the constant, but can't get there from here.
                case "X-S2S-Proxy-Token": // DDS user auth. Would reference the constant, but can't get there from here.
                case "signature": // signature for Xbox requests
                    return true;
                default:
                    return false;
            }
        }

        #region Part-C

        /// <summary>
        ///     Gets or sets the server activity id.
        /// </summary>
        private string ServerActivityId { get; }

        /// <summary>
        ///     Gets or sets the client activity id.
        /// </summary>
        private string ClientActivityId { get; }

        /// <summary>
        ///     Gets or sets the partner id.
        /// </summary>
        public string PartnerId { get; set; }

        /// <summary>
        ///     Gets or sets the error message.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        ///     Gets or sets the number of retries attempted.
        /// </summary>
        public uint AttemptCount { get; protected set; }

        /// <summary>
        ///     Gets or sets the request headers.
        /// </summary>
        public Dictionary<string, string> RequestHeaders { get; private set; }

        /// <summary>
        ///     Gets or sets the request body (if present)
        /// </summary>
        public string RequestContent { get; private set; }

        /// <summary>
        ///     Gets or sets the protocol status reason phrase.
        /// </summary>
        public string ProtocolStatusCodePhrase { get; private set; }

        /// <summary>
        ///     Gets or sets the response headers.
        /// </summary>
        public Dictionary<string, string> ResponseHeaders { get; private set; }

        /// <summary>
        ///     Gets or sets the response content.
        /// </summary>
        public string ResponseContent { get; private set; }

        /// <summary>
        ///     Gets the extra data, property bag style.
        /// </summary>
        public Dictionary<string, string> ExtraData { get; }

        /// <summary>
        ///     Gets the exception type name (optional)
        /// </summary>
        public string ExceptionTypeName { get; set; }

        /// <summary>
        ///     Gets or sets Part C Correlation context.
        /// </summary>
        public Dictionary<string, string> CorrelationContext { get; set; }

        #endregion
    }
}

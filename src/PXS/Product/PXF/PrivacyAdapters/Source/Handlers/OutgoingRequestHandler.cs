// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Handlers
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging.LoggingFilter;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     An HTTP client handler to track the outgoing execution of a request.
    /// </summary>
    public class OutgoingRequestHandler : DelegatingHandler
    {
        public const string ApiEventContextKey = "ApiEventContext";

        public const string CounterInstanceNameKey = "CounterInstanceName";

        private readonly string componentName;

        private readonly ICounterFactory counterFactory;

        private readonly ILogger logger;

        private readonly ILoggingFilter loggingFilter;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OutgoingRequestHandler" /> class.
        /// </summary>
        /// <param name="counterFactory">The counter factory.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="componentName">Name of the component.</param>
        /// <param name="loggingFilter">logging filter</param>
        public OutgoingRequestHandler(
            ICounterFactory counterFactory,
            ILogger logger,
            string componentName,
            ILoggingFilter loggingFilter)
        {
            this.counterFactory = counterFactory;
            this.loggingFilter = loggingFilter;
            this.componentName = componentName;
            this.logger = logger;
        }

        /// <summary>
        ///     Sends an HTTP request to the inner handler to send to the server as an asynchronous operation.
        /// </summary>
        /// <param name="request">The HTTP request message to send to the server.</param>
        /// <param name="cancellationToken">A cancellation token to cancel operation.</param>
        /// <returns>
        ///     Returns <see cref="T:System.Threading.Tasks.Task`1" />. The task object representing the asynchronous operation.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">OutgoingApiEvent was null. It is required to enforce always logging outgoing api events.</exception>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">Request properties did not include the ApiEventContextKey.</exception>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // These properties should have been added to the request's properties beforehand
            var outgoingApiEvent = request.Properties[ApiEventContextKey] as OutgoingApiEventWrapper;

            if (outgoingApiEvent == null)
            {
                throw new InvalidOperationException("OutgoingApiEvent is null. It is required to enforce always logging outgoing api events.");
            }

            TimedHttpOperationExecutionResult timedResult = null;

            try
            {
                // Start the event, which will increment the correlation vector. 
                // Assign the new correlation vector to the outgoing request
                outgoingApiEvent.Start();

                // Remove header if it already exists. Failing to remove the header would result in a list of values
                if (request.Headers.Contains(CorrelationVector.HeaderName))
                {
                    request.Headers.Remove(CorrelationVector.HeaderName);
                }

                string correlationVector = outgoingApiEvent.CorrelationVector;
                if (string.IsNullOrWhiteSpace(correlationVector))
                {
                    // If we don't have a good CV, summon one.
                    correlationVector = new CorrelationVector().Value;
                }

                request.Headers.Add(CorrelationVector.HeaderName, correlationVector);

                if (request.Headers.Contains(CorrelationContext.HeaderName))
                {
                    request.Headers.Remove(CorrelationContext.HeaderName);
                }

                string correlationContextHeader = Sll.Context?.CorrelationContext?.GetCorrelationContext();
                if (correlationContextHeader != null)
                    request.Headers.Add(CorrelationContext.HeaderName, correlationContextHeader);

                // Indirectly logs perf counters through RequestExecutionHelper
                timedResult =
                    await RequestExecutionHelper.ExecuteTimedHttpActionAsync(
                            counterFactory: this.counterFactory,
                            componentName: this.componentName,
                            methodName: this.RetrieveCounterInstanceName(request),
                            action: () => base.SendAsync(request, cancellationToken))
                        .ConfigureAwait(false);

                if (timedResult.Response != null)
                {
                    bool logDetails = this.loggingFilter?.ShouldLogDetailsForUser(outgoingApiEvent.UserInfo?.Id) ?? false;
                    await outgoingApiEvent.PopulateFromResponseAsync(timedResult.Response, logDetails).ConfigureAwait(false);
                }

                if (timedResult.Exception != null)
                {
                    outgoingApiEvent.Success = false;
                    outgoingApiEvent.ErrorMessage = timedResult.Exception.ToString();
                    outgoingApiEvent.ExceptionTypeName = timedResult.Exception.GetType().Name;

                    // re-throw after logging QOS
                    throw timedResult.Exception;
                }
                else if (timedResult.Response == null)
                {
                    outgoingApiEvent.Success = false;
                    outgoingApiEvent.ErrorMessage = "Response was null";
                }

                return timedResult.Response;
            }
            finally
            {
                outgoingApiEvent.Finish(timedResult?.LatencyInMilliseconds);
            }
        }

        private string RetrieveCounterInstanceName(HttpRequestMessage request)
        {
            // These properties should have been added to the request's properties beforehand.
            string counterInstanceName = null;
            object counterInstanceNameValue;

            if (request.Properties.TryGetValue(CounterInstanceNameKey, out counterInstanceNameValue))
            {
                counterInstanceName = counterInstanceNameValue as string;
            }

            if (string.IsNullOrWhiteSpace(counterInstanceName))
            {
                this.logger.Error(this.componentName, CounterInstanceNameKey + " not set for outgoing request.");
            }

            return counterInstanceName;
        }
    }
}

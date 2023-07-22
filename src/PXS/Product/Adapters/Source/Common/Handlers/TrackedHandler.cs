// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Adapters.Common.Handlers
{
    using System.Globalization;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;

    /// <summary>
    /// An HTTP client handler to track the execution of a request. Logs perf counters and outgoing QoS events.
    /// </summary>
    public class TrackedHandler : DelegatingHandler
    {
        private readonly ICounterFactory counterFactory;
        private readonly string componentName;

        public TrackedHandler(ICounterFactory counterFactory, string componentName)
        {
            this.counterFactory = counterFactory;
            this.componentName = componentName;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // These properties should have been added to the request's properties beforehand
            string counterInstanceName = request.Properties[HandlerConstants.CounterInstanceNameKey] as string;
            OutgoingApiEventWrapper outgoingApiEvent = request.Properties[HandlerConstants.ApiEventContextKey] as OutgoingApiEventWrapper;

            if (outgoingApiEvent != null)
            {
                // Start the event, which will increment the correlation vector. Assign the new correlation vector
                // to the outgoing request
                outgoingApiEvent.Start();

                // Remove header if it already exists. Failing to remove the header would result in a list of values
                if (request.Headers.Contains(CorrelationVector.HeaderName))
                {
                    request.Headers.Remove(CorrelationVector.HeaderName);
                }
                request.Headers.Add(CorrelationVector.HeaderName, outgoingApiEvent.CorrelationVector);
            }

            // Indirectly logs perf counters through RequestExecutionHelper
            TimedHttpOperationExecutionResult timedResult =
                await RequestExecutionHelper.ExecuteTimedHttpActionAsync(
                    counterFactory: this.counterFactory,
                    componentName: this.componentName,
                    methodName: counterInstanceName,
                    action: async () => await base.SendAsync(request, cancellationToken));

            if (outgoingApiEvent != null)
            {
                if (timedResult.Exception != null)
                {
                    outgoingApiEvent.Success = false;
                    outgoingApiEvent.ErrorMessage = timedResult.Exception.ToString();
                }
                else if (timedResult.Response == null)
                {
                    outgoingApiEvent.Success = false;
                    outgoingApiEvent.ErrorMessage = "Response was null";
                }
                else
                {
                    await this.PopulateQosEventAsync(timedResult.Response, outgoingApiEvent);
                }

                outgoingApiEvent.Finish();
            }

            // Throw any exception. Must be done after logging QoS.
            if (timedResult.Exception != null)
            {
                throw timedResult.Exception;
            }

            return timedResult.Response;
        }

        /// <summary>
        /// Populate the outgoing QoS event given a non-null HTTP response.
        /// </summary>
        /// <param name="response">The non-null response returned from the inner handler's SendAsync()</param>
        /// <param name="apiEvent">The QoS event to populate</param>
        /// <returns>Task object</returns>
        protected virtual Task PopulateQosEventAsync(HttpResponseMessage response, OutgoingApiEventWrapper apiEvent)
        {
            apiEvent.ProtocolStatusCode = ((int)response.StatusCode).ToString(CultureInfo.InvariantCulture);

            if (!response.IsSuccessStatusCode)
            {
                apiEvent.Success = false;
                apiEvent.ErrorMessage = response.StatusCode.ToString();
            }

            return Task.FromResult(0);
        }
    }
}

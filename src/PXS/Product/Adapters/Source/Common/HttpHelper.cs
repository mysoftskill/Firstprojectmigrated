// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Adapters.Common
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using Microsoft.Membership.MemberServices.Adapters.Common.Handlers;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.Common.Utilities;

    internal static class HttpHelper
    {
        /// <summary>
        /// Creates an HTTP request message ready to be sent with logging and performance tracking context. The context
        /// is to be processed with separate HTTP handlers in the processing pipeline.
        /// </summary>
        /// <param name="method">The HTTP method the request represents.</param>
        /// <param name="requestUri">The Uri request will be sent to.</param>
        /// <param name="content">The HTTP request content sent to the server.</param>
        /// <param name="outgoingApiEvent">An outgoing QoS API event to be logged.</param>
        /// <param name="counterInstance">Counter instance name.</param>
        /// <param name="headers">Headers to add in the request. Optional.</param>
        /// <returns>A new HTTP request message.</returns>
        public static HttpRequestMessage CreateHttpRequestMessage(
            HttpMethod method,
            Uri requestUri,
            HttpContent content,
            OutgoingApiEventWrapper outgoingApiEvent,
            string counterInstance,
            IDictionary<string, string> headers = null)
        {
            var request = DisposableUtilities.SafeCreate<HttpRequestMessage>(() =>
            {
                return new HttpRequestMessage();
            });

            // Add logging values to the request
            request.Method = method;
            request.RequestUri = requestUri;
            request.Content = content;
            request.Properties.Add(HandlerConstants.CounterInstanceNameKey, counterInstance);
            request.Properties.Add(HandlerConstants.ApiEventContextKey, outgoingApiEvent);

            // Add headers if necessary (append)
            if (headers != null)
            {
                foreach (KeyValuePair<string, string> header in headers)
                {
                    // TODO: This is a workaround until the Office GetSubscriptions API starts using 
                    // the standard form of the Authorization header (scheme + value).
                    // It is tracked by Bug 1381314.
                    if (header.Key.Equals("Authorization"))
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                    else
                    {
                        request.Headers.Add(header.Key, header.Value);
                    }
                }
            }

            return request;
        }
    }
}

// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Common
{
    using System;
    using System.Diagnostics;
    using System.Web;

    /// <summary>
    ///     Encapsulates all information about an individual logical operation.
    /// </summary>
    public static class LogicalWebOperationContext
    {
        /// <summary>
        ///     Gets the client-activity-id from the current http-request header.
        /// </summary>
        public static Guid ClientActivityId
        {
            get
            {
                HttpRequest currentRequest = CurrentRequest();
                if (currentRequest != null)
                {
                    // client-request-id is not a required field
                    string clientActivityIdHeaderValue = currentRequest.Headers[HeaderNames.ClientRequestId];

                    // client-request-id is a part C value we define as a guid
                    Guid clientActivityId;
                    if (Guid.TryParse(clientActivityIdHeaderValue, out clientActivityId))
                    {
                        return clientActivityId;
                    }
                }

                return Guid.Empty;
            }
        }

        /// <summary>
        ///     Gets the server-activity-id.
        /// </summary>
        public static Guid ServerActivityId
        {
            get
            {
                // REVISIT(sarubio): This mimics the behavior SLL V3 was providing for us. In the library,
                // every time an operation was started, it would check if ActivityId was an empty Guid and 
                // would assign it a new Guid if it was.
                if (Guid.Empty.Equals(Trace.CorrelationManager.ActivityId))
                {
                    Trace.CorrelationManager.ActivityId = Guid.NewGuid();
                }

                return Trace.CorrelationManager.ActivityId;
            }
        }

        /// <summary>
        ///     Gets the HttpContext.Current.Request.
        /// </summary>
        private static HttpRequest CurrentRequest()
        {
            if (HttpContext.Current != null)
            {
                try
                {
                    return HttpContext.Current.Request;
                }
                catch (HttpException)
                {
                    // No request is available. This is not necessarily an error
                    // REVIST(nnaemeka): how come?
                }
            }

            return null;
        }
    }
}

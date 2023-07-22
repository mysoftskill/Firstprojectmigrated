// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Helpers
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;

    /// <summary>
    /// PrivacyExperience RequestHelper
    /// </summary>
    public static class PrivacyExperienceRequestHelper
    {
        /// <summary>
        /// Creates the service-to-service request.
        /// </summary>
        /// <param name="httpMethod">The HTTP method to set on the request.</param>
        /// <param name="requestUri">The API path to set on the request.</param>
        /// <param name="operationName">The name of the method this request is being created by. Used for tracking the logical operation.</param>
        /// <param name="requestId">A client activity ID that will be used to correlate client and server logs.</param>
        /// <param name="correlationVector">The correlation vector.</param>
        /// <param name="authClient">The authentication client.</param>
        /// <param name="userProxyTicket">The user proxy ticket.</param>
        /// <param name="familyTicket">The family ticket (optional)</param>
        /// <returns>An HTTP request message.</returns>
        public static async Task<HttpRequestMessage> CreateS2SRequestAsync(
            HttpMethod httpMethod,
            Uri requestUri,
            string operationName,
            string requestId,
            string correlationVector,
            IPrivacyAuthClient authClient,
            string userProxyTicket,
            string familyTicket)
        {
            HttpRequestMessage request = CreateBasicRequest(httpMethod, requestUri, operationName, requestId, correlationVector);
            string accessToken = await authClient.GetAccessTokenAsync(CancellationToken.None);
            request.Headers.AddIfValid(HeaderNames.ProxyTicket, userProxyTicket);
            request.Headers.AddIfValid(HeaderNames.AccessToken, accessToken);
            request.Headers.AddIfValid(HeaderNames.FamilyTicket, familyTicket);

            return request;
        }

        /// <summary>
        /// Creates an HTTP request message with Member View specific properties and headers set.
        /// </summary>
        /// <param name="httpMethod">The HTTP method to set on the request.</param>
        /// <param name="requestUri">The API path to set on the request.</param>
        /// <param name="operationName">The name of the method this request is being created by. Used for tracking the logical operation.</param>
        /// <param name="requestId">A client activity ID that will be used to correlate client and server logs.</param>
        /// <param name="correlationVector">The correlation vector.</param>
        /// <returns>An HTTP request message.</returns>
        public static HttpRequestMessage CreateBasicRequest(
            HttpMethod httpMethod, Uri requestUri, string operationName, string requestId, string correlationVector)
        {
            //// Use pattern defined in MSDN document to safely return a disposable object.
            //// https://msdn.microsoft.com/en-us/library/ms182289.aspx

            HttpRequestMessage tempRequest = null;
            HttpRequestMessage request = null;

            try
            {
                tempRequest = new HttpRequestMessage();
                tempRequest.Method = httpMethod;
                tempRequest.RequestUri = requestUri;
                tempRequest.Properties.Add(OperationNames.PropertyKey, operationName);
                tempRequest.Headers.AddIfValid(HeaderNames.ClientRequestId, requestId);
                tempRequest.Headers.AddIfValid(HeaderNames.CorrelationVector, correlationVector);

                request = tempRequest;
                tempRequest = null;
            }
            finally
            {
                if (tempRequest != null)
                {
                    tempRequest.Dispose();
                }
            }

            return request;
        }

        /// <summary>
        /// Add the specified <paramref name="headerValue"/> to the HTTP <paramref name="headers"/>.
        /// NOTE: the specified <paramref name="headerValue"/> must not be empty.
        /// </summary>
        /// <param name="headers">The http-header collection.</param>
        /// <param name="headerKey">The http-header key to add.</param>
        /// <param name="headerValue">The http-header value to add.</param>
        internal static void AddIfValid(this HttpRequestHeaders headers, string headerKey, string headerValue)
        {
            if (!string.IsNullOrWhiteSpace(headerValue))
            {
                headers.Add(headerKey, headerValue);
            }
        }
    }
}
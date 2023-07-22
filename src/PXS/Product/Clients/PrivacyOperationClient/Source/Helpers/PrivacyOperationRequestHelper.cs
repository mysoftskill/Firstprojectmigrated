// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.PrivacyOperation.Client.Helpers
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Identity.Client;
    using Microsoft.PrivacyServices.PrivacyOperation.Client.Clients.Interfaces;

    /// <summary>
    ///     PrivacyOperationRequestHelper
    /// </summary>
    public static class PrivacyOperationRequestHelper
    {
        private const string ProxyTicket = "X-S2S-Proxy-Ticket";
        private const string CorrelationVector = "MS-CV";

        /// <summary>
        /// Creates the service-to-service request.
        /// </summary>
        /// <param name="httpMethod">The HTTP method to set on the request.</param>
        /// <param name="requestUri">The API path to set on the request.</param>
        /// <param name="operationName">The operation name</param>
        /// <param name="correlationVector">The correlation vector.</param>
        /// <param name="authClient">The authentication client.</param>
        /// <param name="userProxyTicket">The user proxy ticket.</param>
        /// <param name="userAssertion">The user assertion.</param>
        /// <returns>An HTTP request message.</returns>
        public static async Task<HttpRequestMessage> CreateS2SRequestAsync(
            HttpMethod httpMethod,
            Uri requestUri,
            string operationName,
            string correlationVector,
            IPrivacyOperationAuthClient authClient,
            string userProxyTicket = null,
            UserAssertion userAssertion = null)
        {
            HttpRequestMessage request = CreateBasicRequest(httpMethod, requestUri, operationName, correlationVector);
            AuthenticationHeaderValue authenticationHeaderValue = await authClient.GetAadAuthToken(CancellationToken.None, userAssertion).ConfigureAwait(false);
            request.Headers.Authorization = authenticationHeaderValue;
            request.Headers.AddIfValid(ProxyTicket, userProxyTicket);

            return request;
        }

        /// <summary>
        ///     Creates an HTTP request message.
        /// </summary>
        /// <param name="httpMethod">The HTTP method to set on the request.</param>
        /// <param name="requestUri">The API path to set on the request.</param>
        /// <param name="operationName">The operation name.</param>
        /// <param name="correlationVector">The correlation vector.</param>
        /// <returns>An HTTP request message.</returns>
        public static HttpRequestMessage CreateBasicRequest(HttpMethod httpMethod, Uri requestUri, string operationName, string correlationVector)
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
                tempRequest.Headers.AddIfValid(CorrelationVector, correlationVector);

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

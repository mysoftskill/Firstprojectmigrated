// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.PrivacyOperation.Client.Helpers
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.PrivacyOperation.Contracts;

    using Newtonsoft.Json;

    /// <summary>
    ///     HttpHelper.
    /// </summary>
    public static class HttpHelper
    {
        /// <summary>
        ///     Deserializes the response content of an HTTP response to the specified type and throws an exception on any error.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the content to.</typeparam>
        /// <param name="content">The response to deserialize.</param>
        /// <returns>The resulting object from deserialize the response body.</returns>
        public static async Task<T> DeserializeHttpResponseContentAsync<T>(HttpContent content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            T result;
            string responseContentValue = await content.ReadAsStringAsync().ConfigureAwait(false);

            if (content.Headers.ContentType?.MediaType == "application/json")
            {
                try
                {
                    result = JsonConvert.DeserializeObject<T>(responseContentValue);
                }
                catch (JsonException ex)
                {
                    var error = new Error(ErrorCode.Unknown, "Could not deserialize response content to given type.")
                    {
                        ErrorDetails = responseContentValue
                    };
                    throw new PrivacyOperationClientException(error, ex);
                }
            }
            else
            {
                var error = new Error(ErrorCode.Unknown, responseContentValue);
                throw new PrivacyOperationClientException(error);
            }

            return result;
        }

        /// <summary>
        ///     Handles the PrivacyOperation response by checking the http status code.
        ///     On success, the response is deserialized to the specified type.
        ///     If the http response is not successful, an exception is thrown.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the body to.</typeparam>
        /// <param name="response">The response to deserialize.</param>
        /// <returns>The resulting object from deserializing the response body.</returns>
        /// <exception cref="PrivacyOperationTransportException">Thrown if any client-transport errors occur.</exception>
        /// <exception cref="PrivacyOperationClientException">Thrown if any client errors occur.</exception>
        /// <exception cref="ArgumentNullException">Thrown if the <see cref="HttpResponseMessage" />is null, or if the response <see cref="HttpContent" /> is null.</exception>
        public static async Task<T> HandleHttpResponseAsync<T>(HttpResponseMessage response)
            where T : class
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (response.IsSuccessStatusCode)
            {
                return await DeserializeHttpResponseContentAsync<T>(response.Content).ConfigureAwait(false);
            }

            Error error;
            try
            {
                error = await DeserializeHttpResponseContentAsync<Error>(response.Content).ConfigureAwait(false);
            }
            catch (Exception)
            {
                error = new Error(ErrorCode.Unknown, "HttpStatusCode=" + ((int)response.StatusCode) + ":" + response.StatusCode + " for request" + response.RequestMessage);
            }

            throw new PrivacyOperationTransportException(error, response.StatusCode);
        }
    }
}

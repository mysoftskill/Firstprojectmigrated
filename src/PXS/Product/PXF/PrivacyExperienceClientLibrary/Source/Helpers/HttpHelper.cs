// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Helpers
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Formatting;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.PrivacySubject;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2;

    using Newtonsoft.Json;

    /// <summary>
    ///     Http Helper
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
                    JsonSerializerSettings settings;
                    if (typeof(T) == typeof(PagedResponse<TimelineCard>))
                    {
                        settings = new JsonSerializerSettings
                        {
                            SerializationBinder = new TimelineCardBinder(),
                            Converters = new JsonConverter[] { new TolerantEnumConverter() }
                        };
                    }
                    else
                    {
                        settings = new JsonSerializerSettings
                        {
                            Converters = new JsonConverter[] { new TolerantEnumConverter() }
                        };
                    }
                    result = JsonConvert.DeserializeObject<T>(responseContentValue, settings);
                }
                catch (JsonException ex)
                {
                    var error = new Error(ErrorCode.Unknown, "Could not deserialize response content to given type.")
                    {
                        ErrorDetails = responseContentValue
                    };
                    throw new PrivacyExperienceClientException(error, ex);
                }
            }
            else
            {
                var error = new Error(ErrorCode.Unknown, responseContentValue);
                throw new PrivacyExperienceClientException(error);
            }

            return result;
        }

        /// <summary>
        ///     Handles the Privacy-Experience-Service response by checking the http status code.
        ///     On success, the response is deserialized to the specified type.
        ///     If the http response is not successful, an exception is thrown.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the body to.</typeparam>
        /// <param name="response">The response to deserialize.</param>
        /// <returns>The resulting object from deserializing the response body.</returns>
        /// <exception cref="PrivacyExperienceTransportException">Thrown if any client-transport errors occur.</exception>
        /// <exception cref="PrivacyExperienceClientException">Thrown if any client errors occur.</exception>
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

            throw new PrivacyExperienceTransportException(error, response.StatusCode);
        }

        /// <summary>
        ///     Handles the Privacy-Experience-Service response by checking the http status code.
        ///     On success, no response is returned.
        ///     If the http response is not successful, an exception is thrown.
        /// </summary>
        /// <param name="response">The response to deserialize.</param>
        /// <returns>The resulting object from deserializing the response body.</returns>
        /// <exception cref="PrivacyExperienceTransportException">Thrown if any client-transport errors occur.</exception>
        /// <exception cref="PrivacyExperienceClientException">Thrown if any client errors occur.</exception>
        /// <exception cref="ArgumentNullException">Thrown if the <see cref="HttpResponseMessage" />is null, or if the response <see cref="HttpContent" /> is null.</exception>
        public static async Task HandleHttpResponseAsync(HttpResponseMessage response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (response.IsSuccessStatusCode)
            {
                return;
            }

            throw new PrivacyExperienceTransportException(await DeserializeHttpResponseContentAsync<Error>(response.Content).ConfigureAwait(false), response.StatusCode);
        }
    }
}

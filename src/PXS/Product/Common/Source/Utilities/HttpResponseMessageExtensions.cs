// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.Utilities
{
    using System.Net.Http;
    using System.Text;
    using System.Web.Http;
    using System.Web.Http.Results;

    public static class HttpResponseMessageExtensions
    {
        /// <summary>
        /// Wraps the HTTP response message and returns it as an action result.
        /// </summary>
        /// <param name="response">The response to wrap.</param>
        /// <returns>An HTTP action result wrapping around the response message.</returns>
        public static IHttpActionResult ToHttpActionResult(this HttpResponseMessage response)
        {
            return new ResponseMessageResult(response);
        }

        /// <summary>
        /// Throws an exception if the IsSuccessStatusCode property for the HTTP response is false.
        /// The exception message will contain the response's contents.
        /// </summary>
        /// <param name="response">The response to verify.</param>
        public static void EnsureSuccessStatusCodeWithContent(this HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            string message = "Response status code does not indicate success: {0} ({1}). Response: \"{2}\"".FormatInvariant(
                (int)response.StatusCode, response.StatusCode, response.ToStringWithContent());

            throw new HttpRequestException(message);
        }

        public static string ToStringWithContent(this HttpResponseMessage response)
        {
            // HttpResponseMessage.ToString() already includes "Content: <null>" so don't add again
            StringBuilder message = new StringBuilder();
            message.AppendFormat("Response: \"{0}\"", response.ToString());

            if (response.Content != null)
            {
                message.AppendFormat(", Content: \"{0}\"", response.Content.ReadAsStringAsync().Result);
            }

            return message.ToString();
        }
    }
}

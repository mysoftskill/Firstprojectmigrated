namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi
{
    using System.Net.Http;

    /// <summary>
    /// Extension functions for the <see cref="HttpResponseMessage" /> class.
    /// </summary>
    public static class HttpResponseMessageExtensions
    {
        /// <summary>
        /// Retrieves the content type of the response.
        /// </summary>
        /// <param name="response">The response object.</param>
        /// <returns>The content type or empty string if not available.</returns>
        public static string GetContentType(this HttpResponseMessage response)
        {
            return response.Content?.Headers.ContentType?.ToString() ?? string.Empty;
        }
    }
}

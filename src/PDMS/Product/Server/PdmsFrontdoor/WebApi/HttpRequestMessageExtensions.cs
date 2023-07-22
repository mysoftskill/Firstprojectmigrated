namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi
{
    using System.Net.Http;

    /// <summary>
    /// Extension functions for the <see cref="HttpRequestMessage" /> class.
    /// </summary>
    public static class HttpRequestMessageExtensions
    {
        /// <summary>
        /// The property name of the HttpContext object.
        /// </summary>
        private const string HttpContext = "MS_HttpContext";

        /// <summary>
        /// The property name of the OWIN message property object.
        /// </summary>
        private const string RemoteEndpointMessage = "System.ServiceModel.Channels.RemoteEndpointMessageProperty";

        /// <summary>
        /// Retrieves the client IP address. Works both in IIS and OWIN.
        /// </summary>
        /// <param name="request">The request object.</param>
        /// <returns>The IP address or empty string if not available.</returns>
        public static string GetClientIpAddress(this HttpRequestMessage request)
        {
            var ip = string.Empty;

            if (request.Properties.ContainsKey(HttpContext))
            {
                dynamic ctx = request.Properties[HttpContext];
                if (ctx != null)
                {
                    ip = ctx.Request.UserHostAddress;
                }
            }

            if (request.Properties.ContainsKey(RemoteEndpointMessage))
            {
                dynamic remoteEndpoint = request.Properties[RemoteEndpointMessage]; // Use dynamic to avoid direct dependencies on OWIN dlls.
                if (remoteEndpoint != null)
                {
                    ip = remoteEndpoint.Address;
                }
            }

            return ip ?? string.Empty;
        }

        /// <summary>
        /// Retrieves the request size.
        /// </summary>
        /// <param name="request">The request object.</param>
        /// <returns>The request size or -1 if not available.</returns>
        public static long GetRequestSizeBytes(this HttpRequestMessage request)
        {
            return request.Content?.Headers.ContentLength ?? -1;
        }
    }
}

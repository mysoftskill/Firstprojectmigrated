namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi
{
    using System.Collections.Generic;
    using System.Net.Http;

    /// <summary>
    /// Standard metadata for all requests/responses.
    /// </summary>
    public sealed class OperationMetadata
    {        
        /// <summary>
        /// Gets or sets the IP address.
        /// </summary>
        public string CallerIpAddress { get; set; }

        /// <summary>
        /// Gets or sets the protocol (e.g. http, https).
        /// </summary>
        public string Protocol { get; set; }

        /// <summary>
        /// Gets or sets the status code (e.g. 200, 404).
        /// </summary>
        public int ProtocolStatusCode { get; set; }

        /// <summary>
        /// Gets or sets the request method (e.g. GET, POST).
        /// </summary>
        public string RequestMethod { get; set; }

        /// <summary>
        /// Gets or sets the request size.
        /// </summary>
        public int RequestSizeBytes { get; set; }

        /// <summary>
        /// Gets or sets the response content.
        /// </summary>
        public string ResponseContentType { get; set; }

        /// <summary>
        /// Gets or sets the request URI.
        /// </summary>
        public string TargetUri { get; set; }

        /// <summary>
        /// Gets or sets the Correlation Context.
        /// </summary>
        public Dictionary<string, string> CC { get; set; }

        /// <summary>
        /// Fills in the metadata based on the request and response.
        /// </summary>
        /// <param name="request">The request object.</param>
        /// <param name="response">The response object.</param>
        /// <returns>This object.</returns>
        public OperationMetadata FillForWebApi(HttpRequestMessage request, HttpResponseMessage response)
        {
            this.FillForWebApi(request);
            this.ProtocolStatusCode = (int)response.StatusCode;
            this.ResponseContentType = response.GetContentType();
            return this;
        }

        /// <summary>
        /// Fills in the metadata based on the request.
        /// </summary>
        /// <param name="request">The request object.</param>
        /// <returns>This object.</returns>
        public OperationMetadata FillForWebApi(HttpRequestMessage request)
        {
            this.CallerIpAddress = request.GetClientIpAddress();
            this.Protocol = request.RequestUri.Scheme;
            this.RequestMethod = request.Method.Method;
            this.RequestSizeBytes = (int)request.GetRequestSizeBytes();
            this.TargetUri = request.RequestUri.AbsoluteUri;
            return this;
        }
    }
}

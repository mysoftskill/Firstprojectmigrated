namespace Microsoft.PrivacyServices.DataManagement.Client
{
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;

    /// <summary>
    /// A result from the service. Contains metadata about the call for instrumentation purposes.
    /// </summary>
    public class HttpResult : IHttpResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResult" /> class.
        /// </summary>
        /// <param name="statusCode">The request status code.</param>
        /// <param name="responseContent">The raw response content.</param>
        /// <param name="headers">The request headers.</param>
        /// <param name="httpMethod">The request http method.</param>
        /// <param name="requestUrl">The request url.</param>
        /// <param name="requestBody">The raw request body.</param>
        /// <param name="durationMilliseconds">The duration of the call.</param>
        /// <param name="operationName">The friendly name of the call.</param>
        public HttpResult(
            HttpStatusCode statusCode,
            string responseContent,
            HttpHeaders headers,
            HttpMethod httpMethod,
            string requestUrl,
            string requestBody,
            long durationMilliseconds,
            string operationName)
        {
            this.HttpStatusCode = statusCode;
            this.ResponseContent = responseContent;
            this.Headers = headers;
            this.RequestMethod = httpMethod;
            this.RequestUrl = requestUrl;
            this.RequestBody = requestBody;
            this.DurationMilliseconds = durationMilliseconds;
            this.OperationName = operationName;
        }

        /// <summary>
        /// Gets the request method used for contacting the service.
        /// </summary>
        public HttpMethod RequestMethod { get; private set; }

        /// <summary>
        /// Gets the url used to contact the service.
        /// </summary>
        public string RequestUrl { get; private set; }

        /// <summary>
        /// Gets the body of the request.
        /// </summary>
        public string RequestBody { get; private set; }

        /// <summary>
        /// Gets all headers used for the request.
        /// </summary>
        public HttpHeaders Headers { get; private set; }

        /// <summary>
        /// Gets the response status code.
        /// </summary>
        public HttpStatusCode HttpStatusCode { get; private set; }

        /// <summary>
        /// Gets the raw response content. Will be null for calls with no response.
        /// </summary>
        public string ResponseContent { get; private set; }

        /// <summary>
        /// Gets the duration of the service call.
        /// </summary>
        public long DurationMilliseconds { get; private set; }

        /// <summary>
        /// Gets the friendly name for the request.
        /// </summary>
        public string OperationName { get; private set; }
    }
}

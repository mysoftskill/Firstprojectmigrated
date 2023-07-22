namespace Microsoft.PrivacyServices.DataManagement.Client
{
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;

    /// <summary>
    /// A result from the service. Contains metadata about the call for instrumentation purposes.
    /// </summary>
    public interface IHttpResult
    {
        /// <summary>
        /// Gets the request method used for contacting the service.
        /// </summary>
        HttpMethod RequestMethod { get; }

        /// <summary>
        /// Gets the url used to contact the service.
        /// </summary>
        string RequestUrl { get; }

        /// <summary>
        /// Gets the body of the request.
        /// </summary>
        string RequestBody { get; }

        /// <summary>
        /// Gets all headers used for the request.
        /// </summary>
        HttpHeaders Headers { get; }

        /// <summary>
        /// Gets the response status code.
        /// </summary>
        HttpStatusCode HttpStatusCode { get; }

        /// <summary>
        /// Gets the raw response content. Will be null for calls with no response.
        /// </summary>
        string ResponseContent { get; }

        /// <summary>
        /// Gets the duration of the service call.
        /// </summary>
        long DurationMilliseconds { get; }

        /// <summary>
        /// Gets the friendly name for the request.
        /// </summary>
        string OperationName { get; }        
    }
}

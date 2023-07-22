namespace Microsoft.PrivacyServices.DataManagement.Client
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;

    using Newtonsoft.Json;

    /// <summary>
    /// A result from the service. Contains metadata about the call for instrumentation purposes.
    /// </summary>
    /// <typeparam name="T">The response type.</typeparam>
    public class HttpResult<T> : HttpResult, IHttpResult<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResult{T}" /> class.
        /// </summary>
        /// <param name="statusCode">The request status code.</param>
        /// <param name="responseContent">The raw response content.</param>
        /// <param name="headers">The request headers.</param>
        /// <param name="httpMethod">The request http method.</param>
        /// <param name="requestUrl">The request url.</param>
        /// <param name="requestBody">The raw request body.</param>
        /// <param name="durationMilliseconds">The duration of the call.</param>
        /// <param name="operationName">The friendly name of the call.</param>
        /// <param name="serializationSettings">Settings to deserialize the response.</param>
        public HttpResult(
            HttpStatusCode statusCode,
            string responseContent,
            HttpHeaders headers,
            HttpMethod httpMethod,
            string requestUrl,
            string requestBody,
            long durationMilliseconds,
            string operationName,
            JsonSerializerSettings serializationSettings)
            : base(statusCode, responseContent, headers, httpMethod, requestUrl, requestBody, durationMilliseconds, operationName)
        {
            if (responseContent != null)
            {
                try
                {
                    this.Response = JsonConvert.DeserializeObject<T>(responseContent, serializationSettings);
                }
                catch (Exception)
                {
                    // trying again to solve the bug - https://msdata.visualstudio.com/ADG_Compliance_Services/_workitems/edit/1532040/
                    // From the below SO discussion, the RC is the locking of the List happening under the function JsonConvert.DeserializeObject.
                    // But here we cannot use toList and make a copy of the list, so retrying is the solution
                    // https://stackoverflow.com/questions/604831/collection-was-modified-enumeration-operation-may-not-execute
                    try
                    {
                        this.Response = JsonConvert.DeserializeObject<T>("["+responseContent+"]", serializationSettings);
                    }
                    catch (Exception ex2)
                    {
                        Trace.TraceInformation($"Could not deserialize response from operation {operationName}: {responseContent}. {ex2}");
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResult{T}" /> class.
        /// Creates a new result from an existing result but changes it's response object.
        /// </summary>
        /// <param name="result">The original result.</param>
        /// <param name="value">The new response value.</param>
        public HttpResult(IHttpResult result, T value)
            : base(
                  result.HttpStatusCode,
                  result.ResponseContent,
                  result.Headers,
                  result.RequestMethod,
                  result.RequestUrl,
                  result.RequestBody,
                  result.DurationMilliseconds,
                  result.OperationName)
        {
            this.Response = value;
        }

        /// <summary>
        /// Gets the response as a strongly typed object.
        /// </summary>
        public T Response { get; private set; }
    }
}

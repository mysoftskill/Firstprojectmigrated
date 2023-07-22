namespace Microsoft.PrivacyServices.DataManagement.Client
{
    using System;

    /// <summary>
    /// An exception that is thrown when the call results in a failure due to the provided data.
    /// These errors are documented responses for the service and should be handled by the calling service.
    /// </summary>
    [Serializable]
    public class CallerError : BaseException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CallerError" /> class.
        /// </summary>
        /// <param name="result">The result that had this error.</param>
        /// <param name="responseError">The parsed response data.</param>
        protected internal CallerError(IHttpResult result, ResponseError responseError)
            : base(result, responseError)
        {
        }

        /// <summary>
        /// Creates a strongly typed CallerError by analyzing the response error codes.
        /// </summary>
        /// <param name="result">The result that had this error.</param>
        /// <param name="version">The exception version to use.</param>
        /// <returns>The strongly typed error object.</returns>
        public static CallerError Create(IHttpResult result, int version)
        {
            var responseError = BaseException.ParseResponse(result);

            CallerError error = null;

            switch (version)
            {
                case 2:
                    error = ParseV2(result, responseError);
                    break;
            }

            return error ?? new CallerError(result, responseError);
        }

        private static CallerError ParseV2(IHttpResult result, ResponseError responseError)
        {
            // At some point, this may need to take a version number to match the correct error response.
            switch (responseError.Code)
            {
                case "BadArgument":
                    return V2.BadArgumentError.Parse(result, responseError);
                case "Conflict":
                    return V2.ConflictError.Parse(result, responseError);
                case "NotFound":
                    return V2.NotFoundError.Parse(result, responseError);
                case "Expired":
                    return V2.ExpiredError.Parse(result, responseError);
                case "NotAuthenticated":
                    return V2.NotAuthenticatedError.Parse(result, responseError);
                case "NotAuthorized":
                    return V2.NotAuthorizedError.Parse(result, responseError);
                default:
                    return null;
            }
        }
    }
}
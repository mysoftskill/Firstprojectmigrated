namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System;

    /// <summary>
    /// A strongly typed error from the service. Indicates that the request is not authenticated.
    /// </summary>
    [Serializable]
    public class NotAuthenticatedError : CallerError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotAuthenticatedError" /> class.
        /// </summary>
        /// <param name="result">The http result.</param>
        /// <param name="responseError">The parsed response data.</param>
        internal NotAuthenticatedError(IHttpResult result, ResponseError responseError) : base(result, responseError)
        {
        }

        /// <summary>
        /// Parses the response error information and converts it into a strong error type.
        /// </summary>
        /// <param name="result">The http result.</param>
        /// <param name="responseError">The parsed response data.</param>
        /// <returns>The strongly typed error object.</returns>
        public static CallerError Parse(IHttpResult result, ResponseError responseError)
        {
            return SubError_1(result, responseError) ?? new NotAuthenticatedError(result, responseError);
        }

        private static CallerError SubError_1(IHttpResult _, ResponseError responseError)
        {
            switch (responseError.InnerError?.Code)
            {
                default:
                    return null;
            }
        }
    }
}
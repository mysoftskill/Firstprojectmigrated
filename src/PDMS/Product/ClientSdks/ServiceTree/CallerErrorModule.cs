namespace Microsoft.PrivacyServices.DataManagement.Client.ServiceTree
{
    /// <summary>
    /// Helper methods for working with CallerErrors.
    /// </summary>
    public static class CallerErrorModule
    {
        /// <summary>
        /// Parses the response error information and converts it into a strong error type.
        /// </summary>
        /// <param name="result">The http result.</param>
        /// <param name="responseError">The parsed response error data.</param>  
        /// <returns>The strongly typed error object.</returns>
        public static CallerError Create(IHttpResult result, ResponseError responseError)
        {
            CallerError error = NotFoundError.Create(result, responseError);

            if (error == null)
            {
                // Do the next error conversion attempt.
                switch (responseError.Code)
                {
                    case "Forbidden":
                        return NotAuthorizedError.Create(result, responseError);
                    default:
                        return null;
                }
            }

            return error;
        }
    }
}
namespace Microsoft.PrivacyServices.DataManagement.Client.ServiceTree
{
    using System;
    using System.Runtime.Serialization;
    using System.Text.RegularExpressions;

    using Newtonsoft.Json;

    /// <summary>
    /// A strongly typed error from the service. Indicates that a requested resource is not found.
    /// </summary>
    [Serializable]
    public class NotAuthorizedError : CallerError
    {
        private static readonly Regex NotAuthorizedRegex = new Regex(@"^Error 403 - Forbidden: ([\w]*)\.$", RegexOptions.Compiled);

        /// <summary>
        /// Initializes a new instance of the <see cref="NotAuthorizedError" /> class.
        /// </summary>
        /// <param name="result">The http result.</param>
        /// <param name="responseError">The parsed response data.</param>
        internal NotAuthorizedError(IHttpResult result, ResponseError responseError) : base(result, responseError)
        {
        }

        /// <summary>
        /// Required by ISerializable.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The serialization context.</param>
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Parses the response error information and converts it into a strong error type.
        /// </summary>
        /// <param name="result">The http result.</param>
        /// <param name="responseError">The parsed response data.</param>
        /// <returns>The strongly typed error object.</returns>
        internal static CallerError Create(IHttpResult result, ResponseError responseError)
        {
            if (result.HttpStatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                responseError.Code = "NotAuthorized";
                var error = new NotAuthorizedError(result, responseError);

                return error;
            }
            
            return null;
        }
    }
}
namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System;
    using System.Runtime.Serialization;

    using Newtonsoft.Json;

    /// <summary>
    /// A strongly typed error from the service. Indicates that a request is stale due to various reason.
    /// </summary>
    [Serializable]
    public class ExpiredError : CallerError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExpiredError" /> class.
        /// </summary>
        /// <param name="result">The http result.</param>
        /// <param name="responseError">The parsed response data.</param>
        internal ExpiredError(IHttpResult result, ResponseError responseError) : base(result, responseError)
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
            return SubError_1(result, responseError) ?? new ExpiredError(result, responseError);
        }

        private static CallerError SubError_1(IHttpResult result, ResponseError responseError)
        {
            switch (responseError.InnerError?.Code)
            {
                case "ETagMismatch":
                    return new ETagMismatch(result, responseError);
                default:
                    return null;
            }
        }

        #region Specific errors
        /// <summary>
        /// A ExpiredError specifically for when ETag does not match with persisted entity.
        /// </summary>
        [Serializable]
        public class ETagMismatch : ExpiredError
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ETagMismatch" /> class.
            /// </summary>
            /// <param name="result">The http result.</param>
            /// <param name="responseError">The parsed response data.</param>
            internal ETagMismatch(IHttpResult result, ResponseError responseError) : base(result, responseError)
            {
                this.Value = responseError.InnerError.Data["value"] as string;
            }

            /// <summary>
            /// Gets the value that was incorrect.
            /// </summary>
            [JsonProperty]
            public string Value { get; private set; }

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
        }
        #endregion
    }
}
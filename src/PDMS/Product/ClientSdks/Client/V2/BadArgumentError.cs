namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System;
    using System.Linq;
    using System.Runtime.Serialization;

    using Newtonsoft.Json;

    /// <summary>
    /// A strongly typed error from the service. Indicates that something is wrong with the provided data.
    /// </summary>
    [Serializable]
    public class BadArgumentError : CallerError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BadArgumentError" /> class.
        /// </summary>
        /// <param name="result">The http result.</param>
        /// <param name="responseError">The parsed response data.</param>
        internal BadArgumentError(IHttpResult result, ResponseError responseError) : base(result, responseError)
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
            if (responseError.Details != null && responseError.Details.Any())
            {
                return new Multiple(result, responseError);
            }
            else
            {
                return SubError_1(result, responseError) ?? new BadArgumentError(result, responseError);
            }
        }

        private static CallerError SubError_1(IHttpResult result, ResponseError responseError)
        {
            switch (responseError.InnerError?.Code)
            {
                case "NullValue":
                    return new NullArgument(result, responseError);
                case "InvalidValue":
                    return SubError_2(result, responseError) ?? new InvalidArgument(result, responseError);
                default:
                    return null;
            }
        }

        private static CallerError SubError_2(IHttpResult result, ResponseError responseError)
        {
            switch (responseError.InnerError?.InnerError?.Code)
            {
                case "UnsupportedCharacter":
                    return new InvalidArgument.UnsupportedCharacter(result, responseError);
                case "MutuallyExclusive":
                    return new InvalidArgument.MutuallyExclusive(result, responseError);
                default:
                    return null;
            }
        }

        #region Specific errors
        /// <summary>
        /// A BadArgumentError specifically for when multiple bad argument errors are returned for a single request.
        /// </summary>
        [Serializable]
        public class Multiple : BadArgumentError
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Multiple" /> class.
            /// </summary>
            /// <param name="result">The http result.</param>
            /// <param name="responseError">The parsed response data.</param>
            internal Multiple(IHttpResult result, ResponseError responseError) : base(result, responseError)
            {
                this.Details = responseError.Details;
            }

            /// <summary>
            /// Gets the details for the multiple errors.
            /// </summary>
            [JsonProperty]
            public ResponseError.Detail[] Details { get; private set; }

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

        /// <summary>
        /// A BadArgumentError specifically for when the given field has malformed data.
        /// </summary>
        [Serializable]
        public class InvalidArgument : BadArgumentError
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="InvalidArgument" /> class.
            /// </summary>
            /// <param name="result">The http result.</param>
            /// <param name="responseError">The parsed response data.</param>
            internal InvalidArgument(IHttpResult result, ResponseError responseError) : base(result, responseError)
            {
                this.Target = responseError.Target;

                if (responseError.InnerError.Data.ContainsKey("value"))
                {
                    this.Value = responseError.InnerError.Data["value"] as string;
                }
            }

            /// <summary>
            /// Gets the source of the error.
            /// </summary>
            [JsonProperty]
            public string Target { get; private set; }

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

            /// <summary>
            /// An InvalidArgument error that indicates the value had unsupported characters in it.
            /// </summary>
            [Serializable]
            public class UnsupportedCharacter : InvalidArgument
            {
                /// <summary>
                /// Initializes a new instance of the <see cref="UnsupportedCharacter" /> class.
                /// </summary>
                /// <param name="result">The http result.</param>
                /// <param name="responseError">The parsed response data.</param>
                internal UnsupportedCharacter(IHttpResult result, ResponseError responseError) : base(result, responseError)
                {
                }
            }

            /// <summary>
            /// An InvalidArgument error that indicates the property violates the mutual exclusiveness.
            /// </summary>
            [Serializable]
            public class MutuallyExclusive : InvalidArgument
            {
                /// <summary>
                /// Initializes a new instance of the <see cref="MutuallyExclusive" /> class.
                /// </summary>
                /// <param name="result">The http result.</param>
                /// <param name="responseError">The parsed response data.</param>
                internal MutuallyExclusive(IHttpResult result, ResponseError responseError) : base(result, responseError)
                {
                    if (responseError.InnerError.InnerError.Data.ContainsKey("source"))
                    {
                        this.Source = responseError.InnerError.InnerError.Data["source"] as string;
                    }
                }

                /// <summary>
                /// Gets the source property name that causes the mutual exclusiveness.
                /// </summary>
                [JsonProperty]
                public new string Source { get; private set; }

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
        }

        /// <summary>
        /// A BadArgumentError specifically for when the given field is missing.
        /// </summary>
        [Serializable]
        public class NullArgument : BadArgumentError
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="NullArgument" /> class.
            /// </summary>
            /// <param name="result">The http result.</param>
            /// <param name="responseError">The parsed response data.</param>
            internal NullArgument(IHttpResult result, ResponseError responseError) : base(result, responseError)
            {
                this.Target = responseError.Target;
            }

            /// <summary>
            /// Gets the source of the error.
            /// </summary>
            [JsonProperty]
            public string Target { get; private set; }

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
namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System;
    using System.Runtime.Serialization;

    using Newtonsoft.Json;

    /// <summary>
    /// A strongly typed error from the service. Indicates that something is wrong with the provided data.
    /// </summary>
    [Serializable]
    public class ConflictError : CallerError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConflictError" /> class.
        /// </summary>
        /// <param name="result">The http result.</param>
        /// <param name="responseError">The parsed response data.</param>
        internal ConflictError(IHttpResult result, ResponseError responseError) : base(result, responseError)
        {
            this.Target = responseError.Target;
        }

        /// <summary>
        /// Gets the property that had a conflict.
        /// </summary>
        [JsonProperty]
        public string Target { get; private set; }

        /// <summary>
        /// Parses the response error information and converts it into a strong error type.
        /// </summary>
        /// <param name="result">The http result.</param>
        /// <param name="responseError">The parsed response data.</param>
        /// <returns>The strongly typed error object.</returns>
        public static CallerError Parse(IHttpResult result, ResponseError responseError)
        {
            return SubError_1(result, responseError) ?? new ConflictError(result, responseError);
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

        private static CallerError SubError_1(IHttpResult result, ResponseError responseError)
        {
            switch (responseError.InnerError?.Code)
            {
                case "InvalidValue":
                    return SubError_2(result, responseError) ?? new InvalidValue(result, responseError);
                case "NullValue":
                    return new NullValue(result, responseError);
                case "DoesNotExist":
                    return new DoesNotExist(result, responseError);
                case "AlreadyExists":
                    return SubError_2(result, responseError) ?? new AlreadyExists(result, responseError);
                case "MaxExpansionSizeExceeded":
                    return new MaxExpansionSizeExceeded(result, responseError);
                case "LinkedEntityExists":
                    return new LinkedEntityExists(result, responseError);
                case "PendingCommandsExists":
                    return new PendingCommandsExists(result, responseError);
                default:
                    return null;
            }
        }

        private static CallerError SubError_2(IHttpResult result, ResponseError responseError)
        {
            switch (responseError.InnerError?.InnerError?.Code)
            {
                case "Immutable":
                    return new InvalidValue.Immutable(result, responseError);
                case "StateTransition":
                    return new InvalidValue.StateTransition(result, responseError);
                case "BadCombination":
                    return new InvalidValue.BadCombination(result, responseError);
                case "ClaimedByOwner":
                    return new AlreadyExists.ClaimedByOwner(result, responseError);
                default:
                    return null;
            }
        }

        #region Specific errors
        /// <summary>
        /// A ConflictError specifically for InvalidValue issues.
        /// </summary>
        [Serializable]
        public class InvalidValue : ConflictError
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ConflictError.InvalidValue" /> class.
            /// </summary>
            /// <param name="result">The http result.</param>
            /// <param name="responseError">The parsed response data.</param>
            internal InvalidValue(IHttpResult result, ResponseError responseError) : base(result, responseError)
            {
                if (responseError.InnerError.Data.ContainsKey("value"))
                {
                    this.Value = responseError.InnerError.Data["value"] as string;
                }
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

            /// <summary>
            /// An InvalidValue error that indicates that the target cannot be modified.
            /// </summary>
            [Serializable]
            public class Immutable : InvalidValue
            {
                /// <summary>
                /// Initializes a new instance of the <see cref="ConflictError.InvalidValue.Immutable" /> class.
                /// </summary>
                /// <param name="result">The http result.</param>
                /// <param name="responseError">The parsed response data.</param>
                internal Immutable(IHttpResult result, ResponseError responseError) : base(result, responseError)
                {
                }
            }

            /// <summary>
            /// An InvalidValue error that indicates that the value specified for the target
            /// is not a valid transition value based on the expected transition flows.
            /// </summary>
            [Serializable]
            public class StateTransition : InvalidValue
            {
                /// <summary>
                /// Initializes a new instance of the <see cref="ConflictError.InvalidValue.StateTransition" /> class.
                /// </summary>
                /// <param name="result">The http result.</param>
                /// <param name="responseError">The parsed response data.</param>
                internal StateTransition(IHttpResult result, ResponseError responseError) : base(result, responseError)
                {
                }
            }

            /// <summary>
            /// An InvalidValue error that indicates an issue with the specific value and a combination of properties.
            /// The Target field contains the conflicting properties in a comma separated list.
            /// </summary>
            [Serializable]
            public class BadCombination : InvalidValue
            {
                /// <summary>
                /// Initializes a new instance of the <see cref="ConflictError.InvalidValue.BadCombination" /> class.
                /// </summary>
                /// <param name="result">The http result.</param>
                /// <param name="responseError">The parsed response data.</param>
                internal BadCombination(IHttpResult result, ResponseError responseError) : base(result, responseError)
                {
                }
            }
        }

        /// <summary>
        /// A ConflictError specifically for NullValue issues.
        /// </summary>
        [Serializable]
        public class NullValue : ConflictError
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ConflictError.NullValue" /> class.
            /// </summary>
            /// <param name="result">The http result.</param>
            /// <param name="responseError">The parsed response data.</param>
            internal NullValue(IHttpResult result, ResponseError responseError) : base(result, responseError)
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
        }

        /// <summary>
        /// A ConflictError specifically for DoesNotExist issues.
        /// </summary>
        [Serializable]
        public class DoesNotExist : ConflictError
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ConflictError.DoesNotExist" /> class.
            /// </summary>
            /// <param name="result">The http result.</param>
            /// <param name="responseError">The parsed response data.</param>
            internal DoesNotExist(IHttpResult result, ResponseError responseError) : base(result, responseError)
            {
                if (responseError.InnerError.Data.ContainsKey("value"))
                {
                    this.Value = responseError.InnerError.Data["value"] as string;
                }
            }

            /// <summary>
            /// Gets the value that does not exist.
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

        /// <summary>
        /// A ConflictError specifically for AlreadyExists issues.
        /// </summary>
        [Serializable]
        public class AlreadyExists : ConflictError
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ConflictError.AlreadyExists" /> class.
            /// </summary>
            /// <param name="result">The http result.</param>
            /// <param name="responseError">The parsed response data.</param>
            internal AlreadyExists(IHttpResult result, ResponseError responseError) : base(result, responseError)
            {
                if (responseError.InnerError.Data.ContainsKey("value"))
                {
                    this.Value = responseError.InnerError.Data["value"] as string;
                }
            }

            /// <summary>
            /// Gets the value that already exists.
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
            /// An AlreadyExists error that indicates that the entity is claimed by another owner.
            /// </summary>
            [Serializable]
            public class ClaimedByOwner : AlreadyExists
            {
                /// <summary>
                /// Initializes a new instance of the <see cref="ConflictError.AlreadyExists.ClaimedByOwner" /> class.
                /// </summary>
                /// <param name="result">The http result.</param>
                /// <param name="responseError">The parsed response data.</param>
                internal ClaimedByOwner(IHttpResult result, ResponseError responseError) : base(result, responseError)
                {
                    if (responseError.InnerError.InnerError.Data.ContainsKey("ownerid"))
                    {
                        this.OwnerId = responseError.InnerError.InnerError.Data["ownerid"] as string;
                    }
                }

                /// <summary>
                /// Gets the value that already exists.
                /// </summary>
                [JsonProperty]
                public string OwnerId { get; private set; }

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
        /// A ConflictError specifically for MaxExpansionSizeExceeded issues.
        /// </summary>
        [Serializable]
        public class MaxExpansionSizeExceeded : ConflictError
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ConflictError.MaxExpansionSizeExceeded" /> class.
            /// </summary>
            /// <param name="result">The http result.</param>
            /// <param name="responseError">The parsed response data.</param>
            internal MaxExpansionSizeExceeded(IHttpResult result, ResponseError responseError) : base(result, responseError)
            {
                if (responseError.InnerError.Data.ContainsKey("value"))
                {
                    this.Value = responseError.InnerError.Data["value"] as string;
                }
            }

            /// <summary>
            /// Gets the total items found.
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

        /// <summary>
        /// A ConflictError specifically for DependencyFound issues.
        /// </summary>
        [Serializable]
        public class LinkedEntityExists : ConflictError
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ConflictError.LinkedEntityExists" /> class.
            /// </summary>
            /// <param name="result">The http result.</param>
            /// <param name="responseError">The parsed response data.</param>
            internal LinkedEntityExists(IHttpResult result, ResponseError responseError) : base(result, responseError)
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
        }

        /// <summary>
        /// A ConflictError specifically for Pending commands found.
        /// </summary>
        [Serializable]
        public class PendingCommandsExists : ConflictError
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ConflictError.PendingCommandsExists" /> class.
            /// </summary>
            /// <param name="result">The http result.</param>
            /// <param name="responseError">The parsed response data.</param>
            internal PendingCommandsExists(IHttpResult result, ResponseError responseError) : base(result, responseError)
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
        }
        #endregion
    }
}
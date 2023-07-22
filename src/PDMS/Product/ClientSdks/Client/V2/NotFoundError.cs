namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System;
    using System.Runtime.Serialization;

    using Newtonsoft.Json;

    /// <summary>
    /// A strongly typed error from the service. Indicates that a requested resource is not found.
    /// </summary>
    [Serializable]
    public class NotFoundError : CallerError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundError" /> class.
        /// </summary>
        /// <param name="result">The http result.</param>
        /// <param name="responseError">The parsed response data.</param>
        internal NotFoundError(IHttpResult result, ResponseError responseError) : base(result, responseError)
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
            return SubError_1(result, responseError) ?? new NotFoundError(result, responseError);
        }

        private static CallerError SubError_1(IHttpResult result, ResponseError responseError)
        {
            switch (responseError.InnerError?.Code)
            {
                case "Entity":
                    return new Entity(result, responseError);
                case "ServiceTree":
                    return SubError_2(result, responseError) ?? new ServiceTree(result, responseError);
                case "SecurityGroup":
                    return new SecurityGroup(result, responseError);
                default:
                    return null;
            }
        }

        private static CallerError SubError_2(IHttpResult result, ResponseError responseError)
        {
            switch (responseError.InnerError?.InnerError?.Code)
            {
                case "Service":
                    return new ServiceTree.Service(result, responseError);
                default:
                    return null;
            }
        }

        #region Specific errors
        /// <summary>
        /// A NotFoundError for when the entity is requested.
        /// </summary>
        [Serializable]
        public class Entity : NotFoundError
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Entity" /> class.
            /// </summary>
            /// <param name="result">The http result.</param>
            /// <param name="responseError">The parsed response data.</param>
            internal Entity(IHttpResult result, ResponseError responseError) : base(result, responseError)
            {
                this.Id = Guid.Parse(responseError.InnerError.Data["id"] as string);
                this.Type = responseError.InnerError.Data["type"] as string;
            }

            /// <summary>
            /// Gets the id that could not be found.
            /// </summary>
            [JsonProperty]
            public Guid Id { get; private set; }

            /// <summary>
            /// Gets the entity type that could not be found.
            /// </summary>
            [JsonProperty]
            public string Type { get; private set; }

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
        /// A NotFoundError for when the service tree entity is requested.
        /// </summary>
        [Serializable]
        public class ServiceTree : NotFoundError
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ServiceTree" /> class.
            /// </summary>
            /// <param name="result">The http result.</param>
            /// <param name="responseError">The parsed response data.</param>
            internal ServiceTree(IHttpResult result, ResponseError responseError) : base(result, responseError)
            {
                this.Id = Guid.Parse(responseError.InnerError.Data["id"] as string);
            }

            /// <summary>
            /// Gets the id that could not be found.
            /// </summary>
            [JsonProperty]
            public Guid Id { get; private set; }

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
            /// A ServiceTree error that indicates the service tree service entity is not found.
            /// </summary>
            [Serializable]
            public class Service : ServiceTree
            {
                /// <summary>
                /// Initializes a new instance of the <see cref="Service" /> class.
                /// </summary>
                /// <param name="result">The http result.</param>
                /// <param name="responseError">The parsed response data.</param>
                internal Service(IHttpResult result, ResponseError responseError) : base(result, responseError)
                {
                }
            }
        }

        /// <summary>
        /// A NotFoundError for when the security group is requested.
        /// </summary>
        [Serializable]
        public class SecurityGroup : NotFoundError
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="SecurityGroup" /> class.
            /// </summary>
            /// <param name="result">The http result.</param>
            /// <param name="responseError">The parsed response data.</param>
            internal SecurityGroup(IHttpResult result, ResponseError responseError) : base(result, responseError)
            {
                this.Id = Guid.Parse(responseError.InnerError.Data["id"] as string);
            }

            /// <summary>
            /// Gets the id that could not be found.
            /// </summary>
            [JsonProperty]
            public Guid Id { get; private set; }

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
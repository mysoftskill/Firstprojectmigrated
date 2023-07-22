namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System;
    using System.Runtime.Serialization;

    using Newtonsoft.Json;

    /// <summary>
    /// A strongly typed error from the service. Indicates that the request is not authorized.
    /// </summary>
    [Serializable]
    public class NotAuthorizedError : CallerError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotAuthorizedError" /> class.
        /// </summary>
        /// <param name="result">The http result.</param>
        /// <param name="responseError">The parsed response data.</param>
        internal NotAuthorizedError(IHttpResult result, ResponseError responseError) : base(result, responseError)
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
            return SubError_1(result, responseError) ?? new NotAuthorizedError(result, responseError);
        }

        private static CallerError SubError_1(IHttpResult result, ResponseError responseError)
        {
            switch (responseError.InnerError?.Code)
            {
                case "User":
                    return SubError_2(result, responseError) ?? new User(result, responseError);
                case "Application":
                    return new Application(result, responseError);
                default:
                    return null;
            }
        }

        private static CallerError SubError_2(IHttpResult result, ResponseError responseError)
        {
            switch (responseError.InnerError?.InnerError?.Code)
            {
                case "ServiceTree":
                    return new User.ServiceTree(result, responseError);
                case "SecurityGroup":
                    return new User.SecurityGroup(result, responseError);
                default:
                    return null;
            }
        }

        #region Specific errors
        /// <summary>
        /// A NotAuthorizedError for when the user does not have write permissions.
        /// </summary>
        [Serializable]
        public class User : NotAuthorizedError
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="User" /> class.
            /// </summary>
            /// <param name="result">The http result.</param>
            /// <param name="responseError">The parsed response data.</param>
            internal User(IHttpResult result, ResponseError responseError) : base(result, responseError)
            {
                this.UserName = responseError.InnerError.Data["userName"] as string;
                this.Role = responseError.InnerError.Data["role"] as string;
            }

            /// <summary>
            /// Gets the user name that does not have write permissions.
            /// </summary>
            [JsonProperty]
            public string UserName { get; private set; }

            /// <summary>
            /// Gets the missing role that triggered this error.
            /// </summary>
            [JsonProperty]
            public string Role { get; private set; }

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
            /// A NotAuthorized:User error that indicates the user does not have service tree write permissions.
            /// </summary>
            [Serializable]
            public class ServiceTree : User
            {
                /// <summary>
                /// Initializes a new instance of the <see cref="ServiceTree" /> class.
                /// </summary>
                /// <param name="result">The http result.</param>
                /// <param name="responseError">The parsed response data.</param>
                internal ServiceTree(IHttpResult result, ResponseError responseError) : base(result, responseError)
                {
                    if (responseError.InnerError.InnerError.Data.ContainsKey("serviceId"))
                    {
                        this.ServiceId = responseError.InnerError.InnerError.Data["serviceId"] as string;
                    }
                }

                /// <summary>
                /// Gets the service id that the user is not in the admin list.
                /// </summary>
                [JsonProperty]
                public string ServiceId { get; private set; }

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
            /// A NotAuthorized:User error that indicates the user does not have security group write permissions.
            /// </summary>
            [Serializable]
            public class SecurityGroup : User
            {
                /// <summary>
                /// Initializes a new instance of the <see cref="SecurityGroup" /> class.
                /// </summary>
                /// <param name="result">The http result.</param>
                /// <param name="responseError">The parsed response data.</param>
                internal SecurityGroup(IHttpResult result, ResponseError responseError) : base(result, responseError)
                {
                    if (responseError.InnerError.InnerError.Data.ContainsKey("securityGroups"))
                    {
                        this.SecurityGroups = responseError.InnerError.InnerError.Data["securityGroups"] as string;
                    }
                }

                /// <summary>
                /// Gets the security groups that the user is not part of.
                /// </summary>
                [JsonProperty]
                public string SecurityGroups { get; private set; }

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
        /// A NotAuthorizedError for when the application does not have ACL permissions.
        /// </summary>
        [Serializable]
        public class Application : NotAuthorizedError
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Application" /> class.
            /// </summary>
            /// <param name="result">The http result.</param>
            /// <param name="responseError">The parsed response data.</param>
            internal Application(IHttpResult result, ResponseError responseError) : base(result, responseError)
            {
                this.ApplicationId = responseError.InnerError.Data["applicationId"] as string;
            }

            /// <summary>
            /// Gets the application id that does not have ACL permissions.
            /// </summary>
            [JsonProperty]
            public string ApplicationId { get; private set; }

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
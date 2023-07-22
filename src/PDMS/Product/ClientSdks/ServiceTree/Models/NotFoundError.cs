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
    public class NotFoundError : CallerError
    {
        private static readonly Regex NotFoundRegex = new Regex(@"The (?:service object|service group|team group|organization hierarchy object) with provided ID ([\w-]{36}) does not exist\.", RegexOptions.Compiled);
        private static readonly Regex NotFoundRegexUrl = new Regex(@"\/api\/(?:Services|ServiceGroups|TeamGroups|OrganizationHierarchy)\(([\w-]{36})\)", RegexOptions.Compiled);

        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundError" /> class.
        /// Dummy public constructor only used for unit test purpose.
        /// </summary>
        /// <param name="id">Entity ID.</param>
        public NotFoundError(Guid id) : base(null, null)
        {
            this.Id = id;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundError" /> class.
        /// </summary>
        /// <param name="result">The http result.</param>
        /// <param name="responseError">The parsed response data.</param>
        internal NotFoundError(IHttpResult result, ResponseError responseError) : base(result, responseError)
        {
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
        /// Parses the response error information and converts it into a strong error type.
        /// </summary>
        /// <param name="result">The http result.</param>
        /// <param name="responseError">The parsed response data.</param>
        /// <returns>The strongly typed error object.</returns>
        internal static CallerError Create(IHttpResult result, ResponseError responseError)
        {
            if (result.HttpStatusCode == System.Net.HttpStatusCode.NotFound
             || result.HttpStatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                responseError.Code = "NotFound";
                var error = new NotFoundError(result, responseError);

                var match = NotFoundRegexUrl.Match(result.RequestUrl);
                if (match.Success)
                {
                    error.Id = Guid.Parse(match.Groups[1].Value);
                }

                return error;
            }
            else
            {
                // This is how service tree used to return not found.
                // Keeping this code just in case the behavior reverts unexpectedly.
                var match = NotFoundRegex.Match(responseError.Message);
                if (match.Success)
                {
                    responseError.Code = "NotFound";
                    var error = new NotFoundError(result, responseError);
                    error.Id = Guid.Parse(match.Groups[1].Value);
                    return error;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Runtime.Serialization;

    /// <summary>
    /// Indicates issues with multiple arguments.
    /// </summary>
    [Serializable]
    public sealed class MultiBadArgumentError : ServiceException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MultiBadArgumentError" /> class.
        /// </summary>
        /// <param name="errors">An array of argument errors.</param>
        public MultiBadArgumentError(BadArgumentError[] errors)
            : base(HttpStatusCode.BadRequest, new ServiceError("BadArgument", "Multiple arguments are incorrect."))
        {
            this.ServiceError.Details = errors.Select(e => e.ToDetail()).ToArray();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiBadArgumentError" /> class.
        /// Required by ISerializable.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The serialization context.</param>
        public MultiBadArgumentError(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}

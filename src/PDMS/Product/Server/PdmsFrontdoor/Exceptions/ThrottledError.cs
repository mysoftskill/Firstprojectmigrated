namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions
{
    using System;
    using System.Net;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Indicates an issue with authentication.
    /// </summary>
    [Serializable]
    public class ThrottledError : ServiceException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ThrottledError" /> class.
        /// </summary>
        /// <param name="message">A friendly error message.</param>    
        /// <param name="rateLimit">The rate limit.</param>
        /// <param name="rateLimitPeriod">The rate limit period.</param>
        public ThrottledError(string message, long rateLimit, string rateLimitPeriod)
            : base((HttpStatusCode)429, new ServiceError("Throttled", message))
        {
            this.ServiceError.InnerError = new LimitExceededInnerError(rateLimit, rateLimitPeriod);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThrottledError" /> class.
        /// Required by ISerializable.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The serialization context.</param>
        protected ThrottledError(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// A custom inner error that includes the value of the bad argument.
        /// </summary>
        public class LimitExceededInnerError : InnerError
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="LimitExceededInnerError" /> class.
            /// </summary>
            /// <param name="rateLimit">The rate limit.</param>
            /// <param name="rateLimitPeriod">The rate limit period.</param>
            public LimitExceededInnerError(long rateLimit, string rateLimitPeriod) : base("LimitExceeded")
            {
                this.RateLimit = rateLimit;
                this.RateLimitPeriod = rateLimitPeriod;
            }

            /// <summary>
            /// Gets or sets the rate limit.
            /// </summary>
            [JsonProperty(PropertyName = "rateLimit")]
            public long RateLimit { get; set; }

            /// <summary>
            /// Gets or sets the rate limit period.
            /// </summary>
            [JsonProperty(PropertyName = "rateLimitPeriod")]
            public string RateLimitPeriod { get; set; }
        }
    }
}
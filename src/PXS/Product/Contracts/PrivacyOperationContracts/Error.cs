// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.PrivacyOperation.Contracts
{
    using System.Globalization;

    using Newtonsoft.Json;

    /// <summary>
    /// Error Response
    /// </summary>
    public class Error
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Error"/> class.
        /// </summary>
        /// <param name="code">The <see cref="ErrorCode"/>.</param>
        /// <param name="message">The message.</param>
        public Error(ErrorCode code, string message)
        {
            this.Code = code.ToString();
            this.Message = message;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Error"/> class.
        /// </summary>
        public Error()
        {
        }

        /// <summary>
        /// Gets or sets the error code.
        /// </summary>
        [JsonProperty("code")]
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the target.
        /// </summary>
        [JsonProperty("target", NullValueHandling = NullValueHandling.Ignore)]
        public string Target { get; set; }

        /// <summary>
        /// Gets or sets the error details.
        /// </summary>
        [JsonProperty("errorDetails", NullValueHandling = NullValueHandling.Ignore)]
        public string ErrorDetails { get; set; }

        /// <summary>
        /// Gets or sets the inner error.
        /// </summary>
        [JsonProperty("innerError", NullValueHandling = NullValueHandling.Ignore)]
        public Error InnerError { get; set; }

        /// <summary>
        /// Gets or sets the tracking identifier.
        /// </summary>
        [JsonProperty("trackingId", NullValueHandling = NullValueHandling.Ignore)]
        public string TrackingId { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "(TrackingId={0}, Code={1}, Message={2}, Target={3}, ErrorDetails={4}, InnerError={5})",
                this.TrackingId,
                this.Code,
                this.Message,
                this.Target,
                this.ErrorDetails,
                this.InnerError);
        }
    }
}
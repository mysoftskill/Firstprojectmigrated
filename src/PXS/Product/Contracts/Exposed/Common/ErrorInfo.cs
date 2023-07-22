//--------------------------------------------------------------------------------
// <copyright file="ErrorInfo.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Contracts.Exposed
{
    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Error information.
    /// </summary>
    /// <remarks>
    /// TODO Add some way to identify where the error originated (e.g. PartnerId/Component/Actor)
    /// </remarks>
    public class ErrorInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorInfo"/> class.
        /// </summary>
        /// <remarks>
        /// TODO: remove once references are removed
        /// </remarks>
        public ErrorInfo()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorInfo"/> class.
        /// </summary>
        /// <param name="code">The error code.</param>
        /// <param name="message">The error message.</param>
        public ErrorInfo(ErrorCode code, string message)
        {
            this.ErrorCode = code;
            this.ErrorMessage = message;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorInfo" /> class.
        /// </summary>
        /// <param name="code">The error-code.</param>
        /// <param name="message">The error-message.</param>
        /// <param name="innerDetails">The inner-error-details.</param>
        public ErrorInfo(ErrorCode code, string message, InnerErrorDetails innerDetails)
        {
            this.ErrorCode = code;
            this.ErrorMessage = message;
            this.InnerErrorDetails = innerDetails;
        }

        /// <summary>
        /// Gets or sets the error code.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public ErrorCode ErrorCode { get; set; }

        /// <summary>
        /// Error code specific to AMC
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public CancelSubscriptionErrorCode CancelSubscriptionErrorCode { get; set; }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the internal error code set by the underlying system/component.
        /// </summary>
        public long InternalErrorCode { get; set; }

        /// <summary>
        /// Gets the inner-error-details.
        /// </summary>
        [JsonProperty("InnerErrorDetails")]
        public InnerErrorDetails InnerErrorDetails { get; private set; }

        /// <summary>
        /// Returns the ErrorCode and all of the codes nested in within InnerErrorDetails concatenated by periods.
        /// </summary>
        public string FlattenedErrorCode
        {
            get
            {
                if (InnerErrorDetails != null)
                {
                    return string.Join(".", ErrorCode, InnerErrorDetails.FlattenedCode);
                }

                return ErrorCode.ToString();
            }
        }

        /// <summary>
        /// Adds the provided error details to the InnerErrorDetails nested list of this error.
        /// </summary>
        /// <param name="error">Error details to add</param>
        public void AddInnerErrorDetails(InnerErrorDetails error)
        {
            if (InnerErrorDetails != null)
            {
                InnerErrorDetails.AddInnerErrorDetails(error);
            }
            else
            {
                InnerErrorDetails = error;
            }
        }

        /// <summary>
        /// Converts the value of this instance to a <see cref="System.String"/>.
        /// </summary>
        /// <returns>A string whose value is the same as this instance.</returns>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "(ErrorCode={0}, ErrorMessage={1})",
                this.ErrorCode,
                this.ErrorMessage);
        }
    }
}

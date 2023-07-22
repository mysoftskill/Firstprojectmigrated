// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Models
{
    /// <summary>
    ///     Adapter-Error
    /// </summary>
    public class AdapterError
    {
        /// <summary>
        ///     Gets or sets the adapter-error-code.
        /// </summary>
        public AdapterErrorCode Code { get; set; }

        /// <summary>
        ///     Gets or sets the adapter-error-message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        ///     Gets or sets the HTTP status code.
        /// </summary>
        public int StatusCode { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="AdapterError" /> class.
        /// </summary>
        /// <param name="code">The error code.</param>
        /// <param name="message">The error message.</param>
        /// <param name="httpStatusCode">The HTTP status code.</param>
        public AdapterError(AdapterErrorCode code, string message, int httpStatusCode)
        {
            this.Code = code;
            this.Message = message;
            this.StatusCode = httpStatusCode;
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"(Code={this.Code}, Message={this.Message}, StatusCode={this.StatusCode})";
        }
    }
}

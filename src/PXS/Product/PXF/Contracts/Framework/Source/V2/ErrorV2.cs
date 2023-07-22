//--------------------------------------------------------------------------------
// <copyright file="ErrorV2.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Privacy.DataContracts.V2
{
    using Newtonsoft.Json;

    /// <summary>
    /// Error V1
    /// </summary>
    public class ErrorV2
    {
        /// <summary>
        /// Gets or sets the error code.
        /// </summary>
        [JsonProperty("code")]
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the error message. 
        /// This describes the error in details, and provides additional debugging information.
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the target. 
        /// This is what specifically caused the error. This is usually the field in the request that was deemed invalid.
        /// </summary>
        [JsonProperty("target")]
        public string Target { get; set; }

        /// <summary>
        /// Gets or sets the error details.
        /// A verbose detail of the error. Typically, this is an Exception.ToString() output.
        /// </summary>
        [JsonProperty("errorDetails")]
        public string ErrorDetails { get; set; }

        /// <summary>
        /// Gets or sets the inner error.
        /// A nested error of the same schema. Though technically there could be many nested errors, should be limited to a single innerError.
        /// </summary>
        [JsonProperty("innerError")]
        public ErrorV2 InnerError { get; set; }

        /// <summary>
        /// Gets or sets the tracking identifier.
        /// </summary>
        [JsonProperty("trackingId")]
        public string TrackingId { get; set; }
    }
}
// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Models
{

    /// <summary>
    /// AdapterResponse contains a result type T if successful.
    /// </summary>
    public class AdapterResponse<T> : AdapterResponse
    {
        /// <summary>
        /// Gets or sets the result.
        /// </summary>
        public T Result { get; set; }
    }

    public class AdapterResponse
    {
        public AdapterResponse() { }

        public AdapterResponse(AdapterError error)
        {
            Error = error;
        }

        /// <summary>
        /// Gets or sets the error.
        /// </summary>
        public AdapterError Error { get; set; }

        /// <summary>
        /// Gets a value indicating whether this response was a success or failure.
        /// </summary>
        public bool IsSuccess => this.Error == null;
    }
}
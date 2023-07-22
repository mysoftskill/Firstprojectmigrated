// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts
{
    using Newtonsoft.Json;

    /// <summary>
    /// Generic service response consisting of a result or an error.
    /// </summary>
    /// <typeparam name="T">The type of result.</typeparam>
    public class ServiceResponse<T> : ServiceResponse
    {
        /// <summary>
        /// Gets or sets the operation result.
        /// </summary>
        [JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
        public T Result { get; set; }
    }

    /// <summary>
    /// Generic service response consisting of an error if any occurred.
    /// </summary>
    public class ServiceResponse
    {
        /// <summary>
        /// Gets or sets the error.
        /// </summary>
        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
        public Error Error { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this response was a success or failure.
        /// </summary>
        [JsonIgnore]
        public bool IsSuccess
        {
            get { return this.Error == null; }
        }
    }
}
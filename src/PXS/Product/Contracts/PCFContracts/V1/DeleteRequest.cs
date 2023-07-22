// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.PXS.Command.Contracts.V1
{
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Predicates;

    using Newtonsoft.Json;

    /// <summary>
    ///     DeleteRequest
    /// </summary>
    public class DeleteRequest : PrivacyRequest
    {
        /// <summary>
        ///     Gets or sets the predicate for the delete request. Note: this value is not directly used by JSON.NET. All access
        ///     is performed with the "RawPredicate" property.
        /// </summary>
        [JsonProperty("predicate")]
        public IPrivacyPredicate Predicate { get; set; }

        /// <summary>
        ///     Gets or sets the privacy data type for this delete request.
        /// </summary>
        [JsonProperty("dataType")]
        public string PrivacyDataType { get; set; }

        /// <summary>
        ///     Gets or sets the time range predicate for the request.
        /// </summary>
        [JsonProperty("timeRangePredicate", NullValueHandling = NullValueHandling.Ignore)]
        public TimeRangePredicate TimeRangePredicate { get; set; }
    }
}

// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Request body for a creating a delete request
    /// </summary>
    public class DeleteRequestV1
    {
        /// <summary>
        /// Delete all data for the specified resource.
        /// </summary>
        [JsonProperty("deleteAll", NullValueHandling = NullValueHandling.Ignore)]
        public bool? DeleteAll { get; set; }

        /// <summary>
        /// First date of a date range deletion request (or the single date, if DateRangeEnd is not specified). Format 'YYYY-MM-DD'.
        /// </summary>
        [JsonProperty("dateRangeStart", NullValueHandling = NullValueHandling.Ignore)]
        public string DateRangeStart { get; set; }

        /// <summary>
        /// Last date (inclusive) of a date range deletion request (for single dates, only use DateRangeStart). Format 'YYYY-MM-DD'.
        /// </summary>
        [JsonProperty("dateRangeEnd", NullValueHandling = NullValueHandling.Ignore)]
        public string DateRangeEnd { get; set; }

        /// <summary>
        /// List of resource Ids to be deleted
        /// </summary>
        [JsonProperty("resourceIds", NullValueHandling = NullValueHandling.Ignore)]
        public IEnumerable<string> ResourceIds { get; set; }
    }
}
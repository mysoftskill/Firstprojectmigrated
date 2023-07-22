// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Location History V1
    /// </summary>
    public class LocationHistoryV1 : LocationV1
    {
        /// <summary>
        /// Gets or sets the aggregate history.
        /// </summary>
        [JsonProperty("aggregateHistory")]
        public IEnumerable<LocationV1> AggregateHistory { get; set; } 
    }
}
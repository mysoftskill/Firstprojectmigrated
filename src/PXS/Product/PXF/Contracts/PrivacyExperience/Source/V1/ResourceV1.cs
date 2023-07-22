// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// ResourceV1
    /// </summary>
    public abstract class ResourceV1
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        [JsonIgnore]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the ids.
        /// </summary>
        [JsonProperty("ids")]
        public IList<string> Ids { get; set; }

        /// <summary>
        /// Gets or sets the date time.
        /// </summary>
        [JsonProperty("dateTime")]
        public DateTimeOffset DateTime { get; set; }

        /// <summary>
        /// Gets or sets the device identifier.
        /// </summary>
        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }

        /// <summary>
        /// Gets or sets the source.
        /// </summary>
        [JsonProperty("source")]
        public string Source { get; set; }

        /// <summary>
        /// Gets or sets the partner identifier.
        /// </summary>
        [JsonProperty("partnerId")]
        public string PartnerId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is an aggregate resource.
        /// </summary>
        [JsonProperty("isAggregate")]
        public bool IsAggregate { get; set; }
    }
}